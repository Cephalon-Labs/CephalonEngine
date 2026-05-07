using System.Globalization;
using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Neo4j.Services;

internal sealed class Neo4jEventStoreContributor(
    string uri,
    string username,
    string password,
    string eventLabel) : IEventStoreContributor
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "neo4j-event-store",
                displayName: "Neo4j Event Store",
                description: "Appends and replays domain events through Neo4j event graph nodes.",
                sourceModuleId: "neo4j-event-sourcing",
                provider: "neo4j",
                mode: "append-only",
                tags: ["event-sourcing", "neo4j", "graph"],
                metadata: BuildMetadata(uri, username, password, eventLabel))
        ];
    }

    private static Dictionary<string, string> BuildMetadata(
        string uri,
        string username,
        string password,
        string eventLabel)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["eventLabel"] = eventLabel,
            ["usernameConfigured"] = string.IsNullOrWhiteSpace(username) ? "false" : "true",
            ["passwordConfigured"] = string.IsNullOrWhiteSpace(password) ? "false" : "true",
            ["secretProjection"] = "redacted"
        };

        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            metadata["scheme"] = parsed.Scheme;
            metadata["host"] = parsed.Host;
            metadata["port"] = parsed.IsDefaultPort ? "default" : parsed.Port.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            metadata["uriProjection"] = "unparsed-redacted";
        }

        return metadata;
    }
}
