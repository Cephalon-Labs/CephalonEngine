# Cephalon.MultiTenancy.Governance.SmtpDelivery

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.SmtpDelivery)
## Namespaces

- `Cephalon.MultiTenancy.Governance.SmtpDelivery.Configuration`
- `Cephalon.MultiTenancy.Governance.SmtpDelivery.Hosting`
- `Cephalon.MultiTenancy.Governance.SmtpDelivery.Services`

<a id="namespace-cephalon-multitenancy-governance-smtpdelivery-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Configuration

<a id="type-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions"></a>

### `SmtpInvitationDeliveryOptions`

Configures SMTP relay delivery for tenant invitations dispatched by the governance companion pack.

Remarks: The SMTP sender sends one email message through a configured SMTP relay. It does not own provider-specific transactional-email APIs, bounce handling, delivery callbacks, or public onboarding flows.

#### Declaration
```csharp
public sealed class SmtpInvitationDeliveryOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-ctor"></a>

##### `SmtpInvitationDeliveryOptions`

```csharp
SmtpInvitationDeliveryOptions()
```

#### Properties

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether the SMTP invitation sender should be registered.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-fromaddress"></a>

##### `FromAddress`

```csharp
string FromAddress { get; set; }
```

Gets or sets the sender email address used in the SMTP message.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-fromdisplayname"></a>

##### `FromDisplayName`

```csharp
string FromDisplayName { get; set; }
```

Gets or sets the optional sender display name used in the SMTP message.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; set; }
```

Gets or sets additional SMTP message headers added to every delivery message.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the SMTP relay host name.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-htmlbodytemplate"></a>

##### `HtmlBodyTemplate`

```csharp
string HtmlBodyTemplate { get; set; }
```

Gets or sets the optional HTML SMTP message body template.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-includecontextheaders"></a>

##### `IncludeContextHeaders`

```csharp
bool IncludeContextHeaders { get; set; }
```

Gets or sets a value indicating whether safe Cephalon context headers should be added to the SMTP message.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-messageiddomain"></a>

##### `MessageIdDomain`

```csharp
string MessageIdDomain { get; set; }
```

Gets or sets the domain used for deterministic SMTP `Message-Id` values.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the SMTP password when the relay requires explicit credentials.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the SMTP relay port.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-recipientaddressmetadatakey"></a>

##### `RecipientAddressMetadataKey`

```csharp
string RecipientAddressMetadataKey { get; set; }
```

Gets or sets the metadata key used to resolve the recipient email address when the invitee id is not an email address.

Remarks: The sender checks dispatch metadata first and invitation metadata second. If neither contains a value and `InviteeKind` is `email`, the invitee id is treated as the recipient address.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-senderid"></a>

##### `SenderId`

```csharp
string SenderId { get; set; }
```

Gets or sets the sender identifier used by `TenantInvitationDeliveryRequest.SenderId`.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-subjecttemplate"></a>

##### `SubjectTemplate`

```csharp
string SubjectTemplate { get; set; }
```

Gets or sets the SMTP message subject template.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-supportedchannels"></a>

##### `SupportedChannels`

```csharp
IReadOnlyList<string> SupportedChannels { get; set; }
```

Gets or sets delivery channels accepted by this sender.

Remarks: When empty, the sender accepts every requested channel.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-textbodytemplate"></a>

##### `TextBodyTemplate`

```csharp
string TextBodyTemplate { get; set; }
```

Gets or sets the plain-text SMTP message body template.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the maximum time allowed for the SMTP send operation.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-username"></a>

##### `UserName`

```csharp
string UserName { get; set; }
```

Gets or sets the SMTP username when the relay requires explicit credentials.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-usessl"></a>

##### `UseSsl`

```csharp
bool UseSsl { get; set; }
```

Gets or sets a value indicating whether SSL/TLS should be enabled for the SMTP relay connection.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
SmtpInvitationDeliveryOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds SMTP invitation delivery options from configuration.

Returns: The bound SMTP invitation delivery options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-multitenancy-governance-smtpdelivery-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Hosting

<a id="type-cephalon-multitenancy-governance-smtpdelivery-hosting-smtpinvitationdeliveryservicecollectionextensions"></a>

### `SmtpInvitationDeliveryServiceCollectionExtensions`

Adds SMTP relay invitation delivery services to a Cephalon host.

