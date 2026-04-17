using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes how a REST endpoint override rule applies its explicit binding descriptors.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RestEndpointOverrideBindingMode>))]
public enum RestEndpointOverrideBindingMode
{
    /// <summary>
    /// No explicit binding-override mode has been selected.
    /// </summary>
    [JsonStringEnumMemberName("unspecified")]
    Unspecified = 0,

    /// <summary>
    /// Replaces the candidate's explicit binding plan with the configured descriptors.
    /// </summary>
    [JsonStringEnumMemberName("replace-explicit")]
    ReplaceExplicit = 1,

    /// <summary>
    /// Merges configured binding descriptors into the candidate's explicit binding plan by
    /// property name and can also remove selected explicit bindings.
    /// </summary>
    [JsonStringEnumMemberName("merge-explicit")]
    MergeExplicit = 2
}
