namespace Cephalon.Edge.KubernetesGateway.Services;

internal sealed class KubernetesGatewayTrafficCleanupSweepResult
{
    public KubernetesGatewayTrafficCleanupSweepResult(
        DateTimeOffset observedAtUtc,
        string state,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Cleanup sweep state is required.", nameof(state));
        }

        ObservedAtUtc = observedAtUtc;
        State = state.Trim().ToLowerInvariant();
        Error = string.IsNullOrWhiteSpace(error)
            ? null
            : error.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public DateTimeOffset ObservedAtUtc { get; }

    public string State { get; }

    public string? Error { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
