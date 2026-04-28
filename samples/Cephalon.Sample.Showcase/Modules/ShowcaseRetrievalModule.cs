using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Retrieval.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Demonstrates an adoption-quality retrieval module with collection metadata and provider-backed documents.
/// </summary>
public sealed class ShowcaseRetrievalModule : ModuleBase, IKnowledgeCollectionContributor, IKnowledgeDocumentProvider
{
    private const string CollectionIdValue = "showcase.docs";
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.retrieval",
        displayName: "Showcase Retrieval",
        description: "Knowledge retrieval contribution module for the showcase sample.",
        tags: ["showcase", "retrieval", "knowledge"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IKnowledgeCollectionContributor>(this);
        services.AddSingleton<IKnowledgeDocumentProvider>(this);
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "showcase.retrieval.knowledge-base",
            displayName: "Showcase retrieval knowledge base",
            description: "Contributes showcase documentation into the Cephalon-managed retrieval index."));
    }

    /// <inheritdoc />
    public void RegisterCollections(IKnowledgeCollectionRegistry collections)
    {
        collections.Add(new KnowledgeCollectionDescriptor(
            id: CollectionIdValue,
            displayName: "Showcase Docs",
            description: "Operational and domain guidance contributed by the showcase sample.",
            tags: ["showcase", "docs", "operator"]));
    }

    /// <inheritdoc />
    public string CollectionId => CollectionIdValue;

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<KnowledgeDocument>> LoadDocumentsAsync(
        KnowledgeDocumentProviderContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<KnowledgeDocument> documents =
        [
            new KnowledgeDocument(
                id: "showcase.docs.catalog",
                title: "Catalog Operator Notes",
                content: "Use catalog operator notes to inspect product readiness, update catalog data, and explain direct REST and GraphQL behavior coverage.",
                uri: new Uri("https://docs.cephalon.local/showcase/catalog"),
                tags: ["catalog", "operator"],
                lastModifiedAtUtc: new DateTimeOffset(2026, 04, 12, 9, 0, 0, TimeSpan.Zero),
                metadata: new Dictionary<string, string>
                {
                    ["area"] = "catalog"
                }),
            new KnowledgeDocument(
                id: "showcase.docs.orders",
                title: "Orders Event Flow",
                content: "The orders event flow explains event-driven integration, outbox dispatch, Wolverine as an optional companion, and operator remediation steps.",
                uri: new Uri("https://docs.cephalon.local/showcase/orders"),
                tags: ["orders", "eventing"],
                lastModifiedAtUtc: new DateTimeOffset(2026, 04, 12, 9, 15, 0, TimeSpan.Zero),
                metadata: new Dictionary<string, string>
                {
                    ["area"] = "orders"
                }),
            new KnowledgeDocument(
                id: "showcase.docs.retrieval",
                title: "Retrieval Runtime Readiness",
                content: "Check retrieval provider count, indexed document count, freshness state, and query execution posture before declaring the showcase knowledge base ready.",
                uri: new Uri("https://docs.cephalon.local/showcase/retrieval"),
                tags: ["retrieval", "readiness"],
                lastModifiedAtUtc: new DateTimeOffset(2026, 04, 12, 9, 30, 0, TimeSpan.Zero),
                metadata: new Dictionary<string, string>
                {
                    ["area"] = "retrieval"
                })
        ];

        return ValueTask.FromResult(documents);
    }
}
