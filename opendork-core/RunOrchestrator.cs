using OpenDork.Abstractions;
using OpenDork.Artifacts;
using OpenDork.Providers;
using OpenDork.State;
using OpenDork.Validation;

namespace OpenDork.Core;

public sealed class RunOrchestrator
{
    private readonly LiteLlmStyleGateway _gateway;
    private readonly ValidationPipeline _pipeline;
    private readonly SqliteStateStore _state;
    private readonly ArtifactService _artifacts;

    public RunOrchestrator(
        LiteLlmStyleGateway gateway,
        ValidationPipeline pipeline,
        SqliteStateStore state,
        ArtifactService artifacts)
    {
        _gateway = gateway;
        _pipeline = pipeline;
        _state = state;
        _artifacts = artifacts;
    }

    public async Task<Candidate> RunAsync(RunContext context, string modelName = "gpt-4o", CancellationToken ct = default)
    {
        _state.Initialize();
        _state.UpsertRun(context);
        _state.InsertEvent(new EventRecord(Guid.NewGuid().ToString("N"), context.RunId, "run_start", "run started", DateTimeOffset.UtcNow));

        var gatewayResult = await _gateway.CompleteAsync(modelName, context.Prompt, context.RuntimeProfile, ct);
        _state.InsertAttempt(new ProviderAttempt(Guid.NewGuid().ToString("N"), context.RunId, gatewayResult.Response.ProviderName, gatewayResult.Response.Success, gatewayResult.Response.Message, DateTimeOffset.UtcNow));
        _state.InsertSpend(new SpendRecord(
            Guid.NewGuid().ToString("N"),
            context.RunId,
            gatewayResult.Model,
            gatewayResult.Response.ProviderName,
            gatewayResult.Usage.EstimatedCost,
            gatewayResult.Usage.PromptTokens,
            gatewayResult.Usage.CompletionTokens,
            gatewayResult.Usage.CacheHit,
            DateTimeOffset.UtcNow));

        var candidate = new Candidate(Guid.NewGuid().ToString("N"), context.RunId, gatewayResult.Response.Content, CandidateState.Raw, 0, DateTimeOffset.UtcNow);
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
