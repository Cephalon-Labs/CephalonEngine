namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Describes the protected resource being evaluated by an authorization policy.
/// </summary>
public sealed class AuthorizationResource
{
    /// <summary>
    /// Creates a new authorization resource.
    /// </summary>
    /// <param name="resourceType">The logical resource type identifier.</param>
    /// <param name="resourceId">The stable resource identifier when one is known.</param>
    /// <param name="tenantId">The tenant identifier associated with the resource.</param>
    /// <param name="ownerSubjectId">The owning subject identifier when one is known.</param>
    /// <param name="attributes">Optional attributes associated with the resource.</param>
    public AuthorizationResource(
        string resourceType,
        string? resourceId = null,
        string? tenantId = null,
        string? ownerSubjectId = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ArgumentException("Authorization resource type is required.", nameof(resourceType));
        }

        ResourceType = resourceType.Trim();
        ResourceId = string.IsNullOrWhiteSpace(resourceId) ? null : resourceId.Trim();
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
        OwnerSubjectId = string.IsNullOrWhiteSpace(ownerSubjectId) ? null : ownerSubjectId.Trim();
        Attributes = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the logical resource type identifier.
    /// </summary>
    public string ResourceType { get; }

    /// <summary>
    /// Gets the stable resource identifier when one is known.
    /// </summary>
    public string? ResourceId { get; }

    /// <summary>
    /// Gets the tenant identifier associated with the resource.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the owning subject identifier when one is known.
    /// </summary>
    public string? OwnerSubjectId { get; }

    /// <summary>
    /// Gets the attributes associated with the resource.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}
