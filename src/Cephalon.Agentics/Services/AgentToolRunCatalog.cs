namespace Cephalon.Agentics.Services;

internal sealed class AgentToolRunCatalog(
    IAgentToolCatalog tools) : IAgentToolRunCatalog, IAgentToolRunReporter
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Lock gate = new();
    private readonly Dictionary<string, AgentToolRunState> runs = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AgentToolRunState> Runs
    {
        get
        {
            lock (gate)
            {
                return runs.Values
                    .OrderBy(static run => run.ToolId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static run => run.RunId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public AgentToolRunState? GetByRunId(string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        lock (gate)
        {
            return runs.GetValueOrDefault(runId.Trim());
        }
    }

    public IReadOnlyList<AgentToolRunState> GetByToolId(string toolId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);
        var normalizedToolId = toolId.Trim();

        lock (gate)
        {
            return runs.Values
                .Where(run => string.Equals(run.ToolId, normalizedToolId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static run => run.LastObservedAtUtc)
                .ThenBy(static run => run.RunId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public bool TryGet(string runId, out AgentToolRunState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        lock (gate)
        {
            return runs.TryGetValue(runId.Trim(), out state);
        }
    }

    public ValueTask ReportAsync(
        AgentToolExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();

        if (!tools.TryGet(report.ToolId, out _))
        {
            throw new InvalidOperationException(
                $"Agent tool '{report.ToolId}' is not registered in the active agentics runtime.");
        }

        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var observedAtUtc = report.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : report.ObservedAtUtc;
        var metadata = report.Metadata.Count == 0
            ? EmptyMetadata
            : new Dictionary<string, string>(report.Metadata, StringComparer.OrdinalIgnoreCase);

        lock (gate)
        {
            var current = runs.TryGetValue(report.RunId, out var existing)
                ? existing
                : new AgentToolRunState(
                    ToolId: report.ToolId,
                    RunId: report.RunId,
                    LastOutcome: null,
                    LastObservedAtUtc: null,
                    LastActorId: null,
                    LastCorrelationId: null,
                    LastAttempt: 0,
                    StartedCount: 0,
                    SucceededCount: 0,
                    FailedCount: 0,
                    SkippedCount: 0,
                    ApprovalRequiredCount: 0,
                    DeniedCount: 0,
                    LastOutputSummary: null,
                    LastError: null,
                    Metadata: EmptyMetadata);

            current = normalizedOutcome switch
            {
                AgentToolExecutionOutcomes.Started => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastActorId = report.ActorId,
                    LastCorrelationId = report.CorrelationId,
                    LastAttempt = report.Attempt,
                    StartedCount = current.StartedCount + 1,
                    LastOutputSummary = report.OutputSummary,
                    LastError = null,
                    Metadata = metadata
                },
                AgentToolExecutionOutcomes.Succeeded => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastActorId = report.ActorId,
                    LastCorrelationId = report.CorrelationId,
                    LastAttempt = report.Attempt,
                    SucceededCount = current.SucceededCount + 1,
                    LastOutputSummary = report.OutputSummary,
                    LastError = null,
                    Metadata = metadata
                },
                AgentToolExecutionOutcomes.Failed => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastActorId = report.ActorId,
                    LastCorrelationId = report.CorrelationId,
                    LastAttempt = report.Attempt,
                    FailedCount = current.FailedCount + 1,
                    LastOutputSummary = report.OutputSummary,
                    LastError = report.Error,
                    Metadata = metadata
                },
                AgentToolExecutionOutcomes.Skipped => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastActorId = report.ActorId,
                    LastCorrelationId = report.CorrelationId,
                    LastAttempt = report.Attempt,
                    SkippedCount = current.SkippedCount + 1,
                    LastOutputSummary = report.OutputSummary,
                    LastError = null,
                    Metadata = metadata
                },
                AgentToolExecutionOutcomes.ApprovalRequired => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastActorId = report.ActorId,
                    LastCorrelationId = report.CorrelationId,
                    LastAttempt = report.Attempt,
                    ApprovalRequiredCount = current.ApprovalRequiredCount + 1,
                    LastOutputSummary = report.OutputSummary,
                    LastError = null,
                    Metadata = metadata
                },
                AgentToolExecutionOutcomes.Denied => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastActorId = report.ActorId,
                    LastCorrelationId = report.CorrelationId,
                    LastAttempt = report.Attempt,
                    DeniedCount = current.DeniedCount + 1,
                    LastOutputSummary = report.OutputSummary,
                    LastError = report.Error,
                    Metadata = metadata
                },
                _ => throw new InvalidOperationException(
                    $"Agent-tool execution outcome '{report.Outcome}' is not supported by the active agentics runtime.")
            };

            runs[report.RunId] = current;
        }

        return ValueTask.CompletedTask;
    }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            AgentToolExecutionOutcomes.Started => AgentToolExecutionOutcomes.Started,
            AgentToolExecutionOutcomes.Succeeded => AgentToolExecutionOutcomes.Succeeded,
            AgentToolExecutionOutcomes.Failed => AgentToolExecutionOutcomes.Failed,
            AgentToolExecutionOutcomes.Skipped => AgentToolExecutionOutcomes.Skipped,
            AgentToolExecutionOutcomes.ApprovalRequired => AgentToolExecutionOutcomes.ApprovalRequired,
            AgentToolExecutionOutcomes.Denied => AgentToolExecutionOutcomes.Denied,
            _ => throw new InvalidOperationException(
                $"Agent-tool execution outcome '{outcome}' is not supported by the active agentics runtime.")
        };
    }
}
