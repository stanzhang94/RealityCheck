# Tax System

## Current Source Facts

- [F] `TaxEvents` wires tax behavior into game lifecycle events.
- [F] `TaxService` contains tax calculations.
- [F] `TaxNoticeService` and `TaxNoticeMailRouter` support custom tax notice delivery/display routing.
- [F] `TaxNoticeMenu` renders the custom notice UI.
- [F] `SaveData` persists tax records and daily property/business property assessments.
- [F] `Data/ModConfig.cs` configures tax notice mail, signature requirement, business property threshold, income tax brackets, business property rates, and property tax settings.

## Recovered Historical Design

- [P] Historical docs describe the system as weekly income tax, property tax, business property tax, outstanding balance, tax notice, signature, and tax history.
- [P] Historical docs stress that business property tax calculation and tax notice display must stay aligned.
- [P] Historical docs say unknown/unclassified income should not automatically affect income tax.

## Current Caution

- [F] Do not change tax formulas unless the task explicitly targets tax behavior.
- [F] If tax calculation changes, verify `TaxService`, `TaxNoticeMenu`, reports, tax history, and actual in-game settlement together.
- [F] Tax UI must be checked in game; build success alone is insufficient.

## Unconfirmed

- [U] Current 1.4.1 weekly tax settlement has not been reverified in game during this documentation task.

