namespace Cephalon.Data.Cassandra.Configuration;

/// <summary>
/// Options controlling how <c>Cephalon.Data.Cassandra</c> connects to a Cassandra cluster and registers data services.
/// </summary>
public sealed class CassandraDataOptions
{
    /// <summary>The provider identifier used in capability and descriptor metadata.</summary>
    public const string ProviderId = "cassandra";

    /// <summary>
    /// One or more Cassandra contact-point host addresses, separated by commas
    /// (e.g. <c>"localhost"</c> or <c>"node1,node2,node3"</c>).
    /// </summary>
    public string ContactPoints { get; set; } = "localhost";

    /// <summary>The Cassandra native transport port. Defaults to <c>9042</c>.</summary>
    public int Port { get; set; } = 9042;

    /// <summary>The Cassandra keyspace to connect to (created if not exists on first operation).</summary>
    public string Keyspace { get; set; } = "cephalon";

    /// <summary>Optional prefix applied to all managed table names (e.g. <c>"cephalon_"</c>).</summary>
    public string TablePrefix { get; set; } = "cephalon_";

    /// <summary>
    /// Gets or sets the number of deterministic shards used by the pending-dispatch eligibility table.
    /// Defaults to <c>16</c> and applies only when <see cref="RegisterOutbox" /> is enabled.
    /// </summary>
    public int PendingDispatchShardCount { get; set; } = 16;

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IOutbox" /> backed by a Cassandra wide-column table.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IInbox" /> backed by a Cassandra wide-column table.</summary>
    public bool RegisterInbox { get; set; }
}
