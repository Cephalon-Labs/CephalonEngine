# Cephalon.Identity.AspNetCore

`Cephalon.Identity.AspNetCore` is the ASP.NET Core host adapter for Cephalon identity and authorization.

## What it owns

- maps `ClaimsPrincipal`, route values, and request metadata into the shared Cephalon authorization contracts
- keeps ASP.NET Core-specific claim parsing, route parsing, and endpoint-filter behavior out of `Cephalon.Abstractions`
- provides `AddCephalonIdentityAspNetCore(...)` host wiring for `IServiceCollection` and `WebApplicationBuilder`
- provides low-ceremony REST helpers, `RequireCephalonAuthorization(...)` and `WithCephalonAuthenticationSchemes(...)`, for minimal API endpoints and route groups
- returns truthful `401` and `403` API responses without pretending to own consumer authentication schemes, and now defers to ASP.NET Core challenge/forbid flows when the host or endpoint metadata already declares authentication schemes
- respects ASP.NET Core `AllowAnonymous` endpoint metadata so public child endpoints can still opt out cleanly inside protected route groups
- stays deterministic even when the built-in `Cephalon.Identity` evaluator is disabled, so protected endpoints deny cleanly instead of drifting into missing-service failures while a consumer is wiring a custom evaluator

## Main surfaces

- `Configuration/IdentityAspNetCoreOptions.cs`
- `Hosting/IdentityAspNetCoreServiceCollectionExtensions.cs`
- `Hosting/IdentityAspNetCoreWebApplicationBuilderExtensions.cs`
- `Transports/Rest/IdentityEndpointConventionBuilderExtensions.cs`

## How it fits

This package is the host-specific follow-through for the `Cephalon.Identity` baseline. The host-agnostic package already owns the authorization evaluator, policy metadata conventions, diagnostics ids, and the `identity-authorization` runtime surface. `Cephalon.Identity.AspNetCore` does the thin host work on top of that: it reads the current ASP.NET Core principal, extracts route and request context, and turns those into `AuthorizationSubject`, `AuthorizationResource`, and `AuthorizationContext` before asking the shared `IAuthorizationEvaluator` for the final answer.

That split matters because Cephalon wants consumer apps to stay low ceremony without hardwiring one host model into the engine core. A team can configure its own authentication schemes, tokens, cookies, or external identity provider however it wants, then let this adapter translate the current request into the shared Cephalon authorization contract at the HTTP boundary.

The first slice is intentionally REST-only and minimal-API friendly. It does not replace ASP.NET Core authentication, it does not infer product-specific business resources from the database, and it does not introduce new `/engine/*` endpoints. Instead, it focuses on one honest job: reduce repeated authorization plumbing at the endpoint layer while preserving the existing host-agnostic Cephalon policy story.

## Related docs

- [Cephalon.Identity](identity.md)
- [Cephalon.AspNetCore](aspnetcore.md)
- [Cephalon.Abstractions](abstractions.md)
- [Operations](../operations.md)
