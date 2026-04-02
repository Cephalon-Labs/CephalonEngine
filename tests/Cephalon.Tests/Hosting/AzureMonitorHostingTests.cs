using Cephalon.Engine.Configuration;
using Cephalon.Observability.AzureMonitor.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class AzureMonitorHostingTests
{
    [Fact]
    public void AddCephalonAzureMonitorSkipsRegistrationWhenConnectionStringIsMissing()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonAzureMonitor();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonAzureMonitorRegistersWhenConnectionStringIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AzureMonitor:ConnectionString"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000";

        builder.AddCephalonAzureMonitor();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public async Task AddCephalonAzureMonitorLogsHostedPlatformSummaryWhenConfigured()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AzureMonitor:ConnectionString"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AzureMonitor:UseDefaultAzureCredential"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AzureMonitor:HostedPlatform"] = "appservice";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonAzureMonitor();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3100 &&
            entry.Message.Contains("DefaultAzureCredential", StringComparison.Ordinal) &&
            entry.Message.Contains("azure.app_service", StringComparison.Ordinal));
    }
}
