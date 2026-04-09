using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Thin adapter that maps a behavior topology to one HTTP transport.
/// Each transport variant (JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, WebSocket, etc.)
/// implements this interface and is registered in the
/// <see cref="IHttpBehaviorBindingRegistry" />.
/// </summary>
public interface IHttpBehaviorBinding
{
    /// <summary>
    /// Gets the canonical transport identifier, e.g. <c>http.jsonrpc</c>.
    /// </summary>
    string TransportId { get; }

    /// <summary>
    /// Maps the behavior's routes/endpoints onto the <see cref="WebApplication" />.
    /// Called at most once per descriptor per transport (lazy-init guards ensure this).
    /// </summary>
    /// <param name="app">The web application to register routes on.</param>
    /// <param name="descriptor">The behavior topology descriptor.</param>
    /// <param name="dispatcher">The behavior dispatcher to invoke.</param>
    /// <returns>A task that completes when all routes are mapped.</returns>
    Task MapAsync(WebApplication app, BehaviorTopologyDescriptor descriptor, BehaviorDispatcher dispatcher);
}
