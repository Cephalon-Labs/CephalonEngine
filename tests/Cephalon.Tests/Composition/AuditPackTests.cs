using System.Diagnostics;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Tenancy;
using Cephalon.Audit.Registration;
using Cephalon.Audit.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class AuditPackTests
{
    [Fact]
    public async Task AddAuditRegistersRecorderDiagnosticsAndAuditStoreCatalog()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                audit: new AuditSettings(enabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddAudit();
        });

        await using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var auditStoreCatalog = provider.GetRequiredService<IAuditStoreCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var snapshot = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var auditStore = Assert.Single(auditStoreCatalog.AuditStores);
        var diagnosticsConvention = Assert.Single(diagnosticsCatalog.GetBySource("Cephalon.Audit"));

        Assert.NotNull(recorder);
        Assert.Equal("audit-default", auditStore.Id);
        Assert.Equal("audit", auditStore.SourceModuleId);
        Assert.Equal("memory", auditStore.Provider);
        Assert.Equal("volatile-buffer", auditStore.Mode);
        Assert.Equal("application-managed", auditStore.Metadata["writeMode"]);
        Assert.Equal("not-configured", auditStore.Metadata["queryMode"]);
        Assert.Equal(4600, diagnosticsConvention.MinimumEventId);
        Assert.Equal(4601, diagnosticsConvention.MaximumEventId);
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4600 && entry.Name == "AuditEntryWritten");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4601 && entry.Name == "AuditEntryWriteFailed");
        Assert.Single(snapshot.AuditStores);
        Assert.Contains(snapshot.AuditStores, item => item.Id == "audit-default");
    }

    [Fact]
    public async Task AddAuditRecordsEntriesWithAmbientTenantActorAndCorrelationDefaults()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase"),
                audit: new AuditSettings(enabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new AuditCaptureModule());
            engine.AddMultiTenancy(options =>
            {
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-001",
                    tenantKey: "acme",
                    displayName: "Acme"));
            });
            engine.AddAudit();
        });

        await using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<ITenantResolver>();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var captureWriter = provider.GetRequiredService<CaptureAuditWriter>();

        var resolvedTenant = await resolver.ResolveAsync(new TenantResolutionRequest(
            requestedTenantKey: "acme",
            userId: "user-007"));
        Assert.True(resolvedTenant.IsResolved);

        using var activity = new Activity("audit-test");
        activity.Start();

        var entry = await recorder.RecordAsync(new AuditRecordRequest(
            category: "identity",
            action: "invite-approved",
            summary: "Approved a tenant invitation.",
            subjectType: "tenant-membership",
            subjectId: "membership-001",
            outcome: AuditOutcome.Succeeded,
            changes:
            [
                new AuditChange("status", "pending", "approved")
            ],
            tags: ["identity", "membership"],
            metadata: new Dictionary<string, string>
            {
                ["origin"] = "backoffice"
            }));

        Assert.NotNull(entry.Id);
        Assert.Equal("tenant-001", entry.TenantId);
        Assert.Equal("user-007", entry.Actor.ActorId);
        Assert.Equal(activity.TraceId.ToString(), entry.CorrelationId);
        Assert.Equal("backoffice", entry.Metadata["origin"]);
        Assert.Single(captureWriter.Entries);
        Assert.Equal(entry.Id, captureWriter.Entries[0].Id);
        Assert.Equal("tenant-001", captureWriter.Entries[0].TenantId);
        Assert.Equal("user-007", captureWriter.Entries[0].Actor.ActorId);
    }

    [Fact]
    public async Task AddAuditCanDisableTheDefaultInMemoryWriterWithoutPretendingTheMemoryStoreIsActive()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                audit: new AuditSettings(enabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new AuditCaptureModule());
            engine.AddAudit(options => options.EnableInMemoryWriter = false);
        });

        await using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var captureWriter = provider.GetRequiredService<CaptureAuditWriter>();
        var auditStoreCatalog = provider.GetRequiredService<IAuditStoreCatalog>();
        var snapshot = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var entry = await recorder.RecordAsync(new AuditRecordRequest(
            category: "tenant",
            action: "tenant-switched",
            summary: "Switched the active tenant context.",
            subjectType: "tenant",
            subjectId: "tenant-001",
            outcome: AuditOutcome.Succeeded));

        Assert.NotNull(entry.Id);
        Assert.Single(captureWriter.Entries);
        Assert.Equal(entry.Id, captureWriter.Entries[0].Id);
        Assert.Empty(auditStoreCatalog.AuditStores);
        Assert.Empty(snapshot.AuditStores);
    }

    [Fact]
    public async Task AddAuditHonorsConfigurationDrivenInMemoryWriterDisablement()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{EngineSettings.SectionName}:Audit:EnableInMemoryWriter"] = "false"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                audit: new AuditSettings(enabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new AuditCaptureModule());
            engine.AddAudit();
        });

        await using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var captureWriter = provider.GetRequiredService<CaptureAuditWriter>();
        var auditStoreCatalog = provider.GetRequiredService<IAuditStoreCatalog>();
        var snapshot = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var entry = await recorder.RecordAsync(new AuditRecordRequest(
            category: "tenant",
            action: "tenant-switched",
            summary: "Switched the active tenant context.",
            subjectType: "tenant",
            subjectId: "tenant-001",
            outcome: AuditOutcome.Succeeded));

        Assert.NotNull(entry.Id);
        Assert.Single(captureWriter.Entries);
        Assert.Equal(entry.Id, captureWriter.Entries[0].Id);
        Assert.Empty(auditStoreCatalog.AuditStores);
        Assert.Empty(snapshot.AuditStores);
    }
}
