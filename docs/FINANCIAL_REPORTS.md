# Financial Reports

## Current Source Facts

- [F] `FinanceMenu` renders Daily, Seasonal, Annual, Tax, and Market Price tabs/views.
- [F] `AnalyticsService` aggregates ledger entries into report summaries.
- [F] `LedgerService` persists entries and outstanding balance.
- [F] `SaveData.Ledger` stores ledger entries.
- [F] `SaveData.FavoriteMarketCommodityKeys` stores market favorites.

## Report Areas

- [F] Daily report.
- [F] Seasonal report.
- [F] Annual report.
- [F] Income trend.
- [F] Expense trend.
- [F] Income details.
- [F] Expense details.
- [F] Tax report/history.
- [F] Market Price table/history.

## Recovered Historical Design

- [P] Historical docs describe known item sales as detailed where the mod can identify item, quantity, and amount.
- [P] Historical docs describe unclassified/unknown income as a fallback for money gains the mod cannot identify.
- [P] Historical docs describe Harvey reimbursement as an expense offset rather than normal income.

## Current Caution

- [F] Do not change report accounting categories casually.
- [F] Report UI needs in-game verification for readability and layout.
- [F] If ledger semantics change, review reports, taxes, health insurance, market price settlement, and exchange transfers together.

