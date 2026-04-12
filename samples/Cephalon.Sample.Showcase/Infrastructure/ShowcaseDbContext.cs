using System.Text.Json;
using Cephalon.Audit.EntityFramework;
using Cephalon.Audit.EntityFramework.Modeling;
using Cephalon.Data.EntityFramework.Modeling;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Shared Entity Framework Core model for the showcase sample commerce read and write databases.
/// </summary>
public abstract class ShowcaseCommerceDbContextBase(DbContextOptions options)
    : DbContext(options)
{
    /// <summary>Gets the catalog products table.</summary>
    public DbSet<ShowcaseProductEntity> Products => Set<ShowcaseProductEntity>();

    /// <summary>Gets the orders table.</summary>
    public DbSet<ShowcaseOrderEntity> Orders => Set<ShowcaseOrderEntity>();

    /// <summary>Gets the order line items table.</summary>
    public DbSet<ShowcaseOrderLineItemEntity> OrderLineItems => Set<ShowcaseOrderLineItemEntity>();

    /// <summary>Gets the inventory table.</summary>
    public DbSet<ShowcaseInventoryEntity> InventoryItems => Set<ShowcaseInventoryEntity>();

    /// <summary>Gets the shipments table.</summary>
    public DbSet<ShowcaseShipmentEntity> Shipments => Set<ShowcaseShipmentEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // --- Catalog: Products ---
        modelBuilder.Entity<ShowcaseProductEntity>(entity =>
        {
            entity.ToTable("showcase_products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(64);
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(64);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(256);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(128);
            entity.Property(e => e.PriceInCents).HasColumnName("price_in_cents");
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(3);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.TagsJson).HasColumnName("tags_json").HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // --- Orders ---
        modelBuilder.Entity<ShowcaseOrderEntity>(entity =>
        {
            entity.ToTable("showcase_orders");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(64);
            entity.Property(e => e.CustomerId).HasColumnName("customer_id").HasMaxLength(128);
            entity.Property(e => e.TenantId).HasColumnName("tenant_id").HasMaxLength(64);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32);
            entity.Property(e => e.TotalInCents).HasColumnName("total_in_cents");
            entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address").HasMaxLength(1000);
            entity.Property(e => e.PlacedAtUtc).HasColumnName("placed_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasMany(e => e.Items).WithOne().HasForeignKey(li => li.OrderId);
        });

        modelBuilder.Entity<ShowcaseOrderLineItemEntity>(entity =>
        {
            entity.ToTable("showcase_order_line_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(64);
            entity.Property(e => e.ProductId).HasColumnName("product_id").HasMaxLength(64);
            entity.Property(e => e.ProductName).HasColumnName("product_name").HasMaxLength(256);
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPriceInCents).HasColumnName("unit_price_in_cents");
        });

        // --- Inventory ---
        modelBuilder.Entity<ShowcaseInventoryEntity>(entity =>
        {
            entity.ToTable("showcase_inventory");
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.ProductId).HasColumnName("product_id").HasMaxLength(64);
            entity.Property(e => e.QuantityOnHand).HasColumnName("quantity_on_hand");
            entity.Property(e => e.QuantityReserved).HasColumnName("quantity_reserved");
            entity.Property(e => e.WarehouseCode).HasColumnName("warehouse_code").HasMaxLength(16);
            entity.Property(e => e.LastUpdatedAtUtc).HasColumnName("last_updated_at_utc");
        });

        // --- Shipping ---
        modelBuilder.Entity<ShowcaseShipmentEntity>(entity =>
        {
            entity.ToTable("showcase_shipments");
            entity.HasKey(e => e.ShipmentId);
            entity.Property(e => e.ShipmentId).HasColumnName("shipment_id").HasMaxLength(64);
            entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(64);
            entity.Property(e => e.DestinationAddress).HasColumnName("destination_address").HasMaxLength(1000);
            entity.Property(e => e.Carrier).HasColumnName("carrier").HasMaxLength(128);
            entity.Property(e => e.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(128);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32);
            entity.Property(e => e.EstimatedDeliveryUtc).HasColumnName("estimated_delivery_utc");
            entity.Property(e => e.DeliveredAtUtc).HasColumnName("delivered_at_utc");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);
        });

    }
}

/// <summary>
/// Read-side Entity Framework Core DbContext for the showcase sample.
/// </summary>
public sealed class ShowcaseReadDbContext(DbContextOptions<ShowcaseReadDbContext> options)
    : ShowcaseCommerceDbContextBase(options);

/// <summary>
/// Write-side Entity Framework Core DbContext for the showcase sample.
/// </summary>
public sealed class ShowcaseWriteDbContext(DbContextOptions<ShowcaseWriteDbContext> options)
    : ShowcaseCommerceDbContextBase(options), IEntityFrameworkOutboxContext, IEntityFrameworkInboxContext
{
    /// <inheritdoc />
    public DbSet<EntityFrameworkOutboxEntry> OutboxMessages => Set<EntityFrameworkOutboxEntry>();

    /// <inheritdoc />
    public DbSet<EntityFrameworkInboxEntry> InboxMessages => Set<EntityFrameworkInboxEntry>();

    /// <summary>Gets the durable read-model projection jobs staged in the write database.</summary>
    public DbSet<ShowcaseReadProjectionJobEntity> ReadProjectionJobs => Set<ShowcaseReadProjectionJobEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureCephalonOutbox();
        modelBuilder.ConfigureCephalonInbox();
        modelBuilder.Entity<ShowcaseReadProjectionJobEntity>(entity =>
        {
            entity.ToTable("showcase_read_projection_jobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(32);
            entity.Property(e => e.EntityKey).HasColumnName("entity_key").HasMaxLength(128);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.LastAttemptAtUtc).HasColumnName("last_attempt_at_utc");
            entity.Property(e => e.AttemptCount).HasColumnName("attempt_count");
            entity.Property(e => e.AvailableAtUtc).HasColumnName("available_at_utc");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.LastError).HasColumnName("last_error").HasMaxLength(4000);
            entity.HasIndex(e => new { e.CompletedAtUtc, e.AvailableAtUtc });
            entity.HasIndex(e => new { e.Scope, e.EntityKey, e.CompletedAtUtc });
        });
    }
}

