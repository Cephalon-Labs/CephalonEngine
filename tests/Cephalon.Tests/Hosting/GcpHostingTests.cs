using Cephalon.Engine.Configuration;
using Cephalon.Observability.Gcp.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class GcpHostingTests
{
    [Fact]
    public void AddCephalonGcpSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonGcp();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonGcpRegistersWhenSharedEndpointIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Gcp:HostedPlatform"] = "cloudrun";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Gcp:Location"] = "asia-southeast1";

        builder.AddCephalonGcp();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonGcpRegistersWhenGoogleManagedIngestionIsEnabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Gcp:UseGoogleManagedIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Gcp:HostedPlatform"] = "gke";

        builder.AddCephalonGcp();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
    }

    [Fact]
    public void AddCephalonGcpRejectsDirectManagedIngestionWhenProtocolIsNotHttp()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Gcp:UseGoogleManagedIngestion"] = "true";

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalonGcp());

        Assert.Contains("otlp/http", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonGcpLogsHostedPlatformSummaryWhenConfigured()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Gcp:UseGoogleManagedIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Gcp:HostedPlatform"] = "cloudrun";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Gcp:Location"] = "asia-southeast1";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonGcp();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3111 &&
            entry.Message.Contains("google-managed-ingestion", StringComparison.Ordinal) &&
            entry.Message.Contains("gcp_cloud_run", StringComparison.Ordinal) &&
            entry.Message.Contains("ApplicationDefaultCredentials", StringComparison.Ordinal) &&
            entry.Message.Contains("logs=platform-or-collector", StringComparison.Ordinal));
    }
}
