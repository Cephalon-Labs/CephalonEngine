using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Modeling;

/// <summary>
/// Declares the write-side Entity Framework Core surface required by the Cephalon outbox implementation.
/// </summary>
public interface IEntityFrameworkOutboxContext
{
    /// <summary>
    /// Gets the outbox rows staged by the current write-side <see cref="DbContext" />.
    /// </summary>
    DbSet<EntityFrameworkOutboxEntry> OutboxMessages { get; }
}
