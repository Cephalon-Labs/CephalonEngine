using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Configuration;

/// <summary>
/// Describes the host-owned options for the Entity Framework Core data companion pack.
/// </summary>
public sealed class EntityFrameworkDataOptions
{
    /// <summary>
    /// Gets the canonical provider identifier emitted by the pack.
    /// </summary>
    public const string ProviderId = "entity-framework";

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkDataOptions" /> class.
    /// </summary>
    /// <param name="readDbContextType">The read-side <see cref="DbContext" /> type.</param>
    /// <param name="writeDbContextType">The write-side <see cref="DbContext" /> type.</param>
    public EntityFrameworkDataOptions(
        Type readDbContextType,
        Type writeDbContextType)
    {
        ReadDbContextType = ValidateDbContextType(readDbContextType, nameof(readDbContextType));
        WriteDbContextType = ValidateDbContextType(writeDbContextType, nameof(writeDbContextType));
    }

    /// <summary>
    /// Gets the read-side <see cref="DbContext" /> type.
    /// </summary>
    public Type ReadDbContextType { get; }

    /// <summary>
    /// Gets the write-side <see cref="DbContext" /> type.
    /// </summary>
    public Type WriteDbContextType { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the pack should publish the provider capability.
    /// </summary>
    public bool RegisterProviderCapability { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should publish read/write <see cref="DbContext" /> role capabilities.
    /// </summary>
    public bool RegisterDbContextCapabilities { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should register the Entity Framework-backed inbox implementation.
    /// </summary>
    public bool RegisterInbox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pack should register the Entity Framework-backed outbox implementation.
    /// </summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pack should enable official <c>Sfid.EntityFramework</c> conventions and key generation.
    /// </summary>
    public bool EnableSfidIdentifiers { get; set; }

    /// <summary>
    /// Gets a value indicating whether distinct read and write <see cref="DbContext" /> types were selected.
    /// </summary>
    public bool UsesReadWriteSplit => ReadDbContextType != WriteDbContextType;

    private static Type ValidateDbContextType(
        Type candidate,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(candidate, parameterName);

        if (!typeof(DbContext).IsAssignableFrom(candidate))
        {
            throw new ArgumentException(
                $"The supplied type '{candidate.FullName}' must derive from '{typeof(DbContext).FullName}'.",
                parameterName);
        }

        return candidate;
    }
}
