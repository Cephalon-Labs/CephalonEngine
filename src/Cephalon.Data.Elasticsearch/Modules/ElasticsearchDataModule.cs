using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Elasticsearch.Configuration;
using Cephalon.Data.Elasticsearch.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Data.Elasticsearch.Modules;

internal sealed class ElasticsearchDataModule(ElasticsearchDataOptions options)
    : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "elasticsearch-data",
        displayName: "Elasticsearch Data",
        description: "Elasticsearch search-store registration for Cephalon data workloads.",
        tags: ["data", "elasticsearch", "search-store"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "elasticsearch-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ElasticsearchClient>(_ =>
        {
            var settings = new ElasticsearchClientSettings(new Uri(options.Uri));
            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                settings = settings.Authentication(new BasicAuthentication(options.Username, options.Password ?? string.Empty));
            }
            return new ElasticsearchClient(settings);
        });

        if (options.RegisterOutbox)
        {
            var indexName = $"{options.IndexPrefix}outbox-messages";
            services.TryAddScoped<IOutbox>(sp =>
                new ElasticsearchOutbox(sp.GetRequiredService<ElasticsearchClient>(), indexName));
        }

        if (options.RegisterInbox)
        {
            var indexName = $"{options.IndexPrefix}inbox-receipts";
            services.TryAddScoped<IInbox>(sp =>
                new ElasticsearchInbox(sp.GetRequiredService<ElasticsearchClient>(), indexName));
        }
    }

    /// <inheritdoc />
    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);
        if (!technologies.IsSelected("event-driven-integration")) return;
        if (options.RegisterOutbox)
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, ElasticsearchOutboxRuntimeSurfaceContributor>());
        if (options.RegisterInbox)
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, ElasticsearchInboxRuntimeSurfaceContributor>());
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        capabilities.Add(new Capability("data.elasticsearch", "Elasticsearch Data Provider",
            "Registers Elasticsearch as the backing data provider for Cephalon data workloads.",
            new Dictionary<string, string> { ["pack"] = "Cephalon.Data.Elasticsearch", ["provider"] = ElasticsearchDataOptions.ProviderId }));
        capabilities.Add(new Capability("data.search-store", "Search Store",
            "The active data provider is a search-oriented document store.",
            new Dictionary<string, string> { ["pack"] = "Cephalon.Data.Elasticsearch", ["provider"] = ElasticsearchDataOptions.ProviderId }));
        if (options.RegisterOutbox)
            capabilities.Add(new Capability("data.outbox.elasticsearch", "Elasticsearch Outbox",
                "Stages outbox messages through the active Elasticsearch index.",
                new Dictionary<string, string> { ["pack"] = "Cephalon.Data.Elasticsearch", ["provider"] = ElasticsearchDataOptions.ProviderId }));
        if (options.RegisterInbox)
            capabilities.Add(new Capability("data.inbox.elasticsearch", "Elasticsearch Inbox",
                "Tracks processed inbound messages through the active Elasticsearch index.",
                new Dictionary<string, string> { ["pack"] = "Cephalon.Data.Elasticsearch", ["provider"] = ElasticsearchDataOptions.ProviderId }));
    }

    /// <inheritdoc />
    public void RegisterOutboxes(IOutboxRegistry outboxes)
    {
        ArgumentNullException.ThrowIfNull(outboxes);
        if (!options.RegisterOutbox) return;
        outboxes.Add(new OutboxDescriptor(
            id: "elasticsearch-outbox",
            displayName: "Elasticsearch Outbox",
            description: "Stages durable outbound messages through the active Elasticsearch index.",
            sourceModuleId: Descriptor.Id,
            provider: ElasticsearchDataOptions.ProviderId,
            mode: "search-index",
            tags: ["data", "elasticsearch", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Elasticsearch",
                ["index"] = $"{options.IndexPrefix}outbox-messages",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic"
            }));
    }

    /// <inheritdoc />
    public void RegisterInboxes(IInboxRegistry inboxes)
    {
        ArgumentNullException.ThrowIfNull(inboxes);
        if (!options.RegisterInbox) return;
        inboxes.Add(new InboxDescriptor(
            id: "elasticsearch-inbox",
            displayName: "Elasticsearch Inbox",
            description: "Tracks processed inbound messages through the active Elasticsearch index.",
            sourceModuleId: Descriptor.Id,
            provider: ElasticsearchDataOptions.ProviderId,
            mode: "search-index",
            tags: ["data", "elasticsearch", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Elasticsearch",
                ["index"] = $"{options.IndexPrefix}inbox-receipts",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["idempotency"] = "message-id",
                ["subscriptionRuntime"] = "not-configured"
            }));
    }
}
