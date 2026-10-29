# Wagenheimer Package Hub

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg?logo=unity)](https://unity.com/)
[![Version](https://img.shields.io/github/v/tag/wagenheimer/UnityPackageHub?label=version&color=cyan)](CHANGELOG.md)
[![License: MIT](https://img.shields.io/badge/License-MIT-emerald.svg)](LICENSE)
[![UI Toolkit](https://img.shields.io/badge/UI-UI%20Toolkit-indigo.svg)](https://docs.unity3d.com/Manual/UIElements.html)
[![Ecosystem](https://img.shields.io/badge/Ecosystem-13%20packages-orange.svg)](https://github.com/wagenheimer)
[![Last commit](https://img.shields.io/github/last-commit/wagenheimer/UnityPackageHub?color=38BDF8)](https://github.com/wagenheimer/UnityPackageHub/commits/main)

**Wagenheimer Package Hub** is the control center for the Wagenheimer Unity ecosystem: a single
editor window that discovers every `com.wagenheimer.*` package in your project, launches their
dashboards in one click, checks for updates in parallel against GitHub, and installs or upgrades
them through UPM — without leaving the editor.

Built with **UI Toolkit**, it is editor-only, has no runtime footprint, and works with any
combination of the suite's packages installed — from just one to all thirteen.

```text
Tools ▸ Wagenheimer ▸ Package Hub...
```

---

## Why it exists

A growing tool suite normally means a growing pile of update checkers, scattered menu paths and
manual `manifest.json` edits. The Hub replaces that with:

- one window that knows which packages are installed, at which version, and what is outdated;
- one menu convention (`Tools ▸ Wagenheimer ▸ <Tool> ▸ Dashboard...`) across the whole suite;
- one parallel, non-blocking update scan instead of one web request per package;
- one installer that pins git URLs to the exact release tag it just found.

---

## Highlights

- **Unified UI Toolkit dashboard** — dark slate theme, flex-based responsive layout, live braille
  spinners, animated progress bar and no per-frame allocations.
- **Automatic package discovery** — reads the project's registered packages through
  `UnityEditor.PackageManager` and merges them with the built-in catalog. Unknown
  `com.wagenheimer.*` packages are picked up automatically.
- **Parallel update scanner** — checks every installed package concurrently, reads the remote
  `package.json`, extracts the matching `CHANGELOG.md` section and shows release recency
  (`2 hours ago`, `3 days ago`).
- **One-click install & "Update All"** — real UPM `Client.Add` requests with version pinning,
  batch progress (`Updating (1 of 3): Cloud Save • 33%`) and per-package error reporting.
- **Ecosystem catalog** — browse, search and install Wagenheimer packages that are not in the
  project yet, filtered by category chips derived from the catalog itself.
- **Dashboard quick-launch bar** — dynamic chips for every installed tool, resolved by reflection or
  menu path so packages never take a hard dependency on each other.
- **Google & EDM4U dependency manager** — diagnoses and migrates Google Play Review/Play Core
  dependencies from scoped registries to official Git URLs.
- **In-game debug center** — shortcuts to inject and manage the runtime overlays of the suite
  (`F6`–`F10`).
- **Off-screen recovery** — every dashboard entry point re-centers the window when a saved position
  lands on a monitor that is no longer connected.
- **Daily background check** — optional, silent, once-per-24-hours update discovery on editor
  startup.

---

## Requirements

| Requirement | Details |
| :--- | :--- |
| Unity | **2021.3** or newer (uses UI Toolkit and `PackageInfo.GetAllRegisteredPackages`) |
| Render pipeline | Any — the package is editor-only |
| Platforms | Windows, macOS, Linux editors |
| Git | A configured `git` is only required for update checks of **private** repositories |

No runtime assembly is shipped: the Hub adds zero bytes to player builds.

---

## Installation

### Option 1 — Unity Package Manager (recommended)

1. Open **Window ▸ Package Manager**.
2. Click **`+`** ▸ **Add package from git URL...**.
3. Paste:

   ```text
   https://github.com/wagenheimer/UnityPackageHub.git#v1.3.18
   ```

### Option 2 — `Packages/manifest.json`

```json
{
  "dependencies": {
    "com.wagenheimer.packagehub": "https://github.com/wagenheimer/UnityPackageHub.git#v1.3.18"
  }
}
```

Pinning a tag (`#v1.3.18`) is recommended. From then on, the Hub itself tells you when a newer tag
exists and can update the entry for you.

### Opening the Hub

| Entry point | Path |
| :--- | :--- |
| Main window | **Tools ▸ Wagenheimer ▸ Package Hub...** (`priority = 0`) |
| Alternate | **Window ▸ Wagenheimer ▸ Package Hub** |
| Immediate scan | **Tools ▸ Wagenheimer ▸ Check for Updates...** |
| Website | **Tools ▸ Wagenheimer ▸ Visit wagenheimer.com ↗** |
| Repositories | **Tools ▸ Wagenheimer ▸ GitHub Repositories ↗** |

---

## What is inside the window

| Tab | Contents |
| :--- | :--- |
| **Installed Packages** | Quick-launch chips, search + category filters, and one card per installed package with version badges, release recency, changelog viewer, *Update*, *Dashboard* and secondary tool buttons. |
| **Explore Catalog** | Every package in the catalog, installed or not, with the same cards plus an *Install* action. |
| **About Cezar Wagenheimer & Ecosystem** | Author profile, engineering principles and the full ecosystem directory with repository links. |
| **Settings & Maintenance** | Background-check toggles, force re-scan, schedule reset, UPM shortcut and Google/EDM4U maintenance. |

A fixed metrics bar above the tabs always shows **Installed in Project**, **Up to Date**,
**Updates Available** and **Dashboards Ready**, and the header exposes *Check for Updates* and
*Update All*.

---

## The ecosystem

Every package is standalone, MIT-licensed and built around the same principles: no forced
dependencies, compile-time platform isolation via `#if`, and first-class editor tooling.

| Package | Category | What it does |
| :--- | :--- | :--- |
| 🎛️ **Package Hub** | Core Tools | This package — discovery, update checks, catalog and dashboard launcher. |
| ⚡ **Unity Utils** | Core Tools | Persistent singleton bootstrap, additive scene loading, missing-script and audio/TMP cleanup audits. |
| 🔨 **Build Pipeline** | Build & CI | Multi-store build automation: profiles, language matrix, keystore management, CLI runner, APK/AAB output and a `F7` build overlay. |
| ⭐ **Rate Control** | Engagement | Store review prompts with engagement milestones, cooldowns, blocker diagnostics and store redirects (`F9`). |
| ☁️ **Cloud Save** | Storage & Cloud | Cross-platform cloud saves over UGS / Steam Cloud / Play Games / Game Center with conflict resolution (`F6`). |
| 📱 **Native Social** | Engagement | GPGS, Game Center and Steam achievements/leaderboards with native overlays (`F8`). |
| 💳 **IAP Helper** | Monetization | Unity In-App Purchasing v5+ framework, catalog validation, restore flow and a `F10` debug overlay. |
| 📊 **LevelPlay Helper** | Monetization | ironSource/LevelPlay mediation for rewarded, interstitial and banner ads plus QA tooling. |
| 🎮 **Rewired Helper** | Input | Rewired setup checker, manager/event-system creation, remapping helpers and hotplug listeners. |
| 🗂️ **Tk2d Porter** | Migration & Tools | Converts legacy 2D Toolkit sprites, fonts and animations to native Unity sprites, UI and prefabs. |
| ✍️ **Timeline Typewriter** | Animation & UI | TextMeshPro typewriter tracks and clips for Unity Timeline with audio blips and callbacks. |
| 🌐 **I2 Localization** | Localization | Private I2 Localization fork with Unity 6 fixes, a UI Toolkit term editor, Sheets import/export and AI translation. **Requires a licensed I2 seat.** |
| 🔁 **I2 Localization Migrator** | Localization | One-time assistant that backs up a legacy `Assets/I2`, keeps the term asset and installs the package above. |

> The catalog lives in `Editor/PackageCatalog.cs`, so adding a package is a single object literal —
> the UI, filters and quick-launch bar pick it up automatically.

---

## How update detection works

1. The Hub lists registered `com.wagenheimer.*` packages and merges them with the catalog.
2. For each installed package it fetches `package.json` from
   `raw.githubusercontent.com/<repo>/<branch>` and compares versions with SemVer rules.
3. It then downloads `CHANGELOG.md` and extracts the section between the remote and installed
   versions, rendering it with colour-coded `Added` / `Fixed` / `Changed` tags.
4. If `raw.githubusercontent.com` is unavailable — typically a **private** repository — it falls
   back to `git ls-remote --tags` using the same credentials UPM already uses, and picks the highest
   tag.
5. Updates are applied with `Client.Add("<git-url>#v<version>")`, either per package or in a batch
   queue that reports progress back to the header.

All requests are asynchronous and never block the editor main thread. Failures are shown per card
and never abort the rest of the scan.

---

## Google Play & EDM4U dependency manager

`GoogleDependencyManager` handles the Android dependencies required by review/rating flows
(External Dependency Manager, Play Common, Play Core, Play In-App Review):

- **Diagnose** the project for scoped registries, missing packages, non-Git installs and legacy
  asset folders.
- **Migrate** `Packages/manifest.json` and `packages-lock.json` to official Git URLs, removing
  scoped registries that produce Unity's *"unverified package"* warning. JSON is edited with
  brace-aware matching and re-validated before writing.
- **Force refresh** cached commit hashes so Unity pulls the latest commits.
- Surface everything in an amber banner only when action is actually required, with
  *Migrate*, *Details* and *Dismiss* actions.

---

## In-game debug overlays

Several packages ship runtime UI Toolkit overlays. The Hub installs them into the open scene and
keeps them one click away:

| Shortcut | Package | What it inspects |
| :---: | :--- | :--- |
| `F6` | Cloud Save | UGS auth, active provider, player ID, conflict timestamp, force-sync and event log |
| `F7` | Build Pipeline | Publisher config, bundle ID, keystore validity, store IDs |
| `F8` | Native Social | GPGS/Game Center/Steam login, achievement and leaderboard test calls |
| `F9` | Rate Control | Threshold counters, blocker diagnostics, cooldowns, simulated prompts |
| `F10` | IAP Helper | Product catalog state and purchase flow diagnostics |

Overlays include `A-` / `A+` scaling for phones through 4K desktops, a draggable toggle button, and
are stripped from non-development builds by default.

---

## Editor API

Everything the window does is available from your own editor scripts
(namespace `Wagenheimer.PackageHub.Editor`):

```csharp
using UnityEditor;
using Wagenheimer.PackageHub.Editor;

public static class MyGameTools
{
    [MenuItem("MyGame/Open Hub")]
    public static void OpenHub() => PackageHubWindow.Open();

    [MenuItem("MyGame/Inspect Cloud Save")]
    public static void InspectCloudSave() =>
        PackageHubWindow.OpenToPackage("com.wagenheimer.cloudsave");

    [MenuItem("MyGame/Check Suite Updates")]
    public static void CheckUpdates()
    {
        var packages = PackageDiscovery.GetAllPackages();
        PackageUpdateService.CheckUpdates(packages, () =>
        {
            foreach (var p in packages)
                if (p.HasUpdate)
                    UnityEngine.Debug.Log($"{p.DisplayName}: {p.InstalledVersion} -> {p.LatestRemoteVersion}");
        });
    }

    [MenuItem("MyGame/Update All Outdated")]
    public static void UpdateAll()
    {
        var outdated = PackageDiscovery.GetAllPackages()
            .FindAll(p => p.IsInstalled && p.HasUpdate);
        PackageInstaller.UpdateAll(outdated, () => UnityEngine.Debug.Log("Done."));
    }
}
```

### Public surface

| Type | Members |
| :--- | :--- |
| `PackageHubWindow` | `Open()`, `OpenToPackage(id)`, `SwitchTab(Tab)` |
| `PackageDiscovery` | `GetAllPackages()`, `CleanVersion(v)`, `PackagePrefix` |
| `PackageUpdateService` | `CheckUpdates(list, onComplete)`, `IsNewerVersion(remote, local)`, `ExtractVersionNotes(...)` |
| `PackageInstaller` | `InstallOrUpdate(item, version, cb)`, `UpdateAll(list, cb)`, `IsBusy`, `BatchProgress` |
| `PackageDashboardLauncher` | `HasDashboard(id)`, `GetDashboardInfo(id)`, `Launch(shortcut, fallbackUrl)` |
| `GoogleDependencyManager` | `Detect()`, `MigrateToRecommended(force, cb)`, `ForceUpdateGooglePackages(cb)` |
| `PackageCatalog` | `KnownPackages` |
| `EditorWindowPlacement` | `EnsureOnScreen(window, minSize, defaultSize)`, `Center(...)` |

To make your own window open on-screen regardless of saved layouts, call
`EditorWindowPlacement.EnsureOnScreen(this, minSize, defaultSize)` right after `Show()`.

---

## Settings

Stored in `EditorPrefs` and editable from **Settings & Maintenance**:

| Preference | Default | Effect |
| :--- | :---: | :--- |
| `Wagenheimer_Hub_AutoCheck` | `true` | Check installed packages once per 24 h on editor startup |
| `Wagenheimer_Hub_AutoOpenWindow` | `false` | Open the Hub automatically when updates are found |
| `Wagenheimer_Hub_LastCheck` | — | Timestamp of the last automatic scan (clearable) |

---

## Project layout

```text
UnityPackageHub/
├── Editor/
│   ├── PackageHubWindow.cs        # UI Toolkit window: header, metrics, 4 tabs, cards
│   ├── PackageHubUIStyle.cs       # Design tokens, USS builder, icon/text helpers
│   ├── PackageCatalog.cs          # The ecosystem catalog (add new packages here)
│   ├── PackageDiscovery.cs        # Registered UPM packages + catalog merge
│   ├── PackageUpdateService.cs    # Parallel GitHub checks, changelog and tag parsing
│   ├── PackageInstaller.cs        # UPM Client.Add, single and batch updates
│   ├── PackageDashboardLauncher.cs# Reflection/menu-based dashboard shortcuts
│   ├── GoogleDependencyManager.cs # EDM4U / Play dependency diagnosis and migration
│   ├── PackageHubAutoChecker.cs   # Daily background update check
│   └── EditorWindowPlacement.cs   # Keeps dashboards on a connected monitor
├── package.json                   # UPM manifest (com.wagenheimer.packagehub)
├── CHANGELOG.md                   # Keep a Changelog + SemVer
└── LICENSE                        # MIT
```

See [AGENTS.md](AGENTS.md) for the repository conventions, including the rule that leading icons
must go through `PackageHubUIStyle.ApplyIconText` / `CreateIconLabel` so emoji never overlap labels
on Windows.

---

## Troubleshooting

| Symptom | Cause & fix |
| :--- | :--- |
| An update check stays on *Checking...* | No network access to GitHub, or the repo is private and `git` is missing/not authenticated. Verify `git ls-remote <url>` works in a terminal. |
| *Unverified package* warnings | Your project still uses scoped registries for Google packages. Run **Settings & Maintenance ▸ Migrate to Recommended Git Setup**. |
| A dashboard "does nothing" | Its saved window position is on a disconnected monitor. The Hub re-centers its own window; other dashboards need `EditorWindowPlacement.EnsureOnScreen`. |
| Package installed but not listed | The package id must start with `com.wagenheimer.`. Wait for the UPM resolve to finish, then use **Force Re-scan Packages**. |
| Batch update stops midway | Check the Console: a failing package is logged and skipped, the rest of the queue continues. |

---

## Contributing

Issues and feature requests are welcome at
[github.com/wagenheimer/UnityPackageHub/issues](https://github.com/wagenheimer/UnityPackageHub/issues).
Commits follow Conventional Commits (`feat:`, `fix:`, `docs:`) and every release updates both
`package.json` and `CHANGELOG.md`, tagged `vX.Y.Z` — that tag is what the Hub reads as the latest
version, so a release without it will not be offered to users.

---

## License

**MIT** — free for commercial and open-source games alike. See [LICENSE](LICENSE).

Created and maintained by **[Cezar Wagenheimer](https://wagenheimer.com)** ·
[GitHub](https://github.com/wagenheimer) ·
[LinkedIn](https://www.linkedin.com/in/cezar-wagenheimer/) ·
[cezar@wagenheimer.com](mailto:cezar@wagenheimer.com)
