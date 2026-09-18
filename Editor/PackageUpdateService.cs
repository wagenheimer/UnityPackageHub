using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Wagenheimer.PackageHub.Editor
{
    public static class PackageUpdateService
    {
        [Serializable]
        private class RemotePackageJson
        {
            public string version;
        }

        public static void CheckUpdates(List<PackageItem> packages, Action onComplete = null)
        {
            if (packages == null || packages.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int remaining = packages.Count;

            foreach (var item in packages)
            {
                item.IsChecking = true;
                item.UpdateError = null;

                var url = item.GetRawPackageJsonUrl();
                var req = UnityWebRequest.Get(url);
                var op = req.SendWebRequest();

                op.completed += _ =>
                {
                    try
                    {
                        if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler?.text))
                        {
                            var json = JsonUtility.FromJson<RemotePackageJson>(req.downloadHandler.text);
                            if (json != null && !string.IsNullOrEmpty(json.version))
                            {
                                var remoteVer = PackageDiscovery.CleanVersion(json.version);
                                item.LatestRemoteVersion = remoteVer;

                                if (item.IsInstalled && !string.IsNullOrEmpty(item.InstalledVersion))
                                {
                                    item.HasUpdate = IsNewerVersion(remoteVer, item.InstalledVersion);
                                }
                                else
                                {
                                    item.HasUpdate = false;
                                }

                                if (item.HasUpdate)
                                {
                                    // Fetch Changelog
                                    FetchChangelog(item, () =>
                                    {
                                        item.IsChecking = false;
                                        remaining--;
                                        if (remaining <= 0) onComplete?.Invoke();
                                    });
                                    req.Dispose();
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        item.UpdateError = ex.Message;
                    }
                    finally
                    {
                        req.Dispose();
                    }

                    item.IsChecking = false;
                    remaining--;
                    if (remaining <= 0)
                    {
                        onComplete?.Invoke();
                    }
                };
            }
        }

        private static void FetchChangelog(PackageItem item, Action onDone)
        {
            var url = item.GetRawChangelogUrl();
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();

            op.completed += _ =>
            {
                try
                {
                    if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler?.text))
                    {
                        item.ReleaseNotes = ExtractVersionNotes(req.downloadHandler.text, item.LatestRemoteVersion, item.InstalledVersion);
                    }
                }
                catch
                {
                    // Silent changelog fail
                }
                finally
                {
                    req.Dispose();
                    onDone?.Invoke();
                }
            };
        }

        public static string ExtractVersionNotes(string changelog, string remoteVersion, string localVersion = null)
        {
            if (string.IsNullOrEmpty(changelog)) return null;

            var cleanRemote = PackageDiscovery.CleanVersion(remoteVersion);
            var cleanLocal = PackageDiscovery.CleanVersion(localVersion);

            var candidates = new[]
            {
                $"## [{cleanRemote}]",
                $"## [v{cleanRemote}]",
                $"## {cleanRemote}",
                $"## v{cleanRemote}"
            };

            int start = -1;
            foreach (var c in candidates)
            {
                start = changelog.IndexOf(c, StringComparison.OrdinalIgnoreCase);
                if (start >= 0) break;
            }

            if (start < 0)
            {
                start = changelog.IndexOf("## [", StringComparison.Ordinal);
                if (start < 0) start = changelog.IndexOf("## ", StringComparison.Ordinal);
            }

            if (start < 0)
                return changelog.Length > 800 ? changelog.Substring(0, 800) + "..." : changelog;

            var bodyStart = changelog.IndexOf('\n', start);
            if (bodyStart < 0) bodyStart = start;

            int end = -1;
            if (!string.IsNullOrEmpty(cleanLocal) && cleanLocal != cleanRemote)
            {
                var localCandidates = new[]
                {
                    $"## [{cleanLocal}]",
                    $"## [v{cleanLocal}]",
                    $"## {cleanLocal}",
                    $"## v{cleanLocal}"
                };

                foreach (var lc in localCandidates)
                {
                    end = changelog.IndexOf(lc, bodyStart, StringComparison.OrdinalIgnoreCase);
                    if (end >= 0) break;
                }
            }

            if (end < 0)
            {
                end = changelog.IndexOf("\n## [", bodyStart, StringComparison.Ordinal);
                if (end < 0) end = changelog.IndexOf("\n## ", bodyStart, StringComparison.Ordinal);
            }

            var length = (end >= 0 ? end : changelog.Length) - bodyStart;
            if (length <= 0) return null;

            return changelog.Substring(bodyStart, length).Trim();
        }

        public static bool IsNewerVersion(string remote, string local)
        {
            if (string.IsNullOrEmpty(remote) || string.IsNullOrEmpty(local)) return false;

            if (string.Equals(remote, local, StringComparison.OrdinalIgnoreCase))
                return false;

            var rParts = remote.Split('.');
            var lParts = local.Split('.');

            int max = Math.Max(rParts.Length, lParts.Length);
            for (int i = 0; i < max; i++)
            {
                int rNum = 0, lNum = 0;
                if (i < rParts.Length)
                {
                    var clean = System.Text.RegularExpressions.Regex.Match(rParts[i], @"\d+").Value;
                    int.TryParse(clean, out rNum);
                }
                if (i < lParts.Length)
                {
                    var clean = System.Text.RegularExpressions.Regex.Match(lParts[i], @"\d+").Value;
                    int.TryParse(clean, out lNum);
                }

                if (rNum > lNum) return true;
                if (rNum < lNum) return false;
            }

            return false;
        }
    }
}
