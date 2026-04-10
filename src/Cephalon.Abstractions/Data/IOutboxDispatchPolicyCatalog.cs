namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the effective dispatch-execution policies visible to the current runtime for active outbox surfaces.
/// </summary>
public interface IOutboxDispatchPolicyCatalog
{
    /// <summary>
    /// Gets the effective dispatch policies visible to the current runtime.
    /// </summary>
    IReadOnlyList<OutboxDispatchPolicyDescriptor> Policies { get; }

    /// <summary>
    /// Gets the effective dispatch policy for one outbox by its stable identifier.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier to resolve.</param>
    /// <returns>The matching dispatch policy, or <see langword="null" /> when the outbox is not active.</returns>
    OutboxDispatchPolicyDescriptor? GetByOutboxId(string outboxId);
}
