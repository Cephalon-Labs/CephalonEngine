namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-domain ownership HTTP proof collection attempt.
/// </summary>
public sealed class TenantDomainOwnershipHttpProofCollectionResult
{
    /// <summary>
    /// Creates a tenant-domain ownership HTTP proof collection result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="domainName">The canonical domain name that was evaluated.</param>
    /// <param name="verificationMethod">The verification method used for collection.</param>
    /// <param name="outcome">The stable HTTP proof collection outcome.</param>
    /// <param name="collected">A value indicating whether HTTP proof content was collected.</param>
    /// <param name="evaluated">A value indicating whether proof evaluation reached a terminal workflow outcome.</param>
    /// <param name="collectedAtUtc">The UTC timestamp when collection executed.</param>
    /// <param name="collectionUri">The URI used to collect the HTTP proof.</param>
    /// <param name="statusCode">The HTTP status code returned by the proof endpoint.</param>
    /// <param name="contentLength">The collected HTTP proof response body length.</param>
    /// <param name="observedProofFingerprint">The SHA-256 fingerprint of the collected proof body.</param>
    /// <param name="publicationPlanResult">The publication-plan result used by collection.</param>
    /// <param name="evaluationResult">The proof-evaluation result produced after collection.</param>
    /// <param name="domainOwnership">The matching or resulting domain ownership descriptor when one exists.</param>
    /// <param name="reason">The operator-facing HTTP proof collection reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipHttpProofCollectionResult(
        string tenantId,
        string domainName,
        string? verificationMethod,
        string outcome,
        bool collected,
        bool evaluated,
        DateTimeOffset collectedAtUtc,
        Uri? collectionUri,
        int? statusCode,
        long? contentLength,
        string? observedProofFingerprint,
        TenantDomainOwnershipProofPublicationPlanResult? publicationPlanResult,
        TenantDomainOwnershipProofEvaluationResult? evaluationResult,
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
        VerificationMethod = string.IsNullOrWhiteSpace(verificationMethod) ? null : verificationMethod.Trim().ToLowerInvariant();
        Outcome = NormalizeOutcome(outcome);
        Collected = collected;
        Evaluated = evaluated;
        CollectedAtUtc = collectedAtUtc;
        CollectionUri = collectionUri;
        StatusCode = statusCode;
        ContentLength = contentLength;
        ObservedProofFingerprint = string.IsNullOrWhiteSpace(observedProofFingerprint) ? null : observedProofFingerprint.Trim();
        PublicationPlanResult = publicationPlanResult;
        EvaluationResult = evaluationResult;
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
    /// Gets the verification method used for collection.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the stable HTTP proof collection outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether HTTP proof content was collected.
    /// </summary>
    public bool Collected { get; }

    /// <summary>
    /// Gets a value indicating whether proof evaluation reached a terminal workflow outcome.
    /// </summary>
    public bool Evaluated { get; }

    /// <summary>
    /// Gets the UTC timestamp when collection executed.
    /// </summary>
    public DateTimeOffset CollectedAtUtc { get; }

    /// <summary>
    /// Gets the URI used to collect the HTTP proof.
    /// </summary>
    public Uri? CollectionUri { get; }

    /// <summary>
    /// Gets the HTTP status code returned by the proof endpoint.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the collected HTTP proof response body length.
    /// </summary>
    public long? ContentLength { get; }

    /// <summary>
    /// Gets the SHA-256 fingerprint of the collected proof body.
    /// </summary>
    public string? ObservedProofFingerprint { get; }

    /// <summary>
    /// Gets the publication-plan result used by collection.
    /// </summary>
    public TenantDomainOwnershipProofPublicationPlanResult? PublicationPlanResult { get; }

    /// <summary>
    /// Gets the proof-evaluation result produced after collection.
    /// </summary>
    public TenantDomainOwnershipProofEvaluationResult? EvaluationResult { get; }

    /// <summary>
    /// Gets the matching or resulting domain ownership descriptor when one exists.
    /// </summary>
    public TenantDomainOwnershipDescriptor? DomainOwnership { get; }

    /// <summary>
    /// Gets the operator-facing HTTP proof collection reason.
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
            TenantDomainOwnershipHttpProofCollectionOutcomes.Collected => TenantDomainOwnershipHttpProofCollectionOutcomes.Collected,
            TenantDomainOwnershipHttpProofCollectionOutcomes.Disabled => TenantDomainOwnershipHttpProofCollectionOutcomes.Disabled,
            TenantDomainOwnershipHttpProofCollectionOutcomes.NotFound => TenantDomainOwnershipHttpProofCollectionOutcomes.NotFound,
            TenantDomainOwnershipHttpProofCollectionOutcomes.TenantMismatch => TenantDomainOwnershipHttpProofCollectionOutcomes.TenantMismatch,
            TenantDomainOwnershipHttpProofCollectionOutcomes.VerificationMethodMismatch => TenantDomainOwnershipHttpProofCollectionOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipHttpProofCollectionOutcomes.MissingExpectedProof => TenantDomainOwnershipHttpProofCollectionOutcomes.MissingExpectedProof,
            TenantDomainOwnershipHttpProofCollectionOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipHttpProofCollectionOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipHttpProofCollectionOutcomes.StoreFailed => TenantDomainOwnershipHttpProofCollectionOutcomes.StoreFailed,
            TenantDomainOwnershipHttpProofCollectionOutcomes.MissingPublicationPlan => TenantDomainOwnershipHttpProofCollectionOutcomes.MissingPublicationPlan,
            TenantDomainOwnershipHttpProofCollectionOutcomes.InvalidUri => TenantDomainOwnershipHttpProofCollectionOutcomes.InvalidUri,
            TenantDomainOwnershipHttpProofCollectionOutcomes.RequestFailed => TenantDomainOwnershipHttpProofCollectionOutcomes.RequestFailed,
            TenantDomainOwnershipHttpProofCollectionOutcomes.UnexpectedStatusCode => TenantDomainOwnershipHttpProofCollectionOutcomes.UnexpectedStatusCode,
            TenantDomainOwnershipHttpProofCollectionOutcomes.EmptyResponse => TenantDomainOwnershipHttpProofCollectionOutcomes.EmptyResponse,
            TenantDomainOwnershipHttpProofCollectionOutcomes.ResponseTooLarge => TenantDomainOwnershipHttpProofCollectionOutcomes.ResponseTooLarge,
            TenantDomainOwnershipHttpProofCollectionOutcomes.EvaluationFailed => TenantDomainOwnershipHttpProofCollectionOutcomes.EvaluationFailed,
            _ => throw new ArgumentException($"Tenant-domain ownership HTTP proof collection outcome '{outcome}' is not supported.", nameof(outcome))
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
