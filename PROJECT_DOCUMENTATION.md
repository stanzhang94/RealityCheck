# Reality Check 项目总文档

本文档是 Reality Check 的唯一项目总文档，给 Stan、Codex 和以后接手项目的 AI 使用。

标注规则：

- `[F]`：当前源码、manifest、git 历史或本地文件可以直接支持。
- `[P]`：根据旧邮件、旧文档或代码结构推断，尚未完全等同于当前事实。
- `[U]`：当前无法确认。

旧邮件和 Nexus 资料只作为历史资料。当前事实以本地源码、`manifest.json`、git 历史和实际运行结果为准。

## 1. 项目一句话说明

Reality Check 是 Stan 的 Stardew Valley 本地 SMAPI Mod，用财务报表、税务、市场价格和 Exchange 系统，让农场经营更接近有财务压力、有市场波动、有账本反馈的状态。[F]

## 2. 当前版本与运行环境

| 项目 | 当前状态 | 来源 |
| --- | --- | --- |
| Mod 名称 | Reality Check | `manifest.json` [F] |
| 作者 | Stan | `manifest.json` [F] |
| 当前版本 | `1.4.2` | `manifest.json` [F] |
| UniqueID | `Stan.RealityCheck` | `manifest.json` [F] |
| Entry DLL | `RealityCheck.dll` | `manifest.json` [F] |
| 最低 SMAPI API | `4.0.0` | `manifest.json` [F] |
| .NET TargetFramework | `net6.0` | `RealityCheck.csproj` [F] |
| 主要依赖 | `Pathoschild.Stardew.ModBuildConfig` `4.3.0`，`Lib.Harmony` `2.3.3` | `RealityCheck.csproj` [F] |
| 默认 Financial Manual 热键 | `O` | `Data/ModConfig.cs` [F] |
| Stardew Valley 版本 | 面向 Stardew Valley 1.6 系列的 SMAPI Mod | 旧 README 和依赖环境推断 [P] |
| 项目根目录 | `/Users/stan/code/RealityCheck` | 当前 Codex 工作区 [F] |
| 当前工作流 | `main` 单线开发，当前任务不创建 PR | 本次任务要求和当前 git 状态 [F] |
| 最近一次 `dotnet build` | `1.4.2` 两项核心修复和 7 日合约资格修复均已正常构建，0 warning、0 error | 本地构建结果 [F] |
| release zip | 使用正式发布结构生成 `RealityCheck-1.4.2.zip` | 本次发布任务 [F] |
| 游戏内 UI 验证 | `1.4.2` 品质售价、大额多头交割和 7 日合约资格已完成人工测试 | Stan 人工验收 [F] |

`1.4.2` 保持现有存档结构，集中修复品质售价、大额多头交割和 7 日合约资格，不扩展新的 Exchange 功能。[F]

## 3. 当前项目包含的主要功能

