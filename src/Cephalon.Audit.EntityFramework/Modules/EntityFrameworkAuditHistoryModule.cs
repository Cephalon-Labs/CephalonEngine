using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Modules;
using Cephalon.Audit.EntityFramework.Configuration;
using Cephalon.Audit.EntityFramework.Services;
using Cephalon.Data.EntityFramework.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Audit.EntityFramework.Modules;

internal sealed class EntityFrameworkAuditHistoryModule<TDbContext>(
    EntityFrameworkAuditHistoryOptions options,
    Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext) : ModuleBase
    where TDbContext : DbContext, IEntityFrameworkAuditHistoryContext
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "audit-entity-framework",
        displayName: "Audit Entity Framework",
        description: "Entity Framework Core durable audit-history provider for Cephalon runtimes.",
        tags: ["audit", "history", "entity-framework", "durable"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "audit-history"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TDbContext>((serviceProvider, dbContextOptions) =>
        {
            configureDbContext(serviceProvider, dbContextOptions);
        });

        services.AddSingleton(options);
        services.TryAddSingleton<EntityFrameworkAuditHistoryStoreRuntimeContributor<TDbContext>>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuditStoreRuntimeContributor, EntityFrameworkAuditHistoryStoreRuntimeContributor<TDbContext>>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuditWriter, EntityFrameworkAuditHistoryWriter<TDbContext>>());

        if (options.UsesEngineDatabaseTopology)
        {
            services.AddSingleton(serviceProvider => new EntityFrameworkDatabaseMigrationRegistration(
                typeof(TDbContext),
                [EntityFrameworkAuditHistorySelection.ResolveDatabaseRole(serviceProvider.GetRequiredService<AppProfile>())]));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EntityFrameworkDatabaseMigrationHostedService>());
        }
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
    }
}
