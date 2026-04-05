using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Modeling;

/// <summary>
/// Declares the write-side Entity Framework Core surface required by the Cephalon inbox implementation.
/// </summary>
public interface IEntityFrameworkInboxContext
{
    /// <summary>
    /// Gets the processed-message rows tracked by the current write-side <see cref="DbContext" />.
    /// </summary>
    DbSet<EntityFrameworkInboxEntry> InboxMessages { get; }
}
