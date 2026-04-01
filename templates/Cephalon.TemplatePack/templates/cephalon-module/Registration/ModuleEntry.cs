using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Modules;
using CephalonTemplateModule.Application;
using Microsoft.Extensions.DependencyInjection;

namespace CephalonTemplateModule.Registration;

public sealed class ModuleEntry : ModuleBase, ILocalizedResourceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "template-module",
        displayName: "Template Module",
        description: "Starter Cephalon module package generated from dotnet new.",
        tags: ["starter", "module"],
        version: "0.1.0-preview");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ModuleStatusService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "template-module.status",
            displayName: "Template module status",
            description: "Exposes starter lifecycle state from the generated module package."));
    }

    public void RegisterResources(ILocalizedResourceRegistry resources)
    {
        resources.Add("en", "module.template.status", "Template module is running.");
        resources.Add("th", "module.template.status", "โมดูลตัวอย่างพร้อมทำงาน");
    }

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<ModuleStatusService>().MarkInitialized();
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<ModuleStatusService>().MarkStarted();
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<ModuleStatusService>().MarkStopped();
        return Task.CompletedTask;
    }
}
