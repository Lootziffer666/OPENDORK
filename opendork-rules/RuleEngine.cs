using System.Text.Json;
using System.Text.RegularExpressions;
namespace OpenDork.Rules;

public enum RouteStatus { Gold, Crap, Rework, Unknown }
public record RoutingResult(RouteStatus Route, string Evidence);


public record RouteRule(string Pattern, RouteStatus Route);
public record LimitRule(string Pattern);
public record ProviderSelector(string Provider, string InputSelector, string SendSelector, string OutputSelector);
public record RulesConfig(List<RouteRule> Routes, List<LimitRule> Limits, List<ProviderSelector> Selectors, int TimeoutSeconds);

public sealed class RuleEngine
{
    private readonly List<(Regex Rx, RouteStatus Route)> _routes;
    private readonly List<Regex> _limits;
    public RuleEngine(RulesConfig c)
    {
        _routes = c.Routes.Select(r => (new Regex(r.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), r.Route)).ToList();
        _limits = c.Limits.Select(l => new Regex(l.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToList();
    }

    public RoutingResult Route(string content)
    {
        foreach (var (rx, route) in _routes) if (rx.IsMatch(content)) return new(route, rx.ToString());
        return new(RouteStatus.Unknown, "no-rule");
    }

    public bool IsLimit(string content) => _limits.Any(l => l.IsMatch(content));

    public static RulesConfig Load(string path)
        => JsonSerializer.Deserialize<RulesConfig>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
           ?? throw new InvalidDataException("rules config invalid");
}
