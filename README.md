# OPENDORK Platform (Unified Runtime)

OPENDORK ist jetzt das einzige Runtime-Produkt. Die früheren Guard-/LiteLLM-Konzepte sind in OPENDORK-nativen Modulen integriert (kein externer Guard-Prozess, keine separate Guard-CLI).

## Module

- `opendork-abstractions`: Kernmodelle und Interfaces.
- `opendork-core`: Orchestrator für Runs/Candidates.
- `opendork-providers`: LiteLLM-ähnliches Gateway (Model Catalog, Failover, Cooldown, Budget, Cache).
- `opendork-validation`: Plugin-basierte Validierungspipelines.
- `opendork-state`: SQLite-State inkl. Migrationen und Spend-Logs.
- `opendork-artifacts`: Export für raw/validated/rejected/gold + diffs/reports/replays.
- `opendork-cli`: zentrales Kommando-Interface.
- `opendork-wrapper-bridge`: deprecated Übergangskomponente.

## CLI

```bash
opendork-cli run "prompt" interactive gpt-4o
opendork-cli chat gpt-4o "Sag Hallo"
opendork-cli status
opendork-cli report
opendork-cli models list
opendork-cli models info gpt-4o
opendork-cli models add my-model openai-compatible 0.01 0.02
opendork-cli models remove my-model
```

Weitere Commands: `jobs`, `replay`.

## LiteLLM-ähnliche Features in OPENDORK

- Model Catalog in `config/providers.json`.
- Provider-Routing mit Retry, Failover, Cooldown.
- Budget Guard (Default: 5 USD / 24h) und Spend-Tracking.
- 24h Response Cache.
- Kosten-/Token-Usage pro Completion.
- SQLite `spend_logs` für Reporting.


## GUI

A new Taildrops-inspired web GUI is available at `opendork-gui/` with:

- runtime stats cards
- run form with CLI preview
- model catalog management
- recent runs + artifact counters

Run locally:

```bash
cd opendork-gui
python3 -m http.server 8080
```

Then open `http://localhost:8080`.

## Config

- `config/providers.json`
- `config/validation-profiles.json`
- `config/runtime-profiles.json`

Runtime-Profile:

- `interactive`
- `batch`
- `overnight_safe`
- `overnight_aggressive`

## Persistenz

SQLite Tabellen:

- `runs`
- `candidates`
- `provider_attempts`
- `validation_results`
- `events`
- `spend_logs`

## Build/Test

```bash
dotnet restore OPENDORK.sln
dotnet build OPENDORK.sln
dotnet test OPENDORK.sln
```
