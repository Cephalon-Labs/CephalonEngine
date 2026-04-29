# Cephalon.MultiTenancy.Governance.MailgunDelivery

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.MailgunDelivery)
## Namespaces

- `Cephalon.MultiTenancy.Governance.MailgunDelivery.Configuration`
- `Cephalon.MultiTenancy.Governance.MailgunDelivery.Hosting`
- `Cephalon.MultiTenancy.Governance.MailgunDelivery.Services`

<a id="namespace-cephalon-multitenancy-governance-mailgundelivery-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Configuration

<a id="type-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions"></a>

### `MailgunInvitationDeliveryOptions`

Configures Mailgun Messages API delivery for tenant invitations dispatched by the governance companion pack.

Remarks: The Mailgun sender posts one multipart Messages API request to Mailgun. It does not own Mailgun webhook callbacks, bounce translation, provider polling, public onboarding, SMS, chat, CRM, or identity-provider invitation flows.

#### Declaration
```csharp
public sealed class MailgunInvitationDeliveryOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-ctor"></a>

##### `MailgunInvitationDeliveryOptions`

```csharp
MailgunInvitationDeliveryOptions()
```

Initializes a new instance of the `MailgunInvitationDeliveryOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-acceptedstatuscodes"></a>

##### `AcceptedStatusCodes`

```csharp
IReadOnlyList<int> AcceptedStatusCodes { get; set; }
```

Gets or sets response status codes that indicate the Mailgun API accepted the request.

Remarks: The default accepts `200 OK`, which Mailgun returns when a message is queued.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-apikey"></a>

##### `ApiKey`

```csharp
string ApiKey { get; set; }
```

Gets or sets the Mailgun private API key used with basic authentication.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-baseurl"></a>

##### `BaseUrl`

```csharp
string BaseUrl { get; set; }
```

Gets or sets the Mailgun API base URL.

Remarks: The default is the US endpoint. Hosts that use EU regional sending can set this value to `https://api.eu.mailgun.net`.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-domainname"></a>

##### `DomainName`

```csharp
string DomainName { get; set; }
```

Gets or sets the Mailgun sending domain name used in the Messages API route.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether the Mailgun invitation sender should be registered.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-enabletestmode"></a>

##### `EnableTestMode`

```csharp
bool EnableTestMode { get; set; }
```

Gets or sets a value indicating whether Mailgun test mode should be enabled through `o:testmode=yes`.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-fromemail"></a>

##### `FromEmail`

```csharp
string FromEmail { get; set; }
```

Gets or sets the sender email address used in the Mailgun message.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-fromname"></a>

##### `FromName`

```csharp
string FromName { get; set; }
```

Gets or sets the optional sender display name used in the Mailgun message.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; set; }
```

Gets or sets additional Mailgun custom headers added through `h:*` form fields.

Remarks: The sender filters empty, multi-line, and message-core headers before sending.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-htmlbodytemplate"></a>

##### `HtmlBodyTemplate`

```csharp
string HtmlBodyTemplate { get; set; }
```

Gets or sets the optional HTML Mailgun message body template.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-includecontextheaders"></a>

##### `IncludeContextHeaders`

```csharp
bool IncludeContextHeaders { get; set; }
```

Gets or sets a value indicating whether safe Cephalon custom headers should be added to the Mailgun request.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-includecontextvariables"></a>

##### `IncludeContextVariables`

```csharp
bool IncludeContextVariables { get; set; }
```

Gets or sets a value indicating whether safe Cephalon user variables should be added to the Mailgun request.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-providermessageidjsonpropertyname"></a>

##### `ProviderMessageIdJsonPropertyName`

```csharp
string ProviderMessageIdJsonPropertyName { get; set; }
```

Gets or sets the JSON property name that contains the Mailgun provider message identifier.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-recipientemailmetadatakey"></a>

##### `RecipientEmailMetadataKey`

```csharp
string RecipientEmailMetadataKey { get; set; }
```

Gets or sets the metadata key used to resolve the recipient email address when the invitee id is not an email address.

Remarks: The sender checks dispatch metadata first and invitation metadata second. If neither contains a value and `InviteeKind` is `email`, the invitee id is treated as the recipient address.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; set; }
```

