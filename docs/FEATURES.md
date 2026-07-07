# Features

## Financial Reports

- [F] Daily, seasonal, and annual reports exist in `FinanceMenu`.
- [F] Income and expense details are aggregated by `AnalyticsService`.
- [F] Outstanding balance is stored in `SaveData`.
- [F] Market Price pages and history charts are part of the Financial Manual.

## Taxes

- [F] `TaxService` and `TaxEvents` implement tax assessment/settlement behavior.
- [F] Current tax model includes income tax, property tax, and business property tax.
- [F] `TaxNoticeMenu` handles custom tax notice UI and signature behavior.
- [F] Tax records persist in `SaveData.TaxRecords`.

## Health Insurance

- [F] `ExpenseEvents` and `HealthInsuranceNoticeService` support medical expense/reimbursement tracking.
- [F] `SaveData.HealthInsuranceClaims` stores claim data.
- [P] Historical docs describe Harvey insurance reimbursement as an expense offset, not normal income.

## Market Prices

- [F] `MarketPriceService` calculates market prices.
- [F] `MarketTrendService` persists trend states and 28-day style history data.
- [F] `ShopSaleMarketPricePatch`, `ShippingSettlementTracePatch`, and `TooltipMarketPricePatch` connect market prices to visible/gameplay sale behavior.
- [F] `FinanceMenu` displays market price sorting, search, favorites, and history.

## Commodity Exchange

- [F] `ExchangeService`, `ExchangeContractCatalogService`, `ExchangeSaveData`, and `ExchangeMenu` exist in current source.
- [F] Current exchange concepts in source include account balance, available balance, locked margin, positions, margin calls, close position, delivery/default, account history, debt collection, and transfer records.
- [P] Historical planning docs describe the exchange as risk management built on the market price system, not a standalone random minigame.

## Localization

- [F] Locale files exist for `default`, `de`, `fr`, `ja`, and `zh`.
- [P] Historical docs say Korean was tested earlier and not retained due tax notice/mail issues. No `ko.json` exists in current source.

