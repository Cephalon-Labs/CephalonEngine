using System.Globalization;

namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-neutral metadata that carries staged Cephalon event context into dispatch runtime reports.
/// </summary>
/// <remarks>
/// The helper preserves the dispatch item's staged metadata and adds conservative runtime boundary markers.
/// Provider and broker context claims remain <c>not-claimed</c> until a provider package reports executable proof.
/// </remarks>
public static class EventDispatchContextReportMetadata
{
    /// <summary>
    /// Creates dispatch report metadata from a pending dispatch item and optional runtime-specific metadata.
    /// </summary>
    /// <param name="dispatchItem">The pending dispatch item whose staged context should be carried into the report.</param>
    /// <param name="additionalMetadata">Optional runtime-specific metadata to add after the provider-neutral context markers.</param>
    /// <returns>A case-insensitive metadata dictionary suitable for <see cref="EventDispatchExecutionReport.Metadata" />.</returns>
    public static Dictionary<string, string> Create(
        EventDispatchItem dispatchItem,
        IReadOnlyDictionary<string, string>? additionalMetadata = null)
    {
        ArgumentNullException.ThrowIfNull(dispatchItem);

        var metadata = new Dictionary<string, string>(dispatchItem.Metadata, StringComparer.OrdinalIgnoreCase);
        var hasContextProof = HasContextProof(dispatchItem, metadata);

        metadata[EventDispatchRuntimeMetadataKeys.DurableDispatchContextPropagation] = hasContextProof
            ? "dispatch-report-metadata"
            : "not-claimed";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaders] = "not-claimed";
        metadata[EventDispatchRuntimeMetadataKeys.ConsumerContextExtraction] = "not-claimed";
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoff] = "not-claimed";
        metadata[EventDispatchRuntimeMetadataKeys.DispatchContextMetadata] = hasContextProof ? "reported" : "not-present";
        metadata[EventDispatchRuntimeMetadataKeys.DispatchContextHeaderCount] = dispatchItem.Headers.Count.ToString(CultureInfo.InvariantCulture);
        metadata[EventDispatchRuntimeMetadataKeys.DispatchContextMetadataCount] = dispatchItem.Metadata.Count.ToString(CultureInfo.InvariantCulture);

        if (additionalMetadata is not null)
        {
            foreach (var pair in additionalMetadata)
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return metadata;
    }

    private static bool HasContextProof(
        EventDispatchItem dispatchItem,
        Dictionary<string, string> metadata) =>
        metadata.ContainsKey(EventContextHandoffMetadataKeys.ContextHandoff) ||
        dispatchItem.Headers.ContainsKey(EventContextHeaderNames.TenantId) ||
        dispatchItem.Headers.ContainsKey(EventContextHeaderNames.CorrelationId) ||
        dispatchItem.Headers.ContainsKey(EventContextHeaderNames.CausationId) ||
        dispatchItem.Headers.ContainsKey(EventContextHeaderNames.Baggage) ||
        dispatchItem.Headers.ContainsKey(EventContextHeaderNames.MessageId);
}
