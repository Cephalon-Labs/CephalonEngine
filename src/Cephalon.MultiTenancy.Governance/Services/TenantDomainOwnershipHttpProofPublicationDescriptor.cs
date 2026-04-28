namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one tenant-domain ownership HTTP proof file published by Cephalon governance.
/// </summary>
public sealed class TenantDomainOwnershipHttpProofPublicationDescriptor
{
    /// <summary>
    /// Creates a tenant-domain ownership HTTP proof publication descriptor.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the domain declaration.</param>
    /// <param name="domainName">The canonical domain name that should serve the proof file.</param>
    /// <param name="httpFilePath">The HTTP path where the proof file is served.</param>
    /// <param name="httpFileContent">The public proof-file content.</param>
    /// <param name="httpContentType">The content type used when serving the proof file.</param>
    /// <param name="proofFingerprint">The SHA-256 fingerprint of the proof-file content.</param>
    /// <param name="publishedAtUtc">The UTC timestamp when publication was recorded.</param>
    /// <param name="metadata">Optional publication metadata.</param>
    public TenantDomainOwnershipHttpProofPublicationDescriptor(
        string tenantId,
        string domainName,
        string httpFilePath,
        string httpFileContent,
        string httpContentType,
        string proofFingerprint,
        DateTimeOffset publishedAtUtc,
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

        if (string.IsNullOrWhiteSpace(httpFilePath))
        {
            throw new ArgumentException("HTTP file path is required.", nameof(httpFilePath));
        }

        if (string.IsNullOrWhiteSpace(httpFileContent))
        {
            throw new ArgumentException("HTTP file content is required.", nameof(httpFileContent));
        }

        if (string.IsNullOrWhiteSpace(httpContentType))
        {
            throw new ArgumentException("HTTP content type is required.", nameof(httpContentType));
        }

        if (string.IsNullOrWhiteSpace(proofFingerprint))
        {
            throw new ArgumentException("Proof fingerprint is required.", nameof(proofFingerprint));
        }

        TenantId = tenantId.Trim();
        DomainName = TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName);
        HttpFilePath = NormalizeHttpFilePath(httpFilePath);
        HttpFileContent = httpFileContent.Trim();
        HttpContentType = httpContentType.Trim();
        ProofFingerprint = proofFingerprint.Trim();
        PublishedAtUtc = publishedAtUtc;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the domain declaration.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name that should serve the proof file.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the HTTP path where the proof file is served.
    /// </summary>
    public string HttpFilePath { get; }

    /// <summary>
    /// Gets the public proof-file content.
    /// </summary>
    public string HttpFileContent { get; }

    /// <summary>
    /// Gets the content type used when serving the proof file.
    /// </summary>
    public string HttpContentType { get; }

    /// <summary>
    /// Gets the SHA-256 fingerprint of the proof-file content.
    /// </summary>
    public string ProofFingerprint { get; }

    /// <summary>
    /// Gets the UTC timestamp when publication was recorded.
    /// </summary>
    public DateTimeOffset PublishedAtUtc { get; }

    /// <summary>
    /// Gets optional publication metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    internal static string NormalizeHttpFilePath(string httpFilePath)
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
