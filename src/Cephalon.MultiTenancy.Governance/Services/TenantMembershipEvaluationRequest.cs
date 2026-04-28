namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one request to evaluate tenant membership.
/// </summary>
public sealed class TenantMembershipEvaluationRequest
{
    /// <summary>
    /// Creates a tenant-membership evaluation request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to evaluate.</param>
    /// <param name="principalId">The principal identifier to evaluate.</param>
    /// <param name="requiredRoles">The optional tenant-local roles required for access.</param>
    /// <param name="atUtc">The UTC timestamp used for time-window evaluation. The runtime clock is used when omitted.</param>
    /// <param name="correlationId">The optional correlation identifier for the evaluation.</param>
    /// <param name="metadata">Optional request metadata.</param>
    /// <param name="principalKind">The principal kind to evaluate. The default is user.</param>
    public TenantMembershipEvaluationRequest(
        string tenantId,
        string principalId,
        IReadOnlyList<string>? requiredRoles = null,
        DateTimeOffset? atUtc = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? principalKind = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(principalId))
        {
            throw new ArgumentException("Principal id is required.", nameof(principalId));
        }

        TenantId = tenantId.Trim();
        PrincipalId = principalId.Trim();
        PrincipalKind = string.IsNullOrWhiteSpace(principalKind) ? "user" : principalKind.Trim();
        RequiredRoles = NormalizeValues(requiredRoles);
        AtUtc = atUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier to evaluate.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the principal identifier to evaluate.
    /// </summary>
    public string PrincipalId { get; }

    /// <summary>
    /// Gets the principal kind to evaluate.
    /// </summary>
    public string PrincipalKind { get; }

    /// <summary>
    /// Gets the tenant-local roles required for access.
    /// </summary>
    public IReadOnlyList<string> RequiredRoles { get; }

    /// <summary>
    /// Gets the UTC timestamp used for time-window evaluation.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the evaluation.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional request metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] NormalizeValues(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
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