| 功能 | 当前状态 | 主要源码位置 |
| --- | --- | --- |
| 财务账本 | 记录收入、支出、税款、医保、Exchange 转账和未分类收入。 | `Services/LedgerService.cs`、`Models/LedgerEntry.cs` [F] |
| Financial Manual | 游戏内财务手册，包含日报、季度报、年报、税务和 Market Price 页面。 | `UI/FinanceMenu.cs` [F] |
| 所得税 | 每周结算，对 Shipping Bin 收入按整档税率征税。 | `Services/TaxService.cs`、`Data/ModConfig.cs` [F] |
| 房产税 | 每日评估建筑、温室、房屋升级和农业扣除，每周汇总结算。 | `Services/TaxService.cs` [F] |
| 经营资产税 | 每日扫描机器数量，超过阈值后按机器类型和规模倍率征税。 | `Services/TaxService.cs`、`Data/ModConfig.cs` [F] |
| 税单 UI | 每周税单邮件和自定义税单界面。 | `Services/TaxNoticeService.cs`、`Services/TaxNoticeMailRouter.cs`、`UI/TaxNoticeMenu.cs` [F] |
| 医保系统 | 记录每日医保保费、医疗支出和医保报销。 | `Events/ExpenseEvents.cs`、`Models/HealthInsuranceClaim.cs` [F] |
| 市场价格系统 | 根据趋势、天气、节日、淡季、品类和历史价格生成 Market Price。 | `Services/MarketPriceService.cs`、`Services/MarketTrendService.cs` [F] |
| Market Price UI | Financial Manual 内的市场价格表，支持搜索、排序、收藏和历史图表。 | `UI/FinanceMenu.cs` [F] |
| Shipping Bin 市场结算 | 出货箱收入可以按 Market Price 结算。 | `Events/IncomeEvents.cs`、`Services/MarketPriceService.cs` [F] |
| 商店出售市场价格 | 商店售卖通过 Harmony patch 接入市场价格与账本。 | `Patches/ShopSalePatch.cs`、`Patches/ShopSaleMarketPricePatch.cs` [F] |
| Tooltip 市场价格 | 物品 tooltip 中显示市场价格信息。 | `Patches/TooltipMarketPricePatch.cs` [F] |
| Exchange | Pelican Town Commodity Exchange，含账户、保证金、持仓、平仓、交割、违约和债务。 | `Services/ExchangeService.cs`、`UI/ExchangeMenu.cs`、`Data/ExchangeSaveData.cs` [F] |
| 多语言 | 默认、中文、日文、德文、法文文本文件。 | `i18n/` [F] |

## 4. 项目目录和代码结构

| 路径 | 作用 |
| --- | --- |
| `ModEntry.cs` | Mod 入口。初始化配置、账本、税务、市场、Exchange、Harmony patch、事件和热键。 [F] |
| `RealityCheck.csproj` | .NET 6 项目文件和 SMAPI 构建依赖。 [F] |
| `manifest.json` | SMAPI manifest，记录名称、版本、入口 DLL、最低 API。 [F] |
| `Data/` | 配置和存档数据模型，例如 `ModConfig`、`SaveData`、`MarketTrendSaveData`、`ExchangeSaveData`。 [F] |
| `Models/` | 账本、税务、市场价格、Exchange 和报表使用的数据结构。 [F] |
| `Services/` | 核心逻辑层：账本、税务、市场价格、市场趋势、Exchange、统计分析、配置、邮件和本地化。 [F] |
| `Events/` | SMAPI 事件接入：收入、支出、税务每日检查。 [F] |
| `Patches/` | Harmony patch：商店出售、tooltip、shipping trace 等。 [F] |
| `UI/` | 游戏内菜单：Financial Manual、Exchange、Tax Notice。 [F] |
| `i18n/` | 本地化文本。 [F] |

## 5. 税务系统总说明

税务系统分为三类税：所得税、房产税、经营资产税。[F]

税务节奏：

- 每天开始时，`TaxEvents.OnDayStarted` 会尝试结算上一税周，并生成当天房产税和经营资产税评估。[F]
- 结算日是每月 1、8、15、22 日。[F]
- 8 日结算本季 1-7 日，15 日结算 8-14 日，22 日结算 15-21 日，1 日结算上季 22-28 日。[F]
- 如果某个税周已经有记录，源码会跳过重复结算。[F]
- 税款会通过 `LedgerService.ChargeObligation` 进入账本义务，不只是 UI 展示。[F]
- 如果启用税单邮件，结算后会发送 Tax Notice。[F]

税务系统的核心原则来自旧资料：所得税看申报的出货箱收入，房产税看固定资产和潜在收益，经营资产税看生产能力和规模，而不是逐台机器当天是否实际开工。[P]

## 6. 所得税公式

当前源码使用“整档税率”，不是边际税率。[F]

公式：

```text
周应税出货箱收入 = 本税周内 Type == "Income" 且来源为 Shipping Bin 的账本收入合计
适用税率 = 最后一个 MinimumTaxableIncome <= 周应税出货箱收入 的税档 Rate
所得税 = floor(周应税出货箱收入 * 适用税率)
```

税档：

