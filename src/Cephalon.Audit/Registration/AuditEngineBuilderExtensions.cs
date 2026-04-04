using Cephalon.Audit.Configuration;
using Cephalon.Audit.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Audit.Registration;

/// <summary>
/// Registers the host-agnostic Cephalon audit companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class AuditEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Cephalon audit companion pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">An optional callback that configures host-owned audit runtime options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddAudit(
        this EngineBuilder builder,
        Action<AuditRuntimeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddModule(new AuditModule(configure));
        return builder;
    }
}
