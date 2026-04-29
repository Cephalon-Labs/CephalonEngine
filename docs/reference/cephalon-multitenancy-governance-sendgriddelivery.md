# Cephalon.MultiTenancy.Governance.SendGridDelivery

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.SendGridDelivery)
## Namespaces

- `Cephalon.MultiTenancy.Governance.SendGridDelivery.Configuration`
- `Cephalon.MultiTenancy.Governance.SendGridDelivery.Hosting`
- `Cephalon.MultiTenancy.Governance.SendGridDelivery.Services`

<a id="namespace-cephalon-multitenancy-governance-sendgriddelivery-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Configuration

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions"></a>

### `SendGridInvitationDeliveryOptions`

Configures SendGrid Mail Send API delivery for tenant invitations dispatched by the governance companion pack.

Remarks: The SendGrid sender sends one transactional email request to the SendGrid Mail Send API. It does not own SendGrid Event Webhook callbacks, bounce translation, provider polling, public onboarding, SMS, chat, CRM, or identity-provider invitation flows.

#### Declaration
```csharp
public sealed class SendGridInvitationDeliveryOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-ctor"></a>

##### `SendGridInvitationDeliveryOptions`

```csharp
SendGridInvitationDeliveryOptions()
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-acceptedstatuscodes"></a>

##### `AcceptedStatusCodes`

```csharp
IReadOnlyList<int> AcceptedStatusCodes { get; set; }
```

Gets or sets response status codes that indicate the SendGrid API accepted the request.

Remarks: The default accepts `202 Accepted`. When sandbox mode is enabled, `200 OK` is also accepted.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-apikey"></a>

##### `ApiKey`

```csharp
string ApiKey { get; set; }
```

Gets or sets the SendGrid API key used as the bearer token.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-baseurl"></a>

##### `BaseUrl`

```csharp
string BaseUrl { get; set; }
```

Gets or sets the SendGrid v3 API base URL.

Remarks: The default is the global SendGrid endpoint. Hosts that use EU regional sending can set this value to `https://api.eu.sendgrid.com`.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-categories"></a>

##### `Categories`

```csharp
IReadOnlyList<string> Categories { get; set; }
```

Gets or sets SendGrid message categories added to each request.

Remarks: SendGrid allows up to ten category names. Values beyond that limit are ignored by the sender.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-customargs"></a>

##### `CustomArgs`

```csharp
IReadOnlyDictionary<string, string> CustomArgs { get; set; }
```

Gets or sets SendGrid custom arguments added to each personalization.

Remarks: These values are sent to SendGrid and may appear in provider activity or webhook data. Do not put secrets here.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether the SendGrid invitation sender should be registered.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-enablesandboxmode"></a>

##### `EnableSandboxMode`

```csharp
bool EnableSandboxMode { get; set; }
```

Gets or sets a value indicating whether SendGrid sandbox mode should be enabled.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-fromemail"></a>

##### `FromEmail`

```csharp
string FromEmail { get; set; }
```

Gets or sets the verified sender email address used in the SendGrid message.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-fromname"></a>

##### `FromName`

```csharp
string FromName { get; set; }
```

Gets or sets the optional sender display name used in the SendGrid message.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; set; }
```

Gets or sets additional SendGrid message headers added to each request.

Remarks: The sender filters empty, multi-line, and SendGrid-reserved headers before sending.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-htmlbodytemplate"></a>

##### `HtmlBodyTemplate`

```csharp
string HtmlBodyTemplate { get; set; }
```

Gets or sets the optional HTML SendGrid message body template.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-includecontextcustomargs"></a>

##### `IncludeContextCustomArgs`

```csharp
bool IncludeContextCustomArgs { get; set; }
```

Gets or sets a value indicating whether safe Cephalon custom arguments should be added to the SendGrid request.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-includecontextheaders"></a>

##### `IncludeContextHeaders`

```csharp
bool IncludeContextHeaders { get; set; }
```

Gets or sets a value indicating whether safe Cephalon context headers should be added to the SendGrid request.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-providermessageidheadername"></a>

##### `ProviderMessageIdHeaderName`

```csharp
string ProviderMessageIdHeaderName { get; set; }
```

Gets or sets the SendGrid response header that contains the provider message identifier.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-recipientemailmetadatakey"></a>

##### `RecipientEmailMetadataKey`

```csharp
string RecipientEmailMetadataKey { get; set; }
```

Gets or sets the metadata key used to resolve the recipient email address when the invitee id is not an email address.

Remarks: The sender checks dispatch metadata first and invitation metadata second. If neither contains a value and `InviteeKind` is `email`, the invitee id is treated as the recipient address.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; set; }
```

