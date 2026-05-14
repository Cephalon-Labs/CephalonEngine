using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cephalon.EventSourcing.MongoDB;

internal sealed class MongoDbEventSnapshotEntry
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string StreamId { get; set; } = string.Empty;

    public string StateType { get; set; } = string.Empty;

    public long StreamVersion { get; set; }

    public string Payload { get; set; } = string.Empty;

    public DateTime SavedAtUtc { get; set; }
}
