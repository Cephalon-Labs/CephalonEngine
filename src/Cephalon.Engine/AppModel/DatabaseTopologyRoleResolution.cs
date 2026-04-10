using Cephalon.Abstractions.AppModel;

namespace Cephalon.Engine.AppModel;

/// <summary>
/// Describes one resolved <c>Engine:Databases</c> role selection, including any explicit role reference.
/// </summary>
public sealed class DatabaseTopologyRoleResolution
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTopologyRoleResolution" /> class.
    /// </summary>
    /// <param name="requestedRoleId">The logical role requested by the caller.</param>
    /// <param name="resolvedRoleId">The concrete role that supplied the physical database target.</param>
    /// <param name="requestedTarget">The target declared for the requested role.</param>
    /// <param name="effectiveTarget">The effective target after applying any role reference.</param>
    /// <param name="useRole">The referenced concrete role declared by the requested target, when present.</param>
    public DatabaseTopologyRoleResolution(
        string requestedRoleId,
        string resolvedRoleId,
        DatabaseTargetSelection requestedTarget,
        DatabaseTargetSelection effectiveTarget,
        string? useRole = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedRoleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedRoleId);
        ArgumentNullException.ThrowIfNull(requestedTarget);
        ArgumentNullException.ThrowIfNull(effectiveTarget);

        RequestedRoleId = requestedRoleId.Trim();
        ResolvedRoleId = resolvedRoleId.Trim();
        RequestedTarget = requestedTarget;
        EffectiveTarget = effectiveTarget;
        UseRole = string.IsNullOrWhiteSpace(useRole) ? null : useRole.Trim();
    }

    /// <summary>
    /// Gets the logical role requested by the caller.
    /// </summary>
    public string RequestedRoleId { get; }

    /// <summary>
    /// Gets the concrete role that supplied the effective provider and connection settings.
    /// </summary>
    public string ResolvedRoleId { get; }

    /// <summary>
    /// Gets the target declared for the requested role.
    /// </summary>
    public DatabaseTargetSelection RequestedTarget { get; }

    /// <summary>
    /// Gets the effective target after applying any role reference.
    /// </summary>
    public DatabaseTargetSelection EffectiveTarget { get; }

    /// <summary>
    /// Gets the referenced concrete role declared by the requested target, when present.
    /// </summary>
    public string? UseRole { get; }

    /// <summary>
    /// Gets a value indicating whether the requested role resolved through an explicit role reference.
    /// </summary>
    public bool UsesRoleReference => UseRole is not null;

    /// <summary>
    /// Gets the stable resolution mode used by runtime introspection surfaces.
    /// </summary>
    public string ResolutionMode => UsesRoleReference ? "role-reference" : "direct";
}
