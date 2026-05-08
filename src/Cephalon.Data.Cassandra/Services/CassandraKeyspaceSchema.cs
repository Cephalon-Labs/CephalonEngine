using Cassandra;
using Cephalon.Data.Cassandra.Configuration;

namespace Cephalon.Data.Cassandra.Services;

internal static class CassandraKeyspaceSchema
{
    public static async Task EnsureKeyspaceAsync(
        ICluster cluster,
        CassandraDataOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        using var session = await cluster.ConnectAsync().ConfigureAwait(false);
        await session.ExecuteAsync(new SimpleStatement($@"
            CREATE KEYSPACE IF NOT EXISTS {options.Keyspace}
            WITH replication = {{ 'class': 'SimpleStrategy', 'replication_factor': 1 }}"))
            .ConfigureAwait(false);
    }
}
