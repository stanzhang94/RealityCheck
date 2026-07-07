# Data And Save Model

## Persisted Save Data

- [F] `LedgerService` uses save data key `save-data`.
- [F] `MarketTrendService` uses save data key `market-trend-state`.
- [F] `ExchangeService` uses save data key `exchange-data`.

## `SaveData`

Current fields:

- [F] `Ledger`
- [F] `TaxRecords`
- [F] `PropertyTaxDailyAssessments`
- [F] `BusinessPropertyTaxDailyAssessments`
- [F] `SignedTaxNoticeIds`
- [F] `HealthInsuranceClaims`
- [F] `OutstandingBalance`
- [F] `FavoriteMarketCommodityKeys`

## `MarketTrendSaveData`

Current fields:

- [F] `PricingModelVersion`
- [F] `TrendStates`
- [F] `PriceHistory`

## `ExchangeSaveData`

Current fields:

- [F] `Account`
- [F] `NextContractSerial`
- [F] `LastSettlementDateIndex`

## Risk Rules

- [F] Do not rename or remove persisted fields without migration planning.
- [F] Do not change save-data keys casually.
- [F] Adding fields may be safer than changing existing semantics, but still needs explicit review.
- [F] Market trend migrations exist in `MarketTrendService`; any future pricing model migration should preserve or intentionally reset history with clear notes.

