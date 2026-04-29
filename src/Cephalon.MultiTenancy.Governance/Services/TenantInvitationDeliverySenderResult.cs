namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the outcome returned by a tenant invitation delivery sender.
/// </summary>
public sealed class TenantInvitationDeliverySenderResult
{
    /// <summary>
    /// Creates a tenant invitation delivery sender result.
    /// </summary>
    /// <param name="outcome">The sender outcome.</param>
    /// <param name="dispatched">A value indicating whether the sender accepted the dispatch.</param>
    /// <param name="providerMessageId">The provider message identifier returned by the sender.</param>
    /// <param name="reason">The provider-facing outcome reason.</param>
    /// <param name="dispatchedAtUtc">The UTC timestamp reported by the sender.</param>
    /// <param name="metadata">Optional sender metadata.</param>
    public TenantInvitationDeliverySenderResult(
        string outcome,
        bool dispatched,
        string? providerMessageId = null,
        string? reason = null,
        DateTimeOffset? dispatchedAtUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        Outcome = NormalizeOutcome(outcome);
        Dispatched = dispatched;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        DispatchedAtUtc = dispatchedAtUtc;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the sender outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the sender accepted the dispatch.
    /// </summary>
    public bool Dispatched { get; }

    /// <summary>
    /// Gets the provider message identifier returned by the sender.
    /// </summary>
    public string? ProviderMessageId { get; }

    /// <summary>
    /// Gets the provider-facing outcome reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the UTC timestamp reported by the sender.
    /// </summary>
    public DateTimeOffset? DispatchedAtUtc { get; }

    /// <summary>
    /// Gets optional sender metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantInvitationDeliveryOutcomes.Dispatched => TenantInvitationDeliveryOutcomes.Dispatched,
            TenantInvitationDeliveryOutcomes.SenderFailed => TenantInvitationDeliveryOutcomes.SenderFailed,
            TenantInvitationDeliveryOutcomes.Suppressed => TenantInvitationDeliveryOutcomes.Suppressed,
            _ => throw new ArgumentException($"Tenant invitation delivery sender outcome '{outcome}' is not supported.", nameof(outcome))
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
