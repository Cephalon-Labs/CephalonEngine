namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one active durable-execution workflow visible to the current runtime.
/// </summary>
/// <remarks>
/// This runtime-facing surface keeps durable workflow truth derived from the shared behavior
/// topology and registered implementation types instead of inventing a host-only workflow
/// registry. It is intentionally static and operator-facing: it describes the active durable
/// contract shape, ownership, transports, and replay semantics rather than per-invocation state.
/// </remarks>
public sealed class DurableExecutionRuntimeDescriptor
{
    /// <summary>
    /// Creates a durable-execution runtime descriptor.
    /// </summary>
    /// <param name="id">The stable durable behavior identifier.</param>
    /// <param name="displayName">The operator-facing durable workflow name.</param>
    /// <param name="description">A human-readable description of the durable workflow.</param>
    /// <param name="behaviorType">The concrete durable behavior implementation type name.</param>
    /// <param name="inputType">The durable workflow input type name.</param>
    /// <param name="stateType">The durable workflow replay-state type name.</param>
    /// <param name="outputType">The durable workflow local output type name.</param>
    /// <param name="executionMode">
    /// The replay mode used by the runtime, such as <c>event-store-replay</c>.
    /// </param>
    /// <param name="sourceModuleId">
    /// The owning module identifier when the workflow came from an explicit module-owned behavior.
    /// </param>
    /// <param name="transportIds">The transport identifiers that expose the durable workflow.</param>
    /// <param name="requiredFeatureFlagIds">
    /// The ordered feature-flag identifiers that must resolve to enabled before the workflow can
    /// execute.
    /// </param>
    /// <param name="eventSourcingEnabled">
    /// Indicates whether the authored behavior topology explicitly enables event sourcing for the
    /// workflow.
    /// </param>
    /// <param name="requiresEventStore">
    /// Indicates whether the runtime contract requires an <c>IEventStore</c> to execute truthfully.
    /// </param>
    /// <param name="successStatusCodes">
    /// The HTTP success status codes the shared durable execution strategy can return for local
    /// output, continuation-only work, pending timer/signal coordination, or completion without
    /// output.
    /// </param>
    /// <param name="metadata">Additional operator-facing metadata describing replay semantics.</param>
    public DurableExecutionRuntimeDescriptor(
        string id,
        string displayName,
        string description,
        string behaviorType,
        string inputType,
        string stateType,
        string outputType,
        string executionMode,
        string? sourceModuleId = null,
        IReadOnlyList<string>? transportIds = null,
        IReadOnlyList<string>? requiredFeatureFlagIds = null,
        bool eventSourcingEnabled = true,
        bool requiresEventStore = true,
        IReadOnlyList<int>? successStatusCodes = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Durable execution id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Durable execution display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Durable execution description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(behaviorType))
        {
            throw new ArgumentException("Durable execution behavior type is required.", nameof(behaviorType));
        }

        if (string.IsNullOrWhiteSpace(inputType))
        {
            throw new ArgumentException("Durable execution input type is required.", nameof(inputType));
        }

        if (string.IsNullOrWhiteSpace(stateType))
        {
            throw new ArgumentException("Durable execution state type is required.", nameof(stateType));
        }

        if (string.IsNullOrWhiteSpace(outputType))
        {
            throw new ArgumentException("Durable execution output type is required.", nameof(outputType));
        }

        if (string.IsNullOrWhiteSpace(executionMode))
        {
            throw new ArgumentException("Durable execution mode is required.", nameof(executionMode));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        BehaviorType = behaviorType.Trim();
        InputType = inputType.Trim();
        StateType = stateType.Trim();
        OutputType = outputType.Trim();
        ExecutionMode = executionMode.Trim();
        SourceModuleId = NormalizeOptional(sourceModuleId);
        TransportIds = NormalizeOrderedStrings(transportIds);
        RequiredFeatureFlagIds = NormalizeOrderedStrings(requiredFeatureFlagIds);
        EventSourcingEnabled = eventSourcingEnabled;
        RequiresEventStore = requiresEventStore;
        SuccessStatusCodes = NormalizeStatusCodes(successStatusCodes);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable durable behavior identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing durable workflow name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable durable workflow description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the concrete durable behavior implementation type name.
    /// </summary>
    public string BehaviorType { get; }

    /// <summary>
    /// Gets the durable workflow input type name.
    /// </summary>
    public string InputType { get; }

    /// <summary>
    /// Gets the durable workflow replay-state type name.
    /// </summary>
    public string StateType { get; }

    /// <summary>
    /// Gets the durable workflow local output type name.
    /// </summary>
    public string OutputType { get; }

    /// <summary>
    /// Gets the replay mode used by the active runtime.
    /// </summary>
    public string ExecutionMode { get; }

    /// <summary>
    /// Gets the owning module identifier when one is known at runtime.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets the transport identifiers that expose the durable workflow.
    /// </summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>
    /// Gets the ordered feature-flag identifiers that gate workflow execution.
    /// </summary>
    public IReadOnlyList<string> RequiredFeatureFlagIds { get; }

    /// <summary>
    /// Gets a value indicating whether the authored behavior topology explicitly enables event sourcing.
    /// </summary>
    public bool EventSourcingEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the runtime contract requires an <c>IEventStore</c>.
    /// </summary>
    public bool RequiresEventStore { get; }

    /// <summary>
    /// Gets the HTTP success status codes the shared durable strategy can return.
    /// </summary>
    public IReadOnlyList<int> SuccessStatusCodes { get; }

    /// <summary>
    /// Gets additional operator-facing metadata describing replay semantics.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string[] NormalizeOrderedStrings(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static int[] NormalizeStatusCodes(IReadOnlyList<int>? statusCodes)
    {
        var normalized = (statusCodes is null || statusCodes.Count == 0
                ? [200, 202, 204]
                : statusCodes)
            .Distinct()
            .OrderBy(static statusCode => statusCode)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one durable execution success status code is required.", nameof(statusCodes));
        }

        if (normalized.Any(static statusCode => statusCode < 100 || statusCode > 999))
        {
            throw new ArgumentOutOfRangeException(nameof(statusCodes), "Durable execution status codes must stay in the HTTP status-code range.");
        }

        return normalized;
    }
}
