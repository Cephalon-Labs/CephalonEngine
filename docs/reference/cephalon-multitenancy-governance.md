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

Remarks: These options seed the host-owned membership baseline. Installed modules can still contribute additional memberships through `ITenantMembershipContributor`.

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

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitations"></a>

##### `Invitations`

```csharp
IList<TenantInvitationDescriptor> Invitations { get; }
```

Gets the host-defined tenant invitations available to the governance runtime.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-memberships"></a>

##### `Memberships`

```csharp
IList<TenantMembershipDescriptor> Memberships { get; }
```

Gets the host-defined tenant memberships available to the governance runtime.

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

Gets the effective invitation set after host options and module contributors have both been applied.

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

Gets the effective membership set after host options and module contributors have both been applied.

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
