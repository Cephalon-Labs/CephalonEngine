using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cephalon.Engine.Configuration;
using Cephalon.Observability.OpenShift.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class OpenShiftHostingTests
{
    [Fact]
    public void AddCephalonOpenShiftSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonOpenShift();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonOpenShiftRegistersWhenSharedEndpointIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:HostedPlatform"] = "aro";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:ClusterName"] = "prod-cluster";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:Namespace"] = "catalog";

        builder.AddCephalonOpenShift();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonOpenShiftRegistersWhenInClusterCollectorServiceIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:UseInClusterCollectorService"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:CollectorServiceName"] = "otel-collector";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:Namespace"] = "operations";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:HostedPlatform"] = "openshift";

        builder.AddCephalonOpenShift();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public async Task AddCephalonOpenShiftRejectsTrustedCaBundleWhenLogsAreEnabledOverHttpsHttp()
    {
        var trustedCaPath = CreateTemporaryTrustedCaBundle();

        try
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:UseInClusterCollectorService"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:CollectorServiceName"] = "otel-collector";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:Namespace"] = "observability";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:CollectorScheme"] = "https";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:TrustedCaCertificatePath"] = trustedCaPath;

            builder.AddCephalonOpenShift();

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
    public async Task AddCephalonOpenShiftLogsHostedPlatformSummaryWhenConfigured()
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
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:HostedPlatform"] = "aro";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:ClusterName"] = "team-a";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:Namespace"] = "payments";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:OpenShift:Headers"] = "Authorization=Bearer sample-token";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonOpenShift();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3114 &&
            entry.Message.Contains("configured-endpoint", StringComparison.Ordinal) &&
            entry.Message.Contains("aro", StringComparison.Ordinal) &&
            entry.Message.Contains("shared-collector-contract", StringComparison.Ordinal) &&
            entry.Message.Contains("headers mode configured", StringComparison.Ordinal) &&
            entry.Message.Contains("signals logs=False, metrics=False, traces=True", StringComparison.Ordinal));
    }

    private static string CreateTemporaryTrustedCaBundle()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Cephalon OpenShift Test Root",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(7));

        var path = Path.Combine(Path.GetTempPath(), $"cephalon-openshift-{Guid.NewGuid():N}.pem");
        File.WriteAllText(path, certificate.ExportCertificatePem());
        return path;
    }
}
