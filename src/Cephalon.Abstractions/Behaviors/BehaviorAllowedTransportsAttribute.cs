namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Restricts which transports may activate for this behavior and can also provide an
/// attribute-only runtime transport baseline when no explicit topology exists.
/// </summary>
/// <remarks>
/// Declared transports remain a transport allowlist for config and topology validation. When a
/// behavior has no explicit topology, the declared transports become the runtime transport baseline.
/// For <c>http.rest</c>, do not also declare the same transport through <c>ConfigureTopology(...)</c>.
/// For author-facing allowlists, <c>http.grpc</c> is accepted and normalized to canonical
/// <c>grpc</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BehaviorAllowedTransportsAttribute : Attribute
{
    /// <summary>Initializes a new instance of <see cref="BehaviorAllowedTransportsAttribute"/>.</summary>
    public BehaviorAllowedTransportsAttribute(params string[] transports) => Transports = [..transports];

    /// <summary>Gets the set of allowed transport identifiers.</summary>
    public IReadOnlyList<string> Transports { get; }
}
