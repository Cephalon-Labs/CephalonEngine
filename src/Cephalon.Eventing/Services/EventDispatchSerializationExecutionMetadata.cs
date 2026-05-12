namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported executable serialization proof metadata for successful dispatch reports.
/// </summary>
/// <remarks>
/// Cephalon's contract, serializer, schema-registry, and upcaster catalogs only prove descriptor-level evidence. This
/// helper records a stronger executable claim only when a provider/runtime reports successful dispatch evidence with
/// payload serialization, schema lookup, upcaster execution, compatibility validation, and provider serialization
/// proof ids.
/// </remarks>
public static class EventDispatchSerializationExecutionMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported serialization execution proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported serialization execution ownership.</param>
    /// <param name="payloadSerializationExecutionId">The executable payload serialization proof id.</param>
    /// <param name="schemaLookupExecutionId">The executable schema lookup proof id.</param>
    /// <param name="upcasterExecutionId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationExecutionId">The executable compatibility validation proof id.</param>
    /// <param name="providerSerializationId">The provider-owned serialization proof id.</param>
    /// <returns>A dispatch report containing the original metadata plus serialization execution proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string payloadSerializationExecutionId,
        string schemaLookupExecutionId,
        string upcasterExecutionId,
        string compatibilityValidationExecutionId,
        string providerSerializationId)
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
            metadata: CreateMetadata(
                report.Metadata,
                report.Outcome,
                source,
                payloadSerializationExecutionId,
                schemaLookupExecutionId,
                upcasterExecutionId,
                compatibilityValidationExecutionId,
                providerSerializationId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported serialization execution proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported serialization execution ownership.</param>
    /// <param name="payloadSerializationExecutionId">The executable payload serialization proof id.</param>
    /// <param name="schemaLookupExecutionId">The executable schema lookup proof id.</param>
    /// <param name="upcasterExecutionId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationExecutionId">The executable compatibility validation proof id.</param>
    /// <param name="providerSerializationId">The provider-owned serialization proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus serialization execution proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string payloadSerializationExecutionId,
        string schemaLookupExecutionId,
        string upcasterExecutionId,
        string compatibilityValidationExecutionId,
        string providerSerializationId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            payloadSerializationExecutionId,
            schemaLookupExecutionId,
            upcasterExecutionId,
            compatibilityValidationExecutionId,
            providerSerializationId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported serialization execution proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported serialization execution ownership.</param>
    /// <param name="payloadSerializationExecutionId">The executable payload serialization proof id.</param>
    /// <param name="schemaLookupExecutionId">The executable schema lookup proof id.</param>
    /// <param name="upcasterExecutionId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationExecutionId">The executable compatibility validation proof id.</param>
    /// <param name="providerSerializationId">The provider-owned serialization proof id.</param>
    /// <returns><see langword="true" /> when serialization execution proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string payloadSerializationExecutionId,
        string schemaLookupExecutionId,
        string upcasterExecutionId,
        string compatibilityValidationExecutionId,
        string providerSerializationId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedPayloadSerializationExecutionId = RequireValue(payloadSerializationExecutionId, nameof(payloadSerializationExecutionId));
        var normalizedSchemaLookupExecutionId = RequireValue(schemaLookupExecutionId, nameof(schemaLookupExecutionId));
        var normalizedUpcasterExecutionId = RequireValue(upcasterExecutionId, nameof(upcasterExecutionId));
        var normalizedCompatibilityValidationExecutionId = RequireValue(compatibilityValidationExecutionId, nameof(compatibilityValidationExecutionId));
        var normalizedProviderSerializationId = RequireValue(providerSerializationId, nameof(providerSerializationId));

        metadata[EventDispatchRuntimeMetadataKeys.SerializationExecutionOwnership] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.SerializationExecutionOwnershipSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.SerializationDurability] = "durable";
        metadata[EventDispatchRuntimeMetadataKeys.SerializationScope] = "cross-node";
        metadata[EventDispatchRuntimeMetadataKeys.PayloadSerializationExecution] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.PayloadSerializationExecutionId] = normalizedPayloadSerializationExecutionId;
        metadata[EventDispatchRuntimeMetadataKeys.SchemaLookupExecution] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.SchemaLookupExecutionId] = normalizedSchemaLookupExecutionId;
        metadata[EventDispatchRuntimeMetadataKeys.UpcasterExecution] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.UpcasterExecutionId] = normalizedUpcasterExecutionId;
        metadata[EventDispatchRuntimeMetadataKeys.CompatibilityValidationExecution] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.CompatibilityValidationExecutionId] = normalizedCompatibilityValidationExecutionId;
        metadata[EventDispatchRuntimeMetadataKeys.ProviderSerialization] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderSerializationId] = normalizedProviderSerializationId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported serialization execution proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete serialization execution proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsSerializationExecutionProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasValue(metadata, EventDispatchRuntimeMetadataKeys.SerializationExecutionOwnership, "provider-reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.SerializationExecutionOwnershipSource) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.SerializationDurability, "durable") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.SerializationScope, "cross-node") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.PayloadSerializationExecution, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.PayloadSerializationExecutionId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.SchemaLookupExecution, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.SchemaLookupExecutionId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.UpcasterExecution, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.UpcasterExecutionId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.CompatibilityValidationExecution, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.CompatibilityValidationExecutionId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ProviderSerialization, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ProviderSerializationId);
    }

    private static bool HasValue(IReadOnlyDictionary<string, string> metadata, string key, string expected) =>
        metadata.TryGetValue(key, out var value) &&
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static bool HasNonEmpty(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) &&
        !string.IsNullOrWhiteSpace(value);

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}
