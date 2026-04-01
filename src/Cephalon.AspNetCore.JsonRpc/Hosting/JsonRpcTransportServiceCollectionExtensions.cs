using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.JsonRpc.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.AspNetCore.JsonRpc.Hosting;

/// <summary>
/// Registers the JSON-RPC ASP.NET Core transport adapter for Cephalon.
/// </summary>
public static class JsonRpcTransportServiceCollectionExtensions
{
    /// <summary>
    /// Adds the JSON-RPC transport mapper to the service collection.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddJsonRpcTransport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportRouteMapper, JsonRpcTransportRouteMapper>());
        return services;
    }

    /// <summary>
    /// Adds the JSON-RPC transport mapper to a <see cref="WebApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder AddJsonRpcTransport(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddJsonRpcTransport();
        return builder;
    }
}
