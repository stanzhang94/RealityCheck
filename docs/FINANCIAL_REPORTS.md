# 财务报表系统

Financial Manual 是 Reality Check 的主要用户界面。它展示账本、报表、税务和 Market Price 信息。

## 当前源码事实

- [F] `FinanceMenu` 渲染 Daily、Seasonal、Annual、Tax 和 Market Price 相关页面。
- [F] `AnalyticsService` 把 ledger entries 汇总成报表数据。
- [F] `LedgerService` 保存账本 entries 和 outstanding balance。
- [F] `SaveData.Ledger` 保存账本记录。
- [F] `SaveData.FavoriteMarketCommodityKeys` 保存 Market Price 收藏。

## 报表区域

- [F] Daily report：当天报表。
- [F] Seasonal report：当前季节报表。
- [F] Annual report：当前年份报表。
- [F] Income trend：收入趋势。
- [F] Expense trend：支出趋势。
- [F] Income details：收入明细。
- [F] Expense details：支出明细。
- [F] Tax report/history：税务报表和历史。
- [F] Market Price table/history：市场价格表和历史。

## 恢复出的历史设计

- [P] 历史文档描述：当 Mod 能识别物品、数量和金额时，已知销售应显示物品明细。
- [P] 历史文档描述：无法识别来源的金币增加会进入 unclassified/unknown income。
- [P] 历史文档描述：Harvey 医保报销是 expense offset，不是普通 income。

## 当前注意事项

- [F] 不要随便改报表分类和统计口径。
- [F] 报表 UI 需要进游戏检查可读性和布局。
- [F] 如果 ledger 语义改变，要一起检查报表、税务、医疗保险、市场价格结算和 Exchange transfer。

