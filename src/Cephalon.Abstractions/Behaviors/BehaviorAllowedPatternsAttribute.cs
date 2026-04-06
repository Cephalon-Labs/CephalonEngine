namespace Cephalon.Abstractions.Behaviors;

/// <summary>Restricts which patterns ops config may activate for this behavior. If absent, no allowlist restriction applies.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BehaviorAllowedPatternsAttribute : Attribute
{
    /// <summary>Initializes a new instance of <see cref="BehaviorAllowedPatternsAttribute"/>.</summary>
    public BehaviorAllowedPatternsAttribute(params string[] patterns) => Patterns = [..patterns];

    /// <summary>Gets the set of allowed pattern identifiers.</summary>
    public IReadOnlyList<string> Patterns { get; }
}
