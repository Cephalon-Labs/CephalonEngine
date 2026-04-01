using Cephalon.Engine.Composition;
using Cephalon.Retrieval.Configuration;
using Cephalon.Retrieval.Modules;

namespace Cephalon.Retrieval.Registration;

/// <summary>
/// Registers the built-in retrieval runtime pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class RetrievalEngineBuilderExtensions
{
    /// <summary>
    /// Adds the retrieval runtime pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned retrieval options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddRetrieval(
        this EngineBuilder builder,
        Action<RetrievalOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new RetrievalOptions();
        configure?.Invoke(options);

        builder.AddModule(new RetrievalModule(options));
        return builder;
    }
}
