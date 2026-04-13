using Cephalon.Abstractions.Behaviors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Describes one public REST route group backed by Cephalon behaviors.
/// </summary>
/// <remarks>
/// Mapping a behavior through this contract automatically declares module ownership for that
/// behavior. Use <see cref="IRestBehaviorModuleBuilder.Internal{TBehavior}()"/> when a behavior
/// should remain internal-only or when the module needs a custom/manual endpoint mapping path.
/// </remarks>
public interface IRestBehaviorEndpointGroupBuilder
{
    /// <summary>
    /// Assigns the route group to the supplied public API major version.
    /// </summary>
    /// <param name="major">The public API major version.</param>
    /// <returns>The same group builder for fluent configuration.</returns>
    IRestBehaviorEndpointGroupBuilder ApiVersion(int major);

    /// <summary>
    /// Overrides the published OpenAPI tag name for the route group.
    /// </summary>
    /// <param name="tagName">The tag name to publish.</param>
    /// <returns>The same group builder for fluent configuration.</returns>
    IRestBehaviorEndpointGroupBuilder WithTagName(string tagName);

    /// <summary>
    /// Overrides the published OpenAPI tag description for the route group.
    /// </summary>
    /// <param name="description">The tag description to publish. Pass <see langword="null"/> to clear it.</param>
    /// <returns>The same group builder for fluent configuration.</returns>
    IRestBehaviorEndpointGroupBuilder WithTagDescription(string? description);

    /// <summary>
    /// Applies additional Minimal API route-group conventions when the group is materialized.
    /// </summary>
    /// <param name="configure">The callback that can add route-group conventions.</param>
    /// <returns>The same group builder for fluent configuration.</returns>
    IRestBehaviorEndpointGroupBuilder Configure(Action<RouteGroupBuilder> configure);

    /// <summary>
    /// Maps a REST endpoint by consuming the metadata-only REST profile declared on the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose through the owning module.</typeparam>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    /// <remarks>
    /// The owning module still controls the public group prefix, tags, and published OpenAPI documents.
    /// The behavior profile contributes only the candidate method, relative pattern, and optional API
    /// major version metadata.
    /// </remarks>
    IRestBehaviorEndpointGroupBuilder MapProfile<TBehavior>(
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST endpoint by consuming the metadata-only REST profile declared on the specified behavior
    /// while applying an explicit topology override during ownership registration.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose through the owning module.</typeparam>
    /// <param name="configureTopology">The explicit topology selection callback.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapProfile<TBehavior>(
        Action<IBehaviorTopologyBuilder> configureTopology,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>GET</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapGet<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>GET</c> endpoint that dispatches into the specified behavior and applies an
    /// explicit topology override while registering ownership.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureTopology">The explicit topology selection callback.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapGet<TBehavior>(
        string pattern,
        Action<IBehaviorTopologyBuilder> configureTopology,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>POST</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapPost<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>POST</c> endpoint that dispatches into the specified behavior and applies an
    /// explicit topology override while registering ownership.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureTopology">The explicit topology selection callback.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapPost<TBehavior>(
        string pattern,
        Action<IBehaviorTopologyBuilder> configureTopology,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>PUT</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapPut<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>PUT</c> endpoint that dispatches into the specified behavior and applies an
    /// explicit topology override while registering ownership.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureTopology">The explicit topology selection callback.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapPut<TBehavior>(
        string pattern,
        Action<IBehaviorTopologyBuilder> configureTopology,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>PATCH</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapPatch<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>PATCH</c> endpoint that dispatches into the specified behavior and applies an
    /// explicit topology override while registering ownership.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureTopology">The explicit topology selection callback.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapPatch<TBehavior>(
        string pattern,
        Action<IBehaviorTopologyBuilder> configureTopology,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>DELETE</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapDelete<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;

    /// <summary>
    /// Maps a REST <c>DELETE</c> endpoint that dispatches into the specified behavior and applies an
    /// explicit topology override while registering ownership.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to expose.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configureTopology">The explicit topology selection callback.</param>
    /// <param name="configureEndpoint">Optional endpoint-level Minimal API customization.</param>
    /// <returns>The same group builder for fluent route composition.</returns>
    IRestBehaviorEndpointGroupBuilder MapDelete<TBehavior>(
        string pattern,
        Action<IBehaviorTopologyBuilder> configureTopology,
        Action<RouteHandlerBuilder>? configureEndpoint = null)
        where TBehavior : class;
}
