using Cephalon.Behaviors.Patterns.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Patterns.Strategies;

/// <summary>
/// Executes behaviors that follow the event-driven (fire-and-forget) pattern.
/// Returns HTTP 202 Accepted immediately while the behavior runs in the background.
/// </summary>
public sealed class EventDrivenExecutionStrategy : IBehaviorExecutionStrategy
{
    private static readonly Action<ILogger, string, Exception?> LogBackgroundFault =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(EventDrivenExecutionStrategy)),
            "EventDriven background fault for {BehaviorId}");

    private readonly ILogger<EventDrivenExecutionStrategy> _logger;

    /// <summary>Initializes a new instance of <see cref="EventDrivenExecutionStrategy"/>.</summary>
    /// <param name="logger">The logger used to report background faults.</param>
    public EventDrivenExecutionStrategy(ILogger<EventDrivenExecutionStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>Gets the pattern identifier handled by this strategy.</summary>
    public string Pattern => "event-driven";

    /// <summary>
    /// Dispatches the behavior invocation on a background thread and immediately returns 202 Accepted.
    /// Any exception thrown synchronously before the background task is launched propagates to the caller.
    /// Background faults are caught and logged; they do not surface to the caller.
    /// </summary>
    /// <param name="context">The execution context for this invocation.</param>
    /// <param name="ct">A token that cancels the execution.</param>
    /// <returns>A fire-and-forget result with HTTP 202.</returns>
    public Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var behaviorId = context.Descriptor.Id;
        var logger = _logger;

        // Launch background work — capture exceptions so they are logged rather than unobserved.
        _ = Task.Run(async () =>
        {
            try
            {
                await context.Slot
                    .InvokeAsync(context.BehaviorInstance, context.Input, context.BehaviorContext, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogBackgroundFault(logger, behaviorId, ex);
            }
        }, ct);

        return Task.FromResult(new BehaviorExecutionResult
        {
            Output = null,
            HttpStatusCode = 202,
            IsFireAndForget = true
        });
    }
}
