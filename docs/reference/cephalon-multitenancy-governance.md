# Cephalon.MultiTenancy.Governance

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance)
## Namespaces

- `Cephalon.MultiTenancy.Governance.Configuration`
- `Cephalon.MultiTenancy.Governance.Registration`
- `Cephalon.MultiTenancy.Governance.Services`

<a id="namespace-cephalon-multitenancy-governance-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.Configuration

<a id="type-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions"></a>

### `MultiTenancyGovernanceOptions`

Configures the tenant-governance companion package.

Remarks: These options seed the host-owned governance baseline. Installed modules can still contribute additional memberships, invitations, domain ownership descriptors, and governance actions through contributor contracts.

#### Declaration
```csharp
public sealed class MultiTenancyGovernanceOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-ctor"></a>

##### `MultiTenancyGovernanceOptions`

```csharp
MultiTenancyGovernanceOptions()
```

Initializes a new instance of the `MultiTenancyGovernanceOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipproofchallengednstxtrecordprefix"></a>

##### `DomainOwnershipProofChallengeDnsTxtRecordPrefix`

```csharp
string DomainOwnershipProofChallengeDnsTxtRecordPrefix { get; set; }
```

Gets or sets the default DNS TXT record prefix used by proof challenge issuance.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipproofchallengehttpfilepath"></a>

##### `DomainOwnershipProofChallengeHttpFilePath`

```csharp
string DomainOwnershipProofChallengeHttpFilePath { get; set; }
```

Gets or sets the default HTTP path used by proof challenge issuance.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownerships"></a>

##### `DomainOwnerships`

```csharp
IList<TenantDomainOwnershipDescriptor> DomainOwnerships { get; }
```

Gets the host-defined tenant-domain ownership descriptors available to the governance runtime.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipstorefilepath"></a>

##### `DomainOwnershipStoreFilePath`

```csharp
string DomainOwnershipStoreFilePath { get; set; }
```

Gets or sets the optional JSON file path used for Cephalon-managed durable tenant-domain ownership state.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipproofchallengeissuance"></a>

##### `EnableDomainOwnershipProofChallengeIssuance`

