using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.ReferenceModule.Operations.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.ReferenceModule.Operations.Registration;

public sealed class OperationsModule : ModuleBase, IRestModule, ILocalizedResourceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "operations",
        displayName: "Operations",
        description: "Reference module package that demonstrates lifecycle-aware operational status.",
        tags: ["reference", "operations", "rest"],
        version: "0.1.0-preview",
        metadata: new Dictionary<string, string>
        {
            ["sample"] = "reference-module",
            ["transport"] = "rest"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<OperationsStatusService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "operations.status",
            displayName: "Operations status",
            description: "Returns lifecycle-aware operational status from the reference module package."));
        capabilities.Add(new Capability(
            key: "operations.localization",
            displayName: "Operations localization",
            description: "Contributes package-owned localized text for the reference module package."));
    }

    public void RegisterResources(ILocalizedResourceRegistry resources)
    {
        resources.Add("en", "module.operations.status.title", "Operations module is running.");
        resources.Add("th", "module.operations.status.title", "โมดูล Operations พร้อมทำงาน");
    }

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<OperationsStatusService>().MarkInitialized();
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<OperationsStatusService>().MarkStarted();
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<OperationsStatusService>().MarkStopped();
        return Task.CompletedTask;
    }

    public void MapRestEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/operations");

        group.MapGet("/status", (HttpContext context, OperationsStatusService status, ILocalizedTextCatalog textCatalog) =>
        {
            var culture = context.Request.Query["culture"].FirstOrDefault() ?? textCatalog.DefaultCulture;
            var message = textCatalog.ResolveText(
                key: "module.operations.status.title",
                culture: culture,
                fallback: "Operations module is running.");

            return TypedResults.Ok(status.CreateEnvelope(culture, message));
        });
    }
}
