namespace Cephalon.Data.Redis.Configuration;

/// <summary>Configuration options for the Redis data provider (Engine:Data:Redis).</summary>
public sealed class RedisDataOptions
{
    /// <summary>Gets the configuration section path used by default for Redis data settings.</summary>
    public const string SectionPath = "Engine:Data:Redis";

    /// <summary>Gets the canonical provider identifier emitted by the pack.</summary>
    public const string ProviderId = "redis";

    /// <summary>Gets the default Redis connection string used when neither connection setting is supplied.</summary>
    public const string DefaultConnectionString = "localhost:6379";

    /// <summary>Gets or sets the root <c>ConnectionStrings</c> entry name to resolve for Redis.</summary>
    /// <remarks>Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.</remarks>
    public string? ConnectionStringName { get; set; }

    /// <summary>Gets or sets the StackExchange.Redis connection string or configuration.</summary>
    /// <remarks>Use either <see cref="ConnectionString" /> or <see cref="ConnectionStringName" />.</remarks>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the key prefix applied to all Cephalon-managed Redis keys (e.g. "myapp:").</summary>
    public string KeyPrefix { get; set; } = "cephalon:";

    /// <summary>Gets or sets a value indicating whether the pack should register the Redis-backed outbox implementation.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>Gets or sets a value indicating whether the pack should register the Redis-backed inbox implementation.</summary>
    public bool RegisterInbox { get; set; }
}
