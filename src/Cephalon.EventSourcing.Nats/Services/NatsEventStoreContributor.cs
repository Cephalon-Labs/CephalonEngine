using System.Globalization;
using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Nats.Services;

internal sealed class NatsEventStoreContributor(string url, string bucketName) : IEventStoreContributor
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "nats-event-store",
                displayName: "NATS Event Store",
                description: "Appends and replays domain events through NATS JetStream key-value storage.",
                sourceModuleId: "nats-event-sourcing",
                provider: "nats",
                mode: "append-only",
                tags: ["event-sourcing", "nats", "jetstream"],
                metadata: BuildMetadata(url, bucketName))
        ];
    }

    private static Dictionary<string, string> BuildMetadata(string url, string bucketName)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bucketName"] = bucketName,
            ["urlConfigured"] = "true",
            ["secretProjection"] = "redacted"
        };

        if (Uri.TryCreate(url, UriKind.Absolute, out var parsed))
        {
            metadata["scheme"] = parsed.Scheme;
            metadata["host"] = parsed.Host;
            metadata["port"] = parsed.IsDefaultPort ? "default" : parsed.Port.ToString(CultureInfo.InvariantCulture);
            metadata["credentialsConfigured"] = string.IsNullOrWhiteSpace(parsed.UserInfo) ? "false" : "true";
        }
        else
        {
            metadata["urlProjection"] = "unparsed-redacted";
        }

        return metadata;
    }
}
