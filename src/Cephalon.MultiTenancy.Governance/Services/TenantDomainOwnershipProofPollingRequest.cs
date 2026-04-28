namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a bounded tenant-domain ownership proof polling request.
/// </summary>
public sealed class TenantDomainOwnershipProofPollingRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership proof polling request.
    /// </summary>
    /// <param name="tenantIds">Optional tenant identifiers to include. When omitted, all tenants are eligible.</param>
    /// <param name="domainNames">Optional domain names to include. When omitted, all domains are eligible.</param>
    /// <param name="verificationMethods">Optional verification methods to include. When omitted, HTTP file and DNS TXT declarations are eligible.</param>
    /// <param name="collectionBaseUri">The optional base URI used by HTTP file proof collection.</param>
    /// <param name="dnsTxtResolverEndpoint">The optional DNS-over-HTTPS resolver endpoint used by DNS TXT proof collection.</param>
    /// <param name="source">The source that requested the polling pass.</param>
    /// <param name="actor">The actor that requested the polling pass when known.</param>
    /// <param name="atUtc">The UTC timestamp used by the polling pass. The runtime clock is used when omitted.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp applied if proof evaluation verifies a declaration.</param>
    /// <param name="correlationId">The optional correlation identifier for the polling pass.</param>
    /// <param name="maxItems">The optional maximum number of declarations to poll in this pass.</param>
    /// <param name="includeHttpFile">A value indicating whether HTTP file declarations are eligible.</param>
    /// <param name="includeDnsTxt">A value indicating whether DNS TXT declarations are eligible.</param>
    /// <param name="includeRejected">A value indicating whether rejected declarations can be retried.</param>
    /// <param name="includeMissingExpectedProof">A value indicating whether declarations without expected proof metadata should still be passed to the verifier.</param>
    /// <param name="recordPublicationPlan">A value indicating whether nested verification should record publication-plan metadata.</param>
    /// <param name="timeout">The optional per-request proof collection timeout.</param>
    /// <param name="metadata">Optional proof polling metadata.</param>
    public TenantDomainOwnershipProofPollingRequest(
        IReadOnlyCollection<string>? tenantIds = null,
        IReadOnlyCollection<string>? domainNames = null,
        IReadOnlyCollection<string>? verificationMethods = null,
        Uri? collectionBaseUri = null,
        Uri? dnsTxtResolverEndpoint = null,
        string? source = null,
        string? actor = null,
        DateTimeOffset? atUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? correlationId = null,
        int? maxItems = null,
        bool includeHttpFile = true,
        bool includeDnsTxt = true,
        bool includeRejected = true,
        bool includeMissingExpectedProof = false,
        bool recordPublicationPlan = false,
        TimeSpan? timeout = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (collectionBaseUri is not null && !collectionBaseUri.IsAbsoluteUri)
        {
            throw new ArgumentException("Collection base URI must be absolute.", nameof(collectionBaseUri));
        }

        if (dnsTxtResolverEndpoint is not null && !dnsTxtResolverEndpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("DNS TXT resolver endpoint must be absolute.", nameof(dnsTxtResolverEndpoint));
        }

        if (maxItems is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems), "Maximum polling item count must be greater than zero.");
        }

        TenantIds = CopyTenantIds(tenantIds);
        DomainNames = CopyDomainNames(domainNames);
        VerificationMethods = CopyVerificationMethods(verificationMethods);
        CollectionBaseUri = collectionBaseUri;
        DnsTxtResolverEndpoint = dnsTxtResolverEndpoint;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        AtUtc = atUtc;
        ExpiresAtUtc = expiresAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        MaxItems = maxItems;
        IncludeHttpFile = includeHttpFile;
        IncludeDnsTxt = includeDnsTxt;
        IncludeRejected = includeRejected;
        IncludeMissingExpectedProof = includeMissingExpectedProof;
        RecordPublicationPlan = recordPublicationPlan;
        Timeout = timeout;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets optional tenant identifiers to include.
    /// </summary>
    public IReadOnlyList<string> TenantIds { get; }

    /// <summary>
    /// Gets optional canonical domain names to include.
    /// </summary>
    public IReadOnlyList<string> DomainNames { get; }

    /// <summary>
    /// Gets optional verification methods to include.
    /// </summary>
    public IReadOnlyList<string> VerificationMethods { get; }

    /// <summary>
    /// Gets the optional base URI used by HTTP file proof collection.
    /// </summary>
    public Uri? CollectionBaseUri { get; }

    /// <summary>
    /// Gets the optional DNS-over-HTTPS resolver endpoint used by DNS TXT proof collection.
    /// </summary>
    public Uri? DnsTxtResolverEndpoint { get; }

    /// <summary>
    /// Gets the source that requested the polling pass.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested the polling pass when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the UTC timestamp used by the polling pass.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp applied if proof evaluation verifies a declaration.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the polling pass.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the optional maximum number of declarations to poll in this pass.
    /// </summary>
    public int? MaxItems { get; }

    /// <summary>
    /// Gets a value indicating whether HTTP file declarations are eligible.
    /// </summary>
    public bool IncludeHttpFile { get; }

    /// <summary>
    /// Gets a value indicating whether DNS TXT declarations are eligible.
    /// </summary>
    public bool IncludeDnsTxt { get; }

    /// <summary>
    /// Gets a value indicating whether rejected declarations can be retried.
    /// </summary>
    public bool IncludeRejected { get; }

    /// <summary>
    /// Gets a value indicating whether declarations without expected proof metadata should still be passed to the verifier.
    /// </summary>
    public bool IncludeMissingExpectedProof { get; }

    /// <summary>
    /// Gets a value indicating whether nested verification should record publication-plan metadata.
    /// </summary>
    public bool RecordPublicationPlan { get; }

    /// <summary>
    /// Gets the optional per-request proof collection timeout.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets optional proof polling metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] CopyTenantIds(IReadOnlyCollection<string>? tenantIds)
    {
        return tenantIds?
            .Where(static tenantId => !string.IsNullOrWhiteSpace(tenantId))
            .Select(static tenantId => tenantId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string[] CopyDomainNames(IReadOnlyCollection<string>? domainNames)
    {
        return domainNames?
            .Where(static domainName => !string.IsNullOrWhiteSpace(domainName))
            .Select(TenantDomainOwnershipDescriptor.NormalizeDomainName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string[] CopyVerificationMethods(IReadOnlyCollection<string>? verificationMethods)
    {
        return verificationMethods?
            .Where(static verificationMethod => !string.IsNullOrWhiteSpace(verificationMethod))
            .Select(static verificationMethod => NormalizeVerificationMethod(verificationMethod))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string NormalizeVerificationMethod(string verificationMethod)
    {
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
