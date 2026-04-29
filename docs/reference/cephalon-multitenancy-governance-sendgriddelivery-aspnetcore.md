# Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore)
## Namespaces

- `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Configuration`
- `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting`

<a id="namespace-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Configuration

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions"></a>

### `SendGridInvitationDeliveryAspNetCoreOptions`

Configures ASP.NET Core SendGrid Event Webhook callback translation for tenant-invitation delivery status updates.

#### Declaration
```csharp
public sealed class SendGridInvitationDeliveryAspNetCoreOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-ctor"></a>

##### `SendGridInvitationDeliveryAspNetCoreOptions`

```csharp
SendGridInvitationDeliveryAspNetCoreOptions()
```

Initializes a new instance of the `SendGridInvitationDeliveryAspNetCoreOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-actor"></a>

##### `Actor`

```csharp
string Actor { get; set; }
```

Gets or sets the actor value recorded on translated SendGrid delivery status observations.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-enablesignedeventwebhookreplayprotection"></a>

##### `EnableSignedEventWebhookReplayProtection`

```csharp
bool EnableSignedEventWebhookReplayProtection { get; set; }
```

Gets or sets a value indicating whether verified SendGrid signed Event Webhook requests should be protected against replay inside the current process.

Remarks: Replay protection is active only when `RequireSignedEventWebhook` is enabled and the request signature verifies successfully. The built-in guard stores bounded signature fingerprints in memory and does not claim distributed replay protection or durable provider callback inbox ownership.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-enablestatuscallbackendpoint"></a>

##### `EnableStatusCallbackEndpoint`

```csharp
bool EnableStatusCallbackEndpoint { get; set; }
```

Gets or sets a value indicating whether the SendGrid Event Webhook callback endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-excludestatuscallbackendpointfromdescription"></a>

##### `ExcludeStatusCallbackEndpointFromDescription`

```csharp
bool ExcludeStatusCallbackEndpointFromDescription { get; set; }
```

Gets or sets a value indicating whether the SendGrid callback endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-mapengagementeventsasdelivered"></a>

##### `MapEngagementEventsAsDelivered`

```csharp
bool MapEngagementEventsAsDelivered { get; set; }
```

Gets or sets a value indicating whether SendGrid engagement events such as open and click should be recorded as delivered.

Remarks: The default is `false` so the endpoint records deliverability events only. Enable this when a host deliberately wants engagement events to update invitation delivery status.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-maxeventsperrequest"></a>

##### `MaxEventsPerRequest`

```csharp
int MaxEventsPerRequest { get; set; }
```

Gets or sets the maximum number of SendGrid events accepted in one callback request.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-maxrequestbodybytes"></a>

##### `MaxRequestBodyBytes`

```csharp
int MaxRequestBodyBytes { get; set; }
```

Gets or sets the maximum request body size accepted by the SendGrid callback endpoint, in bytes.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-normalizeprovidermessageidfromsgmessageid"></a>

##### `NormalizeProviderMessageIdFromSgMessageId`

```csharp
bool NormalizeProviderMessageIdFromSgMessageId { get; set; }
```

Gets or sets a value indicating whether the prefix before the first dot in `sg_message_id` should be used for provider matching.

Remarks: Twilio SendGrid recommends storing the Mail Send API `X-Message-ID` response header to correlate Event Webhook posts. Event Webhook payloads carry `sg_message_id`; the prefix commonly matches the stored `X-Message-ID`, so this option keeps Cephalon's provider-message guard usable without weakening it.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-recordstatus"></a>

##### `RecordStatus`

```csharp
bool RecordStatus { get; set; }
```

Gets or sets a value indicating whether translated delivery status should be recorded on the invitation.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-requireprovidermessagematch"></a>

##### `RequireProviderMessageMatch`

```csharp
bool RequireProviderMessageMatch { get; set; }
```

Gets or sets a value indicating whether translated SendGrid events must match an existing provider message id.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-requiresignedeventwebhook"></a>

##### `RequireSignedEventWebhook`

```csharp
bool RequireSignedEventWebhook { get; set; }
```

Gets or sets a value indicating whether SendGrid signed Event Webhook requests must verify before translation.

Remarks: When enabled, the endpoint verifies the SendGrid ECDSA-SHA256 signature over the exact raw request body plus the timestamp header before parsing JSON or reconciling any event. The public verification key must be configured.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-requirestatuscallbackauthorization"></a>

