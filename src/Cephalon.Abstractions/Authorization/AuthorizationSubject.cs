namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Describes the caller or actor being evaluated by an authorization policy.
/// </summary>
public sealed class AuthorizationSubject
{
    /// <summary>
    /// Creates a new authorization subject.
    /// </summary>
    /// <param name="subjectId">The stable subject identifier.</param>
    /// <param name="displayName">The human-readable subject name when one is known.</param>
    /// <param name="roles">Optional roles assigned to the subject.</param>
    /// <param name="tenantIds">Optional tenant identifiers associated with the subject.</param>
    /// <param name="attributes">Optional attributes associated with the subject.</param>
    public AuthorizationSubject(
        string subjectId,
        string? displayName = null,
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<string>? tenantIds = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new ArgumentException("Authorization subject id is required.", nameof(subjectId));
        }

        SubjectId = subjectId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Roles = Normalize(roles);
        TenantIds = Normalize(tenantIds);
        Attributes = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable subject identifier.
    /// </summary>
    public string SubjectId { get; }

    /// <summary>
    /// Gets the human-readable subject name when one is known.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the roles assigned to the subject.
    /// </summary>
    public IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Gets the tenant identifiers associated with the subject.
    /// </summary>
    public IReadOnlyList<string> TenantIds { get; }

    /// <summary>
    /// Gets the attributes associated with the subject.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
