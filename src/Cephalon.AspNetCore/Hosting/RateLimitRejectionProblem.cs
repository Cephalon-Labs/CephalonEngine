using System.Text.Json.Serialization;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class RateLimitRejectionProblem
{
    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;

    [JsonPropertyName("retryAfterSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RetryAfterSeconds { get; init; }
}
