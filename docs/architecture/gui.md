# GUI (Taildrops-inspired)

`opendork-gui` adds a visual control center for OPENDORK runtime operations:

- spend / budget / run / model KPI cards
- run submission form with `opendork-cli run ...` preview
- model catalog table with enable/disable toggles
- recent run feed and artifact counters

Current implementation is static (Tailwind CDN + vanilla JS) and persists local demo state in browser localStorage.

Next step for production: connect to OPENDORK CLI/state endpoints (status, report, jobs, replay, models).
