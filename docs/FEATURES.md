# 功能清单

## 财务报表

- [F] `FinanceMenu` 中存在 Daily、Seasonal、Annual 报表。
- [F] `AnalyticsService` 汇总收入和支出明细。
- [F] `OutstandingBalance` 存在于 `SaveData`。
- [F] Market Price 页面和历史图是 Financial Manual 的一部分。

## 税务

- [F] `TaxService` 和 `TaxEvents` 实现税务评估/结算行为。
- [F] 当前税务模型包括 Income Tax、Property Tax、Business Property Tax。
- [F] `TaxNoticeMenu` 处理自定义 Tax Notice UI 和签名行为。
- [F] 税务记录保存在 `SaveData.TaxRecords`。

## 医疗保险

- [F] `ExpenseEvents` 和 `HealthInsuranceNoticeService` 支持医疗费用和报销记录。
- [F] `SaveData.HealthInsuranceClaims` 保存理赔数据。
- [P] 历史文档描述 Harvey 医保报销属于 expense offset，不是普通 income。

## Market Price

- [F] `MarketPriceService` 计算市场价格。
- [F] `MarketTrendService` 保存趋势状态和 28 天风格的历史数据。
- [F] `ShopSaleMarketPricePatch`、`ShippingSettlementTracePatch`、`TooltipMarketPricePatch` 把市场价格接入可见 UI 和实际销售行为。
- [F] `FinanceMenu` 显示 Market Price 排序、搜索、收藏和历史。

## Commodity Exchange

- [F] `ExchangeService`、`ExchangeContractCatalogService`、`ExchangeSaveData` 和 `ExchangeMenu` 存在于当前源码。
- [F] 当前源码中的 Exchange 包括账户余额、可用余额、锁定保证金、持仓、margin call、平仓、交割/default、账户历史、债务追缴和 transfer records。
- [P] 历史规划文档把 Exchange 描述为建立在 Market Price 系统上的风险管理系统，而不是独立随机小游戏。

## 本地化

- [F] 当前存在 `default`、`de`、`fr`、`ja`、`zh` 文本文件。
- [P] 历史文档说曾测试韩语，但因 Tax Notice / mail 问题未保留。当前源码没有 `ko.json`。

