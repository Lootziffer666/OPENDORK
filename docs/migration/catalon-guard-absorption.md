# CATALON-GUARD Absorption Notes

## Migrated concepts

- Provider failover + retries + cooldown logic migrated into `opendork-providers`.
- Monitoring-oriented event journaling moved to SQLite in `opendork-state`.
- Candidate progression and scoring promoted as first-class models in `opendork-abstractions`.
- Gold export and diff/report scaffolding implemented in `opendork-artifacts`.
- Runtime command surface consolidated in `opendork-cli` (`run`, `status`, `jobs`, `replay`, `report`).

## Deliberately not preserved

- No standalone guard process.
- No guard-specific `config.yaml` runtime ownership.
- No duplicate startup or stats scripts.
- No runtime `catalon-guard` naming outside this migration note.

## Transitional shim

`opendork-wrapper-bridge` remains as deprecated compatibility only and should be removed after downstream callers move to `opendork-cli` + SQLite state.
