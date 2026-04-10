using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.EntityFramework.Configuration;
using Cephalon.Data.EntityFramework.Modeling;
using Cephalon.Data.EntityFramework.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Cephalon.Eventing.Services;
using SfidNet.Abstractions;
using SfidNet.EntityFramework;

namespace Cephalon.Data.EntityFramework.Modules;

internal sealed class EntityFrameworkDataModule<TReadDbContext, TWriteDbContext>(
    EntityFrameworkDataOptions options,
    Action<IServiceProvider, DbContextOptionsBuilder> configureReadDbContext,
    Action<IServiceProvider, DbContextOptionsBuilder> configureWriteDbContext) : ModuleBase, IInboxContributor, IOutboxContributor, IProjectionContributor, ITechnologyServiceContributor
    where TReadDbContext : DbContext
    where TWriteDbContext : DbContext
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "entity-framework-data",
        displayName: "Entity Framework Data",
        description: "Entity Framework Core read/write DbContext registration for Cephalon data workloads.",
        tags: ["data", "entity-framework", "cqrs"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "entity-framework-data"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (options.EnableSfidIdentifiers)
        {
            services.AddScoped<SfidKeyAssigningSaveChangesInterceptor>(serviceProvider =>
            {
                var generator = serviceProvider.GetService<ISfidGenerator>();
                if (generator is null)
                {
                    throw new InvalidOperationException(
                        "Cephalon.Data.EntityFramework was configured with EnableSfidIdentifiers, but no official Sfid.Net generator is registered. Add Cephalon.Ids.Sfid before enabling Sfid EF integration.");
                }

                ValidateSfidSelection(serviceProvider.GetRequiredService<AppProfile>());
                return new SfidKeyAssigningSaveChangesInterceptor(generator);
            });
        }

        services.AddDbContext<TReadDbContext>((serviceProvider, dbContextOptions) =>
        {
            ConfigureDbContext(
                serviceProvider,
                dbContextOptions,
                configureReadDbContext,
                enableWriteFeatures: !options.UsesReadWriteSplit);
        });

        if (options.UsesReadWriteSplit)
        {
            services.AddDbContext<TWriteDbContext>((serviceProvider, dbContextOptions) =>
            {
                ConfigureDbContext(
                    serviceProvider,
                    dbContextOptions,
                    configureWriteDbContext,
                    enableWriteFeatures: true);
            });
        }

        if (options.RegisterOutbox)
        {
            if (!typeof(IEntityFrameworkOutboxContext).IsAssignableFrom(typeof(TWriteDbContext)))
            {
                throw new InvalidOperationException(
                    $"The write DbContext '{typeof(TWriteDbContext).FullName}' must implement '{typeof(IEntityFrameworkOutboxContext).FullName}' when '{nameof(EntityFrameworkDataOptions.RegisterOutbox)}' is enabled.");
            }

            services.AddScoped<IOutbox>(serviceProvider =>
            {
                var dbContext = serviceProvider.GetRequiredService<TWriteDbContext>();
                return new EntityFrameworkOutbox(
                    dbContext,
                    (IEntityFrameworkOutboxContext)dbContext);
            });
            services.AddScoped<IEventDispatchStore>(serviceProvider =>
            {
                var dbContext = serviceProvider.GetRequiredService<TWriteDbContext>();
                return new EntityFrameworkEventDispatchStore(
                    dbContext,
                    (IEntityFrameworkOutboxContext)dbContext);
            });
        }

        if (options.RegisterInbox)
        {
            if (!typeof(IEntityFrameworkInboxContext).IsAssignableFrom(typeof(TWriteDbContext)))
            {
                throw new InvalidOperationException(
                    $"The write DbContext '{typeof(TWriteDbContext).FullName}' must implement '{typeof(IEntityFrameworkInboxContext).FullName}' when '{nameof(EntityFrameworkDataOptions.RegisterInbox)}' is enabled.");
            }

            services.AddScoped<IInbox>(serviceProvider =>
            {
                var dbContext = serviceProvider.GetRequiredService<TWriteDbContext>();
                return new EntityFrameworkInbox(
                    dbContext,
                    (IEntityFrameworkInboxContext)dbContext);
            });
        }

        if (options.UsesEngineDatabaseTopology)
        {
            services.AddSingleton<ITechnologyRuntimeContributor>(serviceProvider =>
                new EntityFrameworkDatabaseTopologyRuntimeSurfaceContributor(
                    serviceProvider.GetRequiredService<AppProfile>(),
                    options));

            services.AddSingleton(new EntityFrameworkDatabaseMigrationRegistration(
                typeof(TWriteDbContext),
                ["write"]));

            if (options.UsesReadWriteSplit)
            {
                services.AddSingleton(new EntityFrameworkDatabaseMigrationRegistration(
                    typeof(TReadDbContext),
                    ["read"]));
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EntityFrameworkDatabaseMigrationHostedService>());
        }
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (technologies.IsSelected("event-driven-integration"))
        {
            if (options.RegisterOutbox)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EntityFrameworkOutboxRuntimeSurfaceContributor>());
            }

            if (options.RegisterInbox)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EntityFrameworkInboxRuntimeSurfaceContributor>());
            }
        }

        if (options.RegisterProjections)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EntityFrameworkProjectionRuntimeSurfaceContributor>());
        }
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        if (options.RegisterProviderCapability)
        {
            capabilities.Add(new Capability(
                key: "data.entity-framework",
                displayName: "Entity Framework Data Provider",
                description: "Registers read/write DbContext roles through Entity Framework Core.",
                metadata: CreateProviderMetadata()));
        }

        if (!options.RegisterDbContextCapabilities)
        {
            return;
        }

        capabilities.Add(new Capability(
            key: "data.dbcontext.read",
            displayName: "Read DbContext",
            description: "Registers the read-side DbContext used by Cephalon data query handlers.",
            metadata: CreateDbContextMetadata("read", options.ReadDbContextType)));

        capabilities.Add(new Capability(
            key: "data.dbcontext.write",
            displayName: "Write DbContext",
            description: "Registers the write-side DbContext used by Cephalon data command handlers.",
            metadata: CreateDbContextMetadata("write", options.WriteDbContextType)));

        if (options.RegisterOutbox)
        {
            capabilities.Add(new Capability(
                key: "data.outbox",
                displayName: "Entity Framework Outbox",
                description: "Stages outbox messages through the active write-side Entity Framework Core DbContext.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.EntityFramework",
                    ["provider"] = EntityFrameworkDataOptions.ProviderId,
                    ["writeDbContext"] = GetTypeName(options.WriteDbContextType),
                    ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
                }));
        }

        if (options.RegisterInbox)
        {
            capabilities.Add(new Capability(
                key: "data.inbox",
                displayName: "Entity Framework Inbox",
                description: "Tracks processed inbound messages through the active write-side Entity Framework Core DbContext.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.EntityFramework",
                    ["provider"] = EntityFrameworkDataOptions.ProviderId,
                    ["writeDbContext"] = GetTypeName(options.WriteDbContextType),
                    ["idempotency"] = "message-id",
                    ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
                }));
        }

        if (options.RegisterProjections)
        {
            capabilities.Add(new Capability(
                key: "data.projections.entity-framework",
                displayName: "Entity Framework Projections",
                description: "Entity Framework Core-backed projection infrastructure for read-model materialization.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.EntityFramework",
                    ["provider"] = EntityFrameworkDataOptions.ProviderId,
                    ["projectionRuntime"] = "application-managed"
                }));
        }
    }

    public void RegisterInboxes(IInboxRegistry inboxes)
    {
        ArgumentNullException.ThrowIfNull(inboxes);

        if (!options.RegisterInbox)
        {
            return;
        }

        inboxes.Add(new InboxDescriptor(
            id: "entity-framework-inbox",
            displayName: "Entity Framework Inbox",
            description: "Tracks processed inbound messages through the active write-side Entity Framework Core DbContext.",
            sourceModuleId: Descriptor.Id,
            provider: EntityFrameworkDataOptions.ProviderId,
            mode: "processed-message-table",
            tags: ["data", "entity-framework", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.EntityFramework",
                ["writeDbContext"] = GetTypeName(options.WriteDbContextType),
                ["readDbContext"] = GetTypeName(options.ReadDbContextType),
                ["readWriteSplit"] = options.UsesReadWriteSplit ? "true" : "false",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["idempotency"] = "message-id",
                ["subscriptionRuntime"] = "not-configured",
                ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
            }));
    }

    public void RegisterOutboxes(IOutboxRegistry outboxes)
    {
        ArgumentNullException.ThrowIfNull(outboxes);

        if (!options.RegisterOutbox)
        {
            return;
        }

        outboxes.Add(new OutboxDescriptor(
            id: EntityFrameworkDataRuntimeIds.OutboxId,
            displayName: "Entity Framework Outbox",
            description: "Stages durable outbound messages through the active write-side Entity Framework Core DbContext.",
            sourceModuleId: Descriptor.Id,
            provider: EntityFrameworkDataOptions.ProviderId,
            mode: "transactional-table",
            tags: ["data", "entity-framework", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.EntityFramework",
                ["writeDbContext"] = GetTypeName(options.WriteDbContextType),
                ["readDbContext"] = GetTypeName(options.ReadDbContextType),
                ["readWriteSplit"] = options.UsesReadWriteSplit ? "true" : "false",
                ["dispatchRuntime"] = "not-configured",
                ["dispatchStore"] = "available",
                ["eventingLinked"] = "false",
                ["channelMode"] = "dynamic",
                ["idStrategy"] = options.EnableSfidIdentifiers ? "sfid" : "none",
                ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
            }));
    }

    public void RegisterProjections(IProjectionRegistry projections)
    {
        ArgumentNullException.ThrowIfNull(projections);

        if (!options.RegisterProjections)
        {
            return;
        }

        projections.Add(new ProjectionDescriptor(
            id: "entity-framework-projections",
            displayName: "Entity Framework Projections",
            description: "Entity Framework Core-backed projection infrastructure for read-model materialization from staged events.",
            sourceModuleId: Descriptor.Id,
            targetStoreId: "entity-framework-read-store",
            mode: "application-managed",
            sourceContracts: null,
            tags: ["data", "entity-framework", "projections", "cqrs"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.EntityFramework",
                ["provider"] = EntityFrameworkDataOptions.ProviderId,
                ["readDbContext"] = GetTypeName(options.ReadDbContextType),
                ["writeDbContext"] = GetTypeName(options.WriteDbContextType),
                ["readWriteSplit"] = options.UsesReadWriteSplit ? "true" : "false",
                ["projectionRuntime"] = "application-managed",
                ["idStrategy"] = options.EnableSfidIdentifiers ? "sfid" : "none",
                ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
            }));
    }

    private Dictionary<string, string> CreateProviderMetadata()
    {
        return new Dictionary<string, string>
        {
            ["pack"] = "Cephalon.Data.EntityFramework",
            ["provider"] = EntityFrameworkDataOptions.ProviderId,
            ["readDbContext"] = GetTypeName(options.ReadDbContextType),
            ["writeDbContext"] = GetTypeName(options.WriteDbContextType),
            ["readWriteSplit"] = options.UsesReadWriteSplit ? "true" : "false",
            ["idStrategy"] = options.EnableSfidIdentifiers ? "sfid" : "none",
            ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
        };
    }

    private Dictionary<string, string> CreateDbContextMetadata(
        string role,
        Type dbContextType)
    {
        return new Dictionary<string, string>
        {
            ["pack"] = "Cephalon.Data.EntityFramework",
            ["provider"] = EntityFrameworkDataOptions.ProviderId,
            ["role"] = role,
            ["dbContext"] = GetTypeName(dbContextType),
            ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
        };
    }

    private static string GetTypeName(Type type)
    {
        return type.FullName ?? type.Name;
    }

    private static void ValidateSfidSelection(AppProfile appProfile)
    {
        ArgumentNullException.ThrowIfNull(appProfile);

        var configuredGenerator = appProfile.Data.IdGenerator;
        if (string.IsNullOrWhiteSpace(configuredGenerator))
        {
            return;
        }

        var normalized = configuredGenerator.Trim();
        if (string.Equals(normalized, "sfid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "snowfake", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"The active app profile selected id generator '{configuredGenerator}', but Cephalon.Data.EntityFramework was configured with EnableSfidIdentifiers. Update Engine:Data:Ids:Generator or disable Sfid EF integration.");
    }

    private void ConfigureDbContext(
        IServiceProvider serviceProvider,
        DbContextOptionsBuilder dbContextOptions,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext,
        bool enableWriteFeatures)
    {
        configureDbContext(serviceProvider, dbContextOptions);

        if (options.EnableSfidIdentifiers)
        {
            dbContextOptions.UseSfidEntityFramework();

            if (enableWriteFeatures)
            {
                dbContextOptions.AddInterceptors(serviceProvider.GetRequiredService<SfidKeyAssigningSaveChangesInterceptor>());
            }
        }
    }
}
