namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Marks a class as a registered application behavior and assigns its stable identifier.
/// This attribute is required on all types registered via <c>IBehaviorCollectionBuilder.Register&lt;TBehavior&gt;()</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AppBehaviorAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the behavior's stable identifier.
    /// </summary>
    /// <param name="id">The stable, unique behavior identifier used for dispatch and configuration lookup.</param>
    public AppBehaviorAttribute(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Behavior id must not be empty.", nameof(id));
        Id = id.Trim();
    }

    /// <summary>
    /// Gets the stable behavior identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the stable behavior identifier.
    /// </summary>
    /// <remarks>Alias for <see cref="Id" /> retained for source compatibility.</remarks>
    public string BehaviorId => Id;
}
