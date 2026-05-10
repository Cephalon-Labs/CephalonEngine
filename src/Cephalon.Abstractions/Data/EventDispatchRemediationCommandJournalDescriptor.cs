namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the storage and audit posture of an event-dispatch remediation command journal.
/// </summary>
/// <param name="JournalId">The stable journal identifier reported through runtime metadata.</param>
/// <param name="Provider">The provider or companion package that owns the journal implementation.</param>
/// <param name="Storage">The storage medium used by the journal, such as memory or an Entity Framework table.</param>
/// <param name="Durability">The durability class exposed by the journal.</param>
/// <param name="Scope">The runtime scope where command idempotency and audit reads are valid.</param>
/// <param name="CrossNodeCommandAudit">A value indicating whether command audit reads survive process and node boundaries.</param>
/// <param name="DurableReplayCursor">A value indicating whether the journal exposes a durable replay cursor contract.</param>
public sealed record EventDispatchRemediationCommandJournalDescriptor(
    string JournalId,
    string Provider,
    string Storage,
    string Durability,
    string Scope,
    bool CrossNodeCommandAudit,
    bool DurableReplayCursor);
