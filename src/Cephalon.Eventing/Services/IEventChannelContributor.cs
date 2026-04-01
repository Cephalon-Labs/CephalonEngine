namespace Cephalon.Eventing.Services;

/// <summary>
/// Allows a module to contribute event channels into the active eventing runtime pack.
/// </summary>
public interface IEventChannelContributor
{
    /// <summary>
    /// Registers one or more event channel descriptors with the supplied registry.
    /// </summary>
    /// <param name="channels">The registry that collects contributed channel descriptors.</param>
    void RegisterChannels(IEventChannelRegistry channels);
}
