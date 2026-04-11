using Cephalon.Abstractions.Modules;

namespace Cephalon.AspNetCore.GraphQL.Modules;

/// <summary>
/// Marks a Cephalon module as contributing GraphQL schema or resolver behavior on ASP.NET Core.
/// </summary>
/// <remarks>
/// Implementing modules should also register their GraphQL query, mutation, subscription, or
/// type-extension services from <c>ConfigureServices</c> by calling
/// <c>ConfigureGraphQLQuery(...)</c>, <c>ConfigureGraphQLMutation(...)</c>,
/// <c>ConfigureGraphQLSubscription(...)</c>, or <c>ConfigureGraphQLTransport(...)</c> on the
/// shared service collection. Subscription fields still require a concrete Hot Chocolate
/// subscription provider, such as <c>AddInMemorySubscriptions()</c>, to execute over the built-in
/// GraphQL-over-SSE or GraphQL-over-WebSocket routes.
/// </remarks>
public interface IGraphQLModule : IModule
{
}
