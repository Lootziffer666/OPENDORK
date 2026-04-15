namespace OpenDork.Abstractions;

public enum CandidateState { Raw, Validated, Rejected, Gold }

public record RunContext(string RunId, string JobId, string Prompt, string RuntimeProfile, DateTimeOffset StartedAtUtc);
public record Candidate(string CandidateId, string RunId, string Content, CandidateState State, int Score, DateTimeOffset CreatedAtUtc);
public record ProviderAttempt(string AttemptId, string RunId, string ProviderName, bool Success, string Message, DateTimeOffset AttemptedAtUtc);
public record ValidationResult(string ValidationId, string CandidateId, string ValidatorName, bool Passed, string Evidence, DateTimeOffset ValidatedAtUtc);
public record EventRecord(string EventId, string RunId, string Type, string Message, DateTimeOffset CreatedAtUtc);
public record ArtifactRecord(string ArtifactId, string RunId, string ArtifactType, string RelativePath, DateTimeOffset CreatedAtUtc);
public record SpendRecord(string SpendId, string RunId, string ModelName, string ProviderName, decimal CostUsd, int PromptTokens, int CompletionTokens, bool CacheHit, DateTimeOffset CreatedAtUtc);

public record ProviderResponse(string ProviderName, bool Success, string Content, string Message);
public record ValidationOutcome(bool Passed, int ScoreDelta, string Evidence);

public interface IProviderClient
{
    string Name { get; }
    Task<ProviderResponse> GenerateAsync(string prompt, CancellationToken ct = default);
}

public interface IValidator
{
    string Name { get; }
    Task<ValidationOutcome> ValidateAsync(Candidate candidate, CancellationToken ct = default);
}
