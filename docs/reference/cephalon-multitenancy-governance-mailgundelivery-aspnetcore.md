# Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore)
## Namespaces

- `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Configuration`
- `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting`

<a id="namespace-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Configuration

<a id="type-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions"></a>

### `MailgunInvitationDeliveryAspNetCoreOptions`

Configures ASP.NET Core Mailgun webhook callback translation for tenant-invitation delivery status updates.

Remarks: This adapter translates Mailgun webhook payloads and can require Mailgun HMAC-SHA256 webhook signature verification before reconciliation. It can also reject duplicate signed webhook tokens inside a bounded process-local replay window. Durable callback inboxes and provider polling are intentionally separate slices.

#### Declaration
```csharp
public sealed class MailgunInvitationDeliveryAspNetCoreOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-ctor"></a>

##### `MailgunInvitationDeliveryAspNetCoreOptions`

```csharp
MailgunInvitationDeliveryAspNetCoreOptions()
```

Initializes a new instance of the `MailgunInvitationDeliveryAspNetCoreOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-acceptparentsignature"></a>

##### `AcceptParentSignature`

```csharp
bool AcceptParentSignature { get; set; }
```

Gets or sets a value indicating whether Mailgun `parent-signature` should be accepted for subaccount webhook events.

Remarks: Mailgun includes `parent-signature` for subaccount events so receivers can validate with the parent account signing key. Disable this only when a host deliberately requires the child account signature field.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-actor"></a>

##### `Actor`

```csharp
string Actor { get; set; }
```

Gets or sets the actor value recorded on translated Mailgun delivery status observations.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-enablesignedwebhookreplayprotection"></a>

##### `EnableSignedWebhookReplayProtection`

```csharp
bool EnableSignedWebhookReplayProtection { get; set; }
```

Gets or sets a value indicating whether verified Mailgun signed webhook tokens should be protected against replay inside the current process.

Remarks: Replay protection is active only when `RequireSignedWebhook` is enabled and the request signature verifies successfully. The built-in guard stores bounded token fingerprints in memory and does not claim distributed replay protection or durable provider callback inbox ownership.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-enablestatuscallbackendpoint"></a>

##### `EnableStatusCallbackEndpoint`

```csharp
bool EnableStatusCallbackEndpoint { get; set; }
```

Gets or sets a value indicating whether the Mailgun webhook callback endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-excludestatuscallbackendpointfromdescription"></a>

##### `ExcludeStatusCallbackEndpointFromDescription`

```csharp
bool ExcludeStatusCallbackEndpointFromDescription { get; set; }
```

Gets or sets a value indicating whether the Mailgun callback endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-mapengagementeventsasdelivered"></a>

##### `MapEngagementEventsAsDelivered`

```csharp
bool MapEngagementEventsAsDelivered { get; set; }
```

Gets or sets a value indicating whether Mailgun engagement events such as opened and clicked should be recorded as delivered.

Remarks: The default is `false` so the endpoint records deliverability events only. Enable this when a host deliberately wants engagement events to update invitation delivery status.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-maxeventsperrequest"></a>

##### `MaxEventsPerRequest`

```csharp
int MaxEventsPerRequest { get; set; }
```

Gets or sets the maximum number of Mailgun events accepted in one callback request.

Remarks: Mailgun posts one JSON event object by default. The endpoint also accepts a JSON array for controlled replay and test harness scenarios while keeping the same bounded parsing posture.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-maxrequestbodybytes"></a>

##### `MaxRequestBodyBytes`

```csharp
int MaxRequestBodyBytes { get; set; }
```

Gets or sets the maximum request body size accepted by the Mailgun callback endpoint, in bytes.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-normalizeprovidermessageidwithanglebrackets"></a>

##### `NormalizeProviderMessageIdWithAngleBrackets`

```csharp
bool NormalizeProviderMessageIdWithAngleBrackets { get; set; }
```

Gets or sets a value indicating whether Mailgun `message.headers.message-id` values should be wrapped in angle brackets.

Remarks: Mailgun Messages API responses commonly return an angle-bracketed message id, while webhook headers may surface the same id without brackets. This normalization keeps Cephalon's provider-message guard useful.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-recordstatus"></a>

##### `RecordStatus`

```csharp
bool RecordStatus { get; set; }
```

Gets or sets a value indicating whether translated delivery status should be recorded on the invitation.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-requireprovidermessagematch"></a>

