namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Describes the behavior type that owns a source-generated REST profile hint.
/// </summary>
/// <param name="Id">The behavior identifier emitted beside the generated REST profile.</param>
/// <param name="Type">The concrete behavior type that owns the REST profile.</param>
/// <remarks>
/// Source-generated assemblies register these descriptors so REST profile projection can resolve
/// behavior types without invoking generated carrier methods through reflection.
/// </remarks>
public sealed record BehaviorRestProfileBehaviorTypeDescriptor(string Id, Type Type);
