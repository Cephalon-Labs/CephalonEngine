namespace Cephalon.Audit.Conventions;

/// <summary>
/// Defines shared metadata keys used by the Cephalon audit companion pack.
/// </summary>
public static class AuditMetadataKeys
{
    /// <summary>
    /// The metadata key that stores the audit category.
    /// </summary>
    public const string Category = "audit.category";

    /// <summary>
    /// The metadata key that stores the audit action identifier.
    /// </summary>
    public const string Action = "audit.action";

    /// <summary>
    /// The metadata key that stores the audited subject type.
    /// </summary>
    public const string SubjectType = "audit.subjectType";

    /// <summary>
    /// The metadata key that stores the audited subject identifier.
    /// </summary>
    public const string SubjectId = "audit.subjectId";

    /// <summary>
    /// The metadata key that stores the tenant identifier associated with an audit entry.
    /// </summary>
    public const string TenantId = "audit.tenantId";

    /// <summary>
    /// The metadata key that stores the correlation identifier associated with an audit entry.
    /// </summary>
    public const string CorrelationId = "audit.correlationId";

    /// <summary>
    /// The metadata key that stores the actor identifier associated with an audit entry.
    /// </summary>
    public const string ActorId = "audit.actorId";
}
