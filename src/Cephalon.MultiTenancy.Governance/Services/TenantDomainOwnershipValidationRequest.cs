namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one request to validate declared tenant-domain ownership.
/// </summary>
public sealed class TenantDomainOwnershipValidationRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership validation request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to validate.</param>
    /// <param name="domainName">The domain name to validate.</param>
    /// <param name="atUtc">The UTC timestamp used for expiration evaluation. The runtime clock is used when omitted.</param>
    /// <param name="correlationId">The optional correlation identifier for the validation.</param>
    /// <param name="metadata">Optional request metadata.</param>
    public TenantDomainOwnershipValidationRequest(
        string tenantId,
        string domainName,
        DateTimeOffset? atUtc = null,
        string? correlationId = null,
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
        DomainName = TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName);
        AtUtc = atUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier to validate.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name to validate.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the UTC timestamp used for expiration evaluation.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the validation.
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
