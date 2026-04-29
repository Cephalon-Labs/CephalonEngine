# Cephalon.MultiTenancy.Governance.AspNetCore

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.AspNetCore)
## Namespaces

- `Cephalon.MultiTenancy.Governance.AspNetCore.Configuration`
- `Cephalon.MultiTenancy.Governance.AspNetCore.Hosting`

<a id="namespace-cephalon-multitenancy-governance-aspnetcore-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.AspNetCore.Configuration

<a id="type-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions"></a>

### `MultiTenancyGovernanceAspNetCoreOptions`

Configures ASP.NET Core-specific multi-tenancy governance endpoints.

#### Declaration
```csharp
public sealed class MultiTenancyGovernanceAspNetCoreOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-ctor"></a>

##### `MultiTenancyGovernanceAspNetCoreOptions`

```csharp
MultiTenancyGovernanceAspNetCoreOptions()
```

Initializes a new instance of the `MultiTenancyGovernanceAspNetCoreOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-cachecontrolheader"></a>

##### `CacheControlHeader`

```csharp
string CacheControlHeader { get; set; }
```

Gets or sets the cache-control header written for served proof files.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-enablehttpproofpublicationendpoint"></a>

##### `EnableHttpProofPublicationEndpoint`

```csharp
bool EnableHttpProofPublicationEndpoint { get; set; }
```

Gets or sets a value indicating whether the HTTP proof publication endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-enabletenantadministrationcommandendpoint"></a>

##### `EnableTenantAdministrationCommandEndpoint`

```csharp
bool EnableTenantAdministrationCommandEndpoint { get; set; }
```

Gets or sets a value indicating whether the tenant-administration command endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-enabletenantinvitationdeliverydispatchendpoint"></a>

##### `EnableTenantInvitationDeliveryDispatchEndpoint`

```csharp
bool EnableTenantInvitationDeliveryDispatchEndpoint { get; set; }
```

Gets or sets a value indicating whether the tenant-invitation delivery dispatch endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-enabletenantinvitationdeliverystatuscallbackendpoint"></a>

##### `EnableTenantInvitationDeliveryStatusCallbackEndpoint`

```csharp
bool EnableTenantInvitationDeliveryStatusCallbackEndpoint { get; set; }
```

Gets or sets a value indicating whether the tenant-invitation delivery status callback endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-enabletenantinvitationdeliverystatuscallbackreplayprotection"></a>

##### `EnableTenantInvitationDeliveryStatusCallbackReplayProtection`

```csharp
bool EnableTenantInvitationDeliveryStatusCallbackReplayProtection { get; set; }
```

Gets or sets a value indicating whether signed delivery-status callbacks should be protected against replay inside the current process.

Remarks: Replay protection is active only when `TenantInvitationDeliveryStatusCallbackSigningSecret` is configured and the request signature verifies successfully. The built-in guard stores bounded signature fingerprints in memory and does not claim durable inbox storage, cross-node deduplication, or distributed exactly-once delivery.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-enabletenantinvitationdeliverystatusobservationendpoint"></a>

##### `EnableTenantInvitationDeliveryStatusObservationEndpoint`

```csharp
bool EnableTenantInvitationDeliveryStatusObservationEndpoint { get; set; }
```

Gets or sets a value indicating whether the delivery status observation read endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-excludefromdescription"></a>

##### `ExcludeFromDescription`

```csharp
bool ExcludeFromDescription { get; set; }
```

Gets or sets a value indicating whether the proof endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-excludetenantadministrationendpointfromdescription"></a>

##### `ExcludeTenantAdministrationEndpointFromDescription`

```csharp
bool ExcludeTenantAdministrationEndpointFromDescription { get; set; }
```

Gets or sets a value indicating whether the tenant-administration command endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-excludetenantinvitationdeliverydispatchendpointfromdescription"></a>

##### `ExcludeTenantInvitationDeliveryDispatchEndpointFromDescription`

```csharp
bool ExcludeTenantInvitationDeliveryDispatchEndpointFromDescription { get; set; }
```

