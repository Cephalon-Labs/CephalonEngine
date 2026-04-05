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
}
