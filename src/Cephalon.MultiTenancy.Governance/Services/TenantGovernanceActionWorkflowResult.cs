namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-governance action workflow transition.
/// </summary>
public sealed class TenantGovernanceActionWorkflowResult
{
    /// <summary>
    /// Creates a tenant-governance action workflow transition result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was targeted.</param>
    /// <param name="actionId">The governance action identifier that was targeted.</param>
    /// <param name="command">The workflow command that was requested.</param>
    /// <param name="outcome">The workflow transition outcome.</param>
    /// <param name="applied">A value indicating whether the workflow transition was applied.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the workflow transition was evaluated.</param>
    /// <param name="previousStatus">The action status before the workflow transition when one existed.</param>
    /// <param name="currentStatus">The action status after the workflow transition when one exists.</param>
    /// <param name="action">The resulting action descriptor when one exists.</param>
    /// <param name="reason">The operator-facing transition reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantGovernanceActionWorkflowResult(
        string tenantId,
        string actionId,
        string command,
        string outcome,
        bool applied,
        DateTimeOffset occurredAtUtc,
        string? previousStatus,
        string? currentStatus,
        TenantGovernanceActionDescriptor? action,
        string reason,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        TenantId = tenantId;
        ActionId = actionId;
        Command = command;
        Outcome = outcome;
        Applied = applied;
        OccurredAtUtc = occurredAtUtc;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Action = action;
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
    /// Gets the governance action identifier that was targeted.
    /// </summary>
    public string ActionId { get; }

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
    /// Gets the action status before the workflow transition when one existed.
    /// </summary>
    public string? PreviousStatus { get; }

    /// <summary>
    /// Gets the action status after the workflow transition when one exists.
    /// </summary>
    public string? CurrentStatus { get; }

    /// <summary>
    /// Gets the resulting action descriptor when one exists.
    /// </summary>
    public TenantGovernanceActionDescriptor? Action { get; }

    /// <summary>
    /// Gets the operator-facing transition reason.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets optional result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
