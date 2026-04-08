using System.Text.Json;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.Showcase.Domain.Catalog.Models;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Registers the catalog bounded context module.
/// Exposes product CRUD operations via the direct behavior pattern.
/// Uses PostgreSQL (via EF) when available, otherwise falls back to in-memory store.
/// </summary>
public sealed class CatalogModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.catalog",
        displayName: "Showcase Catalog",
        description: "Product catalog module using the direct behavior pattern with multi-transport exposure.",
        tags: ["showcase", "catalog", "direct-pattern"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "showcase.catalog.read",
            displayName: "Catalog read",
            description: "Read access to the product catalog via direct pattern."));
        capabilities.Add(new Capability(
            key: "showcase.catalog.write",
            displayName: "Catalog write",
            description: "Write access to the product catalog via direct pattern."));
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/showcase/catalog")
            .WithGroupName("v1");

        group.MapGet("/products", async (HttpContext ctx) =>
        {
            var db = ctx.RequestServices.GetService<ShowcaseDbContext>();
            if (db is not null)
            {
                var entities = await db.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
                return Results.Ok(entities.Select(ToProduct).ToList());
            }

            return Results.Ok(ShowcaseDataStore.Products.Values
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToList());
        });

        group.MapGet("/products/{productId}", async (string productId, HttpContext ctx) =>
        {
            var db = ctx.RequestServices.GetService<ShowcaseDbContext>();
            if (db is not null)
            {
                var entity = await db.Products.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productId);
                return entity is not null ? Results.Ok(ToProduct(entity)) : Results.NotFound();
            }

            return ShowcaseDataStore.Products.TryGetValue(productId, out var product)
                ? Results.Ok(product)
                : Results.NotFound();
        });

        group.MapPost("/products", async (CreateProductInput input, HttpContext ctx) =>
        {
            var productId = $"prod-{Guid.NewGuid():N}"[..16];
            var db = ctx.RequestServices.GetService<ShowcaseDbContext>();

            if (db is not null)
            {
                var entity = new ShowcaseProductEntity
                {
                    Id = productId,
                    Sku = input.Sku,
                    Name = input.Name,
                    Description = input.Description,
                    Category = input.Category,
                    PriceInCents = input.PriceInCents,
                    Currency = input.Currency,
                    TagsJson = JsonSerializer.Serialize(input.Tags ?? []),
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Products.Add(entity);
                await db.SaveChangesAsync();
                return Results.Created($"/api/v1/showcase/catalog/products/{productId}", ToProduct(entity));
            }

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
            return Results.Created($"/api/v1/showcase/catalog/products/{productId}", product);
        });

        group.MapPut("/products/{productId}", async (string productId, UpdateProductInput input, HttpContext ctx) =>
        {
            var db = ctx.RequestServices.GetService<ShowcaseDbContext>();
            if (db is not null)
            {
                var entity = await db.Products.FindAsync(productId);
                if (entity is null) return Results.NotFound();

                if (input.Name is not null) entity.Name = input.Name;
                if (input.Description is not null) entity.Description = input.Description;
                if (input.PriceInCents.HasValue) entity.PriceInCents = input.PriceInCents.Value;
                if (input.IsActive.HasValue) entity.IsActive = input.IsActive.Value;
                entity.UpdatedAtUtc = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.Ok(ToProduct(entity));
            }

            if (!ShowcaseDataStore.Products.TryGetValue(productId, out var product))
                return Results.NotFound();

            if (input.Name is not null) product.Name = input.Name;
            if (input.Description is not null) product.Description = input.Description;
            if (input.PriceInCents.HasValue) product.PriceInCents = input.PriceInCents.Value;
            if (input.IsActive.HasValue) product.IsActive = input.IsActive.Value;
            product.UpdatedAtUtc = DateTime.UtcNow;

            return Results.Ok(product);
        });
    }

    private static Product ToProduct(ShowcaseProductEntity entity)
    {
        return new Product
        {
            Id = entity.Id,
            Sku = entity.Sku,
            Name = entity.Name,
            Description = entity.Description,
            Category = entity.Category,
            PriceInCents = entity.PriceInCents,
            Currency = entity.Currency,
            IsActive = entity.IsActive,
            Tags = DeserializeTags(entity.TagsJson),
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static List<string> DeserializeTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }
}
