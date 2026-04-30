# Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery)
## Namespaces

- `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Configuration`
- `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Hosting`
- `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services`

<a id="namespace-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Configuration

<a id="type-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions"></a>

### `MicrosoftGraphInvitationDeliveryOptions`

Configures Microsoft Graph `sendMail` delivery for tenant invitations dispatched by the governance companion pack.

Remarks: The Microsoft Graph sender posts one JSON `sendMail` request to Microsoft Graph. It does not own OAuth credential issuance, mailbox provisioning, Graph change notifications, provider polling, public onboarding, SMS, chat, CRM, or identity-provider invitation flows.

#### Declaration
```csharp
public sealed class MicrosoftGraphInvitationDeliveryOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-ctor"></a>

##### `MicrosoftGraphInvitationDeliveryOptions`

```csharp
MicrosoftGraphInvitationDeliveryOptions()
```

Initializes a new instance of the `MicrosoftGraphInvitationDeliveryOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-acceptedstatuscodes"></a>

##### `AcceptedStatusCodes`

```csharp
IReadOnlyList<int> AcceptedStatusCodes { get; set; }
```

Gets or sets response status codes that indicate Microsoft Graph accepted the request.

Remarks: The default accepts `202 Accepted`, which means Graph accepted the send request but not that downstream mail delivery has completed.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-accesstoken"></a>

##### `AccessToken`

```csharp
string AccessToken { get; set; }
```

Gets or sets an optional static Microsoft Graph bearer token.

Remarks: Production hosts should usually register `IMicrosoftGraphInvitationDeliveryAccessTokenProvider` instead of storing a short-lived token in configuration.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-apiversion"></a>

##### `ApiVersion`

```csharp
string ApiVersion { get; set; }
```

Gets or sets the Microsoft Graph API version segment.

Remarks: The default is `v1.0`. Preview or beta endpoints should be used only by hosts that deliberately accept that external API stability posture.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-baseurl"></a>

##### `BaseUrl`

```csharp
string BaseUrl { get; set; }
```

Gets or sets the Microsoft Graph API base URL.

Remarks: The default targets the global Microsoft Graph cloud. Sovereign-cloud hosts can set this to the appropriate Graph endpoint while keeping the same `sendMail` request contract.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-categories"></a>

##### `Categories`

```csharp
IReadOnlyList<string> Categories { get; set; }
```

Gets or sets Microsoft Graph message categories added to each request.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether the Microsoft Graph invitation sender should be registered.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; set; }
```

Gets or sets custom Microsoft Graph internet message headers added to each request.

Remarks: The sender keeps only single-line custom `x-*` headers and filters message-core headers before sending.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-htmlbodytemplate"></a>

##### `HtmlBodyTemplate`

```csharp
string HtmlBodyTemplate { get; set; }
```

Gets or sets the optional HTML Microsoft Graph message body template.

Remarks: When configured, the sender uses a Graph message body with `contentType = HTML`. Otherwise it sends a plain text body.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-includecontextheaders"></a>

##### `IncludeContextHeaders`

```csharp
bool IncludeContextHeaders { get; set; }
```

Gets or sets a value indicating whether safe Cephalon context headers should be added to the Graph message.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-recipientemailmetadatakey"></a>

##### `RecipientEmailMetadataKey`

```csharp
string RecipientEmailMetadataKey { get; set; }
```

Gets or sets the metadata key used to resolve the recipient email address when the invitee id is not an email address.

Remarks: The sender checks dispatch metadata first and invitation metadata second. If neither contains a value and `InviteeKind` is `email`, the invitee id is treated as the recipient address.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-savetosentitems"></a>

##### `SaveToSentItems`

```csharp
bool SaveToSentItems { get; set; }
```

Gets or sets a value indicating whether Microsoft Graph should save the message to Sent Items.

Remarks: The default is `false` so service-style invitation dispatch does not fill the sender mailbox by default. Hosts can opt in when mailbox history is part of their compliance posture.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; set; }
```

Gets or sets the sender identifier used by `TenantInvitationDeliveryRequest.SenderId`.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-senderuserid"></a>

##### `SenderUserId`

```csharp
string SenderUserId { get; set; }
```

Gets or sets the mailbox user id or user principal name used in `/users/{id | userPrincipalName}/sendMail`.

Remarks: When omitted, the sender posts to `/me/sendMail`. Application-permission hosts normally configure this value and provide a token with Microsoft Graph `Mail.Send` permission.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-subjecttemplate"></a>

##### `SubjectTemplate`

```csharp
string SubjectTemplate { get; set; }
```

Gets or sets the Microsoft Graph message subject template.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-supportedchannels"></a>

