namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-domain ownership proof verification runner attempt.
/// </summary>
public sealed class TenantDomainOwnershipProofVerificationResult
{
    /// <summary>
    /// Creates a tenant-domain ownership proof verification runner result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="domainName">The canonical domain name that was evaluated.</param>
    /// <param name="verificationMethod">The verification method used by the runner.</param>
    /// <param name="outcome">The stable proof verification runner outcome.</param>
    /// <param name="verified">A value indicating whether the run verified the declaration.</param>
    /// <param name="rejected">A value indicating whether the run rejected the declaration.</param>
    /// <param name="challengeIssued">A value indicating whether the run issued a proof challenge.</param>
    /// <param name="publicationPlanned">A value indicating whether the run generated publication instructions.</param>
    /// <param name="proofCollected">A value indicating whether the run collected proof content.</param>
    /// <param name="proofEvaluated">A value indicating whether the run evaluated observed proof.</param>
    /// <param name="ranAtUtc">The UTC timestamp when the runner executed.</param>
    /// <param name="challengeResult">The nested proof challenge result when one ran.</param>
    /// <param name="publicationPlanResult">The nested publication plan result when one ran.</param>
    /// <param name="httpProofCollectionResult">The nested HTTP proof collection result when one ran.</param>
    /// <param name="dnsTxtProofCollectionResult">The nested DNS TXT proof collection result when one ran.</param>
    /// <param name="evaluationResult">The nested proof evaluation result when one ran.</param>
    /// <param name="domainOwnership">The matching or resulting domain ownership descriptor when one exists.</param>
    /// <param name="reason">The operator-facing proof verification reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipProofVerificationResult(
        string tenantId,
        string domainName,
        string? verificationMethod,
        string outcome,
        bool verified,
        bool rejected,
        bool challengeIssued,
        bool publicationPlanned,
        bool proofCollected,
        bool proofEvaluated,
        DateTimeOffset ranAtUtc,
        TenantDomainOwnershipProofChallengeResult? challengeResult,
        TenantDomainOwnershipProofPublicationPlanResult? publicationPlanResult,
        TenantDomainOwnershipHttpProofCollectionResult? httpProofCollectionResult,
        TenantDomainOwnershipDnsTxtProofCollectionResult? dnsTxtProofCollectionResult,
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
        Verified = verified;
        Rejected = rejected;
        ChallengeIssued = challengeIssued;
        PublicationPlanned = publicationPlanned;
        ProofCollected = proofCollected;
        ProofEvaluated = proofEvaluated;
        RanAtUtc = ranAtUtc;
        ChallengeResult = challengeResult;
        PublicationPlanResult = publicationPlanResult;
        HttpProofCollectionResult = httpProofCollectionResult;
        DnsTxtProofCollectionResult = dnsTxtProofCollectionResult;
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
    /// Gets the verification method used by the runner.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the stable proof verification runner outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the run verified the declaration.
    /// </summary>
    public bool Verified { get; }

    /// <summary>
    /// Gets a value indicating whether the run rejected the declaration.
    /// </summary>
    public bool Rejected { get; }

    /// <summary>
    /// Gets a value indicating whether the run issued a proof challenge.
    /// </summary>
    public bool ChallengeIssued { get; }

    /// <summary>
    /// Gets a value indicating whether the run generated publication instructions.
    /// </summary>
    public bool PublicationPlanned { get; }

    /// <summary>
    /// Gets a value indicating whether the run collected proof content.
    /// </summary>
    public bool ProofCollected { get; }

    /// <summary>
    /// Gets a value indicating whether the run evaluated observed proof.
    /// </summary>
    public bool ProofEvaluated { get; }

    /// <summary>
    /// Gets the UTC timestamp when the runner executed.
    /// </summary>
    public DateTimeOffset RanAtUtc { get; }

    /// <summary>
    /// Gets the nested proof challenge result when one ran.
    /// </summary>
    public TenantDomainOwnershipProofChallengeResult? ChallengeResult { get; }

    /// <summary>
    /// Gets the nested publication plan result when one ran.
    /// </summary>
    public TenantDomainOwnershipProofPublicationPlanResult? PublicationPlanResult { get; }

