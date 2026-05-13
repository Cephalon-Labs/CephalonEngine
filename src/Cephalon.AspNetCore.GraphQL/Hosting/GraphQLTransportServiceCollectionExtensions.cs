using Cephalon.AspNetCore.GraphQL.Routing;
using Cephalon.AspNetCore.GraphQL.Resilience;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Manifest;
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
        services.TryAddSingleton(serviceProvider =>
            GraphQLExecutionResilienceOptions.FromManifest(
                serviceProvider.GetService<RuntimeManifest>()));
        services.TryAddSingleton<GraphQLExecutionResilienceStateRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, GraphQLExecutionResilienceRuntimeContributor>());
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

    /// <summary>
    /// Adds fields to the shared GraphQL mutation root used by Cephalon modules.
    /// </summary>
    /// <param name="services">The service collection that owns the transport registration.</param>
    /// <param name="configure">The callback that adds fields, arguments, and resolvers to <c>Mutation</c>.</param>
    /// <returns>The same service collection for fluent composition.</returns>
    public static IServiceCollection ConfigureGraphQLMutation(
        this IServiceCollection services,
        Action<IObjectTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        GetOrAddRegistration(services).AddMutationConfiguration(configure);
        return services;
    }

    /// <summary>
    /// Adds fields to the shared GraphQL mutation root on a <see cref="WebApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">The callback that adds fields, arguments, and resolvers to <c>Mutation</c>.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder ConfigureGraphQLMutation(
        this WebApplicationBuilder builder,
        Action<IObjectTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ConfigureGraphQLMutation(configure);
        return builder;
    }

    /// <summary>
    /// Adds fields to the shared GraphQL subscription root used by Cephalon modules.
    /// </summary>
    /// <param name="services">The service collection that owns the transport registration.</param>
    /// <param name="configure">The callback that adds fields, arguments, and resolvers to <c>Subscription</c>.</param>
    /// <returns>The same service collection for fluent composition.</returns>
    /// <remarks>
    /// Subscription field registration only shapes the GraphQL schema. Modules or hosts still need
    /// to register a concrete Hot Chocolate subscription provider, such as
    /// <c>AddInMemorySubscriptions()</c>, through <see cref="ConfigureGraphQLTransport(IServiceCollection,Action{IRequestExecutorBuilder})" />
    /// when they want GraphQL-over-SSE or GraphQL-over-WebSocket operations to execute.
    /// </remarks>
    public static IServiceCollection ConfigureGraphQLSubscription(
        this IServiceCollection services,
        Action<IObjectTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        GetOrAddRegistration(services).AddSubscriptionConfiguration(configure);
        return services;
    }

    /// <summary>
    /// Adds fields to the shared GraphQL subscription root on a <see cref="WebApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">The callback that adds fields, arguments, and resolvers to <c>Subscription</c>.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder ConfigureGraphQLSubscription(
        this WebApplicationBuilder builder,
        Action<IObjectTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ConfigureGraphQLSubscription(configure);
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
    private readonly List<Action<IObjectTypeDescriptor>> mutationConfigurations = [];
    private readonly List<Action<IObjectTypeDescriptor>> subscriptionConfigurations = [];
    private IRequestExecutorBuilder? builder;
    private bool isMutationRootRegistered;
    private bool isSubscriptionRootRegistered;

    public bool IsTransportRegistered { get; private set; }

    public void MarkTransportRegistered()
    {
        IsTransportRegistered = true;
    }

    public void Initialize(IRequestExecutorBuilder builder)
    {
        this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
        builder.ConfigureSchema(schema => schema.Use(GraphQLExecutionResilienceMiddleware.Create));
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

        if (mutationConfigurations.Count > 0)
        {
            EnsureMutationRootRegistered();
        }

        if (subscriptionConfigurations.Count > 0)
        {
            EnsureSubscriptionRootRegistered();
        }

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

    public void AddMutationConfiguration(Action<IObjectTypeDescriptor> configure)
    {
        mutationConfigurations.Add(configure);
        if (builder is not null)
        {
            EnsureMutationRootRegistered();
        }
    }

    public void AddSubscriptionConfiguration(Action<IObjectTypeDescriptor> configure)
    {
        subscriptionConfigurations.Add(configure);
        if (builder is not null)
        {
            EnsureSubscriptionRootRegistered();
        }
    }

    private void EnsureMutationRootRegistered()
    {
        if (isMutationRootRegistered)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(builder);
        builder.AddMutationType(descriptor =>
        {
            descriptor.Name("Mutation");

            foreach (var configureMutation in mutationConfigurations)
            {
                configureMutation(descriptor);
            }
        });

        isMutationRootRegistered = true;
    }

    private void EnsureSubscriptionRootRegistered()
    {
        if (isSubscriptionRootRegistered)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(builder);
        builder.AddSubscriptionType(descriptor =>
        {
            descriptor.Name("Subscription");

            foreach (var configureSubscription in subscriptionConfigurations)
            {
                configureSubscription(descriptor);
            }
        });

        isSubscriptionRootRegistered = true;
    }
}
