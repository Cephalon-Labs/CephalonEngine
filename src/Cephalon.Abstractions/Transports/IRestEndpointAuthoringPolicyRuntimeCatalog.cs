namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Exposes behavior-level REST authoring-policy visibility for the current runtime.
/// </summary>
/// <remarks>
/// This surface complements <see cref="IRestEndpointPublicationGroupRuntimeCatalog" /> by making
/// authoring-policy intent plus authoring-policy-specific runtime effect readable without reopening
/// grouped publication answers, while also keeping explicitly configured policies visible even when
/// no current REST endpoint candidates match the behavior boundary.
/// </remarks>
public interface IRestEndpointAuthoringPolicyRuntimeCatalog
{
    /// <summary>
    /// Gets the behavior-level REST authoring-policy answers visible to the current runtime.
    /// </summary>
    IReadOnlyList<RestEndpointAuthoringPolicyDescriptor> Policies { get; }

    /// <summary>
    /// Gets one REST authoring-policy answer by behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier to resolve.</param>
    /// <returns>The matching authoring-policy descriptor, or <see langword="null" /> when it is not present.</returns>
    RestEndpointAuthoringPolicyDescriptor? GetByBehaviorId(string behaviorId);
}
