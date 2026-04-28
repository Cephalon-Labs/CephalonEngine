namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-domain ownership proof evaluation.
/// </summary>
public sealed class TenantDomainOwnershipProofEvaluationResult
{
    /// <summary>
    /// Creates a tenant-domain ownership proof evaluation result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="domainName">The canonical domain name that was evaluated.</param>
    /// <param name="verificationMethod">The verification method used for evaluation.</param>
    /// <param name="outcome">The stable proof evaluation outcome.</param>
    /// <param name="matched">A value indicating whether the observed proof matched the expected proof.</param>
    /// <param name="applied">A value indicating whether the verification workflow transition was applied.</param>
    /// <param name="evaluatedAtUtc">The UTC timestamp when proof evaluation executed.</param>
    /// <param name="observedProofFingerprint">The SHA-256 fingerprint of the observed proof value when present.</param>
    /// <param name="expectedProofFingerprint">The SHA-256 fingerprint of the expected proof value when present.</param>
    /// <param name="domainOwnership">The matching or resulting domain ownership descriptor when one exists.</param>
    /// <param name="workflowResult">The underlying workflow transition result when proof evaluation reached workflow mutation.</param>
    /// <param name="reason">The operator-facing proof evaluation reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipProofEvaluationResult(
        string tenantId,
        string domainName,
        string? verificationMethod,
        string outcome,
        bool matched,
        bool applied,
        DateTimeOffset evaluatedAtUtc,
        string? observedProofFingerprint,
        string? expectedProofFingerprint,
        TenantDomainOwnershipDescriptor? domainOwnership,
        TenantDomainOwnershipVerificationWorkflowResult? workflowResult,
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
        Matched = matched;
        Applied = applied;
        EvaluatedAtUtc = evaluatedAtUtc;
        ObservedProofFingerprint = string.IsNullOrWhiteSpace(observedProofFingerprint) ? null : observedProofFingerprint.Trim();
        ExpectedProofFingerprint = string.IsNullOrWhiteSpace(expectedProofFingerprint) ? null : expectedProofFingerprint.Trim();
        DomainOwnership = domainOwnership;
        WorkflowResult = workflowResult;
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
    /// Gets the verification method used for evaluation.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the stable proof evaluation outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the observed proof matched the expected proof.
    /// </summary>
    public bool Matched { get; }

    /// <summary>
    /// Gets a value indicating whether the verification workflow transition was applied.
    /// </summary>
    public bool Applied { get; }

    /// <summary>
    /// Gets the UTC timestamp when proof evaluation executed.
    /// </summary>
    public DateTimeOffset EvaluatedAtUtc { get; }

    /// <summary>
    /// Gets the SHA-256 fingerprint of the observed proof value when present.
    /// </summary>
    public string? ObservedProofFingerprint { get; }

    /// <summary>
    /// Gets the SHA-256 fingerprint of the expected proof value when present.
    /// </summary>
    public string? ExpectedProofFingerprint { get; }

    /// <summary>
    /// Gets the matching or resulting domain ownership descriptor when one exists.
    /// </summary>
    public TenantDomainOwnershipDescriptor? DomainOwnership { get; }

    /// <summary>
    /// Gets the underlying workflow transition result when proof evaluation reached workflow mutation.
    /// </summary>
    public TenantDomainOwnershipVerificationWorkflowResult? WorkflowResult { get; }

    /// <summary>
    /// Gets the operator-facing proof evaluation reason.
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
            TenantDomainOwnershipProofEvaluationOutcomes.Verified => TenantDomainOwnershipProofEvaluationOutcomes.Verified,
            TenantDomainOwnershipProofEvaluationOutcomes.Rejected => TenantDomainOwnershipProofEvaluationOutcomes.Rejected,
            TenantDomainOwnershipProofEvaluationOutcomes.NotFound => TenantDomainOwnershipProofEvaluationOutcomes.NotFound,
            TenantDomainOwnershipProofEvaluationOutcomes.TenantMismatch => TenantDomainOwnershipProofEvaluationOutcomes.TenantMismatch,
            TenantDomainOwnershipProofEvaluationOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofEvaluationOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofEvaluationOutcomes.MissingExpectedProof => TenantDomainOwnershipProofEvaluationOutcomes.MissingExpectedProof,
            TenantDomainOwnershipProofEvaluationOutcomes.MissingObservedProof => TenantDomainOwnershipProofEvaluationOutcomes.MissingObservedProof,
            TenantDomainOwnershipProofEvaluationOutcomes.WorkflowDenied => TenantDomainOwnershipProofEvaluationOutcomes.WorkflowDenied,
            TenantDomainOwnershipProofEvaluationOutcomes.Disabled => TenantDomainOwnershipProofEvaluationOutcomes.Disabled,
            _ => throw new ArgumentException($"Tenant-domain ownership proof evaluation outcome '{outcome}' is not supported.", nameof(outcome))
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
