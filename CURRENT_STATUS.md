# Reality Check 当前状态

最后更新：2026-07-07

## 项目身份

- [F] 项目名：Reality Check
- [F] 类型：Stardew Valley SMAPI Mod
- [F] 作者：Stan
- [F] Unique ID：`Stan.RealityCheck`
- [F] 当前 `manifest.json` 版本：`1.4.1`
- [F] 目标框架：`.NET 6.0`
- [F] 最低 SMAPI API：`4.0.0`
- [F] 当前文档中文化分支：`docs/chinese-user-docs`

## 当前 Git 状态

- [F] 上一次文档恢复 PR 已经合入 `origin/main`，merge commit 为 `545137f`。
- [F] 本次中文化分支从最新 `main` 创建。
- [F] 上一次文档恢复 commit：`8771627 docs: recover Reality Check project documentation and workflow`。
- [F] 当前任务不修改业务代码，只修改文档和必要的 `CHANGELOG.md`。

## 当前已完成的功能模块

- [F] 财务账本和 Analytics 汇总。
- [F] Financial Manual，包括日报、季度/季节报、年报、税务报表、收入/支出明细和 Market Price 页面。
- [F] 每周税务系统，包括 Income Tax、Property Tax、Business Property Tax、Tax Records 和自定义 Tax Notice UI。
- [F] Harvey 医疗保险、医疗费用和报销记录。
- [F] Market Price 动态市场价格系统，包括趋势状态、每日价格更新、商店直售价格 patch、tooltip 价格 patch 和市场价格表。
- [F] 出货箱市场结算追踪与可选市场价结算。
- [F] Pelican Town Commodity Exchange，包括账户、出入金、合约目录、持仓、保证金、平仓、交割/default、债务和 Exchange UI。
- [F] 多语言资源文件：default、de、fr、ja、zh。

## 主要源码位置

- `ModEntry.cs`：SMAPI 入口，服务初始化，Harmony patch，事件挂接，Financial Manual 快捷键，Exchange console commands。
- `Data/`：配置和存档数据模型。
- `Models/`：账本、税务、市场价格、Exchange 等状态对象。
- `Events/`：收入、支出、税务相关事件处理。
- `Services/`：账本、统计、税务、市场价格、市场趋势、Exchange、配置、i18n、通知服务。
- `Patches/`：商店销售、市场价、tooltip、出货箱追踪相关 Harmony patch。
- `UI/FinanceMenu.cs`：Financial Manual 和 Market Price UI。
- `UI/ExchangeMenu.cs`：Commodity Exchange UI。
- `UI/TaxNoticeMenu.cs`：自定义税单 UI。
- `i18n/`：本地化文本。

## 验证状态

- [F] 2026-07-07：授权后运行 `dotnet build` 成功，0 warnings，0 errors，并生成 `RealityCheck 1.4.1.zip`。
- [F] 未授权的沙盒 build 会在部署到 Stardew Valley Mods 目录时失败，因为目标目录在仓库外。
- [U] 本次中文化任务没有修改业务代码，因此没有运行新的 `dotnet build`。
- [U] 当前 1.4.1 仍未在本次任务中进行 SMAPI 启动和游戏内 UI 验收。

## 已知风险

- [F] Exchange 已经存在于当前源码中，不再只是未来计划。
- [F] 存档数据结构变更风险高，必须先说明迁移影响并得到确认。
- [F] UI 功能需要进游戏验证；终端 build 成功不能代表界面验收通过。
- [F] Codex 工作流现在要求：不要在没有用户明确要求时新增文档文件；优先更新现有文档。
- [P] 历史邮件中记录过 1.3.x 的部分游戏内验证，但不能替代当前 1.4.1 的实际验收。

## 下一步建议

1. 进入游戏，通过 SMAPI 加载 Reality Check。
2. 进存档后按 `O` 打开 Financial Manual。
3. 检查 Daily、Seasonal、Annual、Tax、Market Price 和 Exchange UI。
4. 查看 SMAPI log，确认没有红色错误或 patch 失败。
5. 如果要继续开发功能，先从 `AGENTS.md`、`CURRENT_STATUS.md`、`TESTING.md` 开始读。

