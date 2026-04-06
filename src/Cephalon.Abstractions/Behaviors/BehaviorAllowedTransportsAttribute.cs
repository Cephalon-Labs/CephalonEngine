namespace Cephalon.Abstractions.Behaviors;

/// <summary>Restricts which transports ops config may activate for this behavior. If absent, no allowlist restriction applies.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BehaviorAllowedTransportsAttribute : Attribute
{
    /// <summary>Initializes a new instance of <see cref="BehaviorAllowedTransportsAttribute"/>.</summary>
    public BehaviorAllowedTransportsAttribute(params string[] transports) => Transports = [..transports];

    /// <summary>Gets the set of allowed transport identifiers.</summary>
    public IReadOnlyList<string> Transports { get; }
}
