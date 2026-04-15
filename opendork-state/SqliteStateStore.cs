using Microsoft.Data.Sqlite;
using OpenDork.Abstractions;

namespace OpenDork.State;

public sealed class SqliteStateStore
{
    private readonly string _connectionString;
    public SqliteStateStore(string dbPath) => _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

    public void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "migrations", "001_init.sql"));
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void UpsertRun(RunContext run)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO runs(id,job_id,prompt,runtime_profile,started_at_utc) VALUES($id,$job,$prompt,$profile,$started) ON CONFLICT(id) DO UPDATE SET runtime_profile=$profile";
        cmd.Parameters.AddWithValue("$id", run.RunId);
        cmd.Parameters.AddWithValue("$job", run.JobId);
        cmd.Parameters.AddWithValue("$prompt", run.Prompt);
        cmd.Parameters.AddWithValue("$profile", run.RuntimeProfile);
        cmd.Parameters.AddWithValue("$started", run.StartedAtUtc.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void InsertCandidate(Candidate candidate)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO candidates(id,run_id,content,state,score,created_at_utc) VALUES($id,$run,$content,$state,$score,$created)";
        cmd.Parameters.AddWithValue("$id", candidate.CandidateId);
        cmd.Parameters.AddWithValue("$run", candidate.RunId);
        cmd.Parameters.AddWithValue("$content", candidate.Content);
        cmd.Parameters.AddWithValue("$state", candidate.State.ToString().ToLowerInvariant());
        cmd.Parameters.AddWithValue("$score", candidate.Score);
        cmd.Parameters.AddWithValue("$created", candidate.CreatedAtUtc.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void InsertAttempt(ProviderAttempt attempt)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO provider_attempts(id,run_id,provider_name,success,message,attempted_at_utc) VALUES($id,$run,$provider,$success,$message,$attempted)";
        cmd.Parameters.AddWithValue("$id", attempt.AttemptId);
        cmd.Parameters.AddWithValue("$run", attempt.RunId);
        cmd.Parameters.AddWithValue("$provider", attempt.ProviderName);
        cmd.Parameters.AddWithValue("$success", attempt.Success ? 1 : 0);
        cmd.Parameters.AddWithValue("$message", attempt.Message);
        cmd.Parameters.AddWithValue("$attempted", attempt.AttemptedAtUtc.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void InsertValidation(ValidationResult result)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO validation_results(id,candidate_id,validator_name,passed,evidence,validated_at_utc) VALUES($id,$candidate,$validator,$passed,$evidence,$validated)";
        cmd.Parameters.AddWithValue("$id", result.ValidationId);
        cmd.Parameters.AddWithValue("$candidate", result.CandidateId);
        cmd.Parameters.AddWithValue("$validator", result.ValidatorName);
        cmd.Parameters.AddWithValue("$passed", result.Passed ? 1 : 0);
        cmd.Parameters.AddWithValue("$evidence", result.Evidence);
        cmd.Parameters.AddWithValue("$validated", result.ValidatedAtUtc.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void InsertEvent(EventRecord evt)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO events(id,run_id,type,message,created_at_utc) VALUES($id,$run,$type,$message,$created)";
        cmd.Parameters.AddWithValue("$id", evt.EventId);
        cmd.Parameters.AddWithValue("$run", evt.RunId);
        cmd.Parameters.AddWithValue("$type", evt.Type);
        cmd.Parameters.AddWithValue("$message", evt.Message);
        cmd.Parameters.AddWithValue("$created", evt.CreatedAtUtc.ToString("O"));
        cmd.ExecuteNonQuery();
    }
}
