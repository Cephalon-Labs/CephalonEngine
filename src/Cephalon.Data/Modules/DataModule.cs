using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.Configuration;
using Cephalon.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Data.Modules;

internal sealed class DataModule(DataRuntimeOptions options) : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "data-runtime",
        displayName: "Data Runtime",
        description: "Runtime-neutral command and query dispatching for Cephalon data workloads.",
        tags: ["data", "cqrs", "runtime"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "data-runtime"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);
        if (options.RegisterReadStore)
        {
            services.TryAddScoped<Abstractions.Data.IReadStore, HandlerDispatchingReadStore>();
        }

        if (options.RegisterWriteStore)
        {
            services.TryAddScoped<Abstractions.Data.IWriteStore, HandlerDispatchingWriteStore>();
        }

        services.TryAddSingleton<CdcCaptureRuntimeStateCatalog>();
        services.TryAddSingleton<Abstractions.Data.ICdcCaptureRuntimeStateCatalog>(static serviceProvider =>
            serviceProvider.GetRequiredService<CdcCaptureRuntimeStateCatalog>());
        services.TryAddSingleton<ICdcCaptureRuntimeReporter>(static serviceProvider =>
            serviceProvider.GetRequiredService<CdcCaptureRuntimeStateCatalog>());
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        if (options.RegisterReadStore)
        {
            capabilities.Add(new Capability(
                key: "data.read",
                displayName: "Data Read Store",
                description: "Executes query handlers through the active Cephalon data runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data"
                }));
        }

        if (options.RegisterWriteStore)
        {
            capabilities.Add(new Capability(
                key: "data.write",
                displayName: "Data Write Store",
                description: "Executes command handlers through the active Cephalon data runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data"
                }));
        }
    }
}
