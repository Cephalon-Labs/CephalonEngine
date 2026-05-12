using Microsoft.AspNetCore.Builder;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Applies Cephalon ASP.NET Core rate-limiting conventions to endpoint builders by consulting the
/// host's effective rate-limiting policy catalog.
/// </summary>
public static class CephalonRateLimitingEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Applies the effective Cephalon rate-limiting policy for the supplied transport and optional
    /// behavior identifier onto the endpoint builder.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint builder to configure.</param>
    /// <param name="services">The application service provider.</param>
    /// <param name="transportId">The transport identifier used by the endpoint.</param>
    /// <param name="behaviorId">The optional behavior identifier when the endpoint maps a single behavior.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static TBuilder ApplyCephalonRateLimiting<TBuilder>(
        this TBuilder builder,
        IServiceProvider services,
        string transportId,
        string? behaviorId = null)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        var resolution = services.GetService(typeof(AspNetCoreRateLimitingPolicyCatalog)) is AspNetCoreRateLimitingPolicyCatalog catalog
            ? catalog.Resolve(transportId, behaviorId)
            : RateLimitingEndpointPolicyResolution.None;
        if (resolution.Mode == RateLimitingEndpointPolicyMode.Require)
        {
            builder.WithMetadata(new CephalonRateLimitingTransportMetadata(transportId));
            return builder.RequireRateLimiting(resolution.Policy!.Id);
        }

        if (resolution.Mode == RateLimitingEndpointPolicyMode.Disable)
        {
            return builder.DisableRateLimiting();
        }

        return builder;
    }

    /// <summary>
    /// Determines whether the effective Cephalon rate-limiting policy for the supplied transport
    /// and optional behavior identifier actively enforces a limiter.
    /// </summary>
    /// <param name="services">The application service provider.</param>
    /// <param name="transportId">The transport identifier used by the endpoint.</param>
    /// <param name="behaviorId">The optional behavior identifier when the endpoint maps a single behavior.</param>
    /// <returns><see langword="true" /> when the endpoint will require a limiter; otherwise <see langword="false" />.</returns>
    public static bool HasCephalonRateLimiting(
        this IServiceProvider services,
        string transportId,
        string? behaviorId = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        return services.GetService(typeof(AspNetCoreRateLimitingPolicyCatalog)) is AspNetCoreRateLimitingPolicyCatalog catalog &&
            catalog.Resolve(transportId, behaviorId).Mode == RateLimitingEndpointPolicyMode.Require;
    }
}
