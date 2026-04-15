# Provider Routing & Lite Gateway

OPENDORK enthält ein LiteLLM-ähnliches Gateway in `opendork-providers`:

- `ProviderModelCatalog`: model registry (list/add/remove/info) aus `config/providers.json`.
- `LiteLlmStyleGateway`: completion entrypoint mit cost estimation, cache und budget checks.
- `ProviderRouter`: retries/failover.
- `ProviderCooldownStore`: temporäre Sperre bei wiederholten Fehlern.
- `ProviderHealthTracker` + `ProviderReputationTracker`: priorisieren stabile Provider.
- `BudgetGuard`: hartes Spending-Limit im Zeitfenster.
- `ResponseCache`: TTL-basierter Prompt+Model Cache.

Damit wird der frühere Guard-/Proxy-Zweck OPENDORK-intern umgesetzt.
