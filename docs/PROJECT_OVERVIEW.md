# 项目总览

Reality Check 是一个 Stardew Valley SMAPI Mod。它关注农场经济压力、财务反馈、税务、市场价格和 Exchange 风险管理。[F]

它不是网站，不是 SaaS，也不是自动化服务。[F]

## 当前身份

- [F] 名称：Reality Check
- [F] 作者：Stan
- [F] Unique ID：`Stan.RealityCheck`
- [F] 当前 `manifest.json` 版本：`1.4.1`
- [F] 目标框架：`net6.0`
- [F] 最低 SMAPI API：`4.0.0`

## Reality Check 解决什么问题

Stardew Valley 原版经济里，钱通常只会越来越多，玩家很少需要面对持续经营压力。

Reality Check 的目标是让钱变得“需要管理”：

- [P] 收入要有来源。
- [P] 支出要有分类。
- [P] 税务要有规则。
- [P] 资产规模要产生压力。
- [P] 市场价格不应该永远固定。
- [F] 当前源码通过账本、税务、Market Price、Financial Manual 和 Exchange 实现这些方向。

## 当前功能范围

Reality Check 当前包括：

- [F] 财务账本和 Financial Manual 报表。
- [F] 税务评估、Tax Notice 和 Tax History。
- [F] 医疗费用与 Harvey 医疗保险报销记录。
- [F] Market Price 计算、市场趋势状态、市场价格表和相关 Harmony patch。
- [F] Pelican Town Commodity Exchange 系统与 UI。

## 当前边界

- [F] Exchange 已经存在于当前源码中，不再只是未来规划。
- [P] 银行、贷款、信用和更完整债务系统只存在于历史路线讨论中。
- [U] 当前没有验证到完整银行/贷款系统实现。
- [F] 存档数据会记录账本、税务、市场趋势、收藏和 Exchange 状态，因此存档结构不能随便改。

## 后续接手提醒

- 当前事实以源码、`manifest.json`、git 和实际运行结果为准。
- 历史邮件、旧文档、Nexus 信息只能作为参考。
- 任何 UI 改动都要进游戏看实际效果。
- 任何税务、Market Price、Exchange、存档结构改动都要先说明影响范围。

