using Microsoft.EntityFrameworkCore;

namespace Cephalon.Audit.EntityFramework.Modeling;

/// <summary>
/// Configures the shared Entity Framework Core model slice used by Cephalon durable audit history.
/// </summary>
public static class EntityFrameworkAuditHistoryModelBuilderExtensions
{
    /// <summary>
    /// Adds the Cephalon durable audit-history entity mapping to the supplied model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to extend.</param>
    /// <param name="tableName">The table name that should hold durable audit-history rows.</param>
    /// <param name="schema">The optional schema that should own the durable audit-history table.</param>
    /// <returns>The same model builder for fluent configuration.</returns>
    public static ModelBuilder ConfigureCephalonAuditHistory(
        this ModelBuilder modelBuilder,
        string tableName = "cephalon_audit_history",
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Audit-history table name is required.", nameof(tableName));
        }

        var normalizedTableName = tableName.Trim();
        var normalizedSchema = string.IsNullOrWhiteSpace(schema)
            ? null
            : schema.Trim();

        modelBuilder.Entity<EntityFrameworkAuditHistoryEntry>(entity =>
        {
            if (normalizedSchema is null)
            {
                entity.ToTable(normalizedTableName);
            }
            else
            {
                entity.ToTable(normalizedTableName, normalizedSchema);
            }

            entity.HasKey(entry => entry.Id);

            entity.Property(entry => entry.Id).HasColumnName("id").HasMaxLength(128);
            entity.Property(entry => entry.Category).HasColumnName("category").HasMaxLength(128);
            entity.Property(entry => entry.Action).HasColumnName("action").HasMaxLength(128);
            entity.Property(entry => entry.Summary).HasColumnName("summary").HasMaxLength(2048);
            entity.Property(entry => entry.SubjectType).HasColumnName("subject_type").HasMaxLength(256);
            entity.Property(entry => entry.SubjectId).HasColumnName("subject_id").HasMaxLength(256);
            entity.Property(entry => entry.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(entry => entry.PersistedAtUtc).HasColumnName("persisted_at_utc");
            entity.Property(entry => entry.ActorId).HasColumnName("actor_id").HasMaxLength(256);
            entity.Property(entry => entry.ActorDisplayName).HasColumnName("actor_display_name").HasMaxLength(512);
            entity.Property(entry => entry.ActorType).HasColumnName("actor_type").HasMaxLength(128);
            entity.Property(entry => entry.ActorIsSystem).HasColumnName("actor_is_system");
            entity.Property(entry => entry.Outcome).HasColumnName("outcome").HasMaxLength(64);
            entity.Property(entry => entry.TenantId).HasColumnName("tenant_id").HasMaxLength(256);
            entity.Property(entry => entry.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            entity.Property(entry => entry.ChangesJson).HasColumnName("changes_json");
            entity.Property(entry => entry.TagsJson).HasColumnName("tags_json");
            entity.Property(entry => entry.MetadataJson).HasColumnName("metadata_json");

            entity.HasIndex(entry => entry.OccurredAtUtc);
            entity.HasIndex(entry => entry.PersistedAtUtc);
            entity.HasIndex(entry => entry.Category);
            entity.HasIndex(entry => entry.SubjectType);
            entity.HasIndex(entry => entry.SubjectId);
            entity.HasIndex(entry => entry.TenantId);
            entity.HasIndex(entry => entry.CorrelationId);
        });

        return modelBuilder;
    }
}
