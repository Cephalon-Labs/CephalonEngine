namespace Cephalon.Eventing.Services;

/// <summary>
/// Collects event context policy descriptors contributed by modules or hosts.
/// </summary>
public interface IEventContextPolicyRegistry
{
    /// <summary>
    /// Adds an event context policy descriptor to the registry.
    /// </summary>
    /// <param name="policy">The context policy descriptor to add.</param>
    void Add(EventContextPolicyDescriptor policy);
}
