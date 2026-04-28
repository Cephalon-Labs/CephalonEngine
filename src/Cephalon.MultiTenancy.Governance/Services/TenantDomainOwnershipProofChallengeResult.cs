namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of tenant-domain ownership proof challenge issuance.
/// </summary>
public sealed class TenantDomainOwnershipProofChallengeResult
{
    /// <summary>
    /// Creates a tenant-domain ownership proof challenge result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="domainName">The canonical domain name that was evaluated.</param>
    /// <param name="verificationMethod">The verification method used for challenge issuance.</param>
    /// <param name="outcome">The stable challenge issuance outcome.</param>
    /// <param name="issued">A value indicating whether a challenge was issued and stored.</param>
    /// <param name="issuedAtUtc">The UTC timestamp when challenge issuance executed.</param>
    /// <param name="challengeValue">The public proof challenge value to publish.</param>
    /// <param name="challengeFingerprint">The SHA-256 fingerprint of the challenge value.</param>
    /// <param name="dnsTxtRecordName">The DNS TXT record name where the challenge should be published.</param>
    /// <param name="httpFilePath">The HTTP path where the challenge should be published.</param>
    /// <param name="domainOwnership">The matching or resulting domain ownership descriptor when one exists.</param>
    /// <param name="reason">The operator-facing challenge issuance reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipProofChallengeResult(
        string tenantId,
        string domainName,
        string? verificationMethod,
        string outcome,
        bool issued,
        DateTimeOffset issuedAtUtc,
        string? challengeValue,
        string? challengeFingerprint,
        string? dnsTxtRecordName,
        string? httpFilePath,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string reason,
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

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        TenantId = tenantId.Trim();
        DomainName = TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName);
        VerificationMethod = string.IsNullOrWhiteSpace(verificationMethod) ? null : verificationMethod.Trim().ToLowerInvariant();
        Outcome = NormalizeOutcome(outcome);
        Issued = issued;
        IssuedAtUtc = issuedAtUtc;
        ChallengeValue = string.IsNullOrWhiteSpace(challengeValue) ? null : challengeValue.Trim();
        ChallengeFingerprint = string.IsNullOrWhiteSpace(challengeFingerprint) ? null : challengeFingerprint.Trim();
        DnsTxtRecordName = string.IsNullOrWhiteSpace(dnsTxtRecordName) ? null : dnsTxtRecordName.Trim();
        HttpFilePath = string.IsNullOrWhiteSpace(httpFilePath) ? null : httpFilePath.Trim();
        DomainOwnership = domainOwnership;
        Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that was evaluated.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name that was evaluated.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the verification method used for challenge issuance.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the stable challenge issuance outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether a challenge was issued and stored.
    /// </summary>
    public bool Issued { get; }

    /// <summary>
    /// Gets the UTC timestamp when challenge issuance executed.
    /// </summary>
    public DateTimeOffset IssuedAtUtc { get; }

    /// <summary>
    /// Gets the public proof challenge value to publish.
    /// </summary>
    public string? ChallengeValue { get; }

    /// <summary>
    /// Gets the SHA-256 fingerprint of the challenge value.
    /// </summary>
    public string? ChallengeFingerprint { get; }

    /// <summary>
    /// Gets the DNS TXT record name where the challenge should be published.
    /// </summary>
    public string? DnsTxtRecordName { get; }

    /// <summary>
    /// Gets the HTTP path where the challenge should be published.
    /// </summary>
    public string? HttpFilePath { get; }

    /// <summary>
    /// Gets the matching or resulting domain ownership descriptor when one exists.
    /// </summary>
    public TenantDomainOwnershipDescriptor? DomainOwnership { get; }

    /// <summary>
    /// Gets the operator-facing challenge issuance reason.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets optional result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantDomainOwnershipProofChallengeOutcomes.Issued => TenantDomainOwnershipProofChallengeOutcomes.Issued,
            TenantDomainOwnershipProofChallengeOutcomes.Disabled => TenantDomainOwnershipProofChallengeOutcomes.Disabled,
            TenantDomainOwnershipProofChallengeOutcomes.TenantMismatch => TenantDomainOwnershipProofChallengeOutcomes.TenantMismatch,
            TenantDomainOwnershipProofChallengeOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofChallengeOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofChallengeOutcomes.AlreadyVerified => TenantDomainOwnershipProofChallengeOutcomes.AlreadyVerified,
            TenantDomainOwnershipProofChallengeOutcomes.InvalidStatus => TenantDomainOwnershipProofChallengeOutcomes.InvalidStatus,
            TenantDomainOwnershipProofChallengeOutcomes.StoreFailed => TenantDomainOwnershipProofChallengeOutcomes.StoreFailed,
            _ => throw new ArgumentException($"Tenant-domain ownership proof challenge outcome '{outcome}' is not supported.", nameof(outcome))
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
