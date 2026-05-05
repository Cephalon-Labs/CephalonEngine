namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="BehaviorRestMethod"/>.
/// </summary>
public static class BehaviorRestMethodExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization for the REST method.
    /// </summary>
    /// <param name="method">The REST method.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this BehaviorRestMethod method)
    {
        return method switch
        {
            BehaviorRestMethod.Unspecified => "unspecified",
            BehaviorRestMethod.Get => "get",
            BehaviorRestMethod.Post => "post",
            BehaviorRestMethod.Put => "put",
            BehaviorRestMethod.Patch => "patch",
            BehaviorRestMethod.Delete => "delete",
            _ => throw new ArgumentOutOfRangeException(
                nameof(method),
                method,
                "A supported behavior REST method is required.")
        };
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization into a REST method.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="method">The parsed REST method when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported REST method; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out BehaviorRestMethod method)
    {
        switch (value?.Trim())
        {
            case "unspecified":
                method = BehaviorRestMethod.Unspecified;
                return true;
            case "get":
                method = BehaviorRestMethod.Get;
                return true;
            case "post":
                method = BehaviorRestMethod.Post;
                return true;
            case "put":
                method = BehaviorRestMethod.Put;
                return true;
            case "patch":
                method = BehaviorRestMethod.Patch;
                return true;
            case "delete":
                method = BehaviorRestMethod.Delete;
                return true;
            default:
                method = default;
                return false;
        }
    }
}
