using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes a resolved REST request-binding fallback mode when the runtime preserves behavior beyond
/// the explicit binding plan.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RestEndpointBindingFallbackMode>))]
public enum RestEndpointBindingFallbackMode
{
    /// <summary>
    /// Preserves the remaining implicit fallback surface from the source shorthand projection.
    /// </summary>
    [JsonStringEnumMemberName("preserve-source-implicit-fallback")]
    PreserveSourceImplicitFallback = 1
}
