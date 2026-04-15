using OpenDork.Abstractions;
using OpenDork.Artifacts;
using OpenDork.Core;
using OpenDork.Providers;
using OpenDork.State;
using OpenDork.Validation;

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
var dbPath = Path.Combine(Environment.CurrentDirectory, "opendork.db");
var resultsRoot = Environment.CurrentDirectory;
var catalogPath = Path.Combine(Environment.CurrentDirectory, "config", "providers.json");

var state = new SqliteStateStore(dbPath);
state.Initialize();

var gateway = BuildGateway(catalogPath);

switch (command)
{
    case "run":
        var prompt = args.Skip(1).FirstOrDefault() ?? "default prompt [STATUS: GOLD]";
        var profile = args.Skip(2).FirstOrDefault() ?? "interactive";
        var model = args.Skip(3).FirstOrDefault() ?? "gpt-4o";

        var artifacts = new ArtifactService(resultsRoot);
        var validators = new List<IValidator> { new LengthValidator(), new StatusTagValidator() };
        var pipeline = new ValidationPipeline(validators);
        var orchestrator = new RunOrchestrator(gateway, pipeline, state, artifacts);

        var run = new RunContext(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), prompt, profile, DateTimeOffset.UtcNow);
        var candidate = await orchestrator.RunAsync(run, model);
        Console.WriteLine($"run={run.RunId} state={candidate.State} score={candidate.Score} model={model}");
        break;

    case "status":
        var (runs, candidates, spend) = state.GetDashboard();
        Console.WriteLine($"runs={runs} candidates={candidates} spend_usd={spend:F4} budget={gateway.CurrentSpend():F4}/{gateway.MaxBudget:F2}");
        break;

    case "jobs":
        Console.WriteLine("jobs: use status + replay for per-run analysis (SQLite-backed)");
        break;

    case "replay":
        Console.WriteLine("replay: query runs/candidates/events/spend_logs and render under results/replays");
        break;

    case "report":
        var reportArtifacts = new ArtifactService(resultsRoot);
        var dashboard = state.GetDashboard();
        var report = reportArtifacts.ExportReport("latest", $"# OPENDORK Report\n\nRuns: {dashboard.Runs}\nCandidates: {dashboard.Candidates}\nSpend USD: {dashboard.SpendUsd:F4}\n");
        Console.WriteLine($"report={report}");
        break;

    case "models":
        HandleModels(args.Skip(1).ToArray(), catalogPath);
        break;

    case "chat":
        var chatModel = args.Skip(1).FirstOrDefault() ?? "gpt-4o";
        var chatPrompt = args.Skip(2).FirstOrDefault() ?? "hello";
        var chatResult = await gateway.CompleteAsync(chatModel, chatPrompt, "interactive");
        Console.WriteLine($"ok={chatResult.Response.Success} provider={chatResult.Response.ProviderName} cost={chatResult.Usage.EstimatedCost:F6} cache={chatResult.Usage.CacheHit} content={chatResult.Response.Content}");
        break;

    default:
        Console.WriteLine("opendork-cli commands: run | status | jobs | replay | report | models | chat");
        Console.WriteLine("models subcommands: list | info <model> | add <model> <provider> <inCost> <outCost> | remove <model>");
        break;
}

static LiteLlmStyleGateway BuildGateway(string catalogPath)
{
    var catalog = ProviderModelCatalog.LoadFromJson(catalogPath);
    if (!catalog.List().Any())
    {
        catalog.Upsert(new ProviderModelDefinition("gpt-4o", "openai-compatible", 0.005m, 0.015m));
        catalog.Upsert(new ProviderModelDefinition("gpt-4o-mini", "local-fallback", 0.000m, 0.000m));
        catalog.SaveToJson(catalogPath);
    }

    return new LiteLlmStyleGateway(
        catalog,
        new ProviderRouter(new ProviderCooldownStore(), new ProviderHealthTracker(), new ProviderReputationTracker()),
        new ProviderChainResolver(),
        [new OpenAiCompatibleClient(), new LocalFallbackClient()],
        new BudgetGuard(5.0m, TimeSpan.FromDays(1)),
        new ResponseCache(TimeSpan.FromHours(24)));
}

static void HandleModels(string[] args, string catalogPath)
{
    var action = args.FirstOrDefault()?.ToLowerInvariant() ?? "list";
    var catalog = ProviderModelCatalog.LoadFromJson(catalogPath);

    switch (action)
    {
        case "list":
            foreach (var model in catalog.List())
                Console.WriteLine($"{model.ModelName} -> {model.ProviderClient} in={model.InputCostPer1K} out={model.OutputCostPer1K} enabled={model.Enabled}");
            break;

        case "info":
            var infoModel = args.Skip(1).FirstOrDefault() ?? string.Empty;
            var info = catalog.Get(infoModel);
            Console.WriteLine(info is null ? "model-not-found" : $"{info.ModelName} provider={info.ProviderClient} inputCost={info.InputCostPer1K} outputCost={info.OutputCostPer1K}");
            break;

        case "add":
            var name = args.Skip(1).FirstOrDefault() ?? throw new InvalidOperationException("missing model name");
            var provider = args.Skip(2).FirstOrDefault() ?? "openai-compatible";
            var inCost = decimal.Parse(args.Skip(3).FirstOrDefault() ?? "0.001");
            var outCost = decimal.Parse(args.Skip(4).FirstOrDefault() ?? "0.001");
            catalog.Upsert(new ProviderModelDefinition(name, provider, inCost, outCost));
            catalog.SaveToJson(catalogPath);
            Console.WriteLine($"model-added={name}");
            break;

        case "remove":
            var remove = args.Skip(1).FirstOrDefault() ?? throw new InvalidOperationException("missing model name");
            Console.WriteLine(catalog.Remove(remove) ? $"model-removed={remove}" : "model-not-found");
            catalog.SaveToJson(catalogPath);
            break;

        default:
            Console.WriteLine("unknown models subcommand");
            break;
    }
}
