namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Declares metadata-only REST projection preferences for a behavior without publishing a public REST route.
/// </summary>
/// <remarks>
/// This attribute does not activate public REST by itself. Cephalon keeps public REST module-owned,
/// so the attribute only describes a candidate method, relative route pattern, and optional API
/// version for future generated or descriptor-backed module projections. Use repeated
/// <see cref="BehaviorRestBindingAttribute" /> declarations when a module-owned projection should
/// bind selected input properties explicitly from route values, query string values, headers, or
/// JSON body fields. When a profile declares explicit bindings and still needs remaining query
/// values to flow implicitly, set <see cref="PreserveImplicitQueryFallback" /> to <see langword="true" />.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BehaviorRestProfileAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="BehaviorRestProfileAttribute"/>.
    /// </summary>
    /// <param name="method">The candidate REST method for a future module-owned projection.</param>
    /// <param name="relativePattern">
    /// The candidate route pattern relative to a future owning REST group, for example
    /// <c>"/{cartId}"</c> or <c>"/{cartId}/items"</c>.
    /// </param>
    public BehaviorRestProfileAttribute(BehaviorRestMethod method, string relativePattern)
    {
        Method = method;
        RelativePattern = relativePattern;
    }

    /// <summary>
    /// Gets the candidate REST method for the future module-owned projection.
    /// </summary>
    public BehaviorRestMethod Method { get; }

    /// <summary>
    /// Gets the candidate route pattern relative to a future owning REST group.
    /// </summary>
    public string RelativePattern { get; }

    /// <summary>
    /// Gets or sets the candidate API major version for the future REST projection.
    /// </summary>
    /// <remarks>
    /// The default value <c>0</c> means "unspecified". Host-level publication still depends on the
    /// published OpenAPI document allow-list such as <c>OpenApi:EnabledVersions</c>.
    /// </remarks>
    public int ApiVersionMajor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether explicit profile bindings should preserve the
    /// remaining implicit query-string fallback surface.
    /// </summary>
    /// <remarks>
    /// This flag is meaningful only when the profile also declares explicit bindings through
    /// <see cref="BehaviorRestBindingAttribute" />. It lets module-owned shorthand keep selected
    /// explicit route/query/header/body bindings while still letting unbound query-string values
    /// populate the remaining input properties deterministically.
    /// </remarks>
    public bool PreserveImplicitQueryFallback { get; set; }
}
