# Replay and Artifacts

State for replay/report is split into:

- SQLite (`runs`, `candidates`, `provider_attempts`, `validation_results`, `events`) for queryable history.
- Filesystem artifact layout:
  - `results/raw`
  - `results/validated`
  - `results/rejected`
  - `results/gold`
  - `results/diffs`
  - `results/reports`
  - `results/replays`

This mirrors operational traceability from the legacy guard tool while keeping OPENDORK as the only runtime system.
