# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-09-25

### Added
- **Modern UI Toolkit Remake**: Complete redesign using Unity's UI Toolkit (Nova UI) matching the modern dark design system across UnityUtils, UnityBuildPipeline, and UnityRateControl.
- **Automatic Dashboard Launcher**: Dedicated 1-click launcher system (`PackageDashboardLauncher`) for all installed packages. Automatically detects and opens dashboards (`UnityUtilsHubWindow`, `BuildPipelineWindow`, `RateControlHubWindow`, `IAPHelperDashboardWindow`, `CloudSaveSetupWindow`, `LevelPlaySetupWindow`, `RewiredHelperSetupWindow`, and secondary tool windows).
- **Quick Launch Dashboards Bar**: Instant 1-click launch chips right at the top of the Installed Packages tab for immediate navigation.
- **Dedicated "Sobre Cezar Wagenheimer & Ecosystem" Tab**: Rich developer bio, architectural philosophy (Zero Boilerplate, Commercial Battle-Tested, Modular & Decoupled, Continuous Automation), and complete interactive ecosystem catalog directory.
- **Live Metrics Counter Bar**: Live counters for Installed in Project, Up to Date, Updates Available, and Active Dashboards Ready.
- **Category Filter & Search Toolbar**: Fast live text search with category filter chips (Core Tools, Monetization, Build & CI, Storage & Cloud, Engagement, Input).

## [1.0.5] - 2026-09-20

### Fixed
- The window could open fully off-screen after a monitor change, resolution switch or corrupted layout: it appeared to "open" with no error but was never visible. Every entry point now re-centers it on the main editor window when its saved position no longer overlaps the editor.
- A package discovery failure on window enable no longer leaves the window blank - it is logged as a warning instead of throwing.
- Header textures are released and nulled on disable, and the styles rebuild correctly if the window is re-enabled.

### Changed
- The footer version is read from the installed package instead of a hardcoded string.

## [1.0.4] - 2026-09-18

### Added
- Added `Tools > Wagenheimer > Visit wagenheimer.com ↗` and `GitHub Repositories ↗` menu shortcuts with priority separation.

## [1.0.3] - 2026-09-18

### Fixed
- Fixed raw `<b>` HTML tag rendering in header installed badge by enabling `richText = true` across badge styles.
- Polished vertical and horizontal alignment of header titles, links, badges, and action buttons.

## [1.0.2] - 2026-09-18

### Fixed
- Fixed header clipping and vanishing content in docked/nested window contexts by refactoring from manual area rects to structured vertical layout container.
- Fixed `<b>` HTML tag not rendering bold in Settings tab by using dedicated styled card headers.
- Fixed startup toggle label getting truncated in Settings tab.

### Added
- Prominent website links to `wagenheimer.com` in header subtitle and footer toolbar.
- Dedicated "About & Links" section in Settings with direct links to `https://wagenheimer.com` and GitHub profile.

## [1.0.1] - 2026-09-18

### Fixed
- Added Unity `.meta` files for all folders, scripts, and assets to resolve immutable UPM PackageCache imports.

## [1.0.0] - 2026-09-18

### Added
- **Unified Package Hub Window**: Modern slate UI in `Tools > Wagenheimer > Package Hub` (and `Tools > Wagenheimer > Check for Updates...`).
- **Dynamic Package Discovery**: Automatically scans project for all `com.wagenheimer.*` packages via `UnityEditor.PackageManager.PackageInfo`.
- **Parallel Remote Update Checker**: Concurrently polls remote GitHub repositories for latest tags, versions, and multi-version changelog diffs.
- **Rich Multi-Version Changelogs**: Formats release notes with color-coded tags (`✦ Added`, `✔ Fixed`, `⚡ Changed`) and markdown bullet points.
- **One-Click Package Updates & Installs**: Triggers seamless updates and installs through Unity Package Manager (`Client.Add`).
- **Update All in Batch**: Consecutively updates all outdated Wagenheimer packages with a single click.
- **Package Ecosystem Catalog**: Built-in discovery browser to explore and install other Wagenheimer packages into the active project.
- **Discreet Background Auto-Checker**: Daily background check on editor startup that consolidates notifications without interrupting developer workflow.

