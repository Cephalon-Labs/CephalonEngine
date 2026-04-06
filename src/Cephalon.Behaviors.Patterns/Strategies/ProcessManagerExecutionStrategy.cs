using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Patterns.Strategies;

/// <summary>
/// Executes behaviors that follow the process-manager pattern.
/// Loads the process checkpoint before invocation, saves it after a successful step,
/// and deletes it when the process signals completion via <see cref="IProcessCompletion"/>.
/// </summary>
public sealed class ProcessManagerExecutionStrategy : IBehaviorExecutionStrategy
{
    private static readonly Action<ILogger, string, string, Exception?> LogNullOutput =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1, nameof(ProcessManagerExecutionStrategy)),
            "ProcessManagerStrategy: behavior {BehaviorId} returned null output for process {ProcessId}; checkpoint preserved.");

    private readonly IProcessCheckpointStore _store;
    private readonly ILogger<ProcessManagerExecutionStrategy> _logger;

    /// <summary>Initializes a new instance of <see cref="ProcessManagerExecutionStrategy"/>.</summary>
    /// <param name="store">The checkpoint store used to load and persist process checkpoints.</param>
    /// <param name="logger">The logger used to report warnings and errors.</param>
    public ProcessManagerExecutionStrategy(IProcessCheckpointStore store, ILogger<ProcessManagerExecutionStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);
        _store = store;
        _logger = logger;
    }

    /// <summary>Gets the pattern identifier handled by this strategy.</summary>
    public string Pattern => "process-manager";

    /// <summary>
    /// Loads the current process checkpoint, invokes the behavior slot, and saves the resulting checkpoint.
    /// If the output implements <see cref="IProcessCompletion"/>, the checkpoint is deleted to signal completion.
    /// If the output is null, a warning is logged and the checkpoint is preserved.
    /// The process identifier is read from <see cref="Cephalon.Abstractions.Behaviors.IBehaviorContext.CorrelationId"/>;
    /// if not set, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <param name="context">The execution context for this invocation.</param>
    /// <param name="ct">A token that cancels the execution.</param>
    /// <returns>A result with HTTP 200 and the behavior output.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <c>CorrelationId</c> is null.</exception>
    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var processId = context.BehaviorContext.CorrelationId
            ?? throw new InvalidOperationException(
                $"ProcessManagerExecutionStrategy requires a CorrelationId on the behavior context for behavior '{context.Descriptor.Id}'.");

        // Load existing checkpoint before invocation (null if no prior checkpoint).
        var existingCheckpoint = await _store.GetAsync(processId, ct).ConfigureAwait(false);
        _ = existingCheckpoint;

        // Invoke the behavior — exceptions propagate raw.
        var output = await context.Slot
            .InvokeAsync(context.BehaviorInstance, context.Input, context.BehaviorContext, ct)
            .ConfigureAwait(false);

        // Determine post-invocation checkpoint action.
        if (output is IProcessCompletion)
        {
            // Process has signalled completion — remove the checkpoint.
            await _store.DeleteAsync(processId, ct).ConfigureAwait(false);
        }
        else if (output is ProcessCheckpoint checkpoint)
        {
            await _store.SaveAsync(processId, checkpoint, ct).ConfigureAwait(false);
        }
        else if (output is null)
        {
            // Null output is NOT treated as completion — log a warning and preserve the checkpoint.
            LogNullOutput(_logger, context.Descriptor.Id, processId, null);
        }

        return new BehaviorExecutionResult
        {
            Output = output,
            HttpStatusCode = 200,
            IsFireAndForget = false
        };
    }
}
