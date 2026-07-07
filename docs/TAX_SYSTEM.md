# 税务系统

这个文档记录 Reality Check 当前税务系统结构。税务逻辑影响扣款、报表和存档记录，不能随便改。

## 当前源码事实

- [F] `TaxEvents` 把税务行为接入游戏生命周期。
- [F] `TaxService` 包含税务计算。
- [F] `TaxNoticeService` 和 `TaxNoticeMailRouter` 支持自定义 Tax Notice 的投递和打开。
- [F] `TaxNoticeMenu` 渲染自定义税单 UI。
- [F] `SaveData` 保存 tax records、每日房产税评估和每日经营资产税评估。
- [F] `Data/ModConfig.cs` 配置税单邮件、签名要求、经营资产税阈值、所得税档位、经营资产税率和房产税设置。

## 恢复出的历史设计

- [P] 历史文档描述税务系统包括每周 Income Tax、Property Tax、Business Property Tax、Outstanding Balance、Tax Notice、签名和 Tax History。
- [P] 历史文档强调 Business Property Tax 的实际计算和 Tax Notice 展示必须一致。
- [P] 历史文档说明 unknown/unclassified income 不应自动进入 Income Tax。

## 当前注意事项

- [F] 不要在任务未明确要求时修改税务公式。
- [F] 如果修改税务计算，必须一起验证 `TaxService`、`TaxNoticeMenu`、报表、Tax History 和游戏内实际结算。
- [F] Tax UI 必须进游戏检查；build 成功不够。

## 未确认事项

- [U] 本次中文化任务没有重新进游戏验证 1.4.1 每周税务结算。

