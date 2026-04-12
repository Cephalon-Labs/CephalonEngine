using System.Text.Json;
using System.Globalization;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.Showcase.Domain.Catalog.Behaviors;
using Cephalon.Sample.Showcase.Domain.Catalog.Models;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Registers the catalog bounded context module.
/// Exposes product CRUD operations through a module-owned REST surface.
/// Uses the EF-backed database role configured for the showcase host.
/// </summary>
public sealed class CatalogModule : RestBehaviorModuleBase
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
    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Internal<ListProductsBehavior>();
        behaviors.Internal<GetProductBehavior>();
        behaviors.Internal<CreateProductBehavior>();
        behaviors.Internal<UpdateProductBehavior>();
    }

    /// <inheritdoc />
    protected override void MapAdditionalEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBehaviorRestGroup(this, "/showcase/catalog");
        var routes = group.Routes;

        routes.MapGet("/products", async (HttpContext ctx) =>
        {
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
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

        routes.MapGet("/products/{productId}", async (string productId, HttpContext ctx) =>
        {
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
            if (db is not null)
            {
                var entity = await db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productId);
                return entity is not null ? Results.Ok(ToProduct(entity)) : Results.NotFound();
            }

            return ShowcaseDataStore.Products.TryGetValue(productId, out var product)
                ? Results.Ok(product)
                : Results.NotFound();
        });

        routes.MapPost("/products", async (CreateProductInput input, HttpContext ctx, ShowcaseReadModelSyncService readModelSync) =>
        {
            var productId = $"prod-{Guid.NewGuid():N}"[..16];
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();

            if (writeDb is not null)
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
                writeDb.Products.Add(entity);
                readModelSync.EnqueueProducts([entity.Id]);
                await writeDb.SaveChangesAsync(ctx.RequestAborted);
                await readModelSync.FlushAsync(ctx.RequestAborted);

                await ShowcaseAuditHelper.RecordAsync(
                    ctx,
                    new Cephalon.Audit.Services.AuditRecordRequest(
                        category: "catalog",
                        action: "product-created",
                        summary: $"Created product '{entity.Name}'.",
                        subjectType: "product",
                        subjectId: entity.Id,
                        outcome: AuditOutcome.Succeeded,
                        changes:
                        [
                            new AuditChange("sku", null, entity.Sku),
                            new AuditChange("name", null, entity.Name),
                            new AuditChange("category", null, entity.Category),
                            new AuditChange("priceInCents", null, entity.PriceInCents.ToString(CultureInfo.InvariantCulture)),
                            new AuditChange("isActive", null, entity.IsActive.ToString())
                        ],
                        tags: ["catalog", "product", "create"],
                        metadata: ShowcaseAuditHelper.CreateMetadata(ctx, Descriptor.Id)));

                return Results.Created(BuildCreatedLocation(ctx, productId), ToProduct(entity));
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
            return Results.Created(BuildCreatedLocation(ctx, productId), product);
        });

        routes.MapPut("/products/{productId}", async (string productId, UpdateProductInput input, HttpContext ctx, ShowcaseReadModelSyncService readModelSync) =>
        {
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            if (writeDb is not null)
            {
                var entity = await writeDb.Products.FindAsync([productId], ctx.RequestAborted);
                if (entity is null)
                {
                    return Results.NotFound();
                }

                var changes = new List<AuditChange>();

                if (input.Name is not null)
                {
                    changes.Add(new AuditChange("name", entity.Name, input.Name));
                    entity.Name = input.Name;
                }

                if (input.Description is not null)
                {
                    changes.Add(new AuditChange("description", entity.Description, input.Description));
                    entity.Description = input.Description;
                }

                if (input.PriceInCents.HasValue)
                {
                    changes.Add(new AuditChange(
                        "priceInCents",
                        entity.PriceInCents.ToString(CultureInfo.InvariantCulture),
                        input.PriceInCents.Value.ToString(CultureInfo.InvariantCulture)));
                    entity.PriceInCents = input.PriceInCents.Value;
                }

                if (input.IsActive.HasValue)
                {
                    changes.Add(new AuditChange("isActive", entity.IsActive.ToString(), input.IsActive.Value.ToString()));
                    entity.IsActive = input.IsActive.Value;
                }

                entity.UpdatedAtUtc = DateTime.UtcNow;
                readModelSync.EnqueueProducts([entity.Id]);
                await writeDb.SaveChangesAsync(ctx.RequestAborted);
                await readModelSync.FlushAsync(ctx.RequestAborted);

                await ShowcaseAuditHelper.RecordAsync(
                    ctx,
                    new Cephalon.Audit.Services.AuditRecordRequest(
                        category: "catalog",
                        action: "product-updated",
                        summary: $"Updated product '{entity.Name}'.",
                        subjectType: "product",
                        subjectId: entity.Id,
                        outcome: AuditOutcome.Succeeded,
                        changes: changes,
                        tags: ["catalog", "product", "update"],
                        metadata: ShowcaseAuditHelper.CreateMetadata(ctx, Descriptor.Id)));

                return Results.Ok(ToProduct(entity));
            }

            if (!ShowcaseDataStore.Products.TryGetValue(productId, out var product))
            {
                return Results.NotFound();
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
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    private static string BuildCreatedLocation(HttpContext context, string resourceId)
    {
        var requestPath = context.Request.Path.Value?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(requestPath)
            ? $"/{resourceId}"
            : $"{requestPath}/{resourceId}";
    }
}
