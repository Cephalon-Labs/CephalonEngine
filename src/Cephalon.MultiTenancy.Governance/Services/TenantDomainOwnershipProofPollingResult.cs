namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the aggregate result of one tenant-domain ownership proof polling pass.
/// </summary>
public sealed class TenantDomainOwnershipProofPollingResult
{
    /// <summary>
    /// Creates a tenant-domain ownership proof polling result.
    /// </summary>
    /// <param name="outcome">The stable proof polling outcome.</param>
    /// <param name="polled">A value indicating whether at least one verification attempt ran.</param>
    /// <param name="ranAtUtc">The UTC timestamp when polling ran.</param>
    /// <param name="candidateCount">The number of declarations that matched request filters before batch limiting.</param>
    /// <param name="verificationCount">The number of verification attempts run.</param>
    /// <param name="skippedCount">The number of declarations skipped by filters, missing expected proof policy, or batch limits.</param>
    /// <param name="verifiedCount">The number of declarations verified by the polling pass.</param>
    /// <param name="rejectedCount">The number of declarations rejected by the polling pass.</param>
    /// <param name="failedCount">The number of attempts that did not reach an accepted terminal outcome.</param>
    /// <param name="batchLimit">The effective maximum number of declarations this pass could poll.</param>
    /// <param name="verificationResults">The nested proof verification results.</param>
    /// <param name="reason">The operator-facing proof polling reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantDomainOwnershipProofPollingResult(
        string outcome,
        bool polled,
        DateTimeOffset ranAtUtc,
        int candidateCount,
        int verificationCount,
        int skippedCount,
        int verifiedCount,
        int rejectedCount,
        int failedCount,
        int batchLimit,
        IReadOnlyList<TenantDomainOwnershipProofVerificationResult> verificationResults,
        string reason,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        ArgumentNullException.ThrowIfNull(verificationResults);

        Outcome = NormalizeOutcome(outcome);
        Polled = polled;
        RanAtUtc = ranAtUtc;
        CandidateCount = candidateCount;
        VerificationCount = verificationCount;
        SkippedCount = skippedCount;
        VerifiedCount = verifiedCount;
        RejectedCount = rejectedCount;
        FailedCount = failedCount;
        BatchLimit = batchLimit;
        VerificationResults = verificationResults.ToArray();
        Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable proof polling outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether at least one verification attempt ran.
    /// </summary>
    public bool Polled { get; }

    /// <summary>
    /// Gets the UTC timestamp when polling ran.
    /// </summary>
    public DateTimeOffset RanAtUtc { get; }

    /// <summary>
    /// Gets the number of declarations that matched request filters before batch limiting.
    /// </summary>
    public int CandidateCount { get; }

    /// <summary>
    /// Gets the number of verification attempts run.
    /// </summary>
    public int VerificationCount { get; }

    /// <summary>
    /// Gets the number of declarations skipped by filters, missing expected proof policy, or batch limits.
    /// </summary>
    public int SkippedCount { get; }

    /// <summary>
    /// Gets the number of declarations verified by the polling pass.
    /// </summary>
    public int VerifiedCount { get; }

    /// <summary>
    /// Gets the number of declarations rejected by the polling pass.
    /// </summary>
    public int RejectedCount { get; }

    /// <summary>
    /// Gets the number of attempts that did not reach an accepted terminal outcome.
    /// </summary>
    public int FailedCount { get; }

    /// <summary>
    /// Gets the effective maximum number of declarations this pass could poll.
    /// </summary>
    public int BatchLimit { get; }

    /// <summary>
    /// Gets the nested proof verification results.
    /// </summary>
    public IReadOnlyList<TenantDomainOwnershipProofVerificationResult> VerificationResults { get; }

    /// <summary>
    /// Gets the operator-facing proof polling reason.
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
            TenantDomainOwnershipProofPollingOutcomes.Completed => TenantDomainOwnershipProofPollingOutcomes.Completed,
            TenantDomainOwnershipProofPollingOutcomes.PartialFailure => TenantDomainOwnershipProofPollingOutcomes.PartialFailure,
            TenantDomainOwnershipProofPollingOutcomes.NoCandidates => TenantDomainOwnershipProofPollingOutcomes.NoCandidates,
            TenantDomainOwnershipProofPollingOutcomes.Disabled => TenantDomainOwnershipProofPollingOutcomes.Disabled,
            _ => throw new ArgumentException($"Tenant-domain ownership proof polling outcome '{outcome}' is not supported.", nameof(outcome))
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
