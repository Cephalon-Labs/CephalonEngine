using Microsoft.EntityFrameworkCore;

namespace Cephalon.EventSourcing.EntityFramework;

/// <summary>
/// Applies the Cephalon event-store schema to an Entity Framework model.
/// </summary>
public static class EntityFrameworkEventSourcingConfiguration
{
    /// <summary>
    /// Configures the <c>CephalonEvents</c> table and indexes required by the Entity Framework event-store provider.
    /// </summary>
    /// <param name="modelBuilder">The model builder to extend.</param>
    public static void ConfigureCephalonEvents(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entity = modelBuilder.Entity<EntityFrameworkEventEntry>();
        entity.HasKey(static entry => entry.Id);
        entity.Property(static entry => entry.Id).ValueGeneratedOnAdd();
        entity.Property(static entry => entry.StreamId).IsRequired().HasMaxLength(500);
        entity.Property(static entry => entry.EventType).IsRequired().HasMaxLength(500);
        entity.Property(static entry => entry.Payload).IsRequired();
        entity.HasIndex(static entry => new { entry.StreamId, entry.StreamVersion }).IsUnique();
        entity.HasIndex(static entry => entry.StreamId);
        entity.HasIndex(static entry => entry.AppendedAtUtc);
    }
}
