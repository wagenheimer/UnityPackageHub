# Wagenheimer Package Hub

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg?logo=unity)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-emerald.svg)](LICENSE)
[![UI Toolkit](https://img.shields.io/badge/UI-UI%20Toolkit%20%28Nova%20UI%29-indigo.svg)](https://docs.unity3d.com/Manual/UIElements.html)
[![Version](https://img.shields.io/badge/Version-v1.1.0-cyan.svg)](CHANGELOG.md)
[![Ecosystem](https://img.shields.io/badge/Ecosystem-11%2B%20Packages-orange.svg)](https://github.com/wagenheimer)

**Wagenheimer Package Hub** is the central command center, unified dashboard launcher, and live ecosystem manager for all open-source Wagenheimer Unity packages. Designed with Unity's modern **UI Toolkit ("Nova UI")**, it provides one-click dashboard access, real-time diagnostic checks, in-game debug overlay management, parallel GitHub update resolution, and automated package discovery.

---

## 🌟 Highlights

- **Unified UI Toolkit Dashboard**: Built from the ground up with responsive flex architecture, dark themes, and fail-safe styling.
- **1-Click Dashboard Quick Launch Bar**: Automatically detects installed Wagenheimer tools and renders dynamic launch chips to open any tool's dedicated dashboard instantly.
- **Comprehensive In-Game Debug Center**: Coordinates runtime debug overlays across packages (`F6` Cloud Save, `F7` Build Pipeline, `F8` Native Social, `F9` Rate Control) with mobile scaling (`A-`/`A+`) and interactive testers.
- **Standardized Menu Hierarchy**: Reorganized across all 11 ecosystem packages so that `Tools > Wagenheimer > <Tool> > Dashboard...` is **always the first item (`priority = 0`)**.
- **Centralized Update Management**: Replaces individual scattered update checkers with a single non-intrusive parallel scanner, rich changelog viewer, and 1-click UPM batch updater.
- **Ecosystem Directory & Author Profile**: Comprehensive guide detailing each package's architectural design principles and repository links.

---

## 🗂️ Wagenheimer Tooling Matrix

Every package in the suite is designed around production battle-tested principles: **zero runtime garbage, compile-time platform separation, and modular decoupling**.

| Package | Menu Shortcut (`priority = 0`) | In-Game Debug | Features & Purpose |
| :--- | :--- | :---: | :--- |
| 🎛️ **Package Hub** | `Tools > Wagenheimer > Package Hub...` | — | Unified dashboard launcher, update checker & package catalog |
| ⚡ **Unity Utils** | `... > Unity Utils > Dashboard...` | — | Missing scripts cleaner, audio/anim audits & code scaffolding |
| 🔨 **Build Pipeline** | `... > Build Pipeline > Dashboard...` | `F7` | Multi-store build automation, keystore management & APK/AAB |
| ⭐ **Rate Control** | `... > Rate Control > Dashboard...` | `F9` | Intelligent review prompts, milestone cooldowns & store redirects |
| ☁️ **Cloud Save** | `... > Cloud Save > Dashboard...` | `F6` | Cross-platform UGS cloud save, conflict resolution & auth QA |
| 📱 **Native Social** | `... > Native Social > Dashboard...` | `F8` | GPGS, Game Center & Steam achievements and leaderboards |
| 💳 **IAP Helper** | `... > IAP Helper > Dashboard...` | — | In-App Purchases (v5+), catalog validation & restore purchases |
| 📊 **LevelPlay Helper** | `... > Level Play Helper > Dashboard...` | — | ironSource / LevelPlay mediation helper & ads QA testing |
| 🎮 **Rewired Helper** | `... > Rewired Helper > Dashboard...` | — | Rewired input management, controller setup & diagnostics |
| 🔄 **Tk2d Porter** | `... > Tk2d Porter > Auto Converter...` | — | Automated 2D Toolkit migration to native Unity Sprites & UI |
| ✍️ **Timeline Typewriter** | `... > Timeline Typewriter > Docs...` | — | Cinematic TextMeshPro typewriter tracks for Unity Timeline |

---

## 🚀 Installation

### Option 1: Via Unity Package Manager (Recommended)
1. In Unity Editor, open **Window > Package Manager**.
2. Click the **`+`** icon in the top-left corner and choose **Add package from git URL...**.
3. Enter the URL:
   ```text
   https://github.com/wagenheimer/UnityPackageHub.git#v1.1.0
   ```

### Option 2: Via `Packages/manifest.json`
Add the dependency directly inside your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.wagenheimer.packagehub": "https://github.com/wagenheimer/UnityPackageHub.git#v1.1.0"
  }
}
```

---

## 🛠️ Features & Architecture

```
┌────────────────────────────────────────────────────────────────────────┐
│                        WAGENHEIMER PACKAGE HUB                         │
├────────────────────────────────────────────────────────────────────────┤
│ [Quick Launch Bar]                                                     │
│ ⚡ Utils  🔨 Build  ⭐ Rate  ☁️ Cloud  📱 Social  💳 IAP  📊 LevelPlay   │
├───────────────────┬────────────────────────────────────────────────────┤
│ METRICS ROW       │ • 5 Installed  • 5 Up to Date  • 5 Dashboards Ready│
├───────────────────┴────────────────────────────────────────────────────┤
│ [Tabs]  📦 Installed (5)  |  🌐 Catalog (11)  |  📖 About Cezar & Suite│
├────────────────────────────────────────────────────────────────────────┤
│ • Interactive package cards with 1-click Dashboard / Debug Launchers   │
│ • Live search, category filters (Core, Monetization, Platform, Audio)   │
│ • Rich Markdown Changelog viewer & parallel GitHub release checker     │
└────────────────────────────────────────────────────────────────────────┘
```

### 1. Dashboard Quick Launch Bar
Installed tools are automatically recognized through `PackageDashboardLauncher`. Clicking any quick-launch chip immediately executes the target window's entrypoint or reflection dispatcher, giving you instant access to setup tools, verification wizards, and diagnostics.

### 2. In-Game Runtime Debug Overlays
Several tools feature comprehensive runtime UI Toolkit debug panels. The Hub provides 1-click scene injection and shortcuts for all of them:
- **Cloud Save Debug (`F6`)**: Inspects UGS authentication, active provider, player ID, conflict timestamp, force-sync triggers, and live event log.
- **Build Pipeline Debug (`F7`)**: Verifies active publisher configuration, package name / bundle ID, keystore validity, and store IDs.
- **Native Social Debug (`F8`)**: Tests GPGS / Game Center / Steam authentication, sends test achievement progress, submits test leaderboard scores, and invokes native overlays.
- **Rate Control Debug (`F9`)**: Displays live threshold counters, blocker diagnostics, cooldown countdowns, and simulated prompts.

All in-game overlays feature:
- **Mobile Scale Controls (`A-` / `A+`)**: Adapts dynamically from compact phone screens to high-DPI tablets and 4K desktop displays.
- **Floating Drag Button**: Quick on-screen toggle that can be repositioned anywhere on screen.
- **Production Stripping**: Automatically disabled in non-development / release builds unless explicitly overridden.

### 3. Asynchronous GitHub Release Scanner
- Scans `git` endpoints concurrently in the background without blocking Unity Editor's main thread.
- Parses `CHANGELOG.md` directly from the repository tags.
- Renders formatted release notes and update recommendations.

---

## 🧭 Menu Reference

The Package Hub standardizes menu paths across the entire ecosystem:

```text
Tools/
└── Wagenheimer/
    ├── Package Hub...                       (Priority = 0)  ← Opens Package Hub Window
    ├── Check for Updates...                 (Priority = 1)  ← Immediate parallel scan
    ├── Unity Utils/
    │   └── Dashboard...                     (Priority = 0)
    ├── Build Pipeline/
    │   ├── Dashboard...                     (Priority = 0)
    │   └── Add Build Debug Overlay to Scene (Priority = 112)
    ├── Rate Control/
    │   └── Dashboard...                     (Priority = 0)
    ├── Cloud Save/
    │   ├── Dashboard...                     (Priority = 0)
    │   └── Add Cloud Save Debug Overlay to Scene (Priority = 19)
    └── Native Social/
        ├── Dashboard...                     (Priority = 0)
        └── Add Native Social Debug Overlay to Scene (Priority = 20)
```

---

## 💻 Developer API & Custom Extensions

You can open the Hub or query package status directly from custom editor scripts:

```csharp
using UnityEditor;
using Wagenheimer.PackageHub.Editor;

public static class CustomDevMenu
{
    // Open the Package Hub window
    [MenuItem("MyGame/Open Hub")]
    public static void OpenHub()
    {
        PackageHubWindow.Open();
    }

    // Open directly focused on a specific package
    [MenuItem("MyGame/Inspect Cloud Save")]
    public static void InspectCloudSave()
    {
        PackageHubWindow.OpenToPackage("com.wagenheimer.cloudsave");
    }

    // Launch a package dashboard programmatically
    [MenuItem("MyGame/Launch Rate Control")]
    public static void LaunchRateControl()
    {
        PackageDashboardLauncher.LaunchPrimary("com.wagenheimer.ratecontrol");
    }
}
```

---

## 👤 About the Author & Architecture Philosophy

Created by **Cezar Wagenheimer**, game developer and software engineer dedicated to building industrial-grade, open-source tooling for the Unity community.

### Core Architectural Principles
1. **Zero Garbage (0 GC)**: Runtime operations are engineered to allocate zero garbage per frame, preserving peak 60/120 FPS performance even on low-end mobile devices.
2. **Compile-Time Platform Isolation**: Platform SDKs and third-party native dependencies (Google Play Games, Game Center, Steamworks) are strictly guarded via `#if`, ensuring lightweight, clean builds on every target platform.
3. **Decoupled & Modular Design**: Every package functions completely standalone with zero forced monolithic dependencies, yet seamlessly interconnects when multiple packages are present.
4. **First-Class Tooling & Developer Experience**: Every single module provides its own **UI Toolkit Dashboard**, automated **Checker & Diagnostics** systems, comprehensive **Live Helpers**, and dedicated **In-Game Debug Overlays**.

---

## 📄 License

This project is licensed under the **MIT License** — feel free to use it in commercial and open-source games alike.

Contributions, issues, and feature suggestions are welcome via [GitHub Issues](https://github.com/wagenheimer/UnityPackageHub/issues)!
