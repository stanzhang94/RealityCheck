# Reality Check Codex 工作规则

这个文件主要给 Codex 看，用来约束后续本地开发和文档维护。

Stan 日常了解项目状态时，优先看 `README.md`、`CURRENT_STATUS.md`、`CHANGELOG.md`、`docs/UI_GUIDE.md` 和 `docs/TESTING_GUIDE.md`。

Reality Check 是 Stardew Valley 本地 SMAPI Mod。处理它时要按游戏 Mod 对待，尤其注意存档数据和游戏内 UI 风险。

项目文档尽量使用来源标签：

- `[F]` 当前源码、manifest、git 历史或 build 输出能直接支持。
- `[P]` 历史文档、邮件、Nexus 资料或代码结构推断支持，但不是当前源码直接证明。
- `[U]` 目前无法确认。

## 任务开始检查

每次开发或文档任务前，先读：

- `AGENTS.md`
- `CURRENT_STATUS.md`
- `TESTING.md`

修改前检查当前 git 状态：

- `git status --short --branch`
- 最近的 `git log --oneline`
- 与任务相关的源码和文档

不要假设当前版本、源码结构或功能状态。以本地文件、git 历史、README、docs、manifest 和实际命令/游戏结果为准。

## 计划和变更控制

- 修改文件前，先说明计划。
- 改动要小、范围清楚、容易回滚。
- 不要在 Stan 没明确要求时重写或重构项目。
- 不要为了风格批量改文件名、namespace、class 或 UI 文案。
- 不要删除旧文档，除非 Stan 明确确认。
- 不要在没有用户明确要求时新增文档文件。优先更新现有文档：`AGENTS.md`、`CURRENT_STATUS.md`、`CHANGELOG.md`、`TESTING.md`、`ROADMAP.md` 和 `docs/` 下已有文件。
- 如果认为必须新增文档，先说明原因并等待 Stan 确认。
- 没有明确确认时，不要自动 commit、push、tag、发布 GitHub 或 Nexus。
- 不要因为 warning cleanup 扩大任务范围。
- 对于纯文档工作，如果 Stan 明确要求自动 commit，且 `git status` 只包含本任务相关文档/ignore 改动，可以自动 commit。
- 永远不要 force push。不要直接 push `main` 或 `master`，除非 Stan 明确要求。

## 功能边界

- 不要在没有明确要求时启动大型未来系统。
- 不要自行推进银行、贷款或新的 Exchange 大扩展。
- 不要在任务未明确要求时修改 Market Price 算法。
- 不要在任务未明确要求时修改税务逻辑。
- 不要随便改财务报表分类或汇总规则。
- 不要在未说明迁移影响并获得确认前修改 JSON/存档数据结构。
- 不要破坏现有存档兼容。

## 验证规则

- 只要改业务代码，条件允许时必须运行 `dotnet build`。
- `dotnet build` 不是最终 UI 验收。
- UI 工作必须写清游戏内验证计划；条件允许时要通过 SMAPI 进游戏检查。
- Financial Manual、Tax Notice、Tax Report、Market Price、Exchange UI 都要通过启动游戏、载入存档、打开 UI 来看实际效果。
- Market Price、税务、报表、Exchange 或存档改动必须说明影响范围，也要说明没有改什么。
- 修改代码或工作流文档后，更新 `CURRENT_STATUS.md` 和 `CHANGELOG.md`。
- 恢复旧资料时，要和当前源码对照；冲突处必须标明。

## 当前已知项目事实

- Mod 名称：Reality Check
- Unique ID：`Stan.RealityCheck`
- 当前 `manifest.json` 版本：`1.4.1`
- 目标框架：`.NET 6.0`
- 最低 SMAPI API：`4.0.0`

## Git 工作流

- 非琐碎文档或代码任务优先使用任务分支。
- 本次用户文档中文化分支：`docs/chinese-user-docs`。
- 本次建议 commit message：`docs: add Chinese-first user documentation`。
- push 前确认 remote 和 branch。
- 只 push 非 main 工作分支，不 force push。
