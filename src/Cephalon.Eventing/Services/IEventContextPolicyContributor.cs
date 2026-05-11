namespace Cephalon.Eventing.Services;

/// <summary>
/// Allows a module to contribute event context policy metadata into the active eventing runtime pack.
/// </summary>
public interface IEventContextPolicyContributor
{
    /// <summary>
    /// Registers one or more event context policy descriptors with the supplied registry.
    /// </summary>
    /// <param name="policies">The registry that collects contributed event context policy descriptors.</param>
    void RegisterEventContextPolicies(IEventContextPolicyRegistry policies);
}
