namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-membership evaluation.
/// </summary>
public sealed class TenantMembershipEvaluationResult
{
    /// <summary>
    /// Creates a tenant-membership evaluation result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="principalId">The principal identifier that was evaluated.</param>
    /// <param name="outcome">The stable evaluation outcome.</param>
    /// <param name="allowed">A value indicating whether evaluation granted access.</param>
    /// <param name="evaluatedAtUtc">The UTC timestamp when evaluation executed.</param>
    /// <param name="requiredRoles">The tenant-local roles required by the request.</param>
    /// <param name="matchedRoles">The tenant-local roles found on active memberships.</param>
    /// <param name="missingRoles">The required roles that were not found.</param>
    /// <param name="matchedMemberships">The matching memberships considered by evaluation.</param>
    /// <param name="reason">The optional operator-facing evaluation reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    /// <param name="principalKind">The principal kind that was evaluated. The default is user.</param>
    public TenantMembershipEvaluationResult(
        string tenantId,
        string principalId,
        string outcome,
        bool allowed,
        DateTimeOffset evaluatedAtUtc,
        IReadOnlyList<string>? requiredRoles = null,
        IReadOnlyList<string>? matchedRoles = null,
        IReadOnlyList<string>? missingRoles = null,
        IReadOnlyList<TenantMembershipDescriptor>? matchedMemberships = null,
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? principalKind = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(principalId))
        {
            throw new ArgumentException("Principal id is required.", nameof(principalId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        TenantId = tenantId.Trim();
        PrincipalId = principalId.Trim();
        PrincipalKind = string.IsNullOrWhiteSpace(principalKind) ? "user" : principalKind.Trim();
        Outcome = NormalizeOutcome(outcome);
        Allowed = allowed;
        EvaluatedAtUtc = evaluatedAtUtc;
        RequiredRoles = NormalizeValues(requiredRoles);
        MatchedRoles = NormalizeValues(matchedRoles);
        MissingRoles = NormalizeValues(missingRoles);
        MatchedMemberships = matchedMemberships?
            .Where(static membership => membership is not null)
            .OrderBy(static membership => membership.PrincipalKind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static membership => membership.PrincipalId, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that was evaluated.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the principal identifier that was evaluated.
    /// </summary>
    public string PrincipalId { get; }

    /// <summary>
    /// Gets the principal kind that was evaluated.
    /// </summary>
    public string PrincipalKind { get; }

    /// <summary>
    /// Gets the stable evaluation outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether evaluation granted access.
    /// </summary>
    public bool Allowed { get; }

    /// <summary>
    /// Gets the UTC timestamp when evaluation executed.
    /// </summary>
    public DateTimeOffset EvaluatedAtUtc { get; }

    /// <summary>
    /// Gets the tenant-local roles required by the request.
    /// </summary>
    public IReadOnlyList<string> RequiredRoles { get; }

    /// <summary>
    /// Gets the tenant-local roles found on active memberships.
    /// </summary>
    public IReadOnlyList<string> MatchedRoles { get; }

    /// <summary>
    /// Gets the required roles that were not found.
    /// </summary>
    public IReadOnlyList<string> MissingRoles { get; }

    /// <summary>
    /// Gets the matching memberships considered by evaluation.
    /// </summary>
    public IReadOnlyList<TenantMembershipDescriptor> MatchedMemberships { get; }

    /// <summary>
    /// Gets the optional operator-facing evaluation reason.
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
            TenantMembershipEvaluationOutcomes.Allowed => TenantMembershipEvaluationOutcomes.Allowed,
            TenantMembershipEvaluationOutcomes.NoMembership => TenantMembershipEvaluationOutcomes.NoMembership,
            TenantMembershipEvaluationOutcomes.Suspended => TenantMembershipEvaluationOutcomes.Suspended,
            TenantMembershipEvaluationOutcomes.Expired => TenantMembershipEvaluationOutcomes.Expired,
            TenantMembershipEvaluationOutcomes.MissingRole => TenantMembershipEvaluationOutcomes.MissingRole,
            TenantMembershipEvaluationOutcomes.Disabled => TenantMembershipEvaluationOutcomes.Disabled,
            _ => throw new ArgumentException($"Tenant-membership evaluation outcome '{outcome}' is not supported.", nameof(outcome))
        };
    }

    private static string[] NormalizeValues(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
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
