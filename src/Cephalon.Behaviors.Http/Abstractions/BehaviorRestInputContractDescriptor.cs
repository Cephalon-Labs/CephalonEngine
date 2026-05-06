namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Describes the behavior input shape paired with a metadata-only REST profile.
/// </summary>
/// <param name="InputType">The concrete behavior input type.</param>
/// <param name="IsScalar">
/// Indicates whether the input is a scalar value that cannot host property-level REST bindings.
/// </param>
/// <param name="Properties">
/// The public readable input properties available to explicit REST profile bindings.
/// </param>
public sealed record BehaviorRestInputContractDescriptor(
    Type InputType,
    bool IsScalar,
    IReadOnlyList<BehaviorRestInputPropertyDescriptor>? Properties = null);
