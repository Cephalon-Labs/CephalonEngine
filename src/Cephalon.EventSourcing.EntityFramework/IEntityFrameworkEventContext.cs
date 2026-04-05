using Microsoft.EntityFrameworkCore;

namespace Cephalon.EventSourcing.EntityFramework;

/// <summary>
/// Represents the minimum Entity Framework context contract required by the Cephalon event-store provider.
/// </summary>
public interface IEntityFrameworkEventContext
{
    /// <summary>
    /// Gets the event rows persisted by the active event-store context.
    /// </summary>
    DbSet<EntityFrameworkEventEntry> Events { get; }
}
