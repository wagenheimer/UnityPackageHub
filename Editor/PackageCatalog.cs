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
                Category = "Core Tools"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.buildpipeline",
                DisplayName = "Unity Build Pipeline",
                Description = "Modular multi-project build automation pipeline: multi-store profiles, language matrix, CLI runner, and build verification overlay.",
                RepoUrl = "https://github.com/wagenheimer/UnityBuildPipeline",
                GitUrl = "https://github.com/wagenheimer/UnityBuildPipeline.git",
                DefaultBranch = "master",
                Category = "Build & CI"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.iaphelper",
                DisplayName = "IAP Helper",
                Description = "Production-ready Unity In-App Purchasing (v5+) framework with zero-code UI prefabs, F10 debug overlay, and restore transaction flow.",
                RepoUrl = "https://github.com/wagenheimer/UnityIAPHelper",
                GitUrl = "https://github.com/wagenheimer/UnityIAPHelper.git",
                DefaultBranch = "main",
                Category = "Monetization"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.ratecontrol",
                DisplayName = "Rate Control",
                Description = "Intelligent app store review and rating prompt manager with player engagement milestones, cooldowns, and store redirect logic.",
                RepoUrl = "https://github.com/wagenheimer/UnityRateControl",
                GitUrl = "https://github.com/wagenheimer/UnityRateControl.git",
                DefaultBranch = "master",
                Category = "Engagement"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.cloudsave",
                DisplayName = "Cloud Save",
                Description = "Cross-platform cloud save abstraction layer supporting Steam Cloud, Google Play Saved Games, Apple Game Center, and custom backends.",
                RepoUrl = "https://github.com/wagenheimer/UnityCloudSave",
                GitUrl = "https://github.com/wagenheimer/UnityCloudSave.git",
                DefaultBranch = "main",
                Category = "Storage & Cloud"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.levelplayhelper",
                DisplayName = "LevelPlay Helper",
                Description = "IronSource LevelPlay ad mediation helper for rewarded video, interstitial, and banner ads with lifecycle callbacks and state tracking.",
                RepoUrl = "https://github.com/wagenheimer/UnityLevelPlayHelper",
                GitUrl = "https://github.com/wagenheimer/UnityLevelPlayHelper.git",
                DefaultBranch = "master",
                Category = "Monetization"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.nativesocial",
                DisplayName = "Native Social",
                Description = "Lightweight cross-platform native iOS & Android sharing and social media integration without heavy third-party SDK dependencies.",
                RepoUrl = "https://github.com/wagenheimer/UnityNativeSocial",
                GitUrl = "https://github.com/wagenheimer/UnityNativeSocial.git",
                DefaultBranch = "master",
                Category = "Engagement"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.rewiredhelper",
                DisplayName = "Rewired Helper",
                Description = "Rewired input system wrapper providing platform auto-detection, remapping helpers, and controller hotplug listeners.",
                RepoUrl = "https://github.com/wagenheimer/RewiredHelper",
                GitUrl = "https://github.com/wagenheimer/RewiredHelper.git",
                DefaultBranch = "main",
                Category = "Input"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.unityutils",
                DisplayName = "Unity Utils",
                Description = "Persistent singleton bootstrap kit, scene additive loading pipeline, and automated AudioListener & TextMeshPro cleanup tools.",
                RepoUrl = "https://github.com/wagenheimer/UnityUtils",
                GitUrl = "https://github.com/wagenheimer/UnityUtils.git",
                DefaultBranch = "master",
                Category = "Core Tools"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.timelinetypewriter",
                DisplayName = "Timeline Typewriter",
                Description = "TextMeshPro typewriter effect track and clips for Unity Timeline with audio blips, variable speed, and completion callbacks.",
                RepoUrl = "https://github.com/wagenheimer/UnityTimelineTypewriter",
                GitUrl = "https://github.com/wagenheimer/UnityTimelineTypewriter.git",
                DefaultBranch = "main",
                Category = "Animation & UI"
            },
            new PackageItem
            {
                PackageId = "com.wagenheimer.tk2dporter",
                DisplayName = "Tk2d Porter",
                Description = "2D Toolkit migration utility to convert legacy 2D Toolkit sprites, fonts, and animations into native Unity SpriteRenderer and Canvas assets.",
                RepoUrl = "https://github.com/wagenheimer/UnityTk2dPorter",
                GitUrl = "https://github.com/wagenheimer/UnityTk2dPorter.git",
                DefaultBranch = "main",
                Category = "Migration & Tools"
            }
        };
    }
}

