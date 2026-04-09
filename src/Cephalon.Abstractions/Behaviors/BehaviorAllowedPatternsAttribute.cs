namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Restricts which patterns may activate for this behavior and can also provide an
/// attribute-only runtime baseline when exactly one pattern is declared.
/// </summary>
/// <remarks>
/// When a behavior has no explicit topology from <c>ConfigureTopology(...)</c>, fluent registration,
/// or configuration overrides, a single declared pattern becomes the runtime baseline. Multiple
/// declared patterns remain an allowlist and require another topology source to choose one.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BehaviorAllowedPatternsAttribute : Attribute
{
    /// <summary>Initializes a new instance of <see cref="BehaviorAllowedPatternsAttribute"/>.</summary>
    public BehaviorAllowedPatternsAttribute(params string[] patterns) => Patterns = [..patterns];

    /// <summary>Gets the set of allowed pattern identifiers.</summary>
    public IReadOnlyList<string> Patterns { get; }
}