Gets or sets the sender identifier used by `TenantInvitationDeliveryRequest.SenderId`.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-subjecttemplate"></a>

##### `SubjectTemplate`

```csharp
string SubjectTemplate { get; set; }
```

Gets or sets the Mailgun message subject template.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-supportedchannels"></a>

##### `SupportedChannels`

```csharp
IReadOnlyList<string> SupportedChannels { get; set; }
```

Gets or sets delivery channels accepted by this sender.

Remarks: When empty, the sender accepts every requested channel.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; set; }
```

Gets or sets Mailgun message tags added through `o:tag` form fields.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-textbodytemplate"></a>

##### `TextBodyTemplate`

```csharp
string TextBodyTemplate { get; set; }
```

Gets or sets the plain-text Mailgun message body template.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the maximum time allowed for the Mailgun API request.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-variables"></a>

##### `Variables`

```csharp
IReadOnlyDictionary<string, string> Variables { get; set; }
```

Gets or sets Mailgun user variables added through `v:*` form fields.

Remarks: These values are sent to Mailgun and may appear in provider activity or webhook data. Do not put secrets here.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MailgunInvitationDeliveryOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Mailgun invitation delivery options from configuration.

Returns: The bound Mailgun invitation delivery options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-multitenancy-governance-mailgundelivery-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Hosting

<a id="type-cephalon-multitenancy-governance-mailgundelivery-hosting-mailguninvitationdeliveryservicecollectionextensions"></a>

### `MailgunInvitationDeliveryServiceCollectionExtensions`

Adds Mailgun Messages API invitation delivery services to a Cephalon host.

#### Declaration
```csharp
public static class MailgunInvitationDeliveryServiceCollectionExtensions
```

#### Fields

<a id="member-f-cephalon-multitenancy-governance-mailgundelivery-hosting-mailguninvitationdeliveryservicecollectionextensions-httpclientname"></a>

##### `HttpClientName`

```csharp
const string HttpClientName
```

The named HTTP client used by the Mailgun invitation delivery client.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-hosting-mailguninvitationdeliveryservicecollectionextensions-addcephalonmailguninvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions"></a>

##### `AddCephalonMailgunInvitationDelivery`

```csharp
IServiceCollection AddCephalonMailgunInvitationDelivery(this IServiceCollection services, Action<MailgunInvitationDeliveryOptions> configure)
```

Adds Mailgun invitation delivery using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures the Mailgun invitation delivery sender.

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-hosting-mailguninvitationdeliveryservicecollectionextensions-addcephalonmailguninvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-mailgundelivery-configuration-mailguninvitationdeliveryoptions"></a>

##### `AddCephalonMailgunInvitationDelivery`

```csharp
IServiceCollection AddCephalonMailgunInvitationDelivery(this IServiceCollection services, IConfiguration configuration, Action<MailgunInvitationDeliveryOptions> configure)
```

Adds Mailgun invitation delivery using configuration as the primary source of Messages API settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven setup.

<a id="namespace-cephalon-multitenancy-governance-mailgundelivery-services"></a>

## Namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Services

<a id="type-cephalon-multitenancy-governance-mailgundelivery-services-imailguninvitationdeliveryclient"></a>

### `IMailgunInvitationDeliveryClient`

Sends Mailgun invitation delivery messages for the Mailgun companion pack.

Remarks: Hosts can replace this service to route Messages API requests through a custom Mailgun client, test double, gateway, or platform-specific HTTP policy while retaining the same Cephalon invitation dispatcher and sender metadata contract.

#### Declaration
```csharp
public interface IMailgunInvitationDeliveryClient
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-services-imailguninvitationdeliveryclient-sendasync-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-system-threading-cancellationtoken"></a>

##### `SendAsync`

```csharp
ValueTask<MailgunInvitationDeliveryClientResult> SendAsync(MailgunInvitationDeliveryMessage message, CancellationToken cancellationToken)
```

