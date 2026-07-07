# Reality Check 路线图

这个文件只记录未来方向。它不是当前任务清单，也不代表 Codex 可以自动开始开发。

## 当前基线

截至 2026-07-07，本地源码已经包含：

- [F] 财务报表和账本记录。
- [F] 每周税务和 Tax Notice UI。
- [F] Harvey 医疗保险记录。
- [F] 动态 Market Price 和市场价格 UI。
- [F] Commodity Exchange，包括账户、合约、持仓、保证金、交割/default 和 UI。

## 维护优先级

- 保持旧存档兼容。
- 保持账本分类和报表总额可解释。
- 保持税务计算能在游戏内验证。
- Market Price 改动必须范围清楚，并能在 Financial Manual 里看见结果。
- UI 在支持语言下必须可读。
- `README.md`、`CURRENT_STATUS.md` 和 `docs/` 要持续与 `manifest.json` 和源码保持一致。
- 用户可读文档中文优先，英文术语只在必要时保留。

## 候选后续工作

- 为报表、税务、Market Price、Exchange 准备更清楚的手动测试场景。
- 补充 1.4.x 的发布说明。
- 根据游戏内截图做小范围 UI 可读性修正。
- 给市场价格、税务评估和 Exchange settlement 增加更安全的诊断说明。
- [P] 银行、贷款、信用、投资等系统只能在明确规划和确认后再做。历史文档提到这些方向，但当前源码没有完整银行系统。

## 除非明确要求，否则不要做

- 大型架构重写。
- 未规划迁移的存档结构变化。
- 新增银行、贷款等大系统。
- 继续扩展 Exchange 大功能。
- 自动发布 GitHub release。
- 自动发布 Nexus。
- 自动 push 到 `main`。

