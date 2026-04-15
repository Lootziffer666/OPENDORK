# Runtime Flow

OPENDORK now runs all orchestration through `opendork-cli` and `RunOrchestrator`.

1. `run` command builds a `RunContext` from CLI args.
2. Providers are selected via `ProviderChainResolver` and executed by `ProviderRouter` with retries and cooldown.
3. Raw candidate is persisted in SQLite and exported under `results/raw`.
4. `ValidationPipeline` executes plugin validators.
5. Candidate is promoted to `validated`, `rejected`, or `gold` and exported.
6. Events and validation outcomes are journaled in SQLite tables for replay/report.
