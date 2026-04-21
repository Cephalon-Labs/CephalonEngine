using MongoDB.Bson.Serialization.Attributes;

namespace Cephalon.Data.MongoDB.Services;

internal sealed class MongoDbChangeStreamCheckpointEntry
{
    [BsonId]
    public string CdcCaptureId { get; set; } = string.Empty;

    public string ResumeTokenJson { get; set; } = string.Empty;

    public string ChangeId { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
