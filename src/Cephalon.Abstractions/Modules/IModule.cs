using Microsoft.Extensions.DependencyInjection;

using Cephalon.Abstractions.Capabilities;

namespace Cephalon.Abstractions.Modules;

/// <summary>
/// Defines the host-agnostic contract that every Cephalon module implements.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the module descriptor used for discovery, ordering, and manifest output.
    /// </summary>
    ModuleDescriptor Descriptor { get; }

    /// <summary>
    /// Configures services required by the module.
    /// </summary>
    /// <param name="services">The service collection receiving module services.</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Registers capabilities exposed by the module.
    /// </summary>
    /// <param name="capabilities">The capability registry receiving module capabilities.</param>
    void RegisterCapabilities(ICapabilityRegistry capabilities);
}
