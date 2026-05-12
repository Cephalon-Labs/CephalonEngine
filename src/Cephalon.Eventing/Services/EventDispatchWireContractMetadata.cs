namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported executable wire-contract proof metadata for successful dispatch reports.
/// </summary>
/// <remarks>
/// Cephalon's contract, serializer, schema-registry, and upcaster catalogs only prove descriptor-level evidence
/// for the wire envelope and contract-version negotiation. This helper records a stronger executable claim
/// when a provider or runtime reports successful dispatch evidence covering payload serialization, wire-envelope
/// schema lookup, contract-version negotiation, upcaster execution, and compatibility validation behind a single
/// wire-contract proof id.
/// </remarks>
public static class EventDispatchWireContractMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported wire-contract proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported wire-contract ownership.</param>
    /// <param name="payloadSerializationProofId">The executable payload serialization proof id.</param>
    /// <param name="wireEnvelopeSchemaId">The executable wire-envelope schema proof id.</param>
    /// <param name="schemaLookupProofId">The executable schema lookup proof id.</param>
    /// <param name="contractVersionNegotiationId">The executable contract-version negotiation proof id.</param>
    /// <param name="upcasterExecutionProofId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationProofId">The executable compatibility validation proof id.</param>
    /// <param name="wireContractProofId">The overall wire-contract proof id.</param>
    /// <returns>A dispatch report containing the original metadata plus wire-contract proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string payloadSerializationProofId,
        string wireEnvelopeSchemaId,
        string schemaLookupProofId,
        string contractVersionNegotiationId,
        string upcasterExecutionProofId,
        string compatibilityValidationProofId,
        string wireContractProofId)
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
                payloadSerializationProofId,
                wireEnvelopeSchemaId,
                schemaLookupProofId,
                contractVersionNegotiationId,
                upcasterExecutionProofId,
                compatibilityValidationProofId,
                wireContractProofId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported wire-contract proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported wire-contract ownership.</param>
    /// <param name="payloadSerializationProofId">The executable payload serialization proof id.</param>
    /// <param name="wireEnvelopeSchemaId">The executable wire-envelope schema proof id.</param>
    /// <param name="schemaLookupProofId">The executable schema lookup proof id.</param>
    /// <param name="contractVersionNegotiationId">The executable contract-version negotiation proof id.</param>
    /// <param name="upcasterExecutionProofId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationProofId">The executable compatibility validation proof id.</param>
    /// <param name="wireContractProofId">The overall wire-contract proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus wire-contract proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string payloadSerializationProofId,
        string wireEnvelopeSchemaId,
        string schemaLookupProofId,
        string contractVersionNegotiationId,
        string upcasterExecutionProofId,
        string compatibilityValidationProofId,
        string wireContractProofId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            payloadSerializationProofId,
            wireEnvelopeSchemaId,
            schemaLookupProofId,
            contractVersionNegotiationId,
            upcasterExecutionProofId,
            compatibilityValidationProofId,
            wireContractProofId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported wire-contract proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported wire-contract ownership.</param>
    /// <param name="payloadSerializationProofId">The executable payload serialization proof id.</param>
    /// <param name="wireEnvelopeSchemaId">The executable wire-envelope schema proof id.</param>
    /// <param name="schemaLookupProofId">The executable schema lookup proof id.</param>
    /// <param name="contractVersionNegotiationId">The executable contract-version negotiation proof id.</param>
    /// <param name="upcasterExecutionProofId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationProofId">The executable compatibility validation proof id.</param>
    /// <param name="wireContractProofId">The overall wire-contract proof id.</param>
    /// <returns><see langword="true" /> when wire-contract proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string payloadSerializationProofId,
        string wireEnvelopeSchemaId,
        string schemaLookupProofId,
        string contractVersionNegotiationId,
        string upcasterExecutionProofId,
        string compatibilityValidationProofId,
        string wireContractProofId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedPayloadSerializationProofId = RequireValue(payloadSerializationProofId, nameof(payloadSerializationProofId));
        var normalizedWireEnvelopeSchemaId = RequireValue(wireEnvelopeSchemaId, nameof(wireEnvelopeSchemaId));
        var normalizedSchemaLookupProofId = RequireValue(schemaLookupProofId, nameof(schemaLookupProofId));
        var normalizedContractVersionNegotiationId = RequireValue(contractVersionNegotiationId, nameof(contractVersionNegotiationId));
        var normalizedUpcasterExecutionProofId = RequireValue(upcasterExecutionProofId, nameof(upcasterExecutionProofId));
        var normalizedCompatibilityValidationProofId = RequireValue(compatibilityValidationProofId, nameof(compatibilityValidationProofId));
        var normalizedWireContractProofId = RequireValue(wireContractProofId, nameof(wireContractProofId));

        metadata[EventDispatchRuntimeMetadataKeys.WireContractOwnership] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.WireContractOwnershipSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.WireContractDurability] = "durable";
        metadata[EventDispatchRuntimeMetadataKeys.WireContractScope] = "cross-node";
        metadata[EventDispatchRuntimeMetadataKeys.WireContractPayloadSerialization] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.WireContractPayloadSerializationId] = normalizedPayloadSerializationProofId;
        metadata[EventDispatchRuntimeMetadataKeys.WireEnvelopeSchema] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.WireEnvelopeSchemaId] = normalizedWireEnvelopeSchemaId;
        metadata[EventDispatchRuntimeMetadataKeys.WireContractSchemaLookup] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.WireContractSchemaLookupId] = normalizedSchemaLookupProofId;
        metadata[EventDispatchRuntimeMetadataKeys.ContractVersionNegotiation] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ContractVersionNegotiationId] = normalizedContractVersionNegotiationId;
        metadata[EventDispatchRuntimeMetadataKeys.WireContractUpcasterExecution] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.WireContractUpcasterExecutionId] = normalizedUpcasterExecutionProofId;
        metadata[EventDispatchRuntimeMetadataKeys.WireContractCompatibilityValidation] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.WireContractCompatibilityValidationId] = normalizedCompatibilityValidationProofId;
        metadata[EventDispatchRuntimeMetadataKeys.WireContractProofId] = normalizedWireContractProofId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported wire-contract proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete wire-contract proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsWireContractProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractOwnership, "provider-reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireContractOwnershipSource) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractDurability, "durable") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractScope, "cross-node") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractPayloadSerialization, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireContractPayloadSerializationId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireEnvelopeSchema, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireEnvelopeSchemaId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractSchemaLookup, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireContractSchemaLookupId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ContractVersionNegotiation, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ContractVersionNegotiationId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractUpcasterExecution, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireContractUpcasterExecutionId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractCompatibilityValidation, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireContractCompatibilityValidationId) &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireContractProofId);
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
