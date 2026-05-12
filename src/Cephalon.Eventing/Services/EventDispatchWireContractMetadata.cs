namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds executable wire-contract proof metadata for successful dispatch reports.
/// </summary>
/// <remarks>
/// Serialization execution proof records payload serialization, schema lookup, upcaster execution, compatibility
/// validation, and provider serialization. This helper layers the remaining wire-contract proof on top of that base:
/// wire-envelope schema materialization, contract-version negotiation, and a complete wire-contract proof id.
/// </remarks>
public static class EventDispatchWireContractMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with executable wire-contract proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful dispatch report to copy.</param>
    /// <param name="source">The stable provider or engine runtime source that reported executable wire-contract ownership.</param>
    /// <param name="payloadSerializationExecutionId">The executable payload serialization proof id.</param>
    /// <param name="wireEnvelopeSchemaExecutionId">The executable wire-envelope schema proof id.</param>
    /// <param name="schemaLookupExecutionId">The executable schema lookup proof id.</param>
    /// <param name="contractVersionNegotiationExecutionId">The executable contract-version negotiation proof id.</param>
    /// <param name="upcasterExecutionId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationExecutionId">The executable compatibility validation proof id.</param>
    /// <param name="providerSerializationId">The provider-owned serialization proof id.</param>
    /// <param name="wireContractProofId">The complete executable wire-contract proof id.</param>
    /// <returns>A dispatch report containing the original metadata plus executable wire-contract proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string payloadSerializationExecutionId,
        string wireEnvelopeSchemaExecutionId,
        string schemaLookupExecutionId,
        string contractVersionNegotiationExecutionId,
        string upcasterExecutionId,
        string compatibilityValidationExecutionId,
        string providerSerializationId,
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
                payloadSerializationExecutionId,
                wireEnvelopeSchemaExecutionId,
                schemaLookupExecutionId,
                contractVersionNegotiationExecutionId,
                upcasterExecutionId,
                compatibilityValidationExecutionId,
                providerSerializationId,
                wireContractProofId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with executable wire-contract proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or engine runtime source that reported executable wire-contract ownership.</param>
    /// <param name="payloadSerializationExecutionId">The executable payload serialization proof id.</param>
    /// <param name="wireEnvelopeSchemaExecutionId">The executable wire-envelope schema proof id.</param>
    /// <param name="schemaLookupExecutionId">The executable schema lookup proof id.</param>
    /// <param name="contractVersionNegotiationExecutionId">The executable contract-version negotiation proof id.</param>
    /// <param name="upcasterExecutionId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationExecutionId">The executable compatibility validation proof id.</param>
    /// <param name="providerSerializationId">The provider-owned serialization proof id.</param>
    /// <param name="wireContractProofId">The complete executable wire-contract proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus executable wire-contract proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string payloadSerializationExecutionId,
        string wireEnvelopeSchemaExecutionId,
        string schemaLookupExecutionId,
        string contractVersionNegotiationExecutionId,
        string upcasterExecutionId,
        string compatibilityValidationExecutionId,
        string providerSerializationId,
        string wireContractProofId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            payloadSerializationExecutionId,
            wireEnvelopeSchemaExecutionId,
            schemaLookupExecutionId,
            contractVersionNegotiationExecutionId,
            upcasterExecutionId,
            compatibilityValidationExecutionId,
            providerSerializationId,
            wireContractProofId);
        return result;
    }

    /// <summary>
    /// Applies executable wire-contract proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or engine runtime source that reported executable wire-contract ownership.</param>
    /// <param name="payloadSerializationExecutionId">The executable payload serialization proof id.</param>
    /// <param name="wireEnvelopeSchemaExecutionId">The executable wire-envelope schema proof id.</param>
    /// <param name="schemaLookupExecutionId">The executable schema lookup proof id.</param>
    /// <param name="contractVersionNegotiationExecutionId">The executable contract-version negotiation proof id.</param>
    /// <param name="upcasterExecutionId">The executable upcaster execution proof id.</param>
    /// <param name="compatibilityValidationExecutionId">The executable compatibility validation proof id.</param>
    /// <param name="providerSerializationId">The provider-owned serialization proof id.</param>
    /// <param name="wireContractProofId">The complete executable wire-contract proof id.</param>
    /// <returns><see langword="true" /> when executable wire-contract proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string payloadSerializationExecutionId,
        string wireEnvelopeSchemaExecutionId,
        string schemaLookupExecutionId,
        string contractVersionNegotiationExecutionId,
        string upcasterExecutionId,
        string compatibilityValidationExecutionId,
        string providerSerializationId,
        string wireContractProofId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        EventDispatchSerializationExecutionMetadata.TryApplyMetadata(
            metadata,
            outcome,
            source,
            payloadSerializationExecutionId,
            schemaLookupExecutionId,
            upcasterExecutionId,
            compatibilityValidationExecutionId,
            providerSerializationId);

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedWireEnvelopeSchemaExecutionId = RequireValue(wireEnvelopeSchemaExecutionId, nameof(wireEnvelopeSchemaExecutionId));
        var normalizedContractVersionNegotiationExecutionId = RequireValue(contractVersionNegotiationExecutionId, nameof(contractVersionNegotiationExecutionId));
        var normalizedWireContractProofId = RequireValue(wireContractProofId, nameof(wireContractProofId));

        metadata[EventDispatchRuntimeMetadataKeys.WireContractOwnership] = "runtime-reported";
        metadata[EventDispatchRuntimeMetadataKeys.WireContractOwnershipSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.WireEnvelopeSchemaExecution] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.WireEnvelopeSchemaExecutionId] = normalizedWireEnvelopeSchemaExecutionId;
        metadata[EventDispatchRuntimeMetadataKeys.ContractVersionNegotiationExecution] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ContractVersionNegotiationExecutionId] = normalizedContractVersionNegotiationExecutionId;
        metadata[EventDispatchRuntimeMetadataKeys.WireContractProofId] = normalizedWireContractProofId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete executable wire-contract proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete executable wire-contract proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsWireContractProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return EventDispatchSerializationExecutionMetadata.IsSerializationExecutionProven(metadata) &&
            HasAnyValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractOwnership, "runtime-reported", "provider-reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireContractOwnershipSource) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.WireEnvelopeSchemaExecution, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireEnvelopeSchemaExecutionId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ContractVersionNegotiationExecution, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ContractVersionNegotiationExecutionId) &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.WireContractProofId);
    }

    private static bool HasAnyValue(IReadOnlyDictionary<string, string> metadata, string key, params string[] expectedValues)
    {
        if (!metadata.TryGetValue(key, out var value))
        {
            return false;
        }

        foreach (var expectedValue in expectedValues)
        {
            if (string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
