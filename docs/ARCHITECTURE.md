# Architecture

## Entry Point

- [F] `ModEntry.cs` is the SMAPI entry point.
- [F] It initializes config, ledger, analytics, market, tax, exchange, and notice services.
- [F] It applies Harmony patches for shop sales, shop market prices, tooltip prices, and shipping settlement tracing.
- [F] It registers exchange debug console commands.
- [F] It opens `FinanceMenu` when `OpenReportKey` is pressed and no other clickable menu is active.

## Service Layer

- [F] `LedgerService` owns persisted financial save data under the key `save-data`.
- [F] `AnalyticsService` builds report summaries from ledger data.
- [F] `TaxService` calculates income tax, property tax, and business property tax.
- [F] `MarketPriceService` is the current market price source.
- [F] `MarketTrendService` persists market trends and price history under `market-trend-state`.
- [F] `ExchangeService` persists exchange account data under `exchange-data`.
- [F] `ExchangeContractCatalogService` builds tradable exchange contract candidates from market data.

## UI Layer

- [F] `UI/FinanceMenu.cs` implements the Financial Manual reports and Market Price page.
- [F] `UI/ExchangeMenu.cs` implements the Commodity Exchange UI.
- [F] `UI/TaxNoticeMenu.cs` implements custom tax notice display/signature behavior.

## Data Flow

1. [F] Game events and Harmony patches detect income, expenses, market-price sale effects, and daily lifecycle events.
2. [F] `LedgerService` records financial facts and persists them per save.
3. [F] `MarketTrendService` keeps market trend state and history per save.
4. [F] `MarketPriceService` calculates current market prices from config, categories, weather/festival/off-season factors, trends, and item identity.
5. [F] `AnalyticsService` reads ledger entries to build UI summaries.
6. [F] `TaxService` reads ledger and game state to calculate taxes.
7. [F] `FinanceMenu`, `TaxNoticeMenu`, and `ExchangeMenu` display the results.

## Configuration

- [F] `Data/ModConfig.cs` defines `Tax`, `Market`, and `OpenReportKey`.
- [F] Default `OpenReportKey` is `O`.
- [F] Market config includes shipping-bin shadow/trace toggles and market settlement enablement.
- [F] Tax config includes notice mail/signature toggles, business property tax threshold, income tax brackets, and property tax settings.

## Build And Deploy

- [F] `RealityCheck.csproj` targets `net6.0`.
- [F] It references `Pathoschild.Stardew.ModBuildConfig` and `Lib.Harmony`.
- [F] `dotnet build` compiles, deploys to the configured Stardew Valley Mods directory, and generates a release zip.

