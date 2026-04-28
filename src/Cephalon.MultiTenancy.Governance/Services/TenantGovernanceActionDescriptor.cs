namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one tenant-governance approval or remediation action.
/// </summary>
public sealed class TenantGovernanceActionDescriptor
{
    /// <summary>
    /// Creates a tenant-governance action descriptor.
    /// </summary>
    /// <param name="actionId">The stable action identifier.</param>
    /// <param name="tenantId">The stable tenant identifier.</param>
    /// <param name="actionKind">The governance action kind.</param>
    /// <param name="subjectKind">The kind of subject affected by the action.</param>
    /// <param name="subjectId">The stable subject identifier affected by the action.</param>
    /// <param name="displayName">The optional operator-facing action name.</param>
    /// <param name="status">The governance action status.</param>
    /// <param name="requestedBy">The actor that requested the action when known.</param>
    /// <param name="approvedBy">The actor that approved or remediated the action when known.</param>
    /// <param name="createdAtUtc">The UTC timestamp when the action was created.</param>
    /// <param name="decidedAtUtc">The UTC timestamp when the action was approved, rejected, or remediated.</param>
    /// <param name="expiresAtUtc">The UTC timestamp when the action expires.</param>
    /// <param name="sourceModuleId">The module that contributed the action when one is known.</param>
    /// <param name="metadata">Optional operator-facing metadata attached to the action.</param>
    public TenantGovernanceActionDescriptor(
        string actionId,
        string tenantId,
        string actionKind,
        string? subjectKind = null,
        string? subjectId = null,
        string? displayName = null,
        string status = TenantGovernanceActionStatuses.PendingApproval,
        string? requestedBy = null,
        string? approvedBy = null,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? decidedAtUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? sourceModuleId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            throw new ArgumentException("Action id is required.", nameof(actionId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(actionKind))
        {
            throw new ArgumentException("Action kind is required.", nameof(actionKind));
        }

        ActionId = actionId.Trim();
        TenantId = tenantId.Trim();
        ActionKind = NormalizeActionKind(actionKind);
        SubjectKind = NormalizeSubjectKind(subjectKind);
        SubjectId = string.IsNullOrWhiteSpace(subjectId) ? TenantId : subjectId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Status = NormalizeStatus(status);
        RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? null : requestedBy.Trim();
        ApprovedBy = string.IsNullOrWhiteSpace(approvedBy) ? null : approvedBy.Trim();
        CreatedAtUtc = createdAtUtc;
        DecidedAtUtc = decidedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        SourceModuleId = string.IsNullOrWhiteSpace(sourceModuleId) ? null : sourceModuleId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable action identifier.
    /// </summary>
    public string ActionId { get; }

    /// <summary>
    /// Gets the stable tenant identifier.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the governance action kind.
    /// </summary>
    public string ActionKind { get; }

    /// <summary>
    /// Gets the kind of subject affected by the action.
    /// </summary>
    public string SubjectKind { get; }

    /// <summary>
    /// Gets the stable subject identifier affected by the action.
    /// </summary>
    public string SubjectId { get; }

    /// <summary>
    /// Gets the optional operator-facing action name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the governance action status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the actor that requested the action when known.
    /// </summary>
    public string? RequestedBy { get; }

    /// <summary>
    /// Gets the actor that approved or remediated the action when known.
    /// </summary>
    public string? ApprovedBy { get; }

    /// <summary>
    /// Gets the UTC timestamp when the action was created.
    /// </summary>
    public DateTimeOffset? CreatedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the action was approved, rejected, or remediated.
    /// </summary>
    public DateTimeOffset? DecidedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the action expires.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the module that contributed the action when one is known.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets optional operator-facing metadata attached to the action.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    internal static string NormalizeActionKind(string actionKind)
    {
        if (string.IsNullOrWhiteSpace(actionKind))
        {
            throw new ArgumentException("Action kind is required.", nameof(actionKind));
        }

        return actionKind.Trim().ToLowerInvariant();
    }

    internal static string NormalizeSubjectKind(string? subjectKind)
    {
        return string.IsNullOrWhiteSpace(subjectKind)
            ? "tenant"
            : subjectKind.Trim().ToLowerInvariant();
    }

    private static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return TenantGovernanceActionStatuses.PendingApproval;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantGovernanceActionStatuses.PendingApproval => TenantGovernanceActionStatuses.PendingApproval,
            TenantGovernanceActionStatuses.Approved => TenantGovernanceActionStatuses.Approved,
            TenantGovernanceActionStatuses.Rejected => TenantGovernanceActionStatuses.Rejected,
            TenantGovernanceActionStatuses.RemediationRequired => TenantGovernanceActionStatuses.RemediationRequired,
            TenantGovernanceActionStatuses.Remediated => TenantGovernanceActionStatuses.Remediated,
            TenantGovernanceActionStatuses.Expired => TenantGovernanceActionStatuses.Expired,
            _ => throw new ArgumentException($"Tenant-governance action status '{status}' is not supported.", nameof(status))
        };
    }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
