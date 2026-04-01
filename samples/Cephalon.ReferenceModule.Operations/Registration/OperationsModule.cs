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

/// <summary>
/// Registers the reference operations module, including lifecycle tracking and REST endpoints.
/// </summary>
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

    /// <summary>
    /// Gets the module descriptor exposed by the reference operations package.
    /// </summary>
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <summary>
    /// Registers the services required by the operations module.
    /// </summary>
    /// <param name="services">
    /// The service collection used to compose the host.
    /// </param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<OperationsStatusService>();
    }

    /// <summary>
    /// Registers the capabilities exposed by the operations module.
    /// </summary>
    /// <param name="capabilities">
    /// The capability registry used during module discovery.
    /// </param>
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

    /// <summary>
    /// Contributes localized text owned by the operations module package.
    /// </summary>
    /// <param name="resources">
    /// The localized resource registry that stores culture-specific text.
    /// </param>
    public void RegisterResources(ILocalizedResourceRegistry resources)
    {
        resources.Add("en", "module.operations.status.title", "Operations module is running.");
        resources.Add("th", "module.operations.status.title", "โมดูล Operations พร้อมทำงาน");
    }

    /// <summary>
    /// Updates the status service when the module is initialized.
    /// </summary>
    /// <param name="context">
    /// The module context for the current lifecycle execution.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token for the lifecycle operation.
    /// </param>
    /// <returns>
    /// A completed task after initialization state is recorded.
    /// </returns>
    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<OperationsStatusService>().MarkInitialized();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the status service when the module starts.
    /// </summary>
    /// <param name="context">
    /// The module context for the current lifecycle execution.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token for the lifecycle operation.
    /// </param>
    /// <returns>
    /// A completed task after startup state is recorded.
    /// </returns>
    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<OperationsStatusService>().MarkStarted();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the status service when the module stops.
    /// </summary>
    /// <param name="context">
    /// The module context for the current lifecycle execution.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token for the lifecycle operation.
    /// </param>
    /// <returns>
    /// A completed task after shutdown state is recorded.
    /// </returns>
    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<OperationsStatusService>().MarkStopped();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Maps the REST endpoints exposed by the operations module.
    /// </summary>
    /// <param name="endpoints">
    /// The endpoint route builder used by the host adapter.
    /// </param>
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
