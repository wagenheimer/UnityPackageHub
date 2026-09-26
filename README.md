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

| Icon | Package | Package ID | Primary Dashboard (`priority = 0`) | In-Game Debug Hotkey | Description |
| :--- | :--- | :--- | :--- | :---: | :--- |
| 🎛️ | **Package Hub** | `com.wagenheimer.packagehub` | `Tools/Wagenheimer/Package Hub...` | — | Centralized ecosystem manager, launcher & updater |
| ⚡ | **Unity Utils** | `com.wagenheimer.unityutils` | `Tools/Wagenheimer/Unity Utils/Dashboard...` | — | Project cleanup, audio/animation audits & bootstrap builder |
| 🔨 | **Build Pipeline** | `com.wagenheimer.buildpipeline` | `Tools/Wagenheimer/Build Pipeline/Dashboard...` | `F7` | Multi-publisher automated builds, keystores & APK/AAB |
| ⭐ | **Rate Control** | `com.wagenheimer.ratecontrol` | `Tools/Wagenheimer/Rate Control/Dashboard...` | `F9` | Smart in-app review prompts, cooldowns & thresholds |
| ☁️ | **Cloud Save** | `com.wagenheimer.cloudsave` | `Tools/Wagenheimer/Cloud Save/Dashboard...` | `F6` | Cross-platform UGS cloud save, conflict resolution & auth |
| 📱 | **Native Social** | `com.wagenheimer.nativesocial` | `Tools/Wagenheimer/Native Social/Dashboard...` | `F8` | GPGS, Game Center & Steam achievements/leaderboards |
| 💳 | **IAP Helper** | `com.wagenheimer.iaphelper` | `Tools/Wagenheimer/IAP Helper/Dashboard...` | — | In-App Purchases, catalog management & restore flows |
| 📊 | **LevelPlay Helper** | `com.wagenheimer.levelplayhelper` | `Tools/Wagenheimer/Level Play Helper/Dashboard...` | — | ironSource / Unity LevelPlay mediation helper & ads QA |
| 🎮 | **Rewired Helper** | `com.wagenheimer.rewiredhelper` | `Tools/Wagenheimer/Rewired Helper/Dashboard...` | — | Rewired input management, controller setup & diagnostics |
| 🔄 | **Tk2d Porter** | `com.wagenheimer.tk2dporter` | `Tools/Wagenheimer/Tk2d Porter/Auto Converter...` | — | Automated 2D Toolkit migration to native Unity Sprite/UI |
| ✍️ | **Timeline Typewriter** | `com.wagenheimer.timelinetypewriter`| `Tools/Wagenheimer/Timeline Typewriter/Docs...` | — | Cinematic TMP typewriter tracks for Unity Timeline |

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
│ [Tabs]  📦 Installed (5)  |  🌐 Catalog (11)  |  📖 Sobre Cezar & Suite│
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

## 👤 Sobre o Autor & Filosofia

Desenvolvido por **Cezar Wagenheimer**, desenvolvedor de jogos e engenheiro de software focado em criar ferramentas open-source de padrão industrial para a comunidade Unity.

### Princípios Arquiteturais da Suite
1. **Zero Garbage (0 GC)**: Operações em runtime são otimizadas para não alocar lixo por frame, preservando a fluidez em dispositivos mobile modestos.
2. **Separação em Tempo de Compilação**: SDKs de terceiros e APIs nativas (Google Play Games, Game Center, Steamworks) são estritamente isoladas via `#if`, garantindo builds limpas e leves em todas as plataformas.
3. **Desacoplamento e Independência**: Cada pacote funciona perfeitamente de forma isolada, mas integra-se organicamente à suite quando múltiplos pacotes estão instalados.
4. **Tooling & DX de Primeira Linha**: Todo módulo possui seu próprio **Dashboard em UI Toolkit**, mecanismos automáticos de **Checker & Diagnostics**, ferramentas de **Live Helper & QA**, e painéis de **Debug In-Game**.

---

## 📄 License

This project is licensed under the **MIT License** — feel free to use it in commercial and open-source games alike.

Contributions, issues, and feature suggestions are welcome via [GitHub Issues](https://github.com/wagenheimer/UnityPackageHub/issues)!
