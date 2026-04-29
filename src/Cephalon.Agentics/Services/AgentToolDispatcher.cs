using Cephalon.Abstractions.Agentics;

namespace Cephalon.Agentics.Services;

internal sealed class AgentToolDispatcher(
    IAgentToolCatalog catalog,
    IEnumerable<IAgentToolExecutor> executors,
    IEnumerable<IAgentToolExecutionPolicy> policies,
    IAgentToolRunReporter reporter,
    IEnumerable<IAgentToolExecutionObserver> observers) : IAgentToolDispatcher
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

        if (!catalog.TryGet(request.ToolId, out var tool))
        {
            throw new InvalidOperationException(
                $"Agent tool '{request.ToolId}' is not registered in the active agentics runtime.");
        }

        var context = new AgentToolExecutionContext(
            tool,
            request.RunId,
            request.Arguments,
            request.ActorId,
            request.CorrelationId,
            request.Attempt,
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
                    var approvalRequired = AgentToolExecutionResult.ApprovalRequired(
                        decision.Reason ?? "Agent-tool execution requires approval.",
                        decision.Metadata);
                    await RecordResultAsync(context, approvalRequired, cancellationToken).ConfigureAwait(false);
                    return approvalRequired;

                case AgentToolExecutionDecisionKinds.Deny:
                    var denied = AgentToolExecutionResult.Denied(
                        decision.Reason ?? "Agent-tool execution was denied by policy.",
                        decision.Metadata);
                    await RecordResultAsync(context, denied, cancellationToken).ConfigureAwait(false);
                    return denied;

                default:
                    throw new InvalidOperationException(
                        $"Agent-tool execution decision '{decision.Kind}' is not supported by the active agentics runtime.");
            }
        }

        var executor = ResolveExecutor(tool.Id);
        if (executor is null)
        {
            var error = $"No agent tool executor is registered for tool '{tool.Id}'.";
            await RecordResultAsync(
                context,
                AgentToolExecutionResult.Failed(error),
                cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(error);
        }

        try
        {
            var result = await executor.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                throw new InvalidOperationException(
                    $"Agent tool executor for tool '{tool.Id}' returned a null execution result.");
            }

            await RecordResultAsync(context, result, cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordResultAsync(
                context,
                AgentToolExecutionResult.Failed(exception.Message),
                cancellationToken).ConfigureAwait(false);
            throw;
        }
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
}
