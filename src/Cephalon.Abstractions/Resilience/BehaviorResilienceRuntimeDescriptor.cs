namespace Cephalon.Abstractions.Resilience;

/// <summary>
/// Describes one effective behavior-execution resilience policy exposed by the current runtime.
/// </summary>
/// <param name="Id">The stable runtime policy identifier.</param>
/// <param name="DisplayName">The human-readable policy name.</param>
/// <param name="Description">The human-readable policy description.</param>
/// <param name="ExecutionMode">
/// The enforcement mode used by the active runtime, such as <c>behavior-dispatch-middleware</c> or <c>contract-only</c>.
/// </param>
/// <param name="Scope">The runtime scope covered by the policy, such as <c>all-behavior-executions</c>.</param>
/// <param name="Requested">The requested behavior-execution resilience contract.</param>
/// <param name="Effective">The effective behavior-execution resilience contract after runtime normalization.</param>
/// <param name="Metadata">Additional runtime-specific metadata describing the policy.</param>
public sealed record BehaviorResilienceRuntimeDescriptor(
    string Id,
    string DisplayName,
    string Description,
    string ExecutionMode,
    string Scope,
    BehaviorExecutionResilienceSelection Requested,
    BehaviorExecutionResilienceSelection Effective,
    IReadOnlyDictionary<string, string> Metadata);
