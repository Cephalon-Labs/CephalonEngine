using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

/// <summary>
/// Contributes one or more operator-facing durable dispatch runtimes to the active eventing technology.
/// </summary>
public interface IEventDispatchRuntimeContributor
{
    /// <summary>
    /// Registers one or more dispatch-runtime descriptors owned by the contributor.
    /// </summary>
    /// <param name="dispatchRuntimes">The dispatch-runtime registry receiving contributed descriptors.</param>
    void RegisterDispatchRuntimes(IEventDispatchRuntimeRegistry dispatchRuntimes);
}
