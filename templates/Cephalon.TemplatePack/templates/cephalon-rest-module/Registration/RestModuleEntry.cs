using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using CephalonTemplateModule.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CephalonTemplateModule.Registration;

public sealed class RestModuleEntry : ModuleBase, IRestModule, ILocalizedResourceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "template-rest-module",
        displayName: "Template REST Module",
        description: "Starter Cephalon REST module package generated from dotnet new.",
        tags: ["starter", "module", "rest"],
        version: "0.1.0-preview");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<RestModuleStatusService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "template-rest-module.status",
            displayName: "Template REST module status",
            description: "Exposes a localized REST status surface from the generated module package."));
    }

    public void RegisterResources(ILocalizedResourceRegistry resources)
    {
        resources.Add("en", "module.template.rest.status", "Template REST module is running.");
        resources.Add("th", "module.template.rest.status", "โมดูล REST ตัวอย่างพร้อมทำงาน");
    }

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<RestModuleStatusService>().MarkInitialized();
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<RestModuleStatusService>().MarkStarted();
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<RestModuleStatusService>().MarkStopped();
        return Task.CompletedTask;
    }

    public void MapRestEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/template-module");

        group.MapGet("/status", (HttpContext context, RestModuleStatusService status, ILocalizedTextCatalog textCatalog) =>
        {
            var culture = context.Request.Query["culture"].FirstOrDefault() ?? textCatalog.DefaultCulture;
            var message = textCatalog.ResolveText(
                key: "module.template.rest.status",
                culture: culture,
                fallback: "Template REST module is running.");

            return TypedResults.Ok(status.CreateEnvelope(culture, message));
        });
    }
}
