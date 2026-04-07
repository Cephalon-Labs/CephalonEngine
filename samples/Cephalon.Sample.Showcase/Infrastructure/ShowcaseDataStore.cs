using System.Collections.Concurrent;
using Cephalon.Sample.Showcase.Domain.Cart.Models;
using Cephalon.Sample.Showcase.Domain.Catalog.Models;
using Cephalon.Sample.Showcase.Domain.Inventory.Models;
using Cephalon.Sample.Showcase.Domain.Orders.Models;
using Cephalon.Sample.Showcase.Domain.Shipping.Models;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Thread-safe in-memory data store seeded with realistic e-commerce sample data.
/// Used by all five domain modules to demonstrate cross-cutting data flows
/// without requiring external database infrastructure.
/// </summary>
public static class ShowcaseDataStore
{
    /// <summary>Gets the product catalog store.</summary>
    public static ConcurrentDictionary<string, Product> Products { get; } = new(SeedProducts());

    /// <summary>Gets the shopping cart store.</summary>
    public static ConcurrentDictionary<string, ShoppingCart> Carts { get; } = new();

    /// <summary>Gets the order store.</summary>
    public static ConcurrentDictionary<string, Order> Orders { get; } = new();

    /// <summary>Gets the inventory store.</summary>
    public static ConcurrentDictionary<string, InventoryItem> Inventory { get; } = new(SeedInventory());

    /// <summary>Gets the shipment store.</summary>
    public static ConcurrentDictionary<string, Shipment> Shipments { get; } = new();

    private static Dictionary<string, Product> SeedProducts()
    {
        var products = new[]
        {
            new Product
            {
                Id = "prod-001", Sku = "LAPTOP-PRO-15", Name = "ProBook Laptop 15\"",
                Description = "High-performance laptop with 32GB RAM and 1TB SSD",
                Category = "Electronics", PriceInCents = 149999, Currency = "USD",
                Tags = ["laptop", "electronics", "premium"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-002", Sku = "MOUSE-ERGO-01", Name = "ErgoGrip Wireless Mouse",
                Description = "Ergonomic wireless mouse with 4000 DPI sensor",
                Category = "Electronics", PriceInCents = 4999, Currency = "USD",
                Tags = ["mouse", "electronics", "ergonomic"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-003", Sku = "DESK-STAND-ADJ", Name = "AdjustaPro Standing Desk",
                Description = "Electric height-adjustable standing desk, 60\" wide",
                Category = "Furniture", PriceInCents = 59999, Currency = "USD",
                Tags = ["desk", "furniture", "standing"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-004", Sku = "CHAIR-MESH-EXE", Name = "MeshComfort Executive Chair",
                Description = "Breathable mesh office chair with lumbar support",
                Category = "Furniture", PriceInCents = 39999, Currency = "USD",
                Tags = ["chair", "furniture", "office"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-005", Sku = "MONITOR-4K-27", Name = "ClearView 4K Monitor 27\"",
                Description = "4K UHD IPS monitor with USB-C connectivity",
                Category = "Electronics", PriceInCents = 44999, Currency = "USD",
                Tags = ["monitor", "electronics", "4k"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-006", Sku = "KEYBOARD-MECH", Name = "TypeMaster Mechanical Keyboard",
                Description = "Mechanical keyboard with Cherry MX Brown switches and RGB",
                Category = "Electronics", PriceInCents = 12999, Currency = "USD",
                Tags = ["keyboard", "electronics", "mechanical"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-007", Sku = "HEADSET-ANC-PRO", Name = "SilentZone ANC Headset",
                Description = "Active noise cancelling headset with 30-hour battery",
                Category = "Electronics", PriceInCents = 29999, Currency = "USD",
                Tags = ["headset", "electronics", "anc"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-008", Sku = "WEBCAM-HD-1080", Name = "StreamPro HD Webcam",
                Description = "1080p webcam with auto-focus and built-in ring light",
                Category = "Electronics", PriceInCents = 7999, Currency = "USD",
                Tags = ["webcam", "electronics", "streaming"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-009", Sku = "DOCK-USB-C", Name = "UnifyHub USB-C Dock",
                Description = "12-in-1 USB-C docking station with dual 4K output",
                Category = "Electronics", PriceInCents = 18999, Currency = "USD",
                Tags = ["dock", "electronics", "usb-c"], CreatedAtUtc = DateTime.UtcNow
            },
            new Product
            {
                Id = "prod-010", Sku = "CABLE-MGMT-KIT", Name = "TidyDesk Cable Management Kit",
                Description = "Under-desk cable tray with velcro straps and clips",
                Category = "Accessories", PriceInCents = 2499, Currency = "USD",
                Tags = ["cable", "accessories", "organization"], CreatedAtUtc = DateTime.UtcNow
            }
        };

        return products.ToDictionary(p => p.Id);
    }

    private static Dictionary<string, InventoryItem> SeedInventory()
    {
        return new Dictionary<string, InventoryItem>
        {
            ["prod-001"] = new() { ProductId = "prod-001", QuantityOnHand = 50, WarehouseCode = "WH-01" },
            ["prod-002"] = new() { ProductId = "prod-002", QuantityOnHand = 200, WarehouseCode = "WH-01" },
            ["prod-003"] = new() { ProductId = "prod-003", QuantityOnHand = 30, WarehouseCode = "WH-02" },
            ["prod-004"] = new() { ProductId = "prod-004", QuantityOnHand = 75, WarehouseCode = "WH-02" },
            ["prod-005"] = new() { ProductId = "prod-005", QuantityOnHand = 100, WarehouseCode = "WH-01" },
            ["prod-006"] = new() { ProductId = "prod-006", QuantityOnHand = 150, WarehouseCode = "WH-01" },
            ["prod-007"] = new() { ProductId = "prod-007", QuantityOnHand = 80, WarehouseCode = "WH-01" },
            ["prod-008"] = new() { ProductId = "prod-008", QuantityOnHand = 120, WarehouseCode = "WH-01" },
            ["prod-009"] = new() { ProductId = "prod-009", QuantityOnHand = 60, WarehouseCode = "WH-02" },
            ["prod-010"] = new() { ProductId = "prod-010", QuantityOnHand = 300, WarehouseCode = "WH-01" }
        };
    }
}
