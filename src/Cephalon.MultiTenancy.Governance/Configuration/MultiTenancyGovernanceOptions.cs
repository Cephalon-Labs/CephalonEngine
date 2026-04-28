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
    /// Gets or sets a value indicating whether the built-in tenant-domain ownership verification workflow executor is active.
    /// </summary>
    public bool EnableDomainOwnershipVerificationWorkflow { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in tenant-domain ownership proof evaluator is active.
    /// </summary>
    public bool EnableDomainOwnershipProofEvaluation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in tenant-domain ownership proof challenge issuer is active.
    /// </summary>
    public bool EnableDomainOwnershipProofChallengeIssuance { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in tenant-domain ownership proof publication planner is active.
    /// </summary>
    public bool EnableDomainOwnershipProofPublicationPlanning { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in tenant-domain ownership HTTP proof collector is active.
    /// </summary>
    public bool EnableDomainOwnershipHttpProofCollection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in tenant-domain ownership proof verification runner is active.
    /// </summary>
    public bool EnableDomainOwnershipProofVerificationRunner { get; set; } = true;

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
    /// Gets or sets the default DNS TXT record prefix used by proof challenge issuance.
    /// </summary>
    public string DomainOwnershipProofChallengeDnsTxtRecordPrefix { get; set; } = "_cephalon-domain-verification";

    /// <summary>
    /// Gets or sets the default HTTP path used by proof challenge issuance.
    /// </summary>
    public string DomainOwnershipProofChallengeHttpFilePath { get; set; } = "/.well-known/cephalon/domain-ownership.txt";

    /// <summary>
    /// Gets or sets a value indicating whether HTTP proof collection may use non-HTTPS URLs.
    /// </summary>
    public bool AllowInsecureDomainOwnershipHttpProofCollection { get; set; }

    /// <summary>
    /// Gets or sets the default timeout, in seconds, used by HTTP proof collection.
    /// </summary>
    public int DomainOwnershipHttpProofCollectionTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum response body size, in bytes, accepted by HTTP proof collection.
    /// </summary>
    public int DomainOwnershipHttpProofCollectionMaxResponseBytes { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the optional JSON file path used for Cephalon-managed durable governance-action workflow state.
    /// </summary>
    public string? GovernanceActionStoreFilePath { get; set; }
}
