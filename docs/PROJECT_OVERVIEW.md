# Project Overview

Reality Check is a Stardew Valley SMAPI mod focused on economic pressure, financial feedback, taxes, market prices, and exchange-style risk management. [F]

It is not a web app, SaaS project, or background automation service. [F]

## Current Identity

- [F] Name: Reality Check
- [F] Author: Stan
- [F] Unique ID: `Stan.RealityCheck`
- [F] Current version in `manifest.json`: `1.4.1`
- [F] Target framework: `net6.0`
- [F] Minimum SMAPI API: `4.0.0`

## Current Scope

Reality Check currently includes:

- [F] Financial ledger and report UI.
- [F] Tax assessment, tax notice, and tax history systems.
- [F] Medical expense and health insurance reimbursement tracking.
- [F] Market price calculation, market trend state, market price table, and price-related Harmony patches.
- [F] Pelican Town Commodity Exchange systems and UI.

## Design Intent

- [P] Historical project docs describe the mod as making Stardew Valley farm money less consequence-free.
- [P] The recurring design line is that income should have sources, expenses should have categories, taxes should have rules, assets should create pressure, and market prices should not remain static forever.
- [F] Current source implements that direction through services, persisted state, UI menus, and Harmony patches.

## Current Boundary

- [F] Exchange exists in current source; it is no longer only a future concept.
- [P] Banking, loans, credit, and broader debt systems appear in historical docs as possible future directions.
- [U] No current source implementation of a full bank/loan system was verified.
- [F] Save-data changes are high risk because the mod persists ledger, tax, market trend, favorites, and exchange state.

