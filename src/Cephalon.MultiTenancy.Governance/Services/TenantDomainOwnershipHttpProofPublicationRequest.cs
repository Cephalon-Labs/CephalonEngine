namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a tenant-domain ownership HTTP proof publication request.
/// </summary>
public sealed class TenantDomainOwnershipHttpProofPublicationRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership HTTP proof publication request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the domain declaration.</param>
    /// <param name="domainName">The domain name that should publish the HTTP proof file.</param>
    /// <param name="source">The source that requested HTTP proof publication.</param>
    /// <param name="actor">The actor that requested HTTP proof publication when known.</param>
    /// <param name="atUtc">The UTC timestamp used for publication. The runtime clock is used when omitted.</param>
    /// <param name="correlationId">The optional correlation identifier for HTTP proof publication.</param>
    /// <param name="recordPublication">A value indicating whether publication metadata should be recorded.</param>
    /// <param name="metadata">Optional HTTP proof publication metadata.</param>
    public TenantDomainOwnershipHttpProofPublicationRequest(
        string tenantId,
        string domainName,
        string? source = null,
        string? actor = null,
        DateTimeOffset? atUtc = null,
        string? correlationId = null,
        bool recordPublication = true,
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
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        AtUtc = atUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        RecordPublication = recordPublication;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the domain declaration.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name that should publish the HTTP proof file.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the source that requested HTTP proof publication.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested HTTP proof publication when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the UTC timestamp used for publication.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for HTTP proof publication.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether publication metadata should be recorded.
    /// </summary>
    public bool RecordPublication { get; }

    /// <summary>
    /// Gets optional HTTP proof publication metadata.
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
