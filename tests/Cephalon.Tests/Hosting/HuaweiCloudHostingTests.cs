using Cephalon.Engine.Configuration;
using Cephalon.Observability.HuaweiCloud.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class HuaweiCloudHostingTests
{
    [Fact]
    public void AddCephalonHuaweiCloudSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonHuaweiCloud();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonHuaweiCloudRegistersWhenSharedEndpointIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:HostedPlatform"] = "cce";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:Region"] = "ap-southeast-3";

        builder.AddCephalonHuaweiCloud();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonHuaweiCloudRegistersWhenManagedTraceIngestionIsEnabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:UseApmManagedTraceIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:ApmEndpoint"] = "http://127.0.0.1:4317";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:AuthenticationToken"] = "sample-token";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:HostedPlatform"] = "ecs";

        builder.AddCephalonHuaweiCloud();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
    }

    [Fact]
    public void AddCephalonHuaweiCloudRejectsManagedTraceIngestionWhenProtocolIsNotGrpc()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:UseApmManagedTraceIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:ApmEndpoint"] = "http://127.0.0.1:4317";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:AuthenticationToken"] = "sample-token";

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalonHuaweiCloud());

        Assert.Contains("otlp", exception.Message, StringComparison.Ordinal);
        Assert.Contains("otlp/grpc", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonHuaweiCloudLogsHostedPlatformSummaryWhenConfigured()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:UseApmManagedTraceIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:ApmEndpoint"] = "http://127.0.0.1:4317";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:AuthenticationToken"] = "sample-token";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:HostedPlatform"] = "cce";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:HuaweiCloud:Region"] = "ap-southeast-3";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonHuaweiCloud();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3112 &&
            entry.Message.Contains("huawei-managed-trace-ingestion", StringComparison.Ordinal) &&
            entry.Message.Contains("huawei_cloud_cce", StringComparison.Ordinal) &&
            entry.Message.Contains("Authentication-header", StringComparison.Ordinal) &&
            entry.Message.Contains("logs=not-exported-by-managed-apm", StringComparison.Ordinal));
    }
}
