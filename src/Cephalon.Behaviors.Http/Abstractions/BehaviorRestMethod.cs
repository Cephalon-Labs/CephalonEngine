using System.Text.Json.Serialization;

namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Defines the candidate REST verbs supported by behavior-authored REST profile metadata.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BehaviorRestMethod>))]
public enum BehaviorRestMethod
{
    /// <summary>
    /// No REST verb has been selected.
    /// </summary>
    [JsonStringEnumMemberName("unspecified")]
    Unspecified = 0,

    /// <summary>
    /// Indicates a candidate <c>GET</c> projection.
    /// </summary>
    [JsonStringEnumMemberName("get")]
    Get = 1,

    /// <summary>
    /// Indicates a candidate <c>POST</c> projection.
    /// </summary>
    [JsonStringEnumMemberName("post")]
    Post = 2,

    /// <summary>
    /// Indicates a candidate <c>PUT</c> projection.
    /// </summary>
    [JsonStringEnumMemberName("put")]
    Put = 3,

    /// <summary>
    /// Indicates a candidate <c>PATCH</c> projection.
    /// </summary>
    [JsonStringEnumMemberName("patch")]
    Patch = 4,

    /// <summary>
    /// Indicates a candidate <c>DELETE</c> projection.
    /// </summary>
    [JsonStringEnumMemberName("delete")]
    Delete = 5
}
