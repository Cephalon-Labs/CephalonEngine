# Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore)
## Namespaces

- `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration`
- `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting`

<a id="namespace-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions"></a>

### `AmazonSesInvitationDeliveryAspNetCoreOptions`

Configures ASP.NET Core Amazon SES over SNS callback translation for tenant-invitation delivery status updates.

Remarks: This adapter translates SNS-wrapped Amazon SES event publishing payloads into Cephalon delivery-status reconciliation requests. It does not own AWS account setup, SES identity verification, SNS topic/subscription creation, durable callback inboxes, distributed replay protection, or provider polling. When configured, it can verify the Amazon SNS message signature before translation and skip duplicate SNS message identifiers already recorded by the Cephalon delivery-status observation store.

#### Declaration
```csharp
public sealed class AmazonSesInvitationDeliveryAspNetCoreOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-ctor"></a>

##### `AmazonSesInvitationDeliveryAspNetCoreOptions`

```csharp
AmazonSesInvitationDeliveryAspNetCoreOptions()
```

Initializes a new instance of the `AmazonSesInvitationDeliveryAspNetCoreOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-acceptrawseseventpayloads"></a>

##### `AcceptRawSesEventPayloads`

```csharp
bool AcceptRawSesEventPayloads { get; set; }
```

Gets or sets a value indicating whether raw Amazon SES event payloads should be accepted for controlled replay.

Remarks: Production SNS HTTP subscriptions post an SNS envelope whose `Message` field contains the SES event. This option lets tests or replay tools post the SES event body directly without claiming a durable callback inbox.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-actor"></a>

##### `Actor`

```csharp
string Actor { get; set; }
```

Gets or sets the actor value recorded on translated Amazon SES delivery status observations.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-allowedsnstopicarns"></a>

##### `AllowedSnsTopicArns`

```csharp
string[] AllowedSnsTopicArns { get; set; }
```

Gets or sets the SNS topic ARNs accepted by this callback endpoint when topic allow-listing is required.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-enablesnsmessageididempotency"></a>

##### `EnableSnsMessageIdIdempotency`

```csharp
bool EnableSnsMessageIdIdempotency { get; set; }
```

Gets or sets a value indicating whether translated SNS notifications should skip duplicate `MessageId` values that already exist in the Cephalon delivery-status observation store.

Remarks: This guard uses the stable SNS `MessageId`-derived observation id emitted by the translator. It does not replace durable inboxing or distributed callback processing; the durability of the guard follows the configured `ITenantInvitationDeliveryStatusObservationStore`.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-enablesnsreplayprotection"></a>

##### `EnableSnsReplayProtection`

```csharp
bool EnableSnsReplayProtection { get; set; }
```

Gets or sets a value indicating whether verified SNS callbacks should be protected against replay inside the current process.

Remarks: Replay protection is active only when `RequireSnsSignatureVerification` is enabled and the SNS envelope verifies successfully. The built-in guard stores bounded fingerprints derived from `TopicArn` and `MessageId` in memory and does not claim distributed replay protection or durable callback inbox ownership.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-enablestatuscallbackendpoint"></a>

##### `EnableStatusCallbackEndpoint`

```csharp
bool EnableStatusCallbackEndpoint { get; set; }
```

Gets or sets a value indicating whether the Amazon SES callback endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-excludestatuscallbackendpointfromdescription"></a>

##### `ExcludeStatusCallbackEndpointFromDescription`

```csharp
bool ExcludeStatusCallbackEndpointFromDescription { get; set; }
```

Gets or sets a value indicating whether the Amazon SES callback endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-mapengagementeventsasdelivered"></a>

##### `MapEngagementEventsAsDelivered`

```csharp
bool MapEngagementEventsAsDelivered { get; set; }
```

Gets or sets a value indicating whether Amazon SES engagement events such as open and click should be recorded as delivered.

Remarks: The default is `false` so the endpoint records deliverability events only. Enable this when a host deliberately wants engagement events to update invitation delivery status.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-maxeventsperrequest"></a>

##### `MaxEventsPerRequest`

```csharp
int MaxEventsPerRequest { get; set; }
```

Gets or sets the maximum number of Amazon SES events accepted in one callback request.

Remarks: SNS HTTP callbacks normally contain one SES event in the `Message` field. Arrays are accepted only for controlled replay and test harness scenarios while keeping the same bounded parsing posture.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-maxrequestbodybytes"></a>

##### `MaxRequestBodyBytes`

```csharp
int MaxRequestBodyBytes { get; set; }
```

Gets or sets the maximum request body size accepted by the Amazon SES callback endpoint, in bytes.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-pinnedsnssigningcertificatepem"></a>

##### `PinnedSnsSigningCertificatePem`

```csharp
string PinnedSnsSigningCertificatePem { get; set; }
```

