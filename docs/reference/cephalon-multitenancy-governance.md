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

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-allowinsecuredomainownershiphttpproofcollection"></a>

##### `AllowInsecureDomainOwnershipHttpProofCollection`

```csharp
bool AllowInsecureDomainOwnershipHttpProofCollection { get; set; }
```

Gets or sets a value indicating whether HTTP proof collection may use non-HTTPS URLs.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipdnstxtproofcollectionmaxresponsebytes"></a>

##### `DomainOwnershipDnsTxtProofCollectionMaxResponseBytes`

```csharp
int DomainOwnershipDnsTxtProofCollectionMaxResponseBytes { get; set; }
```

Gets or sets the maximum response body size, in bytes, accepted by DNS TXT proof collection.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipdnstxtproofcollectiontimeoutseconds"></a>

##### `DomainOwnershipDnsTxtProofCollectionTimeoutSeconds`

```csharp
int DomainOwnershipDnsTxtProofCollectionTimeoutSeconds { get; set; }
```

Gets or sets the default timeout, in seconds, used by DNS TXT proof collection.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipdnstxtproofresolverendpoint"></a>

##### `DomainOwnershipDnsTxtProofResolverEndpoint`

```csharp
Uri DomainOwnershipDnsTxtProofResolverEndpoint { get; set; }
```

Gets or sets the optional DNS-over-HTTPS resolver endpoint used by DNS TXT proof collection.

Remarks: When omitted, callers can still provide a per-request resolver endpoint. Cephalon does not use a hidden public resolver by default.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershiphttpproofcollectionmaxresponsebytes"></a>

##### `DomainOwnershipHttpProofCollectionMaxResponseBytes`

```csharp
int DomainOwnershipHttpProofCollectionMaxResponseBytes { get; set; }
```

Gets or sets the maximum response body size, in bytes, accepted by HTTP proof collection.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershiphttpproofcollectiontimeoutseconds"></a>

##### `DomainOwnershipHttpProofCollectionTimeoutSeconds`

```csharp
int DomainOwnershipHttpProofCollectionTimeoutSeconds { get; set; }
```

Gets or sets the default timeout, in seconds, used by HTTP proof collection.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipproofbackgroundpollingintervalseconds"></a>

##### `DomainOwnershipProofBackgroundPollingIntervalSeconds`

```csharp
int DomainOwnershipProofBackgroundPollingIntervalSeconds { get; set; }
```

Gets or sets the proof background polling interval, in seconds.

Remarks: Values less than one are coerced to the default interval.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipproofbackgroundpollingrunonstartup"></a>

##### `DomainOwnershipProofBackgroundPollingRunOnStartup`

```csharp
bool DomainOwnershipProofBackgroundPollingRunOnStartup { get; set; }
```

Gets or sets a value indicating whether proof background polling should run once during hosted-service startup.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipproofbackgroundpollingsource"></a>

##### `DomainOwnershipProofBackgroundPollingSource`

```csharp
string DomainOwnershipProofBackgroundPollingSource { get; set; }
```

Gets or sets the source recorded on proof polling requests created by the background polling hosted service.

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

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-domainownershipproofpollingmaxitems"></a>

##### `DomainOwnershipProofPollingMaxItems`

```csharp
int DomainOwnershipProofPollingMaxItems { get; set; }
```

Gets or sets the default maximum number of tenant-domain ownership declarations polled in one runner pass.

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

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipdnstxtproofcollection"></a>

##### `EnableDomainOwnershipDnsTxtProofCollection`