Gets or sets a value indicating whether the tenant-invitation delivery dispatch endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-excludetenantinvitationdeliverystatuscallbackendpointfromdescription"></a>

##### `ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription`

```csharp
bool ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription { get; set; }
```

Gets or sets a value indicating whether the delivery status callback endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-excludetenantinvitationdeliverystatusobservationendpointfromdescription"></a>

##### `ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription`

```csharp
bool ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription { get; set; }
```

Gets or sets a value indicating whether the delivery status observation read endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-requiretenantadministrationauthorization"></a>

##### `RequireTenantAdministrationAuthorization`

```csharp
bool RequireTenantAdministrationAuthorization { get; set; }
```

Gets or sets a value indicating whether the tenant-administration command endpoint should require authorization.

Remarks: The endpoint also performs a fail-closed in-handler authorization check so accidental hosts without ASP.NET Core authorization middleware do not execute tenant-administration commands anonymously.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-requiretenantinvitationdeliverydispatchauthorization"></a>

##### `RequireTenantInvitationDeliveryDispatchAuthorization`

```csharp
bool RequireTenantInvitationDeliveryDispatchAuthorization { get; set; }
```

Gets or sets a value indicating whether the tenant-invitation delivery dispatch endpoint should require authorization.

Remarks: The endpoint also performs a fail-closed in-handler authorization check so accidental hosts without ASP.NET Core authorization middleware do not dispatch tenant invitations anonymously.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-requiretenantinvitationdeliverystatuscallbackauthorization"></a>

##### `RequireTenantInvitationDeliveryStatusCallbackAuthorization`

```csharp
bool RequireTenantInvitationDeliveryStatusCallbackAuthorization { get; set; }
```

Gets or sets a value indicating whether the delivery status callback endpoint should require authorization.

Remarks: The endpoint also performs a fail-closed in-handler authorization check so accidental hosts without ASP.NET Core authorization middleware do not accept provider or adapter status callbacks anonymously.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-requiretenantinvitationdeliverystatuscallbackprovidermessagematch"></a>

##### `RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch`

```csharp
bool RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch { get; set; }
```

Gets or sets a value indicating whether callback requests must keep provider message matching enabled.

Remarks: Provider message matching is enforced by default so a generic callback cannot opt out of the host-agnostic reconciliation safety check unless the host deliberately relaxes this setting.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-requiretenantinvitationdeliverystatusobservationauthorization"></a>

##### `RequireTenantInvitationDeliveryStatusObservationAuthorization`

```csharp
bool RequireTenantInvitationDeliveryStatusObservationAuthorization { get; set; }
```

Gets or sets a value indicating whether the delivery status observation read endpoint should require authorization.

Remarks: The endpoint also performs a fail-closed in-handler authorization check so accidental hosts without ASP.NET Core authorization middleware do not expose invitation delivery audit data anonymously.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-routepattern"></a>

##### `RoutePattern`

```csharp
string RoutePattern { get; set; }
```

Gets or sets the endpoint route pattern used for published HTTP proof files.

Remarks: The default catch-all route is intentionally constrained under `/.well-known/cephalon/` so it does not compete with application-owned routes.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantadministrationauthorizationpolicy"></a>

##### `TenantAdministrationAuthorizationPolicy`

```csharp
string TenantAdministrationAuthorizationPolicy { get; set; }
```

Gets or sets the optional ASP.NET Core authorization policy required by the tenant-administration command endpoint.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantadministrationcommandroutepattern"></a>

##### `TenantAdministrationCommandRoutePattern`

```csharp
string TenantAdministrationCommandRoutePattern { get; set; }
```

Gets or sets the endpoint route pattern used for tenant-administration workflow commands.

Remarks: The default route stays under `/engine` because the endpoint is an operator/admin surface, not an application-owned public onboarding API.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverydispatchauthorizationpolicy"></a>

##### `TenantInvitationDeliveryDispatchAuthorizationPolicy`

```csharp
string TenantInvitationDeliveryDispatchAuthorizationPolicy { get; set; }
```

