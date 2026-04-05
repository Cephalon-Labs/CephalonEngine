using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Audit.Registration;
using Cephalon.Audit.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Identity.AspNetCore.Hosting;
using Cephalon.Identity.Registration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cephalon.Tests.Hosting;

public sealed class IdentityAspNetCoreAuditActorBridgeTests
{
    [Fact]
    public async Task AddCephalonIdentityAspNetCoreProjectsAuthenticatedPrincipalIntoAuditActors()
    {
        var app = await CreateAppAsync(
            configureRoutes: webApplication =>
            {
                webApplication.MapPost("/audit/entries", async (IAuditRecorder recorder) =>
                {
                    await recorder.RecordAsync(new AuditRecordRequest(
                        category: "documents",
                        action: "publish",
                        summary: "Published a document.",
                        subjectType: "document",
                        subjectId: "doc-001",
                        outcome: AuditOutcome.Succeeded));

                    return TypedResults.Accepted("/audit/entries");
                });
            },
            configureBuilder: builder =>
            {
                builder.Configuration[$"{EngineSettings.SectionName}:Audit:Enabled"] = "true";
            },
            configureEngine: engine =>
            {
                engine.AddModule(new AuditCaptureWriterModule());
                engine.AddAudit();
            });

        await using (app)
        {
            var client = app.GetTestClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/audit/entries");
            request.Headers.Add("X-Test-Subject", "user-002");
            request.Headers.Add("X-Test-Name", "Avery Example");
            request.Headers.Add("X-Test-Role", "member");
            request.Headers.Add("X-Test-Tenant", "tenant-001");
            request.Headers.Add("X-Test-Region", "apac");

            var response = await client.SendAsync(request);
            var captureWriter = app.Services.GetRequiredService<CaptureAuditWriter>();
            var entry = Assert.Single(captureWriter.Entries);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("user-002", entry.Actor.ActorId);
            Assert.Equal("Avery Example", entry.Actor.DisplayName);
            Assert.Equal("principal", entry.Actor.ActorType);
            Assert.Equal(TestAuthenticationDefaults.DefaultScheme, entry.Actor.Attributes["authenticationType"]);
            Assert.Equal("member", entry.Actor.Attributes["roles"]);
            Assert.Equal("tenant-001", entry.Actor.Attributes["tenantIds"]);
            Assert.Equal("apac", entry.Actor.Attributes["region"]);
        }
    }

    [Fact]
    public async Task AddCephalonIdentityAspNetCoreLeavesConsumerAuditActorAccessorsInControl()
    {
        var app = await CreateAppAsync(
            configureRoutes: webApplication =>
            {
                webApplication.MapPost("/audit/entries", async (IAuditRecorder recorder) =>
                {
                    await recorder.RecordAsync(new AuditRecordRequest(
                        category: "documents",
                        action: "publish",
                        summary: "Published a document.",
                        subjectType: "document",
                        subjectId: "doc-001",
                        outcome: AuditOutcome.Succeeded));

                    return TypedResults.Accepted("/audit/entries");
                });
            },
            configureBuilder: builder =>
            {
                builder.Configuration[$"{EngineSettings.SectionName}:Audit:Enabled"] = "true";
            },
            configureEngine: engine =>
            {
                engine.AddModule(new AuditCaptureModule());
                engine.AddAudit();
            });

        await using (app)
        {
            var client = app.GetTestClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/audit/entries");
            request.Headers.Add("X-Test-Subject", "user-002");
            request.Headers.Add("X-Test-Name", "Avery Example");

            var response = await client.SendAsync(request);
            var captureWriter = app.Services.GetRequiredService<CaptureAuditWriter>();
            var entry = Assert.Single(captureWriter.Entries);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("user-007", entry.Actor.ActorId);
            Assert.Equal("Avery", entry.Actor.DisplayName);
        }
    }

    [Fact]
    public async Task MapCephalonExposesIdentityAspNetCoreAuditActorBridgeState()
    {
        var app = await CreateAppAsync(
            configureRoutes: webApplication =>
            {
                webApplication.MapGet("/audit/status", () => TypedResults.Ok(new { ok = true }));
            },
            configureBuilder: builder =>
            {
                builder.Configuration[$"{EngineSettings.SectionName}:Audit:Enabled"] = "true";
            },
            configureEngine: engine =>
            {
                engine.AddModule(new AuditCaptureWriterModule());
                engine.AddAudit();
            });

        await using (app)
        {
            var client = app.GetTestClient();
            var identitySurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/identity-access");

            Assert.NotNull(identitySurfaces);

            var adapterSurface = Assert.Single(identitySurfaces, surface => surface.SurfaceId == "identity-aspnetcore");
            var adapterEntry = Assert.Single(adapterSurface.Entries);

            Assert.Equal("active", adapterEntry.Metadata["auditActorBridgeStatus"]);
            Assert.Equal("claims-principal", adapterEntry.Metadata["auditActorBridgeSource"]);
        }
    }

    private static async Task<WebApplication> CreateAppAsync(
        Action<WebApplication> configureRoutes,
        Action<WebApplicationBuilder>? configureBuilder,
        Action<EngineBuilder>? configureEngine)
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
            configureEngine?.Invoke(engine);
        });
        builder.AddCephalonIdentityAspNetCore();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationDefaults.DefaultScheme;
                options.DefaultChallengeScheme = TestAuthenticationDefaults.DefaultScheme;
                options.DefaultForbidScheme = TestAuthenticationDefaults.DefaultScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationDefaults.DefaultScheme, static _ => { });

        var app = builder.Build();
        app.UseAuthentication();
        app.MapCephalon();
        configureRoutes(app);
        await app.StartAsync();
        return app;
    }

    private static bool TryCreateTestPrincipal(
        IHeaderDictionary headers,
        string authenticationType,
        out ClaimsPrincipal principal)
    {
        principal = new ClaimsPrincipal(new ClaimsIdentity());
        if (!headers.TryGetValue("X-Test-Subject", out var subjectValues))
        {
            return false;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subjectValues.ToString())
        };

        if (headers.TryGetValue("X-Test-Name", out var nameValues))
        {
            claims.Add(new Claim(ClaimTypes.Name, nameValues.ToString()));
        }

        if (headers.TryGetValue("X-Test-Role", out var roleValues))
        {
            foreach (var role in roleValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        if (headers.TryGetValue("X-Test-Tenant", out var tenantValues))
        {
            foreach (var tenantId in tenantValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("tenant_id", tenantId));
            }
        }

        if (headers.TryGetValue("X-Test-Region", out var regionValues))
        {
            claims.Add(new Claim("region", regionValues.ToString()));
        }

        principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
        return true;
    }

    private static class TestAuthenticationDefaults
    {
        public const string DefaultScheme = "CephalonDefault";
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!TryCreateTestPrincipal(Request.Headers, Scheme.Name, out var principal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
