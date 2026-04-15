# Provider Routing

Provider routing is OPENDORK-native and absorbs the legacy proxy failover concepts:

- `ProviderRouter`: retries provider calls and fails over to next provider in chain.
- `ProviderCooldownStore`: blocks providers temporarily after repeated failures.
- `ProviderHealthTracker`: tracks rolling failures.
- `ProviderReputationTracker`: biases provider order by observed success.
- `OpenAiCompatibleClient` and `LocalFallbackClient`: first runnable provider clients.
