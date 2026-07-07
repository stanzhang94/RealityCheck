# 恢复资料索引

这个文件只记录资料来源摘要。不要把私人邮件正文复制进仓库。

## 本地源码

- [F] `manifest.json`：Mod 名称、作者、版本 `1.4.1`、描述、Unique ID、入口 DLL、最低 SMAPI API。
- [F] `RealityCheck.csproj`：`net6.0`、ModBuildConfig、Harmony 依赖。
- [F] `ModEntry.cs`：服务初始化、patch、事件挂接、UI 快捷键、Exchange 命令。
- [F] `Data/`：配置和持久化数据模型。
- [F] `Services/`：账本、统计、税务、市场、Exchange、配置、本地化、通知服务。
- [F] `UI/`：Financial Manual、Exchange menu、Tax Notice menu。
- [F] `Patches/`：商店销售、商店市场价、出货箱追踪、tooltip 市场价 patch。
- [F] `i18n/`：default、German、French、Japanese、Chinese 文本。

## 本地 Git

- [F] 文档恢复前的 recent head：`88dac9c Release Reality Check 1.4.1 UI layout update`。
- [F] 找到 tags：`v1.2.2`、`v1.2.3`。
- [F] Git 历史确认 1.3.x 的市场系统和 1.4.x 的 Exchange 开发记录。

## Gmail / 邮箱

搜索范围：通过 Gmail 连接器搜索 Reality Check、Stardew、SMAPI、Financial Manual、Market Price、Exchange、版本号和中文项目关键词。

恢复出的摘要：

- [P] `Reality Check 1.0-1.3.0 整体总文档`，2026-06-29：从早期税务/医保/报表到 1.3.0 市场价格的历史总览。
- [P] `Reality Check 1.3.4版本项目总文档`，2026-06-30：记录到 1.3.4 tooltip 市场价显示修正。
- [P] `RealityCheck 市场趋势规则 6月29号22点37分`，2026-06-29：1.3.3 市场趋势和平衡规则。
- [P] `Reality Check交易所规划V1`，2026-06-29：Exchange 账户、标准化合约、保证金、交割/default 和 MarketPriceService 价格来源原则。
- [P] `Reality Check交易所清算原则`，2026-06-28：不做现金交割、每日盯市、强平、交割/default、Exchange debt。
- [P] `RealityCheck1.4实现路径文档V1版本（草稿）`，2026-07-02：提示完整 1.4 实现路径文档过长，未直接进入邮件正文。
- [P] `rc交易所规则补充`，2026-06-30：发现附件 `Reality Check交易所规划V2（2026-06-30 09-13）.docx`，本次没有导入仓库。

## Nexus

- [U] 当前环境没有确认 Reality Check 的公开 Nexus 页面和版本历史。
- [U] 发布前需要手动检查 Nexus 描述、files、posts、bugs 和 version history，或由用户提供链接/导出。

## 冲突和优先级

- [F] 旧 README 描述的是较早版本边界，与当前 `manifest.json` 1.4.1 和源码不完全一致。
- [F] 旧规划文档曾把 Exchange 当成未来功能，但当前源码已经包含 Exchange 服务、数据、命令和 UI。
- [F] 当前源码和 `manifest.json` 是当前状态的最高依据。

