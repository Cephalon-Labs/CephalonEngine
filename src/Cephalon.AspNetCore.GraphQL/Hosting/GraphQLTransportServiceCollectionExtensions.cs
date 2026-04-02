using Cephalon.AspNetCore.GraphQL.Routing;
using Cephalon.AspNetCore.Hosting;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.AspNetCore.GraphQL.Hosting;

/// <summary>
/// Registers the GraphQL ASP.NET Core transport adapter for Cephalon.
/// </summary>
public static class GraphQLTransportServiceCollectionExtensions
{
    /// <summary>
    /// Adds the GraphQL transport mapper and GraphQL server services to the service collection.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddGraphQLTransport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var registration = GetOrAddRegistration(services);
        if (registration.IsTransportRegistered)
        {
            return services;
        }

        registration.MarkTransportRegistered();
        registration.Initialize(services.AddGraphQLServer());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportRouteMapper, GraphQLTransportRouteMapper>());
        return services;
    }

    /// <summary>
    /// Adds the GraphQL transport mapper and GraphQL server services to a <see cref="WebApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder AddGraphQLTransport(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddGraphQLTransport();
        return builder;
    }

    /// <summary>
    /// Applies GraphQL schema configuration for Cephalon modules and hosts.
    /// </summary>
    /// <param name="services">The service collection that owns the transport registration.</param>
    /// <param name="configure">The callback that extends the GraphQL request executor builder.</param>
    /// <returns>The same service collection for fluent composition.</returns>
    /// <remarks>
    /// Hosts typically call <see cref="AddGraphQLTransport(IServiceCollection)" /> once, while
    /// individual modules use this method from <c>ConfigureServices</c> to add query, mutation,
    /// subscription, scalar, or type-extension registrations without forcing the engine core to
    /// know anything about the GraphQL implementation.
    /// </remarks>
    public static IServiceCollection ConfigureGraphQLTransport(
        this IServiceCollection services,
        Action<IRequestExecutorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        GetOrAddRegistration(services).Apply(configure);
        return services;
    }

    /// <summary>
    /// Applies GraphQL schema configuration on a <see cref="WebApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">The callback that extends the GraphQL request executor builder.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder ConfigureGraphQLTransport(
        this WebApplicationBuilder builder,
        Action<IRequestExecutorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ConfigureGraphQLTransport(configure);
        return builder;
    }

    /// <summary>
    /// Adds fields to the shared GraphQL query root used by Cephalon modules.
    /// </summary>
    /// <param name="services">The service collection that owns the transport registration.</param>
    /// <param name="configure">The callback that adds fields, arguments, and resolvers to <c>Query</c>.</param>
    /// <returns>The same service collection for fluent composition.</returns>
    public static IServiceCollection ConfigureGraphQLQuery(
        this IServiceCollection services,
        Action<IObjectTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        GetOrAddRegistration(services).AddQueryConfiguration(configure);
        return services;
    }

    /// <summary>
    /// Adds fields to the shared GraphQL query root on a <see cref="WebApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">The callback that adds fields, arguments, and resolvers to <c>Query</c>.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder ConfigureGraphQLQuery(
        this WebApplicationBuilder builder,
        Action<IObjectTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ConfigureGraphQLQuery(configure);
        return builder;
    }

    private static GraphQLTransportRegistration GetOrAddRegistration(IServiceCollection services)
    {
        var existing = services
            .LastOrDefault(service => service.ServiceType == typeof(GraphQLTransportRegistration))
            ?.ImplementationInstance as GraphQLTransportRegistration;
        if (existing is not null)
        {
            return existing;
        }

        var registration = new GraphQLTransportRegistration();
        services.AddSingleton(registration);
        return registration;
    }
}

internal sealed class GraphQLTransportRegistration
{
    private readonly List<Action<IRequestExecutorBuilder>> pendingConfigurations = [];
    private readonly List<Action<IObjectTypeDescriptor>> queryConfigurations = [];
    private IRequestExecutorBuilder? builder;

    public bool IsTransportRegistered { get; private set; }

    public void MarkTransportRegistered()
    {
        IsTransportRegistered = true;
    }

    public void Initialize(IRequestExecutorBuilder builder)
    {
        this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
        builder.AddQueryType(descriptor =>
        {
            descriptor.Name("Query");
            descriptor.Field("_service")
                .Description("Indicates that the Cephalon GraphQL transport is active.")
                .Resolve(static _ => "Cephalon");

            foreach (var configureQuery in queryConfigurations)
            {
                configureQuery(descriptor);
            }
        });

        foreach (var configure in pendingConfigurations)
        {
            configure(builder);
        }

        pendingConfigurations.Clear();
    }

    public void Apply(Action<IRequestExecutorBuilder> configure)
    {
        if (builder is not null)
        {
            configure(builder);
            return;
        }

        pendingConfigurations.Add(configure);
    }

    public void AddQueryConfiguration(Action<IObjectTypeDescriptor> configure)
    {
        queryConfigurations.Add(configure);
    }
}
