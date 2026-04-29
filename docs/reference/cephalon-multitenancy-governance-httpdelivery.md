# Cephalon.MultiTenancy.Governance.HttpDelivery

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.HttpDelivery)
## Namespaces

- `Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration`
- `Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting`
- `Cephalon.MultiTenancy.Governance.HttpDelivery.Services`

<a id="namespace-cephalon-multitenancy-governance-httpdelivery-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration

<a id="type-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions"></a>

### `HttpInvitationDeliveryOptions`

Configures HTTP webhook delivery for tenant invitations dispatched by the governance companion pack.

Remarks: The HTTP sender queues or sends a delivery request to a configured endpoint. It does not guarantee final recipient delivery and does not encode product-specific email, SMS, chat, or identity-provider semantics.

#### Declaration
```csharp
public sealed class HttpInvitationDeliveryOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-ctor"></a>

##### `HttpInvitationDeliveryOptions`

```csharp
HttpInvitationDeliveryOptions()
```

Creates HTTP invitation delivery options with the default sender identifier and timeout.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether the HTTP invitation sender should be registered.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-endpoint"></a>

##### `Endpoint`

```csharp
string Endpoint { get; set; }
```

Gets or sets the absolute HTTP endpoint that receives invitation delivery payloads.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-expectedstatuscodes"></a>

##### `ExpectedStatusCodes`

```csharp
IReadOnlyList<int> ExpectedStatusCodes { get; set; }
```

Gets or sets explicit response status codes that indicate the webhook accepted the dispatch.

Remarks: When empty, any successful 2xx response is accepted.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; set; }
```

Gets or sets additional HTTP headers added to every delivery request.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-includeinvitationmetadata"></a>

##### `IncludeInvitationMetadata`

```csharp
bool IncludeInvitationMetadata { get; set; }
```

Gets or sets a value indicating whether invitation metadata should be included in the webhook payload.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-includerequestmetadata"></a>

##### `IncludeRequestMetadata`

```csharp
bool IncludeRequestMetadata { get; set; }
```

Gets or sets a value indicating whether dispatch request metadata should be included in the webhook payload.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-includeresponsebodyinmetadata"></a>

##### `IncludeResponseBodyInMetadata`

```csharp
bool IncludeResponseBodyInMetadata { get; set; }
```

Gets or sets a value indicating whether a bounded response body excerpt should be copied into sender metadata.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-maxattempts"></a>

##### `MaxAttempts`

```csharp
int MaxAttempts { get; set; }
```

Gets or sets the total number of HTTP dispatch attempts for transient delivery failures.

Remarks: The value is clamped to the supported range of 1 through 10. The default preserves single-attempt behavior.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-method"></a>

##### `Method`

```csharp
string Method { get; set; }
```

Gets or sets the HTTP method used for delivery requests.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-providermessageidheadername"></a>

##### `ProviderMessageIdHeaderName`

```csharp
string ProviderMessageIdHeaderName { get; set; }
```

Gets or sets the response header that contains the provider message identifier.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-responsebodymetadatalimit"></a>

##### `ResponseBodyMetadataLimit`

```csharp
int ResponseBodyMetadataLimit { get; set; }
```

Gets or sets the maximum response body characters copied into sender metadata when enabled.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-retrydelaymilliseconds"></a>

##### `RetryDelayMilliseconds`

```csharp
int RetryDelayMilliseconds { get; set; }
```

Gets or sets the fixed delay, in milliseconds, between retry attempts.

Remarks: The value is clamped to the supported range of 0 through 60000 milliseconds.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-retrystatuscodes"></a>

##### `RetryStatusCodes`

```csharp
IReadOnlyList<int> RetryStatusCodes { get; set; }
```

Gets or sets response status codes that should be retried when the dispatch has attempts remaining.

Remarks: The default covers common transient HTTP responses: 408, 429, 500, 502, 503, and 504.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-retrytransportfailures"></a>

##### `RetryTransportFailures`

```csharp
bool RetryTransportFailures { get; set; }
```