#### Declaration
```csharp
public static class SmtpInvitationDeliveryServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-smtpdelivery-hosting-smtpinvitationdeliveryservicecollectionextensions-addcephalonsmtpinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions"></a>

##### `AddCephalonSmtpInvitationDelivery`

```csharp
IServiceCollection AddCephalonSmtpInvitationDelivery(this IServiceCollection services, Action<SmtpInvitationDeliveryOptions> configure)
```

Adds SMTP invitation delivery using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures the SMTP invitation delivery sender.

<a id="member-m-cephalon-multitenancy-governance-smtpdelivery-hosting-smtpinvitationdeliveryservicecollectionextensions-addcephalonsmtpinvitationdelivery-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-smtpdelivery-configuration-smtpinvitationdeliveryoptions"></a>

##### `AddCephalonSmtpInvitationDelivery`

```csharp
IServiceCollection AddCephalonSmtpInvitationDelivery(this IServiceCollection services, IConfiguration configuration, Action<SmtpInvitationDeliveryOptions> configure)
```

Adds SMTP invitation delivery using configuration as the primary source of relay settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven setup.

<a id="namespace-cephalon-multitenancy-governance-smtpdelivery-services"></a>

## Namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Services

<a id="type-cephalon-multitenancy-governance-smtpdelivery-services-ismtpinvitationdeliveryclient"></a>

### `ISmtpInvitationDeliveryClient`

Sends SMTP invitation delivery messages for the SMTP companion pack.

Remarks: Hosts can replace this service to route SMTP messages through a custom relay client, test double, or platform-specific mail transport while retaining the same Cephalon invitation dispatcher and sender metadata contract.

#### Declaration
```csharp
public interface ISmtpInvitationDeliveryClient
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-smtpdelivery-services-ismtpinvitationdeliveryclient-sendasync-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-system-threading-cancellationtoken"></a>

##### `SendAsync`

```csharp
ValueTask<SmtpInvitationDeliveryClientResult> SendAsync(SmtpInvitationDeliveryMessage message, CancellationToken cancellationToken)
```

Sends one SMTP invitation delivery message.

Returns: The client result normalized for Cephalon sender reporting.

Parameters:
- `message`: The message prepared by the SMTP invitation delivery sender.
- `cancellationToken`: A token that cancels the send operation.

<a id="type-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliveryclientresult"></a>

### `SmtpInvitationDeliveryClientResult`

Describes the result returned by an SMTP invitation delivery client.

#### Declaration
```csharp
public sealed class SmtpInvitationDeliveryClientResult
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliveryclientresult-ctor-system-boolean-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `SmtpInvitationDeliveryClientResult`

```csharp
SmtpInvitationDeliveryClientResult(bool accepted, string providerMessageId, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates an SMTP invitation delivery client result.

Parameters:
- `accepted`: A value indicating whether the SMTP relay accepted the message.
- `providerMessageId`: The provider or relay message identifier when one is known.
- `reason`: The provider-facing outcome reason.
- `metadata`: Optional safe client metadata.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliveryclientresult-accepted"></a>

##### `Accepted`

```csharp
bool Accepted { get; }
```

Gets a value indicating whether the SMTP relay accepted the message.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliveryclientresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional safe client metadata.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliveryclientresult-providermessageid"></a>

##### `ProviderMessageId`

```csharp
string ProviderMessageId { get; }
```

Gets the provider or relay message identifier when one is known.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliveryclientresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the provider-facing outcome reason.

<a id="type-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage"></a>

### `SmtpInvitationDeliveryMessage`

Describes a prepared SMTP invitation delivery message.

#### Declaration
```csharp
public sealed class SmtpInvitationDeliveryMessage
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `SmtpInvitationDeliveryMessage`

```csharp
SmtpInvitationDeliveryMessage(string messageId, string fromAddress, string fromDisplayName, string toAddress, string toDisplayName, string subject, string textBody, string htmlBody, IReadOnlyDictionary<string, string> headers)
```

Creates a prepared SMTP invitation delivery message.

Parameters:
- `messageId`: The deterministic SMTP message identifier.
- `fromAddress`: The sender email address.
- `fromDisplayName`: The optional sender display name.
- `toAddress`: The recipient email address.
- `toDisplayName`: The optional recipient display name.
- `subject`: The message subject.
- `textBody`: The plain-text message body.
- `htmlBody`: The optional HTML message body.
- `headers`: Safe additional SMTP headers.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-fromaddress"></a>

##### `FromAddress`

```csharp
string FromAddress { get; }
```

Gets the sender email address.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-fromdisplayname"></a>

##### `FromDisplayName`

```csharp
string FromDisplayName { get; }
```

Gets the optional sender display name.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; }
```

Gets safe additional SMTP headers.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-htmlbody"></a>

##### `HtmlBody`

```csharp
string HtmlBody { get; }
```

Gets the optional HTML message body.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; }
```

Gets the deterministic SMTP message identifier.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-subject"></a>

##### `Subject`

```csharp
string Subject { get; }
```

Gets the message subject.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-textbody"></a>

##### `TextBody`

```csharp
string TextBody { get; }
```

Gets the plain-text message body.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-toaddress"></a>

##### `ToAddress`

```csharp
string ToAddress { get; }
```

Gets the recipient email address.

<a id="member-p-cephalon-multitenancy-governance-smtpdelivery-services-smtpinvitationdeliverymessage-todisplayname"></a>

##### `ToDisplayName`

```csharp
string ToDisplayName { get; }
```

Gets the optional recipient display name.
