using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Wagenheimer.PackageHub.Editor
{
    [InitializeOnLoad]
    public static class PackageHubAutoChecker
    {
        public const string PrefAutoCheck = "Wagenheimer_Hub_AutoCheck";
        public const string PrefLastCheck = "Wagenheimer_Hub_LastCheck";
        public const string PrefAutoOpenWindow = "Wagenheimer_Hub_AutoOpenWindow";

        static PackageHubAutoChecker()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (!EditorPrefs.GetBool(PrefAutoCheck, true))
                return;

            var lastCheckStr = EditorPrefs.GetString(PrefLastCheck, string.Empty);
            if (DateTime.TryParse(lastCheckStr, out var lastCheck))
            {
                // Run once per day
                if ((DateTime.UtcNow - lastCheck).TotalHours < 24)
                    return;
            }

            EditorPrefs.SetString(PrefLastCheck, DateTime.UtcNow.ToString("o"));

            // Discover and check installed packages silently
            var installed = PackageDiscovery.GetAllPackages().Where(p => p.IsInstalled).ToList();
            if (installed.Count == 0) return;

            PackageUpdateService.CheckUpdates(installed, () =>
            {
                var outdated = installed.Where(p => p.HasUpdate).ToList();
                if (outdated.Count > 0)
                {
                    var names = string.Join(", ", outdated.Select(p => $"{p.DisplayName} ({p.InstalledVersion} ➔ {p.LatestRemoteVersion})"));
                    Debug.Log($"<color=#38BDF8><b>[Wagenheimer Package Hub]</b></color> {outdated.Count} package update(s) available: {names}. Open <b>Tools > Wagenheimer > Package Hub</b> to review.");

                    if (EditorPrefs.GetBool(PrefAutoOpenWindow, false))
                    {
                        PackageHubWindow.ShowWindow();
                    }
                }
            });
        }
    }
}