Gets or sets a pinned X.509 certificate PEM used to verify SNS signatures instead of downloading the certificate from `SigningCertURL`.

Remarks: This is primarily useful for tests, controlled replay, or hosts that deliberately pin the SNS signing certificate. Production hosts usually leave this unset so the endpoint retrieves the AWS SNS signing certificate from the validated HTTPS URL in the SNS envelope.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-recordstatus"></a>

##### `RecordStatus`

```csharp
bool RecordStatus { get; set; }
```

Gets or sets a value indicating whether translated delivery status should be recorded on the invitation.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-requireallowedsnstopicarn"></a>

##### `RequireAllowedSnsTopicArn`

```csharp
bool RequireAllowedSnsTopicArn { get; set; }
```

Gets or sets a value indicating whether `TopicArn` must match `AllowedSnsTopicArns` when signature verification is required.

Remarks: Keeping this enabled follows the SNS spoofing-prevention guidance that receivers reject messages from unexpected topics. Disable only for controlled multi-topic gateways that apply their own allow-list.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-requireprovidermessagematch"></a>

##### `RequireProviderMessageMatch`

```csharp
bool RequireProviderMessageMatch { get; set; }
```

Gets or sets a value indicating whether translated Amazon SES events must match an existing provider message id.

Remarks: Amazon SES event payloads expose the SES-assigned message id through `mail.messageId`. Keeping this guard enabled makes the callback translator reconcile only the invitation dispatch previously accepted by SES.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-requiresnssignatureverification"></a>

##### `RequireSnsSignatureVerification`

```csharp
bool RequireSnsSignatureVerification { get; set; }
```

Gets or sets a value indicating whether SNS message signatures must verify before translation.

Remarks: When enabled, the endpoint rejects raw SES replay payloads, validates the SNS envelope, verifies the Base64-encoded RSA signature over the canonical SNS string-to-sign, and records safe verification metadata.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-requiresnssignatureversion2"></a>

##### `RequireSnsSignatureVersion2`

```csharp
bool RequireSnsSignatureVersion2 { get; set; }
```

Gets or sets a value indicating whether verified SNS messages must use `SignatureVersion` 2.

Remarks: Amazon SNS topics default to signature version 1, but version 2 uses SHA-256 and is the recommended setting for new deployments. Disable this only when a host deliberately accepts legacy SHA-1 SNS signatures.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-requirestatuscallbackauthorization"></a>

##### `RequireStatusCallbackAuthorization`

```csharp
bool RequireStatusCallbackAuthorization { get; set; }
```

Gets or sets a value indicating whether the Amazon SES callback endpoint should require authorization.

Remarks: The endpoint performs an in-handler authorization check by default. Hosts can satisfy it with ASP.NET Core authentication, a gateway, or deliberately disable it for trusted test hosts.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-snsreplaycachelimit"></a>

##### `SnsReplayCacheLimit`

```csharp
int SnsReplayCacheLimit { get; set; }
```

Gets or sets the maximum number of verified SNS callback replay fingerprints retained in the current process.

Remarks: When the bounded cache is full, the oldest fingerprint is evicted before recording a new accepted signed callback.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-snsreplayretentionseconds"></a>

##### `SnsReplayRetentionSeconds`

```csharp
int SnsReplayRetentionSeconds { get; set; }
```

Gets or sets the process-local retention window, in seconds, for verified SNS callback replay fingerprints.

Remarks: The endpoint clamps the effective retention to at least one second. The default is five minutes.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-source"></a>

##### `Source`

```csharp
string Source { get; set; }
```

Gets or sets the source value recorded on translated Amazon SES delivery status observations.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-statuscallbackauthorizationpolicy"></a>

##### `StatusCallbackAuthorizationPolicy`

```csharp
string StatusCallbackAuthorizationPolicy { get; set; }
```

Gets or sets the optional ASP.NET Core authorization policy required by the Amazon SES callback endpoint.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-statuscallbackroutepattern"></a>

##### `StatusCallbackRoutePattern`

```csharp
string StatusCallbackRoutePattern { get; set; }
```

Gets or sets the ASP.NET Core route pattern used for SNS-wrapped Amazon SES callbacks.

Remarks: The default route stays under `/engine` because this endpoint is a provider-adapter ingress surface, not an application-owned onboarding API.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-validatesnssigningcertificatechain"></a>

##### `ValidateSnsSigningCertificateChain`

```csharp
bool ValidateSnsSigningCertificateChain { get; set; }
```

Gets or sets a value indicating whether the SNS signing certificate chain and validity window should be checked.

Remarks: The default is `true` for production safety. Tests using self-signed pinned certificates can disable this without weakening the canonical message-signature proof.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
AmazonSesInvitationDeliveryAspNetCoreOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads Amazon SES ASP.NET Core callback options from configuration.

