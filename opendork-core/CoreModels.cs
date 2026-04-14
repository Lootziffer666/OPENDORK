using OpenDork.Rules;

namespace OpenDork.Core;

public enum WorkflowState { Idle, Running, Paused, Stopped }
public enum BrowserJobStatus { Healthy, WaitingForResponse, RateLimited, NeedsLogin, SelectorFailed, CaptureFailed, ManualAttentionRequired }
public enum JobStep { Generator, SyntaxGate, Reviewer, Routed }

public record PromptJob(string Id, string Prompt, string? Language = null, int Attempts = 0);
public record RetryPolicy(int MaxAttempts, int BaseSeconds);
public record SyntaxGateResult(bool IsValid, string Evidence);
public record IterationEvent(DateTimeOffset TimestampUtc, string JobId, JobStep Step, string Message);
public record JobSnapshot(string JobId, WorkflowState State, JobStep Step, int Attempts, RouteStatus Route);
