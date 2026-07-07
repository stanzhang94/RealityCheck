# Release Checklist

Do not publish GitHub releases or Nexus files automatically.

## Before Release

- [ ] [F] Confirm `manifest.json` version.
- [ ] [F] Confirm `RealityCheck.csproj` builds.
- [ ] [F] Run `dotnet build`.
- [ ] [F] Confirm generated zip name/version.
- [ ] [F] Confirm deployed mod folder contains current files.
- [ ] [F] Launch through SMAPI.
- [ ] [F] Load a save.
- [ ] [F] Open Financial Manual with `O` or configured key.
- [ ] [F] Check Daily, Seasonal, Annual, Tax, Market Price, and Exchange UI.
- [ ] [F] Review SMAPI log.
- [ ] [P] Prepare GitHub/Nexus changelog from `CHANGELOG.md` and `docs/VERSION_HISTORY.md`.
- [ ] [U] Confirm Nexus page/file/version history manually if publishing there.

## Release Safety

- Do not change save-data schema in a release without migration notes.
- Do not change tax/market/report formulas without explicit release notes.
- Do not publish from a dirty working tree.
- Do not force-push release branches.

