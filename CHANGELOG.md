# CHANGELOG

## Unreleased

- Financial Manual 新增按住鼠标左键或触摸后上下拖动正文滚动，并保留原有鼠标滚轮操作。
- 交易所账户页保留原有自定义金额输入，并新增 `+100`、`+1K`、`+10K`、`+100K`、`清零` 快捷金额按钮；所有按钮与原输入框共用同一金额。
- 快捷金额按钮用于 Android 系统键盘未弹出时的基础输入补充，不会直接执行转账，也不属于完整 Android 适配；Android 实机效果仍需玩家验证。
- 本次仅为基础触屏滚动适配，不属于完整 Android UI 重构；Android 实机效果仍需玩家验证。
- 将 Mod 版本更新为 `1.4.3`。
- 修复带品质物品的商店售价、出货箱结算和 Tooltip，使其在普通品质标准 Market Price 上只应用一次品质系数。
- 修复 Exchange 多头实物交割超过单堆上限时的物品丢失；交割现在会按实际堆叠上限拆分并在背包容量不足时完整失败。
- Restored 7-day contract eligibility for fast-growing crops and fruit-tree produce.
- 将 Mod 版本更新为 `1.4.2`。
- 删除复杂的分散工作流文档，改为一个中文优先的 `PROJECT_DOCUMENTATION.md` 项目总文档。
- 简化 `README.md`，让它只作为项目入口和阅读指引。
- 简化 `.gitignore`，保留当前项目需要的基础忽略规则。
- 保留 `CHANGELOG.md` 作为轻量变更记录。
- 本次没有修改业务代码。

## Historical Notes

- [F] Git history contains tags `v1.2.2` and `v1.2.3`.
- [F] Git history records `1.3.0`, `1.3.1`, `1.3.3`, `1.3.4`, `1.4.0`, and `1.4.1` commits.
- [P] Gmail recovery contains older project-total documents for 1.0-1.3.4, tax details, market pricing, and Exchange planning. See `PROJECT_DOCUMENTATION.md`.
