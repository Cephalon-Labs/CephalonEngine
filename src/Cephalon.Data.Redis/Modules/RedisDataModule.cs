using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Redis.Configuration;
using Cephalon.Data.Redis.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Cephalon.Data.Redis.Modules;

internal sealed class RedisDataModule(RedisDataOptions options) : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "redis-data",
        displayName: "Redis Data",
        description: "Redis key-value store registration for Cephalon data workloads.",
        tags: ["data", "redis", "key-value"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "redis-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IConnectionMultiplexer>(serviceProvider =>
            ConnectionMultiplexer.Connect(ConnectionStringResolution.Resolve(
                serviceProvider.GetService<IConfiguration>(),
                options.ConnectionString,
                options.ConnectionStringName,
                RedisDataOptions.DefaultConnectionString,
                RedisDataOptions.SectionPath,
                "Redis")));

        if (options.RegisterOutbox)
        {
            services.TryAddScoped<IOutbox>(serviceProvider =>
            {
                var multiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
                return new RedisOutbox(multiplexer, options);
            });
            services.TryAddScoped<IEventDispatchStore>(serviceProvider =>
            {
                var multiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
                return new RedisEventDispatchStore(multiplexer, options);
            });
        }

        if (options.RegisterInbox)
        {
            services.TryAddScoped<IInbox>(serviceProvider =>
            {
                var multiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
                return new RedisInbox(multiplexer, options);
            });
        }
    }

    /// <inheritdoc />
    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration"))
        {
            return;
        }

        if (options.RegisterOutbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, RedisOutboxRuntimeSurfaceContributor>());
        }

        if (options.RegisterInbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, RedisInboxRuntimeSurfaceContributor>());
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.redis",
            displayName: "Redis Data Provider",
            description: "Registers Redis key-value store as the backing data provider for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Redis",
                ["provider"] = RedisDataOptions.ProviderId,
                ["keyPrefix"] = options.KeyPrefix
            }));

        capabilities.Add(new Capability(
            key: "data.key-value-store",
            displayName: "Key-Value Store",
            description: "The active data provider is a key-value store.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Redis",
                ["provider"] = RedisDataOptions.ProviderId
            }));

        if (options.RegisterOutbox)
        {
            capabilities.Add(new Capability(
                key: "data.outbox.redis",
                displayName: "Redis Outbox",
                description: "Stages outbox messages through the active Redis key-value store.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Redis",
                    ["provider"] = RedisDataOptions.ProviderId
                }));
        }

        if (options.RegisterInbox)
        {
            capabilities.Add(new Capability(
                key: "data.inbox.redis",
                displayName: "Redis Inbox",
                description: "Tracks processed inbound messages through the active Redis key-value store.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Redis",
                    ["provider"] = RedisDataOptions.ProviderId
                }));
        }
    }

    /// <inheritdoc />
    public void RegisterOutboxes(IOutboxRegistry outboxes)
    {
        ArgumentNullException.ThrowIfNull(outboxes);

        if (!options.RegisterOutbox)
        {
            return;
        }

        outboxes.Add(new OutboxDescriptor(
            id: "redis-outbox",
            displayName: "Redis Outbox",
            description: "Stages durable outbound messages through the active Redis key-value store using a Hash and Sorted Set.",
            sourceModuleId: Descriptor.Id,
            provider: RedisDataOptions.ProviderId,
            mode: "sorted-set",
            tags: ["data", "redis", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Redis",
                ["keyPrefix"] = options.KeyPrefix,
                ["hashKeyPattern"] = $"{options.KeyPrefix}outbox:msg:{{messageId}}",
                ["pendingSetKey"] = $"{options.KeyPrefix}outbox:pending",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic"
            }));
    }

    /// <inheritdoc />
    public void RegisterInboxes(IInboxRegistry inboxes)
    {
        ArgumentNullException.ThrowIfNull(inboxes);

        if (!options.RegisterInbox)
        {
            return;
        }

        inboxes.Add(new InboxDescriptor(
            id: "redis-inbox",
            displayName: "Redis Inbox",
            description: "Tracks processed inbound messages through the active Redis key-value store using a Set.",
            sourceModuleId: Descriptor.Id,
            provider: RedisDataOptions.ProviderId,
            mode: "set",
            tags: ["data", "redis", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Redis",
                ["keyPrefix"] = options.KeyPrefix,
                ["receiptsSetKey"] = $"{options.KeyPrefix}inbox:receipts",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["idempotency"] = "message-id",
                ["subscriptionRuntime"] = "not-configured"
            }));
    }
}
