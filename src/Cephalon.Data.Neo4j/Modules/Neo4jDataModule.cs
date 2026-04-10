using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Neo4j.Configuration;
using Cephalon.Data.Neo4j.Services;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neo4j.Driver;

namespace Cephalon.Data.Neo4j.Modules;

internal sealed class Neo4jDataModule(Neo4jDataOptions options) : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "neo4j-data",
        displayName: "Neo4j Data",
        description: "Neo4j graph store registration for Cephalon data workloads.",
        tags: ["data", "neo4j", "graph-store"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "neo4j-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDriver>(serviceProvider =>
        {
            var effectiveUri = UriResolution.Resolve(
                serviceProvider.GetService<IConfiguration>(),
                options.Uri,
                options.UriName,
                Neo4jDataOptions.DefaultUri,
                Neo4jDataOptions.SectionPath,
                "Neo4j");
            return GraphDatabase.Driver(effectiveUri, AuthTokens.Basic(options.Username, options.Password));
        });

        if (options.RegisterOutbox)
        {
            services.TryAddScoped<IOutbox>(serviceProvider =>
            {
                var driver = serviceProvider.GetRequiredService<IDriver>();
                return new Neo4jOutbox(driver, options);
            });
        }

        if (options.RegisterInbox)
        {
            services.TryAddScoped<IInbox>(serviceProvider =>
            {
                var driver = serviceProvider.GetRequiredService<IDriver>();
                return new Neo4jInbox(driver, options);
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, Neo4jOutboxRuntimeSurfaceContributor>());
        }

        if (options.RegisterInbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, Neo4jInboxRuntimeSurfaceContributor>());
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.neo4j",
            displayName: "Neo4j Data Provider",
            description: "Registers Neo4j graph store as the backing data provider for Cephalon data workloads.",
            metadata: CreateProviderMetadata()));

        capabilities.Add(new Capability(
            key: "data.graph-store",
            displayName: "Graph Store",
            description: "The active data provider is a graph-oriented database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Neo4j",
                ["provider"] = Neo4jDataOptions.ProviderId
            }));

        if (options.RegisterOutbox)
        {
            capabilities.Add(new Capability(
                key: "data.outbox.neo4j",
                displayName: "Neo4j Outbox",
                description: "Stages outbox messages through the active Neo4j graph store.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Neo4j",
                    ["provider"] = Neo4jDataOptions.ProviderId
                }));
        }

        if (options.RegisterInbox)
        {
            capabilities.Add(new Capability(
                key: "data.inbox.neo4j",
                displayName: "Neo4j Inbox",
                description: "Tracks processed inbound messages through the active Neo4j graph store.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Neo4j",
                    ["provider"] = Neo4jDataOptions.ProviderId
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
            id: "neo4j-outbox",
            displayName: "Neo4j Outbox",
            description: "Stages durable outbound messages through the active Neo4j graph store as graph nodes.",
            sourceModuleId: Descriptor.Id,
            provider: Neo4jDataOptions.ProviderId,
            mode: "graph-node",
            tags: ["data", "neo4j", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Neo4j",
                ["nodeLabel"] = $"{options.LabelPrefix}OutboxMessage",
                ["idempotency"] = "merge-on-message-id",
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
            id: "neo4j-inbox",
            displayName: "Neo4j Inbox",
            description: "Tracks processed inbound messages through the active Neo4j graph store as graph nodes.",
            sourceModuleId: Descriptor.Id,
            provider: Neo4jDataOptions.ProviderId,
            mode: "graph-node",
            tags: ["data", "neo4j", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Neo4j",
                ["nodeLabel"] = $"{options.LabelPrefix}InboxReceipt",
                ["idempotency"] = "merge-on-message-id",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["subscriptionRuntime"] = "not-configured"
            })); 
    }

    private Dictionary<string, string> CreateProviderMetadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["pack"] = "Cephalon.Data.Neo4j",
            ["provider"] = Neo4jDataOptions.ProviderId
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
