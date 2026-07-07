# Recovered References

This index records source summaries only. Do not copy private email bodies into the repository.

## Local Source

- [F] `manifest.json`: mod name, author, version `1.4.1`, description, unique ID, entry DLL, minimum SMAPI API.
- [F] `RealityCheck.csproj`: `net6.0`, ModBuildConfig, Harmony dependency.
- [F] `ModEntry.cs`: service setup, patches, event hooks, UI hotkey, exchange commands.
- [F] `Data/`: config and persisted save-data models.
- [F] `Services/`: ledger, analytics, tax, market, exchange, config, localization, notice services.
- [F] `UI/`: Financial Manual, Exchange menu, Tax Notice menu.
- [F] `Patches/`: shop sale, shop market price, shipping trace, tooltip market price patches.
- [F] `i18n/`: default, German, French, Japanese, Chinese text.

## Local Git

- [F] Recent head before recovery: `88dac9c Release Reality Check 1.4.1 UI layout update`.
- [F] Tags found: `v1.2.2`, `v1.2.3`.
- [F] Git history confirms market and exchange development commits from 1.3.x and 1.4.x.

## Gmail / Email

Search scope: Gmail connector searches for Reality Check, Stardew, SMAPI, Financial Manual, Market Price, Exchange, version numbers, and Chinese project terms.

Recovered summaries:

- [P] `Reality Check 1.0-1.3.0 整体总文档`, 2026-06-29: historical project overview from early tax/insurance/reports through 1.3.0 market pricing.
- [P] `Reality Check 1.3.4版本项目总文档`, 2026-06-30: historical project total document through 1.3.4 tooltip market price display.
- [P] `RealityCheck 市场趋势规则 6月29号22点37分`, 2026-06-29: market trend/balance design notes for 1.3.3.
- [P] `Reality Check交易所规划V1`, 2026-06-29: exchange planning around account, standardized contracts, margin, delivery/default, and MarketPriceService as price source.
- [P] `Reality Check交易所清算原则`, 2026-06-28: exchange clearing rules, no cash delivery, daily mark-to-market, forced liquidation, delivery/default, exchange debt.
- [P] `RealityCheck1.4实现路径文档V1版本（草稿）`, 2026-07-02: draft notice that a full 1.4 implementation path document was too long to send directly.
- [P] `rc交易所规则补充`, 2026-06-30: email with attachment `Reality Check交易所规划V2（2026-06-30 09-13）.docx`; attachment was discovered but not imported into repo.

## Nexus

- [U] Current environment did not confirm a public Nexus page/version history for Reality Check through web search.
- [U] Nexus descriptions, files, posts, bugs, and version history should be checked manually or with user-provided URLs/exports before public release work.

## Conflicts And Source Priority

- [F] Older README content described older 1.0/1.2.3 boundaries and did not fully match current `manifest.json` 1.4.1/source state.
- [F] Older planning docs describe exchange as future work, while current source includes exchange services, data, commands, and UI.
- [F] Current source and manifest are authoritative for current state.

