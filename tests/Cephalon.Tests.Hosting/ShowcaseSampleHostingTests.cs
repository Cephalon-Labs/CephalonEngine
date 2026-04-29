using System.Net;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Agentics;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Health;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.Agentics.Services;
using Cephalon.Retrieval.Services;
using Cephalon.Sample.Showcase;
using Cephalon.Sample.Showcase.Infrastructure;
using Cephalon.Sample.Showcase.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

/// <summary>
/// Integration tests verifying that the showcase sample boots correctly and exposes
/// all five behavior patterns, domain modules, and cross-cutting capabilities.
/// </summary>
public sealed class ShowcaseSampleHostingTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] DatabaseTopologyProjectionTags =
    [
        "database-topology",
        "projection"
    ];
    private static readonly string[] DatabaseTopologyReadyActionSourceRoleIds =
    [
        "history",
        "outbox",
        "read",
        "write"
    ];
    private static readonly string[] DatabaseTopologyReadyActionSourceMigrationIds =
    [
        "history",
        "read",
        "write"
    ];
    private static readonly string OrdersRoutePrefix = BuildModuleRestPrefix<OrdersModule>("/showcase/orders");

    [Fact]
    public async Task ShowcaseSampleBootsAndExposesRootSummary()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Showcase", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Modular", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShowcaseSampleResolvesCanonicalShowcaseRoute()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/showcase");

        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found,
            $"Expected /showcase to resolve cleanly but received {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task ShowcaseSampleShowcaseHtmlPromotesDatabaseTopologyOperatorSection()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/showcase.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("href=\"#database-topology\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"database-topology\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"databaseTopologyReadiness\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"databaseTopologyActionPlanSummary\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"databaseTopologyActionPlanList\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"databaseTopologyInsights\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"databaseMigrationPlaybookSummary\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"databaseMigrationPlaybookList\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"databaseRoleTable\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"databaseMigrationTable\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"readModelScopeList\"", html, StringComparison.Ordinal);
        Assert.Contains("Runnable Guidance", html, StringComparison.Ordinal);
        Assert.Contains("Read-Model Sync", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesAppProfileWithAllCapabilities()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var profile = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");

        Assert.NotNull(profile);
        Assert.Equal("modular-monolith", profile.BlueprintId);
        Assert.Equal("EntityFramework", profile.Data.Provider);
        Assert.True(profile.Data.ReadWriteSplit);
        Assert.True(profile.Data.OutboxEnabled);
        Assert.Equal("Sfid", profile.Data.IdGenerator);
        Assert.True(profile.Audit.Enabled);
        Assert.True(profile.Audit.History.Enabled);
        Assert.Equal("entity-framework", profile.Audit.History.Provider);
        Assert.Equal("history", profile.Audit.History.DatabaseRole);
        Assert.True(profile.Audit.History.Retention.Enabled);
        Assert.Equal(90, profile.Audit.History.Retention.MaxAgeDays);
        Assert.Equal(250, profile.Audit.History.Retention.DeleteBatchSize);
        Assert.True(profile.Audit.History.Retention.ApplyOnStartup);
        Assert.Null(profile.Audit.History.Retention.RunIntervalMinutes);
        Assert.True(profile.Identity.Enabled);
        Assert.True(profile.Tenancy.Enabled);
        Assert.Equal(30, profile.Databases.Runtime.RoleProbeFreshnessSeconds);
        Assert.Equal("InMemory", profile.Databases.Write.Provider);
        Assert.NotNull(profile.Databases.Write.ConnectionString);
        Assert.StartsWith("showcase-write-", profile.Databases.Write.ConnectionString!, StringComparison.Ordinal);
        Assert.Null(profile.Databases.Write.ConnectionStringName);
        Assert.Equal("InMemory", profile.Databases.Read.Provider);
        Assert.NotNull(profile.Databases.Read.ConnectionString);
        Assert.StartsWith("showcase-read-", profile.Databases.Read.ConnectionString!, StringComparison.Ordinal);
        Assert.Null(profile.Databases.Read.ConnectionStringName);
        Assert.True(profile.Databases.Outbox.HasValues);
        Assert.Equal("write", profile.Databases.Outbox.UseRole);
        Assert.Equal("outbox01", profile.Databases.Outbox.Schema);
        Assert.Equal("InMemory", profile.Databases.History.Provider);
        Assert.NotNull(profile.Databases.History.ConnectionString);
        Assert.StartsWith("showcase-history-", profile.Databases.History.ConnectionString!, StringComparison.Ordinal);
        Assert.Null(profile.Databases.History.ConnectionStringName);
        Assert.True(profile.Databases.Migrations.ApplyOnStartup);
        Assert.Equal(3, profile.Databases.Migrations.Targets.Count);
        Assert.Contains("write", profile.Databases.Migrations.Targets);
        Assert.Contains("read", profile.Databases.Migrations.Targets);
        Assert.Contains("history", profile.Databases.Migrations.Targets);
        Assert.True(profile.Resilience.RateLimiting.Enabled);
        Assert.Equal("SlidingWindow", profile.Resilience.RateLimiting.Algorithm);
        Assert.Equal(200, profile.Resilience.RateLimiting.PermitLimit);
        Assert.Equal(20, profile.Resilience.RateLimiting.QueueLimit);
        Assert.Equal(60, profile.Resilience.RateLimiting.WindowSeconds);
        Assert.Equal(4, profile.Resilience.RateLimiting.SegmentsPerWindow);
        var overridePolicy = Assert.Single(profile.Resilience.RateLimiting.Overrides);
        Assert.Equal("cart-read-hot-path", overridePolicy.Id);
        Assert.Equal(["cart.get"], overridePolicy.BehaviorIds);
        Assert.Equal(["rest-api"], overridePolicy.TransportIds);
        Assert.Null(overridePolicy.Enabled);
        Assert.Equal(400, overridePolicy.PermitLimit);
        Assert.Equal(40, overridePolicy.QueueLimit);
    }

    [Fact]
    public async Task ShowcaseSampleExecutesAgenticToolAndProjectsRunState()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var dispatcher = app.Services.GetRequiredService<IAgentToolDispatcher>();

        var result = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "showcase.catalog-inspector",
            runId: "showcase-agent-run-001",
            arguments: new Dictionary<string, string>
            {
                ["focus"] = "catalog"
            },
            actorId: "showcase-test",
            correlationId: "corr-showcase-agentics-001"));

        var client = app.GetTestClient();
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/agentic-workloads");

        Assert.Equal(AgentToolExecutionOutcomes.Succeeded, result.Outcome);
        Assert.Equal("Inspected showcase catalog posture.", result.OutputSummary);
        Assert.NotNull(surfaces);
        var agentics = Assert.Single(surfaces);
        var tool = Assert.Single(agentics.Entries, entry => entry.Id == "showcase.catalog-inspector");
        Assert.Equal("cephalon-managed", tool.Metadata["executionOwnership"]);
        Assert.Equal("reported", tool.Metadata["runtimeState"]);
        Assert.Equal("showcase-agent-run-001", tool.Metadata["lastRunId"]);
        Assert.Equal("succeeded", tool.Metadata["lastOutcome"]);
        Assert.Equal("2", tool.Metadata["totalReports"]);
        Assert.Equal("showcase-test", tool.Metadata["lastActorId"]);
        Assert.Equal("corr-showcase-agentics-001", tool.Metadata["lastCorrelationId"]);
        Assert.Equal("Inspected showcase catalog posture.", tool.Metadata["lastOutputSummary"]);
        Assert.Equal("catalog", tool.Metadata["reported.focus"]);
    }

    [Fact]
    public async Task ShowcaseSampleIndexesQueriesAndProjectsRetrievalFreshness()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var indexer = app.Services.GetRequiredService<IKnowledgeIndexer>();
        var queryEngine = app.Services.GetRequiredService<IKnowledgeQueryEngine>();

        var indexResult = await indexer.IndexAsync(new KnowledgeIndexingRequest(
            collectionId: "showcase.docs",
            runId: "showcase-retrieval-index-001",
            actorId: "showcase-test",
            correlationId: "corr-showcase-retrieval-001"));
        var queryResult = await queryEngine.QueryAsync(new KnowledgeQueryRequest(
            collectionId: "showcase.docs",
            queryText: "retrieval readiness",
            maxResults: 5,
            actorId: "showcase-test",
            correlationId: "corr-showcase-retrieval-query-001"));
        var client = app.GetTestClient();
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/knowledge-retrieval");

        Assert.Equal(KnowledgeIndexingOutcomes.Succeeded, indexResult.Outcome);
        Assert.Equal(3, indexResult.DocumentCount);
        Assert.True(queryResult.HasMatches);
        Assert.Contains(queryResult.Matches, match => match.DocumentId == "showcase.docs.retrieval");
        Assert.NotNull(surfaces);
        var retrieval = Assert.Single(surfaces);
        var entry = Assert.Single(retrieval.Entries, item => item.Id == "showcase.docs");
        Assert.Equal("cephalon-managed", entry.Metadata["indexingOwnership"]);
        Assert.Equal("cephalon-managed", entry.Metadata["queryOwnership"]);
        Assert.Equal("indexed", entry.Metadata["runtimeState"]);
        Assert.Equal(KnowledgeIndexFreshnessStates.Fresh, entry.Metadata["freshnessState"]);
        Assert.Equal("3", entry.Metadata["documentCount"]);
        Assert.Equal("1", entry.Metadata["queryCount"]);
        Assert.False(string.IsNullOrWhiteSpace(entry.Metadata["lastQueryFingerprint"]));
    }

    [Fact]
    public async Task ShowcaseSampleExposesEffectiveRateLimitingRuntimeCatalog()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(policies);
        Assert.Equal(2, policies.Length);
        var defaultPolicy = Assert.Single(policies, policy => policy.Id == "cephalon-public-http");
        Assert.Equal("aspnetcore-endpoint-policy", defaultPolicy.ExecutionMode);
        Assert.Contains("behavior-http", defaultPolicy.TransportIds);
        Assert.Contains("rest-api", defaultPolicy.TransportIds);
        Assert.Contains("/engine", defaultPolicy.ExcludedPathPrefixes);
        Assert.Contains("/openapi", defaultPolicy.ExcludedPathPrefixes);
        Assert.Contains("/scalar", defaultPolicy.ExcludedPathPrefixes);
        Assert.True(defaultPolicy.Effective.Enabled);
        Assert.Equal("SlidingWindow", defaultPolicy.Effective.Algorithm);
        Assert.Equal(200, defaultPolicy.Effective.PermitLimit);
        var overridePolicy = Assert.Single(policies, policy => policy.Id == "cephalon-rate-limit-cart-read-hot-path");
        Assert.Equal("aspnetcore-endpoint-policy", overridePolicy.ExecutionMode);
        Assert.Equal("behavior-transport-endpoints", overridePolicy.Scope);
        Assert.Equal("cart.get", overridePolicy.Metadata["behaviorIds"]);
        Assert.Equal("rest-api", overridePolicy.Metadata["transportIds"]);
        Assert.Equal("true", overridePolicy.Metadata["isOverride"]);
        Assert.Equal(400, overridePolicy.Effective.PermitLimit);
        Assert.Equal(40, overridePolicy.Effective.QueueLimit);
        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot.RateLimitingPolicies.Count);
        Assert.Contains(snapshot.RateLimitingPolicies, policy => policy.Id == "cephalon-public-http");
        Assert.Contains(snapshot.RateLimitingPolicies, policy => policy.Id == "cephalon-rate-limit-cart-read-hot-path");
    }

    [Fact]
    public async Task ShowcaseSampleRateLimitingRejectsPublicBurstsButLeavesOperatorRoutesAvailable()
    {
        await using var app = BuildShowcaseWithTightRateLimiting();

        await app.StartAsync();
        var client = app.GetTestClient();

        var firstPublicResponse = await client.GetAsync("/api/v1/showcase/catalog/products");
        var secondPublicResponse = await client.GetAsync("/api/v1/showcase/catalog/products");
        var manifestResponse = await client.GetAsync("/engine/manifest");
        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var rejectedPayload = await secondPublicResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, firstPublicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondPublicResponse.StatusCode);
        Assert.Contains("Too Many Requests", rejectedPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, manifestResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);
    }

    [Fact]
    public async Task ShowcaseSampleExposesDatabaseRoleCatalogWithRequestedAndResolvedTruth()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var roles = await client.GetFromJsonAsync<DatabaseRoleDescriptor[]>("/engine/database-roles");
        var outbox = await client.GetFromJsonAsync<DatabaseRoleDescriptor>("/engine/database-roles/outbox");
        var readRole = await client.GetFromJsonAsync<DatabaseRoleDescriptor>("/engine/database-roles/read");
        var history = await client.GetFromJsonAsync<DatabaseRoleDescriptor>("/engine/database-roles/history");
        var migrations = await client.GetFromJsonAsync<DatabaseMigrationDescriptor[]>("/engine/database-migrations");
        var readMigration = await client.GetFromJsonAsync<DatabaseMigrationDescriptor>("/engine/database-migrations/read");
        var historyMigration = await client.GetFromJsonAsync<DatabaseMigrationDescriptor>("/engine/database-migrations/history");
        var migrationPlaybook = await client.GetFromJsonAsync<DatabaseMigrationOperationalPlaybook>("/engine/database-migration-playbook");
        var databaseTopology = await client.GetFromJsonAsync<DatabaseTopologyOperationalSnapshot>("/engine/database-topology");
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(roles);
        Assert.Equal(4, roles.Length);
        var write = Assert.Single(roles, role => role.Id == "write");
        var read = Assert.Single(roles, role => role.Id == "read");

        Assert.NotNull(outbox);
        Assert.Equal("outbox", outbox.RequestedRoleId);
        Assert.Equal("write", outbox.ResolvedRoleId);
        Assert.Equal("role-reference", outbox.ResolutionMode);
        Assert.True(outbox.UsesRoleReference);
        Assert.Equal("write", outbox.UseRole);
        Assert.NotNull(outbox.PhysicalTargetId);
        Assert.Equal(write.PhysicalTargetId, outbox.PhysicalTargetId);
        Assert.Equal("InMemory", outbox.Provider);
        Assert.Equal("inline", outbox.ConnectionMode);
        Assert.Null(outbox.ConnectionStringName);
        Assert.Equal("outbox01", outbox.Schema);
        Assert.Contains("outbox", outbox.Consumers);
        Assert.Contains("write", outbox.CoLocatedRoles);
        Assert.Contains("write", outbox.PhysicalCoLocatedRoles);
        Assert.Equal("true", outbox.Metadata["inheritsResolvedRoleRuntime"]);

        Assert.NotNull(readRole);
        Assert.Equal("read", readRole.RequestedRoleId);
        Assert.Equal("read", readRole.ResolvedRoleId);
        Assert.Equal("direct", readRole.ResolutionMode);
        Assert.False(readRole.UsesRoleReference);
        Assert.NotNull(readRole.PhysicalTargetId);
        Assert.NotEqual(write.PhysicalTargetId, readRole.PhysicalTargetId);
        Assert.Equal("InMemory", readRole.Provider);
        Assert.Equal("inline", readRole.ConnectionMode);
        Assert.Null(readRole.ConnectionStringName);
        Assert.Contains("migrations", readRole.Consumers);

        Assert.NotNull(history);
        Assert.Equal("history", history.RequestedRoleId);
        Assert.Equal("history", history.ResolvedRoleId);
        Assert.Equal("direct", history.ResolutionMode);
        Assert.False(history.UsesRoleReference);
        Assert.NotNull(history.PhysicalTargetId);
        Assert.NotEqual(write.PhysicalTargetId, history.PhysicalTargetId);
        Assert.Equal("InMemory", history.Provider);
        Assert.Equal("inline", history.ConnectionMode);
        Assert.Null(history.ConnectionStringName);
        Assert.Contains("audit-history", history.Consumers);
        Assert.Contains("migrations", history.Consumers);
        Assert.Equal("entity-framework", history.Metadata["auditHistoryProvider"]);
        Assert.Equal(HealthState.Healthy, write.HealthState);
        Assert.Equal(HealthState.Healthy, read.HealthState);
        Assert.Equal(HealthState.Healthy, outbox.HealthState);
        Assert.Equal(HealthState.Healthy, history.HealthState);
        Assert.Equal("succeeded", write.MigrationState);
        Assert.Equal("succeeded", read.MigrationState);
        Assert.Equal("succeeded", outbox.MigrationState);
        Assert.Equal("succeeded", history.MigrationState);
        Assert.Equal("entity-framework", write.RuntimeMetadata["providerPack"]);
        Assert.Equal("entity-framework", read.RuntimeMetadata["providerPack"]);
        Assert.Equal("entity-framework", outbox.RuntimeMetadata["providerPack"]);
        Assert.Equal("entity-framework", history.RuntimeMetadata["providerPack"]);
        Assert.NotNull(write.Probe);
        Assert.True(write.Probe.CacheEnabled);
        Assert.Equal(30, write.Probe.FreshnessSeconds);
        Assert.Equal("configured", write.Probe.FreshnessOrigin);
        Assert.Equal(write.RuntimeMetadata["probeSource"], write.Probe.Source);
        Assert.True(write.Probe.Source is "live" or "cache");
        Assert.NotNull(read.Probe);
        Assert.Equal(read.RuntimeMetadata["probeSource"], read.Probe.Source);
        Assert.True(read.Probe.Source is "live" or "cache");
        Assert.NotNull(outbox.Probe);
        Assert.Equal(outbox.RuntimeMetadata["probeSource"], outbox.Probe.Source);
        Assert.True(outbox.Probe.Source is "live" or "cache");
        Assert.NotNull(history.Probe);
        Assert.Equal(history.RuntimeMetadata["probeSource"], history.Probe.Source);
        Assert.True(history.Probe.Source is "live" or "cache");
        Assert.Equal("true", write.RuntimeMetadata["probeCacheEnabled"]);
        Assert.Equal("30", write.RuntimeMetadata["probeFreshnessSeconds"]);
        Assert.Equal("configured", write.RuntimeMetadata["probeFreshnessOrigin"]);
        Assert.Equal("startup-hosted-service", write.RuntimeMetadata["executionMode"]);
        Assert.Equal("startup-hosted-service", read.RuntimeMetadata["executionMode"]);
        Assert.Equal("startup-hosted-service", outbox.RuntimeMetadata["executionMode"]);
        Assert.Equal("startup-hosted-service", history.RuntimeMetadata["executionMode"]);
        Assert.Equal("succeeded", write.RuntimeMetadata["probeOutcome"]);
        Assert.Equal("succeeded", read.RuntimeMetadata["probeOutcome"]);
        Assert.Equal("succeeded", outbox.RuntimeMetadata["probeOutcome"]);
        Assert.Equal("succeeded", history.RuntimeMetadata["probeOutcome"]);
        Assert.NotNull(migrations);
        Assert.Equal(3, migrations.Length);
        Assert.Equal("write", migrations[0].Id);
        Assert.Equal(1, migrations[0].RecommendedExecutionOrder);
        Assert.Equal("read", migrations[1].Id);
        Assert.Equal(2, migrations[1].RecommendedExecutionOrder);
        Assert.Equal("history", migrations[2].Id);
        Assert.Equal(3, migrations[2].RecommendedExecutionOrder);
        Assert.NotNull(readMigration);
        Assert.Equal("read", readMigration.Id);
        Assert.Equal("read", readMigration.RequestedRoleId);
        Assert.Equal("read", readMigration.ResolvedRoleId);
        Assert.Equal(2, readMigration.RecommendedExecutionOrder);
        Assert.Equal(DatabaseMigrationStatus.Succeeded, readMigration.Status);
        Assert.Equal("startup-hosted-service", readMigration.ExecutionMode);
        Assert.Equal("InMemory", readMigration.Provider);
        Assert.Equal(HealthState.Healthy, readMigration.RoleHealthState);
        Assert.Equal("succeeded", readMigration.RoleMigrationState);
        Assert.True(readMigration.RoleObservedAtUtc.HasValue);
        Assert.Equal("entity-framework", readMigration.Metadata["runtimeProvider"]);
        Assert.Equal("healthy", readMigration.Metadata["roleHealthState"]);
        Assert.Equal("succeeded", readMigration.Metadata["roleMigrationState"]);
        Assert.Equal("succeeded", readMigration.Metadata["roleRuntime.probeOutcome"]);
        Assert.NotNull(historyMigration);
        Assert.Equal("history", historyMigration.Id);
        Assert.Equal("history", historyMigration.RequestedRoleId);
        Assert.Equal("history", historyMigration.ResolvedRoleId);
        Assert.Equal(3, historyMigration.RecommendedExecutionOrder);
        Assert.Equal(DatabaseMigrationStatus.Succeeded, historyMigration.Status);
        Assert.Equal("startup-hosted-service", historyMigration.ExecutionMode);
        Assert.Equal("InMemory", historyMigration.Provider);
        Assert.Equal(HealthState.Healthy, historyMigration.RoleHealthState);
        Assert.Equal("succeeded", historyMigration.RoleMigrationState);
        Assert.True(historyMigration.RoleObservedAtUtc.HasValue);
        Assert.Equal("entity-framework", historyMigration.Metadata["runtimeProvider"]);
        Assert.Equal("healthy", historyMigration.Metadata["roleHealthState"]);
        Assert.Equal("succeeded", historyMigration.Metadata["roleMigrationState"]);
        Assert.Equal("succeeded", historyMigration.Metadata["roleRuntime.probeOutcome"]);
        Assert.Equal("bundle-or-script", historyMigration.Metadata["recommendedExecutionMode"]);
        Assert.Collection(
            historyMigration.Commands,
            bundle =>
            {
                Assert.Equal("bundle", bundle.Id);
                Assert.True(bundle.RecommendedForProduction);
                Assert.Equal("dotnet ef migrations bundle --context ShowcaseAuditHistoryDbContext", bundle.CommandTemplate);
            },
            script =>
            {
                Assert.Equal("script", script.Id);
                Assert.True(script.RecommendedForProduction);
                Assert.Equal("dotnet ef migrations script --context ShowcaseAuditHistoryDbContext --idempotent", script.CommandTemplate);
            },
            update =>
            {
                Assert.Equal("update", update.Id);
                Assert.False(update.RecommendedForProduction);
                Assert.Equal("dotnet ef database update --context ShowcaseAuditHistoryDbContext", update.CommandTemplate);
            });
        Assert.NotNull(migrationPlaybook);
        Assert.Equal(3, migrationPlaybook.TargetCount);
        Assert.Equal(3, migrationPlaybook.ExecutionGroupCount);
        Assert.Equal(3, migrationPlaybook.ProductionReadyTargetCount);
        Assert.Equal(3, migrationPlaybook.ManualPathTargetCount);
        Assert.Equal(3, migrationPlaybook.ApplyOnStartupTargetCount);
        Assert.Equal(0, migrationPlaybook.CoordinationRequiredTargetCount);
        Assert.Equal(0, migrationPlaybook.CoordinationRequiredGroupCount);
        Assert.Equal(3, migrationPlaybook.Steps.Count);
        Assert.Equal(3, migrationPlaybook.ExecutionGroups.Count);
        Assert.Equal("write", migrationPlaybook.Steps[0].DatabaseMigrationId);
        Assert.Equal("read", migrationPlaybook.Steps[1].DatabaseMigrationId);
        Assert.Equal("history", migrationPlaybook.Steps[2].DatabaseMigrationId);
        Assert.All(migrationPlaybook.Steps, static step => Assert.False(step.RequiresPhysicalTargetCoordination));
        Assert.All(migrationPlaybook.ExecutionGroups, static group => Assert.False(group.RequiresPhysicalTargetCoordination));
        Assert.Equal(["write"], migrationPlaybook.ExecutionGroups[0].DatabaseMigrationIds);
        var writeGroupProductionCommand = Assert.Single(migrationPlaybook.ExecutionGroups[0].ProductionCommands);
        Assert.Equal("write", writeGroupProductionCommand.DatabaseMigrationId);
        Assert.Equal("bundle", writeGroupProductionCommand.Command.Id);
        var writeGroupProductionBatch = Assert.IsType<DatabaseMigrationOperationalExecutionGroupCommandBatch>(
            migrationPlaybook.ExecutionGroups[0].ProductionCommandBatch);
        Assert.Equal("production", writeGroupProductionBatch.Id);
        Assert.Equal(["write"], writeGroupProductionBatch.DatabaseMigrationIds);
        Assert.Equal(
            "dotnet ef migrations bundle --context ShowcaseWriteDbContext",
            writeGroupProductionBatch.CommandTemplate);
        var historyGroupManualCommand = Assert.Single(migrationPlaybook.ExecutionGroups[2].ManualCommands);
        Assert.Equal("history", historyGroupManualCommand.DatabaseMigrationId);
        Assert.Equal("update", historyGroupManualCommand.Command.Id);
        var historyGroupManualBatch = Assert.IsType<DatabaseMigrationOperationalExecutionGroupCommandBatch>(
            migrationPlaybook.ExecutionGroups[2].ManualCommandBatch);
        Assert.Equal("manual", historyGroupManualBatch.Id);
        Assert.Equal(["history"], historyGroupManualBatch.DatabaseMigrationIds);
        Assert.Equal(
            "dotnet ef database update --context ShowcaseAuditHistoryDbContext",
            historyGroupManualBatch.CommandTemplate);
        Assert.True(migrationPlaybook.Steps[2].HasProductionRecommendedCommand);
        Assert.NotNull(migrationPlaybook.Steps[2].ProductionCommand);
        Assert.Equal("bundle", migrationPlaybook.Steps[2].ProductionCommand!.Id);
        Assert.NotNull(migrationPlaybook.Steps[2].ManualCommand);
        Assert.Equal("update", migrationPlaybook.Steps[2].ManualCommand!.Id);

        Assert.NotNull(snapshot);
        Assert.Equal(4, snapshot.DatabaseRoles.Count);
        Assert.Equal(3, snapshot.DatabaseMigrations.Count);
        Assert.NotNull(snapshot.DatabaseMigrationPlaybook);
        Assert.Equal(migrationPlaybook.TargetCount, snapshot.DatabaseMigrationPlaybook.TargetCount);
        Assert.Equal(migrationPlaybook.ProductionReadyTargetCount, snapshot.DatabaseMigrationPlaybook.ProductionReadyTargetCount);
        Assert.Equal(migrationPlaybook.ManualPathTargetCount, snapshot.DatabaseMigrationPlaybook.ManualPathTargetCount);
        Assert.Equal(migrationPlaybook.ApplyOnStartupTargetCount, snapshot.DatabaseMigrationPlaybook.ApplyOnStartupTargetCount);
        Assert.Equal(migrationPlaybook.CoordinationRequiredTargetCount, snapshot.DatabaseMigrationPlaybook.CoordinationRequiredTargetCount);
        Assert.Equal(migrationPlaybook.ExecutionGroupCount, snapshot.DatabaseMigrationPlaybook.ExecutionGroupCount);
        Assert.Equal(migrationPlaybook.CoordinationRequiredGroupCount, snapshot.DatabaseMigrationPlaybook.CoordinationRequiredGroupCount);
        Assert.Equal(migrationPlaybook.Steps.Count, snapshot.DatabaseMigrationPlaybook.Steps.Count);
        Assert.Equal(migrationPlaybook.ExecutionGroups.Count, snapshot.DatabaseMigrationPlaybook.ExecutionGroups.Count);
        Assert.Equal("history", snapshot.DatabaseMigrationPlaybook.Steps[2].DatabaseMigrationId);
        Assert.Equal("bundle", snapshot.DatabaseMigrationPlaybook.Steps[2].ProductionCommand!.Id);
        Assert.Equal("update", snapshot.DatabaseMigrationPlaybook.Steps[2].ManualCommand!.Id);
        Assert.Equal("bundle", Assert.Single(snapshot.DatabaseMigrationPlaybook.ExecutionGroups[0].ProductionCommands).Command.Id);
        Assert.Equal("update", Assert.Single(snapshot.DatabaseMigrationPlaybook.ExecutionGroups[2].ManualCommands).Command.Id);
        var snapshotWriteGroupProductionBatch = Assert.IsType<DatabaseMigrationOperationalExecutionGroupCommandBatch>(
            snapshot.DatabaseMigrationPlaybook.ExecutionGroups[0].ProductionCommandBatch);
        Assert.Equal(
            "dotnet ef migrations bundle --context ShowcaseWriteDbContext",
            snapshotWriteGroupProductionBatch.CommandTemplate);
        var snapshotHistoryGroupManualBatch = Assert.IsType<DatabaseMigrationOperationalExecutionGroupCommandBatch>(
            snapshot.DatabaseMigrationPlaybook.ExecutionGroups[2].ManualCommandBatch);
        Assert.Equal(
            "dotnet ef database update --context ShowcaseAuditHistoryDbContext",
            snapshotHistoryGroupManualBatch.CommandTemplate);
        Assert.NotNull(databaseTopology);
        Assert.Equal("Ready", databaseTopology.Summary.Status);
        Assert.Equal("Database topology is ready", databaseTopology.Summary.Headline);
        Assert.Equal("/engine/snapshot", databaseTopology.Summary.ActionPath);
        Assert.Equal(4, databaseTopology.Summary.RoleCount);
        Assert.Equal(4, databaseTopology.Summary.HealthyRoleCount);
        Assert.Equal(3, databaseTopology.Summary.MigrationTargetCount);
        Assert.Equal(3, databaseTopology.Summary.SucceededMigrationTargetCount);
        Assert.Equal(3, databaseTopology.Summary.ProductionReadyMigrationTargetCount);
        Assert.Equal(1, databaseTopology.ActionPlan.TotalActionCount);
        Assert.Equal(0, databaseTopology.ActionPlan.BlockingActionCount);
        Assert.Equal(0, databaseTopology.ActionPlan.AttentionActionCount);
        Assert.Equal(1, databaseTopology.ActionPlan.ReadyActionCount);
        Assert.Contains(databaseTopology.ActionPlan.Actions, action =>
            action.Id == "topology-ready-for-validation" &&
            action.Category == "topology-posture" &&
            action.Tone == "Success" &&
            action.ActionPath == "/engine/snapshot" &&
            action.SourceRoleIds.SequenceEqual(["history", "outbox", "read", "write"]) &&
            action.SourceMigrationIds.SequenceEqual(["history", "read", "write"]));
        Assert.Contains(databaseTopology.Advisories, advisory =>
            advisory.Id == "topology-aligned" &&
            advisory.Tone == "Success" &&
            advisory.ActionPath == "/engine/snapshot");
        Assert.Contains(databaseTopology.Advisories, advisory =>
            advisory.Id == "migration-production-guidance" &&
            advisory.Tone == "Success" &&
            advisory.ActionPath == "/engine/database-migrations");
        Assert.NotNull(snapshot.DatabaseTopology);
        Assert.Equal(databaseTopology.Summary.Status, snapshot.DatabaseTopology.Summary.Status);
        Assert.Equal(databaseTopology.Summary.ProductionReadyMigrationTargetCount, snapshot.DatabaseTopology.Summary.ProductionReadyMigrationTargetCount);
        Assert.Equal(databaseTopology.ActionPlan.TotalActionCount, snapshot.DatabaseTopology.ActionPlan.TotalActionCount);
        Assert.Contains(snapshot.DatabaseTopology.ActionPlan.Actions, action =>
            action.Id == "topology-ready-for-validation" &&
            action.Category == "topology-posture");
        Assert.Equal("write", snapshot.DatabaseMigrations[0].Id);
        Assert.Equal(1, snapshot.DatabaseMigrations[0].RecommendedExecutionOrder);
        Assert.Equal("read", snapshot.DatabaseMigrations[1].Id);
        Assert.Equal(2, snapshot.DatabaseMigrations[1].RecommendedExecutionOrder);
        Assert.Equal("history", snapshot.DatabaseMigrations[2].Id);
        Assert.Equal(3, snapshot.DatabaseMigrations[2].RecommendedExecutionOrder);
        Assert.All(snapshot.DatabaseMigrations, migration => Assert.Equal(3, migration.Commands.Count));
        Assert.All(snapshot.DatabaseMigrations, migration => Assert.Equal(HealthState.Healthy, migration.RoleHealthState));
        Assert.All(snapshot.DatabaseMigrations, migration => Assert.Equal("succeeded", migration.RoleMigrationState));
        Assert.All(snapshot.DatabaseMigrations, migration => Assert.True(migration.RoleObservedAtUtc.HasValue));
    }

    [Fact]
    public async Task ShowcaseSampleExposesSharedPhysicalMigrationCoordinationWhenWriteAndReadShareOneTarget()
    {
        await using var app = BuildShowcaseForTests(configureBuilder: ShareReadRoleWithWriteAndDisableStartupApply);

        await app.StartAsync();
        var client = app.GetTestClient();

        var roles = await client.GetFromJsonAsync<DatabaseRoleDescriptor[]>("/engine/database-roles");
        var migrationPlaybook = await client.GetFromJsonAsync<DatabaseMigrationOperationalPlaybook>("/engine/database-migration-playbook");
        var databaseTopology = await client.GetFromJsonAsync<DatabaseTopologyOperationalSnapshot>("/engine/database-topology");

        Assert.NotNull(roles);
        Assert.NotNull(migrationPlaybook);
        Assert.NotNull(databaseTopology);

        var write = Assert.Single(roles, role => role.Id == "write");
        var read = Assert.Single(roles, role => role.Id == "read");
        Assert.NotNull(write.PhysicalTargetId);
        Assert.Equal(write.PhysicalTargetId, read.PhysicalTargetId);
        Assert.NotNull(write.PhysicalTargetDisplayName);
        Assert.Contains("inline connection", write.PhysicalTargetDisplayName!, StringComparison.Ordinal);
        Assert.Contains("read", write.PhysicalCoLocatedRoles);
        Assert.Contains("write", read.PhysicalCoLocatedRoles);

        Assert.Equal(3, migrationPlaybook.TargetCount);
        Assert.Equal(2, migrationPlaybook.ExecutionGroupCount);
        Assert.Equal(2, migrationPlaybook.CoordinationRequiredTargetCount);
        Assert.Equal(1, migrationPlaybook.CoordinationRequiredGroupCount);
        var writeStep = Assert.Single(migrationPlaybook.Steps, static step => step.DatabaseMigrationId == "write");
        var readStep = Assert.Single(migrationPlaybook.Steps, static step => step.DatabaseMigrationId == "read");
        var historyStep = Assert.Single(migrationPlaybook.Steps, static step => step.DatabaseMigrationId == "history");
        var sharedGroup = Assert.Single(migrationPlaybook.ExecutionGroups, static group => group.RequiresPhysicalTargetCoordination);
        Assert.Equal(write.PhysicalTargetId, sharedGroup.PhysicalTargetId);
        Assert.Equal(DatabaseMigrationStatus.Planned, sharedGroup.Status);
        Assert.Equal(["read", "write"], sharedGroup.DatabaseMigrationIds);
        Assert.Equal(2, sharedGroup.ProductionReadyTargetCount);
        Assert.Equal(2, sharedGroup.ManualPathTargetCount);
        Assert.Equal(0, sharedGroup.ApplyOnStartupTargetCount);
        Assert.Contains("coordinated physical-target batch", sharedGroup.CoordinationHint!, StringComparison.Ordinal);
        Assert.Equal(["write", "read"], sharedGroup.ProductionCommands.Select(static command => command.DatabaseMigrationId));
        Assert.All(sharedGroup.ProductionCommands, static command => Assert.Equal("bundle", command.Command.Id));
        var sharedGroupProductionBatch = Assert.IsType<DatabaseMigrationOperationalExecutionGroupCommandBatch>(
            sharedGroup.ProductionCommandBatch);
        Assert.Equal(["write", "read"], sharedGroupProductionBatch.DatabaseMigrationIds);
        Assert.Equal(["bundle"], sharedGroupProductionBatch.CommandIds);
        Assert.Equal(
            string.Join(
                "\n",
                [
                    "dotnet ef migrations bundle --context ShowcaseWriteDbContext",
                    "dotnet ef migrations bundle --context ShowcaseReadDbContext"
                ]),
            sharedGroupProductionBatch.CommandTemplate);
        Assert.Equal(["write", "read"], sharedGroup.ManualCommands.Select(static command => command.DatabaseMigrationId));
        Assert.All(sharedGroup.ManualCommands, static command => Assert.Equal("update", command.Command.Id));
        var sharedGroupManualBatch = Assert.IsType<DatabaseMigrationOperationalExecutionGroupCommandBatch>(
            sharedGroup.ManualCommandBatch);
        Assert.Equal(["write", "read"], sharedGroupManualBatch.DatabaseMigrationIds);
        Assert.Equal(["update"], sharedGroupManualBatch.CommandIds);
        Assert.Equal(
            string.Join(
                "\n",
                [
                    "dotnet ef database update --context ShowcaseWriteDbContext",
                    "dotnet ef database update --context ShowcaseReadDbContext"
                ]),
            sharedGroupManualBatch.CommandTemplate);
        Assert.Equal(["read"], writeStep.CoordinatedMigrationIds);
        Assert.True(writeStep.RequiresPhysicalTargetCoordination);
        Assert.Contains("separate migrations projects", writeStep.CoordinationHint!, StringComparison.Ordinal);
        Assert.Equal(["write"], readStep.CoordinatedMigrationIds);
        Assert.True(readStep.RequiresPhysicalTargetCoordination);
        Assert.False(historyStep.RequiresPhysicalTargetCoordination);

        Assert.Equal("Attention", databaseTopology.Summary.Status);
        Assert.Contains(databaseTopology.ActionPlan.Actions, action =>
            action.Id == "coordinate-shared-database-migrations" &&
            action.ActionPath == "/engine/database-migration-playbook" &&
            action.SourceRoleIds.SequenceEqual(["outbox", "read", "write"]) &&
            action.SourceMigrationIds.SequenceEqual(["read", "write"]));
        Assert.Contains(databaseTopology.Advisories, advisory =>
            advisory.Id == "shared-physical-target-migration-coordination" &&
            advisory.ActionPath == "/engine/database-migration-playbook" &&
            advisory.SourceMigrationIds.SequenceEqual(["read", "write"]));

        var response = await client.GetAsync("/api/v1/showcase/system/database-topology");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var playbookSummary = root.GetProperty("migrationPlaybook").GetProperty("summary");
        Assert.Equal(2, playbookSummary.GetProperty("coordinationRequiredTargetCount").GetInt32());
        Assert.Equal(2, playbookSummary.GetProperty("executionGroupCount").GetInt32());
        Assert.Equal(1, playbookSummary.GetProperty("coordinationRequiredGroupCount").GetInt32());
        Assert.Contains(
            root.GetProperty("migrationPlaybook").GetProperty("executionGroups").EnumerateArray().ToArray(),
            executionGroup =>
                executionGroup.GetProperty("requiresPhysicalTargetCoordination").GetBoolean() &&
                executionGroup.GetProperty("productionBatch").GetProperty("commandCount").GetInt32() == 2 &&
                executionGroup.GetProperty("localBatch").GetProperty("commandCount").GetInt32() == 2 &&
                executionGroup.GetProperty("productionCommands").EnumerateArray().Any(command => string.Equals(command.GetProperty("commandId").GetString(), "bundle", StringComparison.Ordinal)) &&
                executionGroup.GetProperty("localCommands").EnumerateArray().Any(command => string.Equals(command.GetProperty("commandId").GetString(), "update", StringComparison.Ordinal)) &&
                executionGroup.GetProperty("databaseMigrationIds").EnumerateArray().Any(item => string.Equals(item.GetString(), "write", StringComparison.Ordinal)) &&
                executionGroup.GetProperty("databaseMigrationIds").EnumerateArray().Any(item => string.Equals(item.GetString(), "read", StringComparison.Ordinal)));
        Assert.Contains(
            root.GetProperty("migrationPlaybook").GetProperty("steps").EnumerateArray().ToArray(),
            step =>
                string.Equals(step.GetProperty("targetId").GetString(), "write", StringComparison.Ordinal) &&
                step.GetProperty("requiresPhysicalTargetCoordination").GetBoolean() &&
                step.GetProperty("coordinatedMigrationIds").EnumerateArray().Any(item => string.Equals(item.GetString(), "read", StringComparison.Ordinal)));
        Assert.Contains(
            root.GetProperty("roles").EnumerateArray().ToArray(),
            role =>
                string.Equals(role.GetProperty("id").GetString(), "write", StringComparison.Ordinal) &&
                role.GetProperty("physicalCoLocatedRoles").EnumerateArray().Any(item => string.Equals(item.GetString(), "read", StringComparison.Ordinal)));
        Assert.Contains(
            root.GetProperty("insights").EnumerateArray().ToArray(),
            insight =>
                string.Equals(insight.GetProperty("id").GetString(), "shared-physical-target-migration-coordination", StringComparison.Ordinal) &&
                string.Equals(insight.GetProperty("actionPath").GetString(), "/engine/database-migration-playbook", StringComparison.Ordinal));

        var brief = await client.GetStringAsync("/api/v1/showcase/system/database-topology/brief");
        Assert.Contains("Shared-target coordination: 2 migration target(s)", brief, StringComparison.Ordinal);
        Assert.Contains("Execution groups: 2 total, 1 coordinated shared-target group(s)", brief, StringComparison.Ordinal);
        Assert.Contains("## Migration Execution Groups", brief, StringComparison.Ordinal);
        Assert.Contains("Production batch: 2 command(s) across write, read", brief, StringComparison.Ordinal);
        Assert.Contains("Production paths:", brief, StringComparison.Ordinal);
        Assert.Contains("Local fallback batch: 2 command(s) across write, read", brief, StringComparison.Ordinal);
        Assert.Contains("Local fallback paths:", brief, StringComparison.Ordinal);
        Assert.Contains("via `bundle`", brief, StringComparison.Ordinal);
        Assert.Contains("Coordination:", brief, StringComparison.Ordinal);
        Assert.Contains("separate migrations projects", brief, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleKeepsReadStoreSeparateFromDirectWriteChanges()
    {
        await using var app = BuildShowcaseForTests(configureBuilder: DisableReadModelProjectionLoop);

        await app.StartAsync();
        var client = app.GetTestClient();

        using var scope = app.Services.CreateScope();
        var writeDb = scope.ServiceProvider.GetRequiredService<ShowcaseWriteDbContext>();
        var readDb = scope.ServiceProvider.GetRequiredService<ShowcaseReadDbContext>();

        const string productId = "proj-separation-001";
        writeDb.Products.Add(new ShowcaseProductEntity
        {
            Id = productId,
            Sku = "PROJ-SEPARATION-001",
            Name = "Projection Separation Test Product",
            Description = "Verifies read/write stores stay separate until projection runs.",
            Category = "Testing",
            PriceInCents = 4242,
            Currency = "USD",
            IsActive = true,
            TagsJson = "[\"projection\",\"separation\"]",
            CreatedAtUtc = DateTime.UtcNow
        });

        await writeDb.SaveChangesAsync();

        Assert.False(await readDb.Products.AnyAsync(product => product.Id == productId));
        var response = await client.GetAsync($"/api/v1/showcase/catalog/products/{productId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShowcaseSampleCompletesProjectionJobsAndResetClearsThem()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();
        var payload = new
        {
            sku = "PROJ-JOB-001",
            name = "Projection Job Test Product",
            description = "Verifies projection jobs complete and reset clears them.",
            category = "Testing",
            priceInCents = 5151,
            currency = "USD",
            tags = new[] { "projection", "jobs" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/showcase/catalog/products", payload);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync());
        var productId = created.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(productId));

        using (var createdScope = app.Services.CreateScope())
        {
            var writeDb = createdScope.ServiceProvider.GetRequiredService<ShowcaseWriteDbContext>();
            var readDb = createdScope.ServiceProvider.GetRequiredService<ShowcaseReadDbContext>();

            var jobs = await writeDb.ReadProjectionJobs
                .Where(job => job.Scope == "products" && job.EntityKey == productId)
                .ToListAsync();

            var completedJob = Assert.Single(jobs);
            Assert.NotNull(completedJob.CompletedAtUtc);
            Assert.True(await readDb.Products.AnyAsync(product => product.Id == productId));
        }

        var resetResponse = await client.PostAsJsonAsync("/api/v1/showcase/system/reset", new { });
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        using var resetScope = app.Services.CreateScope();
        var resetWriteDb = resetScope.ServiceProvider.GetRequiredService<ShowcaseWriteDbContext>();
        var resetReadDb = resetScope.ServiceProvider.GetRequiredService<ShowcaseReadDbContext>();

        Assert.Empty(await resetWriteDb.ReadProjectionJobs.ToListAsync());
        Assert.False(await resetReadDb.Products.AnyAsync(product => product.Id == productId));
        Assert.True(await resetReadDb.Products.AnyAsync(product => product.Id == "prod-001"));
    }

    [Fact]
    public async Task ShowcaseSampleExposesConfiguredEventDispatchRuntimeCatalog()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var runtimes = await client.GetFromJsonAsync<EventDispatchRuntimeDescriptor[]>("/engine/event-dispatch-runtimes");
        var runtime = await client.GetFromJsonAsync<EventDispatchRuntimeDescriptor>("/engine/event-dispatch-runtimes/wolverine-dispatch-loop");
        var outboxes = await client.GetFromJsonAsync<OutboxDescriptor[]>("/engine/outboxes");
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(runtimes);
        var descriptor = Assert.Single(runtimes);
        Assert.Equal("wolverine-dispatch-loop", descriptor.Id);
        Assert.Equal("wolverine", descriptor.Metadata["adapter"]);
        Assert.Equal(["entity-framework-outbox"], descriptor.OutboxIds);
        Assert.False(descriptor.Summary.HasReports);
        Assert.Equal(0, descriptor.Summary.TotalReports);
        Assert.NotNull(runtime);
        Assert.Equal("wolverine-dispatch-loop", runtime.Id);
        Assert.False(runtime.Summary.HasReports);
        Assert.NotNull(outboxes);
        var outbox = Assert.Single(outboxes);
        Assert.Equal("wolverine-managed", outbox.DispatchPolicy.PolicyId);
        Assert.Equal("runtime-managed", outbox.DispatchPolicy.ExecutionMode);
        Assert.NotNull(snapshot);
        Assert.Single(snapshot.EventDispatchRuntimes);
        Assert.Equal("wolverine-dispatch-loop", snapshot.EventDispatchRuntimes[0].Id);
        Assert.Equal(0, snapshot.EventDispatchRuntimes[0].Summary.TotalReports);
    }

    [Fact]
    public async Task ShowcaseSampleDocumentsAndServesAuditHistoryThroughDurableProvider()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var openApiPayload = await client.GetStringAsync("/openapi/v1.json");
        var payload = new
        {
            sku = "AUDIT-HISTORY-001",
            name = "Audit History Test Product",
            description = "Creates an audit-history entry in the showcase sample.",
            category = "Testing",
            priceInCents = 1234,
            currency = "USD",
            tags = new[] { "audit", "history" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/showcase/catalog/products", payload);
        var queryResponse = await client.GetAsync("/api/v1/showcase/audit/history?category=catalog&limit=10");
        var queryResult = await queryResponse.Content.ReadFromJsonAsync<AuditHistoryQueryResult>();
        var auditEntry = Assert.Single(queryResult!.Entries);
        var byIdResponse = await client.GetAsync($"/api/v1/showcase/audit/history/{auditEntry.Id}");
        var byIdEntry = await byIdResponse.Content.ReadFromJsonAsync<AuditHistoryEntry>();
        var exportResponse = await client.GetAsync("/api/v1/showcase/audit/history/export?category=catalog&maxEntries=10");
        var exportBody = await exportResponse.Content.ReadAsStringAsync();
        var exportLines = exportBody
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Contains("/api/v1/showcase/audit/history", openApiPayload, StringComparison.Ordinal);
        Assert.Contains("/api/v1/showcase/audit/history/export", openApiPayload, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
        Assert.NotNull(queryResult);
        Assert.Equal(1, queryResult.TotalCount);
        Assert.Equal("catalog", auditEntry.Category);
        Assert.Equal("product-created", auditEntry.Action);
        Assert.Equal("product", auditEntry.SubjectType);
        Assert.Equal(AuditOutcome.Succeeded, auditEntry.Outcome);
        Assert.Equal(HttpStatusCode.OK, byIdResponse.StatusCode);
        Assert.NotNull(byIdEntry);
        Assert.Equal(auditEntry.Id, byIdEntry.Id);
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("application/x-ndjson; charset=utf-8", exportResponse.Content.Headers.ContentType?.ToString());
        Assert.Contains("showcase-audit-history.ndjson", exportResponse.Content.Headers.ContentDisposition?.FileName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Single(exportLines);
        Assert.Contains(auditEntry.Id, exportLines[0], StringComparison.Ordinal);
        Assert.Contains("\"category\":\"catalog\"", exportLines[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesEngineManifest()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/engine/manifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("showcase.catalog", body, StringComparison.Ordinal);
        Assert.Contains("showcase.cart", body, StringComparison.Ordinal);
        Assert.Contains("showcase.orders", body, StringComparison.Ordinal);
        Assert.Contains("showcase.inventory", body, StringComparison.Ordinal);
        Assert.Contains("showcase.shipping", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesCapabilities()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/engine/capabilities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Catalog capabilities
        Assert.Contains("showcase.catalog.read", body, StringComparison.Ordinal);
        Assert.Contains("showcase.catalog.write", body, StringComparison.Ordinal);

        // Cart capabilities
        Assert.Contains("showcase.cart.cqrs", body, StringComparison.Ordinal);
        Assert.Contains("showcase.cart.event-sourcing", body, StringComparison.Ordinal);

        // Orders capabilities
        Assert.Contains("showcase.orders.event-driven", body, StringComparison.Ordinal);

        // Inventory capabilities
        Assert.Contains("showcase.inventory.saga", body, StringComparison.Ordinal);

        // Shipping capabilities
        Assert.Contains("showcase.shipping.process-manager", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesCatalogProductsViaRestEndpoint()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/catalog/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Verify seeded products are present
        Assert.Contains("ProBook Laptop", body, StringComparison.Ordinal);
        Assert.Contains("ErgoGrip Wireless Mouse", body, StringComparison.Ordinal);
        Assert.Contains("AdjustaPro Standing Desk", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesCatalogProductByIdViaRestEndpoint()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/catalog/products/prod-001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("prod-001", body, StringComparison.Ordinal);
        Assert.Contains("LAPTOP-PRO-15", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingProduct()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/catalog/products/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShowcaseSampleExposesHealthEndpoints()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var healthResponse = await client.GetAsync("/health");
        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
    }

    [Fact]
    public async Task ShowcaseSampleExposesModulesEndpoint()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/engine/modules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // All five domain modules should be registered
        Assert.Contains("showcase.catalog", body, StringComparison.Ordinal);
        Assert.Contains("showcase.cart", body, StringComparison.Ordinal);
        Assert.Contains("showcase.orders", body, StringComparison.Ordinal);
        Assert.Contains("showcase.inventory", body, StringComparison.Ordinal);
        Assert.Contains("showcase.shipping", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesMultipleTenants()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var profile = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");

        Assert.NotNull(profile);
        Assert.True(profile.Tenancy.Enabled);
    }

    [Fact]
    public async Task ShowcaseSampleExposesAuthorizationPolicies()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/engine/authorization-policies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("showcase.viewer", body, StringComparison.Ordinal);
        Assert.Contains("showcase.customer", body, StringComparison.Ordinal);
        Assert.Contains("showcase.admin", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleSupportsCreateProductViaPost()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var payload = new
        {
            sku = "TEST-CREATE-001",
            name = "Test Product",
            description = "Created via integration test",
            category = "Testing",
            priceInCents = 1999,
            currency = "USD",
            tags = new[] { "test" }
        };
        var content = JsonContent.Create(payload);
        var response = await client.PostAsync("/api/v1/showcase/catalog/products", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("TEST-CREATE-001", body, StringComparison.Ordinal);
        Assert.Contains("Test Product", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleKeepsCommittedWritesSuccessfulWhenAuditWriterFails()
    {
        await using var app = BuildShowcaseForTests(configureBuilder: builder =>
        {
            builder.Services.AddSingleton<IAuditWriter, FailingAuditWriter>();
        });

        await app.StartAsync();
        var client = app.GetTestClient();

        var payload = new
        {
            sku = "AUDIT-FAIL-001",
            name = "Audit Failure Safe Product",
            description = "Create should still succeed when the audit writer fails.",
            category = "Testing",
            priceInCents = 2999,
            currency = "USD",
            tags = new[] { "audit", "failure" }
        };

        var response = await client.PostAsync("/api/v1/showcase/catalog/products", JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/showcase/catalog/products");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var body = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("AUDIT-FAIL-001", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleSupportsUpdateProductViaPut()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var updatePayload = new
        {
            productId = "prod-001",
            name = "ProBook Laptop 15\" Updated",
            priceInCents = 139999
        };
        var content = JsonContent.Create(updatePayload);
        var response = await client.PutAsync("/api/v1/showcase/catalog/products/prod-001", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Updated", body, StringComparison.Ordinal);
        Assert.Contains("139999", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleHandlesConcurrentReadsWithoutErrors()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Fire 20 concurrent read requests
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => client.GetAsync("/api/v1/showcase/catalog/products"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task ShowcaseSampleHandlesConcurrentWritesWithoutDataLoss()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Count initial products
        var initialResponse = await client.GetAsync("/api/v1/showcase/catalog/products");
        var initialBody = await initialResponse.Content.ReadAsStringAsync();
        var initialCount = JsonSerializer.Deserialize<JsonElement>(initialBody).GetArrayLength();

        // Fire 10 concurrent writes
        var writeTasks = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var payload = new
                {
                    sku = $"CONC-{i:D3}",
                    name = $"Concurrent Product {i}",
                    category = "ConcurrencyTest",
                    priceInCents = (i + 1) * 100,
                    currency = "USD"
                };
                return client.PostAsync("/api/v1/showcase/catalog/products", JsonContent.Create(payload));
            })
            .ToArray();

        var writeResponses = await Task.WhenAll(writeTasks);
        Assert.All(writeResponses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        // Verify all 10 were persisted
        var finalResponse = await client.GetAsync("/api/v1/showcase/catalog/products");
        var finalBody = await finalResponse.Content.ReadAsStringAsync();
        var finalCount = JsonSerializer.Deserialize<JsonElement>(finalBody).GetArrayLength();

        Assert.Equal(initialCount + 10, finalCount);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForUpdateOfMissingProduct()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var payload = new { productId = "nonexistent", name = "Ghost" };
        var response = await client.PutAsync(
            "/api/v1/showcase/catalog/products/nonexistent",
            JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  Orders domain tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleExposesOrdersListEndpoint()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync(OrdersRoutePrefix);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var orders = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, orders.ValueKind);
    }

    [Fact]
    public async Task ShowcaseSamplePlacesAndRetrievesOrder()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Place an order
        var placePayload = new
        {
            customerId = "cust-test-001",
            shippingAddress = "123 Test Street",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1, unitPriceInCents = 149999L }
            }
        };
        var placeResponse = await client.PostAsync(OrdersRoutePrefix, JsonContent.Create(placePayload));

        Assert.Equal(HttpStatusCode.Created, placeResponse.StatusCode);
        var placeBody = await placeResponse.Content.ReadAsStringAsync();
        var placed = JsonSerializer.Deserialize<JsonElement>(placeBody);
        var orderId = placed.GetProperty("orderId").GetString();
        Assert.NotNull(orderId);
        Assert.Equal("Pending", placed.GetProperty("status").GetString());

        // Retrieve the order
        var getResponse = await client.GetAsync($"{OrdersRoutePrefix}/{orderId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains(orderId, getBody, StringComparison.Ordinal);
        Assert.Contains("cust-test-001", getBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleCancelsOrder()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Place an order first
        var placePayload = new
        {
            customerId = "cust-cancel-001",
            shippingAddress = "456 Cancel Lane",
            items = new[]
            {
                new { productId = "prod-002", productName = "ErgoGrip Wireless Mouse", quantity = 2, unitPriceInCents = 4999L }
            }
        };
        var placeResponse = await client.PostAsync(OrdersRoutePrefix, JsonContent.Create(placePayload));
        var placed = JsonSerializer.Deserialize<JsonElement>(await placeResponse.Content.ReadAsStringAsync());
        var orderId = placed.GetProperty("orderId").GetString()!;

        // Cancel the order
        var cancelPayload = new { orderId, reason = "Changed my mind" };
        var cancelResponse = await client.PutAsync(
            $"{OrdersRoutePrefix}/{orderId}/cancel",
            JsonContent.Create(cancelPayload));

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelBody = await cancelResponse.Content.ReadAsStringAsync();
        Assert.Contains("Cancelled", cancelBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingOrder()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync($"{OrdersRoutePrefix}/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  Inventory domain tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleExposesInventoryListEndpoint()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/inventory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("prod-001", body, StringComparison.Ordinal);
        Assert.Contains("WH-01", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesInventoryByProductId()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/inventory/prod-005");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("prod-005", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReservesStock()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var payload = new
        {
            orderId = "ord-reserve-001",
            items = new[]
            {
                new { productId = "prod-001", quantity = 2 },
                new { productId = "prod-002", quantity = 5 }
            }
        };
        var response = await client.PostAsync("/api/v1/showcase/inventory/reserve", JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.True(result.GetProperty("allReserved").GetBoolean());
    }

    [Fact]
    public async Task ShowcaseSampleReserveStockPromotesOrderToConfirmed()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var orderPayload = new
        {
            customerId = "cust-reserve-status",
            shippingAddress = "12 Reserve Way",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1, unitPriceInCents = 149999L }
            }
        };
        var orderResponse = await client.PostAsync(OrdersRoutePrefix, JsonContent.Create(orderPayload));
        var orderResult = JsonSerializer.Deserialize<JsonElement>(await orderResponse.Content.ReadAsStringAsync());
        var orderId = orderResult.GetProperty("orderId").GetString()!;

        var reservePayload = new
        {
            orderId,
            items = new[]
            {
                new { productId = "prod-001", quantity = 1 }
            }
        };

        var reserveResponse = await client.PostAsync("/api/v1/showcase/inventory/reserve", JsonContent.Create(reservePayload));
        Assert.Equal(HttpStatusCode.OK, reserveResponse.StatusCode);

        var getOrderResponse = await client.GetAsync($"{OrdersRoutePrefix}/{orderId}");
        Assert.Equal(HttpStatusCode.OK, getOrderResponse.StatusCode);
        var orderBody = await getOrderResponse.Content.ReadAsStringAsync();
        Assert.Contains("Confirmed", orderBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingInventoryItem()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/inventory/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  Shipping domain tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleExposesShipmentsListEndpoint()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/shipping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var shipments = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, shipments.ValueKind);
    }

    [Fact]
    public async Task ShowcaseSampleInitiatesAndTracksShipment()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Initiate a shipment
        var initiatePayload = new
        {
            orderId = "ord-ship-001",
            destinationAddress = "789 Delivery Ave",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1 }
            }
        };
        var initiateResponse = await client.PostAsync("/api/v1/showcase/shipping", JsonContent.Create(initiatePayload));

        Assert.Equal(HttpStatusCode.Created, initiateResponse.StatusCode);
        var initiateBody = await initiateResponse.Content.ReadAsStringAsync();
        var initiated = JsonSerializer.Deserialize<JsonElement>(initiateBody);
        var shipmentId = initiated.GetProperty("shipmentId").GetString();
        Assert.NotNull(shipmentId);
        Assert.Equal("LabelCreated", initiated.GetProperty("status").GetString());

        // Track the shipment
        var trackResponse = await client.GetAsync($"/api/v1/showcase/shipping/{shipmentId}");
        Assert.Equal(HttpStatusCode.OK, trackResponse.StatusCode);
        var trackBody = await trackResponse.Content.ReadAsStringAsync();
        Assert.Contains(shipmentId, trackBody, StringComparison.Ordinal);
        Assert.Contains("Showcase Express", trackBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleConfirmsDelivery()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Initiate
        var initiatePayload = new
        {
            orderId = "ord-deliver-001",
            destinationAddress = "321 Complete St",
            items = new[] { new { productId = "prod-003", productName = "AdjustaPro Standing Desk", quantity = 1 } }
        };
        var initiateResponse = await client.PostAsync("/api/v1/showcase/shipping", JsonContent.Create(initiatePayload));
        var initiated = JsonSerializer.Deserialize<JsonElement>(await initiateResponse.Content.ReadAsStringAsync());
        var shipmentId = initiated.GetProperty("shipmentId").GetString()!;

        // Confirm delivery
        var deliverPayload = new { shipmentId, recipientName = "Test User" };
        var deliverResponse = await client.PutAsync(
            $"/api/v1/showcase/shipping/{shipmentId}/deliver",
            JsonContent.Create(deliverPayload));

        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        var deliverBody = await deliverResponse.Content.ReadAsStringAsync();
        Assert.Contains("Delivered", deliverBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleShippingPromotesOrderToProcessingAndRejectsDuplicateShipment()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var orderPayload = new
        {
            customerId = "cust-ship-status",
            shippingAddress = "45 Shipping Way",
            items = new[]
            {
                new { productId = "prod-003", productName = "AdjustaPro Standing Desk", quantity = 1, unitPriceInCents = 59999L }
            }
        };
        var orderResponse = await client.PostAsync(OrdersRoutePrefix, JsonContent.Create(orderPayload));
        var orderResult = JsonSerializer.Deserialize<JsonElement>(await orderResponse.Content.ReadAsStringAsync());
        var orderId = orderResult.GetProperty("orderId").GetString()!;

        var initiatePayload = new
        {
            orderId,
            destinationAddress = "45 Shipping Way",
            items = new[] { new { productId = "prod-003", productName = "AdjustaPro Standing Desk", quantity = 1 } }
        };

        var firstShipmentResponse = await client.PostAsync("/api/v1/showcase/shipping", JsonContent.Create(initiatePayload));
        Assert.Equal(HttpStatusCode.Created, firstShipmentResponse.StatusCode);

        var duplicateShipmentResponse = await client.PostAsync("/api/v1/showcase/shipping", JsonContent.Create(initiatePayload));
        Assert.Equal(HttpStatusCode.Conflict, duplicateShipmentResponse.StatusCode);

        var getOrderResponse = await client.GetAsync($"{OrdersRoutePrefix}/{orderId}");
        Assert.Equal(HttpStatusCode.OK, getOrderResponse.StatusCode);
        var orderBody = await getOrderResponse.Content.ReadAsStringAsync();
        Assert.Contains("Processing", orderBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingShipment()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/shipping/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  Cart domain tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleAddsItemToCartAndRetrievesCart()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Add item to cart
        var addPayload = new
        {
            cartId = "cart-test-001",
            customerId = "cust-cart-001",
            productId = "prod-001",
            productName = "ProBook Laptop 15\"",
            quantity = 1,
            priceInCents = 149999L
        };
        var addResponse = await client.PostAsync("/api/v1/showcase/cart/cart-test-001/items", JsonContent.Create(addPayload));

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var addBody = await addResponse.Content.ReadAsStringAsync();
        var added = JsonSerializer.Deserialize<JsonElement>(addBody).GetProperty("data");
        Assert.Equal(1, added.GetProperty("itemCount").GetInt32());
        Assert.Equal(149999, added.GetProperty("totalInCents").GetInt64());

        // Retrieve cart
        var getResponse = await client.GetAsync("/api/v1/showcase/cart/cart-test-001");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains("cart-test-001", getBody, StringComparison.Ordinal);
        Assert.Contains("ProBook Laptop", getBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleAddToCartBehaviorProjectsValidationFaultCollectionsWhenRestEnvelopeEnabled()
    {
        await using var app = BuildShowcaseWithRestEnvelope();

        await app.StartAsync();
        var client = app.GetTestClient();

        var invalidPayload = new
        {
            cartId = "cart-invalid-001",
            customerId = "",
            productId = "",
            productName = "",
            quantity = 0,
            priceInCents = -1L
        };

        var response = await client.PostAsync("/api/v1/showcase/cart/cart-invalid-001/items", JsonContent.Create(invalidPayload));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.False(payload.GetProperty("success").GetBoolean());
        Assert.Equal(400, payload.GetProperty("status_code").GetInt32());
        Assert.Equal("Cart add-item request is invalid.", payload.GetProperty("message").GetString());

        var errors = payload.GetProperty("errors");
        Assert.Equal(5, errors.GetArrayLength());
        Assert.Contains(errors.EnumerateArray(), error => error.GetProperty("key").GetString() == "showcase.cart.add_item.customer_id.required");
        Assert.Contains(errors.EnumerateArray(), error => error.GetProperty("key").GetString() == "showcase.cart.add_item.product_id.required");
        Assert.Contains(errors.EnumerateArray(), error => error.GetProperty("key").GetString() == "showcase.cart.add_item.product_name.required");
        Assert.Contains(errors.EnumerateArray(), error => error.GetProperty("key").GetString() == "showcase.cart.add_item.quantity.invalid");
        Assert.Contains(errors.EnumerateArray(), error => error.GetProperty("key").GetString() == "showcase.cart.add_item.price.invalid");
    }

    [Fact]
    public async Task ShowcaseSampleAddToCartBehaviorReturnsConflictAfterCheckoutWhenRestEnvelopeEnabled()
    {
        await using var app = BuildShowcaseWithRestEnvelope();

        await app.StartAsync();
        var client = app.GetTestClient();

        var addPayload = new
        {
            cartId = "cart-locked-001",
            customerId = "cust-locked",
            productId = "prod-006",
            productName = "TypeMaster Mechanical Keyboard",
            quantity = 1,
            priceInCents = 12999L
        };
        await client.PostAsync("/api/v1/showcase/cart/cart-locked-001/items", JsonContent.Create(addPayload));

        var checkoutPayload = new { cartId = "cart-locked-001", shippingAddress = "123 Checkout Blvd" };
        var checkoutResponse = await client.PostAsync(
            "/api/v1/showcase/cart/cart-locked-001/checkout",
            JsonContent.Create(checkoutPayload));
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);

        var retryPayload = new
        {
            cartId = "cart-locked-001",
            customerId = "cust-locked",
            productId = "prod-007",
            productName = "NoiseBlock Headphones",
            quantity = 1,
            priceInCents = 19999L
        };

        var conflictResponse = await client.PostAsync("/api/v1/showcase/cart/cart-locked-001/items", JsonContent.Create(retryPayload));

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        var conflictBody = await conflictResponse.Content.ReadAsStringAsync();
        var conflictPayload = JsonSerializer.Deserialize<JsonElement>(conflictBody);
        Assert.False(conflictPayload.GetProperty("success").GetBoolean());
        Assert.Equal(409, conflictPayload.GetProperty("status_code").GetInt32());
        Assert.Equal($"Cart 'cart-locked-001' has already been checked out.", conflictPayload.GetProperty("message").GetString());

        var errors = conflictPayload.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("showcase.cart.add_item.checked_out", errors[0].GetProperty("key").GetString());
    }

    [Fact]
    public async Task ShowcaseSampleRemovesItemFromCart()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Add two items
        var item1 = new { cartId = "cart-remove-001", customerId = "cust-rm", productId = "prod-001",
            productName = "Laptop", quantity = 1, priceInCents = 149999L };
        await client.PostAsync("/api/v1/showcase/cart/cart-remove-001/items", JsonContent.Create(item1));

        var item2 = new { cartId = "cart-remove-001", customerId = "cust-rm", productId = "prod-002",
            productName = "Mouse", quantity = 1, priceInCents = 4999L };
        await client.PostAsync("/api/v1/showcase/cart/cart-remove-001/items", JsonContent.Create(item2));

        // Remove one
        var removeResponse = await client.DeleteAsync("/api/v1/showcase/cart/cart-remove-001/items/prod-001");

        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        var body = await removeResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body).GetProperty("data");
        Assert.Equal(1, result.GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task ShowcaseSampleChecksOutCart()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Add item
        var addPayload = new { cartId = "cart-checkout-001", customerId = "cust-co",
            productId = "prod-006", productName = "TypeMaster Mechanical Keyboard",
            quantity = 1, priceInCents = 12999L };
        await client.PostAsync("/api/v1/showcase/cart/cart-checkout-001/items", JsonContent.Create(addPayload));

        // Checkout
        var checkoutPayload = new { cartId = "cart-checkout-001", shippingAddress = "123 Checkout Blvd" };
        var checkoutResponse = await client.PostAsync(
            "/api/v1/showcase/cart/cart-checkout-001/checkout",
            JsonContent.Create(checkoutPayload));

        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);
        var body = await checkoutResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body).GetProperty("data");
        Assert.StartsWith("ord-", result.GetProperty("orderId").GetString()!);
        Assert.Equal(12999, result.GetProperty("totalInCents").GetInt64());
    }

    [Fact]
    public async Task ShowcaseSamplePlacesOrderUsingCheckoutGeneratedOrderId()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var addPayload = new
        {
            cartId = "cart-linked-001",
            customerId = "cust-linked",
            productId = "prod-006",
            productName = "TypeMaster Mechanical Keyboard",
            quantity = 1,
            priceInCents = 12999L
        };
        await client.PostAsync("/api/v1/showcase/cart/cart-linked-001/items", JsonContent.Create(addPayload));

        var checkoutPayload = new { cartId = "cart-linked-001", shippingAddress = "123 Checkout Blvd" };
        var checkoutResponse = await client.PostAsync(
            "/api/v1/showcase/cart/cart-linked-001/checkout",
            JsonContent.Create(checkoutPayload));
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);

        var checkoutBody = JsonSerializer.Deserialize<JsonElement>(await checkoutResponse.Content.ReadAsStringAsync());
        var checkoutData = checkoutBody.GetProperty("data");
        var linkedOrderId = checkoutData.GetProperty("orderId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(linkedOrderId));

        var orderPayload = new
        {
            orderId = linkedOrderId,
            customerId = "cust-linked",
            shippingAddress = "123 Checkout Blvd",
            items = new[]
            {
                new
                {
                    productId = "prod-006",
                    productName = "TypeMaster Mechanical Keyboard",
                    quantity = 1,
                    unitPriceInCents = 12999L
                }
            }
        };

        var orderResponse = await client.PostAsync(OrdersRoutePrefix, JsonContent.Create(orderPayload));

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var orderBody = JsonSerializer.Deserialize<JsonElement>(await orderResponse.Content.ReadAsStringAsync());
        Assert.Equal(linkedOrderId, orderBody.GetProperty("orderId").GetString());

        var getOrderResponse = await client.GetAsync($"{OrdersRoutePrefix}/{linkedOrderId}");
        Assert.Equal(HttpStatusCode.OK, getOrderResponse.StatusCode);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingCart()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/cart/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShowcaseSampleOpenApiUsesBehaviorSummaryAndRemarksWithoutDuplicatingSummary()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var getOperation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/showcase/cart/{cartId}")
            .GetProperty("get");

        var summary = getOperation.GetProperty("summary").GetString();
        var description = getOperation.GetProperty("description").GetString();

        Assert.NotNull(summary);
        Assert.NotNull(description);
        Assert.Equal("Retrieve the current shopping cart state.", summary);
        Assert.Equal(
            "Uses the CQRS query side. Rebuilds the cart from the event stream on every read.",
            description);
        Assert.DoesNotContain(summary!, description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesSystemSummaryProjectionForOperatorConsole()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/system/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Started", root.GetProperty("runtime").GetProperty("status").GetString());
        Assert.Equal("/scalar/v1", root.GetProperty("documentation").GetProperty("scalarPath").GetString());
        Assert.Equal("/openapi/v1.json", root.GetProperty("documentation").GetProperty("openApiJsonPath").GetString());
        Assert.Equal("/api/v1/showcase/system/database-topology", root.GetProperty("documentation").GetProperty("databaseTopologyPath").GetString());
        Assert.Equal("/api/v1/showcase/system/database-topology/brief", root.GetProperty("documentation").GetProperty("databaseTopologyBriefPath").GetString());
        Assert.Equal("/api/v1/showcase/system/database-topology/handoff", root.GetProperty("documentation").GetProperty("databaseTopologyHandoffPath").GetString());
        Assert.True(root.GetProperty("business").GetProperty("activeProducts").GetInt32() >= 10);
        Assert.True(root.GetProperty("suggestedJourneys").GetArrayLength() > 0);
    }

    [Fact]
    public async Task ShowcaseSampleExposesDatabaseTopologyProjectionForOperatorConsole()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/showcase/catalog/products", new
        {
            sku = "TOPOLOGY-PROJ-001",
            name = "Topology Projection Product",
            description = "Exercises the database-topology operator projection.",
            category = "Testing",
            priceInCents = 6161,
            currency = "USD",
            tags = DatabaseTopologyProjectionTags
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await client.GetAsync("/api/v1/showcase/system/database-topology");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var engineTopology = await client.GetFromJsonAsync<DatabaseTopologyOperationalSnapshot>("/engine/database-topology");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var summary = root.GetProperty("summary");

        Assert.Equal(4, summary.GetProperty("roleCount").GetInt32());
        Assert.Equal(4, summary.GetProperty("healthyRoleCount").GetInt32());
        Assert.Equal(3, summary.GetProperty("migrationTargetCount").GetInt32());
        Assert.Equal(3, summary.GetProperty("succeededMigrationTargetCount").GetInt32());
        Assert.True(summary.GetProperty("readModelSyncEnabled").GetBoolean());
        Assert.Equal("InMemory", summary.GetProperty("writeProvider").GetString());
        Assert.Equal("InMemory", summary.GetProperty("readProvider").GetString());
        Assert.Equal("InMemory", summary.GetProperty("historyProvider").GetString());

        var readiness = root.GetProperty("readiness");
        Assert.Equal("Ready", readiness.GetProperty("state").GetString());
        Assert.Equal("Database topology is ready", readiness.GetProperty("headline").GetString());
        Assert.Equal("/engine/snapshot", readiness.GetProperty("actionPath").GetString());
        Assert.NotNull(engineTopology);
        Assert.Equal(engineTopology.Summary.Detail, readiness.GetProperty("detail").GetString());

        var actionPlan = root.GetProperty("actionPlan");
        var actionPlanSummary = actionPlan.GetProperty("summary");
        Assert.Equal(1, actionPlanSummary.GetProperty("totalActionCount").GetInt32());
        Assert.Equal(0, actionPlanSummary.GetProperty("blockingActionCount").GetInt32());
        Assert.Equal(0, actionPlanSummary.GetProperty("attentionActionCount").GetInt32());
        Assert.Equal(1, actionPlanSummary.GetProperty("readyActionCount").GetInt32());

        var actionPlanActions = actionPlan.GetProperty("actions").EnumerateArray().ToArray();
        Assert.Contains(
            actionPlanActions,
            action =>
                action.GetProperty("order").GetInt32() == 1 &&
                string.Equals(action.GetProperty("id").GetString(), "topology-ready-for-validation", StringComparison.Ordinal) &&
                string.Equals(action.GetProperty("category").GetString(), "topology-posture", StringComparison.Ordinal) &&
                string.Equals(action.GetProperty("tone").GetString(), "Success", StringComparison.Ordinal) &&
                string.Equals(action.GetProperty("actionPath").GetString(), "/engine/snapshot", StringComparison.Ordinal) &&
                action.GetProperty("sourceRoleIds").EnumerateArray().Select(static item => item.GetString()).SequenceEqual(DatabaseTopologyReadyActionSourceRoleIds) &&
                action.GetProperty("sourceMigrationIds").EnumerateArray().Select(static item => item.GetString()).SequenceEqual(DatabaseTopologyReadyActionSourceMigrationIds) &&
                action.GetProperty("completionSignal").GetString()!.Contains("No remediation", StringComparison.Ordinal));

        var roles = root.GetProperty("roles").EnumerateArray().ToArray();
        var readRole = Assert.Single(
            roles,
            role => string.Equals(role.GetProperty("id").GetString(), "read", StringComparison.Ordinal));
        Assert.Equal("InMemory", readRole.GetProperty("provider").GetString());
        Assert.Equal("Healthy", readRole.GetProperty("healthState").GetString());
        Assert.True(readRole.GetProperty("probeCacheEnabled").GetBoolean());
        Assert.Equal(30, readRole.GetProperty("probeFreshnessSeconds").GetInt32());
        Assert.Equal("configured", readRole.GetProperty("probeFreshnessOrigin").GetString());
        Assert.Equal("live", readRole.GetProperty("probeSource").GetString());
        Assert.True(readRole.GetProperty("observedAtUtc").ValueKind == JsonValueKind.String);
        Assert.True(readRole.GetProperty("probeFreshUntilUtc").ValueKind == JsonValueKind.String);
        Assert.Equal(0, readRole.GetProperty("probeAgeSeconds").GetInt32());
        Assert.Equal("0", readRole.GetProperty("runtimeMetadataPreview").GetProperty("pendingMigrationCount").GetString());

        var migrations = root.GetProperty("migrations").EnumerateArray().ToArray();
        Assert.Contains(
            migrations,
            migration =>
                string.Equals(migration.GetProperty("id").GetString(), "read", StringComparison.Ordinal) &&
                string.Equals(migration.GetProperty("status").GetString(), "Succeeded", StringComparison.Ordinal) &&
                string.Equals(migration.GetProperty("executionMode").GetString(), "startup-hosted-service", StringComparison.Ordinal) &&
                string.Equals(migration.GetProperty("roleHealthState").GetString(), "Healthy", StringComparison.Ordinal) &&
                string.Equals(migration.GetProperty("roleMigrationState").GetString(), "succeeded", StringComparison.Ordinal) &&
                migration.GetProperty("roleObservedAtUtc").ValueKind == JsonValueKind.String &&
                string.Equals(
                    migration.GetProperty("metadataPreview").GetProperty("roleRuntime.probeOutcome").GetString(),
                    "succeeded",
                    StringComparison.Ordinal));
        Assert.Contains(
            migrations,
            migration =>
            {
                if (!string.Equals(migration.GetProperty("id").GetString(), "history", StringComparison.Ordinal))
                {
                    return false;
                }

                if (migration.GetProperty("recommendedExecutionOrder").GetInt32() != 3)
                {
                    return false;
                }

                var commands = migration.GetProperty("commands").EnumerateArray().ToArray();
                if (!string.Equals(migration.GetProperty("roleHealthState").GetString(), "Healthy", StringComparison.Ordinal) ||
                    !string.Equals(migration.GetProperty("roleMigrationState").GetString(), "succeeded", StringComparison.Ordinal) ||
                    migration.GetProperty("roleObservedAtUtc").ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                return commands.Any(command =>
                    string.Equals(command.GetProperty("id").GetString(), "bundle", StringComparison.Ordinal) &&
                    string.Equals(command.GetProperty("displayName").GetString(), "EF Core migration bundle", StringComparison.Ordinal) &&
                    command.GetProperty("recommendedForProduction").GetBoolean() &&
                    string.Equals(command.GetProperty("toolId").GetString(), "dotnet-ef", StringComparison.Ordinal) &&
                    string.Equals(command.GetProperty("executionCategory").GetString(), "deploy-time", StringComparison.Ordinal) &&
                    string.Equals(command.GetProperty("workingDirectoryHint").GetString(), "startup-project", StringComparison.Ordinal) &&
                    string.Equals(command.GetProperty("sampleCommandHint").GetString(), "Run from the repository root, or adapt the project paths for another host layout.", StringComparison.Ordinal) &&
                    string.Equals(command.GetProperty("sampleCommand").GetString(), "dotnet ef migrations bundle --context ShowcaseAuditHistoryDbContext --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj", StringComparison.Ordinal) &&
                    command.GetProperty("description").GetString()!.Contains("'history'", StringComparison.Ordinal) &&
                    string.Equals(command.GetProperty("metadataPreview").GetProperty("tool").GetString(), "dotnet-ef", StringComparison.Ordinal));
            });

        var readModelSync = root.GetProperty("readModelSync");
        Assert.True(readModelSync.GetProperty("enabled").GetBoolean());
        Assert.False(readModelSync.GetProperty("isLagging").GetBoolean());
        var insights = root.GetProperty("insights").EnumerateArray().ToArray();
        Assert.Contains(
            insights,
            insight =>
                string.Equals(insight.GetProperty("id").GetString(), "topology-aligned", StringComparison.Ordinal) &&
                string.Equals(insight.GetProperty("tone").GetString(), "Success", StringComparison.Ordinal) &&
                string.Equals(insight.GetProperty("actionPath").GetString(), "/engine/snapshot", StringComparison.Ordinal));
        Assert.Contains(
            insights,
            insight =>
                string.Equals(insight.GetProperty("id").GetString(), "migration-production-guidance", StringComparison.Ordinal) &&
                string.Equals(insight.GetProperty("tone").GetString(), "Success", StringComparison.Ordinal) &&
                string.Equals(insight.GetProperty("actionPath").GetString(), "/engine/database-migrations", StringComparison.Ordinal) &&
                insight.GetProperty("detail").GetString()!.Contains("production-recommended bundle or script guidance", StringComparison.Ordinal));

        var migrationPlaybook = root.GetProperty("migrationPlaybook");
        var playbookSummary = migrationPlaybook.GetProperty("summary");
        Assert.Equal(3, playbookSummary.GetProperty("targetCount").GetInt32());
        Assert.Equal(3, playbookSummary.GetProperty("productionReadyTargetCount").GetInt32());
        Assert.Equal(3, playbookSummary.GetProperty("localFallbackTargetCount").GetInt32());
        Assert.Equal(3, playbookSummary.GetProperty("applyOnStartupTargetCount").GetInt32());

        var playbookSteps = migrationPlaybook.GetProperty("steps").EnumerateArray().ToArray();
        Assert.Equal(3, playbookSteps.Length);
        Assert.Equal("write", playbookSteps[0].GetProperty("targetId").GetString());
        Assert.Equal("read", playbookSteps[1].GetProperty("targetId").GetString());
        Assert.Equal("history", playbookSteps[2].GetProperty("targetId").GetString());
        Assert.Equal("bundle", playbookSteps[2].GetProperty("productionCommandId").GetString());
        Assert.Equal("update", playbookSteps[2].GetProperty("localCommandId").GetString());
        Assert.Equal(
            "dotnet ef migrations bundle --context ShowcaseAuditHistoryDbContext --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj",
            playbookSteps[2].GetProperty("productionSampleCommand").GetString());
        var executionGroups = migrationPlaybook.GetProperty("executionGroups").EnumerateArray().ToArray();
        Assert.Equal(3, executionGroups.Length);
        Assert.Equal("bundle", executionGroups[0].GetProperty("productionCommands").EnumerateArray().Single().GetProperty("commandId").GetString());
        Assert.Equal("update", executionGroups[2].GetProperty("localCommands").EnumerateArray().Single().GetProperty("commandId").GetString());
        Assert.Equal("production", executionGroups[0].GetProperty("productionBatch").GetProperty("batchId").GetString());
        Assert.Equal(
            "dotnet ef migrations bundle --context ShowcaseWriteDbContext --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj",
            executionGroups[0].GetProperty("productionBatch").GetProperty("sampleCommandBatch").GetString());
        Assert.Equal("manual", executionGroups[2].GetProperty("localBatch").GetProperty("batchId").GetString());
        Assert.Equal(
            "dotnet ef database update --context ShowcaseAuditHistoryDbContext --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj",
            executionGroups[2].GetProperty("localBatch").GetProperty("sampleCommandBatch").GetString());

        var writeStore = readModelSync.GetProperty("writeStore");
        var readStore = readModelSync.GetProperty("readStore");
        Assert.Equal(writeStore.GetProperty("products").GetInt32(), readStore.GetProperty("products").GetInt32());
        Assert.Equal(writeStore.GetProperty("inventory").GetInt32(), readStore.GetProperty("inventory").GetInt32());

        var jobs = readModelSync.GetProperty("jobs");
        Assert.True(jobs.GetProperty("totalJobs").GetInt32() >= 1);
        Assert.True(jobs.GetProperty("completedJobs").GetInt32() >= 1);
        Assert.Equal(0, jobs.GetProperty("pendingJobs").GetInt32());
        Assert.Equal(0, jobs.GetProperty("failedJobs").GetInt32());
        Assert.True(jobs.GetProperty("distinctScopes").GetInt32() >= 1);

        var scopes = readModelSync.GetProperty("scopes").EnumerateArray().ToArray();
        Assert.Contains(
            scopes,
            scope =>
                string.Equals(scope.GetProperty("scope").GetString(), "products", StringComparison.Ordinal) &&
                scope.GetProperty("completedJobs").GetInt32() >= 1);

        var briefResponse = await client.GetAsync("/api/v1/showcase/system/database-topology/brief");

        Assert.Equal(HttpStatusCode.OK, briefResponse.StatusCode);
        Assert.Equal("text/markdown", briefResponse.Content.Headers.ContentType?.MediaType);

        var brief = await briefResponse.Content.ReadAsStringAsync();
        Assert.Contains("# Database Topology Operator Brief", brief, StringComparison.Ordinal);
        Assert.Contains("Readiness: **Ready**", brief, StringComparison.Ordinal);
        Assert.Contains("1. Topology is ready for operator validation", brief, StringComparison.Ordinal);
        Assert.Contains("Showcase projection JSON: `/api/v1/showcase/system/database-topology`", brief, StringComparison.Ordinal);

        var handoffResponse = await client.GetAsync("/api/v1/showcase/system/database-topology/handoff");

        Assert.Equal(HttpStatusCode.OK, handoffResponse.StatusCode);
        Assert.Equal("application/zip", handoffResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("database-topology-handoff.zip", handoffResponse.Content.Headers.ContentDisposition?.ToString(), StringComparison.Ordinal);

        using var handoffStream = new MemoryStream(await handoffResponse.Content.ReadAsByteArrayAsync());
        using var handoffArchive = new ZipArchive(handoffStream, ZipArchiveMode.Read);
        Assert.NotNull(handoffArchive.GetEntry("README.md"));
        Assert.NotNull(handoffArchive.GetEntry("database-topology-brief.md"));
        Assert.NotNull(handoffArchive.GetEntry("database-topology-projection.json"));
        Assert.NotNull(handoffArchive.GetEntry("handoff-manifest.json"));

        var archivedReadme = await ReadZipEntryAsStringAsync(handoffArchive, "README.md");
        Assert.Contains("# Database Topology Handoff Package", archivedReadme, StringComparison.Ordinal);
        Assert.Contains("State: `Ready`", archivedReadme, StringComparison.Ordinal);
        Assert.Contains("`handoff-manifest.json`", archivedReadme, StringComparison.Ordinal);
        Assert.Contains("Showcase projection JSON: `/api/v1/showcase/system/database-topology`", archivedReadme, StringComparison.Ordinal);

        var archivedBrief = await ReadZipEntryAsStringAsync(handoffArchive, "database-topology-brief.md");
        Assert.Contains("Readiness: **Ready**", archivedBrief, StringComparison.Ordinal);

        var archivedProjection = await ReadZipEntryAsStringAsync(handoffArchive, "database-topology-projection.json");
        using var archivedProjectionDocument = JsonDocument.Parse(archivedProjection);
        Assert.Equal("Ready", archivedProjectionDocument.RootElement.GetProperty("readiness").GetProperty("state").GetString());
        Assert.Equal(4, archivedProjectionDocument.RootElement.GetProperty("summary").GetProperty("roleCount").GetInt32());
        Assert.Contains(
            archivedProjectionDocument.RootElement.GetProperty("migrations").EnumerateArray(),
            migration =>
                string.Equals(migration.GetProperty("id").GetString(), "history", StringComparison.Ordinal) &&
                string.Equals(migration.GetProperty("roleHealthState").GetString(), "Healthy", StringComparison.Ordinal) &&
                string.Equals(migration.GetProperty("roleMigrationState").GetString(), "succeeded", StringComparison.Ordinal) &&
                migration.GetProperty("roleObservedAtUtc").ValueKind == JsonValueKind.String);

        var archivedManifest = await ReadZipEntryAsStringAsync(handoffArchive, "handoff-manifest.json");
        using var archivedManifestDocument = JsonDocument.Parse(archivedManifest);
        Assert.Equal("showcase.database-topology.handoff", archivedManifestDocument.RootElement.GetProperty("packageId").GetString());
        Assert.Equal("1.0", archivedManifestDocument.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal("sample-operator-handoff", archivedManifestDocument.RootElement.GetProperty("scope").GetString());
        Assert.Equal("Ready", archivedManifestDocument.RootElement.GetProperty("readiness").GetProperty("state").GetString());
        Assert.Equal("/api/v1/showcase/system/database-topology", archivedManifestDocument.RootElement.GetProperty("sourceRoutes").GetProperty("projection").GetString());
        Assert.Equal("/api/v1/showcase/system/database-topology/brief", archivedManifestDocument.RootElement.GetProperty("sourceRoutes").GetProperty("brief").GetString());
        Assert.Equal("/api/v1/showcase/system/database-topology/handoff", archivedManifestDocument.RootElement.GetProperty("sourceRoutes").GetProperty("handoff").GetString());
        Assert.Equal("/engine/database-topology", archivedManifestDocument.RootElement.GetProperty("sourceRoutes").GetProperty("databaseTopology").GetString());
        Assert.Equal("/engine/database-roles", archivedManifestDocument.RootElement.GetProperty("sourceRoutes").GetProperty("databaseRoles").GetString());
        Assert.Equal("/engine/database-migrations", archivedManifestDocument.RootElement.GetProperty("sourceRoutes").GetProperty("databaseMigrations").GetString());
        Assert.Equal("/engine/snapshot", archivedManifestDocument.RootElement.GetProperty("sourceRoutes").GetProperty("runtimeSnapshot").GetString());
        Assert.Equal(4, archivedManifestDocument.RootElement.GetProperty("contents").GetArrayLength());
        Assert.Contains(
            archivedManifestDocument.RootElement.GetProperty("contents").EnumerateArray(),
            entry =>
                string.Equals(entry.GetProperty("fileName").GetString(), "README.md", StringComparison.Ordinal) &&
                entry.GetProperty("recommendedReviewOrder").GetInt32() == 1);
        Assert.Contains(
            archivedManifestDocument.RootElement.GetProperty("contents").EnumerateArray(),
            entry =>
                string.Equals(entry.GetProperty("fileName").GetString(), "handoff-manifest.json", StringComparison.Ordinal) &&
                entry.GetProperty("recommendedReviewOrder").GetInt32() == 3);
    }

    [Fact]
    public async Task ShowcaseSampleDatabaseTopologyProjectionFlagsReadModelDriftWithOperatorInsight()
    {
        await using var app = BuildShowcaseForTests(configureBuilder: DisableReadModelProjectionLoop);

        await app.StartAsync();

        using var scope = app.Services.CreateScope();
        var writeDb = scope.ServiceProvider.GetRequiredService<ShowcaseWriteDbContext>();
        var readDb = scope.ServiceProvider.GetRequiredService<ShowcaseReadDbContext>();
        var projectionServiceType = typeof(ShowcaseSampleApp).Assembly.GetType(
            "Cephalon.Sample.Showcase.Infrastructure.ShowcaseSystemProjectionService",
            throwOnError: true)!;
        var projections = ServiceProviderServiceExtensions.GetRequiredService(scope.ServiceProvider, projectionServiceType);

        const string productId = "topology-drift-001";
        writeDb.Products.Add(new ShowcaseProductEntity
        {
            Id = productId,
            Sku = "TOPOLOGY-DRIFT-001",
            Name = "Topology Drift Product",
            Description = "Verifies the database-topology operator projection flags read-model drift.",
            Category = "Testing",
            PriceInCents = 7171,
            Currency = "USD",
            IsActive = true,
            TagsJson = "[\"database-topology\",\"drift\"]",
            CreatedAtUtc = DateTime.UtcNow
        });

        await writeDb.SaveChangesAsync();
        writeDb.ReadProjectionJobs.Add(new ShowcaseReadProjectionJobEntity
        {
            Scope = "products",
            EntityKey = productId,
            CreatedAtUtc = DateTime.UtcNow,
            AvailableAtUtc = DateTime.UtcNow
        });
        await writeDb.SaveChangesAsync();
        Assert.True(await writeDb.Products.AnyAsync(product => product.Id == productId));
        Assert.True(await writeDb.ReadProjectionJobs.AnyAsync(job => job.EntityKey == productId));

        var projection = await InvokeAsyncWithResult(projections, "GetDatabaseTopologyAsync");
        using var projectionDocument = JsonDocument.Parse(
            JsonSerializer.Serialize(
                projection,
                projection.GetType(),
                WebJsonOptions));
        var root = projectionDocument.RootElement;
        var readiness = root.GetProperty("readiness");
        Assert.Equal("Attention", readiness.GetProperty("state").GetString());
        Assert.Equal("Read-model catch-up is still in progress", readiness.GetProperty("headline").GetString());
        Assert.Equal("/api/v1/showcase/system/database-topology", readiness.GetProperty("actionPath").GetString());
        var actionPlan = root.GetProperty("actionPlan");
        var actionPlanSummary = actionPlan.GetProperty("summary");
        Assert.Equal(1, actionPlanSummary.GetProperty("totalActionCount").GetInt32());
        Assert.Equal(0, actionPlanSummary.GetProperty("blockingActionCount").GetInt32());
        Assert.Equal(1, actionPlanSummary.GetProperty("attentionActionCount").GetInt32());
        Assert.Equal(0, actionPlanSummary.GetProperty("readyActionCount").GetInt32());
        Assert.Contains(
            actionPlan.GetProperty("actions").EnumerateArray().ToArray(),
            action =>
                action.GetProperty("order").GetInt32() == 1 &&
                string.Equals(action.GetProperty("id").GetString(), "wait-for-read-model-catch-up", StringComparison.Ordinal) &&
                string.Equals(action.GetProperty("tone").GetString(), "Warning", StringComparison.Ordinal) &&
                string.Equals(action.GetProperty("actionPath").GetString(), "/api/v1/showcase/system/database-topology", StringComparison.Ordinal) &&
                action.GetProperty("completionSignal").GetString()!.Contains("store delta magnitude is 0", StringComparison.Ordinal));
        var readModelSync = root.GetProperty("readModelSync");
        Assert.True(readModelSync.GetProperty("enabled").GetBoolean());
        Assert.True(readModelSync.GetProperty("isLagging").GetBoolean());
        Assert.True(
            readModelSync.GetProperty("jobs").GetProperty("pendingJobs").GetInt32() >= 1 ||
            Math.Abs(readModelSync.GetProperty("productDelta").GetInt32()) >= 1);
        Assert.Contains(
            root.GetProperty("insights").EnumerateArray().ToArray(),
            insight =>
                string.Equals(insight.GetProperty("id").GetString(), "read-model-catching-up", StringComparison.Ordinal) &&
                string.Equals(insight.GetProperty("tone").GetString(), "Warning", StringComparison.Ordinal) &&
                string.Equals(insight.GetProperty("actionPath").GetString(), "/api/v1/showcase/system/database-topology", StringComparison.Ordinal) &&
                insight.GetProperty("detail").GetString()!.Contains("Store delta magnitude is", StringComparison.Ordinal));

        var brief = (string)await InvokeAsyncWithResult(projections, "GetDatabaseTopologyBriefAsync");
        Assert.Contains("Readiness: **Attention**", brief, StringComparison.Ordinal);
        Assert.Contains("1. Let the read-model catch up", brief, StringComparison.Ordinal);
        Assert.Contains("Showcase projection JSON: `/api/v1/showcase/system/database-topology`", brief, StringComparison.Ordinal);
        Assert.Contains("Migration playbook: `/engine/database-migration-playbook`", brief, StringComparison.Ordinal);

        var handoff = await InvokeAsyncWithResult(projections, "GetDatabaseTopologyHandoffAsync");
        Assert.Equal("application/zip", GetPropertyValue<string>(handoff, "ContentType"));
        Assert.Equal("database-topology-handoff.zip", GetPropertyValue<string>(handoff, "FileName"));

        using var handoffStream = new MemoryStream(GetPropertyValue<byte[]>(handoff, "Bytes"));
        using var handoffArchive = new ZipArchive(handoffStream, ZipArchiveMode.Read);
        Assert.NotNull(handoffArchive.GetEntry("README.md"));
        Assert.NotNull(handoffArchive.GetEntry("handoff-manifest.json"));

        var archivedReadme = await ReadZipEntryAsStringAsync(handoffArchive, "README.md");
        Assert.Contains("State: `Attention`", archivedReadme, StringComparison.Ordinal);
        Assert.Contains("Engine migration playbook: `/engine/database-migration-playbook`", archivedReadme, StringComparison.Ordinal);

        var archivedBrief = await ReadZipEntryAsStringAsync(handoffArchive, "database-topology-brief.md");
        Assert.Contains("Readiness: **Attention**", archivedBrief, StringComparison.Ordinal);

        var archivedProjection = await ReadZipEntryAsStringAsync(handoffArchive, "database-topology-projection.json");
        using var archivedProjectionDocument = JsonDocument.Parse(archivedProjection);
        Assert.Equal("Attention", archivedProjectionDocument.RootElement.GetProperty("readiness").GetProperty("state").GetString());

        var archivedManifest = await ReadZipEntryAsStringAsync(handoffArchive, "handoff-manifest.json");
        using var archivedManifestDocument = JsonDocument.Parse(archivedManifest);
        Assert.Equal("Attention", archivedManifestDocument.RootElement.GetProperty("readiness").GetProperty("state").GetString());
        Assert.Equal(1, archivedManifestDocument.RootElement.GetProperty("readiness").GetProperty("totalActionCount").GetInt32());
        Assert.Equal("/api/v1/showcase/system/database-topology", archivedManifestDocument.RootElement.GetProperty("readiness").GetProperty("actionPath").GetString());
        Assert.Equal(4, archivedManifestDocument.RootElement.GetProperty("contents").GetArrayLength());
    }

    [Fact]
    public async Task ShowcaseSampleTransportProjectionSeparatesModuleOwnedRestFromGenericBehaviorTransports()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/system/transports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var behaviors = root.GetProperty("behaviors").EnumerateArray().ToArray();
        var cartGet = Assert.Single(behaviors, behavior =>
            string.Equals(behavior.GetProperty("behaviorId").GetString(), "cart.get", StringComparison.Ordinal));

        Assert.DoesNotContain(
            cartGet.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.rest", StringComparison.Ordinal));
        Assert.Contains(
            cartGet.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.graphql", StringComparison.Ordinal));
        Assert.Contains(
            cartGet.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.sse", StringComparison.Ordinal));
        Assert.Contains(
            cartGet.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.ws", StringComparison.Ordinal));
        Assert.DoesNotContain(
            cartGet.GetProperty("routes").EnumerateArray(),
            route =>
                string.Equals(route.GetProperty("method").GetString(), "GET", StringComparison.Ordinal) &&
                string.Equals(route.GetProperty("route").GetString(), "/api/v1/showcase/cart/{cartId}", StringComparison.Ordinal));

        var catalogList = Assert.Single(behaviors, behavior =>
            string.Equals(behavior.GetProperty("behaviorId").GetString(), "catalog.list-products", StringComparison.Ordinal));
        Assert.Contains(
            catalogList.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.graphql", StringComparison.Ordinal));
        Assert.Contains(
            catalogList.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.jsonrpc", StringComparison.Ordinal));

        var orderStatus = Assert.Single(behaviors, behavior =>
            string.Equals(behavior.GetProperty("behaviorId").GetString(), "orders.get-status", StringComparison.Ordinal));
        Assert.Contains(
            orderStatus.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.sse", StringComparison.Ordinal));
        Assert.Contains(
            orderStatus.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.graphql-sse", StringComparison.Ordinal));
        Assert.Contains(
            orderStatus.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "http.graphql-ws", StringComparison.Ordinal));

        var ordersPlace = Assert.Single(behaviors, behavior =>
            string.Equals(behavior.GetProperty("behaviorId").GetString(), "orders.place", StringComparison.Ordinal));
        Assert.Contains(
            ordersPlace.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "rabbitmq", StringComparison.Ordinal));

        var inventoryReserve = Assert.Single(behaviors, behavior =>
            string.Equals(behavior.GetProperty("behaviorId").GetString(), "inventory.reserve-stock", StringComparison.Ordinal));
        Assert.Contains(
            inventoryReserve.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "in-memory", StringComparison.Ordinal));

        var shippingInitiate = Assert.Single(behaviors, behavior =>
            string.Equals(behavior.GetProperty("behaviorId").GetString(), "shipping.initiate", StringComparison.Ordinal));
        Assert.Contains(
            shippingInitiate.GetProperty("transportIds").EnumerateArray().Select(item => item.GetString()),
            transportId => string.Equals(transportId, "grpc", StringComparison.Ordinal));

        Assert.Contains(
            root.GetProperty("restOperations").EnumerateArray(),
            operation =>
                string.Equals(operation.GetProperty("method").GetString(), "GET", StringComparison.Ordinal) &&
                RouteEquals(operation.GetProperty("route").GetString(), "/api/v1/showcase/cart/{cartId}"));
        Assert.Contains(
            root.GetProperty("restOperations").EnumerateArray(),
            operation => RouteEquals(operation.GetProperty("route").GetString(), "/api/v1/showcase/system/summary"));
    }

    [Fact]
    public async Task ShowcaseSampleClientConfigPublishesBrowserTransportPrefixes()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/showcase/client-config.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        const string prefix = "window.CEPHALON_SHOWCASE = ";
        var script = await response.Content.ReadAsStringAsync();
        Assert.StartsWith(prefix, script, StringComparison.Ordinal);

        var payload = script[prefix.Length..].Trim();
        payload = payload.TrimEnd(';');

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var behaviorRoutes = root.GetProperty("behaviorRoutes");
        var engine = root.GetProperty("engine");

        Assert.Equal("/api/v1/showcase", root.GetProperty("restApiBase").GetString());
        Assert.Equal("v1", root.GetProperty("behaviorVersion").GetString());
        Assert.Equal("/graphql", behaviorRoutes.GetProperty("graphql").GetString());
        Assert.Equal("/json-rpc", behaviorRoutes.GetProperty("jsonRpc").GetString());
        Assert.Equal("/ws", behaviorRoutes.GetProperty("ws").GetString());
        Assert.Equal("/sse", behaviorRoutes.GetProperty("sse").GetString());
        Assert.Equal("/graphql-ws", behaviorRoutes.GetProperty("graphQLWs").GetString());
        Assert.Equal("/graphql-sse", behaviorRoutes.GetProperty("graphQLSse").GetString());
        Assert.Equal("/engine/databases", engine.GetProperty("databases").GetString());
        Assert.Equal("/engine/database-topology", engine.GetProperty("databaseTopology").GetString());
        Assert.Equal("/engine/database-roles", engine.GetProperty("databaseRoles").GetString());
        Assert.Equal("/engine/database-migrations", engine.GetProperty("databaseMigrations").GetString());
        Assert.Equal("/engine/database-migration-playbook", engine.GetProperty("databaseMigrationPlaybook").GetString());
        Assert.Equal("/scalar/v1", root.GetProperty("docs").GetProperty("scalar").GetString());
        Assert.Equal("/openapi/v1.json", root.GetProperty("docs").GetProperty("openApiJson").GetString());
    }

    [Fact]
    public async Task ShowcaseSampleActivityProjectionCapturesRecentRequestsWithDurations()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        _ = await client.GetAsync("/api/v1/showcase/system/summary");
        _ = await client.GetAsync("/api/v1/showcase/system/runtime");

        var response = await client.GetAsync("/api/v1/showcase/system/activity?limit=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.True(root.GetProperty("totalRecorded").GetInt64() >= 2);
        Assert.Contains(
            root.GetProperty("entries").EnumerateArray(),
            entry =>
                string.Equals(entry.GetProperty("path").GetString(), "/api/v1/showcase/system/runtime", StringComparison.Ordinal) &&
                entry.GetProperty("durationMs").GetDouble() >= 0d);
    }

    [Fact]
    public async Task ShowcaseSampleActivityStreamTreatsRequestCancellationAsGracefulDisconnect()
    {
        var streamMethod = typeof(Cephalon.Sample.Showcase.Modules.ShowcaseSystemModule)
            .GetMethod("StreamActivityAsync", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(streamMethod);

        var activityFeedType = typeof(ShowcaseSampleApp).Assembly.GetType(
            "Cephalon.Sample.Showcase.Infrastructure.ShowcaseActivityFeed",
            throwOnError: true);
        Assert.NotNull(activityFeedType);

        var activityFeed = Activator.CreateInstance(activityFeedType!);
        Assert.NotNull(activityFeed);

        var httpContext = new DefaultHttpContext();
        await using var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        using var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;

        var task = (Task<IResult>)streamMethod!.Invoke(null, [httpContext, activityFeed, cts.Token])!;
        await WaitForResponseBodyToContainAsync(responseBody, "event: ready", TimeSpan.FromSeconds(2));

        cts.Cancel();

        var result = await task;

        Assert.NotNull(result);
        Assert.Equal("text/event-stream", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ShowcaseSampleResetProjectionRestoresDeterministicBusinessState()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var addPayload = new
        {
            cartId = "cart-reset-001",
            customerId = "cust-reset",
            productId = "prod-001",
            productName = "ProBook Laptop 15\"",
            quantity = 1,
            priceInCents = 149999L
        };
        await client.PostAsync("/api/v1/showcase/cart/cart-reset-001/items", JsonContent.Create(addPayload));

        var orderPayload = new
        {
            customerId = "cust-reset",
            shippingAddress = "1 Reset Way",
            items = new[]
            {
                new
                {
                    productId = "prod-001",
                    productName = "ProBook Laptop 15\"",
                    quantity = 1,
                    unitPriceInCents = 149999L
                }
            }
        };
        await client.PostAsync(OrdersRoutePrefix, JsonContent.Create(orderPayload));

        var resetResponse = await client.PostAsync("/api/v1/showcase/system/reset", JsonContent.Create(new { }));

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        using (var resetDocument = JsonDocument.Parse(await resetResponse.Content.ReadAsStringAsync()))
        {
            var root = resetDocument.RootElement;
            Assert.True(root.GetProperty("productCount").GetInt32() >= 10);
            Assert.Equal(0, root.GetProperty("orderCount").GetInt32());
            Assert.Equal(0, root.GetProperty("shipmentCount").GetInt32());
            Assert.Equal(0, root.GetProperty("cartStreamCount").GetInt32());
        }

        var activityResponse = await client.GetAsync("/api/v1/showcase/system/activity?limit=10");
        Assert.Equal(HttpStatusCode.OK, activityResponse.StatusCode);

        using (var activityDocument = JsonDocument.Parse(await activityResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(0, activityDocument.RootElement.GetProperty("totalRecorded").GetInt64());
            Assert.Equal(0, activityDocument.RootElement.GetProperty("entries").GetArrayLength());
        }

        var businessResponse = await client.GetAsync("/api/v1/showcase/system/business");
        Assert.Equal(HttpStatusCode.OK, businessResponse.StatusCode);

        using var businessDocument = JsonDocument.Parse(await businessResponse.Content.ReadAsStringAsync());
        var businessRoot = businessDocument.RootElement;
        Assert.Equal(0, businessRoot.GetProperty("summary").GetProperty("orders").GetInt32());
        Assert.Equal(0, businessRoot.GetProperty("summary").GetProperty("shipments").GetInt32());
        Assert.Equal(0, businessRoot.GetProperty("summary").GetProperty("openCarts").GetInt32());
        Assert.True(businessRoot.GetProperty("products").GetArrayLength() >= 10);
    }

    [Fact]
    public async Task ShowcaseSampleGovernanceProjectionSummarizesPolicyAndTechnologySurfaces()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/v1/showcase/system/governance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var summary = root.GetProperty("summary");
        var trust = root.GetProperty("trust");
        var authorizationPolicies = root.GetProperty("authorizationPolicies");
        var technologySurfaces = root.GetProperty("technologySurfaces");
        var runtimeStory = root.GetProperty("runtimeStory");
        var recentTimeline = root.GetProperty("recentTimeline");

        Assert.Equal(0, summary.GetProperty("loadedPackageCount").GetInt32());
        Assert.True(summary.GetProperty("allowAssemblyPathPackages").GetBoolean());
        Assert.Equal("Allowed", summary.GetProperty("defaultCapabilityAccess").GetString());
        Assert.True(summary.GetProperty("authorizationPolicyCount").GetInt32() >= 3);
        Assert.True(summary.GetProperty("technologySurfaceCount").GetInt32() >= 1);
        Assert.True(summary.GetProperty("timelineEventCount").GetInt32() >= 1);

        Assert.False(trust.GetProperty("requireTrustedPackages").GetBoolean());
        Assert.Equal("Allowed", trust.GetProperty("defaultCapabilityAccess").GetString());

        Assert.Contains(
            authorizationPolicies.EnumerateArray(),
            policy => string.Equals(policy.GetProperty("id").GetString(), "showcase.admin", StringComparison.Ordinal));
        Assert.Contains(
            technologySurfaces.EnumerateArray(),
            surface => string.Equals(surface.GetProperty("technologyId").GetString(), "identity-access", StringComparison.Ordinal) ||
                string.Equals(surface.GetProperty("technologyId").GetString(), "behaviors", StringComparison.Ordinal));

        Assert.True(runtimeStory.GetProperty("moduleCount").GetInt32() >= 1);
        Assert.True(runtimeStory.GetProperty("startedModuleCount").GetInt32() >= 1);
        Assert.True(recentTimeline.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task ShowcaseSampleSystemEndpointsHonorCapabilityPolicy()
    {
        await using var app = BuildShowcaseForTests(configureBuilder: builder =>
        {
            builder.Configuration["Engine:Trust:Capabilities:showcase.system.read"] = "Denied";
            builder.Configuration["Engine:Trust:Capabilities:showcase.system.reset"] = "Denied";
        });

        await app.StartAsync();
        var client = app.GetTestClient();

        var governanceResponse = await client.GetAsync("/api/v1/showcase/system/governance");
        var resetResponse = await client.PostAsync("/api/v1/showcase/system/reset", JsonContent.Create(new { }));

        Assert.Equal(HttpStatusCode.Forbidden, governanceResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, resetResponse.StatusCode);

        var governanceProblem = await governanceResponse.Content.ReadAsStringAsync();
        var resetProblem = await resetResponse.Content.ReadAsStringAsync();

        Assert.Contains("Capability access denied", governanceProblem, StringComparison.Ordinal);
        Assert.Contains("showcase.system.read", governanceProblem, StringComparison.Ordinal);
        Assert.Contains("Capability access denied", resetProblem, StringComparison.Ordinal);
        Assert.Contains("showcase.system.reset", resetProblem, StringComparison.Ordinal);
    }

    // ──────────────────────────────────────────────
    //  End-to-end flow test
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleSupportsEndToEndOrderFlow()
    {
        await using var app = BuildShowcaseForTests();

        await app.StartAsync();
        var client = app.GetTestClient();

        // 1. Add items to cart
        var addItem1 = new { cartId = "cart-e2e-001", customerId = "cust-e2e",
            productId = "prod-001", productName = "ProBook Laptop 15\"",
            quantity = 1, priceInCents = 149999L };
        await client.PostAsync("/api/v1/showcase/cart/cart-e2e-001/items", JsonContent.Create(addItem1));

        var addItem2 = new { cartId = "cart-e2e-001", customerId = "cust-e2e",
            productId = "prod-006", productName = "TypeMaster Mechanical Keyboard",
            quantity = 2, priceInCents = 12999L };
        await client.PostAsync("/api/v1/showcase/cart/cart-e2e-001/items", JsonContent.Create(addItem2));

        // 2. Checkout cart
        var checkoutPayload = new { cartId = "cart-e2e-001", shippingAddress = "1 E2E Lane" };
        var checkoutResponse = await client.PostAsync(
            "/api/v1/showcase/cart/cart-e2e-001/checkout",
            JsonContent.Create(checkoutPayload));
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);

        // 3. Place order
        var orderPayload = new
        {
            orderId = JsonSerializer.Deserialize<JsonElement>(await checkoutResponse.Content.ReadAsStringAsync())
                .GetProperty("data")
                .GetProperty("orderId")
                .GetString(),
            customerId = "cust-e2e",
            shippingAddress = "1 E2E Lane",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1, unitPriceInCents = 149999L },
                new { productId = "prod-006", productName = "TypeMaster Mechanical Keyboard", quantity = 2, unitPriceInCents = 12999L }
            }
        };
        var orderResponse = await client.PostAsync(OrdersRoutePrefix, JsonContent.Create(orderPayload));
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var orderResult = JsonSerializer.Deserialize<JsonElement>(await orderResponse.Content.ReadAsStringAsync());
        var orderId = orderResult.GetProperty("orderId").GetString()!;
        Assert.Equal(orderPayload.orderId, orderId);

        // 4. Reserve inventory
        var reservePayload = new
        {
            orderId,
            items = new[]
            {
                new { productId = "prod-001", quantity = 1 },
                new { productId = "prod-006", quantity = 2 }
            }
        };
        var reserveResponse = await client.PostAsync("/api/v1/showcase/inventory/reserve", JsonContent.Create(reservePayload));
        Assert.Equal(HttpStatusCode.OK, reserveResponse.StatusCode);
        var reserved = JsonSerializer.Deserialize<JsonElement>(await reserveResponse.Content.ReadAsStringAsync());
        Assert.True(reserved.GetProperty("allReserved").GetBoolean());

        // 5. Initiate shipping
        var shipPayload = new
        {
            orderId,
            destinationAddress = "1 E2E Lane",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1 },
                new { productId = "prod-006", productName = "TypeMaster Mechanical Keyboard", quantity = 2 }
            }
        };
        var shipResponse = await client.PostAsync("/api/v1/showcase/shipping", JsonContent.Create(shipPayload));
        Assert.Equal(HttpStatusCode.Created, shipResponse.StatusCode);
        var shipped = JsonSerializer.Deserialize<JsonElement>(await shipResponse.Content.ReadAsStringAsync());
        var shipmentId = shipped.GetProperty("shipmentId").GetString()!;

        // 6. Confirm delivery
        var deliverPayload = new { shipmentId, recipientName = "E2E Customer" };
        var deliverResponse = await client.PutAsync(
            $"/api/v1/showcase/shipping/{shipmentId}/deliver",
            JsonContent.Create(deliverPayload));
        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        var delivered = JsonSerializer.Deserialize<JsonElement>(await deliverResponse.Content.ReadAsStringAsync());
        Assert.Equal("Delivered", delivered.GetProperty("status").GetString());

        // 7. Verify order status reflects delivery
        var orderStatus = await client.GetAsync($"{OrdersRoutePrefix}/{orderId}");
        Assert.Equal(HttpStatusCode.OK, orderStatus.StatusCode);
        var orderBody = await orderStatus.Content.ReadAsStringAsync();
        Assert.Contains("Delivered", orderBody, StringComparison.Ordinal);
    }

    private static WebApplication BuildShowcaseWithRestEnvelope()
    {
        return BuildShowcaseForTests(configureBuilder: builder =>
        {
            builder.Configuration["ApiRoutes:ResultEnvelope:Enabled"] = "true";
        });
    }

    private static WebApplication BuildShowcaseWithTightRateLimiting()
    {
        return BuildShowcaseForTests(configureBuilder: builder =>
        {
            builder.Configuration["Engine:Resilience:RateLimiting:Enabled"] = "true";
            builder.Configuration["Engine:Resilience:RateLimiting:Algorithm"] = "FixedWindow";
            builder.Configuration["Engine:Resilience:RateLimiting:PermitLimit"] = "1";
            builder.Configuration["Engine:Resilience:RateLimiting:QueueLimit"] = "0";
            builder.Configuration["Engine:Resilience:RateLimiting:WindowSeconds"] = "60";
        });
    }

    private static void DisableReadModelProjectionLoop(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var descriptors = builder.Services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                string.Equals(
                    descriptor.ImplementationType?.Name,
                    "ShowcaseReadModelProjectionHostedService",
                    StringComparison.Ordinal))
            .ToArray();

        foreach (var descriptor in descriptors)
        {
            builder.Services.Remove(descriptor);
        }
    }

    private static void ShareReadRoleWithWriteAndDisableStartupApply(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var writeConnection = builder.Configuration["Engine:Databases:Write:ConnectionString"];
        Assert.False(string.IsNullOrWhiteSpace(writeConnection));

        builder.Configuration["Engine:Databases:Read:ConnectionStringName"] = string.Empty;
        builder.Configuration["Engine:Databases:Read:ConnectionString"] = writeConnection;
        builder.Configuration["Engine:Databases:Migrations:ApplyOnStartup"] = "false";
        builder.Configuration["Engine:Databases:Migrations:ExitAfterApply"] = "false";
    }

    private static WebApplication BuildShowcaseForTests(
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        return ShowcaseSampleApp.Build(
            configureBuilder: builder =>
            {
                builder.WebHost.UseTestServer();
                ConfigureShowcaseInMemoryTestProfile(builder);
                configureBuilder?.Invoke(builder);
            },
            contentRootPath: ResolveShowcaseContentRoot());
    }

    private static string ResolveShowcaseContentRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "samples", "Cephalon.Sample.Showcase");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not resolve the Cephalon.Sample.Showcase content root from the current test assembly location.");
    }

    private static string BuildModuleRestPrefix<TModule>(string routePrefix)
        where TModule : IModule, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);

        var normalizedRoutePrefix = routePrefix.StartsWith('/')
            ? routePrefix
            : $"/{routePrefix}";
        var descriptor = new TModule().Descriptor;
        if (!Version.TryParse(descriptor.Version, out var parsedVersion) || parsedVersion.Major < 1)
        {
            throw new InvalidOperationException(
                $"Module '{descriptor.Id}' must declare a parseable semantic version for versioned REST route tests.");
        }

        return $"/api/v{parsedVersion.Major}{normalizedRoutePrefix}";
    }

    private static bool RouteEquals(string? actual, string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        return string.Equals(
            actual?.TrimEnd('/'),
            expected.TrimEnd('/'),
            StringComparison.Ordinal);
    }

    private static void ConfigureShowcaseInMemoryTestProfile(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var suffix = Guid.NewGuid().ToString("N");

        ConfigureInMemoryRole(builder, "Write", $"showcase-write-{suffix}");
        ConfigureInMemoryRole(builder, "Read", $"showcase-read-{suffix}");
        ConfigureInMemoryRole(builder, "History", $"showcase-history-{suffix}");

        builder.Configuration["Engine:Data:MongoDB:ConnectionStringName"] = string.Empty;
        builder.Configuration["Engine:Data:MongoDB:ConnectionString"] = string.Empty;
        builder.Configuration["Engine:Data:Redis:ConnectionStringName"] = string.Empty;
        builder.Configuration["Engine:Data:Redis:ConnectionString"] = string.Empty;
    }

    private static void ConfigureInMemoryRole(
        WebApplicationBuilder builder,
        string roleSectionName,
        string databaseName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleSectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        builder.Configuration[$"Engine:Databases:{roleSectionName}:Provider"] = "InMemory";
        builder.Configuration[$"Engine:Databases:{roleSectionName}:ConnectionStringName"] = string.Empty;
        builder.Configuration[$"Engine:Databases:{roleSectionName}:ConnectionString"] = databaseName;
    }

    private static async Task<object> InvokeAsyncWithResult(object target, string methodName)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(CancellationToken)],
            modifiers: null);
        Assert.NotNull(method);

        var task = method.Invoke(target, [CancellationToken.None]) as Task;
        Assert.NotNull(task);

        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(resultProperty);

        return resultProperty.GetValue(task) ?? throw new InvalidOperationException(
            $"Method '{target.GetType().FullName}.{methodName}' completed without a result.");
    }

    private static T GetPropertyValue<T>(object target, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);

        return Assert.IsType<T>(property.GetValue(target));
    }

    private static async Task WaitForResponseBodyToContainAsync(
        MemoryStream responseBody,
        string expectedContent,
        TimeSpan timeout)
    {
        var startedAt = DateTimeOffset.UtcNow;
        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            if (Encoding.UTF8.GetString(responseBody.ToArray()).Contains(expectedContent, StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException($"Timed out waiting for response body to contain '{expectedContent}'.");
    }

    private static async Task<string> ReadZipEntryAsStringAsync(ZipArchive archive, string entryName)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryName);

        var entry = archive.GetEntry(entryName);
        Assert.NotNull(entry);

        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private sealed class FailingAuditWriter : IAuditWriter
    {
        public ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            throw new InvalidOperationException("Synthetic audit writer failure for showcase integration coverage.");
        }
    }
}