| 周应税出货箱收入 | 税率 | 来源 |
| --- | ---: | --- |
| `0g+` | `0%` | `Data/ModConfig.cs` [F] |
| `5001g+` | `5%` | `Data/ModConfig.cs` [F] |
| `20001g+` | `8%` | `Data/ModConfig.cs` [F] |
| `50001g+` | `12%` | `Data/ModConfig.cs` [F] |
| `100001g+` | `15%` | `Data/ModConfig.cs` [F] |

旧税务资料说明：商店出售更像现金交易，不进入所得税口径；出货箱收入是申报收入，所以进入所得税。[P] 当前源码中所得税只取 Shipping Bin 收入。[F]

## 7. 房产税公式

房产税每天评估一次，每周结算时把本税周的每日评估相加并四舍五入到整数。[F]

当前公式：

```text
RC = Replacement Cost Amount
IPV = Income Potential Value Amount
UP = Utility Premium Amount
RSP = Risk Shield Premium Amount
DF = Depreciation Factor
AD = Agricultural Deduction
AF = Administrative Fee
DFee = Documentation Fee

每日评估资产价值 = (RC + IPV + UP + RSP) * DF
每日应税房产额 = max(0, 每日评估资产价值 - AD)
每日房产税 = 每日应税房产额 + AF + DFee
周房产税 = round(本税周每日房产税合计, AwayFromZero)
```

房产税关键表：

| 项目 | 当前源码值 | 当前公式用途 | 设计来源 | 源码位置 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| 房产税结算口径 | 每日评估，周结算求和 | 得到周房产税 | 旧税务资料说明房产税看固定资产和潜在收益 | `TaxService.GetPropertyTaxAmountForPeriod` | [F] |
| 折旧系数 | 第 1 年 `1.00`，第 2 年 `0.95`，第 3 年 `0.90`，第 4 年 `0.85`，第 5 年起 `0.80` | 乘到资产评估值 | 旧税务资料提到随年份折旧 | `TaxService.GetDepreciationFactor` | [F] |
| 农业扣除 | 每周最高 `1000g`，每日最高 `1000 / 7` | 从评估资产价值中扣除 | 旧税务资料提到鼓励户外种植 | `ModConfig.PropertyTax`、`TaxService.GetTodayAgriculturalDeduction` | [F] |
| 农业扣除比例 | 户外已种作物格数 / `3427`，限制在 0-1 | 决定当天农业扣除 | 源码当前实现；旧资料中曾讨论季末锁定等方案，以源码为准 | `TaxService.CountOutdoorPlantedTiles` | [F] |
| 行政费 | 每周 `50g`，每日 `50 / 7` | 直接加到每日房产税 | 源码配置 | `ModConfig.PropertyTax`、`TaxService.GetDailyAdministrativeFee` | [F] |
| 文件费 | 每周 `10g`，每日 `10 / 7` | 直接加到每日房产税 | 源码配置 | `ModConfig.PropertyTax`、`TaxService.GetDailyDocumentationFee` | [F] |
| 房屋升级 1 | `10000 / 80 / 7` | 加入 RC | 源码当前值 | `TaxService.AddFarmhouseAssessment` | [F] |
| 房屋升级 2 | `75000 / 80 / 7` | 加入 RC | 源码当前值 | `TaxService.AddFarmhouseAssessment` | [F] |
| 房屋升级 3+ | `175000 / 80 / 7` | 加入 RC | 源码当前值 | `TaxService.AddFarmhouseAssessment` | [F] |
| Coop | RC `4000 / 80 / 7`，IPV `4 * 175 / 7` | 加入建筑评估 | 源码当前值；旧资料说明 IPV 是潜在收益而非实际收益 | `TaxService.GetBuildingPropertyTaxConfig` | [F] |
| Big Coop | RC `14000 / 80 / 7`，IPV `8 * 175 / 7` | 加入建筑评估 | 同上 | `TaxService.GetBuildingPropertyTaxConfig` | [F] |
| Deluxe Coop | RC `34000 / 80 / 7`，IPV `12 * 175 / 7` | 加入建筑评估 | 同上 | `TaxService.GetBuildingPropertyTaxConfig` | [F] |
| Barn | RC `6000 / 80 / 7`，IPV `4 * 175 / 7` | 加入建筑评估 | 同上 | `TaxService.GetBuildingPropertyTaxConfig` | [F] |
| Big Barn | RC `18000 / 80 / 7`，IPV `8 * 175 / 7` | 加入建筑评估 | 同上 | `TaxService.GetBuildingPropertyTaxConfig` | [F] |
| Deluxe Barn | RC `43000 / 80 / 7`，IPV `12 * 175 / 7` | 加入建筑评估 | 同上 | `TaxService.GetBuildingPropertyTaxConfig` | [F] |
| Utility 建筑 | RC `constructionCost / 80 / 7`，UP `RC * 0.2` | 加入建筑评估 | 旧税务资料说明 Utility Premium 是便利性溢价 | `TaxService.CreateUtilityConfig` | [F] |
| Utility 建筑成本 | Fish Pond `5000`，Mill `2500`，Shed `15000`，Big Shed `35000`，Silo `100`，Slime Hutch `10000`，Stable `10000`，Well `1000`，Shipping Bin `250`，Pet Bowl `5000`，Earth/Water Obelisk `500000`，Desert/Island Obelisk `1000000`，Junimo Hut `20000`，Gold Clock `10000000` | 计算 Utility RC 和 UP | 源码当前值 | `TaxService.GetBuildingPropertyTaxConfig` | [F] |
| Cabin | RC `100 / 80 / 7` | 加入建筑评估 | 源码当前值 | `TaxService.GetBuildingPropertyTaxConfig` | [F] |
| Greenhouse | RC `35000 / 80 / 7`，IPV `120 * 13.125 / 7`，RSP `IPV * 2` | 温室解锁后加入评估 | 旧资料说明 RSP 是稳定、跨季节、免风险生产能力 | `TaxService.GetGreenhousePropertyTaxConfig` | [F] |
| 80 周摊销 | `80 = 5 年 * 16 周` | 建筑 RC 的摊销周期 | 旧税务资料说明 | 多处 RC 公式 | [P] |

