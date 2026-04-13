namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Identifies which side of a strangler-fig migration boundary currently owns traffic.
/// </summary>
public enum StranglerFigTarget
{
    /// <summary>
    /// Routes traffic to the legacy boundary.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// Routes traffic to the modern Cephalon boundary.
    /// </summary>
    Modern = 1
}
