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
    /// Replaces the shorthand candidate's explicit binding plan with the configured descriptors.
    /// </summary>
    ReplaceExplicit = 1,

    /// <summary>
    /// Merges the configured descriptors into the shorthand candidate's explicit binding plan by property name.
    /// </summary>
    MergeExplicit = 2
}
