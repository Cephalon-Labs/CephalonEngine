namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes how stop failures are handled.
/// </summary>
public enum StopFailureBehavior
{
    /// <summary>
    /// Stop shutdown immediately and rethrow the failure.
    /// </summary>
    FailFast = 0,

    /// <summary>
    /// Continue stopping remaining modules and report failures afterward.
    /// </summary>
    BestEffortContinue = 1
}
