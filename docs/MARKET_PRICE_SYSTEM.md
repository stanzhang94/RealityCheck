# Market Price 系统

Market Price 是 Reality Check 的市场价格系统。它决定玩家看到和结算时使用的市场价。

## 当前源码事实

- [F] `MarketPriceService` 计算当前价格和市场价格表条目。
- [F] `MarketTrendService` 保存趋势状态和价格历史。
- [F] `MarketCategoryResolver` 把物品归入市场分类。
- [F] `WeatherFactorService`、`FestivalFactorService`、`OffSeasonFactorService` 提供天气、节日和反季因子。
- [F] `ShopSaleMarketPricePatch` 把市场价接入商店直售。
- [F] `ShippingSettlementTracePatch` 参与出货箱结算追踪/拦截。
- [F] `TooltipMarketPricePatch` 影响物品 tooltip 售价显示。
- [F] `FinanceMenu` 展示市场价格、搜索、收藏、排序和历史图。

## 恢复出的历史设计

- [P] 历史文档把 `MarketPriceService` 定义为市场价格真相来源。
- [P] 历史文档强调 Financial Manual 只展示价格，不负责定价。
- [P] 历史文档强调 Ledger 记录交易事实，TaxService 读取账本/税务事实，不负责生成价格。
- [P] 历史文档描述了递推式每日市场趋势模型、28 天可见历史、趋势状态、天气/节日/反季因子和工匠品传导。

## 当前注意事项

- [F] 不要在任务未明确要求时修改市场算法。
- [F] 不要随便重置 `MarketTrendSaveData.PriceHistory`。
- [F] 任何 Market Price 改动都要检查商店直售、出货箱结算、tooltip、Market Price UI、税务和报表影响。

## 未确认事项

- [U] 本次中文化任务没有重新进游戏验证 1.4.1 市场价格行为。
- [U] Nexus 上公开的市场价格 changelog 没有在当前环境中确认。

