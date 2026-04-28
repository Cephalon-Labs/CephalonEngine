namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a tenant-domain ownership HTTP proof collection request.
/// </summary>
public sealed class TenantDomainOwnershipHttpProofCollectionRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership HTTP proof collection request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the domain declaration.</param>
    /// <param name="domainName">The domain name whose HTTP proof should be collected.</param>
    /// <param name="verificationMethod">The optional verification method boundary. Only HTTP file verification can be collected.</param>
    /// <param name="collectionBaseUri">The optional base URI used for collection. When omitted, HTTPS on the requested domain is used.</param>
    /// <param name="source">The source that requested HTTP proof collection.</param>
    /// <param name="actor">The actor that requested HTTP proof collection when known.</param>
    /// <param name="atUtc">The UTC timestamp used for collection. The runtime clock is used when omitted.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp applied if proof evaluation verifies the declaration.</param>
    /// <param name="correlationId">The optional correlation identifier for collection and evaluation.</param>
    /// <param name="recordPublicationPlan">A value indicating whether the publication plan should be recorded before collection.</param>
    /// <param name="timeout">The optional per-request HTTP collection timeout.</param>
    /// <param name="metadata">Optional HTTP proof collection metadata.</param>
    public TenantDomainOwnershipHttpProofCollectionRequest(
        string tenantId,
        string domainName,
        string? verificationMethod = null,
        Uri? collectionBaseUri = null,
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

        if (collectionBaseUri is not null && !collectionBaseUri.IsAbsoluteUri)
        {
            throw new ArgumentException("Collection base URI must be absolute.", nameof(collectionBaseUri));
        }

        TenantId = tenantId.Trim();
        DomainName = TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName);
        VerificationMethod = NormalizeVerificationMethod(verificationMethod) ?? TenantDomainVerificationMethods.HttpFile;
        CollectionBaseUri = collectionBaseUri;
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
    /// Gets the canonical domain name whose HTTP proof should be collected.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the verification method boundary.
    /// </summary>
    public string VerificationMethod { get; }

    /// <summary>
    /// Gets the optional base URI used for collection.
    /// </summary>
    public Uri? CollectionBaseUri { get; }

    /// <summary>
    /// Gets the source that requested HTTP proof collection.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested HTTP proof collection when known.
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
    /// Gets the optional per-request HTTP collection timeout.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets optional HTTP proof collection metadata.
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