##### `RequireProviderMessageMatch`

```csharp
bool RequireProviderMessageMatch { get; set; }
```

Gets or sets a value indicating whether translated Mailgun events must match an existing provider message id.

Remarks: Mailgun webhook payloads expose the message identifier through `message.headers.message-id`. The default keeps provider-message guarding enabled and wraps header values in angle brackets to match the Messages API response id shape used by the sender package.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-requiresignedwebhook"></a>

##### `RequireSignedWebhook`

```csharp
bool RequireSignedWebhook { get; set; }
```

Gets or sets a value indicating whether Mailgun webhook requests must carry a valid Mailgun signature before payload translation and reconciliation can run.

Remarks: When enabled, the endpoint verifies the Mailgun HMAC-SHA256 hex digest over `timestamp + token` using the configured webhook signing key. Signature verification is separate from replay-token caching so hosts can adopt authentication first without claiming durable inbox or distributed replay ownership.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-requirestatuscallbackauthorization"></a>

##### `RequireStatusCallbackAuthorization`

```csharp
bool RequireStatusCallbackAuthorization { get; set; }
```

Gets or sets a value indicating whether the Mailgun callback endpoint should require authorization.

Remarks: The endpoint performs an in-handler authorization check by default. Hosts can satisfy it with ASP.NET Core authentication, a gateway, or deliberately disable it for trusted test hosts.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-signedwebhookreplaycachelimit"></a>

##### `SignedWebhookReplayCacheLimit`

```csharp
int SignedWebhookReplayCacheLimit { get; set; }
```

Gets or sets the maximum number of verified Mailgun webhook token fingerprints retained in the current process.

Remarks: When the bounded cache is full, the oldest token fingerprint is evicted before recording a new accepted signed callback.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-signedwebhookreplayretentionseconds"></a>

##### `SignedWebhookReplayRetentionSeconds`

```csharp
int SignedWebhookReplayRetentionSeconds { get; set; }
```

Gets or sets the process-local retention window, in seconds, for verified Mailgun webhook token fingerprints.

Remarks: The endpoint clamps the effective retention to at least one second. The default matches the signature timestamp tolerance.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-signedwebhooksignaturetoleranceseconds"></a>

##### `SignedWebhookSignatureToleranceSeconds`

```csharp
int SignedWebhookSignatureToleranceSeconds { get; set; }
```

Gets or sets the allowed clock skew, in seconds, for signed Mailgun webhook timestamps.

Remarks: The endpoint clamps the effective tolerance to at least one second. The default is five minutes.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-source"></a>

##### `Source`

```csharp
string Source { get; set; }
```

Gets or sets the source value recorded on translated Mailgun delivery status observations.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-statuscallbackauthorizationpolicy"></a>

##### `StatusCallbackAuthorizationPolicy`

```csharp
string StatusCallbackAuthorizationPolicy { get; set; }
```

Gets or sets the optional ASP.NET Core authorization policy required by the Mailgun callback endpoint.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-statuscallbackroutepattern"></a>

##### `StatusCallbackRoutePattern`

```csharp
string StatusCallbackRoutePattern { get; set; }
```

Gets or sets the ASP.NET Core route pattern used for Mailgun webhook callbacks.

Remarks: The default route stays under `/engine` because this endpoint is a provider-adapter ingress surface, not an application-owned onboarding API.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-webhooksigningkey"></a>

##### `WebhookSigningKey`

```csharp
string WebhookSigningKey { get; set; }
```

Gets or sets the Mailgun webhook signing key used for HMAC-SHA256 verification.

Remarks: This is the Mailgun Send webhook signing key, not the Mailgun API key or an Alerts webhook signing key.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MailgunInvitationDeliveryAspNetCoreOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads Mailgun ASP.NET Core callback options from configuration.

Returns: The parsed Mailgun ASP.NET Core callback options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The engine root section path to read from.

<a id="namespace-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting

<a id="type-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliveryaspnetcoreservicecollectionextensions"></a>

### `MailgunInvitationDeliveryAspNetCoreServiceCollectionExtensions`

Registers ASP.NET Core Mailgun webhook translation services for tenant-invitation delivery status callbacks.

