using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Debezium.Configuration;
using Cephalon.Data.Debezium.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class DebeziumDataCdcPackTests
{
    private const string RuntimeId = "inventory-debezium-connector";
    private const string CaptureId = "inventory-customers-cdc";

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorClaimsCaptureAndAcceptsExternalReportsWithoutBaseOptIn()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData();
            engine.AddDebeziumData(options =>
            {
                var connector = new DebeziumConnectorOptions
                {
                    Id = RuntimeId,
                    DisplayName = "Inventory Debezium Connector",
                    Description = "Represents a Debezium-managed PostgreSQL connector that reports through the shared Cephalon CDC runtime story.",
                    ConnectClusterId = "connect-cluster-a",
                    ConnectorClass = "io.debezium.connector.postgresql.PostgresConnector",
                    SourceProviderId = "postgresql",
                    TopicPrefix = "inventory",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "managed-connector",
                    AcknowledgementMode = "connector-offset-commit",
                    ManagementMode = "observe-only",
                    ObservationStaleAfterSeconds = 180,
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true,
                    ExpectedTaskCount = 2
                };
                connector.TaskIds.Add("0");
                connector.TaskIds.Add("1");
                connector.EdgeNodeIds.Add("edge-bkk-01");
                connector.CdcCaptures.Add(new DebeziumCaptureOptions
                {
                    Id = CaptureId,
                    DisplayName = "Inventory Customers CDC",
                    Description = "Projects Debezium customer-change truth through the shared Cephalon CDC catalogs.",
                    SourceModuleId = "phase8-runtime-catalogs",
                    OutboxId = "tenant-event-outbox",
                    TopicName = "inventory.public.customers",
                    SnapshotMode = "initial"
                });

                options.Connectors.Add(connector);
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var stateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        var capture = captureCatalog.GetById(CaptureId);
        Assert.NotNull(capture);
        Assert.Equal("phase8-runtime-catalogs", capture.SourceModuleId);
        Assert.Equal(DebeziumDataOptions.ProviderId, capture.Provider);
        Assert.Equal(RuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("external-managed", capture.ExecutionBinding.ExecutionOwnership);
        Assert.Equal("managed-connector", capture.ExecutionBinding.ExecutionTopology);
        Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
        Assert.Equal("debezium-data", capture.Metadata["contributorModuleId"]);
        Assert.Equal("connect-cluster-a", capture.Metadata["connectClusterId"]);
        Assert.Equal("postgresql", capture.Metadata["sourceProviderId"]);
        Assert.Equal("inventory.public.customers", capture.Metadata["topicName"]);
        Assert.Equal("initial", capture.Metadata["snapshotMode"]);
        Assert.Equal("external-managed", capture.Metadata["publicationMode"]);

        var runtime = runtimeCatalog.GetById(RuntimeId);
        Assert.NotNull(runtime);
        Assert.Equal("external-managed", runtime.ExecutionOwnership);
        Assert.Equal("managed-connector", runtime.ExecutionTopology);
        Assert.Equal("connector-offset-commit", runtime.AcknowledgementMode);
        Assert.Equal(180, runtime.ObservationStaleAfterSeconds);
        Assert.Equal(120, runtime.ReporterLeaseSeconds);
        Assert.True(runtime.RejectConflictingReporterIds);
        Assert.Equal(["edge-bkk-01"], runtime.EdgeNodeIds);
        Assert.Equal([CaptureId], runtime.CdcCaptureIds);
        Assert.Equal("observe-only", runtime.Metadata["debeziumManagementMode"]);
        Assert.Equal("2", runtime.Metadata["debeziumExpectedTaskCount"]);
        Assert.Equal("0,1", runtime.Metadata["debeziumDeclaredTaskIds"]);

        await reportSink.ReportAsync(
            RuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: CaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T05:00:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-001",
                    capturedChangeCount: 3,
                    producedMessageCount: 3,
                    changeId: "inventory:0001",
                    checkpoint: "connect-offset:customers:42",
                    freshness: new CdcCaptureFreshnessStatus(CdcCaptureFreshnessStates.Fresh),
                    lag: new CdcCaptureLagStatus(CdcCaptureLagStates.Current, 0),
                    metadata: new Dictionary<string, string>
                    {
                        ["captureExecution"] = "external-runtime-report",
                        ["acknowledgement"] = "connector-offset-commit",
                        ["connectorState"] = "RUNNING",
                        ["reportedTaskIds"] = "0,2",
                        ["activeTaskIds"] = "0,2",
                        ["taskStateSummary"] = "RUNNING:2",
                        ["connectorGeneration"] = "42",
                        ["workerId"] = "connect-worker-a-1"
                    },
                    reporterId: "connect-worker-a",
                    edgeNodeId: "edge-bkk-01")
            ]);

        var state = stateCatalog.GetById(CaptureId);
        Assert.NotNull(state);
        Assert.Equal(RuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
        Assert.Equal("debezium-report-001", state.LastReportId);
        Assert.Equal(3, state.LastCapturedChangeCount);
        Assert.Equal(3, state.LastProducedMessageCount);
        Assert.Equal("inventory:0001", state.LastChangeId);
        Assert.Equal("connect-offset:customers:42", state.LastCheckpoint);
        Assert.Equal("external-runtime-report", state.Metadata["captureExecution"]);
        Assert.Equal("connector-offset-commit", state.Metadata["acknowledgement"]);
        Assert.Equal(RuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
        Assert.Equal("connect-worker-a", state.Metadata["cdcCaptureReporterId"]);
        Assert.Equal("edge-bkk-01", state.Metadata["cdcCaptureEdgeNodeId"]);
        Assert.Equal("RUNNING", state.Metadata["connectorState"]);
        Assert.Equal("running", state.Metadata["debeziumConnectorState"]);
        Assert.Equal("running", state.Metadata["debeziumConnectorLifecycleState"]);
        Assert.Equal("task-mismatch", state.Metadata["debeziumTaskReconciliationState"]);
        Assert.Equal("task-mismatch", state.Metadata["debeziumReconciliationState"]);
        Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", state.Metadata["debeziumReconciliationReason"]);
        Assert.Equal("observe-only", state.Metadata["debeziumManagementMode"]);
        Assert.Equal("2", state.Metadata["debeziumExpectedTaskCount"]);
        Assert.Equal("0,1", state.Metadata["debeziumDeclaredTaskIds"]);
        Assert.Equal("0,2", state.Metadata["debeziumReportedTaskIds"]);
        Assert.Equal("0,2", state.Metadata["debeziumActiveTaskIds"]);
        Assert.Equal("RUNNING:2", state.Metadata["debeziumTaskStateSummary"]);
        Assert.Equal("42", state.Metadata["debeziumConnectorGeneration"]);
        Assert.Equal("connect-worker-a-1", state.Metadata["debeziumWorkerId"]);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.Metadata["observationFreshnessState"]);
        Assert.Equal("180", state.Metadata["observationStaleAfterSeconds"]);
        Assert.Equal("2026-04-23T05:02:00.0000000+00:00", state.Metadata["cdcCaptureReporterLeaseExpiresAtUtc"]);

        runtime = runtimeCatalog.GetById(RuntimeId);
        Assert.NotNull(runtime);
        Assert.True(runtime.Summary.HasReports);
        Assert.Equal(CaptureId, runtime.Summary.LastCdcCaptureId);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, runtime.Summary.LastOutcome);
        Assert.Equal("debezium-report-001", runtime.Summary.LastReportId);
        Assert.Equal("connect-worker-a", runtime.Summary.LastReporterId);
        Assert.Equal("connect-worker-a", runtime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-23T05:02:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(["edge-bkk-01"], runtime.Summary.ObservedEdgeNodeIds);
        Assert.Equal("edge-bkk-01", runtime.Summary.LastEdgeNodeId);
        Assert.Equal(3, runtime.Summary.TotalCapturedChangeCount);
        Assert.Equal(3, runtime.Summary.TotalProducedMessageCount);
        Assert.Equal("connector-offset-commit", runtime.Summary.LastAcknowledgement);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, runtime.Summary.ObservationFreshness.State);
        Assert.Equal("running", runtime.Metadata["debeziumConnectorState"]);
        Assert.Equal("running", runtime.Metadata["debeziumConnectorLifecycleState"]);
        Assert.Equal("task-mismatch", runtime.Metadata["debeziumTaskReconciliationState"]);
        Assert.Equal("task-mismatch", runtime.Metadata["debeziumReconciliationState"]);
        Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", runtime.Metadata["debeziumReconciliationReason"]);
        Assert.Equal("0,1", runtime.Metadata["debeziumDeclaredTaskIds"]);
        Assert.Equal("0,2", runtime.Metadata["debeziumReportedTaskIds"]);
        Assert.Equal("0,2", runtime.Metadata["debeziumActiveTaskIds"]);
        Assert.Equal("RUNNING:2", runtime.Metadata["debeziumTaskStateSummary"]);
        Assert.Equal("42", runtime.Metadata["debeziumConnectorGeneration"]);
        Assert.Equal("connect-worker-a-1", runtime.Metadata["debeziumWorkerId"]);
    }
}
