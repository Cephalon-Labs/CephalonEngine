namespace Cephalon.Data.MongoDB.Configuration;

/// <summary>Configuration options for the MongoDB data provider (Engine:Data:MongoDB).</summary>
public sealed class MongoDbDataOptions
{
    /// <summary>Gets the configuration section path used by default for MongoDB data settings.</summary>
    public const string SectionPath = "Engine:Data:MongoDB";

    /// <summary>Gets the canonical provider identifier emitted by the pack.</summary>
    public const string ProviderId = "mongodb";

    /// <summary>Gets the default MongoDB connection string used when neither connection setting is supplied.</summary>
    public const string DefaultConnectionString = "mongodb://localhost:27017";

    /// <summary>Gets or sets the root <c>ConnectionStrings</c> entry name to resolve for MongoDB.</summary>
    /// <remarks>Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.</remarks>
    public string? ConnectionStringName { get; set; }

    /// <summary>Gets or sets the inline MongoDB connection string.</summary>
    /// <remarks>Use either <see cref="ConnectionString" /> or <see cref="ConnectionStringName" />.</remarks>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the target database name.</summary>
    public string DatabaseName { get; set; } = "cephalon";

    /// <summary>Gets or sets an optional prefix for all Cephalon-managed collections (e.g. "app_").</summary>
    public string CollectionPrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the pack should register the MongoDB-backed outbox implementation.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>Gets or sets a value indicating whether the pack should register the MongoDB-backed inbox implementation.</summary>
    public bool RegisterInbox { get; set; }

    /// <summary>
    /// Gets the provider-native MongoDB change-stream captures that should be contributed to the active CDC runtime.
    /// </summary>
    public IList<MongoDbChangeStreamCaptureOptions> ChangeStreamCaptures { get; } = [];
}
