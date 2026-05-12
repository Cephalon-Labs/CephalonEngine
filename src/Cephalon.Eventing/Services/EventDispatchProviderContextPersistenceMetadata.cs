namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds truthful provider-side context-persistence metadata for dispatch reports that were durably applied by a dispatch store.
/// </summary>
/// <remarks>
/// The helper only claims provider-side persistence when provider or broker context headers were already projected with Cephalon
/// propagation headers. It keeps cross-node handoff unclaimed because persistence inside a dispatch store does not prove that a
/// different node consumed the propagated context.
/// </remarks>
public static class EventDispatchProviderContextPersistenceMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-side context-persistence proof when the source metadata supports it.
    /// </summary>
    /// <param name="report">The dispatch report that was already applied by a capable provider-side dispatch store.</param>
    /// <param name="source">The stable dispatch-store or provider identifier that persisted the context proof.</param>
    /// <returns>A dispatch report containing the original metadata plus provider-side persistence proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(EventDispatchExecutionReport report, string source)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new EventDispatchExecutionReport(
            outboxId: report.OutboxId,
            channelId: report.ChannelId,
            outcome: report.Outcome,
            observedAtUtc: report.ObservedAtUtc,
            messageId: report.MessageId,
            attempt: report.Attempt,
            error: report.Error,
            metadata: CreateMetadata(report.Metadata, source));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-side context-persistence proof when the source metadata supports it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="source">The stable dispatch-store or provider identifier that persisted the context proof.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus persistence proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(IReadOnlyDictionary<string, string> metadata, string source)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(result, source);
        return result;
    }

    /// <summary>
    /// Applies provider-side context-persistence proof to an existing metadata dictionary when projected context headers are present.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to enrich.</param>
    /// <param name="source">The stable dispatch-store or provider identifier that persisted the context proof.</param>
    /// <returns><see langword="true" /> when provider-side persistence metadata was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(IDictionary<string, string> metadata, string source)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Provider-side context persistence source is required.", nameof(source));
        }

        if (!HasProjectedProviderBrokerContext(metadata))
        {
            return false;
        }

        metadata[EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistence] = "dispatch-store-persisted";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceSource] = source.Trim();
        metadata[EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceHeaderCount] = GetValueOrDefault(
            metadata,
            EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderCount);
        metadata[EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceHeaderNames] = GetValueOrDefault(
            metadata,
            EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderNames);
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoff] = "not-claimed";
        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains provider-side context-persistence proof.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when the provider-side persistence proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsPersisted(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistence, out var value) &&
            string.Equals(value, "dispatch-store-persisted", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasProjectedProviderBrokerContext(IDictionary<string, string> metadata) =>
        metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaders, out var providerBrokerContextHeaders) &&
        string.Equals(providerBrokerContextHeaders, "projected", StringComparison.OrdinalIgnoreCase) &&
        metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderProjection, out var projection) &&
        string.Equals(projection, "cephalon-context-headers", StringComparison.OrdinalIgnoreCase) &&
        metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderNames, out var headerNames) &&
        HasPropagationHeaderNames(headerNames);

    private static bool HasPropagationHeaderNames(string headerNames)
    {
        if (string.IsNullOrWhiteSpace(headerNames))
        {
            return false;
        }

        return headerNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(static headerName =>
                string.Equals(headerName, EventContextHeaderNames.TenantId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(headerName, EventContextHeaderNames.CorrelationId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(headerName, EventContextHeaderNames.CausationId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(headerName, EventContextHeaderNames.Baggage, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetValueOrDefault(IDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) ? value : string.Empty;
}
