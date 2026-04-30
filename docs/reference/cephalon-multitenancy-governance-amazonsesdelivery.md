# Cephalon.MultiTenancy.Governance.AmazonSesDelivery

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.AmazonSesDelivery)
## Namespaces

- `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Configuration`
- `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Hosting`
- `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services`

<a id="namespace-cephalon-multitenancy-governance-amazonsesdelivery-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Configuration

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions"></a>

### `AmazonSesInvitationDeliveryOptions`

Configures Amazon SES v2 delivery for tenant invitations dispatched by the governance companion pack.

Remarks: The Amazon SES sender submits one SES v2 `SendEmail` simple message request. It does not own AWS account setup, sender identity verification, bounce/complaint callbacks, provider polling, public onboarding, SMS, chat, CRM, or identity-provider invitation flows.

#### Declaration
```csharp
public sealed class AmazonSesInvitationDeliveryOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-ctor"></a>

##### `AmazonSesInvitationDeliveryOptions`

```csharp
AmazonSesInvitationDeliveryOptions()
```

Initializes a new instance of the `AmazonSesInvitationDeliveryOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-acceptedstatuscodes"></a>

##### `AcceptedStatusCodes`

```csharp
IReadOnlyList<int> AcceptedStatusCodes { get; set; }
```

Gets or sets response status codes that indicate Amazon SES accepted the request.

Remarks: The default accepts `200 OK`, which the AWS SDK reports for a successful SES v2 `SendEmail` call.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-configurationsetname"></a>

##### `ConfigurationSetName`

```csharp
string ConfigurationSetName { get; set; }
```

Gets or sets the optional SES configuration set name attached to the request.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether the Amazon SES invitation sender should be registered.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-fromemail"></a>

##### `FromEmail`

```csharp
string FromEmail { get; set; }
```

Gets or sets the sender email address used in the SES message.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-fromname"></a>

##### `FromName`

```csharp
string FromName { get; set; }
```

Gets or sets the optional sender display name used in the SES message.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-htmlbodytemplate"></a>

##### `HtmlBodyTemplate`

```csharp
string HtmlBodyTemplate { get; set; }
```

Gets or sets the optional HTML Amazon SES message body template.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-includecontexttags"></a>

##### `IncludeContextTags`

```csharp
bool IncludeContextTags { get; set; }
```

Gets or sets a value indicating whether safe Cephalon context tags should be added to the SES request.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-recipientemailmetadatakey"></a>

##### `RecipientEmailMetadataKey`

```csharp
string RecipientEmailMetadataKey { get; set; }
```

Gets or sets the metadata key used to resolve the recipient email address when the invitee id is not an email address.

Remarks: The sender checks dispatch metadata first and invitation metadata second. If neither contains a value and `InviteeKind` is `email`, the invitee id is treated as the recipient address.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-regionsystemname"></a>

##### `RegionSystemName`

```csharp
string RegionSystemName { get; set; }
```

Gets or sets the optional AWS region system name used when Cephalon creates the default SES v2 client.

Remarks: When omitted, the AWS SDK default region resolution chain remains authoritative. Example values include `us-east-1` and `eu-west-1`.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-replytoaddresses"></a>

##### `ReplyToAddresses`

```csharp
IReadOnlyList<string> ReplyToAddresses { get; set; }
```

Gets or sets reply-to addresses attached to the SES message.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; set; }
```

Gets or sets the sender identifier used by `TenantInvitationDeliveryRequest.SenderId`.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-subjecttemplate"></a>

##### `SubjectTemplate`

```csharp
string SubjectTemplate { get; set; }
```

Gets or sets the Amazon SES message subject template.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-supportedchannels"></a>

##### `SupportedChannels`

```csharp
IReadOnlyList<string> SupportedChannels { get; set; }
```

Gets or sets delivery channels accepted by this sender.

Remarks: When empty, the sender accepts every requested channel.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-tags"></a>

##### `Tags`

```csharp
IReadOnlyDictionary<string, string> Tags { get; set; }
```

Gets or sets Amazon SES message tags attached to the request.

Remarks: These values are sent to Amazon SES and can appear in provider event publishing. Do not put secrets here.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-textbodytemplate"></a>

##### `TextBodyTemplate`

```csharp
string TextBodyTemplate { get; set; }
```

Gets or sets the plain-text Amazon SES message body template.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the maximum time allowed for the Amazon SES SDK request.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
AmazonSesInvitationDeliveryOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Amazon SES invitation delivery options from configuration.

Returns: The bound Amazon SES invitation delivery options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-multitenancy-governance-amazonsesdelivery-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Hosting

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-hosting-amazonsesinvitationdeliveryservicecollectionextensions"></a>

### `AmazonSesInvitationDeliveryServiceCollectionExtensions`

Adds Amazon SES v2 invitation delivery services to a Cephalon host.

#### Declaration
```csharp
public static class AmazonSesInvitationDeliveryServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-hosting-amazonsesinvitationdeliveryservicecollectionextensions-addcephalonamazonsesinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions"></a>

##### `AddCephalonAmazonSesInvitationDelivery`

```csharp
IServiceCollection AddCephalonAmazonSesInvitationDelivery(this IServiceCollection services, Action<AmazonSesInvitationDeliveryOptions> configure)
```

Adds Amazon SES invitation delivery using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures the Amazon SES invitation delivery sender.

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-hosting-amazonsesinvitationdeliveryservicecollectionextensions-addcephalonamazonsesinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-amazonsesdelivery-configuration-amazonsesinvitationdeliveryoptions"></a>

