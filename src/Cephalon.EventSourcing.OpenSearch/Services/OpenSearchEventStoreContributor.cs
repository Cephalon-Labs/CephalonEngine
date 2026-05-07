using System.Globalization;
using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.OpenSearch.Services;

internal sealed class OpenSearchEventStoreContributor(string uri, string indexName) : IEventStoreContributor
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "opensearch-event-store",
                displayName: "OpenSearch Event Store",
                description: "Appends and replays domain events through an OpenSearch index.",
                sourceModuleId: "opensearch-event-sourcing",
                provider: "opensearch",
                mode: "append-only",
                tags: ["event-sourcing", "opensearch", "search"],
                metadata: BuildMetadata(uri, indexName))
        ];
    }

    private static Dictionary<string, string> BuildMetadata(string uri, string indexName)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["indexName"] = indexName,
            ["uriConfigured"] = "true",
            ["secretProjection"] = "redacted"
        };

        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            metadata["scheme"] = parsed.Scheme;
            metadata["host"] = parsed.Host;
            metadata["port"] = parsed.IsDefaultPort ? "default" : parsed.Port.ToString(CultureInfo.InvariantCulture);
            metadata["credentialsConfigured"] = string.IsNullOrWhiteSpace(parsed.UserInfo) ? "false" : "true";
        }
        else
        {
            metadata["uriProjection"] = "unparsed-redacted";
        }

        return metadata;
    }
}
