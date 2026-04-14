using System.Collections.Concurrent;
using System.Text.Json;
using OpenDork.Rules;

namespace OpenDork.Core;

public interface IBrowserAdapter
{
    Task<(BrowserJobStatus Status, string Response)> RunRoleAsync(string role, string payload, CancellationToken ct);
}

public interface ISyntaxGate { SyntaxGateResult Validate(string content, string? language); }

public sealed class QueueStore
{
    private readonly string _path;
    private readonly ConcurrentDictionary<string, byte> _processed = new();
    public QueueStore(string path) => _path = path;
    public void MarkProcessed(string id) => _processed[id] = 1;
    public bool IsProcessed(string id) => _processed.ContainsKey(id);
    public void SaveSnapshot(JobSnapshot s) => File.AppendAllText(_path, JsonSerializer.Serialize(s) + Environment.NewLine);
}

public sealed class JsonlLog
{
    private readonly string _root;
    public JsonlLog(string root) { _root = root; Directory.CreateDirectory(root); }
    public void Event(IterationEvent e) => Append("events.jsonl", e);
    public void Failure(object e) => Append("failures.jsonl", e);
    public void Iteration(object e) => Append("iterations.jsonl", e);
    public void Route(RouteStatus route, string payload)
    {
        var file = route switch { RouteStatus.Gold => "gold.md", RouteStatus.Crap => "crap.md", RouteStatus.Rework => "rework.md", _ => "crap.md" };
        File.AppendAllText(Path.Combine(_root, file), $"\n## {DateTimeOffset.UtcNow:u}\n{payload}\n");
    }
    private void Append(string file, object e) => File.AppendAllText(Path.Combine(_root, file), JsonSerializer.Serialize(e) + Environment.NewLine);
}

public sealed class OrchestratorEngine
{
    private readonly QueueStore _store;
    private readonly JsonlLog _log;
    private readonly RuleEngine _rules;
    private readonly IBrowserAdapter _browser;
    private readonly ISyntaxGate _syntax;
    private readonly RetryPolicy _retry;
    public WorkflowState State { get; private set; } = WorkflowState.Idle;

    public OrchestratorEngine(QueueStore store, JsonlLog log, RuleEngine rules, IBrowserAdapter browser, ISyntaxGate syntax, RetryPolicy retry)
        => (_store, _log, _rules, _browser, _syntax, _retry) = (store, log, rules, browser, syntax, retry);

    public void Start() => State = WorkflowState.Running;
    public void Pause() => State = WorkflowState.Paused;
    public void Stop() => State = WorkflowState.Stopped;

    public async Task<RouteStatus> ProcessAsync(PromptJob job, CancellationToken ct = default)
    {
        if (State != WorkflowState.Running || _store.IsProcessed(job.Id)) return RouteStatus.Unknown;
        _log.Event(new(DateTimeOffset.UtcNow, job.Id, JobStep.Generator, "Begin"));

        var gen = await _browser.RunRoleAsync("Generator", job.Prompt, ct);
        if (NeedsManual(job, gen.Status, JobStep.Generator)) return RouteStatus.Crap;

        if (!string.IsNullOrWhiteSpace(job.Language))
        {
            var syntax = _syntax.Validate(gen.Response, job.Language);
            if (!syntax.IsValid)
            {
                _log.Failure(new { job.Id, reason = "syntax_failed", syntax.Evidence });
                _log.Route(RouteStatus.Crap, gen.Response);
                _store.MarkProcessed(job.Id); _store.SaveSnapshot(new(job.Id, State, JobStep.SyntaxGate, job.Attempts, RouteStatus.Crap));
                return RouteStatus.Crap;
            }
        }

        var rev = await _browser.RunRoleAsync("Reviewer", gen.Response, ct);
        if (NeedsManual(job, rev.Status, JobStep.Reviewer)) return RouteStatus.Crap;

        var route = _rules.Route(rev.Response);
        _log.Route(route.Route, rev.Response);
        _log.Iteration(new { job.Id, generator = gen.Response, reviewer = rev.Response, route = route.Route.ToString() });
        if (route.Route == RouteStatus.Rework && job.Attempts < _retry.MaxAttempts)
        {
            _store.SaveSnapshot(new(job.Id, State, JobStep.Routed, job.Attempts, route.Route));
            await Task.Delay(TimeSpan.FromSeconds(_retry.BaseSeconds * (job.Attempts + 1)), ct);
            return await ProcessAsync(job with { Attempts = job.Attempts + 1 }, ct);
        }

        _store.MarkProcessed(job.Id);
        _store.SaveSnapshot(new(job.Id, State, JobStep.Routed, job.Attempts, route.Route));
        return route.Route;
    }

    private bool NeedsManual(PromptJob job, BrowserJobStatus status, JobStep step)
    {
        if (status is BrowserJobStatus.RateLimited or BrowserJobStatus.NeedsLogin or BrowserJobStatus.SelectorFailed or BrowserJobStatus.CaptureFailed or BrowserJobStatus.ManualAttentionRequired)
        {
            _log.Failure(new { job.Id, step = step.ToString(), status = status.ToString() });
            _store.SaveSnapshot(new(job.Id, State, step, job.Attempts, RouteStatus.Crap));
            return true;
        }
        return false;
    }
}

public sealed class NodeSyntaxGate : ISyntaxGate
{
    public SyntaxGateResult Validate(string content, string? language)
    {
        if (!string.Equals(language, "js", StringComparison.OrdinalIgnoreCase) && !string.Equals(language, "ts", StringComparison.OrdinalIgnoreCase))
            return new(true, "Skipped for non-js/ts language.");
        var p = Path.GetTempFileName() + ".js"; File.WriteAllText(p, content);
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("node", $"-c \"{p}\"") { RedirectStandardError = true, RedirectStandardOutput = true };
            using var proc = System.Diagnostics.Process.Start(psi); proc!.WaitForExit(10000);
            var evidence = (proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd()).Trim();
            return new(proc.ExitCode == 0, string.IsNullOrWhiteSpace(evidence) ? "ok" : evidence);
        }
        catch (Exception ex) { return new(false, ex.Message); }
        finally { if (File.Exists(p)) File.Delete(p); }
    }
}
