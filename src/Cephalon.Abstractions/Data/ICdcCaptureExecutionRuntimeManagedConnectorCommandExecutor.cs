namespace Cephalon.Abstractions.Data;

/// <summary>
/// Evaluates one shared managed-connector command request against the active provider execution-adapter set.
/// </summary>
public interface ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor
{
    /// <summary>
    /// Evaluates one managed-connector command request for the supplied execution runtime.
    /// </summary>
    /// <param name="executionRuntimeId">The stable execution-runtime identifier that owns the managed connector.</param>
    /// <param name="operationId">The stable managed-connector operation identifier to evaluate.</param>
    /// <param name="request">Optional operator intent supplied with the execution request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The typed command-execution result describing how Cephalon handled the request.</returns>
    ValueTask<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> ExecuteAsync(
        string executionRuntimeId,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request = null,
        CancellationToken cancellationToken = default);
}