当前没有在本次任务中进游戏重新验证房产税 UI 和实际结算金额。[U]

## 8. 经营资产税公式

经营资产税每天扫描机器数量，每周结算时把本税周每日评估直接相加。[F]

扫描范围：

- `Game1.locations` 中的地点。[F]
- 农场建筑室内空间。[F]
- Cellar 只在主玩家房屋升级到 3 级后纳入；非主 cellar 会排除。[F]

纳税对象：

- Keg
- Preserves Jar
- Cask
- Bee House
- Mayonnaise Machine
- Cheese Press
- Loom
- Oil Maker
- Dehydrator
- Fish Smoker

当前公式：

```text
threshold = 20

如果 count <= threshold:
    taxableCount = 0
否则:
    taxableCount = count

scaleMultiplier:
    count >= 100 => 2.0
    count >= 50  => 1.5
    其他        => 1.0

单类机器每日经营资产税 =
    round(taxableCount * dailyTaxRate * scaleMultiplier, AwayFromZero)

当日经营资产税 =
    所有机器类型每日经营资产税之和

周经营资产税 =
    本税周每日经营资产税之和
```

注意：当前源码在超过 20 台后，对该类机器的全部数量征税，不是只对超过 20 台的部分征税。[F]

经营资产税税率和历史设计来源：