#### Declaration
```csharp
public static class MailgunInvitationDeliveryAspNetCoreServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliveryaspnetcoreservicecollectionextensions-addcephalonmailguninvitationdeliveryaspnetcore-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-configuration-mailguninvitationdeliveryaspnetcoreoptions"></a>

##### `AddCephalonMailgunInvitationDeliveryAspNetCore`

```csharp
IServiceCollection AddCephalonMailgunInvitationDeliveryAspNetCore(this IServiceCollection services, IConfiguration configuration, Action<MailgunInvitationDeliveryAspNetCoreOptions> configure)
```

Adds Mailgun webhook callback translation services using configuration as the primary setup source.

Returns: The same service collection for fluent registration.

Parameters:
- `services`: The service collection to extend.
- `configuration`: The optional configuration root.
- `configure`: An optional callback that can extend or override configuration-driven options.

<a id="type-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult"></a>

### `MailgunInvitationDeliveryStatusCallbackEventResult`

Describes how one Mailgun webhook event was translated and reconciled.

#### Declaration
```csharp
public sealed class MailgunInvitationDeliveryStatusCallbackEventResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-ctor-system-int32-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-string"></a>

##### `MailgunInvitationDeliveryStatusCallbackEventResult`

```csharp
MailgunInvitationDeliveryStatusCallbackEventResult(int index, string mailgunEventId, string mailgunMessageId, string mailgunEventType, string tenantId, string invitationId, string status, string outcome, bool translated, bool reconciled, string reason)
```

Creates a Mailgun callback event result.

Parameters:
- `index`: The zero-based event index inside the request payload.
- `mailgunEventId`: The Mailgun event identifier when supplied.
- `mailgunMessageId`: The Mailgun message identifier when supplied.
- `mailgunEventType`: The Mailgun event type when supplied.
- `tenantId`: The Cephalon tenant identifier when supplied.
- `invitationId`: The Cephalon invitation identifier when supplied.
- `status`: The normalized Cephalon delivery status when translated.
- `outcome`: The translation or reconciliation outcome.
- `translated`: A value indicating whether the event was translated into a reconciliation request.
- `reconciled`: A value indicating whether the event reconciled a tenant invitation.
- `reason`: The operator-facing reason for the event outcome.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-index"></a>

##### `Index`

```csharp
int Index { get; }
```

Gets the zero-based event index inside the request payload.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the Cephalon invitation identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-mailguneventid"></a>

##### `MailgunEventId`

```csharp
string MailgunEventId { get; }
```

Gets the Mailgun event identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-mailguneventtype"></a>

##### `MailgunEventType`

```csharp
string MailgunEventType { get; }
```

Gets the Mailgun event type when supplied.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-mailgunmessageid"></a>

##### `MailgunMessageId`

```csharp
string MailgunMessageId { get; }
```

Gets the Mailgun message identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the translation or reconciliation outcome.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing reason for the event outcome.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-reconciled"></a>

##### `Reconciled`

```csharp
bool Reconciled { get; }
```

Gets a value indicating whether the event reconciled a tenant invitation.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the normalized Cephalon delivery status when translated.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the Cephalon tenant identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-translated"></a>

##### `Translated`

```csharp
bool Translated { get; }
```

Gets a value indicating whether the event was translated into a reconciliation request.

<a id="type-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult"></a>

### `MailgunInvitationDeliveryStatusCallbackResult`

Describes a Mailgun webhook callback translation response.

