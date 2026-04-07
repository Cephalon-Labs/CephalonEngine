using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Catalog.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Catalog.Behaviors;

/// <summary>
/// Lists products with optional filtering using the direct pattern.
/// Exposed via REST, GraphQL, gRPC, and JSON-RPC transports.
/// </summary>
[AppBehavior("catalog.list-products")]
[BehaviorAllowedPatterns("direct")]
[BehaviorAllowedTransports("http.rest", "http.graphql", "grpc", "http.jsonrpc")]
public sealed class ListProductsBehavior : IAppBehavior<ListProductsInput, ListProductsOutput>
{
    /// <inheritdoc />
    public Task<ListProductsOutput> HandleAsync(
        ListProductsInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var query = ShowcaseDataStore.Products.Values.AsEnumerable();

        if (input.ActiveOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(input.Category))
        {
            query = query.Where(p =>
                string.Equals(p.Category, input.Category, StringComparison.OrdinalIgnoreCase));
        }

        var all = query.ToList();
        var page = all.Take(input.MaxResults).ToList();

        return Task.FromResult(new ListProductsOutput(page, all.Count));
    }

    /// <summary>
    /// Declares the direct pattern with multi-transport exposure.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect()
            .ViaHttpRest()
            .ViaHttpGraphQl()
            .ViaGrpc()
            .ViaHttpJsonRpc();
    }
}
