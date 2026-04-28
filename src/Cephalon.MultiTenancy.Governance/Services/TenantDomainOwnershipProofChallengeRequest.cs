namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a tenant-domain ownership proof challenge issuance request.
/// </summary>
public sealed class TenantDomainOwnershipProofChallengeRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership proof challenge request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the domain declaration.</param>
    /// <param name="domainName">The domain name that should receive a proof challenge.</param>
    /// <param name="verificationMethod">The optional verification method boundary.</param>
    /// <param name="displayName">The optional operator-facing domain display name.</param>
    /// <param name="challengeValue">An optional caller-supplied challenge value. A secure random value is generated when omitted.</param>
    /// <param name="source">The source that requested challenge issuance.</param>
    /// <param name="actor">The actor that requested challenge issuance when known.</param>
    /// <param name="atUtc">The UTC timestamp used for challenge issuance. The runtime clock is used when omitted.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp when the challenge and ownership declaration expire.</param>
    /// <param name="correlationId">The optional correlation identifier for challenge issuance.</param>
    /// <param name="dnsTxtRecordName">The optional DNS TXT record name where the challenge should be published.</param>
    /// <param name="httpFilePath">The optional HTTP path where the challenge should be published.</param>
    /// <param name="metadata">Optional proof challenge metadata.</param>
    public TenantDomainOwnershipProofChallengeRequest(
        string tenantId,
        string domainName,
        string? verificationMethod = null,
        string? displayName = null,
        string? challengeValue = null,
        string? source = null,
        string? actor = null,
        DateTimeOffset? atUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? correlationId = null,
        string? dnsTxtRecordName = null,
        string? httpFilePath = null,
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
        VerificationMethod = NormalizeVerificationMethod(verificationMethod);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        ChallengeValue = string.IsNullOrWhiteSpace(challengeValue) ? null : challengeValue.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        AtUtc = atUtc;
        ExpiresAtUtc = expiresAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        DnsTxtRecordName = string.IsNullOrWhiteSpace(dnsTxtRecordName) ? null : dnsTxtRecordName.Trim();
        HttpFilePath = string.IsNullOrWhiteSpace(httpFilePath) ? null : NormalizeHttpFilePath(httpFilePath);
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the domain declaration.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name that should receive a proof challenge.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the optional verification method boundary.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the optional operator-facing domain display name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets an optional caller-supplied challenge value.
    /// </summary>
    public string? ChallengeValue { get; }

    /// <summary>
    /// Gets the source that requested challenge issuance.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested challenge issuance when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the UTC timestamp used for challenge issuance.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when the challenge and ownership declaration expire.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for challenge issuance.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the optional DNS TXT record name where the challenge should be published.
    /// </summary>
    public string? DnsTxtRecordName { get; }

    /// <summary>
    /// Gets the optional HTTP path where the challenge should be published.
    /// </summary>
    public string? HttpFilePath { get; }

    /// <summary>
    /// Gets optional proof challenge metadata.
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

    private static string NormalizeHttpFilePath(string httpFilePath)
    {
        var normalized = httpFilePath.Trim();
        return normalized.Length > 0 && normalized[0] == '/'
            ? normalized
            : $"/{normalized}";
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
