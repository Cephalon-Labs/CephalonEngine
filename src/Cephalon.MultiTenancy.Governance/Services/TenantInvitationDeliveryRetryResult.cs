namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the aggregate result of one tenant invitation delivery retry runner pass.
/// </summary>
public sealed class TenantInvitationDeliveryRetryResult
{
    /// <summary>
    /// Creates a tenant invitation delivery retry result.
    /// </summary>
    /// <param name="outcome">The stable retry runner outcome.</param>
    /// <param name="attemptedCount">The number of retry entries attempted.</param>
    /// <param name="dispatchedCount">The number of retry entries dispatched successfully.</param>
    /// <param name="failedCount">The number of attempted entries that remained pending after failure.</param>
    /// <param name="exhaustedCount">The number of attempted entries that exhausted their retry budget.</param>
    /// <param name="terminalCount">The number of attempted entries that hit a terminal invitation state.</param>
    /// <param name="remainingPendingCount">The number of pending entries retained after the pass.</param>
    /// <param name="atUtc">The UTC timestamp used for retry evaluation.</param>
    /// <param name="deliveryResults">The delivery dispatch results produced by this pass.</param>
    /// <param name="metadata">Optional retry result metadata.</param>
    public TenantInvitationDeliveryRetryResult(
        string outcome,
        int attemptedCount,
        int dispatchedCount,
        int failedCount,
        int exhaustedCount,
        int terminalCount,
        int remainingPendingCount,
        DateTimeOffset atUtc,
        IReadOnlyList<TenantInvitationDeliveryResult>? deliveryResults = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        Outcome = NormalizeOutcome(outcome);
        AttemptedCount = Math.Max(0, attemptedCount);
        DispatchedCount = Math.Max(0, dispatchedCount);
        FailedCount = Math.Max(0, failedCount);
        ExhaustedCount = Math.Max(0, exhaustedCount);
        TerminalCount = Math.Max(0, terminalCount);
        RemainingPendingCount = Math.Max(0, remainingPendingCount);
        AtUtc = atUtc;
        DeliveryResults = deliveryResults?.ToArray() ?? [];
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable retry runner outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets the number of retry entries attempted.
    /// </summary>
    public int AttemptedCount { get; }

    /// <summary>
    /// Gets the number of retry entries dispatched successfully.
    /// </summary>
    public int DispatchedCount { get; }

    /// <summary>
    /// Gets the number of attempted entries that remained pending after failure.
    /// </summary>
    public int FailedCount { get; }

    /// <summary>
    /// Gets the number of attempted entries that exhausted their retry budget.
    /// </summary>
    public int ExhaustedCount { get; }

    /// <summary>
    /// Gets the number of attempted entries that hit a terminal invitation state.
    /// </summary>
    public int TerminalCount { get; }

    /// <summary>
    /// Gets the number of pending entries retained after the pass.
    /// </summary>
    public int RemainingPendingCount { get; }

    /// <summary>
    /// Gets the UTC timestamp used for retry evaluation.
    /// </summary>
    public DateTimeOffset AtUtc { get; }

    /// <summary>
    /// Gets the delivery dispatch results produced by this pass.
    /// </summary>
    public IReadOnlyList<TenantInvitationDeliveryResult> DeliveryResults { get; }

    /// <summary>
    /// Gets optional retry result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantInvitationDeliveryRetryOutcomes.Disabled => TenantInvitationDeliveryRetryOutcomes.Disabled,
            TenantInvitationDeliveryRetryOutcomes.NoPendingRetries => TenantInvitationDeliveryRetryOutcomes.NoPendingRetries,
            TenantInvitationDeliveryRetryOutcomes.AlreadyRunning => TenantInvitationDeliveryRetryOutcomes.AlreadyRunning,
            TenantInvitationDeliveryRetryOutcomes.Retried => TenantInvitationDeliveryRetryOutcomes.Retried,
            TenantInvitationDeliveryRetryOutcomes.Partial => TenantInvitationDeliveryRetryOutcomes.Partial,
            TenantInvitationDeliveryRetryOutcomes.Failed => TenantInvitationDeliveryRetryOutcomes.Failed,
            _ => throw new ArgumentException($"Tenant invitation delivery retry outcome '{outcome}' is not supported.", nameof(outcome))
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
