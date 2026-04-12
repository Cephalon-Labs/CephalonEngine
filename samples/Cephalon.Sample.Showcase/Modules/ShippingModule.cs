using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Audit;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.Showcase.Domain.Shipping.Behaviors;
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
/// Uses the EF-backed database role configured for the showcase host.
/// </summary>
public sealed class ShippingModule : RestBehaviorModuleBase
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
    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Internal<InitiateShippingBehavior>();
        behaviors.Internal<TrackShipmentBehavior>();
        behaviors.Internal<ConfirmDeliveryBehavior>();
    }

    /// <inheritdoc />
    protected override void MapAdditionalEndpoints(IEndpointRouteBuilder endpoints)
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

        routes.MapPost(string.Empty, async (InitiateShippingInput input, HttpContext ctx, ShowcaseReadModelSyncService readModelSync) =>
        {
            var shipmentId = $"shp-{Guid.NewGuid():N}"[..16];
            var trackingNumber = $"TRK-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
            var estimatedDelivery = DateTime.UtcNow.AddDays(3);

            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            if (writeDb is not null)
            {
                var existingShipment = await writeDb.Shipments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(shipment => shipment.OrderId == input.OrderId, ctx.RequestAborted);
                if (existingShipment is not null)
                {
                    return Results.Conflict($"Order '{input.OrderId}' already has shipment '{existingShipment.ShipmentId}'.");
                }

                var order = await writeDb.Orders.FindAsync([input.OrderId], ctx.RequestAborted);
                if (order is not null)
                {
                    order.Status = "Processing";
                    order.UpdatedAtUtc = DateTime.UtcNow;
                }

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
                readModelSync.EnqueueShipments([entity.ShipmentId]);
                if (order is not null)
                {
                    readModelSync.EnqueueOrders([order.OrderId]);
                }

                await writeDb.SaveChangesAsync(ctx.RequestAborted);
                await readModelSync.FlushAsync(ctx.RequestAborted);

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
                        metadata: ShowcaseAuditHelper.CreateMetadata(ctx, Descriptor.Id)));

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
            if (ShowcaseDataStore.Shipments.Values.Any(existing => string.Equals(existing.OrderId, input.OrderId, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.Conflict($"Order '{input.OrderId}' already has an active shipment.");
            }

            ShowcaseDataStore.Shipments[shipmentId] = shipment;
            if (ShowcaseDataStore.Orders.TryGetValue(input.OrderId, out var memOrder))
            {
                memOrder.Status = Domain.Orders.Models.OrderStatus.Processing;
                memOrder.UpdatedAtUtc = DateTime.UtcNow;
            }

            return Results.Created(
                BuildCreatedLocation(ctx, shipmentId),
                new InitiateShippingOutput(shipmentId, "LabelCreated", estimatedDelivery));
        });

        routes.MapPut("/{shipmentId}/deliver", async (string shipmentId, ConfirmDeliveryInput input, HttpContext ctx, ShowcaseReadModelSyncService readModelSync) =>
        {
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            if (writeDb is not null)
            {
                var entity = await writeDb.Shipments.FindAsync([shipmentId], ctx.RequestAborted);
                if (entity is null)
                {
                    return Results.NotFound();
                }

                entity.Status = "Delivered";
                entity.DeliveredAtUtc = DateTime.UtcNow;

                var order = await writeDb.Orders.FindAsync([entity.OrderId], ctx.RequestAborted);
                if (order is not null)
                {
                    order.Status = "Delivered";
                    order.UpdatedAtUtc = DateTime.UtcNow;
                }

                readModelSync.EnqueueShipments([entity.ShipmentId]);
                if (order is not null)
                {
                    readModelSync.EnqueueOrders([order.OrderId]);
                }

                await writeDb.SaveChangesAsync(ctx.RequestAborted);
                await readModelSync.FlushAsync(ctx.RequestAborted);

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
                        metadata: ShowcaseAuditHelper.CreateMetadata(ctx, Descriptor.Id)));

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
