# Cephalon.Identity.AspNetCore

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Identity.AspNetCore)
## Namespaces

- `Cephalon.Identity.AspNetCore.Configuration`
- `Cephalon.Identity.AspNetCore.Hosting`
- `Cephalon.Identity.AspNetCore.Transports.Rest`

<a id="namespace-cephalon-identity-aspnetcore-configuration"></a>

## Namespace Cephalon.Identity.AspNetCore.Configuration

<a id="type-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions"></a>

### `IdentityAspNetCoreOptions`

Describes ASP.NET Core-specific identity and authorization adapter options for Cephalon.

#### Declaration
```csharp
public sealed class IdentityAspNetCoreOptions
```

#### Constructors

<a id="member-m-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-ctor"></a>

##### `IdentityAspNetCoreOptions`

```csharp
IdentityAspNetCoreOptions()
```

Initializes a new instance of the `IdentityAspNetCoreOptions` class.

#### Properties

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-allowidentitynameassubjectidfallback"></a>

##### `AllowIdentityNameAsSubjectIdFallback`

```csharp
bool AllowIdentityNameAsSubjectIdFallback { get; set; }
```

Gets or sets a value indicating whether `Name` can be used as the subject id fallback when none of the configured claim types are present.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-authorizationdecisionitemkey"></a>

##### `AuthorizationDecisionItemKey`

```csharp
string AuthorizationDecisionItemKey { get; set; }
```

Gets or sets the `Items` key that stores the most recent Cephalon authorization decision for the current request.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-displaynameclaimtypes"></a>

##### `DisplayNameClaimTypes`

```csharp
List<string> DisplayNameClaimTypes { get; }
```

Gets the claim types that can provide the human-readable display name for the current subject.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-includeallclaimsassubjectattributes"></a>

##### `IncludeAllClaimsAsSubjectAttributes`

```csharp
bool IncludeAllClaimsAsSubjectAttributes { get; set; }
```

Gets or sets a value indicating whether unmatched claims should be projected into `Attributes`.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-includeheadersascontextattributes"></a>

##### `IncludeHeadersAsContextAttributes`

```csharp
bool IncludeHeadersAsContextAttributes { get; set; }
```

Gets or sets a value indicating whether request headers should be projected into `Attributes`.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-includequerystringascontextattributes"></a>

##### `IncludeQueryStringAsContextAttributes`

```csharp
bool IncludeQueryStringAsContextAttributes { get; set; }
```

Gets or sets a value indicating whether query-string values should be projected into `Attributes`.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-includeroutevaluesasresourceattributes"></a>

##### `IncludeRouteValuesAsResourceAttributes`

```csharp
bool IncludeRouteValuesAsResourceAttributes { get; set; }
```

Gets or sets a value indicating whether route values should be projected into `Attributes`.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-ownersubjectidroutekeys"></a>

##### `OwnerSubjectIdRouteKeys`

```csharp
List<string> OwnerSubjectIdRouteKeys { get; }
```

Gets the route-value keys that can provide the owning subject identifier for the current resource.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-resourceidroutekeys"></a>

##### `ResourceIdRouteKeys`

```csharp
List<string> ResourceIdRouteKeys { get; }
```

Gets the route-value keys that can provide the current resource identifier.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-roleclaimtypes"></a>

##### `RoleClaimTypes`

```csharp
List<string> RoleClaimTypes { get; }
```

Gets the claim types that can provide role memberships for the current subject.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-subjectidclaimtypes"></a>

##### `SubjectIdClaimTypes`

```csharp
List<string> SubjectIdClaimTypes { get; }
```

Gets the claim types that can provide the stable Cephalon authorization subject identifier.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-tenantclaimtypes"></a>

##### `TenantClaimTypes`

```csharp
List<string> TenantClaimTypes { get; }
```

Gets the claim types that can provide tenant memberships for the current subject.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-tenantheadernames"></a>

##### `TenantHeaderNames`

```csharp
List<string> TenantHeaderNames { get; }
```

Gets the request-header names that can provide the current tenant identifier when route values do not.

<a id="member-p-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-tenantroutekeys"></a>

##### `TenantRouteKeys`

```csharp
List<string> TenantRouteKeys { get; }
```

Gets the route-value keys that can provide the current tenant identifier.

#### Methods

<a id="member-m-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
IdentityAspNetCoreOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads ASP.NET Core identity adapter options from configuration.

Returns: The parsed ASP.NET Core identity adapter options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The engine root section path to read from.

<a id="namespace-cephalon-identity-aspnetcore-hosting"></a>

## Namespace Cephalon.Identity.AspNetCore.Hosting

<a id="type-cephalon-identity-aspnetcore-hosting-identityaspnetcoreservicecollectionextensions"></a>

### `IdentityAspNetCoreServiceCollectionExtensions`

Registers the ASP.NET Core identity adapter services used by Cephalon.

