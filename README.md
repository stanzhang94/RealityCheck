# Reality Check

Reality Check 是 Stan 的 Stardew Valley 本地 SMAPI Mod。

当前源码版本是 `1.4.3`。项目包含财务报表、税务、医保、市场价格、Financial Manual 和 Pelican Town Commodity Exchange。

## 阅读入口

请优先阅读：

1. `PROJECT_DOCUMENTATION.md`：唯一项目总文档，包含当前状态、公式、源码结构、测试方式、历史资料索引和以后给 Codex/AI 的规则。
2. `CHANGELOG.md`：轻量变更记录。

过去拆散的工作流文档已经合并进 `PROJECT_DOCUMENTATION.md`。不要再从旧的 `AGENTS.md`、`CURRENT_STATUS.md`、`ROADMAP.md`、`TESTING.md` 或 `docs/` 分散文档找项目事实。

## 快速运行

构建：

```bash
dotnet build
```

启动游戏后通过 SMAPI 进入存档，默认按 `O` 打开 Financial Manual。Financial Manual 支持鼠标滚轮，也支持在正文区域按住鼠标左键或触摸后上下拖动滚动。

`1.4.3` 只增加基础拖动滚动，不属于完整 Android UI 适配；Android 端触屏效果仍需要玩家实机反馈验证。

交易所账户页保留原有自定义金额输入和 PC 键盘操作，并增加 `+100`、`+1K`、`+10K`、`+100K`、`清零` 快捷按钮。按钮与原输入框共用同一金额，只帮助 Android 玩家在系统键盘未弹出时填写金额，不会直接执行存入或取出；Android 实机仍需玩家验证。

`dotnet build` 通过不等于游戏内验收通过。UI、税务、市场价格和 Exchange 相关改动都需要进游戏确认。

## 开发规则摘要

- 默认在 `main` 单线工作。
- 可以 commit 到 `main`，但不要自动 push；Stan 手动执行 `git push`。
- 不要擅自新增文档文件，优先更新 `PROJECT_DOCUMENTATION.md`、`README.md` 和 `CHANGELOG.md`。
- 不要擅自修改税务、市场价格、账本、Exchange 或存档结构。
- 不要发布 Nexus，不要创建 release。
