namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a bounded tenant invitation delivery retry runner request.
/// </summary>
public sealed class TenantInvitationDeliveryRetryRequest
{
    /// <summary>
    /// Creates a tenant invitation delivery retry runner request.
    /// </summary>
    /// <param name="atUtc">The UTC timestamp used for retry evaluation.</param>
    /// <param name="maxItems">The maximum number of retry entries to attempt.</param>
    /// <param name="dueOnly">A value indicating whether entries scheduled after <paramref name="atUtc" /> should be skipped.</param>
    /// <param name="source">The source recorded on retry attempts.</param>
    /// <param name="actor">The actor recorded on retry attempts.</param>
    /// <param name="correlationId">The correlation identifier recorded on retry attempts.</param>
    /// <param name="metadata">Optional retry runner metadata.</param>
    public TenantInvitationDeliveryRetryRequest(
        DateTimeOffset? atUtc = null,
        int? maxItems = null,
        bool dueOnly = true,
        string? source = null,
        string? actor = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        AtUtc = atUtc;
        MaxItems = maxItems;
        DueOnly = dueOnly;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the UTC timestamp used for retry evaluation.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the maximum number of retry entries to attempt.
    /// </summary>
    public int? MaxItems { get; }

    /// <summary>
    /// Gets a value indicating whether entries scheduled after <see cref="AtUtc" /> should be skipped.
    /// </summary>
    public bool DueOnly { get; }

    /// <summary>
    /// Gets the source recorded on retry attempts.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor recorded on retry attempts.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the correlation identifier recorded on retry attempts.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional retry runner metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

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
