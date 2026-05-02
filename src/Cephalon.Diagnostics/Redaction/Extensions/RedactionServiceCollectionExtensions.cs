using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Diagnostics.Redaction.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> extensions that wire <see cref="IRedactionFilter"/>
/// implementations and a composed <see cref="RedactionPipeline"/> into the consumer app's
/// dependency-injection container.
/// </summary>
/// <remarks>
/// <para>
/// The canonical wiring is: consumer apps register one or more <see cref="IRedactionFilter"/>
/// implementations against the container, then call <see cref="AddRedactionPipeline"/> once to
/// register a singleton <see cref="RedactionPipeline"/> composed of every registered filter in
/// DI registration order. Engine emission sites that route values through registered filters
/// resolve the <see cref="RedactionPipeline"/> from DI rather than the raw filter set, so the
/// pipeline's ordering and pipe-through semantics are authoritative.
/// </para>
/// <para>
/// Calling <see cref="AddRedactionPipeline"/> more than once is idempotent — subsequent calls
/// observe the already-registered <see cref="RedactionPipeline"/> singleton and skip the
/// registration. Filters added to the container after the pipeline is first resolved do not
/// retroactively appear in the resolved pipeline; the pipeline is materialised at first
/// resolution.
/// </para>
/// </remarks>
public static class RedactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="RedactionPipeline"/> composed of every
    /// <see cref="IRedactionFilter"/> implementation registered against
    /// <paramref name="services"/> in DI registration order.
    /// </summary>
    /// <param name="services">The service collection to register the pipeline against.</param>
    /// <returns>The same <paramref name="services"/> for fluent chaining.</returns>
    /// <remarks>
    /// The registration is idempotent — calling this method multiple times against the same
    /// <see cref="IServiceCollection"/> registers the pipeline once and reuses the existing
    /// registration on subsequent calls.
    /// </remarks>
    public static IServiceCollection AddRedactionPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<RedactionPipeline>(static provider =>
            new RedactionPipeline(provider.GetServices<IRedactionFilter>()));

        return services;
    }
}
