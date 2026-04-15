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
    public async Task Gateway_Provides_Failover_Budget_And_Cache()
    {
        var catalog = new ProviderModelCatalog([new ProviderModelDefinition("gpt-4o", "broken", 0.01m, 0.01m)]);
        var gateway = new LiteLlmStyleGateway(
            catalog,
            new ProviderRouter(new ProviderCooldownStore(), new ProviderHealthTracker(), new ProviderReputationTracker()),
            new ProviderChainResolver(),
            [new AlwaysFailProvider("broken"), new LocalFallbackClient("local-fallback")],
            new BudgetGuard(10m, TimeSpan.FromDays(1)),
            new ResponseCache(TimeSpan.FromMinutes(10)));

        var first = await gateway.CompleteAsync("gpt-4o", "hello [STATUS: GOLD]", "interactive");
        var second = await gateway.CompleteAsync("gpt-4o", "hello [STATUS: GOLD]", "interactive");

        Assert.True(first.Response.Success);
        Assert.Equal("local-fallback", first.Response.ProviderName);
        Assert.True(second.Usage.CacheHit);
        Assert.Equal(0m, second.Usage.EstimatedCost);
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
    public async Task Orchestrator_Persists_State_Artifacts_And_Spend()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var run = new RunContext("run1", "job1", "hello [STATUS: GOLD]", "interactive", DateTimeOffset.UtcNow);

        var gateway = new LiteLlmStyleGateway(
            new ProviderModelCatalog([new ProviderModelDefinition("gpt-4o", "openai-compatible", 0.01m, 0.01m)]),
            new ProviderRouter(new ProviderCooldownStore(), new ProviderHealthTracker(), new ProviderReputationTracker()),
            new ProviderChainResolver(),
            [new OpenAiCompatibleClient()],
            new BudgetGuard(10m, TimeSpan.FromDays(1)),
            new ResponseCache(TimeSpan.FromHours(1)));

        var store = new SqliteStateStore(Path.Combine(temp, "state.db"));
        var orchestrator = new RunOrchestrator(gateway, new ValidationPipeline([new LengthValidator(), new StatusTagValidator()]), store, new ArtifactService(temp));

        var candidate = await orchestrator.RunAsync(run, "gpt-4o");
        Assert.Contains(candidate.State, [CandidateState.Validated, CandidateState.Gold]);
        Assert.True(File.Exists(Path.Combine(temp, "state.db")));
        Assert.True(Directory.EnumerateFiles(Path.Combine(temp, "results", "raw")).Any());

        var dashboard = store.GetDashboard();
        Assert.True(dashboard.SpendUsd >= 0);
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