Gets or sets the optional ASP.NET Core authorization policy required by the tenant-invitation delivery dispatch endpoint.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverydispatchroutepattern"></a>

##### `TenantInvitationDeliveryDispatchRoutePattern`

```csharp
string TenantInvitationDeliveryDispatchRoutePattern { get; set; }
```

Gets or sets the endpoint route pattern used for tenant-invitation delivery dispatch requests.

Remarks: The default route stays under `/engine` because the endpoint is an operator/action surface over the host-agnostic dispatcher, not a product-owned public onboarding API.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbackauthorizationpolicy"></a>

##### `TenantInvitationDeliveryStatusCallbackAuthorizationPolicy`

```csharp
string TenantInvitationDeliveryStatusCallbackAuthorizationPolicy { get; set; }
```

Gets or sets the optional ASP.NET Core authorization policy required by the delivery status callback endpoint.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbackreplaycachelimit"></a>

##### `TenantInvitationDeliveryStatusCallbackReplayCacheLimit`

```csharp
int TenantInvitationDeliveryStatusCallbackReplayCacheLimit { get; set; }
```

Gets or sets the maximum number of signed callback replay fingerprints retained in the current process.

Remarks: When the bounded cache is full, the oldest fingerprint is evicted before recording a new accepted signed callback.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbackreplayretentionseconds"></a>

##### `TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds`

```csharp
int TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds { get; set; }
```

Gets or sets the process-local retention window, in seconds, for signed callback replay fingerprints.

Remarks: The endpoint clamps the effective retention to at least one second. The default matches the signature timestamp tolerance.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbackroutepattern"></a>

##### `TenantInvitationDeliveryStatusCallbackRoutePattern`

```csharp
string TenantInvitationDeliveryStatusCallbackRoutePattern { get; set; }
```

Gets or sets the endpoint route pattern used for normalized tenant-invitation delivery status callbacks.

Remarks: The default route stays under `/engine` because the endpoint is an operator/provider-adapter ingress surface, not an application-owned public onboarding API.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbacksignatureheadername"></a>

##### `TenantInvitationDeliveryStatusCallbackSignatureHeaderName`

```csharp
string TenantInvitationDeliveryStatusCallbackSignatureHeaderName { get; set; }
```

Gets or sets the request header that carries the callback signature.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbacksignaturekeyidheadername"></a>

##### `TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName`

```csharp
string TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName { get; set; }
```

Gets or sets the request header that carries the optional callback signing key identifier.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbacksignaturetimestampheadername"></a>

##### `TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName`

```csharp
string TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName { get; set; }
```

Gets or sets the request header that carries the Unix timestamp included in the callback signature.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbacksignaturetoleranceseconds"></a>

##### `TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds`

```csharp
int TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds { get; set; }
```

Gets or sets the allowed clock skew, in seconds, for signed delivery-status callback timestamps.

Remarks: The endpoint clamps the effective tolerance to at least one second. The default is five minutes.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbacksigningkeyid"></a>

##### `TenantInvitationDeliveryStatusCallbackSigningKeyId`

```csharp
string TenantInvitationDeliveryStatusCallbackSigningKeyId { get; set; }
```

Gets or sets the optional signing key identifier expected on signed delivery-status callback requests.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatuscallbacksigningsecret"></a>

##### `TenantInvitationDeliveryStatusCallbackSigningSecret`

```csharp
string TenantInvitationDeliveryStatusCallbackSigningSecret { get; set; }
```

Gets or sets the shared secret used to verify normalized delivery-status callback request bodies with HMAC-SHA256.

Remarks: When a value is configured, every callback request must include a valid Cephalon callback signature before the request is reconciled. Leave this empty when the host uses ASP.NET Core authorization or a provider-specific companion to authenticate callback ingress instead.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatusobservationauthorizationpolicy"></a>

##### `TenantInvitationDeliveryStatusObservationAuthorizationPolicy`

```csharp
string TenantInvitationDeliveryStatusObservationAuthorizationPolicy { get; set; }
```

