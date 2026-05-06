namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Describes one public readable behavior input property available to REST profile binding metadata.
/// </summary>
/// <param name="Name">The canonical behavior input property name.</param>
/// <param name="Type">The declared property type.</param>
public sealed record BehaviorRestInputPropertyDescriptor(string Name, Type Type);
