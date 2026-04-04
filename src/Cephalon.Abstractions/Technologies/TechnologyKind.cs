namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Categorizes the role a technology profile plays in an app.
/// </summary>
public enum TechnologyKind
{
    /// <summary>
    /// Identifies an intelligence-oriented technology.
    /// </summary>
    Intelligence = 0,

    /// <summary>
    /// Identifies a messaging-oriented technology.
    /// </summary>
    Messaging = 1,

    /// <summary>
    /// Identifies a data-oriented technology.
    /// </summary>
    Data = 2,

    /// <summary>
    /// Identifies an experience-oriented technology.
    /// </summary>
    Experience = 3,

    /// <summary>
    /// Identifies a deployment-oriented technology.
    /// </summary>
    Deployment = 4,

    /// <summary>
    /// Identifies a security-oriented technology.
    /// </summary>
    Security = 5,

    /// <summary>
    /// Identifies a platform- or runtime-oriented technology.
    /// </summary>
    Platform = 6
}
