using System;
using System.Collections.Generic;

namespace Wagenheimer.PackageHub.Editor
{
    [Serializable]
    public class PackageItem
    {
        public string PackageId;
        public string DisplayName;
        public string Description;
        public string RepoUrl;
        public string GitUrl;
        public string DefaultBranch;
        public string Category;

        // Runtime status
        public bool IsInstalled;
        public string InstalledVersion;
        public string LatestRemoteVersion;
        public bool HasUpdate;
        public string ReleaseNotes;
        public bool IsChecking;
        public bool IsUpdating;
        public string UpdateError;
        public bool ExpandedNotes;
        public DateTimeOffset? ReleaseDate;
        public string ReleaseDateString;

        public string GetRelativeReleaseTime()
        {
            if (!ReleaseDate.HasValue)
            {
                if (!string.IsNullOrEmpty(ReleaseDateString) && DateTimeOffset.TryParse(ReleaseDateString, out var parsed))
                {
                    ReleaseDate = parsed;
                }
                else
                {
                    return null;
                }
            }

            var span = DateTimeOffset.UtcNow - ReleaseDate.Value;
            if (span.TotalSeconds < 0) return "just now";
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60)
            {
                var mins = (int)span.TotalMinutes;
                return mins == 1 ? "1 min ago" : $"{mins} mins ago";
            }
            if (span.TotalHours < 24)
            {
                var hours = (int)span.TotalHours;
                var mins = span.Minutes;
                if (hours == 1) return mins > 0 ? $"1 hour {mins}m ago" : "1 hour ago";
                return mins > 0 ? $"{hours} hours {mins}m ago" : $"{hours} hours ago";
            }
            if (span.TotalDays < 30)
            {
                var days = (int)span.TotalDays;
                var hours = span.Hours;
                var dayStr = days == 1 ? "1 day" : $"{days} days";
                if (hours > 0)
                {
                    var hourStr = hours == 1 ? "1 hour" : $"{hours} hours";
                    return $"{dayStr} {hourStr} ago";
                }
                return $"{dayStr} ago";
            }
            if (span.TotalDays < 365)
            {
                var months = (int)(span.TotalDays / 30);
                return months == 1 ? "1 month ago" : $"{months} months ago";
            }
            var years = (int)(span.TotalDays / 365);
            return years == 1 ? "1 year ago" : $"{years} years ago";
        }

        public string GetRawPackageJsonUrl()
        {
            var branch = string.IsNullOrEmpty(DefaultBranch) ? "main" : DefaultBranch;
            var repoName = RepoUrl.Substring(RepoUrl.LastIndexOf('/') + 1);
            return $"https://raw.githubusercontent.com/wagenheimer/{repoName}/{branch}/package.json";
        }

        public string GetRawChangelogUrl()
        {
            var branch = string.IsNullOrEmpty(DefaultBranch) ? "main" : DefaultBranch;
            var repoName = RepoUrl.Substring(RepoUrl.LastIndexOf('/') + 1);
            return $"https://raw.githubusercontent.com/wagenheimer/{repoName}/{branch}/CHANGELOG.md";
        }

