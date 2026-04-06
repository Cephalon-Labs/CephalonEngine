using Cephalon.Behaviors.Patterns.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Patterns.Strategies;

/// <summary>
/// Executes behaviors that follow the saga-step pattern.
/// Loads saga state before invocation and persists it after a successful execution.
/// On exception, state is NOT saved so that explicit compensation logic can be applied.
/// </summary>
public sealed class SagaExecutionStrategy : IBehaviorExecutionStrategy
{
    private static readonly Action<ILogger, string, string, Exception?> LogNoCorrelationId =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1, nameof(SagaExecutionStrategy)),
            "SagaStrategy: no CorrelationId on context for behavior {BehaviorId}; generated {SagaId}.");

    private static readonly Action<ILogger, string, Exception?> LogSlotFault =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(SagaExecutionStrategy)),
            "SagaStrategy: slot threw for correlationId {CorrelationId}; state not saved.");

    private readonly ISagaStateStore _store;
    private readonly ILogger<SagaExecutionStrategy> _logger;

    /// <summary>Initializes a new instance of <see cref="SagaExecutionStrategy"/>.</summary>
    /// <param name="store">The saga state store used to load and persist saga state.</param>
    /// <param name="logger">The logger used to report warnings and errors.</param>
    public SagaExecutionStrategy(ISagaStateStore store, ILogger<SagaExecutionStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);
        _store = store;
        _logger = logger;
    }

    /// <summary>Gets the pattern identifier handled by this strategy.</summary>
    public string Pattern => "saga-step";

    /// <summary>
    /// Loads saga state, invokes the behavior slot, and saves the updated state on success.
    /// If the behavior throws, state is not persisted so compensation can be applied externally.
    /// The saga identifier is read from <see cref="Cephalon.Abstractions.Behaviors.IBehaviorContext.CorrelationId"/>;
    /// if not set, a new <see cref="Guid"/> is generated and a warning is logged.
    /// </summary>
    /// <param name="context">The execution context for this invocation.</param>
    /// <param name="ct">A token that cancels the execution.</param>
    /// <returns>A result with HTTP 200 and the behavior output.</returns>
    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.BehaviorContext.CorrelationId;
        if (correlationId is null)
        {
            correlationId = Guid.NewGuid().ToString();
            LogNoCorrelationId(_logger, context.Descriptor.Id, correlationId, null);
        }

        // Load existing saga state before invocation (null if no prior state).
        var existingState = await _store.GetAsync<object>(correlationId, ct).ConfigureAwait(false);
        _ = existingState; // state is available to the behavior via context in real implementations

        // Invoke — do NOT catch non-cancellation exceptions silently; on throw state must NOT be saved.
        object? output;
        try
        {
            output = await context.Slot
                .InvokeAsync(context.BehaviorInstance, context.Input, context.BehaviorContext, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogSlotFault(_logger, correlationId, ex);
            throw;
        }

        // Persist updated saga state (use output as new state if not null).
        if (output is not null)
        {
            await _store.SaveAsync(correlationId, output, ct).ConfigureAwait(false);
        }

        return new BehaviorExecutionResult
        {
            Output = output,
            HttpStatusCode = 200,
            IsFireAndForget = false
        };
    }
}
