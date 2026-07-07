# 架构说明

这个文档说明 Reality Check 当前源码的大致结构。类名、文件名和 SMAPI 术语保持英文。

## 入口

- [F] `ModEntry.cs` 是 SMAPI 入口。
- [F] 它初始化 config、ledger、analytics、market、tax、exchange 和 notice 相关服务。
- [F] 它安装 Harmony patch，包括商店销售、商店市场价、tooltip 市场价和出货箱结算追踪。
- [F] 它注册 Exchange 调试用 console commands。
- [F] 当玩家按下 `OpenReportKey` 且当前没有其他菜单时，它打开 `FinanceMenu`。

## 服务层

- [F] `LedgerService` 管理财务账本，并用 save data key `save-data` 持久化。
- [F] `AnalyticsService` 根据 ledger 数据生成报表汇总。
- [F] `TaxService` 计算 Income Tax、Property Tax 和 Business Property Tax。
- [F] `MarketPriceService` 是当前市场价格来源。
- [F] `MarketTrendService` 用 `market-trend-state` 保存市场趋势和历史价格。
- [F] `ExchangeService` 用 `exchange-data` 保存 Exchange 账户和持仓状态。
- [F] `ExchangeContractCatalogService` 根据市场数据生成可交易合约候选。

## UI 层

- [F] `UI/FinanceMenu.cs` 实现 Financial Manual、报表和 Market Price 页面。
- [F] `UI/ExchangeMenu.cs` 实现 Commodity Exchange UI。
- [F] `UI/TaxNoticeMenu.cs` 实现自定义 Tax Notice 和签名显示。

## 数据流

1. [F] 游戏事件和 Harmony patch 检测收入、支出、市场价销售效果和每日生命周期。
2. [F] `LedgerService` 记录财务事实，并按存档保存。
3. [F] `MarketTrendService` 保存每个存档的市场趋势和历史价格。
4. [F] `MarketPriceService` 根据配置、分类、天气/节日/反季因子、趋势和物品身份计算当前市场价。
5. [F] `AnalyticsService` 从 ledger entries 生成 UI 报表汇总。
6. [F] `TaxService` 从 ledger 和游戏状态计算税务。
7. [F] `FinanceMenu`、`TaxNoticeMenu`、`ExchangeMenu` 负责展示结果。

## 配置

- [F] `Data/ModConfig.cs` 定义 `Tax`、`Market` 和 `OpenReportKey`。
- [F] 默认 `OpenReportKey` 是 `O`。
- [F] Market config 包括出货箱 shadow/trace 开关和市场结算开关。
- [F] Tax config 包括税单邮件、签名要求、经营资产税阈值、所得税档位和房产税设置。

## 构建和部署

- [F] `RealityCheck.csproj` 目标框架是 `net6.0`。
- [F] 项目引用 `Pathoschild.Stardew.ModBuildConfig` 和 `Lib.Harmony`。
- [F] `dotnet build` 会编译、部署到配置的 Stardew Valley Mods 目录，并生成 zip。

