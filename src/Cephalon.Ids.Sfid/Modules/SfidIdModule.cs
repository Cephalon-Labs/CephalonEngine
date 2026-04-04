using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Ids;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.AppModel;
using Cephalon.Ids.Sfid.Configuration;
using Cephalon.Ids.Sfid.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SfidNet.Abstractions;

namespace Cephalon.Ids.Sfid.Modules;

internal sealed class SfidIdModule(Action<SfidIdOptions>? configureOptions) : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "sfid-id-strategy",
        displayName: "Sfid Id Strategy",
        description: "Official Sfid.Net-backed identifier generation for Cephalon runtimes.",
        tags: ["ids", "sfid", "data"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "id-strategy"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var options = SfidIdOptions.FromConfiguration(configuration);
            configureOptions?.Invoke(options);
            return options;
        });

        services.TryAddSingleton<ISfidGenerator>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<SfidIdOptions>();
            var timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

            return SfidGeneratorFactory.Create(options, timeProvider);
        });

        services.TryAddSingleton<IIdGenerator>(serviceProvider =>
        {
            var generator = serviceProvider.GetRequiredService<ISfidGenerator>();
            var appProfile = serviceProvider.GetRequiredService<AppProfile>();

            return new SfidIdGenerator(generator, appProfile);
        });
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}
