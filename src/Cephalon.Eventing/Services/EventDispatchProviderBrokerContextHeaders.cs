using System.Globalization;

namespace Cephalon.Eventing.Services;

/// <summary>
/// Projects staged Cephalon event context into provider-neutral headers before a dispatch runtime hands a message to a provider or broker.
/// </summary>
/// <remarks>
/// The helper only creates stable Cephalon context headers from an <see cref="EventDispatchItem" />. It does not claim that a
/// provider persisted the headers, that a consumer extracted them, or that cross-node handoff has completed.
/// </remarks>
public static class EventDispatchProviderBrokerContextHeaders
{
    /// <summary>
    /// Creates a deterministic set of Cephalon context headers for a pending dispatch item.
    /// </summary>
    /// <param name="dispatchItem">The pending dispatch item whose staged context should be projected.</param>
    /// <returns>A case-insensitive dictionary of provider-neutral context headers.</returns>
    public static Dictionary<string, string> Create(EventDispatchItem dispatchItem)
    {
        ArgumentNullException.ThrowIfNull(dispatchItem);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CopyIfPresent(dispatchItem.Headers, headers, EventContextHeaderNames.TenantId);
        CopyIfPresent(dispatchItem.Headers, headers, EventContextHeaderNames.CorrelationId);
        CopyIfPresent(dispatchItem.Headers, headers, EventContextHeaderNames.CausationId);
        CopyIfPresent(dispatchItem.Headers, headers, EventContextHeaderNames.Baggage);
        CopyIfPresent(dispatchItem.Headers, headers, EventContextHeaderNames.MessageId);

        SetIfPresent(headers, EventContextHeaderNames.TenantId, dispatchItem.TenantId);
        SetIfPresent(headers, EventContextHeaderNames.CorrelationId, dispatchItem.CorrelationId);
        SetIfPresent(headers, EventContextHeaderNames.MessageId, dispatchItem.MessageId);

        return headers;
    }

    /// <summary>
    /// Adds conservative dispatch-report metadata for a projected provider or broker context-header set.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata dictionary to enrich.</param>
    /// <param name="providerBrokerHeaders">The headers created for the provider or broker handoff.</param>
    public static void ApplyReportMetadata(
        IDictionary<string, string> metadata,
        IReadOnlyDictionary<string, string> providerBrokerHeaders)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(providerBrokerHeaders);

        var hasPropagationHeaders = HasPropagationHeaders(providerBrokerHeaders);
        metadata[EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaders] = hasPropagationHeaders
            ? "projected"
            : "not-claimed";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderProjection] = hasPropagationHeaders
            ? "cephalon-context-headers"
            : "message-id-only";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderCount] =
            providerBrokerHeaders.Count.ToString(CultureInfo.InvariantCulture);
        metadata[EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderNames] = string.Join(
            ",",
            providerBrokerHeaders.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));
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