        public string GetRepoApiCommitsUrl()
        {
            var branch = string.IsNullOrEmpty(DefaultBranch) ? "main" : DefaultBranch;
            var repoName = RepoUrl.Substring(RepoUrl.LastIndexOf('/') + 1);
            return $"https://api.github.com/repos/wagenheimer/{repoName}/commits?sha={branch}&per_page=1";
        }
    }

    public static class PackageCatalog
    {
        public static readonly List<PackageItem> KnownPackages = new List<PackageItem>
        {
            new PackageItem
            {
                PackageId = "com.wagenheimer.packagehub",
                DisplayName = "Wagenheimer Package Hub",
                Description = "Centralized package manager, update checker, and ecosystem catalog for all Wagenheimer Unity packages.",
                RepoUrl = "https://github.com/wagenheimer/UnityPackageHub",
                GitUrl = "https://github.com/wagenheimer/UnityPackageHub.git",
                DefaultBranch = "main",
                Category = "Core Tools",
                ReleaseDateString = "2026-09-25T21:30:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.buildpipeline",
                DisplayName = "Unity Build Pipeline",
                Description = "Modular multi-project build automation pipeline: multi-store profiles, language matrix, CLI runner, and build verification overlay.",
                RepoUrl = "https://github.com/wagenheimer/UnityBuildPipeline",
                GitUrl = "https://github.com/wagenheimer/UnityBuildPipeline.git",
                DefaultBranch = "master",
                Category = "Build & CI",
                ReleaseDateString = "2026-09-25T18:00:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.iaphelper",
                DisplayName = "IAP Helper",
                Description = "Production-ready Unity In-App Purchasing (v5+) framework with zero-code UI prefabs, F10 debug overlay, and restore transaction flow.",
                RepoUrl = "https://github.com/wagenheimer/UnityIAPHelper",
                GitUrl = "https://github.com/wagenheimer/UnityIAPHelper.git",
                DefaultBranch = "main",
                Category = "Monetization",
                ReleaseDateString = "2026-09-24T12:00:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.ratecontrol",
                DisplayName = "Rate Control",
                Description = "Intelligent app store review and rating prompt manager with player engagement milestones, cooldowns, and store redirect logic.",
                RepoUrl = "https://github.com/wagenheimer/UnityRateControl",
                GitUrl = "https://github.com/wagenheimer/UnityRateControl.git",
                DefaultBranch = "master",
                Category = "Engagement",
                ReleaseDateString = "2026-09-25T18:00:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.cloudsave",
                DisplayName = "Cloud Save",
                Description = "Cross-platform cloud save abstraction layer supporting Steam Cloud, Google Play Saved Games, Apple Game Center, and custom backends.",
                RepoUrl = "https://github.com/wagenheimer/UnityCloudSave",
                GitUrl = "https://github.com/wagenheimer/UnityCloudSave.git",
                DefaultBranch = "main",
                Category = "Storage & Cloud",
                ReleaseDateString = "2026-09-25T21:40:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.levelplayhelper",
                DisplayName = "LevelPlay Helper",
                Description = "IronSource LevelPlay ad mediation helper for rewarded video, interstitial, and banner ads with lifecycle callbacks and state tracking.",
                RepoUrl = "https://github.com/wagenheimer/UnityLevelPlayHelper",
                GitUrl = "https://github.com/wagenheimer/UnityLevelPlayHelper.git",
                DefaultBranch = "master",
                Category = "Monetization",
                ReleaseDateString = "2026-09-24T12:00:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.nativesocial",
                DisplayName = "Native Social",
                Description = "Lightweight cross-platform native iOS & Android sharing and social media integration without heavy third-party SDK dependencies.",
                RepoUrl = "https://github.com/wagenheimer/UnityNativeSocial",
                GitUrl = "https://github.com/wagenheimer/UnityNativeSocial.git",
                DefaultBranch = "master",
                Category = "Engagement",
                ReleaseDateString = "2026-09-25T21:40:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.rewiredhelper",
                DisplayName = "Rewired Helper",
                Description = "Rewired input system wrapper providing platform auto-detection, remapping helpers, and controller hotplug listeners.",
                RepoUrl = "https://github.com/wagenheimer/RewiredHelper",
                GitUrl = "https://github.com/wagenheimer/RewiredHelper.git",
                DefaultBranch = "main",
                Category = "Input",
                ReleaseDateString = "2026-09-22T10:00:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.unityutils",
                DisplayName = "Unity Utils",
                Description = "Persistent singleton bootstrap kit, scene additive loading pipeline, and automated AudioListener & TextMeshPro cleanup tools.",
                RepoUrl = "https://github.com/wagenheimer/UnityUtils",
                GitUrl = "https://github.com/wagenheimer/UnityUtils.git",
                DefaultBranch = "master",
                Category = "Core Tools",
                ReleaseDateString = "2026-09-25T18:00:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.timelinetypewriter",
                DisplayName = "Timeline Typewriter",
                Description = "TextMeshPro typewriter effect track and clips for Unity Timeline with audio blips, variable speed, and completion callbacks.",
                RepoUrl = "https://github.com/wagenheimer/UnityTimelineTypewriter",
                GitUrl = "https://github.com/wagenheimer/UnityTimelineTypewriter.git",
                DefaultBranch = "main",
                Category = "Animation & UI",
                ReleaseDateString = "2026-09-20T10:00:00Z"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.tk2dporter",
                DisplayName = "Tk2d Porter",
                Description = "2D Toolkit migration utility to convert legacy 2D Toolkit sprites, fonts, and animations into native Unity SpriteRenderer and Canvas assets.",
                RepoUrl = "https://github.com/wagenheimer/UnityTk2dPorter",
                GitUrl = "https://github.com/wagenheimer/UnityTk2dPorter.git",
                DefaultBranch = "main",
                Category = "Migration & Tools",
                ReleaseDateString = "2026-09-20T10:00:00Z"
            }
        };
    }
}

