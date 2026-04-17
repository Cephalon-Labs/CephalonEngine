namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Exposes the REST endpoint suppression rules visible to the current runtime.
/// </summary>
/// <remarks>
/// This surface complements <see cref="IRestEndpointCandidateRuntimeCatalog" /> by publishing the
/// configured host-level suppression rules that can hide descriptor-backed module-owned REST
/// candidates that participate in host governance, including shorthand candidates and explicit
/// module-DSL route groups that opted in, before precedence resolution selects the final public
/// REST surface.
/// </remarks>
public interface IRestEndpointSuppressionRuntimeCatalog
{
    /// <summary>
    /// Gets all REST endpoint suppression rules visible to the current runtime.
    /// </summary>
    IReadOnlyList<RestEndpointSuppressionDescriptor> Suppressions { get; }

    /// <summary>
    /// Gets one REST endpoint suppression rule by its stable identifier.
    /// </summary>
    /// <param name="suppressionId">The suppression identifier to resolve.</param>
    /// <returns>The matching suppression descriptor, or <see langword="null" /> when it is not present.</returns>
    RestEndpointSuppressionDescriptor? GetById(string suppressionId);

    /// <summary>
    /// Gets all REST endpoint suppression rules that target the requested source module identifier.
    /// </summary>
    /// <param name="sourceModuleId">The stable source-module identifier to filter by.</param>
    /// <returns>The matching suppression descriptors, or an empty list when no rules exist.</returns>
    IReadOnlyList<RestEndpointSuppressionDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all REST endpoint suppression rules that target the requested behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier to filter by.</param>
    /// <returns>The matching suppression descriptors, or an empty list when no rules exist.</returns>
    IReadOnlyList<RestEndpointSuppressionDescriptor> GetByBehaviorId(string behaviorId);
}
