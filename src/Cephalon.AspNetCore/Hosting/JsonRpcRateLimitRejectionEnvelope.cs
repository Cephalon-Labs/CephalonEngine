using System.Text.Json.Serialization;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class JsonRpcRateLimitRejectionEnvelope
{
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; init; } = "2.0";

    [JsonPropertyName("error")]
    public JsonRpcRateLimitRejectionError Error { get; init; } = new();

    [JsonPropertyName("id")]
    public object? Id { get; init; }
}

internal sealed class JsonRpcRateLimitRejectionError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Data { get; init; }
}
