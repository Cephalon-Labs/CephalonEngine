namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointBindingSource"/>.
/// </summary>
public static class RestEndpointBindingSourceExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization and REST governance config for the binding source.
    /// </summary>
    /// <param name="source">The binding source.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointBindingSource source)
    {
        return source switch
        {
            RestEndpointBindingSource.Unspecified => "unspecified",
            RestEndpointBindingSource.Route => "route",
            RestEndpointBindingSource.Query => "query",
            RestEndpointBindingSource.Header => "header",
            RestEndpointBindingSource.Body => "body",
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "A supported REST endpoint binding source is required.")
        };
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and REST governance config into a binding source.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="source">The parsed binding source when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported binding source; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointBindingSource source)
    {
        switch (value?.Trim())
        {
            case "unspecified":
                source = RestEndpointBindingSource.Unspecified;
                return true;
            case "route":
                source = RestEndpointBindingSource.Route;
                return true;
            case "query":
                source = RestEndpointBindingSource.Query;
                return true;
            case "header":
                source = RestEndpointBindingSource.Header;
                return true;
            case "body":
                source = RestEndpointBindingSource.Body;
                return true;
            default:
                source = default;
                return false;
        }
    }
}
