using System.Net;
using System.Net.Http.Json;
using Cephalon.Abstractions.Audit;
using Cephalon.Audit.EntityFramework;
using Cephalon.Audit.EntityFramework.Modeling;
using Cephalon.Audit.EntityFramework.Registration;
using Cephalon.Audit.Registration;
using Cephalon.Audit.Services;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class AuditHistoryHostingTests
{
    [Fact]
    public async Task MapCephalonExposesAuditHistoryQueryRoutesWhenAReaderIsRegistered()
    {
        var databaseName = $"cephalon-audit-history-hosting-{Guid.NewGuid():N}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:EnableInMemoryWriter"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:History:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:History:Provider"] = "entity-framework";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:History:DatabaseRole"] = "history";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:History:Provider"] = "Sqlite";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:History:ConnectionString"] = "Data Source=ignored-for-inmemory";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddAudit();
            engine.AddEntityFrameworkAuditHistory<TestAuditHistoryDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var recorder = app.Services.GetRequiredService<IAuditRecorder>();
        var createdEntry = await recorder.RecordAsync(new AuditRecordRequest(
            category: "catalog",
            action: "product-created",
            summary: "Created a product through the audit-history host test.",
            subjectType: "product",
            subjectId: "prod-001",
            outcome: AuditOutcome.Succeeded,
            tenantId: "tenant-alpha"));
        var failedEntry = await recorder.RecordAsync(new AuditRecordRequest(
            category: "orders",
            action: "order-cancelled",
            summary: "Cancelled an order through the audit-history host test.",
            subjectType: "order",
            subjectId: "ord-001",
            outcome: AuditOutcome.Failed,
            tenantId: "tenant-beta"));

        var client = app.GetTestClient();
        var queryResponse = await client.GetAsync("/engine/audit-history?category=catalog&limit=10");
        var queryResult = await queryResponse.Content.ReadFromJsonAsync<AuditHistoryQueryResult>();
        var byIdResponse = await client.GetAsync($"/engine/audit-history/{failedEntry.Id}");
        var byIdEntry = await byIdResponse.Content.ReadFromJsonAsync<AuditHistoryEntry>();

        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
        Assert.NotNull(queryResult);
        Assert.Single(queryResult.Entries);
        Assert.Equal(1, queryResult.TotalCount);
        Assert.Equal(createdEntry.Id, queryResult.Entries[0].Id);
        Assert.Equal("catalog", queryResult.Entries[0].Category);
        Assert.Equal(AuditOutcome.Succeeded, queryResult.Entries[0].Outcome);

        Assert.Equal(HttpStatusCode.OK, byIdResponse.StatusCode);
        Assert.NotNull(byIdEntry);
        Assert.Equal(failedEntry.Id, byIdEntry.Id);
        Assert.Equal("orders", byIdEntry.Category);
        Assert.Equal(AuditOutcome.Failed, byIdEntry.Outcome);
        Assert.Equal("tenant-beta", byIdEntry.TenantId);
    }

    private sealed class TestAuditHistoryDbContext(DbContextOptions<TestAuditHistoryDbContext> options)
        : DbContext(options), IEntityFrameworkAuditHistoryContext
    {
        public DbSet<EntityFrameworkAuditHistoryEntry> AuditEntries => Set<EntityFrameworkAuditHistoryEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.ConfigureCephalonAuditHistory(tableName: "test_audit_history");
        }
    }
}
