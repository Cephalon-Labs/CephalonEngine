using System.Text.Json.Serialization;

namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Identifies the HTTP request source that should populate one behavior input property for a
/// module-owned REST projection.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BehaviorRestBindingSource>))]
public enum BehaviorRestBindingSource
{
    /// <summary>
    /// No explicit source has been selected.
    /// </summary>
    [JsonStringEnumMemberName("unspecified")]
    Unspecified = 0,

    /// <summary>
    /// Reads the value from a route placeholder such as <c>{cartId}</c>.
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
