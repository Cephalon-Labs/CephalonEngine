using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cephalon.Engine.Configuration;
using Cephalon.Observability.Tanzu.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class TanzuHostingTests
{
    [Fact]
    public void AddCephalonTanzuSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonTanzu();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonTanzuRegistersWhenSharedEndpointIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:HostedPlatform"] = "tap";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:ClusterName"] = "prod-cluster";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:Namespace"] = "catalog";

        builder.AddCephalonTanzu();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonTanzuRegistersWhenInClusterProxyServiceIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:HostedPlatform"] = "tkg";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:UseInClusterProxyService"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:ProxyServiceName"] = "wavefront-proxy";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:Namespace"] = "observability";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:ProxyPort"] = "4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:ClusterName"] = "prod-cluster";

        builder.AddCephalonTanzu();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonTanzuRejectsProxyServiceWhenMetricsAreEnabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:UseInClusterProxyService"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:ProxyServiceName"] = "wavefront-proxy";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:Namespace"] = "observability";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:ProxyPort"] = "4318";

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalonTanzu());

        Assert.Contains("only supports traces", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UseInClusterProxyService", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCephalonTanzuRejectsTrustedCaBundleWhenLogsAreEnabledOverHttpsHttp()
    {
        var trustedCaPath = CreateTemporaryTrustedCaBundle();

        try
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "https://127.0.0.1:4318";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:TrustedCaCertificatePath"] = trustedCaPath;

            builder.AddCephalonTanzu();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                using var host = builder.Build();
                return host.StartAsync();
            });

            Assert.Contains("HttpClientFactory", exception.Message, StringComparison.Ordinal);
            Assert.Contains("TrustedCaCertificatePath", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(trustedCaPath);
        }
    }

    [Fact]
    public async Task AddCephalonTanzuLogsHostedPlatformSummaryWhenConfigured()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:HostedPlatform"] = "tap";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:ClusterName"] = "team-a";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:Namespace"] = "payments";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Tanzu:Headers"] = "Authorization=Bearer sample-token";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonTanzu();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3116 &&
            entry.Message.Contains("configured-endpoint", StringComparison.Ordinal) &&
            entry.Message.Contains("vmware_tanzu_application_platform", StringComparison.Ordinal) &&
            entry.Message.Contains("shared-collector-contract", StringComparison.Ordinal) &&
            entry.Message.Contains("shared-otlp, headers=configured", StringComparison.Ordinal) &&
            entry.Message.Contains("signals logs=False, metrics=False, traces=True", StringComparison.Ordinal));
    }

    private static string CreateTemporaryTrustedCaBundle()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Cephalon Tanzu Test Root",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(7));

        var path = Path.Combine(Path.GetTempPath(), $"cephalon-tanzu-{Guid.NewGuid():N}.pem");
        File.WriteAllText(path, certificate.ExportCertificatePem());
        return path;
    }
}