/// <summary>
/// Durable audit-history Entity Framework Core DbContext for the showcase sample.
/// </summary>
public sealed class ShowcaseAuditHistoryDbContext(DbContextOptions<ShowcaseAuditHistoryDbContext> options)
    : DbContext(options), IEntityFrameworkAuditHistoryContext
{
    /// <inheritdoc />
    public DbSet<EntityFrameworkAuditHistoryEntry> AuditEntries => Set<EntityFrameworkAuditHistoryEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ConfigureCephalonAuditHistory(tableName: "showcase_audit_history");
    }
}

/// <summary>
/// Durable projection job persisted in the write database so the read model can recover after failures.
/// </summary>
public sealed class ShowcaseReadProjectionJobEntity
{
    /// <summary>Gets or sets the auto-generated row identifier.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the projection scope name.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Gets or sets the aggregate or entity key to project.</summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the last attempt timestamp.</summary>
    public DateTime? LastAttemptAtUtc { get; set; }

    /// <summary>Gets or sets the number of projection attempts.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Gets or sets when the next attempt becomes eligible.</summary>
    public DateTime AvailableAtUtc { get; set; }

    /// <summary>Gets or sets when the job completed successfully.</summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Gets or sets the last projection error, when available.</summary>
    public string? LastError { get; set; }
}

// ──────────────────────────────────────────────
//  Catalog entities
// ──────────────────────────────────────────────

/// <summary>
/// EF entity for the showcase products table in PostgreSQL.
/// </summary>
public sealed class ShowcaseProductEntity
{
    /// <summary>Gets or sets the product identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the SKU code.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the price in smallest currency unit.</summary>
    public long PriceInCents { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets whether the product is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the tags as JSON array string.</summary>
    public string TagsJson { get; set; } = "[]";

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}

// ──────────────────────────────────────────────
//  Orders entities
// ──────────────────────────────────────────────

/// <summary>
/// EF entity for the showcase orders table in PostgreSQL.
/// </summary>
public sealed class ShowcaseOrderEntity
{
    /// <summary>Gets or sets the order identifier.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer identifier.</summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant identifier.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the order status.</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Gets or sets the total amount in cents.</summary>
    public long TotalInCents { get; set; }

    /// <summary>Gets or sets the shipping address.</summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the order was placed.</summary>
    public DateTime PlacedAtUtc { get; set; }

    /// <summary>Gets or sets the timestamp when the order was last updated.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Gets or sets the cancellation reason.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Gets or sets the order line items.</summary>
    public List<ShowcaseOrderLineItemEntity> Items { get; set; } = [];
}

/// <summary>
/// EF entity for order line items in PostgreSQL.
/// </summary>
public sealed class ShowcaseOrderLineItemEntity
{
    /// <summary>Gets or sets the auto-generated row identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the parent order identifier.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Gets or sets the product identifier.</summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>Gets or sets the product name at time of order.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit price in cents at time of order.</summary>
    public long UnitPriceInCents { get; set; }
}

// ──────────────────────────────────────────────
//  Inventory entities
// ──────────────────────────────────────────────

/// <summary>
/// EF entity for the showcase inventory table in PostgreSQL.
/// </summary>
public sealed class ShowcaseInventoryEntity
{
    /// <summary>Gets or sets the product identifier (primary key).</summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>Gets or sets the total stock on hand.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Gets or sets the quantity reserved by pending orders.</summary>
    public int QuantityReserved { get; set; }

    /// <summary>Gets or sets the warehouse location code.</summary>
    public string WarehouseCode { get; set; } = "WH-01";

    /// <summary>Gets or sets the last stock update timestamp.</summary>
    public DateTime LastUpdatedAtUtc { get; set; }
}

// ──────────────────────────────────────────────
//  Shipping entities
// ──────────────────────────────────────────────

/// <summary>
/// EF entity for the showcase shipments table in PostgreSQL.
/// </summary>
public sealed class ShowcaseShipmentEntity
{
    /// <summary>Gets or sets the shipment identifier.</summary>
    public string ShipmentId { get; set; } = string.Empty;

    /// <summary>Gets or sets the order identifier.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination address.</summary>
    public string DestinationAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the carrier name.</summary>
    public string Carrier { get; set; } = "Showcase Express";

    /// <summary>Gets or sets the carrier tracking number.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the shipment status.</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Gets or sets the estimated delivery timestamp.</summary>
    public DateTime? EstimatedDeliveryUtc { get; set; }

    /// <summary>Gets or sets the actual delivery timestamp.</summary>
    public DateTime? DeliveredAtUtc { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }
}
