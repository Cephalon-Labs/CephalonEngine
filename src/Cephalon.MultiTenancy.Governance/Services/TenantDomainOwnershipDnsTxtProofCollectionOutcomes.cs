namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable tenant-domain ownership DNS TXT proof collection outcome labels.
/// </summary>
public static class TenantDomainOwnershipDnsTxtProofCollectionOutcomes
{
    /// <summary>
    /// DNS TXT proof content was collected and proof evaluation reached a terminal workflow outcome.
    /// </summary>
    public const string Collected = "collected";

    /// <summary>
    /// DNS TXT proof collection is disabled by governance options.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// DNS TXT proof collection has no configured resolver endpoint.
    /// </summary>
    public const string ResolverNotConfigured = "resolver-not-configured";

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
    /// The verification method cannot be collected by the built-in DNS TXT proof collector.
    /// </summary>
    public const string UnsupportedVerificationMethod = "unsupported-verification-method";

    /// <summary>
    /// Publication-plan metadata could not be recorded before collection.
    /// </summary>
    public const string StoreFailed = "store-failed";

    /// <summary>
    /// Publication planning did not provide a DNS TXT record name and expected proof value.
    /// </summary>
    public const string MissingPublicationPlan = "missing-publication-plan";

    /// <summary>
    /// The resolved DNS TXT proof resolver URI is invalid or unsafe.
    /// </summary>
    public const string InvalidResolverUri = "invalid-resolver-uri";

    /// <summary>
    /// The DNS TXT proof resolver could not be reached or timed out.
    /// </summary>
    public const string RequestFailed = "request-failed";

    /// <summary>
    /// The DNS TXT proof resolver returned a non-success status code.
    /// </summary>
    public const string UnexpectedStatusCode = "unexpected-status-code";

    /// <summary>
    /// The DNS TXT proof resolver returned an empty response body.
    /// </summary>
    public const string EmptyResponse = "empty-response";

    /// <summary>
    /// The DNS TXT proof resolver response body exceeded the configured collection size limit.
    /// </summary>
    public const string ResponseTooLarge = "response-too-large";

    /// <summary>
    /// The DNS TXT proof resolver response could not be parsed as a DNS JSON response.
    /// </summary>
    public const string InvalidResponse = "invalid-response";

    /// <summary>
    /// No DNS TXT answer was returned for the planned proof record.
    /// </summary>
    public const string NoTxtRecords = "no-txt-records";

    /// <summary>
    /// DNS TXT answers were returned, but none matched the expected proof value.
    /// </summary>
    public const string NoMatchingTxtRecord = "no-matching-txt-record";

    /// <summary>
    /// DNS TXT content was collected, but proof evaluation did not apply a terminal workflow outcome.
    /// </summary>
    public const string EvaluationFailed = "evaluation-failed";
}
