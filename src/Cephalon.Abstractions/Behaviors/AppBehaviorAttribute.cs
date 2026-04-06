namespace Cephalon.Abstractions.Behaviors;

/// <summary>Maps an <see cref="IAppBehavior{TIn,TOut}"/> class to its configuration entry id.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AppBehaviorAttribute : Attribute
{
    /// <summary>Initializes a new instance of <see cref="AppBehaviorAttribute"/>.</summary>
    public AppBehaviorAttribute(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
            throw new ArgumentException("Behavior id must not be empty.", nameof(behaviorId));
        BehaviorId = behaviorId;
    }

    /// <summary>Gets the behavior configuration entry id.</summary>
    public string BehaviorId { get; }
}
