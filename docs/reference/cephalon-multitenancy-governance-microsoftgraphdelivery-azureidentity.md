# Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity)
## Namespaces

- `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Configuration`
- `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Hosting`

<a id="namespace-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Configuration

<a id="type-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions"></a>

### `MicrosoftGraphInvitationDeliveryAzureIdentityOptions`

Configures Azure Identity token acquisition for Microsoft Graph tenant-invitation delivery.

Remarks: This companion package only supplies a Microsoft Graph bearer token through `Azure.Identity`. The Microsoft Graph sender still owns the `sendMail` request, while Microsoft Entra application registration, permissions, mailbox access policy, and credential lifecycle remain outside the Cephalon engine boundary.

#### Declaration
```csharp
public sealed class MicrosoftGraphInvitationDeliveryAzureIdentityOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-ctor"></a>

##### `MicrosoftGraphInvitationDeliveryAzureIdentityOptions`

```csharp
MicrosoftGraphInvitationDeliveryAzureIdentityOptions()
```

Initializes a new instance of the `MicrosoftGraphInvitationDeliveryAzureIdentityOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-authorityhost"></a>

##### `AuthorityHost`

```csharp
string AuthorityHost { get; set; }
```

Gets or sets the Microsoft Entra authority host used for token acquisition.

Remarks: Supported aliases are `AzurePublicCloud`, `AzureGovernment`, and `AzureChina`. Hosts can also provide an absolute HTTPS authority URI for a sovereign or private cloud.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether the Azure Identity token provider should be registered.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-excludeinteractivebrowsercredential"></a>

##### `ExcludeInteractiveBrowserCredential`

```csharp
bool ExcludeInteractiveBrowserCredential { get; set; }
```

Gets or sets a value indicating whether interactive browser authentication should be excluded.

Remarks: The default is `true` so production hosts do not accidentally launch browser prompts.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-excludemanagedidentitycredential"></a>

##### `ExcludeManagedIdentityCredential`

```csharp
bool ExcludeManagedIdentityCredential { get; set; }
```

Gets or sets a value indicating whether managed identity authentication should be excluded.

Remarks: The default keeps managed identity available because hosted Azure, workload identity, and service-style invitation delivery are the primary production scenarios for this package.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-managedidentityclientid"></a>

##### `ManagedIdentityClientId`

```csharp
string ManagedIdentityClientId { get; set; }
```

Gets or sets the client id for a user-assigned managed identity.

Remarks: Leave this value empty to allow `DefaultAzureCredential` to use a system-assigned managed identity or another credential source from its chain.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-scopes"></a>

##### `Scopes`

```csharp
IReadOnlyList<string> Scopes { get; set; }
```

Gets or sets the Microsoft Graph scopes requested from the configured Azure credential.

Remarks: The default is `https://graph.microsoft.com/.default`, which asks Microsoft Entra ID for the app's configured application permissions such as Microsoft Graph `Mail.Send`.

<a id="member-p-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; set; }
```

Gets or sets the Microsoft Entra tenant id used by `DefaultAzureCredential`.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MicrosoftGraphInvitationDeliveryAzureIdentityOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Azure Identity token-provider options from configuration.

Returns: The bound Azure Identity token-provider options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Hosting

<a id="type-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-hosting-microsoftgraphinvitationdeliveryazureidentityservicecollectionextensions"></a>

### `MicrosoftGraphInvitationDeliveryAzureIdentityServiceCollectionExtensions`

Adds Azure Identity token acquisition for Microsoft Graph invitation delivery.

#### Declaration
```csharp
public static class MicrosoftGraphInvitationDeliveryAzureIdentityServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-hosting-microsoftgraphinvitationdeliveryazureidentityservicecollectionextensions-addcephalonmicrosoftgraphinvitationdeliveryazureidentity-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions"></a>

##### `AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity`

```csharp
IServiceCollection AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(this IServiceCollection services, Action<MicrosoftGraphInvitationDeliveryAzureIdentityOptions> configure)
```

Adds the Azure Identity token provider using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures token acquisition.

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-hosting-microsoftgraphinvitationdeliveryazureidentityservicecollectionextensions-addcephalonmicrosoftgraphinvitationdeliveryazureidentity-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions"></a>

##### `AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity`

```csharp
IServiceCollection AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(this IServiceCollection services, IConfiguration configuration, Action<MicrosoftGraphInvitationDeliveryAzureIdentityOptions> configure)
```

Adds the Azure Identity token provider using configuration as the primary source of token-acquisition settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override configuration-driven settings.

<a id="member-m-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-hosting-microsoftgraphinvitationdeliveryazureidentityservicecollectionextensions-addcephalonmicrosoftgraphinvitationdeliveryazureidentity-microsoft-extensions-dependencyinjection-iservicecollection-azure-core-tokencredential-system-action-cephalon-multitenancy-governance-microsoftgraphdelivery-azureidentity-configuration-microsoftgraphinvitationdeliveryazureidentityoptions"></a>

##### `AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity`

```csharp
IServiceCollection AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(this IServiceCollection services, TokenCredential credential, Action<MicrosoftGraphInvitationDeliveryAzureIdentityOptions> configure)
```

Adds the Azure Identity token provider with an explicit credential instance.

Remarks: This overload is useful for tests, shared host credential factories, or applications that want to provide a specific `TokenCredential` such as `ManagedIdentityCredential`.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `credential`: The credential used to acquire Microsoft Graph access tokens.
- `configure`: An optional callback that configures token acquisition.
