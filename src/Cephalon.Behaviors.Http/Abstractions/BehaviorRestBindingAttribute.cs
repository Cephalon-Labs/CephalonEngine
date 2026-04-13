namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Declares one explicit request-source binding for a behavior input property on a metadata-only
/// REST profile.
/// </summary>
/// <remarks>
/// This attribute does not publish public REST by itself. It augments
/// <see cref="BehaviorRestProfileAttribute" /> so module-owned projections such as
/// <c>MapProfile&lt;TBehavior&gt;()</c> can bind selected input properties from route values, query
/// string values, headers, or the JSON body without relying only on implicit merge rules.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class BehaviorRestBindingAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="BehaviorRestBindingAttribute" />.
    /// </summary>
    /// <param name="propertyName">The behavior input property that receives the bound value.</param>
    /// <param name="source">The HTTP request source that supplies the value.</param>
    public BehaviorRestBindingAttribute(string propertyName, BehaviorRestBindingSource source)
    {
        PropertyName = propertyName;
        Source = source;
    }

    /// <summary>
    /// Gets the behavior input property that receives the bound value.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the HTTP request source that supplies the value.
    /// </summary>
    public BehaviorRestBindingSource Source { get; }

    /// <summary>
    /// Gets or sets the external route key, query-string key, header name, or body property name to
    /// read from. When omitted, Cephalon uses <see cref="PropertyName" />.
    /// </summary>
    public string? Name { get; set; }
}
