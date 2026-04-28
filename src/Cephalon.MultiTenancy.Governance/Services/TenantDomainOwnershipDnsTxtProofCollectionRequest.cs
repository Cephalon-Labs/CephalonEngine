namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a tenant-domain ownership DNS TXT proof collection request.
/// </summary>
public sealed class TenantDomainOwnershipDnsTxtProofCollectionRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership DNS TXT proof collection request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the domain declaration.</param>
    /// <param name="domainName">The domain name whose DNS TXT proof should be collected.</param>
    /// <param name="verificationMethod">The optional verification method boundary. Only DNS TXT verification can be collected.</param>
    /// <param name="resolverEndpoint">The optional DNS-over-HTTPS resolver endpoint used for collection.</param>
    /// <param name="source">The source that requested DNS TXT proof collection.</param>
    /// <param name="actor">The actor that requested DNS TXT proof collection when known.</param>
    /// <param name="atUtc">The UTC timestamp used for collection. The runtime clock is used when omitted.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp applied if proof evaluation verifies the declaration.</param>
    /// <param name="correlationId">The optional correlation identifier for collection and evaluation.</param>
    /// <param name="recordPublicationPlan">A value indicating whether the publication plan should be recorded before collection.</param>
    /// <param name="timeout">The optional per-request DNS TXT collection timeout.</param>
    /// <param name="metadata">Optional DNS TXT proof collection metadata.</param>
    public TenantDomainOwnershipDnsTxtProofCollectionRequest(
        string tenantId,
        string domainName,
        string? verificationMethod = null,
        Uri? resolverEndpoint = null,
        string? source = null,
        string? actor = null,
        DateTimeOffset? atUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? correlationId = null,
        bool recordPublicationPlan = true,
        TimeSpan? timeout = null,
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

        if (resolverEndpoint is not null && !resolverEndpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("Resolver endpoint must be absolute.", nameof(resolverEndpoint));
        }

        TenantId = tenantId.Trim();
        DomainName = TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName);
        VerificationMethod = NormalizeVerificationMethod(verificationMethod) ?? TenantDomainVerificationMethods.DnsTxt;
        ResolverEndpoint = resolverEndpoint;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        AtUtc = atUtc;
        ExpiresAtUtc = expiresAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        RecordPublicationPlan = recordPublicationPlan;
        Timeout = timeout;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the domain declaration.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name whose DNS TXT proof should be collected.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the verification method boundary.
    /// </summary>
    public string VerificationMethod { get; }

    /// <summary>
    /// Gets the optional DNS-over-HTTPS resolver endpoint used for collection.
    /// </summary>
    public Uri? ResolverEndpoint { get; }

    /// <summary>
    /// Gets the source that requested DNS TXT proof collection.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested DNS TXT proof collection when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the UTC timestamp used for collection.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp applied if proof evaluation verifies the declaration.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for collection and evaluation.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether the publication plan should be recorded before collection.
    /// </summary>
    public bool RecordPublicationPlan { get; }

    /// <summary>
    /// Gets the optional per-request DNS TXT collection timeout.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets optional DNS TXT proof collection metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string? NormalizeVerificationMethod(string? verificationMethod)
    {
        if (string.IsNullOrWhiteSpace(verificationMethod))
        {
            return null;
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