##### `RequireStatusCallbackAuthorization`

```csharp
bool RequireStatusCallbackAuthorization { get; set; }
```

Gets or sets a value indicating whether the SendGrid callback endpoint should require authorization.

Remarks: The endpoint performs an in-handler authorization check by default. Hosts can satisfy it with ASP.NET Core authentication, a SendGrid OAuth policy, a gateway, or deliberately disable it for trusted test hosts.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-signedeventwebhookpublickey"></a>

##### `SignedEventWebhookPublicKey`

```csharp
string SignedEventWebhookPublicKey { get; set; }
```

Gets or sets the SendGrid public verification key used for signed Event Webhook verification.

Remarks: The value may be a PEM public key or a Base64-encoded SubjectPublicKeyInfo payload. Environment-variable friendly escaped newlines (`\n`) are normalized before import.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-signedeventwebhookreplaycachelimit"></a>

##### `SignedEventWebhookReplayCacheLimit`

```csharp
int SignedEventWebhookReplayCacheLimit { get; set; }
```

Gets or sets the maximum number of verified SendGrid signed Event Webhook fingerprints retained in the current process.

Remarks: When the bounded cache is full, the oldest fingerprint is evicted before recording a new accepted signed callback.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-signedeventwebhookreplayretentionseconds"></a>

##### `SignedEventWebhookReplayRetentionSeconds`

```csharp
int SignedEventWebhookReplayRetentionSeconds { get; set; }
```

Gets or sets the process-local retention window, in seconds, for verified SendGrid signed Event Webhook fingerprints.

Remarks: The endpoint clamps the effective retention to at least one second. The default matches the signature timestamp tolerance.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-signedeventwebhooksignatureheadername"></a>

##### `SignedEventWebhookSignatureHeaderName`

```csharp
string SignedEventWebhookSignatureHeaderName { get; set; }
```

Gets or sets the request header that carries the SendGrid Event Webhook signature.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-signedeventwebhooksignaturetoleranceseconds"></a>

##### `SignedEventWebhookSignatureToleranceSeconds`

```csharp
int SignedEventWebhookSignatureToleranceSeconds { get; set; }
```

Gets or sets the allowed clock skew, in seconds, for SendGrid signed Event Webhook timestamps.

Remarks: The endpoint clamps the effective tolerance to at least one second. The default is five minutes.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-signedeventwebhooktimestampheadername"></a>

##### `SignedEventWebhookTimestampHeaderName`

```csharp
string SignedEventWebhookTimestampHeaderName { get; set; }
```

Gets or sets the request header that carries the Unix timestamp included in the SendGrid Event Webhook signature.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-source"></a>

##### `Source`

```csharp
string Source { get; set; }
```

Gets or sets the source value recorded on translated SendGrid delivery status observations.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-statuscallbackauthorizationpolicy"></a>

##### `StatusCallbackAuthorizationPolicy`

```csharp
string StatusCallbackAuthorizationPolicy { get; set; }
```

Gets or sets the optional ASP.NET Core authorization policy required by the SendGrid callback endpoint.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-statuscallbackroutepattern"></a>

##### `StatusCallbackRoutePattern`

```csharp
string StatusCallbackRoutePattern { get; set; }
```

Gets or sets the ASP.NET Core route pattern used for SendGrid Event Webhook callbacks.

Remarks: The default route stays under `/engine` because this endpoint is a provider-adapter ingress surface, not an application-owned onboarding API.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
SendGridInvitationDeliveryAspNetCoreOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads SendGrid ASP.NET Core callback options from configuration.

Returns: The parsed SendGrid ASP.NET Core callback options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The engine root section path to read from.

<a id="namespace-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliveryaspnetcoreservicecollectionextensions"></a>

### `SendGridInvitationDeliveryAspNetCoreServiceCollectionExtensions`

Registers ASP.NET Core SendGrid Event Webhook translation services for tenant-invitation delivery status callbacks.

