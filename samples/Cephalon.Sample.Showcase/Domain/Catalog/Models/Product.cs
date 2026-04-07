namespace Cephalon.Sample.Showcase.Domain.Catalog.Models;

/// <summary>
/// Represents a product in the catalog domain.
/// </summary>
public sealed class Product
{
    /// <summary>Gets or sets the unique product identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets or sets the product SKU code.</summary>
    public required string Sku { get; init; }

    /// <summary>Gets or sets the product display name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the product description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the product category.</summary>
    public required string Category { get; set; }

    /// <summary>Gets or sets the unit price in the smallest currency unit (e.g., cents).</summary>
    public required long PriceInCents { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets whether the product is currently active and visible.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the product tags for search and filtering.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the UTC timestamp when the product was created.</summary>
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when the product was last modified.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Input for retrieving a single product by identifier.
/// </summary>
/// <param name="ProductId">The product identifier to look up.</param>
public sealed record GetProductInput(string ProductId);

/// <summary>
/// Output returned when a product is found.
/// </summary>
/// <param name="Product">The product details.</param>
public sealed record GetProductOutput(Product Product);

/// <summary>
/// Input for listing products with optional filtering.
/// </summary>
/// <param name="Category">Optional category filter.</param>
/// <param name="ActiveOnly">Whether to return only active products.</param>
/// <param name="MaxResults">Maximum number of results to return.</param>
public sealed record ListProductsInput(string? Category = null, bool ActiveOnly = true, int MaxResults = 50);

/// <summary>
/// Output containing a list of matching products.
/// </summary>
/// <param name="Products">The matching products.</param>
/// <param name="TotalCount">The total count of matching products.</param>
public sealed record ListProductsOutput(IReadOnlyList<Product> Products, int TotalCount);

/// <summary>
/// Input for creating a new product in the catalog.
/// </summary>
/// <param name="Sku">The product SKU code.</param>
/// <param name="Name">The product display name.</param>
/// <param name="Description">Optional product description.</param>
/// <param name="Category">The product category.</param>
/// <param name="PriceInCents">The unit price in the smallest currency unit.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="Tags">Optional product tags.</param>
public sealed record CreateProductInput(
    string Sku,
    string Name,
    string? Description,
    string Category,
    long PriceInCents,
    string Currency = "USD",
    List<string>? Tags = null);

/// <summary>
/// Output returned after a product is created.
/// </summary>
/// <param name="ProductId">The newly assigned product identifier.</param>
public sealed record CreateProductOutput(string ProductId);

/// <summary>
/// Input for updating an existing product.
/// </summary>
/// <param name="ProductId">The product identifier to update.</param>
/// <param name="Name">Optional new display name.</param>
/// <param name="Description">Optional new description.</param>
/// <param name="PriceInCents">Optional new price.</param>
/// <param name="IsActive">Optional new active state.</param>
public sealed record UpdateProductInput(
    string ProductId,
    string? Name = null,
    string? Description = null,
    long? PriceInCents = null,
    bool? IsActive = null);

/// <summary>
/// Output confirming a product update.
/// </summary>
/// <param name="Updated">Whether the product was modified.</param>
public sealed record UpdateProductOutput(bool Updated);
