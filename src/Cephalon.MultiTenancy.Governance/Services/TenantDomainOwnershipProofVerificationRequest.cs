namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a tenant-domain ownership proof verification runner request.
/// </summary>
public sealed class TenantDomainOwnershipProofVerificationRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership proof verification runner request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the domain declaration.</param>
    /// <param name="domainName">The domain name being verified.</param>
    /// <param name="verificationMethod">The optional verification method boundary. When omitted, the existing declaration method or HTTP file is used.</param>
    /// <param name="observedProof">Optional observed proof supplied by an application or provider pack.</param>
    /// <param name="collectionBaseUri">The optional base URI used by HTTP file proof collection.</param>
    /// <param name="source">The source that requested the verification run.</param>
    /// <param name="actor">The actor that requested the verification run when known.</param>
    /// <param name="atUtc">The UTC timestamp used by the run. The runtime clock is used when omitted.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp applied if proof evaluation verifies the declaration.</param>
    /// <param name="correlationId">The optional correlation identifier for the run.</param>
    /// <param name="issueChallengeWhenMissingExpectedProof">A value indicating whether the runner should issue a challenge when expected proof metadata is missing.</param>
    /// <param name="collectHttpProof">A value indicating whether the runner should use the built-in HTTP proof collector for HTTP file declarations.</param>
    /// <param name="recordPublicationPlan">A value indicating whether publication planning metadata should be recorded.</param>
    /// <param name="timeout">The optional per-request HTTP collection timeout.</param>
    /// <param name="metadata">Optional proof verification runner metadata.</param>
    public TenantDomainOwnershipProofVerificationRequest(
        string tenantId,
        string domainName,
        string? verificationMethod = null,
        string? observedProof = null,
        Uri? collectionBaseUri = null,
        string? source = null,
        string? actor = null,
        DateTimeOffset? atUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? correlationId = null,
        bool issueChallengeWhenMissingExpectedProof = true,
        bool collectHttpProof = true,
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
        VerificationMethod = NormalizeVerificationMethod(verificationMethod);
        ObservedProof = string.IsNullOrWhiteSpace(observedProof) ? null : observedProof.Trim();
        CollectionBaseUri = collectionBaseUri;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        AtUtc = atUtc;
        ExpiresAtUtc = expiresAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        IssueChallengeWhenMissingExpectedProof = issueChallengeWhenMissingExpectedProof;
        CollectHttpProof = collectHttpProof;
        RecordPublicationPlan = recordPublicationPlan;
        Timeout = timeout;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the domain declaration.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name being verified.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the optional verification method boundary.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets optional observed proof supplied by an application or provider pack.
    /// </summary>
    public string? ObservedProof { get; }

    /// <summary>
    /// Gets the optional base URI used by HTTP file proof collection.
    /// </summary>
    public Uri? CollectionBaseUri { get; }

    /// <summary>
    /// Gets the source that requested the verification run.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested the verification run when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the UTC timestamp used by the run.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp applied if proof evaluation verifies the declaration.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the run.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether the runner should issue a challenge when expected proof metadata is missing.
    /// </summary>
    public bool IssueChallengeWhenMissingExpectedProof { get; }

    /// <summary>
    /// Gets a value indicating whether the runner should use the built-in HTTP proof collector for HTTP file declarations.
    /// </summary>
    public bool CollectHttpProof { get; }

    /// <summary>
    /// Gets a value indicating whether publication planning metadata should be recorded.
    /// </summary>
    public bool RecordPublicationPlan { get; }

    /// <summary>
    /// Gets the optional per-request HTTP collection timeout.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets optional proof verification runner metadata.
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