Gets or sets the optional ASP.NET Core authorization policy required by the delivery status observation read endpoint.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatusobservationdefaultlimit"></a>

##### `TenantInvitationDeliveryStatusObservationDefaultLimit`

```csharp
int TenantInvitationDeliveryStatusObservationDefaultLimit { get; set; }
```

Gets or sets the default number of observations returned when a read request does not specify a limit.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatusobservationmaxlimit"></a>

##### `TenantInvitationDeliveryStatusObservationMaxLimit`

```csharp
int TenantInvitationDeliveryStatusObservationMaxLimit { get; set; }
```

Gets or sets the maximum number of observations returned by one read request.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-tenantinvitationdeliverystatusobservationroutepattern"></a>

##### `TenantInvitationDeliveryStatusObservationRoutePattern`

```csharp
string TenantInvitationDeliveryStatusObservationRoutePattern { get; set; }
```

Gets or sets the endpoint route pattern used for reading normalized tenant-invitation delivery status observations.

Remarks: The default route stays under `/engine` because the endpoint is an operator/audit surface over Cephalon's normalized observation store, not a provider-specific callback inbox.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MultiTenancyGovernanceAspNetCoreOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads ASP.NET Core governance adapter options from configuration.

Returns: The parsed ASP.NET Core governance adapter options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The engine root section path to read from.

<a id="namespace-cephalon-multitenancy-governance-aspnetcore-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-multitenancygovernanceaspnetcoreservicecollectionextensions"></a>

### `MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions`

Registers the ASP.NET Core governance adapter services used by Cephalon multi-tenancy governance.

#### Declaration
```csharp
public static class MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-multitenancygovernanceaspnetcoreservicecollectionextensions-addcephalonmultitenancygovernanceaspnetcore-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions"></a>

##### `AddCephalonMultiTenancyGovernanceAspNetCore`

```csharp
IServiceCollection AddCephalonMultiTenancyGovernanceAspNetCore(this IServiceCollection services, IConfiguration configuration, Action<MultiTenancyGovernanceAspNetCoreOptions> configure)
```

Adds the Cephalon ASP.NET Core multi-tenancy governance adapter to the service collection.

Returns: The same service collection for fluent registration.

Parameters:
- `services`: The service collection to extend.
- `configuration`: The optional configuration root used to populate `MultiTenancyGovernanceAspNetCoreOptions` from `Engine:MultiTenancy:Governance:AspNetCore`.
- `configure`: An optional callback that can extend or override the configuration-driven ASP.NET Core governance adapter options.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-multitenancygovernanceaspnetcorewebapplicationbuilderextensions"></a>

### `MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions`

Registers the ASP.NET Core multi-tenancy governance adapter on a `WebApplicationBuilder`.

#### Declaration
```csharp
public static class MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-multitenancygovernanceaspnetcorewebapplicationbuilderextensions-addcephalonmultitenancygovernanceaspnetcore-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions"></a>

##### `AddCephalonMultiTenancyGovernanceAspNetCore`

```csharp
WebApplicationBuilder AddCephalonMultiTenancyGovernanceAspNetCore(this WebApplicationBuilder builder, Action<MultiTenancyGovernanceAspNetCoreOptions> configure)
```

Adds the Cephalon ASP.NET Core multi-tenancy governance adapter to the target application builder.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: An optional callback that can extend or override the configuration-driven ASP.NET Core governance adapter options.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-tenantadministrationendpointroutebuilderextensions"></a>

### `TenantAdministrationEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for Cephalon tenant-administration workflow commands.

#### Declaration
```csharp
public static class TenantAdministrationEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-tenantadministrationendpointroutebuilderextensions-mapcephalontenantadministrationcommands-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonTenantAdministrationCommands`

```csharp
IEndpointRouteBuilder MapCephalonTenantAdministrationCommands(this IEndpointRouteBuilder endpoints)
```

Maps the optional tenant-administration command endpoint.

