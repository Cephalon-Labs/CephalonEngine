using Microsoft.EntityFrameworkCore;

namespace Cephalon.Audit.EntityFramework.Configuration;

/// <summary>
/// Describes host-owned options for the Entity Framework durable audit-history provider.
/// </summary>
public sealed class EntityFrameworkAuditHistoryOptions
{
    /// <summary>
    /// Gets the canonical provider identifier emitted by this pack.
    /// </summary>
    public const string ProviderId = "entity-framework";

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkAuditHistoryOptions" /> class.
    /// </summary>
    /// <param name="dbContextType">The <see cref="DbContext" /> type that persists durable audit-history rows.</param>
    public EntityFrameworkAuditHistoryOptions(Type dbContextType)
    {
        ArgumentNullException.ThrowIfNull(dbContextType);

        if (!typeof(DbContext).IsAssignableFrom(dbContextType))
        {
            throw new ArgumentException(
                $"The supplied type '{dbContextType.FullName}' must derive from '{typeof(DbContext).FullName}'.",
                nameof(dbContextType));
        }

        DbContextType = dbContextType;
    }

    /// <summary>
    /// Gets the <see cref="DbContext" /> type that persists durable audit-history rows.
    /// </summary>
    public Type DbContextType { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this pack resolves its database role from <c>Engine:Databases</c>.
    /// </summary>
    public bool UsesEngineDatabaseTopology { get; set; }
}
