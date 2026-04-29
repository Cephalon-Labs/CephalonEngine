using Cephalon.Eventing.Services;

namespace Cephalon.Eventing.Configuration;

/// <summary>
/// Configures the built-in eventing runtime pack.
/// </summary>
/// <remarks>
/// These options seed the host-owned part of the eventing runtime. Installed modules can still
/// contribute additional channels through <see cref="Services.IEventChannelContributor" /> and
/// additional subscription descriptors through <see cref="Services.IEventSubscriptionContributor" />.
/// </remarks>
public sealed class EventingOptions
{
    /// <summary>
    /// Creates eventing options with the default host-owned features enabled.
    /// </summary>
    public EventingOptions()
    {
    }

    /// <summary>
    /// Gets the host-defined event channels that should be available to the eventing runtime.
    /// </summary>
    public IList<EventChannelDescriptor> Channels { get; } = [];

    /// <summary>
    /// Gets the host-defined event subscription descriptors that should be available to the eventing runtime.
    /// </summary>
    public IList<EventSubscriptionDescriptor> Subscriptions { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether publishing features are enabled.
    /// </summary>
    public bool EnablePublishing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether subscription features are enabled.
    /// </summary>
    public bool EnableSubscriptions { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the core eventing pack should execute matching
    /// subscription executors directly inside the current process when a publication is accepted.
    /// </summary>
    /// <remarks>
    /// This is an opt-in managed execution baseline for lightweight hosts and tests. It is not a
    /// durable broker, inbox, or retry runtime; companion packs should still own those richer
    /// delivery guarantees when they are selected.
    /// </remarks>
    public bool EnableInProcessSubscriptionExecution { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the in-process publisher should continue executing
    /// later subscriptions on the same channel after one subscription fails.
    /// </summary>
    /// <remarks>
    /// The publisher still reports failed subscriptions and throws after the publication attempt
    /// finishes. This setting only controls whether independent subscriptions on the same channel
    /// get a chance to run before the failure is returned to the caller.
    /// </remarks>
    public bool ContinueInProcessSubscriptionExecutionAfterFailure { get; set; } = true;
}