#### Declaration
```csharp
public static class SendGridInvitationDeliveryAspNetCoreServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliveryaspnetcoreservicecollectionextensions-addcephalonsendgridinvitationdeliveryaspnetcore-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-configuration-sendgridinvitationdeliveryaspnetcoreoptions"></a>

##### `AddCephalonSendGridInvitationDeliveryAspNetCore`

```csharp
IServiceCollection AddCephalonSendGridInvitationDeliveryAspNetCore(this IServiceCollection services, IConfiguration configuration, Action<SendGridInvitationDeliveryAspNetCoreOptions> configure)
```

Adds SendGrid Event Webhook callback translation services using configuration as the primary setup source.

Returns: The same service collection for fluent registration.

Parameters:
- `services`: The service collection to extend.
- `configuration`: The optional configuration root.
- `configure`: An optional callback that can extend or override configuration-driven options.

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult"></a>

### `SendGridInvitationDeliveryStatusCallbackEventResult`

Describes how one SendGrid Event Webhook item was translated and reconciled.

#### Declaration
```csharp
public sealed class SendGridInvitationDeliveryStatusCallbackEventResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-ctor-system-int32-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-string"></a>

##### `SendGridInvitationDeliveryStatusCallbackEventResult`

```csharp
SendGridInvitationDeliveryStatusCallbackEventResult(int index, string sendGridEventId, string sendGridMessageId, string sendGridEventType, string tenantId, string invitationId, string status, string outcome, bool translated, bool reconciled, string reason)
```

Creates a SendGrid callback event result.

Parameters:
- `index`: The zero-based event index inside the request payload.
- `sendGridEventId`: The SendGrid event identifier when supplied.
- `sendGridMessageId`: The SendGrid message identifier when supplied.
- `sendGridEventType`: The SendGrid event type when supplied.
- `tenantId`: The Cephalon tenant identifier when supplied.
- `invitationId`: The Cephalon invitation identifier when supplied.
- `status`: The normalized Cephalon delivery status when translated.
- `outcome`: The translation or reconciliation outcome.
- `translated`: A value indicating whether the event was translated into a reconciliation request.
- `reconciled`: A value indicating whether the event reconciled a tenant invitation.
- `reason`: The operator-facing reason for the event outcome.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-index"></a>

##### `Index`

```csharp
int Index { get; }
```

Gets the zero-based event index inside the request payload.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the Cephalon invitation identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the translation or reconciliation outcome.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing reason for the event outcome.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-reconciled"></a>

##### `Reconciled`

```csharp
bool Reconciled { get; }
```

Gets a value indicating whether the event reconciled a tenant invitation.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-sendgrideventid"></a>

##### `SendGridEventId`

```csharp
string SendGridEventId { get; }
```

Gets the SendGrid event identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-sendgrideventtype"></a>

##### `SendGridEventType`

```csharp
string SendGridEventType { get; }
```

Gets the SendGrid event type when supplied.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-sendgridmessageid"></a>

##### `SendGridMessageId`

```csharp
string SendGridMessageId { get; }
```

Gets the SendGrid message identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the normalized Cephalon delivery status when translated.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the Cephalon tenant identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-translated"></a>

##### `Translated`

```csharp
bool Translated { get; }
```

Gets a value indicating whether the event was translated into a reconciliation request.

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult"></a>

### `SendGridInvitationDeliveryStatusCallbackResult`

Describes a SendGrid Event Webhook callback translation response.

