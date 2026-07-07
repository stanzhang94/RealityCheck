# 数据与存档模型

这个文档记录 Reality Check 会写入哪些存档数据。改这些字段前必须非常小心。

## 持久化数据入口

- [F] `LedgerService` 使用 save data key `save-data`。
- [F] `MarketTrendService` 使用 save data key `market-trend-state`。
- [F] `ExchangeService` 使用 save data key `exchange-data`。

## `SaveData`

当前字段：

- [F] `Ledger`：账本记录。
- [F] `TaxRecords`：税务结算记录。
- [F] `PropertyTaxDailyAssessments`：每日房产税评估。
- [F] `BusinessPropertyTaxDailyAssessments`：每日经营资产税评估。
- [F] `SignedTaxNoticeIds`：已签名税单 ID。
- [F] `HealthInsuranceClaims`：医疗保险理赔记录。
- [F] `OutstandingBalance`：未支付余额。
- [F] `FavoriteMarketCommodityKeys`：Market Price 收藏列表。

## `MarketTrendSaveData`

当前字段：

- [F] `PricingModelVersion`：市场价格模型版本。
- [F] `TrendStates`：各商品趋势状态。
- [F] `PriceHistory`：历史价格。

## `ExchangeSaveData`

当前字段：

- [F] `Account`：Exchange 账户和持仓。
- [F] `NextContractSerial`：下一份合约序号。
- [F] `LastSettlementDateIndex`：上次结算日期索引。

## 风险规则

- [F] 不要在没有迁移计划时重命名或删除持久化字段。
- [F] 不要随便改 save data key。
- [F] 新增字段通常比改变旧字段含义安全，但仍需要说明影响。
- [F] `MarketTrendService` 里已有市场模型迁移逻辑；未来迁移必须明确保留历史还是清空历史。