##### `SupportedChannels`

```csharp
IReadOnlyList<string> SupportedChannels { get; set; }
```

Gets or sets delivery channels accepted by this sender.

Remarks: When empty, the sender accepts every requested channel.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-textbodytemplate"></a>

##### `TextBodyTemplate`

```csharp
string TextBodyTemplate { get; set; }
```

Gets or sets the plain-text Microsoft Graph message body template.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the maximum time allowed for the Microsoft Graph API request.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MicrosoftGraphInvitationDeliveryOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Microsoft Graph invitation delivery options from configuration.

Returns: The bound Microsoft Graph invitation delivery options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-multitenancy-governance-microsoftgraphdelivery-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Hosting

<a id="type-cephalon-multitenancy-governance-microsoftgraphdelivery-hosting-microsoftgraphinvitationdeliveryservicecollectionextensions"></a>

### `MicrosoftGraphInvitationDeliveryServiceCollectionExtensions`

Adds Microsoft Graph `sendMail` invitation delivery services to a Cephalon host.

#### Declaration
```csharp
public static class MicrosoftGraphInvitationDeliveryServiceCollectionExtensions
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-microsoftgraphdelivery-hosting-microsoftgraphinvitationdeliveryservicecollectionextensions-httpclientname"></a>

##### `HttpClientName`

```csharp
const string HttpClientName
```

The named HTTP client used by the Microsoft Graph invitation delivery client.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-hosting-microsoftgraphinvitationdeliveryservicecollectionextensions-addcephalonmicrosoftgraphinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions"></a>

##### `AddCephalonMicrosoftGraphInvitationDelivery`

```csharp
IServiceCollection AddCephalonMicrosoftGraphInvitationDelivery(this IServiceCollection services, Action<MicrosoftGraphInvitationDeliveryOptions> configure)
```

Adds Microsoft Graph invitation delivery using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures the Microsoft Graph invitation delivery sender.

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-hosting-microsoftgraphinvitationdeliveryservicecollectionextensions-addcephalonmicrosoftgraphinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-microsoftgraphdelivery-configuration-microsoftgraphinvitationdeliveryoptions"></a>

##### `AddCephalonMicrosoftGraphInvitationDelivery`

```csharp
IServiceCollection AddCephalonMicrosoftGraphInvitationDelivery(this IServiceCollection services, IConfiguration configuration, Action<MicrosoftGraphInvitationDeliveryOptions> configure)
```

Adds Microsoft Graph invitation delivery using configuration as the primary source of `sendMail` settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven setup.

<a id="namespace-cephalon-multitenancy-governance-microsoftgraphdelivery-services"></a>

## Namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services

<a id="type-cephalon-multitenancy-governance-microsoftgraphdelivery-services-imicrosoftgraphinvitationdeliveryaccesstokenprovider"></a>

### `IMicrosoftGraphInvitationDeliveryAccessTokenProvider`

Provides bearer tokens for Microsoft Graph invitation delivery requests.

Remarks: Hosts can replace this service to integrate Azure.Identity, managed identity, workload identity, a token cache, or another organization-specific OAuth flow without changing the Cephalon invitation dispatcher.

#### Declaration
```csharp
public interface IMicrosoftGraphInvitationDeliveryAccessTokenProvider
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-services-imicrosoftgraphinvitationdeliveryaccesstokenprovider-getaccesstokenasync-system-threading-cancellationtoken"></a>

##### `GetAccessTokenAsync`

```csharp
ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
```

Gets a bearer token that authorizes the Microsoft Graph `sendMail` request.

Returns: The bearer token without the `Bearer` scheme prefix.

Parameters:
- `cancellationToken`: A token that cancels token acquisition.

<a id="type-cephalon-multitenancy-governance-microsoftgraphdelivery-services-imicrosoftgraphinvitationdeliveryclient"></a>

### `IMicrosoftGraphInvitationDeliveryClient`

Sends Microsoft Graph invitation delivery messages for the Microsoft Graph companion pack.

Remarks: Hosts can replace this service to route `sendMail` requests through a custom Graph SDK client, test double, gateway, or platform-specific HTTP policy while retaining the same Cephalon invitation dispatcher and sender metadata contract.

