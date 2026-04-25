using System.Threading;
using System.Threading.Tasks;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Hosting;
using CephalonTemplateModule.Application;
using Microsoft.Extensions.DependencyInjection;

namespace CephalonTemplateModule.Registration;

public sealed class RestBehaviorModuleEntry : RestBehaviorModuleBase, ILocalizedResourceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "template-rest-behavior-module",
        displayName: "Template REST Behavior Module",
        description: "Starter Cephalon behavior-backed REST module package generated from dotnet new.",
        tags: ["starter", "module", "rest", "behaviors"],
        version: "0.1.0-preview");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<RestBehaviorModuleStatusService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "template-rest-behavior-module.status",
            displayName: "Template REST behavior module status",
            description: "Exposes a behavior-backed REST status surface from the generated module package."));
    }

    public void RegisterResources(ILocalizedResourceRegistry resources)
    {
        resources.Add("en", "module.template.behavior-rest.status", "Template behavior-backed REST module is running.");
        resources.Add("th", "module.template.behavior-rest.status", "โมดูล REST แบบ behavior-backed ตัวอย่างพร้อมทำงาน");
    }

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<RestBehaviorModuleStatusService>().MarkInitialized();
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<RestBehaviorModuleStatusService>().MarkStarted();
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<RestBehaviorModuleStatusService>().MarkStopped();
        return Task.CompletedTask;
    }

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Group("/template-module")
            .WithTagName("Template Module API")
            .MapProfile<GetModuleStatusBehavior>();
    }
}
