using Cephalon.Engine.Configuration;
using Cephalon.Observability.GrafanaCloud.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class GrafanaCloudHostingTests
{
    [Fact]
    public void AddCephalonGrafanaCloudSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonGrafanaCloud();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonGrafanaCloudRegistersWhenDirectEndpointAndHeadersAreConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:UseDirectGrafanaCloudEndpoint"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:Endpoint"] = "https://otlp-gateway-prod-us-central-0.grafana.net/otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:Headers"] = "Authorization=Basic abc123";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:ServiceNamespace"] = "checkout";

        builder.AddCephalonGrafanaCloud();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public async Task AddCephalonGrafanaCloudRejectsDirectEndpointWhenNoAuthenticationInputIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:UseDirectGrafanaCloudEndpoint"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:Endpoint"] = "https://otlp-gateway-prod-us-central-0.grafana.net/otlp";

        builder.AddCephalonGrafanaCloud();

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains("Grafana Cloud direct OTLP endpoint mode requires", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonGrafanaCloudLogsSummaryWhenAccessPolicyAuthenticationIsConfigured()
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
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:UseDirectGrafanaCloudEndpoint"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:Endpoint"] = "https://otlp-gateway-prod-us-central-0.grafana.net/otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:InstanceId"] = "39812";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:AccessPolicyToken"] = "glc_example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:GrafanaCloud:ServiceNamespace"] = "checkout";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonGrafanaCloud();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3119 &&
            entry.Message.Contains("grafana-cloud-direct", StringComparison.Ordinal) &&
            entry.Message.Contains("access-policy-basic", StringComparison.Ordinal) &&
            entry.Message.Contains("service.namespace=checkout", StringComparison.Ordinal) &&
            entry.Message.Contains("logs=True", StringComparison.Ordinal));
    }
}
