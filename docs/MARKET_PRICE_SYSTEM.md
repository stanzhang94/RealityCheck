# Market Price System

## Current Source Facts

- [F] `MarketPriceService` calculates current prices and market price table entries.
- [F] `MarketTrendService` persists trend state and price history.
- [F] `MarketCategoryResolver` maps items into market categories.
- [F] `WeatherFactorService`, `FestivalFactorService`, and `OffSeasonFactorService` provide contextual factors.
- [F] `ShopSaleMarketPricePatch` applies market prices to shop sale behavior.
- [F] `ShippingSettlementTracePatch` participates in shipping-bin settlement tracing/interception.
- [F] `TooltipMarketPricePatch` affects item tooltip price display.
- [F] `FinanceMenu` displays market prices, search, favorites, sorting, and history.

## Recovered Historical Design

- [P] Historical docs define `MarketPriceService` as the market price source of truth.
- [P] Historical docs say Financial Manual displays prices but does not generate them.
- [P] Historical docs say Ledger records transaction facts and TaxService reads ledger/tax facts rather than generating prices.
- [P] Historical docs describe a recursive/daily market trend model with 28-day visible history, trend states, weather/festival/off-season factors, and artisan transmission.

## Current Caution

- [F] Do not modify market algorithms unless the task explicitly asks.
- [F] Do not reset `MarketTrendSaveData.PriceHistory` casually.
- [F] Any market change should verify shop sale, shipping-bin settlement, tooltip display, Market Price UI, and tax/report side effects.

## Unconfirmed

- [U] Current 1.4.1 in-game price behavior has not been reverified during this documentation task.
- [U] Exact Nexus-public market changelog could not be confirmed.

