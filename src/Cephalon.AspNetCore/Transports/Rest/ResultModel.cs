using System.Text.Json.Serialization;
using Cephalon.Abstractions.Behaviors;

namespace Cephalon.AspNetCore.Transports.Rest;

/// <summary>
/// Represents the optional Cephalon REST success envelope projected by the ASP.NET Core adapter.
/// </summary>
/// <typeparam name="TModel">The payload type carried by the response.</typeparam>
public class ResultModel<TModel>
{
    /// <summary>
    /// Gets or sets the short response title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Ok";

    /// <summary>
    /// Gets or sets the human-readable response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "Successful";

    /// <summary>
    /// Gets or sets a value indicating whether the response is successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the effective HTTP status code associated with the response.
    /// </summary>
    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Gets or sets the payload returned by the endpoint.
    /// </summary>
    [JsonPropertyName("data")]
    public TModel? Data { get; set; }

    /// <summary>
    /// Gets or sets the structured error details when the response is not successful.
    /// </summary>
    [JsonPropertyName("error")]
    public ResultModelErrorDetail? Error { get; set; }
}

/// <summary>
/// Represents the optional Cephalon REST error envelope projected by the ASP.NET Core adapter.
/// </summary>
public sealed class ResultModelError : ResultModel<object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultModelError"/> class.
    /// </summary>
    public ResultModelError()
    {
        Title = "Error";
        Message = "The request failed.";
        Success = false;
        Data = null;
    }
}

/// <summary>
/// Represents structured error details inside a <see cref="ResultModel{TModel}"/>.
/// </summary>
public sealed class ResultModelErrorDetail
{
    /// <summary>
    /// Gets or sets the stable error key.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error severity.
    /// </summary>
    [JsonPropertyName("severity")]
    [JsonConverter(typeof(JsonStringEnumConverter<BehaviorFaultSeverity>))]
    public BehaviorFaultSeverity Severity { get; set; } = BehaviorFaultSeverity.Error;

    /// <summary>
    /// Gets or sets additional error details when one was supplied.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
