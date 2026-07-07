# 详细测试流程

这个文档给 Stan 或后续接手者照着做 Reality Check 手动测试。

## 1. 构建

在项目根目录运行：

```bash
dotnet build
```

预期结果：

- [F] 项目编译出 `bin/Debug/net6.0/RealityCheck.dll`。
- [F] ModBuildConfig 把文件复制到本地 Stardew Valley Mods 目录。
- [F] ModBuildConfig 在 `bin/Debug/net6.0/` 生成 zip。

Codex 注意：

- [F] 受限沙盒可能在部署阶段失败，因为 Stardew Valley Mods 目录在仓库外。
- [F] 2026-07-07，授权后的 `dotnet build` 成功，0 warnings，0 errors。

## 2. 检查部署文件

到部署后的 `RealityCheck` Mod 文件夹，确认至少有：

- `manifest.json`
- `RealityCheck.dll`
- `i18n/`

## 3. 启动 SMAPI

通过 SMAPI 启动 Stardew Valley。

检查：

- Reality Check 被加载。
- 加载版本与 `manifest.json` 一致。
- 启动时没有红色错误。
- Harmony patch 没有异常失败。
- 能正常进入存档。

## 4. 检查 Financial Manual

进入存档后按 `O`，除非 `config.json` 改了快捷键。

逐项检查：

- Daily report 能打开。
- Seasonal report 能打开。
- Annual report 能打开。
- Tax report 能打开。
- Income details 和 Expense details 可读。
- Outstanding balance 显示合理。
- Market Price 表能打开。
- Market Price 搜索、排序、收藏、历史图正常。
- Exchange 按钮能打开 Commodity Exchange UI。

## 5. 检查税务

如果本次改动涉及税务：

- 检查每周结算时机。
- 检查 Income Tax、Property Tax、Business Property Tax 数值是否合理。
- 检查 Tax Notice mail 是否能打开自定义 UI。
- 检查签名机制。
- 检查 Tax History 和报表记录是否一致。

## 6. 检查 Market Price

如果本次改动涉及市场价格：

- 过一天，确认价格会更新。
- 检查 Market Price 页面显示的价格和倍率。
- 检查 tooltip 价格是否显示 Reality Check 市场价。
- 检查商店直售和出货箱结算是否只受到预期影响。
- 确认历史价格没有被意外清空。

## 7. 检查 Exchange

如果本次改动涉及 Exchange：

- 检查账户余额、锁定保证金、可用现金、债务。
- 测试 Deposit / Withdraw。
- 检查合约目录。
- 测试 Long / Short 持仓创建。
- 测试 Margin Call、Top Up、Forced Liquidation。
- 测试 Close position。
- 测试 Delivery、Default、Debt Collection。
- 检查 Exchange history 是否可读。

可选 SMAPI 命令：

```text
rc_exchange_status
rc_exchange_deposit <amount>
rc_exchange_withdraw <amount>
rc_exchange_catalog
```

## 8. 查看日志

测试后查看 SMAPI log：

- 是否有红色错误。
- 是否有重复 warning。
- 是否有 Harmony patch 失败。
- 是否有 save/load 失败。
- 是否有 Market trend migration 信息。
- 是否有 Exchange 持久化异常。

## 验收口径

最终报告必须说明：

- 是否运行 `dotnet build`。
- 是否通过 SMAPI 进游戏验证。
- 如果没有进游戏，原因是什么。

不要把终端 build 成功当成 UI 验收成功。

