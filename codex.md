````prompt
### Role
Act as an experienced .NET/C# principal engineer for multi-project monorepo refactors and platform consolidation.

### Task
Refactor OPENDORK to fully absorb CATALON-GUARD as native functionality, so OPENDORK becomes the only remaining product identity, runtime model, CLI surface, and configuration world.

### Context
- Repository scope: whole project.
- Host architecture: OPENDORK remains the host system and strategic center.
- Legacy source of capabilities: CATALON-GUARD contains useful operational concepts that must be absorbed, but not preserved as a separate subsystem or product.
- Relevant areas to inspect and modify:
  - `OPENDORK.sln`
  - `README.md`
  - `opendork-core/*`
  - `opendork-browser-playwright/*`
  - `opendork-rules/*`
  - `opendork-tests/*`
  - `opendork-wrapper-bridge/*` (deprecate if still needed temporarily; do not expand it)
  - `CATALON-GUARD/**` (mine for useful behavior, then remove/deprecate where safe)
- Default decisions:
  - `opendork-wrapper-bridge` should be deprecated first, not treated as the architectural center.
  - Ignore any CATALON-GUARD UI unless it is required for runtime behavior.
  - SQLite must be implemented now, not only stubbed.
  - Provider clients should be interface-first, but minimally runnable.
- Known constraints:
  - Do not preserve the `CATALON-GUARD` name in runtime code, folders, commands, docs, or config keys except in migration documentation.
  - Do not create a bridge-first or proxy-first architecture.
  - Do not keep duplicate run scripts, duplicate stats scripts, duplicate config systems, or a separate guard CLI.
  - Do not rely on any external “guard” process for OPENDORK to function.
  - No external network calls after setup script.
  - Never expose secrets, API keys, or PII.

### Non-Negotiable Architectural Intent
- Keep OPENDORK as the host architecture.
- Preserve only useful QoL and operational capabilities from CATALON-GUARD.
- Re-express useful legacy concepts idiomatically inside OPENDORK-native modules.
- Discard legacy files that do not map cleanly; extract the capability, not the file identity.

### Capabilities to Preserve from CATALON-GUARD
- provider failover
- provider cooldown handling
- retry orchestration
- validation pipeline hooks
- candidate scoring/promotion concepts
- SQLite-backed job and candidate state
- artifact export for gold candidates
- diffs between promoted candidates
- replay/report/status ergonomics
- overnight-oriented runtime profiles

### Things That Must Not Be Preserved
- separate CATALON-GUARD repo identity
- separate guard CLI or standalone proxy startup flow
- separate `guard` config world such as `config.yaml` as the central source
- any architecture where OPENDORK depends on an external guard process
- leftover `catalon-guard` naming outside migration notes

### Target Modules
Refactor or create OPENDORK into these modules:
- `opendork-abstractions`
- `opendork-core`
- `opendork-browser-playwright`
- `opendork-providers`
- `opendork-validation`
- `opendork-rules`
- `opendork-state`
- `opendork-artifacts`
- `opendork-cli`
- `opendork-tests`

### Architectural Rules
1. `opendork-core` orchestrates runs, steps, retries, budgets, and routing decisions.
2. `opendork-providers` owns provider selection, fallback chains, cooldowns, health, and reputation.
3. `opendork-validation` owns validators and validation pipelines, including adapters for internal tools.
4. `opendork-state` owns SQLite persistence, replay metadata, candidate history, and event journaling.
5. `opendork-artifacts` owns gold export, diffs, reports, and artifact directory layout.
6. `opendork-cli` is the only first-class command surface for runtime operations and reporting.
7. Existing wrapper-bridge ideas must be deprecated or minimized; do not expand them.

### Required First-Class Data Models
Implement first-class models for:
- `RunContext`
- `Candidate`
- `ProviderAttempt`
- `ValidationResult`
- `EventRecord`
- `ArtifactRecord`

### Required Candidate Lifecycle
Candidates must support these states:
- `raw`
- `validated`
- `rejected`
- `gold`

### Required SQLite Persistence
Create storage schema and migrations for:
- `runs`
- `candidates`
- `provider_attempts`
- `validation_results`
- `events`

### Required Runtime / Config Split
Replace any monolithic guard-style config with OPENDORK-native config files:
- `config/providers.json`
- `config/validation-profiles.json`
- `config/runtime-profiles.json`

### Required Runtime Profiles
Support at least:
- `interactive`
- `batch`
- `overnight_safe`
- `overnight_aggressive`

### Required Validation Architecture
Validation must be plugin-oriented, not hardcoded into the orchestrator.
Introduce:
- `IValidator`
- `ValidationPipeline`
- `ValidationProfileResolver`
- adapters for internal tool validators

Validation profiles must allow task-specific composition.

### Required Provider Architecture
Implement:
- `ProviderRouter`
- `ProviderChainResolver`
- `ProviderCooldownStore`
- `ProviderHealthTracker`
- `ProviderReputationTracker`
- `OpenAiCompatibleClient`
- `LocalFallbackClient`

