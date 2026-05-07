using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Configuration;
using Cephalon.Observability.AlibabaCloud.Hosting;
using Cephalon.Observability.Aws.Hosting;
using Cephalon.Observability.AzureMonitor.Hosting;
using Cephalon.Observability.DigitalOcean.Hosting;
using Cephalon.Observability.Gcp.Hosting;
using Cephalon.Observability.GrafanaCloud.Hosting;
using Cephalon.Observability.Hosting;
using Cephalon.Observability.HuaweiCloud.Hosting;
using Cephalon.Observability.Kubernetes.Hosting;
using Cephalon.Observability.NewRelic.Hosting;
using Cephalon.Observability.OpenShift.Hosting;
using Cephalon.Observability.OpenTelemetry.Hosting;
using Cephalon.Observability.OracleCloud.Hosting;
using Cephalon.Observability.Tanzu.Hosting;
using Cephalon.Tests.Support;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class ObservabilityTelemetryRuntimeSurfaceTests
{
    public static IEnumerable<object[]> TelemetryProviderRuntimeSurfaceCases
    {
        get
        {
            yield return new object[]
            {
                "Cephalon.Observability.AlibabaCloud",
                "telemetry-export-alibaba-cloud",
                "alibaba-cloud",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonAlibabaCloud())
            };
            yield return new object[]
            {
                "Cephalon.Observability.Aws",
                "telemetry-export-aws",
                "aws",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonAws())
            };
            yield return new object[]
            {
                "Cephalon.Observability.AzureMonitor",
                "telemetry-export-azure-monitor",
                "azure-monitor",
                (Action<HostApplicationBuilder>)(builder =>
                {
                    builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:AzureMonitor:ConnectionString"] =
                        "InstrumentationKey=00000000-0000-0000-0000-000000000000";
                    builder.AddCephalonAzureMonitor();
                })
            };
            yield return new object[]
            {
                "Cephalon.Observability.DigitalOcean",
                "telemetry-export-digitalocean",
                "digitalocean",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonDigitalOcean())
            };
            yield return new object[]
            {
                "Cephalon.Observability.Gcp",
                "telemetry-export-gcp",
                "gcp",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonGcp())
            };
            yield return new object[]
            {
                "Cephalon.Observability.GrafanaCloud",
                "telemetry-export-grafana-cloud",
                "grafana-cloud",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonGrafanaCloud())
            };
            yield return new object[]
            {
                "Cephalon.Observability.HuaweiCloud",
                "telemetry-export-huawei-cloud",
                "huawei-cloud",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonHuaweiCloud())
            };
            yield return new object[]
            {
                "Cephalon.Observability.Kubernetes",
                "telemetry-export-kubernetes",
                "kubernetes",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonKubernetes())
            };
            yield return new object[]
            {
                "Cephalon.Observability.NewRelic",
                "telemetry-export-new-relic",
                "new-relic",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonNewRelic())
            };
            yield return new object[]
            {
                "Cephalon.Observability.OpenShift",
                "telemetry-export-openshift",
                "openshift",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonOpenShift())
            };
            yield return new object[]
            {
                "Cephalon.Observability.OpenTelemetry",
                "telemetry-export-opentelemetry",
                "opentelemetry",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonOpenTelemetry())
            };
            yield return new object[]
            {
                "Cephalon.Observability.OracleCloud",
                "telemetry-export-oracle-cloud",
                "oracle-cloud",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonOracleCloud())
            };
            yield return new object[]
            {
                "Cephalon.Observability.Tanzu",
                "telemetry-export-tanzu",
                "tanzu",
                (Action<HostApplicationBuilder>)(builder => builder.AddCephalonTanzu())
            };
        }
    }

    [Fact]
    public void AddCephalonObservabilityProjectsSanitizedTelemetryGuidanceRuntimeSurface()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry",
                [$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http",
                [$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "https://collector.internal:4318/tenant-secret",
                [$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "true",
                [$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true",
                [$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddCephalonObservability(configuration);

        using var provider = services.BuildServiceProvider();
        var surface = Assert.Single(
            provider.GetServices<ITechnologyRuntimeContributor>()
                .Select(contributor => contributor.DescribeRuntimeSurface()),
            surface => surface.SurfaceId == "telemetry-export-guidance");
        var entry = Assert.Single(surface.Entries);

        Assert.Equal("observability", surface.TechnologyId);
        Assert.Equal("telemetry-export-guidance", entry.Id);
        Assert.Equal("Cephalon.Observability", entry.Metadata["pack"]);
        Assert.Equal("redacted", entry.Metadata["secretProjection"]);
        AssertSanitized(entry.Metadata);
    }

    [Theory]
    [MemberData(nameof(TelemetryProviderRuntimeSurfaceCases))]
    public void AddCephalonTelemetryProviderProjectsSanitizedRuntimeSurface(
        string expectedPack,
        string expectedSurfaceId,
        string expectedEntryId,
        Action<HostApplicationBuilder> registerProvider)
    {
        var builder = Host.CreateApplicationBuilder();
        ConfigureSharedTelemetry(builder);

        registerProvider(builder);

        using var host = builder.Build();
        var surface = Assert.Single(
            host.Services.GetServices<ITechnologyRuntimeContributor>()
                .Select(contributor => contributor.DescribeRuntimeSurface()),
            surface => surface.SurfaceId == expectedSurfaceId);
        var entry = Assert.Single(surface.Entries);

        Assert.Equal("observability", surface.TechnologyId);
        Assert.Equal(expectedEntryId, entry.Id);
        Assert.Equal(expectedPack, entry.Metadata["pack"]);
        Assert.Equal("redacted", entry.Metadata["secretProjection"]);
        Assert.Equal("true", entry.Metadata["sharedEndpointConfigured"]);
        AssertSanitized(entry.Metadata);
    }

    [Fact]
    public void ActiveTelemetryProviderFlowsThroughTechnologyRuntimeCatalog()
    {
        var builder = Host.CreateApplicationBuilder();
        ConfigureSharedTelemetry(builder);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.AddCephalon(cephalon => cephalon.AddModule(new PlatformTestModule()));
        builder.Services.AddCephalonObservability(builder.Configuration);
        builder.AddCephalonOpenTelemetry();

        using var host = builder.Build();
        var catalog = host.Services.GetRequiredService<ITechnologyRuntimeCatalog>();

        Assert.Contains(catalog.GetByTechnology("observability"), surface => surface.SurfaceId == "telemetry-export-guidance");
        Assert.Contains(catalog.GetByTechnology("observability"), surface => surface.SurfaceId == "telemetry-export-opentelemetry");
    }

    private static void ConfigureSharedTelemetry(HostApplicationBuilder builder)
    {
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "https://collector.internal:4318/tenant-secret";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "false";
    }

    private static void AssertSanitized(IReadOnlyDictionary<string, string> metadata)
    {
        var values = string.Join("|", metadata.Values);

        Assert.DoesNotContain("collector.internal", values, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant-secret", values, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InstrumentationKey", values, StringComparison.OrdinalIgnoreCase);
    }
}
