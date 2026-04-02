namespace Cephalon.Abstractions.Capabilities;

/// <summary>
/// Describes how a capability may be consumed under the active trust policy.
/// </summary>
public enum CapabilityAccess
{
    /// <summary>
    /// Indicates the capability can be used without additional trust requirements.
    /// </summary>
    Allowed = 0,

    /// <summary>
    /// Indicates the capability can be used only by trusted modules or packages.
    /// </summary>
    TrustedOnly = 1,

    /// <summary>
    /// Indicates the capability is denied.
    /// </summary>
    Denied = 2
}
