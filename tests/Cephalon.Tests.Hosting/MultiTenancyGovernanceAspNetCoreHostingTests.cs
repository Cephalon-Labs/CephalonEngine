using Cephalon.AspNetCore.Hosting;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Configuration;
using Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cephalon.Tests.Hosting;

public sealed class MultiTenancyGovernanceAspNetCoreHostingTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

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
    public async Task MapCephalonTenantInvitationDeliveryDispatchesDispatchesInvitationAndReportsMappedRuntimeSurface()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        var sender = new RecordingTenantInvitationDeliverySender("test-email");
        builder.Services.AddSingleton<ITenantInvitationDeliverySender>(sender);
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-dispatch-http",
                    tenantId: "tenant-dispatch-http",
                    inviteeId: "user-dispatch-http",
                    displayName: "Dispatch Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
        {
            options.RequireTenantInvitationDeliveryDispatchAuthorization = false;
        });

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryDispatches();

        await app.StartAsync();
        var client = app.GetTestClient();
        var dispatchedAtUtc = new DateTimeOffset(2026, 04, 29, 15, 0, 0, TimeSpan.Zero);
        var request = new TenantInvitationDeliveryRequest(
            tenantId: "tenant-dispatch-http",
            invitationId: "invite-dispatch-http",
            channel: "email",
            senderId: "test-email",
            actor: "tenant-operator",
            atUtc: dispatchedAtUtc,
            correlationId: "delivery-dispatch-http-001",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["dispatchReason"] = "welcome"
            });

        var response = await client.PostAsJsonAsync("/engine/tenant-invitations/delivery-dispatches", request);
        var result = await response.Content.ReadFromJsonAsync<TenantInvitationDeliveryResult>();
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var run = Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryRunCatalog>().Runs);
        var context = Assert.Single(sender.Contexts);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal("provider-message-dispatch-http", result.ProviderMessageId);
        Assert.Equal("tenant-dispatch-http", context.TenantId);
        Assert.Equal("invite-dispatch-http", context.InvitationId);
        Assert.Equal("email", context.Channel);
        Assert.Equal("test-email", context.RequestedSenderId);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.Equal("provider-message-dispatch-http", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId]);
        Assert.Equal("aspnetcore-invitation-delivery-dispatch", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliverySource]);
        Assert.Equal("tenant-operator", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryActor]);
        Assert.Equal("delivery-dispatch-http-001", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryCorrelationId]);
        Assert.Equal("true", invitation.Metadata["aspNetCoreInvitationDeliveryDispatch"]);
        Assert.Equal("/engine/tenant-invitations/delivery-dispatches", invitation.Metadata["aspNetCoreInvitationDeliveryDispatchRoute"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["invitationDeliveryDispatchEndpointOwnership"]);
        Assert.Equal("welcome", invitation.Metadata["dispatchReason"]);
        Assert.Equal("accepted", invitation.Metadata["senderMetadata"]);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, run.Outcome);
        Assert.Equal("provider-message-dispatch-http", run.ProviderMessageId);
        Assert.Equal("mapped", endpointEntry.Metadata["runtimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["endpointMapped"]);
        Assert.Equal("false", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("/engine/tenant-invitations/delivery-dispatches", endpointEntry.Metadata["routePattern"]);
        Assert.Equal("POST", endpointEntry.Metadata["httpMethod"]);
        Assert.Equal("TenantInvitationDeliveryRequest", endpointEntry.Metadata["requestBodyContract"]);
        Assert.Equal("TenantInvitationDeliveryResult", endpointEntry.Metadata["responseBodyContract"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["tenantInvitationDeliveryDispatchEndpointOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["providerSpecificSenderOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["durableRetryQueueOwnership"]);
    }

    [Fact]
    public async Task MapCephalonTenantInvitationDeliveryDispatchesDeniesAnonymousByDefault()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        var sender = new RecordingTenantInvitationDeliverySender("test-email");
        builder.Services.AddSingleton<ITenantInvitationDeliverySender>(sender);
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-dispatch-http",
                    tenantId: "tenant-dispatch-http",
                    inviteeId: "user-dispatch-http",
                    displayName: "Dispatch Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore();

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryDispatches();

        await app.StartAsync();
        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/tenant-invitations/delivery-dispatches",
            new TenantInvitationDeliveryRequest(
                tenantId: "tenant-dispatch-http",
                invitationId: "invite-dispatch-http",
                channel: "email",
                senderId: "test-email"));
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(sender.Contexts);
        Assert.Empty(app.Services.GetRequiredService<ITenantInvitationDeliveryRunCatalog>().Runs);
        Assert.False(invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome));
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
        var observation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
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
        Assert.Equal(invitation.Metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId], observation.ObservationId);
        Assert.Equal(TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled, observation.Outcome);
        Assert.Equal("aspnetcore-delivery-status-callback", observation.Source);
        Assert.Equal("mapped", endpointEntry.Metadata["runtimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["endpointMapped"]);
        Assert.Equal("false", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("true", endpointEntry.Metadata["requireProviderMessageMatch"]);
        Assert.Equal("/engine/tenant-invitations/delivery-status", endpointEntry.Metadata["routePattern"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["tenantInvitationDeliveryStatusCallbackEndpointOwnership"]);
        Assert.Equal("false", endpointEntry.Metadata["callbackSignatureVerificationConfigured"]);
        Assert.Equal("not-configured", endpointEntry.Metadata["callbackSignatureVerificationOwnership"]);
        Assert.Equal("false", endpointEntry.Metadata["callbackReplayProtectionConfigured"]);
        Assert.Equal("not-configured", endpointEntry.Metadata["callbackReplayProtectionOwnership"]);
        Assert.Equal("none", endpointEntry.Metadata["callbackReplayProtectionPolicy"]);
        Assert.Equal("none", endpointEntry.Metadata["callbackReplayProtectionScope"]);
        Assert.Equal("not-configured", invitation.Metadata["deliveryStatusCallbackReplayProtection"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["providerSpecificCallbackTranslationOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["providerPollingOwnership"]);
    }

    [Fact]
    public async Task MapCephalonTenantInvitationDeliveryStatusObservationsReadsFilteredBoundedHistoryWithRollupsAndReportsMappedRuntimeSurface()
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
                    invitationId: "invite-observation-delivered",
                    tenantId: "tenant-observation",
                    inviteeId: "user-observation-delivered",
                    displayName: "Observation Delivered Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "provider-message-observation-delivered"
                    }));
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-observation-bounced",
                    tenantId: "tenant-observation",
                    inviteeId: "user-observation-bounced",
                    displayName: "Observation Bounced Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "provider-message-observation-bounced"
                    }));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
        {
            options.RequireTenantInvitationDeliveryStatusCallbackAuthorization = false;
            options.RequireTenantInvitationDeliveryStatusObservationAuthorization = false;
            options.TenantInvitationDeliveryStatusObservationDefaultLimit = 2;
            options.TenantInvitationDeliveryStatusObservationMaxLimit = 1;
        });

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryStatusCallbacks();
        app.MapCephalonTenantInvitationDeliveryStatusObservations();

        await app.StartAsync();
        var client = app.GetTestClient();
        var deliveredAtUtc = new DateTimeOffset(2026, 04, 29, 14, 0, 0, TimeSpan.Zero);

        await client.PostAsJsonAsync(
            "/engine/tenant-invitations/delivery-status",
            new TenantInvitationDeliveryStatusCallbackRequest
            {
                TenantId = "tenant-observation",
                InvitationId = "invite-observation-delivered",
                Status = TenantInvitationDeliveryStatuses.Delivered,
                ProviderMessageId = "provider-message-observation-delivered",
                SenderId = "http-webhook",
                Channel = "email",
                ObservedAtUtc = deliveredAtUtc,
                Actor = "notification-provider",
                CorrelationId = "delivery-observation-delivered"
            });
        await client.PostAsJsonAsync(
            "/engine/tenant-invitations/delivery-status",
            new TenantInvitationDeliveryStatusCallbackRequest
            {
                TenantId = "tenant-observation",
                InvitationId = "invite-observation-bounced",
                Status = TenantInvitationDeliveryStatuses.Bounced,
                ProviderMessageId = "provider-message-observation-bounced",
                SenderId = "http-webhook",
                Channel = "email",
                ObservedAtUtc = deliveredAtUtc.AddMinutes(1),
                Actor = "notification-provider",
                CorrelationId = "delivery-observation-bounced"
            });

        var response = await client.GetAsync("/engine/tenant-invitations/delivery-status/observations?tenantId=tenant-observation&status=delivered&limit=25");
        var result = await response.Content.ReadFromJsonAsync<TenantInvitationDeliveryStatusObservationQueryResult>();
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-status-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("in-memory", result.StoreKind);
        Assert.False(result.IsDurable);
        Assert.Equal("cephalon-managed", result.Ownership);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(1, result.ReturnedCount);
        Assert.Equal(6, result.SummaryCount);
        Assert.Equal(1, result.Limit);
        Assert.Equal("tenant-observation", result.Filters["tenantId"]);
        Assert.Equal("delivered", result.Filters["status"]);
        var observation = Assert.Single(result.Observations);
        Assert.Equal("tenant-observation", observation.TenantId);
        Assert.Equal("invite-observation-delivered", observation.InvitationId);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, observation.Status);
        Assert.Equal(TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled, observation.Outcome);
        Assert.Equal(deliveredAtUtc, observation.ObservedAtUtc);
        Assert.Equal("delivery-observation-delivered", observation.CorrelationId);
        var deliveredSummary = Assert.Single(
            result.Summaries,
            summary => summary.Dimension == "status" && summary.Value == TenantInvitationDeliveryStatuses.Delivered);
        Assert.Equal(1, deliveredSummary.Count);
        Assert.Equal(1, deliveredSummary.ReconciledCount);
        Assert.Equal(1, deliveredSummary.RecordedCount);
        Assert.Equal(deliveredAtUtc, deliveredSummary.LatestObservedAtUtc);

        var allResponse = await client.GetAsync("/engine/tenant-invitations/delivery-status/observations?tenantId=tenant-observation&limit=25");
        var allResult = await allResponse.Content.ReadFromJsonAsync<TenantInvitationDeliveryStatusObservationQueryResult>();
        Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
        Assert.NotNull(allResult);
        Assert.Equal(2, allResult.MatchedCount);
        Assert.Equal(1, allResult.ReturnedCount);
        Assert.Equal(7, allResult.SummaryCount);
        var statusSummaries = allResult.Summaries.Where(static summary => summary.Dimension == "status").ToArray();
        Assert.Contains(statusSummaries, summary => summary.Value == TenantInvitationDeliveryStatuses.Delivered && summary.Count == 1);
        Assert.Contains(statusSummaries, summary => summary.Value == TenantInvitationDeliveryStatuses.Bounced && summary.Count == 1);
        var sourceSummary = Assert.Single(
            allResult.Summaries,
            summary => summary.Dimension == "source" && summary.Value == "aspnetcore-delivery-status-callback");
        Assert.Equal(2, sourceSummary.Count);
        Assert.Equal(2, sourceSummary.ReconciledCount);
        Assert.Equal(2, sourceSummary.RecordedCount);
        Assert.Equal(deliveredAtUtc.AddMinutes(1), sourceSummary.LatestObservedAtUtc);

        Assert.Equal("mapped", endpointEntry.Metadata["observationEndpointRuntimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["observationEndpointMapped"]);
        Assert.Equal("false", endpointEntry.Metadata["observationRequireAuthorization"]);
        Assert.Equal("/engine/tenant-invitations/delivery-status/observations", endpointEntry.Metadata["observationRoutePattern"]);
        Assert.Equal("GET", endpointEntry.Metadata["observationHttpMethod"]);
        Assert.Equal("TenantInvitationDeliveryStatusObservationQueryResult", endpointEntry.Metadata["observationResponseBodyContract"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["tenantInvitationDeliveryStatusObservationEndpointOwnership"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["observationSummaryOwnership"]);
        Assert.Equal("filtered-normalized-observations", endpointEntry.Metadata["observationSummaryScope"]);
        Assert.Equal("status,outcome,source,channel,sender,tenant", endpointEntry.Metadata["observationSummaryDimensions"]);
        Assert.Equal("1", endpointEntry.Metadata["observationDefaultLimit"]);
        Assert.Equal("1", endpointEntry.Metadata["observationMaxLimit"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["providerSpecificCallbackInboxOwnership"]);
    }

    [Fact]
    public async Task MapCephalonTenantInvitationDeliveryStatusCallbacksVerifiesConfiguredCallbackSignature()
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
                    invitationId: "invite-signed-callback",
                    tenantId: "tenant-signed-callback",
                    inviteeId: "user-signed-callback",
                    displayName: "Signed Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "provider-message-signed"
                    }));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
        {
            options.RequireTenantInvitationDeliveryStatusCallbackAuthorization = false;
            options.TenantInvitationDeliveryStatusCallbackSigningSecret = "status-callback-secret";
            options.TenantInvitationDeliveryStatusCallbackSigningKeyId = "callback-key-1";
        });

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var request = new TenantInvitationDeliveryStatusCallbackRequest
        {
            TenantId = "tenant-signed-callback",
            InvitationId = "invite-signed-callback",
            Status = TenantInvitationDeliveryStatuses.Delivered,
            ProviderMessageId = "provider-message-signed",
            SenderId = "http-webhook",
            Channel = "email",
            Actor = "signed-provider",
            CorrelationId = "delivery-callback-signed"
        };
        var requestBody = JsonSerializer.Serialize(request, SerializerOptions);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        using var message = CreateSignedCallbackMessage(
            "status-callback-secret",
            timestamp,
            requestBody,
            keyId: "callback-key-1");

        var response = await client.SendAsync(message);
        var result = await response.Content.ReadFromJsonAsync<TenantInvitationDeliveryStatusReconciliationResult>();
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-status-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Reconciled);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("verified", invitation.Metadata["deliveryStatusCallbackSignatureVerification"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["deliveryStatusCallbackSignatureVerificationOwnership"]);
        Assert.Equal(timestamp, invitation.Metadata["deliveryStatusCallbackSignatureTimestamp"]);
        Assert.Equal("callback-key-1", invitation.Metadata["deliveryStatusCallbackSignatureKeyId"]);
        Assert.Equal("recorded", invitation.Metadata["deliveryStatusCallbackReplayProtection"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["deliveryStatusCallbackReplayProtectionOwnership"]);
        Assert.Equal("signed-callback", invitation.Metadata["deliveryStatusCallbackReplayPolicy"]);
        Assert.Equal("signature-fingerprint", invitation.Metadata["deliveryStatusCallbackReplayKey"]);
        Assert.Equal("process-local", invitation.Metadata["deliveryStatusCallbackReplayScope"]);
        Assert.Equal("none", invitation.Metadata["deliveryStatusCallbackReplayDurability"]);
        Assert.Equal("300", invitation.Metadata["deliveryStatusCallbackReplayRetentionSeconds"]);
        Assert.Equal("4096", invitation.Metadata["deliveryStatusCallbackReplayCacheLimit"]);
        Assert.StartsWith("sha256:", invitation.Metadata["deliveryStatusCallbackReplayFingerprint"], StringComparison.Ordinal);
        Assert.Equal("true", endpointEntry.Metadata["callbackSignatureVerificationConfigured"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["callbackSignatureVerificationOwnership"]);
        Assert.Equal("true", endpointEntry.Metadata["signatureKeyIdConfigured"]);
        Assert.Equal("X-Cephalon-Callback-Signature", endpointEntry.Metadata["signatureHeaderName"]);
        Assert.Equal("X-Cephalon-Callback-Signature-Timestamp", endpointEntry.Metadata["signatureTimestampHeaderName"]);
        Assert.Equal("X-Cephalon-Callback-Key-Id", endpointEntry.Metadata["signatureKeyIdHeaderName"]);
        Assert.Equal("300", endpointEntry.Metadata["signatureToleranceSeconds"]);
        Assert.Equal("true", endpointEntry.Metadata["callbackReplayProtectionConfigured"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["callbackReplayProtectionOwnership"]);
        Assert.Equal("signed-callback", endpointEntry.Metadata["callbackReplayProtectionPolicy"]);
        Assert.Equal("signature-fingerprint", endpointEntry.Metadata["callbackReplayProtectionKey"]);
        Assert.Equal("process-local", endpointEntry.Metadata["callbackReplayProtectionScope"]);
        Assert.Equal("none", endpointEntry.Metadata["callbackReplayProtectionDurability"]);
        Assert.Equal("300", endpointEntry.Metadata["callbackReplayProtectionRetentionSeconds"]);
        Assert.Equal("4096", endpointEntry.Metadata["callbackReplayProtectionCacheLimit"]);
        Assert.Equal("true", endpointEntry.Metadata["callbackReplayProtectionRequiresSignature"]);
    }

    [Fact]
    public async Task MapCephalonTenantInvitationDeliveryStatusCallbacksRejectsReplayedSignedCallback()
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
                    invitationId: "invite-replayed-callback",
                    tenantId: "tenant-replayed-callback",
                    inviteeId: "user-replayed-callback",
                    displayName: "Replayed Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "provider-message-replayed"
                    }));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
        {
            options.RequireTenantInvitationDeliveryStatusCallbackAuthorization = false;
            options.TenantInvitationDeliveryStatusCallbackSigningSecret = "status-callback-secret";
            options.TenantInvitationDeliveryStatusCallbackSigningKeyId = "callback-key-1";
            options.TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds = 600;
            options.TenantInvitationDeliveryStatusCallbackReplayCacheLimit = 16;
        });

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var request = new TenantInvitationDeliveryStatusCallbackRequest
        {
            TenantId = "tenant-replayed-callback",
            InvitationId = "invite-replayed-callback",
            Status = TenantInvitationDeliveryStatuses.Delivered,
            ProviderMessageId = "provider-message-replayed",
            SenderId = "http-webhook",
            Channel = "email",
            Actor = "signed-provider",
            CorrelationId = "delivery-callback-replayed"
        };
        var requestBody = JsonSerializer.Serialize(request, SerializerOptions);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        using var firstMessage = CreateSignedCallbackMessage(
            "status-callback-secret",
            timestamp,
            requestBody,
            keyId: "callback-key-1");
        using var replayedMessage = CreateSignedCallbackMessage(
            "status-callback-secret",
            timestamp,
            requestBody,
            keyId: "callback-key-1");

        var firstResponse = await client.SendAsync(firstMessage);
        var replayedResponse = await client.SendAsync(replayedMessage);
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var observation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-status-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, replayedResponse.StatusCode);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("recorded", invitation.Metadata["deliveryStatusCallbackReplayProtection"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["deliveryStatusCallbackReplayProtectionOwnership"]);
        Assert.Equal("signed-callback", invitation.Metadata["deliveryStatusCallbackReplayPolicy"]);
        Assert.Equal("signature-fingerprint", invitation.Metadata["deliveryStatusCallbackReplayKey"]);
        Assert.Equal("process-local", invitation.Metadata["deliveryStatusCallbackReplayScope"]);
        Assert.Equal("none", invitation.Metadata["deliveryStatusCallbackReplayDurability"]);
        Assert.Equal("600", invitation.Metadata["deliveryStatusCallbackReplayRetentionSeconds"]);
        Assert.Equal("16", invitation.Metadata["deliveryStatusCallbackReplayCacheLimit"]);
        Assert.StartsWith("sha256:", invitation.Metadata["deliveryStatusCallbackReplayFingerprint"], StringComparison.Ordinal);
        Assert.Equal(invitation.Metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId], observation.ObservationId);
        Assert.Equal(TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled, observation.Outcome);
        Assert.Equal("true", endpointEntry.Metadata["callbackReplayProtectionConfigured"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["callbackReplayProtectionOwnership"]);
        Assert.Equal("signed-callback", endpointEntry.Metadata["callbackReplayProtectionPolicy"]);
        Assert.Equal("signature-fingerprint", endpointEntry.Metadata["callbackReplayProtectionKey"]);
        Assert.Equal("process-local", endpointEntry.Metadata["callbackReplayProtectionScope"]);
        Assert.Equal("none", endpointEntry.Metadata["callbackReplayProtectionDurability"]);
        Assert.Equal("600", endpointEntry.Metadata["callbackReplayProtectionRetentionSeconds"]);
        Assert.Equal("16", endpointEntry.Metadata["callbackReplayProtectionCacheLimit"]);
        Assert.Equal("true", endpointEntry.Metadata["callbackReplayProtectionRequiresSignature"]);
    }

    [Fact]
    public async Task MapCephalonTenantInvitationDeliveryStatusCallbacksDoesNotPoisonReplayGuardWhenReconciliationFails()
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
                    invitationId: "invite-failed-replay",
                    tenantId: "tenant-failed-replay",
                    inviteeId: "user-failed-replay",
                    displayName: "Failed Replay Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "provider-message-expected"
                    }));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
        {
            options.RequireTenantInvitationDeliveryStatusCallbackAuthorization = false;
            options.TenantInvitationDeliveryStatusCallbackSigningSecret = "status-callback-secret";
            options.TenantInvitationDeliveryStatusCallbackSigningKeyId = "callback-key-1";
        });

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var request = new TenantInvitationDeliveryStatusCallbackRequest
        {
            TenantId = "tenant-failed-replay",
            InvitationId = "invite-failed-replay",
            Status = TenantInvitationDeliveryStatuses.Delivered,
            ProviderMessageId = "provider-message-wrong",
            SenderId = "http-webhook",
            Channel = "email"
        };
        var requestBody = JsonSerializer.Serialize(request, SerializerOptions);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        using var firstMessage = CreateSignedCallbackMessage(
            "status-callback-secret",
            timestamp,
            requestBody,
            keyId: "callback-key-1");
        using var secondMessage = CreateSignedCallbackMessage(
            "status-callback-secret",
            timestamp,
            requestBody,
            keyId: "callback-key-1");

        var firstResponse = await client.SendAsync(firstMessage);
        var secondResponse = await client.SendAsync(secondMessage);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<TenantInvitationDeliveryStatusReconciliationResult>();
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<TenantInvitationDeliveryStatusReconciliationResult>();
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);

        Assert.Equal(HttpStatusCode.Conflict, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.Equal(TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMismatch, firstResult.Outcome);
        Assert.Equal(TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMismatch, secondResult.Outcome);
        Assert.False(invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus));
        Assert.False(invitation.Metadata.ContainsKey("deliveryStatusCallbackReplayProtection"));
    }

    [Fact]
    public async Task MapCephalonTenantInvitationDeliveryStatusCallbacksRejectsUnsignedConfiguredCallback()
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
                    invitationId: "invite-unsigned-callback",
                    tenantId: "tenant-unsigned-callback",
                    inviteeId: "user-unsigned-callback",
                    displayName: "Unsigned Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
        {
            options.RequireTenantInvitationDeliveryStatusCallbackAuthorization = false;
            options.TenantInvitationDeliveryStatusCallbackSigningSecret = "status-callback-secret";
        });

        await using var app = builder.Build();
        app.MapCephalonTenantInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/tenant-invitations/delivery-status",
            new TenantInvitationDeliveryStatusCallbackRequest
            {
                TenantId = "tenant-unsigned-callback",
                InvitationId = "invite-unsigned-callback",
                Status = TenantInvitationDeliveryStatuses.Delivered
            });
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-status-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus));
        Assert.Equal("true", endpointEntry.Metadata["callbackSignatureVerificationConfigured"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["callbackSignatureVerificationOwnership"]);
        Assert.Equal("true", endpointEntry.Metadata["callbackReplayProtectionConfigured"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["callbackReplayProtectionOwnership"]);
        Assert.Equal("signed-callback", endpointEntry.Metadata["callbackReplayProtectionPolicy"]);
        Assert.Equal("process-local", endpointEntry.Metadata["callbackReplayProtectionScope"]);
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
    public async Task MapCephalonTenantInvitationDeliveryStatusObservationsDeniesAnonymousByDefault()
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
        app.MapCephalonTenantInvitationDeliveryStatusObservations();

        await app.StartAsync();
        var client = app.GetTestClient();
        var response = await client.GetAsync("/engine/tenant-invitations/delivery-status/observations");
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-status-http-endpoints");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("mapped", endpointEntry.Metadata["observationEndpointRuntimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["observationEndpointMapped"]);
        Assert.Equal("true", endpointEntry.Metadata["observationRequireAuthorization"]);
        Assert.Equal("none", endpointEntry.Metadata["observationAuthorizationPolicy"]);
    }

    private sealed class RecordingTenantInvitationDeliverySender(string senderId) : ITenantInvitationDeliverySender
    {
        public string SenderId { get; } = senderId;

        public List<TenantInvitationDeliveryContext> Contexts { get; } = [];

        public ValueTask<TenantInvitationDeliverySenderResult> SendAsync(
            TenantInvitationDeliveryContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Contexts.Add(context);
            return ValueTask.FromResult(new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.Dispatched,
                dispatched: true,
                providerMessageId: "provider-message-dispatch-http",
                reason: "Accepted by hosting test sender.",
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["senderMetadata"] = "accepted"
                }));
        }
    }

    private static string CreateCallbackSignature(string secret, string timestamp, string requestBody)
    {
        var signedPayload = $"{timestamp}.{requestBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        return "v1=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static HttpRequestMessage CreateSignedCallbackMessage(
        string secret,
        string timestamp,
        string requestBody,
        string? keyId = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/engine/tenant-invitations/delivery-status")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        message.Headers.TryAddWithoutValidation("X-Cephalon-Callback-Signature-Timestamp", timestamp);
        if (!string.IsNullOrWhiteSpace(keyId))
        {
            message.Headers.TryAddWithoutValidation("X-Cephalon-Callback-Key-Id", keyId);
        }

        message.Headers.TryAddWithoutValidation(
            "X-Cephalon-Callback-Signature",
            CreateCallbackSignature(secret, timestamp, requestBody));
        return message;
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
