using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenDork.Core;
using OpenDork.Rules;
using OpenDork.WrapperBridge;
using Xunit;

namespace OpenDork.Tests;

public class MvpTests
{
    [Fact]
    public void Routes_By_Status_Tag()
    {
        var re = new RuleEngine(new RulesConfig([new("\\[STATUS:\\s*GOLD\\]", RouteStatus.Gold)], [], [], 30));
        Assert.Equal(RouteStatus.Gold, re.Route("x [STATUS: GOLD]").Route);
    }

    [Fact]
    public void Detects_Limits()
    {
        var re = new RuleEngine(new RulesConfig([], [new("message cap")], [], 30));
        Assert.True(re.IsLimit("message cap reached"));
    }

    [Fact]
    public async Task StateMachine_And_Retry_Are_Deterministic()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        var core = new OrchestratorEngine(new QueueStore(Path.Combine(temp, "snap.jsonl")), new JsonlLog(temp),
            new RuleEngine(new RulesConfig([new("\\[STATUS:\\s*REWORK\\]", RouteStatus.Rework), new("\\[STATUS:\\s*GOLD\\]", RouteStatus.Gold)], [], [], 30)),
            new FakeBrowser(["generator out", "[STATUS: REWORK]", "generator out 2", "[STATUS: GOLD]"]),
            new PassSyntax(), new RetryPolicy(2, 0));
        core.Start();
        var result = await core.ProcessAsync(new PromptJob("1", "prompt", "js"));
        Assert.Equal(RouteStatus.Gold, result);
    }

    [Fact]
    public async Task Failure_Status_Is_Handled()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        var core = new OrchestratorEngine(new QueueStore(Path.Combine(temp, "snap.jsonl")), new JsonlLog(temp),
            new RuleEngine(new RulesConfig([], [], [], 30)), new FailingBrowser(), new PassSyntax(), new RetryPolicy(1, 0));
        core.Start();
        var result = await core.ProcessAsync(new PromptJob("2", "p"));
        Assert.Equal(RouteStatus.Crap, result);
    }

    [Fact]
    public void Wrapper_Ipc_Roundtrip()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        var b = new FileIpcBridge(temp);
        File.WriteAllText(b.StatusFile, "{\"activeProvider\":\"a\",\"activeModel\":\"m\",\"ready\":true}");
        Assert.Equal("a", b.ReadStatus().ActiveProvider);
        b.RequestSwitch(new("limit", "a", DateTimeOffset.UtcNow));
        Assert.True(File.Exists(b.RequestsFile));
    }

    private sealed class PassSyntax : ISyntaxGate { public SyntaxGateResult Validate(string content, string? language) => new(true, "ok"); }
    private sealed class FakeBrowser(string[] outputs) : IBrowserAdapter
    {
        private int _i;
        public Task<(BrowserJobStatus Status, string Response)> RunRoleAsync(string role, string payload, CancellationToken ct)
            => Task.FromResult((BrowserJobStatus.Healthy, outputs[_i++]));
    }
    private sealed class FailingBrowser : IBrowserAdapter
    {
        public Task<(BrowserJobStatus Status, string Response)> RunRoleAsync(string role, string payload, CancellationToken ct)
            => Task.FromResult((BrowserJobStatus.SelectorFailed, ""));
    }
}