#### Declaration
```csharp
public interface IMicrosoftGraphInvitationDeliveryClient
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-services-imicrosoftgraphinvitationdeliveryclient-sendasync-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-system-threading-cancellationtoken"></a>

##### `SendAsync`

```csharp
ValueTask<MicrosoftGraphInvitationDeliveryClientResult> SendAsync(MicrosoftGraphInvitationDeliveryMessage message, CancellationToken cancellationToken)
```

Sends one Microsoft Graph invitation delivery message.

Returns: The client result normalized for Cephalon sender reporting.

Parameters:
- `message`: The message prepared by the Microsoft Graph invitation delivery sender.
- `cancellationToken`: A token that cancels the send operation.

<a id="type-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliveryclientresult"></a>

### `MicrosoftGraphInvitationDeliveryClientResult`

Describes the result returned by a Microsoft Graph invitation delivery client.

#### Declaration
```csharp
public sealed class MicrosoftGraphInvitationDeliveryClientResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliveryclientresult-ctor-system-boolean-system-nullable-system-int32-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `MicrosoftGraphInvitationDeliveryClientResult`

```csharp
MicrosoftGraphInvitationDeliveryClientResult(bool accepted, int? statusCode, string providerMessageId, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a Microsoft Graph invitation delivery client result.

Parameters:
- `accepted`: A value indicating whether Microsoft Graph accepted the request.
- `statusCode`: The HTTP status code returned by Microsoft Graph when one is known.
- `providerMessageId`: The provider message identifier when one is known.
- `reason`: The provider-facing outcome reason.
- `metadata`: Optional safe client metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliveryclientresult-accepted"></a>

##### `Accepted`

```csharp
bool Accepted { get; }
```

Gets a value indicating whether Microsoft Graph accepted the request.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliveryclientresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional safe client metadata.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliveryclientresult-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the provider message identifier when one is known.

Remarks: Microsoft Graph `sendMail` normally returns `202 Accepted` without a message id. Implementations should leave this value empty unless they have a real provider message identifier rather than a request-id header.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliveryclientresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the provider-facing outcome reason.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliveryclientresult-statuscode"></a>

##### `StatusCode`

```csharp
int? StatusCode { get; }
```

Gets the HTTP status code returned by Microsoft Graph when one is known.

<a id="type-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage"></a>

### `MicrosoftGraphInvitationDeliveryMessage`

Describes a prepared Microsoft Graph invitation delivery message.

#### Declaration
```csharp
public sealed class MicrosoftGraphInvitationDeliveryMessage
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-boolean"></a>

##### `MicrosoftGraphInvitationDeliveryMessage`

```csharp
MicrosoftGraphInvitationDeliveryMessage(string messageId, string senderUserId, string toEmail, string toName, string subject, string textBody, string htmlBody, IReadOnlyList<string> categories, IReadOnlyDictionary<string, string> headers, bool saveToSentItems)
```

Creates a prepared Microsoft Graph invitation delivery message.

Parameters:
- `messageId`: The deterministic Cephalon message identifier carried in request metadata.
- `senderUserId`: The Microsoft Graph sender mailbox scope or `me`.
- `toEmail`: The recipient email address.
- `toName`: The optional recipient display name.
- `subject`: The message subject.
- `textBody`: The plain-text message body.
- `htmlBody`: The optional HTML message body.
- `categories`: Microsoft Graph categories attached to the message.
- `headers`: Safe Microsoft Graph internet message headers.
- `saveToSentItems`: A value indicating whether Graph should save the message to Sent Items.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-categories"></a>

##### `Categories`

```csharp
IReadOnlyList<string> Categories { get; }
```

Gets Microsoft Graph categories attached to the message.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-hashtmlbody"></a>

##### `HasHtmlBody`

```csharp
bool HasHtmlBody { get; }
```

Gets a value indicating whether the message uses HTML content.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; }
```

Gets safe Microsoft Graph internet message headers.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-htmlbody"></a>

##### `HtmlBody`

```csharp
string HtmlBody { get; }
```

Gets the optional HTML message body.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; }
```

Gets the deterministic Cephalon message identifier carried in request metadata.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-savetosentitems"></a>

##### `SaveToSentItems`

```csharp
bool SaveToSentItems { get; }
```

Gets a value indicating whether Graph should save the message to Sent Items.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-senderuserid"></a>

##### `SenderUserId`

```csharp
string SenderUserId { get; }
```

Gets the Microsoft Graph sender mailbox scope or `me`.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-subject"></a>

##### `Subject`

```csharp
string Subject { get; }
```

Gets the message subject.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-textbody"></a>

##### `TextBody`

```csharp
string TextBody { get; }
```

Gets the plain-text message body.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-toemail"></a>

##### `ToEmail`

```csharp
string ToEmail { get; }
```

Gets the recipient email address.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-services-microsoftgraphinvitationdeliverymessage-toname"></a>

##### `ToName`

```csharp
string ToName { get; }
```

Gets the optional recipient display name.
