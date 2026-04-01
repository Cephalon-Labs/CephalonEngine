namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes how startup failures are handled.
/// </summary>
public enum StartupFailureBehavior
{
    /// <summary>
    /// Stop startup immediately and rethrow the failure.
    /// </summary>
    FailFast = 0,

    /// <summary>
    /// Capture the failure in runtime status without rethrowing it to the host.
    /// </summary>
    CaptureOnly = 1
}
