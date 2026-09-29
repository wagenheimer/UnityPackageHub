using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Wagenheimer.PackageHub.Editor
{
    /// <summary>
    /// Manages Google dependencies (EDM4U, Play Common, Play Core, Play Review)
    /// required by <c>com.wagenheimer.ratecontrol</c> and other Android packages.
    ///
    /// <para>
    /// Migrates projects away from Scoped Registries (which cause Unity "unverified package" warnings)
    /// and stale/legacy asset folders to the official, verified Git UPM repositories.
    /// Also allows detecting and fetching the latest updates directly from GitHub.
    /// </para>
    /// </summary>
    public static class GoogleDependencyManager
    {
        public const string RateControlPackageId = "com.wagenheimer.ratecontrol";
        public const string ManifestPath = "Packages/manifest.json";
        public const string LockfilePath = "Packages/packages-lock.json";

        public sealed class GooglePackageDef
        {
            public string Id { get; }
            public string DisplayName { get; }
            public string RecommendedGitUrl { get; }
            public string GitHubRepo { get; }
            public string Branch { get; }

            public GooglePackageDef(string id, string displayName, string gitUrl, string gitHubRepo, string branch = "master")
            {
                Id = id;
                DisplayName = displayName;
                RecommendedGitUrl = gitUrl;
                GitHubRepo = gitHubRepo;
                Branch = branch;
            }
        }

        public static readonly List<GooglePackageDef> RequiredPackages = new List<GooglePackageDef>
        {
            new GooglePackageDef(
                "com.google.external-dependency-manager",
                "External Dependency Manager (EDM4U)",
                "https://github.com/googlesamples/unity-jar-resolver.git?path=upm",
                "googlesamples/unity-jar-resolver",
                "master"
            ),
            new GooglePackageDef(
                "com.google.play.common",
                "Google Play Common",
                "https://github.com/google/play-common-unity.git?path=com.google.play.common",
                "google/play-common-unity",
                "master"
            ),
            new GooglePackageDef(
                "com.google.play.core",
                "Google Play Core",
                "https://github.com/google/play-core-unity.git?path=com.google.play.core",
                "google/play-core-unity",
                "master"
            ),
            new GooglePackageDef(
                "com.google.play.review",
                "Google Play In-App Review",
                "https://github.com/google/play-in-app-reviews-unity.git?path=com.google.play.review",
                "google/play-in-app-reviews-unity",
                "master"
            )
        };

        public sealed class Diagnosis
        {
            public bool IsRateControlInstalled;
            public bool HasScopedRegistriesWarning;
            public List<string> ConflictingScopedRegistryNames = new List<string>();
            public bool HasMissingPackages;
            public List<string> MissingPackageIds = new List<string>();
            public bool HasNonGitInstallations;
            public List<string> NonGitPackageIds = new List<string>();
            public bool HasLegacyAssetFolders;
            public List<string> LegacyAssetFolderPaths = new List<string>();
            public bool IsFullyCompliant;
            public bool NeedsMigration;
            public List<string> Issues = new List<string>();

            public string Summary
            {
                get
                {
                    if (!IsRateControlInstalled)
                        return "RateControl is not installed in this project.";
                    if (IsFullyCompliant)
                        return "All Google packages are configured via clean official Git repositories (no warnings).";
                    if (NeedsMigration)
                        return "Action recommended: Google dependencies use Scoped Registries or are incomplete/outdated.";
                    return "Google dependencies are healthy.";
                }
            }
        }

        public static event Action OnDependenciesChanged;

        // ── Detection ─────────────────────────────────────────────────────────────

        public static Diagnosis Detect()
        {
            var d = new Diagnosis();

            // 1. Check if RateControl is installed or declared
            d.IsRateControlInstalled = IsPackageInstalledOrDeclared(RateControlPackageId);
            if (!d.IsRateControlInstalled)
            {
                d.IsFullyCompliant = true;
                return d;
            }

            var manifestText = ReadFileSafe(ManifestPath);
            if (string.IsNullOrEmpty(manifestText))
            {
                d.Issues.Add("Could not read Packages/manifest.json");
                return d;
            }

            // 2. Check for Scoped Registries referencing Google packages
            CheckScopedRegistries(manifestText, d);

            // 3. Check for presence and Git configuration of required Google packages
            foreach (var req in RequiredPackages)
            {
                var isDeclared = Regex.IsMatch(manifestText, $"\"{Regex.Escape(req.Id)}\"\\s*:");
                if (!isDeclared)
                {
                    d.HasMissingPackages = true;
                    d.MissingPackageIds.Add(req.Id);
                    d.Issues.Add($"Missing package: {req.DisplayName} ({req.Id})");
                }
                else
                {
                    // Check if installed with recommended git URL
                    var matchUrl = Regex.Match(manifestText, $"\"{Regex.Escape(req.Id)}\"\\s*:\\s*\"([^\"]+)\"");
                    if (matchUrl.Success)
                    {
                        var val = matchUrl.Groups[1].Value.Trim();
                        if (!val.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) &&
                            !val.StartsWith("git://", StringComparison.OrdinalIgnoreCase) &&
                            !val.StartsWith("git+", StringComparison.OrdinalIgnoreCase))
                        {
                            d.HasNonGitInstallations = true;
                            d.NonGitPackageIds.Add(req.Id);
                            d.Issues.Add($"Package '{req.Id}' is using registry version '{val}' instead of clean Git URL.");
                        }
                    }
                }
            }

            // 4. Check for obsolete legacy asset folders
            string[] legacyDirs = { "Assets/PlayServicesResolver", "Assets/ExternalDependencyManager" };
            foreach (var ldir in legacyDirs)
            {
                if (Directory.Exists(ldir))
                {
                    d.HasLegacyAssetFolders = true;
                    d.LegacyAssetFolderPaths.Add(ldir);
                    d.Issues.Add($"Legacy asset folder found: '{ldir}' (superseded by EDM4U UPM package).");
                }
            }

            // Overall compliance
            d.NeedsMigration = d.HasScopedRegistriesWarning || d.HasMissingPackages || d.HasNonGitInstallations || d.HasLegacyAssetFolders;
            d.IsFullyCompliant = !d.NeedsMigration;

            return d;
        }

        private static void CheckScopedRegistries(string manifestText, Diagnosis d)
        {
            // Scoped registries block check
            var matchScopes = Regex.Matches(manifestText, @"\{\s*""name""\s*:\s*""([^""]+)"".*?""scopes""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
            foreach (Match m in matchScopes)
            {
                var regName = m.Groups[1].Value;
                var scopesContent = m.Groups[2].Value;

                foreach (var req in RequiredPackages)
                {
                    if (scopesContent.Contains($"\"{req.Id}\""))
                    {
                        d.HasScopedRegistriesWarning = true;
                        if (!d.ConflictingScopedRegistryNames.Contains(regName))
                            d.ConflictingScopedRegistryNames.Add(regName);
                    }
                }
            }

            if (d.HasScopedRegistriesWarning)
            {
                var regList = string.Join(", ", d.ConflictingScopedRegistryNames);
                d.Issues.Add($"Scoped registry ({regList}) includes Google packages. This triggers Unity 'Unverified Package' warnings.");
            }
        }

        // ── Migration ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Migrates the project to the recommended clean Git setup:
        /// 1. Strips Google package scopes from scopedRegistries in manifest.json (or deletes empty registry).
        /// 2. Injects or updates all 4 required Google packages with official Git URLs.
        /// 3. Cleans lockfile entries for those 4 packages so Unity downloads the latest commits.
        /// 4. Optionally moves legacy asset folders to trash.
        /// 5. Triggers UPM Resolve.
        /// </summary>
        public static bool MigrateToRecommended(bool forceRefreshLockfile = true, Action<bool, string> onComplete = null)
        {
            try
            {
                if (!File.Exists(ManifestPath))
                {
                    onComplete?.Invoke(false, "Packages/manifest.json not found.");
                    return false;
                }

                var manifestContent = File.ReadAllText(ManifestPath);

                // 1. Remove Google scopes from scopedRegistries
                manifestContent = CleanScopedRegistries(manifestContent);

                // 2. Ensure all Google packages are in dependencies with recommended Git URLs
                manifestContent = EnsureDependencies(manifestContent);

                // Write updated manifest
                File.WriteAllText(ManifestPath, manifestContent);
                Debug.Log("[GoogleDependencyManager] Packages/manifest.json successfully updated with recommended Git URLs.");

                // 3. Clean lockfile entries if requested to force pulling newest git commits
                if (forceRefreshLockfile && File.Exists(LockfilePath))
                {
                    var lockContent = File.ReadAllText(LockfilePath);
                    lockContent = CleanLockfileEntries(lockContent);
                    File.WriteAllText(LockfilePath, lockContent);
                    Debug.Log("[GoogleDependencyManager] Packages/packages-lock.json cleaned to fetch latest Git commits.");
                }

                // 4. Move legacy folders to Trash
                string[] legacyDirs = { "Assets/PlayServicesResolver", "Assets/ExternalDependencyManager" };
                foreach (var ldir in legacyDirs)
                {
                    if (Directory.Exists(ldir))
                    {
                        AssetDatabase.MoveAssetToTrash(ldir);
                        Debug.Log($"[GoogleDependencyManager] Moved obsolete legacy folder '{ldir}' to Trash.");
                    }
                }

                // 5. Trigger Unity Package Manager resolution
                Client.Resolve();
                AssetDatabase.Refresh();

                OnDependenciesChanged?.Invoke();
                onComplete?.Invoke(true, "Successfully migrated Google dependencies to recommended Git setup.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoogleDependencyManager] Migration failed: {ex}");
                onComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        private static string CleanScopedRegistries(string json)
        {
            var googleIds = RequiredPackages.Select(p => p.Id).ToHashSet();

            // Match each registry object inside scopedRegistries array
            var regMatch = Regex.Match(json, @"""scopedRegistries""\s*:\s*\[(.*?)\]\s*,", RegexOptions.Singleline);
            if (!regMatch.Success) return json;

            var fullBlock = regMatch.Groups[0].Value;
            var arrayBody = regMatch.Groups[1].Value;

            // Split into individual registry objects
            var objectMatches = Regex.Matches(arrayBody, @"\{[^{}]*\}", RegexOptions.Singleline);
            var keptRegistries = new List<string>();

            foreach (Match objMatch in objectMatches)
            {
                var objText = objMatch.Value;
                var scopesMatch = Regex.Match(objText, @"""scopes""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);

                if (scopesMatch.Success)
                {
                    var scopesBody = scopesMatch.Groups[1].Value;
                    var scopeItems = Regex.Matches(scopesBody, @"""([^""]+)""")
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .Where(s => !googleIds.Contains(s))
                        .ToList();

                    if (scopeItems.Count > 0)
                    {
                        // Registry still has non-google scopes, keep it with remaining scopes
                        var newScopes = string.Join(",\n        ", scopeItems.Select(s => $"\"{s}\""));
                        var newObjText = Regex.Replace(objText, @"""scopes""\s*:\s*\[.*?\]", $"\"scopes\": [\n        {newScopes}\n      ]", RegexOptions.Singleline);
                        keptRegistries.Add(newObjText);
                    }
                    // If no scopes left, registry is removed completely!
                }
                else
                {
                    keptRegistries.Add(objText);
                }
            }

            if (keptRegistries.Count > 0)
            {
                var newArrayBody = string.Join(",\n    ", keptRegistries);
                var replacement = $"\"scopedRegistries\": [\n    {newArrayBody}\n  ],";
                return json.Replace(fullBlock, replacement);
            }
            else
            {
                // All scoped registries were Google-only, remove the entire scopedRegistries field!
                return json.Replace(fullBlock, "");
            }
        }

        private static string EnsureDependencies(string json)
        {
            var depsMatch = Regex.Match(json, @"""dependencies""\s*:\s*\{(.*?)\}", RegexOptions.Singleline);
            if (!depsMatch.Success) return json;

            var depsBody = depsMatch.Groups[1].Value;

            foreach (var req in RequiredPackages)
            {
                var pkgPattern = $"\"{Regex.Escape(req.Id)}\"\\s*:\\s*\"[^\"]*\"";
                var cleanEntry = $"\"{req.Id}\": \"{req.RecommendedGitUrl}\"";

                if (Regex.IsMatch(depsBody, pkgPattern))
                {
                    // Replace existing entry
                    depsBody = Regex.Replace(depsBody, pkgPattern, cleanEntry);
                }
                else
                {
                    // Prepend new entry
                    depsBody = $"\n    {cleanEntry}," + depsBody;
                }
            }

            var replacement = $"\"dependencies\": {{{depsBody}}}";
            return json.Substring(0, depsMatch.Index) + replacement + json.Substring(depsMatch.Index + depsMatch.Length);
        }

        private static string CleanLockfileEntries(string json)
        {
            foreach (var req in RequiredPackages)
            {
                json = RemoveJsonEntry(json, req.Id);
            }
            return json;
        }

        /// <summary>
        /// Removes a top-level "key": { ... } entry (plus its trailing comma) from a JSON object body,
        /// correctly honoring brace nesting depth. A naive non-greedy regex (\{.*?\}) stops at the first
        /// '}' it encounters, which is the closing brace of a nested object (e.g. "dependencies": {}),
        /// not the entry's own closing brace — silently truncating the entry and corrupting the file.
        /// </summary>
        private static string RemoveJsonEntry(string json, string key)
        {
            var keyMatch = Regex.Match(json, $"\"{Regex.Escape(key)}\"\\s*:\\s*\\{{");
            if (!keyMatch.Success) return json;

            var braceStart = keyMatch.Index + keyMatch.Length - 1; // index of the opening '{'
            var depth = 0;
            var braceEnd = -1;
            for (var i = braceStart; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        braceEnd = i;
                        break;
                    }
                }
            }

            if (braceEnd < 0) return json; // unbalanced braces; leave untouched rather than corrupt

            var entryEnd = braceEnd + 1;
            // Also consume a trailing comma (and any whitespace before it) so we don't leave a dangling ",".
            var afterEntry = entryEnd;
            while (afterEntry < json.Length && char.IsWhiteSpace(json[afterEntry])) afterEntry++;
            if (afterEntry < json.Length && json[afterEntry] == ',') afterEntry++;

            return json.Substring(0, keyMatch.Index) + json.Substring(afterEntry);
        }

        // ── Update Checker ─────────────────────────────────────────────────────────

        /// <summary>
        /// Forces Unity to pull latest commits for Google packages by stripping their lockfile hashes and running Resolve.
        /// </summary>
        public static void ForceUpdateGooglePackages(Action<bool, string> onComplete = null)
        {
            try
            {
                if (File.Exists(LockfilePath))
                {
                    var lockContent = File.ReadAllText(LockfilePath);
                    lockContent = CleanLockfileEntries(lockContent);
                    File.WriteAllText(LockfilePath, lockContent);
                    Debug.Log("[GoogleDependencyManager] Removed lockfile hashes. Resolving UPM to pull latest git commits...");
                }

                Client.Resolve();
                AssetDatabase.Refresh();

                OnDependenciesChanged?.Invoke();
                onComplete?.Invoke(true, "Triggered update: Unity is resolving the newest Git commits for Google dependencies.");
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(false, ex.Message);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private static bool IsPackageInstalledOrDeclared(string packageId)
        {
            try
            {
                var registered = PackageInfo.GetAllRegisteredPackages();
                if (registered != null && registered.Any(p => p.name == packageId))
                    return true;
            }
            catch { }

            var manifest = ReadFileSafe(ManifestPath);
            return !string.IsNullOrEmpty(manifest) && manifest.Contains($"\"{packageId}\"");
        }

        private static string ReadFileSafe(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
