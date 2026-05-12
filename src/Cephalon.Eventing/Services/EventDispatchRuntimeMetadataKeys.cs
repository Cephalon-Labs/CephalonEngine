namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines stable metadata keys used by event-dispatch runtime observations.
/// </summary>
/// <remarks>
/// These keys appear in dispatch runtime reports and the derived event-dispatch runtime surfaces
/// so operators and dispatch stores can distinguish retryable failures from terminal failures
/// without parsing provider-specific metadata.
/// </remarks>
public static class EventDispatchRuntimeMetadataKeys
{
    /// <summary>
    /// Identifies the next UTC time when a retryable dispatch failure should become eligible again.
    /// </summary>
    public const string NextRetryAtUtc = "nextRetryAtUtc";

    /// <summary>
    /// Identifies the retry policy applied by the active dispatch runtime.
    /// </summary>
    public const string RetryPolicy = "retryPolicy";

    /// <summary>
    /// Identifies the maximum number of dispatch attempts allowed for one staged message.
    /// </summary>
    public const string RetryMaxAttempts = "retryMaxAttempts";

    /// <summary>
    /// Identifies the retry delay in seconds when the active dispatch runtime uses a delayed retry policy.
    /// </summary>
    public const string RetryDelaySeconds = "retryDelaySeconds";

    /// <summary>
    /// Identifies where retry eligibility is persisted.
    /// </summary>
    public const string RetryDurability = "retryDurability";

    /// <summary>
    /// Identifies who owns the retry policy.
    /// </summary>
    public const string RetryScope = "retryScope";

    /// <summary>
    /// Identifies the retry decision represented by the latest observation.
    /// </summary>
    public const string RetryOutcome = "retryOutcome";

    /// <summary>
    /// Identifies whether the retry budget was exhausted for the latest observation.
    /// </summary>
    public const string RetryExhausted = "retryExhausted";

    /// <summary>
    /// Identifies whether the latest failure should stop re-entering pending-dispatch reads.
    /// </summary>
    public const string TerminalFailure = "terminalFailure";

    /// <summary>
    /// Identifies the dead-letter decision represented by the latest operator observation.
    /// </summary>
    public const string DeadLetterOutcome = "deadLetterOutcome";

    /// <summary>
    /// Identifies the scope that owns the dead-letter decision.
    /// </summary>
    public const string DeadLetterScope = "deadLetterScope";

    /// <summary>
    /// Identifies where the dead-letter decision is persisted.
    /// </summary>
    public const string DeadLetterDurability = "deadLetterDurability";

    /// <summary>
    /// Identifies whether the dead-letter decision is owned by a broker-specific dead-letter queue.
    /// </summary>
    public const string BrokerDeadLetter = "brokerDeadLetter";

    /// <summary>
    /// Identifies the durable dispatch context propagation boundary proven by the latest runtime observation.
    /// </summary>
    public const string DurableDispatchContextPropagation = "durableDispatchContextPropagation";

    /// <summary>
    /// Identifies whether provider or broker headers carry the same context beyond Cephalon dispatch metadata.
    /// </summary>
    public const string ProviderBrokerContextHeaders = "providerBrokerContextHeaders";

    /// <summary>
    /// Identifies the provider-neutral projection used for provider or broker context headers.
    /// </summary>
    public const string ProviderBrokerContextHeaderProjection = "providerBrokerContextHeaderProjection";

    /// <summary>
    /// Identifies the number of Cephalon context headers projected toward the provider or broker boundary.
    /// </summary>
    public const string ProviderBrokerContextHeaderCount = "providerBrokerContextHeaderCount";

    /// <summary>
    /// Identifies the comma-separated Cephalon context header names projected toward the provider or broker boundary.
    /// </summary>
    public const string ProviderBrokerContextHeaderNames = "providerBrokerContextHeaderNames";

    /// <summary>
    /// Identifies whether consumer-side extraction has been proven for the dispatched context.
    /// </summary>
    public const string ConsumerContextExtraction = "consumerContextExtraction";

    /// <summary>
    /// Identifies whether cross-node context handoff has been proven for the dispatched context.
    /// </summary>
    public const string CrossNodeContextHandoff = "crossNodeContextHandoff";

    /// <summary>
    /// Identifies whether the latest dispatch runtime observation includes Cephalon context metadata.
    /// </summary>
    public const string DispatchContextMetadata = "dispatchContextMetadata";

    /// <summary>
    /// Identifies the number of context-capable headers present on the dispatch item used by the report.
    /// </summary>
    public const string DispatchContextHeaderCount = "dispatchContextHeaderCount";

    /// <summary>
    /// Identifies the number of context metadata entries carried from the dispatch item into the report.
    /// </summary>
    public const string DispatchContextMetadataCount = "dispatchContextMetadataCount";

    /// <summary>
    /// Gets a value indicating whether the supplied metadata describes a terminal failure.
    /// </summary>
    /// <param name="metadata">The dispatch observation metadata to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when either <see cref="TerminalFailure" /> or
    /// <see cref="RetryExhausted" /> is set to <c>true</c>; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsTerminalFailure(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return IsTrue(metadata, TerminalFailure) || IsTrue(metadata, RetryExhausted);
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) &&
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
