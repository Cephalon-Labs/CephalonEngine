namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one provider-managed materialization result for a cell traffic automation answer.
/// </summary>
public sealed class CellTrafficAutomationProviderMaterializationResult
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
    public CellTrafficAutomationProviderMaterializationResult(
        string state,
        DateTimeOffset observedAtUtc,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Provider materialization state is required.", nameof(state));
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
    /// Gets the stable provider-materialization state.
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
    /// Gets optional provider-facing metadata captured alongside the result.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeState(string state)
    {
        var normalized = state.Trim().ToLowerInvariant();
        return normalized switch
        {
            CellTrafficAutomationProviderMaterializationStates.Pending => CellTrafficAutomationProviderMaterializationStates.Pending,
            CellTrafficAutomationProviderMaterializationStates.Applied => CellTrafficAutomationProviderMaterializationStates.Applied,
            CellTrafficAutomationProviderMaterializationStates.Failed => CellTrafficAutomationProviderMaterializationStates.Failed,
            CellTrafficAutomationProviderMaterializationStates.Unavailable => CellTrafficAutomationProviderMaterializationStates.Unavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Provider materialization state is not supported.")
        };
    }
}
