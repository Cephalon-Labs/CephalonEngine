namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one principal membership inside a tenant.
/// </summary>
public sealed class TenantMembershipDescriptor
{
    /// <summary>
    /// Creates a new tenant-membership descriptor.
    /// </summary>
    /// <param name="tenantId">The stable tenant identifier.</param>
    /// <param name="principalId">The stable principal identifier.</param>
    /// <param name="principalKind">The principal kind, such as user, group, service, or organization.</param>
    /// <param name="displayName">The optional operator-facing membership name.</param>
    /// <param name="roles">The tenant-local roles associated with the principal.</param>
    /// <param name="status">The membership status.</param>
    /// <param name="effectiveFromUtc">The UTC timestamp when the membership becomes active.</param>
    /// <param name="expiresAtUtc">The UTC timestamp when the membership expires.</param>
    /// <param name="sourceModuleId">The module that contributed the membership when one is known.</param>
    /// <param name="metadata">Optional operator-facing metadata attached to the membership.</param>
    public TenantMembershipDescriptor(
        string tenantId,
        string principalId,
        string? principalKind = null,
        string? displayName = null,
        IReadOnlyList<string>? roles = null,
        string status = TenantMembershipStatuses.Active,
        DateTimeOffset? effectiveFromUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? sourceModuleId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
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
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Roles = NormalizeValues(roles);
        Status = NormalizeStatus(status);
        EffectiveFromUtc = effectiveFromUtc;
        ExpiresAtUtc = expiresAtUtc;
        SourceModuleId = string.IsNullOrWhiteSpace(sourceModuleId) ? null : sourceModuleId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable tenant identifier.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the stable principal identifier.
    /// </summary>
    public string PrincipalId { get; }

    /// <summary>
    /// Gets the principal kind, such as user, group, service, or organization.
    /// </summary>
    public string PrincipalKind { get; }

    /// <summary>
    /// Gets the optional operator-facing membership name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the tenant-local roles associated with the principal.
    /// </summary>
    public IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Gets the membership status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the UTC timestamp when the membership becomes active.
    /// </summary>
    public DateTimeOffset? EffectiveFromUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the membership expires.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the module that contributed the membership when one is known.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets optional operator-facing metadata attached to the membership.
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

    private static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return TenantMembershipStatuses.Active;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantMembershipStatuses.Active => TenantMembershipStatuses.Active,
            TenantMembershipStatuses.Suspended => TenantMembershipStatuses.Suspended,
            TenantMembershipStatuses.Expired => TenantMembershipStatuses.Expired,
            _ => throw new ArgumentException($"Tenant-membership status '{status}' is not supported.", nameof(status))
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
