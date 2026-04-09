using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Adds fluent REST-route topology helpers on top of the host-agnostic behavior topology builder.
/// </summary>
public static class BehaviorRestTopologyBuilderExtensions
{
    /// <summary>
    /// Declares the generic REST transport and configures its explicit HTTP method plus route pattern.
    /// </summary>
    /// <param name="builder">The host-agnostic topology builder.</param>
    /// <param name="configure">The callback that describes the generic REST route contract.</param>
    /// <returns>The same topology builder for fluent chaining.</returns>
    /// <remarks>
    /// Use this overload when a behavior should keep using the generic behavior HTTP transport surface
    /// but needs an explicit REST route shape beyond the default canonical path derived from its behavior id.
    /// </remarks>
    public static IBehaviorTopologyBuilder ViaHttpRest(
        this IBehaviorTopologyBuilder builder,
        Action<BehaviorRestTopologyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.ViaHttpRest();
        configure(new BehaviorRestTopologyBuilder(builder));
        return builder;
    }
}
