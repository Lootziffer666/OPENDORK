using System.Text.Json;
using OpenDork.Abstractions;

namespace OpenDork.Providers;

public sealed record ProviderModelDefinition(string ModelName, string ProviderClient, decimal InputCostPer1K, decimal OutputCostPer1K, bool Enabled = true);
public sealed record GatewayUsage(decimal EstimatedCost, int PromptTokens, int CompletionTokens, bool CacheHit);
public sealed record GatewayResult(ProviderResponse Response, GatewayUsage Usage, string Model);

public sealed class ProviderModelCatalog
{
    private readonly Dictionary<string, ProviderModelDefinition> _models = new(StringComparer.OrdinalIgnoreCase);

    public ProviderModelCatalog(IEnumerable<ProviderModelDefinition>? seed = null)
    {
        if (seed is null) return;
        foreach (var model in seed) _models[model.ModelName] = model;
    }

    public IReadOnlyCollection<ProviderModelDefinition> List() => _models.Values.OrderBy(x => x.ModelName).ToList();
    public ProviderModelDefinition? Get(string modelName) => _models.TryGetValue(modelName, out var model) ? model : null;
    public void Upsert(ProviderModelDefinition model) => _models[model.ModelName] = model;
    public bool Remove(string modelName) => _models.Remove(modelName);

    public static ProviderModelCatalog LoadFromJson(string path)
    {
        if (!File.Exists(path)) return new ProviderModelCatalog();
        var root = JsonSerializer.Deserialize<ProviderCatalogConfig>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        return new ProviderModelCatalog(root.Models);
    }

    public void SaveToJson(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var json = JsonSerializer.Serialize(new ProviderCatalogConfig { Models = List().ToList() }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public sealed class ProviderCatalogConfig
    {
        public List<ProviderModelDefinition> Models { get; set; } = [];
    }
}

public sealed class BudgetGuard
{
    private readonly decimal _maxBudget;
    private readonly TimeSpan _window;
    private readonly Queue<(DateTimeOffset Timestamp, decimal Cost)> _entries = new();

    public BudgetGuard(decimal maxBudget, TimeSpan window)
    {
        _maxBudget = maxBudget;
        _window = window;
    }

    public bool TryReserve(decimal cost, out decimal spent, out decimal remaining)
    {
        Trim();
        spent = _entries.Sum(x => x.Cost);
        if (spent + cost > _maxBudget)
        {
            remaining = _maxBudget - spent;
            return false;
        }

        _entries.Enqueue((DateTimeOffset.UtcNow, cost));
        spent += cost;
        remaining = _maxBudget - spent;
        return true;
    }

    public decimal CurrentSpend()
    {
        Trim();
        return _entries.Sum(x => x.Cost);
    }

    public decimal MaxBudget => _maxBudget;

    private void Trim()
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(_window);
        while (_entries.Count > 0 && _entries.Peek().Timestamp < cutoff) _entries.Dequeue();
    }
}

public sealed class ResponseCache
{
    private readonly TimeSpan _ttl;
    private readonly Dictionary<string, (DateTimeOffset Timestamp, string Value)> _entries = new(StringComparer.OrdinalIgnoreCase);

    public ResponseCache(TimeSpan ttl) => _ttl = ttl;

    public bool TryGet(string key, out string value)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.Timestamp.Add(_ttl) > DateTimeOffset.UtcNow)
        {
            value = entry.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public void Put(string key, string value) => _entries[key] = (DateTimeOffset.UtcNow, value);
}

public sealed class LiteLlmStyleGateway
{
    private readonly ProviderModelCatalog _catalog;
    private readonly ProviderRouter _router;
    private readonly ProviderChainResolver _chainResolver;
    private readonly IReadOnlyDictionary<string, IProviderClient> _providers;
    private readonly BudgetGuard _budget;
    private readonly ResponseCache _cache;

    public LiteLlmStyleGateway(
        ProviderModelCatalog catalog,
        ProviderRouter router,
        ProviderChainResolver chainResolver,
        IEnumerable<IProviderClient> providers,
        BudgetGuard budget,
        ResponseCache cache)
    {
        _catalog = catalog;
        _router = router;
        _chainResolver = chainResolver;
        _providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _budget = budget;
        _cache = cache;
    }

    public async Task<GatewayResult> CompleteAsync(string modelName, string prompt, string runtimeProfile, CancellationToken ct = default)
    {
        var model = _catalog.Get(modelName) ?? throw new InvalidOperationException($"Model '{modelName}' not found.");
        if (!model.Enabled) throw new InvalidOperationException($"Model '{modelName}' is disabled.");

        var cacheKey = $"{modelName}:{prompt}";
        if (_cache.TryGet(cacheKey, out var cached))
        {
            var usageCached = BuildUsage(model, prompt, cached, true);
            return new GatewayResult(new ProviderResponse("cache", true, cached, "cache-hit"), usageCached, modelName);
        }

        var providerChain = ResolveProviderChain(model.ProviderClient, runtimeProfile);
        var response = await _router.RouteWithFailoverAsync(providerChain, prompt, 2, ct);
        var usage = BuildUsage(model, prompt, response.Content, false);

        if (!_budget.TryReserve(usage.EstimatedCost, out var spent, out var remaining))
        {
            return new GatewayResult(new ProviderResponse(response.ProviderName, false, string.Empty, $"budget-exceeded spent={spent:F4} remaining={remaining:F4}"), usage, modelName);
        }

        if (response.Success) _cache.Put(cacheKey, response.Content);
        return new GatewayResult(response, usage, modelName);
    }

    public decimal CurrentSpend() => _budget.CurrentSpend();
    public decimal MaxBudget => _budget.MaxBudget;

    private IReadOnlyList<IProviderClient> ResolveProviderChain(string primaryProvider, string runtimeProfile)
    {
        var chain = new List<IProviderClient>();
        if (_providers.TryGetValue(primaryProvider, out var primary)) chain.Add(primary);
        chain.AddRange(_providers.Values.Where(x => !string.Equals(x.Name, primaryProvider, StringComparison.OrdinalIgnoreCase)));
        return _chainResolver.Resolve(chain, runtimeProfile);
    }

    private static GatewayUsage BuildUsage(ProviderModelDefinition model, string prompt, string completion, bool cacheHit)
    {
        var promptTokens = EstimateTokens(prompt);
        var completionTokens = EstimateTokens(completion);
        var cost = ((promptTokens / 1000m) * model.InputCostPer1K) + ((completionTokens / 1000m) * model.OutputCostPer1K);
        if (cacheHit) cost = 0;
        return new GatewayUsage(cost, promptTokens, completionTokens, cacheHit);
    }

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);
}
