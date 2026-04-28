namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one request to decide whether a tenant-governance action can proceed.
/// </summary>
public sealed class TenantGovernanceActionDecisionRequest
{
    /// <summary>
    /// Creates a tenant-governance action decision request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to validate.</param>
    /// <param name="actionId">The governance action identifier to validate.</param>
    /// <param name="actionKind">The optional expected governance action kind.</param>
    /// <param name="subjectKind">The optional expected subject kind.</param>
    /// <param name="subjectId">The optional expected subject identifier.</param>
    /// <param name="atUtc">The UTC timestamp used for expiration evaluation. The runtime clock is used when omitted.</param>
    /// <param name="correlationId">The optional correlation identifier for the decision.</param>
    /// <param name="metadata">Optional request metadata.</param>
    public TenantGovernanceActionDecisionRequest(
        string tenantId,
        string actionId,
        string? actionKind = null,
        string? subjectKind = null,
        string? subjectId = null,
        DateTimeOffset? atUtc = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(actionId))
        {
            throw new ArgumentException("Action id is required.", nameof(actionId));
        }

        TenantId = tenantId.Trim();
        ActionId = actionId.Trim();
        ActionKind = string.IsNullOrWhiteSpace(actionKind) ? null : TenantGovernanceActionDescriptor.NormalizeActionKind(actionKind);
        SubjectKind = string.IsNullOrWhiteSpace(subjectKind) ? null : TenantGovernanceActionDescriptor.NormalizeSubjectKind(subjectKind);
        SubjectId = string.IsNullOrWhiteSpace(subjectId) ? null : subjectId.Trim();
        AtUtc = atUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier to validate.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the governance action identifier to validate.
    /// </summary>
    public string ActionId { get; }

    /// <summary>
    /// Gets the optional expected governance action kind.
    /// </summary>
    public string? ActionKind { get; }

    /// <summary>
    /// Gets the optional expected subject kind.
    /// </summary>
    public string? SubjectKind { get; }

    /// <summary>
    /// Gets the optional expected subject identifier.
    /// </summary>
    public string? SubjectId { get; }

    /// <summary>
    /// Gets the UTC timestamp used for expiration evaluation.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the decision.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional request metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

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
