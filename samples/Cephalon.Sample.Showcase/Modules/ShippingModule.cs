using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.Showcase.Domain.Shipping.Models;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Registers the shipping bounded context module.
/// Implements the process-manager behavior pattern — shipment lifecycle
/// is tracked through durable checkpoints from initiation to delivery confirmation.
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
        var group = endpoints.MapGroup("/v1/showcase/shipping")
            .WithGroupName("v1");

        group.MapGet("/", async (HttpContext ctx) =>
        {
            var db = ctx.RequestServices.GetService<ShowcaseDbContext>();
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

        group.MapGet("/{shipmentId}", async (string shipmentId, HttpContext ctx) =>
        {
            var db = ctx.RequestServices.GetService<ShowcaseDbContext>();
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

        group.MapPost("/", async (InitiateShippingInput input, HttpContext ctx) =>
        {
            var shipmentId = $"shp-{Guid.NewGuid():N}"[..16];
            var trackingNumber = $"TRK-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
            var estimatedDelivery = DateTime.UtcNow.AddDays(3);

            var db = ctx.RequestServices.GetService<ShowcaseDbContext>();
            if (db is not null)
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
                db.Shipments.Add(entity);
                await db.SaveChangesAsync();
                return Results.Created(BuildCreatedLocation(ctx, shipmentId),
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
            return Results.Created(BuildCreatedLocation(ctx, shipmentId),
                new InitiateShippingOutput(shipmentId, "LabelCreated", estimatedDelivery));
        });

        group.MapPut("/{shipmentId}/deliver", async (string shipmentId, ConfirmDeliveryInput input, HttpContext ctx) =>
        {
            var db = ctx.RequestServices.GetService<ShowcaseDbContext>();
            if (db is not null)
            {
                var entity = await db.Shipments.FindAsync(shipmentId);
                if (entity is null) return Results.NotFound();

                entity.Status = "Delivered";
                entity.DeliveredAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();

                // Synchronize order status to Delivered
                var order = await db.Orders.FindAsync(entity.OrderId);
                if (order is not null)
                {
                    order.Status = "Delivered";
                    order.UpdatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }

                return Results.Ok(new ConfirmDeliveryOutput(shipmentId, "Delivered", entity.DeliveredAtUtc.Value));
            }

            if (!ShowcaseDataStore.Shipments.TryGetValue(shipmentId, out var shipment))
                return Results.NotFound();

            shipment.Status = ShipmentStatus.Delivered;
            shipment.DeliveredAtUtc = DateTime.UtcNow;

            // Synchronize order status
            if (ShowcaseDataStore.Orders.TryGetValue(shipment.OrderId, out var memOrder))
            {
                memOrder.Status = Domain.Orders.Models.OrderStatus.Delivered;
                memOrder.UpdatedAtUtc = DateTime.UtcNow;
            }

            return Results.Ok(new ConfirmDeliveryOutput(shipmentId, "Delivered", shipment.DeliveredAtUtc!.Value));
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
