using System.Globalization;

namespace Cephalon.Eventing.Services;

/// <summary>
/// Extracts Cephalon event context from a publication that is about to be delivered to a consumer.
/// </summary>
/// <remarks>
/// The extractor normalizes the publication fields and stable Cephalon headers into one consumer-side
/// context-header set. It does not claim provider-side persistence, delivery completion, or cross-node handoff.
/// </remarks>
public static class EventConsumerContextExtractor
{
    /// <summary>
    /// Creates a deterministic set of consumer-visible Cephalon context headers for a publication.
    /// </summary>
    /// <param name="publication">The publication being delivered to a subscription executor.</param>
    /// <returns>A case-insensitive dictionary of extracted consumer context headers.</returns>
    public static Dictionary<string, string> CreateHeaders(EventPublication publication)
    {
        ArgumentNullException.ThrowIfNull(publication);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CopyIfPresent(publication.Headers, headers, EventContextHeaderNames.TenantId);
        CopyIfPresent(publication.Headers, headers, EventContextHeaderNames.CorrelationId);
        CopyIfPresent(publication.Headers, headers, EventContextHeaderNames.CausationId);
        CopyIfPresent(publication.Headers, headers, EventContextHeaderNames.Baggage);
        CopyIfPresent(publication.Headers, headers, EventContextHeaderNames.MessageId);

        SetIfPresent(headers, EventContextHeaderNames.TenantId, publication.TenantId);
        SetIfPresent(headers, EventContextHeaderNames.CorrelationId, publication.CorrelationId);
        SetIfPresent(headers, EventContextHeaderNames.MessageId, publication.Id);

        return headers;
    }

    /// <summary>
    /// Adds consumer-side context extraction evidence to subscription execution metadata.
    /// </summary>
    /// <param name="metadata">The subscription execution metadata dictionary to enrich.</param>
    /// <param name="consumerContextHeaders">The consumer-visible context headers extracted from the publication.</param>
    public static void ApplyMetadata(
        IDictionary<string, string> metadata,
        IReadOnlyDictionary<string, string> consumerContextHeaders)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(consumerContextHeaders);

        var hasPropagationHeaders = HasPropagationHeaders(consumerContextHeaders);
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtraction] = hasPropagationHeaders
            ? "extracted"
            : "not-claimed";
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtractionSource] = hasPropagationHeaders
            ? "event-publication-context-headers"
            : "message-id-only";
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderCount] =
            consumerContextHeaders.Count.ToString(CultureInfo.InvariantCulture);
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderNames] = string.Join(
            ",",
            consumerContextHeaders.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));
    }

    private static void CopyIfPresent(
        IReadOnlyDictionary<string, string> source,
        IDictionary<string, string> headers,
        string headerName)
    {
        if (source.TryGetValue(headerName, out var value))
        {
            SetIfPresent(headers, headerName, value);
        }
    }

    private static void SetIfPresent(IDictionary<string, string> headers, string headerName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            headers[headerName] = value.Trim();
        }
    }

    private static bool HasPropagationHeaders(IReadOnlyDictionary<string, string> headers) =>
        headers.Keys.Any(static headerName =>
            string.Equals(headerName, EventContextHeaderNames.TenantId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(headerName, EventContextHeaderNames.CorrelationId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(headerName, EventContextHeaderNames.CausationId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(headerName, EventContextHeaderNames.Baggage, StringComparison.OrdinalIgnoreCase));
}
