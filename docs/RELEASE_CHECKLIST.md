# 发布前检查清单

不要自动发布 GitHub release。不要自动发布 Nexus。

## 发布前

- [ ] [F] 确认 `manifest.json` 版本。
- [ ] [F] 确认 `RealityCheck.csproj` 能 build。
- [ ] [F] 运行 `dotnet build`。
- [ ] [F] 确认生成的 zip 名称和版本。
- [ ] [F] 确认部署后的 Mod 目录包含当前文件。
- [ ] [F] 通过 SMAPI 启动游戏。
- [ ] [F] 载入一个存档。
- [ ] [F] 用 `O` 或配置后的快捷键打开 Financial Manual。
- [ ] [F] 检查 Daily、Seasonal、Annual、Tax、Market Price 和 Exchange UI。
- [ ] [F] 检查 SMAPI log。
- [ ] [P] 根据 `CHANGELOG.md` 和 `docs/VERSION_HISTORY.md` 准备 GitHub/Nexus changelog。
- [ ] [U] 如果要发布 Nexus，手动确认 Nexus 页面、文件和版本历史。

## 发布安全规则

- 没有迁移说明时，不要改变存档数据结构。
- 改税务、Market Price 或报表公式时，必须写清 release note。
- 不要从脏工作区发布。
- 不要 force push 发布分支。

