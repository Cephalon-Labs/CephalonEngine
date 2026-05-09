using System.Text.Json.Nodes;

namespace Cephalon.AspNetCore.Health;

internal sealed class HealthResponsePayload
{
    public string Status { get; init; } = string.Empty;

    public double TotalDurationMs { get; init; }

    public IReadOnlyDictionary<string, HealthResponseEntryPayload> Entries { get; init; } =
        new Dictionary<string, HealthResponseEntryPayload>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class HealthResponseEntryPayload
{
    public string Status { get; init; } = string.Empty;

    public string? Description { get; init; }

    public double DurationMs { get; init; }

    public IReadOnlyDictionary<string, JsonNode?> Data { get; init; } =
        new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class HealthResponseDependencyPayload
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool Required { get; init; }

    public string? Source { get; init; }
}
