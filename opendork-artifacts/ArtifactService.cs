using OpenDork.Abstractions;

namespace OpenDork.Artifacts;

public sealed class ArtifactService
{
    private readonly string _root;
    public ArtifactService(string root)
    {
        _root = root;
        foreach (var directory in new[] { "results/raw", "results/validated", "results/rejected", "results/gold", "results/diffs", "results/reports", "results/replays" })
            Directory.CreateDirectory(Path.Combine(_root, directory));
    }

    public ArtifactRecord ExportCandidate(Candidate candidate)
    {
        var folder = candidate.State switch
        {
            CandidateState.Raw => "raw",
            CandidateState.Validated => "validated",
            CandidateState.Rejected => "rejected",
            CandidateState.Gold => "gold",
            _ => "raw"
        };

        var relative = Path.Combine("results", folder, $"{candidate.CandidateId}.md");
        File.WriteAllText(Path.Combine(_root, relative), candidate.Content);
        return new ArtifactRecord(Guid.NewGuid().ToString("N"), candidate.RunId, folder, relative, DateTimeOffset.UtcNow);
    }

    public string ExportDiff(Candidate from, Candidate to)
    {
        var diff = $"--- {from.CandidateId}\n+++ {to.CandidateId}\n- {from.Content}\n+ {to.Content}\n";
        var relative = Path.Combine("results", "diffs", $"{from.CandidateId}_to_{to.CandidateId}.diff");
        File.WriteAllText(Path.Combine(_root, relative), diff);
        return relative;
    }

    public string ExportReport(string runId, string body)
    {
        var relative = Path.Combine("results", "reports", $"{runId}.md");
        File.WriteAllText(Path.Combine(_root, relative), body);
        return relative;
    }
}
