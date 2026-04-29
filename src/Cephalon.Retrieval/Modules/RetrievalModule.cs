using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Retrieval;
using Cephalon.Abstractions.Technologies;
using Cephalon.Retrieval.Configuration;
using Cephalon.Retrieval.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;

namespace Cephalon.Retrieval.Modules;

internal sealed class RetrievalModule : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "knowledge-retrieval-runtime",
        displayName: "Knowledge Retrieval Runtime",
        description: "Companion runtime services for retrieval-heavy applications.",
        tags: ["technology", "retrieval"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "technology-pack",
            ["technology"] = "knowledge-retrieval"
        });

    private readonly RetrievalOptions options;
    private bool hasCollectionContributors;
    private bool hasDocumentProviders;

    public RetrievalModule(RetrievalOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("knowledge-retrieval"))
        {
            return;
        }

        hasCollectionContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IKnowledgeCollectionContributor));
        hasDocumentProviders = services.Any(static descriptor => descriptor.ServiceType == typeof(IKnowledgeDocumentProvider));
        services.TryAddSingleton(options);
        services.TryAddSingleton<IKnowledgeCatalog, KnowledgeCatalog>();
        services.TryAddSingleton<KnowledgeRuntimeCatalog>();
        services.TryAddSingleton<IKnowledgeIndexCatalog>(static serviceProvider =>
            serviceProvider.GetRequiredService<KnowledgeRuntimeCatalog>());
        if (options.EnableIngestion)
        {
            services.TryAddSingleton<IKnowledgeIndexer, KnowledgeIndexer>();
        }

        if (options.EnableQuerying)
        {
            services.TryAddSingleton<IKnowledgeQueryEngine, KnowledgeQueryEngine>();
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, RetrievalRuntimeSurfaceContributor>());
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("knowledge-retrieval"))
        {
            return;
        }

        if (options.EnableQuerying)
        {
            capabilities.Add(new Capability(
                key: "retrieval.query",
                displayName: "Retrieval Query",
                description: "Executes managed lexical retrieval queries over indexed knowledge collections.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "knowledge-retrieval",
                    ["executionOwnership"] = options.EnableQuerying ? "cephalon-managed" : "not-configured",
                    ["providerConfigured"] = hasDocumentProviders.ToString().ToLowerInvariant()
                }));
        }

        if (options.EnableIngestion)
        {
            capabilities.Add(new Capability(
                key: "retrieval.ingestion",
                displayName: "Retrieval Ingestion",
                description: "Builds managed indexes from registered knowledge document providers.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "knowledge-retrieval",
                    ["executionOwnership"] = options.EnableIngestion && hasDocumentProviders ? "cephalon-managed" : "awaiting-provider",
                    ["providerConfigured"] = hasDocumentProviders.ToString().ToLowerInvariant()
                }));
        }

        if (options.Collections.Count > 0 || hasCollectionContributors)
        {
            capabilities.Add(new Capability(
                key: "retrieval.collections",
                displayName: "Knowledge Collections",
                description: "Exposes registered knowledge collection descriptors to the runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "knowledge-retrieval",
                    ["collectionCount"] = options.Collections.Count.ToString(CultureInfo.InvariantCulture),
                    ["hasCollectionContributors"] = hasCollectionContributors.ToString().ToLowerInvariant(),
                    ["hasDocumentProviders"] = hasDocumentProviders.ToString().ToLowerInvariant()
                }));
        }
    }
}