Returns: The parsed Amazon SES ASP.NET Core callback options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The engine root section path to read from.

<a id="namespace-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliveryaspnetcoreservicecollectionextensions"></a>

### `AmazonSesInvitationDeliveryAspNetCoreServiceCollectionExtensions`

Registers ASP.NET Core Amazon SES over SNS callback translation services for tenant-invitation delivery status callbacks.

#### Declaration
```csharp
public static class AmazonSesInvitationDeliveryAspNetCoreServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliveryaspnetcoreservicecollectionextensions-addcephalonamazonsesinvitationdeliveryaspnetcore-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-configuration-amazonsesinvitationdeliveryaspnetcoreoptions"></a>

##### `AddCephalonAmazonSesInvitationDeliveryAspNetCore`

```csharp
IServiceCollection AddCephalonAmazonSesInvitationDeliveryAspNetCore(this IServiceCollection services, IConfiguration configuration, Action<AmazonSesInvitationDeliveryAspNetCoreOptions> configure)
```

Adds Amazon SES over SNS callback translation services using configuration as the primary setup source.

Returns: The same service collection for fluent registration.

Parameters:
- `services`: The service collection to extend.
- `configuration`: The optional configuration root.
- `configure`: An optional callback that can extend or override configuration-driven options.

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult"></a>

### `AmazonSesInvitationDeliveryStatusCallbackEventResult`

Describes how one SNS-wrapped Amazon SES event was translated and reconciled.

#### Declaration
```csharp
public sealed class AmazonSesInvitationDeliveryStatusCallbackEventResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-ctor-system-int32-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-string"></a>

##### `AmazonSesInvitationDeliveryStatusCallbackEventResult`

```csharp
AmazonSesInvitationDeliveryStatusCallbackEventResult(int index, string snsMessageId, string snsMessageType, string amazonSesMessageId, string amazonSesEventType, string tenantId, string invitationId, string status, string outcome, bool translated, bool reconciled, string reason)
```

Creates an Amazon SES callback event result.

Parameters:
- `index`: The zero-based event index inside the request payload.
- `snsMessageId`: The SNS message identifier when supplied.
- `snsMessageType`: The SNS message type when supplied.
- `amazonSesMessageId`: The Amazon SES message identifier when supplied.
- `amazonSesEventType`: The Amazon SES event type when supplied.
- `tenantId`: The Cephalon tenant identifier when supplied.
- `invitationId`: The Cephalon invitation identifier when supplied.
- `status`: The normalized Cephalon delivery status when translated.
- `outcome`: The translation or reconciliation outcome.
- `translated`: A value indicating whether the event was translated into a reconciliation request.
- `reconciled`: A value indicating whether the event reconciled a tenant invitation.
- `reason`: The operator-facing reason for the event outcome.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-amazonseseventtype"></a>

##### `AmazonSesEventType`

```csharp
string AmazonSesEventType { get; }
```

Gets the Amazon SES event type when supplied.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-amazonsesmessageid"></a>

##### `AmazonSesMessageId`

```csharp
string AmazonSesMessageId { get; }
```

Gets the Amazon SES message identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-index"></a>

##### `Index`

```csharp
int Index { get; }
```

Gets the zero-based event index inside the request payload.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; }
```

Gets the Cephalon invitation identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the translation or reconciliation outcome.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing reason for the event outcome.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-reconciled"></a>

##### `Reconciled`

```csharp
bool Reconciled { get; }
```

Gets a value indicating whether the event reconciled a tenant invitation.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-snsmessageid"></a>

##### `SnsMessageId`

```csharp
string SnsMessageId { get; }
```

Gets the SNS message identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-snsmessagetype"></a>

##### `SnsMessageType`

```csharp
string SnsMessageType { get; }
```

Gets the SNS message type when supplied.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the normalized Cephalon delivery status when translated.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the Cephalon tenant identifier when supplied.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-translated"></a>

##### `Translated`

```csharp
bool Translated { get; }
```

Gets a value indicating whether the event was translated into a reconciliation request.

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult"></a>

### `AmazonSesInvitationDeliveryStatusCallbackResult`

Describes an Amazon SES over SNS callback translation response.

#### Declaration
```csharp
public sealed class AmazonSesInvitationDeliveryStatusCallbackResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-ctor-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-boolean-system-boolean-system-string-system-collections-generic-ireadonlylist-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackeventresult-system-boolean-system-string-system-int32"></a>

##### `AmazonSesInvitationDeliveryStatusCallbackResult`

```csharp
AmazonSesInvitationDeliveryStatusCallbackResult(string routePattern, int totalEvents, int translatedEvents, int reconciledEvents, int skippedEvents, int deniedEvents, bool snsSignatureVerificationRequired, bool snsSignatureVerified, string snsSignatureVerificationOutcome, IReadOnlyList<AmazonSesInvitationDeliveryStatusCallbackEventResult> events, bool snsReplayProtectionEnabled, string snsReplayProtectionOutcome, int duplicateEvents)
```

