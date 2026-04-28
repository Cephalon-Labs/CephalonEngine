namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-governance action decision.
/// </summary>
public sealed class TenantGovernanceActionDecisionResult
{
    /// <summary>
    /// Creates a tenant-governance action decision result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="actionId">The governance action identifier that was evaluated.</param>
    /// <param name="outcome">The stable decision outcome.</param>
    /// <param name="allowed">A value indicating whether the governance action can proceed.</param>
    /// <param name="decidedAtUtc">The UTC timestamp when decision evaluation executed.</param>
    /// <param name="matchedAction">The matching governance action descriptor considered by decision evaluation.</param>
    /// <param name="reason">The optional operator-facing decision reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantGovernanceActionDecisionResult(
        string tenantId,
        string actionId,
        string outcome,
        bool allowed,
        DateTimeOffset decidedAtUtc,
        TenantGovernanceActionDescriptor? matchedAction = null,
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(actionId))
        {
            throw new ArgumentException("Action id is required.", nameof(actionId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        TenantId = tenantId.Trim();
        ActionId = actionId.Trim();
        Outcome = NormalizeOutcome(outcome);
        Allowed = allowed;
        DecidedAtUtc = decidedAtUtc;
        MatchedAction = matchedAction;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that was evaluated.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the governance action identifier that was evaluated.
    /// </summary>
    public string ActionId { get; }

    /// <summary>
    /// Gets the stable decision outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the governance action can proceed.
    /// </summary>
    public bool Allowed { get; }

    /// <summary>
    /// Gets the UTC timestamp when decision evaluation executed.
    /// </summary>
    public DateTimeOffset DecidedAtUtc { get; }

    /// <summary>
    /// Gets the matching governance action descriptor considered by decision evaluation.
    /// </summary>
    public TenantGovernanceActionDescriptor? MatchedAction { get; }

    /// <summary>
    /// Gets the optional operator-facing decision reason.
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
            TenantGovernanceActionDecisionOutcomes.Allowed => TenantGovernanceActionDecisionOutcomes.Allowed,
            TenantGovernanceActionDecisionOutcomes.NotFound => TenantGovernanceActionDecisionOutcomes.NotFound,
            TenantGovernanceActionDecisionOutcomes.TenantMismatch => TenantGovernanceActionDecisionOutcomes.TenantMismatch,
            TenantGovernanceActionDecisionOutcomes.ActionKindMismatch => TenantGovernanceActionDecisionOutcomes.ActionKindMismatch,
            TenantGovernanceActionDecisionOutcomes.SubjectMismatch => TenantGovernanceActionDecisionOutcomes.SubjectMismatch,
            TenantGovernanceActionDecisionOutcomes.PendingApproval => TenantGovernanceActionDecisionOutcomes.PendingApproval,
            TenantGovernanceActionDecisionOutcomes.Rejected => TenantGovernanceActionDecisionOutcomes.Rejected,
            TenantGovernanceActionDecisionOutcomes.RemediationRequired => TenantGovernanceActionDecisionOutcomes.RemediationRequired,
            TenantGovernanceActionDecisionOutcomes.Expired => TenantGovernanceActionDecisionOutcomes.Expired,
            TenantGovernanceActionDecisionOutcomes.Disabled => TenantGovernanceActionDecisionOutcomes.Disabled,
            _ => throw new ArgumentException($"Tenant-governance action decision outcome '{outcome}' is not supported.", nameof(outcome))
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
