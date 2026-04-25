using Wolverine;

namespace Cephalon.Eventing.Wolverine.Configuration;

/// <summary>
/// Describes the host-owned options for the optional Wolverine eventing companion pack.
/// </summary>
public sealed class WolverineEventingOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineEventingOptions" /> class.
    /// </summary>
    public WolverineEventingOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pack should register Wolverine host wiring into the current service collection.
    /// </summary>
    public bool EnableHostWiring { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should publish its runtime surface into Cephalon technology introspection.
    /// </summary>
    public bool EnableRuntimeSurface { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should own the durable staged-event dispatch loop instead of leaving dispatch consumer-managed. Defaults to <see langword="false" />.
    /// </summary>
    public bool EnableDispatchLoop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pack should execute declared event subscriptions through the Wolverine-managed staged-event dispatch path. Defaults to <see langword="false" />.
    /// </summary>
    public bool EnableSubscriptionExecution { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of staged events the Wolverine-owned dispatch loop should read per polling cycle.
    /// </summary>
    public int DispatchBatchSize { get; set; } = 25;

    /// <summary>
    /// Gets or sets the number of seconds the Wolverine-owned dispatch loop should wait between polling cycles.
    /// </summary>
    public int DispatchPollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the number of seconds the Wolverine-owned dispatch loop should wait before retrying a failed dispatch attempt.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of seconds the Wolverine-managed subscription execution path should wait before requeueing a failed subscription attempt.
    /// </summary>
    public int SubscriptionRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets an optional callback that can extend Wolverine host wiring before the runtime starts.
    /// </summary>
    public Action<WolverineOptions>? ConfigureHost { get; set; }
}
