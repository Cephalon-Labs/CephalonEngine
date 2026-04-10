using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Audit;
using Cephalon.AspNetCore.Modules;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.Showcase.Domain.Shipping.Models;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Registers the shipping bounded context module.
/// Implements the process-manager behavior pattern with a module-owned REST surface.
/// Uses PostgreSQL (via EF) when available, otherwise falls back to in-memory store.
/// </summary>
public sealed class ShippingModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.shipping",
        displayName: "Showcase Shipping",
        description: "Shipping module using the process-manager behavior pattern with durable checkpoint tracking.",
        tags: ["showcase", "shipping", "process-manager-pattern", "checkpoints"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "showcase.shipping.process-manager",
            displayName: "Shipping process manager",
            description: "Long-running shipping process with checkpoint-based lifecycle management."));
        capabilities.Add(new Capability(
            key: "showcase.shipping.tracking",
            displayName: "Shipment tracking",
            description: "Real-time shipment status tracking across carriers."));
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBehaviorRestGroup(this, "/showcase/shipping");
        var routes = group.Routes;

        routes.MapGet(string.Empty, async (HttpContext ctx) =>
        {
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
            if (db is not null)
            {
                var entities = await db.Shipments
                    .AsNoTracking()
                    .OrderByDescending(s => s.CreatedAtUtc)
                    .ToListAsync();
                return Results.Ok(entities.Select(ToShipmentDto).ToList());
            }

            return Results.Ok(ShowcaseDataStore.Shipments.Values
                .OrderByDescending(s => s.CreatedAtUtc)
                .Select(ToShipmentDto)
                .ToList());
        });

        routes.MapGet("/{shipmentId}", async (string shipmentId, HttpContext ctx) =>
        {
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
            if (db is not null)
            {
                var entity = await db.Shipments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);
                return entity is not null ? Results.Ok(ToShipmentDto(entity)) : Results.NotFound();
            }

            return ShowcaseDataStore.Shipments.TryGetValue(shipmentId, out var shipment)
                ? Results.Ok(ToShipmentDto(shipment))
                : Results.NotFound();
        });

        routes.MapPost(string.Empty, async (InitiateShippingInput input, HttpContext ctx) =>
        {
            var shipmentId = $"shp-{Guid.NewGuid():N}"[..16];
            var trackingNumber = $"TRK-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
            var estimatedDelivery = DateTime.UtcNow.AddDays(3);

            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            if (writeDb is not null)
            {
                var entity = new ShowcaseShipmentEntity
                {
                    ShipmentId = shipmentId,
                    OrderId = input.OrderId,
                    DestinationAddress = input.DestinationAddress,
                    Carrier = "Showcase Express",
                    TrackingNumber = trackingNumber,
                    Status = "LabelCreated",
                    EstimatedDeliveryUtc = estimatedDelivery,
                    CreatedAtUtc = DateTime.UtcNow
                };
                writeDb.Shipments.Add(entity);
                await writeDb.SaveChangesAsync(ctx.RequestAborted);

                if (readDb is not null)
                {
                    await UpsertReadShipmentAsync(readDb, entity, ctx.RequestAborted);
                }

                await ShowcaseAuditHelper.RecordAsync(
                    ctx,
                    new Cephalon.Audit.Services.AuditRecordRequest(
                        category: "shipping",
                        action: "shipment-initiated",
                        summary: $"Initiated shipment '{shipmentId}' for order '{input.OrderId}'.",
                        subjectType: "shipment",
                        subjectId: shipmentId,
                        outcome: AuditOutcome.Succeeded,
                        changes:
                        [
                            new AuditChange("status", null, entity.Status),
                            new AuditChange("trackingNumber", null, entity.TrackingNumber)
                        ],
                        tags: ["shipping", "initiate", "process-manager"],
                        metadata: ShowcaseAuditHelper.CreateMetadata(Descriptor.Id, "/api/v1/showcase/shipping")));

                return Results.Created(
                    BuildCreatedLocation(ctx, shipmentId),
                    new InitiateShippingOutput(shipmentId, "LabelCreated", estimatedDelivery));
            }

            var shipment = new Shipment
            {
                ShipmentId = shipmentId,
                OrderId = input.OrderId,
                DestinationAddress = input.DestinationAddress,
                TrackingNumber = trackingNumber,
                Status = ShipmentStatus.LabelCreated,
                EstimatedDeliveryUtc = estimatedDelivery,
                CreatedAtUtc = DateTime.UtcNow
            };
            ShowcaseDataStore.Shipments[shipmentId] = shipment;
            return Results.Created(
                BuildCreatedLocation(ctx, shipmentId),
                new InitiateShippingOutput(shipmentId, "LabelCreated", estimatedDelivery));
        });

        routes.MapPut("/{shipmentId}/deliver", async (string shipmentId, ConfirmDeliveryInput input, HttpContext ctx) =>
        {
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            if (writeDb is not null)
            {
                var entity = await writeDb.Shipments.FindAsync([shipmentId], ctx.RequestAborted);
                if (entity is null)
                {
                    return Results.NotFound();
                }

                entity.Status = "Delivered";
                entity.DeliveredAtUtc = DateTime.UtcNow;
                await writeDb.SaveChangesAsync(ctx.RequestAborted);

                var order = await writeDb.Orders.FindAsync([entity.OrderId], ctx.RequestAborted);
                if (order is not null)
                {
                    order.Status = "Delivered";
                    order.UpdatedAtUtc = DateTime.UtcNow;
                    await writeDb.SaveChangesAsync(ctx.RequestAborted);
                }

                if (readDb is not null)
                {
                    await UpsertReadShipmentAsync(readDb, entity, ctx.RequestAborted);
                    if (order is not null)
                    {
                        await UpsertReadOrderStatusAsync(readDb, order, ctx.RequestAborted);
                    }
                }

                await ShowcaseAuditHelper.RecordAsync(
                    ctx,
                    new Cephalon.Audit.Services.AuditRecordRequest(
                        category: "shipping",
                        action: "shipment-delivered",
                        summary: $"Confirmed delivery for shipment '{shipmentId}'.",
                        subjectType: "shipment",
                        subjectId: shipmentId,
                        outcome: AuditOutcome.Succeeded,
                        changes:
                        [
                            new AuditChange("status", "LabelCreated", "Delivered"),
                            new AuditChange("recipientName", null, input.RecipientName)
                        ],
                        tags: ["shipping", "deliver", "tracking"],
                        metadata: ShowcaseAuditHelper.CreateMetadata(Descriptor.Id, $"/api/v1/showcase/shipping/{shipmentId}/deliver")));

                return Results.Ok(new ConfirmDeliveryOutput(
                    shipmentId,
                    "Delivered",
                    entity.DeliveredAtUtc.Value));
            }

            if (!ShowcaseDataStore.Shipments.TryGetValue(shipmentId, out var shipment))
            {
                return Results.NotFound();
            }

            shipment.Status = ShipmentStatus.Delivered;
            shipment.DeliveredAtUtc = DateTime.UtcNow;

            if (ShowcaseDataStore.Orders.TryGetValue(shipment.OrderId, out var memOrder))
            {
                memOrder.Status = Domain.Orders.Models.OrderStatus.Delivered;
                memOrder.UpdatedAtUtc = DateTime.UtcNow;
            }

            return Results.Ok(new ConfirmDeliveryOutput(
                shipmentId,
                "Delivered",
                shipment.DeliveredAtUtc!.Value));
        });
    }

    private static async Task UpsertReadShipmentAsync(
        ShowcaseReadDbContext readDb,
        ShowcaseShipmentEntity source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readDb);
        ArgumentNullException.ThrowIfNull(source);

        var projection = await readDb.Shipments.FindAsync([source.ShipmentId], cancellationToken);
        if (projection is null)
        {
            projection = new ShowcaseShipmentEntity
            {
                ShipmentId = source.ShipmentId
            };
            readDb.Shipments.Add(projection);
        }

        projection.OrderId = source.OrderId;
        projection.DestinationAddress = source.DestinationAddress;
        projection.Carrier = source.Carrier;
        projection.TrackingNumber = source.TrackingNumber;
        projection.Status = source.Status;
        projection.EstimatedDeliveryUtc = source.EstimatedDeliveryUtc;
        projection.DeliveredAtUtc = source.DeliveredAtUtc;
        projection.CreatedAtUtc = source.CreatedAtUtc;

        await readDb.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertReadOrderStatusAsync(
        ShowcaseReadDbContext readDb,
        ShowcaseOrderEntity source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readDb);
        ArgumentNullException.ThrowIfNull(source);

        var projection = await readDb.Orders.FindAsync([source.OrderId], cancellationToken);
        if (projection is null)
        {
            return;
        }

        projection.Status = source.Status;
        projection.UpdatedAtUtc = source.UpdatedAtUtc;
        projection.CancellationReason = source.CancellationReason;
        await readDb.SaveChangesAsync(cancellationToken);
    }

    private static object ToShipmentDto(ShowcaseShipmentEntity entity)
    {
        return new
        {
            entity.ShipmentId,
            entity.OrderId,
            entity.DestinationAddress,
            entity.Carrier,
            entity.TrackingNumber,
            entity.Status,
            entity.EstimatedDeliveryUtc,
            entity.DeliveredAtUtc,
            entity.CreatedAtUtc
        };
    }

    private static object ToShipmentDto(Shipment shipment)
    {
        return new
        {
            shipment.ShipmentId,
            shipment.OrderId,
            shipment.DestinationAddress,
            shipment.Carrier,
            shipment.TrackingNumber,
            Status = shipment.Status.ToString(),
            shipment.EstimatedDeliveryUtc,
            shipment.DeliveredAtUtc,
            shipment.CreatedAtUtc
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
