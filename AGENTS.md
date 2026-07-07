# Reality Check Codex Rules

Reality Check is a local Stardew Valley SMAPI mod. Treat it as game mod source code with save-data and in-game UI risk, not as a website, SaaS app, or automation service.

Use source labels in project documents when possible:

- `[F]` current source files, manifest, git history, or build output directly support the statement.
- `[P]` historical docs/email/Nexus notes or code structure suggest the statement, but it is not current-source proof by itself.
- `[U]` unconfirmed or unavailable.

## Start-of-task checklist

Before each development task, read:

- `AGENTS.md`
- `CURRENT_STATUS.md`
- `TESTING.md`

Also check the current git state before editing:

- `git status --short --branch`
- recent `git log --oneline`
- relevant source and documentation files for the requested area

Do not assume the current version, source layout, or feature state. Use the local files, git history, README, docs, manifest, and actual command/game results as the source of truth.

## Planning and change control

- Before modifying files, explain the plan.
- Keep changes small, scoped, and easy to revert.
- Do not rewrite or restructure the project unless Stan explicitly asks.
- Do not batch-rename files, namespaces, classes, or UI labels for style alone.
- Do not delete old documentation unless Stan explicitly confirms it.
- Do not auto-commit, push, tag, publish to GitHub, or publish to Nexus without explicit confirmation.
- Do not expand warning cleanup into broader work unless it is part of the requested task.
- For documentation-only workflow tasks, auto-commit is allowed when Stan explicitly requested it and `git status` shows only task-related documentation/ignore changes.
- Never force-push. Never push `main` or `master` unless Stan explicitly requests it.

## Feature boundaries

- Do not start large future systems unless explicitly requested.
- Do not advance banking, loans, or new exchange expansions from the roadmap on your own.
- Do not change market price algorithms unless the task explicitly targets market pricing.
- Do not change tax logic unless the task explicitly targets tax behavior.
- Do not change financial report accounting categories or aggregation rules casually.
- Do not change JSON save-data structures without first explaining the migration impact and asking for confirmation.
- Do not break existing save compatibility.

## Verification rules

- `dotnet build` is required for code changes when feasible, but it is not enough for final acceptance.
- UI work must include an in-game verification plan and, when possible, actual in-game checking through SMAPI.
- For Financial Manual, tax notices, tax reports, market prices, and exchange UI, verify by launching Stardew Valley with SMAPI, loading a save, opening the UI, and checking the visible result.
- For market price, tax, report, exchange, or save-data changes, state the expected impact area and what was not changed.
- After modifying code or workflow docs, update `CURRENT_STATUS.md` and `CHANGELOG.md`.
- For documentation recovery, compare old references against current source and mark conflicts explicitly.

## Current known project facts

- Mod name: Reality Check
- Unique ID: `Stan.RealityCheck`
- Current manifest version: `1.4.1`
- Target framework: `.NET 6.0`
- Minimum SMAPI API version: `4.0.0`
- Current repository branch at onboarding: `main`

## Git workflow

- Prefer a task branch for non-trivial documentation or code work.
- Current documentation recovery branch: `docs/recover-project-docs`.
- Commit message for this recovery task: `docs: recover Reality Check project documentation and workflow`.
- Push only to a non-main working branch after confirming remote/branch and never with force.
