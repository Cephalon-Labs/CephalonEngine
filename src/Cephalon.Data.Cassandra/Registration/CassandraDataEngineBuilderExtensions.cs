using Cephalon.Data.Cassandra.Configuration;
using Cephalon.Data.Cassandra.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Cassandra.Registration;

/// <summary>
/// Registers the Cassandra data companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class CassandraDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Cassandra data pack with the supplied contact points and keyspace.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="contactPoints">
    /// One or more Cassandra contact-point host addresses, separated by commas
    /// (e.g. <c>"localhost"</c> or <c>"node1,node2,node3"</c>).
    /// </param>
    /// <param name="keyspace">The Cassandra keyspace to connect to.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned Cassandra pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Register outbox or inbox support by configuring <see cref="CassandraDataOptions.RegisterOutbox" />
    /// or <see cref="CassandraDataOptions.RegisterInbox" /> in the <paramref name="configure" /> callback.
    /// </remarks>
    public static EngineBuilder AddCassandraData(
        this EngineBuilder builder,
        string contactPoints,
        string keyspace,
        Action<CassandraDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactPoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyspace);

        var options = new CassandraDataOptions
        {
            ContactPoints = contactPoints,
            Keyspace = keyspace
        };

        configure?.Invoke(options);

        builder.AddModule(new CassandraDataModule(options));
        return builder;
    }
}
