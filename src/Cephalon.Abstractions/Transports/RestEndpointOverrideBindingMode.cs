namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes how a REST endpoint override rule applies its explicit binding descriptors.
/// </summary>
public enum RestEndpointOverrideBindingMode
{
    /// <summary>
    /// No explicit binding-override mode has been selected.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Replaces the candidate's explicit binding plan with the configured descriptors.
    /// </summary>
    ReplaceExplicit = 1,

    /// <summary>
    /// Merges configured binding descriptors into the candidate's explicit binding plan by
    /// property name and can also remove selected explicit bindings.
    /// </summary>
    MergeExplicit = 2
}
