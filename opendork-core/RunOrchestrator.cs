using OpenDork.Abstractions;
using OpenDork.Artifacts;
using OpenDork.Providers;
using OpenDork.State;
using OpenDork.Validation;

namespace OpenDork.Core;

public sealed class RunOrchestrator
{
    private readonly ProviderRouter _providerRouter;
    private readonly ProviderChainResolver _chainResolver;
    private readonly ValidationPipeline _pipeline;
    private readonly SqliteStateStore _state;
    private readonly ArtifactService _artifacts;
    private readonly IReadOnlyList<IProviderClient> _providers;

    public RunOrchestrator(
        ProviderRouter providerRouter,
        ProviderChainResolver chainResolver,
        ValidationPipeline pipeline,
        SqliteStateStore state,
        ArtifactService artifacts,
        IEnumerable<IProviderClient> providers)
    {
        _providerRouter = providerRouter;
        _chainResolver = chainResolver;
        _pipeline = pipeline;
        _state = state;
        _artifacts = artifacts;
        _providers = providers.ToList();
    }

    public async Task<Candidate> RunAsync(RunContext context, CancellationToken ct = default)
    {
        _state.Initialize();
        _state.UpsertRun(context);
        _state.InsertEvent(new EventRecord(Guid.NewGuid().ToString("N"), context.RunId, "run_start", "run started", DateTimeOffset.UtcNow));

        var chain = _chainResolver.Resolve(_providers, context.RuntimeProfile);
        var response = await _providerRouter.RouteWithFailoverAsync(chain, context.Prompt, 2, ct);
        _state.InsertAttempt(new ProviderAttempt(Guid.NewGuid().ToString("N"), context.RunId, response.ProviderName, response.Success, response.Message, DateTimeOffset.UtcNow));

        var candidate = new Candidate(Guid.NewGuid().ToString("N"), context.RunId, response.Content, CandidateState.Raw, 0, DateTimeOffset.UtcNow);
        _state.InsertCandidate(candidate);
        _artifacts.ExportCandidate(candidate);

        var (passed, score, results) = await _pipeline.ExecuteAsync(candidate, ct);
        foreach (var result in results) _state.InsertValidation(result);

        var final = candidate with
        {
            Score = score,
            State = passed ? (score >= 5 ? CandidateState.Gold : CandidateState.Validated) : CandidateState.Rejected
        };

        _state.InsertCandidate(final with { CandidateId = Guid.NewGuid().ToString("N"), CreatedAtUtc = DateTimeOffset.UtcNow });
        _artifacts.ExportCandidate(final);
        _state.InsertEvent(new EventRecord(Guid.NewGuid().ToString("N"), context.RunId, "run_complete", final.State.ToString(), DateTimeOffset.UtcNow));

        return final;
    }
}
