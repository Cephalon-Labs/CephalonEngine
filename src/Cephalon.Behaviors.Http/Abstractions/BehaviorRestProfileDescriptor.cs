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
public sealed record BehaviorRestProfileDescriptor(
    string BehaviorId,
    BehaviorRestMethod Method,
    string RelativePattern,
    int? ApiVersionMajor);
