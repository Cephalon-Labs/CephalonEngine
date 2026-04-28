using Cephalon.MultiTenancy.Governance.Services;

namespace Cephalon.MultiTenancy.Governance.Configuration;

/// <summary>
/// Configures the tenant-governance companion package.
/// </summary>
/// <remarks>
/// These options seed the host-owned governance baseline. Installed modules can still contribute
/// additional memberships, invitations, domain ownership descriptors, and governance actions through
/// contributor contracts.
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
    /// Gets the host-defined tenant-domain ownership descriptors available to the governance runtime.
    /// </summary>
    public IList<TenantDomainOwnershipDescriptor> DomainOwnerships { get; } = [];

    /// <summary>
    /// Gets the host-defined approval and remediation actions available to the governance runtime.
    /// </summary>
    public IList<TenantGovernanceActionDescriptor> GovernanceActions { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the built-in membership evaluator is active.
    /// </summary>
    public bool EnableMembershipEvaluation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in invitation validator is active.
    /// </summary>
    public bool EnableInvitationValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in tenant-domain ownership validator is active.
    /// </summary>
    public bool EnableDomainOwnershipValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in tenant-governance action decider is active.
    /// </summary>
    public bool EnableGovernanceActionDecision { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in tenant-governance action workflow executor is active.
    /// </summary>
    public bool EnableGovernanceActionWorkflow { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional JSON file path used for Cephalon-managed durable tenant-membership state.
    /// </summary>
    public string? MembershipStoreFilePath { get; set; }

    /// <summary>
    /// Gets or sets the optional JSON file path used for Cephalon-managed durable tenant-invitation state.
    /// </summary>
    public string? InvitationStoreFilePath { get; set; }

    /// <summary>
    /// Gets or sets the optional JSON file path used for Cephalon-managed durable tenant-domain ownership state.
    /// </summary>
    public string? DomainOwnershipStoreFilePath { get; set; }

    /// <summary>
    /// Gets or sets the optional JSON file path used for Cephalon-managed durable governance-action workflow state.
    /// </summary>
    public string? GovernanceActionStoreFilePath { get; set; }
}
