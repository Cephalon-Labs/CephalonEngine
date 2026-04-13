namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Identifies which part of the HTTP request populates one resolved REST endpoint input binding.
/// </summary>
public enum RestEndpointBindingSource
{
    /// <summary>
    /// No explicit source has been selected.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Reads the value from a route placeholder such as <c>{orderId}</c>.
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
