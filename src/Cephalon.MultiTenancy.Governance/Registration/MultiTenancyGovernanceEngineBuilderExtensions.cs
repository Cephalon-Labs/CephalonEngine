using Cephalon.Engine.Composition;
using Cephalon.MultiTenancy.Governance.Configuration;
using Cephalon.MultiTenancy.Governance.Modules;

namespace Cephalon.MultiTenancy.Governance.Registration;

/// <summary>
/// Registers the Cephalon tenant-governance companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class MultiTenancyGovernanceEngineBuilderExtensions
{
    /// <summary>
    /// Adds the tenant-governance companion pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">An optional callback that configures membership governance options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddMultiTenancyGovernance(
        this EngineBuilder builder,
        Action<MultiTenancyGovernanceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new MultiTenancyGovernanceOptions();
        configure?.Invoke(options);

        builder.AddModule(new MultiTenancyGovernanceModule(options));
        return builder;
    }
}
