namespace Cephalon.Abstractions.Data;

/// <summary>
/// Contributes one or more inbox descriptors to the active runtime.
/// </summary>
public interface IInboxContributor
{
    /// <summary>
    /// Registers one or more inbox descriptors with the supplied registry.
    /// </summary>
    /// <param name="inboxes">The registry that collects contributed inbox descriptors.</param>
    void RegisterInboxes(IInboxRegistry inboxes);
}
