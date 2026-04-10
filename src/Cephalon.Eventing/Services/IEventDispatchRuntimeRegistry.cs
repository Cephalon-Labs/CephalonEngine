using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

/// <summary>
/// Receives operator-facing durable dispatch-runtime descriptors contributed by active eventing packs.
/// </summary>
public interface IEventDispatchRuntimeRegistry
{
    /// <summary>
    /// Adds one dispatch runtime to the current eventing technology composition.
    /// </summary>
    /// <param name="dispatchRuntime">The dispatch runtime to register.</param>
    void Add(EventDispatchRuntimeDescriptor dispatchRuntime);
}
