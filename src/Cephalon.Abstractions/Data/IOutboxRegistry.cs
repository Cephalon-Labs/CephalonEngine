namespace Cephalon.Abstractions.Data;

/// <summary>
/// Receives outbox descriptors contributed by active modules or packages.
/// </summary>
public interface IOutboxRegistry
{
    /// <summary>
    /// Adds an outbox to the current runtime composition.
    /// </summary>
    /// <param name="outbox">The outbox descriptor to register.</param>
    void Add(OutboxDescriptor outbox);
}
