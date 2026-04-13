namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Identifies the HTTP request source that should populate one behavior input property for a
/// module-owned REST projection.
/// </summary>
public enum BehaviorRestBindingSource
{
    /// <summary>
    /// No explicit source has been selected.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Reads the value from a route placeholder such as <c>{cartId}</c>.
    /// </summary>
    Route = 1,

    /// <summary>
    /// Reads the value from the query string.
    /// </summary>
    Query = 2,

    /// <summary>
    /// Reads the value from an HTTP header.
    /// </summary>
    Header = 3,

    /// <summary>
    /// Reads the value from the JSON request body.
    /// </summary>
    Body = 4
}