Remarks: The endpoint is opt-in, executes the host-agnostic `ITenantAdministrationWorkflow`, and performs a fail-closed authorization check by default. It does not provide public onboarding, tenant-admin UI, provider-specific invitation senders, external invitation delivery, or identity-provider synchronization.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-tenantdomainownershiphttpproofendpointroutebuilderextensions"></a>

### `TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for tenant-domain ownership HTTP proof files published by Cephalon governance.

#### Declaration
```csharp
public static class TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-tenantdomainownershiphttpproofendpointroutebuilderextensions-mapcephalontenantdomainownershiphttpproofs-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonTenantDomainOwnershipHttpProofs`

```csharp
IEndpointRouteBuilder MapCephalonTenantDomainOwnershipHttpProofs(this IEndpointRouteBuilder endpoints)
```

Maps the tenant-domain ownership HTTP proof publication endpoint.

Remarks: This endpoint is opt-in and reads proof-file state from `ITenantDomainOwnershipHttpProofPublicationCatalog`. The core governance package remains host-agnostic and only records the proof state that this adapter serves.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverydispatchendpointroutebuilderextensions"></a>

### `TenantInvitationDeliveryDispatchEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for tenant-invitation delivery dispatch requests.

#### Declaration
```csharp
public static class TenantInvitationDeliveryDispatchEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverydispatchendpointroutebuilderextensions-mapcephalontenantinvitationdeliverydispatches-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonTenantInvitationDeliveryDispatches`

```csharp
IEndpointRouteBuilder MapCephalonTenantInvitationDeliveryDispatches(this IEndpointRouteBuilder endpoints)
```

Maps the optional tenant-invitation delivery dispatch endpoint.

Remarks: The endpoint is opt-in, executes the host-agnostic `ITenantInvitationDeliveryDispatcher`, and performs a fail-closed authorization check by default. It does not implement provider-specific senders, durable retry queues, public onboarding, tenant-admin UI, provider polling, or identity-provider synchronization.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackendpointroutebuilderextensions"></a>

### `TenantInvitationDeliveryStatusCallbackEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for normalized tenant-invitation delivery status callbacks.

#### Declaration
```csharp
public static class TenantInvitationDeliveryStatusCallbackEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackendpointroutebuilderextensions-mapcephalontenantinvitationdeliverystatuscallbacks-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonTenantInvitationDeliveryStatusCallbacks`

```csharp
IEndpointRouteBuilder MapCephalonTenantInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
```

Maps the optional tenant-invitation delivery status callback endpoint.

Remarks: The endpoint is opt-in, executes the host-agnostic `ITenantInvitationDeliveryStatusReconciler`, and performs a fail-closed authorization check by default. It accepts normalized status observations only; provider webhook payload translation, provider signature verification, provider polling, and provider-specific status vocabularies remain application-managed or future provider-pack responsibilities.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest"></a>

### `TenantInvitationDeliveryStatusCallbackRequest`

Describes a normalized ASP.NET Core tenant-invitation delivery status callback request.

Remarks: Provider-specific webhook payloads should be translated into this provider-neutral shape by the host or a future provider companion before the request is reconciled by Cephalon governance.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryStatusCallbackRequest
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-ctor"></a>

##### `TenantInvitationDeliveryStatusCallbackRequest`

```csharp
TenantInvitationDeliveryStatusCallbackRequest()
```

Initializes a new instance of the `TenantInvitationDeliveryStatusCallbackRequest` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-actor"></a>

##### `Actor`

```csharp
string Actor { get; set; }
```

Gets or sets the actor that reported the status observation when known.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-channel"></a>

##### `Channel`

```csharp
string Channel { get; set; }
```

Gets or sets the delivery channel associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or sets the optional correlation identifier for the status observation.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; set; }
```

Gets or sets the invitation identifier to reconcile.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; set; }
```

Gets or sets optional delivery status metadata.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset? ObservedAtUtc { get; set; }
```

Gets or sets the UTC timestamp when the status was observed. The runtime clock is used when omitted.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; set; }
```

Gets or sets the provider message identifier associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-reason"></a>

##### `Reason`

```csharp
string Reason { get; set; }
```

