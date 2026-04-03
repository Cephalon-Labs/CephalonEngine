using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cephalon.Engine.Configuration;
using Cephalon.Observability.Kubernetes.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class KubernetesHostingTests
{
    [Fact]
    public void AddCephalonKubernetesSkipsRegistrationWhenNoEndpointModeIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";

        builder.AddCephalonKubernetes();

        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonKubernetesRegistersWhenSharedEndpointIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4318";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:ClusterName"] = "prod-cluster";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:Namespace"] = "catalog";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:PodName"] = "catalog-7b9c9d";

        builder.AddCephalonKubernetes();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void AddCephalonKubernetesRegistersWhenInClusterCollectorServiceIsConfigured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:UseInClusterCollectorService"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:CollectorServiceName"] = "otel-collector";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:CollectorNamespace"] = "observability";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:ServiceDnsSuffix"] = "svc.cluster.internal";

        builder.AddCephalonKubernetes();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public async Task AddCephalonKubernetesRejectsTrustedCaBundleWhenLogsAreEnabledOverHttpsHttp()
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
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:UseInClusterCollectorService"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:CollectorServiceName"] = "otel-collector";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:Namespace"] = "observability";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:CollectorScheme"] = "https";
            builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:TrustedCaCertificatePath"] = trustedCaPath;

            builder.AddCephalonKubernetes();

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
    public async Task AddCephalonKubernetesLogsSummaryWhenConfigured()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:UseInClusterCollectorService"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:CollectorServiceName"] = "otel-collector";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:CollectorNamespace"] = "observability";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:ServiceDnsSuffix"] = "svc.cluster.internal";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:ClusterName"] = "prod-cluster";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:Namespace"] = "payments";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:PodName"] = "payments-api-5799c";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:NodeName"] = "node-a";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:ContainerName"] = "api";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Kubernetes:Headers"] = "Authorization=Bearer sample-token";
        builder.Environment.EnvironmentName = Environments.Production;

        builder.AddCephalonKubernetes();

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3117 &&
            entry.Message.Contains("kubernetes-collector-service", StringComparison.Ordinal) &&
            entry.Message.Contains("otel-collector.observability.svc.cluster.internal", StringComparison.Ordinal) &&
            entry.Message.Contains("cluster=prod-cluster", StringComparison.Ordinal) &&
            entry.Message.Contains("namespace=payments", StringComparison.Ordinal) &&
            entry.Message.Contains("headers mode configured", StringComparison.Ordinal) &&
            entry.Message.Contains("signals logs=False, metrics=False, traces=True", StringComparison.Ordinal));
    }

    private static string CreateTemporaryTrustedCaBundle()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Cephalon Kubernetes Test Root",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(7));

        var path = Path.Combine(Path.GetTempPath(), $"cephalon-kubernetes-{Guid.NewGuid():N}.pem");
        File.WriteAllText(path, certificate.ExportCertificatePem());
        return path;
    }
}
