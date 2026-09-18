# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

