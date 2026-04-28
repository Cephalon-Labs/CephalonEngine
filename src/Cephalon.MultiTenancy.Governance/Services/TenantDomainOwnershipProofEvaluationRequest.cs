namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes reported proof evidence for a tenant-domain ownership declaration.
/// </summary>
public sealed class TenantDomainOwnershipProofEvaluationRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership proof evaluation request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the domain declaration.</param>
    /// <param name="domainName">The domain name whose proof should be evaluated.</param>
    /// <param name="observedProof">The proof value observed by the application or provider pack.</param>
    /// <param name="verificationMethod">The optional verification method boundary.</param>
    /// <param name="expectedProof">The optional expected proof value. Descriptor metadata is used when this is omitted.</param>
    /// <param name="source">The source that reported the observed proof evidence.</param>
    /// <param name="actor">The actor that requested proof evaluation when known.</param>
    /// <param name="atUtc">The UTC timestamp used for proof evaluation. The runtime clock is used when omitted.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp when the ownership declaration expires.</param>
    /// <param name="correlationId">The optional correlation identifier for proof evaluation.</param>
    /// <param name="metadata">Optional proof evaluation metadata.</param>
    public TenantDomainOwnershipProofEvaluationRequest(
        string tenantId,
        string domainName,
        string? observedProof = null,
        string? verificationMethod = null,
        string? expectedProof = null,
        string? source = null,
        string? actor = null,
        DateTimeOffset? atUtc = null,
        DateTimeOffset? expiresAtUtc = null,
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
        ObservedProof = string.IsNullOrWhiteSpace(observedProof) ? null : observedProof.Trim();
        VerificationMethod = NormalizeVerificationMethod(verificationMethod);
        ExpectedProof = string.IsNullOrWhiteSpace(expectedProof) ? null : expectedProof.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        AtUtc = atUtc;
        ExpiresAtUtc = expiresAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the domain declaration.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name whose proof should be evaluated.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the proof value observed by the application or provider pack.
    /// </summary>
    public string? ObservedProof { get; }

    /// <summary>
    /// Gets the optional verification method boundary.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the optional expected proof value. Descriptor metadata is used when this is omitted.
    /// </summary>
    public string? ExpectedProof { get; }

    /// <summary>
    /// Gets the source that reported the observed proof evidence.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested proof evaluation when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the UTC timestamp used for proof evaluation.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when the ownership declaration expires.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for proof evaluation.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional proof evaluation metadata.
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