#### Declaration
```csharp
public sealed class SendGridInvitationDeliveryStatusCallbackResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-ctor-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-collections-generic-ireadonlylist-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackeventresult-system-boolean-system-boolean-system-string-system-boolean-system-string"></a>

##### `SendGridInvitationDeliveryStatusCallbackResult`

```csharp
SendGridInvitationDeliveryStatusCallbackResult(string routePattern, int totalEvents, int translatedEvents, int reconciledEvents, int skippedEvents, int deniedEvents, IReadOnlyList<SendGridInvitationDeliveryStatusCallbackEventResult> events, bool signedEventWebhookVerificationRequired, bool signedEventWebhookVerified, string signedEventWebhookVerificationOutcome, bool signedEventWebhookReplayProtectionEnabled, string signedEventWebhookReplayProtectionOutcome)
```

Creates a SendGrid callback translation response.

Parameters:
- `routePattern`: The endpoint route pattern that accepted the callback.
- `totalEvents`: The number of events supplied in the callback payload.
- `translatedEvents`: The number of events translated into Cephalon reconciliation requests.
- `reconciledEvents`: The number of events reconciled by Cephalon governance.
- `skippedEvents`: The number of events skipped before reconciliation.
- `deniedEvents`: The number of translated events denied by the reconciler.
- `events`: Per-event translation and reconciliation results.
- `signedEventWebhookVerificationRequired`: A value indicating whether SendGrid signed Event Webhook verification was required for this callback.
- `signedEventWebhookVerified`: A value indicating whether the required SendGrid signed Event Webhook signature verified.
- `signedEventWebhookVerificationOutcome`: The signed Event Webhook verification outcome for this callback.
- `signedEventWebhookReplayProtectionEnabled`: A value indicating whether process-local replay protection was enabled for this verified signed callback.
- `signedEventWebhookReplayProtectionOutcome`: The replay-protection outcome for this callback.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-deniedevents"></a>

##### `DeniedEvents`

```csharp
int DeniedEvents { get; }
```

Gets the number of translated events denied by the reconciler.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-events"></a>

##### `Events`

```csharp
IReadOnlyList<SendGridInvitationDeliveryStatusCallbackEventResult> Events { get; }
```

Gets per-event translation and reconciliation results.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-reconciledevents"></a>

##### `ReconciledEvents`

```csharp
int ReconciledEvents { get; }
```

Gets the number of events reconciled by Cephalon governance.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-routepattern"></a>

##### `RoutePattern`

```csharp
string RoutePattern { get; }
```

Gets the endpoint route pattern that accepted the callback.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-signedeventwebhookreplayprotectionenabled"></a>

##### `SignedEventWebhookReplayProtectionEnabled`

```csharp
bool SignedEventWebhookReplayProtectionEnabled { get; }
```

Gets a value indicating whether process-local replay protection was enabled for this verified signed callback.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-signedeventwebhookreplayprotectionoutcome"></a>

##### `SignedEventWebhookReplayProtectionOutcome`

```csharp
string SignedEventWebhookReplayProtectionOutcome { get; }
```

Gets the replay-protection outcome for this callback.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-signedeventwebhookverificationoutcome"></a>

##### `SignedEventWebhookVerificationOutcome`

```csharp
string SignedEventWebhookVerificationOutcome { get; }
```

Gets the signed Event Webhook verification outcome for this callback.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-signedeventwebhookverificationrequired"></a>

##### `SignedEventWebhookVerificationRequired`

```csharp
bool SignedEventWebhookVerificationRequired { get; }
```

Gets a value indicating whether SendGrid signed Event Webhook verification was required for this callback.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-signedeventwebhookverified"></a>

##### `SignedEventWebhookVerified`

```csharp
bool SignedEventWebhookVerified { get; }
```

Gets a value indicating whether the required SendGrid signed Event Webhook signature verified.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-skippedevents"></a>

##### `SkippedEvents`

```csharp
int SkippedEvents { get; }
```

Gets the number of events skipped before reconciliation.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-totalevents"></a>

##### `TotalEvents`

```csharp
int TotalEvents { get; }
```

Gets the number of events supplied in the callback payload.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatuscallbackresult-translatedevents"></a>

##### `TranslatedEvents`

```csharp
int TranslatedEvents { get; }
```

Gets the number of events translated into Cephalon reconciliation requests.

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatusendpointroutebuilderextensions"></a>

### `SendGridInvitationDeliveryStatusEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for SendGrid Event Webhook tenant-invitation delivery status callbacks.

#### Declaration
```csharp
public static class SendGridInvitationDeliveryStatusEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-aspnetcore-hosting-sendgridinvitationdeliverystatusendpointroutebuilderextensions-mapcephalonsendgridinvitationdeliverystatuscallbacks-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonSendGridInvitationDeliveryStatusCallbacks`

```csharp
IEndpointRouteBuilder MapCephalonSendGridInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
```

Maps the optional SendGrid Event Webhook tenant-invitation delivery status callback endpoint.

Remarks: The endpoint translates SendGrid Event Webhook JSON arrays into the host-agnostic `ITenantInvitationDeliveryStatusReconciler`. It can also verify SendGrid signed Event Webhook signatures and reject bounded process-local signed-callback replays when configured. OAuth token validation, durable inboxing, and distributed replay protection remain host-managed or future provider-pack responsibilities.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.
