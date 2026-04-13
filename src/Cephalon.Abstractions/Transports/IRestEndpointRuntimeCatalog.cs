namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Exposes the resolved public REST endpoints visible to the current runtime.
/// </summary>
/// <remarks>
/// This surface is runtime-facing rather than app-model-facing because it reflects the effective
/// REST endpoints published by the active host adapter after route prefixes, API version defaults,
/// and endpoint materialization have been resolved.
/// </remarks>
public interface IRestEndpointRuntimeCatalog
{
    /// <summary>
    /// Gets all resolved public REST endpoints visible to the current runtime.
    /// </summary>
    IReadOnlyList<RestEndpointRuntimeDescriptor> Endpoints { get; }

    /// <summary>
    /// Gets one resolved public REST endpoint by its stable identifier.
    /// </summary>
    /// <param name="endpointId">The endpoint identifier to resolve.</param>
    /// <returns>The matching endpoint descriptor, or <see langword="null" /> when it is not active.</returns>
    RestEndpointRuntimeDescriptor? GetById(string endpointId);

    /// <summary>
    /// Gets all resolved public REST endpoints owned by the requested source module.
    /// </summary>
    /// <param name="sourceModuleId">The stable source-module identifier to filter by.</param>
    /// <returns>The matching endpoint descriptors, or an empty list when the module is not active.</returns>
    IReadOnlyList<RestEndpointRuntimeDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all resolved public REST endpoints that dispatch through the requested behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier to filter by.</param>
    /// <returns>The matching endpoint descriptors, or an empty list when the behavior is not exposed publicly.</returns>
    IReadOnlyList<RestEndpointRuntimeDescriptor> GetByBehaviorId(string behaviorId);
}
