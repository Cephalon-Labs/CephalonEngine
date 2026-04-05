using Cephalon.Abstractions.Audit;

namespace Cephalon.Audit.Services;

/// <summary>
/// Exposes the audit actor currently associated with the ambient runtime scope when one is known.
/// </summary>
public interface IAuditActorAccessor
{
    /// <summary>
    /// Gets the audit actor currently associated with the ambient runtime scope when one is known.
    /// </summary>
    AuditActor? Current { get; }
}
