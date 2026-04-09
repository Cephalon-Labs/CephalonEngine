using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Catalog.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Catalog.Behaviors;

/// <summary>
/// Creates a new product in the catalog using the direct pattern.
/// Exposed through JSON-RPC while REST stays module-owned.
/// </summary>
[AppBehavior("catalog.create-product")]
[BehaviorAllowedPatterns("direct")]
[BehaviorAllowedTransports("http.jsonrpc")]
public sealed class CreateProductBehavior : IAppBehavior<CreateProductInput, CreateProductOutput>
{
    /// <inheritdoc />
    public Task<CreateProductOutput> HandleAsync(
        CreateProductInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var productId = $"prod-{Guid.NewGuid():N}"[..16];

        var product = new Product
        {
            Id = productId,
            Sku = input.Sku,
            Name = input.Name,
            Description = input.Description,
            Category = input.Category,
            PriceInCents = input.PriceInCents,
            Currency = input.Currency,
            Tags = input.Tags ?? [],
            CreatedAtUtc = DateTime.UtcNow
        };

        ShowcaseDataStore.Products[productId] = product;

        return Task.FromResult(new CreateProductOutput(productId));
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