##### `AddCephalonAmazonSesInvitationDelivery`

```csharp
IServiceCollection AddCephalonAmazonSesInvitationDelivery(this IServiceCollection services, IConfiguration configuration, Action<AmazonSesInvitationDeliveryOptions> configure)
```

Adds Amazon SES invitation delivery using configuration as the primary source of SES settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven setup.

<a id="namespace-cephalon-multitenancy-governance-amazonsesdelivery-services"></a>

## Namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliveryclientresult"></a>

### `AmazonSesInvitationDeliveryClientResult`

Describes the result returned by an Amazon SES invitation delivery client.

#### Declaration
```csharp
public sealed class AmazonSesInvitationDeliveryClientResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliveryclientresult-ctor-system-boolean-system-nullable-system-int32-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AmazonSesInvitationDeliveryClientResult`

```csharp
AmazonSesInvitationDeliveryClientResult(bool accepted, int? statusCode, string providerMessageId, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates an Amazon SES invitation delivery client result.

Parameters:
- `accepted`: A value indicating whether Amazon SES accepted the request.
- `statusCode`: The HTTP status code reported by the AWS SDK when one is known.
- `providerMessageId`: The Amazon SES message identifier when one is known.
- `reason`: The provider-facing outcome reason.
- `metadata`: Optional safe client metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliveryclientresult-accepted"></a>

##### `Accepted`

```csharp
bool Accepted { get; }
```

Gets a value indicating whether Amazon SES accepted the request.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliveryclientresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional safe client metadata.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliveryclientresult-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the Amazon SES message identifier when one is known.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliveryclientresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the provider-facing outcome reason.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliveryclientresult-statuscode"></a>

##### `StatusCode`

```csharp
int? StatusCode { get; }
```

Gets the HTTP status code reported by the AWS SDK when one is known.

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage"></a>

### `AmazonSesInvitationDeliveryMessage`

Describes a prepared Amazon SES invitation delivery message.

#### Declaration
```csharp
public sealed class AmazonSesInvitationDeliveryMessage
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string"></a>

##### `AmazonSesInvitationDeliveryMessage`

```csharp
AmazonSesInvitationDeliveryMessage(string messageId, string from, string toEmail, string subject, string textBody, string htmlBody, IReadOnlyList<string> replyToAddresses, IReadOnlyDictionary<string, string> tags, string configurationSetName)
```

Creates a prepared Amazon SES invitation delivery message.

Parameters:
- `messageId`: The deterministic Cephalon message identifier carried in Amazon SES message tags.
- `from`: The formatted sender address.
- `toEmail`: The recipient email address.
- `subject`: The message subject.
- `textBody`: The plain-text message body.
- `htmlBody`: The optional HTML message body.
- `replyToAddresses`: Reply-to addresses attached to the message.
- `tags`: Amazon SES message tags attached to the message.
- `configurationSetName`: The optional SES configuration set name attached to the request.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-configurationsetname"></a>

##### `ConfigurationSetName`

```csharp
string ConfigurationSetName { get; }
```

Gets the optional SES configuration set name attached to the request.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-from"></a>

##### `From`

```csharp
string From { get; }
```

Gets the formatted sender address.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-hashtmlbody"></a>

##### `HasHtmlBody`

```csharp
bool HasHtmlBody { get; }
```

Gets a value indicating whether the message has an HTML body.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-htmlbody"></a>

##### `HtmlBody`

```csharp
string HtmlBody { get; }
```

Gets the optional HTML message body.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; }
```

Gets the deterministic Cephalon message identifier carried in Amazon SES message tags.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-replytoaddresses"></a>

##### `ReplyToAddresses`

```csharp
IReadOnlyList<string> ReplyToAddresses { get; }
```

Gets reply-to addresses attached to the message.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-subject"></a>

##### `Subject`

```csharp
string Subject { get; }
```

Gets the message subject.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-tags"></a>

##### `Tags`

```csharp
IReadOnlyDictionary<string, string> Tags { get; }
```

Gets Amazon SES message tags attached to the message.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-textbody"></a>

##### `TextBody`

```csharp
string TextBody { get; }
```

Gets the plain-text message body.

<a id="member-p-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-toemail"></a>

##### `ToEmail`

```csharp
string ToEmail { get; }
```

Gets the recipient email address.

<a id="type-cephalon-multitenancy-governance-amazonsesdelivery-services-iamazonsesinvitationdeliveryclient"></a>

### `IAmazonSesInvitationDeliveryClient`

Sends Amazon SES invitation delivery messages for the Amazon SES companion pack.

Remarks: Hosts can replace this service to route `SendEmail` requests through a custom AWS SDK client, test double, gateway, or platform-specific retry policy while retaining the same Cephalon invitation dispatcher and sender metadata contract.

#### Declaration
```csharp
public interface IAmazonSesInvitationDeliveryClient
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-amazonsesdelivery-services-iamazonsesinvitationdeliveryclient-sendasync-cephalon-multitenancy-governance-amazonsesdelivery-services-amazonsesinvitationdeliverymessage-system-threading-cancellationtoken"></a>

##### `SendAsync`

```csharp
ValueTask<AmazonSesInvitationDeliveryClientResult> SendAsync(AmazonSesInvitationDeliveryMessage message, CancellationToken cancellationToken)
```

Sends one Amazon SES invitation delivery message.

Returns: The client result normalized for Cephalon sender reporting.

Parameters:
- `message`: The message prepared by the Amazon SES invitation delivery sender.
- `cancellationToken`: A token that cancels the send operation.
