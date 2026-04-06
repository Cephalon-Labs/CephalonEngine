namespace Cephalon.Data.MongoDB.Configuration;

/// <summary>Configuration options for the MongoDB data provider (Engine:Data:MongoDB).</summary>
public sealed class MongoDbDataOptions
{
    /// <summary>Gets the canonical provider identifier emitted by the pack.</summary>
    public const string ProviderId = "mongodb";

    /// <summary>Gets or sets the MongoDB connection string. Defaults to localhost.</summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>Gets or sets the target database name.</summary>
    public string DatabaseName { get; set; } = "cephalon";

    /// <summary>Gets or sets an optional prefix for all Cephalon-managed collections (e.g. "app_").</summary>
    public string CollectionPrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the pack should register the MongoDB-backed outbox implementation.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>Gets or sets a value indicating whether the pack should register the MongoDB-backed inbox implementation.</summary>
    public bool RegisterInbox { get; set; }
}