#### Declaration
```csharp
public static class IdentityAspNetCoreServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-identity-aspnetcore-hosting-identityaspnetcoreservicecollectionextensions-addcephalonidentityaspnetcore-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions"></a>

##### `AddCephalonIdentityAspNetCore`

```csharp
IServiceCollection AddCephalonIdentityAspNetCore(this IServiceCollection services, IConfiguration configuration, Action<IdentityAspNetCoreOptions> configure)
```

Adds the Cephalon ASP.NET Core identity adapter to the service collection.

Returns: The same service collection for fluent registration.

Parameters:
- `services`: The service collection to extend.
- `configuration`: The optional configuration root used to populate `IdentityAspNetCoreOptions` from `Engine:Identity:AspNetCore`.
- `configure`: An optional callback that can extend or override the configuration-driven ASP.NET Core identity adapter options.

<a id="type-cephalon-identity-aspnetcore-hosting-identityaspnetcorewebapplicationbuilderextensions"></a>

### `IdentityAspNetCoreWebApplicationBuilderExtensions`

Registers the ASP.NET Core identity adapter on a `WebApplicationBuilder`.

#### Declaration
```csharp
public static class IdentityAspNetCoreWebApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-identity-aspnetcore-hosting-identityaspnetcorewebapplicationbuilderextensions-addcephalonidentityaspnetcore-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-cephalon-identity-aspnetcore-configuration-identityaspnetcoreoptions"></a>

##### `AddCephalonIdentityAspNetCore`

```csharp
WebApplicationBuilder AddCephalonIdentityAspNetCore(this WebApplicationBuilder builder, Action<IdentityAspNetCoreOptions> configure)
```

Adds the Cephalon ASP.NET Core identity adapter to the target application builder.

Remarks: This keeps ASP.NET Core-specific principal, claim, and route-bound authorization mapping in the host layer while still feeding the host-agnostic Cephalon authorization contracts.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: An optional callback that can extend or override the configuration-driven ASP.NET Core identity adapter options.

<a id="namespace-cephalon-identity-aspnetcore-transports-rest"></a>

## Namespace Cephalon.Identity.AspNetCore.Transports.Rest

<a id="type-cephalon-identity-aspnetcore-transports-rest-identityendpointconventionbuilderextensions"></a>

### `IdentityEndpointConventionBuilderExtensions`

Adds Cephalon-specific authorization conventions to REST route handlers and groups.

#### Declaration
```csharp
public static class IdentityEndpointConventionBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-identity-aspnetcore-transports-rest-identityendpointconventionbuilderextensions-requirecephalonauthorization-microsoft-aspnetcore-builder-routehandlerbuilder-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `RequireCephalonAuthorization`

```csharp
RouteHandlerBuilder RequireCephalonAuthorization(this RouteHandlerBuilder builder, string policyId, string action, string resourceType, string resourceIdRouteKey, string tenantRouteKey, string ownerSubjectIdRouteKey)
```

Requires a Cephalon authorization decision before a REST route handler can execute.

Remarks: This helper keeps ASP.NET Core principal and route parsing in the host layer while still evaluating the shared Cephalon authorization contracts through `IAuthorizationEvaluator`.

Returns: The same route handler builder for fluent convention chaining.

Parameters:
- `builder`: The route handler builder to protect.
- `policyId`: The Cephalon authorization policy id that must allow the request.
- `action`: The optional action to evaluate. When omitted, the adapter maps the HTTP method to a conventional action such as `read`, `create`, `update`, or `delete`.
- `resourceType`: The optional logical resource type. When omitted, the adapter derives it from the final literal segment in the endpoint route pattern.
- `resourceIdRouteKey`: The optional route-value key that provides the resource identifier. When omitted, the adapter falls back to the configured resource-id route keys.
- `tenantRouteKey`: The optional route-value key that provides the tenant identifier. When omitted, the adapter falls back to the configured tenant route keys and tenant headers.
- `ownerSubjectIdRouteKey`: The optional route-value key that provides the owning subject identifier for owner-based policies.

<a id="member-m-cephalon-identity-aspnetcore-transports-rest-identityendpointconventionbuilderextensions-requirecephalonauthorization-microsoft-aspnetcore-routing-routegroupbuilder-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `RequireCephalonAuthorization`

```csharp
RouteGroupBuilder RequireCephalonAuthorization(this RouteGroupBuilder builder, string policyId, string action, string resourceType, string resourceIdRouteKey, string tenantRouteKey, string ownerSubjectIdRouteKey)
```

Requires a Cephalon authorization decision before every REST route handler in the route group can execute.

Returns: The same route group builder for fluent convention chaining.

Parameters:
- `builder`: The route group builder to protect.
- `policyId`: The Cephalon authorization policy id that must allow the request.
- `action`: The optional action to evaluate. When omitted, the adapter maps the HTTP method to a conventional action such as `read`, `create`, `update`, or `delete`.
- `resourceType`: The optional logical resource type. When omitted, the adapter derives it from the final literal segment in the endpoint route pattern.
- `resourceIdRouteKey`: The optional route-value key that provides the resource identifier. When omitted, the adapter falls back to the configured resource-id route keys.
- `tenantRouteKey`: The optional route-value key that provides the tenant identifier. When omitted, the adapter falls back to the configured tenant route keys and tenant headers.
- `ownerSubjectIdRouteKey`: The optional route-value key that provides the owning subject identifier for owner-based policies.

<a id="member-m-cephalon-identity-aspnetcore-transports-rest-identityendpointconventionbuilderextensions-withcephalonauthenticationschemes-1-0-system-string"></a>

##### `WithCephalonAuthenticationSchemes`

```csharp
TBuilder WithCephalonAuthenticationSchemes<TBuilder>(this TBuilder builder, string[] authenticationSchemes)
```

Declares the ASP.NET Core authentication schemes that should own challenge and forbid responses for an endpoint or route group.

Returns: The same builder for fluent chaining.

Type parameters:
- `TBuilder`: The endpoint convention builder type.

Parameters:
- `builder`: The endpoint or route-group builder to annotate.
- `authenticationSchemes`: The authentication scheme names to use for boundary responses.
