using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenDork.Abstractions;
using OpenDork.Artifacts;
using OpenDork.Core;
using OpenDork.Providers;
using OpenDork.Rules;
using OpenDork.State;
using OpenDork.Validation;
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
    public async Task Provider_Failover_And_Cooldown_Work()
    {
        var cooldowns = new ProviderCooldownStore();
        var router = new ProviderRouter(cooldowns, new ProviderHealthTracker(), new ProviderReputationTracker());
        var result = await router.RouteWithFailoverAsync([new AlwaysFailProvider("p1"), new LocalFallbackClient("local")], "hello", 1);
        Assert.True(result.Success);
        Assert.Equal("local", result.ProviderName);
        Assert.True(cooldowns.IsCoolingDown("p1"));
    }

    [Fact]
    public async Task Validation_Pipeline_Composes_Validators()
    {
        var pipeline = new ValidationPipeline([new LengthValidator(), new StatusTagValidator()]);
        var candidate = new Candidate("c1", "r1", "this is long enough [STATUS: GOLD]", CandidateState.Raw, 0, DateTimeOffset.UtcNow);
        var result = await pipeline.ExecuteAsync(candidate);
        Assert.True(result.Passed);
        Assert.True(result.Score >= 5);
    }

    [Fact]
    public async Task Orchestrator_Persists_State_And_Artifacts()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var run = new RunContext("run1", "job1", "hello [STATUS: GOLD]", "interactive", DateTimeOffset.UtcNow);

        var orchestrator = new RunOrchestrator(
            new ProviderRouter(new ProviderCooldownStore(), new ProviderHealthTracker(), new ProviderReputationTracker()),
            new ProviderChainResolver(),
            new ValidationPipeline([new LengthValidator(), new StatusTagValidator()]),
            new SqliteStateStore(Path.Combine(temp, "state.db")),
            new ArtifactService(temp),
            [new OpenAiCompatibleClient()]);

        var candidate = await orchestrator.RunAsync(run);
        Assert.Contains(candidate.State, [CandidateState.Validated, CandidateState.Gold]);
        Assert.True(File.Exists(Path.Combine(temp, "state.db")));
        Assert.True(Directory.EnumerateFiles(Path.Combine(temp, "results", "raw")).Any());
    }

    [Fact]
    public void Wrapper_Ipc_Roundtrip_Transitional()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        var b = new FileIpcBridge(temp);
        File.WriteAllText(b.StatusFile, "{\"activeProvider\":\"a\",\"activeModel\":\"m\",\"ready\":true}");
        Assert.Equal("a", b.ReadStatus().ActiveProvider);
        b.RequestSwitch(new("limit", "a", DateTimeOffset.UtcNow));
        Assert.True(File.Exists(b.RequestsFile));
    }

    private sealed class AlwaysFailProvider(string name) : IProviderClient
    {
        public string Name => name;
        public Task<ProviderResponse> GenerateAsync(string prompt, CancellationToken ct = default)
            => Task.FromResult(new ProviderResponse(name, false, string.Empty, "fail"));
    }
}
