namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable tenant-domain ownership HTTP proof collection outcome labels.
/// </summary>
public static class TenantDomainOwnershipHttpProofCollectionOutcomes
{
    /// <summary>
    /// HTTP proof content was collected and proof evaluation reached a terminal workflow outcome.
    /// </summary>
    public const string Collected = "collected";

    /// <summary>
    /// HTTP proof collection is disabled by governance options.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// No tenant-domain ownership declaration matched the supplied tenant and domain.
    /// </summary>
    public const string NotFound = "not-found";

    /// <summary>
    /// A declaration for the supplied domain belongs to a different tenant.
    /// </summary>
    public const string TenantMismatch = "tenant-mismatch";

    /// <summary>
    /// The matching domain ownership declaration uses a different verification method.
    /// </summary>
    public const string VerificationMethodMismatch = "verification-method-mismatch";

    /// <summary>
    /// Expected proof metadata is missing from the tenant-domain ownership declaration.
    /// </summary>
    public const string MissingExpectedProof = "missing-expected-proof";

    /// <summary>
    /// The verification method cannot be collected by the built-in HTTP proof collector.
    /// </summary>
    public const string UnsupportedVerificationMethod = "unsupported-verification-method";

    /// <summary>
    /// Publication-plan metadata could not be recorded before collection.
    /// </summary>
    public const string StoreFailed = "store-failed";

    /// <summary>
    /// Publication planning did not provide an HTTP file path and expected proof content.
    /// </summary>
    public const string MissingPublicationPlan = "missing-publication-plan";

    /// <summary>
    /// The resolved HTTP proof collection URI is invalid or unsafe for the requested domain.
    /// </summary>
    public const string InvalidUri = "invalid-uri";

    /// <summary>
    /// The HTTP proof endpoint could not be reached or timed out.
    /// </summary>
    public const string RequestFailed = "request-failed";

    /// <summary>
    /// The HTTP proof endpoint returned a non-success status code.
    /// </summary>
    public const string UnexpectedStatusCode = "unexpected-status-code";

    /// <summary>
    /// The HTTP proof endpoint returned an empty response body.
    /// </summary>
    public const string EmptyResponse = "empty-response";

    /// <summary>
    /// The HTTP proof endpoint response body exceeded the configured collection size limit.
    /// </summary>
    public const string ResponseTooLarge = "response-too-large";

    /// <summary>
    /// HTTP content was collected, but proof evaluation did not apply a terminal workflow outcome.
    /// </summary>
    public const string EvaluationFailed = "evaluation-failed";
}
