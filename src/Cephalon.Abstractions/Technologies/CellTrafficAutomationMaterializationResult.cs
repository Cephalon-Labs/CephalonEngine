namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one materialization result for a cell traffic automation answer.
/// </summary>
public class CellTrafficAutomationMaterializationResult
{
    /// <summary>
    /// Creates a materialization result.
    /// </summary>
    /// <param name="state">
    /// The stable materialization state, such as <c>applied</c> or <c>failed</c>.
    /// </param>
    /// <param name="observedAtUtc">The UTC timestamp when the result was observed.</param>
    /// <param name="error">The operator-facing error summary when the materialization failed.</param>
    /// <param name="metadata">Optional runtime-facing metadata captured alongside the result.</param>
    public CellTrafficAutomationMaterializationResult(
        string state,
        DateTimeOffset observedAtUtc,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Materialization state is required.", nameof(state));
        }

        State = NormalizeState(state);
        ObservedAtUtc = observedAtUtc;
        Error = string.IsNullOrWhiteSpace(error)
            ? null
            : error.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable materialization state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the UTC timestamp when the result was observed.
    /// </summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>
    /// Gets the operator-facing error summary when the materialization failed.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets optional runtime-facing metadata captured alongside the result.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeState(string state)
    {
        var normalized = state.Trim().ToLowerInvariant();
        return normalized switch
        {
            CellTrafficAutomationMaterializationStates.Pending => CellTrafficAutomationMaterializationStates.Pending,
            CellTrafficAutomationMaterializationStates.Applied => CellTrafficAutomationMaterializationStates.Applied,
            CellTrafficAutomationMaterializationStates.Partial => CellTrafficAutomationMaterializationStates.Partial,
            CellTrafficAutomationMaterializationStates.Failed => CellTrafficAutomationMaterializationStates.Failed,
            CellTrafficAutomationMaterializationStates.Unavailable => CellTrafficAutomationMaterializationStates.Unavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Materialization state is not supported.")
        };
    }
}
