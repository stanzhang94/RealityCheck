# Reality Check Roadmap

This file records possible future direction. Do not treat any item here as permission to implement it automatically.

## Current baseline

As of 2026-07-07, the local source already includes:

- [F] Financial reports and ledger tracking.
- [F] Weekly taxes and tax notice UI.
- [F] Health insurance claim tracking.
- [F] Dynamic market prices and market price UI.
- [F] Commodity Exchange account, contracts, positions, margin, delivery/default, and UI flows.

## Maintenance priorities

- Keep save compatibility stable.
- Keep accounting categories and report totals understandable.
- Keep tax calculations explainable and testable in game.
- Keep market price changes scoped and visible in the Financial Manual.
- Keep UI layout readable in supported locales.
- Keep README and `docs/` aligned with the current manifest/source.

## Candidate future work

- Better manual test scenarios and save files for reports, taxes, market prices, and exchange flows.
- More complete README and release notes for 1.4.x.
- Focused UI polish where in-game screenshots show crowding or unreadable text.
- Safer diagnostics for market pricing, tax assessments, and exchange settlement.
- [P] Optional future banking, loans, or investment systems only after explicit planning and confirmation. Historical docs discuss bank/debt/credit after exchange, but no current implementation was verified.

## Out of scope unless explicitly requested

- Large architecture rewrites.
- Save-data schema changes without migration planning.
- New major systems such as banking or loans.
- Further exchange expansion beyond maintenance/fixes.
- Automatic publishing, tagging, or release packaging.
