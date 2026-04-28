using Cephalon.MultiTenancy.Governance.Services;

namespace Cephalon.MultiTenancy.Governance.Configuration;

/// <summary>
/// Configures the tenant-governance companion package.
/// </summary>
/// <remarks>
/// These options seed the host-owned membership baseline. Installed modules can still contribute
/// additional memberships through <see cref="ITenantMembershipContributor" />.
/// </remarks>
public sealed class MultiTenancyGovernanceOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiTenancyGovernanceOptions" /> class.
    /// </summary>
    public MultiTenancyGovernanceOptions()
    {
    }

    /// <summary>
    /// Gets the host-defined tenant memberships available to the governance runtime.
    /// </summary>
    public IList<TenantMembershipDescriptor> Memberships { get; } = [];

    /// <summary>
    /// Gets the host-defined tenant invitations available to the governance runtime.
    /// </summary>
    public IList<TenantInvitationDescriptor> Invitations { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the built-in membership evaluator is active.
    /// </summary>
    public bool EnableMembershipEvaluation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in invitation validator is active.
    /// </summary>
    public bool EnableInvitationValidation { get; set; } = true;
}
