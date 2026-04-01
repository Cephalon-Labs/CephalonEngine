using Microsoft.Extensions.DependencyInjection;

using Cephalon.Abstractions.Capabilities;

namespace Cephalon.Abstractions.Modules;

public abstract class ModuleBase : IModule, IModuleLifecycle
{
    public abstract ModuleDescriptor Descriptor { get; }

    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public virtual void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public virtual Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
