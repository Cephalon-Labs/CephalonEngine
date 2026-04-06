using Cephalon.Engine.Configuration;
using Cephalon.Observability.NewRelic.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class NewRelicHostingTests
{
    [Fact]
    public void AddCephalonNewRelicSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonNewRelic();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonNewRelicRegistersWhenDirectEndpointAndLicenseKeyAreConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:UseNativeOtlpEndpoint"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:Region"] = "eu";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:LicenseKey"] = "nr-license-key";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:ServiceNamespace"] = "checkout";

        builder.AddCephalonNewRelic();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public async Task AddCephalonNewRelicRejectsDirectEndpointWhenNoAuthenticationInputIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:UseNativeOtlpEndpoint"] = "true";

        builder.AddCephalonNewRelic();

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains("Cephalon New Relic direct OTLP endpoint mode requires", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonNewRelicLogsSummaryWhenLicenseKeyAuthenticationIsConfigured()
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
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:UseNativeOtlpEndpoint"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:Region"] = "eu";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:LicenseKey"] = "nr-license-key";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:NewRelic:ServiceNamespace"] = "checkout";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonNewRelic();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3154 &&
            entry.Message.Contains("newrelic-direct", StringComparison.Ordinal) &&
            entry.Message.Contains("api-key", StringComparison.Ordinal) &&
            entry.Message.Contains("eu", StringComparison.Ordinal) &&
            entry.Message.Contains("service.namespace=checkout", StringComparison.Ordinal) &&
            entry.Message.Contains("logs=True", StringComparison.Ordinal));
    }
}
