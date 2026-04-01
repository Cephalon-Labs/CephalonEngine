using Microsoft.Extensions.DependencyInjection;

using Cephalon.Abstractions.Capabilities;

namespace Cephalon.Abstractions.Modules;

/// <summary>
/// Provides default no-op implementations for module and lifecycle contracts.
/// </summary>
public abstract class ModuleBase : IModule, IModuleLifecycle
{
    /// <summary>
    /// Gets the module descriptor used for discovery, ordering, and manifest output.
    /// </summary>
    public abstract ModuleDescriptor Descriptor { get; }

    /// <summary>
    /// Configures services required by the module.
    /// </summary>
    /// <param name="services">The service collection receiving module services.</param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Registers capabilities exposed by the module.
    /// </summary>
    /// <param name="capabilities">The capability registry receiving module capabilities.</param>
    public virtual void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    /// <summary>
    /// Initializes the module before the runtime starts serving work.
    /// </summary>
    /// <param name="context">The module runtime context.</param>
    /// <param name="cancellationToken">A token that cancels initialization.</param>
    /// <returns>A task that completes when initialization finishes.</returns>
    public virtual Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the module after initialization has completed.
    /// </summary>
    /// <param name="context">The module runtime context.</param>
    /// <param name="cancellationToken">A token that cancels startup.</param>
    /// <returns>A task that completes when startup finishes.</returns>
    public virtual Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the module during runtime shutdown.
    /// </summary>
    /// <param name="context">The module runtime context.</param>
    /// <param name="cancellationToken">A token that cancels shutdown.</param>
    /// <returns>A task that completes when shutdown finishes.</returns>
    public virtual Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
