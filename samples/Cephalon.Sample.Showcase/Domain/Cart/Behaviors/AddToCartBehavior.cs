using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Cart.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Cart.Behaviors;

/// <summary>
/// Adds an item to the shopping cart using the CQRS pattern with event sourcing.
/// </summary>
/// <remarks>
/// Demonstrates a transport-neutral behavior contract with <see cref="BehaviorResult{T}" />:
/// valid requests return the updated cart summary, invalid requests return multiple structured
/// validation faults, and attempts to add items after checkout return a conflict result without
/// throwing transport-shaped exceptions.
/// </remarks>
[AppBehavior("cart.add-item")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.grpc")]
public sealed class AddToCartBehavior : IAppBehavior<AddToCartInput, BehaviorResult<AddToCartOutput>>
{
    /// <inheritdoc />
    public async Task<BehaviorResult<AddToCartOutput>> HandleAsync(
        AddToCartInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var validationFaults = Validate(input);
        if (validationFaults.Count > 0)
        {
            return BehaviorResult.Invalid(
                "showcase.cart.add_item.invalid",
                "Cart add-item request is invalid.",
                new BehaviorFault
                {
                    Code = "showcase.cart.add_item.invalid",
                    Message = "Cart add-item request is invalid.",
                    Severity = BehaviorFaultSeverity.Error,
                    InnerFaults = validationFaults
                });
        }

        var streamId = $"cart-{input.CartId}";
        var eventStore = context.EventStore
            ?? throw new InvalidOperationException("Event store is required for the cart CQRS pattern.");

        var currentVersion = await eventStore.GetVersionAsync(streamId, ct);
        if (currentVersion >= 0)
        {
            var currentCart = await ShowcaseEventSourcingHelper.RebuildCartAsync(eventStore, streamId, input.CustomerId, ct);
            if (currentCart.IsCheckedOut)
            {
                return BehaviorResult.Conflict(
                    "showcase.cart.add_item.checked_out",
                    $"Cart '{input.CartId}' has already been checked out.",
                    new BehaviorFault
                    {
                        Code = "showcase.cart.add_item.checked_out",
                        Message = $"Cart '{input.CartId}' has already been checked out.",
                        Severity = BehaviorFaultSeverity.Error
                    });
            }
        }

        var evt = new ItemAddedToCart(
            StreamId: streamId,
            StreamVersion: currentVersion + 1,
            OccurredAtUtc: DateTime.UtcNow,
            ProductId: input.ProductId,
            ProductName: input.ProductName,
            Quantity: input.Quantity,
            PriceInCents: input.PriceInCents);

        await eventStore.AppendAsync(streamId, [evt], currentVersion, ct);

        // Rebuild cart for response
        var cart = await ShowcaseEventSourcingHelper.RebuildCartAsync(eventStore, streamId, input.CustomerId, ct);

        return BehaviorResult.Ok(
            new AddToCartOutput(input.CartId, cart.Items.Count, cart.TotalInCents),
            message: "Item added to cart.");
    }

    private static List<BehaviorFault> Validate(AddToCartInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var faults = new List<BehaviorFault>();

        if (string.IsNullOrWhiteSpace(input.CustomerId))
        {
            faults.Add(new BehaviorFault
            {
                Code = "showcase.cart.add_item.customer_id.required",
                Message = "Customer id is required.",
                Severity = BehaviorFaultSeverity.Error
            });
        }

        if (string.IsNullOrWhiteSpace(input.ProductId))
        {
            faults.Add(new BehaviorFault
            {
                Code = "showcase.cart.add_item.product_id.required",
                Message = "Product id is required.",
                Severity = BehaviorFaultSeverity.Error
            });
        }

        if (string.IsNullOrWhiteSpace(input.ProductName))
        {
            faults.Add(new BehaviorFault
            {
                Code = "showcase.cart.add_item.product_name.required",
                Message = "Product name is required.",
                Severity = BehaviorFaultSeverity.Error
            });
        }

        if (input.Quantity <= 0)
        {
            faults.Add(new BehaviorFault
            {
                Code = "showcase.cart.add_item.quantity.invalid",
                Message = "Quantity must be greater than zero.",
                Severity = BehaviorFaultSeverity.Error
            });
        }

        if (input.PriceInCents < 0)
        {
            faults.Add(new BehaviorFault
            {
                Code = "showcase.cart.add_item.price.invalid",
                Message = "Price in cents cannot be negative.",
                Severity = BehaviorFaultSeverity.Error
            });
        }

        return faults;
    }
}
