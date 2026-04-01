using Microsoft.Extensions.DependencyInjection;

using Cephalon.Abstractions.Capabilities;

namespace Cephalon.Abstractions.Modules;

public interface IModule
{
    ModuleDescriptor Descriptor { get; }

    void ConfigureServices(IServiceCollection services);

    void RegisterCapabilities(ICapabilityRegistry capabilities);
}
