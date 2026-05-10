namespace Cephalon.Abstractions.Data;

/// <summary>
/// Identifies a stable position in a remediation command journal replay stream.
/// </summary>
/// <param name="ObservedAtUtc">The UTC observation timestamp of the last replayed command record.</param>
/// <param name="CommandId">The command identifier of the last replayed command record.</param>
public sealed record EventDispatchRemediationCommandReplayCursor(
    DateTimeOffset ObservedAtUtc,
    string CommandId);
