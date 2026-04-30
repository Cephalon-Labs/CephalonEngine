using System.Text.Json.Serialization;
using Cephalon.Abstractions.Behaviors;

namespace Cephalon.AspNetCore.Transports.Rest;

/// <summary>
/// Represents the optional Cephalon REST success envelope projected by the ASP.NET Core adapter.
/// </summary>
/// <typeparam name="TModel">The payload type carried by the response.</typeparam>
public class ResultModel<TModel>
{
    private int statusCode = 200;
    private string? type;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultModel{TModel}"/> class.
    /// </summary>
    public ResultModel()
    {
    }

    /// <summary>
    /// Gets or sets the optional problem type URI associated with the response.
    /// </summary>
    /// <remarks>
    /// Success envelopes omit this value by default. Error envelopes derive the RFC problem type from
    /// <see cref="StatusCode"/> unless a host or mapper supplies a more specific URI.
    /// </remarks>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type
    {
        get => type ?? (Success ? null : ResultModelProblemTypes.Resolve(StatusCode));
        set => type = string.IsNullOrWhiteSpace(value) ? null : value;
    }

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
    [JsonPropertyName("status")]
    public int StatusCode
    {
        get => statusCode;
        set => statusCode = value;
    }

    /// <summary>
    /// Gets or sets the payload returned by the endpoint.
    /// </summary>
    [JsonPropertyName("data")]
    public TModel? Data { get; set; }

    /// <summary>
    /// Gets or sets the structured error details when the response is not successful.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<ResultModelErrorDetail>? Errors { get; set; }
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
        StatusCode = 500;
        Data = null;
    }
}

/// <summary>
/// Represents structured error details inside a <see cref="ResultModel{TModel}"/>.
/// </summary>
public sealed class ResultModelErrorDetail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultModelErrorDetail"/> class.
    /// </summary>
    public ResultModelErrorDetail()
    {
    }

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

internal static class ResultModelProblemTypes
{
    private const string Rfc9110SectionPrefix = "https://tools.ietf.org/html/rfc9110#section-";
    private const string Rfc6585SectionPrefix = "https://tools.ietf.org/html/rfc6585#section-";
    private const string Rfc7725SectionPrefix = "https://tools.ietf.org/html/rfc7725#section-";

    public static string? Resolve(int statusCode)
        => statusCode switch
        {
            400 => Rfc9110SectionPrefix + "15.5.1",
            401 => Rfc9110SectionPrefix + "15.5.2",
            402 => Rfc9110SectionPrefix + "15.5.3",
            403 => Rfc9110SectionPrefix + "15.5.4",
            404 => Rfc9110SectionPrefix + "15.5.5",
            405 => Rfc9110SectionPrefix + "15.5.6",
            406 => Rfc9110SectionPrefix + "15.5.7",
            407 => Rfc9110SectionPrefix + "15.5.8",
            408 => Rfc9110SectionPrefix + "15.5.9",
            409 => Rfc9110SectionPrefix + "15.5.10",
            410 => Rfc9110SectionPrefix + "15.5.11",
            411 => Rfc9110SectionPrefix + "15.5.12",
            412 => Rfc9110SectionPrefix + "15.5.13",
            413 => Rfc9110SectionPrefix + "15.5.14",
            414 => Rfc9110SectionPrefix + "15.5.15",
            415 => Rfc9110SectionPrefix + "15.5.16",
            416 => Rfc9110SectionPrefix + "15.5.17",
            417 => Rfc9110SectionPrefix + "15.5.18",
            418 => Rfc9110SectionPrefix + "15.5.19",
            421 => Rfc9110SectionPrefix + "15.5.20",
            422 => Rfc9110SectionPrefix + "15.5.21",
            425 => "https://tools.ietf.org/html/rfc8470#section-5.2",
            426 => Rfc9110SectionPrefix + "15.5.22",
            428 => Rfc6585SectionPrefix + "3",
            429 => Rfc6585SectionPrefix + "4",
            431 => Rfc6585SectionPrefix + "5",
            451 => Rfc7725SectionPrefix + "3",
            500 => Rfc9110SectionPrefix + "15.6.1",
            501 => Rfc9110SectionPrefix + "15.6.2",
            502 => Rfc9110SectionPrefix + "15.6.3",
            503 => Rfc9110SectionPrefix + "15.6.4",
            504 => Rfc9110SectionPrefix + "15.6.5",
            505 => Rfc9110SectionPrefix + "15.6.6",
            _ => null
        };
}
