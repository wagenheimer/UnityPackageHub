using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Wagenheimer.PackageHub.Editor
{
    public static class PackageDiscovery
    {
        public const string PackagePrefix = "com.wagenheimer.";

        public static List<PackageItem> GetAllPackages()
        {
            var result = new List<PackageItem>();

            // 1. Copy known catalog items
            foreach (var known in PackageCatalog.KnownPackages)
            {
                result.Add(new PackageItem
                {
                    PackageId = known.PackageId,
                    DisplayName = known.DisplayName,
                    Description = known.Description,
                    RepoUrl = known.RepoUrl,
                    GitUrl = known.GitUrl,
                    DefaultBranch = known.DefaultBranch,
                    Category = known.Category,
                    IsInstalled = false
                });
            }

            // 2. Discover installed packages via Unity Package Manager
            try
            {
                var registeredPackages = PackageInfo.GetAllRegisteredPackages();
                foreach (var pkg in registeredPackages)
                {
                    if (string.IsNullOrEmpty(pkg.name) || !pkg.name.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var item = result.FirstOrDefault(x => string.Equals(x.PackageId, pkg.name, StringComparison.OrdinalIgnoreCase));
                    if (item == null)
                    {
                        // Unknown or new Wagenheimer package
                        var gitUrl = pkg.packageId;
                        if (gitUrl.Contains("@"))
                        {
                            var parts = gitUrl.Split('@');
                            gitUrl = parts.Length > 1 ? parts[1] : gitUrl;
                        }
                        if (gitUrl.Contains("#"))
                        {
                            gitUrl = gitUrl.Substring(0, gitUrl.IndexOf('#'));
                        }

                        var displayName = string.IsNullOrEmpty(pkg.displayName) ? pkg.name.Replace(PackagePrefix, "") : pkg.displayName;
                        var repoName = displayName.Replace(" ", "");
                        var repoUrl = $"https://github.com/wagenheimer/{repoName}";

                        item = new PackageItem
                        {
                            PackageId = pkg.name,
                            DisplayName = displayName,
                            Description = pkg.description ?? "Wagenheimer Package",
                            RepoUrl = repoUrl,
                            GitUrl = gitUrl.EndsWith(".git") ? gitUrl : gitUrl + ".git",
                            DefaultBranch = "main",
                            Category = "Installed",
                            IsInstalled = true
                        };
                        result.Add(item);
                    }
                    else
                    {
                        item.IsInstalled = true;
                    }

                    item.InstalledVersion = CleanVersion(pkg.version);

                    // If git url in packageId has specific repo, update it
                    if (!string.IsNullOrEmpty(pkg.packageId) && pkg.packageId.Contains("github.com/wagenheimer/"))
                    {
                        var rawGit = pkg.packageId.Substring(pkg.packageId.IndexOf("https://", StringComparison.OrdinalIgnoreCase));
                        if (rawGit.Contains("#"))
                            rawGit = rawGit.Substring(0, rawGit.IndexOf('#'));
                        item.GitUrl = rawGit;
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[Wagenheimer.PackageHub] Error during package discovery: {ex.Message}");
            }

            return result;
        }

        public static string CleanVersion(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Trim().TrimStart('v', 'V');
        }
    }
}
