# CATALON-GUARD Absorption Notes

## Migrated concepts

- Provider failover + retries + cooldown (`opendork-providers`).
- Model management (list/add/remove/info) via `opendork-cli models` + `ProviderModelCatalog`.
- Budget guard + usage/cost estimation + cache behavior via `LiteLlmStyleGateway`.
- SQLite state + spend logging (`spend_logs`) for reporting.
- Candidate lifecycle + artifact export via OPENDORK modules.
- Unified CLI command surface (`run`, `chat`, `status`, `jobs`, `replay`, `report`, `models`).

## Deliberately not preserved

- No standalone guard process.
- No guard-specific startup/stats scripts.
- No central guard `config.yaml` runtime ownership.
- No runtime `catalon-guard` naming outside migration notes.

## Transitional shim

`opendork-wrapper-bridge` bleibt nur als deprecated compatibility layer, bis alle Altaufrufe auf OPENDORK CLI/SQLite umgestellt sind.
