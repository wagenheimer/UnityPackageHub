using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Wagenheimer.PackageHub.Editor
{
    internal enum I2MigrationOutcome
    {
        NotNeeded,
        Migrated,
        Cancelled,
        Failed
    }

    internal sealed class LegacyFolder
    {
        public string AssetPath;
        public string AbsolutePath;
        public int FileCount;
        public bool IsToolkit;
    }

    internal sealed class MigrationReport
    {
        public readonly List<string> Steps = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Errors = new List<string>();
        public string BackupFolder;
        public bool Success => Errors.Count == 0;

        public void Step(string message)
        {
            Steps.Add(message);
            Debug.Log("[PackageHub] " + message);
        }

        public void Warn(string message)
        {
            Warnings.Add(message);
            Debug.LogWarning("[PackageHub] " + message);
        }

        public void Error(string message)
        {
            Errors.Add(message);
            Debug.LogError("[PackageHub] " + message);
        }

        public string ToText()
        {
            var builder = new StringBuilder();
            if (BackupFolder != null) builder.AppendLine("Backup: " + BackupFolder);
            foreach (string step in Steps) builder.AppendLine("• " + step);
            foreach (string warning in Warnings) builder.AppendLine("! " + warning);
            foreach (string error in Errors) builder.AppendLine("x " + error);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Migrates a legacy <c>Assets/I2</c> install before the Hub installs/updates
    /// <c>com.wagenheimer.i2localization</c>.
    ///
    /// This must happen outside the package: the legacy source and the package define the same
    /// <c>I2.Loc</c> types, so adding the package first breaks compilation and no code from the
    /// package (or any script that needs the package) can run to fix it. The Hub is already
    /// loaded and compiling, so it is the safe place for the preflight.
    /// </summary>
    internal static class I2LegacyMigration
    {
        public const string PackageId = "com.wagenheimer.i2localization";
        private const string MigratorId = "com.wagenheimer.i2localization.migrator";
        private const string BackupFolderName = "I2MigrationBackup";

        public static I2MigrationOutcome Prepare(PackageItem item)
        {
            if (item == null || !string.Equals(item.PackageId, PackageId, StringComparison.OrdinalIgnoreCase))
                return I2MigrationOutcome.NotNeeded;

            var legacyRoots = FindLegacyRoots();
            var toolkitFolders = FindToolkitFolders(legacyRoots);
            if (legacyRoots.Count == 0 && toolkitFolders.Count == 0)
                return I2MigrationOutcome.NotNeeded;

            if (!Application.isBatchMode)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "I2 Localization",
                    "This project still has the old I2 source under Assets/I2.\n\n" +
                    "The package cannot be installed while both copies exist (they define the same I2.Loc types).\n\n" +
                    "The Hub will back it up to " + BackupFolderName + "/, move I2Languages to Assets/Resources " +
                    "and delete only the old I2 scripts. Continue?",
                    "Migrate and install",
                    "Cancel");

                if (!proceed)
                    return I2MigrationOutcome.Cancelled;
            }

            var report = Run(legacyRoots, toolkitFolders);
            if (!report.Success)
            {
                Debug.LogError("[PackageHub] The legacy I2 migration failed:\n" + report.ToText());
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "I2 Localization",
                        "The migration did not finish, so the package was not installed.\n\n" + report.ToText(),
                        "OK");
                }

                return I2MigrationOutcome.Failed;
            }

            Debug.Log("[PackageHub] Legacy I2 migrated before installing the package:\n" + report.ToText());
            return I2MigrationOutcome.Migrated;
        }

        // -- migration ------------------------------------------------------

        private static MigrationReport Run(IReadOnlyList<LegacyFolder> legacyRoots, IReadOnlyList<LegacyFolder> toolkitFolders)
        {
            var report = new MigrationReport();
            var folders = legacyRoots.Concat(toolkitFolders).ToList();
            var languageSources = FindLanguageSources(folders);

            try
            {
                report.BackupFolder = CreateBackup(folders);
                report.Step("Backup created at " + report.BackupFolder + ".");
                if (EnsureGitIgnoreEntry(BackupFolderName + "/"))
                    report.Step("Added " + BackupFolderName + "/ to .gitignore so the backup is not committed.");
            }
            catch (Exception exception)
            {
                report.Error("Backup failed, migration aborted to avoid data loss: " + exception.Message);
                return report;
            }

            foreach (string source in languageSources)
                PreserveLanguageSource(source, report);

            AssetDatabase.Refresh();

            foreach (var folder in folders)
                DeleteFolder(folder, report);

            AssetDatabase.Refresh();
            RemoveDeprecatedMigrator(report);

            report.Step("Legacy I2 removed. The packaged com.wagenheimer.i2localization can be installed.");
            return report;
        }

        private static string CreateBackup(IReadOnlyList<LegacyFolder> folders)
        {
            string projectRoot = ProjectRoot;
            string folder = Path.Combine(projectRoot, BackupFolderName, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(folder);

            CopyFileIfExists(ManifestPath, Path.Combine(folder, "manifest.json"));
            CopyFileIfExists(Path.Combine(projectRoot, "Packages", "packages-lock.json"),
                Path.Combine(folder, "packages-lock.json"));

            foreach (var legacy in folders)
                CopyDirectory(legacy.AbsolutePath, Path.Combine(folder, legacy.AssetPath.Replace('/', Path.DirectorySeparatorChar)));

            return folder;
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relative = dir.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar);
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target) ?? destination);
                File.Copy(file, target, true);
            }
        }

        private static void CopyFileIfExists(string source, string destination)
        {
            if (!File.Exists(source)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? ".");
            File.Copy(source, destination, true);
        }

        private static bool EnsureGitIgnoreEntry(string entry)
        {
            try
            {
                string path = Path.Combine(ProjectRoot, ".gitignore");
                if (!File.Exists(path)) return false;

                string text = File.ReadAllText(path);
                foreach (string line in text.Split('\n'))
                {
                    if (string.Equals(line.Trim().TrimEnd('/'), entry.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                string suffix = text.EndsWith("\n", StringComparison.Ordinal) ? string.Empty : "\n";
                File.AppendAllText(path, suffix + entry + "\n");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void PreserveLanguageSource(string assetAbsolutePath, MigrationReport report)
        {
            string assetsRoot = AssetsRoot.TrimEnd('/');
            string normalized = assetAbsolutePath.Replace('\\', '/');
            if (!normalized.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
                return;

            string fileName = Path.GetFileName(normalized);
            string resources = Path.Combine(assetsRoot, "Resources");
            string destination = Path.Combine(resources, fileName);

            if (File.Exists(destination))
            {
                report.Warn("Assets/Resources/" + fileName + " already exists, so the legacy copy stayed in the backup. " +
                            "The existing asset was kept.");
                return;
            }

            Directory.CreateDirectory(resources);

            try
            {
                File.Move(normalized, destination);
                if (File.Exists(normalized + ".meta"))
                    File.Move(normalized + ".meta", destination + ".meta");

                report.Step("Moved " + fileName + " to Assets/Resources so I2 keeps loading it.");
            }
            catch (Exception exception)
            {
                report.Error("Could not move " + fileName + " to Assets/Resources: " + exception.Message);
            }
        }

        private static void DeleteFolder(LegacyFolder folder, MigrationReport report)
        {
            try
            {
                bool deleted = AssetDatabase.DeleteAsset(folder.AssetPath);
                if (!deleted && Directory.Exists(folder.AbsolutePath))
                {
                    Directory.Delete(folder.AbsolutePath, true);
                    deleted = true;
                }

                if (deleted)
                {
                    report.Step("Deleted " + folder.AssetPath + " (" + folder.FileCount + " files).");
                    PruneEmptyParent(folder, report);
                }
                else
                {
                    report.Error("Could not delete " + folder.AssetPath + ".");
                }
            }
            catch (Exception exception)
            {
                report.Error("Could not delete " + folder.AssetPath + ": " + exception.Message);
            }
        }

        private static void PruneEmptyParent(LegacyFolder folder, MigrationReport report)
        {
            try
            {
                string parent = Path.GetDirectoryName(folder.AbsolutePath);
                if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                    return;

                bool hasFolders = Directory.GetDirectories(parent).Length > 0;
                bool hasFiles = Directory.GetFiles(parent)
                    .Any(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));

                if (hasFolders || hasFiles)
                    return;

                string parentAssetPath = ToAssetPath(parent);
                if (string.IsNullOrEmpty(parentAssetPath) ||
                    string.Equals(parentAssetPath, "Assets", StringComparison.OrdinalIgnoreCase))
                    return;

                if (AssetDatabase.DeleteAsset(parentAssetPath))
                    report.Step("Removed the now empty " + parentAssetPath + ".");
            }
            catch
            {
                // Cosmetic cleanup only.
            }
        }

        private static void RemoveDeprecatedMigrator(MigrationReport report)
        {
            try
            {
                string path = ManifestPath;
                if (!File.Exists(path)) return;

                string text = File.ReadAllText(path);
                var pattern = new Regex("\\s*\"" + Regex.Escape(MigratorId) + "\"\\s*:\\s*\"[^\"]*\"\\s*,?");
                if (!pattern.IsMatch(text)) return;

                text = pattern.Replace(text, string.Empty);
                text = Regex.Replace(text, "\\{\\s*,", "{");
                text = Regex.Replace(text, ",\\s*\\}", "}");
                File.WriteAllText(path, text, new UTF8Encoding(false));
                report.Step("Removed the deprecated migrator package from Packages/manifest.json.");
            }
            catch
            {
                report.Warn("Could not remove " + MigratorId + " from the manifest; remove it by hand.");
            }
        }

        // -- detection ------------------------------------------------------

        private static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName;

        private static string AssetsRoot => Application.dataPath.Replace('\\', '/');

        private static string ManifestPath => Path.Combine(ProjectRoot ?? string.Empty, "Packages", "manifest.json");

        private static List<LegacyFolder> FindLegacyRoots()
        {
            var result = new List<LegacyFolder>();

            foreach (string script in EnumerateFiles("LocalizationManager.cs"))
            {
                if (!FileContains(script, "namespace I2.Loc", 6000)) continue;

                string root = ResolveLegacyRoot(script);
                if (!string.IsNullOrEmpty(root)) AddFolder(result, root, false);
            }

            foreach (string dir in EnumerateDirectories("I2"))
            {
                if (File.Exists(Path.Combine(dir, "Localization", "Scripts", "Manager", "LocalizationManager.cs")))
                    AddFolder(result, Path.Combine(dir, "Localization"), false);
            }

            return result;
        }

        private static List<LegacyFolder> FindToolkitFolders(IEnumerable<LegacyFolder> legacyRoots)
        {
            var roots = legacyRoots.Select(r => r.AbsolutePath.Replace('\\', '/')).ToList();
            var result = new List<LegacyFolder>();

            foreach (string script in EnumerateFiles("LocalizationToolkitWindow.cs"))
            {
                string dir = Path.GetDirectoryName(script)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(dir)) continue;
                if (roots.Any(r => dir.StartsWith(r + "/", StringComparison.OrdinalIgnoreCase))) continue;
                if (!FileContains(script, "class LocalizationToolkitWindow", 4000)) continue;

                AddFolder(result, dir, true);
            }

            return result;
        }

        private static List<string> FindLanguageSources(IEnumerable<LegacyFolder> folders)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots = folders.Select(f => f.AbsolutePath).ToList();

            foreach (string file in SafeEnumerate(AssetsRoot, "*.asset"))
            {
                string name = Path.GetFileName(file);
                if (!string.Equals(name, "I2Languages.asset", StringComparison.OrdinalIgnoreCase) &&
                    !FileContains(file, "mTerms:", 200000))
                    continue;

                if (roots.Any(root => file.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)))
                    result.Add(file.Replace('\\', '/'));
            }

            return result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void AddFolder(List<LegacyFolder> list, string absolutePath, bool isToolkit)
        {
            string normalized = absolutePath.Replace('\\', '/').TrimEnd('/');
            if (list.Any(f => string.Equals(f.AbsolutePath, normalized, StringComparison.OrdinalIgnoreCase)))
                return;

            string assetPath = ToAssetPath(normalized);
            if (string.IsNullOrEmpty(assetPath) || string.Equals(assetPath, "Assets", StringComparison.OrdinalIgnoreCase))
                return;

            list.Add(new LegacyFolder
            {
                AbsolutePath = normalized,
                AssetPath = assetPath,
                IsToolkit = isToolkit,
                FileCount = Directory.Exists(normalized)
                    ? Directory.GetFiles(normalized, "*", SearchOption.AllDirectories).Length
                    : 0
            });
        }

        private static string ResolveLegacyRoot(string scriptPath)
        {
            string normalized = scriptPath.Replace('\\', '/');
            string[] parts = normalized.Split('/');

            int index = -1;
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (string.Equals(parts[i], "Localization", StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            return index <= 0 ? null : string.Join("/", parts.Take(index + 1));
        }

        private static string ToAssetPath(string absolutePath)
        {
            string assets = AssetsRoot.TrimEnd('/');
            string path = absolutePath.Replace('\\', '/');
            if (!path.StartsWith(assets, StringComparison.OrdinalIgnoreCase)) return null;
            return "Assets" + path.Substring(assets.Length);
        }

        private static IEnumerable<string> EnumerateFiles(string fileName) =>
            SafeEnumerate(AssetsRoot, fileName)
                .Where(f => f.IndexOf("/Library/", StringComparison.OrdinalIgnoreCase) < 0 &&
                            f.IndexOf("/Temp/", StringComparison.OrdinalIgnoreCase) < 0);

        private static IEnumerable<string> SafeEnumerate(string root, string pattern)
        {
            if (!Directory.Exists(root)) yield break;

            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string current = pending.Pop();

                string[] files;
                try { files = Directory.GetFiles(current, pattern); }
                catch { files = Array.Empty<string>(); }

                foreach (string file in files)
                    yield return file.Replace('\\', '/');

                string[] dirs;
                try { dirs = Directory.GetDirectories(current); }
                catch { dirs = Array.Empty<string>(); }

                foreach (string dir in dirs)
                {
                    string name = Path.GetFileName(dir);
                    if (name == "." || name == ".." || name.EndsWith("~", StringComparison.Ordinal))
                        continue;
                    pending.Push(dir);
                }
            }
        }

        private static IEnumerable<string> EnumerateDirectories(string name)
        {
            var pending = new Stack<string>();
            pending.Push(AssetsRoot);

            while (pending.Count > 0)
            {
                string current = pending.Pop();

                string[] dirs;
                try { dirs = Directory.GetDirectories(current); }
                catch { dirs = Array.Empty<string>(); }

                foreach (string dir in dirs)
                {
                    string currentName = Path.GetFileName(dir);
                    if (string.Equals(currentName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return dir;
                        continue;
                    }

                    if (currentName.EndsWith("~", StringComparison.Ordinal))
                        continue;

                    pending.Push(dir);
                }
            }
        }

        private static bool FileContains(string path, string token, int maxBytes)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new StreamReader(stream);
                var buffer = new char[Math.Min(maxBytes, 65536)];
                var builder = new StringBuilder();
                int remaining = maxBytes;
                int read;

                while (remaining > 0 && (read = reader.Read(buffer, 0, Math.Min(buffer.Length, remaining))) > 0)
                {
                    builder.Append(buffer, 0, read);
                    remaining -= read;
                }

                return builder.ToString().IndexOf(token, StringComparison.Ordinal) >= 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
