using Cephalon.AspNetCore.Grpc.Routing;
using Cephalon.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.AspNetCore.Grpc.Hosting;

/// <summary>
/// Registers the gRPC ASP.NET Core transport adapter for Cephalon.
/// </summary>
public static class GrpcTransportServiceCollectionExtensions
{
    /// <summary>
    /// Adds the gRPC transport mapper and ASP.NET Core gRPC services to the service collection.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddGrpcTransport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<CephalonGrpcResilienceInterceptor>();
        services.AddGrpc(static options =>
        {
            options.Interceptors.Add<CephalonGrpcResilienceInterceptor>();
        });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportRouteMapper, GrpcTransportRouteMapper>());
        return services;
    }

    /// <summary>
    /// Adds the gRPC transport mapper to a <see cref="WebApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder AddGrpcTransport(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddGrpcTransport();
        return builder;
    }
}
