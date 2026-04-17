using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Identifies which part of the HTTP request populates one resolved REST endpoint input binding.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RestEndpointBindingSource>))]
public enum RestEndpointBindingSource
{
    /// <summary>
    /// No explicit source has been selected.
    /// </summary>
    [JsonStringEnumMemberName("unspecified")]
    Unspecified = 0,

    /// <summary>
    /// Reads the value from a route placeholder such as <c>{orderId}</c>.
    /// </summary>
    [JsonStringEnumMemberName("route")]
    Route = 1,

    /// <summary>
    /// Reads the value from the query string.
    /// </summary>
    [JsonStringEnumMemberName("query")]
    Query = 2,

    /// <summary>
    /// Reads the value from an HTTP header.
    /// </summary>
    [JsonStringEnumMemberName("header")]
    Header = 3,

    /// <summary>
    /// Reads the value from the JSON request body.
    /// </summary>
    [JsonStringEnumMemberName("body")]
    Body = 4
}
