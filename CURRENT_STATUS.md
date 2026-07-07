# Reality Check Current Status

Last updated: 2026-07-07

## Project identity

- [F] Project: Reality Check
- [F] Type: Stardew Valley SMAPI mod
- [F] Unique ID: `Stan.RealityCheck`
- [F] Current manifest version: `1.4.1`
- [F] Target framework: `.NET 6.0`
- [F] Minimum SMAPI API version: `4.0.0`
- [F] Declared game requirement in README: Stardew Valley 1.6+
- [F] Documentation recovery branch: `docs/recover-project-docs`

## Current repository state

- [F] Git status before first documentation onboarding: clean on `main...origin/main`.
- [F] Recent head commit before this recovery task: `88dac9c Release Reality Check 1.4.1 UI layout update`.
- [F] README has now been refreshed as part of the documentation recovery task.
- [P] Gmail recovery found older project-total documents for 1.0-1.3.4 and exchange planning. Those are treated as historical references, not current truth.
- [U] Nexus public page/version data was not confirmed from the current environment.

## Implemented feature areas observed in source

- [F] Financial ledger and analytics.
- [F] Daily, seasonal, annual, tax, income, expense, and balance reporting through `FinanceMenu`.
- [F] Weekly tax system, including income tax, property tax, business property tax, tax records, and custom tax notice UI.
- [F] Health insurance tracking and Harvey claim mail.
- [F] Dynamic market price services, trend state, daily price updates, shop-sale price patches, tooltip price patches, and market price table UI.
- [F] Shipping-bin settlement tracking and optional market settlement behavior.
- [F] Pelican Town Commodity Exchange code, including account balances, deposits, withdrawals, contract catalog, positions, margin calls, close position flow, delivery/default flow, exchange history, and exchange UI.
- [F] i18n resources for default, German, French, Japanese, and Chinese locale files.

## Main source map

- `ModEntry.cs`: SMAPI entry point, service wiring, Harmony patch setup, save/load hooks, hotkey handling, exchange console commands.
- `Data/`: mod config and save-data containers for ledger, taxes, market trends, exchange account state, and preferences.
- `Models/`: records and DTO-style state objects for ledger entries, summaries, tax records, market prices/trends, and exchange positions/accounts/contracts.
- `Events/`: day/start/end and gameplay event handlers for income, expenses, and taxes.
- `Services/LedgerService.cs`: persistent financial ledger and outstanding balance state.
- `Services/AnalyticsService.cs`: report aggregation and financial summaries.
- `Services/TaxService.cs`: tax calculations and assessments.
- `Services/MarketPriceService.cs`: current market price calculation and table generation.
- `Services/MarketTrendService.cs`: persistent market trend state and pricing model migration.
- `Services/ExchangeService.cs`: exchange account, transfers, positions, settlement, margin, close, delivery, and debt behavior.
- `Services/ExchangeContractCatalogService.cs`: tradable contract catalog generation from market price data.
- `Services/ConfigService.cs`: config load/defaulting.
- `Services/I18n.cs`: translation helper methods.
- `Patches/`: Harmony patches for shop sales, market sale prices, tooltips, and shipping settlement tracing.
- `UI/FinanceMenu.cs`: in-game Financial Manual / finance report UI, including market prices and exchange entry button.
- `UI/ExchangeMenu.cs`: in-game commodity exchange UI.
- `UI/TaxNoticeMenu.cs`: custom tax notice UI.
- `i18n/`: localized UI strings.

## Verification status

- [F] 2026-07-07: `dotnet build` succeeded after allowing the ModBuildConfig deploy step to write to the local Stardew Valley Mods folder.
- [F] The same build failed inside the restricted sandbox because deployment tried to write outside the workspace to `~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/RealityCheck/manifest.json`.
- [F] 2026-07-07 documentation recovery task: authorized `dotnet build` succeeded with 0 warnings and 0 errors, deployed the mod locally, and generated `RealityCheck 1.4.1.zip`.
- [U] No in-game SMAPI launch or UI verification has been performed during this documentation recovery pass.

## Known risks and notes

- [F] Exchange functionality already exists in source despite older planning notes treating it as future work. Future work should treat exchange as existing code, not as a greenfield feature.
- [F] Save-data structure changes are high risk and require explicit confirmation before implementation.
- [F] UI verification matters for this project; terminal success alone is not accepted as final validation for UI-facing work.
- [P] Historical email docs report prior in-game checks for 1.3.x tooltip/market behavior, but those do not replace current 1.4.1 verification.
