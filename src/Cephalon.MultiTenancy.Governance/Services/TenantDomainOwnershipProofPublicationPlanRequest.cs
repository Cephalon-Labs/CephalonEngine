namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a tenant-domain ownership proof publication planning request.
/// </summary>
public sealed class TenantDomainOwnershipProofPublicationPlanRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership proof publication planning request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the domain declaration.</param>
    /// <param name="domainName">The domain name that should receive publication instructions.</param>
    /// <param name="verificationMethod">The optional verification method boundary.</param>
    /// <param name="source">The source that requested publication planning.</param>
    /// <param name="actor">The actor that requested publication planning when known.</param>
    /// <param name="atUtc">The UTC timestamp used for publication planning. The runtime clock is used when omitted.</param>
    /// <param name="correlationId">The optional correlation identifier for publication planning.</param>
    /// <param name="recordPlan">A value indicating whether the plan should be recorded in domain ownership metadata.</param>
    /// <param name="metadata">Optional proof publication planning metadata.</param>
    public TenantDomainOwnershipProofPublicationPlanRequest(
        string tenantId,
        string domainName,
        string? verificationMethod = null,
        string? source = null,
        string? actor = null,
        DateTimeOffset? atUtc = null,
        string? correlationId = null,
        bool recordPlan = true,
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
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        AtUtc = atUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        RecordPlan = recordPlan;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the domain declaration.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name that should receive publication instructions.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the optional verification method boundary.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the source that requested publication planning.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested publication planning when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the UTC timestamp used for publication planning.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for publication planning.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether the plan should be recorded in domain ownership metadata.
    /// </summary>
    public bool RecordPlan { get; }

    /// <summary>
    /// Gets optional proof publication planning metadata.
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