    /// <summary>
    /// Gets the nested HTTP proof collection result when one ran.
    /// </summary>
    public TenantDomainOwnershipHttpProofCollectionResult? HttpProofCollectionResult { get; }

    /// <summary>
    /// Gets the nested DNS TXT proof collection result when one ran.
    /// </summary>
    public TenantDomainOwnershipDnsTxtProofCollectionResult? DnsTxtProofCollectionResult { get; }

    /// <summary>
    /// Gets the nested proof evaluation result when one ran.
    /// </summary>
    public TenantDomainOwnershipProofEvaluationResult? EvaluationResult { get; }

    /// <summary>
    /// Gets the matching or resulting domain ownership descriptor when one exists.
    /// </summary>
    public TenantDomainOwnershipDescriptor? DomainOwnership { get; }

    /// <summary>
    /// Gets the operator-facing proof verification reason.
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
            TenantDomainOwnershipProofVerificationOutcomes.Verified => TenantDomainOwnershipProofVerificationOutcomes.Verified,
            TenantDomainOwnershipProofVerificationOutcomes.Rejected => TenantDomainOwnershipProofVerificationOutcomes.Rejected,
            TenantDomainOwnershipProofVerificationOutcomes.ChallengeIssued => TenantDomainOwnershipProofVerificationOutcomes.ChallengeIssued,
            TenantDomainOwnershipProofVerificationOutcomes.PublicationPlanned => TenantDomainOwnershipProofVerificationOutcomes.PublicationPlanned,
            TenantDomainOwnershipProofVerificationOutcomes.AlreadyVerified => TenantDomainOwnershipProofVerificationOutcomes.AlreadyVerified,
            TenantDomainOwnershipProofVerificationOutcomes.Disabled => TenantDomainOwnershipProofVerificationOutcomes.Disabled,
            TenantDomainOwnershipProofVerificationOutcomes.NotFound => TenantDomainOwnershipProofVerificationOutcomes.NotFound,
            TenantDomainOwnershipProofVerificationOutcomes.TenantMismatch => TenantDomainOwnershipProofVerificationOutcomes.TenantMismatch,
            TenantDomainOwnershipProofVerificationOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofVerificationOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofVerificationOutcomes.MissingExpectedProof => TenantDomainOwnershipProofVerificationOutcomes.MissingExpectedProof,
            TenantDomainOwnershipProofVerificationOutcomes.MissingObservedProof => TenantDomainOwnershipProofVerificationOutcomes.MissingObservedProof,
            TenantDomainOwnershipProofVerificationOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipProofVerificationOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipProofVerificationOutcomes.ChallengeFailed => TenantDomainOwnershipProofVerificationOutcomes.ChallengeFailed,
            TenantDomainOwnershipProofVerificationOutcomes.PublicationPlanFailed => TenantDomainOwnershipProofVerificationOutcomes.PublicationPlanFailed,
            TenantDomainOwnershipProofVerificationOutcomes.HttpCollectionUnavailable => TenantDomainOwnershipProofVerificationOutcomes.HttpCollectionUnavailable,
            TenantDomainOwnershipProofVerificationOutcomes.HttpCollectionFailed => TenantDomainOwnershipProofVerificationOutcomes.HttpCollectionFailed,
            TenantDomainOwnershipProofVerificationOutcomes.DnsTxtCollectionUnavailable => TenantDomainOwnershipProofVerificationOutcomes.DnsTxtCollectionUnavailable,
            TenantDomainOwnershipProofVerificationOutcomes.DnsTxtCollectionFailed => TenantDomainOwnershipProofVerificationOutcomes.DnsTxtCollectionFailed,
            TenantDomainOwnershipProofVerificationOutcomes.EvaluationFailed => TenantDomainOwnershipProofVerificationOutcomes.EvaluationFailed,
            TenantDomainOwnershipProofVerificationOutcomes.StoreFailed => TenantDomainOwnershipProofVerificationOutcomes.StoreFailed,
            _ => throw new ArgumentException($"Tenant-domain ownership proof verification outcome '{outcome}' is not supported.", nameof(outcome))
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