```csharp
bool EnableDomainOwnershipProofChallengeIssuance { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership proof challenge issuer is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipproofevaluation"></a>

##### `EnableDomainOwnershipProofEvaluation`

```csharp
bool EnableDomainOwnershipProofEvaluation { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership proof evaluator is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipvalidation"></a>

##### `EnableDomainOwnershipValidation`

```csharp
bool EnableDomainOwnershipValidation { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership validator is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipverificationworkflow"></a>

##### `EnableDomainOwnershipVerificationWorkflow`

```csharp
bool EnableDomainOwnershipVerificationWorkflow { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership verification workflow executor is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enablegovernanceactiondecision"></a>

##### `EnableGovernanceActionDecision`

```csharp
bool EnableGovernanceActionDecision { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-governance action decider is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enablegovernanceactionworkflow"></a>

##### `EnableGovernanceActionWorkflow`

```csharp
bool EnableGovernanceActionWorkflow { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-governance action workflow executor is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enableinvitationvalidation"></a>

##### `EnableInvitationValidation`

```csharp
bool EnableInvitationValidation { get; set; }
```

Gets or sets a value indicating whether the built-in invitation validator is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enablemembershipevaluation"></a>

##### `EnableMembershipEvaluation`

```csharp
bool EnableMembershipEvaluation { get; set; }
```

Gets or sets a value indicating whether the built-in membership evaluator is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-governanceactions"></a>

##### `GovernanceActions`

```csharp
IList<TenantGovernanceActionDescriptor> GovernanceActions { get; }
```

Gets the host-defined approval and remediation actions available to the governance runtime.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-governanceactionstorefilepath"></a>

##### `GovernanceActionStoreFilePath`

```csharp
string GovernanceActionStoreFilePath { get; set; }
```

Gets or sets the optional JSON file path used for Cephalon-managed durable governance-action workflow state.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitations"></a>

##### `Invitations`

```csharp
IList<TenantInvitationDescriptor> Invitations { get; }
```

Gets the host-defined tenant invitations available to the governance runtime.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationstorefilepath"></a>

##### `InvitationStoreFilePath`

```csharp
string InvitationStoreFilePath { get; set; }
```

Gets or sets the optional JSON file path used for Cephalon-managed durable tenant-invitation state.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-memberships"></a>

##### `Memberships`

```csharp
IList<TenantMembershipDescriptor> Memberships { get; }
```

Gets the host-defined tenant memberships available to the governance runtime.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-membershipstorefilepath"></a>

##### `MembershipStoreFilePath`

```csharp
string MembershipStoreFilePath { get; set; }
```

Gets or sets the optional JSON file path used for Cephalon-managed durable tenant-membership state.

<a id="namespace-cephalon-multitenancy-governance-registration"></a>

## Namespace Cephalon.MultiTenancy.Governance.Registration

<a id="type-cephalon-multitenancy-governance-registration-multitenancygovernanceenginebuilderextensions"></a>

### `MultiTenancyGovernanceEngineBuilderExtensions`

Registers the Cephalon tenant-governance companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class MultiTenancyGovernanceEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-registration-multitenancygovernanceenginebuilderextensions-addmultitenancygovernance-cephalon-engine-composition-enginebuilder-system-action-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions"></a>

##### `AddMultiTenancyGovernance`

```csharp
EngineBuilder AddMultiTenancyGovernance(this EngineBuilder builder, Action<MultiTenancyGovernanceOptions> configure)
```

Adds the tenant-governance companion pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures membership governance options.

<a id="namespace-cephalon-multitenancy-governance-services"></a>

## Namespace Cephalon.MultiTenancy.Governance.Services

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipcatalog"></a>

### `ITenantDomainOwnershipCatalog`

Exposes the merged tenant-domain ownership set available to the active governance runtime.

#### Declaration
```csharp
public interface ITenantDomainOwnershipCatalog
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantdomainownershipcatalog-domainownerships"></a>

##### `DomainOwnerships`

```csharp
IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships { get; }
```

Gets the effective domain ownership set after runtime storage, host options, and module contributors have been applied.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipcatalog-getbydomainname-system-string"></a>

##### `GetByDomainName`

```csharp
IReadOnlyList<TenantDomainOwnershipDescriptor> GetByDomainName(string domainName)
```

Gets domain ownership descriptors by canonical domain name across all tenants.

Returns: The matching domain ownership descriptors.

Parameters:
- `domainName`: The domain name to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipcatalog-getbytenantanddomain-system-string-system-string"></a>

##### `GetByTenantAndDomain`

```csharp
IReadOnlyList<TenantDomainOwnershipDescriptor> GetByTenantAndDomain(string tenantId, string domainName)
```

Gets domain ownership descriptors by tenant and domain name.

Returns: The matching domain ownership descriptors.

Parameters:
- `tenantId`: The tenant identifier to resolve.
- `domainName`: The domain name to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipcatalog-getbytenantid-system-string"></a>

##### `GetByTenantId`

```csharp
IReadOnlyList<TenantDomainOwnershipDescriptor> GetByTenantId(string tenantId)
```

Gets domain ownership descriptors for one tenant.

Returns: The matching domain ownership descriptors.

Parameters:
- `tenantId`: The tenant identifier to resolve.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipcontributor"></a>

### `ITenantDomainOwnershipContributor`

Allows a module to contribute tenant-domain ownership descriptors into the active governance runtime.

#### Declaration
```csharp
public interface ITenantDomainOwnershipContributor
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipcontributor-registerdomainownerships-cephalon-multitenancy-governance-services-itenantdomainownershipregistry"></a>

##### `RegisterDomainOwnerships`

```csharp
void RegisterDomainOwnerships(ITenantDomainOwnershipRegistry domainOwnerships)
```

Registers one or more tenant-domain ownership descriptors with the supplied registry.

Parameters:
- `domainOwnerships`: The registry that collects contributed domain ownership descriptors.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipproofchallengeissuer"></a>

### `ITenantDomainOwnershipProofChallengeIssuer`

Issues tenant-domain ownership proof challenges and records the expected proof value for later evaluation.

Remarks: The issuer owns challenge generation and runtime metadata mutation. It does not publish DNS records, host HTTP proof files, or poll external endpoints; applications or provider packs publish and observe the issued challenge.

#### Declaration
```csharp
public interface ITenantDomainOwnershipProofChallengeIssuer
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipproofchallengeissuer-issueasync-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-system-threading-cancellationtoken"></a>

##### `IssueAsync`

```csharp
ValueTask<TenantDomainOwnershipProofChallengeResult> IssueAsync(TenantDomainOwnershipProofChallengeRequest request, CancellationToken cancellationToken)
```

Issues or refreshes a proof challenge for a tenant-domain ownership declaration.

Returns: The issued challenge details and runtime state outcome.

Parameters:
- `request`: The proof challenge request.
- `cancellationToken`: A token that cancels challenge issuance before runtime state is stored.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipproofevaluator"></a>

### `ITenantDomainOwnershipProofEvaluator`

Evaluates reported tenant-domain ownership proof evidence and applies the resulting verification workflow transition.

Remarks: The evaluator owns proof comparison and workflow mutation. It does not collect DNS records, HTTP files, or external polling evidence itself; applications or provider packs report the observed proof value into this contract.

#### Declaration
```csharp
public interface ITenantDomainOwnershipProofEvaluator
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipproofevaluator-evaluateasync-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-system-threading-cancellationtoken"></a>

##### `EvaluateAsync`

```csharp
ValueTask<TenantDomainOwnershipProofEvaluationResult> EvaluateAsync(TenantDomainOwnershipProofEvaluationRequest request, CancellationToken cancellationToken)
```

Evaluates reported proof evidence for a tenant-domain ownership declaration.

Returns: The proof evaluation result and workflow transition outcome.

Parameters:
- `request`: The proof evaluation request.
- `cancellationToken`: A token that cancels proof evaluation before workflow mutation starts.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipregistry"></a>

### `ITenantDomainOwnershipRegistry`

Collects tenant-domain ownership descriptors contributed to the active governance runtime.

#### Declaration
```csharp
public interface ITenantDomainOwnershipRegistry
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipregistry-add-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor"></a>

##### `Add`

```csharp
void Add(TenantDomainOwnershipDescriptor domainOwnership)
```

Adds a tenant-domain ownership descriptor to the registry.

Parameters:
- `domainOwnership`: The domain ownership descriptor to contribute.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipstore"></a>

### `ITenantDomainOwnershipStore`

Stores runtime tenant-domain ownership declarations managed by the multi-tenancy governance companion pack.

#### Declaration
```csharp
public interface ITenantDomainOwnershipStore
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantdomainownershipstore-count"></a>

##### `Count`

```csharp
int Count { get; }
```

Gets the number of stored runtime tenant-domain ownership declarations.

<a id="member-p-cephalon-multitenancy-governance-services-itenantdomainownershipstore-domainownerships"></a>

##### `DomainOwnerships`

```csharp
IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships { get; }
```

Gets the stored runtime tenant-domain ownership declarations.

<a id="member-p-cephalon-multitenancy-governance-services-itenantdomainownershipstore-isdurable"></a>

##### `IsDurable`

```csharp
bool IsDurable { get; }
```

Gets a value indicating whether tenant-domain ownership state survives process restarts.

<a id="member-p-cephalon-multitenancy-governance-services-itenantdomainownershipstore-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; }
```

Gets the ownership mode for the store implementation.

<a id="member-p-cephalon-multitenancy-governance-services-itenantdomainownershipstore-storekind"></a>

##### `StoreKind`

```csharp
string StoreKind { get; }
```

Gets the operator-facing store kind.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipstore-upsert-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor"></a>

##### `Upsert`

```csharp
void Upsert(TenantDomainOwnershipDescriptor domainOwnership)
```

Creates or replaces one stored runtime tenant-domain ownership declaration.

Parameters:
- `domainOwnership`: The tenant-domain ownership declaration to store.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipvalidator"></a>

### `ITenantDomainOwnershipValidator`

Validates declared tenant-domain ownership against the active governance runtime.

#### Declaration
```csharp
public interface ITenantDomainOwnershipValidator
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipvalidator-validateasync-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationrequest-system-threading-cancellationtoken"></a>

##### `ValidateAsync`

```csharp
ValueTask<TenantDomainOwnershipValidationResult> ValidateAsync(TenantDomainOwnershipValidationRequest request, CancellationToken cancellationToken)
```

Validates a tenant-domain ownership request.

Returns: The validation result.

Parameters:
- `request`: The validation request.
- `cancellationToken`: A token that cancels validation.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipverificationworkflow"></a>

### `ITenantDomainOwnershipVerificationWorkflow`

Applies in-process tenant-domain ownership verification workflow transitions.

#### Declaration
```csharp
public interface ITenantDomainOwnershipVerificationWorkflow
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipverificationworkflow-applyasync-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-system-threading-cancellationtoken"></a>

##### `ApplyAsync`

```csharp
ValueTask<TenantDomainOwnershipVerificationWorkflowResult> ApplyAsync(TenantDomainOwnershipVerificationWorkflowRequest request, CancellationToken cancellationToken)
```

Applies one tenant-domain ownership verification workflow command.

Returns: The evaluated workflow transition result.

Parameters:
- `request`: The workflow transition request to evaluate.
- `cancellationToken`: A cancellation token for the workflow operation.

<a id="type-cephalon-multitenancy-governance-services-itenantgovernanceactioncatalog"></a>

### `ITenantGovernanceActionCatalog`

Exposes the merged tenant-governance action set available to the active governance runtime.

#### Declaration
```csharp
public interface ITenantGovernanceActionCatalog
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantgovernanceactioncatalog-actions"></a>

##### `Actions`

```csharp
IReadOnlyList<TenantGovernanceActionDescriptor> Actions { get; }
```

Gets the effective governance action set after host options and module contributors have both been applied.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantgovernanceactioncatalog-getbyactionid-system-string"></a>

##### `GetByActionId`

```csharp
IReadOnlyList<TenantGovernanceActionDescriptor> GetByActionId(string actionId)
```

Gets governance action descriptors by action identifier across all tenants.

Returns: The matching governance action descriptors.

Parameters:
- `actionId`: The action identifier to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantgovernanceactioncatalog-getbytenantandaction-system-string-system-string"></a>

##### `GetByTenantAndAction`

```csharp
IReadOnlyList<TenantGovernanceActionDescriptor> GetByTenantAndAction(string tenantId, string actionId)
```

Gets governance action descriptors by tenant and action identifier.

Returns: The matching governance action descriptors.

Parameters:
- `tenantId`: The tenant identifier to resolve.
- `actionId`: The action identifier to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantgovernanceactioncatalog-getbytenantid-system-string"></a>

##### `GetByTenantId`

```csharp
IReadOnlyList<TenantGovernanceActionDescriptor> GetByTenantId(string tenantId)
```

Gets governance action descriptors for one tenant.

Returns: The matching governance action descriptors.

Parameters:
- `tenantId`: The tenant identifier to resolve.

<a id="type-cephalon-multitenancy-governance-services-itenantgovernanceactioncontributor"></a>

### `ITenantGovernanceActionContributor`

Allows a module to contribute tenant-governance approval or remediation actions into the active governance runtime.

#### Declaration
```csharp
public interface ITenantGovernanceActionContributor
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantgovernanceactioncontributor-registergovernanceactions-cephalon-multitenancy-governance-services-itenantgovernanceactionregistry"></a>

##### `RegisterGovernanceActions`

```csharp
void RegisterGovernanceActions(ITenantGovernanceActionRegistry actions)
```

Registers one or more tenant-governance actions with the supplied registry.

Parameters:
- `actions`: The registry that collects contributed governance action descriptors.

<a id="type-cephalon-multitenancy-governance-services-itenantgovernanceactiondecider"></a>

### `ITenantGovernanceActionDecider`

Decides whether a tenant-governance action can proceed against the active governance runtime.

#### Declaration
```csharp
public interface ITenantGovernanceActionDecider
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantgovernanceactiondecider-decideasync-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-system-threading-cancellationtoken"></a>

##### `DecideAsync`

```csharp
ValueTask<TenantGovernanceActionDecisionResult> DecideAsync(TenantGovernanceActionDecisionRequest request, CancellationToken cancellationToken)
```

Decides one tenant-governance action request.

Returns: The decision result.

Parameters:
- `request`: The decision request.
- `cancellationToken`: A token that cancels decision evaluation.

<a id="type-cephalon-multitenancy-governance-services-itenantgovernanceactionregistry"></a>

### `ITenantGovernanceActionRegistry`

Collects tenant-governance action descriptors contributed to the active governance runtime.

#### Declaration
```csharp
public interface ITenantGovernanceActionRegistry
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantgovernanceactionregistry-add-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor"></a>

##### `Add`

```csharp
void Add(TenantGovernanceActionDescriptor action)
```

Adds a tenant-governance action descriptor to the registry.

Parameters:
- `action`: The governance action descriptor to contribute.

<a id="type-cephalon-multitenancy-governance-services-itenantgovernanceactionstore"></a>

### `ITenantGovernanceActionStore`

Stores runtime tenant-governance actions created or transitioned by the governance action workflow.

#### Declaration
```csharp
public interface ITenantGovernanceActionStore
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantgovernanceactionstore-actions"></a>

##### `Actions`

```csharp
IReadOnlyList<TenantGovernanceActionDescriptor> Actions { get; }
```

Gets the stored runtime tenant-governance actions.

<a id="member-p-cephalon-multitenancy-governance-services-itenantgovernanceactionstore-count"></a>

##### `Count`

```csharp
int Count { get; }
```

Gets the number of stored runtime tenant-governance actions.

<a id="member-p-cephalon-multitenancy-governance-services-itenantgovernanceactionstore-isdurable"></a>

##### `IsDurable`

```csharp
bool IsDurable { get; }
```

Gets a value indicating whether action state survives process restarts.

<a id="member-p-cephalon-multitenancy-governance-services-itenantgovernanceactionstore-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; }
```

Gets the ownership mode for the store implementation.

<a id="member-p-cephalon-multitenancy-governance-services-itenantgovernanceactionstore-storekind"></a>

##### `StoreKind`

```csharp
string StoreKind { get; }
```

Gets the operator-facing store kind.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantgovernanceactionstore-upsert-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor"></a>

##### `Upsert`

```csharp
void Upsert(TenantGovernanceActionDescriptor action)
```

Creates or replaces one stored runtime tenant-governance action.

Parameters:
- `action`: The tenant-governance action to store.

<a id="type-cephalon-multitenancy-governance-services-itenantgovernanceactionworkflow"></a>

### `ITenantGovernanceActionWorkflow`

Applies host-agnostic tenant-governance action workflow transitions.

#### Declaration
```csharp
public interface ITenantGovernanceActionWorkflow
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantgovernanceactionworkflow-applyasync-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-system-threading-cancellationtoken"></a>

##### `ApplyAsync`

```csharp
ValueTask<TenantGovernanceActionWorkflowResult> ApplyAsync(TenantGovernanceActionWorkflowRequest request, CancellationToken cancellationToken)
```

Applies one tenant-governance action workflow transition.

Returns: The workflow transition result.

Parameters:
- `request`: The workflow transition request.
- `cancellationToken`: A token that cancels the transition.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationcatalog"></a>

### `ITenantInvitationCatalog`

Exposes the merged tenant-invitation set available to the active governance runtime.

#### Declaration
```csharp
public interface ITenantInvitationCatalog
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationcatalog-invitations"></a>

##### `Invitations`

```csharp
IReadOnlyList<TenantInvitationDescriptor> Invitations { get; }
```

Gets the effective invitation set after runtime storage, host options, and module contributors have all been applied.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationcatalog-getbyinvitationid-system-string"></a>

##### `GetByInvitationId`

```csharp
IReadOnlyList<TenantInvitationDescriptor> GetByInvitationId(string invitationId)
```

Gets invitations by invitation identifier across all tenants.

Returns: The matching invitations.

Parameters:
- `invitationId`: The invitation identifier to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationcatalog-getbyinviteeid-system-string"></a>

##### `GetByInviteeId`

```csharp
IReadOnlyList<TenantInvitationDescriptor> GetByInviteeId(string inviteeId)
```

Gets invitations for one invitee across all tenants.

Returns: The matching invitations.

Parameters:
- `inviteeId`: The invitee identifier to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationcatalog-getbytenantandinvitation-system-string-system-string"></a>

##### `GetByTenantAndInvitation`

```csharp
IReadOnlyList<TenantInvitationDescriptor> GetByTenantAndInvitation(string tenantId, string invitationId)
```

Gets invitations by tenant and invitation identifier.

Returns: The matching invitations.

Parameters:
- `tenantId`: The tenant identifier to resolve.
- `invitationId`: The invitation identifier to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationcatalog-getbytenantid-system-string"></a>

##### `GetByTenantId`

```csharp
IReadOnlyList<TenantInvitationDescriptor> GetByTenantId(string tenantId)
```

Gets invitations for one tenant.

Returns: The matching invitations.

Parameters:
- `tenantId`: The tenant identifier to resolve.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationcontributor"></a>

### `ITenantInvitationContributor`

Allows a module to contribute tenant invitations into the active governance runtime.

#### Declaration
```csharp
public interface ITenantInvitationContributor
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationcontributor-registerinvitations-cephalon-multitenancy-governance-services-itenantinvitationregistry"></a>

##### `RegisterInvitations`

```csharp
void RegisterInvitations(ITenantInvitationRegistry invitations)
```

Registers one or more tenant invitations with the supplied registry.

Parameters:
- `invitations`: The registry that collects contributed invitations.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationregistry"></a>

### `ITenantInvitationRegistry`

Collects tenant invitations contributed to the active governance runtime.

#### Declaration
```csharp
public interface ITenantInvitationRegistry
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationregistry-add-cephalon-multitenancy-governance-services-tenantinvitationdescriptor"></a>

##### `Add`

```csharp
void Add(TenantInvitationDescriptor invitation)
```

Adds a tenant-invitation descriptor to the registry.

Parameters:
- `invitation`: The invitation descriptor to contribute.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationstore"></a>

### `ITenantInvitationStore`

Stores runtime tenant invitations managed by the multi-tenancy governance companion pack.

#### Declaration
```csharp
public interface ITenantInvitationStore
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationstore-count"></a>

##### `Count`

```csharp
int Count { get; }
```

Gets the number of stored runtime tenant invitations.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationstore-invitations"></a>

##### `Invitations`

```csharp
IReadOnlyList<TenantInvitationDescriptor> Invitations { get; }
```

Gets the stored runtime tenant invitations.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationstore-isdurable"></a>

##### `IsDurable`

```csharp
bool IsDurable { get; }
```

Gets a value indicating whether invitation state survives process restarts.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationstore-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; }
```

Gets the ownership mode for the store implementation.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationstore-storekind"></a>

##### `StoreKind`

```csharp
string StoreKind { get; }
```

Gets the operator-facing store kind.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationstore-upsert-cephalon-multitenancy-governance-services-tenantinvitationdescriptor"></a>

##### `Upsert`

```csharp
void Upsert(TenantInvitationDescriptor invitation)
```

Creates or replaces one stored runtime tenant invitation.

Parameters:
- `invitation`: The tenant invitation to store.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationvalidator"></a>

### `ITenantInvitationValidator`

Validates tenant invitations against the active governance runtime.

#### Declaration
```csharp
public interface ITenantInvitationValidator
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationvalidator-validateasync-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-system-threading-cancellationtoken"></a>

##### `ValidateAsync`

```csharp
ValueTask<TenantInvitationValidationResult> ValidateAsync(TenantInvitationValidationRequest request, CancellationToken cancellationToken)
```

Validates a tenant invitation request.

Returns: The validation result.

Parameters:
- `request`: The validation request.
- `cancellationToken`: A token that cancels validation.

<a id="type-cephalon-multitenancy-governance-services-itenantmembershipcatalog"></a>

### `ITenantMembershipCatalog`

Exposes the merged tenant-membership set available to the active governance runtime.

#### Declaration
```csharp
public interface ITenantMembershipCatalog
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantmembershipcatalog-memberships"></a>

##### `Memberships`

```csharp
IReadOnlyList<TenantMembershipDescriptor> Memberships { get; }
```

Gets the effective membership set after runtime storage, host options, and module contributors have all been applied.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantmembershipcatalog-getbyprincipalid-system-string"></a>

##### `GetByPrincipalId`

```csharp
IReadOnlyList<TenantMembershipDescriptor> GetByPrincipalId(string principalId)
```

Gets memberships for one principal across all tenants.

Returns: The matching memberships.

Parameters:
- `principalId`: The principal identifier to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantmembershipcatalog-getbytenantandprincipal-system-string-system-string"></a>

##### `GetByTenantAndPrincipal`

```csharp
IReadOnlyList<TenantMembershipDescriptor> GetByTenantAndPrincipal(string tenantId, string principalId)
```

Gets memberships for one principal in one tenant.

Returns: The matching memberships.

Parameters:
- `tenantId`: The tenant identifier to resolve.
- `principalId`: The principal identifier to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantmembershipcatalog-getbytenantid-system-string"></a>

##### `GetByTenantId`

```csharp
IReadOnlyList<TenantMembershipDescriptor> GetByTenantId(string tenantId)
```

Gets memberships for one tenant.

Returns: The matching memberships.

Parameters:
- `tenantId`: The tenant identifier to resolve.

<a id="member-m-cephalon-multitenancy-governance-services-itenantmembershipcatalog-getbytenantprincipalandkind-system-string-system-string-system-string"></a>

##### `GetByTenantPrincipalAndKind`

```csharp
IReadOnlyList<TenantMembershipDescriptor> GetByTenantPrincipalAndKind(string tenantId, string principalKind, string principalId)
```

Gets memberships for one principal kind and principal identifier in one tenant.

Returns: The matching memberships.

Parameters:
- `tenantId`: The tenant identifier to resolve.
- `principalKind`: The principal kind to resolve.
- `principalId`: The principal identifier to resolve.

<a id="type-cephalon-multitenancy-governance-services-itenantmembershipcontributor"></a>

### `ITenantMembershipContributor`

Allows a module to contribute tenant memberships into the active governance runtime.

#### Declaration
```csharp
public interface ITenantMembershipContributor
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantmembershipcontributor-registermemberships-cephalon-multitenancy-governance-services-itenantmembershipregistry"></a>

##### `RegisterMemberships`

```csharp
void RegisterMemberships(ITenantMembershipRegistry memberships)
```

Registers one or more tenant memberships with the supplied registry.

Parameters:
- `memberships`: The registry that collects contributed memberships.

<a id="type-cephalon-multitenancy-governance-services-itenantmembershipevaluator"></a>

### `ITenantMembershipEvaluator`

Evaluates whether a principal has active membership in a tenant.

#### Declaration
```csharp
public interface ITenantMembershipEvaluator
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantmembershipevaluator-evaluateasync-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-system-threading-cancellationtoken"></a>

##### `EvaluateAsync`

```csharp
ValueTask<TenantMembershipEvaluationResult> EvaluateAsync(TenantMembershipEvaluationRequest request, CancellationToken cancellationToken)
```

Evaluates tenant membership for the supplied request.

Returns: The membership evaluation result.

Parameters:
- `request`: The membership evaluation request.
- `cancellationToken`: The token that cancels evaluation.

<a id="type-cephalon-multitenancy-governance-services-itenantmembershipregistry"></a>

### `ITenantMembershipRegistry`

Collects tenant memberships contributed to the active governance runtime.

#### Declaration
```csharp
public interface ITenantMembershipRegistry
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantmembershipregistry-add-cephalon-multitenancy-governance-services-tenantmembershipdescriptor"></a>

##### `Add`

```csharp
void Add(TenantMembershipDescriptor membership)
```

Adds a tenant-membership descriptor to the registry.

Parameters:
- `membership`: The membership descriptor to contribute.

<a id="type-cephalon-multitenancy-governance-services-itenantmembershipstore"></a>

### `ITenantMembershipStore`

Stores runtime tenant memberships managed by the multi-tenancy governance companion pack.

#### Declaration
```csharp
public interface ITenantMembershipStore
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantmembershipstore-count"></a>

##### `Count`

```csharp
int Count { get; }
```

Gets the number of stored runtime tenant memberships.

<a id="member-p-cephalon-multitenancy-governance-services-itenantmembershipstore-isdurable"></a>

##### `IsDurable`

```csharp
bool IsDurable { get; }
```

Gets a value indicating whether membership state survives process restarts.

<a id="member-p-cephalon-multitenancy-governance-services-itenantmembershipstore-memberships"></a>

##### `Memberships`

```csharp
IReadOnlyList<TenantMembershipDescriptor> Memberships { get; }
```

Gets the stored runtime tenant memberships.

<a id="member-p-cephalon-multitenancy-governance-services-itenantmembershipstore-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; }
```

Gets the ownership mode for the store implementation.

<a id="member-p-cephalon-multitenancy-governance-services-itenantmembershipstore-storekind"></a>

##### `StoreKind`

```csharp
string StoreKind { get; }
```

Gets the operator-facing store kind.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantmembershipstore-upsert-cephalon-multitenancy-governance-services-tenantmembershipdescriptor"></a>

##### `Upsert`

```csharp
void Upsert(TenantMembershipDescriptor membership)
```

Creates or replaces one stored runtime tenant membership.

Parameters:
- `membership`: The tenant membership to store.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor"></a>

### `TenantDomainOwnershipDescriptor`

Describes one declared domain ownership relationship for a tenant.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipDescriptor
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipDescriptor`

```csharp
TenantDomainOwnershipDescriptor(string tenantId, string domainName, string displayName, string status, string verificationMethod, DateTimeOffset? verifiedAtUtc, DateTimeOffset? expiresAtUtc, string sourceModuleId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership descriptor.

Parameters:
- `tenantId`: The stable tenant identifier.
- `domainName`: The domain name claimed by the tenant.
- `displayName`: The optional operator-facing domain name.
- `status`: The domain ownership status.
- `verificationMethod`: The verification method associated with the descriptor.
- `verifiedAtUtc`: The UTC timestamp when ownership was verified.
- `expiresAtUtc`: The UTC timestamp when ownership expires.
- `sourceModuleId`: The module that contributed the domain ownership descriptor when one is known.
- `metadata`: Optional operator-facing metadata attached to the descriptor.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing domain name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name claimed by the tenant.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the UTC timestamp when ownership expires.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata attached to the descriptor.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that contributed the domain ownership descriptor when one is known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the domain ownership status.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the stable tenant identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method associated with the descriptor.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-verifiedatutc"></a>

##### `VerifiedAtUtc`

```csharp
DateTimeOffset? VerifiedAtUtc { get; }
```

Gets the UTC timestamp when ownership was verified.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys"></a>

### `TenantDomainOwnershipProofChallengeMetadataKeys`

Stable metadata keys written by tenant-domain ownership proof challenge issuance.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofChallengeMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-dnstxtrecordname"></a>

##### `DnsTxtRecordName`

```csharp
const string DnsTxtRecordName
```

Metadata key for the DNS TXT record name where the challenge should be published.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-httpfilepath"></a>

##### `HttpFilePath`

```csharp
const string HttpFilePath
```

Metadata key for the HTTP path where the challenge should be published.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-lastproofchallengeactor"></a>

##### `LastProofChallengeActor`

```csharp
const string LastProofChallengeActor
```

Metadata key for the actor that requested proof challenge issuance.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-lastproofchallengecorrelationid"></a>

##### `LastProofChallengeCorrelationId`

```csharp
const string LastProofChallengeCorrelationId
```

Metadata key for the challenge issuance correlation identifier.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-lastproofchallengeexpiresatutc"></a>

##### `LastProofChallengeExpiresAtUtc`

```csharp
const string LastProofChallengeExpiresAtUtc
```

Metadata key for the UTC timestamp when the challenge expires.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-lastproofchallengefingerprint"></a>

##### `LastProofChallengeFingerprint`

```csharp
const string LastProofChallengeFingerprint
```

Metadata key for the SHA-256 fingerprint of the issued challenge value.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-lastproofchallengeissuedatutc"></a>

##### `LastProofChallengeIssuedAtUtc`

```csharp
const string LastProofChallengeIssuedAtUtc
```

Metadata key for the UTC timestamp when the challenge was issued.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-lastproofchallengeoutcome"></a>

##### `LastProofChallengeOutcome`

```csharp
const string LastProofChallengeOutcome
```

Metadata key for the last proof challenge issuance outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-lastproofchallengesource"></a>

##### `LastProofChallengeSource`

```csharp
const string LastProofChallengeSource
```

Metadata key for the source that requested proof challenge issuance.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengemetadatakeys-proofchallengeownership"></a>

##### `ProofChallengeOwnership`

```csharp
const string ProofChallengeOwnership
```

Metadata key that identifies Cephalon as the challenge issuance owner.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeoutcomes"></a>

### `TenantDomainOwnershipProofChallengeOutcomes`

Stable tenant-domain ownership proof challenge issuance outcomes.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofChallengeOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeoutcomes-alreadyverified"></a>

##### `AlreadyVerified`

```csharp
const string AlreadyVerified
```

The domain is already verified and does not need a new challenge.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Challenge issuance is disabled by governance options.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeoutcomes-invalidstatus"></a>

##### `InvalidStatus`

```csharp
const string InvalidStatus
```

The current declaration status cannot receive a new proof challenge.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeoutcomes-issued"></a>

##### `Issued`

```csharp
const string Issued
```

Challenge issuance created or refreshed the pending proof challenge.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

Runtime state could not be persisted.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

The domain is already declared for a different tenant.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeoutcomes-verificationmethodmismatch"></a>

##### `VerificationMethodMismatch`

```csharp
const string VerificationMethodMismatch
```

The requested verification method does not match the existing declaration.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest"></a>

### `TenantDomainOwnershipProofChallengeRequest`

Describes a tenant-domain ownership proof challenge issuance request.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofChallengeRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofChallengeRequest`

```csharp
TenantDomainOwnershipProofChallengeRequest(string tenantId, string domainName, string verificationMethod, string displayName, string challengeValue, string source, string actor, DateTimeOffset? atUtc, DateTimeOffset? expiresAtUtc, string correlationId, string dnsTxtRecordName, string httpFilePath, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof challenge request.

Parameters:
- `tenantId`: The tenant identifier that owns the domain declaration.
- `domainName`: The domain name that should receive a proof challenge.
- `verificationMethod`: The optional verification method boundary.
- `displayName`: The optional operator-facing domain display name.
- `challengeValue`: An optional caller-supplied challenge value. A secure random value is generated when omitted.
- `source`: The source that requested challenge issuance.
- `actor`: The actor that requested challenge issuance when known.
- `atUtc`: The UTC timestamp used for challenge issuance. The runtime clock is used when omitted.
- `expiresAtUtc`: The optional UTC timestamp when the challenge and ownership declaration expire.
- `correlationId`: The optional correlation identifier for challenge issuance.
- `dnsTxtRecordName`: The optional DNS TXT record name where the challenge should be published.
- `httpFilePath`: The optional HTTP path where the challenge should be published.
- `metadata`: Optional proof challenge metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested challenge issuance when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for challenge issuance.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-challengevalue"></a>

##### `ChallengeValue`

```csharp
string ChallengeValue { get; }
```

Gets an optional caller-supplied challenge value.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for challenge issuance.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing domain display name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-dnstxtrecordname"></a>

##### `DnsTxtRecordName`

```csharp
string DnsTxtRecordName { get; }
```

Gets the optional DNS TXT record name where the challenge should be published.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that should receive a proof challenge.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp when the challenge and ownership declaration expire.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-httpfilepath"></a>

##### `HttpFilePath`

```csharp
string HttpFilePath { get; }
```

Gets the optional HTTP path where the challenge should be published.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional proof challenge metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested challenge issuance.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the domain declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengerequest-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the optional verification method boundary.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult"></a>

### `TenantDomainOwnershipProofChallengeResult`

Describes the result of tenant-domain ownership proof challenge issuance.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofChallengeResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-datetimeoffset-system-string-system-string-system-string-system-string-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofChallengeResult`

```csharp
TenantDomainOwnershipProofChallengeResult(string tenantId, string domainName, string verificationMethod, string outcome, bool issued, DateTimeOffset issuedAtUtc, string challengeValue, string challengeFingerprint, string dnsTxtRecordName, string httpFilePath, TenantDomainOwnershipDescriptor domainOwnership, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof challenge result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `domainName`: The canonical domain name that was evaluated.
- `verificationMethod`: The verification method used for challenge issuance.
- `outcome`: The stable challenge issuance outcome.
- `issued`: A value indicating whether a challenge was issued and stored.
- `issuedAtUtc`: The UTC timestamp when challenge issuance executed.
- `challengeValue`: The public proof challenge value to publish.
- `challengeFingerprint`: The SHA-256 fingerprint of the challenge value.
- `dnsTxtRecordName`: The DNS TXT record name where the challenge should be published.
- `httpFilePath`: The HTTP path where the challenge should be published.
- `domainOwnership`: The matching or resulting domain ownership descriptor when one exists.
- `reason`: The operator-facing challenge issuance reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-challengefingerprint"></a>

##### `ChallengeFingerprint`

```csharp
string ChallengeFingerprint { get; }
```

Gets the SHA-256 fingerprint of the challenge value.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-challengevalue"></a>

##### `ChallengeValue`

```csharp
string ChallengeValue { get; }
```

Gets the public proof challenge value to publish.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-dnstxtrecordname"></a>

##### `DnsTxtRecordName`

```csharp
string DnsTxtRecordName { get; }
```

Gets the DNS TXT record name where the challenge should be published.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-domainownership"></a>

##### `DomainOwnership`

```csharp
TenantDomainOwnershipDescriptor DomainOwnership { get; }
```

Gets the matching or resulting domain ownership descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-httpfilepath"></a>

##### `HttpFilePath`

```csharp
string HttpFilePath { get; }
```

Gets the HTTP path where the challenge should be published.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-issued"></a>

##### `Issued`

```csharp
bool Issued { get; }
```

Gets a value indicating whether a challenge was issued and stored.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-issuedatutc"></a>

##### `IssuedAtUtc`

```csharp
DateTimeOffset IssuedAtUtc { get; }
```

Gets the UTC timestamp when challenge issuance executed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable challenge issuance outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing challenge issuance reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method used for challenge issuance.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes"></a>

### `TenantDomainOwnershipProofEvaluationOutcomes`

Defines stable tenant-domain ownership proof evaluation outcome labels.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofEvaluationOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

The built-in proof evaluator is disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-missingexpectedproof"></a>

##### `MissingExpectedProof`

```csharp
const string MissingExpectedProof
```

The request and descriptor did not contain an expected proof value to compare against.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-missingobservedproof"></a>

##### `MissingObservedProof`

```csharp
const string MissingObservedProof
```

The request did not contain an observed proof value to evaluate.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No tenant-domain ownership declaration matched the supplied tenant and domain.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-rejected"></a>

##### `Rejected`

```csharp
const string Rejected
```

The observed proof did not match the expected proof and the domain ownership was rejected.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

A declaration for the supplied domain belongs to a different tenant.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-verificationmethodmismatch"></a>

##### `VerificationMethodMismatch`

```csharp
const string VerificationMethodMismatch
```

The matching domain ownership declaration uses a different verification method.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-verified"></a>

##### `Verified`

```csharp
const string Verified
```

The observed proof matched the expected proof and the domain ownership was verified.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationoutcomes-workflowdenied"></a>

##### `WorkflowDenied`

```csharp
const string WorkflowDenied
```

Proof evaluation matched or mismatched, but the verification workflow refused or failed the transition.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest"></a>

### `TenantDomainOwnershipProofEvaluationRequest`

Describes reported proof evidence for a tenant-domain ownership declaration.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofEvaluationRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofEvaluationRequest`

```csharp
TenantDomainOwnershipProofEvaluationRequest(string tenantId, string domainName, string observedProof, string verificationMethod, string expectedProof, string source, string actor, DateTimeOffset? atUtc, DateTimeOffset? expiresAtUtc, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof evaluation request.

Parameters:
- `tenantId`: The tenant identifier that owns the domain declaration.
- `domainName`: The domain name whose proof should be evaluated.
- `observedProof`: The proof value observed by the application or provider pack.
- `verificationMethod`: The optional verification method boundary.
- `expectedProof`: The optional expected proof value. Descriptor metadata is used when this is omitted.
- `source`: The source that reported the observed proof evidence.
- `actor`: The actor that requested proof evaluation when known.
- `atUtc`: The UTC timestamp used for proof evaluation. The runtime clock is used when omitted.
- `expiresAtUtc`: The optional UTC timestamp when the ownership declaration expires.
- `correlationId`: The optional correlation identifier for proof evaluation.
- `metadata`: Optional proof evaluation metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested proof evaluation when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for proof evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for proof evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name whose proof should be evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-expectedproof"></a>

##### `ExpectedProof`

```csharp
string ExpectedProof { get; }
```

Gets the optional expected proof value. Descriptor metadata is used when this is omitted.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp when the ownership declaration expires.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional proof evaluation metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-observedproof"></a>

##### `ObservedProof`

```csharp
string ObservedProof { get; }
```

Gets the proof value observed by the application or provider pack.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that reported the observed proof evidence.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the domain declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationrequest-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the optional verification method boundary.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult"></a>

### `TenantDomainOwnershipProofEvaluationResult`

Describes the result of one tenant-domain ownership proof evaluation.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofEvaluationResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-string-system-string-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofEvaluationResult`

```csharp
TenantDomainOwnershipProofEvaluationResult(string tenantId, string domainName, string verificationMethod, string outcome, bool matched, bool applied, DateTimeOffset evaluatedAtUtc, string observedProofFingerprint, string expectedProofFingerprint, TenantDomainOwnershipDescriptor domainOwnership, TenantDomainOwnershipVerificationWorkflowResult workflowResult, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof evaluation result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `domainName`: The canonical domain name that was evaluated.
- `verificationMethod`: The verification method used for evaluation.
- `outcome`: The stable proof evaluation outcome.
- `matched`: A value indicating whether the observed proof matched the expected proof.
- `applied`: A value indicating whether the verification workflow transition was applied.
- `evaluatedAtUtc`: The UTC timestamp when proof evaluation executed.
- `observedProofFingerprint`: The SHA-256 fingerprint of the observed proof value when present.
- `expectedProofFingerprint`: The SHA-256 fingerprint of the expected proof value when present.
- `domainOwnership`: The matching or resulting domain ownership descriptor when one exists.
- `workflowResult`: The underlying workflow transition result when proof evaluation reached workflow mutation.
- `reason`: The operator-facing proof evaluation reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-applied"></a>

##### `Applied`

```csharp
bool Applied { get; }
```

Gets a value indicating whether the verification workflow transition was applied.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-domainownership"></a>

##### `DomainOwnership`

```csharp
TenantDomainOwnershipDescriptor DomainOwnership { get; }
```

Gets the matching or resulting domain ownership descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-evaluatedatutc"></a>

##### `EvaluatedAtUtc`

```csharp
DateTimeOffset EvaluatedAtUtc { get; }
```

Gets the UTC timestamp when proof evaluation executed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-expectedprooffingerprint"></a>

##### `ExpectedProofFingerprint`

```csharp
string ExpectedProofFingerprint { get; }
```

Gets the SHA-256 fingerprint of the expected proof value when present.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-matched"></a>

##### `Matched`

```csharp
bool Matched { get; }
```

Gets a value indicating whether the observed proof matched the expected proof.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-observedprooffingerprint"></a>

##### `ObservedProofFingerprint`

```csharp
string ObservedProofFingerprint { get; }
```

Gets the SHA-256 fingerprint of the observed proof value when present.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable proof evaluation outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing proof evaluation reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method used for evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-workflowresult"></a>

##### `WorkflowResult`

```csharp
TenantDomainOwnershipVerificationWorkflowResult WorkflowResult { get; }
```

Gets the underlying workflow transition result when proof evaluation reached workflow mutation.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys"></a>

### `TenantDomainOwnershipProofMetadataKeys`

Defines stable metadata keys used by tenant-domain ownership proof evaluation.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-expecteddnstxtproof"></a>

##### `ExpectedDnsTxtProof`

```csharp
const string ExpectedDnsTxtProof
```

Expected DNS TXT proof value for DNS-based domain ownership verification.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-expectedhttpfileproof"></a>

##### `ExpectedHttpFileProof`

```csharp
const string ExpectedHttpFileProof
```

Expected HTTP file or well-known endpoint proof value for HTTP-based domain ownership verification.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-expectedproof"></a>

##### `ExpectedProof`

```csharp
const string ExpectedProof
```

Generic expected proof value used when a method-specific expected value is not present.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-lastproofevaluationactor"></a>

##### `LastProofEvaluationActor`

```csharp
const string LastProofEvaluationActor
```

Actor that requested or reported the proof evaluation when known.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-lastproofevaluationcorrelationid"></a>

##### `LastProofEvaluationCorrelationId`

```csharp
const string LastProofEvaluationCorrelationId
```

Correlation identifier for the last proof evaluation when known.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-lastproofevaluationexpectedfingerprint"></a>

##### `LastProofEvaluationExpectedFingerprint`

```csharp
const string LastProofEvaluationExpectedFingerprint
```

SHA-256 fingerprint of the expected proof value considered by the evaluator.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-lastproofevaluationobservedfingerprint"></a>

##### `LastProofEvaluationObservedFingerprint`

```csharp
const string LastProofEvaluationObservedFingerprint
```

SHA-256 fingerprint of the observed proof value considered by the evaluator.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-lastproofevaluationoutcome"></a>

##### `LastProofEvaluationOutcome`

```csharp
const string LastProofEvaluationOutcome
```

Last proof evaluation outcome recorded on the domain ownership descriptor.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-lastproofevaluationsource"></a>

##### `LastProofEvaluationSource`

```csharp
const string LastProofEvaluationSource
```

Source that reported the observed proof evidence.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofmetadatakeys-proofevaluationownership"></a>

##### `ProofEvaluationOwnership`

```csharp
const string ProofEvaluationOwnership
```

Ownership marker for proof evaluation performed by the governance companion.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipstatuses"></a>

### `TenantDomainOwnershipStatuses`

Defines stable tenant-domain ownership statuses understood by the governance runtime.

#### Declaration
```csharp
public static class TenantDomainOwnershipStatuses
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipstatuses-expired"></a>

##### `Expired`

```csharp
const string Expired
```

The tenant domain ownership is no longer within its valid time window.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipstatuses-pending"></a>

##### `Pending`

```csharp
const string Pending
```

The tenant domain ownership is declared but not yet verified.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipstatuses-rejected"></a>

##### `Rejected`

```csharp
const string Rejected
```

The tenant domain ownership proof was rejected.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipstatuses-suspended"></a>

##### `Suspended`

```csharp
const string Suspended
```

The tenant domain ownership has been suspended.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipstatuses-verified"></a>

##### `Verified`

```csharp
const string Verified
```

The tenant domain ownership has been verified and can be validated.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes"></a>

### `TenantDomainOwnershipValidationOutcomes`

Defines stable outcomes returned by tenant-domain ownership validation.

#### Declaration
```csharp
public static class TenantDomainOwnershipValidationOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Domain ownership validation is disabled by host configuration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes-expired"></a>

##### `Expired`

```csharp
const string Expired
```

The domain ownership descriptor is expired or outside its valid time window.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No domain ownership descriptor matched the supplied domain.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes-pending"></a>

##### `Pending`

```csharp
const string Pending
```

The domain ownership descriptor is still pending verification.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes-rejected"></a>

##### `Rejected`

```csharp
const string Rejected
```

The domain ownership descriptor was rejected.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes-suspended"></a>

##### `Suspended`

```csharp
const string Suspended
```

The domain ownership descriptor is suspended.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

The domain exists but belongs to a different tenant.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationoutcomes-valid"></a>

##### `Valid`

```csharp
const string Valid
```

The domain ownership descriptor is verified and satisfies the validation request.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationrequest"></a>

### `TenantDomainOwnershipValidationRequest`

Describes one request to validate declared tenant-domain ownership.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipValidationRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationrequest-ctor-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipValidationRequest`

```csharp
TenantDomainOwnershipValidationRequest(string tenantId, string domainName, DateTimeOffset? atUtc, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership validation request.

Parameters:
- `tenantId`: The tenant identifier to validate.
- `domainName`: The domain name to validate.
- `atUtc`: The UTC timestamp used for expiration evaluation. The runtime clock is used when omitted.
- `correlationId`: The optional correlation identifier for the validation.
- `metadata`: Optional request metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for expiration evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the validation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationrequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name to validate.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional request metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier to validate.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult"></a>

### `TenantDomainOwnershipValidationResult`

Describes the result of one tenant-domain ownership validation.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipValidationResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-ctor-system-string-system-string-system-string-system-boolean-system-datetimeoffset-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipValidationResult`

```csharp
TenantDomainOwnershipValidationResult(string tenantId, string domainName, string outcome, bool valid, DateTimeOffset validatedAtUtc, TenantDomainOwnershipDescriptor matchedDomainOwnership, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership validation result.

Parameters:
- `tenantId`: The tenant identifier that was validated.
- `domainName`: The canonical domain name that was validated.
- `outcome`: The stable validation outcome.
- `valid`: A value indicating whether validation granted domain ownership use.
- `validatedAtUtc`: The UTC timestamp when validation executed.
- `matchedDomainOwnership`: The matching domain ownership descriptor considered by validation.
- `reason`: The optional operator-facing validation reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was validated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-matcheddomainownership"></a>

##### `MatchedDomainOwnership`

```csharp
TenantDomainOwnershipDescriptor MatchedDomainOwnership { get; }
```

Gets the matching domain ownership descriptor considered by validation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable validation outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the optional operator-facing validation reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was validated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-valid"></a>

##### `Valid`

```csharp
bool Valid { get; }
```

Gets a value indicating whether validation granted domain ownership use.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipvalidationresult-validatedatutc"></a>

##### `ValidatedAtUtc`

```csharp
DateTimeOffset ValidatedAtUtc { get; }
```

Gets the UTC timestamp when validation executed.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowcommands"></a>

### `TenantDomainOwnershipVerificationWorkflowCommands`

Defines built-in tenant-domain ownership verification workflow commands.

#### Declaration
```csharp
public static class TenantDomainOwnershipVerificationWorkflowCommands
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowcommands-expire"></a>

##### `Expire`

```csharp
const string Expire
```

Expires a non-expired tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowcommands-reject"></a>

##### `Reject`

```csharp
const string Reject
```

Rejects a pending tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowcommands-request"></a>

##### `Request`

```csharp
const string Request
```

Creates a pending tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowcommands-suspend"></a>

##### `Suspend`

```csharp
const string Suspend
```

Suspends a verified tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowcommands-verify"></a>

##### `Verify`

```csharp
const string Verify
```

Marks a pending tenant-domain ownership declaration as verified.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes"></a>

### `TenantDomainOwnershipVerificationWorkflowOutcomes`

Defines stable tenant-domain ownership verification workflow transition outcomes.

#### Declaration
```csharp
public static class TenantDomainOwnershipVerificationWorkflowOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes-applied"></a>

##### `Applied`

```csharp
const string Applied
```

The workflow transition updated an existing tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes-created"></a>

##### `Created`

```csharp
const string Created
```

The workflow transition created a new pending tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Tenant-domain ownership verification workflow execution is disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes-invalidtransition"></a>

##### `InvalidTransition`

```csharp
const string InvalidTransition
```

The requested workflow transition is not valid from the current domain ownership status.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No tenant-domain ownership declaration matched the supplied identifiers.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

The requested workflow transition could not be persisted.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

The matching tenant-domain ownership declaration belongs to a different tenant.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowoutcomes-verificationmethodmismatch"></a>

##### `VerificationMethodMismatch`

```csharp
const string VerificationMethodMismatch
```

The matching tenant-domain ownership declaration uses a different verification method.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest"></a>

### `TenantDomainOwnershipVerificationWorkflowRequest`

Describes one tenant-domain ownership verification workflow transition request.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipVerificationWorkflowRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipVerificationWorkflowRequest`

```csharp
TenantDomainOwnershipVerificationWorkflowRequest(string command, string tenantId, string domainName, string displayName, string verificationMethod, string actor, string reason, string evidence, DateTimeOffset? atUtc, DateTimeOffset? expiresAtUtc, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership verification workflow transition request.

Parameters:
- `command`: The workflow command to apply.
- `tenantId`: The tenant identifier to transition.
- `domainName`: The domain name to transition.
- `displayName`: The optional operator-facing domain name.
- `verificationMethod`: The optional verification method boundary.
- `actor`: The actor that requested the workflow transition when known.
- `reason`: The optional operator-facing transition reason.
- `evidence`: The optional evidence summary observed by the application or provider.
- `atUtc`: The UTC timestamp used for the transition. The runtime clock is used when omitted.
- `expiresAtUtc`: The optional UTC timestamp when the ownership declaration expires.
- `correlationId`: The optional correlation identifier for the workflow transition.
- `metadata`: Optional transition metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested the workflow transition when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for the transition.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-command"></a>

##### `Command`

```csharp
string Command { get; }
```

Gets the workflow command to apply.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the workflow transition.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing domain name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name to transition.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-evidence"></a>

##### `Evidence`

```csharp
string Evidence { get; }
```

Gets the optional evidence summary observed by the application or provider.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp when the ownership declaration expires.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional transition metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the optional operator-facing transition reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier to transition.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowrequest-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the optional verification method boundary.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult"></a>

### `TenantDomainOwnershipVerificationWorkflowResult`

Describes the result of one tenant-domain ownership verification workflow transition.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipVerificationWorkflowResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-datetimeoffset-system-string-system-string-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipVerificationWorkflowResult`

```csharp
TenantDomainOwnershipVerificationWorkflowResult(string tenantId, string domainName, string command, string outcome, bool applied, DateTimeOffset occurredAtUtc, string previousStatus, string currentStatus, TenantDomainOwnershipDescriptor domainOwnership, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership verification workflow transition result.

Parameters:
- `tenantId`: The tenant identifier that was targeted.
- `domainName`: The domain name that was targeted.
- `command`: The workflow command that was requested.
- `outcome`: The workflow transition outcome.
- `applied`: A value indicating whether the workflow transition was applied.
- `occurredAtUtc`: The UTC timestamp when the workflow transition was evaluated.
- `previousStatus`: The domain ownership status before the workflow transition when one existed.
- `currentStatus`: The domain ownership status after the workflow transition when one exists.
- `domainOwnership`: The resulting domain ownership descriptor when one exists.
- `reason`: The operator-facing transition reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-applied"></a>

##### `Applied`

```csharp
bool Applied { get; }
```

Gets a value indicating whether the workflow transition was applied.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-command"></a>

##### `Command`

```csharp
string Command { get; }
```

Gets the workflow command that was requested.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-currentstatus"></a>

##### `CurrentStatus`

```csharp
string CurrentStatus { get; }
```

Gets the domain ownership status after the workflow transition when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was targeted.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-domainownership"></a>

##### `DomainOwnership`

```csharp
TenantDomainOwnershipDescriptor DomainOwnership { get; }
```

Gets the resulting domain ownership descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; }
```

Gets the UTC timestamp when the workflow transition was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the workflow transition outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-previousstatus"></a>

##### `PreviousStatus`

```csharp
string PreviousStatus { get; }
```

Gets the domain ownership status before the workflow transition when one existed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing transition reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipverificationworkflowresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was targeted.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainverificationmethods"></a>

### `TenantDomainVerificationMethods`

Defines stable verification-method labels for tenant domain ownership descriptors.

#### Declaration
```csharp
public static class TenantDomainVerificationMethods
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainverificationmethods-dnstxt"></a>

##### `DnsTxt`

```csharp
const string DnsTxt
```

Domain ownership is expected to be verified by a DNS TXT record.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainverificationmethods-httpfile"></a>

##### `HttpFile`

```csharp
const string HttpFile
```

Domain ownership is expected to be verified by an HTTP file or well-known endpoint.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainverificationmethods-manual"></a>

##### `Manual`

```csharp
const string Manual
```

Domain ownership was verified by an operator or another trusted manual process.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes"></a>

### `TenantGovernanceActionDecisionOutcomes`

Defines stable outcomes returned by tenant-governance action decisions.

#### Declaration
```csharp
public static class TenantGovernanceActionDecisionOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-actionkindmismatch"></a>

##### `ActionKindMismatch`

```csharp
const string ActionKindMismatch
```

The action exists but has a different action kind.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-allowed"></a>

##### `Allowed`

```csharp
const string Allowed
```

The governance action is approved or remediated and satisfies the decision request.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Governance action decisions are disabled by host configuration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-expired"></a>

##### `Expired`

```csharp
const string Expired
```

The action is expired or outside its valid time window.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No governance action descriptor matched the supplied action.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-pendingapproval"></a>

##### `PendingApproval`

```csharp
const string PendingApproval
```

The action is still waiting for approval.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-rejected"></a>

##### `Rejected`

```csharp
const string Rejected
```

The action was rejected.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-remediationrequired"></a>

##### `RemediationRequired`

```csharp
const string RemediationRequired
```

The action requires remediation before it can proceed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-subjectmismatch"></a>

##### `SubjectMismatch`

```csharp
const string SubjectMismatch
```

The action exists but targets a different subject.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

The action exists but belongs to a different tenant.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest"></a>

### `TenantGovernanceActionDecisionRequest`

Describes one request to decide whether a tenant-governance action can proceed.

#### Declaration
```csharp
public sealed class TenantGovernanceActionDecisionRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantGovernanceActionDecisionRequest`

```csharp
TenantGovernanceActionDecisionRequest(string tenantId, string actionId, string actionKind, string subjectKind, string subjectId, DateTimeOffset? atUtc, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-governance action decision request.

Parameters:
- `tenantId`: The tenant identifier to validate.
- `actionId`: The governance action identifier to validate.
- `actionKind`: The optional expected governance action kind.
- `subjectKind`: The optional expected subject kind.
- `subjectId`: The optional expected subject identifier.
- `atUtc`: The UTC timestamp used for expiration evaluation. The runtime clock is used when omitted.
- `correlationId`: The optional correlation identifier for the decision.
- `metadata`: Optional request metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-actionid"></a>

##### `ActionId`

```csharp
string ActionId { get; }
```

Gets the governance action identifier to validate.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-actionkind"></a>

##### `ActionKind`

```csharp
string ActionKind { get; }
```

Gets the optional expected governance action kind.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for expiration evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the decision.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional request metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the optional expected subject identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-subjectkind"></a>

##### `SubjectKind`

```csharp
string SubjectKind { get; }
```

Gets the optional expected subject kind.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier to validate.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult"></a>

### `TenantGovernanceActionDecisionResult`

Describes the result of one tenant-governance action decision.

#### Declaration
```csharp
public sealed class TenantGovernanceActionDecisionResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-ctor-system-string-system-string-system-string-system-boolean-system-datetimeoffset-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantGovernanceActionDecisionResult`

```csharp
TenantGovernanceActionDecisionResult(string tenantId, string actionId, string outcome, bool allowed, DateTimeOffset decidedAtUtc, TenantGovernanceActionDescriptor matchedAction, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-governance action decision result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `actionId`: The governance action identifier that was evaluated.
- `outcome`: The stable decision outcome.
- `allowed`: A value indicating whether the governance action can proceed.
- `decidedAtUtc`: The UTC timestamp when decision evaluation executed.
- `matchedAction`: The matching governance action descriptor considered by decision evaluation.
- `reason`: The optional operator-facing decision reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-actionid"></a>

##### `ActionId`

```csharp
string ActionId { get; }
```

Gets the governance action identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-allowed"></a>

##### `Allowed`

```csharp
bool Allowed { get; }
```

Gets a value indicating whether the governance action can proceed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-decidedatutc"></a>

##### `DecidedAtUtc`

```csharp
DateTimeOffset DecidedAtUtc { get; }
```

Gets the UTC timestamp when decision evaluation executed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-matchedaction"></a>

##### `MatchedAction`

```csharp
TenantGovernanceActionDescriptor MatchedAction { get; }
```

Gets the matching governance action descriptor considered by decision evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable decision outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the optional operator-facing decision reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondecisionresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor"></a>

### `TenantGovernanceActionDescriptor`

Describes one tenant-governance approval or remediation action.

#### Declaration
```csharp
public sealed class TenantGovernanceActionDescriptor
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantGovernanceActionDescriptor`

```csharp
TenantGovernanceActionDescriptor(string actionId, string tenantId, string actionKind, string subjectKind, string subjectId, string displayName, string status, string requestedBy, string approvedBy, DateTimeOffset? createdAtUtc, DateTimeOffset? decidedAtUtc, DateTimeOffset? expiresAtUtc, string sourceModuleId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-governance action descriptor.

Parameters:
- `actionId`: The stable action identifier.
- `tenantId`: The stable tenant identifier.
- `actionKind`: The governance action kind.
- `subjectKind`: The kind of subject affected by the action.
- `subjectId`: The stable subject identifier affected by the action.
- `displayName`: The optional operator-facing action name.
- `status`: The governance action status.
- `requestedBy`: The actor that requested the action when known.
- `approvedBy`: The actor that approved or remediated the action when known.
- `createdAtUtc`: The UTC timestamp when the action was created.
- `decidedAtUtc`: The UTC timestamp when the action was approved, rejected, or remediated.
- `expiresAtUtc`: The UTC timestamp when the action expires.
- `sourceModuleId`: The module that contributed the action when one is known.
- `metadata`: Optional operator-facing metadata attached to the action.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-actionid"></a>

##### `ActionId`

```csharp
string ActionId { get; }
```

Gets the stable action identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-actionkind"></a>

##### `ActionKind`

```csharp
string ActionKind { get; }
```

Gets the governance action kind.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-approvedby"></a>

##### `ApprovedBy`

```csharp
string ApprovedBy { get; }
```

Gets the actor that approved or remediated the action when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-createdatutc"></a>

##### `CreatedAtUtc`

```csharp
DateTimeOffset? CreatedAtUtc { get; }
```

Gets the UTC timestamp when the action was created.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-decidedatutc"></a>

##### `DecidedAtUtc`

```csharp
DateTimeOffset? DecidedAtUtc { get; }
```

Gets the UTC timestamp when the action was approved, rejected, or remediated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing action name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the UTC timestamp when the action expires.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata attached to the action.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-requestedby"></a>

##### `RequestedBy`

```csharp
string RequestedBy { get; }
```

Gets the actor that requested the action when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that contributed the action when one is known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the governance action status.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the stable subject identifier affected by the action.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-subjectkind"></a>

##### `SubjectKind`

```csharp
string SubjectKind { get; }
```

Gets the kind of subject affected by the action.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the stable tenant identifier.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactionkinds"></a>

### `TenantGovernanceActionKinds`

Defines stable tenant-governance action kinds understood by the governance runtime.

#### Declaration
```csharp
public static class TenantGovernanceActionKinds
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionkinds-domainownership"></a>

##### `DomainOwnership`

```csharp
const string DomainOwnership
```

A governance action that changes declared tenant-domain ownership posture.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionkinds-invitationlifecycle"></a>

##### `InvitationLifecycle`

```csharp
const string InvitationLifecycle
```

A governance action that changes an invitation lifecycle.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionkinds-membershipchange"></a>

##### `MembershipChange`

```csharp
const string MembershipChange
```

A governance action that changes tenant membership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionkinds-remediation"></a>

##### `Remediation`

```csharp
const string Remediation
```

A governance action that represents an operator remediation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionkinds-tenantadministration"></a>

##### `TenantAdministration`

```csharp
const string TenantAdministration
```

A governance action that represents tenant administration.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactionstatuses"></a>

### `TenantGovernanceActionStatuses`

Defines stable tenant-governance action statuses understood by the governance runtime.

#### Declaration
```csharp
public static class TenantGovernanceActionStatuses
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionstatuses-approved"></a>

##### `Approved`

```csharp
const string Approved
```

The action has been approved and can be decided as allowed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionstatuses-expired"></a>

##### `Expired`

```csharp
const string Expired
```

The action is no longer within its valid time window.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionstatuses-pendingapproval"></a>

##### `PendingApproval`

```csharp
const string PendingApproval
```

The action is declared but still waiting for approval.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionstatuses-rejected"></a>

##### `Rejected`

```csharp
const string Rejected
```

The action was rejected.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionstatuses-remediated"></a>

##### `Remediated`

```csharp
const string Remediated
```

The action has been remediated and can be decided as allowed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionstatuses-remediationrequired"></a>

##### `RemediationRequired`

```csharp
const string RemediationRequired
```

The action requires remediation before it can proceed.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowcommands"></a>

### `TenantGovernanceActionWorkflowCommands`

Defines built-in tenant-governance action workflow commands.

#### Declaration
```csharp
public static class TenantGovernanceActionWorkflowCommands
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowcommands-approve"></a>

##### `Approve`

```csharp
const string Approve
```

Approves a pending tenant-governance action.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowcommands-expire"></a>

##### `Expire`

```csharp
const string Expire
```

Expires a non-terminal tenant-governance action.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowcommands-markremediated"></a>

##### `MarkRemediated`

```csharp
const string MarkRemediated
```

Marks a remediation-required tenant-governance action as remediated.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowcommands-reject"></a>

##### `Reject`

```csharp
const string Reject
```

Rejects a pending or remediation-required tenant-governance action.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowcommands-request"></a>

##### `Request`

```csharp
const string Request
```

Creates a pending tenant-governance action.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowcommands-requireremediation"></a>

##### `RequireRemediation`

```csharp
const string RequireRemediation
```

Marks a pending or approved tenant-governance action as requiring remediation.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes"></a>

### `TenantGovernanceActionWorkflowOutcomes`

Defines built-in tenant-governance action workflow transition outcomes.

#### Declaration
```csharp
public static class TenantGovernanceActionWorkflowOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-actionkindmismatch"></a>

##### `ActionKindMismatch`

```csharp
const string ActionKindMismatch
```

The matching tenant-governance action has a different action kind.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-applied"></a>

##### `Applied`

```csharp
const string Applied
```

The workflow transition updated an existing tenant-governance action.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-created"></a>

##### `Created`

```csharp
const string Created
```

The workflow transition created a new tenant-governance action.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Tenant-governance action workflow execution is disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-invalidtransition"></a>

##### `InvalidTransition`

```csharp
const string InvalidTransition
```

The requested workflow transition is not valid from the current action status.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No tenant-governance action matched the supplied identifiers.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

The requested workflow transition could not be persisted.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-subjectmismatch"></a>

##### `SubjectMismatch`

```csharp
const string SubjectMismatch
```

The matching tenant-governance action has a different subject boundary.

<a id="member-f-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

The matching tenant-governance action belongs to a different tenant.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest"></a>

### `TenantGovernanceActionWorkflowRequest`

Describes one tenant-governance action workflow transition request.

#### Declaration
```csharp
public sealed class TenantGovernanceActionWorkflowRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantGovernanceActionWorkflowRequest`

```csharp
TenantGovernanceActionWorkflowRequest(string command, string tenantId, string actionId, string actionKind, string subjectKind, string subjectId, string displayName, string actor, string reason, DateTimeOffset? atUtc, DateTimeOffset? expiresAtUtc, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-governance action workflow transition request.

Parameters:
- `command`: The workflow command to apply.
- `tenantId`: The tenant identifier to transition.
- `actionId`: The governance action identifier to transition.
- `actionKind`: The optional governance action kind.
- `subjectKind`: The optional subject kind affected by the action.
- `subjectId`: The optional subject identifier affected by the action.
- `displayName`: The optional operator-facing action name.
- `actor`: The actor that requested the workflow transition when known.
- `reason`: The optional operator-facing transition reason.
- `atUtc`: The UTC timestamp used for the transition. The runtime clock is used when omitted.
- `expiresAtUtc`: The optional UTC timestamp when the action expires.
- `correlationId`: The optional correlation identifier for the workflow transition.
- `metadata`: Optional transition metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-actionid"></a>

##### `ActionId`

```csharp
string ActionId { get; }
```

Gets the governance action identifier to transition.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-actionkind"></a>

##### `ActionKind`

```csharp
string ActionKind { get; }
```

Gets the optional governance action kind.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested the workflow transition when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for the transition.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-command"></a>

##### `Command`

```csharp
string Command { get; }
```

Gets the workflow command to apply.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the workflow transition.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing action name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp when the action expires.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional transition metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the optional operator-facing transition reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the optional subject identifier affected by the action.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-subjectkind"></a>

##### `SubjectKind`

```csharp
string SubjectKind { get; }
```

Gets the optional subject kind affected by the action.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier to transition.

<a id="type-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult"></a>

### `TenantGovernanceActionWorkflowResult`

Describes the result of one tenant-governance action workflow transition.

#### Declaration
```csharp
public sealed class TenantGovernanceActionWorkflowResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-datetimeoffset-system-string-system-string-cephalon-multitenancy-governance-services-tenantgovernanceactiondescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantGovernanceActionWorkflowResult`

```csharp
TenantGovernanceActionWorkflowResult(string tenantId, string actionId, string command, string outcome, bool applied, DateTimeOffset occurredAtUtc, string previousStatus, string currentStatus, TenantGovernanceActionDescriptor action, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-governance action workflow transition result.

Parameters:
- `tenantId`: The tenant identifier that was targeted.
- `actionId`: The governance action identifier that was targeted.
- `command`: The workflow command that was requested.
- `outcome`: The workflow transition outcome.
- `applied`: A value indicating whether the workflow transition was applied.
- `occurredAtUtc`: The UTC timestamp when the workflow transition was evaluated.
- `previousStatus`: The action status before the workflow transition when one existed.
- `currentStatus`: The action status after the workflow transition when one exists.
- `action`: The resulting action descriptor when one exists.
- `reason`: The operator-facing transition reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-action"></a>

##### `Action`

```csharp
TenantGovernanceActionDescriptor Action { get; }
```

Gets the resulting action descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-actionid"></a>

##### `ActionId`

```csharp
string ActionId { get; }
```

Gets the governance action identifier that was targeted.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-applied"></a>

##### `Applied`

```csharp
bool Applied { get; }
```

Gets a value indicating whether the workflow transition was applied.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-command"></a>

##### `Command`

```csharp
string Command { get; }
```

Gets the workflow command that was requested.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-currentstatus"></a>

##### `CurrentStatus`

```csharp
string CurrentStatus { get; }
```

Gets the action status after the workflow transition when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; }
```

Gets the UTC timestamp when the workflow transition was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the workflow transition outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-previousstatus"></a>

##### `PreviousStatus`

```csharp
string PreviousStatus { get; }
```

Gets the action status before the workflow transition when one existed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing transition reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantgovernanceactionworkflowresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was targeted.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdescriptor"></a>

### `TenantInvitationDescriptor`

Describes one invitation to join or access a tenant.

#### Declaration
```csharp
public sealed class TenantInvitationDescriptor
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDescriptor`

```csharp
TenantInvitationDescriptor(string invitationId, string tenantId, string inviteeId, string inviteeKind, string displayName, IReadOnlyList<string> roles, string status, DateTimeOffset? createdAtUtc, DateTimeOffset? expiresAtUtc, string sourceModuleId, IReadOnlyDictionary<string, string> metadata)
```

Creates a new tenant-invitation descriptor.

Parameters:
- `invitationId`: The stable invitation identifier within the tenant.
- `tenantId`: The stable tenant identifier.
- `inviteeId`: The stable invitee identifier.
- `inviteeKind`: The invitee kind, such as user, group, service, or organization.
- `displayName`: The optional operator-facing invitation name.
- `roles`: The tenant-local roles proposed by the invitation.
- `status`: The invitation status.
- `createdAtUtc`: The UTC timestamp when the invitation was created.
- `expiresAtUtc`: The UTC timestamp when the invitation expires.
- `sourceModuleId`: The module that contributed the invitation when one is known.
- `metadata`: Optional operator-facing metadata attached to the invitation.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-createdatutc"></a>

##### `CreatedAtUtc`

```csharp
DateTimeOffset? CreatedAtUtc { get; }
```

Gets the UTC timestamp when the invitation was created.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing invitation name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the UTC timestamp when the invitation expires.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the stable invitation identifier within the tenant.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-inviteeid"></a>

##### `InviteeId`

```csharp
string InviteeId { get; }
```

Gets the stable invitee identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-inviteekind"></a>

##### `InviteeKind`

```csharp
string InviteeKind { get; }
```

Gets the invitee kind, such as user, group, service, or organization.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata attached to the invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-roles"></a>

##### `Roles`

```csharp
IReadOnlyList<string> Roles { get; }
```

Gets the tenant-local roles proposed by the invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that contributed the invitation when one is known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the invitation status.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the stable tenant identifier.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationstatuses"></a>

### `TenantInvitationStatuses`

Defines stable tenant-invitation statuses understood by the governance runtime.

#### Declaration
```csharp
public static class TenantInvitationStatuses
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationstatuses-accepted"></a>

##### `Accepted`

```csharp
const string Accepted
```

The invitation has already been accepted.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationstatuses-expired"></a>

##### `Expired`

```csharp
const string Expired
```

The invitation is no longer within its valid time window.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationstatuses-pending"></a>

##### `Pending`

```csharp
const string Pending
```

The invitation can still be validated.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationstatuses-revoked"></a>

##### `Revoked`

```csharp
const string Revoked
```

The invitation has been revoked before acceptance.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes"></a>

### `TenantInvitationValidationOutcomes`

Defines stable outcomes returned by tenant-invitation validation.

#### Declaration
```csharp
public static class TenantInvitationValidationOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes-accepted"></a>

##### `Accepted`

```csharp
const string Accepted
```

The invitation has already been accepted.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Invitation validation is disabled by host configuration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes-expired"></a>

##### `Expired`

```csharp
const string Expired
```

The invitation is expired or outside its valid time window.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes-inviteemismatch"></a>

##### `InviteeMismatch`

```csharp
const string InviteeMismatch
```

The invitation exists but does not match the requested invitee boundary.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes-missingrole"></a>

##### `MissingRole`

```csharp
const string MissingRole
```

The invitation does not include every required tenant-local role.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No invitation matched the supplied tenant and invitation identifiers.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes-revoked"></a>

##### `Revoked`

```csharp
const string Revoked
```

The invitation has been revoked.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationvalidationoutcomes-valid"></a>

##### `Valid`

```csharp
const string Valid
```

The invitation is pending and satisfies the validation request.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest"></a>

### `TenantInvitationValidationRequest`

Describes one request to validate a tenant invitation.

#### Declaration
```csharp
public sealed class TenantInvitationValidationRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationValidationRequest`

```csharp
TenantInvitationValidationRequest(string tenantId, string invitationId, string inviteeId, string inviteeKind, IReadOnlyList<string> requiredRoles, DateTimeOffset? atUtc, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-invitation validation request.

Parameters:
- `tenantId`: The tenant identifier to validate.
- `invitationId`: The invitation identifier to validate.
- `inviteeId`: The optional invitee identifier expected by the caller.
- `inviteeKind`: The optional invitee kind expected by the caller.
- `requiredRoles`: The optional tenant-local roles required for validation.
- `atUtc`: The UTC timestamp used for expiration evaluation. The runtime clock is used when omitted.
- `correlationId`: The optional correlation identifier for the validation.
- `metadata`: Optional request metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for expiration evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the validation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier to validate.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-inviteeid"></a>

##### `InviteeId`

```csharp
string InviteeId { get; }
```

Gets the optional invitee identifier expected by the caller.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-inviteekind"></a>

##### `InviteeKind`

```csharp
string InviteeKind { get; }
```

Gets the invitee kind expected by the caller.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional request metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-requiredroles"></a>

##### `RequiredRoles`

```csharp
IReadOnlyList<string> RequiredRoles { get; }
```

Gets the tenant-local roles required for validation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier to validate.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult"></a>

### `TenantInvitationValidationResult`

Describes the result of one tenant-invitation validation.

#### Declaration
```csharp
public sealed class TenantInvitationValidationResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-ctor-system-string-system-string-system-string-system-boolean-system-datetimeoffset-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string"></a>

##### `TenantInvitationValidationResult`

```csharp
TenantInvitationValidationResult(string tenantId, string invitationId, string outcome, bool valid, DateTimeOffset validatedAtUtc, IReadOnlyList<string> requiredRoles, IReadOnlyList<string> matchedRoles, IReadOnlyList<string> missingRoles, TenantInvitationDescriptor matchedInvitation, string reason, IReadOnlyDictionary<string, string> metadata, string inviteeId, string inviteeKind)
```

Creates a tenant-invitation validation result.

Parameters:
- `tenantId`: The tenant identifier that was validated.
- `invitationId`: The invitation identifier that was validated.
- `outcome`: The stable validation outcome.
- `valid`: A value indicating whether validation granted invitation use.
- `validatedAtUtc`: The UTC timestamp when validation executed.
- `requiredRoles`: The tenant-local roles required by the request.
- `matchedRoles`: The tenant-local roles found on the invitation.
- `missingRoles`: The required roles that were not found.
- `matchedInvitation`: The matching invitation considered by validation.
- `reason`: The optional operator-facing validation reason.
- `metadata`: Optional result metadata.
- `inviteeId`: The optional invitee identifier expected by the request.
- `inviteeKind`: The invitee kind expected by the request.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier that was validated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-inviteeid"></a>

##### `InviteeId`

```csharp
string InviteeId { get; }
```

Gets the optional invitee identifier expected by the request.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-inviteekind"></a>

##### `InviteeKind`

```csharp
string InviteeKind { get; }
```

Gets the invitee kind expected by the request.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-matchedinvitation"></a>

##### `MatchedInvitation`

```csharp
TenantInvitationDescriptor MatchedInvitation { get; }
```

Gets the matching invitation considered by validation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-matchedroles"></a>

##### `MatchedRoles`

```csharp
IReadOnlyList<string> MatchedRoles { get; }
```

Gets the tenant-local roles found on the invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-missingroles"></a>

##### `MissingRoles`

```csharp
IReadOnlyList<string> MissingRoles { get; }
```

Gets the required roles that were not found.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable validation outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the optional operator-facing validation reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-requiredroles"></a>

##### `RequiredRoles`

```csharp
IReadOnlyList<string> RequiredRoles { get; }
```

Gets the tenant-local roles required by the request.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was validated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-valid"></a>

##### `Valid`

```csharp
bool Valid { get; }
```

Gets a value indicating whether validation granted invitation use.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationvalidationresult-validatedatutc"></a>

##### `ValidatedAtUtc`

```csharp
DateTimeOffset ValidatedAtUtc { get; }
```

Gets the UTC timestamp when validation executed.

<a id="type-cephalon-multitenancy-governance-services-tenantmembershipdescriptor"></a>

### `TenantMembershipDescriptor`

Describes one principal membership inside a tenant.

#### Declaration
```csharp
public sealed class TenantMembershipDescriptor
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantMembershipDescriptor`

```csharp
TenantMembershipDescriptor(string tenantId, string principalId, string principalKind, string displayName, IReadOnlyList<string> roles, string status, DateTimeOffset? effectiveFromUtc, DateTimeOffset? expiresAtUtc, string sourceModuleId, IReadOnlyDictionary<string, string> metadata)
```

Creates a new tenant-membership descriptor.

Parameters:
- `tenantId`: The stable tenant identifier.
- `principalId`: The stable principal identifier.
- `principalKind`: The principal kind, such as user, group, service, or organization.
- `displayName`: The optional operator-facing membership name.
- `roles`: The tenant-local roles associated with the principal.
- `status`: The membership status.
- `effectiveFromUtc`: The UTC timestamp when the membership becomes active.
- `expiresAtUtc`: The UTC timestamp when the membership expires.
- `sourceModuleId`: The module that contributed the membership when one is known.
- `metadata`: Optional operator-facing metadata attached to the membership.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing membership name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-effectivefromutc"></a>

##### `EffectiveFromUtc`

```csharp
DateTimeOffset? EffectiveFromUtc { get; }
```

Gets the UTC timestamp when the membership becomes active.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the UTC timestamp when the membership expires.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata attached to the membership.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-principalid"></a>

##### `PrincipalId`

```csharp
string PrincipalId { get; }
```

Gets the stable principal identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-principalkind"></a>

##### `PrincipalKind`

```csharp
string PrincipalKind { get; }
```

Gets the principal kind, such as user, group, service, or organization.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-roles"></a>

##### `Roles`

```csharp
IReadOnlyList<string> Roles { get; }
```

Gets the tenant-local roles associated with the principal.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that contributed the membership when one is known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the membership status.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the stable tenant identifier.

<a id="type-cephalon-multitenancy-governance-services-tenantmembershipevaluationoutcomes"></a>

### `TenantMembershipEvaluationOutcomes`

Defines stable outcomes returned by tenant-membership evaluation.

#### Declaration
```csharp
public static class TenantMembershipEvaluationOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipevaluationoutcomes-allowed"></a>

##### `Allowed`

```csharp
const string Allowed
```

Gets the outcome used when membership evaluation grants access.

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipevaluationoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Gets the outcome used when membership evaluation is disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipevaluationoutcomes-expired"></a>

##### `Expired`

```csharp
const string Expired
```

Gets the outcome used when matching memberships are expired or outside their validity window.

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipevaluationoutcomes-missingrole"></a>

##### `MissingRole`

```csharp
const string MissingRole
```

Gets the outcome used when matching memberships do not satisfy required roles.

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipevaluationoutcomes-nomembership"></a>

##### `NoMembership`

```csharp
const string NoMembership
```

Gets the outcome used when no matching membership exists.

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipevaluationoutcomes-suspended"></a>

##### `Suspended`

```csharp
const string Suspended
```

Gets the outcome used when matching memberships are suspended.

<a id="type-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest"></a>

### `TenantMembershipEvaluationRequest`

Describes one request to evaluate tenant membership.

#### Declaration
```csharp
public sealed class TenantMembershipEvaluationRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-ctor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string"></a>

##### `TenantMembershipEvaluationRequest`

```csharp
TenantMembershipEvaluationRequest(string tenantId, string principalId, IReadOnlyList<string> requiredRoles, DateTimeOffset? atUtc, string correlationId, IReadOnlyDictionary<string, string> metadata, string principalKind)
```

Creates a tenant-membership evaluation request.

Parameters:
- `tenantId`: The tenant identifier to evaluate.
- `principalId`: The principal identifier to evaluate.
- `requiredRoles`: The optional tenant-local roles required for access.
- `atUtc`: The UTC timestamp used for time-window evaluation. The runtime clock is used when omitted.
- `correlationId`: The optional correlation identifier for the evaluation.
- `metadata`: Optional request metadata.
- `principalKind`: The principal kind to evaluate. The default is user.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for time-window evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional request metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-principalid"></a>

##### `PrincipalId`

```csharp
string PrincipalId { get; }
```

Gets the principal identifier to evaluate.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-principalkind"></a>

##### `PrincipalKind`

```csharp
string PrincipalKind { get; }
```

Gets the principal kind to evaluate.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-requiredroles"></a>

##### `RequiredRoles`

```csharp
IReadOnlyList<string> RequiredRoles { get; }
```

Gets the tenant-local roles required for access.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier to evaluate.

<a id="type-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult"></a>

### `TenantMembershipEvaluationResult`

Describes the result of one tenant-membership evaluation.

#### Declaration
```csharp
public sealed class TenantMembershipEvaluationResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-ctor-system-string-system-string-system-string-system-boolean-system-datetimeoffset-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string"></a>

##### `TenantMembershipEvaluationResult`

```csharp
TenantMembershipEvaluationResult(string tenantId, string principalId, string outcome, bool allowed, DateTimeOffset evaluatedAtUtc, IReadOnlyList<string> requiredRoles, IReadOnlyList<string> matchedRoles, IReadOnlyList<string> missingRoles, IReadOnlyList<TenantMembershipDescriptor> matchedMemberships, string reason, IReadOnlyDictionary<string, string> metadata, string principalKind)
```

Creates a tenant-membership evaluation result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `principalId`: The principal identifier that was evaluated.
- `outcome`: The stable evaluation outcome.
- `allowed`: A value indicating whether evaluation granted access.
- `evaluatedAtUtc`: The UTC timestamp when evaluation executed.
- `requiredRoles`: The tenant-local roles required by the request.
- `matchedRoles`: The tenant-local roles found on active memberships.
- `missingRoles`: The required roles that were not found.
- `matchedMemberships`: The matching memberships considered by evaluation.
- `reason`: The optional operator-facing evaluation reason.
- `metadata`: Optional result metadata.
- `principalKind`: The principal kind that was evaluated. The default is user.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-allowed"></a>

##### `Allowed`

```csharp
bool Allowed { get; }
```

Gets a value indicating whether evaluation granted access.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-evaluatedatutc"></a>

##### `EvaluatedAtUtc`

```csharp
DateTimeOffset EvaluatedAtUtc { get; }
```

Gets the UTC timestamp when evaluation executed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-matchedmemberships"></a>

##### `MatchedMemberships`

```csharp
IReadOnlyList<TenantMembershipDescriptor> MatchedMemberships { get; }
```

Gets the matching memberships considered by evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-matchedroles"></a>

##### `MatchedRoles`

```csharp
IReadOnlyList<string> MatchedRoles { get; }
```

Gets the tenant-local roles found on active memberships.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-missingroles"></a>

##### `MissingRoles`

```csharp
IReadOnlyList<string> MissingRoles { get; }
```

Gets the required roles that were not found.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable evaluation outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-principalid"></a>

##### `PrincipalId`

```csharp
string PrincipalId { get; }
```

Gets the principal identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-principalkind"></a>

##### `PrincipalKind`

```csharp
string PrincipalKind { get; }
```

Gets the principal kind that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the optional operator-facing evaluation reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-requiredroles"></a>

##### `RequiredRoles`

```csharp
IReadOnlyList<string> RequiredRoles { get; }
```

Gets the tenant-local roles required by the request.

<a id="member-p-cephalon-multitenancy-governance-services-tenantmembershipevaluationresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="type-cephalon-multitenancy-governance-services-tenantmembershipstatuses"></a>

### `TenantMembershipStatuses`

Defines stable tenant-membership status identifiers.

#### Declaration
```csharp
public static class TenantMembershipStatuses
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipstatuses-active"></a>

##### `Active`

```csharp
const string Active
```

Gets the status used when the membership can participate in evaluation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipstatuses-expired"></a>

##### `Expired`

```csharp
const string Expired
```

Gets the status used when the membership has intentionally expired.

<a id="member-f-cephalon-multitenancy-governance-services-tenantmembershipstatuses-suspended"></a>

##### `Suspended`

```csharp
const string Suspended
```

Gets the status used when the membership exists but is blocked from evaluation.
