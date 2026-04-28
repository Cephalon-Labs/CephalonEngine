namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable tenant-domain ownership HTTP proof publication outcomes.
/// </summary>
public static class TenantDomainOwnershipHttpProofPublicationOutcomes
{
    /// <summary>
    /// The HTTP proof file was materialized and recorded.
    /// </summary>
    public const string Published = "published";

    /// <summary>
    /// HTTP proof publication was disabled.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// The proof publication planner did not produce a usable plan.
    /// </summary>
    public const string PublicationPlanUnavailable = "publication-plan-unavailable";

    /// <summary>
    /// The publication plan did not include HTTP proof-file instructions.
    /// </summary>
    public const string MissingHttpFilePublicationPlan = "missing-http-file-publication-plan";

    /// <summary>
    /// Publication state could not be stored.
    /// </summary>
    public const string StoreFailed = "store-failed";
}
