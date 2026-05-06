namespace Cephalon.Behaviors.Services;

/// <summary>
/// Describes one public readable input property discovered for a behavior contract.
/// </summary>
/// <remarks>
/// Source-generated and explicitly registered contract metadata use this descriptor so
/// transport adapters can validate property-level binding plans without inspecting
/// runtime input types.
/// </remarks>
public sealed class BehaviorInputPropertyDescriptor
{
    /// <summary>
    /// Initializes a new <see cref="BehaviorInputPropertyDescriptor" />.
    /// </summary>
    /// <param name="name">The canonical input property name.</param>
    /// <param name="type">The declared property type.</param>
    public BehaviorInputPropertyDescriptor(string name, Type type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Gets the canonical input property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the declared property type.
    /// </summary>
    public Type Type { get; }
}
