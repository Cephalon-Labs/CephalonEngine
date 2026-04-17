using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes why a REST endpoint candidate was suppressed by authoring-policy enforcement.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RestEndpointAuthoringPolicySuppressionKind>))]
public enum RestEndpointAuthoringPolicySuppressionKind
{
    /// <summary>
    /// The candidate was not classified with an authoring-policy suppression kind.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The candidate authoring style is explicitly disallowed by the behavior-level authoring policy.
    /// </summary>
    [JsonStringEnumMemberName("disallowed-authoring-style")]
    DisallowedAuthoringStyle = 1,

    /// <summary>
    /// The candidate authoring style is outside the explicitly allowed authoring-style set.
    /// </summary>
    [JsonStringEnumMemberName("not-allowed-authoring-style")]
    NotAllowedAuthoringStyle = 2,

    /// <summary>
    /// The candidate was suppressed because a preferred authoring style is present for the behavior boundary.
    /// </summary>
    [JsonStringEnumMemberName("preferred-authoring-style-selected")]
    PreferredAuthoringStyleSelected = 3
}