```csharp
bool EnableDomainOwnershipDnsTxtProofCollection { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership DNS TXT proof collector is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershiphttpproofcollection"></a>

##### `EnableDomainOwnershipHttpProofCollection`

```csharp
bool EnableDomainOwnershipHttpProofCollection { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership HTTP proof collector is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershiphttpproofpublication"></a>

##### `EnableDomainOwnershipHttpProofPublication`

```csharp
bool EnableDomainOwnershipHttpProofPublication { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership HTTP proof publisher is active.

Remarks: The governance package materializes and records HTTP proof-file publication state. It does not map an ASP.NET Core endpoint by itself; HTTP serving stays in the ASP.NET Core adapter so the core package remains host-agnostic. DNS records and provider control-plane mutations remain outside this option.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipproofbackgroundpolling"></a>

##### `EnableDomainOwnershipProofBackgroundPolling`

```csharp
bool EnableDomainOwnershipProofBackgroundPolling { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership proof polling hosted service is active.

Remarks: This option is disabled by default so installing the governance package never starts recurring HTTP or DNS proof checks without an explicit host decision. When enabled, the hosted service schedules the bounded proof polling runner; it still does not publish DNS records, host HTTP proof files, or mutate provider control planes.

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

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipproofpollingrunner"></a>

##### `EnableDomainOwnershipProofPollingRunner`

```csharp
bool EnableDomainOwnershipProofPollingRunner { get; set; }
```

Gets or sets a value indicating whether the built-in bounded tenant-domain ownership proof polling runner is active.

Remarks: The polling runner owns one on-demand scan over pending or rejected declarations and delegates each attempt to the proof verification runner. It does not schedule background polling or publish DNS/HTTP proof values.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipproofpublicationplanning"></a>

##### `EnableDomainOwnershipProofPublicationPlanning`

```csharp
bool EnableDomainOwnershipProofPublicationPlanning { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership proof publication planner is active.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabledomainownershipproofverificationrunner"></a>

##### `EnableDomainOwnershipProofVerificationRunner`

```csharp
bool EnableDomainOwnershipProofVerificationRunner { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-domain ownership proof verification runner is active.

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

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enableinvitationdeliverydispatch"></a>

##### `EnableInvitationDeliveryDispatch`

```csharp
bool EnableInvitationDeliveryDispatch { get; set; }
```

Gets or sets a value indicating whether the built-in invitation delivery dispatcher is active.

Remarks: The dispatcher owns invitation lookup, pending/expiry checks, runtime reporting, and outcome persistence. It requires a registered `ITenantInvitationDeliverySender` before any external delivery can happen.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enableinvitationdeliveryretrybackgroundscheduling"></a>

##### `EnableInvitationDeliveryRetryBackgroundScheduling`

```csharp
bool EnableInvitationDeliveryRetryBackgroundScheduling { get; set; }
```

Gets or sets a value indicating whether the built-in invitation delivery retry hosted service is active.

Remarks: This option is disabled by default so installing the governance package never starts recurring delivery attempts without an explicit host decision. When enabled, the hosted service schedules the bounded retry runner; it still does not provide distributed queues, cross-node leases, exactly-once delivery, or provider-specific senders.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enableinvitationdeliveryretryqueue"></a>

##### `EnableInvitationDeliveryRetryQueue`

```csharp
bool EnableInvitationDeliveryRetryQueue { get; set; }
```

Gets or sets a value indicating whether sender-failed invitation delivery attempts are queued for explicit retry.

Remarks: This queue is enabled deliberately because it can cause later delivery attempts. It stores retry intent and exposes a bounded manual runner; it does not start background delivery unless retry background scheduling is explicitly enabled, provide distributed leases, or guarantee exactly-once delivery.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enableinvitationdeliverystatusobservationstore"></a>

##### `EnableInvitationDeliveryStatusObservationStore`

```csharp
bool EnableInvitationDeliveryStatusObservationStore { get; set; }
```

Gets or sets a value indicating whether delivery status reconciliation observations are recorded.

Remarks: Observation storage records normalized reconciliation outcomes for audit and operator review. It does not provide provider-specific callback translation, provider polling, cross-node replay protection, or distributed exactly-once delivery.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enableinvitationdeliverystatusreconciliation"></a>

##### `EnableInvitationDeliveryStatusReconciliation`

```csharp
bool EnableInvitationDeliveryStatusReconciliation { get; set; }
```

Gets or sets a value indicating whether the built-in invitation delivery status reconciler is active.

Remarks: The reconciler owns host-agnostic status matching, metadata normalization, and persistence after a provider or receiver reports delivery status. It does not map webhooks or poll provider APIs by itself.

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

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-enabletenantadministrationworkflow"></a>

##### `EnableTenantAdministrationWorkflow`

```csharp
bool EnableTenantAdministrationWorkflow { get; set; }
```

Gets or sets a value indicating whether the built-in tenant-administration workflow executor is active.

Remarks: The workflow mutates Cephalon-managed membership and invitation stores through explicit host-driven commands. It does not provide public onboarding screens, tenant-admin HTTP endpoints, provider-specific delivery senders, or identity-provider sync.

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

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliveryretrybackgroundintervalseconds"></a>

##### `InvitationDeliveryRetryBackgroundIntervalSeconds`

```csharp
int InvitationDeliveryRetryBackgroundIntervalSeconds { get; set; }
```

Gets or sets the invitation delivery retry background scheduling interval, in seconds.

Remarks: Values less than one are coerced to the default interval.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliveryretrybackgroundrunonstartup"></a>

##### `InvitationDeliveryRetryBackgroundRunOnStartup`

```csharp
bool InvitationDeliveryRetryBackgroundRunOnStartup { get; set; }
```

Gets or sets a value indicating whether invitation delivery retry background scheduling should run once during hosted-service startup.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliveryretrybackgroundsource"></a>

##### `InvitationDeliveryRetryBackgroundSource`

```csharp
string InvitationDeliveryRetryBackgroundSource { get; set; }
```

Gets or sets the source recorded on retry requests created by the background retry hosted service.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliveryretrydelayseconds"></a>

##### `InvitationDeliveryRetryDelaySeconds`

```csharp
int InvitationDeliveryRetryDelaySeconds { get; set; }
```

Gets or sets the delay, in seconds, before a failed retry entry is due again.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliveryretrymaxattempts"></a>

##### `InvitationDeliveryRetryMaxAttempts`

```csharp
int InvitationDeliveryRetryMaxAttempts { get; set; }
```

Gets or sets the maximum dispatch attempts retained for one retry entry, including the original failed attempt.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliveryretrymaxitems"></a>

##### `InvitationDeliveryRetryMaxItems`

```csharp
int InvitationDeliveryRetryMaxItems { get; set; }
```

Gets or sets the default maximum number of retry entries attempted by one retry runner pass.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliveryretryqueuefilepath"></a>

##### `InvitationDeliveryRetryQueueFilePath`

```csharp
string InvitationDeliveryRetryQueueFilePath { get; set; }
```

Gets or sets the optional JSON file path used for Cephalon-managed durable invitation delivery retry entries.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliveryrunhistorylimit"></a>

##### `InvitationDeliveryRunHistoryLimit`

```csharp
int InvitationDeliveryRunHistoryLimit { get; set; }
```

Gets or sets the maximum number of invitation delivery dispatch attempts retained in the runtime catalog.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliverystatusobservationhistorylimit"></a>

##### `InvitationDeliveryStatusObservationHistoryLimit`

```csharp
int InvitationDeliveryStatusObservationHistoryLimit { get; set; }
```

Gets or sets the maximum number of delivery status observations retained by the built-in observation store.

<a id="member-p-cephalon-multitenancy-governance-configuration-multitenancygovernanceoptions-invitationdeliverystatusobservationstorefilepath"></a>

##### `InvitationDeliveryStatusObservationStoreFilePath`

```csharp
string InvitationDeliveryStatusObservationStoreFilePath { get; set; }
```

Gets or sets the optional JSON file path used for Cephalon-managed durable delivery status observations.

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

<a id="type-cephalon-multitenancy-governance-services-itenantadministrationworkflow"></a>

### `ITenantAdministrationWorkflow`

Applies host-agnostic tenant administration commands over Cephalon-managed governance stores.

#### Declaration
```csharp
public interface ITenantAdministrationWorkflow
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantadministrationworkflow-applyasync-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-system-threading-cancellationtoken"></a>

##### `ApplyAsync`

```csharp
ValueTask<TenantAdministrationWorkflowResult> ApplyAsync(TenantAdministrationWorkflowRequest request, CancellationToken cancellationToken)
```

Applies one tenant administration command.

Returns: The evaluated tenant administration command result.

Parameters:
- `request`: The tenant administration command request.
- `cancellationToken`: A token that cancels the command.

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

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipdnstxtproofcollector"></a>

### `ITenantDomainOwnershipDnsTxtProofCollector`

Collects tenant-domain ownership DNS TXT proof evidence and evaluates the collected proof through the governance workflow.

Remarks: The collector owns the on-demand DNS TXT proof lookup path for declarations that use `DnsTxt`. It does not mutate DNS provider records, publish proof values, or run background polling.

#### Declaration
```csharp
public interface ITenantDomainOwnershipDnsTxtProofCollector
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipdnstxtproofcollector-collectasync-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-system-threading-cancellationtoken"></a>

##### `CollectAsync`

```csharp
ValueTask<TenantDomainOwnershipDnsTxtProofCollectionResult> CollectAsync(TenantDomainOwnershipDnsTxtProofCollectionRequest request, CancellationToken cancellationToken)
```

Collects and evaluates one tenant-domain ownership DNS TXT proof.

Returns: The collection result and nested proof-evaluation outcome.

Parameters:
- `request`: The DNS TXT proof collection request.
- `cancellationToken`: A token that cancels collection before the proof evaluator is invoked.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershiphttpproofcollector"></a>

### `ITenantDomainOwnershipHttpProofCollector`

Collects tenant-domain ownership HTTP file proof evidence and evaluates the collected proof through the governance workflow.

Remarks: The collector owns the on-demand HTTP file proof collection path for declarations that use `HttpFile`. It does not publish the proof file, mutate DNS records, collect DNS TXT values, or run background polling.

#### Declaration
```csharp
public interface ITenantDomainOwnershipHttpProofCollector
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershiphttpproofcollector-collectasync-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-system-threading-cancellationtoken"></a>

##### `CollectAsync`

```csharp
ValueTask<TenantDomainOwnershipHttpProofCollectionResult> CollectAsync(TenantDomainOwnershipHttpProofCollectionRequest request, CancellationToken cancellationToken)
```

Collects and evaluates one tenant-domain ownership HTTP file proof.

Returns: The collection result and nested proof-evaluation outcome.

Parameters:
- `request`: The HTTP proof collection request.
- `cancellationToken`: A token that cancels collection before the proof evaluator is invoked.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershiphttpproofpublicationcatalog"></a>

### `ITenantDomainOwnershipHttpProofPublicationCatalog`

Exposes tenant-domain ownership HTTP proof files materialized by the governance companion pack.

#### Declaration
```csharp
public interface ITenantDomainOwnershipHttpProofPublicationCatalog
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantdomainownershiphttpproofpublicationcatalog-publishedproofs"></a>

##### `PublishedProofs`

```csharp
IReadOnlyList<TenantDomainOwnershipHttpProofPublicationDescriptor> PublishedProofs { get; }
```

Gets the HTTP proof files currently published by the governance companion pack.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershiphttpproofpublicationcatalog-getbyhostandpath-system-string-system-string"></a>

##### `GetByHostAndPath`

```csharp
TenantDomainOwnershipHttpProofPublicationDescriptor GetByHostAndPath(string hostName, string httpFilePath)
```

Finds a published HTTP proof file by request host and path.

Returns: The matching published HTTP proof file, or `null` when no published proof matches.

Parameters:
- `hostName`: The request host name without a URI scheme.
- `httpFilePath`: The HTTP path requested by the client.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershiphttpproofpublisher"></a>

### `ITenantDomainOwnershipHttpProofPublisher`

Publishes tenant-domain ownership HTTP proof-file state inside the governance companion pack.

Remarks: The publisher records the proof file path, content type, and fingerprint so host adapters can serve the proof through their own transport-specific endpoints. It does not mutate DNS records or external provider control planes.

#### Declaration
```csharp
public interface ITenantDomainOwnershipHttpProofPublisher
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershiphttpproofpublisher-publishasync-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-system-threading-cancellationtoken"></a>

##### `PublishAsync`

```csharp
ValueTask<TenantDomainOwnershipHttpProofPublicationResult> PublishAsync(TenantDomainOwnershipHttpProofPublicationRequest request, CancellationToken cancellationToken)
```

Materializes an HTTP proof-file publication from an issued tenant-domain ownership proof challenge.

Returns: The HTTP proof publication outcome.

Parameters:
- `request`: The HTTP proof publication request.
- `cancellationToken`: A token that cancels publication before runtime state is stored.

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

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipproofpollingrunner"></a>

### `ITenantDomainOwnershipProofPollingRunner`

Runs bounded tenant-domain ownership proof polling over the governance domain-ownership catalog.

Remarks: The polling runner reduces application glue code by selecting pending or rejected domain-ownership declarations and delegating each proof attempt to `ITenantDomainOwnershipProofVerificationRunner`. It owns the on-demand polling loop, not DNS mutation, HTTP file hosting, provider control-plane mutation, or automatic background scheduling.

#### Declaration
```csharp
public interface ITenantDomainOwnershipProofPollingRunner
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipproofpollingrunner-pollasync-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-system-threading-cancellationtoken"></a>

##### `PollAsync`

```csharp
ValueTask<TenantDomainOwnershipProofPollingResult> PollAsync(TenantDomainOwnershipProofPollingRequest request, CancellationToken cancellationToken)
```

Runs one bounded polling pass over matching tenant-domain ownership declarations.

Returns: The aggregate polling result plus the nested verification attempts.

Parameters:
- `request`: The proof polling request.
- `cancellationToken`: A token that cancels the polling pass.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipproofpollingruntimecatalog"></a>

### `ITenantDomainOwnershipProofPollingRuntimeCatalog`

Exposes runtime state for tenant-domain ownership proof polling.

Remarks: The catalog reports the background polling hosted-service posture. It does not represent DNS or HTTP proof publication ownership.

#### Declaration
```csharp
public interface ITenantDomainOwnershipProofPollingRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantdomainownershipproofpollingruntimecatalog-current"></a>

##### `Current`

```csharp
TenantDomainOwnershipProofPollingRuntimeSnapshot Current { get; }
```

Gets the latest tenant-domain ownership proof polling runtime snapshot.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipproofpublicationplanner"></a>

### `ITenantDomainOwnershipProofPublicationPlanner`

Builds tenant-domain ownership proof publication instructions from an issued proof challenge.

Remarks: The planner owns deterministic instruction generation and optional runtime metadata recording. It does not mutate DNS records, host HTTP proof files, or poll external endpoints; applications or provider packs publish and observe the planned proof outside the engine.

#### Declaration
```csharp
public interface ITenantDomainOwnershipProofPublicationPlanner
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipproofpublicationplanner-planasync-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-system-threading-cancellationtoken"></a>

##### `PlanAsync`

```csharp
ValueTask<TenantDomainOwnershipProofPublicationPlanResult> PlanAsync(TenantDomainOwnershipProofPublicationPlanRequest request, CancellationToken cancellationToken)
```

Builds publication instructions for a tenant-domain ownership proof challenge.

Returns: The publication instructions and runtime state outcome.

Parameters:
- `request`: The proof publication planning request.
- `cancellationToken`: A token that cancels planning before runtime state is stored.

<a id="type-cephalon-multitenancy-governance-services-itenantdomainownershipproofverificationrunner"></a>

### `ITenantDomainOwnershipProofVerificationRunner`

Runs the built-in tenant-domain ownership proof verification flow.

Remarks: The runner reduces application glue code by composing challenge issuance, publication planning, reported-proof evaluation, and optional HTTP file proof collection and configured DNS TXT proof collection without claiming DNS mutation, HTTP file hosting, provider control-plane mutation, or automatic background polling ownership.

#### Declaration
```csharp
public interface ITenantDomainOwnershipProofVerificationRunner
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantdomainownershipproofverificationrunner-verifyasync-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-system-threading-cancellationtoken"></a>

##### `VerifyAsync`

```csharp
ValueTask<TenantDomainOwnershipProofVerificationResult> VerifyAsync(TenantDomainOwnershipProofVerificationRequest request, CancellationToken cancellationToken)
```

Runs one tenant-domain ownership proof verification attempt.

Returns: The proof verification outcome and nested runtime results.

Parameters:
- `request`: The proof verification request.
- `cancellationToken`: A token that cancels the attempt.

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

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationdeliverydispatcher"></a>

### `ITenantInvitationDeliveryDispatcher`

Dispatches tenant invitation delivery through a registered sender and records delivery outcome metadata.

Remarks: The dispatcher owns host-agnostic lookup, validation, runtime reporting, and outcome persistence. Actual transport delivery, such as email, SMS, chat, or an identity-provider invite, is supplied by `ITenantInvitationDeliverySender` implementations registered by the host or an optional provider companion package.

#### Declaration
```csharp
public interface ITenantInvitationDeliveryDispatcher
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliverydispatcher-dispatchasync-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-system-threading-cancellationtoken"></a>

##### `DispatchAsync`

```csharp
ValueTask<TenantInvitationDeliveryResult> DispatchAsync(TenantInvitationDeliveryRequest request, CancellationToken cancellationToken)
```

Dispatches one tenant invitation delivery request.

Returns: The dispatch outcome.

Parameters:
- `request`: The tenant invitation delivery request.
- `cancellationToken`: A token that cancels dispatch before a sender is invoked or state is stored.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretryrunner"></a>

### `ITenantInvitationDeliveryRetryRunner`

Runs bounded retries for queued tenant invitation delivery failures.

#### Declaration
```csharp
public interface ITenantInvitationDeliveryRetryRunner
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretryrunner-retrypendingasync-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-system-threading-cancellationtoken"></a>

##### `RetryPendingAsync`

```csharp
ValueTask<TenantInvitationDeliveryRetryResult> RetryPendingAsync(TenantInvitationDeliveryRetryRequest request, CancellationToken cancellationToken)
```

Retries pending tenant invitation delivery queue entries.

Returns: The aggregate retry pass result.

Parameters:
- `request`: The retry runner request.
- `cancellationToken`: A token that cancels the retry pass.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretryruntimecatalog"></a>

### `ITenantInvitationDeliveryRetryRuntimeCatalog`

Exposes runtime state for automatic tenant-invitation delivery retry scheduling.

Remarks: The catalog reports the opt-in background retry hosted-service posture. It does not represent distributed retry leases, cross-node exactly-once delivery, or provider-specific sender ownership.

#### Declaration
```csharp
public interface ITenantInvitationDeliveryRetryRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretryruntimecatalog-current"></a>

##### `Current`

```csharp
TenantInvitationDeliveryRetryRuntimeSnapshot Current { get; }
```

Gets the latest tenant-invitation delivery retry scheduling runtime snapshot.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore"></a>

### `ITenantInvitationDeliveryRetryStore`

Stores tenant invitation delivery retry entries managed by the governance companion pack.

#### Declaration
```csharp
public interface ITenantInvitationDeliveryRetryStore
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-count"></a>

##### `Count`

```csharp
int Count { get; }
```

Gets the number of retained retry entries.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-entries"></a>

##### `Entries`

```csharp
IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> Entries { get; }
```

Gets every retained retry entry.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-isdurable"></a>

##### `IsDurable`

```csharp
bool IsDurable { get; }
```

Gets a value indicating whether retry entries survive process restarts.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-latestentry"></a>

##### `LatestEntry`

```csharp
TenantInvitationDeliveryRetryDescriptor LatestEntry { get; }
```

Gets the latest retained retry entry when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; }
```

Gets the runtime ownership label for the retry queue.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-storekind"></a>

##### `StoreKind`

```csharp
string StoreKind { get; }
```

Gets the storage kind used by the retry queue.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-getbyid-system-string"></a>

##### `GetById`

```csharp
TenantInvitationDeliveryRetryDescriptor GetById(string retryId)
```

Gets one retry entry by identifier.

Returns: The matching retry entry, or `null` when none exists.

Parameters:
- `retryId`: The retry entry identifier.

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-getpending-system-datetimeoffset-system-int32-system-boolean"></a>

##### `GetPending`

```csharp
IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> GetPending(DateTimeOffset atUtc, int limit, bool dueOnly)
```

Gets retry entries that are pending and optionally due at or before the supplied timestamp.

Returns: The matching pending retry entries.

Parameters:
- `atUtc`: The timestamp used to decide due entries.
- `limit`: The maximum number of entries to return.
- `dueOnly`: A value indicating whether entries scheduled after `atUtc` should be skipped.

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-remove-system-string"></a>

##### `Remove`

```csharp
bool Remove(string retryId)
```

Removes a retry entry.

Returns: `true` when an entry was removed; otherwise `false`.

Parameters:
- `retryId`: The retry entry identifier.

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliveryretrystore-upsert-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor"></a>

##### `Upsert`

```csharp
void Upsert(TenantInvitationDeliveryRetryDescriptor entry)
```

Adds or replaces a retry entry.

Parameters:
- `entry`: The retry entry to store.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationdeliveryruncatalog"></a>

### `ITenantInvitationDeliveryRunCatalog`

Exposes runtime tenant invitation delivery dispatch attempts observed by the governance companion pack.

#### Declaration
```csharp
public interface ITenantInvitationDeliveryRunCatalog
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryruncatalog-count"></a>

##### `Count`

```csharp
int Count { get; }
```

Gets the number of recorded tenant invitation delivery dispatch attempts.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryruncatalog-latestrun"></a>

##### `LatestRun`

```csharp
TenantInvitationDeliveryRunDescriptor LatestRun { get; }
```

Gets the latest recorded tenant invitation delivery dispatch attempt when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliveryruncatalog-runs"></a>

##### `Runs`

```csharp
IReadOnlyList<TenantInvitationDeliveryRunDescriptor> Runs { get; }
```

Gets the recorded tenant invitation delivery dispatch attempts.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliveryruncatalog-getbyinvitationid-system-string"></a>

##### `GetByInvitationId`

```csharp
IReadOnlyList<TenantInvitationDeliveryRunDescriptor> GetByInvitationId(string invitationId)
```

Gets recorded tenant invitation delivery dispatch attempts for one invitation identifier.

Returns: The matching dispatch attempts.

Parameters:
- `invitationId`: The invitation identifier.

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliveryruncatalog-getbytenantid-system-string"></a>

##### `GetByTenantId`

```csharp
IReadOnlyList<TenantInvitationDeliveryRunDescriptor> GetByTenantId(string tenantId)
```

Gets recorded tenant invitation delivery dispatch attempts for one tenant.

Returns: The matching dispatch attempts.

Parameters:
- `tenantId`: The tenant identifier.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationdeliverysender"></a>

### `ITenantInvitationDeliverySender`

Sends tenant invitation delivery payloads for a host or provider-specific channel.

Remarks: Sender implementations own external delivery behavior and provider semantics. Cephalon calls the sender only after resolving a pending invitation and records the returned outcome without assuming that every provider can guarantee final recipient delivery.

#### Declaration
```csharp
public interface ITenantInvitationDeliverySender
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliverysender-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; }
```

Gets the stable sender identifier used by configuration, runtime metadata, and diagnostics.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliverysender-sendasync-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-system-threading-cancellationtoken"></a>

##### `SendAsync`

```csharp
ValueTask<TenantInvitationDeliverySenderResult> SendAsync(TenantInvitationDeliveryContext context, CancellationToken cancellationToken)
```

Sends or queues one tenant invitation delivery payload.

Returns: The provider-specific sender outcome normalized for Cephalon runtime reporting.

Parameters:
- `context`: The delivery context resolved by the governance companion pack.
- `cancellationToken`: A token that cancels sender execution.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusobservationstore"></a>

### `ITenantInvitationDeliveryStatusObservationStore`

Stores tenant invitation delivery status observations recorded by the governance reconciler.

Remarks: The store is host-agnostic and records normalized reconciliation observations only. It does not translate provider-specific callback payloads, verify provider-specific signatures, poll delivery providers, or provide distributed exactly-once delivery semantics.

#### Declaration
```csharp
public interface ITenantInvitationDeliveryStatusObservationStore
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusobservationstore-count"></a>

##### `Count`

```csharp
int Count { get; }
```

Gets the number of recorded delivery status observations.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusobservationstore-isdurable"></a>

##### `IsDurable`

```csharp
bool IsDurable { get; }
```

Gets a value indicating whether the observation store survives process restarts.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusobservationstore-observations"></a>

##### `Observations`

```csharp
IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> Observations { get; }
```

Gets the recorded delivery status observations.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusobservationstore-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; }
```

Gets the ownership mode for the observation store.

<a id="member-p-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusobservationstore-storekind"></a>

##### `StoreKind`

```csharp
string StoreKind { get; }
```

Gets the store kind, such as `in-memory` or `file`.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusobservationstore-upsert-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor"></a>

##### `Upsert`

```csharp
void Upsert(TenantInvitationDeliveryStatusObservationDescriptor observation)
```

Records or replaces a delivery status observation.

Parameters:
- `observation`: The observation to record.

<a id="type-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusreconciler"></a>

### `ITenantInvitationDeliveryStatusReconciler`

Reconciles provider or receiver delivery status observations into tenant invitation runtime metadata.

#### Declaration
```csharp
public interface ITenantInvitationDeliveryStatusReconciler
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-itenantinvitationdeliverystatusreconciler-reconcileasync-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-system-threading-cancellationtoken"></a>

##### `ReconcileAsync`

```csharp
ValueTask<TenantInvitationDeliveryStatusReconciliationResult> ReconcileAsync(TenantInvitationDeliveryStatusReconciliationRequest request, CancellationToken cancellationToken)
```

Reconciles one delivery status observation for a tenant invitation.

Returns: The reconciliation result.

Parameters:
- `request`: The delivery status reconciliation request.
- `cancellationToken`: The token used to cancel reconciliation.

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

<a id="type-cephalon-multitenancy-governance-services-tenantadministrationworkflowcommands"></a>

### `TenantAdministrationWorkflowCommands`

Defines stable tenant-administration workflow command identifiers.

#### Declaration
```csharp
public static class TenantAdministrationWorkflowCommands
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowcommands-acceptinvitation"></a>

##### `AcceptInvitation`

```csharp
const string AcceptInvitation
```

Accepts an existing pending tenant invitation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowcommands-expireinvitation"></a>

##### `ExpireInvitation`

```csharp
const string ExpireInvitation
```

Expires an existing tenant invitation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowcommands-expiremembership"></a>

##### `ExpireMembership`

```csharp
const string ExpireMembership
```

Expires an existing tenant membership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowcommands-grantmembership"></a>

##### `GrantMembership`

```csharp
const string GrantMembership
```

Creates or replaces an active tenant membership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowcommands-issueinvitation"></a>

##### `IssueInvitation`

```csharp
const string IssueInvitation
```

Creates or replaces a pending tenant invitation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowcommands-revokeinvitation"></a>

##### `RevokeInvitation`

```csharp
const string RevokeInvitation
```

Revokes an existing pending tenant invitation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowcommands-suspendmembership"></a>

##### `SuspendMembership`

```csharp
const string SuspendMembership
```

Suspends an existing tenant membership.

<a id="type-cephalon-multitenancy-governance-services-tenantadministrationworkflowmetadatakeys"></a>

### `TenantAdministrationWorkflowMetadataKeys`

Defines stable metadata keys written by tenant-administration workflow commands.

#### Declaration
```csharp
public static class TenantAdministrationWorkflowMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowmetadatakeys-administrationworkflowownership"></a>

##### `AdministrationWorkflowOwnership`

```csharp
const string AdministrationWorkflowOwnership
```

Metadata key describing who owns the tenant-administration workflow that wrote the descriptor.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowmetadatakeys-lastadministrationactor"></a>

##### `LastAdministrationActor`

```csharp
const string LastAdministrationActor
```

Metadata key containing the actor that requested the last tenant-administration command.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowmetadatakeys-lastadministrationcommand"></a>

##### `LastAdministrationCommand`

```csharp
const string LastAdministrationCommand
```

Metadata key containing the last tenant-administration command.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowmetadatakeys-lastadministrationcorrelationid"></a>

##### `LastAdministrationCorrelationId`

```csharp
const string LastAdministrationCorrelationId
```

Metadata key containing the correlation identifier for the last tenant-administration command.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowmetadatakeys-lastadministrationoccurredatutc"></a>

##### `LastAdministrationOccurredAtUtc`

```csharp
const string LastAdministrationOccurredAtUtc
```

Metadata key containing the UTC timestamp when the last tenant-administration command was evaluated.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowmetadatakeys-lastadministrationoutcome"></a>

##### `LastAdministrationOutcome`

```csharp
const string LastAdministrationOutcome
```

Metadata key containing the last tenant-administration command outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowmetadatakeys-lastadministrationreason"></a>

##### `LastAdministrationReason`

```csharp
const string LastAdministrationReason
```

Metadata key containing the operator-facing reason for the last tenant-administration command.

<a id="type-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes"></a>

### `TenantAdministrationWorkflowOutcomes`

Defines stable tenant-administration workflow outcomes.

#### Declaration
```csharp
public static class TenantAdministrationWorkflowOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes-applied"></a>

##### `Applied`

```csharp
const string Applied
```

The requested tenant-administration command was applied.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Tenant-administration workflow execution is disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes-invalidinvitationstate"></a>

##### `InvalidInvitationState`

```csharp
const string InvalidInvitationState
```

An invitation command targeted an invitation whose state cannot transition through the requested command.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes-invitationnotfound"></a>

##### `InvitationNotFound`

```csharp
const string InvitationNotFound
```

An invitation command targeted an invitation that does not exist.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes-invitationtargetrequired"></a>

##### `InvitationTargetRequired`

```csharp
const string InvitationTargetRequired
```

An invitation command was missing its required invitation target.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes-membershipnotfound"></a>

##### `MembershipNotFound`

```csharp
const string MembershipNotFound
```

A membership command targeted a membership that does not exist.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes-membershiptargetrequired"></a>

##### `MembershipTargetRequired`

```csharp
const string MembershipTargetRequired
```

A membership command was missing its required principal target.

<a id="member-f-cephalon-multitenancy-governance-services-tenantadministrationworkflowoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

A governance store failed before the requested command could be reported as applied.

<a id="type-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest"></a>

### `TenantAdministrationWorkflowRequest`

Describes one host-driven tenant-administration workflow command.

#### Declaration
```csharp
public sealed class TenantAdministrationWorkflowRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantAdministrationWorkflowRequest`

```csharp
TenantAdministrationWorkflowRequest(string command, string tenantId, string principalId, string principalKind, string invitationId, string inviteeId, string inviteeKind, string displayName, IReadOnlyList<string> roles, string actor, string reason, DateTimeOffset? atUtc, DateTimeOffset? effectiveFromUtc, DateTimeOffset? expiresAtUtc, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-administration workflow command request.

Parameters:
- `command`: The tenant-administration command to apply.
- `tenantId`: The stable tenant identifier.
- `principalId`: The principal identifier for membership commands.
- `principalKind`: The principal kind for membership commands, such as user, group, service, or organization.
- `invitationId`: The invitation identifier for invitation commands.
- `inviteeId`: The invitee identifier for invitation commands.
- `inviteeKind`: The invitee kind for invitation commands, such as user, group, service, or organization.
- `displayName`: The optional operator-facing membership or invitation name.
- `roles`: The tenant-local roles associated with the membership or invitation.
- `actor`: The actor that requested the command when known.
- `reason`: The optional operator-facing command reason.
- `atUtc`: The UTC timestamp used for the command. The runtime clock is used when omitted.
- `effectiveFromUtc`: The optional UTC timestamp when a granted membership becomes active.
- `expiresAtUtc`: The optional UTC timestamp when the membership or invitation expires.
- `correlationId`: The optional correlation identifier for the command.
- `metadata`: Optional command metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested the command when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for the command.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-command"></a>

##### `Command`

```csharp
string Command { get; }
```

Gets the tenant-administration command to apply.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the command.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing membership or invitation name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-effectivefromutc"></a>

##### `EffectiveFromUtc`

```csharp
DateTimeOffset? EffectiveFromUtc { get; }
```

Gets the optional UTC timestamp when a granted membership becomes active.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp when the membership or invitation expires.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier for invitation commands.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-inviteeid"></a>

##### `InviteeId`

```csharp
string InviteeId { get; }
```

Gets the invitee identifier for invitation commands.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-inviteekind"></a>

##### `InviteeKind`

```csharp
string InviteeKind { get; }
```

Gets the invitee kind for invitation commands.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional command metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-principalid"></a>

##### `PrincipalId`

```csharp
string PrincipalId { get; }
```

Gets the principal identifier for membership commands.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-principalkind"></a>

##### `PrincipalKind`

```csharp
string PrincipalKind { get; }
```

Gets the principal kind for membership commands.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the optional operator-facing command reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-roles"></a>

##### `Roles`

```csharp
IReadOnlyList<string> Roles { get; }
```

Gets the tenant-local roles associated with the membership or invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the stable tenant identifier.

<a id="type-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult"></a>

### `TenantAdministrationWorkflowResult`

Describes the result of one tenant-administration workflow command.

#### Declaration
```csharp
public sealed class TenantAdministrationWorkflowResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-ctor-system-string-system-string-system-string-system-string-system-string-system-boolean-system-datetimeoffset-system-string-system-string-cephalon-multitenancy-governance-services-tenantmembershipdescriptor-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantAdministrationWorkflowResult`

```csharp
TenantAdministrationWorkflowResult(string tenantId, string command, string targetKind, string targetId, string outcome, bool applied, DateTimeOffset occurredAtUtc, string previousStatus, string currentStatus, TenantMembershipDescriptor membership, TenantInvitationDescriptor invitation, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-administration workflow command result.

Parameters:
- `tenantId`: The stable tenant identifier targeted by the command.
- `command`: The tenant-administration command that was requested.
- `targetKind`: The kind of target affected by the command.
- `targetId`: The stable target identifier affected by the command.
- `outcome`: The tenant-administration command outcome.
- `applied`: A value indicating whether the command was fully applied.
- `occurredAtUtc`: The UTC timestamp when the command was evaluated.
- `previousStatus`: The target status before the command when a target existed.
- `currentStatus`: The target status after the command when a target exists.
- `membership`: The resulting membership descriptor when a membership command produced one.
- `invitation`: The resulting invitation descriptor when an invitation command produced one.
- `reason`: The operator-facing command result reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-applied"></a>

##### `Applied`

```csharp
bool Applied { get; }
```

Gets a value indicating whether the command was fully applied.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-command"></a>

##### `Command`

```csharp
string Command { get; }
```

Gets the tenant-administration command that was requested.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-currentstatus"></a>

##### `CurrentStatus`

```csharp
string CurrentStatus { get; }
```

Gets the target status after the command when a target exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-invitation"></a>

##### `Invitation`

```csharp
TenantInvitationDescriptor Invitation { get; }
```

Gets the resulting invitation descriptor when an invitation command produced one.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-membership"></a>

##### `Membership`

```csharp
TenantMembershipDescriptor Membership { get; }
```

Gets the resulting membership descriptor when a membership command produced one.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; }
```

Gets the UTC timestamp when the command was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the tenant-administration command outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-previousstatus"></a>

##### `PreviousStatus`

```csharp
string PreviousStatus { get; }
```

Gets the target status before the command when a target existed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing command result reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-targetid"></a>

##### `TargetId`

```csharp
string TargetId { get; }
```

Gets the stable target identifier affected by the command.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-targetkind"></a>

##### `TargetKind`

```csharp
string TargetKind { get; }
```

Gets the kind of target affected by the command.

<a id="member-p-cephalon-multitenancy-governance-services-tenantadministrationworkflowresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the stable tenant identifier targeted by the command.

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

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys"></a>

### `TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys`

Stable metadata keys written by tenant-domain ownership DNS TXT proof collection.

#### Declaration
```csharp
public static class TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-backgroundproofpollingownership"></a>

##### `BackgroundProofPollingOwnership`

```csharp
const string BackgroundProofPollingOwnership
```

Metadata key that keeps automatic background proof polling ownership explicit.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-dnstxtproofcollectionownership"></a>

##### `DnsTxtProofCollectionOwnership`

```csharp
const string DnsTxtProofCollectionOwnership
```

Metadata key that identifies Cephalon as the DNS TXT proof collection owner when a resolver endpoint is configured.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-externalproofpollingownership"></a>

##### `ExternalProofPollingOwnership`

```csharp
const string ExternalProofPollingOwnership
```

Metadata key that keeps on-demand external proof polling ownership explicit.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-httpproofcollectionownership"></a>

##### `HttpProofCollectionOwnership`

```csharp
const string HttpProofCollectionOwnership
```

Metadata key for HTTP proof collection ownership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectedatutc"></a>

##### `LastDnsTxtProofCollectedAtUtc`

```csharp
const string LastDnsTxtProofCollectedAtUtc
```

Metadata key for the UTC timestamp when DNS TXT proof collection executed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionactor"></a>

##### `LastDnsTxtProofCollectionActor`

```csharp
const string LastDnsTxtProofCollectionActor
```

Metadata key for the actor that requested DNS TXT proof collection when known.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectioncontentlength"></a>

##### `LastDnsTxtProofCollectionContentLength`

```csharp
const string LastDnsTxtProofCollectionContentLength
```

Metadata key for the DNS TXT resolver response body length.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectioncorrelationid"></a>

##### `LastDnsTxtProofCollectionCorrelationId`

```csharp
const string LastDnsTxtProofCollectionCorrelationId
```

Metadata key for the DNS TXT proof collection correlation identifier.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionobservedfingerprint"></a>

##### `LastDnsTxtProofCollectionObservedFingerprint`

```csharp
const string LastDnsTxtProofCollectionObservedFingerprint
```

Metadata key for the SHA-256 fingerprint of the matching collected DNS TXT proof.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionobservedtxtrecordcount"></a>

##### `LastDnsTxtProofCollectionObservedTxtRecordCount`

```csharp
const string LastDnsTxtProofCollectionObservedTxtRecordCount
```

Metadata key for the number of TXT answers observed by collection.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionoutcome"></a>

##### `LastDnsTxtProofCollectionOutcome`

```csharp
const string LastDnsTxtProofCollectionOutcome
```

Metadata key for the last DNS TXT proof collection outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionpublicationplanoutcome"></a>

##### `LastDnsTxtProofCollectionPublicationPlanOutcome`

```csharp
const string LastDnsTxtProofCollectionPublicationPlanOutcome
```

Metadata key for the nested publication-plan outcome used by collection.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionrecordname"></a>

##### `LastDnsTxtProofCollectionRecordName`

```csharp
const string LastDnsTxtProofCollectionRecordName
```

Metadata key for the DNS TXT record name queried during collection.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionresolveruri"></a>

##### `LastDnsTxtProofCollectionResolverUri`

```csharp
const string LastDnsTxtProofCollectionResolverUri
```

Metadata key for the resolver URI used to collect the DNS TXT proof.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionsource"></a>

##### `LastDnsTxtProofCollectionSource`

```csharp
const string LastDnsTxtProofCollectionSource
```

Metadata key for the source that requested DNS TXT proof collection.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionmetadatakeys-lastdnstxtproofcollectionstatuscode"></a>

##### `LastDnsTxtProofCollectionStatusCode`

```csharp
const string LastDnsTxtProofCollectionStatusCode
```

Metadata key for the HTTP status code returned by the DNS TXT resolver.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes"></a>

### `TenantDomainOwnershipDnsTxtProofCollectionOutcomes`

Stable tenant-domain ownership DNS TXT proof collection outcome labels.

#### Declaration
```csharp
public static class TenantDomainOwnershipDnsTxtProofCollectionOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-collected"></a>

##### `Collected`

```csharp
const string Collected
```

DNS TXT proof content was collected and proof evaluation reached a terminal workflow outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

DNS TXT proof collection is disabled by governance options.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-emptyresponse"></a>

##### `EmptyResponse`

```csharp
const string EmptyResponse
```

The DNS TXT proof resolver returned an empty response body.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-evaluationfailed"></a>

##### `EvaluationFailed`

```csharp
const string EvaluationFailed
```

DNS TXT content was collected, but proof evaluation did not apply a terminal workflow outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-invalidresolveruri"></a>

##### `InvalidResolverUri`

```csharp
const string InvalidResolverUri
```

The resolved DNS TXT proof resolver URI is invalid or unsafe.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-invalidresponse"></a>

##### `InvalidResponse`

```csharp
const string InvalidResponse
```

The DNS TXT proof resolver response could not be parsed as a DNS JSON response.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-missingexpectedproof"></a>

##### `MissingExpectedProof`

```csharp
const string MissingExpectedProof
```

Expected proof metadata is missing from the tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-missingpublicationplan"></a>

##### `MissingPublicationPlan`

```csharp
const string MissingPublicationPlan
```

Publication planning did not provide a DNS TXT record name and expected proof value.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-nomatchingtxtrecord"></a>

##### `NoMatchingTxtRecord`

```csharp
const string NoMatchingTxtRecord
```

DNS TXT answers were returned, but none matched the expected proof value.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No tenant-domain ownership declaration matched the supplied tenant and domain.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-notxtrecords"></a>

##### `NoTxtRecords`

```csharp
const string NoTxtRecords
```

No DNS TXT answer was returned for the planned proof record.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-requestfailed"></a>

##### `RequestFailed`

```csharp
const string RequestFailed
```

The DNS TXT proof resolver could not be reached or timed out.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-resolvernotconfigured"></a>

##### `ResolverNotConfigured`

```csharp
const string ResolverNotConfigured
```

DNS TXT proof collection has no configured resolver endpoint.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-responsetoolarge"></a>

##### `ResponseTooLarge`

```csharp
const string ResponseTooLarge
```

The DNS TXT proof resolver response body exceeded the configured collection size limit.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

Publication-plan metadata could not be recorded before collection.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

A declaration for the supplied domain belongs to a different tenant.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-unexpectedstatuscode"></a>

##### `UnexpectedStatusCode`

```csharp
const string UnexpectedStatusCode
```

The DNS TXT proof resolver returned a non-success status code.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-unsupportedverificationmethod"></a>

##### `UnsupportedVerificationMethod`

```csharp
const string UnsupportedVerificationMethod
```

The verification method cannot be collected by the built-in DNS TXT proof collector.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionoutcomes-verificationmethodmismatch"></a>

##### `VerificationMethodMismatch`

```csharp
const string VerificationMethodMismatch
```

The matching domain ownership declaration uses a different verification method.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest"></a>

### `TenantDomainOwnershipDnsTxtProofCollectionRequest`

Describes a tenant-domain ownership DNS TXT proof collection request.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipDnsTxtProofCollectionRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-ctor-system-string-system-string-system-string-system-uri-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-boolean-system-nullable-system-timespan-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipDnsTxtProofCollectionRequest`

```csharp
TenantDomainOwnershipDnsTxtProofCollectionRequest(string tenantId, string domainName, string verificationMethod, Uri resolverEndpoint, string source, string actor, DateTimeOffset? atUtc, DateTimeOffset? expiresAtUtc, string correlationId, bool recordPublicationPlan, TimeSpan? timeout, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership DNS TXT proof collection request.

Parameters:
- `tenantId`: The tenant identifier that owns the domain declaration.
- `domainName`: The domain name whose DNS TXT proof should be collected.
- `verificationMethod`: The optional verification method boundary. Only DNS TXT verification can be collected.
- `resolverEndpoint`: The optional DNS-over-HTTPS resolver endpoint used for collection.
- `source`: The source that requested DNS TXT proof collection.
- `actor`: The actor that requested DNS TXT proof collection when known.
- `atUtc`: The UTC timestamp used for collection. The runtime clock is used when omitted.
- `expiresAtUtc`: The optional UTC timestamp applied if proof evaluation verifies the declaration.
- `correlationId`: The optional correlation identifier for collection and evaluation.
- `recordPublicationPlan`: A value indicating whether the publication plan should be recorded before collection.
- `timeout`: The optional per-request DNS TXT collection timeout.
- `metadata`: Optional DNS TXT proof collection metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested DNS TXT proof collection when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for collection and evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name whose DNS TXT proof should be collected.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp applied if proof evaluation verifies the declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional DNS TXT proof collection metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-recordpublicationplan"></a>

##### `RecordPublicationPlan`

```csharp
bool RecordPublicationPlan { get; }
```

Gets a value indicating whether the publication plan should be recorded before collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-resolverendpoint"></a>

##### `ResolverEndpoint`

```csharp
Uri ResolverEndpoint { get; }
```

Gets the optional DNS-over-HTTPS resolver endpoint used for collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested DNS TXT proof collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the domain declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-timeout"></a>

##### `Timeout`

```csharp
TimeSpan? Timeout { get; }
```

Gets the optional per-request DNS TXT collection timeout.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionrequest-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method boundary.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult"></a>

### `TenantDomainOwnershipDnsTxtProofCollectionResult`

Describes the result of one tenant-domain ownership DNS TXT proof collection attempt.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipDnsTxtProofCollectionResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-uri-system-string-system-nullable-system-int32-system-nullable-system-int64-system-int32-system-string-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipDnsTxtProofCollectionResult`

```csharp
TenantDomainOwnershipDnsTxtProofCollectionResult(string tenantId, string domainName, string verificationMethod, string outcome, bool collected, bool evaluated, DateTimeOffset collectedAtUtc, Uri resolverUri, string dnsTxtRecordName, int? statusCode, long? contentLength, int observedTxtRecordCount, string observedProofFingerprint, TenantDomainOwnershipProofPublicationPlanResult publicationPlanResult, TenantDomainOwnershipProofEvaluationResult evaluationResult, TenantDomainOwnershipDescriptor domainOwnership, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership DNS TXT proof collection result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `domainName`: The canonical domain name that was evaluated.
- `verificationMethod`: The verification method used for collection.
- `outcome`: The stable DNS TXT proof collection outcome.
- `collected`: A value indicating whether DNS TXT proof content was collected.
- `evaluated`: A value indicating whether proof evaluation reached a terminal workflow outcome.
- `collectedAtUtc`: The UTC timestamp when collection executed.
- `resolverUri`: The resolver URI used to collect the DNS TXT proof.
- `dnsTxtRecordName`: The DNS TXT record name queried during collection.
- `statusCode`: The HTTP status code returned by the DNS TXT resolver.
- `contentLength`: The collected DNS TXT resolver response body length.
- `observedTxtRecordCount`: The number of TXT answers observed by collection.
- `observedProofFingerprint`: The SHA-256 fingerprint of the matching collected TXT proof.
- `publicationPlanResult`: The publication-plan result used by collection.
- `evaluationResult`: The proof-evaluation result produced after collection.
- `domainOwnership`: The matching or resulting domain ownership descriptor when one exists.
- `reason`: The operator-facing DNS TXT proof collection reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-collected"></a>

##### `Collected`

```csharp
bool Collected { get; }
```

Gets a value indicating whether DNS TXT proof content was collected.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-collectedatutc"></a>

##### `CollectedAtUtc`

```csharp
DateTimeOffset CollectedAtUtc { get; }
```

Gets the UTC timestamp when collection executed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-contentlength"></a>

##### `ContentLength`

```csharp
long? ContentLength { get; }
```

Gets the collected DNS TXT resolver response body length.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-dnstxtrecordname"></a>

##### `DnsTxtRecordName`

```csharp
string DnsTxtRecordName { get; }
```

Gets the DNS TXT record name queried during collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-domainownership"></a>

##### `DomainOwnership`

```csharp
TenantDomainOwnershipDescriptor DomainOwnership { get; }
```

Gets the matching or resulting domain ownership descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-evaluated"></a>

##### `Evaluated`

```csharp
bool Evaluated { get; }
```

Gets a value indicating whether proof evaluation reached a terminal workflow outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-evaluationresult"></a>

##### `EvaluationResult`

```csharp
TenantDomainOwnershipProofEvaluationResult EvaluationResult { get; }
```

Gets the proof-evaluation result produced after collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-observedprooffingerprint"></a>

##### `ObservedProofFingerprint`

```csharp
string ObservedProofFingerprint { get; }
```

Gets the SHA-256 fingerprint of the matching collected TXT proof.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-observedtxtrecordcount"></a>

##### `ObservedTxtRecordCount`

```csharp
int ObservedTxtRecordCount { get; }
```

Gets the number of TXT answers observed by collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable DNS TXT proof collection outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-publicationplanresult"></a>

##### `PublicationPlanResult`

```csharp
TenantDomainOwnershipProofPublicationPlanResult PublicationPlanResult { get; }
```

Gets the publication-plan result used by collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing DNS TXT proof collection reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-resolveruri"></a>

##### `ResolverUri`

```csharp
Uri ResolverUri { get; }
```

Gets the resolver URI used to collect the DNS TXT proof.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-statuscode"></a>

##### `StatusCode`

```csharp
int? StatusCode { get; }
```

Gets the HTTP status code returned by the DNS TXT resolver.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method used for collection.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys"></a>

### `TenantDomainOwnershipHttpProofCollectionMetadataKeys`

Stable metadata keys written by tenant-domain ownership HTTP proof collection.

#### Declaration
```csharp
public static class TenantDomainOwnershipHttpProofCollectionMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-backgroundproofpollingownership"></a>

##### `BackgroundProofPollingOwnership`

```csharp
const string BackgroundProofPollingOwnership
```

Metadata key that keeps automatic background proof polling ownership explicit.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-dnstxtproofcollectionownership"></a>

##### `DnsTxtProofCollectionOwnership`

```csharp
const string DnsTxtProofCollectionOwnership
```

Metadata key that keeps DNS TXT proof collection ownership explicit.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-externalproofpollingownership"></a>

##### `ExternalProofPollingOwnership`

```csharp
const string ExternalProofPollingOwnership
```

Metadata key that keeps on-demand external proof polling ownership explicit.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-httpproofcollectionownership"></a>

##### `HttpProofCollectionOwnership`

```csharp
const string HttpProofCollectionOwnership
```

Metadata key that identifies Cephalon as the HTTP proof collection owner.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectedatutc"></a>

##### `LastHttpProofCollectedAtUtc`

```csharp
const string LastHttpProofCollectedAtUtc
```

Metadata key for the UTC timestamp when HTTP proof collection executed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectionactor"></a>

##### `LastHttpProofCollectionActor`

```csharp
const string LastHttpProofCollectionActor
```

Metadata key for the actor that requested HTTP proof collection when known.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectioncontentlength"></a>

##### `LastHttpProofCollectionContentLength`

```csharp
const string LastHttpProofCollectionContentLength
```

Metadata key for the collected HTTP proof response body length.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectioncorrelationid"></a>

##### `LastHttpProofCollectionCorrelationId`

```csharp
const string LastHttpProofCollectionCorrelationId
```

Metadata key for the HTTP proof collection correlation identifier.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectionobservedfingerprint"></a>

##### `LastHttpProofCollectionObservedFingerprint`

```csharp
const string LastHttpProofCollectionObservedFingerprint
```

Metadata key for the SHA-256 fingerprint of the collected HTTP proof body.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectionoutcome"></a>

##### `LastHttpProofCollectionOutcome`

```csharp
const string LastHttpProofCollectionOutcome
```

Metadata key for the last HTTP proof collection outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectionpublicationplanoutcome"></a>

##### `LastHttpProofCollectionPublicationPlanOutcome`

```csharp
const string LastHttpProofCollectionPublicationPlanOutcome
```

Metadata key for the nested publication-plan outcome used by collection.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectionsource"></a>

##### `LastHttpProofCollectionSource`

```csharp
const string LastHttpProofCollectionSource
```

Metadata key for the source that requested HTTP proof collection.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectionstatuscode"></a>

##### `LastHttpProofCollectionStatusCode`

```csharp
const string LastHttpProofCollectionStatusCode
```

Metadata key for the HTTP status code returned by the proof endpoint.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionmetadatakeys-lasthttpproofcollectionuri"></a>

##### `LastHttpProofCollectionUri`

```csharp
const string LastHttpProofCollectionUri
```

Metadata key for the URI used to collect the HTTP proof.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes"></a>

### `TenantDomainOwnershipHttpProofCollectionOutcomes`

Stable tenant-domain ownership HTTP proof collection outcome labels.

#### Declaration
```csharp
public static class TenantDomainOwnershipHttpProofCollectionOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-collected"></a>

##### `Collected`

```csharp
const string Collected
```

HTTP proof content was collected and proof evaluation reached a terminal workflow outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

HTTP proof collection is disabled by governance options.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-emptyresponse"></a>

##### `EmptyResponse`

```csharp
const string EmptyResponse
```

The HTTP proof endpoint returned an empty response body.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-evaluationfailed"></a>

##### `EvaluationFailed`

```csharp
const string EvaluationFailed
```

HTTP content was collected, but proof evaluation did not apply a terminal workflow outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-invaliduri"></a>

##### `InvalidUri`

```csharp
const string InvalidUri
```

The resolved HTTP proof collection URI is invalid or unsafe for the requested domain.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-missingexpectedproof"></a>

##### `MissingExpectedProof`

```csharp
const string MissingExpectedProof
```

Expected proof metadata is missing from the tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-missingpublicationplan"></a>

##### `MissingPublicationPlan`

```csharp
const string MissingPublicationPlan
```

Publication planning did not provide an HTTP file path and expected proof content.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No tenant-domain ownership declaration matched the supplied tenant and domain.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-requestfailed"></a>

##### `RequestFailed`

```csharp
const string RequestFailed
```

The HTTP proof endpoint could not be reached or timed out.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-responsetoolarge"></a>

##### `ResponseTooLarge`

```csharp
const string ResponseTooLarge
```

The HTTP proof endpoint response body exceeded the configured collection size limit.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

Publication-plan metadata could not be recorded before collection.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

A declaration for the supplied domain belongs to a different tenant.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-unexpectedstatuscode"></a>

##### `UnexpectedStatusCode`

```csharp
const string UnexpectedStatusCode
```

The HTTP proof endpoint returned a non-success status code.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-unsupportedverificationmethod"></a>

##### `UnsupportedVerificationMethod`

```csharp
const string UnsupportedVerificationMethod
```

The verification method cannot be collected by the built-in HTTP proof collector.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionoutcomes-verificationmethodmismatch"></a>

##### `VerificationMethodMismatch`

```csharp
const string VerificationMethodMismatch
```

The matching domain ownership declaration uses a different verification method.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest"></a>

### `TenantDomainOwnershipHttpProofCollectionRequest`

Describes a tenant-domain ownership HTTP proof collection request.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipHttpProofCollectionRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-ctor-system-string-system-string-system-string-system-uri-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-boolean-system-nullable-system-timespan-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipHttpProofCollectionRequest`

```csharp
TenantDomainOwnershipHttpProofCollectionRequest(string tenantId, string domainName, string verificationMethod, Uri collectionBaseUri, string source, string actor, DateTimeOffset? atUtc, DateTimeOffset? expiresAtUtc, string correlationId, bool recordPublicationPlan, TimeSpan? timeout, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership HTTP proof collection request.

Parameters:
- `tenantId`: The tenant identifier that owns the domain declaration.
- `domainName`: The domain name whose HTTP proof should be collected.
- `verificationMethod`: The optional verification method boundary. Only HTTP file verification can be collected.
- `collectionBaseUri`: The optional base URI used for collection. When omitted, HTTPS on the requested domain is used.
- `source`: The source that requested HTTP proof collection.
- `actor`: The actor that requested HTTP proof collection when known.
- `atUtc`: The UTC timestamp used for collection. The runtime clock is used when omitted.
- `expiresAtUtc`: The optional UTC timestamp applied if proof evaluation verifies the declaration.
- `correlationId`: The optional correlation identifier for collection and evaluation.
- `recordPublicationPlan`: A value indicating whether the publication plan should be recorded before collection.
- `timeout`: The optional per-request HTTP collection timeout.
- `metadata`: Optional HTTP proof collection metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested HTTP proof collection when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-collectionbaseuri"></a>

##### `CollectionBaseUri`

```csharp
Uri CollectionBaseUri { get; }
```

Gets the optional base URI used for collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for collection and evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name whose HTTP proof should be collected.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp applied if proof evaluation verifies the declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional HTTP proof collection metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-recordpublicationplan"></a>

##### `RecordPublicationPlan`

```csharp
bool RecordPublicationPlan { get; }
```

Gets a value indicating whether the publication plan should be recorded before collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested HTTP proof collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the domain declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-timeout"></a>

##### `Timeout`

```csharp
TimeSpan? Timeout { get; }
```

Gets the optional per-request HTTP collection timeout.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionrequest-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method boundary.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult"></a>

### `TenantDomainOwnershipHttpProofCollectionResult`

Describes the result of one tenant-domain ownership HTTP proof collection attempt.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipHttpProofCollectionResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-uri-system-nullable-system-int32-system-nullable-system-int64-system-string-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipHttpProofCollectionResult`

```csharp
TenantDomainOwnershipHttpProofCollectionResult(string tenantId, string domainName, string verificationMethod, string outcome, bool collected, bool evaluated, DateTimeOffset collectedAtUtc, Uri collectionUri, int? statusCode, long? contentLength, string observedProofFingerprint, TenantDomainOwnershipProofPublicationPlanResult publicationPlanResult, TenantDomainOwnershipProofEvaluationResult evaluationResult, TenantDomainOwnershipDescriptor domainOwnership, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership HTTP proof collection result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `domainName`: The canonical domain name that was evaluated.
- `verificationMethod`: The verification method used for collection.
- `outcome`: The stable HTTP proof collection outcome.
- `collected`: A value indicating whether HTTP proof content was collected.
- `evaluated`: A value indicating whether proof evaluation reached a terminal workflow outcome.
- `collectedAtUtc`: The UTC timestamp when collection executed.
- `collectionUri`: The URI used to collect the HTTP proof.
- `statusCode`: The HTTP status code returned by the proof endpoint.
- `contentLength`: The collected HTTP proof response body length.
- `observedProofFingerprint`: The SHA-256 fingerprint of the collected proof body.
- `publicationPlanResult`: The publication-plan result used by collection.
- `evaluationResult`: The proof-evaluation result produced after collection.
- `domainOwnership`: The matching or resulting domain ownership descriptor when one exists.
- `reason`: The operator-facing HTTP proof collection reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-collected"></a>

##### `Collected`

```csharp
bool Collected { get; }
```

Gets a value indicating whether HTTP proof content was collected.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-collectedatutc"></a>

##### `CollectedAtUtc`

```csharp
DateTimeOffset CollectedAtUtc { get; }
```

Gets the UTC timestamp when collection executed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-collectionuri"></a>

##### `CollectionUri`

```csharp
Uri CollectionUri { get; }
```

Gets the URI used to collect the HTTP proof.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-contentlength"></a>

##### `ContentLength`

```csharp
long? ContentLength { get; }
```

Gets the collected HTTP proof response body length.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-domainownership"></a>

##### `DomainOwnership`

```csharp
TenantDomainOwnershipDescriptor DomainOwnership { get; }
```

Gets the matching or resulting domain ownership descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-evaluated"></a>

##### `Evaluated`

```csharp
bool Evaluated { get; }
```

Gets a value indicating whether proof evaluation reached a terminal workflow outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-evaluationresult"></a>

##### `EvaluationResult`

```csharp
TenantDomainOwnershipProofEvaluationResult EvaluationResult { get; }
```

Gets the proof-evaluation result produced after collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-observedprooffingerprint"></a>

##### `ObservedProofFingerprint`

```csharp
string ObservedProofFingerprint { get; }
```

Gets the SHA-256 fingerprint of the collected proof body.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable HTTP proof collection outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-publicationplanresult"></a>

##### `PublicationPlanResult`

```csharp
TenantDomainOwnershipProofPublicationPlanResult PublicationPlanResult { get; }
```

Gets the publication-plan result used by collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing HTTP proof collection reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-statuscode"></a>

##### `StatusCode`

```csharp
int? StatusCode { get; }
```

Gets the HTTP status code returned by the proof endpoint.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method used for collection.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor"></a>

### `TenantDomainOwnershipHttpProofPublicationDescriptor`

Describes one tenant-domain ownership HTTP proof file published by Cephalon governance.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipHttpProofPublicationDescriptor
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-datetimeoffset-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipHttpProofPublicationDescriptor`

```csharp
TenantDomainOwnershipHttpProofPublicationDescriptor(string tenantId, string domainName, string httpFilePath, string httpFileContent, string httpContentType, string proofFingerprint, DateTimeOffset publishedAtUtc, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership HTTP proof publication descriptor.

Parameters:
- `tenantId`: The tenant identifier that owns the domain declaration.
- `domainName`: The canonical domain name that should serve the proof file.
- `httpFilePath`: The HTTP path where the proof file is served.
- `httpFileContent`: The public proof-file content.
- `httpContentType`: The content type used when serving the proof file.
- `proofFingerprint`: The SHA-256 fingerprint of the proof-file content.
- `publishedAtUtc`: The UTC timestamp when publication was recorded.
- `metadata`: Optional publication metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that should serve the proof file.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-httpcontenttype"></a>

##### `HttpContentType`

```csharp
string HttpContentType { get; }
```

Gets the content type used when serving the proof file.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-httpfilecontent"></a>

##### `HttpFileContent`

```csharp
string HttpFileContent { get; }
```

Gets the public proof-file content.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-httpfilepath"></a>

##### `HttpFilePath`

```csharp
string HttpFilePath { get; }
```

Gets the HTTP path where the proof file is served.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional publication metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-prooffingerprint"></a>

##### `ProofFingerprint`

```csharp
string ProofFingerprint { get; }
```

Gets the SHA-256 fingerprint of the proof-file content.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-publishedatutc"></a>

##### `PublishedAtUtc`

```csharp
DateTimeOffset PublishedAtUtc { get; }
```

Gets the UTC timestamp when publication was recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationdescriptor-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the domain declaration.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys"></a>

### `TenantDomainOwnershipHttpProofPublicationMetadataKeys`

Stable metadata keys written by tenant-domain ownership HTTP proof publication.

#### Declaration
```csharp
public static class TenantDomainOwnershipHttpProofPublicationMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-httpproofpublicationcontentfingerprint"></a>

##### `HttpProofPublicationContentFingerprint`

```csharp
const string HttpProofPublicationContentFingerprint
```

Metadata key for the SHA-256 fingerprint of the published proof-file content.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-httpproofpublicationcontenttype"></a>

##### `HttpProofPublicationContentType`

```csharp
const string HttpProofPublicationContentType
```

Metadata key for the HTTP content type used by the published proof file.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-httpproofpublicationownership"></a>

##### `HttpProofPublicationOwnership`

```csharp
const string HttpProofPublicationOwnership
```

Metadata key that identifies Cephalon as the HTTP proof publication owner.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-httpproofpublicationpath"></a>

##### `HttpProofPublicationPath`

```csharp
const string HttpProofPublicationPath
```

Metadata key for the HTTP path where the proof file is published.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-httpproofpublicationservedby"></a>

##### `HttpProofPublicationServedBy`

```csharp
const string HttpProofPublicationServedBy
```

Metadata key that identifies the component responsible for serving the proof file.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-lasthttpproofpublicationactor"></a>

##### `LastHttpProofPublicationActor`

```csharp
const string LastHttpProofPublicationActor
```

Metadata key for the actor that requested HTTP proof publication.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-lasthttpproofpublicationcorrelationid"></a>

##### `LastHttpProofPublicationCorrelationId`

```csharp
const string LastHttpProofPublicationCorrelationId
```

Metadata key for the HTTP proof publication correlation identifier.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-lasthttpproofpublicationoutcome"></a>

##### `LastHttpProofPublicationOutcome`

```csharp
const string LastHttpProofPublicationOutcome
```

Metadata key for the last HTTP proof publication outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-lasthttpproofpublicationsource"></a>

##### `LastHttpProofPublicationSource`

```csharp
const string LastHttpProofPublicationSource
```

Metadata key for the source that requested HTTP proof publication.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationmetadatakeys-lasthttpproofpublishedatutc"></a>

##### `LastHttpProofPublishedAtUtc`

```csharp
const string LastHttpProofPublishedAtUtc
```

Metadata key for the UTC timestamp when HTTP proof publication was recorded.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationoutcomes"></a>

### `TenantDomainOwnershipHttpProofPublicationOutcomes`

Stable tenant-domain ownership HTTP proof publication outcomes.

#### Declaration
```csharp
public static class TenantDomainOwnershipHttpProofPublicationOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

HTTP proof publication was disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationoutcomes-missinghttpfilepublicationplan"></a>

##### `MissingHttpFilePublicationPlan`

```csharp
const string MissingHttpFilePublicationPlan
```

The publication plan did not include HTTP proof-file instructions.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationoutcomes-publicationplanunavailable"></a>

##### `PublicationPlanUnavailable`

```csharp
const string PublicationPlanUnavailable
```

The proof publication planner did not produce a usable plan.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationoutcomes-published"></a>

##### `Published`

```csharp
const string Published
```

The HTTP proof file was materialized and recorded.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

Publication state could not be stored.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest"></a>

### `TenantDomainOwnershipHttpProofPublicationRequest`

Describes a tenant-domain ownership HTTP proof publication request.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipHttpProofPublicationRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-ctor-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-boolean-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipHttpProofPublicationRequest`

```csharp
TenantDomainOwnershipHttpProofPublicationRequest(string tenantId, string domainName, string source, string actor, DateTimeOffset? atUtc, string correlationId, bool recordPublication, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership HTTP proof publication request.

Parameters:
- `tenantId`: The tenant identifier that owns the domain declaration.
- `domainName`: The domain name that should publish the HTTP proof file.
- `source`: The source that requested HTTP proof publication.
- `actor`: The actor that requested HTTP proof publication when known.
- `atUtc`: The UTC timestamp used for publication. The runtime clock is used when omitted.
- `correlationId`: The optional correlation identifier for HTTP proof publication.
- `recordPublication`: A value indicating whether publication metadata should be recorded.
- `metadata`: Optional HTTP proof publication metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested HTTP proof publication when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for publication.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for HTTP proof publication.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that should publish the HTTP proof file.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional HTTP proof publication metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-recordpublication"></a>

##### `RecordPublication`

```csharp
bool RecordPublication { get; }
```

Gets a value indicating whether publication metadata should be recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested HTTP proof publication.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the domain declaration.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult"></a>

### `TenantDomainOwnershipHttpProofPublicationResult`

Describes the result of tenant-domain ownership HTTP proof publication.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipHttpProofPublicationResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-ctor-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-string-system-string-system-string-system-string-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipHttpProofPublicationResult`

```csharp
TenantDomainOwnershipHttpProofPublicationResult(string tenantId, string domainName, string outcome, bool published, bool recorded, DateTimeOffset publishedAtUtc, string httpFilePath, string httpFileContent, string httpContentType, string proofFingerprint, TenantDomainOwnershipDescriptor domainOwnership, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership HTTP proof publication result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `domainName`: The canonical domain name that was evaluated.
- `outcome`: The stable HTTP proof publication outcome.
- `published`: A value indicating whether the HTTP proof file was materialized.
- `recorded`: A value indicating whether publication metadata was recorded.
- `publishedAtUtc`: The UTC timestamp used for publication.
- `httpFilePath`: The HTTP path where the proof file is served.
- `httpFileContent`: The public proof-file content.
- `httpContentType`: The content type used when serving the proof file.
- `proofFingerprint`: The SHA-256 fingerprint of the proof-file content.
- `domainOwnership`: The resulting domain ownership descriptor when one exists.
- `reason`: The operator-facing HTTP proof publication reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-domainownership"></a>

##### `DomainOwnership`

```csharp
TenantDomainOwnershipDescriptor DomainOwnership { get; }
```

Gets the resulting domain ownership descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-httpcontenttype"></a>

##### `HttpContentType`

```csharp
string HttpContentType { get; }
```

Gets the content type used when serving the proof file.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-httpfilecontent"></a>

##### `HttpFileContent`

```csharp
string HttpFileContent { get; }
```

Gets the public proof-file content.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-httpfilepath"></a>

##### `HttpFilePath`

```csharp
string HttpFilePath { get; }
```

Gets the HTTP path where the proof file is served.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable HTTP proof publication outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-prooffingerprint"></a>

##### `ProofFingerprint`

```csharp
string ProofFingerprint { get; }
```

Gets the SHA-256 fingerprint of the proof-file content.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-published"></a>

##### `Published`

```csharp
bool Published { get; }
```

Gets a value indicating whether the HTTP proof file was materialized.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-publishedatutc"></a>

##### `PublishedAtUtc`

```csharp
DateTimeOffset PublishedAtUtc { get; }
```

Gets the UTC timestamp used for publication.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing HTTP proof publication reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-recorded"></a>

##### `Recorded`

```csharp
bool Recorded { get; }
```

Gets a value indicating whether publication metadata was recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofpublicationresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

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

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys"></a>

### `TenantDomainOwnershipProofPollingMetadataKeys`

Stable metadata keys emitted by tenant-domain ownership proof polling.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofPollingMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-backgroundproofpollingownership"></a>

##### `BackgroundProofPollingOwnership`

```csharp
const string BackgroundProofPollingOwnership
```

Metadata key containing the automatic background proof polling ownership mode.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-batchlimit"></a>

##### `BatchLimit`

```csharp
const string BatchLimit
```

Metadata key containing the effective batch limit used by the polling pass.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-candidatecount"></a>

##### `CandidateCount`

```csharp
const string CandidateCount
```

Metadata key containing the number of declarations that matched the request filters before batch limiting.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-externalproofpollingownership"></a>

##### `ExternalProofPollingOwnership`

```csharp
const string ExternalProofPollingOwnership
```

Metadata key containing the on-demand external proof polling ownership mode.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-failedcount"></a>

##### `FailedCount`

```csharp
const string FailedCount
```

Metadata key containing the number of polling attempts that did not reach an accepted terminal outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-lastproofpollingactor"></a>

##### `LastProofPollingActor`

```csharp
const string LastProofPollingActor
```

Metadata key containing the latest proof polling actor.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-lastproofpollingcorrelationid"></a>

##### `LastProofPollingCorrelationId`

```csharp
const string LastProofPollingCorrelationId
```

Metadata key containing the latest proof polling correlation identifier.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-lastproofpollingoutcome"></a>

##### `LastProofPollingOutcome`

```csharp
const string LastProofPollingOutcome
```

Metadata key containing the latest proof polling outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-lastproofpollingranatutc"></a>

##### `LastProofPollingRanAtUtc`

```csharp
const string LastProofPollingRanAtUtc
```

Metadata key containing the latest proof polling timestamp.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-lastproofpollingsource"></a>

##### `LastProofPollingSource`

```csharp
const string LastProofPollingSource
```

Metadata key containing the latest proof polling source.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-proofpollingrunnerownership"></a>

##### `ProofPollingRunnerOwnership`

```csharp
const string ProofPollingRunnerOwnership
```

Metadata key containing the proof polling runner ownership mode.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-rejectedcount"></a>

##### `RejectedCount`

```csharp
const string RejectedCount
```

Metadata key containing the number of declarations rejected during the polling pass.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-skippedcount"></a>

##### `SkippedCount`

```csharp
const string SkippedCount
```

Metadata key containing the number of declarations skipped by filters, missing expected proof policy, or batch limits.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-verificationcount"></a>

##### `VerificationCount`

```csharp
const string VerificationCount
```

Metadata key containing the number of proof verification attempts run during the polling pass.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingmetadatakeys-verifiedcount"></a>

##### `VerifiedCount`

```csharp
const string VerifiedCount
```

Metadata key containing the number of declarations verified during the polling pass.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingoutcomes"></a>

### `TenantDomainOwnershipProofPollingOutcomes`

Stable tenant-domain ownership proof polling outcome labels.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofPollingOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingoutcomes-completed"></a>

##### `Completed`

```csharp
const string Completed
```

At least one matching domain ownership declaration was polled and all attempts reached a terminal or non-failing outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Proof polling is disabled by governance options.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingoutcomes-nocandidates"></a>

##### `NoCandidates`

```csharp
const string NoCandidates
```

No matching domain ownership declarations needed a polling attempt.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingoutcomes-partialfailure"></a>

##### `PartialFailure`

```csharp
const string PartialFailure
```

At least one matching domain ownership declaration was polled, but one or more attempts could not complete.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest"></a>

### `TenantDomainOwnershipProofPollingRequest`

Describes a bounded tenant-domain ownership proof polling request.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofPollingRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-ctor-system-collections-generic-ireadonlycollection-system-string-system-collections-generic-ireadonlycollection-system-string-system-collections-generic-ireadonlycollection-system-string-system-uri-system-uri-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-nullable-system-int32-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-nullable-system-timespan-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofPollingRequest`

```csharp
TenantDomainOwnershipProofPollingRequest(IReadOnlyCollection<string> tenantIds, IReadOnlyCollection<string> domainNames, IReadOnlyCollection<string> verificationMethods, Uri collectionBaseUri, Uri dnsTxtResolverEndpoint, string source, string actor, DateTimeOffset? atUtc, DateTimeOffset? expiresAtUtc, string correlationId, int? maxItems, bool includeHttpFile, bool includeDnsTxt, bool includeRejected, bool includeMissingExpectedProof, bool recordPublicationPlan, TimeSpan? timeout, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof polling request.

Parameters:
- `tenantIds`: Optional tenant identifiers to include. When omitted, all tenants are eligible.
- `domainNames`: Optional domain names to include. When omitted, all domains are eligible.
- `verificationMethods`: Optional verification methods to include. When omitted, HTTP file and DNS TXT declarations are eligible.
- `collectionBaseUri`: The optional base URI used by HTTP file proof collection.
- `dnsTxtResolverEndpoint`: The optional DNS-over-HTTPS resolver endpoint used by DNS TXT proof collection.
- `source`: The source that requested the polling pass.
- `actor`: The actor that requested the polling pass when known.
- `atUtc`: The UTC timestamp used by the polling pass. The runtime clock is used when omitted.
- `expiresAtUtc`: The optional UTC timestamp applied if proof evaluation verifies a declaration.
- `correlationId`: The optional correlation identifier for the polling pass.
- `maxItems`: The optional maximum number of declarations to poll in this pass.
- `includeHttpFile`: A value indicating whether HTTP file declarations are eligible.
- `includeDnsTxt`: A value indicating whether DNS TXT declarations are eligible.
- `includeRejected`: A value indicating whether rejected declarations can be retried.
- `includeMissingExpectedProof`: A value indicating whether declarations without expected proof metadata should still be passed to the verifier.
- `recordPublicationPlan`: A value indicating whether nested verification should record publication-plan metadata.
- `timeout`: The optional per-request proof collection timeout.
- `metadata`: Optional proof polling metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested the polling pass when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used by the polling pass.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-collectionbaseuri"></a>

##### `CollectionBaseUri`

```csharp
Uri CollectionBaseUri { get; }
```

Gets the optional base URI used by HTTP file proof collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the polling pass.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-dnstxtresolverendpoint"></a>

##### `DnsTxtResolverEndpoint`

```csharp
Uri DnsTxtResolverEndpoint { get; }
```

Gets the optional DNS-over-HTTPS resolver endpoint used by DNS TXT proof collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-domainnames"></a>

##### `DomainNames`

```csharp
IReadOnlyList<string> DomainNames { get; }
```

Gets optional canonical domain names to include.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp applied if proof evaluation verifies a declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-includednstxt"></a>

##### `IncludeDnsTxt`

```csharp
bool IncludeDnsTxt { get; }
```

Gets a value indicating whether DNS TXT declarations are eligible.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-includehttpfile"></a>

##### `IncludeHttpFile`

```csharp
bool IncludeHttpFile { get; }
```

Gets a value indicating whether HTTP file declarations are eligible.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-includemissingexpectedproof"></a>

##### `IncludeMissingExpectedProof`

```csharp
bool IncludeMissingExpectedProof { get; }
```

Gets a value indicating whether declarations without expected proof metadata should still be passed to the verifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-includerejected"></a>

##### `IncludeRejected`

```csharp
bool IncludeRejected { get; }
```

Gets a value indicating whether rejected declarations can be retried.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-maxitems"></a>

##### `MaxItems`

```csharp
int? MaxItems { get; }
```

Gets the optional maximum number of declarations to poll in this pass.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional proof polling metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-recordpublicationplan"></a>

##### `RecordPublicationPlan`

```csharp
bool RecordPublicationPlan { get; }
```

Gets a value indicating whether nested verification should record publication-plan metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested the polling pass.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-tenantids"></a>

##### `TenantIds`

```csharp
IReadOnlyList<string> TenantIds { get; }
```

Gets optional tenant identifiers to include.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-timeout"></a>

##### `Timeout`

```csharp
TimeSpan? Timeout { get; }
```

Gets the optional per-request proof collection timeout.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingrequest-verificationmethods"></a>

##### `VerificationMethods`

```csharp
IReadOnlyList<string> VerificationMethods { get; }
```

Gets optional verification methods to include.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult"></a>

### `TenantDomainOwnershipProofPollingResult`

Describes the aggregate result of one tenant-domain ownership proof polling pass.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofPollingResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-ctor-system-string-system-boolean-system-datetimeoffset-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-collections-generic-ireadonlylist-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofPollingResult`

```csharp
TenantDomainOwnershipProofPollingResult(string outcome, bool polled, DateTimeOffset ranAtUtc, int candidateCount, int verificationCount, int skippedCount, int verifiedCount, int rejectedCount, int failedCount, int batchLimit, IReadOnlyList<TenantDomainOwnershipProofVerificationResult> verificationResults, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof polling result.

Parameters:
- `outcome`: The stable proof polling outcome.
- `polled`: A value indicating whether at least one verification attempt ran.
- `ranAtUtc`: The UTC timestamp when polling ran.
- `candidateCount`: The number of declarations that matched request filters before batch limiting.
- `verificationCount`: The number of verification attempts run.
- `skippedCount`: The number of declarations skipped by filters, missing expected proof policy, or batch limits.
- `verifiedCount`: The number of declarations verified by the polling pass.
- `rejectedCount`: The number of declarations rejected by the polling pass.
- `failedCount`: The number of attempts that did not reach an accepted terminal outcome.
- `batchLimit`: The effective maximum number of declarations this pass could poll.
- `verificationResults`: The nested proof verification results.
- `reason`: The operator-facing proof polling reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-batchlimit"></a>

##### `BatchLimit`

```csharp
int BatchLimit { get; }
```

Gets the effective maximum number of declarations this pass could poll.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-candidatecount"></a>

##### `CandidateCount`

```csharp
int CandidateCount { get; }
```

Gets the number of declarations that matched request filters before batch limiting.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; }
```

Gets the number of attempts that did not reach an accepted terminal outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable proof polling outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-polled"></a>

##### `Polled`

```csharp
bool Polled { get; }
```

Gets a value indicating whether at least one verification attempt ran.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-ranatutc"></a>

##### `RanAtUtc`

```csharp
DateTimeOffset RanAtUtc { get; }
```

Gets the UTC timestamp when polling ran.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing proof polling reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-rejectedcount"></a>

##### `RejectedCount`

```csharp
int RejectedCount { get; }
```

Gets the number of declarations rejected by the polling pass.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-skippedcount"></a>

##### `SkippedCount`

```csharp
int SkippedCount { get; }
```

Gets the number of declarations skipped by filters, missing expected proof policy, or batch limits.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-verificationcount"></a>

##### `VerificationCount`

```csharp
int VerificationCount { get; }
```

Gets the number of verification attempts run.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-verificationresults"></a>

##### `VerificationResults`

```csharp
IReadOnlyList<TenantDomainOwnershipProofVerificationResult> VerificationResults { get; }
```

Gets the nested proof verification results.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingresult-verifiedcount"></a>

##### `VerifiedCount`

```csharp
int VerifiedCount { get; }
```

Gets the number of declarations verified by the polling pass.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot"></a>

### `TenantDomainOwnershipProofPollingRuntimeSnapshot`

Describes the latest runtime state of tenant-domain ownership proof polling.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofPollingRuntimeSnapshot
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-ctor-system-boolean-system-string-system-int32-system-int32-system-boolean-system-boolean-system-int64-system-int64-system-int64-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofPollingRuntimeSnapshot`

```csharp
TenantDomainOwnershipProofPollingRuntimeSnapshot(bool enabled, string ownership, int intervalSeconds, int batchLimit, bool runOnStartup, bool dnsTxtResolverConfigured, long runCount, long successfulRunCount, long failedRunCount, DateTimeOffset? lastStartedAtUtc, DateTimeOffset? lastCompletedAtUtc, string lastOutcome, string lastReason, int lastCandidateCount, int lastVerificationCount, int lastVerifiedCount, int lastRejectedCount, int lastFailedCount, string lastError, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof polling runtime snapshot.

Parameters:
- `enabled`: A value indicating whether automatic background proof polling is effectively enabled.
- `ownership`: The automatic background proof polling ownership mode.
- `intervalSeconds`: The effective background polling interval in seconds.
- `batchLimit`: The effective proof polling batch limit.
- `runOnStartup`: A value indicating whether background proof polling runs once during hosted-service startup.
- `dnsTxtResolverConfigured`: A value indicating whether DNS TXT proof collection has an explicit resolver endpoint.
- `runCount`: The number of background polling passes that reached a completed or failed terminal state.
- `successfulRunCount`: The number of background polling passes that completed without an unhandled failure.
- `failedRunCount`: The number of background polling passes that failed before producing a polling result.
- `lastStartedAtUtc`: The UTC timestamp when the latest background polling pass started.
- `lastCompletedAtUtc`: The UTC timestamp when the latest background polling pass completed or failed.
- `lastOutcome`: The latest proof polling outcome.
- `lastReason`: The latest operator-facing proof polling reason.
- `lastCandidateCount`: The latest candidate count.
- `lastVerificationCount`: The latest verification-attempt count.
- `lastVerifiedCount`: The latest verified count.
- `lastRejectedCount`: The latest rejected count.
- `lastFailedCount`: The latest failed-attempt count.
- `lastError`: The latest unhandled background polling error message.
- `metadata`: Optional runtime metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-batchlimit"></a>

##### `BatchLimit`

```csharp
int BatchLimit { get; }
```

Gets the effective proof polling batch limit.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-dnstxtresolverconfigured"></a>

##### `DnsTxtResolverConfigured`

```csharp
bool DnsTxtResolverConfigured { get; }
```

Gets a value indicating whether DNS TXT proof collection has an explicit resolver endpoint.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; }
```

Gets a value indicating whether automatic background proof polling is effectively enabled.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-failedruncount"></a>

##### `FailedRunCount`

```csharp
long FailedRunCount { get; }
```

Gets the number of background polling passes that failed before producing a polling result.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-intervalseconds"></a>

##### `IntervalSeconds`

```csharp
int IntervalSeconds { get; }
```

Gets the effective background polling interval in seconds.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lastcandidatecount"></a>

##### `LastCandidateCount`

```csharp
int LastCandidateCount { get; }
```

Gets the latest candidate count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lastcompletedatutc"></a>

##### `LastCompletedAtUtc`

```csharp
DateTimeOffset? LastCompletedAtUtc { get; }
```

Gets the UTC timestamp when the latest background polling pass completed or failed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; }
```

Gets the latest unhandled background polling error message.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lastfailedcount"></a>

##### `LastFailedCount`

```csharp
int LastFailedCount { get; }
```

Gets the latest failed-attempt count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; }
```

Gets the latest proof polling outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lastreason"></a>

##### `LastReason`

```csharp
string LastReason { get; }
```

Gets the latest operator-facing proof polling reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lastrejectedcount"></a>

##### `LastRejectedCount`

```csharp
int LastRejectedCount { get; }
```

Gets the latest rejected count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-laststartedatutc"></a>

##### `LastStartedAtUtc`

```csharp
DateTimeOffset? LastStartedAtUtc { get; }
```

Gets the UTC timestamp when the latest background polling pass started.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lastverificationcount"></a>

##### `LastVerificationCount`

```csharp
int LastVerificationCount { get; }
```

Gets the latest verification-attempt count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-lastverifiedcount"></a>

##### `LastVerifiedCount`

```csharp
int LastVerifiedCount { get; }
```

Gets the latest verified count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional runtime metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; }
```

Gets the automatic background proof polling ownership mode.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-runcount"></a>

##### `RunCount`

```csharp
long RunCount { get; }
```

Gets the number of background polling passes that reached a completed or failed terminal state.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-runonstartup"></a>

##### `RunOnStartup`

```csharp
bool RunOnStartup { get; }
```

Gets a value indicating whether background proof polling runs once during hosted-service startup.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpollingruntimesnapshot-successfulruncount"></a>

##### `SuccessfulRunCount`

```csharp
long SuccessfulRunCount { get; }
```

Gets the number of background polling passes that completed without an unhandled failure.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys"></a>

### `TenantDomainOwnershipProofPublicationPlanMetadataKeys`

Stable metadata keys written by tenant-domain ownership proof publication planning.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofPublicationPlanMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-dnstxtrecordname"></a>

##### `DnsTxtRecordName`

```csharp
const string DnsTxtRecordName
```

Metadata key for the DNS TXT record name where the proof should be published.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-dnstxtrecordvaluefingerprint"></a>

##### `DnsTxtRecordValueFingerprint`

```csharp
const string DnsTxtRecordValueFingerprint
```

Metadata key for the DNS TXT record value fingerprint.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-externalpublicationownership"></a>

##### `ExternalPublicationOwnership`

```csharp
const string ExternalPublicationOwnership
```

Metadata key that keeps external publication ownership explicit.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-httpcontenttype"></a>

##### `HttpContentType`

```csharp
const string HttpContentType
```

Metadata key for the HTTP content type used by the publication plan.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-httpfilecontentfingerprint"></a>

##### `HttpFileContentFingerprint`

```csharp
const string HttpFileContentFingerprint
```

Metadata key for the HTTP file content fingerprint.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-httpfilepath"></a>

##### `HttpFilePath`

```csharp
const string HttpFilePath
```

Metadata key for the HTTP path where the proof file should be published.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-lastproofpublicationplanactor"></a>

##### `LastProofPublicationPlanActor`

```csharp
const string LastProofPublicationPlanActor
```

Metadata key for the actor that requested publication planning.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-lastproofpublicationplancorrelationid"></a>

##### `LastProofPublicationPlanCorrelationId`

```csharp
const string LastProofPublicationPlanCorrelationId
```

Metadata key for the publication planning correlation identifier.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-lastproofpublicationplanfingerprint"></a>

##### `LastProofPublicationPlanFingerprint`

```csharp
const string LastProofPublicationPlanFingerprint
```

Metadata key for the SHA-256 fingerprint of the planned proof value.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-lastproofpublicationplannedatutc"></a>

##### `LastProofPublicationPlannedAtUtc`

```csharp
const string LastProofPublicationPlannedAtUtc
```

Metadata key for the UTC timestamp when publication planning executed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-lastproofpublicationplanoutcome"></a>

##### `LastProofPublicationPlanOutcome`

```csharp
const string LastProofPublicationPlanOutcome
```

Metadata key for the last publication planning outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-lastproofpublicationplansource"></a>

##### `LastProofPublicationPlanSource`

```csharp
const string LastProofPublicationPlanSource
```

Metadata key for the source that requested publication planning.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanmetadatakeys-proofpublicationplanningownership"></a>

##### `ProofPublicationPlanningOwnership`

```csharp
const string ProofPublicationPlanningOwnership
```

Metadata key that identifies Cephalon as the publication planning owner.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes"></a>

### `TenantDomainOwnershipProofPublicationPlanOutcomes`

Stable tenant-domain ownership proof publication planning outcomes.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofPublicationPlanOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Publication planning is disabled by governance options.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes-missingexpectedproof"></a>

##### `MissingExpectedProof`

```csharp
const string MissingExpectedProof
```

Expected proof metadata is missing from the tenant-domain ownership declaration.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No tenant-domain ownership declaration matched the supplied tenant and domain.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes-planned"></a>

##### `Planned`

```csharp
const string Planned
```

Publication instructions were generated.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

Runtime state could not be persisted.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

The domain is already declared for a different tenant.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes-unsupportedverificationmethod"></a>

##### `UnsupportedVerificationMethod`

```csharp
const string UnsupportedVerificationMethod
```

The verification method does not have built-in publication instructions.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanoutcomes-verificationmethodmismatch"></a>

##### `VerificationMethodMismatch`

```csharp
const string VerificationMethodMismatch
```

The requested verification method does not match the existing declaration.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest"></a>

### `TenantDomainOwnershipProofPublicationPlanRequest`

Describes a tenant-domain ownership proof publication planning request.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofPublicationPlanRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-boolean-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofPublicationPlanRequest`

```csharp
TenantDomainOwnershipProofPublicationPlanRequest(string tenantId, string domainName, string verificationMethod, string source, string actor, DateTimeOffset? atUtc, string correlationId, bool recordPlan, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof publication planning request.

Parameters:
- `tenantId`: The tenant identifier that owns the domain declaration.
- `domainName`: The domain name that should receive publication instructions.
- `verificationMethod`: The optional verification method boundary.
- `source`: The source that requested publication planning.
- `actor`: The actor that requested publication planning when known.
- `atUtc`: The UTC timestamp used for publication planning. The runtime clock is used when omitted.
- `correlationId`: The optional correlation identifier for publication planning.
- `recordPlan`: A value indicating whether the plan should be recorded in domain ownership metadata.
- `metadata`: Optional proof publication planning metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested publication planning when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for publication planning.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for publication planning.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that should receive publication instructions.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional proof publication planning metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-recordplan"></a>

##### `RecordPlan`

```csharp
bool RecordPlan { get; }
```

Gets a value indicating whether the plan should be recorded in domain ownership metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested publication planning.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the domain declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanrequest-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the optional verification method boundary.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult"></a>

### `TenantDomainOwnershipProofPublicationPlanResult`

Describes generated tenant-domain ownership proof publication instructions.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofPublicationPlanResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-string-system-string-system-string-system-string-system-string-system-string-system-string-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofPublicationPlanResult`

```csharp
TenantDomainOwnershipProofPublicationPlanResult(string tenantId, string domainName, string verificationMethod, string outcome, bool planned, bool recorded, DateTimeOffset plannedAtUtc, string proofValue, string proofFingerprint, string dnsTxtRecordName, string dnsTxtRecordValue, string httpFilePath, string httpFileContent, string httpContentType, TenantDomainOwnershipDescriptor domainOwnership, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof publication planning result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `domainName`: The canonical domain name that was evaluated.
- `verificationMethod`: The verification method used for publication planning.
- `outcome`: The stable publication planning outcome.
- `planned`: A value indicating whether publication instructions were generated.
- `recorded`: A value indicating whether publication plan metadata was recorded.
- `plannedAtUtc`: The UTC timestamp when publication planning executed.
- `proofValue`: The public proof value to publish.
- `proofFingerprint`: The SHA-256 fingerprint of the public proof value.
- `dnsTxtRecordName`: The DNS TXT record name where the proof value should be published.
- `dnsTxtRecordValue`: The DNS TXT record value to publish.
- `httpFilePath`: The HTTP path where the proof file should be published.
- `httpFileContent`: The HTTP file content to publish.
- `httpContentType`: The HTTP content type to use when the proof is published as a file.
- `domainOwnership`: The matching or resulting domain ownership descriptor when one exists.
- `reason`: The operator-facing publication planning reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-dnstxtrecordname"></a>

##### `DnsTxtRecordName`

```csharp
string DnsTxtRecordName { get; }
```

Gets the DNS TXT record name where the proof value should be published.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-dnstxtrecordvalue"></a>

##### `DnsTxtRecordValue`

```csharp
string DnsTxtRecordValue { get; }
```

Gets the DNS TXT record value to publish.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-domainownership"></a>

##### `DomainOwnership`

```csharp
TenantDomainOwnershipDescriptor DomainOwnership { get; }
```

Gets the matching or resulting domain ownership descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-httpcontenttype"></a>

##### `HttpContentType`

```csharp
string HttpContentType { get; }
```

Gets the HTTP content type to use when the proof is published as a file.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-httpfilecontent"></a>

##### `HttpFileContent`

```csharp
string HttpFileContent { get; }
```

Gets the HTTP file content to publish.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-httpfilepath"></a>

##### `HttpFilePath`

```csharp
string HttpFilePath { get; }
```

Gets the HTTP path where the proof file should be published.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable publication planning outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-planned"></a>

##### `Planned`

```csharp
bool Planned { get; }
```

Gets a value indicating whether publication instructions were generated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-plannedatutc"></a>

##### `PlannedAtUtc`

```csharp
DateTimeOffset PlannedAtUtc { get; }
```

Gets the UTC timestamp when publication planning executed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-prooffingerprint"></a>

##### `ProofFingerprint`

```csharp
string ProofFingerprint { get; }
```

Gets the SHA-256 fingerprint of the public proof value.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-proofvalue"></a>

##### `ProofValue`

```csharp
string ProofValue { get; }
```

Gets the public proof value to publish.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing publication planning reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-recorded"></a>

##### `Recorded`

```csharp
bool Recorded { get; }
```

Gets a value indicating whether publication plan metadata was recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method used for publication planning.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys"></a>

### `TenantDomainOwnershipProofVerificationMetadataKeys`

Stable metadata keys returned by tenant-domain ownership proof verification runs.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofVerificationMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-backgroundproofpollingownership"></a>

##### `BackgroundProofPollingOwnership`

```csharp
const string BackgroundProofPollingOwnership
```

Metadata key for automatic background proof polling ownership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-dnstxtproofcollectionownership"></a>

##### `DnsTxtProofCollectionOwnership`

```csharp
const string DnsTxtProofCollectionOwnership
```

Metadata key for DNS TXT proof collection ownership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-externalproofpollingownership"></a>

##### `ExternalProofPollingOwnership`

```csharp
const string ExternalProofPollingOwnership
```

Metadata key for on-demand external proof polling ownership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-httpproofcollectionownership"></a>

##### `HttpProofCollectionOwnership`

```csharp
const string HttpProofCollectionOwnership
```

Metadata key for HTTP proof collection ownership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationactor"></a>

##### `LastProofVerificationActor`

```csharp
const string LastProofVerificationActor
```

Metadata key for the actor that requested the latest proof verification run.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationchallengeoutcome"></a>

##### `LastProofVerificationChallengeOutcome`

```csharp
const string LastProofVerificationChallengeOutcome
```

Metadata key for the challenge issuance outcome observed by the latest proof verification run.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationcorrelationid"></a>

##### `LastProofVerificationCorrelationId`

```csharp
const string LastProofVerificationCorrelationId
```

Metadata key for the correlation identifier attached to the latest proof verification run.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationdnstxtcollectionoutcome"></a>

##### `LastProofVerificationDnsTxtCollectionOutcome`

```csharp
const string LastProofVerificationDnsTxtCollectionOutcome
```

Metadata key for the DNS TXT proof collection outcome observed by the latest proof verification run.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationevaluationoutcome"></a>

##### `LastProofVerificationEvaluationOutcome`

```csharp
const string LastProofVerificationEvaluationOutcome
```

Metadata key for the proof evaluation outcome observed by the latest proof verification run.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationhttpcollectionoutcome"></a>

##### `LastProofVerificationHttpCollectionOutcome`

```csharp
const string LastProofVerificationHttpCollectionOutcome
```

Metadata key for the HTTP proof collection outcome observed by the latest proof verification run.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationoutcome"></a>

##### `LastProofVerificationOutcome`

```csharp
const string LastProofVerificationOutcome
```

Metadata key for the latest proof verification runner outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationpublicationplanoutcome"></a>

##### `LastProofVerificationPublicationPlanOutcome`

```csharp
const string LastProofVerificationPublicationPlanOutcome
```

Metadata key for the publication planning outcome observed by the latest proof verification run.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationranatutc"></a>

##### `LastProofVerificationRanAtUtc`

```csharp
const string LastProofVerificationRanAtUtc
```

Metadata key for the UTC timestamp when the latest proof verification run executed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-lastproofverificationsource"></a>

##### `LastProofVerificationSource`

```csharp
const string LastProofVerificationSource
```

Metadata key for the source that requested the latest proof verification run.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-proofpollingrunnerownership"></a>

##### `ProofPollingRunnerOwnership`

```csharp
const string ProofPollingRunnerOwnership
```

Metadata key for proof polling runner ownership.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationmetadatakeys-proofverificationrunnerownership"></a>

##### `ProofVerificationRunnerOwnership`

```csharp
const string ProofVerificationRunnerOwnership
```

Metadata key for proof verification runner ownership.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes"></a>

### `TenantDomainOwnershipProofVerificationOutcomes`

Stable tenant-domain ownership proof verification runner outcome labels.

#### Declaration
```csharp
public static class TenantDomainOwnershipProofVerificationOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-alreadyverified"></a>

##### `AlreadyVerified`

```csharp
const string AlreadyVerified
```

The domain ownership declaration was already verified and no new proof run was needed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-challengefailed"></a>

##### `ChallengeFailed`

```csharp
const string ChallengeFailed
```

Challenge issuance failed before proof verification could proceed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-challengeissued"></a>

##### `ChallengeIssued`

```csharp
const string ChallengeIssued
```

A new or refreshed proof challenge was issued and publication instructions were produced.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Proof verification runner execution is disabled by governance options.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-dnstxtcollectionfailed"></a>

##### `DnsTxtCollectionFailed`

```csharp
const string DnsTxtCollectionFailed
```

DNS TXT proof collection failed before a proof could be evaluated.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-dnstxtcollectionunavailable"></a>

##### `DnsTxtCollectionUnavailable`

```csharp
const string DnsTxtCollectionUnavailable
```

DNS TXT proof collection is required but the built-in collector is not registered.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-evaluationfailed"></a>

##### `EvaluationFailed`

```csharp
const string EvaluationFailed
```

Proof evaluation failed before a terminal workflow outcome could be applied.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-httpcollectionfailed"></a>

##### `HttpCollectionFailed`

```csharp
const string HttpCollectionFailed
```

HTTP proof collection failed before a proof could be evaluated.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-httpcollectionunavailable"></a>

##### `HttpCollectionUnavailable`

```csharp
const string HttpCollectionUnavailable
```

HTTP proof collection is required but the built-in collector is not registered.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-missingexpectedproof"></a>

##### `MissingExpectedProof`

```csharp
const string MissingExpectedProof
```

Expected proof metadata is missing and challenge issuance was not available or not requested.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-missingobservedproof"></a>

##### `MissingObservedProof`

```csharp
const string MissingObservedProof
```

No observed proof was supplied and no built-in collector can collect the requested method.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-notfound"></a>

##### `NotFound`

```csharp
const string NotFound
```

No tenant-domain ownership declaration matched the supplied tenant and domain.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-publicationplanfailed"></a>

##### `PublicationPlanFailed`

```csharp
const string PublicationPlanFailed
```

Publication planning failed before proof verification could proceed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-publicationplanned"></a>

##### `PublicationPlanned`

```csharp
const string PublicationPlanned
```

Publication instructions were produced, but no observed proof was available to evaluate.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-rejected"></a>

##### `Rejected`

```csharp
const string Rejected
```

The observed proof mismatched and the domain ownership declaration was rejected.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

Runtime state could not be persisted.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-tenantmismatch"></a>

##### `TenantMismatch`

```csharp
const string TenantMismatch
```

A declaration for the supplied domain belongs to a different tenant.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-unsupportedverificationmethod"></a>

##### `UnsupportedVerificationMethod`

```csharp
const string UnsupportedVerificationMethod
```

The requested verification method is not supported by the runner.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-verificationmethodmismatch"></a>

##### `VerificationMethodMismatch`

```csharp
const string VerificationMethodMismatch
```

The matching domain ownership declaration uses a different verification method.

<a id="member-f-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationoutcomes-verified"></a>

##### `Verified`

```csharp
const string Verified
```

The observed proof matched and the domain ownership declaration was verified.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest"></a>

### `TenantDomainOwnershipProofVerificationRequest`

Describes a tenant-domain ownership proof verification runner request.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofVerificationRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-ctor-system-string-system-string-system-string-system-string-system-uri-system-uri-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-boolean-system-boolean-system-boolean-system-boolean-system-nullable-system-timespan-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofVerificationRequest`

```csharp
TenantDomainOwnershipProofVerificationRequest(string tenantId, string domainName, string verificationMethod, string observedProof, Uri collectionBaseUri, Uri dnsTxtResolverEndpoint, string source, string actor, DateTimeOffset? atUtc, DateTimeOffset? expiresAtUtc, string correlationId, bool issueChallengeWhenMissingExpectedProof, bool collectHttpProof, bool collectDnsTxtProof, bool recordPublicationPlan, TimeSpan? timeout, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof verification runner request.

Parameters:
- `tenantId`: The tenant identifier that owns the domain declaration.
- `domainName`: The domain name being verified.
- `verificationMethod`: The optional verification method boundary. When omitted, the existing declaration method or HTTP file is used.
- `observedProof`: Optional observed proof supplied by an application or provider pack.
- `collectionBaseUri`: The optional base URI used by HTTP file proof collection.
- `dnsTxtResolverEndpoint`: The optional DNS-over-HTTPS resolver endpoint used by DNS TXT proof collection.
- `source`: The source that requested the verification run.
- `actor`: The actor that requested the verification run when known.
- `atUtc`: The UTC timestamp used by the run. The runtime clock is used when omitted.
- `expiresAtUtc`: The optional UTC timestamp applied if proof evaluation verifies the declaration.
- `correlationId`: The optional correlation identifier for the run.
- `issueChallengeWhenMissingExpectedProof`: A value indicating whether the runner should issue a challenge when expected proof metadata is missing.
- `collectHttpProof`: A value indicating whether the runner should use the built-in HTTP proof collector for HTTP file declarations.
- `collectDnsTxtProof`: A value indicating whether the runner should use the built-in DNS TXT proof collector for DNS TXT declarations.
- `recordPublicationPlan`: A value indicating whether publication planning metadata should be recorded.
- `timeout`: The optional per-request proof collection timeout.
- `metadata`: Optional proof verification runner metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested the verification run when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used by the run.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-collectdnstxtproof"></a>

##### `CollectDnsTxtProof`

```csharp
bool CollectDnsTxtProof { get; }
```

Gets a value indicating whether the runner should use the built-in DNS TXT proof collector for DNS TXT declarations.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-collecthttpproof"></a>

##### `CollectHttpProof`

```csharp
bool CollectHttpProof { get; }
```

Gets a value indicating whether the runner should use the built-in HTTP proof collector for HTTP file declarations.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-collectionbaseuri"></a>

##### `CollectionBaseUri`

```csharp
Uri CollectionBaseUri { get; }
```

Gets the optional base URI used by HTTP file proof collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the run.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-dnstxtresolverendpoint"></a>

##### `DnsTxtResolverEndpoint`

```csharp
Uri DnsTxtResolverEndpoint { get; }
```

Gets the optional DNS-over-HTTPS resolver endpoint used by DNS TXT proof collection.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name being verified.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-expiresatutc"></a>

##### `ExpiresAtUtc`

```csharp
DateTimeOffset? ExpiresAtUtc { get; }
```

Gets the optional UTC timestamp applied if proof evaluation verifies the declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-issuechallengewhenmissingexpectedproof"></a>

##### `IssueChallengeWhenMissingExpectedProof`

```csharp
bool IssueChallengeWhenMissingExpectedProof { get; }
```

Gets a value indicating whether the runner should issue a challenge when expected proof metadata is missing.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional proof verification runner metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-observedproof"></a>

##### `ObservedProof`

```csharp
string ObservedProof { get; }
```

Gets optional observed proof supplied by an application or provider pack.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-recordpublicationplan"></a>

##### `RecordPublicationPlan`

```csharp
bool RecordPublicationPlan { get; }
```

Gets a value indicating whether publication planning metadata should be recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested the verification run.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the domain declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-timeout"></a>

##### `Timeout`

```csharp
TimeSpan? Timeout { get; }
```

Gets the optional per-request HTTP collection timeout.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationrequest-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the optional verification method boundary.

<a id="type-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult"></a>

### `TenantDomainOwnershipProofVerificationResult`

Describes the result of one tenant-domain ownership proof verification runner attempt.

#### Declaration
```csharp
public sealed class TenantDomainOwnershipProofVerificationResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-datetimeoffset-cephalon-multitenancy-governance-services-tenantdomainownershipproofchallengeresult-cephalon-multitenancy-governance-services-tenantdomainownershipproofpublicationplanresult-cephalon-multitenancy-governance-services-tenantdomainownershiphttpproofcollectionresult-cephalon-multitenancy-governance-services-tenantdomainownershipdnstxtproofcollectionresult-cephalon-multitenancy-governance-services-tenantdomainownershipproofevaluationresult-cephalon-multitenancy-governance-services-tenantdomainownershipdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantDomainOwnershipProofVerificationResult`

```csharp
TenantDomainOwnershipProofVerificationResult(string tenantId, string domainName, string verificationMethod, string outcome, bool verified, bool rejected, bool challengeIssued, bool publicationPlanned, bool proofCollected, bool proofEvaluated, DateTimeOffset ranAtUtc, TenantDomainOwnershipProofChallengeResult challengeResult, TenantDomainOwnershipProofPublicationPlanResult publicationPlanResult, TenantDomainOwnershipHttpProofCollectionResult httpProofCollectionResult, TenantDomainOwnershipDnsTxtProofCollectionResult dnsTxtProofCollectionResult, TenantDomainOwnershipProofEvaluationResult evaluationResult, TenantDomainOwnershipDescriptor domainOwnership, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-domain ownership proof verification runner result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `domainName`: The canonical domain name that was evaluated.
- `verificationMethod`: The verification method used by the runner.
- `outcome`: The stable proof verification runner outcome.
- `verified`: A value indicating whether the run verified the declaration.
- `rejected`: A value indicating whether the run rejected the declaration.
- `challengeIssued`: A value indicating whether the run issued a proof challenge.
- `publicationPlanned`: A value indicating whether the run generated publication instructions.
- `proofCollected`: A value indicating whether the run collected proof content.
- `proofEvaluated`: A value indicating whether the run evaluated observed proof.
- `ranAtUtc`: The UTC timestamp when the runner executed.
- `challengeResult`: The nested proof challenge result when one ran.
- `publicationPlanResult`: The nested publication plan result when one ran.
- `httpProofCollectionResult`: The nested HTTP proof collection result when one ran.
- `dnsTxtProofCollectionResult`: The nested DNS TXT proof collection result when one ran.
- `evaluationResult`: The nested proof evaluation result when one ran.
- `domainOwnership`: The matching or resulting domain ownership descriptor when one exists.
- `reason`: The operator-facing proof verification reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-challengeissued"></a>

##### `ChallengeIssued`

```csharp
bool ChallengeIssued { get; }
```

Gets a value indicating whether the run issued a proof challenge.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-challengeresult"></a>

##### `ChallengeResult`

```csharp
TenantDomainOwnershipProofChallengeResult ChallengeResult { get; }
```

Gets the nested proof challenge result when one ran.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-dnstxtproofcollectionresult"></a>

##### `DnsTxtProofCollectionResult`

```csharp
TenantDomainOwnershipDnsTxtProofCollectionResult DnsTxtProofCollectionResult { get; }
```

Gets the nested DNS TXT proof collection result when one ran.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; }
```

Gets the canonical domain name that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-domainownership"></a>

##### `DomainOwnership`

```csharp
TenantDomainOwnershipDescriptor DomainOwnership { get; }
```

Gets the matching or resulting domain ownership descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-evaluationresult"></a>

##### `EvaluationResult`

```csharp
TenantDomainOwnershipProofEvaluationResult EvaluationResult { get; }
```

Gets the nested proof evaluation result when one ran.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-httpproofcollectionresult"></a>

##### `HttpProofCollectionResult`

```csharp
TenantDomainOwnershipHttpProofCollectionResult HttpProofCollectionResult { get; }
```

Gets the nested HTTP proof collection result when one ran.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable proof verification runner outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-proofcollected"></a>

##### `ProofCollected`

```csharp
bool ProofCollected { get; }
```

Gets a value indicating whether the run collected proof content.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-proofevaluated"></a>

##### `ProofEvaluated`

```csharp
bool ProofEvaluated { get; }
```

Gets a value indicating whether the run evaluated observed proof.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-publicationplanned"></a>

##### `PublicationPlanned`

```csharp
bool PublicationPlanned { get; }
```

Gets a value indicating whether the run generated publication instructions.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-publicationplanresult"></a>

##### `PublicationPlanResult`

```csharp
TenantDomainOwnershipProofPublicationPlanResult PublicationPlanResult { get; }
```

Gets the nested publication plan result when one ran.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-ranatutc"></a>

##### `RanAtUtc`

```csharp
DateTimeOffset RanAtUtc { get; }
```

Gets the UTC timestamp when the runner executed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing proof verification reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-rejected"></a>

##### `Rejected`

```csharp
bool Rejected { get; }
```

Gets a value indicating whether the run rejected the declaration.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-verificationmethod"></a>

##### `VerificationMethod`

```csharp
string VerificationMethod { get; }
```

Gets the verification method used by the runner.

<a id="member-p-cephalon-multitenancy-governance-services-tenantdomainownershipproofverificationresult-verified"></a>

##### `Verified`

```csharp
bool Verified { get; }
```

Gets a value indicating whether the run verified the declaration.

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

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext"></a>

### `TenantInvitationDeliveryContext`

Describes the tenant invitation payload passed to an invitation delivery sender.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryContext
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-ctor-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-system-string-system-string-system-string-system-string-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryContext`

```csharp
TenantInvitationDeliveryContext(TenantInvitationDescriptor invitation, string channel, string requestedSenderId, string source, string actor, DateTimeOffset dispatchedAtUtc, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery context.

Parameters:
- `invitation`: The invitation being delivered.
- `channel`: The requested delivery channel.
- `requestedSenderId`: The requested sender identifier when one was specified.
- `source`: The source that requested delivery dispatch.
- `actor`: The actor that requested delivery dispatch when known.
- `dispatchedAtUtc`: The UTC timestamp used for dispatch.
- `correlationId`: The optional correlation identifier for delivery dispatch.
- `metadata`: Optional request metadata for the sender.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested delivery dispatch when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-channel"></a>

##### `Channel`

```csharp
string Channel { get; }
```

Gets the requested delivery channel.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for delivery dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-dispatchedatutc"></a>

##### `DispatchedAtUtc`

```csharp
DateTimeOffset DispatchedAtUtc { get; }
```

Gets the UTC timestamp used for dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing invitation name.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-invitation"></a>

##### `Invitation`

```csharp
TenantInvitationDescriptor Invitation { get; }
```

Gets the invitation being delivered.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-inviteeid"></a>

##### `InviteeId`

```csharp
string InviteeId { get; }
```

Gets the invitee identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-inviteekind"></a>

##### `InviteeKind`

```csharp
string InviteeKind { get; }
```

Gets the invitee kind, such as user, group, service, or organization.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional request metadata for the sender.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-requestedsenderid"></a>

##### `RequestedSenderId`

```csharp
string RequestedSenderId { get; }
```

Gets the requested sender identifier when one was specified.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-roles"></a>

##### `Roles`

```csharp
IReadOnlyList<string> Roles { get; }
```

Gets the tenant-local roles proposed by the invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested delivery dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverycontext-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys"></a>

### `TenantInvitationDeliveryMetadataKeys`

Defines stable metadata keys written by tenant invitation delivery dispatch and status reconciliation.

#### Declaration
```csharp
public static class TenantInvitationDeliveryMetadataKeys
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverydispatchownership"></a>

##### `DeliveryDispatchOwnership`

```csharp
const string DeliveryDispatchOwnership
```

Metadata key describing Cephalon ownership of the host-agnostic dispatch pipeline.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretrybackgroundownership"></a>

##### `DeliveryRetryBackgroundOwnership`

```csharp
const string DeliveryRetryBackgroundOwnership
```

Metadata key describing Cephalon ownership of automatic background retry scheduling.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretrybackgroundscheduling"></a>

##### `DeliveryRetryBackgroundScheduling`

```csharp
const string DeliveryRetryBackgroundScheduling
```

Metadata key that marks a dispatch request created by automatic background retry scheduling.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryexecution"></a>

##### `DeliveryRetryExecution`

```csharp
const string DeliveryRetryExecution
```

Metadata key that marks a dispatch request created by the retry runner.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueueattempt"></a>

##### `DeliveryRetryQueueAttempt`

```csharp
const string DeliveryRetryQueueAttempt
```

Metadata key containing the retry queue attempt number.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuedelayseconds"></a>

##### `DeliveryRetryQueueDelaySeconds`

```csharp
const string DeliveryRetryQueueDelaySeconds
```

Metadata key containing the retry delay in seconds.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueueentrycount"></a>

##### `DeliveryRetryQueueEntryCount`

```csharp
const string DeliveryRetryQueueEntryCount
```

Metadata key containing the total number of retained invitation delivery retry entries.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueueentryid"></a>

##### `DeliveryRetryQueueEntryId`

```csharp
const string DeliveryRetryQueueEntryId
```

Metadata key containing the invitation delivery retry queue entry identifier.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuelastattemptatutc"></a>

##### `DeliveryRetryQueueLastAttemptAtUtc`

```csharp
const string DeliveryRetryQueueLastAttemptAtUtc
```

Metadata key containing the UTC timestamp of the latest retry attempt.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuelastoutcome"></a>

##### `DeliveryRetryQueueLastOutcome`

```csharp
const string DeliveryRetryQueueLastOutcome
```

Metadata key containing the latest retry dispatch outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuelastreason"></a>

##### `DeliveryRetryQueueLastReason`

```csharp
const string DeliveryRetryQueueLastReason
```

Metadata key containing the latest retry dispatch reason.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuemaxattempts"></a>

##### `DeliveryRetryQueueMaxAttempts`

```csharp
const string DeliveryRetryQueueMaxAttempts
```

Metadata key containing the maximum attempts configured for retry queue entries.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuenextattemptatutc"></a>

##### `DeliveryRetryQueueNextAttemptAtUtc`

```csharp
const string DeliveryRetryQueueNextAttemptAtUtc
```

Metadata key containing the UTC timestamp when the next retry attempt is due.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueueoutcome"></a>

##### `DeliveryRetryQueueOutcome`

```csharp
const string DeliveryRetryQueueOutcome
```

Metadata key describing whether a sender failure was queued for retry.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueueownership"></a>

##### `DeliveryRetryQueueOwnership`

```csharp
const string DeliveryRetryQueueOwnership
```

Metadata key describing Cephalon ownership of the invitation delivery retry queue.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuependingcount"></a>

##### `DeliveryRetryQueuePendingCount`

```csharp
const string DeliveryRetryQueuePendingCount
```

Metadata key containing the number of pending invitation delivery retry entries.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuestoredurable"></a>

##### `DeliveryRetryQueueStoreDurable`

```csharp
const string DeliveryRetryQueueStoreDurable
```

Metadata key describing whether the invitation delivery retry queue is durable.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliveryretryqueuestorekind"></a>

##### `DeliveryRetryQueueStoreKind`

```csharp
const string DeliveryRetryQueueStoreKind
```

Metadata key describing the invitation delivery retry queue storage kind.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverystatusobservationid"></a>

##### `DeliveryStatusObservationId`

```csharp
const string DeliveryStatusObservationId
```

Metadata key containing the delivery status observation identifier recorded by the observation store.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverystatusobservationstoredurable"></a>

##### `DeliveryStatusObservationStoreDurable`

```csharp
const string DeliveryStatusObservationStoreDurable
```

Metadata key describing whether the delivery status observation store is durable.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverystatusobservationstoreexceptiontype"></a>

##### `DeliveryStatusObservationStoreExceptionType`

```csharp
const string DeliveryStatusObservationStoreExceptionType
```

Metadata key containing the exception type observed when delivery status observation storage fails.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverystatusobservationstorehistorylimit"></a>

##### `DeliveryStatusObservationStoreHistoryLimit`

```csharp
const string DeliveryStatusObservationStoreHistoryLimit
```

Metadata key describing the retention limit used by the delivery status observation store.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverystatusobservationstorekind"></a>

##### `DeliveryStatusObservationStoreKind`

```csharp
const string DeliveryStatusObservationStoreKind
```

Metadata key describing the delivery status observation store kind.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverystatusobservationstoreoutcome"></a>

##### `DeliveryStatusObservationStoreOutcome`

```csharp
const string DeliveryStatusObservationStoreOutcome
```

Metadata key describing whether the delivery status observation store recorded the observation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverystatusobservationstoreownership"></a>

##### `DeliveryStatusObservationStoreOwnership`

```csharp
const string DeliveryStatusObservationStoreOwnership
```

Metadata key describing Cephalon ownership of delivery status observation storage.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-deliverystatusreconciliationownership"></a>

##### `DeliveryStatusReconciliationOwnership`

```csharp
const string DeliveryStatusReconciliationOwnership
```

Metadata key describing Cephalon ownership of host-agnostic status reconciliation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-externaldeliveryownership"></a>

##### `ExternalDeliveryOwnership`

```csharp
const string ExternalDeliveryOwnership
```

Metadata key describing who owns provider-specific external delivery.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-externaldeliverystatusownership"></a>

##### `ExternalDeliveryStatusOwnership`

```csharp
const string ExternalDeliveryStatusOwnership
```

Metadata key describing who owns provider-specific external delivery status truth.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliveryactor"></a>

##### `LastDeliveryActor`

```csharp
const string LastDeliveryActor
```

Metadata key containing the actor that requested the last delivery dispatch.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverychannel"></a>

##### `LastDeliveryChannel`

```csharp
const string LastDeliveryChannel
```

Metadata key containing the delivery channel used by the last dispatch attempt.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverycorrelationid"></a>

##### `LastDeliveryCorrelationId`

```csharp
const string LastDeliveryCorrelationId
```

Metadata key containing the correlation identifier for the last delivery dispatch.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverydispatchedatutc"></a>

##### `LastDeliveryDispatchedAtUtc`

```csharp
const string LastDeliveryDispatchedAtUtc
```

Metadata key containing the UTC timestamp when delivery dispatch was evaluated.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliveryoutcome"></a>

##### `LastDeliveryOutcome`

```csharp
const string LastDeliveryOutcome
```

Metadata key containing the last tenant invitation delivery outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliveryprovidermessageid"></a>

##### `LastDeliveryProviderMessageId`

```csharp
const string LastDeliveryProviderMessageId
```

Metadata key containing the provider message identifier returned by the sender.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverysenderid"></a>

##### `LastDeliverySenderId`

```csharp
const string LastDeliverySenderId
```

Metadata key containing the sender identifier used by the last dispatch attempt.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverysource"></a>

##### `LastDeliverySource`

```csharp
const string LastDeliverySource
```

Metadata key containing the source that requested the last delivery dispatch.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatus"></a>

##### `LastDeliveryStatus`

```csharp
const string LastDeliveryStatus
```

Metadata key containing the last reconciled delivery status.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatusactor"></a>

##### `LastDeliveryStatusActor`

```csharp
const string LastDeliveryStatusActor
```

Metadata key containing the actor that reported the last delivery status observation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatuschannel"></a>

##### `LastDeliveryStatusChannel`

```csharp
const string LastDeliveryStatusChannel
```

Metadata key containing the delivery channel associated with the last delivery status observation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatuscorrelationid"></a>

##### `LastDeliveryStatusCorrelationId`

```csharp
const string LastDeliveryStatusCorrelationId
```

Metadata key containing the correlation identifier for the last delivery status observation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatusobservedatutc"></a>

##### `LastDeliveryStatusObservedAtUtc`

```csharp
const string LastDeliveryStatusObservedAtUtc
```

Metadata key containing the UTC timestamp when delivery status was observed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatusprovidermessageid"></a>

##### `LastDeliveryStatusProviderMessageId`

```csharp
const string LastDeliveryStatusProviderMessageId
```

Metadata key containing the provider message identifier associated with the last delivery status observation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatusreason"></a>

##### `LastDeliveryStatusReason`

```csharp
const string LastDeliveryStatusReason
```

Metadata key containing the provider or receiver reason for the last delivery status observation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatusreconciliationoutcome"></a>

##### `LastDeliveryStatusReconciliationOutcome`

```csharp
const string LastDeliveryStatusReconciliationOutcome
```

Metadata key containing the last delivery status reconciliation outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatussenderid"></a>

##### `LastDeliveryStatusSenderId`

```csharp
const string LastDeliveryStatusSenderId
```

Metadata key containing the sender identifier associated with the last delivery status observation.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverymetadatakeys-lastdeliverystatussource"></a>

##### `LastDeliveryStatusSource`

```csharp
const string LastDeliveryStatusSource
```

Metadata key containing the source that reported the last delivery status observation.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes"></a>

### `TenantInvitationDeliveryOutcomes`

Defines stable outcomes for tenant invitation delivery dispatch.

#### Declaration
```csharp
public static class TenantInvitationDeliveryOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Invitation delivery dispatch is disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-dispatched"></a>

##### `Dispatched`

```csharp
const string Dispatched
```

The invitation was dispatched through a configured sender.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-invitationexpired"></a>

##### `InvitationExpired`

```csharp
const string InvitationExpired
```

The requested invitation expired before dispatch.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-invitationnotfound"></a>

##### `InvitationNotFound`

```csharp
const string InvitationNotFound
```

The requested invitation was not found.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-invitationnotpending"></a>

##### `InvitationNotPending`

```csharp
const string InvitationNotPending
```

The requested invitation is no longer pending.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-senderfailed"></a>

##### `SenderFailed`

```csharp
const string SenderFailed
```

The sender failed or returned a failed outcome.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-sendernotconfigured"></a>

##### `SenderNotConfigured`

```csharp
const string SenderNotConfigured
```

No matching delivery sender was registered.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

The dispatch outcome could not be persisted.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryoutcomes-suppressed"></a>

##### `Suppressed`

```csharp
const string Suppressed
```

The sender deliberately suppressed delivery.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest"></a>

### `TenantInvitationDeliveryRequest`

Describes a tenant invitation delivery dispatch request.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-boolean-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryRequest`

```csharp
TenantInvitationDeliveryRequest(string tenantId, string invitationId, string channel, string senderId, string source, string actor, DateTimeOffset? atUtc, string correlationId, bool recordDelivery, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery dispatch request.

Parameters:
- `tenantId`: The tenant identifier that owns the invitation.
- `invitationId`: The invitation identifier to deliver.
- `channel`: The requested delivery channel.
- `senderId`: The preferred delivery sender identifier.
- `source`: The source that requested delivery dispatch.
- `actor`: The actor that requested delivery dispatch when known.
- `atUtc`: The UTC timestamp used for dispatch. The runtime clock is used when omitted.
- `correlationId`: The optional correlation identifier for delivery dispatch.
- `recordDelivery`: A value indicating whether delivery outcome metadata should be recorded on the invitation.
- `metadata`: Optional delivery dispatch metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that requested delivery dispatch when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-channel"></a>

##### `Channel`

```csharp
string Channel { get; }
```

Gets the requested delivery channel.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for delivery dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier to deliver.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional delivery dispatch metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-recorddelivery"></a>

##### `RecordDelivery`

```csharp
bool RecordDelivery { get; }
```

Gets a value indicating whether delivery outcome metadata should be recorded on the invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; }
```

Gets the preferred delivery sender identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that requested delivery dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the invitation.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult"></a>

### `TenantInvitationDeliveryResult`

Describes the result of tenant invitation delivery dispatch.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-ctor-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-string-system-string-system-string-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryResult`

```csharp
TenantInvitationDeliveryResult(string tenantId, string invitationId, string outcome, bool dispatched, bool recorded, DateTimeOffset dispatchedAtUtc, string channel, string senderId, string providerMessageId, TenantInvitationDescriptor invitation, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery dispatch result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `invitationId`: The invitation identifier that was evaluated.
- `outcome`: The stable delivery dispatch outcome.
- `dispatched`: A value indicating whether a sender accepted the dispatch.
- `recorded`: A value indicating whether delivery outcome metadata was recorded.
- `dispatchedAtUtc`: The UTC timestamp used for dispatch.
- `channel`: The delivery channel used by the dispatch attempt.
- `senderId`: The delivery sender identifier used by the dispatch attempt.
- `providerMessageId`: The provider message identifier returned by the sender.
- `invitation`: The resulting invitation descriptor when one exists.
- `reason`: The operator-facing delivery dispatch reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-channel"></a>

##### `Channel`

```csharp
string Channel { get; }
```

Gets the delivery channel used by the dispatch attempt.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-dispatched"></a>

##### `Dispatched`

```csharp
bool Dispatched { get; }
```

Gets a value indicating whether a sender accepted the dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-dispatchedatutc"></a>

##### `DispatchedAtUtc`

```csharp
DateTimeOffset DispatchedAtUtc { get; }
```

Gets the UTC timestamp used for dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-invitation"></a>

##### `Invitation`

```csharp
TenantInvitationDescriptor Invitation { get; }
```

Gets the resulting invitation descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable delivery dispatch outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the provider message identifier returned by the sender.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing delivery dispatch reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-recorded"></a>

##### `Recorded`

```csharp
bool Recorded { get; }
```

Gets a value indicating whether delivery outcome metadata was recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; }
```

Gets the delivery sender identifier used by the dispatch attempt.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor"></a>

### `TenantInvitationDeliveryRetryDescriptor`

Describes one queued tenant invitation delivery retry entry.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryRetryDescriptor
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-string-system-int32-system-int32-system-datetimeoffset-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryRetryDescriptor`

```csharp
TenantInvitationDeliveryRetryDescriptor(string retryId, string tenantId, string invitationId, string channel, string senderId, string source, string actor, string correlationId, bool recordDelivery, string status, int attemptCount, int maxAttempts, DateTimeOffset createdAtUtc, DateTimeOffset nextAttemptAtUtc, DateTimeOffset? lastAttemptAtUtc, string lastOutcome, string lastReason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery retry descriptor.

Parameters:
- `retryId`: The stable retry entry identifier.
- `tenantId`: The tenant identifier that owns the invitation.
- `invitationId`: The invitation identifier to retry.
- `channel`: The delivery channel to retry.
- `senderId`: The sender identifier to retry when specified.
- `source`: The source recorded on the next retry attempt.
- `actor`: The actor recorded on the next retry attempt.
- `correlationId`: The correlation identifier retained for retry attempts.
- `recordDelivery`: A value indicating whether retry attempts should record delivery metadata.
- `status`: The retry entry status.
- `attemptCount`: The number of dispatch attempts represented by this entry, including the original failed attempt.
- `maxAttempts`: The maximum number of dispatch attempts allowed for this entry.
- `createdAtUtc`: The UTC timestamp when the retry entry was created.
- `nextAttemptAtUtc`: The UTC timestamp when the next attempt is due.
- `lastAttemptAtUtc`: The UTC timestamp of the latest dispatch attempt.
- `lastOutcome`: The latest delivery dispatch outcome.
- `lastReason`: The latest delivery dispatch reason.
- `metadata`: Optional retry metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor recorded on retry attempts.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-attemptcount"></a>

##### `AttemptCount`

```csharp
int AttemptCount { get; }
```

Gets the number of dispatch attempts represented by this entry.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-channel"></a>

##### `Channel`

```csharp
string Channel { get; }
```

Gets the delivery channel to retry.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier retained for retry attempts.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-createdatutc"></a>

##### `CreatedAtUtc`

```csharp
DateTimeOffset CreatedAtUtc { get; }
```

Gets the UTC timestamp when the retry entry was created.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier to retry.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-lastattemptatutc"></a>

##### `LastAttemptAtUtc`

```csharp
DateTimeOffset? LastAttemptAtUtc { get; }
```

Gets the UTC timestamp of the latest dispatch attempt.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; }
```

Gets the latest delivery dispatch outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-lastreason"></a>

##### `LastReason`

```csharp
string LastReason { get; }
```

Gets the latest delivery dispatch reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-maxattempts"></a>

##### `MaxAttempts`

```csharp
int MaxAttempts { get; }
```

Gets the maximum number of dispatch attempts allowed for this entry.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional retry metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-nextattemptatutc"></a>

##### `NextAttemptAtUtc`

```csharp
DateTimeOffset NextAttemptAtUtc { get; }
```

Gets the UTC timestamp when the next attempt is due.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-recorddelivery"></a>

##### `RecordDelivery`

```csharp
bool RecordDelivery { get; }
```

Gets a value indicating whether retry attempts should record delivery metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-retryid"></a>

##### `RetryId`

```csharp
string RetryId { get; }
```

Gets the stable retry entry identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; }
```

Gets the sender identifier to retry when specified.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source recorded on retry attempts.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the retry entry status.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the invitation.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrydescriptor-withretrystate-system-string-system-int32-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `WithRetryState`

```csharp
TenantInvitationDeliveryRetryDescriptor WithRetryState(string status, int attemptCount, DateTimeOffset nextAttemptAtUtc, DateTimeOffset? lastAttemptAtUtc, string lastOutcome, string lastReason, IReadOnlyDictionary<string, string> metadata)
```

Creates a copy of this retry entry with updated retry state.

Returns: The updated retry descriptor.

Parameters:
- `status`: The updated status.
- `attemptCount`: The updated attempt count.
- `nextAttemptAtUtc`: The updated next-attempt timestamp.
- `lastAttemptAtUtc`: The updated last-attempt timestamp.
- `lastOutcome`: The updated latest outcome.
- `lastReason`: The updated latest reason.
- `metadata`: The updated metadata.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryoutcomes"></a>

### `TenantInvitationDeliveryRetryOutcomes`

Defines stable outcomes for tenant invitation delivery retry runner passes.

#### Declaration
```csharp
public static class TenantInvitationDeliveryRetryOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Invitation delivery retry queue processing is disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryoutcomes-failed"></a>

##### `Failed`

```csharp
const string Failed
```

No attempted retry entries dispatched successfully.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryoutcomes-nopendingretries"></a>

##### `NoPendingRetries`

```csharp
const string NoPendingRetries
```

No pending retry entries matched the request.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryoutcomes-partial"></a>

##### `Partial`

```csharp
const string Partial
```

Some attempted retry entries succeeded and some remained pending or terminal.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryoutcomes-retried"></a>

##### `Retried`

```csharp
const string Retried
```

Every attempted retry entry dispatched successfully.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest"></a>

### `TenantInvitationDeliveryRetryRequest`

Describes a bounded tenant invitation delivery retry runner request.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryRetryRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-ctor-system-nullable-system-datetimeoffset-system-nullable-system-int32-system-boolean-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryRetryRequest`

```csharp
TenantInvitationDeliveryRetryRequest(DateTimeOffset? atUtc, int? maxItems, bool dueOnly, string source, string actor, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery retry runner request.

Parameters:
- `atUtc`: The UTC timestamp used for retry evaluation.
- `maxItems`: The maximum number of retry entries to attempt.
- `dueOnly`: A value indicating whether entries scheduled after `atUtc` should be skipped.
- `source`: The source recorded on retry attempts.
- `actor`: The actor recorded on retry attempts.
- `correlationId`: The correlation identifier recorded on retry attempts.
- `metadata`: Optional retry runner metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor recorded on retry attempts.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset? AtUtc { get; }
```

Gets the UTC timestamp used for retry evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier recorded on retry attempts.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-dueonly"></a>

##### `DueOnly`

```csharp
bool DueOnly { get; }
```

Gets a value indicating whether entries scheduled after `AtUtc` should be skipped.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-maxitems"></a>

##### `MaxItems`

```csharp
int? MaxItems { get; }
```

Gets the maximum number of retry entries to attempt.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional retry runner metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source recorded on retry attempts.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult"></a>

### `TenantInvitationDeliveryRetryResult`

Describes the aggregate result of one tenant invitation delivery retry runner pass.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryRetryResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-ctor-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-datetimeoffset-system-collections-generic-ireadonlylist-cephalon-multitenancy-governance-services-tenantinvitationdeliveryresult-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryRetryResult`

```csharp
TenantInvitationDeliveryRetryResult(string outcome, int attemptedCount, int dispatchedCount, int failedCount, int exhaustedCount, int terminalCount, int remainingPendingCount, DateTimeOffset atUtc, IReadOnlyList<TenantInvitationDeliveryResult> deliveryResults, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery retry result.

Parameters:
- `outcome`: The stable retry runner outcome.
- `attemptedCount`: The number of retry entries attempted.
- `dispatchedCount`: The number of retry entries dispatched successfully.
- `failedCount`: The number of attempted entries that remained pending after failure.
- `exhaustedCount`: The number of attempted entries that exhausted their retry budget.
- `terminalCount`: The number of attempted entries that hit a terminal invitation state.
- `remainingPendingCount`: The number of pending entries retained after the pass.
- `atUtc`: The UTC timestamp used for retry evaluation.
- `deliveryResults`: The delivery dispatch results produced by this pass.
- `metadata`: Optional retry result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-attemptedcount"></a>

##### `AttemptedCount`

```csharp
int AttemptedCount { get; }
```

Gets the number of retry entries attempted.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-atutc"></a>

##### `AtUtc`

```csharp
DateTimeOffset AtUtc { get; }
```

Gets the UTC timestamp used for retry evaluation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-deliveryresults"></a>

##### `DeliveryResults`

```csharp
IReadOnlyList<TenantInvitationDeliveryResult> DeliveryResults { get; }
```

Gets the delivery dispatch results produced by this pass.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-dispatchedcount"></a>

##### `DispatchedCount`

```csharp
int DispatchedCount { get; }
```

Gets the number of retry entries dispatched successfully.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-exhaustedcount"></a>

##### `ExhaustedCount`

```csharp
int ExhaustedCount { get; }
```

Gets the number of attempted entries that exhausted their retry budget.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; }
```

Gets the number of attempted entries that remained pending after failure.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional retry result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable retry runner outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-remainingpendingcount"></a>

##### `RemainingPendingCount`

```csharp
int RemainingPendingCount { get; }
```

Gets the number of pending entries retained after the pass.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryresult-terminalcount"></a>

##### `TerminalCount`

```csharp
int TerminalCount { get; }
```

Gets the number of attempted entries that hit a terminal invitation state.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot"></a>

### `TenantInvitationDeliveryRetryRuntimeSnapshot`

Describes the latest runtime state of automatic tenant-invitation delivery retry scheduling.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryRetryRuntimeSnapshot
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-ctor-system-boolean-system-string-system-int32-system-int32-system-boolean-system-int64-system-int64-system-int64-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryRetryRuntimeSnapshot`

```csharp
TenantInvitationDeliveryRetryRuntimeSnapshot(bool enabled, string ownership, int intervalSeconds, int maxItems, bool runOnStartup, long runCount, long successfulRunCount, long failedRunCount, DateTimeOffset? lastStartedAtUtc, DateTimeOffset? lastCompletedAtUtc, string lastOutcome, int lastAttemptedCount, int lastDispatchedCount, int lastFailedCount, int lastExhaustedCount, int lastTerminalCount, int lastRemainingPendingCount, string lastError, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant-invitation delivery retry scheduling runtime snapshot.

Parameters:
- `enabled`: A value indicating whether automatic background retry scheduling is effectively enabled.
- `ownership`: The automatic background retry scheduling ownership mode.
- `intervalSeconds`: The effective background retry interval in seconds.
- `maxItems`: The effective retry entry limit for one scheduled pass.
- `runOnStartup`: A value indicating whether background retry scheduling runs once during hosted-service startup.
- `runCount`: The number of background retry passes that reached a completed or failed terminal state.
- `successfulRunCount`: The number of background retry passes that completed without an unhandled failure.
- `failedRunCount`: The number of background retry passes that failed before producing a retry result.
- `lastStartedAtUtc`: The UTC timestamp when the latest background retry pass started.
- `lastCompletedAtUtc`: The UTC timestamp when the latest background retry pass completed or failed.
- `lastOutcome`: The latest retry runner outcome.
- `lastAttemptedCount`: The latest attempted retry-entry count.
- `lastDispatchedCount`: The latest successfully dispatched retry-entry count.
- `lastFailedCount`: The latest still-retryable failed retry-entry count.
- `lastExhaustedCount`: The latest exhausted retry-entry count.
- `lastTerminalCount`: The latest terminal retry-entry count.
- `lastRemainingPendingCount`: The latest remaining pending retry-entry count.
- `lastError`: The latest unhandled background retry error message.
- `metadata`: Optional runtime metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; }
```

Gets a value indicating whether automatic background retry scheduling is effectively enabled.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-failedruncount"></a>

##### `FailedRunCount`

```csharp
long FailedRunCount { get; }
```

Gets the number of background retry passes that failed before producing a retry result.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-intervalseconds"></a>

##### `IntervalSeconds`

```csharp
int IntervalSeconds { get; }
```

Gets the effective background retry interval in seconds.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lastattemptedcount"></a>

##### `LastAttemptedCount`

```csharp
int LastAttemptedCount { get; }
```

Gets the latest attempted retry-entry count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lastcompletedatutc"></a>

##### `LastCompletedAtUtc`

```csharp
DateTimeOffset? LastCompletedAtUtc { get; }
```

Gets the UTC timestamp when the latest background retry pass completed or failed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lastdispatchedcount"></a>

##### `LastDispatchedCount`

```csharp
int LastDispatchedCount { get; }
```

Gets the latest successfully dispatched retry-entry count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; }
```

Gets the latest unhandled background retry error message.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lastexhaustedcount"></a>

##### `LastExhaustedCount`

```csharp
int LastExhaustedCount { get; }
```

Gets the latest exhausted retry-entry count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lastfailedcount"></a>

##### `LastFailedCount`

```csharp
int LastFailedCount { get; }
```

Gets the latest still-retryable failed retry-entry count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; }
```

Gets the latest retry runner outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lastremainingpendingcount"></a>

##### `LastRemainingPendingCount`

```csharp
int LastRemainingPendingCount { get; }
```

Gets the latest remaining pending retry-entry count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-laststartedatutc"></a>

##### `LastStartedAtUtc`

```csharp
DateTimeOffset? LastStartedAtUtc { get; }
```

Gets the UTC timestamp when the latest background retry pass started.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-lastterminalcount"></a>

##### `LastTerminalCount`

```csharp
int LastTerminalCount { get; }
```

Gets the latest terminal retry-entry count.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-maxitems"></a>

##### `MaxItems`

```csharp
int MaxItems { get; }
```

Gets the effective retry entry limit for one scheduled pass.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional runtime metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; }
```

Gets the automatic background retry scheduling ownership mode.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-runcount"></a>

##### `RunCount`

```csharp
long RunCount { get; }
```

Gets the number of background retry passes that reached a completed or failed terminal state.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-runonstartup"></a>

##### `RunOnStartup`

```csharp
bool RunOnStartup { get; }
```

Gets a value indicating whether background retry scheduling runs once during hosted-service startup.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretryruntimesnapshot-successfulruncount"></a>

##### `SuccessfulRunCount`

```csharp
long SuccessfulRunCount { get; }
```

Gets the number of background retry passes that completed without an unhandled failure.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrystatuses"></a>

### `TenantInvitationDeliveryRetryStatuses`

Defines stable statuses for tenant invitation delivery retry queue entries.

#### Declaration
```csharp
public static class TenantInvitationDeliveryRetryStatuses
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrystatuses-dispatched"></a>

##### `Dispatched`

```csharp
const string Dispatched
```

The retry entry was dispatched and removed from the active retry queue.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrystatuses-exhausted"></a>

##### `Exhausted`

```csharp
const string Exhausted
```

The retry entry exhausted its configured retry budget.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrystatuses-pending"></a>

##### `Pending`

```csharp
const string Pending
```

The retry entry is waiting for another attempt.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliveryretrystatuses-terminal"></a>

##### `Terminal`

```csharp
const string Terminal
```

The retry entry hit a terminal invitation state that should not be retried.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor"></a>

### `TenantInvitationDeliveryRunDescriptor`

Describes one observed tenant invitation delivery dispatch attempt.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryRunDescriptor
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-ctor-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryRunDescriptor`

```csharp
TenantInvitationDeliveryRunDescriptor(string tenantId, string invitationId, string outcome, bool dispatched, bool recorded, DateTimeOffset dispatchedAtUtc, string channel, string senderId, string providerMessageId, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery dispatch run descriptor.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `invitationId`: The invitation identifier that was evaluated.
- `outcome`: The stable delivery dispatch outcome.
- `dispatched`: A value indicating whether a sender accepted the dispatch.
- `recorded`: A value indicating whether delivery outcome metadata was recorded.
- `dispatchedAtUtc`: The UTC timestamp used for dispatch.
- `channel`: The delivery channel used by the dispatch attempt.
- `senderId`: The delivery sender identifier used by the dispatch attempt.
- `providerMessageId`: The provider message identifier returned by the sender.
- `reason`: The operator-facing delivery dispatch reason.
- `metadata`: Optional run metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-channel"></a>

##### `Channel`

```csharp
string Channel { get; }
```

Gets the delivery channel used by the dispatch attempt.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-dispatched"></a>

##### `Dispatched`

```csharp
bool Dispatched { get; }
```

Gets a value indicating whether a sender accepted the dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-dispatchedatutc"></a>

##### `DispatchedAtUtc`

```csharp
DateTimeOffset DispatchedAtUtc { get; }
```

Gets the UTC timestamp used for dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional run metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable delivery dispatch outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the provider message identifier returned by the sender.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing delivery dispatch reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-recorded"></a>

##### `Recorded`

```csharp
bool Recorded { get; }
```

Gets a value indicating whether delivery outcome metadata was recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; }
```

Gets the delivery sender identifier used by the dispatch attempt.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliveryrundescriptor-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliverysenderresult"></a>

### `TenantInvitationDeliverySenderResult`

Describes the outcome returned by a tenant invitation delivery sender.

#### Declaration
```csharp
public sealed class TenantInvitationDeliverySenderResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliverysenderresult-ctor-system-string-system-boolean-system-string-system-string-system-nullable-system-datetimeoffset-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliverySenderResult`

```csharp
TenantInvitationDeliverySenderResult(string outcome, bool dispatched, string providerMessageId, string reason, DateTimeOffset? dispatchedAtUtc, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery sender result.

Parameters:
- `outcome`: The sender outcome.
- `dispatched`: A value indicating whether the sender accepted the dispatch.
- `providerMessageId`: The provider message identifier returned by the sender.
- `reason`: The provider-facing outcome reason.
- `dispatchedAtUtc`: The UTC timestamp reported by the sender.
- `metadata`: Optional sender metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverysenderresult-dispatched"></a>

##### `Dispatched`

```csharp
bool Dispatched { get; }
```

Gets a value indicating whether the sender accepted the dispatch.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverysenderresult-dispatchedatutc"></a>

##### `DispatchedAtUtc`

```csharp
DateTimeOffset? DispatchedAtUtc { get; }
```

Gets the UTC timestamp reported by the sender.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverysenderresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional sender metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverysenderresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the sender outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverysenderresult-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the provider message identifier returned by the sender.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverysenderresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the provider-facing outcome reason.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatuses"></a>

### `TenantInvitationDeliveryStatuses`

Defines stable status values reported by tenant invitation delivery providers or receivers.

#### Declaration
```csharp
public static class TenantInvitationDeliveryStatuses
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatuses-accepted"></a>

##### `Accepted`

```csharp
const string Accepted
```

The delivery provider accepted the message for processing.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatuses-bounced"></a>

##### `Bounced`

```csharp
const string Bounced
```

The invitation bounced at the provider or receiver boundary.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatuses-deferred"></a>

##### `Deferred`

```csharp
const string Deferred
```

The invitation delivery was deferred by the provider or receiver boundary.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatuses-delivered"></a>

##### `Delivered`

```csharp
const string Delivered
```

The invitation was delivered to the provider-recognized recipient endpoint.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatuses-failed"></a>

##### `Failed`

```csharp
const string Failed
```

The invitation delivery failed.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatuses-suppressed"></a>

##### `Suppressed`

```csharp
const string Suppressed
```

The invitation delivery was suppressed by provider or policy rules.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatuses-unknown"></a>

##### `Unknown`

```csharp
const string Unknown
```

The delivery provider reported a status that could not be classified.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor"></a>

### `TenantInvitationDeliveryStatusObservationDescriptor`

Describes one recorded tenant invitation delivery status observation.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryStatusObservationDescriptor
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-datetimeoffset-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryStatusObservationDescriptor`

```csharp
TenantInvitationDeliveryStatusObservationDescriptor(string observationId, string tenantId, string invitationId, string status, string outcome, bool reconciled, bool recorded, DateTimeOffset observedAtUtc, DateTimeOffset recordedAtUtc, string providerMessageId, string senderId, string channel, string source, string actor, string correlationId, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery status observation descriptor.

Parameters:
- `observationId`: The stable observation identifier.
- `tenantId`: The tenant identifier that owns the invitation.
- `invitationId`: The invitation identifier associated with the observation.
- `status`: The normalized provider or receiver delivery status.
- `outcome`: The reconciliation outcome produced for the observation.
- `reconciled`: A value indicating whether the observation was accepted for the invitation.
- `recorded`: A value indicating whether invitation delivery status metadata was recorded.
- `observedAtUtc`: The UTC timestamp when the status was observed.
- `recordedAtUtc`: The UTC timestamp when Cephalon recorded the observation.
- `providerMessageId`: The provider message identifier associated with the observation.
- `senderId`: The delivery sender identifier associated with the observation.
- `channel`: The delivery channel associated with the observation.
- `source`: The source that reported the observation.
- `actor`: The actor that reported the observation when known.
- `correlationId`: The optional correlation identifier for the observation.
- `reason`: The provider or receiver status reason.
- `metadata`: Optional safe observation metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that reported the observation when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-channel"></a>

##### `Channel`

```csharp
string Channel { get; }
```

Gets the delivery channel associated with the observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier associated with the observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional safe observation metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-observationid"></a>

##### `ObservationId`

```csharp
string ObservationId { get; }
```

Gets the stable observation identifier.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; }
```

Gets the UTC timestamp when the status was observed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the reconciliation outcome produced for the observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the provider message identifier associated with the observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the provider or receiver status reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-reconciled"></a>

##### `Reconciled`

```csharp
bool Reconciled { get; }
```

Gets a value indicating whether the observation was accepted for the invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-recorded"></a>

##### `Recorded`

```csharp
bool Recorded { get; }
```

Gets a value indicating whether invitation delivery status metadata was recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-recordedatutc"></a>

##### `RecordedAtUtc`

```csharp
DateTimeOffset RecordedAtUtc { get; }
```

Gets the UTC timestamp when Cephalon recorded the observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; }
```

Gets the delivery sender identifier associated with the observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that reported the observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the normalized provider or receiver delivery status.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusobservationdescriptor-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the invitation.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationoutcomes"></a>

### `TenantInvitationDeliveryStatusReconciliationOutcomes`

Defines stable outcomes for tenant invitation delivery status reconciliation.

#### Declaration
```csharp
public static class TenantInvitationDeliveryStatusReconciliationOutcomes
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationoutcomes-disabled"></a>

##### `Disabled`

```csharp
const string Disabled
```

Delivery status reconciliation is disabled.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationoutcomes-invitationnotfound"></a>

##### `InvitationNotFound`

```csharp
const string InvitationNotFound
```

The requested invitation was not found.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationoutcomes-providermessagemismatch"></a>

##### `ProviderMessageMismatch`

```csharp
const string ProviderMessageMismatch
```

The supplied provider message identifier does not match the identifier recorded during dispatch.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationoutcomes-providermessagemissing"></a>

##### `ProviderMessageMissing`

```csharp
const string ProviderMessageMissing
```

The invitation has a recorded provider message identifier, but the reconciliation request did not provide one.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationoutcomes-reconciled"></a>

##### `Reconciled`

```csharp
const string Reconciled
```

The delivery status observation was reconciled into invitation metadata.

<a id="member-f-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationoutcomes-storefailed"></a>

##### `StoreFailed`

```csharp
const string StoreFailed
```

The delivery status observation could not be persisted.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest"></a>

### `TenantInvitationDeliveryStatusReconciliationRequest`

Describes a tenant invitation delivery status reconciliation request.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryStatusReconciliationRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-string-system-string-system-boolean-system-boolean-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryStatusReconciliationRequest`

```csharp
TenantInvitationDeliveryStatusReconciliationRequest(string tenantId, string invitationId, string status, string providerMessageId, string senderId, string channel, string reason, DateTimeOffset? observedAtUtc, string source, string actor, string correlationId, bool recordStatus, bool requireProviderMessageMatch, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery status reconciliation request.

Parameters:
- `tenantId`: The tenant identifier that owns the invitation.
- `invitationId`: The invitation identifier to reconcile.
- `status`: The provider or receiver delivery status.
- `providerMessageId`: The provider message identifier associated with the status observation.
- `senderId`: The delivery sender identifier associated with the status observation.
- `channel`: The delivery channel associated with the status observation.
- `reason`: The provider or receiver status reason.
- `observedAtUtc`: The UTC timestamp when the status was observed. The runtime clock is used when omitted.
- `source`: The source that reported the status observation.
- `actor`: The actor that reported the status observation when known.
- `correlationId`: The optional correlation identifier for the status observation.
- `recordStatus`: A value indicating whether reconciled status metadata should be recorded on the invitation.
- `requireProviderMessageMatch`: A value indicating whether an existing dispatch provider message identifier must match the request.
- `metadata`: Optional delivery status metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; }
```

Gets the actor that reported the status observation when known.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-channel"></a>

##### `Channel`

```csharp
string Channel { get; }
```

Gets the delivery channel associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the status observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier to reconcile.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional delivery status metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset? ObservedAtUtc { get; }
```

Gets the UTC timestamp when the status was observed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the provider message identifier associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the provider or receiver status reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-recordstatus"></a>

##### `RecordStatus`

```csharp
bool RecordStatus { get; }
```

Gets a value indicating whether reconciled status metadata should be recorded on the invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-requireprovidermessagematch"></a>

##### `RequireProviderMessageMatch`

```csharp
bool RequireProviderMessageMatch { get; }
```

Gets a value indicating whether an existing dispatch provider message identifier must match the request.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; }
```

Gets the delivery sender identifier associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source that reported the status observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the provider or receiver delivery status.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that owns the invitation.

<a id="type-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult"></a>

### `TenantInvitationDeliveryStatusReconciliationResult`

Describes the result of tenant invitation delivery status reconciliation.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryStatusReconciliationResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-ctor-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-datetimeoffset-system-string-system-string-system-string-cephalon-multitenancy-governance-services-tenantinvitationdescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantInvitationDeliveryStatusReconciliationResult`

```csharp
TenantInvitationDeliveryStatusReconciliationResult(string tenantId, string invitationId, string status, string outcome, bool reconciled, bool recorded, DateTimeOffset observedAtUtc, string providerMessageId, string senderId, string channel, TenantInvitationDescriptor invitation, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a tenant invitation delivery status reconciliation result.

Parameters:
- `tenantId`: The tenant identifier that was evaluated.
- `invitationId`: The invitation identifier that was evaluated.
- `status`: The provider or receiver delivery status.
- `outcome`: The stable delivery status reconciliation outcome.
- `reconciled`: A value indicating whether the status observation was accepted for the invitation.
- `recorded`: A value indicating whether delivery status metadata was recorded.
- `observedAtUtc`: The UTC timestamp when the status was observed.
- `providerMessageId`: The provider message identifier associated with the status observation.
- `senderId`: The delivery sender identifier associated with the status observation.
- `channel`: The delivery channel associated with the status observation.
- `invitation`: The resulting invitation descriptor when one exists.
- `reason`: The operator-facing delivery status reconciliation reason.
- `metadata`: Optional result metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-channel"></a>

##### `Channel`

```csharp
string Channel { get; }
```

Gets the delivery channel associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-invitation"></a>

##### `Invitation`

```csharp
TenantInvitationDescriptor Invitation { get; }
```

Gets the resulting invitation descriptor when one exists.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the invitation identifier that was evaluated.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional result metadata.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; }
```

Gets the UTC timestamp when the status was observed.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable delivery status reconciliation outcome.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the provider message identifier associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing delivery status reconciliation reason.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-reconciled"></a>

##### `Reconciled`

```csharp
bool Reconciled { get; }
```

Gets a value indicating whether the status observation was accepted for the invitation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-recorded"></a>

##### `Recorded`

```csharp
bool Recorded { get; }
```

Gets a value indicating whether delivery status metadata was recorded.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; }
```

Gets the delivery sender identifier associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the provider or receiver delivery status.

<a id="member-p-cephalon-multitenancy-governance-services-tenantinvitationdeliverystatusreconciliationresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier that was evaluated.

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
