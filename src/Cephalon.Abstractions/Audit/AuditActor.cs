namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Describes the actor responsible for one audited operation.
/// </summary>
public sealed class AuditActor
{
    /// <summary>
    /// Creates a new audit actor.
    /// </summary>
    /// <param name="actorId">The stable actor identifier.</param>
    /// <param name="displayName">The human-readable actor name when one is known.</param>
    /// <param name="actorType">The logical actor type such as <c>user</c>, <c>service</c>, or <c>system</c>.</param>
    /// <param name="isSystem">Whether the actor represents system-owned automation.</param>
    /// <param name="attributes">Optional actor attributes.</param>
    public AuditActor(
        string actorId,
        string? displayName = null,
        string? actorType = null,
        bool isSystem = false,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("Audit actor id is required.", nameof(actorId));
        }

        ActorId = actorId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        ActorType = string.IsNullOrWhiteSpace(actorType) ? null : actorType.Trim();
        IsSystem = isSystem;
        Attributes = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable actor identifier.
    /// </summary>
    public string ActorId { get; }

    /// <summary>
    /// Gets the human-readable actor name when one is known.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the logical actor type when one is known.
    /// </summary>
    public string? ActorType { get; }

    /// <summary>
    /// Gets a value indicating whether the actor represents system-owned automation.
    /// </summary>
    public bool IsSystem { get; }

    /// <summary>
    /// Gets the actor attributes.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}
