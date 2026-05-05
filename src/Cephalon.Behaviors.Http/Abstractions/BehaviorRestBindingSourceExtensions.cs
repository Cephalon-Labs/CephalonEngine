namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="BehaviorRestBindingSource"/>.
/// </summary>
public static class BehaviorRestBindingSourceExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization for the binding source.
    /// </summary>
    /// <param name="source">The binding source.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this BehaviorRestBindingSource source)
    {
        return source switch
        {
            BehaviorRestBindingSource.Unspecified => "unspecified",
            BehaviorRestBindingSource.Route => "route",
            BehaviorRestBindingSource.Query => "query",
            BehaviorRestBindingSource.Header => "header",
            BehaviorRestBindingSource.Body => "body",
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "A supported behavior REST binding source is required.")
        };
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization into a binding source.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="source">The parsed binding source when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported binding source; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out BehaviorRestBindingSource source)
    {
        switch (value?.Trim())
        {
            case "unspecified":
                source = BehaviorRestBindingSource.Unspecified;
                return true;
            case "route":
                source = BehaviorRestBindingSource.Route;
                return true;
            case "query":
                source = BehaviorRestBindingSource.Query;
                return true;
            case "header":
                source = BehaviorRestBindingSource.Header;
                return true;
            case "body":
                source = BehaviorRestBindingSource.Body;
                return true;
            default:
                source = default;
                return false;
        }
    }
}