Gets or sets the provider or receiver status reason.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-recordstatus"></a>

##### `RecordStatus`

```csharp
bool RecordStatus { get; set; }
```

Gets or sets a value indicating whether reconciled status metadata should be recorded on the invitation.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-requireprovidermessagematch"></a>

##### `RequireProviderMessageMatch`

```csharp
bool RequireProviderMessageMatch { get; set; }
```

Gets or sets a value indicating whether an existing dispatch provider message identifier must match the request.

Remarks: Hosts can also enforce matching through `RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch`.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; set; }
```

Gets or sets the delivery sender identifier associated with the status observation.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-source"></a>

##### `Source`

```csharp
string Source { get; set; }
```

Gets or sets the source that reported the status observation.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-status"></a>

##### `Status`

```csharp
string Status { get; set; }
```

Gets or sets the provider or receiver delivery status.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatuscallbackrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; set; }
```

Gets or sets the tenant identifier that owns the invitation.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationendpointroutebuilderextensions"></a>

### `TenantInvitationDeliveryStatusObservationEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for reading normalized tenant-invitation delivery status observations.

#### Declaration
```csharp
public static class TenantInvitationDeliveryStatusObservationEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationendpointroutebuilderextensions-mapcephalontenantinvitationdeliverystatusobservations-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonTenantInvitationDeliveryStatusObservations`

```csharp
IEndpointRouteBuilder MapCephalonTenantInvitationDeliveryStatusObservations(this IEndpointRouteBuilder endpoints)
```

Maps the optional tenant-invitation delivery status observation read endpoint.

Remarks: The endpoint is opt-in, reads the host-agnostic `ITenantInvitationDeliveryStatusObservationStore`, and performs a fail-closed authorization check by default. It exposes bounded normalized observation history only; provider-specific callback inboxes, provider polling, and distributed replay semantics remain application-managed or future provider-pack responsibilities.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult"></a>

### `TenantInvitationDeliveryStatusObservationQueryResult`

Describes a bounded read of normalized tenant-invitation delivery status observations.

Remarks: The result is an operator/audit view over `ITenantInvitationDeliveryStatusObservationStore`. It does not represent a provider-specific callback inbox, provider polling state, or distributed replay ledger.

#### Declaration
```csharp
public sealed class TenantInvitationDeliveryStatusObservationQueryResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-ctor"></a>

##### `TenantInvitationDeliveryStatusObservationQueryResult`

```csharp
TenantInvitationDeliveryStatusObservationQueryResult()
```

Initializes a new instance of the `TenantInvitationDeliveryStatusObservationQueryResult` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-filters"></a>

##### `Filters`

```csharp
IReadOnlyDictionary<string, string> Filters { get; set; }
```

Gets the normalized filters applied to this read.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-isdurable"></a>

##### `IsDurable`

```csharp
bool IsDurable { get; set; }
```

Gets a value indicating whether the underlying observation store survives process restarts.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-limit"></a>

##### `Limit`

```csharp
int Limit { get; set; }
```

Gets the effective response limit used for this read.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-matchedcount"></a>

##### `MatchedCount`

```csharp
int MatchedCount { get; set; }
```

Gets the number of observations that matched the supplied endpoint filters before the response limit was applied.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-observations"></a>

##### `Observations`

```csharp
IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> Observations { get; set; }
```

Gets the normalized delivery status observations returned by this read.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-ownership"></a>

##### `Ownership`

```csharp
string Ownership { get; set; }
```

Gets the ownership mode reported by the underlying observation store.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-returnedcount"></a>

##### `ReturnedCount`

```csharp
int ReturnedCount { get; set; }
```

Gets the number of observations included in this response after filtering and limiting.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-storekind"></a>

##### `StoreKind`

```csharp
string StoreKind { get; set; }
```

Gets the observation store kind, such as `in-memory` or `file`.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-hosting-tenantinvitationdeliverystatusobservationqueryresult-totalcount"></a>

##### `TotalCount`

```csharp
int TotalCount { get; set; }
```

Gets the number of observations in the store before endpoint filters are applied.
