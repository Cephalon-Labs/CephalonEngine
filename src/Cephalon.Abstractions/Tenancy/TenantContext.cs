namespace Cephalon.Abstractions.Tenancy;

/// <summary>
/// Describes the tenant currently associated with an operation or ambient runtime scope.
/// </summary>
public sealed class TenantContext
{
    /// <summary>
    /// Creates a new tenant context.
    /// </summary>
    /// <param name="tenantId">The stable tenant identifier.</param>
    /// <param name="tenantKey">The tenant key, slug, or subdomain-friendly identifier when one is known.</param>
    /// <param name="displayName">The human-readable tenant name when one is known.</param>
    /// <param name="parentTenantId">The parent tenant identifier when one is known.</param>
    /// <param name="domains">Optional domains associated with the tenant.</param>
    /// <param name="attributes">Optional tenant attributes.</param>
    public TenantContext(
        string tenantId,
        string? tenantKey = null,
        string? displayName = null,
        string? parentTenantId = null,
        IReadOnlyList<string>? domains = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        TenantId = tenantId.Trim();
        TenantKey = string.IsNullOrWhiteSpace(tenantKey) ? null : tenantKey.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        ParentTenantId = string.IsNullOrWhiteSpace(parentTenantId) ? null : parentTenantId.Trim();
        Domains = domains?
            .Where(static domain => !string.IsNullOrWhiteSpace(domain))
            .Select(static domain => domain.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static domain => domain, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Attributes = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable tenant identifier.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the tenant key, slug, or subdomain-friendly identifier when one is known.
    /// </summary>
    public string? TenantKey { get; }

    /// <summary>
    /// Gets the human-readable tenant name when one is known.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the parent tenant identifier when one is known.
    /// </summary>
    public string? ParentTenantId { get; }

    /// <summary>
    /// Gets the domains associated with the tenant.
    /// </summary>
    public IReadOnlyList<string> Domains { get; }

    /// <summary>
    /// Gets the tenant attributes.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}
