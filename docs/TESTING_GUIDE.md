# Testing Guide

## 1. Build

Run:

```bash
dotnet build
```

Expected:

- [F] Project compiles to `bin/Debug/net6.0/RealityCheck.dll`.
- [F] ModBuildConfig copies files into the local Stardew Valley Mods folder.
- [F] ModBuildConfig generates a zip under `bin/Debug/net6.0/`.

Codex note:

- [F] Restricted sandbox builds can fail at deployment because the Stardew Mods folder is outside the repository.
- [F] On 2026-07-07, an authorized `dotnet build` succeeded with 0 warnings and 0 errors.

## 2. Confirm Deployed Files

Check the deployed `RealityCheck` mod folder contains:

- `manifest.json`
- `RealityCheck.dll`
- `i18n/`

## 3. Launch SMAPI

Launch Stardew Valley through SMAPI and verify:

- Reality Check loads.
- Loaded version matches `manifest.json`.
- No red errors appear during startup.
- Harmony patch logs do not report unexpected failures.
- A save loads without save-data errors.

## 4. Financial Manual Check

Load a save and press `O` unless config changed.

Verify:

- Daily report opens.
- Seasonal report opens.
- Annual report opens.
- Tax report opens.
- Income and expense details are readable.
- Outstanding balance display is plausible.
- Market Price table opens.
- Search, sort, favorites, and history still work.
- Exchange button opens Commodity Exchange UI.

## 5. Tax Check

For tax work:

- Verify weekly settlement timing.
- Verify income/property/business property values are plausible.
- Verify tax notice mail opens custom UI if enabled.
- Verify signature behavior if enabled.
- Verify tax history and reports agree with settlement records.

## 6. Market Price Check

For market work:

- Verify a new day updates prices.
- Verify Market Price page and tooltips show expected prices.
- Verify shop sale and shipping-bin settlement behavior match config.
- Verify history is preserved unless migration intentionally changes it.

## 7. Exchange Check

For exchange work:

- Verify account balances and transfer flows.
- Verify contract catalog creation.
- Verify long/short position creation.
- Verify margin call/top-up/forced liquidation scenarios.
- Verify close position flow.
- Verify delivery/default/debt behavior.
- Verify exchange history text.

Optional SMAPI commands:

```text
rc_exchange_status
rc_exchange_deposit <amount>
rc_exchange_withdraw <amount>
rc_exchange_catalog
```

## 8. Log Review

Review SMAPI logs for:

- Red errors.
- Repeated warning spam.
- Save/load failures.
- Harmony patch failures.
- Market trend migration messages.
- Exchange persistence issues.

