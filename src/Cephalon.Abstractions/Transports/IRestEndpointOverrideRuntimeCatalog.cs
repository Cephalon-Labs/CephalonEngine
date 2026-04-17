namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Exposes the REST endpoint override rules visible to the current runtime.
/// </summary>
/// <remarks>
/// This surface complements <see cref="IRestEndpointCandidateRuntimeCatalog" /> by publishing the
/// configured host-level override rules that can rewrite descriptor-backed module-owned REST
/// candidates that participate in host governance, including shorthand candidates and explicit
/// module-DSL route groups that opted in, before precedence resolution selects the final public
/// REST surface.
/// </remarks>
public interface IRestEndpointOverrideRuntimeCatalog
{
    /// <summary>
    /// Gets all REST endpoint override rules visible to the current runtime.
    /// </summary>
    IReadOnlyList<RestEndpointOverrideDescriptor> OverrideRules { get; }

    /// <summary>
    /// Gets one REST endpoint override rule by its stable identifier.
    /// </summary>
    /// <param name="overrideId">The override identifier to resolve.</param>
    /// <returns>The matching override descriptor, or <see langword="null" /> when it is not present.</returns>
    RestEndpointOverrideDescriptor? GetById(string overrideId);

    /// <summary>
    /// Gets all REST endpoint override rules that target the requested source module identifier.
    /// </summary>
    /// <param name="sourceModuleId">The stable source-module identifier to filter by.</param>
    /// <returns>The matching override descriptors, or an empty list when no rules exist.</returns>
    IReadOnlyList<RestEndpointOverrideDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all REST endpoint override rules that target the requested behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier to filter by.</param>
    /// <returns>The matching override descriptors, or an empty list when no rules exist.</returns>
    IReadOnlyList<RestEndpointOverrideDescriptor> GetByBehaviorId(string behaviorId);
}
