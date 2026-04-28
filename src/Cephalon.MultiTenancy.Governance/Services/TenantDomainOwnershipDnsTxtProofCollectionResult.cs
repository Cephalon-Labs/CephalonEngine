namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-domain ownership DNS TXT proof collection attempt.
/// </summary>
public sealed class TenantDomainOwnershipDnsTxtProofCollectionResult
{
    /// <summary>
    /// Creates a tenant-domain ownership DNS TXT proof collection result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="domainName">The canonical domain name that was evaluated.</param>
    /// <param name="verificationMethod">The verification method used for collection.</param>
    /// <param name="outcome">The stable DNS TXT proof collection outcome.</param>
    /// <param name="collected">A value indicating whether DNS TXT proof content was collected.</param>
    /// <param name="evaluated">A value indicating whether proof evaluation reached a terminal workflow outcome.</param>
    /// <param name="collectedAtUtc">The UTC timestamp when collection executed.</param>
    /// <param name="resolverUri">The resolver URI used to collect the DNS TXT proof.</param>
    /// <param name="dnsTxtRecordName">The DNS TXT record name queried during collection.</param>
    /// <param name="statusCode">The HTTP status code returned by the DNS TXT resolver.</param>
    /// <param name="contentLength">The collected DNS TXT resolver response body length.</param>
    /// <param name="observedTxtRecordCount">The number of TXT answers observed by collection.</param>
    /// <param name="observedProofFingerprint">The SHA-256 fingerprint of the matching collected TXT proof.</param>
    /// <param name="publicationPlanResult">The publication-plan result used by collection.</param>
    /// <param name="evaluationResult">The proof-evaluation result produced after collection.</param>
    /// <param name="domainOwnership">The matching or resulting domain ownership descriptor when one exists.</param>
    /// <param name="reason">The operator-facing DNS TXT proof collection reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipDnsTxtProofCollectionResult(
        string tenantId,
        string domainName,
        string? verificationMethod,
        string outcome,
        bool collected,
        bool evaluated,
        DateTimeOffset collectedAtUtc,
        Uri? resolverUri,
        string? dnsTxtRecordName,
        int? statusCode,
        long? contentLength,
        int observedTxtRecordCount,
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
        ResolverUri = resolverUri;
        DnsTxtRecordName = string.IsNullOrWhiteSpace(dnsTxtRecordName) ? null : TenantDomainOwnershipDescriptor.NormalizeDomainName(dnsTxtRecordName);
        StatusCode = statusCode;
        ContentLength = contentLength;
        ObservedTxtRecordCount = observedTxtRecordCount < 0 ? 0 : observedTxtRecordCount;
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
    /// Gets the stable DNS TXT proof collection outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether DNS TXT proof content was collected.
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
    /// Gets the resolver URI used to collect the DNS TXT proof.
    /// </summary>
    public Uri? ResolverUri { get; }

    /// <summary>
    /// Gets the DNS TXT record name queried during collection.
    /// </summary>
    public string? DnsTxtRecordName { get; }

    /// <summary>
    /// Gets the HTTP status code returned by the DNS TXT resolver.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the collected DNS TXT resolver response body length.
    /// </summary>
    public long? ContentLength { get; }

    /// <summary>
    /// Gets the number of TXT answers observed by collection.
    /// </summary>
    public int ObservedTxtRecordCount { get; }

    /// <summary>
    /// Gets the SHA-256 fingerprint of the matching collected TXT proof.
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
    /// Gets the operator-facing DNS TXT proof collection reason.
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
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Disabled => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Disabled,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResolverNotConfigured => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResolverNotConfigured,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NotFound => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NotFound,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.TenantMismatch => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.TenantMismatch,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.VerificationMethodMismatch => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingExpectedProof => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingExpectedProof,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.StoreFailed => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.StoreFailed,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingPublicationPlan => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingPublicationPlan,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.InvalidResolverUri => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.InvalidResolverUri,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.RequestFailed => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.RequestFailed,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnexpectedStatusCode => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnexpectedStatusCode,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.EmptyResponse => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.EmptyResponse,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResponseTooLarge => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResponseTooLarge,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.InvalidResponse => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.InvalidResponse,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NoTxtRecords => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NoTxtRecords,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NoMatchingTxtRecord => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NoMatchingTxtRecord,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.EvaluationFailed => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.EvaluationFailed,
            _ => throw new ArgumentException($"Tenant-domain ownership DNS TXT proof collection outcome '{outcome}' is not supported.", nameof(outcome))
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
