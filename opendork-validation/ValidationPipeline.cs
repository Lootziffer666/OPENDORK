using OpenDork.Abstractions;

namespace OpenDork.Validation;

public sealed class ValidationPipeline
{
    private readonly IReadOnlyList<IValidator> _validators;
    public ValidationPipeline(IEnumerable<IValidator> validators) => _validators = validators.ToList();

    public async Task<(bool Passed, int Score, List<ValidationResult> Results)> ExecuteAsync(Candidate candidate, CancellationToken ct = default)
    {
        var score = candidate.Score;
        var results = new List<ValidationResult>();
        foreach (var validator in _validators)
        {
            var outcome = await validator.ValidateAsync(candidate, ct);
            score += outcome.ScoreDelta;
            results.Add(new ValidationResult(Guid.NewGuid().ToString("N"), candidate.CandidateId, validator.Name, outcome.Passed, outcome.Evidence, DateTimeOffset.UtcNow));
            if (!outcome.Passed) return (false, score, results);
        }
        return (true, score, results);
    }
}

public sealed class ValidationProfileResolver
{
    private readonly Dictionary<string, string[]> _profiles;
    public ValidationProfileResolver(Dictionary<string, string[]> profiles) => _profiles = profiles;
    public string[] Resolve(string profileName) => _profiles.TryGetValue(profileName, out var validators) ? validators : [];
}

public sealed class LengthValidator : IValidator
{
    public string Name => "length";
    public Task<ValidationOutcome> ValidateAsync(Candidate candidate, CancellationToken ct = default)
        => Task.FromResult(candidate.Content.Length > 10 ? new ValidationOutcome(true, 2, "length-ok") : new ValidationOutcome(false, -10, "too-short"));
}

public sealed class StatusTagValidator : IValidator
{
    public string Name => "status-tag";
    public Task<ValidationOutcome> ValidateAsync(Candidate candidate, CancellationToken ct = default)
        => Task.FromResult(candidate.Content.Contains("[STATUS:", StringComparison.OrdinalIgnoreCase)
            ? new ValidationOutcome(true, 3, "status-tag-found")
            : new ValidationOutcome(false, -5, "status-tag-missing"));
}