| 机器 | 当前每日税率 | 历史代表物品/逻辑摘要 | 当前公式用途 | 来源 |
| --- | ---: | --- | --- | --- |
| Keg | `48` | Starfruit Wine，旧资料按高价值单轮利润、处理天数和 20% 税率推导 | `count * 48 * scaleMultiplier` | `Data/ModConfig.cs` [F]，Gmail 税务总详情 [P] |
| Preserves Jar | `64` | Starfruit Jelly | `count * 64 * scaleMultiplier` | 同上 |
| Cask | `8` | Starfruit Wine 升铱星，长周期摊销 | `count * 8 * scaleMultiplier` | 同上 |
| Bee House | `34` | Fairy Rose Honey | `count * 34 * scaleMultiplier` | 同上 |
| Mayonnaise Machine | `260` | Ostrich Egg 到 10x Mayonnaise | `count * 260 * scaleMultiplier` | 同上 |
| Cheese Press | `51` | Large Goat Milk 到 Gold Goat Cheese | `count * 51 * scaleMultiplier` | 同上 |
| Loom | `26` | Wool 到 Cloth | `count * 26 * scaleMultiplier` | 同上 |
| Oil Maker | `88` | Truffle 到 Truffle Oil | `count * 88 * scaleMultiplier` | 同上 |
| Dehydrator | `380` | 5x Starfruit 到 Dried Starfruit | `count * 380 * scaleMultiplier` | 同上 |
| Fish Smoker | `137` | Lava Eel + Coal 到 Smoked Lava Eel | `count * 137 * scaleMultiplier` | 同上 |

旧税务资料说明：经营资产税评估的是“生产能力”，不是机器当天实际使用情况；税率来自高价值代表用法，低价值或空闲状态不会降低评估。[P]

## 9. 市场价格系统

市场价格系统由 `MarketPriceService` 和 `MarketTrendService` 主导。[F]

当前源码事实：

- 市场趋势存档 key 是 `market-trend-state`。[F]
- 当前定价模型版本是 `16.9-market-balance-v1`。[F]
- 市场价格历史最多保留 28 天。[F]
- 单日物品基础随机因子范围是 `0.95` 到 `1.05`。[F]
- Soft 区间是 `0.75` 到 `1.35`，Hard 区间是 `0.55` 到 `1.60`。[F]
- StrongUp 和 StrongDown 的每日变化幅度为 5%-9%。[F]
- 市场价格表会记录历史价格，用于 UI 历史图表。[F]
- Shipping Bin 可以启用市场价结算，默认启用。[F]
- Shop Sale 和 Tooltip 通过 Harmony patch 接入市场价格。[F]
- `1.4.2` 继续只维护普通品质的标准 Market Price；品质不进入 market key、市场历史、走势图或 Exchange 标准合约报价。[F]
- 玩家查看或出售具体物品时，银星、金星、铱星只在最终售价层应用一次原版品质倍率；普通品质标准价保持不变。[F]
- Tooltip、商店出售、Shipping Bin 实际结算、财务账本和税务收入使用一致的最终品质售价。[F]

旧市场资料说明：1.3.x 的重点是让价格波动有惯性、有上下限、不会长期单向漂移，同时保留玩家能观察到的 Market Price 反馈。[P]

## 10. 财务报表系统

财务报表由账本和统计服务支持，主要在 Financial Manual 中展示。[F]

当前 UI 页面：

- Daily：当天收入、支出和净额。[F]
- Seasonal：本季累计和每日趋势。[F]
- Annual：全年累计和趋势。[F]
- Tax：当前税务估算、历史税单摘要。[F]
- Market：Market Price 表格、搜索、排序、收藏和价格历史图表。[F]

收入来源包括 Shipping Bin、Shop Sale、Exchange Transfer 和未分类收入等，具体取决于事件和 patch 是否能识别来源。[F] 全局未分类收入 fallback 会把未被专门识别的增收记为 `RC.UnclassifiedIncome`。[F]

## 11. Financial Manual / UI 说明

打开方式：

1. 启动 SMAPI。
2. 进入存档。
3. 默认按 `O` 打开 Financial Manual。[F]
4. 如果已经生成 `config.json`，热键由 `OpenReportKey` 控制。[F]

Financial Manual 里需要重点检查：

- Daily/Seasonal/Annual 页面是否显示收入、支出、净额和明细。
- Tax 页面是否显示所得税、房产税、经营资产税和历史税单。
- Market 页面是否能搜索、排序、收藏，并打开某个商品的历史价格图。
- Exchange 按钮是否能打开 Exchange UI。

Exchange UI 当前包含账户、创建合约、持仓等页面，并支持 Long/Short、保证金、平仓、交割和违约相关操作。[F]

