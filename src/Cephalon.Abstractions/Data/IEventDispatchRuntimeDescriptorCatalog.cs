namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the configured operator-facing durable dispatch runtimes visible to the current runtime.
/// </summary>
public interface IEventDispatchRuntimeDescriptorCatalog
{
    /// <summary>
    /// Gets the configured dispatch runtimes visible to the current runtime.
    /// </summary>
    IReadOnlyList<EventDispatchRuntimeDescriptor> Runtimes { get; }

    /// <summary>
    /// Gets one dispatch runtime by its stable identifier.
    /// </summary>
    /// <param name="dispatchRuntimeId">The stable dispatch-runtime identifier to resolve.</param>
    /// <returns>The matching dispatch-runtime descriptor, or <see langword="null" /> when none exists.</returns>
    EventDispatchRuntimeDescriptor? GetById(string dispatchRuntimeId);
}
