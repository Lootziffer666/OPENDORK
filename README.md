# OPENDORK MVP (Deterministic, AI-free Orchestrator)

## High-level design
OPENDORK is split into deterministic modules with strict separation:
- **`opendork-core`**: queue/state machine, retry/backoff, syntax gate, deterministic routing execution, append-only persistence.
- **`opendork-browser-playwright`**: browser adapter seam for Playwright + persistent context per role.
- **`opendork-rules`**: data-driven rules (routing, limits, selectors, timeouts).
- **`opendork-wrapper-bridge`**: optional file-based JSON IPC to existing wrapper.
- **`opendork-tests`**: unit/integration-lite tests for rules, state machine, retry, IPC, failure handling.

Runtime flow:
1. Prompt job dequeued.
2. Generator role runs in browser adapter.
3. Optional syntax gate (e.g., JS/TS).
4. Reviewer role classifies with `[STATUS: ...]` tags.
5. Route to GOLD / CRAP / REWORK.
6. Rework loops by deterministic retry policy.
7. Append-only logs and snapshots persisted every iteration.

## Project structure
- `OPENDORK.sln`
- `opendork-core/`
- `opendork-rules/`
- `opendork-browser-playwright/`
- `opendork-wrapper-bridge/`
- `opendork-tests/`
- `config/rules.json`
- `config/browser-roles.json`

## How to run
1. Install .NET 8 SDK on Windows.
2. Open `OPENDORK.sln` in Visual Studio or run CLI build/test.
3. Configure rules and role profiles in `config/`.
4. Integrate `PlaywrightBrowserAdapter` with Microsoft.Playwright runtime methods for production browser control.

## Persistent browser contexts
`browser-roles.json` defines one profile directory per role:
- Generator -> `profiles/generator`
- Reviewer -> `profiles/reviewer`
- Comparator -> `profiles/comparator`

This keeps sessions isolated and restart-safe.

## Rules/configuration
`config/rules.json` controls:
- route tags (`[STATUS: GOLD|CRAP|REWORK]`)
- limit regexes
- provider selectors
- timeout settings

No business logic is in UI; core consumes rules deterministically.

## Replay and logging
Append-only files under chosen log root:
- `iterations.jsonl`
- `events.jsonl`
- `failures.jsonl`
- `gold.md`
- `crap.md`
- `rework.md`
- queue snapshots (`snapshots.jsonl` path configured by caller)

This supports morning review and deterministic replay.

## In MVP / Out of MVP
### In MVP
- deterministic orchestrator core
- config-driven routing/limits
- browser adapter boundary for Playwright integration
- optional wrapper IPC bridge
- retry/backoff and failure states

### Out of MVP
- embedded AI/agents/autonomous strategy
- custom browser/electron monolith
- scheduler as orchestration brain
- multi-user orchestration


## Verification policy
- This repository must only report checks that were actually executed in the current environment.
- In this container, `dotnet` is unavailable, so `dotnet build/test` cannot be executed here.
- Required local validation on a Windows machine with .NET 8 SDK:
  - `dotnet build OPENDORK.sln`
  - `dotnet test OPENDORK.sln`

