using Cephalon.Abstractions.Audit;
using Cephalon.Audit.Registration;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class WorkerHostingTests
{
    [Fact]
    public async Task AddCephalonFailsHostStartupByDefaultWhenModuleStartupFails()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Services.AddSingleton<FailurePolicyRecorder>();
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new FailurePolicyPlatformModule());
            engine.AddModule(new FlakyStartModule());
        });

        using var host = builder.Build();
        var runtime = host.Services.GetRequiredService<IRuntime>();

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => host.StartAsync());

        Assert.Equal(RuntimeStatus.Failed, runtime.Status);
        Assert.Equal("flaky-start", runtime.LastFailure?.ModuleId);
        Assert.Contains("flaky-start", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonStartsAndStopsRuntimeWithinGenericHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Services.AddSingleton<LifecycleRecorder>();
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new LifecycleDiscoveryModule());
            cephalon.AddModule(new LifecyclePlatformModule());
        });

        using var host = builder.Build();
        var runtime = host.Services.GetRequiredService<IRuntime>();
        var health = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var recorder = host.Services.GetRequiredService<LifecycleRecorder>();

        Assert.Equal(RuntimeStatus.Created, runtime.Status);
        Assert.Equal(RuntimeHealthState.Healthy, health.EvaluateLiveness().State);
        Assert.Equal(RuntimeHealthState.Unhealthy, health.EvaluateReadiness().State);

        await host.StartAsync();

        Assert.Equal(RuntimeStatus.Started, runtime.Status);
        Assert.Equal(RuntimeHealthState.Healthy, health.EvaluateLiveness().State);
        Assert.Equal(RuntimeHealthState.Healthy, health.EvaluateReadiness().State);
        Assert.Equal("modular-monolith", runtime.Manifest.AppProfile.BlueprintId);
        Assert.Equal(
            [
                "initialize:platform",
                "initialize:discovery",
                "start:platform",
                "start:discovery"
            ],
            recorder.Events);

        await host.StopAsync();

        Assert.Equal(RuntimeStatus.Stopped, runtime.Status);
        Assert.Equal(RuntimeHealthState.Unhealthy, health.EvaluateLiveness().State);
        Assert.Equal(RuntimeHealthState.Unhealthy, health.EvaluateReadiness().State);
        Assert.NotNull(runtime.StatusSnapshot.StoppedAtUtc);
        Assert.Equal(
            [
                "initialize:platform",
                "initialize:discovery",
                "start:platform",
                "start:discovery",
                "stop:discovery",
                "stop:platform"
            ],
            recorder.Events);
    }

    [Fact]
    public async Task AddCephalonUsesConfigurationDiscoveryWithinGenericHost()
    {
        var builder = Host.CreateApplicationBuilder();
        var testAssemblyName = typeof(PlatformTestModule).Assembly.GetName().Name
            ?? throw new InvalidOperationException("Test assembly name was not available.");

        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Discovery:Assemblies:0"] = testAssemblyName;
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:lifecycle-platform:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:lifecycle-discovery:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:failure-platform:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:flaky-start:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:failing-stop:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:stop-observer:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:slow-stop:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:phase8-runtime-catalogs:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:invalid-phase8-data-product:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:invalid-phase8-cdc:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:invalid-phase8-cdc-outbox:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:invalid-phase8-projection:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:invalid-phase8-outbox:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:invalid-phase8-inbox:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:invalid-phase8-audit-store:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:entity-framework-single-context-tests:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:entity-framework-split-context-tests:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:entity-framework-outbox-tests:Enabled"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Modules:entity-framework-sfid-tests:Enabled"] = "false";
        builder.AddCephalon();

        using var host = builder.Build();
        var runtime = host.Services.GetRequiredService<IRuntime>();

        await host.StartAsync();

        Assert.Contains(runtime.Manifest.Modules, module => module.Id == "platform");
        Assert.Contains(runtime.Manifest.Modules, module => module.Id == "discovery");
        Assert.DoesNotContain(runtime.Manifest.Modules, module => module.Id == "lifecycle-platform");
        Assert.DoesNotContain(runtime.Manifest.Modules, module => module.Id == "lifecycle-discovery");
        Assert.Equal(RuntimeStatus.Started, runtime.Status);

        await host.StopAsync();

        Assert.Equal(RuntimeStatus.Stopped, runtime.Status);
    }

    [Fact]
    public async Task AddCephalonSurfacesDependencyHealthWithinGenericHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new DependencyHealthModule());
        });

        using var host = builder.Build();
        var health = host.Services.GetRequiredService<RuntimeHealthEvaluator>();

        await host.StartAsync();

        var dependencies = health.EvaluateDependencies();
        var liveness = health.EvaluateLiveness();
        var readiness = health.EvaluateReadiness();

        Assert.Equal(2, dependencies.Length);
        Assert.Equal(RuntimeHealthState.Degraded, liveness.State);
        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Contains(dependencies, dependency => dependency.Id == "primary-sql" && dependency.Required);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonSurfacesHostedExecutionLifecycleWithinGenericHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new WorkflowCatalogTestModule("worker-hosted-test"));
        });

        using var host = builder.Build();
        var runtime = host.Services.GetRequiredService<IRuntime>();

        await host.StartAsync();

        var startedHostedExecution = Assert.Single(runtime.OperationalStory.HostedExecutions);
        Assert.Equal("approval-pump", startedHostedExecution.HostedExecutionId);
        Assert.True(startedHostedExecution.IsActive);
        Assert.Equal("approval-flow", startedHostedExecution.ExecutionGraphId);

        await host.StopAsync();

        var stoppedHostedExecution = Assert.Single(runtime.OperationalStory.HostedExecutions);
        Assert.True(stoppedHostedExecution.IsDeactivated);
        Assert.False(stoppedHostedExecution.IsActive);
    }

    [Fact]
    public async Task AddCephalonHonorsConfigurationDrivenAuditWriterDisablementWithinGenericHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:EnableInMemoryWriter"] = "false";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new AuditCaptureModule());
            cephalon.AddAudit();
        });

        using var host = builder.Build();
        var auditStoreCatalog = host.Services.GetRequiredService<IAuditStoreCatalog>();
        var snapshotProvider = host.Services.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();

        await host.StartAsync();

        var snapshot = snapshotProvider.CreateSnapshot();

        Assert.Empty(auditStoreCatalog.AuditStores);
        Assert.Empty(snapshot.AuditStores);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonPreservesConsumerAuditStoreWhenBuiltInAuditWriterIsDisabledWithinGenericHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:EnableInMemoryWriter"] = "false";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddAudit();
        });

        using var host = builder.Build();
        var auditStoreCatalog = host.Services.GetRequiredService<IAuditStoreCatalog>();
        var snapshotProvider = host.Services.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();

        await host.StartAsync();

        var snapshot = snapshotProvider.CreateSnapshot();

        Assert.Single(auditStoreCatalog.AuditStores);
        Assert.Equal("tenant-audit-store", auditStoreCatalog.AuditStores[0].Id);
        Assert.Single(snapshot.AuditStores);
        Assert.Equal("tenant-audit-store", snapshot.AuditStores[0].Id);
        Assert.DoesNotContain(auditStoreCatalog.AuditStores, item => item.Id == "audit-default");
        Assert.DoesNotContain(snapshot.AuditStores, item => item.Id == "audit-default");

        await host.StopAsync();
    }
}
