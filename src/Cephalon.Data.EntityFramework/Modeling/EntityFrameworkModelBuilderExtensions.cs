using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Modeling;

/// <summary>
/// Configures shared Cephalon Entity Framework Core model slices.
/// </summary>
public static class EntityFrameworkModelBuilderExtensions
{
    /// <summary>
    /// Adds the Cephalon inbox entity mapping to the supplied model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to extend.</param>
    /// <param name="tableName">The table name that should hold processed inbox rows.</param>
    /// <returns>The same model builder for fluent configuration.</returns>
    public static ModelBuilder ConfigureCephalonInbox(
        this ModelBuilder modelBuilder,
        string tableName = "cephalon_inbox_messages")
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Inbox table name is required.", nameof(tableName));
        }

        var normalizedTableName = tableName.Trim();
        modelBuilder.Entity<EntityFrameworkInboxEntry>(entity =>
        {
            entity.ToTable(normalizedTableName);
            entity.HasKey(entry => entry.Id);

            entity.Property(entry => entry.Id).HasColumnName("id");
            entity.Property(entry => entry.ChannelId).HasColumnName("channel_id");
            entity.Property(entry => entry.MessageType).HasColumnName("message_type");
            entity.Property(entry => entry.Payload).HasColumnName("payload");
            entity.Property(entry => entry.ReceivedAtUtc).HasColumnName("received_at_utc");
            entity.Property(entry => entry.ProcessedAtUtc).HasColumnName("processed_at_utc");
            entity.Property(entry => entry.ContentType).HasColumnName("content_type");
            entity.Property(entry => entry.CorrelationId).HasColumnName("correlation_id");
            entity.Property(entry => entry.TenantId).HasColumnName("tenant_id");
            entity.Property(entry => entry.HeadersJson).HasColumnName("headers_json");
            entity.Property(entry => entry.MetadataJson).HasColumnName("metadata_json");

            entity.HasIndex(entry => entry.ChannelId);
            entity.HasIndex(entry => entry.ProcessedAtUtc);
        });

        return modelBuilder;
    }

    /// <summary>
    /// Adds the Cephalon outbox entity mapping to the supplied model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to extend.</param>
    /// <param name="tableName">The table name that should hold durable outbox rows.</param>
    /// <returns>The same model builder for fluent configuration.</returns>
    public static ModelBuilder ConfigureCephalonOutbox(
        this ModelBuilder modelBuilder,
        string tableName = "cephalon_outbox_messages")
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Outbox table name is required.", nameof(tableName));
        }

        var normalizedTableName = tableName.Trim();
        modelBuilder.Entity<EntityFrameworkOutboxEntry>(entity =>
        {
            entity.ToTable(normalizedTableName);
            entity.HasKey(entry => entry.Id);

            entity.Property(entry => entry.Id).HasColumnName("id");
            entity.Property(entry => entry.ChannelId).HasColumnName("channel_id");
            entity.Property(entry => entry.MessageType).HasColumnName("message_type");
            entity.Property(entry => entry.Payload).HasColumnName("payload");
            entity.Property(entry => entry.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(entry => entry.ContentType).HasColumnName("content_type");
            entity.Property(entry => entry.CorrelationId).HasColumnName("correlation_id");
            entity.Property(entry => entry.TenantId).HasColumnName("tenant_id");
            entity.Property(entry => entry.HeadersJson).HasColumnName("headers_json");
            entity.Property(entry => entry.MetadataJson).HasColumnName("metadata_json");
            entity.Property(entry => entry.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(entry => entry.DispatchedAtUtc).HasColumnName("dispatched_at_utc");
            entity.Property(entry => entry.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc");
            entity.Property(entry => entry.DispatchAttemptCount).HasColumnName("dispatch_attempt_count");

            entity.HasIndex(entry => entry.DispatchedAtUtc);
            entity.HasIndex(entry => entry.NextAttemptAtUtc);
        });

        return modelBuilder;
    }
}
