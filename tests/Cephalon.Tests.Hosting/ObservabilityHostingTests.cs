using Cephalon.Engine.Configuration;
using Cephalon.Observability.Hosting;
using Cephalon.Tests.Support;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Tests.Hosting;

public sealed class ObservabilityHostingTests
{
    [Fact]
    public async Task AddCephalonObservabilityLogsManifestSummaryAndRuntimeSurface()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://collector:4317";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
            cephalon.AddModule(new WorkflowCatalogTestModule("observability-test"));
        });
        builder.Services.AddCephalonObservability(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3000 &&
            entry.Message.Contains("Runtime manifest 2.0", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3001 &&
            entry.Message.Contains("platform", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3002 &&
            entry.Message.Contains("discovery.greetings", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3003 &&
            entry.Message.Contains("Cephalon.Engine", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3004 &&
            entry.Message.Contains("liveness Healthy", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3005 &&
            entry.Message.Contains("http://collector:4317", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3006 &&
            entry.Message.Contains("Cephalon.Engine", StringComparison.Ordinal) &&
            entry.Message.Contains("2000-2005", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3006 &&
            entry.Message.Contains("Cephalon.Observability", StringComparison.Ordinal) &&
            entry.Message.Contains("3000-3006", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 2000 &&
            entry.Message.Contains("Runtime phase 'start' completed", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 2005 &&
            entry.Message.Contains("approval-pump", StringComparison.Ordinal) &&
            entry.Message.Contains("phase 'activate'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddCephalonObservabilityLogsSelfHostedTelemetryDefaultEndpointWhenEnabled()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:UseSelfHostedDefaults"] = "true";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });
        builder.Services.AddCephalonObservability(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3005 &&
            entry.Message.Contains("http://localhost:4318 (self-hosted default)", StringComparison.Ordinal));
    }
}
