namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Describes the metadata-only REST projection preference declared by a behavior.
/// </summary>
/// <param name="BehaviorId">The stable Cephalon behavior identifier.</param>
/// <param name="Method">The candidate REST method.</param>
/// <param name="RelativePattern">The candidate route pattern relative to a future owning REST group.</param>
/// <param name="ApiVersionMajor">
/// The candidate API major version, or <see langword="null" /> when the behavior leaves version
/// selection to the owning module or host defaults.
/// </param>
/// <param name="Bindings">
/// Optional explicit input-binding hints that describe where individual input properties should be
/// sourced from when an owning module consumes the profile.
/// </param>
/// <param name="PreserveImplicitQueryFallback">
/// Indicates whether explicit profile bindings should preserve the remaining implicit query-string
/// fallback surface when an owning module consumes the profile.
/// </param>
public sealed record BehaviorRestProfileDescriptor(
    string BehaviorId,
    BehaviorRestMethod Method,
    string RelativePattern,
    int? ApiVersionMajor,
    IReadOnlyList<BehaviorRestBindingDescriptor>? Bindings = null,
    bool PreserveImplicitQueryFallback = false)
{
    /// <summary>
    /// Gets compile-time or explicitly provided input-shape metadata used to validate explicit
    /// REST profile bindings without inspecting behavior/input types at runtime.
    /// </summary>
    public BehaviorRestInputContractDescriptor? InputContract { get; init; }
}
