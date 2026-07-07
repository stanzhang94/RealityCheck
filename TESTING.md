# Reality Check 测试指南

Reality Check 的测试分两层：技术检查和游戏内检查。

`dotnet build` 通过不等于游戏内验收通过。特别是 Financial Manual、Tax Notice、Market Price、Exchange 等 UI 功能，必须进游戏看实际效果。

详细流程见 `docs/TESTING_GUIDE.md`。

## 1. 构建检查

在项目根目录运行：

```bash
dotnet build
```

当前已知行为：

- 项目使用 `Pathoschild.Stardew.ModBuildConfig`。
- build 会先编译 `RealityCheck.dll`。
- 随后会尝试把 Mod 文件复制到本地 Stardew Valley `Mods/RealityCheck` 目录。
- 还会在 `bin/Debug/net6.0/` 下生成 release zip。
- 在 Codex 受限沙盒里，部署到游戏 Mods 目录可能失败，因为该目录在仓库外。
- 2026-07-07 授权后 build 成功，0 warnings，0 errors。

## 2. 本地部署检查

默认部署位置：

```text
~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/RealityCheck
```

成功 build 后，检查部署目录至少包含：

- `manifest.json`
- `RealityCheck.dll`
- `i18n/`

## 3. SMAPI 启动检查

通过 SMAPI 启动 Stardew Valley，检查：

- SMAPI 能加载 `Reality Check`。
- 加载版本与 `manifest.json` 一致。
- 启动时没有红色错误。
- Harmony patch 没有异常失败。
- 能正常载入一个存档。

## 4. Financial Manual 游戏内检查

进入存档后按默认快捷键：

```text
O
```

检查：

- Daily report 能打开。
- Seasonal report 能打开。
- Annual report 能打开。
- Tax report 能打开。
- Tax history 可读。
- Income details 可读。
- Expense details 可读。
- Outstanding balance 显示合理。
- Market Price 表能打开并可读。
- Market Price 排序、搜索、收藏、历史图不异常。
- Exchange 按钮能打开 Commodity Exchange UI。

## 5. 税务检查

涉及税务时，进游戏确认：

- 每周税务结算时机正确。
- Income Tax、Property Tax、Business Property Tax 数值合理。
- 开启税单邮件时，Tax Notice 能正常出现。
- 税单文字和公式布局可读。
- 签名要求能正常工作。
- Tax History 和报表中的记录一致。

## 6. Market Price 检查

涉及市场价格时，进游戏确认：

- 新一天后市场价格会更新。
- Market Price 页面显示 item、base price、daily multiplier、total multiplier、market price。
- tooltip 显示的售价与 Reality Check 市场价一致。
- 商店直售和出货箱结算只受到预期影响。
- 旧的市场趋势历史没有被意外清空。

## 7. Exchange 检查

涉及 Exchange 时，进游戏确认：

- 账户页显示总额、锁定保证金、可用现金、债务、持仓和历史。
- Deposit / Withdraw 会同时影响农场钱包和 Exchange 账户。
- 合约目录能列出可交易商品。
- Long / Short 持仓创建条件正确。
- Margin call、Top Up、Forced Liquidation、Close、Delivery、Default、Debt Collection 行为符合预期。
- Exchange 历史文字在当前语言下可读。

可选 SMAPI console 命令：

```text
rc_exchange_status
rc_exchange_deposit <amount>
rc_exchange_withdraw <amount>
rc_exchange_catalog
```

## 8. 日志检查

测试后查看 SMAPI log，重点看：

- 红色错误。
- 重复 warning。
- Harmony patch 失败。
- Save/load 失败。
- Market trend migration 信息。
- Exchange 持久化异常。

## 验收规则

最终报告至少说明：

- 是否运行 `dotnet build`，结果如何。
- 是否进入 Stardew Valley / SMAPI 验证。
- 如果没有游戏内验证，明确说明原因。

