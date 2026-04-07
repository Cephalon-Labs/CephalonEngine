using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Catalog.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Catalog.Behaviors;

/// <summary>
/// Retrieves a single product by identifier using the direct pattern.
/// Exposed via REST, GraphQL, gRPC, and JSON-RPC transports.
/// </summary>
[AppBehavior("catalog.get-product")]
[BehaviorAllowedPatterns("direct")]
[BehaviorAllowedTransports("http.rest", "http.graphql", "grpc", "http.jsonrpc")]
public sealed class GetProductBehavior : IAppBehavior<GetProductInput, GetProductOutput?>
{
    /// <inheritdoc />
    public Task<GetProductOutput?> HandleAsync(
        GetProductInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var product = ShowcaseDataStore.Products.GetValueOrDefault(input.ProductId);
        return Task.FromResult(product is null ? null : new GetProductOutput(product));
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
