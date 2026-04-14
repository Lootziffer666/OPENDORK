using OpenDork.Core;

namespace OpenDork.Browser.Playwright;

public record BrowserRoleConfig(string Role, string UserDataDir, string StartUrl, string Provider);

public sealed class PlaywrightBrowserAdapter : IBrowserAdapter
{
    private readonly Dictionary<string, BrowserRoleConfig> _roles;
    public PlaywrightBrowserAdapter(IEnumerable<BrowserRoleConfig> roles) => _roles = roles.ToDictionary(x => x.Role, x => x);

    public Task<(BrowserJobStatus Status, string Response)> RunRoleAsync(string role, string payload, CancellationToken ct)
    {
        // Deterministic adapter seam. Wire Microsoft.Playwright calls here in Windows runtime:
        // - launch persistent context per role (UserDataDir)
        // - navigate/start url
        // - fill prompt and submit
        // - wait for response-finished heuristic
        // - return captured text + BrowserJobStatus
        if (!_roles.ContainsKey(role)) return Task.FromResult((BrowserJobStatus.ManualAttentionRequired, "missing role config"));
        if (payload.Contains("[SIMULATE_LIMIT]", StringComparison.OrdinalIgnoreCase)) return Task.FromResult((BrowserJobStatus.RateLimited, "rate limit"));
        return Task.FromResult((BrowserJobStatus.Healthy, payload));
    }
}
