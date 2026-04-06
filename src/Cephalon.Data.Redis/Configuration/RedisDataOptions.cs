namespace Cephalon.Data.Redis.Configuration;

/// <summary>Configuration options for the Redis data provider (Engine:Data:Redis).</summary>
public sealed class RedisDataOptions
{
    /// <summary>Gets the canonical provider identifier emitted by the pack.</summary>
    public const string ProviderId = "redis";

    /// <summary>Gets or sets the StackExchange.Redis connection string or configuration. Defaults to localhost.</summary>
    public string Configuration { get; set; } = "localhost:6379";

    /// <summary>Gets or sets the key prefix applied to all Cephalon-managed Redis keys (e.g. "myapp:").</summary>
    public string KeyPrefix { get; set; } = "cephalon:";

    /// <summary>Gets or sets a value indicating whether the pack should register the Redis-backed outbox implementation.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>Gets or sets a value indicating whether the pack should register the Redis-backed inbox implementation.</summary>
    public bool RegisterInbox { get; set; }
}
