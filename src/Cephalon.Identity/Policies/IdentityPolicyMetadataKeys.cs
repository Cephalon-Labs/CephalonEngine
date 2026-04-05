namespace Cephalon.Identity.Policies;

/// <summary>
/// Defines the metadata keys understood by the built-in Cephalon identity evaluator.
/// </summary>
public static class IdentityPolicyMetadataKeys
{
    /// <summary>
    /// The metadata key that lists required subject roles as a comma-separated value.
    /// </summary>
    public const string RequiredRoles = "requiredRoles";

    /// <summary>
    /// The metadata key that controls how <see cref="RequiredRoles" /> should be matched.
    /// </summary>
    public const string RequiredRoleMatch = "requiredRoleMatch";

    /// <summary>
    /// The value used by <see cref="RequiredRoleMatch" /> when any listed role may satisfy the policy.
    /// </summary>
    public const string RequiredRoleMatchAny = "any";

    /// <summary>
    /// The value used by <see cref="RequiredRoleMatch" /> when all listed roles must be present.
    /// </summary>
    public const string RequiredRoleMatchAll = "all";

    /// <summary>
    /// The metadata key that requires the current subject to match the resource owner when set to
    /// <see langword="true" />.
    /// </summary>
    public const string RequireOwner = "requireOwner";

    /// <summary>
    /// The metadata key that requires the subject, resource, and context to stay within the same tenant
    /// boundary when set to <see langword="true" />.
    /// </summary>
    public const string RequireTenantMatch = "requireTenantMatch";

    /// <summary>
    /// The metadata key prefix for required subject attributes.
    /// </summary>
    public const string SubjectAttributePrefix = "subject.";

    /// <summary>
    /// The metadata key prefix for required resource attributes.
    /// </summary>
    public const string ResourceAttributePrefix = "resource.";

    /// <summary>
    /// The metadata key prefix for required evaluation-context attributes.
    /// </summary>
    public const string ContextAttributePrefix = "context.";
}
