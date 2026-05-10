using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Modeling;

/// <summary>
/// Declares the write-side Entity Framework Core surface required by the durable event-dispatch remediation command journal.
/// </summary>
public interface IEntityFrameworkEventDispatchRemediationCommandJournalContext
{
    /// <summary>
    /// Gets the durable remediation command journal rows owned by the current write-side <see cref="DbContext" />.
    /// </summary>
    DbSet<EntityFrameworkEventDispatchRemediationCommandEntry> EventDispatchRemediationCommandJournalEntries { get; }
}
