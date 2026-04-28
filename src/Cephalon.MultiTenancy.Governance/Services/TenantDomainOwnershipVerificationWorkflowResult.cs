namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-domain ownership verification workflow transition.
/// </summary>
public sealed class TenantDomainOwnershipVerificationWorkflowResult
{
    /// <summary>
    /// Creates a tenant-domain ownership verification workflow transition result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was targeted.</param>
    /// <param name="domainName">The domain name that was targeted.</param>
    /// <param name="command">The workflow command that was requested.</param>
    /// <param name="outcome">The workflow transition outcome.</param>
    /// <param name="applied">A value indicating whether the workflow transition was applied.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the workflow transition was evaluated.</param>
    /// <param name="previousStatus">The domain ownership status before the workflow transition when one existed.</param>
    /// <param name="currentStatus">The domain ownership status after the workflow transition when one exists.</param>
    /// <param name="domainOwnership">The resulting domain ownership descriptor when one exists.</param>
    /// <param name="reason">The operator-facing transition reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipVerificationWorkflowResult(
        string tenantId,
        string domainName,
        string command,
        string outcome,
        bool applied,
        DateTimeOffset occurredAtUtc,
        string? previousStatus,
        string? currentStatus,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string reason,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        TenantId = tenantId;
        DomainName = domainName;
        Command = command;
        Outcome = outcome;
        Applied = applied;
        OccurredAtUtc = occurredAtUtc;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        DomainOwnership = domainOwnership;
        Reason = reason;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the tenant identifier that was targeted.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name that was targeted.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the workflow command that was requested.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the workflow transition outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the workflow transition was applied.
    /// </summary>
    public bool Applied { get; }

    /// <summary>
    /// Gets the UTC timestamp when the workflow transition was evaluated.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>
    /// Gets the domain ownership status before the workflow transition when one existed.
    /// </summary>
    public string? PreviousStatus { get; }

    /// <summary>
    /// Gets the domain ownership status after the workflow transition when one exists.
    /// </summary>
    public string? CurrentStatus { get; }

    /// <summary>
    /// Gets the resulting domain ownership descriptor when one exists.
    /// </summary>
    public TenantDomainOwnershipDescriptor? DomainOwnership { get; }

    /// <summary>
    /// Gets the operator-facing transition reason.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets optional result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
