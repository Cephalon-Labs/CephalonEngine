namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-domain ownership validation.
/// </summary>
public sealed class TenantDomainOwnershipValidationResult
{
    /// <summary>
    /// Creates a tenant-domain ownership validation result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was validated.</param>
    /// <param name="domainName">The canonical domain name that was validated.</param>
    /// <param name="outcome">The stable validation outcome.</param>
    /// <param name="valid">A value indicating whether validation granted domain ownership use.</param>
    /// <param name="validatedAtUtc">The UTC timestamp when validation executed.</param>
    /// <param name="matchedDomainOwnership">The matching domain ownership descriptor considered by validation.</param>
    /// <param name="reason">The optional operator-facing validation reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipValidationResult(
        string tenantId,
        string domainName,
        string outcome,
        bool valid,
        DateTimeOffset validatedAtUtc,
        TenantDomainOwnershipDescriptor? matchedDomainOwnership = null,
        string? reason = null,
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
        Valid = valid;
        ValidatedAtUtc = validatedAtUtc;
        MatchedDomainOwnership = matchedDomainOwnership;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that was validated.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the canonical domain name that was validated.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the stable validation outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether validation granted domain ownership use.
    /// </summary>
    public bool Valid { get; }

    /// <summary>
    /// Gets the UTC timestamp when validation executed.
    /// </summary>
    public DateTimeOffset ValidatedAtUtc { get; }

    /// <summary>
    /// Gets the matching domain ownership descriptor considered by validation.
    /// </summary>
    public TenantDomainOwnershipDescriptor? MatchedDomainOwnership { get; }

    /// <summary>
    /// Gets the optional operator-facing validation reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets optional result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantDomainOwnershipValidationOutcomes.Valid => TenantDomainOwnershipValidationOutcomes.Valid,
            TenantDomainOwnershipValidationOutcomes.NotFound => TenantDomainOwnershipValidationOutcomes.NotFound,
            TenantDomainOwnershipValidationOutcomes.TenantMismatch => TenantDomainOwnershipValidationOutcomes.TenantMismatch,
            TenantDomainOwnershipValidationOutcomes.Pending => TenantDomainOwnershipValidationOutcomes.Pending,
            TenantDomainOwnershipValidationOutcomes.Rejected => TenantDomainOwnershipValidationOutcomes.Rejected,
            TenantDomainOwnershipValidationOutcomes.Suspended => TenantDomainOwnershipValidationOutcomes.Suspended,
            TenantDomainOwnershipValidationOutcomes.Expired => TenantDomainOwnershipValidationOutcomes.Expired,
            TenantDomainOwnershipValidationOutcomes.Disabled => TenantDomainOwnershipValidationOutcomes.Disabled,
            _ => throw new ArgumentException($"Tenant-domain ownership validation outcome '{outcome}' is not supported.", nameof(outcome))
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