Gets or sets a value indicating whether transient transport failures should be retried when attempts remain.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; set; }
```

Gets or sets the sender identifier used by `TenantInvitationDeliveryRequest.SenderId`.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-signatureheadername"></a>

##### `SignatureHeaderName`

```csharp
string SignatureHeaderName { get; set; }
```

Gets or sets the request header that carries the webhook signature.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-signaturekeyidheadername"></a>

##### `SignatureKeyIdHeaderName`

```csharp
string SignatureKeyIdHeaderName { get; set; }
```

Gets or sets the request header that carries the optional signing key identifier.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-signaturetimestampheadername"></a>

##### `SignatureTimestampHeaderName`

```csharp
string SignatureTimestampHeaderName { get; set; }
```

Gets or sets the request header that carries the Unix timestamp included in the webhook signature.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-signingkeyid"></a>

##### `SigningKeyId`

```csharp
string SigningKeyId { get; set; }
```

Gets or sets an optional key identifier sent with signed webhook requests.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-signingsecret"></a>

##### `SigningSecret`

```csharp
string SigningSecret { get; set; }
```

Gets or sets the shared secret used to sign webhook payloads with HMAC-SHA256.

Remarks: When empty, the sender does not add Cephalon webhook signature headers.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-supportedchannels"></a>

##### `SupportedChannels`

```csharp
IReadOnlyList<string> SupportedChannels { get; set; }
```

Gets or sets delivery channels accepted by this sender.

Remarks: When empty, the sender accepts every requested channel.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the maximum time allowed for the HTTP delivery request.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
HttpInvitationDeliveryOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds HTTP invitation delivery options from configuration.

Returns: The bound HTTP invitation delivery options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-multitenancy-governance-httpdelivery-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting

<a id="type-cephalon-multitenancy-governance-httpdelivery-hosting-httpinvitationdeliveryservicecollectionextensions"></a>

### `HttpInvitationDeliveryServiceCollectionExtensions`

Adds HTTP webhook invitation delivery services to a Cephalon host.

#### Declaration
```csharp
public static class HttpInvitationDeliveryServiceCollectionExtensions
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-httpdelivery-hosting-httpinvitationdeliveryservicecollectionextensions-httpclientname"></a>

##### `HttpClientName`

```csharp
const string HttpClientName
```

The named HTTP client used by the HTTP invitation delivery sender.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-httpdelivery-hosting-httpinvitationdeliveryservicecollectionextensions-addcephalonhttpinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions"></a>

##### `AddCephalonHttpInvitationDelivery`

```csharp
IServiceCollection AddCephalonHttpInvitationDelivery(this IServiceCollection services, Action<HttpInvitationDeliveryOptions> configure)
```

Adds HTTP invitation delivery using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures the HTTP invitation delivery sender.

<a id="member-m-cephalon-multitenancy-governance-httpdelivery-hosting-httpinvitationdeliveryservicecollectionextensions-addcephalonhttpinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-httpdelivery-configuration-httpinvitationdeliveryoptions"></a>

##### `AddCephalonHttpInvitationDelivery`

```csharp
IServiceCollection AddCephalonHttpInvitationDelivery(this IServiceCollection services, IConfiguration configuration, Action<HttpInvitationDeliveryOptions> configure)
```

Adds HTTP invitation delivery using configuration as the primary source of webhook settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven setup.

<a id="namespace-cephalon-multitenancy-governance-httpdelivery-services"></a>

## Namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Services

<a id="type-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload"></a>

### `HttpInvitationDeliveryPayload`

Describes the JSON payload sent to an HTTP invitation delivery webhook.

#### Declaration
```csharp
public sealed class HttpInvitationDeliveryPayload
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-ctor"></a>

##### `HttpInvitationDeliveryPayload`

```csharp
HttpInvitationDeliveryPayload()
```

Creates an empty HTTP invitation delivery payload for JSON serialization.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-actor"></a>

##### `Actor`

```csharp
string Actor { get; set; }
```

Gets or sets the actor that requested dispatch.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-channel"></a>

##### `Channel`

```csharp
string Channel { get; set; }
```

Gets or sets the requested delivery channel.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or sets the optional correlation identifier.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-dispatchedatutc"></a>

##### `DispatchedAtUtc`

```csharp
DateTimeOffset DispatchedAtUtc { get; set; }
```

Gets or sets the UTC timestamp used for dispatch.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the optional invitation display name.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-invitationid"></a>

##### `InvitationId`

```csharp
string InvitationId { get; set; }
```

Gets or sets the invitation identifier.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-invitationmetadata"></a>

##### `InvitationMetadata`

```csharp
IReadOnlyDictionary<string, string> InvitationMetadata { get; set; }
```

Gets or sets optional metadata attached to the invitation.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-inviteeid"></a>

##### `InviteeId`

```csharp
string InviteeId { get; set; }
```

Gets or sets the invitee identifier.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-inviteekind"></a>

##### `InviteeKind`

```csharp
string InviteeKind { get; set; }
```

Gets or sets the invitee kind.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Gets or sets optional request metadata.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-requestedsenderid"></a>

##### `RequestedSenderId`

```csharp
string RequestedSenderId { get; set; }
```

Gets or sets the requested sender identifier.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-roles"></a>

##### `Roles`

```csharp
IReadOnlyList<string> Roles { get; set; }
```

Gets or sets the tenant-local roles proposed by the invitation.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-source"></a>

##### `Source`

```csharp
string Source { get; set; }
```

Gets or sets the dispatch source.

<a id="member-p-cephalon-multitenancy-governance-httpdelivery-services-httpinvitationdeliverypayload-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; set; }
```

Gets or sets the tenant identifier.
