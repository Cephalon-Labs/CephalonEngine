using System.Text.Json.Serialization;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class StranglerFigUnsupportedEndpointProblem
{
    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("routeId")]
    public string RouteId { get; init; } = string.Empty;

    [JsonPropertyName("selectedEndpoint")]
    public string? SelectedEndpoint { get; init; }

    [JsonPropertyName("selectedTarget")]
    public string SelectedTarget { get; init; } = string.Empty;

    [JsonPropertyName("handlingMode")]
    public string HandlingMode { get; init; } = string.Empty;
}
