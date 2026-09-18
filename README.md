# Wagenheimer Package Hub

Centralized package manager, update checker, and ecosystem browser for all Wagenheimer Unity packages.

## Features

- **Automated Package Discovery**: Scans the project for all installed `com.wagenheimer.*` packages.
- **Parallel Update Checking**: Concurrently checks GitHub for newer releases and extracts changelog notes.
- **Rich Changelog Viewer**: Renders styled release notes directly in the Editor window.
- **One-Click Updates**: Updates individual packages or all outdated packages at once via UPM.
- **Catalog Explorer**: Browse and install other Wagenheimer packages (`UnityBuildPipeline`, `UnityIAPHelper`, `UnityRateControl`, `UnityCloudSave`, etc.) with 1 click.
- **Consolidated Auto-Check**: A single startup check replaces multiple per-package update prompts.

## Installation

Add this Git URL to your Unity Package Manager (`Window > Package Manager > Add package from git URL...`):

```
https://github.com/wagenheimer/UnityPackageHub.git#v1.0.0
```

Or declare it in your `Packages/manifest.json`:

```json
"com.wagenheimer.packagehub": "https://github.com/wagenheimer/UnityPackageHub.git#v1.0.0"
```

## Menu Access

- `Tools > Wagenheimer > Package Hub...`
- `Tools > Wagenheimer > Check for Updates...`
- `Window > Wagenheimer > Package Hub`

## License

MIT © Cezar Wagenheimer
