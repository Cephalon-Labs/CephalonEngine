namespace Cephalon.Abstractions.Resilience;

/// <summary>
/// Exposes the active behavior-execution resilience policies visible to the current runtime.
/// </summary>
/// <remarks>
/// This runtime-facing surface reports what the behavior dispatch pipeline actually enforces after
/// defaults and implementation limits have been applied. It complements the requested contract
/// projected through <c>AppProfile.Resilience</c>.
/// </remarks>
public interface IBehaviorResilienceRuntimeCatalog
{
    /// <summary>
    /// Gets all behavior-execution resilience policies visible to the current runtime.
    /// </summary>
    IReadOnlyList<BehaviorResilienceRuntimeDescriptor> Policies { get; }

    /// <summary>
    /// Gets one behavior-execution resilience policy by its stable identifier.
    /// </summary>
    /// <param name="policyId">The stable policy identifier to resolve.</param>
    /// <returns>The matching policy descriptor, or <see langword="null" /> when it is not active.</returns>
    BehaviorResilienceRuntimeDescriptor? GetById(string policyId);

    /// <summary>
    /// Resolves the effective behavior-execution resilience policy for one behavior and optional transport.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier to resolve.</param>
    /// <param name="transportId">The stable transport identifier when one is known.</param>
    /// <returns>
    /// The matched policy descriptor, including explicit disable overrides when one suppresses the default policy;
    /// otherwise <see langword="null" /> when no behavior-execution policy applies.
    /// </returns>
    BehaviorResilienceRuntimeDescriptor? Resolve(string behaviorId, string? transportId = null);
}
