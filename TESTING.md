# Reality Check Testing Guide

Reality Check testing must include both technical checks and in-game checks. `dotnet build` alone is not final acceptance for UI or gameplay behavior.

For the expanded guide, see `docs/TESTING_GUIDE.md`.

## Build check

From the repository root:

```bash
dotnet build
```

Current observed behavior:

- The project uses `Pathoschild.Stardew.ModBuildConfig`.
- A normal build compiles the DLL and then tries to deploy the mod to the local Stardew Valley Mods folder.
- In a restricted Codex sandbox, deployment may fail because the target folder is outside the workspace.
- With permission to write to the local Stardew Valley Mods folder, `dotnet build` succeeded on 2026-07-07 with 0 warnings and 0 errors.

## Local deployment

The build package is configured to deploy to:

```text
~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/RealityCheck
```

After a successful build, confirm that the deployed mod folder contains at least:

- `manifest.json`
- `RealityCheck.dll`
- `i18n/`

## SMAPI launch verification

Launch Stardew Valley through SMAPI and check:

- SMAPI loads `Reality Check` without red errors.
- The loaded version matches `manifest.json`.
- No startup errors appear from Harmony patches, content assets, config loading, or save-data loading.
- A save can be loaded without save-data migration errors.

## In-game UI verification

Load a save, then press the configured report hotkey. Default:

```text
O
```

Verify the Financial Manual / finance UI:

- Daily report opens.
- Seasonal report opens.
- Annual report opens.
- Tax report opens.
- Tax history is readable.
- Income details are readable.
- Expense details are readable.
- Outstanding balance display is correct.
- Market price table opens and is readable.
- Market price sorting/search/favorites still behave as expected.
- Exchange button opens the Commodity Exchange UI when available.

## Tax verification

For tax-related changes, verify in game:

- Weekly tax assessment occurs at the expected time.
- Income tax, property tax, and business property tax values are plausible.
- Tax notice mail appears if enabled.
- Tax notice text layout is readable.
- Signature requirement works if enabled.
- Closing behavior works, including the safety exit.
- Tax records remain visible through reports.

## Market price verification

For market price changes, verify in game:

- Daily market prices update on a new day.
- Market price table displays item, base price, daily multiplier, total multiplier, and market price.
- Shipping-bin settlement behavior matches config.
- Shop sale prices and tooltip prices are affected only as intended.
- Existing market trend history is not unexpectedly reset.

## Exchange verification

For exchange-related changes, verify in game:

- Exchange account view displays total, locked, available cash, debt, positions, and history.
- Deposit and withdraw flows update both farm money and exchange account state.
- Contract catalog lists tradable items.
- Long and short positions can be created only when valid.
- Margin calls, top-ups, close position, delivery, default, and debt collection behavior match the intended scenario.
- Exchange history text is readable in the current locale.

Optional SMAPI console checks:

```text
rc_exchange_status
rc_exchange_deposit <amount>
rc_exchange_withdraw <amount>
rc_exchange_catalog
```

## Log review

After in-game testing, check the SMAPI log for:

- Red errors.
- Repeated warning spam.
- Harmony patch failures.
- Save/load failures.
- Market trend migration messages.
- Unexpected ledger or exchange persistence issues.

## Acceptance rule

For gameplay or UI changes, final validation should say both:

- whether `dotnet build` passed; and
- what was verified in Stardew Valley/SMAPI, or why in-game verification was not performed.