Gets or sets the sender identifier used by `TenantInvitationDeliveryRequest.SenderId`.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-subjecttemplate"></a>

##### `SubjectTemplate`

```csharp
string SubjectTemplate { get; set; }
```

Gets or sets the SendGrid message subject template.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-supportedchannels"></a>

##### `SupportedChannels`

```csharp
IReadOnlyList<string> SupportedChannels { get; set; }
```

Gets or sets delivery channels accepted by this sender.

Remarks: When empty, the sender accepts every requested channel.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-textbodytemplate"></a>

##### `TextBodyTemplate`

```csharp
string TextBodyTemplate { get; set; }
```

Gets or sets the plain-text SendGrid message body template.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the maximum time allowed for the SendGrid API request.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
SendGridInvitationDeliveryOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds SendGrid invitation delivery options from configuration.

Returns: The bound SendGrid invitation delivery options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-multitenancy-governance-sendgriddelivery-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Hosting

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-hosting-sendgridinvitationdeliveryservicecollectionextensions"></a>

### `SendGridInvitationDeliveryServiceCollectionExtensions`

Adds SendGrid Mail Send API invitation delivery services to a Cephalon host.

#### Declaration
```csharp
public static class SendGridInvitationDeliveryServiceCollectionExtensions
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-sendgriddelivery-hosting-sendgridinvitationdeliveryservicecollectionextensions-httpclientname"></a>

##### `HttpClientName`

```csharp
const string HttpClientName
```

The named HTTP client used by the SendGrid invitation delivery client.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-hosting-sendgridinvitationdeliveryservicecollectionextensions-addcephalonsendgridinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions"></a>

##### `AddCephalonSendGridInvitationDelivery`

```csharp
IServiceCollection AddCephalonSendGridInvitationDelivery(this IServiceCollection services, Action<SendGridInvitationDeliveryOptions> configure)
```

Adds SendGrid invitation delivery using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures the SendGrid invitation delivery sender.

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-hosting-sendgridinvitationdeliveryservicecollectionextensions-addcephalonsendgridinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-sendgriddelivery-configuration-sendgridinvitationdeliveryoptions"></a>

##### `AddCephalonSendGridInvitationDelivery`

```csharp
IServiceCollection AddCephalonSendGridInvitationDelivery(this IServiceCollection services, IConfiguration configuration, Action<SendGridInvitationDeliveryOptions> configure)
```

Adds SendGrid invitation delivery using configuration as the primary source of Mail Send API settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven setup.

<a id="namespace-cephalon-multitenancy-governance-sendgriddelivery-services"></a>

## Namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Services

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-services-isendgridinvitationdeliveryclient"></a>

### `ISendGridInvitationDeliveryClient`

Sends SendGrid invitation delivery messages for the SendGrid companion pack.

Remarks: Hosts can replace this service to route Mail Send requests through a custom SendGrid client, test double, gateway, or platform-specific HTTP policy while retaining the same Cephalon invitation dispatcher and sender metadata contract.

#### Declaration
```csharp
public interface ISendGridInvitationDeliveryClient
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-services-isendgridinvitationdeliveryclient-sendasync-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-system-threading-cancellationtoken"></a>

##### `SendAsync`

```csharp
ValueTask<SendGridInvitationDeliveryClientResult> SendAsync(SendGridInvitationDeliveryMessage message, CancellationToken cancellationToken)
```

Sends one SendGrid invitation delivery message.

Returns: The client result normalized for Cephalon sender reporting.

Parameters:
- `message`: The message prepared by the SendGrid invitation delivery sender.
- `cancellationToken`: A token that cancels the send operation.

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliveryclientresult"></a>

### `SendGridInvitationDeliveryClientResult`

