using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Modules;
using Cephalon.Engine.Diagnostics;
using Cephalon.Audit.Configuration;
using Cephalon.Audit.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Audit.Modules;

internal sealed class AuditModule(Action<AuditRuntimeOptions>? configureOptions)
    : ModuleBase, IAuditStoreContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "audit",
        displayName: "Audit",
        description: "Host-agnostic audit recording baseline for Cephalon runtimes.",
        tags: ["audit", "history", "platform"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var options = AuditRuntimeOptions.FromConfiguration(configuration);
            configureOptions?.Invoke(options);
            return options;
        });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, AuditDiagnosticsConventionContributor>());
        services.TryAddSingleton<ILogger<DefaultAuditRecorder>>(NullLogger<DefaultAuditRecorder>.Instance);
        services.TryAddSingleton<IAuditActorAccessor, DefaultAuditActorAccessor>();
        services.TryAddSingleton<InMemoryAuditWriter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuditWriter, InMemoryAuditWriter>());
        services.TryAddSingleton<IAuditRecorder, DefaultAuditRecorder>();
    }

    public override void RegisterCapabilities(Cephalon.Abstractions.Capabilities.ICapabilityRegistry capabilities)
    {
    }

    public void RegisterAuditStores(IAuditStoreRegistry auditStores)
    {
        ArgumentNullException.ThrowIfNull(auditStores);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["writeMode"] = "application-managed",
            ["queryMode"] = "not-configured",
            ["tenantAware"] = "true",
            ["actorSource"] = "ambient-or-explicit",
            ["correlation"] = "activity-current-or-explicit"
        };

        auditStores.Add(new AuditStoreDescriptor(
            id: "audit-default",
            displayName: "Default Audit Store",
            description: "Records audit entries through the built-in Cephalon audit baseline.",
            sourceModuleId: Descriptor.Id,
            provider: "memory",
            mode: "volatile-buffer",
            tags: ["audit", "default", "memory"],
            metadata: metadata));
    }
}
