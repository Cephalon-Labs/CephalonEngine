using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Nats.Configuration;
using Cephalon.Data.Nats.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NATS.Client.Core;

namespace Cephalon.Data.Nats.Modules;

internal sealed class NatsDataModule(NatsDataOptions options) : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "nats-data",
        displayName: "NATS Data",
        description: "NATS JetStream KV ledger-store registration for Cephalon data workloads.",
        tags: ["data", "nats", "ledger-store", "jetstream"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "nats-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // NatsConnection does NOT connect on construction — connection is deferred to first use.
        // This allows DI resolution tests without a live NATS server.
        services.TryAddSingleton<INatsConnection>(serviceProvider =>
        {
            var effectiveUri = UriResolution.Resolve(
                serviceProvider.GetService<IConfiguration>(),
                options.Uri,
                options.UriName,
                NatsDataOptions.DefaultUri,
                NatsDataOptions.SectionPath,
                "NATS");
            return new NatsConnection(new NatsOpts { Url = effectiveUri });
        });

        var outboxBucket = $"{options.BucketPrefix}-outbox";
        var inboxBucket = $"{options.BucketPrefix}-inbox";

        if (options.RegisterOutbox)
        {
            services.TryAddScoped<IOutbox>(serviceProvider =>
            {
                var nats = serviceProvider.GetRequiredService<INatsConnection>();
                return new NatsOutbox(nats, outboxBucket);
            });
            services.TryAddScoped<IEventDispatchStore>(serviceProvider =>
            {
                var nats = serviceProvider.GetRequiredService<INatsConnection>();
                return new NatsEventDispatchStore(nats, outboxBucket);
            });
        }

        if (options.RegisterInbox)
        {
            services.TryAddScoped<IInbox>(serviceProvider =>
            {
                var nats = serviceProvider.GetRequiredService<INatsConnection>();
                return new NatsInbox(nats, inboxBucket);
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, NatsOutboxRuntimeSurfaceContributor>());
        }

        if (options.RegisterInbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, NatsInboxRuntimeSurfaceContributor>());
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.nats",
            displayName: "NATS Data Provider",
            description: "Registers NATS JetStream KV as the backing data provider for Cephalon data workloads.",
            metadata: CreateProviderMetadata()));

        capabilities.Add(new Capability(
            key: "data.ledger-store",
            displayName: "Ledger Store",
            description: "The active data provider is a durable ledger-style store.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Nats",
                ["provider"] = NatsDataOptions.ProviderId
            }));

        if (options.RegisterOutbox)
        {
            capabilities.Add(new Capability(
                key: "data.outbox.nats",
                displayName: "NATS Outbox",
                description: "Stages outbox messages through the active NATS JetStream KV bucket.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Nats",
                    ["provider"] = NatsDataOptions.ProviderId
                }));
        }

        if (options.RegisterInbox)
        {
            capabilities.Add(new Capability(
                key: "data.inbox.nats",
                displayName: "NATS Inbox",
                description: "Tracks processed inbound messages through the active NATS JetStream KV bucket.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Nats",
                    ["provider"] = NatsDataOptions.ProviderId
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
            id: "nats-outbox",
            displayName: "NATS Outbox",
            description: "Stages durable outbound messages through the active NATS JetStream KV bucket using CreateAsync for idempotent key creation.",
            sourceModuleId: Descriptor.Id,
            provider: NatsDataOptions.ProviderId,
            mode: "jetstream-kv",
            tags: ["data", "nats", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Nats",
                ["bucket"] = $"{options.BucketPrefix}-outbox",
                ["idempotency"] = "kv-create",
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
            id: "nats-inbox",
            displayName: "NATS Inbox",
            description: "Tracks processed inbound messages through the active NATS JetStream KV bucket using CreateAsync for idempotent key creation.",
            sourceModuleId: Descriptor.Id,
            provider: NatsDataOptions.ProviderId,
            mode: "jetstream-kv",
            tags: ["data", "nats", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Nats",
                ["bucket"] = $"{options.BucketPrefix}-inbox",
                ["idempotency"] = "kv-create",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["subscriptionRuntime"] = "not-configured"
            })); 
    }

    private Dictionary<string, string> CreateProviderMetadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["pack"] = "Cephalon.Data.Nats",
            ["provider"] = NatsDataOptions.ProviderId
        };

        if (!string.IsNullOrWhiteSpace(options.Uri))
        {
            metadata["uri"] = options.Uri.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(options.UriName))
        {
            metadata["uriName"] = options.UriName.Trim();
        }

        return metadata;
    }
}