Describes the result returned by a SendGrid invitation delivery client.

#### Declaration
```csharp
public sealed class SendGridInvitationDeliveryClientResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliveryclientresult-ctor-system-boolean-system-nullable-system-int32-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `SendGridInvitationDeliveryClientResult`

```csharp
SendGridInvitationDeliveryClientResult(bool accepted, int? statusCode, string providerMessageId, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a SendGrid invitation delivery client result.

Parameters:
- `accepted`: A value indicating whether the SendGrid API accepted the request.
- `statusCode`: The HTTP status code returned by SendGrid when one is known.
- `providerMessageId`: The SendGrid message identifier when one is known.
- `reason`: The provider-facing outcome reason.
- `metadata`: Optional safe client metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliveryclientresult-accepted"></a>

##### `Accepted`

```csharp
bool Accepted { get; }
```

Gets a value indicating whether the SendGrid API accepted the request.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliveryclientresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional safe client metadata.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliveryclientresult-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the SendGrid message identifier when one is known.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliveryclientresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the provider-facing outcome reason.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliveryclientresult-statuscode"></a>

##### `StatusCode`

```csharp
int? StatusCode { get; }
```

Gets the HTTP status code returned by SendGrid when one is known.

<a id="type-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage"></a>

### `SendGridInvitationDeliveryMessage`

Describes a prepared SendGrid invitation delivery message.

#### Declaration
```csharp
public sealed class SendGridInvitationDeliveryMessage
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-boolean"></a>

##### `SendGridInvitationDeliveryMessage`

```csharp
SendGridInvitationDeliveryMessage(string messageId, string fromEmail, string fromName, string toEmail, string toName, string subject, string textBody, string htmlBody, IReadOnlyList<string> categories, IReadOnlyDictionary<string, string> customArgs, IReadOnlyDictionary<string, string> headers, bool sandboxMode)
```

Creates a prepared SendGrid invitation delivery message.

Parameters:
- `messageId`: The deterministic Cephalon message identifier carried in SendGrid custom arguments.
- `fromEmail`: The sender email address.
- `fromName`: The optional sender display name.
- `toEmail`: The recipient email address.
- `toName`: The optional recipient display name.
- `subject`: The message subject.
- `textBody`: The plain-text message body.
- `htmlBody`: The optional HTML message body.
- `categories`: SendGrid categories attached to the message.
- `customArgs`: SendGrid custom arguments attached to the personalization.
- `headers`: Safe SendGrid message headers.
- `sandboxMode`: A value indicating whether SendGrid sandbox mode should be enabled.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-categories"></a>

##### `Categories`

```csharp
IReadOnlyList<string> Categories { get; }
```

Gets SendGrid categories attached to the message.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-customargs"></a>

##### `CustomArgs`

```csharp
IReadOnlyDictionary<string, string> CustomArgs { get; }
```

Gets SendGrid custom arguments attached to the personalization.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-fromemail"></a>

##### `FromEmail`

```csharp
string FromEmail { get; }
```

Gets the sender email address.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-fromname"></a>

##### `FromName`

```csharp
string FromName { get; }
```

Gets the optional sender display name.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; }
```

Gets safe SendGrid message headers.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-htmlbody"></a>

##### `HtmlBody`

```csharp
string HtmlBody { get; }
```

Gets the optional HTML message body.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; }
```

Gets the deterministic Cephalon message identifier carried in SendGrid custom arguments.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-sandboxmode"></a>

##### `SandboxMode`

```csharp
bool SandboxMode { get; }
```

Gets a value indicating whether SendGrid sandbox mode should be enabled.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-subject"></a>

##### `Subject`

```csharp
string Subject { get; }
```

Gets the message subject.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-textbody"></a>

##### `TextBody`

```csharp
string TextBody { get; }
```

Gets the plain-text message body.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-toemail"></a>

##### `ToEmail`

```csharp
string ToEmail { get; }
```

Gets the recipient email address.

<a id="member-p-cephalon-multitenancy-governance-sendgriddelivery-services-sendgridinvitationdeliverymessage-toname"></a>

##### `ToName`

```csharp
string ToName { get; }
```

Gets the optional recipient display name.
