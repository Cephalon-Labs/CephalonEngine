using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Identity.AspNetCore.Hosting;
using Cephalon.Identity.AspNetCore.Transports.Rest;
using Cephalon.Identity.Registration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;

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

    private static async Task<WebApplication> CreateAppAsync(Action<WebApplication> configureRoutes)
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
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new IdentityAuthorizationTestModule());
            engine.AddIdentityAccess();
        });
        builder.AddCephalonIdentityAspNetCore();

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (TryCreateTestPrincipal(context, out var principal))
            {
                context.User = principal;
            }

            await next();
        });

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
}
