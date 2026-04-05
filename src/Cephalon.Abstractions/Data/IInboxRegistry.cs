namespace Cephalon.Abstractions.Data;

/// <summary>
/// Receives inbox descriptors contributed by active modules or packages.
/// </summary>
public interface IInboxRegistry
{
    /// <summary>
    /// Adds an inbox to the current runtime composition.
    /// </summary>
    /// <param name="inbox">The inbox descriptor to register.</param>
    void Add(InboxDescriptor inbox);
}
