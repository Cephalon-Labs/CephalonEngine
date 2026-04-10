using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.OpenSearch.Configuration;
using Cephalon.Data.OpenSearch.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenSearch.Client;

namespace Cephalon.Data.OpenSearch.Modules;

internal sealed class OpenSearchDataModule(OpenSearchDataOptions options)
    : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "opensearch-data",
        displayName: "OpenSearch Data",
        description: "OpenSearch search-store registration for Cephalon data workloads.",
        tags: ["data", "opensearch", "search-store"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "opensearch-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<OpenSearchClient>(_ =>
        {
            var effectiveUri = UriResolution.Resolve(
                _.GetService<IConfiguration>(),
                options.Uri,
                options.UriName,
                OpenSearchDataOptions.DefaultUri,
                OpenSearchDataOptions.SectionPath,
                "OpenSearch");
            var settings = new ConnectionSettings(new Uri(effectiveUri));
            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                settings = settings.BasicAuthentication(options.Username, options.Password ?? string.Empty);
            }
            return new OpenSearchClient(settings);
        });

        if (options.RegisterOutbox)
        {
            var indexName = $"{options.IndexPrefix}outbox-messages";
            services.TryAddScoped<IOutbox>(sp =>
                new OpenSearchOutbox(sp.GetRequiredService<OpenSearchClient>(), indexName));
            services.TryAddScoped<IEventDispatchStore>(sp =>
                new OpenSearchEventDispatchStore(sp.GetRequiredService<OpenSearchClient>(), indexName));
        }

        if (options.RegisterInbox)
        {
            var indexName = $"{options.IndexPrefix}inbox-receipts";
            services.TryAddScoped<IInbox>(sp =>
                new OpenSearchInbox(sp.GetRequiredService<OpenSearchClient>(), indexName));
        }
    }

    /// <inheritdoc />
    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);
        if (!technologies.IsSelected("event-driven-integration")) return;
        if (options.RegisterOutbox)
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, OpenSearchOutboxRuntimeSurfaceContributor>());
        if (options.RegisterInbox)
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, OpenSearchInboxRuntimeSurfaceContributor>());
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        capabilities.Add(new Capability("data.opensearch", "OpenSearch Data Provider",
            "Registers OpenSearch as the backing data provider for Cephalon data workloads.",
            CreateProviderMetadata()));
        capabilities.Add(new Capability("data.search-store", "Search Store",
            "The active data provider is a search-oriented document store.",
            new Dictionary<string, string> { ["pack"] = "Cephalon.Data.OpenSearch", ["provider"] = OpenSearchDataOptions.ProviderId }));
        if (options.RegisterOutbox)
            capabilities.Add(new Capability("data.outbox.opensearch", "OpenSearch Outbox",
                "Stages outbox messages through the active OpenSearch index.",
                new Dictionary<string, string> { ["pack"] = "Cephalon.Data.OpenSearch", ["provider"] = OpenSearchDataOptions.ProviderId }));
        if (options.RegisterInbox)
            capabilities.Add(new Capability("data.inbox.opensearch", "OpenSearch Inbox",
                "Tracks processed inbound messages through the active OpenSearch index.",
                new Dictionary<string, string> { ["pack"] = "Cephalon.Data.OpenSearch", ["provider"] = OpenSearchDataOptions.ProviderId }));
    }

    /// <inheritdoc />
    public void RegisterOutboxes(IOutboxRegistry outboxes)
    {
        ArgumentNullException.ThrowIfNull(outboxes);
        if (!options.RegisterOutbox) return;
        outboxes.Add(new OutboxDescriptor(
            id: "opensearch-outbox",
            displayName: "OpenSearch Outbox",
            description: "Stages durable outbound messages through the active OpenSearch index.",
            sourceModuleId: Descriptor.Id,
            provider: OpenSearchDataOptions.ProviderId,
            mode: "search-index",
            tags: ["data", "opensearch", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.OpenSearch",
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
            id: "opensearch-inbox",
            displayName: "OpenSearch Inbox",
            description: "Tracks processed inbound messages through the active OpenSearch index.",
            sourceModuleId: Descriptor.Id,
            provider: OpenSearchDataOptions.ProviderId,
            mode: "search-index",
            tags: ["data", "opensearch", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.OpenSearch",
                ["index"] = $"{options.IndexPrefix}inbox-receipts",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["idempotency"] = "message-id",
                ["subscriptionRuntime"] = "not-configured"
            })); 
    }

    private Dictionary<string, string> CreateProviderMetadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["pack"] = "Cephalon.Data.OpenSearch",
            ["provider"] = OpenSearchDataOptions.ProviderId
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
