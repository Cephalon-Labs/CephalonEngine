using Cephalon.Data.Neo4j.Configuration;
using Cephalon.Data.Neo4j.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Neo4j.Registration;

/// <summary>
/// Registers the Neo4j data companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class Neo4jDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Neo4j data pack with the supplied Bolt URI, username, and password.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="uri">The Neo4j Bolt URI (e.g. <c>bolt://localhost:7687</c>).</param>
    /// <param name="username">The Neo4j username.</param>
    /// <param name="password">The Neo4j password.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned Neo4j pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Register outbox or inbox support by configuring <see cref="Neo4jDataOptions.RegisterOutbox" />
    /// or <see cref="Neo4jDataOptions.RegisterInbox" /> in the <paramref name="configure" /> callback.
    /// </remarks>
    public static EngineBuilder AddNeo4jData(
        this EngineBuilder builder,
        string uri,
        string username,
        string password,
        Action<Neo4jDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var options = new Neo4jDataOptions
        {
            Uri = uri,
            Username = username,
            Password = password
        };

        configure?.Invoke(options);

        builder.AddModule(new Neo4jDataModule(options));
        return builder;
    }
}
