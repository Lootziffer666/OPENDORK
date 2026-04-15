using OpenDork.Abstractions;

namespace OpenDork.Providers;

public sealed class ProviderCooldownStore
{
    private readonly Dictionary<string, DateTimeOffset> _cooldowns = new(StringComparer.OrdinalIgnoreCase);
    public void MarkCooldown(string provider, TimeSpan ttl) => _cooldowns[provider] = DateTimeOffset.UtcNow.Add(ttl);
    public bool IsCoolingDown(string provider) => _cooldowns.TryGetValue(provider, out var until) && until > DateTimeOffset.UtcNow;
}

public sealed class ProviderHealthTracker
{
    private readonly Dictionary<string, int> _failures = new(StringComparer.OrdinalIgnoreCase);
    public void Register(string provider, bool success) => _failures[provider] = success ? 0 : (_failures.TryGetValue(provider, out var f) ? f + 1 : 1);
    public int GetFailureCount(string provider) => _failures.TryGetValue(provider, out var f) ? f : 0;
}

public sealed class ProviderReputationTracker
{
    private readonly Dictionary<string, int> _scores = new(StringComparer.OrdinalIgnoreCase);
    public void Register(string provider, bool success) => _scores[provider] = (_scores.TryGetValue(provider, out var s) ? s : 0) + (success ? 1 : -1);
    public int Score(string provider) => _scores.TryGetValue(provider, out var s) ? s : 0;
}

public sealed class ProviderChainResolver
{
    public IReadOnlyList<IProviderClient> Resolve(IReadOnlyList<IProviderClient> providers, string runtimeProfile)
        => runtimeProfile.Contains("overnight", StringComparison.OrdinalIgnoreCase)
            ? providers.OrderByDescending(p => p.Name.Contains("local", StringComparison.OrdinalIgnoreCase)).ToList()
            : providers;
}

public sealed class ProviderRouter
{
    private readonly ProviderCooldownStore _cooldowns;
    private readonly ProviderHealthTracker _health;
    private readonly ProviderReputationTracker _reputation;

    public ProviderRouter(ProviderCooldownStore cooldowns, ProviderHealthTracker health, ProviderReputationTracker reputation)
        => (_cooldowns, _health, _reputation) = (cooldowns, health, reputation);

    public async Task<ProviderResponse> RouteWithFailoverAsync(IEnumerable<IProviderClient> chain, string prompt, int maxRetries, CancellationToken ct = default)
    {
        foreach (var provider in chain.OrderByDescending(p => _reputation.Score(p.Name)))
        {
            if (_cooldowns.IsCoolingDown(provider.Name)) continue;

            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                var response = await provider.GenerateAsync(prompt, ct);
                _health.Register(provider.Name, response.Success);
                _reputation.Register(provider.Name, response.Success);
                if (response.Success) return response;

                if (_health.GetFailureCount(provider.Name) >= 2)
                {
                    _cooldowns.MarkCooldown(provider.Name, TimeSpan.FromMinutes(2));
                    break;
                }
            }
        }

        return new ProviderResponse("none", false, string.Empty, "all providers unavailable");
    }
}

public sealed class OpenAiCompatibleClient : IProviderClient
{
    public string Name { get; }
    public OpenAiCompatibleClient(string name = "openai-compatible") => Name = name;
    public Task<ProviderResponse> GenerateAsync(string prompt, CancellationToken ct = default)
        => Task.FromResult(new ProviderResponse(Name, true, $"[openai-compatible] {prompt}", "ok"));
}

public sealed class LocalFallbackClient : IProviderClient
{
    public string Name { get; }
    public LocalFallbackClient(string name = "local-fallback") => Name = name;
    public Task<ProviderResponse> GenerateAsync(string prompt, CancellationToken ct = default)
        => Task.FromResult(new ProviderResponse(Name, true, $"[local-fallback] {prompt}", "ok"));
}
