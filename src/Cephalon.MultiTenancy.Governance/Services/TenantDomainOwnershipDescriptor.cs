namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one declared domain ownership relationship for a tenant.
/// </summary>
public sealed class TenantDomainOwnershipDescriptor
{
    /// <summary>
    /// Creates a tenant-domain ownership descriptor.
    /// </summary>
    /// <param name="tenantId">The stable tenant identifier.</param>
    /// <param name="domainName">The domain name claimed by the tenant.</param>
    /// <param name="displayName">The optional operator-facing domain name.</param>
    /// <param name="status">The domain ownership status.</param>
    /// <param name="verificationMethod">The verification method associated with the descriptor.</param>
    /// <param name="verifiedAtUtc">The UTC timestamp when ownership was verified.</param>
    /// <param name="expiresAtUtc">The UTC timestamp when ownership expires.</param>
    /// <param name="sourceModuleId">The module that contributed the domain ownership descriptor when one is known.</param>
    /// <param name="metadata">Optional operator-facing metadata attached to the descriptor.</param>
    public TenantDomainOwnershipDescriptor(
        string tenantId,
        string domainName,
        string? displayName = null,
        string status = TenantDomainOwnershipStatuses.Pending,
        string? verificationMethod = null,
        DateTimeOffset? verifiedAtUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? sourceModuleId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(domainName))
        {
            throw new ArgumentException("Domain name is required.", nameof(domainName));
        }

        TenantId = tenantId.Trim();
        DomainName = NormalizeDomainName(domainName);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Status = NormalizeStatus(status);
        VerificationMethod = NormalizeVerificationMethod(verificationMethod);
        VerifiedAtUtc = verifiedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        SourceModuleId = string.IsNullOrWhiteSpace(sourceModuleId) ? null : sourceModuleId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable tenant identifier.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name claimed by the tenant.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the optional operator-facing domain name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the domain ownership status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the verification method associated with the descriptor.
    /// </summary>
    public string VerificationMethod { get; }

    /// <summary>
    /// Gets the UTC timestamp when ownership was verified.
    /// </summary>
    public DateTimeOffset? VerifiedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when ownership expires.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the module that contributed the domain ownership descriptor when one is known.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets optional operator-facing metadata attached to the descriptor.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    internal static string NormalizeDomainName(string domainName)
    {
        var normalized = domainName.Trim().TrimEnd('.').ToLowerInvariant();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Domain name is required.", nameof(domainName));
        }

        if (normalized.Contains("://", StringComparison.Ordinal))
        {
            throw new ArgumentException("Domain name must not include a URI scheme.", nameof(domainName));
        }

        return normalized;
    }

    private static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return TenantDomainOwnershipStatuses.Pending;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantDomainOwnershipStatuses.Verified => TenantDomainOwnershipStatuses.Verified,
            TenantDomainOwnershipStatuses.Pending => TenantDomainOwnershipStatuses.Pending,
            TenantDomainOwnershipStatuses.Rejected => TenantDomainOwnershipStatuses.Rejected,
            TenantDomainOwnershipStatuses.Suspended => TenantDomainOwnershipStatuses.Suspended,
            TenantDomainOwnershipStatuses.Expired => TenantDomainOwnershipStatuses.Expired,
            _ => throw new ArgumentException($"Tenant-domain ownership status '{status}' is not supported.", nameof(status))
        };
    }

    private static string NormalizeVerificationMethod(string? verificationMethod)
    {
        if (string.IsNullOrWhiteSpace(verificationMethod))
        {
            return TenantDomainVerificationMethods.Manual;
        }

        var normalized = verificationMethod.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantDomainVerificationMethods.Manual => TenantDomainVerificationMethods.Manual,
            TenantDomainVerificationMethods.DnsTxt => TenantDomainVerificationMethods.DnsTxt,
            TenantDomainVerificationMethods.HttpFile => TenantDomainVerificationMethods.HttpFile,
            _ => normalized
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
