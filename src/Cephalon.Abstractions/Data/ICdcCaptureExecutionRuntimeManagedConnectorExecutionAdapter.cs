namespace Cephalon.Abstractions.Data;

/// <summary>
/// Translates one shared managed-connector command request into a provider-facing execution shape.
/// </summary>
public interface ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter
{
    /// <summary>
    /// Gets the stable provider execution-adapter identifier exposed on shared runtime surfaces.
    /// </summary>
    string AdapterId { get; }

    /// <summary>
    /// Gets a value indicating whether the adapter can currently handle the supplied execution runtime.
    /// </summary>
    /// <param name="runtime">The execution runtime being evaluated.</param>
    /// <returns><see langword="true" /> when the adapter can translate commands for the runtime; otherwise, <see langword="false" />.</returns>
    bool CanHandle(CdcCaptureExecutionRuntimeDescriptor runtime);

    /// <summary>
    /// Translates one shared managed-connector command request into a provider-facing command shape.
    /// </summary>
    /// <param name="runtime">The execution runtime that owns the managed connector.</param>
    /// <param name="operationId">The stable managed-connector operation identifier to translate.</param>
    /// <param name="request">Optional operator intent supplied with the execution request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The typed command-execution result describing how the provider adapter handled the request.</returns>
    ValueTask<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> ExecuteAsync(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request = null,
        CancellationToken cancellationToken = default);
}
