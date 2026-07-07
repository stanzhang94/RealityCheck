# Reality Check

## 中文阅读指南

如果你只是想了解项目当前状态，优先阅读：

1. `README.md`
2. `CURRENT_STATUS.md`
3. `CHANGELOG.md`
4. `docs/UI_GUIDE.md`
5. `docs/TESTING_GUIDE.md`

`AGENTS.md` 主要给 Codex 使用，不是普通用户日常阅读文档。

`docs/` 下的专题文档用于后续维护和防止项目上下文丢失。

## 项目简介

Reality Check 是 Stan 的 Stardew Valley 本地 SMAPI Mod。它让农场经营不再只是金币无压力增长，而是加入财务记录、税务、医疗保险、市场价格和 Exchange 等经济反馈系统。[F]

当前 `manifest.json` 版本：`1.4.1`。[F]

## 运行要求

- Stardew Valley 1.6+。[P: 历史 README]
- SMAPI 4.0+。[F: `manifest.json`]
- `.NET 6` 目标框架。[F: `RealityCheck.csproj`]

## 当前主要功能

- 财务账本与 Financial Manual 报表。[F]
- 每周税务系统，包括 Income Tax、Property Tax、Business Property Tax、Tax History 和自定义 Tax Notice UI。[F]
- Harvey 医疗保险、医疗支出和报销记录。[F]
- Market Price 动态市场价格，包括价格趋势、商店直售/出货箱结算接入、tooltip 价格显示和市场价格表 UI。[F]
- Pelican Town Commodity Exchange，包括账户转入转出、合约目录、持仓、保证金、平仓、交割/default、债务和 Exchange UI。[F]
- 多语言文本文件：default、de、fr、ja、zh。[F]

## 如何打开游戏内界面

进入存档后，按默认快捷键：

```text
O
```

会打开 Financial Manual。快捷键由首次启动后生成的 `config.json` 中 `OpenReportKey` 控制。[F]

## 构建与本地部署

在项目根目录运行：

```bash
dotnet build
```

项目使用 `Pathoschild.Stardew.ModBuildConfig`。正常 build 会编译 `RealityCheck.dll`，尝试复制到本地 Stardew Valley `Mods/RealityCheck` 目录，并生成 release zip。[F]

在 Codex 沙盒里，部署步骤可能因为 Mods 目录在仓库外而需要授权。[F]

注意：`dotnet build` 通过只代表代码能编译和部署，不代表游戏内 UI 已经验收通过。UI、税务、Market Price、Exchange 等功能仍需要进游戏通过 SMAPI 验证。

## 安装方式

1. 安装 SMAPI。
2. build 或下载 Reality Check。
3. 把 `RealityCheck` 文件夹放入 Stardew Valley 的 `Mods` 目录。
4. 通过 SMAPI 启动游戏。

## 文档入口

- `CURRENT_STATUS.md`：当前项目状态，适合先看。
- `CHANGELOG.md`：变更记录。
- `TESTING.md`：简版测试说明。
- `ROADMAP.md`：后续方向，不代表自动开发计划。
- `docs/PROJECT_OVERVIEW.md`：项目总览。
- `docs/UI_GUIDE.md`：游戏内 UI 使用说明。
- `docs/TESTING_GUIDE.md`：详细测试流程。
- `docs/ARCHITECTURE.md`：架构说明。
- `docs/RECOVERED_REFERENCES.md`：恢复资料来源索引。

## 开发边界

- 不要随便修改存档数据结构；需要先说明迁移影响并得到确认。[F]
- 不要随便改 Market Price、税务、Exchange 或报表统计口径。[F]
- 旧邮件、Nexus、规划文档只能作为历史资料，当前事实以本地源码、`manifest.json`、git 和实际运行结果为准。[F]
- UI 相关任务不能只用 `dotnet build` 作为最终验收。[F]

## 权限

请不要未经允许重新上传 Reality Check 或修改版本。

可以阅读源码用于学习。翻译补丁、兼容补丁或修改发布请先获得许可。

## 作者

Created by Stan.

