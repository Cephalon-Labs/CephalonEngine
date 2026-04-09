using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Bindings;
using Cephalon.Behaviors.Http.Registry;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Extension methods for registering the HTTP transport binding pack into the
/// behavior DI container.
/// </summary>
public static class HttpBehaviorBindingExtensions
{
    /// <summary>
    /// Registers all 6 generic HTTP transport bindings (JSON-RPC, GraphQL, GraphQL-SSE,
    /// GraphQL-WS, SSE, WebSocket) and the <see cref="IHttpBehaviorBindingRegistry" />
    /// as singletons in the service collection.
    /// </summary>
    /// <param name="builder">The behavior collection builder to extend.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public static IBehaviorCollectionBuilder AddHttpBehaviorBindings(
        this IBehaviorCollectionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        // Register each generic binding as a singleton IHttpBehaviorBinding.
        services.AddSingleton<IHttpBehaviorBinding, JsonRpcHttpBehaviorBinding>();
        services.AddSingleton<IHttpBehaviorBinding, GraphqlHttpBehaviorBinding>();
        services.AddSingleton<IHttpBehaviorBinding, GraphqlSseBehaviorBinding>();
        services.AddSingleton<IHttpBehaviorBinding, GraphqlWsBehaviorBinding>();
        services.AddSingleton<IHttpBehaviorBinding, SseBehaviorBinding>();
        services.AddSingleton<IHttpBehaviorBinding, WebSocketBehaviorBinding>();

        // Register the registry that wraps all bindings.
        services.AddSingleton<IHttpBehaviorBindingRegistry>(sp =>
            new HttpBehaviorBindingRegistry(sp.GetServices<IHttpBehaviorBinding>()));

        // Register the transport route mapper that bridges MapCephalon() to behavior HTTP bindings.
        // This mapper is invoked when the "behavior-http" transport is selected in the engine manifest.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ITransportRouteMapper, BehaviorHttpTransportRouteMapper>());

        return builder;
    }
}
