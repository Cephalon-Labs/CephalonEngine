using Cephalon.Agentics.Configuration;
using Cephalon.Agentics.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Agentics.Registration;

/// <summary>
/// Registers the built-in agentic runtime pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class AgenticEngineBuilderExtensions
{
    /// <summary>
    /// Adds the agentic runtime pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned agentic runtime options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// The pack activates only when the matching technology profile is selected, but registering it
    /// here makes its services, capabilities, and runtime surfaces available when that selection is active.
    /// </remarks>
    public static EngineBuilder AddAgentics(
        this EngineBuilder builder,
        Action<AgenticRuntimeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new AgenticRuntimeOptions();
        configure?.Invoke(options);

        builder.AddModule(new AgenticsModule(options));
        return builder;
    }
}