`1.4.2` Exchange 修复规则：[F]

- 大额多头实物交割按物品实际 `maximumStackSize()` 拆分为合法 Stack。
- 交割保持原子性：只有全部物品都能放进背包才成功；空间不足时不发放任何物品、不扣款、不记录成功，合约保持 `PendingDelivery`。
- 多头交割不使用 `DeliveryStorage`，不支持分批领取。
- 原始地面作物的 7 日合约资格按 `Data/Crops` 的首次成熟周期判断；`DaysInPhase` 总和不超过 7 天才支持。
- 资格判断不计算生长激素、农业学家或特殊灌溉加速；可重复收获作物仍只看首次成熟周期，不看再生间隔。
- `Data/FruitTrees` 中成熟果树的水果允许 7 日合约，因为交易的是成熟果树的日常果实产出；14 日和 28 日资格不变。

上述 `1.4.2` 三项修复已完成针对性游戏内人工验证。[F]

## 12. 数据与存档

Reality Check 会写入 SMAPI save data。不要随便改这些结构。[F]

| Save key | 数据模型 | 内容 |
| --- | --- | --- |
| `save-data` | `SaveData` | 账本、税务记录、每日房产税评估、每日经营资产税评估、已签税单、医保记录、未清余额、Market Price 收藏。 [F] |
| `market-trend-state` | `MarketTrendSaveData` | 当前市场定价模型版本、趋势状态、价格历史。 [F] |
| `exchange-data` | `ExchangeSaveData` | Exchange 账户、合约序号、每日结算日期。 [F] |

任何字段删除、改名、类型变化，都可能影响旧存档读取。必须先设计迁移方案并得到 Stan 明确确认。[F]

## 13. 构建、安装、测试

构建：

```bash
dotnet build
```

项目使用 `Pathoschild.Stardew.ModBuildConfig`。正常构建会尝试把 Mod 部署到本机 Stardew Valley `Mods` 目录，并生成 release zip。[F]

测试原则：

- `dotnet build` 通过，只说明 C# 编译和构建流程通过。
- UI、税单、市场价格、Exchange、日报/季报/年报必须进游戏确认。
- 修改税务、市场价格、账本、Exchange 或存档结构时，不能只看终端。

游戏内测试清单：

1. 启动 SMAPI。
2. 进入一个测试存档。
3. 按 `O` 打开 Financial Manual。
4. 查看 Daily、Seasonal、Annual 页面。
5. 查看 Tax 页面，确认所得税、房产税、经营资产税显示。
6. 查看 Market 页面，测试搜索、排序、收藏和历史图。
7. 打开 Exchange UI，检查账户、创建合约和持仓页面。
8. 过夜，查看 SMAPI 日志、账本变化、税务每日评估和市场价格更新。
9. 在税务结算日检查 Tax Notice 和实际扣款/债务。

`1.4.2` 品质售价、大额多头交割和 7 日合约资格已完成针对性游戏内验证；其他完整回归仍按上述清单执行。[F]

## 14. 版本历史 / Change Log

| 版本/节点 | 内容摘要 | 来源 |
| --- | --- | --- |
| `1.0.0` | 旧资料描述为税务、医保和财务报表基础版本。 | Gmail 旧项目总文档 [P] |
| `1.1.0` | 旧资料描述为中文本地化和通知/报表文本打磨。 | Gmail 旧项目总文档 [P] |
| `1.2.0` | 旧资料描述为扩展税务、医保、自定义税单、税务历史和 Financial Manual。 | Gmail 旧资料 [P] |
| `1.2.1` | 旧资料重点描述经营资产税倍率修正、税务总详情。 | Gmail `Reality Check 1.2.1 税务总详情` [P] |
| `1.2.2` | 可配置 Financial Manual 热键。 | git tag `v1.2.2` [F] |
| `1.2.3` | 全局收入 fallback / 未分类收入。 | git tag `v1.2.3` [F] |
| `1.3.0` | 市场价格系统发布。 | git commit `ce35d2f` [F] |
| `1.3.1` | Market Price 搜索和收藏改进。 | git commit `8259cde` [F] |
| `1.3.3` | 市场价格平衡调整。 | git commit `6c9449b` [F] |
| `1.3.4` | Tooltip 显示市场价格。 | git commit `6b87237` [F] |
| `1.4.0` | Exchange 系统发布，包含账户、合约创建、保证金和基础交易流程。 | git commits `1c82533`、`6be3264`、`f52ab47`、`bbfbe58` [F] |
| `1.4.1` | UI layout update。 | git commit `88dac9c` [F] |
| `1.4.2` | 修复品质售价、大额多头原子交割，以及快速生长作物和果树产物的 7 日合约资格。 | `manifest.json`、git commit `90e6e61`、当前源码与人工验收 [F] |

