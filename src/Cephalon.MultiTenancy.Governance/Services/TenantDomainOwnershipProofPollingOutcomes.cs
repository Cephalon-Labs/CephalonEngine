namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable tenant-domain ownership proof polling outcome labels.
/// </summary>
public static class TenantDomainOwnershipProofPollingOutcomes
{
    /// <summary>
    /// At least one matching domain ownership declaration was polled and all attempts reached a terminal or non-failing outcome.
    /// </summary>
    public const string Completed = "completed";

    /// <summary>
    /// At least one matching domain ownership declaration was polled, but one or more attempts could not complete.
    /// </summary>
    public const string PartialFailure = "partial-failure";

    /// <summary>
    /// No matching domain ownership declarations needed a polling attempt.
    /// </summary>
    public const string NoCandidates = "no-candidates";

    /// <summary>
    /// Proof polling is disabled by governance options.
    /// </summary>
    public const string Disabled = "disabled";
}
