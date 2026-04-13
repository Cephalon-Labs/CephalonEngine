namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Defines the candidate REST verbs supported by behavior-authored REST profile metadata.
/// </summary>
public enum BehaviorRestMethod
{
    /// <summary>
    /// No REST verb has been selected.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Indicates a candidate <c>GET</c> projection.
    /// </summary>
    Get = 1,

    /// <summary>
    /// Indicates a candidate <c>POST</c> projection.
    /// </summary>
    Post = 2,

    /// <summary>
    /// Indicates a candidate <c>PUT</c> projection.
    /// </summary>
    Put = 3,

    /// <summary>
    /// Indicates a candidate <c>PATCH</c> projection.
    /// </summary>
    Patch = 4,

    /// <summary>
    /// Indicates a candidate <c>DELETE</c> projection.
    /// </summary>
    Delete = 5
}
