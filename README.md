# Reality Check

Reality Check is a local Stardew Valley SMAPI mod by Stan. It adds financial pressure and feedback systems so farm money is tracked, taxed, explained, and affected by a changing market. [F]

Current manifest version: `1.4.1`. [F]

## Requirements

- Stardew Valley 1.6+ [P: README history]
- SMAPI 4.0+ [F: `manifest.json` has `MinimumApiVersion` `4.0.0`]
- .NET 6 target framework [F: `RealityCheck.csproj`]

## Current Feature Areas

- Financial ledger and Financial Manual reports. [F]
- Weekly tax system with income tax, property tax, business property tax, tax history, and custom tax notice UI. [F]
- Harvey health insurance expense/reimbursement tracking. [F]
- Dynamic market prices with market trend history, shop sale and shipping settlement integration, tooltip price patching, and Market Price UI. [F]
- Pelican Town Commodity Exchange with account transfers, contract catalog, positions, margin calls, close position flow, delivery/default handling, debt, and Exchange UI. [F]
- Localization files for default, German, French, Japanese, and Chinese. [F]

## How To Open The UI

Load a save and press the configured Financial Manual hotkey. The default is:

```text
O
```

The key is controlled by `OpenReportKey` in `config.json` after first launch. [F]

## Build

From the repository root:

```bash
dotnet build
```

The project uses `Pathoschild.Stardew.ModBuildConfig`, so a normal build also tries to deploy the mod to the local Stardew Valley Mods folder and generate a release zip. [F]

In Codex sandboxed runs, deployment may require permission because the Mods folder is outside the repository. [F]

## Installation

1. Install SMAPI.
2. Build or download Reality Check.
3. Put the `RealityCheck` mod folder in your Stardew Valley `Mods` folder.
4. Launch Stardew Valley through SMAPI.

## Permissions

Please do not reupload Reality Check or modified versions without permission.

You may inspect the source code for learning purposes. Translation patches, compatibility patches, or modified releases should request permission first.

## Credits

Created by Stan.

## Documentation

Start here:

- `AGENTS.md`: Codex workflow rules.
- `CURRENT_STATUS.md`: current source-verified state.
- `TESTING.md`: short testing guide.
- `ROADMAP.md`: future direction, not an implementation queue.
- `CHANGELOG.md`: project change record.
- `docs/PROJECT_OVERVIEW.md`: full project overview.
- `docs/ARCHITECTURE.md`: source architecture map.
- `docs/RECOVERED_REFERENCES.md`: recovered source index.

## Development Boundaries

- Do not change save-data structures without migration planning and Stan's confirmation. [F]
- Do not change market price, tax, exchange, or report accounting logic casually. [F]
- Do not treat old email/Nexus/planning docs as current truth without source verification. [F]
- Do not accept `dotnet build` alone as final validation for UI-facing work; verify in game through SMAPI when possible. [F]
