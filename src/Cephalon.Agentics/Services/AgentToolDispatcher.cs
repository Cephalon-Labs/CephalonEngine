using Cephalon.Abstractions.Agentics;
using Cephalon.Agentics.Configuration;
using Cephalon.Diagnostics.Redaction;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace Cephalon.Agentics.Services;

internal sealed class AgentToolDispatcher(
    IAgentToolCatalog catalog,
    AgenticRuntimeOptions options,
    IAgentToolRunCatalog runCatalog,
    IEnumerable<IAgentToolExecutor> executors,
    IEnumerable<IAgentToolExecutionPolicy> policies,
    IAgentToolRunReporter reporter,
    IEnumerable<IAgentToolExecutionObserver> observers,
    RedactionPipeline? redactionPipeline = null) : IAgentToolDispatcher
{
    private readonly IAgentToolExecutor[] executors = executors.ToArray();
    private readonly IAgentToolExecutionPolicy[] policies = policies.ToArray();
    private readonly IAgentToolExecutionObserver[] observers = observers.ToArray();

    public async ValueTask<AgentToolExecutionResult> ExecuteAsync(
        AgentToolExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var dispatchActivity = AgenticsDiagnostics.ActivitySource.StartActivity(
            AgenticsDiagnostics.ToolDispatchActivityName,
            ActivityKind.Internal);
        SetTag(dispatchActivity, AgenticsDiagnostics.DispatcherIdTag, InProcessAgenticsRuntimeIds.DispatcherId);
        SetTag(dispatchActivity, AgenticsDiagnostics.ToolIdTag, request.ToolId);
        SetTag(dispatchActivity, AgenticsDiagnostics.RunIdTag, request.RunId);
        if (!string.IsNullOrEmpty(request.ActorId))
        {
            SetTag(dispatchActivity, AgenticsDiagnostics.ActorIdTag, request.ActorId);
        }
        if (!string.IsNullOrEmpty(request.CorrelationId))
        {
            SetTag(dispatchActivity, AgenticsDiagnostics.CorrelationIdTag, request.CorrelationId);
        }
        SetTag(dispatchActivity, AgenticsDiagnostics.AttemptTag, request.Attempt);

        if (!catalog.TryGet(request.ToolId, out var tool))
        {
            var missingError = $"Agent tool '{request.ToolId}' is not registered in the active agentics runtime.";
            CompleteDispatchActivity(
                dispatchActivity,
                request.ToolId,
                AgentToolExecutionOutcomes.Failed,
                error: missingError);
            throw new InvalidOperationException(missingError);
        }

        if (TryResolveDuplicateCompletedRun(
            tool.Id,
            request.RunId,
            out var completedRun,
            out var completedObservedAtUtc,
            out var idempotencyRetention))
        {
            var duplicateContext = new AgentToolExecutionContext(
                tool,
                request.RunId,
                request.Arguments,
                request.ActorId,
                request.CorrelationId,
                request.Attempt,
                request.Metadata);
            var duplicateResult = WithRequestMetadata(
                duplicateContext,
                AgentToolExecutionResult.Skipped(
                    "Agent-tool run already completed in this process.",
                    CreateIdempotencyMetadata(
                        completedRun!,
                        completedObservedAtUtc,
                        idempotencyRetention)));

            await RecordResultAsync(
                duplicateContext,
                duplicateResult,
                cancellationToken).ConfigureAwait(false);
            CompleteDispatchActivity(
                dispatchActivity,
                tool.Id,
                duplicateResult.Outcome);
            return duplicateResult;
        }

        var executor = ResolveExecutor(tool.Id);
        if (executor is null)
        {
            var context = new AgentToolExecutionContext(
                tool,
                request.RunId,
                request.Arguments,
                request.ActorId,
                request.CorrelationId,
                request.Attempt,
                request.Metadata);
            var error = $"No agent tool executor is registered for tool '{tool.Id}'.";
            await RecordAsync(
                CreateReport(context, AgentToolExecutionOutcomes.Started),
                cancellationToken).ConfigureAwait(false);
            await RecordResultAsync(
                context,
                AgentToolExecutionResult.Failed(error),
                cancellationToken).ConfigureAwait(false);
            CompleteDispatchActivity(
                dispatchActivity,
                tool.Id,
                AgentToolExecutionOutcomes.Failed,
                error: error);
            throw new InvalidOperationException(error);
        }

        var maxAttempts = NormalizeMaxAttempts(options.ExecutionMaxAttempts);
        var retryDelay = NormalizeRetryDelay(options.ExecutionRetryDelayMilliseconds);
        Exception? lastException = null;

        for (var attemptOffset = 0; attemptOffset < maxAttempts; attemptOffset++)
        {
            var attempt = request.Attempt + attemptOffset;
            var context = new AgentToolExecutionContext(
                tool,
                request.RunId,
                request.Arguments,
                request.ActorId,
                request.CorrelationId,
                attempt,
                request.Metadata);

            await RecordAsync(
                CreateReport(context, AgentToolExecutionOutcomes.Started),
                cancellationToken).ConfigureAwait(false);

            foreach (var policy in policies)
            {
                var decision = await policy.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
                switch (decision.Kind)
                {
                    case AgentToolExecutionDecisionKinds.Allow:
                        continue;

                    case AgentToolExecutionDecisionKinds.ApprovalRequired:
                        var approvalRequired = WithRequestMetadata(
                            context,
                            AgentToolExecutionResult.ApprovalRequired(
                                decision.Reason ?? "Agent-tool execution requires approval.",
                                decision.Metadata));
                        await RecordResultAsync(context, approvalRequired, cancellationToken).ConfigureAwait(false);
                        CompleteDispatchActivity(
                            dispatchActivity,
                            tool.Id,
                            approvalRequired.Outcome);
                        return approvalRequired;

                    case AgentToolExecutionDecisionKinds.Deny:
                        var denied = WithRequestMetadata(
                            context,
                            AgentToolExecutionResult.Denied(
                                decision.Reason ?? "Agent-tool execution was denied by policy.",
                                decision.Metadata));
                        await RecordResultAsync(context, denied, cancellationToken).ConfigureAwait(false);
                        CompleteDispatchActivity(
                            dispatchActivity,
                            tool.Id,
                            denied.Outcome,
                            error: denied.Error);
                        return denied;

                    default:
                        throw new InvalidOperationException(
                            $"Agent-tool execution decision '{decision.Kind}' is not supported by the active agentics runtime.");
                }
            }

            try
            {
                var result = await executor.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                if (result is null)
                {
                    throw new InvalidOperationException(
                        $"Agent tool executor for tool '{tool.Id}' returned a null execution result.");
                }

                var mergedResult = WithRequestMetadata(context, result);
                if (ShouldRetry(mergedResult, attemptOffset, maxAttempts))
                {
                    await RecordRetryScheduledAsync(
                        context,
                        mergedResult.Metadata,
                        mergedResult.Error ?? "Agent-tool executor returned a failed result.",
                        maxAttempts,
                        retryDelay,
                        cancellationToken).ConfigureAwait(false);
                    await DelayBeforeRetryAsync(retryDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await RecordResultAsync(context, mergedResult, cancellationToken).ConfigureAwait(false);
                CompleteDispatchActivity(
                    dispatchActivity,
                    tool.Id,
                    mergedResult.Outcome,
                    error: mergedResult.Error);
                return mergedResult;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastException = exception;
                if (attemptOffset < maxAttempts - 1)
                {
                    await RecordRetryScheduledAsync(
                        context,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["exceptionType"] = exception.GetType().Name
                        },
                        exception.Message,
                        maxAttempts,
                        retryDelay,
                        cancellationToken).ConfigureAwait(false);
                    await DelayBeforeRetryAsync(retryDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await RecordResultAsync(
                    context,
                    AgentToolExecutionResult.Failed(
                        exception.Message,
                        CreateRetryMetadata(
                            context,
                            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["exceptionType"] = exception.GetType().Name
                            },
                            maxAttempts,
                            retryDelay,
                            "max-attempts-exhausted")),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        CompleteDispatchActivity(
            dispatchActivity,
            tool.Id,
            AgentToolExecutionOutcomes.Failed,
            error: lastException?.Message);
        throw lastException ?? new InvalidOperationException(
            $"Agent tool '{tool.Id}' did not return an execution result.");
    }

    /// <summary>
    /// Sets the terminal execution-outcome tag, optional error status, and increments the
    /// tool-dispatch counter for the given dispatch activity. Tag values are routed through the
    /// redaction pipeline so consumer-registered redaction filters apply uniformly across the
    /// dispatch span.
    /// </summary>
    private void CompleteDispatchActivity(
        Activity? dispatchActivity,
        string toolId,
        string outcome,
        string? error = null)
    {
        SetTag(dispatchActivity, AgenticsDiagnostics.ExecutionOutcomeTag, outcome);

        if (string.Equals(outcome, AgentToolExecutionOutcomes.Failed, StringComparison.OrdinalIgnoreCase))
        {
            dispatchActivity?.SetStatus(ActivityStatusCode.Error, error);
        }

        AgenticsDiagnostics.ToolDispatchCounter.Add(
            1,
            new TagList
            {
                { AgenticsDiagnostics.ToolIdTag, toolId },
                { AgenticsDiagnostics.ExecutionOutcomeTag, outcome }
            });
    }

    /// <summary>
    /// Sets a tag on <paramref name="activity"/> after routing the value through the consumer
    /// -registered <see cref="RedactionPipeline"/>. The pipeline is empty by default when no
    /// consumer registered any <see cref="IRedactionFilter"/>; in that case (and when DI did not
    /// supply a pipeline at all) this method short-circuits to passthrough so dispatch emission
    /// stays cheap.
    /// </summary>
    private void SetTag(Activity? activity, string attributeKey, object? value)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(attributeKey, Redact(activity, attributeKey, value));
    }

    private object? Redact(Activity? activity, string attributeKey, object? value)
    {
        if (redactionPipeline is null)
        {
            return value;
        }

        var context = new RedactionContext(
            ActivitySourceName: activity?.Source.Name,
            MeterName: null,
            AttributeKey: attributeKey,
            LoggerCategory: null);
        return redactionPipeline.Filter(context, value);
    }

    private IAgentToolExecutor? ResolveExecutor(string toolId)
    {
        var matches = executors
            .Where(executor => string.Equals(executor.ToolId, toolId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matches.Length switch
        {
            0 => null,
            1 => matches[0],
            _ => throw new InvalidOperationException(
                $"Multiple agent tool executors are registered for tool '{toolId}'.")
        };
    }

    private async ValueTask RecordResultAsync(
        AgentToolExecutionContext context,
        AgentToolExecutionResult result,
        CancellationToken cancellationToken)
    {
        await RecordAsync(
            CreateReport(context, result.Outcome, result.OutputSummary, result.Error, result.Metadata),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask RecordAsync(
        AgentToolExecutionReport report,
        CancellationToken cancellationToken)
    {
        await reporter.ReportAsync(report, cancellationToken).ConfigureAwait(false);
        foreach (var observer in observers)
        {
            await observer.ObserveAsync(report, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask RecordRetryScheduledAsync(
        AgentToolExecutionContext context,
        IReadOnlyDictionary<string, string> metadata,
        string error,
        int maxAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken)
    {
        var retryMetadata = CreateRetryMetadata(
            context,
            metadata,
            maxAttempts,
            retryDelay,
            "retry-scheduled");
        await RecordAsync(
            CreateReport(
                context,
                AgentToolExecutionOutcomes.RetryScheduled,
                error: error,
                metadata: retryMetadata),
            cancellationToken).ConfigureAwait(false);
    }

    private static AgentToolExecutionReport CreateReport(
        AgentToolExecutionContext context,
        string outcome,
        string? outputSummary = null,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionReport(
            toolId: context.Tool.Id,
            runId: context.RunId,
            outcome: outcome,
            observedAtUtc: DateTimeOffset.UtcNow,
            actorId: context.ActorId,
            correlationId: context.CorrelationId,
            attempt: context.Attempt,
            outputSummary: outputSummary,
            error: error,
            metadata: metadata);
    }

    private static bool ShouldRetry(
        AgentToolExecutionResult result,
        int attemptOffset,
        int maxAttempts)
    {
        return attemptOffset < maxAttempts - 1 &&
            string.Equals(result.Outcome, AgentToolExecutionOutcomes.Failed, StringComparison.OrdinalIgnoreCase);
    }

    private static int NormalizeMaxAttempts(int maxAttempts) => Math.Max(1, maxAttempts);

    private static TimeSpan NormalizeRetryDelay(int retryDelayMilliseconds) =>
        TimeSpan.FromMilliseconds(Math.Max(0, retryDelayMilliseconds));

    private static TimeSpan NormalizeIdempotencyRetention(int retentionMinutes) =>
        TimeSpan.FromMinutes(Math.Max(1, retentionMinutes));

    private static ValueTask DelayBeforeRetryAsync(TimeSpan retryDelay, CancellationToken cancellationToken)
    {
        if (retryDelay <= TimeSpan.Zero)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(Task.Delay(retryDelay, cancellationToken));
    }

    private bool TryResolveDuplicateCompletedRun(
        string toolId,
        string runId,
        out AgentToolRunState? completedRun,
        out DateTimeOffset completedObservedAtUtc,
        out TimeSpan retention)
    {
        completedRun = null;
        completedObservedAtUtc = default;
        retention = NormalizeIdempotencyRetention(options.ExecutionIdempotencyRetentionMinutes);

        if (!options.EnableExecutionIdempotency ||
            !runCatalog.TryGet(runId, out var state) ||
            state is null ||
            state.SucceededCount <= 0 ||
            !string.Equals(state.ToolId, toolId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var completedAt = ResolveCompletedObservedAtUtc(state);
        if (completedAt is null)
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - completedAt.Value > retention)
        {
            return false;
        }

        completedRun = state;
        completedObservedAtUtc = completedAt.Value;
        return true;
    }

    private static DateTimeOffset? ResolveCompletedObservedAtUtc(AgentToolRunState state)
    {
        if (state.Metadata.TryGetValue("completedObservedAtUtc", out var completedObservedAtUtc) &&
            DateTimeOffset.TryParse(
                completedObservedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedCompletedObservedAtUtc))
        {
            return parsedCompletedObservedAtUtc;
        }

        return string.Equals(state.LastOutcome, AgentToolExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase)
            ? state.LastObservedAtUtc
            : null;
    }

    private static Dictionary<string, string> CreateIdempotencyMetadata(
        AgentToolRunState completedRun,
        DateTimeOffset completedObservedAtUtc,
        TimeSpan retention)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["idempotencyPolicy"] = "completed-run",
            ["idempotencyKey"] = "tool-run",
            ["idempotencyRetentionMinutes"] = retention.TotalMinutes.ToString("0", CultureInfo.InvariantCulture),
            ["idempotencyDurability"] = "none",
            ["idempotencyScope"] = "process-local",
            ["idempotencyOutcome"] = "duplicate-skipped",
            ["completedToolId"] = completedRun.ToolId,
            ["completedRunId"] = completedRun.RunId,
            ["completedOutcome"] = AgentToolExecutionOutcomes.Succeeded,
            ["completedObservedAtUtc"] = completedObservedAtUtc.ToString("O", CultureInfo.InvariantCulture)
        };
    }

    private static Dictionary<string, string> CreateRetryMetadata(
        AgentToolExecutionContext context,
        IReadOnlyDictionary<string, string> metadata,
        int maxAttempts,
        TimeSpan retryDelay,
        string retryOutcome)
    {
        var values = MergeMetadata(context.Metadata, metadata);
        var retryMetadata = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase)
        {
            ["retryPolicy"] = maxAttempts > 1 ? "bounded-in-process" : "none",
            ["retryMaxAttempts"] = maxAttempts.ToString(CultureInfo.InvariantCulture),
            ["retryDelayMilliseconds"] = retryDelay.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture),
            ["retryDurability"] = "none",
            ["retryScope"] = maxAttempts > 1 ? "process-local" : "none",
            ["retryOutcome"] = retryOutcome
        };

        if (string.Equals(retryOutcome, "retry-scheduled", StringComparison.OrdinalIgnoreCase))
        {
            retryMetadata["nextAttempt"] = (context.Attempt + 1).ToString(CultureInfo.InvariantCulture);
            if (retryDelay > TimeSpan.Zero)
            {
                retryMetadata["nextRetryAtUtc"] = DateTimeOffset.UtcNow.Add(retryDelay).ToString("O", CultureInfo.InvariantCulture);
            }
        }

        return retryMetadata;
    }

    private static AgentToolExecutionResult WithRequestMetadata(
        AgentToolExecutionContext context,
        AgentToolExecutionResult result)
    {
        var metadata = MergeMetadata(context.Metadata, result.Metadata);
        return new AgentToolExecutionResult(
            result.Outcome,
            result.OutputSummary,
            result.Error,
            metadata);
    }

    private static IReadOnlyDictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string> requestMetadata,
        IReadOnlyDictionary<string, string> resultMetadata)
    {
        if (requestMetadata.Count == 0)
        {
            return resultMetadata;
        }

        if (resultMetadata.Count == 0)
        {
            return requestMetadata;
        }

        var metadata = new Dictionary<string, string>(requestMetadata, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in resultMetadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        return metadata;
    }
}
