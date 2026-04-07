using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.Sample.Showcase.Domain.Cart.Models;

/// <summary>
/// Represents a shopping cart aggregate rebuilt from its event stream.
/// </summary>
public sealed class ShoppingCart
{
    /// <summary>Gets the cart identifier (matches the event stream id).</summary>
    public required string CartId { get; init; }

    /// <summary>Gets the tenant owning this cart.</summary>
    public string? TenantId { get; init; }

    /// <summary>Gets the customer identifier.</summary>
    public required string CustomerId { get; init; }

    /// <summary>Gets the current items in the cart.</summary>
    public Dictionary<string, CartItem> Items { get; init; } = new();

    /// <summary>Gets or sets whether the cart has been checked out.</summary>
    public bool IsCheckedOut { get; set; }

    /// <summary>Gets the current stream version.</summary>
    public long Version { get; set; }

    /// <summary>
    /// Computes the total price of all items in the cart.
    /// </summary>
    public long TotalInCents => Items.Values.Sum(i => i.PriceInCents * i.Quantity);
}

/// <summary>
/// Represents a single line item in a shopping cart.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="ProductName">The product display name at the time of addition.</param>
/// <param name="Quantity">The quantity of this item.</param>
/// <param name="PriceInCents">The unit price at the time of addition.</param>
public sealed record CartItem(string ProductId, string ProductName, int Quantity, long PriceInCents);

// --- Cart domain events ---

/// <summary>Raised when an item is added to the cart.</summary>
public sealed record ItemAddedToCart(
    string StreamId,
    long StreamVersion,
    DateTime OccurredAtUtc,
    string ProductId,
    string ProductName,
    int Quantity,
    long PriceInCents) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

/// <summary>Raised when an item is removed from the cart.</summary>
public sealed record ItemRemovedFromCart(
    string StreamId,
    long StreamVersion,
    DateTime OccurredAtUtc,
    string ProductId) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

/// <summary>Raised when the cart is checked out, locking further modifications.</summary>
public sealed record CartCheckedOut(
    string StreamId,
    long StreamVersion,
    DateTime OccurredAtUtc,
    string OrderId) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

/// <summary>
/// Applies cart domain events to rebuild <see cref="ShoppingCart"/> state.
/// </summary>
public sealed class ShoppingCartAggregate : IAggregate<ShoppingCart>
{
    /// <summary>
    /// Applies a single domain event to the current cart state.
    /// </summary>
    /// <param name="current">The current cart state.</param>
    /// <param name="evt">The domain event to apply.</param>
    /// <returns>The updated cart state.</returns>
    public ShoppingCart Apply(ShoppingCart current, IDomainEvent evt)
    {
        switch (evt)
        {
            case ItemAddedToCart added:
                current.Items[added.ProductId] = new CartItem(
                    added.ProductId, added.ProductName, added.Quantity, added.PriceInCents);
                current.Version = added.StreamVersion;
                break;

            case ItemRemovedFromCart removed:
                current.Items.Remove(removed.ProductId);
                current.Version = removed.StreamVersion;
                break;

            case CartCheckedOut checkedOut:
                current.IsCheckedOut = true;
                current.Version = checkedOut.StreamVersion;
                break;
        }

        return current;
    }
}

// --- Cart behavior input/output contracts ---

/// <summary>Input for adding an item to a cart.</summary>
public sealed record AddToCartInput(
    string CartId,
    string CustomerId,
    string ProductId,
    string ProductName,
    int Quantity,
    long PriceInCents);

/// <summary>Output after adding an item to a cart.</summary>
public sealed record AddToCartOutput(string CartId, int ItemCount, long TotalInCents);

/// <summary>Input for removing an item from a cart.</summary>
public sealed record RemoveFromCartInput(string CartId, string ProductId);

/// <summary>Output after removing an item from a cart.</summary>
public sealed record RemoveFromCartOutput(string CartId, int ItemCount, long TotalInCents);

/// <summary>Input for retrieving cart contents.</summary>
public sealed record GetCartInput(string CartId);

/// <summary>Output containing cart details.</summary>
public sealed record GetCartOutput(ShoppingCart Cart);

/// <summary>Input for checking out a cart.</summary>
public sealed record CheckoutCartInput(string CartId, string ShippingAddress);

/// <summary>Output after a cart checkout completes.</summary>
public sealed record CheckoutCartOutput(string OrderId, long TotalInCents, int ItemCount);
