namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes optional operator intent supplied when Cephalon evaluates one managed-connector command-execution request.
/// </summary>
public sealed class CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
{
    /// <summary>
    /// Creates a new managed-connector command-execution request.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the caller is intentionally approving an approval-gated provider command.
    /// </summary>
    public bool Approve { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the caller explicitly allows destructive operations such as connector deletion.
    /// </summary>
    public bool AllowDestructive { get; set; }
}
