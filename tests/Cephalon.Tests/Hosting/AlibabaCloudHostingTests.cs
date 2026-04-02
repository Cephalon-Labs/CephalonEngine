using Cephalon.Engine.Configuration;
using Cephalon.Observability.AlibabaCloud.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class AlibabaCloudHostingTests
{
    [Fact]
    public void AddCephalonAlibabaCloudSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonAlibabaCloud();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonAlibabaCloudRegistersWhenSharedEndpointIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:HostedPlatform"] = "ecs";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:Region"] = "cn-hangzhou";

        builder.AddCephalonAlibabaCloud();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonAlibabaCloudRegistersWhenManagedGrpcIngestionIsEnabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:UseManagedOpenTelemetryIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:ManagedGrpcEndpoint"] = "http://127.0.0.1:8000";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:AuthenticationToken"] = "sample-token";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:HostedPlatform"] = "ecs";

        builder.AddCephalonAlibabaCloud();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonAlibabaCloudRegistersWhenManagedHttpIngestionIsEnabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:UseManagedOpenTelemetryIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:ManagedHttpTracesEndpoint"] = "https://otel.example.aliyuncs.com/adapt_xxx/api/otlp/traces";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:ManagedHttpMetricsEndpoint"] = "https://otel.example.aliyuncs.com/adapt_xxx/api/otlp/metrics";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:HostedPlatform"] = "functioncompute";

        builder.AddCephalonAlibabaCloud();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public async Task AddCephalonAlibabaCloudRejectsManagedGrpcIngestionWhenAuthenticationTokenIsMissing()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:UseManagedOpenTelemetryIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:ManagedGrpcEndpoint"] = "http://127.0.0.1:8000";

        builder.AddCephalonAlibabaCloud();

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains("AuthenticationToken", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonAlibabaCloudLogsHostedPlatformSummaryWhenConfigured()
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
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:UseManagedOpenTelemetryIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:ManagedGrpcEndpoint"] = "http://127.0.0.1:8000";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:AuthenticationToken"] = "sample-token";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:HostedPlatform"] = "fc";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AlibabaCloud:Region"] = "cn-hangzhou";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonAlibabaCloud();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3113 &&
            entry.Message.Contains("alibaba-managed-ingestion", StringComparison.Ordinal) &&
            entry.Message.Contains("alibaba_cloud_fc", StringComparison.Ordinal) &&
            entry.Message.Contains("Authentication-header", StringComparison.Ordinal) &&
            entry.Message.Contains("logs=not-exported-by-managed-ingestion", StringComparison.Ordinal));
    }
}
