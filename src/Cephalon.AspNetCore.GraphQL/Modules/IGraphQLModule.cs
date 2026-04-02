using Cephalon.Abstractions.Modules;

namespace Cephalon.AspNetCore.GraphQL.Modules;

/// <summary>
/// Marks a Cephalon module as contributing GraphQL schema or resolver behavior on ASP.NET Core.
/// </summary>
/// <remarks>
/// Implementing modules should also register their GraphQL query, mutation, subscription, or
/// type-extension services from <c>ConfigureServices</c> by calling
/// <c>ConfigureGraphQLTransport(...)</c> on the shared service collection.
/// </remarks>
public interface IGraphQLModule : IModule
{
}