### Required Artifact Layout
Implement these artifact directories and ownership:
- `results/raw`
- `results/validated`
- `results/rejected`
- `results/gold`
- `results/diffs`
- `results/reports`
- `results/replays`

### Required CLI Surface
Implement commands or runnable stubs for:
- `run`
- `status`
- `jobs`
- `replay`
- `report`

### Migration Behavior
- Move useful logic from CATALON-GUARD into OPENDORK-native modules.
- Do not do a line-by-line language port unless strictly necessary.
- Preserve behavior and concepts, not file identity or naming.
- Add migration notes under `docs/migration/catalon-guard-absorption.md`.
- Mark obsolete bridge-only or guard-only code paths as deprecated and remove them where safe.

### Deletion / Cleanup Behavior
After migration, remove or deprecate:
- standalone guard startup scripts
- guard-specific stats scripts
- guard-specific root identity
- leftover `catalon-guard` naming in runtime code
- duplicate config sources
- duplicate environment/setup instructions

### Explicit Do-Not-Drift Constraints
- Do not build a second product inside OPENDORK.
- Do not keep CATALON-GUARD as a subfoldered subsystem with its own lifecycle.
- Do not preserve old script-based runtime patterns if OPENDORK CLI can own them.
- Do not centralize validation inside one giant class.
- Do not store only the last good response; store candidate history.
- Do not hardcode validators; make them composable by profile.
- Do not keep wrapper-bridge as the strategic center.
- Do not retain `catalon-guard` naming except in migration docs.

### File Operations Guidance

#### Create
```text
docs/architecture/runtime-flow.md
docs/architecture/provider-routing.md
docs/architecture/validation-pipeline.md
docs/architecture/replay-and-artifacts.md
docs/migration/catalon-guard-absorption.md

config/providers.json
config/validation-profiles.json
config/runtime-profiles.json

opendork-abstractions/*
opendork-providers/*
opendork-validation/*
opendork-state/*
opendork-artifacts/*
opendork-cli/*
````

#### Modify

```text
OPENDORK.sln
README.md
opendork-core/*
opendork-browser-playwright/*
opendork-rules/*
opendork-tests/*
```

#### Deprecate or Remove

```text
opendork-wrapper-bridge/*          # deprecate first if functionality still referenced
CATALON-GUARD/catalon-guard/*
CATALON-GUARD/run scripts
CATALON-GUARD/guard stats scripts
CATALON-GUARD/config.yaml
```

### Execution Strategy

Perform this in two passes inside one response.

#### Pass 1 — Structure and Skeleton

Create the target OPENDORK architecture skeleton for absorbing CATALON-GUARD. Do not attempt full feature completion yet. Focus on:

* solution/project restructuring
* new module creation
* core models
* interfaces
* SQLite schema/migrations
* config files
* CLI command scaffolding
* docs and migration notes
* file operation summary

#### Pass 2 — Functional Migration

Then migrate useful capabilities from CATALON-GUARD into the new OPENDORK-native modules. Preserve behavior, not file identity. Implement:

* provider routing / fallback / cooldown basics
* validation pipeline composition
* candidate persistence and promotion states
* artifact export layout
* report / status / replay plumbing

Then remove or deprecate obsolete guard-specific code and summarize remaining TODOs.

### Priority Order

1. native architecture correctness
2. removal of duplicate concepts
3. persistence and traceability
4. validation composability
5. provider reliability behavior
6. reporting ergonomics
7. polish

### Expected Output

Produce all of the following:

1. A brief architecture summary of the final target design.
2. The final target tree.
3. A concrete file operation list: create / modify / move / delete / deprecate.
4. A unified diff patch for the most important implementation changes, plus new file contents where needed.
5. The key interfaces, classes, and migrations added.
6. Any temporary compatibility shims, clearly labeled as transitional.
7. A cleanup summary showing what legacy CATALON-GUARD pieces were removed or deprecated.
8. A follow-up TODO list for anything not fully completed in one pass.
9. Tests proving the new structure and core behavior.

### Guidance for Codex

1. Think step by step using structured reasoning: inspect → design → patch → review.
2. Run a self-critique loop once: generate → review for drift/duplication → improve.
3. Prefer idiomatic OPENDORK-native abstractions over wrappers or bridges.
4. Keep the output focused and implementation-oriented.
5. Do not exceed 500 new lines unless absolutely necessary; prioritize the highest-value files if you must trim.
6. If some legacy behavior is unclear, choose the design that best satisfies the architectural rules and document the assumption in migration notes.
7. Never preserve `CATALON-GUARD` naming outside migration docs.

### Setup Script

```bash
# Fill only with repo-specific restore/build/test commands if needed.
# Example:
# dotnet restore OPENDORK.sln
# dotnet build OPENDORK.sln
# dotnet test OPENDORK.sln
```

### End

```
```
