using Cephalon.AspNetCore.Hosting;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Configuration;
using Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Cephalon.Tests.Hosting;

public sealed class MultiTenancyGovernanceAspNetCoreHostingTests
{
    [Fact]
    public async Task MapCephalonTenantAdministrationCommandsAppliesWorkflowAndReportsMappedRuntimeSurface()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance();
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
        {
            options.RequireTenantAdministrationAuthorization = false;
        });

        await using var app = builder.Build();
        app.MapCephalonTenantAdministrationCommands();

        await app.StartAsync();
        var client = app.GetTestClient();
        var request = new TenantAdministrationWorkflowRequest(
            command: TenantAdministrationWorkflowCommands.GrantMembership,
            tenantId: "tenant-admin-http",
            principalId: "user-admin-http",
            displayName: "HTTP Admin",
            roles: ["owner", "member"],
            actor: "tenant-owner",
            reason: "Bootstrap tenant owner",
            atUtc: new DateTimeOffset(2026, 04, 29, 12, 0, 0, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["source"] = "hosting-test"
            });

        var response = await client.PostAsJsonAsync("/engine/tenant-administration/commands", request);
        var result = await response.Content.ReadFromJsonAsync<TenantAdministrationWorkflowResult>();
        var membership = Assert.Single(app.Services.GetRequiredService<ITenantMembershipCatalog>().Memberships);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-administration-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Applied);
        Assert.Equal(TenantAdministrationWorkflowOutcomes.Applied, result.Outcome);
        Assert.Equal("tenant-admin-http", membership.TenantId);
        Assert.Equal("user-admin-http", membership.PrincipalId);
        Assert.Equal(TenantMembershipStatuses.Active, membership.Status);
        Assert.Equal("tenant-owner", membership.Metadata[TenantAdministrationWorkflowMetadataKeys.LastAdministrationActor]);
        Assert.Equal("mapped", endpointEntry.Metadata["runtimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["endpointMapped"]);
        Assert.Equal("false", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("/engine/tenant-administration/commands", endpointEntry.Metadata["routePattern"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["tenantAdminEndpointOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["tenantAdminUiOwnership"]);
    }

    [Fact]
    public async Task MapCephalonTenantAdministrationCommandsDeniesAnonymousByDefault()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance();
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore();

        await using var app = builder.Build();
        app.MapCephalonTenantAdministrationCommands();

        await app.StartAsync();
        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/tenant-administration/commands",
            new TenantAdministrationWorkflowRequest(
                command: TenantAdministrationWorkflowCommands.GrantMembership,
                tenantId: "tenant-admin-http",
                principalId: "anonymous-user"));
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-administration-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(app.Services.GetRequiredService<ITenantMembershipCatalog>().Memberships);
        Assert.Equal("mapped", endpointEntry.Metadata["runtimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["endpointMapped"]);
        Assert.Equal("true", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("none", endpointEntry.Metadata["authorizationPolicy"]);
    }

    [Fact]
    public async Task MapCephalonTenantInvitationDeliveryStatusCallbacksReconcilesStatusAndReportsMappedRuntimeSurface()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-callback",
                    tenantId: "tenant-callback",
                    inviteeId: "user-callback",
                    displayName: "Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "provider-message-callback"
                    }));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
        {
            options.RequireTenantInvitationDeliveryStatusCallbackAuthorization = false;
        });

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var observedAtUtc = new DateTimeOffset(2026, 04, 29, 13, 0, 0, TimeSpan.Zero);
        var request = new TenantInvitationDeliveryStatusCallbackRequest
        {
            TenantId = "tenant-callback",
            InvitationId = "invite-callback",
            Status = TenantInvitationDeliveryStatuses.Delivered,
            ProviderMessageId = "provider-message-callback",
            SenderId = "http-webhook",
            Channel = "email",
            Reason = "Receiver accepted the invitation.",
            ObservedAtUtc = observedAtUtc,
            Actor = "notification-provider",
            CorrelationId = "delivery-callback-001",
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["providerStatusCode"] = "250"
            }
        };

        var response = await client.PostAsJsonAsync("/engine/tenant-invitations/delivery-status", request);
        var result = await response.Content.ReadFromJsonAsync<TenantInvitationDeliveryStatusReconciliationResult>();
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-status-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Reconciled);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled, result.Outcome);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, result.Status);
        Assert.Equal("provider-message-callback", result.ProviderMessageId);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal(observedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture), invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusObservedAtUtc]);
        Assert.Equal("aspnetcore-delivery-status-callback", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusSource]);
        Assert.Equal("notification-provider", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusActor]);
        Assert.Equal("delivery-callback-001", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusCorrelationId]);
        Assert.Equal("250", invitation.Metadata["providerStatusCode"]);
        Assert.Equal("true", invitation.Metadata["aspNetCoreDeliveryStatusCallback"]);
        Assert.Equal("/engine/tenant-invitations/delivery-status", invitation.Metadata["aspNetCoreDeliveryStatusCallbackRoute"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["deliveryStatusCallbackIngressOwnership"]);
        Assert.Equal("mapped", endpointEntry.Metadata["runtimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["endpointMapped"]);
        Assert.Equal("false", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("true", endpointEntry.Metadata["requireProviderMessageMatch"]);
        Assert.Equal("/engine/tenant-invitations/delivery-status", endpointEntry.Metadata["routePattern"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["tenantInvitationDeliveryStatusCallbackEndpointOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["providerSpecificCallbackTranslationOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["providerPollingOwnership"]);
    }

    [Fact]
    public async Task MapCephalonTenantInvitationDeliveryStatusCallbacksDeniesAnonymousByDefault()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-callback",
                    tenantId: "tenant-callback",
                    inviteeId: "user-callback",
                    displayName: "Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore();

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/tenant-invitations/delivery-status",
            new TenantInvitationDeliveryStatusCallbackRequest
            {
                TenantId = "tenant-callback",
                InvitationId = "invite-callback",
                Status = TenantInvitationDeliveryStatuses.Delivered
            });
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-status-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus));
        Assert.Equal("mapped", endpointEntry.Metadata["runtimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["endpointMapped"]);
        Assert.Equal("true", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("none", endpointEntry.Metadata["authorizationPolicy"]);
    }

    [Fact]
    public async Task MapCephalonTenantDomainOwnershipHttpProofsServesPublishedProofsByHostAndPath()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance();
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore();

        await using var app = builder.Build();
        app.MapCephalonTenantDomainOwnershipHttpProofs();

        var issuer = app.Services.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var publisher = app.Services.GetRequiredService<ITenantDomainOwnershipHttpProofPublisher>();
        var challenge = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "Proof.Example.",
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            challengeValue: "published-http-proof",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 0, 0, TimeSpan.Zero)));
        var publication = await publisher.PublishAsync(new TenantDomainOwnershipHttpProofPublicationRequest(
            tenantId: "tenant-001",
            domainName: "proof.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 5, 0, TimeSpan.Zero)));

        await app.StartAsync();
        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, publication.HttpFilePath!);
        request.Headers.Host = "proof.example";
        using var missingHostRequest = new HttpRequestMessage(HttpMethod.Get, publication.HttpFilePath!);
        missingHostRequest.Headers.Host = "other.example";

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        var missingHostResponse = await client.SendAsync(missingHostRequest);

        Assert.True(challenge.Issued);
        Assert.True(publication.Published);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("published-http-proof", payload);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(HttpStatusCode.NotFound, missingHostResponse.StatusCode);
    }
}
