namespace Cephalon.Abstractions.Data;

/// <summary>
/// Contributes one or more outbox descriptors to the active runtime.
/// </summary>
public interface IOutboxContributor
{
    /// <summary>
    /// Registers one or more outbox descriptors with the supplied registry.
    /// </summary>
    /// <param name="outboxes">The registry that collects contributed outbox descriptors.</param>
    void RegisterOutboxes(IOutboxRegistry outboxes);
}
