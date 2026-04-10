namespace Cephalon.Audit.EntityFramework;

/// <summary>
/// Represents one durable audit-history row persisted by the Entity Framework provider.
/// </summary>
public sealed class EntityFrameworkAuditHistoryEntry
{
    /// <summary>
    /// Gets or sets the stable audit-entry identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical audit category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical action identifier.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable audit summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical subject type associated with the entry.
    /// </summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stable subject identifier associated with the entry.
    /// </summary>
    public string? SubjectId { get; set; }

    /// <summary>
    /// Gets or sets the time at which the audited operation occurred.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the time at which the durable history row was persisted.
    /// </summary>
    public DateTimeOffset PersistedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the actor identifier associated with the entry.
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actor display name associated with the entry.
    /// </summary>
    public string ActorDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actor type associated with the entry.
    /// </summary>
    public string ActorType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the actor represents a system principal.
    /// </summary>
    public bool ActorIsSystem { get; set; }

    /// <summary>
    /// Gets or sets the serialized audit outcome.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant identifier associated with the entry.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier associated with the entry.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the serialized change set associated with the entry.
    /// </summary>
    public string ChangesJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the serialized descriptive tags associated with the entry.
    /// </summary>
    public string TagsJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the serialized audit metadata associated with the entry.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
}
