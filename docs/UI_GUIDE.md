# UI 使用指南

这个文档给进游戏检查 Reality Check UI 的人看。

## 打开 Financial Manual

- [F] 默认快捷键是 `O`。
- [F] 快捷键由 `config.json` 里的 `OpenReportKey` 控制。
- [F] `ModEntry.OnButtonsChanged` 会在世界已载入、且当前没有其他菜单时打开 `FinanceMenu`。

操作步骤：

1. 通过 SMAPI 启动 Stardew Valley。
2. 载入一个存档。
3. 按 `O`。
4. 确认 Financial Manual 打开。

## Financial Manual 页面

当前源码中可以看到这些报表或视图：

- [F] Daily report：查看当天收入、支出和明细。
- [F] Seasonal report：查看当前季节到目前为止的趋势和明细。
- [F] Annual report：查看当前年份的趋势和明细。
- [F] Tax report：查看税务相关信息。
- [F] Market Price report：查看当前市场价格表。
- [F] Market Price history chart：点击市场商品后查看历史价格图。
- [F] Income details / Expense details：查看收入和支出明细。

## Market Price 页面

Market Price 是市场价格页面。它显示 Reality Check 当前认为的市场价。

检查重点：

- [F] 物品名称、市场价、原版基础价、当日倍率、总倍率。
- [F] 排序功能。
- [F] 搜索框。
- [F] 收藏标记。
- [F] 点击物品后的历史价格图。

## Exchange UI

Exchange 是商品交易所 UI。

- [F] 从 Financial Manual 进入。
- [F] 当前源码包含账户页、创建合约、持仓页等流程。
- [F] UI 会显示账户历史、余额、持仓、风险图、交割操作和确认弹窗。

检查重点：

- 账户余额和可用现金是否可读。
- Deposit / Withdraw 是否清楚。
- 合约列表是否可读。
- 持仓状态、保证金、盈亏、交割按钮是否可读。
- 弹窗文字是否被遮挡。

## 当前未验收内容

- [U] 本次中文化任务没有进游戏验证 1.4.1 Financial Manual 布局。
- [U] 本次中文化任务没有进游戏验证 1.4.1 Exchange UI 布局。
- [U] 本次中文化任务没有检查所有语言下的文本适配。

