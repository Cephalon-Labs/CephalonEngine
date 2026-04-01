using Cephalon.Edge.Services;

namespace Cephalon.Edge.Configuration;

/// <summary>
/// Configures the built-in edge runtime pack.
/// </summary>
/// <remarks>
/// These options seed the host-owned part of the edge runtime. Installed modules can still
/// contribute additional nodes through <see cref="Services.IEdgeNodeContributor" />.
/// </remarks>
public sealed class EdgeRuntimeOptions
{
    /// <summary>
    /// Gets the host-defined edge nodes that should be available to the edge runtime.
    /// </summary>
    public IList<EdgeNodeDescriptor> Nodes { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether offline mode features are enabled.
    /// </summary>
    public bool EnableOfflineMode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether synchronization features are enabled.
    /// </summary>
    public bool EnableSynchronization { get; set; } = true;
}
