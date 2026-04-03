using Cephalon.Engine.Configuration;
using Cephalon.Observability.OracleCloud.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class OracleCloudHostingTests
{
    [Fact]
    public void AddCephalonOracleCloudSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonOracleCloud();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonOracleCloudRegistersWhenSharedEndpointIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:HostedPlatform"] = "oke";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:Region"] = "us-ashburn-1";

        builder.AddCephalonOracleCloud();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonOracleCloudRegistersWhenManagedHttpIngestionIsEnabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:UseManagedOpenTelemetryIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:DataUploadEndpoint"] = "https://aaaaaaaaaaaaaaaaaaaaaa.apm-agt.us-ashburn-1.oci.oraclecloud.com";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:TraceDataKey"] = "trace-data-key";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:MetricsDataKey"] = "metrics-data-key";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:HostedPlatform"] = "oke";

        builder.AddCephalonOracleCloud();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonOracleCloudRejectsManagedIngestionWhenProtocolIsNotHttp()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:UseManagedOpenTelemetryIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:DataUploadEndpoint"] = "https://aaaaaaaaaaaaaaaaaaaaaa.apm-agt.us-ashburn-1.oci.oraclecloud.com";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:TraceDataKey"] = "trace-data-key";

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalonOracleCloud());

        Assert.Contains("otlp/http", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonOracleCloudRejectsManagedMetricsIngestionWhenMetricsDataKeyIsMissing()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:UseManagedOpenTelemetryIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:DataUploadEndpoint"] = "https://aaaaaaaaaaaaaaaaaaaaaa.apm-agt.us-ashburn-1.oci.oraclecloud.com";

        builder.AddCephalonOracleCloud();

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains("MetricsDataKey", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonOracleCloudLogsHostedPlatformSummaryWhenConfigured()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:UseManagedOpenTelemetryIngestion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:DataUploadEndpoint"] = "https://aaaaaaaaaaaaaaaaaaaaaa.apm-agt.us-ashburn-1.oci.oraclecloud.com";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:UsePublicTraceDataKey"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:TraceDataKey"] = "trace-data-key";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:MetricsDataKey"] = "metrics-data-key";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:HostedPlatform"] = "oke";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OracleCloud:Region"] = "us-ashburn-1";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonOracleCloud();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3118 &&
            entry.Message.Contains("oracle-apm-managed-ingestion", StringComparison.Ordinal) &&
            entry.Message.Contains("oracle_cloud_oke", StringComparison.Ordinal) &&
            entry.Message.Contains("traces=public-data-key", StringComparison.Ordinal) &&
            entry.Message.Contains("metrics=private-data-key", StringComparison.Ordinal) &&
            entry.Message.Contains("logs=not-exported-by-managed-ingestion", StringComparison.Ordinal));
    }
}
