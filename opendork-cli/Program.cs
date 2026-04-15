using OpenDork.Abstractions;
using OpenDork.Artifacts;
using OpenDork.Core;
using OpenDork.Providers;
using OpenDork.State;
using OpenDork.Validation;

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
var dbPath = Path.Combine(Environment.CurrentDirectory, "opendork.db");
var resultsRoot = Environment.CurrentDirectory;

switch (command)
{
    case "run":
        var prompt = args.Skip(1).FirstOrDefault() ?? "default prompt";
        var profile = args.Skip(2).FirstOrDefault() ?? "interactive";

        var state = new SqliteStateStore(dbPath);
        var artifacts = new ArtifactService(resultsRoot);
        var router = new ProviderRouter(new ProviderCooldownStore(), new ProviderHealthTracker(), new ProviderReputationTracker());
        var providers = new List<IProviderClient> { new OpenAiCompatibleClient(), new LocalFallbackClient() };
        var resolver = new ProviderChainResolver();
        var validators = new List<IValidator> { new LengthValidator(), new StatusTagValidator() };
        var pipeline = new ValidationPipeline(validators);
        var orchestrator = new RunOrchestrator(router, resolver, pipeline, state, artifacts, providers);

        var run = new RunContext(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), prompt, profile, DateTimeOffset.UtcNow);
        var candidate = await orchestrator.RunAsync(run);
        Console.WriteLine($"run={run.RunId} state={candidate.State} score={candidate.Score}");
        break;

    case "status":
        Console.WriteLine("status: use sqlite db + reports under results/reports");
        break;
    case "jobs":
        Console.WriteLine("jobs: query runs table for active/recent jobs");
        break;
    case "replay":
        Console.WriteLine("replay: read events + candidates from sqlite and results/replays");
        break;
    case "report":
        Console.WriteLine("report: render markdown summaries from state and artifacts");
        break;
    default:
        Console.WriteLine("opendork-cli commands: run | status | jobs | replay | report");
        break;
}