Sends one Mailgun invitation delivery message.

Returns: The client result normalized for Cephalon sender reporting.

Parameters:
- `message`: The message prepared by the Mailgun invitation delivery sender.
- `cancellationToken`: A token that cancels the send operation.

<a id="type-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliveryclientresult"></a>

### `MailgunInvitationDeliveryClientResult`

Describes the result returned by a Mailgun invitation delivery client.

#### Declaration
```csharp
public sealed class MailgunInvitationDeliveryClientResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliveryclientresult-ctor-system-boolean-system-nullable-system-int32-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `MailgunInvitationDeliveryClientResult`

```csharp
MailgunInvitationDeliveryClientResult(bool accepted, int? statusCode, string providerMessageId, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a Mailgun invitation delivery client result.

Parameters:
- `accepted`: A value indicating whether the Mailgun API accepted the request.
- `statusCode`: The HTTP status code returned by Mailgun when one is known.
- `providerMessageId`: The Mailgun message identifier when one is known.
- `reason`: The provider-facing outcome reason.
- `metadata`: Optional safe client metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliveryclientresult-accepted"></a>

##### `Accepted`

```csharp
bool Accepted { get; }
```

Gets a value indicating whether the Mailgun API accepted the request.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliveryclientresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional safe client metadata.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliveryclientresult-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the Mailgun message identifier when one is known.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliveryclientresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the provider-facing outcome reason.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliveryclientresult-statuscode"></a>

##### `StatusCode`

```csharp
int? StatusCode { get; }
```

Gets the HTTP status code returned by Mailgun when one is known.

<a id="type-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage"></a>

### `MailgunInvitationDeliveryMessage`

Describes a prepared Mailgun invitation delivery message.

#### Declaration
```csharp
public sealed class MailgunInvitationDeliveryMessage
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-boolean"></a>

##### `MailgunInvitationDeliveryMessage`

```csharp
MailgunInvitationDeliveryMessage(string messageId, string from, string to, string toEmail, string subject, string textBody, string htmlBody, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> variables, IReadOnlyDictionary<string, string> headers, bool testMode)
```

Creates a prepared Mailgun invitation delivery message.

Parameters:
- `messageId`: The deterministic Cephalon message identifier carried in Mailgun user variables.
- `from`: The formatted sender address.
- `to`: The formatted recipient address.
- `toEmail`: The recipient email address.
- `subject`: The message subject.
- `textBody`: The plain-text message body.
- `htmlBody`: The optional HTML message body.
- `tags`: Mailgun tags attached to the message.
- `variables`: Mailgun user variables attached to the message.
- `headers`: Mailgun custom headers attached to the message.
- `testMode`: A value indicating whether Mailgun test mode should be enabled.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-from"></a>

##### `From`

```csharp
string From { get; }
```

Gets the formatted sender address.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; }
```

Gets Mailgun custom headers attached to the message.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-htmlbody"></a>

##### `HtmlBody`

```csharp
string HtmlBody { get; }
```

Gets the optional HTML message body.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; }
```

Gets the deterministic Cephalon message identifier carried in Mailgun user variables.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-subject"></a>

##### `Subject`

```csharp
string Subject { get; }
```

Gets the message subject.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets Mailgun tags attached to the message.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-testmode"></a>

##### `TestMode`

```csharp
bool TestMode { get; }
```

Gets a value indicating whether Mailgun test mode should be enabled.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-textbody"></a>

##### `TextBody`

```csharp
string TextBody { get; }
```

Gets the plain-text message body.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-to"></a>

##### `To`

```csharp
string To { get; }
```

Gets the formatted recipient address.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-toemail"></a>

##### `ToEmail`

```csharp
string ToEmail { get; }
```

Gets the recipient email address.

<a id="member-p-cephalon-multitenancy-governance-mailgundelivery-services-mailguninvitationdeliverymessage-variables"></a>

##### `Variables`

```csharp
IReadOnlyDictionary<string, string> Variables { get; }
```

Gets Mailgun user variables attached to the message.