#### Declaration
```csharp
public sealed class MailgunInvitationDeliveryStatusCallbackResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-ctor-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-boolean-system-boolean-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackeventresult-system-boolean-system-string"></a>

##### `MailgunInvitationDeliveryStatusCallbackResult`

```csharp
MailgunInvitationDeliveryStatusCallbackResult(string routePattern, int totalEvents, int translatedEvents, int reconciledEvents, int skippedEvents, int deniedEvents, bool signedWebhookVerificationRequired, bool signedWebhookVerified, string signedWebhookVerificationOutcome, string signedWebhookSignatureField, IReadOnlyList<MailgunInvitationDeliveryStatusCallbackEventResult> events, bool signedWebhookReplayProtectionEnabled, string signedWebhookReplayProtectionOutcome)
```

Creates a Mailgun callback translation response.

Parameters:
- `routePattern`: The endpoint route pattern that accepted the callback.
- `totalEvents`: The number of events supplied in the callback payload.
- `translatedEvents`: The number of events translated into Cephalon reconciliation requests.
- `reconciledEvents`: The number of events reconciled by Cephalon governance.
- `skippedEvents`: The number of events skipped before reconciliation.
- `deniedEvents`: The number of translated events denied by the reconciler.
- `signedWebhookVerificationRequired`: A value indicating whether Mailgun webhook signature verification was required.
- `signedWebhookVerified`: A value indicating whether the Mailgun webhook signature verified.
- `signedWebhookVerificationOutcome`: The Mailgun webhook signature verification outcome.
- `signedWebhookSignatureField`: The Mailgun signature field that verified the callback, when configured.
- `events`: Per-event translation and reconciliation results.
- `signedWebhookReplayProtectionEnabled`: A value indicating whether process-local replay protection was enabled for this verified signed callback.
- `signedWebhookReplayProtectionOutcome`: The replay-protection outcome for this callback.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-deniedevents"></a>

##### `DeniedEvents`

```csharp
int DeniedEvents { get; }
```

Gets the number of translated events denied by the reconciler.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-events"></a>

##### `Events`

```csharp
IReadOnlyList<MailgunInvitationDeliveryStatusCallbackEventResult> Events { get; }
```

Gets per-event translation and reconciliation results.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-reconciledevents"></a>

##### `ReconciledEvents`

```csharp
int ReconciledEvents { get; }
```

Gets the number of events reconciled by Cephalon governance.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-routepattern"></a>

##### `RoutePattern`

```csharp
string RoutePattern { get; }
```

Gets the endpoint route pattern that accepted the callback.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-signedwebhookreplayprotectionenabled"></a>

##### `SignedWebhookReplayProtectionEnabled`

```csharp
bool SignedWebhookReplayProtectionEnabled { get; }
```

Gets a value indicating whether process-local replay protection was enabled for this verified signed callback.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-signedwebhookreplayprotectionoutcome"></a>

##### `SignedWebhookReplayProtectionOutcome`

```csharp
string SignedWebhookReplayProtectionOutcome { get; }
```

Gets the replay-protection outcome for this callback.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-signedwebhooksignaturefield"></a>

##### `SignedWebhookSignatureField`

```csharp
string SignedWebhookSignatureField { get; }
```

Gets the Mailgun signature field that verified the callback, when configured.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-signedwebhookverificationoutcome"></a>

##### `SignedWebhookVerificationOutcome`

```csharp
string SignedWebhookVerificationOutcome { get; }
```

Gets the Mailgun webhook signature verification outcome.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-signedwebhookverificationrequired"></a>

##### `SignedWebhookVerificationRequired`

```csharp
bool SignedWebhookVerificationRequired { get; }
```

Gets a value indicating whether Mailgun webhook signature verification was required.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-signedwebhookverified"></a>

##### `SignedWebhookVerified`

```csharp
bool SignedWebhookVerified { get; }
```

Gets a value indicating whether the Mailgun webhook signature verified.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-skippedevents"></a>

##### `SkippedEvents`

```csharp
int SkippedEvents { get; }
```

Gets the number of events skipped before reconciliation.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-totalevents"></a>

##### `TotalEvents`

```csharp
int TotalEvents { get; }
```

Gets the number of events supplied in the callback payload.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatuscallbackresult-translatedevents"></a>

##### `TranslatedEvents`

```csharp
int TranslatedEvents { get; }
```

Gets the number of events translated into Cephalon delivery-status events.

<a id="type-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatusendpointroutebuilderextensions"></a>

### `MailgunInvitationDeliveryStatusEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for Mailgun webhook tenant-invitation delivery status callbacks.

#### Declaration
```csharp
public static class MailgunInvitationDeliveryStatusEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-aspnetcore-hosting-mailguninvitationdeliverystatusendpointroutebuilderextensions-mapcephalonmailguninvitationdeliverystatuscallbacks-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonMailgunInvitationDeliveryStatusCallbacks`

```csharp
IEndpointRouteBuilder MapCephalonMailgunInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
```

Maps the optional Mailgun webhook tenant-invitation delivery status callback endpoint.

Remarks: The endpoint translates Mailgun webhook JSON payloads into the host-agnostic `ITenantInvitationDeliveryStatusReconciler`. It can also verify Mailgun HMAC-SHA256 webhook signatures and reject bounded process-local token replays when configured. Durable inboxing and provider polling remain host-managed or future provider-pack responsibilities.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.
