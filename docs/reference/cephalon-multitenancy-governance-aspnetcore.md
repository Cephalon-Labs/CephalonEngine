# Cephalon.MultiTenancy.Governance.AspNetCore

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy.Governance.AspNetCore)
## Namespaces

- `Cephalon.MultiTenancy.Governance.AspNetCore.Configuration`
- `Cephalon.MultiTenancy.Governance.AspNetCore.Hosting`

<a id="namespace-cephalon-multitenancy-governance-aspnetcore-configuration"></a>

## Namespace Cephalon.MultiTenancy.Governance.AspNetCore.Configuration

<a id="type-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions"></a>

### `MultiTenancyGovernanceAspNetCoreOptions`

Configures ASP.NET Core-specific tenant-domain ownership governance endpoints.

#### Declaration
```csharp
public sealed class MultiTenancyGovernanceAspNetCoreOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-ctor"></a>

##### `MultiTenancyGovernanceAspNetCoreOptions`

```csharp
MultiTenancyGovernanceAspNetCoreOptions()
```

Initializes a new instance of the `MultiTenancyGovernanceAspNetCoreOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-cachecontrolheader"></a>

##### `CacheControlHeader`

```csharp
string CacheControlHeader { get; set; }
```

Gets or sets the cache-control header written for served proof files.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-enablehttpproofpublicationendpoint"></a>

##### `EnableHttpProofPublicationEndpoint`

```csharp
bool EnableHttpProofPublicationEndpoint { get; set; }
```

Gets or sets a value indicating whether the HTTP proof publication endpoint should be mapped.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-excludefromdescription"></a>

##### `ExcludeFromDescription`

```csharp
bool ExcludeFromDescription { get; set; }
```

Gets or sets a value indicating whether the proof endpoint should be excluded from OpenAPI descriptions.

<a id="member-p-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-routepattern"></a>

##### `RoutePattern`

```csharp
string RoutePattern { get; set; }
```

Gets or sets the endpoint route pattern used for published HTTP proof files.

Remarks: The default catch-all route is intentionally constrained under `/.well-known/cephalon/` so it does not compete with application-owned routes.

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MultiTenancyGovernanceAspNetCoreOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads ASP.NET Core governance adapter options from configuration.

Returns: The parsed ASP.NET Core governance adapter options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The engine root section path to read from.

<a id="namespace-cephalon-multitenancy-governance-aspnetcore-hosting"></a>

## Namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-multitenancygovernanceaspnetcoreservicecollectionextensions"></a>

### `MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions`

Registers the ASP.NET Core governance adapter services used by Cephalon multi-tenancy governance.

#### Declaration
```csharp
public static class MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-multitenancygovernanceaspnetcoreservicecollectionextensions-addcephalonmultitenancygovernanceaspnetcore-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions"></a>

##### `AddCephalonMultiTenancyGovernanceAspNetCore`

```csharp
IServiceCollection AddCephalonMultiTenancyGovernanceAspNetCore(this IServiceCollection services, IConfiguration configuration, Action<MultiTenancyGovernanceAspNetCoreOptions> configure)
```

Adds the Cephalon ASP.NET Core multi-tenancy governance adapter to the service collection.

Returns: The same service collection for fluent registration.

Parameters:
- `services`: The service collection to extend.
- `configuration`: The optional configuration root used to populate `MultiTenancyGovernanceAspNetCoreOptions` from `Engine:MultiTenancy:Governance:AspNetCore`.
- `configure`: An optional callback that can extend or override the configuration-driven ASP.NET Core governance adapter options.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-multitenancygovernanceaspnetcorewebapplicationbuilderextensions"></a>

### `MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions`

Registers the ASP.NET Core multi-tenancy governance adapter on a `WebApplicationBuilder`.

#### Declaration
```csharp
public static class MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-multitenancygovernanceaspnetcorewebapplicationbuilderextensions-addcephalonmultitenancygovernanceaspnetcore-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-cephalon-multitenancy-governance-aspnetcore-configuration-multitenancygovernanceaspnetcoreoptions"></a>

##### `AddCephalonMultiTenancyGovernanceAspNetCore`

```csharp
WebApplicationBuilder AddCephalonMultiTenancyGovernanceAspNetCore(this WebApplicationBuilder builder, Action<MultiTenancyGovernanceAspNetCoreOptions> configure)
```

Adds the Cephalon ASP.NET Core multi-tenancy governance adapter to the target application builder.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: An optional callback that can extend or override the configuration-driven ASP.NET Core governance adapter options.

<a id="type-cephalon-multitenancy-governance-aspnetcore-hosting-tenantdomainownershiphttpproofendpointroutebuilderextensions"></a>

### `TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions`

Maps ASP.NET Core endpoints for tenant-domain ownership HTTP proof files published by Cephalon governance.

#### Declaration
```csharp
public static class TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-governance-aspnetcore-hosting-tenantdomainownershiphttpproofendpointroutebuilderextensions-mapcephalontenantdomainownershiphttpproofs-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapCephalonTenantDomainOwnershipHttpProofs`

```csharp
IEndpointRouteBuilder MapCephalonTenantDomainOwnershipHttpProofs(this IEndpointRouteBuilder endpoints)
```

Maps the tenant-domain ownership HTTP proof publication endpoint.

Remarks: This endpoint is opt-in and reads proof-file state from `ITenantDomainOwnershipHttpProofPublicationCatalog`. The core governance package remains host-agnostic and only records the proof state that this adapter serves.

Returns: The same endpoint route builder for fluent routing composition.

Parameters:
- `endpoints`: The endpoint route builder to extend.
