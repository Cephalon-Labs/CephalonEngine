using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Benchmarks.Support;
using Cephalon.Data.Debezium.Configuration;
using Cephalon.Data.Debezium.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures repeated CDC execution-runtime catalog projections used by operator drill-downs.
/// The benchmark builds Debezium-managed external runtimes through the public engine composition path,
/// reports live observations, and then exercises the versioned snapshot used by state and category filters.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class CdcExecutionRuntimeCatalogBenchmarks
{
    private const int RuntimeCount = 24;
    private const int CapturesPerRuntime = 2;
    private const int CatalogOperationsPerIteration = 2048;
    private const string SourceModuleId = "benchmark-data-module";
    private const string OutboxId = "benchmark-outbox";

    private static readonly DateTimeOffset ObservedAtUtc = new(2026, 5, 7, 5, 0, 0, TimeSpan.Zero);

    private ServiceProvider provider = null!;
    private ICdcCaptureExecutionRuntimeCatalog runtimeCatalog = null!;

    /// <summary>
    /// Builds a representative external-managed CDC runtime catalog and warms the filter snapshot.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(ObservedAtUtc.AddSeconds(30)));

        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["cqrs"],
            transports: ["rest-api"]));
        builder.AddModule(new BenchmarkCdcOutboxModule());
        builder.AddData();
        builder.AddDebeziumData(options =>
        {
            for (var runtimeIndex = 0; runtimeIndex < RuntimeCount; runtimeIndex++)
            {
                options.Connectors.Add(CreateConnector(runtimeIndex));
            }
        });

        using var runtime = builder.Build();
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider);

        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        for (var runtimeIndex = 0; runtimeIndex < RuntimeCount; runtimeIndex++)
        {
            await reportSink.ReportAsync(
                RuntimeId(runtimeIndex),
                CreateObservations(runtimeIndex));
        }

        runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        _ = runtimeCatalog.Runtimes;
        _ = runtimeCatalog.GetByManagedConnectorDriftState(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted);
        _ = runtimeCatalog.GetByManagedConnectorDryRunState(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred);
        _ = runtimeCatalog.GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.OperatorOnly);
        _ = runtimeCatalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerState(
            CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.OperatorOnly);
    }

    /// <summary>
    /// Releases all DI resources.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        provider.Dispose();
    }

    /// <summary>
    /// Enumerates the full CDC execution-runtime catalog after external report enrichment.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public int EnumerateRuntimes()
    {
        var total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            foreach (var runtime in runtimeCatalog.Runtimes)
            {
                total += runtime.CdcCaptureIds.Count + runtime.Summary.ReportedCaptureCount;
            }
        }

        return total;
    }

    /// <summary>
    /// Filters repeated managed-connector drift posture queries against the warmed runtime snapshot.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public int FilterManagedConnectorDriftState()
    {
        var total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            total += runtimeCatalog
                .GetByManagedConnectorDriftState(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted)
                .Count;
        }

        return total;
    }

    /// <summary>
    /// Filters repeated dry-run posture queries for observe-only managed connectors.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public int FilterManagedConnectorDryRunState()
    {
        var total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            total += runtimeCatalog
                .GetByManagedConnectorDryRunState(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred)
                .Count;
        }

        return total;
    }

    /// <summary>
    /// Filters repeated command-issuance posture queries for operator-owned managed connectors.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public int FilterManagedConnectorCommandIssuanceState()
    {
        var total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            total += runtimeCatalog
                .GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.OperatorOnly)
                .Count;
        }

        return total;
    }

    /// <summary>
    /// Exercises a compact multi-selector operator drill-down over the same warmed snapshot.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public int FilterManagedConnectorOperatorSelectors()
    {
        var total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            total += runtimeCatalog
                .GetByManagedConnectorDriftCategory(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectClusterMismatch)
                .Count;
            total += runtimeCatalog
                .GetByManagedConnectorDryRunState(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred)
                .Count;
            total += runtimeCatalog
                .GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.OperatorOnly)
                .Count;
            total += runtimeCatalog
                .GetByManagedConnectorProviderSpecificControlPlaneMaterializerState(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.OperatorOnly)
                .Count;
        }

        return total;
    }

    private static DebeziumConnectorOptions CreateConnector(int runtimeIndex)
    {
        var connector = new DebeziumConnectorOptions
        {
            Id = RuntimeId(runtimeIndex),
            DisplayName = $"Benchmark CDC connector {runtimeIndex:D2}",
            Description = "Benchmark external-managed CDC runtime used by the hot-path catalog guardrail.",
            ConnectClusterId = "connect-cluster-a",
            ConnectorClass = "io.debezium.connector.postgresql.PostgresConnector",
            SourceProviderId = "postgresql",
            TopicPrefix = $"benchmark-{runtimeIndex:D2}",
            ManagementMode = "observe-only",
            ObservationStaleAfterSeconds = 180,
            ReporterLeaseSeconds = 120,
            RejectConflictingReporterIds = true,
            ExpectedTaskCount = 2
        };

        connector.TaskIds.Add("0");
        connector.TaskIds.Add("1");
        connector.EdgeNodeIds.Add("edge-benchmark-01");
        connector.Metadata["managedConnectorProviderSpecificControlPlaneProviderId"] = "debezium";
        connector.Metadata["managedConnectorProviderSpecificControlPlaneMaterializerId"] = "debezium-kafka-connect-rest";
        connector.Metadata["managedConnectorProviderSpecificControlPlaneTransportKind"] = "http-rest";
        connector.Metadata["managedConnectorProviderSpecificControlPlaneSurfaceId"] = "debezium-kafka-connect-rest";
        connector.Metadata["managedConnectorProviderSpecificControlPlaneConnectorId"] = connector.Id;

        for (var captureIndex = 0; captureIndex < CapturesPerRuntime; captureIndex++)
        {
            var captureId = CaptureId(runtimeIndex, captureIndex);
            connector.CdcCaptures.Add(new DebeziumCaptureOptions
            {
                Id = captureId,
                DisplayName = $"Benchmark capture {runtimeIndex:D2}-{captureIndex:D2}",
                Description = "Benchmark Debezium-managed CDC capture bound to an external runtime.",
                SourceModuleId = SourceModuleId,
                SourceId = $"benchmark-source-{runtimeIndex:D2}",
                OutboxId = OutboxId,
                TopicName = $"benchmark.public.table_{runtimeIndex:D2}_{captureIndex:D2}",
                SnapshotMode = "initial"
            });
        }

        return connector;
    }

    private static List<CdcCaptureRuntimeObservation> CreateObservations(int runtimeIndex)
    {
        var observations = new List<CdcCaptureRuntimeObservation>(CapturesPerRuntime);
        for (var captureIndex = 0; captureIndex < CapturesPerRuntime; captureIndex++)
        {
            var captureId = CaptureId(runtimeIndex, captureIndex);
            var isDrifted = runtimeIndex % 2 == 1;
            observations.Add(new CdcCaptureRuntimeObservation(
                cdcCaptureId: captureId,
                outcome: CdcCaptureRuntimeOutcomes.Captured,
                observedAtUtc: ObservedAtUtc.AddSeconds(runtimeIndex + captureIndex),
                reportId: $"benchmark-report-{runtimeIndex:D2}-{captureIndex:D2}",
                capturedChangeCount: runtimeIndex + captureIndex + 1,
                producedMessageCount: runtimeIndex + captureIndex + 1,
                changeId: $"benchmark-change-{runtimeIndex:D2}-{captureIndex:D2}",
                checkpoint: $"connect-offset:{runtimeIndex:D2}:{captureIndex:D2}",
                metadata: new Dictionary<string, string>
                {
                    ["connectorState"] = "RUNNING",
                    ["connectClusterId"] = isDrifted ? "connect-cluster-b" : "connect-cluster-a",
                    ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                    ["sourceProviderId"] = "postgresql",
                    ["reportedTaskIds"] = isDrifted ? "0,2" : "0,1",
                    ["activeTaskIds"] = isDrifted ? "0,2" : "0,1",
                    ["taskStateSummary"] = "RUNNING:2",
                    ["connectorGeneration"] = runtimeIndex.ToString("D2", CultureInfo.InvariantCulture),
                    ["workerId"] = $"connect-worker-{runtimeIndex % 3:D2}"
                },
                reporterId: $"connect-worker-{runtimeIndex % 3:D2}",
                edgeNodeId: "edge-benchmark-01"));
        }

        return observations;
    }

    private static string RuntimeId(int runtimeIndex) => $"benchmark-debezium-runtime-{runtimeIndex:D2}";

    private static string CaptureId(int runtimeIndex, int captureIndex) => $"benchmark-capture-{runtimeIndex:D2}-{captureIndex:D2}";

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class BenchmarkCdcOutboxModule : ModuleBase, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: SourceModuleId,
            displayName: "Benchmark Data Module",
            description: "Benchmark source module that owns the CDC outbox descriptor.",
            tags: ["benchmark", "data", "cdc"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: OutboxId,
                displayName: "Benchmark Outbox",
                description: "In-memory benchmark outbox boundary used by CDC runtime catalog guardrails.",
                sourceModuleId: SourceModuleId,
                provider: "benchmark",
                mode: "in-memory"));
        }
    }
}
