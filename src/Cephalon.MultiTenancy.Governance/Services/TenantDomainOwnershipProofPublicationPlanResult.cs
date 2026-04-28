namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes generated tenant-domain ownership proof publication instructions.
/// </summary>
public sealed class TenantDomainOwnershipProofPublicationPlanResult
{
    /// <summary>
    /// Creates a tenant-domain ownership proof publication planning result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="domainName">The canonical domain name that was evaluated.</param>
    /// <param name="verificationMethod">The verification method used for publication planning.</param>
    /// <param name="outcome">The stable publication planning outcome.</param>
    /// <param name="planned">A value indicating whether publication instructions were generated.</param>
    /// <param name="recorded">A value indicating whether publication plan metadata was recorded.</param>
    /// <param name="plannedAtUtc">The UTC timestamp when publication planning executed.</param>
    /// <param name="proofValue">The public proof value to publish.</param>
    /// <param name="proofFingerprint">The SHA-256 fingerprint of the public proof value.</param>
    /// <param name="dnsTxtRecordName">The DNS TXT record name where the proof value should be published.</param>
    /// <param name="dnsTxtRecordValue">The DNS TXT record value to publish.</param>
    /// <param name="httpFilePath">The HTTP path where the proof file should be published.</param>
    /// <param name="httpFileContent">The HTTP file content to publish.</param>
    /// <param name="httpContentType">The HTTP content type to use when the proof is published as a file.</param>
    /// <param name="domainOwnership">The matching or resulting domain ownership descriptor when one exists.</param>
    /// <param name="reason">The operator-facing publication planning reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipProofPublicationPlanResult(
        string tenantId,
        string domainName,
        string? verificationMethod,
        string outcome,
        bool planned,
        bool recorded,
        DateTimeOffset plannedAtUtc,
        string? proofValue,
        string? proofFingerprint,
        string? dnsTxtRecordName,
        string? dnsTxtRecordValue,
        string? httpFilePath,
        string? httpFileContent,
        string? httpContentType,
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
        Planned = planned;
        Recorded = recorded;
        PlannedAtUtc = plannedAtUtc;
        ProofValue = string.IsNullOrWhiteSpace(proofValue) ? null : proofValue.Trim();
        ProofFingerprint = string.IsNullOrWhiteSpace(proofFingerprint) ? null : proofFingerprint.Trim();
        DnsTxtRecordName = string.IsNullOrWhiteSpace(dnsTxtRecordName) ? null : TenantDomainOwnershipDescriptor.NormalizeDomainName(dnsTxtRecordName);
        DnsTxtRecordValue = string.IsNullOrWhiteSpace(dnsTxtRecordValue) ? null : dnsTxtRecordValue.Trim();
        HttpFilePath = string.IsNullOrWhiteSpace(httpFilePath) ? null : NormalizeHttpFilePath(httpFilePath);
        HttpFileContent = string.IsNullOrWhiteSpace(httpFileContent) ? null : httpFileContent.Trim();
        HttpContentType = string.IsNullOrWhiteSpace(httpContentType) ? null : httpContentType.Trim();
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
    /// Gets the verification method used for publication planning.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the stable publication planning outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether publication instructions were generated.
    /// </summary>
    public bool Planned { get; }

    /// <summary>
    /// Gets a value indicating whether publication plan metadata was recorded.
    /// </summary>
    public bool Recorded { get; }

    /// <summary>
    /// Gets the UTC timestamp when publication planning executed.
    /// </summary>
    public DateTimeOffset PlannedAtUtc { get; }

    /// <summary>
    /// Gets the public proof value to publish.
    /// </summary>
    public string? ProofValue { get; }

    /// <summary>
    /// Gets the SHA-256 fingerprint of the public proof value.
    /// </summary>
    public string? ProofFingerprint { get; }

    /// <summary>
    /// Gets the DNS TXT record name where the proof value should be published.
    /// </summary>
    public string? DnsTxtRecordName { get; }

    /// <summary>
    /// Gets the DNS TXT record value to publish.
    /// </summary>
    public string? DnsTxtRecordValue { get; }

    /// <summary>
    /// Gets the HTTP path where the proof file should be published.
    /// </summary>
    public string? HttpFilePath { get; }

    /// <summary>
    /// Gets the HTTP file content to publish.
    /// </summary>
    public string? HttpFileContent { get; }

    /// <summary>
    /// Gets the HTTP content type to use when the proof is published as a file.
    /// </summary>
    public string? HttpContentType { get; }

    /// <summary>
    /// Gets the matching or resulting domain ownership descriptor when one exists.
    /// </summary>
    public TenantDomainOwnershipDescriptor? DomainOwnership { get; }

    /// <summary>
    /// Gets the operator-facing publication planning reason.
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
            TenantDomainOwnershipProofPublicationPlanOutcomes.Planned => TenantDomainOwnershipProofPublicationPlanOutcomes.Planned,
            TenantDomainOwnershipProofPublicationPlanOutcomes.Disabled => TenantDomainOwnershipProofPublicationPlanOutcomes.Disabled,
            TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound => TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound,
            TenantDomainOwnershipProofPublicationPlanOutcomes.TenantMismatch => TenantDomainOwnershipProofPublicationPlanOutcomes.TenantMismatch,
            TenantDomainOwnershipProofPublicationPlanOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofPublicationPlanOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofPublicationPlanOutcomes.MissingExpectedProof => TenantDomainOwnershipProofPublicationPlanOutcomes.MissingExpectedProof,
            TenantDomainOwnershipProofPublicationPlanOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipProofPublicationPlanOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipProofPublicationPlanOutcomes.StoreFailed => TenantDomainOwnershipProofPublicationPlanOutcomes.StoreFailed,
            _ => throw new ArgumentException($"Tenant-domain ownership proof publication planning outcome '{outcome}' is not supported.", nameof(outcome))
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
