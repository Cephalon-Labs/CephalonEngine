using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the bulkhead-isolation inputs resolved for a Cephalon app.
/// </summary>
public sealed class BulkheadSelection
{
    /// <summary>
    /// Gets an empty bulkhead-selection instance.
    /// </summary>
    public static BulkheadSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadSelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether bulkhead isolation was explicitly enabled.</param>
    /// <param name="maxConcurrentExecutions">The maximum concurrent executions allowed inside the bulkhead.</param>
    /// <param name="maxQueuedActions">The maximum queued actions allowed before rejection.</param>
    [JsonConstructor]
    public BulkheadSelection(
        bool? enabled = null,
        int? maxConcurrentExecutions = null,
        int? maxQueuedActions = null)
    {
        Enabled = enabled;
        MaxConcurrentExecutions = maxConcurrentExecutions;
        MaxQueuedActions = maxQueuedActions;
    }

    /// <summary>
    /// Gets a value indicating whether bulkhead isolation was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the maximum concurrent executions allowed inside the bulkhead.
    /// </summary>
    public int? MaxConcurrentExecutions { get; }

    /// <summary>
    /// Gets the maximum queued actions allowed before rejection.
    /// </summary>
    public int? MaxQueuedActions { get; }

    /// <summary>
    /// Gets a value indicating whether any bulkhead-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        MaxConcurrentExecutions.HasValue ||
        MaxQueuedActions.HasValue;
}
