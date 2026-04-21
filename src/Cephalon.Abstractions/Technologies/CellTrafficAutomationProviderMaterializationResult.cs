namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one provider-managed materialization result for a cell traffic automation answer.
/// </summary>
public sealed class CellTrafficAutomationProviderMaterializationResult : CellTrafficAutomationMaterializationResult
{
    /// <summary>
    /// Creates a provider-managed materialization result.
    /// </summary>
    /// <param name="state">
    /// The stable provider-materialization state, such as <c>applied</c> or <c>failed</c>.
    /// </param>
    /// <param name="observedAtUtc">The UTC timestamp when the result was observed.</param>
    /// <param name="error">The operator-facing error summary when the materialization failed.</param>
    /// <param name="metadata">Optional provider-facing metadata captured alongside the result.</param>
    /// <param name="conditions">Optional typed provider-materialization conditions captured alongside the result.</param>
    public CellTrafficAutomationProviderMaterializationResult(
        string state,
        DateTimeOffset observedAtUtc,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<CellTrafficAutomationMaterializationConditionDescriptor>? conditions = null)
        : base(state, observedAtUtc, error, metadata, conditions)
    {
    }
}
