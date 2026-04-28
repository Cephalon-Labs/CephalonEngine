namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of tenant-domain ownership HTTP proof publication.
/// </summary>
public sealed class TenantDomainOwnershipHttpProofPublicationResult
{
    /// <summary>
    /// Creates a tenant-domain ownership HTTP proof publication result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="domainName">The canonical domain name that was evaluated.</param>
    /// <param name="outcome">The stable HTTP proof publication outcome.</param>
    /// <param name="published">A value indicating whether the HTTP proof file was materialized.</param>
    /// <param name="recorded">A value indicating whether publication metadata was recorded.</param>
    /// <param name="publishedAtUtc">The UTC timestamp used for publication.</param>
    /// <param name="httpFilePath">The HTTP path where the proof file is served.</param>
    /// <param name="httpFileContent">The public proof-file content.</param>
    /// <param name="httpContentType">The content type used when serving the proof file.</param>
    /// <param name="proofFingerprint">The SHA-256 fingerprint of the proof-file content.</param>
    /// <param name="domainOwnership">The resulting domain ownership descriptor when one exists.</param>
    /// <param name="reason">The operator-facing HTTP proof publication reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipHttpProofPublicationResult(
        string tenantId,
        string domainName,
        string outcome,
        bool published,
        bool recorded,
        DateTimeOffset publishedAtUtc,
        string? httpFilePath,
        string? httpFileContent,
        string? httpContentType,
        string? proofFingerprint,
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
        Outcome = NormalizeOutcome(outcome);
        Published = published;
        Recorded = recorded;
        PublishedAtUtc = publishedAtUtc;
        HttpFilePath = string.IsNullOrWhiteSpace(httpFilePath)
            ? null
            : TenantDomainOwnershipHttpProofPublicationDescriptor.NormalizeHttpFilePath(httpFilePath);
        HttpFileContent = string.IsNullOrWhiteSpace(httpFileContent) ? null : httpFileContent.Trim();
        HttpContentType = string.IsNullOrWhiteSpace(httpContentType) ? null : httpContentType.Trim();
        ProofFingerprint = string.IsNullOrWhiteSpace(proofFingerprint) ? null : proofFingerprint.Trim();
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
    /// Gets the stable HTTP proof publication outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the HTTP proof file was materialized.
    /// </summary>
    public bool Published { get; }

    /// <summary>
    /// Gets a value indicating whether publication metadata was recorded.
    /// </summary>
    public bool Recorded { get; }

    /// <summary>
    /// Gets the UTC timestamp used for publication.
    /// </summary>
    public DateTimeOffset PublishedAtUtc { get; }

    /// <summary>
    /// Gets the HTTP path where the proof file is served.
    /// </summary>
    public string? HttpFilePath { get; }

    /// <summary>
    /// Gets the public proof-file content.
    /// </summary>
    public string? HttpFileContent { get; }

    /// <summary>
    /// Gets the content type used when serving the proof file.
    /// </summary>
    public string? HttpContentType { get; }

    /// <summary>
    /// Gets the SHA-256 fingerprint of the proof-file content.
    /// </summary>
    public string? ProofFingerprint { get; }

    /// <summary>
    /// Gets the resulting domain ownership descriptor when one exists.
    /// </summary>
    public TenantDomainOwnershipDescriptor? DomainOwnership { get; }

    /// <summary>
    /// Gets the operator-facing HTTP proof publication reason.
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
            TenantDomainOwnershipHttpProofPublicationOutcomes.Published => TenantDomainOwnershipHttpProofPublicationOutcomes.Published,
            TenantDomainOwnershipHttpProofPublicationOutcomes.Disabled => TenantDomainOwnershipHttpProofPublicationOutcomes.Disabled,
            TenantDomainOwnershipHttpProofPublicationOutcomes.PublicationPlanUnavailable => TenantDomainOwnershipHttpProofPublicationOutcomes.PublicationPlanUnavailable,
            TenantDomainOwnershipHttpProofPublicationOutcomes.MissingHttpFilePublicationPlan => TenantDomainOwnershipHttpProofPublicationOutcomes.MissingHttpFilePublicationPlan,
            TenantDomainOwnershipHttpProofPublicationOutcomes.StoreFailed => TenantDomainOwnershipHttpProofPublicationOutcomes.StoreFailed,
            _ => throw new ArgumentException($"Tenant-domain ownership HTTP proof publication outcome '{outcome}' is not supported.", nameof(outcome))
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
