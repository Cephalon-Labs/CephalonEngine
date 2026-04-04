using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Identity.AspNetCore.Hosting;
using Cephalon.Identity.AspNetCore.Transports.Rest;
using Cephalon.Identity.Registration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Cephalon.Tests.Hosting;

public sealed class IdentityAspNetCoreHostingTests
{
    [Fact]
    public async Task RequireCephalonAuthorizationAllowsAuthenticatedTenantBoundaryRequests()
    {
        var app = await CreateAppAsync(configureRoutes: webApplication =>
        {
            var group = webApplication.MapGroup("/tenants/{tenantId}/documents")
                .RequireCephalonAuthorization("tenant-boundary", resourceType: "document");
            group.MapGet("/{id}/{classification}", () => TypedResults.Ok(new { ok = true }));
        });

        await using (app)
        {
            var client = app.GetTestClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/tenants/tenant-001/documents/doc-001/internal");
            request.Headers.Add("X-Test-Subject", "user-002");
            request.Headers.Add("X-Test-Role", "member");
            request.Headers.Add("X-Test-Tenant", "tenant-001");
            request.Headers.Add("X-Test-Region", "apac");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task RequireCephalonAuthorizationReturnsUnauthorizedProblemWhenPrincipalIsMissing()
    {
        var app = await CreateAppAsync(configureRoutes: webApplication =>
        {
            webApplication.MapGet("/tenants/{tenantId}/documents/{id}/{classification}", () => TypedResults.Ok())
                .RequireCephalonAuthorization("tenant-boundary", resourceType: "document");
        });

        await using (app)
        {
            var client = app.GetTestClient();
            var response = await client.GetAsync("/tenants/tenant-001/documents/doc-001/internal");
            var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotNull(payload);
            Assert.Equal("Authentication required", payload.Title);
            Assert.Contains("authenticated user", payload.Detail!, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task RequireCephalonAuthorizationReturnsForbiddenProblemWhenPolicyIsDenied()
    {
        var app = await CreateAppAsync(configureRoutes: webApplication =>
        {
            webApplication.MapGet("/tenants/{tenantId}/documents/{id}/{classification}", () => TypedResults.Ok())
                .RequireCephalonAuthorization("tenant-boundary", resourceType: "document");
        });

        await using (app)
        {
            var client = app.GetTestClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/tenants/tenant-001/documents/doc-001/internal");
            request.Headers.Add("X-Test-Subject", "user-004");
            request.Headers.Add("X-Test-Role", "member");
            request.Headers.Add("X-Test-Tenant", "tenant-999");
            request.Headers.Add("X-Test-Region", "apac");

            var response = await client.SendAsync(request);
            var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.NotNull(payload);
            Assert.Equal("Authorization denied", payload.Title);
            Assert.Contains("tenant boundary", payload.Detail!, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task RequireCephalonAuthorizationRespectsAllowAnonymousOnChildEndpointsInsideProtectedGroups()
    {
        var app = await CreateAppAsync(configureRoutes: webApplication =>
        {
            var group = webApplication.MapGroup("/tenants/{tenantId}/documents")
                .RequireCephalonAuthorization("tenant-boundary", resourceType: "document");

            group.MapGet("/public", () => TypedResults.Ok(new { ok = true }))
                .WithMetadata(new AllowAnonymousAttribute());
            group.MapGet("/{id}/{classification}", () => TypedResults.Ok(new { ok = true }));
        });

        await using (app)
        {
            var client = app.GetTestClient();

            var publicResponse = await client.GetAsync("/tenants/tenant-001/documents/public");
            Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);

            var privateResponse = await client.GetAsync("/tenants/tenant-001/documents/doc-001/internal");
            Assert.Equal(HttpStatusCode.Unauthorized, privateResponse.StatusCode);
        }
    }

    [Fact]
    public async Task RequireCephalonAuthorizationMapsPatchRequestsAndOwnerRouteValuesForOwnerPolicies()
    {
        var app = await CreateAppAsync(configureRoutes: webApplication =>
        {
            webApplication.MapMethods(
                    "/tenants/{tenantId}/documents/{id}/owners/{ownerSubjectId}",
                    ["PATCH"],
                    () => TypedResults.Ok(new { ok = true }))
                .RequireCephalonAuthorization("document-owner", resourceType: "document");
        });

        await using (app)
        {
            var client = app.GetTestClient();
            var request = new HttpRequestMessage(HttpMethod.Patch, "/tenants/tenant-001/documents/doc-002/owners/user-003");
            request.Headers.Add("X-Test-Subject", "user-003");
            request.Headers.Add("X-Test-Tenant", "tenant-001");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task RequireCephalonAuthorizationUsesDefaultAuthenticationChallengeAndForbidSchemesWhenAvailable()
    {
        var app = await CreateAppAsync(
            configureRoutes: webApplication =>
            {
                webApplication.MapGet("/tenants/{tenantId}/documents/{id}/{classification}", () => TypedResults.Ok())
                    .RequireCephalonAuthorization("tenant-boundary", resourceType: "document");
            },
            useAuthenticationSchemes: true);

        await using (app)
        {
            var client = app.GetTestClient();

            var unauthorizedResponse = await client.GetAsync("/tenants/tenant-001/documents/doc-001/internal");
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
            Assert.Equal(TestAuthenticationDefaults.DefaultScheme, unauthorizedResponse.Headers.GetValues(TestAuthenticationDefaults.ChallengeHeaderName).Single());

            var forbiddenRequest = new HttpRequestMessage(HttpMethod.Get, "/tenants/tenant-001/documents/doc-001/internal");
            forbiddenRequest.Headers.Add("X-Test-Subject", "user-004");
            forbiddenRequest.Headers.Add("X-Test-Role", "member");
            forbiddenRequest.Headers.Add("X-Test-Tenant", "tenant-999");
            forbiddenRequest.Headers.Add("X-Test-Region", "apac");

            var forbiddenResponse = await client.SendAsync(forbiddenRequest);
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
            Assert.Equal(TestAuthenticationDefaults.DefaultScheme, forbiddenResponse.Headers.GetValues(TestAuthenticationDefaults.ForbidHeaderName).Single());
        }
    }

    [Fact]
    public async Task RequireCephalonAuthorizationPrefersExplicitAuthorizeMetadataSchemesForChallengeResponses()
    {
        var app = await CreateAppAsync(
            configureRoutes: webApplication =>
            {
                webApplication.MapGet("/tenants/{tenantId}/documents/{id}", () => TypedResults.Ok())
                    .WithCephalonAuthenticationSchemes(TestAuthenticationDefaults.OverrideScheme)
                    .RequireCephalonAuthorization("tenant-boundary", resourceType: "document");
            },
            useAuthenticationSchemes: true);

        await using (app)
        {
            var client = app.GetTestClient();
            var response = await client.GetAsync("/tenants/tenant-001/documents/doc-001");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(TestAuthenticationDefaults.OverrideScheme, response.Headers.GetValues(TestAuthenticationDefaults.ChallengeHeaderName).Single());
        }
    }

    [Fact]
    public async Task RequireCephalonAuthorizationReturnsDeterministicForbiddenProblemWhenTheBuiltInEvaluatorIsDisabled()
    {
        var app = await CreateAppAsync(
            configureRoutes: webApplication =>
            {
                webApplication.MapGet("/tenants/{tenantId}/documents/{id}/{classification}", () => TypedResults.Ok())
                    .RequireCephalonAuthorization("tenant-boundary", resourceType: "document");
            },
            useAuthenticationSchemes: false,
            configureBuilder: builder =>
            {
                builder.Configuration[$"{EngineSettings.SectionName}:Identity:EnableDefaultEvaluator"] = "false";
            });

        await using (app)
        {
            var client = app.GetTestClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/tenants/tenant-001/documents/doc-001/internal");
            request.Headers.Add("X-Test-Subject", "user-002");
            request.Headers.Add("X-Test-Role", "member");
            request.Headers.Add("X-Test-Tenant", "tenant-001");
            request.Headers.Add("X-Test-Region", "apac");

            var response = await client.SendAsync(request);
            var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.NotNull(payload);
            Assert.Equal("Authorization denied", payload.Title);
            Assert.Contains("disabled", payload.Detail!, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<WebApplication> CreateAppAsync(Action<WebApplication> configureRoutes)
    {
        return await CreateAppAsync(configureRoutes, useAuthenticationSchemes: false, configureBuilder: null);
    }

    private static async Task<WebApplication> CreateAppAsync(Action<WebApplication> configureRoutes, bool useAuthenticationSchemes)
    {
        return await CreateAppAsync(configureRoutes, useAuthenticationSchemes, configureBuilder: null);
    }

    private static async Task<WebApplication> CreateAppAsync(
        Action<WebApplication> configureRoutes,
        bool useAuthenticationSchemes,
        Action<WebApplicationBuilder>? configureBuilder)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "IdentityAccess";
        builder.Configuration[$"{EngineSettings.SectionName}:Identity:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Identity:AuthorizationModes:0"] = "Policy";
        builder.Configuration[$"{EngineSettings.SectionName}:Identity:AuthorizationModes:1"] = "RBAC";
        builder.Configuration[$"{EngineSettings.SectionName}:Identity:AuthorizationModes:2"] = "ABAC";
        configureBuilder?.Invoke(builder);
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new IdentityAuthorizationTestModule());
            engine.AddIdentityAccess();
        });
        builder.AddCephalonIdentityAspNetCore();
        if (useAuthenticationSchemes)
        {
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationDefaults.DefaultScheme;
                    options.DefaultChallengeScheme = TestAuthenticationDefaults.DefaultScheme;
                    options.DefaultForbidScheme = TestAuthenticationDefaults.DefaultScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationDefaults.DefaultScheme, static _ => { })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationDefaults.OverrideScheme, static _ => { });
        }

        var app = builder.Build();
        if (useAuthenticationSchemes)
        {
            app.UseAuthentication();
        }
        else
        {
            app.Use(async (context, next) =>
            {
                if (TryCreateTestPrincipal(context, out var principal))
                {
                    context.User = principal;
                }

                await next();
            });
        }

        app.MapCephalon();
        configureRoutes(app);
        await app.StartAsync();
        return app;
    }

    private static bool TryCreateTestPrincipal(HttpContext httpContext, out ClaimsPrincipal principal)
    {
        principal = new ClaimsPrincipal(new ClaimsIdentity());
        if (!httpContext.Request.Headers.TryGetValue("X-Test-Subject", out var subjectValues))
        {
            return false;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subjectValues.ToString())
        };

        if (httpContext.Request.Headers.TryGetValue("X-Test-Role", out var roleValues))
        {
            foreach (var role in roleValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        if (httpContext.Request.Headers.TryGetValue("X-Test-Tenant", out var tenantValues))
        {
            foreach (var tenantId in tenantValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("tenant_id", tenantId));
            }
        }

        if (httpContext.Request.Headers.TryGetValue("X-Test-Region", out var regionValues))
        {
            claims.Add(new Claim("region", regionValues.ToString()));
        }

        principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
        return true;
    }

    private static class TestAuthenticationDefaults
    {
        public const string DefaultScheme = "CephalonDefault";
        public const string OverrideScheme = "CephalonOverride";
        public const string ChallengeHeaderName = "X-Test-Challenge";
        public const string ForbidHeaderName = "X-Test-Forbid";
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Subject", out var subjectValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, subjectValues.ToString())
            };

            if (Request.Headers.TryGetValue("X-Test-Role", out var roleValues))
            {
                foreach (var role in roleValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            if (Request.Headers.TryGetValue("X-Test-Tenant", out var tenantValues))
            {
                foreach (var tenantId in tenantValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    claims.Add(new Claim("tenant_id", tenantId));
                }
            }

            if (Request.Headers.TryGetValue("X-Test-Region", out var regionValues))
            {
                claims.Add(new Claim("region", regionValues.ToString()));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers[TestAuthenticationDefaults.ChallengeHeaderName] = Scheme.Name;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.Headers[TestAuthenticationDefaults.ForbidHeaderName] = Scheme.Name;
            return Task.CompletedTask;
        }
    }
}