## 15. 旧资料来源索引

| 来源 | 摘要 | 状态 |
| --- | --- | --- |
| 本地源码 | `manifest.json`、`.csproj`、`ModEntry.cs`、`Data/`、`Services/`、`UI/`、`Events/`、`Patches/`。 | [F] |
| 本地 git | tags `v1.2.2`、`v1.2.3`，以及 1.3.x、1.4.x 相关 commit。 | [F] |
| Gmail：`Reality Check 1.2.1 税务总详情`，2026-06-24 | 所得税、房产税、经营资产税原则和经营资产税代表物品推导。 | [P] |
| Gmail：`Reality Check 1.0-1.3.0 整体总文档`，2026-06-29 | 1.0 到 1.3.0 的历史项目总览。 | [P] |
| Gmail：`Reality Check 1.3.4版本项目总文档`，2026-06-30 | 1.3.4 tooltip 市场价格显示和项目总览。 | [P] |
| Gmail：`RealityCheck 市场趋势规则 6月29号22点37分`，2026-06-29 | 市场价格趋势和平衡规则。 | [P] |
| Gmail：`Reality Check交易所规划V1`，2026-06-29 | Exchange 账户、合约、保证金、交割和违约设计。 | [P] |
| Gmail：`Reality Check交易所清算原则`，2026-06-28 | Exchange 清算、强平、交割和债务原则。 | [P] |
| Nexus | 当前环境没有确认到可用公开页面、文件、posts、bugs 或版本历史。 | [U] |

不要把私人邮件全文复制进仓库。只能记录项目事实、日期、标题和摘要。

## 16. 当前未确认事项

- Nexus 页面、文件列表、posts、bugs 和公开 version history 尚未确认。[U]
- `1.4.2` 三项修复已完成人工验收，但完整 UI 全量回归仍需在后续发布检查中持续执行。[U]
- 房产税、经营资产税、所得税公式已经从源码确认，但没有在本次任务中用实际存档跨周结算重新验算。[U]
- Exchange 完整交易生命周期在本次任务中未进游戏验证。[U]
- 旧资料里的部分 1.0、1.1、1.2.0、1.2.1 叙述来自邮件摘要，不等同于当前源码事实。[P]

## 17. 以后给 Codex/AI 的最简规则

1. 先读 `PROJECT_DOCUMENTATION.md`，再读源码。
2. 默认在 `main` 单线工作；不要擅自创建分支、PR 或 release。
3. 可以 commit 到 `main`，但不要 push；Stan 手动执行 `git push`。
4. 不要新增文档文件。优先更新 `PROJECT_DOCUMENTATION.md`、`README.md` 和 `CHANGELOG.md`。
5. 如果认为必须新增文档，先说明原因，等 Stan 确认。
6. 不要修改业务代码，除非任务明确要求。
7. 不要凭旧邮件、旧文档或记忆改源码。旧资料只能作为线索。
8. 不要擅自改税务、市场价格、账本、Exchange 或存档结构。
9. 存档数据结构变化必须先征求 Stan 明确确认，并写迁移方案。
10. UI 功能不能只用 `dotnet build` 验收，必须说明游戏内验证步骤。
11. 每次修改后更新 `CHANGELOG.md`。
12. 不要发布 Nexus，不要创建 GitHub release。
