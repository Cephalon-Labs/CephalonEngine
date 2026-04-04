namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Describes one authorization policy surface contributed to the active runtime.
/// </summary>
public sealed class AuthorizationPolicyDescriptor
{
    /// <summary>
    /// Creates a new authorization policy descriptor.
    /// </summary>
    /// <param name="id">The stable authorization-policy identifier.</param>
    /// <param name="displayName">The operator-facing authorization-policy name.</param>
    /// <param name="description">The human-readable authorization-policy description.</param>
    /// <param name="modes">The authorization modes supported by the policy.</param>
    /// <param name="tags">Optional descriptive tags associated with the policy.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the policy.</param>
    public AuthorizationPolicyDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyList<AuthorizationMode>? modes = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Authorization policy id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Authorization policy display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Authorization policy description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Modes = modes?
            .Distinct()
            .OrderBy(static mode => mode)
            .ToArray() ?? [];
        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable authorization-policy identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing authorization-policy name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable authorization-policy description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the authorization modes supported by the policy.
    /// </summary>
    public IReadOnlyList<AuthorizationMode> Modes { get; }

    /// <summary>
    /// Gets descriptive tags associated with the policy.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the policy.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
