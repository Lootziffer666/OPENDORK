# OPENDORK Platform (Unified Runtime)

OPENDORK is now the single product/runtime identity. Legacy guard capabilities were absorbed as native modules, so there is no separate guard process, CLI, or config world.

## Modules

- `opendork-abstractions`: shared models and interfaces (`RunContext`, `Candidate`, `ProviderAttempt`, `ValidationResult`, `EventRecord`, `ArtifactRecord`).
- `opendork-core`: orchestration runtime (`RunOrchestrator`) and legacy deterministic MVP engine.
- `opendork-providers`: provider chain resolution, failover, retry/cooldown, health and reputation tracking.
- `opendork-validation`: plugin validators and profile-based validation pipelines.
- `opendork-state`: SQLite state schema + migration + persistence.
- `opendork-artifacts`: candidate export, diffs, reports, replay file layout.
- `opendork-cli`: first-class operational command surface.
- `opendork-rules`, `opendork-browser-playwright`: rules + browser integration seams.
- `opendork-wrapper-bridge`: deprecated transitional shim (do not extend).
- `opendork-tests`: structure and behavior tests.

## CLI

`opendork-cli` commands:

- `run`
- `status`
- `jobs`
- `replay`
- `report`

## Config files

- `config/providers.json`
- `config/validation-profiles.json`
- `config/runtime-profiles.json`

Supported runtime profiles:

- `interactive`
- `batch`
- `overnight_safe`
- `overnight_aggressive`

## Persistence + artifacts

SQLite tables:

- `runs`
- `candidates`
- `provider_attempts`
- `validation_results`
- `events`

Artifact tree:

- `results/raw`
- `results/validated`
- `results/rejected`
- `results/gold`
- `results/diffs`
- `results/reports`
- `results/replays`

## Build/test

```bash
dotnet restore OPENDORK.sln
dotnet build OPENDORK.sln
dotnet test OPENDORK.sln
```
