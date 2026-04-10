using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Audit;
using System.Globalization;
using Cephalon.AspNetCore.Modules;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.Showcase.Domain.Orders.Models;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Registers the orders bounded context module.
/// Implements the event-driven behavior pattern with a module-owned REST surface.
/// Uses PostgreSQL (via EF) when available, otherwise falls back to in-memory store.
/// </summary>
public sealed class OrdersModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.orders",
        displayName: "Showcase Orders",
        description: "Order management module using the event-driven behavior pattern with messaging integration.",
        tags: ["showcase", "orders", "event-driven-pattern", "messaging"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "showcase.orders.event-driven",
            displayName: "Orders event-driven",
            description: "Event-driven order processing with fire-and-forget semantics."));
        capabilities.Add(new Capability(
            key: "showcase.orders.outbox",
            displayName: "Orders outbox",
            description: "Transactional outbox for reliable downstream event delivery."));
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBehaviorRestGroup(this, "/showcase/orders");
        var routes = group.Routes;

        routes.MapGet(string.Empty, async (HttpContext ctx) =>
        {
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
            if (db is not null)
            {
                var entities = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.PlacedAtUtc)
                    .ToListAsync();
                return Results.Ok(entities.Select(ToOrderDto).ToList());
            }

            return Results.Ok(ShowcaseDataStore.Orders.Values
                .OrderByDescending(o => o.PlacedAtUtc)
                .Select(ToOrderDto)
                .ToList());
        });

        routes.MapGet("/{orderId}", async (string orderId, HttpContext ctx) =>
        {
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
            if (db is not null)
            {
                var entity = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);
                return entity is not null ? Results.Ok(ToOrderDto(entity)) : Results.NotFound();
            }

            return ShowcaseDataStore.Orders.TryGetValue(orderId, out var order)
                ? Results.Ok(ToOrderDto(order))
                : Results.NotFound();
        });

        routes.MapPost(string.Empty, async (PlaceOrderInput input, HttpContext ctx) =>
        {
            var orderId = $"ord-{Guid.NewGuid():N}"[..16];
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();

            if (writeDb is not null)
            {
                var entity = new ShowcaseOrderEntity
                {
                    OrderId = orderId,
                    CustomerId = input.CustomerId,
                    ShippingAddress = input.ShippingAddress,
                    Status = "Pending",
                    TotalInCents = input.Items.Sum(i => (long)i.Quantity * i.UnitPriceInCents),
                    PlacedAtUtc = DateTime.UtcNow,
                    Items = input.Items.Select(i => new ShowcaseOrderLineItemEntity
                    {
                        OrderId = orderId,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPriceInCents = i.UnitPriceInCents
                    }).ToList()
                };
                writeDb.Orders.Add(entity);
                await writeDb.SaveChangesAsync(ctx.RequestAborted);

                if (readDb is not null)
                {
                    await UpsertReadOrderAsync(readDb, entity, ctx.RequestAborted);
                }

                await ShowcaseAuditHelper.RecordAsync(
                    ctx,
                    new Cephalon.Audit.Services.AuditRecordRequest(
                        category: "orders",
                        action: "order-placed",
                        summary: $"Placed order '{orderId}'.",
                        subjectType: "order",
                        subjectId: orderId,
                        outcome: AuditOutcome.Succeeded,
                        changes:
                        [
                            new AuditChange("status", null, entity.Status),
                            new AuditChange("totalInCents", null, entity.TotalInCents.ToString(CultureInfo.InvariantCulture)),
                            new AuditChange("itemCount", null, entity.Items.Count.ToString(CultureInfo.InvariantCulture))
                        ],
                        tags: ["orders", "place", "event-driven"],
                        metadata: ShowcaseAuditHelper.CreateMetadata(Descriptor.Id, "/api/v1/showcase/orders")));

                return Results.Created(BuildCreatedLocation(ctx, orderId), new PlaceOrderOutput(orderId, "Pending"));
            }

            var order = new Order
            {
                OrderId = orderId,
                CustomerId = input.CustomerId,
                ShippingAddress = input.ShippingAddress,
                Status = OrderStatus.Pending,
                Items = input.Items.Select(i => new OrderLineItem(
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPriceInCents)).ToList(),
                TotalInCents = input.Items.Sum(i => (long)i.Quantity * i.UnitPriceInCents),
                PlacedAtUtc = DateTime.UtcNow
            };
            ShowcaseDataStore.Orders[orderId] = order;
            return Results.Created(BuildCreatedLocation(ctx, orderId), new PlaceOrderOutput(orderId, "Pending"));
        });

        routes.MapPut("/{orderId}/cancel", async (string orderId, CancelOrderInput input, HttpContext ctx) =>
        {
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            if (writeDb is not null)
            {
                var entity = await writeDb.Orders.FindAsync([orderId], ctx.RequestAborted);
                if (entity is null)
                {
                    return Results.NotFound();
                }

                if (entity.Status is "Shipped" or "Delivered")
                {
                    return Results.BadRequest($"Order '{orderId}' cannot be cancelled in status '{entity.Status}'.");
                }

                entity.Status = "Cancelled";
                entity.CancellationReason = input.Reason;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                await writeDb.SaveChangesAsync(ctx.RequestAborted);

                if (readDb is not null)
                {
                    await UpsertReadOrderAsync(readDb, entity, ctx.RequestAborted);
                }

                await ShowcaseAuditHelper.RecordAsync(
                    ctx,
                    new Cephalon.Audit.Services.AuditRecordRequest(
                        category: "orders",
                        action: "order-cancelled",
                        summary: $"Cancelled order '{orderId}'.",
                        subjectType: "order",
                        subjectId: orderId,
                        outcome: AuditOutcome.Succeeded,
                        changes:
                        [
                            new AuditChange("status", "Pending", "Cancelled"),
                            new AuditChange("cancellationReason", null, input.Reason)
                        ],
                        tags: ["orders", "cancel", "event-driven"],
                        metadata: ShowcaseAuditHelper.CreateMetadata(Descriptor.Id, $"/api/v1/showcase/orders/{orderId}/cancel")));

                return Results.Ok(new CancelOrderOutput(orderId, "Cancelled"));
            }

            if (!ShowcaseDataStore.Orders.TryGetValue(orderId, out var order))
            {
                return Results.NotFound();
            }

            if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
            {
                return Results.BadRequest($"Order '{orderId}' cannot be cancelled in status '{order.Status}'.");
            }

            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = input.Reason;
            order.UpdatedAtUtc = DateTime.UtcNow;
            return Results.Ok(new CancelOrderOutput(orderId, "Cancelled"));
        });
    }

    private static async Task UpsertReadOrderAsync(
        ShowcaseReadDbContext readDb,
        ShowcaseOrderEntity source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readDb);
        ArgumentNullException.ThrowIfNull(source);

        var projection = await readDb.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.OrderId == source.OrderId, cancellationToken);
        if (projection is null)
        {
            projection = new ShowcaseOrderEntity
            {
                OrderId = source.OrderId
            };
            readDb.Orders.Add(projection);
        }
        else
        {
            var existingItems = await readDb.OrderLineItems
                .Where(item => item.OrderId == source.OrderId)
                .ToListAsync(cancellationToken);
            readDb.OrderLineItems.RemoveRange(existingItems);
        }

        projection.CustomerId = source.CustomerId;
        projection.TenantId = source.TenantId;
        projection.Status = source.Status;
        projection.TotalInCents = source.TotalInCents;
        projection.ShippingAddress = source.ShippingAddress;
        projection.PlacedAtUtc = source.PlacedAtUtc;
        projection.UpdatedAtUtc = source.UpdatedAtUtc;
        projection.CancellationReason = source.CancellationReason;
        projection.Items = source.Items
            .Select(item => new ShowcaseOrderLineItemEntity
            {
                OrderId = source.OrderId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPriceInCents = item.UnitPriceInCents
            })
            .ToList();

        await readDb.SaveChangesAsync(cancellationToken);
    }

    private static object ToOrderDto(ShowcaseOrderEntity entity)
    {
        return new
        {
            entity.OrderId,
            entity.CustomerId,
            entity.TenantId,
            entity.Status,
            Items = entity.Items.Select(i => new
            {
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPriceInCents,
                LineTotalInCents = (long)i.Quantity * i.UnitPriceInCents
            }).ToList(),
            entity.TotalInCents,
            entity.ShippingAddress,
            entity.PlacedAtUtc,
            entity.UpdatedAtUtc,
            entity.CancellationReason
        };
    }

    private static object ToOrderDto(Order order)
    {
        return new
        {
            order.OrderId,
            order.CustomerId,
            order.TenantId,
            Status = order.Status.ToString(),
            Items = order.Items.Select(i => new
            {
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPriceInCents,
                i.LineTotalInCents
            }).ToList(),
            order.TotalInCents,
            order.ShippingAddress,
            order.PlacedAtUtc,
            order.UpdatedAtUtc,
            order.CancellationReason
        };
    }

    private static string BuildCreatedLocation(HttpContext context, string resourceId)
    {
        var requestPath = context.Request.Path.Value?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(requestPath)
            ? $"/{resourceId}"
            : $"{requestPath}/{resourceId}";
    }
}