Creates an Amazon SES callback translation response.

Parameters:
- `routePattern`: The endpoint route pattern that accepted the callback.
- `totalEvents`: The number of events supplied in the callback payload.
- `translatedEvents`: The number of events translated into Cephalon reconciliation requests.
- `reconciledEvents`: The number of events reconciled by Cephalon governance.
- `skippedEvents`: The number of events skipped before reconciliation.
- `deniedEvents`: The number of translated events denied by the reconciler.
- `snsSignatureVerificationRequired`: A value indicating whether SNS signature verification was required.
- `snsSignatureVerified`: A value indicating whether the SNS signature verified.
- `snsSignatureVerificationOutcome`: The SNS signature verification outcome.
- `events`: Per-event translation and reconciliation results.
- `snsReplayProtectionEnabled`: A value indicating whether process-local SNS replay protection was enabled for this verified callback.
- `snsReplayProtectionOutcome`: The SNS replay-protection outcome.
- `duplicateEvents`: The number of translated Amazon SES SNS events skipped because their SNS message id was already observed.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-deniedevents"></a>

##### `DeniedEvents`

```csharp
int DeniedEvents { get; }
```

Gets the number of translated events denied by the reconciler.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-duplicateevents"></a>

##### `DuplicateEvents`

```csharp
int DuplicateEvents { get; }
```

Gets the number of translated Amazon SES SNS events skipped because their SNS message id was already observed.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-events"></a>

##### `Events`

```csharp
IReadOnlyList<AmazonSesInvitationDeliveryStatusCallbackEventResult> Events { get; }
```

Gets per-event translation and reconciliation results.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-reconciledevents"></a>

##### `ReconciledEvents`

```csharp
int ReconciledEvents { get; }
```

Gets the number of events reconciled by Cephalon governance.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-routepattern"></a>

##### `RoutePattern`

```csharp
string RoutePattern { get; }
```

Gets the endpoint route pattern that accepted the callback.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-skippedevents"></a>

##### `SkippedEvents`

```csharp
int SkippedEvents { get; }
```

Gets the number of events skipped before reconciliation.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-snsreplayprotectionenabled"></a>

##### `SnsReplayProtectionEnabled`

```csharp
bool SnsReplayProtectionEnabled { get; }
```

Gets a value indicating whether process-local SNS replay protection was enabled for this verified callback.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-snsreplayprotectionoutcome"></a>

##### `SnsReplayProtectionOutcome`

```csharp
string SnsReplayProtectionOutcome { get; }
```

Gets the SNS replay-protection outcome.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-snssignatureverificationoutcome"></a>

##### `SnsSignatureVerificationOutcome`

```csharp
string SnsSignatureVerificationOutcome { get; }
```

Gets the SNS signature verification outcome.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-snssignatureverificationrequired"></a>

##### `SnsSignatureVerificationRequired`

```csharp
bool SnsSignatureVerificationRequired { get; }
```

Gets a value indicating whether SNS signature verification was required.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-snssignatureverified"></a>

##### `SnsSignatureVerified`

```csharp
bool SnsSignatureVerified { get; }
```

Gets a value indicating whether the SNS signature verified.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-totalevents"></a>

##### `TotalEvents`

```csharp
int TotalEvents { get; }
```

Gets the number of events supplied in the callback payload.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatuscallbackresult-translatedevents"></a>

##### `TranslatedEvents`

```csharp
int TranslatedEvents { get; }
```

Gets the number of events translated into Cephalon delivery-status events.

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatusendpointroutebuilderextensions"></a>

### `AmazonSesInvitationDeliveryStatusEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for SNS-wrapped Amazon SES tenant-invitation delivery status callbacks.

#### Declaration
```csharp
public static class AmazonSesInvitationDeliveryStatusEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-aspnetcore-hosting-amazonsesinvitationdeliverystatusendpointroutebuilderextensions-mapcephalonamazonsesinvitationdeliverystatuscallbacks-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonAmazonSesInvitationDeliveryStatusCallbacks`

```csharp
IEndpointRouteBuilder MapCephalonAmazonSesInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
```

Maps the optional Amazon SES over SNS tenant-invitation delivery status callback endpoint.

Remarks: The endpoint translates SNS HTTP notifications containing Amazon SES event publishing payloads into the host-agnostic `ITenantInvitationDeliveryStatusReconciler`. SNS subscription confirmation, durable inboxing, distributed replay protection, and provider polling remain host-managed or future provider-pack responsibilities. When configured, the endpoint verifies the SNS message signature before translation and skips duplicate SNS message identifiers already present in the Cephalon delivery-status observation store.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.
