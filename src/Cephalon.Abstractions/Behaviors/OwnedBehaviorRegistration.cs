using System;

namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Describes one explicit module-owned behavior registration collected during engine composition.
/// </summary>
public sealed class OwnedBehaviorRegistration
{
    /// <summary>
    /// Initializes a new <see cref="OwnedBehaviorRegistration" />.
    /// </summary>
    /// <param name="sourceModuleId">The stable module identifier that owns the behavior.</param>
    /// <param name="behaviorId">The stable behavior identifier.</param>
    /// <param name="behaviorType">The concrete behavior implementation type.</param>
    /// <param name="configureTopology">
    /// An optional topology callback used when the owning module needs to select an explicit behavior topology.
    /// </param>
    public OwnedBehaviorRegistration(
        string sourceModuleId,
        string behaviorId,
        Type behaviorType,
        Action<IBehaviorTopologyBuilder>? configureTopology = null)
    {
        SourceModuleId = NormalizeRequired(sourceModuleId, nameof(sourceModuleId));
        BehaviorId = NormalizeRequired(behaviorId, nameof(behaviorId));
        BehaviorType = behaviorType ?? throw new ArgumentNullException(nameof(behaviorType));
        ConfigureTopology = configureTopology;
    }

    /// <summary>
    /// Gets the stable module identifier that owns the behavior.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the stable behavior identifier.
    /// </summary>
    public string BehaviorId { get; }

    /// <summary>
    /// Gets the concrete behavior implementation type.
    /// </summary>
    public Type BehaviorType { get; }

    /// <summary>
    /// Gets the optional topology callback supplied by the owning module.
    /// </summary>
    public Action<IBehaviorTopologyBuilder>? ConfigureTopology { get; }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", paramName);
        }

        return value.Trim();
    }
}
