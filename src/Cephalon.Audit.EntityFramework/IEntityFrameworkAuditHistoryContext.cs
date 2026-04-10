using Microsoft.EntityFrameworkCore;

namespace Cephalon.Audit.EntityFramework;

/// <summary>
/// Represents a <see cref="DbContext" /> that can persist Cephalon durable audit-history rows.
/// </summary>
public interface IEntityFrameworkAuditHistoryContext
{
    /// <summary>
    /// Gets the durable audit-history rows persisted by this context.
    /// </summary>
    DbSet<EntityFrameworkAuditHistoryEntry> AuditEntries { get; }
}
