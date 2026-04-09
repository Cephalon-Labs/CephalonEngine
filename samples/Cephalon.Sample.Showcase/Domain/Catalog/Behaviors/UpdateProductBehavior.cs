using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Catalog.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Catalog.Behaviors;

/// <summary>
/// Updates an existing product in the catalog using the direct pattern.
/// Exposed through JSON-RPC while REST stays module-owned.
/// </summary>
[AppBehavior("catalog.update-product")]
[BehaviorAllowedPatterns("direct")]
[BehaviorAllowedTransports("http.jsonrpc")]
public sealed class UpdateProductBehavior : IAppBehavior<UpdateProductInput, UpdateProductOutput>
{
    /// <inheritdoc />
    public Task<UpdateProductOutput> HandleAsync(
        UpdateProductInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        if (!ShowcaseDataStore.Products.TryGetValue(input.ProductId, out var product))
        {
            return Task.FromResult(new UpdateProductOutput(Updated: false));
        }

        if (input.Name is not null)
        {
            product.Name = input.Name;
        }

        if (input.Description is not null)
        {
            product.Description = input.Description;
        }

        if (input.PriceInCents.HasValue)
        {
            product.PriceInCents = input.PriceInCents.Value;
        }

        if (input.IsActive.HasValue)
        {
            product.IsActive = input.IsActive.Value;
        }

        product.UpdatedAtUtc = DateTime.UtcNow;

        return Task.FromResult(new UpdateProductOutput(Updated: true));
    }

    /// <summary>
    /// Declares the direct pattern with write-oriented transports.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect()
            .ViaHttpJsonRpc();
    }
}
