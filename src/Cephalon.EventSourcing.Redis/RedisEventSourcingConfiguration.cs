namespace Cephalon.EventSourcing.Redis;

/// <summary>
/// Provides key-naming conventions for the Redis Streams event-store provider.
/// </summary>
public static class RedisEventSourcingConfiguration
{
    /// <summary>
    /// Computes the Redis Stream key for the given stream identifier and key prefix.
    /// </summary>
    /// <param name="keyPrefix">The key prefix configured for the Redis provider (e.g. <c>"cephalon:"</c>).</param>
    /// <param name="streamId">The stable logical stream identifier.</param>
    /// <returns>The fully qualified Redis key for the stream (e.g. <c>"cephalon:stream:orders-42"</c>).</returns>
    public static string StreamKey(string keyPrefix, string streamId) =>
        $"{keyPrefix}stream:{streamId}";
}
