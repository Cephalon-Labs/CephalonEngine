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
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class DebeziumDataCdcPackTests
{
    private const string RuntimeId = "inventory-debezium-connector";
    private const string CaptureId = "inventory-customers-cdc";
    private const string ObserveOnlyRuntimeId = "inventory-observe-only-connector";
    private const string ObserveOnlyCaptureId = "inventory-orders-cdc";
    private const string FutureControlPlaneRuntimeId = "inventory-managed-connector";
    private const string FutureControlPlaneCaptureId = "inventory-products-cdc";
    private const string OutOfPolicyRuntimeId = "inventory-out-of-policy-connector";
    private const string OutOfPolicyCaptureId = "inventory-suppliers-cdc";
    private const string WaitingRuntimeId = "inventory-waiting-connector";
    private const string WaitingCaptureId = "inventory-shipments-cdc";
    private const string BlockedRuntimeId = "inventory-blocked-connector";
    private const string BlockedCaptureId = "inventory-payments-cdc";
    private const string ReadyRuntimeId = "inventory-ready-connector";
    private const string ReadyCaptureId = "inventory-invoices-cdc";
    private const string PauseRequiredRuntimeId = "inventory-pause-required-connector";
    private const string PauseRequiredCaptureId = "inventory-returns-cdc";
    private const string PauseSatisfiedRuntimeId = "inventory-pause-satisfied-connector";
    private const string PauseSatisfiedCaptureId = "inventory-refunds-cdc";
    private const string DeleteRequiredRuntimeId = "inventory-delete-required-connector";
    private const string DeleteRequiredCaptureId = "inventory-archives-cdc";

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorClaimsCaptureAndAcceptsExternalReportsWithoutBaseOptIn()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T05:01:30Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: RuntimeId,
                    captureId: CaptureId,
                    displayName: "Inventory Debezium Connector",
                    captureDisplayName: "Inventory Customers CDC",
                    captureDescription: "Projects Debezium customer-change truth through the shared Cephalon CDC catalogs.",
                    connectClusterId: "connect-cluster-a",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory",
                    managementMode: "observe-only",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"],
                    edgeNodeIds: ["edge-bkk-01"]));
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
        Assert.Equal("observe-only", runtime.Metadata["managedConnectorManagementMode"]);
        Assert.Equal("observe-only", runtime.Metadata["debeziumManagementMode"]);
        Assert.Equal("2", runtime.Metadata["managedConnectorExpectedTaskCount"]);
        Assert.Equal("2", runtime.Metadata["debeziumExpectedTaskCount"]);
        Assert.Equal("0,1", runtime.Metadata["managedConnectorDeclaredTaskIds"]);
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
                        ["connectClusterId"] = "connect-cluster-b",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
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
        Assert.Equal("observe-only", state.Metadata["managedConnectorManagementMode"]);
        Assert.Equal("observe-only", state.Metadata["debeziumManagementMode"]);
        Assert.Equal("connect-cluster-a", state.Metadata["managedConnectorDeclaredConnectClusterId"]);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", state.Metadata["managedConnectorDeclaredConnectorClass"]);
        Assert.Equal("postgresql", state.Metadata["managedConnectorDeclaredSourceProviderId"]);
        Assert.Equal("connect-cluster-b", state.Metadata["managedConnectorReportedConnectClusterId"]);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", state.Metadata["managedConnectorReportedConnectorClass"]);
        Assert.Equal("postgresql", state.Metadata["managedConnectorReportedSourceProviderId"]);
        Assert.Equal("connect-cluster-a", state.Metadata["debeziumDeclaredConnectClusterId"]);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", state.Metadata["debeziumDeclaredConnectorClass"]);
        Assert.Equal("postgresql", state.Metadata["debeziumDeclaredSourceProviderId"]);
        Assert.Equal("connect-cluster-b", state.Metadata["debeziumReportedConnectClusterId"]);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", state.Metadata["debeziumReportedConnectorClass"]);
        Assert.Equal("postgresql", state.Metadata["debeziumReportedSourceProviderId"]);
        Assert.Equal("2", state.Metadata["managedConnectorExpectedTaskCount"]);
        Assert.Equal("2", state.Metadata["debeziumExpectedTaskCount"]);
        Assert.Equal("0,1", state.Metadata["managedConnectorDeclaredTaskIds"]);
        Assert.Equal("0,1", state.Metadata["debeziumDeclaredTaskIds"]);
        Assert.Equal("0,2", state.Metadata["managedConnectorReportedTaskIds"]);
        Assert.Equal("0,2", state.Metadata["debeziumReportedTaskIds"]);
        Assert.Equal("0,2", state.Metadata["managedConnectorActiveTaskIds"]);
        Assert.Equal("0,2", state.Metadata["debeziumActiveTaskIds"]);
        Assert.Equal("task-mismatch", state.Metadata["managedConnectorTaskReconciliationState"]);
        Assert.Equal("task-mismatch", state.Metadata["managedConnectorReconciliationState"]);
        Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", state.Metadata["managedConnectorReconciliationReason"]);
        Assert.Equal("RUNNING:2", state.Metadata["debeziumTaskStateSummary"]);
        Assert.Equal("42", state.Metadata["debeziumConnectorGeneration"]);
        Assert.Equal("connect-worker-a-1", state.Metadata["debeziumWorkerId"]);
        Assert.Equal("180", state.Metadata["observationStaleAfterSeconds"]);
        Assert.Equal("2026-04-23T05:02:00.0000000+00:00", state.Metadata["cdcCaptureReporterLeaseExpiresAtUtc"]);

        runtime = runtimeCatalog.GetById(RuntimeId);
        Assert.NotNull(runtime);
        Assert.True(runtime.Summary.HasReports);
        Assert.Equal(CaptureId, runtime.Summary.LastCdcCaptureId);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, runtime.Summary.LastOutcome);
        Assert.Equal("debezium-report-001", runtime.Summary.LastReportId);
        Assert.Equal("connect-worker-a", runtime.Summary.LastReporterId);
        Assert.Equal(["edge-bkk-01"], runtime.Summary.ObservedEdgeNodeIds);
        Assert.Equal("edge-bkk-01", runtime.Summary.LastEdgeNodeId);
        Assert.Equal(3, runtime.Summary.TotalCapturedChangeCount);
        Assert.Equal(3, runtime.Summary.TotalProducedMessageCount);
        Assert.Equal("connector-offset-commit", runtime.Summary.LastAcknowledgement);
        Assert.Equal("observe-only", runtime.ManagedConnectorGovernance.State);
        Assert.Equal("observe-only", runtime.ManagedConnectorGovernance.ManagementMode);
        Assert.Equal("connect-cluster-a", runtime.ManagedConnectorGovernance.ConnectClusterId);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.ManagedConnectorGovernance.ConnectorClass);
        Assert.Equal("postgresql", runtime.ManagedConnectorGovernance.SourceProviderId);
        Assert.Equal(2, runtime.ManagedConnectorGovernance.ExpectedTaskCount);
        Assert.Equal(2, runtime.ManagedConnectorGovernance.ReportedTaskCount);
        Assert.Equal(["0", "1"], runtime.ManagedConnectorGovernance.DeclaredTaskIds);
        Assert.Equal(["0", "2"], runtime.ManagedConnectorGovernance.ReportedTaskIds);
        Assert.Equal(["0", "2"], runtime.ManagedConnectorGovernance.ActiveTaskIds);
        Assert.Equal("running", runtime.ManagedConnectorGovernance.ConnectorLifecycleState);
        Assert.Equal("task-mismatch", runtime.ManagedConnectorGovernance.TaskReconciliationState);
        Assert.Equal("task-mismatch", runtime.ManagedConnectorGovernance.ReconciliationState);
        Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", runtime.ManagedConnectorGovernance.ReconciliationReason);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.KeepObserveOnly, runtime.ManagedConnectorGovernance.RecommendedActionId);
        Assert.True(runtime.ManagedConnectorGovernance.IsObserveOnly);
        Assert.False(runtime.ManagedConnectorGovernance.RequiresAttention);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted, runtime.ManagedConnectorDrift.State);
        Assert.Equal("observe-only", runtime.ManagedConnectorDrift.ManagementMode);
        Assert.Equal("connect-cluster-a", runtime.ManagedConnectorDrift.DeclaredConnectClusterId);
        Assert.Equal("connect-cluster-b", runtime.ManagedConnectorDrift.ReportedConnectClusterId);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.ManagedConnectorDrift.DeclaredConnectorClass);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.ManagedConnectorDrift.ReportedConnectorClass);
        Assert.Equal("postgresql", runtime.ManagedConnectorDrift.DeclaredSourceProviderId);
        Assert.Equal("postgresql", runtime.ManagedConnectorDrift.ReportedSourceProviderId);
        Assert.Equal(2, runtime.ManagedConnectorDrift.ExpectedTaskCount);
        Assert.Equal(2, runtime.ManagedConnectorDrift.ReportedTaskCount);
        Assert.Equal(["0", "1"], runtime.ManagedConnectorDrift.DeclaredTaskIds);
        Assert.Equal(["0", "2"], runtime.ManagedConnectorDrift.ReportedTaskIds);
        Assert.Equal(["0", "2"], runtime.ManagedConnectorDrift.ActiveTaskIds);
        Assert.Equal(["1"], runtime.ManagedConnectorDrift.MissingDeclaredTaskIds);
        Assert.Equal(["2"], runtime.ManagedConnectorDrift.UnexpectedReportedTaskIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingDeclaredTaskReports, runtime.ManagedConnectorDrift.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.UnexpectedReportedTasks, runtime.ManagedConnectorDrift.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectClusterMismatch, runtime.ManagedConnectorDrift.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.InvestigateDrift, runtime.ManagedConnectorDrift.RecommendedActionId);
        Assert.True(runtime.ManagedConnectorDrift.CanEvaluateDrift);
        Assert.True(runtime.ManagedConnectorDrift.RequiresAttention);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired, runtime.ManagedConnectorActionPlan.State);
        Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift], runtime.ManagedConnectorActionPlan.ActionIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift, runtime.ManagedConnectorActionPlan.PrimaryActionId);
        Assert.Equal(CdcCaptureExecutionRuntimeRemediationStates.Ready, runtime.ManagedConnectorActionPlan.RemediationState);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, runtime.ManagedConnectorActionPlan.GovernanceState);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted, runtime.ManagedConnectorActionPlan.DriftState);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.DriftDetected, runtime.ManagedConnectorActionPlan.CategoryIds);
        Assert.True(runtime.ManagedConnectorActionPlan.RequiresAction);
        Assert.Equal("running", runtime.Metadata["debeziumConnectorState"]);
        Assert.Equal("running", runtime.Metadata["debeziumConnectorLifecycleState"]);
        Assert.Equal("task-mismatch", runtime.Metadata["debeziumTaskReconciliationState"]);
        Assert.Equal("task-mismatch", runtime.Metadata["debeziumReconciliationState"]);
        Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", runtime.Metadata["debeziumReconciliationReason"]);
        Assert.Equal("observe-only", runtime.Metadata["managedConnectorManagementMode"]);
        Assert.Equal("connect-cluster-a", runtime.Metadata["managedConnectorDeclaredConnectClusterId"]);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.Metadata["managedConnectorDeclaredConnectorClass"]);
        Assert.Equal("postgresql", runtime.Metadata["managedConnectorDeclaredSourceProviderId"]);
        Assert.Equal("connect-cluster-b", runtime.Metadata["managedConnectorReportedConnectClusterId"]);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.Metadata["managedConnectorReportedConnectorClass"]);
        Assert.Equal("postgresql", runtime.Metadata["managedConnectorReportedSourceProviderId"]);
        Assert.Equal("2", runtime.Metadata["managedConnectorExpectedTaskCount"]);
        Assert.Equal("0,1", runtime.Metadata["managedConnectorDeclaredTaskIds"]);
        Assert.Equal("task-mismatch", runtime.Metadata["managedConnectorTaskReconciliationState"]);
        Assert.Equal("task-mismatch", runtime.Metadata["managedConnectorReconciliationState"]);
        Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", runtime.Metadata["managedConnectorReconciliationReason"]);
        Assert.Equal("0,2", runtime.Metadata["managedConnectorReportedTaskIds"]);
        Assert.Equal("0,2", runtime.Metadata["managedConnectorActiveTaskIds"]);
        Assert.Equal("0,2", runtime.Metadata["debeziumReportedTaskIds"]);
        Assert.Equal("0,2", runtime.Metadata["debeziumActiveTaskIds"]);
        Assert.Equal("connect-cluster-a", runtime.Metadata["debeziumDeclaredConnectClusterId"]);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.Metadata["debeziumDeclaredConnectorClass"]);
        Assert.Equal("postgresql", runtime.Metadata["debeziumDeclaredSourceProviderId"]);
        Assert.Equal("connect-cluster-b", runtime.Metadata["debeziumReportedConnectClusterId"]);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.Metadata["debeziumReportedConnectorClass"]);
        Assert.Equal("postgresql", runtime.Metadata["debeziumReportedSourceProviderId"]);
        Assert.Equal("RUNNING:2", runtime.Metadata["debeziumTaskStateSummary"]);
        Assert.Equal("42", runtime.Metadata["debeziumConnectorGeneration"]);
        Assert.Equal("connect-worker-a-1", runtime.Metadata["debeziumWorkerId"]);
    }

    [Fact]
    public void AddDebeziumData_ManagedConnectorGovernanceCatalogExposesSharedStateAndCategoryFilters()
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: ObserveOnlyRuntimeId,
                    captureId: ObserveOnlyCaptureId,
                    displayName: "Inventory Observe-Only Connector",
                    captureDisplayName: "Inventory Orders CDC",
                    captureDescription: "Projects inventory order CDC truth through the shared Cephalon runtime catalog.",
                    connectClusterId: "connect-cluster-a",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: FutureControlPlaneRuntimeId,
                    captureId: FutureControlPlaneCaptureId,
                    displayName: "Inventory Managed Connector",
                    captureDisplayName: "Inventory Products CDC",
                    captureDescription: "Declares a future write-path management mode while still using shared runtime truth.",
                    connectClusterId: "connect-cluster-b",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-products",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: OutOfPolicyRuntimeId,
                    captureId: OutOfPolicyCaptureId,
                    displayName: "Inventory Out-of-Policy Connector",
                    captureDisplayName: "Inventory Suppliers CDC",
                    captureDescription: "Omits connector-cluster identity so governance falls back to an out-of-policy answer.",
                    connectClusterId: null,
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-suppliers",
                    managementMode: "observe-only",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        var observeOnly = runtimeCatalog.GetById(ObserveOnlyRuntimeId);
        var futureControlPlane = runtimeCatalog.GetById(FutureControlPlaneRuntimeId);
        var outOfPolicy = runtimeCatalog.GetById(OutOfPolicyRuntimeId);

        Assert.NotNull(observeOnly);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, observeOnly.ManagedConnectorGovernance.State);
        Assert.Equal("observe-only", observeOnly.ManagedConnectorGovernance.ManagementMode);
        Assert.Equal(["0"], observeOnly.ManagedConnectorGovernance.DeclaredTaskIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.KeepObserveOnly, observeOnly.ManagedConnectorGovernance.RecommendedActionId);
        Assert.False(observeOnly.ManagedConnectorGovernance.RequiresAttention);

        Assert.NotNull(futureControlPlane);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.FutureControlPlane, futureControlPlane.ManagedConnectorGovernance.State);
        Assert.Equal("apply-and-reconcile", futureControlPlane.ManagedConnectorGovernance.ManagementMode);
        Assert.Equal(["0", "1"], futureControlPlane.ManagedConnectorGovernance.DeclaredTaskIds);
        Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.FutureControlPlaneMode], futureControlPlane.ManagedConnectorGovernance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.DeferControlPlane, futureControlPlane.ManagedConnectorGovernance.RecommendedActionId);
        Assert.True(futureControlPlane.ManagedConnectorGovernance.RequiresControlPlaneSupport);
        Assert.True(futureControlPlane.ManagedConnectorGovernance.RequiresAttention);

        Assert.NotNull(outOfPolicy);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.OutOfPolicy, outOfPolicy.ManagedConnectorGovernance.State);
        Assert.Equal("observe-only", outOfPolicy.ManagedConnectorGovernance.ManagementMode);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectClusterId, outOfPolicy.ManagedConnectorGovernance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.CompleteGovernanceDeclaration, outOfPolicy.ManagedConnectorGovernance.RecommendedActionId);
        Assert.True(outOfPolicy.ManagedConnectorGovernance.RequiresAttention);
        Assert.True(outOfPolicy.ManagedConnectorGovernance.IsOutOfPolicy);

        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorGovernanceState(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorGovernanceState(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.FutureControlPlane)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorGovernanceState(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.OutOfPolicy)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorGovernanceCategory(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.FutureControlPlaneMode)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorGovernanceCategory(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectClusterId)
            .Select(static runtime => runtime.Id)
            .ToArray());
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorDriftCatalogExposesSharedStateAndCategoryFilters()
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: ObserveOnlyRuntimeId,
                    captureId: ObserveOnlyCaptureId,
                    displayName: "Inventory Observe-Only Connector",
                    captureDisplayName: "Inventory Orders CDC",
                    captureDescription: "Projects inventory order CDC truth through the shared Cephalon runtime catalog.",
                    connectClusterId: "connect-cluster-a",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-orders",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: FutureControlPlaneRuntimeId,
                    captureId: FutureControlPlaneCaptureId,
                    displayName: "Inventory Managed Connector",
                    captureDisplayName: "Inventory Products CDC",
                    captureDescription: "Declares a future write-path management mode while still using shared runtime truth.",
                    connectClusterId: "connect-cluster-b",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-products",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: OutOfPolicyRuntimeId,
                    captureId: OutOfPolicyCaptureId,
                    displayName: "Inventory Out-of-Policy Connector",
                    captureDisplayName: "Inventory Suppliers CDC",
                    captureDescription: "Omits connector-cluster identity so governance falls back to an out-of-policy answer.",
                    connectClusterId: null,
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-suppliers",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            ObserveOnlyRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: ObserveOnlyCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-observe-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-a",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-a")
            ]);

        await reportSink.ReportAsync(
            FutureControlPlaneRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: FutureControlPlaneCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-managed-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-b",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-b")
            ]);

        var observeOnly = runtimeCatalog.GetById(ObserveOnlyRuntimeId);
        var futureControlPlane = runtimeCatalog.GetById(FutureControlPlaneRuntimeId);
        var outOfPolicy = runtimeCatalog.GetById(OutOfPolicyRuntimeId);

        Assert.NotNull(observeOnly);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.InSync, observeOnly.ManagedConnectorDrift.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.None, observeOnly.ManagedConnectorDrift.RecommendedActionId);
        Assert.True(observeOnly.ManagedConnectorDrift.IsInSync);
        Assert.True(observeOnly.ManagedConnectorDrift.CanEvaluateDrift);

        Assert.NotNull(futureControlPlane);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted, futureControlPlane.ManagedConnectorDrift.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.TaskCountMismatch, futureControlPlane.ManagedConnectorDrift.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingDeclaredTaskReports, futureControlPlane.ManagedConnectorDrift.CategoryIds);
        Assert.Equal(["1"], futureControlPlane.ManagedConnectorDrift.MissingDeclaredTaskIds);
        Assert.Equal([], futureControlPlane.ManagedConnectorDrift.UnexpectedReportedTaskIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.InvestigateDrift, futureControlPlane.ManagedConnectorDrift.RecommendedActionId);
        Assert.True(futureControlPlane.ManagedConnectorDrift.RequiresAttention);

        Assert.NotNull(outOfPolicy);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown, outOfPolicy.ManagedConnectorDrift.State);
        Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskTopologyUnavailable], outOfPolicy.ManagedConnectorDrift.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.WaitForRuntimeReport, outOfPolicy.ManagedConnectorDrift.RecommendedActionId);
        Assert.False(outOfPolicy.ManagedConnectorDrift.CanEvaluateDrift);

        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorDriftState(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.InSync)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorDriftState(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorDriftState(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorDriftCategory(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.TaskCountMismatch)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorDriftCategory(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskTopologyUnavailable)
            .Select(static runtime => runtime.Id)
            .ToArray());
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorActionPlanCatalogExposesSharedStateAndActionFilters()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: ObserveOnlyRuntimeId,
                    captureId: ObserveOnlyCaptureId,
                    displayName: "Inventory Observe-Only Connector",
                    captureDisplayName: "Inventory Orders CDC",
                    captureDescription: "Projects inventory order CDC truth through the shared Cephalon runtime catalog.",
                    connectClusterId: "connect-cluster-a",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-orders",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: FutureControlPlaneRuntimeId,
                    captureId: FutureControlPlaneCaptureId,
                    displayName: "Inventory Managed Connector",
                    captureDisplayName: "Inventory Products CDC",
                    captureDescription: "Declares a future write-path management mode while still using shared runtime truth.",
                    connectClusterId: "connect-cluster-b",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-products",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: OutOfPolicyRuntimeId,
                    captureId: OutOfPolicyCaptureId,
                    displayName: "Inventory Out-of-Policy Connector",
                    captureDisplayName: "Inventory Suppliers CDC",
                    captureDescription: "Omits connector-cluster identity so governance falls back to an out-of-policy answer.",
                    connectClusterId: null,
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-suppliers",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: WaitingRuntimeId,
                    captureId: WaitingCaptureId,
                    displayName: "Inventory Waiting Connector",
                    captureDisplayName: "Inventory Shipments CDC",
                    captureDescription: "Declares a healthy observe-only baseline but has not reported task topology yet.",
                    connectClusterId: "connect-cluster-c",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-shipments",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: BlockedRuntimeId,
                    captureId: BlockedCaptureId,
                    displayName: "Inventory Blocked Connector",
                    captureDisplayName: "Inventory Payments CDC",
                    captureDescription: "Reports a failed runtime outcome so remediation blocks deeper connector follow-through.",
                    connectClusterId: "connect-cluster-d",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-payments",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            ObserveOnlyRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: ObserveOnlyCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-observe-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-a",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-a")
            ]);

        await reportSink.ReportAsync(
            FutureControlPlaneRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: FutureControlPlaneCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:30Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-managed-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-b",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-b")
            ]);

        await reportSink.ReportAsync(
            BlockedRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: BlockedCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-blocked-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "FAILED",
                        ["connectClusterId"] = "connect-cluster-d",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-d")
            ]);

        var observeOnly = runtimeCatalog.GetById(ObserveOnlyRuntimeId);
        var futureControlPlane = runtimeCatalog.GetById(FutureControlPlaneRuntimeId);
        var outOfPolicy = runtimeCatalog.GetById(OutOfPolicyRuntimeId);
        var waiting = runtimeCatalog.GetById(WaitingRuntimeId);
        var blocked = runtimeCatalog.GetById(BlockedRuntimeId);

        Assert.NotNull(observeOnly);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Observe, observeOnly.ManagedConnectorActionPlan.State);
        Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly], observeOnly.ManagedConnectorActionPlan.ActionIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly, observeOnly.ManagedConnectorActionPlan.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.ObserveOnlySteadyState, observeOnly.ManagedConnectorActionPlan.CategoryIds);
        Assert.True(observeOnly.ManagedConnectorActionPlan.IsObserve);
        Assert.False(observeOnly.ManagedConnectorActionPlan.RequiresAction);

        Assert.NotNull(futureControlPlane);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired, futureControlPlane.ManagedConnectorActionPlan.State);
        Assert.Equal(
            [CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.DeferControlPlane],
            futureControlPlane.ManagedConnectorActionPlan.ActionIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift, futureControlPlane.ManagedConnectorActionPlan.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.DriftDetected, futureControlPlane.ManagedConnectorActionPlan.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.FutureControlPlaneDeferred, futureControlPlane.ManagedConnectorActionPlan.CategoryIds);

        Assert.NotNull(outOfPolicy);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired, outOfPolicy.ManagedConnectorActionPlan.State);
        Assert.Equal(
            [CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.CompleteGovernanceDeclaration, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.WaitForRuntimeReport],
            outOfPolicy.ManagedConnectorActionPlan.ActionIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.CompleteGovernanceDeclaration, outOfPolicy.ManagedConnectorActionPlan.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorActionPlan.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.WaitingForRuntimeTruth, outOfPolicy.ManagedConnectorActionPlan.CategoryIds);

        Assert.NotNull(waiting);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Waiting, waiting.ManagedConnectorActionPlan.State);
        Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.WaitForRuntimeReport], waiting.ManagedConnectorActionPlan.ActionIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.WaitForRuntimeReport, waiting.ManagedConnectorActionPlan.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.WaitingForRuntimeTruth, waiting.ManagedConnectorActionPlan.CategoryIds);
        Assert.True(waiting.ManagedConnectorActionPlan.IsWaiting);

        Assert.NotNull(blocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Blocked, blocked.ManagedConnectorActionPlan.State);
        Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.ResolveRuntimeRemediation], blocked.ManagedConnectorActionPlan.ActionIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.ResolveRuntimeRemediation, blocked.ManagedConnectorActionPlan.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.BlockingRemediation, blocked.ManagedConnectorActionPlan.CategoryIds);
        Assert.True(blocked.ManagedConnectorActionPlan.IsBlocked);

        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionPlanState(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Observe)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionPlanState(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([WaitingRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionPlanState(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Waiting)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionPlanState(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Blocked)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionId(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionId(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.CompleteGovernanceDeclaration)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionId(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionId(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.DeferControlPlane)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorActionId(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.ResolveRuntimeRemediation)
            .Select(static runtime => runtime.Id)
            .ToArray());
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorWritePathReadinessCatalogExposesSharedStateAndCategoryFilters()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: ObserveOnlyRuntimeId,
                    captureId: ObserveOnlyCaptureId,
                    displayName: "Inventory Observe-Only Connector",
                    captureDisplayName: "Inventory Orders CDC",
                    captureDescription: "Projects inventory order CDC truth through the shared Cephalon runtime catalog.",
                    connectClusterId: "connect-cluster-a",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-orders",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: FutureControlPlaneRuntimeId,
                    captureId: FutureControlPlaneCaptureId,
                    displayName: "Inventory Managed Connector",
                    captureDisplayName: "Inventory Products CDC",
                    captureDescription: "Declares a future write-path management mode while still using shared runtime truth.",
                    connectClusterId: "connect-cluster-b",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-products",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: OutOfPolicyRuntimeId,
                    captureId: OutOfPolicyCaptureId,
                    displayName: "Inventory Out-of-Policy Connector",
                    captureDisplayName: "Inventory Suppliers CDC",
                    captureDescription: "Omits connector-cluster identity so governance falls back to an out-of-policy answer.",
                    connectClusterId: null,
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-suppliers",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: WaitingRuntimeId,
                    captureId: WaitingCaptureId,
                    displayName: "Inventory Waiting Connector",
                    captureDisplayName: "Inventory Shipments CDC",
                    captureDescription: "Declares a healthy observe-only baseline but has not reported task topology yet.",
                    connectClusterId: "connect-cluster-c",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-shipments",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: BlockedRuntimeId,
                    captureId: BlockedCaptureId,
                    displayName: "Inventory Blocked Connector",
                    captureDisplayName: "Inventory Payments CDC",
                    captureDescription: "Reports a failed runtime outcome so remediation blocks deeper connector follow-through.",
                    connectClusterId: "connect-cluster-d",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-payments",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: ReadyRuntimeId,
                    captureId: ReadyCaptureId,
                    displayName: "Inventory Ready Connector",
                    captureDisplayName: "Inventory Invoices CDC",
                    captureDescription: "Declares a future write-path management mode with healthy shared runtime truth.",
                    connectClusterId: "connect-cluster-e",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-invoices",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            ObserveOnlyRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: ObserveOnlyCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-observe-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-a",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-a")
            ]);

        await reportSink.ReportAsync(
            FutureControlPlaneRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: FutureControlPlaneCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:30Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-managed-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-b",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-b")
            ]);

        await reportSink.ReportAsync(
            BlockedRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: BlockedCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-blocked-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "FAILED",
                        ["connectClusterId"] = "connect-cluster-d",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-d")
            ]);

        await reportSink.ReportAsync(
            ReadyRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: ReadyCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:45Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-ready-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-e",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-e")
            ]);

        var observeOnly = runtimeCatalog.GetById(ObserveOnlyRuntimeId);
        var futureControlPlane = runtimeCatalog.GetById(FutureControlPlaneRuntimeId);
        var outOfPolicy = runtimeCatalog.GetById(OutOfPolicyRuntimeId);
        var waiting = runtimeCatalog.GetById(WaitingRuntimeId);
        var blocked = runtimeCatalog.GetById(BlockedRuntimeId);
        var ready = runtimeCatalog.GetById(ReadyRuntimeId);

        Assert.NotNull(observeOnly);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Deferred, observeOnly.ManagedConnectorWritePathReadiness.State);
        Assert.Equal("observe-only", observeOnly.ManagedConnectorWritePathReadiness.ManagementMode);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly, observeOnly.ManagedConnectorWritePathReadiness.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.ObserveOnlyMode, observeOnly.ManagedConnectorWritePathReadiness.CategoryIds);
        Assert.True(observeOnly.ManagedConnectorWritePathReadiness.IsDeferred);
        Assert.False(observeOnly.ManagedConnectorWritePathReadiness.RequiresAttention);

        Assert.NotNull(futureControlPlane);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady, futureControlPlane.ManagedConnectorWritePathReadiness.State);
        Assert.Equal("apply-and-reconcile", futureControlPlane.ManagedConnectorWritePathReadiness.ManagementMode);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.DriftDetected, futureControlPlane.ManagedConnectorWritePathReadiness.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathRequested, futureControlPlane.ManagedConnectorWritePathReadiness.CategoryIds);
        Assert.True(futureControlPlane.ManagedConnectorWritePathReadiness.RequiresAttention);

        Assert.NotNull(outOfPolicy);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady, outOfPolicy.ManagedConnectorWritePathReadiness.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorWritePathReadiness.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete, outOfPolicy.ManagedConnectorWritePathReadiness.CategoryIds);

        Assert.NotNull(waiting);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady, waiting.ManagedConnectorWritePathReadiness.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.IncompleteReportingCoverage, waiting.ManagedConnectorWritePathReadiness.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete, waiting.ManagedConnectorWritePathReadiness.CategoryIds);

        Assert.NotNull(blocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Blocked, blocked.ManagedConnectorWritePathReadiness.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.BlockingRemediation, blocked.ManagedConnectorWritePathReadiness.CategoryIds);
        Assert.True(blocked.ManagedConnectorWritePathReadiness.IsBlocked);

        Assert.NotNull(ready);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Ready, ready.ManagedConnectorWritePathReadiness.State);
        Assert.Equal("apply-and-reconcile", ready.ManagedConnectorWritePathReadiness.ManagementMode);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.DeferControlPlane, ready.ManagedConnectorWritePathReadiness.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathRequested, ready.ManagedConnectorWritePathReadiness.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathReady, ready.ManagedConnectorWritePathReadiness.CategoryIds);
        Assert.True(ready.ManagedConnectorWritePathReadiness.IsReady);

        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorWritePathReadinessState(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Deferred)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([ReadyRuntimeId], runtimeCatalog
            .GetByManagedConnectorWritePathReadinessState(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Ready)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorWritePathReadinessState(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorWritePathReadinessState(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Blocked)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, ObserveOnlyRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorWritePathReadinessCategory(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.ObserveOnlyMode)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorWritePathReadinessCategory(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathRequested)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ReadyRuntimeId], runtimeCatalog
            .GetByManagedConnectorWritePathReadinessCategory(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathReady)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorWritePathReadinessCategory(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.GovernanceOutOfPolicy)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorWritePathReadinessCategory(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorWritePathReadinessCategory(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.BlockingRemediation)
            .Select(static runtime => runtime.Id)
            .ToArray());
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorPreflightCatalogExposesSharedStateCategoryAndOperationFilters()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: ObserveOnlyRuntimeId,
                    captureId: ObserveOnlyCaptureId,
                    displayName: "Inventory Observe-Only Connector",
                    captureDisplayName: "Inventory Orders CDC",
                    captureDescription: "Projects inventory order CDC truth through the shared Cephalon runtime catalog.",
                    connectClusterId: "connect-cluster-a",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-orders",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: FutureControlPlaneRuntimeId,
                    captureId: FutureControlPlaneCaptureId,
                    displayName: "Inventory Managed Connector",
                    captureDisplayName: "Inventory Products CDC",
                    captureDescription: "Declares a future write-path management mode while still using shared runtime truth.",
                    connectClusterId: "connect-cluster-b",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-products",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: OutOfPolicyRuntimeId,
                    captureId: OutOfPolicyCaptureId,
                    displayName: "Inventory Out-of-Policy Connector",
                    captureDisplayName: "Inventory Suppliers CDC",
                    captureDescription: "Omits connector-cluster identity so governance falls back to an out-of-policy answer.",
                    connectClusterId: null,
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-suppliers",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: WaitingRuntimeId,
                    captureId: WaitingCaptureId,
                    displayName: "Inventory Waiting Connector",
                    captureDisplayName: "Inventory Shipments CDC",
                    captureDescription: "Declares a healthy observe-only baseline but has not reported task topology yet.",
                    connectClusterId: "connect-cluster-c",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-shipments",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: BlockedRuntimeId,
                    captureId: BlockedCaptureId,
                    displayName: "Inventory Blocked Connector",
                    captureDisplayName: "Inventory Payments CDC",
                    captureDescription: "Reports a failed runtime outcome so remediation blocks deeper connector follow-through.",
                    connectClusterId: "connect-cluster-d",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-payments",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: ReadyRuntimeId,
                    captureId: ReadyCaptureId,
                    displayName: "Inventory Ready Connector",
                    captureDisplayName: "Inventory Invoices CDC",
                    captureDescription: "Declares a future write-path management mode with healthy shared runtime truth.",
                    connectClusterId: "connect-cluster-e",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-invoices",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            ObserveOnlyRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: ObserveOnlyCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-observe-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-a",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-a")
            ]);

        await reportSink.ReportAsync(
            FutureControlPlaneRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: FutureControlPlaneCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:30Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-managed-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-b",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-b")
            ]);

        await reportSink.ReportAsync(
            BlockedRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: BlockedCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-blocked-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "FAILED",
                        ["connectClusterId"] = "connect-cluster-d",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-d")
            ]);

        await reportSink.ReportAsync(
            ReadyRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: ReadyCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:45Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-ready-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-e",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-e")
            ]);

        var observeOnly = runtimeCatalog.GetById(ObserveOnlyRuntimeId);
        var futureControlPlane = runtimeCatalog.GetById(FutureControlPlaneRuntimeId);
        var outOfPolicy = runtimeCatalog.GetById(OutOfPolicyRuntimeId);
        var waiting = runtimeCatalog.GetById(WaitingRuntimeId);
        var blocked = runtimeCatalog.GetById(BlockedRuntimeId);
        var ready = runtimeCatalog.GetById(ReadyRuntimeId);

        Assert.NotNull(observeOnly);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Deferred, observeOnly.ManagedConnectorPreflight.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None, observeOnly.ManagedConnectorPreflight.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly, observeOnly.ManagedConnectorPreflight.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.ObserveOnlyMode, observeOnly.ManagedConnectorPreflight.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Deferred, observeOnly.ManagedConnectorPreflight.WritePathReadinessState);
        Assert.True(observeOnly.ManagedConnectorPreflight.IsDeferred);
        Assert.False(observeOnly.ManagedConnectorPreflight.RequiresAttention);

        Assert.NotNull(futureControlPlane);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady, futureControlPlane.ManagedConnectorPreflight.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Reconcile, futureControlPlane.ManagedConnectorPreflight.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.ReconcileIntent, futureControlPlane.ManagedConnectorPreflight.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.DriftDetected, futureControlPlane.ManagedConnectorPreflight.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady, futureControlPlane.ManagedConnectorPreflight.WritePathReadinessState);
        Assert.True(futureControlPlane.ManagedConnectorPreflight.RequiresAttention);

        Assert.NotNull(outOfPolicy);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady, outOfPolicy.ManagedConnectorPreflight.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None, outOfPolicy.ManagedConnectorPreflight.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorPreflight.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.ObserveOnlyMode, outOfPolicy.ManagedConnectorPreflight.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete, outOfPolicy.ManagedConnectorPreflight.CategoryIds);

        Assert.NotNull(waiting);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady, waiting.ManagedConnectorPreflight.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None, waiting.ManagedConnectorPreflight.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.IncompleteReportingCoverage, waiting.ManagedConnectorPreflight.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete, waiting.ManagedConnectorPreflight.CategoryIds);

        Assert.NotNull(blocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Blocked, blocked.ManagedConnectorPreflight.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None, blocked.ManagedConnectorPreflight.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.BlockingRemediation, blocked.ManagedConnectorPreflight.CategoryIds);
        Assert.True(blocked.ManagedConnectorPreflight.IsBlocked);

        Assert.NotNull(ready);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Ready, ready.ManagedConnectorPreflight.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Reconcile, ready.ManagedConnectorPreflight.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.DeferControlPlane, ready.ManagedConnectorPreflight.PrimaryActionId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.ReconcileIntent, ready.ManagedConnectorPreflight.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.PreflightReady, ready.ManagedConnectorPreflight.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Ready, ready.ManagedConnectorPreflight.WritePathReadinessState);
        Assert.True(ready.ManagedConnectorPreflight.IsReady);

        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorPreflightState(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Deferred)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([ReadyRuntimeId], runtimeCatalog
            .GetByManagedConnectorPreflightState(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Ready)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorPreflightState(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorPreflightState(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Blocked)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, ObserveOnlyRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorPreflightCategory(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.ObserveOnlyMode)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorPreflightCategory(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.ReconcileIntent)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ReadyRuntimeId], runtimeCatalog
            .GetByManagedConnectorPreflightCategory(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.PreflightReady)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorPreflightCategory(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.GovernanceOutOfPolicy)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorPreflightCategory(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorPreflightCategory(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.BlockingRemediation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, ObserveOnlyRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorPreflightOperationId(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorPreflightOperationId(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorDryRunExecutionIntentApprovalCommandEnvelopeAndCommandIssuanceCatalogsExposeSharedStateCategoryAndOperationFilters()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: ObserveOnlyRuntimeId,
                    captureId: ObserveOnlyCaptureId,
                    displayName: "Inventory Observe-Only Connector",
                    captureDisplayName: "Inventory Orders CDC",
                    captureDescription: "Projects inventory order CDC truth through the shared Cephalon runtime catalog.",
                    connectClusterId: "connect-cluster-a",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-orders",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: FutureControlPlaneRuntimeId,
                    captureId: FutureControlPlaneCaptureId,
                    displayName: "Inventory Managed Connector",
                    captureDisplayName: "Inventory Products CDC",
                    captureDescription: "Declares a future write-path management mode while still using shared runtime truth.",
                    connectClusterId: "connect-cluster-b",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-products",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: OutOfPolicyRuntimeId,
                    captureId: OutOfPolicyCaptureId,
                    displayName: "Inventory Out-of-Policy Connector",
                    captureDisplayName: "Inventory Suppliers CDC",
                    captureDescription: "Omits connector-cluster identity so governance falls back to an out-of-policy answer.",
                    connectClusterId: null,
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-suppliers",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: WaitingRuntimeId,
                    captureId: WaitingCaptureId,
                    displayName: "Inventory Waiting Connector",
                    captureDisplayName: "Inventory Shipments CDC",
                    captureDescription: "Declares a healthy observe-only baseline but has not reported task topology yet.",
                    connectClusterId: "connect-cluster-c",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-shipments",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: BlockedRuntimeId,
                    captureId: BlockedCaptureId,
                    displayName: "Inventory Blocked Connector",
                    captureDisplayName: "Inventory Payments CDC",
                    captureDescription: "Reports a failed runtime outcome so remediation blocks deeper connector follow-through.",
                    connectClusterId: "connect-cluster-d",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-payments",
                    managementMode: "observe-only",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: ReadyRuntimeId,
                    captureId: ReadyCaptureId,
                    displayName: "Inventory Ready Connector",
                    captureDisplayName: "Inventory Invoices CDC",
                    captureDescription: "Declares a future write-path management mode with healthy shared runtime truth.",
                    connectClusterId: "connect-cluster-e",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-invoices",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: PauseRequiredRuntimeId,
                    captureId: PauseRequiredCaptureId,
                    displayName: "Inventory Pause Required Connector",
                    captureDisplayName: "Inventory Returns CDC",
                    captureDescription: "Declares a pause operation and still reports a running lifecycle posture.",
                    connectClusterId: "connect-cluster-f",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-returns",
                    managementMode: "pause",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: PauseSatisfiedRuntimeId,
                    captureId: PauseSatisfiedCaptureId,
                    displayName: "Inventory Pause Satisfied Connector",
                    captureDisplayName: "Inventory Refunds CDC",
                    captureDescription: "Declares a pause operation and already reports a paused lifecycle posture.",
                    connectClusterId: "connect-cluster-g",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-refunds",
                    managementMode: "pause",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: DeleteRequiredRuntimeId,
                    captureId: DeleteRequiredCaptureId,
                    displayName: "Inventory Delete Required Connector",
                    captureDisplayName: "Inventory Archives CDC",
                    captureDescription: "Declares a delete operation so execution approval must surface destructive follow-through truth.",
                    connectClusterId: "connect-cluster-h",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-archives",
                    managementMode: "delete",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            ObserveOnlyRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: ObserveOnlyCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-observe-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-a",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-a")
            ]);

        await reportSink.ReportAsync(
            FutureControlPlaneRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: FutureControlPlaneCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:30Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-managed-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-b",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-b")
            ]);

        await reportSink.ReportAsync(
            BlockedRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: BlockedCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-blocked-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "FAILED",
                        ["connectClusterId"] = "connect-cluster-d",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-d")
            ]);

        await reportSink.ReportAsync(
            ReadyRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: ReadyCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:45Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-ready-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-e",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-e")
            ]);

        await reportSink.ReportAsync(
            PauseRequiredRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: PauseRequiredCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:50Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-pause-required-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-f",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-f")
            ]);

        await reportSink.ReportAsync(
            PauseSatisfiedRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: PauseSatisfiedCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:55Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-pause-satisfied-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "PAUSED",
                        ["connectClusterId"] = "connect-cluster-g",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-g")
            ]);

        await reportSink.ReportAsync(
            DeleteRequiredRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: DeleteRequiredCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:58Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-delete-required-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-h",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-h")
            ]);

        var observeOnly = runtimeCatalog.GetById(ObserveOnlyRuntimeId);
        var futureControlPlane = runtimeCatalog.GetById(FutureControlPlaneRuntimeId);
        var outOfPolicy = runtimeCatalog.GetById(OutOfPolicyRuntimeId);
        var waiting = runtimeCatalog.GetById(WaitingRuntimeId);
        var blocked = runtimeCatalog.GetById(BlockedRuntimeId);
        var ready = runtimeCatalog.GetById(ReadyRuntimeId);
        var pauseRequired = runtimeCatalog.GetById(PauseRequiredRuntimeId);
        var pauseSatisfied = runtimeCatalog.GetById(PauseSatisfiedRuntimeId);
        var deleteRequired = runtimeCatalog.GetById(DeleteRequiredRuntimeId);

        Assert.NotNull(observeOnly);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred, observeOnly.ManagedConnectorDryRun.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None, observeOnly.ManagedConnectorDryRun.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ObserveOnlyMode, observeOnly.ManagedConnectorDryRun.CategoryIds);
        Assert.True(observeOnly.ManagedConnectorDryRun.IsDeferred);
        Assert.False(observeOnly.ManagedConnectorDryRun.WouldApplyChanges);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Deferred, observeOnly.ManagedConnectorExecutionIntent.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None, observeOnly.ManagedConnectorExecutionIntent.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ObserveOnlyMode, observeOnly.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.DryRun, observeOnly.ManagedConnectorExecutionIntent.ConfidenceSourceId);
        Assert.True(observeOnly.ManagedConnectorExecutionIntent.IsDeferred);
        Assert.False(observeOnly.ManagedConnectorExecutionIntent.CanExecuteThroughEngine);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable, observeOnly.ManagedConnectorExecutionApproval.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.None, observeOnly.ManagedConnectorExecutionApproval.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ObserveOnlyMode, observeOnly.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Governance, observeOnly.ManagedConnectorExecutionApproval.SourceId);
        Assert.False(observeOnly.ManagedConnectorExecutionApproval.HasSafetyGateClearance);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable, observeOnly.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None, observeOnly.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ObserveOnlyMode, observeOnly.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionIntent, observeOnly.ManagedConnectorCommandEnvelope.SourceId);
        Assert.False(observeOnly.ManagedConnectorCommandEnvelope.HasCommandTarget);

        Assert.NotNull(futureControlPlane);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked, futureControlPlane.ManagedConnectorDryRun.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, futureControlPlane.ManagedConnectorDryRun.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ChangePlanned, futureControlPlane.ManagedConnectorDryRun.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.TaskTopologyChange, futureControlPlane.ManagedConnectorDryRun.CategoryIds);
        Assert.True(futureControlPlane.ManagedConnectorDryRun.WouldApplyChanges);
        Assert.Equal(1, futureControlPlane.ManagedConnectorDryRun.PotentialChangeCount);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.OperatorAction, futureControlPlane.ManagedConnectorExecutionIntent.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile, futureControlPlane.ManagedConnectorExecutionIntent.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.FutureControlPlane, futureControlPlane.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.OperatorOnly, futureControlPlane.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ChangePlanned, futureControlPlane.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.DryRun, futureControlPlane.ManagedConnectorExecutionIntent.ConfidenceSourceId);
        Assert.True(futureControlPlane.ManagedConnectorExecutionIntent.IsOperatorAction);
        Assert.True(futureControlPlane.ManagedConnectorExecutionIntent.IsOperatorOnly);
        Assert.True(futureControlPlane.ManagedConnectorExecutionIntent.WouldApplyChanges);
        Assert.Equal(1, futureControlPlane.ManagedConnectorExecutionIntent.PotentialChangeCount);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.PolicyBlocked, futureControlPlane.ManagedConnectorExecutionApproval.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Reconcile, futureControlPlane.ManagedConnectorExecutionApproval.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ControlPlaneOwnershipGap, futureControlPlane.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Governance, futureControlPlane.ManagedConnectorExecutionApproval.SourceId);
        Assert.True(futureControlPlane.ManagedConnectorExecutionApproval.IsPolicyBlocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.OperatorOnly, futureControlPlane.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Reconcile, futureControlPlane.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ControlPlaneOwnershipGap, futureControlPlane.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.OperatorOnly, futureControlPlane.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval, futureControlPlane.ManagedConnectorCommandEnvelope.SourceId);
        Assert.True(futureControlPlane.ManagedConnectorCommandEnvelope.IsOperatorOnly);
        Assert.True(futureControlPlane.ManagedConnectorCommandEnvelope.HasCommandTarget);

        Assert.NotNull(outOfPolicy);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked, outOfPolicy.ManagedConnectorDryRun.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None, outOfPolicy.ManagedConnectorDryRun.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorDryRun.CategoryIds);
        Assert.False(outOfPolicy.ManagedConnectorDryRun.WouldApplyChanges);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked, outOfPolicy.ManagedConnectorExecutionIntent.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None, outOfPolicy.ManagedConnectorExecutionIntent.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.True(outOfPolicy.ManagedConnectorExecutionIntent.IsBlocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.PolicyBlocked, outOfPolicy.ManagedConnectorExecutionApproval.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.None, outOfPolicy.ManagedConnectorExecutionApproval.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Governance, outOfPolicy.ManagedConnectorExecutionApproval.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked, outOfPolicy.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None, outOfPolicy.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval, outOfPolicy.ManagedConnectorCommandEnvelope.SourceId);
        Assert.False(outOfPolicy.ManagedConnectorCommandEnvelope.HasCommandTarget);

        Assert.NotNull(waiting);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked, waiting.ManagedConnectorDryRun.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.IncompleteReportingCoverage, waiting.ManagedConnectorDryRun.CategoryIds);
        Assert.False(waiting.ManagedConnectorDryRun.WouldApplyChanges);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked, waiting.ManagedConnectorExecutionIntent.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.IncompleteReportingCoverage, waiting.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.RuntimeTruthIncomplete, waiting.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoBlocked, waiting.ManagedConnectorExecutionApproval.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.IncompleteReportingCoverage, waiting.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.RuntimeTruthIncomplete, waiting.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.ExecutionIntent, waiting.ManagedConnectorExecutionApproval.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked, waiting.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None, waiting.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.IncompleteReportingCoverage, waiting.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.RuntimeTruthIncomplete, waiting.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval, waiting.ManagedConnectorCommandEnvelope.SourceId);

        Assert.NotNull(blocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked, blocked.ManagedConnectorDryRun.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.BlockingRemediation, blocked.ManagedConnectorDryRun.CategoryIds);
        Assert.True(blocked.ManagedConnectorDryRun.IsBlocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked, blocked.ManagedConnectorExecutionIntent.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.BlockingRemediation, blocked.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.True(blocked.ManagedConnectorExecutionIntent.IsBlocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoBlocked, blocked.ManagedConnectorExecutionApproval.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.BlockingRemediation, blocked.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Remediation, blocked.ManagedConnectorExecutionApproval.SourceId);
        Assert.True(blocked.ManagedConnectorExecutionApproval.IsAutoBlocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked, blocked.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None, blocked.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.BlockingRemediation, blocked.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval, blocked.ManagedConnectorCommandEnvelope.SourceId);
        Assert.True(blocked.ManagedConnectorCommandEnvelope.IsBlocked);

        Assert.NotNull(ready);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NoOp, ready.ManagedConnectorDryRun.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, ready.ManagedConnectorDryRun.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.NoChangesRequired, ready.ManagedConnectorDryRun.CategoryIds);
        Assert.True(ready.ManagedConnectorDryRun.IsNoOp);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.ReadyToExecute, ready.ManagedConnectorExecutionIntent.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile, ready.ManagedConnectorExecutionIntent.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.EngineExecutionCandidate, ready.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.NoExecutionNeeded, ready.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.True(ready.ManagedConnectorExecutionIntent.IsReadyToExecute);
        Assert.True(ready.ManagedConnectorExecutionIntent.CanExecuteThroughEngine);
        Assert.False(ready.ManagedConnectorExecutionIntent.IsApprovalRequired);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoEligible, ready.ManagedConnectorExecutionApproval.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Reconcile, ready.ManagedConnectorExecutionApproval.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.AutoEligible, ready.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.NoExecutionNeeded, ready.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.ExecutionIntent, ready.ManagedConnectorExecutionApproval.SourceId);
        Assert.True(ready.ManagedConnectorExecutionApproval.IsAutoEligible);
        Assert.True(ready.ManagedConnectorExecutionApproval.CanAutoExecuteThroughEngine);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.EngineReady, ready.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Reconcile, ready.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.EngineReady, ready.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.NoExecutionNeeded, ready.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval, ready.ManagedConnectorCommandEnvelope.SourceId);
        Assert.Equal("connect-cluster-e", ready.ManagedConnectorCommandEnvelope.ConnectClusterId);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", ready.ManagedConnectorCommandEnvelope.ConnectorClass);
        Assert.Equal("postgresql", ready.ManagedConnectorCommandEnvelope.SourceProviderId);
        Assert.True(ready.ManagedConnectorCommandEnvelope.IsEngineReady);
        Assert.True(ready.ManagedConnectorCommandEnvelope.HasCommandTarget);
        Assert.False(string.IsNullOrWhiteSpace(ready.ManagedConnectorCommandEnvelope.CommandFingerprint));

        Assert.NotNull(pauseRequired);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.WouldChange, pauseRequired.ManagedConnectorDryRun.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause, pauseRequired.ManagedConnectorDryRun.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ChangePlanned, pauseRequired.ManagedConnectorDryRun.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.LifecycleChange, pauseRequired.ManagedConnectorDryRun.CategoryIds);
        Assert.True(pauseRequired.ManagedConnectorDryRun.IsWouldChange);
        Assert.True(pauseRequired.ManagedConnectorDryRun.WouldApplyChanges);
        Assert.Equal(1, pauseRequired.ManagedConnectorDryRun.PotentialChangeCount);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.RequiresApproval, pauseRequired.ManagedConnectorExecutionIntent.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Pause, pauseRequired.ManagedConnectorExecutionIntent.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.EngineExecutionCandidate, pauseRequired.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ApprovalRequired, pauseRequired.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ChangePlanned, pauseRequired.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.LifecycleChange, pauseRequired.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.True(pauseRequired.ManagedConnectorExecutionIntent.IsApprovalRequired);
        Assert.True(pauseRequired.ManagedConnectorExecutionIntent.CanExecuteThroughEngine);
        Assert.True(pauseRequired.ManagedConnectorExecutionIntent.WouldApplyChanges);
        Assert.Equal(1, pauseRequired.ManagedConnectorExecutionIntent.PotentialChangeCount);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalReady, pauseRequired.ManagedConnectorExecutionApproval.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Pause, pauseRequired.ManagedConnectorExecutionApproval.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalRequired, pauseRequired.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalReady, pauseRequired.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.LifecycleChange, pauseRequired.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.ExecutionIntent, pauseRequired.ManagedConnectorExecutionApproval.SourceId);
        Assert.True(pauseRequired.ManagedConnectorExecutionApproval.IsApprovalReady);
        Assert.True(pauseRequired.ManagedConnectorExecutionApproval.RequiresExplicitApproval);
        Assert.True(pauseRequired.ManagedConnectorExecutionApproval.CanRequestApproval);
        Assert.True(pauseRequired.ManagedConnectorExecutionApproval.HasSafetyGateClearance);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.ApprovalGated, pauseRequired.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Pause, pauseRequired.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalGated, pauseRequired.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalReady, pauseRequired.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.LifecycleChange, pauseRequired.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval, pauseRequired.ManagedConnectorCommandEnvelope.SourceId);
        Assert.True(pauseRequired.ManagedConnectorCommandEnvelope.IsApprovalGated);
        Assert.True(pauseRequired.ManagedConnectorCommandEnvelope.RequiresExplicitApproval);

        Assert.NotNull(pauseSatisfied);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NoOp, pauseSatisfied.ManagedConnectorDryRun.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause, pauseSatisfied.ManagedConnectorDryRun.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.NoChangesRequired, pauseSatisfied.ManagedConnectorDryRun.CategoryIds);
        Assert.True(pauseSatisfied.ManagedConnectorDryRun.IsNoOp);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.ReadyToExecute, pauseSatisfied.ManagedConnectorExecutionIntent.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Pause, pauseSatisfied.ManagedConnectorExecutionIntent.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.EngineExecutionCandidate, pauseSatisfied.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.NoExecutionNeeded, pauseSatisfied.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.True(pauseSatisfied.ManagedConnectorExecutionIntent.IsReadyToExecute);
        Assert.True(pauseSatisfied.ManagedConnectorExecutionIntent.CanExecuteThroughEngine);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoEligible, pauseSatisfied.ManagedConnectorExecutionApproval.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Pause, pauseSatisfied.ManagedConnectorExecutionApproval.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.AutoEligible, pauseSatisfied.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.NoExecutionNeeded, pauseSatisfied.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.True(pauseSatisfied.ManagedConnectorExecutionApproval.IsAutoEligible);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.EngineReady, pauseSatisfied.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Pause, pauseSatisfied.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.EngineReady, pauseSatisfied.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.NoExecutionNeeded, pauseSatisfied.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval, pauseSatisfied.ManagedConnectorCommandEnvelope.SourceId);
        Assert.True(pauseSatisfied.ManagedConnectorCommandEnvelope.IsEngineReady);

        Assert.NotNull(deleteRequired);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.WouldChange, deleteRequired.ManagedConnectorDryRun.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete, deleteRequired.ManagedConnectorDryRun.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ChangePlanned, deleteRequired.ManagedConnectorDryRun.CategoryIds);
        Assert.True(deleteRequired.ManagedConnectorDryRun.IsWouldChange);
        Assert.True(deleteRequired.ManagedConnectorDryRun.WouldApplyChanges);
        Assert.Equal(1, deleteRequired.ManagedConnectorDryRun.PotentialChangeCount);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.RequiresApproval, deleteRequired.ManagedConnectorExecutionIntent.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Delete, deleteRequired.ManagedConnectorExecutionIntent.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.EngineExecutionCandidate, deleteRequired.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ApprovalRequired, deleteRequired.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ChangePlanned, deleteRequired.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.True(deleteRequired.ManagedConnectorExecutionIntent.IsApprovalRequired);
        Assert.True(deleteRequired.ManagedConnectorExecutionIntent.CanExecuteThroughEngine);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalRequired, deleteRequired.ManagedConnectorExecutionApproval.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Delete, deleteRequired.ManagedConnectorExecutionApproval.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.DestructiveOperation, deleteRequired.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalRequired, deleteRequired.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.LifecycleChange, deleteRequired.ManagedConnectorExecutionApproval.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.ExecutionIntent, deleteRequired.ManagedConnectorExecutionApproval.SourceId);
        Assert.True(deleteRequired.ManagedConnectorExecutionApproval.IsApprovalRequired);
        Assert.True(deleteRequired.ManagedConnectorExecutionApproval.RequiresExplicitApproval);
        Assert.False(deleteRequired.ManagedConnectorExecutionApproval.HasSafetyGateClearance);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.ApprovalGated, deleteRequired.ManagedConnectorCommandEnvelope.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Delete, deleteRequired.ManagedConnectorCommandEnvelope.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalGated, deleteRequired.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalRequired, deleteRequired.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.DestructiveOperation, deleteRequired.ManagedConnectorCommandEnvelope.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval, deleteRequired.ManagedConnectorCommandEnvelope.SourceId);
        Assert.True(deleteRequired.ManagedConnectorCommandEnvelope.IsApprovalGated);
        Assert.True(deleteRequired.ManagedConnectorCommandEnvelope.RequiresExplicitApproval);
        Assert.True(deleteRequired.ManagedConnectorCommandEnvelope.IsDestructiveOperation);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable, observeOnly.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None, observeOnly.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ObserveOnlyMode, observeOnly.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, observeOnly.ManagedConnectorCommandIssuance.SourceId);
        Assert.False(observeOnly.ManagedConnectorCommandIssuance.HasIssuableCommand);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.OperatorOnly, futureControlPlane.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Reconcile, futureControlPlane.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ControlPlaneOwnershipGap, futureControlPlane.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.OperatorOnly, futureControlPlane.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, futureControlPlane.ManagedConnectorCommandIssuance.SourceId);
        Assert.True(futureControlPlane.ManagedConnectorCommandIssuance.IsOperatorOnly);
        Assert.True(futureControlPlane.ManagedConnectorCommandIssuance.HasIssuableCommand);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked, outOfPolicy.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None, outOfPolicy.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, outOfPolicy.ManagedConnectorCommandIssuance.SourceId);
        Assert.False(outOfPolicy.ManagedConnectorCommandIssuance.HasIssuableCommand);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked, waiting.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None, waiting.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.IncompleteReportingCoverage, waiting.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.RuntimeTruthIncomplete, waiting.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, waiting.ManagedConnectorCommandIssuance.SourceId);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked, blocked.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None, blocked.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.BlockingRemediation, blocked.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, blocked.ManagedConnectorCommandIssuance.SourceId);
        Assert.True(blocked.ManagedConnectorCommandIssuance.IsBlocked);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Rejected, ready.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Reconcile, ready.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Rejected, ready.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.NoExecutionNeeded, ready.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, ready.ManagedConnectorCommandIssuance.SourceId);
        Assert.Equal("connect-cluster-e", ready.ManagedConnectorCommandIssuance.ConnectClusterId);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", ready.ManagedConnectorCommandIssuance.ConnectorClass);
        Assert.Equal("postgresql", ready.ManagedConnectorCommandIssuance.SourceProviderId);
        Assert.True(ready.ManagedConnectorCommandIssuance.IsRejected);
        Assert.True(ready.ManagedConnectorCommandIssuance.HasIssuableCommand);
        Assert.False(string.IsNullOrWhiteSpace(ready.ManagedConnectorCommandIssuance.CommandFingerprint));
        Assert.False(string.IsNullOrWhiteSpace(ready.ManagedConnectorCommandIssuance.IssuanceFingerprint));

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Accepted, pauseRequired.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Pause, pauseRequired.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Accepted, pauseRequired.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalGated, pauseRequired.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalReady, pauseRequired.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.LifecycleChange, pauseRequired.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, pauseRequired.ManagedConnectorCommandIssuance.SourceId);
        Assert.True(pauseRequired.ManagedConnectorCommandIssuance.IsAccepted);
        Assert.True(pauseRequired.ManagedConnectorCommandIssuance.RequiresExplicitApproval);
        Assert.True(pauseRequired.ManagedConnectorCommandIssuance.CanEnterFutureIssuanceLane);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Rejected, pauseSatisfied.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Pause, pauseSatisfied.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Rejected, pauseSatisfied.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.NoExecutionNeeded, pauseSatisfied.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, pauseSatisfied.ManagedConnectorCommandIssuance.SourceId);
        Assert.True(pauseSatisfied.ManagedConnectorCommandIssuance.IsRejected);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Accepted, deleteRequired.ManagedConnectorCommandIssuance.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Delete, deleteRequired.ManagedConnectorCommandIssuance.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Accepted, deleteRequired.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalRequired, deleteRequired.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.DestructiveOperation, deleteRequired.ManagedConnectorCommandIssuance.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope, deleteRequired.ManagedConnectorCommandIssuance.SourceId);
        Assert.True(deleteRequired.ManagedConnectorCommandIssuance.IsAccepted);
        Assert.True(deleteRequired.ManagedConnectorCommandIssuance.RequiresExplicitApproval);
        Assert.True(deleteRequired.ManagedConnectorCommandIssuance.IsDestructiveOperation);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable, observeOnly.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None, observeOnly.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ObserveOnlyMode, observeOnly.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, observeOnly.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, observeOnly.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.False(observeOnly.ManagedConnectorExecutionAdapter.HasAdaptableCommand);
        Assert.False(observeOnly.ManagedConnectorExecutionAdapter.CanUseProviderExecutionAdapter);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.OperatorOnly, futureControlPlane.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, futureControlPlane.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ControlPlaneOwnershipGap, futureControlPlane.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.OperatorOnly, futureControlPlane.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ChangePlanned, futureControlPlane.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, futureControlPlane.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, futureControlPlane.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.True(futureControlPlane.ManagedConnectorExecutionAdapter.IsOperatorOnly);
        Assert.True(futureControlPlane.ManagedConnectorExecutionAdapter.HasAdaptableCommand);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Blocked, outOfPolicy.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None, outOfPolicy.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, outOfPolicy.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, outOfPolicy.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.True(outOfPolicy.ManagedConnectorExecutionAdapter.IsBlocked);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Blocked, waiting.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None, waiting.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.IncompleteReportingCoverage, waiting.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.RuntimeTruthIncomplete, waiting.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, waiting.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, waiting.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.True(waiting.ManagedConnectorExecutionAdapter.IsBlocked);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Blocked, blocked.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None, blocked.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.BlockingRemediation, blocked.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, blocked.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, blocked.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.True(blocked.ManagedConnectorExecutionAdapter.IsBlocked);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, ready.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, ready.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterReady, ready.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.NoExecutionNeeded, ready.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, ready.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, ready.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.Equal("connect-cluster-e", ready.ManagedConnectorExecutionAdapter.ConnectClusterId);
        Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", ready.ManagedConnectorExecutionAdapter.ConnectorClass);
        Assert.Equal("postgresql", ready.ManagedConnectorExecutionAdapter.SourceProviderId);
        Assert.True(ready.ManagedConnectorExecutionAdapter.IsReady);
        Assert.True(ready.ManagedConnectorExecutionAdapter.HasAdaptableCommand);
        Assert.True(ready.ManagedConnectorExecutionAdapter.CanUseProviderExecutionAdapter);
        Assert.False(string.IsNullOrWhiteSpace(ready.ManagedConnectorExecutionAdapter.AdapterFingerprint));

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, pauseRequired.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause, pauseRequired.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterReady, pauseRequired.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalReady, pauseRequired.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.LifecycleChange, pauseRequired.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, pauseRequired.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, pauseRequired.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.True(pauseRequired.ManagedConnectorExecutionAdapter.IsReady);
        Assert.True(pauseRequired.ManagedConnectorExecutionAdapter.RequiresExplicitApproval);
        Assert.True(pauseRequired.ManagedConnectorExecutionAdapter.WouldApplyChanges);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, pauseSatisfied.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause, pauseSatisfied.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterReady, pauseSatisfied.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.NoExecutionNeeded, pauseSatisfied.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, pauseSatisfied.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, pauseSatisfied.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.True(pauseSatisfied.ManagedConnectorExecutionAdapter.IsReady);
        Assert.False(pauseSatisfied.ManagedConnectorExecutionAdapter.WouldApplyChanges);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, deleteRequired.ManagedConnectorExecutionAdapter.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete, deleteRequired.ManagedConnectorExecutionAdapter.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterReady, deleteRequired.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalRequired, deleteRequired.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.DestructiveOperation, deleteRequired.ManagedConnectorExecutionAdapter.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance, deleteRequired.ManagedConnectorExecutionAdapter.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, deleteRequired.ManagedConnectorExecutionAdapter.AdapterId);
        Assert.True(deleteRequired.ManagedConnectorExecutionAdapter.IsReady);
        Assert.True(deleteRequired.ManagedConnectorExecutionAdapter.RequiresExplicitApproval);
        Assert.True(deleteRequired.ManagedConnectorExecutionAdapter.IsDestructiveOperation);
        Assert.True(deleteRequired.ManagedConnectorExecutionAdapter.WouldApplyChanges);

        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorDryRunState(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorDryRunState(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorDryRunState(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NoOp)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorDryRunState(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.WouldChange)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, FutureControlPlaneRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorDryRunCategory(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ChangePlanned)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorDryRunCategory(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.NoChangesRequired)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorDryRunCategory(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.TaskTopologyChange)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorDryRunOperationId(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorDryRunOperationId(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorDryRunOperationId(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionIntentState(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Deferred)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentState(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionIntentState(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.OperatorAction)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentState(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.RequiresApproval)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentState(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.ReadyToExecute)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ApprovalRequired)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionIntentCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.EngineExecutionCandidate)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.NoExecutionNeeded)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionIntentOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalState(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionApprovalState(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoBlocked)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionApprovalState(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.PolicyBlocked)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalState(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalRequired)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalState(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalReady)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionApprovalState(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoEligible)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ObserveOnlyMode)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.BlockingRemediation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.GovernanceOutOfPolicy)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ControlPlaneOwnershipGap)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.DestructiveOperation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalRequired)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalReady)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.AutoEligible)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionApprovalCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.NoExecutionNeeded)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionApprovalOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionApprovalOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionApprovalOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeState(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeState(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeState(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeState(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.ApprovalGated)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeState(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.EngineReady)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ObserveOnlyMode)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ControlPlaneOwnershipGap)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.GovernanceOutOfPolicy)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.BlockingRemediation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, FutureControlPlaneRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ChangePlanned)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalGated)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalReady)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalRequired)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.EngineReady)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.NoExecutionNeeded)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.DestructiveOperation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeOperationId(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandEnvelopeOperationId(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandEnvelopeOperationId(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Accepted)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Rejected)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Empty(runtimeCatalog
            .GetByManagedConnectorCommandIssuanceState(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Issued));
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ObserveOnlyMode)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ControlPlaneOwnershipGap)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.GovernanceOutOfPolicy)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.BlockingRemediation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, FutureControlPlaneRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ChangePlanned)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalGated)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalReady)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalRequired)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Accepted)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Rejected)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.NoExecutionNeeded)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Empty(runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Issued));
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.DestructiveOperation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceOperationId(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandIssuanceOperationId(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandIssuanceOperationId(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterState(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionAdapterState(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Blocked)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterState(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Empty(runtimeCatalog
            .GetByManagedConnectorExecutionAdapterState(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Unavailable));
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionAdapterState(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ObserveOnlyMode)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ControlPlaneOwnershipGap)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([OutOfPolicyRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.GovernanceOutOfPolicy)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([BlockedRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.BlockingRemediation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterReady)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Empty(runtimeCatalog
            .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterUnavailable));
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.NoExecutionNeeded)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalReady)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalRequired)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.DestructiveOperation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionAdapterOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionAdapterOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionAdapterOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable, observeOnly.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, futureControlPlane.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, outOfPolicy.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, waiting.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, blocked.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, ready.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, pauseRequired.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, pauseSatisfied.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, deleteRequired.ManagedConnectorCommandExecution.State);

        var commandExecutor = provider.GetRequiredService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>();

        var readyRecordedAt = DateTimeOffset.Parse("2026-04-23T06:10:31Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(readyRecordedAt);
        var readyCommandExecution = await commandExecutor.ExecuteAsync(
            ReadyRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile);

        var pauseBlockedRecordedAt = DateTimeOffset.Parse("2026-04-23T06:10:32Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(pauseBlockedRecordedAt);
        var pauseBlockedCommandExecution = await commandExecutor.ExecuteAsync(
            PauseRequiredRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause);

        var pauseAdaptedRecordedAt = DateTimeOffset.Parse("2026-04-23T06:10:33Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(pauseAdaptedRecordedAt);
        var pauseAdaptedCommandExecution = await commandExecutor.ExecuteAsync(
            PauseRequiredRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause,
            new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
            {
                Approve = true
            });

        var deleteBlockedRecordedAt = DateTimeOffset.Parse("2026-04-23T06:10:34Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(deleteBlockedRecordedAt);
        var deleteBlockedCommandExecution = await commandExecutor.ExecuteAsync(
            DeleteRequiredRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete,
            new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
            {
                Approve = true
            });

        var deleteAdaptedRecordedAt = DateTimeOffset.Parse("2026-04-23T06:10:35Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(deleteAdaptedRecordedAt);
        var deleteAdaptedCommandExecution = await commandExecutor.ExecuteAsync(
            DeleteRequiredRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete,
            new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
            {
                Approve = true,
                AllowDestructive = true
            });

        var futureRecordedAt = DateTimeOffset.Parse("2026-04-23T06:10:36Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(futureRecordedAt);
        var futureCommandExecution = await commandExecutor.ExecuteAsync(
            FutureControlPlaneRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile);

        Assert.True(readyCommandExecution.HasRecordedOutcome);
        Assert.False(string.IsNullOrWhiteSpace(readyCommandExecution.AttemptId));
        Assert.Equal(readyRecordedAt, readyCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, readyCommandExecution.State);

        Assert.True(pauseBlockedCommandExecution.HasRecordedOutcome);
        Assert.False(string.IsNullOrWhiteSpace(pauseBlockedCommandExecution.AttemptId));
        Assert.Equal(pauseBlockedRecordedAt, pauseBlockedCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, pauseBlockedCommandExecution.State);

        Assert.True(pauseAdaptedCommandExecution.HasRecordedOutcome);
        Assert.False(string.IsNullOrWhiteSpace(pauseAdaptedCommandExecution.AttemptId));
        Assert.Equal(pauseAdaptedRecordedAt, pauseAdaptedCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, pauseAdaptedCommandExecution.State);

        Assert.True(deleteBlockedCommandExecution.HasRecordedOutcome);
        Assert.False(string.IsNullOrWhiteSpace(deleteBlockedCommandExecution.AttemptId));
        Assert.Equal(deleteBlockedRecordedAt, deleteBlockedCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, deleteBlockedCommandExecution.State);

        Assert.True(deleteAdaptedCommandExecution.HasRecordedOutcome);
        Assert.False(string.IsNullOrWhiteSpace(deleteAdaptedCommandExecution.AttemptId));
        Assert.Equal(deleteAdaptedRecordedAt, deleteAdaptedCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, deleteAdaptedCommandExecution.State);

        Assert.True(futureCommandExecution.HasRecordedOutcome);
        Assert.False(string.IsNullOrWhiteSpace(futureCommandExecution.AttemptId));
        Assert.Equal(futureRecordedAt, futureCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly, futureCommandExecution.State);

        ready = runtimeCatalog.GetById(ReadyRuntimeId);
        pauseRequired = runtimeCatalog.GetById(PauseRequiredRuntimeId);
        deleteRequired = runtimeCatalog.GetById(DeleteRequiredRuntimeId);
        futureControlPlane = runtimeCatalog.GetById(FutureControlPlaneRuntimeId);
        pauseSatisfied = runtimeCatalog.GetById(PauseSatisfiedRuntimeId);
        observeOnly = runtimeCatalog.GetById(ObserveOnlyRuntimeId);
        outOfPolicy = runtimeCatalog.GetById(OutOfPolicyRuntimeId);
        waiting = runtimeCatalog.GetById(WaitingRuntimeId);
        blocked = runtimeCatalog.GetById(BlockedRuntimeId);

        Assert.NotNull(ready);
        Assert.NotNull(pauseRequired);
        Assert.NotNull(deleteRequired);
        Assert.NotNull(futureControlPlane);
        Assert.NotNull(pauseSatisfied);
        Assert.NotNull(observeOnly);
        Assert.NotNull(outOfPolicy);
        Assert.NotNull(waiting);
        Assert.NotNull(blocked);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, ready.ManagedConnectorCommandExecution.State);
        Assert.Equal(readyRecordedAt, ready.ManagedConnectorCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, ready.ManagedConnectorCommandExecution.RequestedOperationId);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, pauseRequired.ManagedConnectorCommandExecution.State);
        Assert.Equal(pauseAdaptedRecordedAt, pauseRequired.ManagedConnectorCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause, pauseRequired.ManagedConnectorCommandExecution.RequestedOperationId);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, deleteRequired.ManagedConnectorCommandExecution.State);
        Assert.Equal(deleteAdaptedRecordedAt, deleteRequired.ManagedConnectorCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete, deleteRequired.ManagedConnectorCommandExecution.RequestedOperationId);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly, futureControlPlane.ManagedConnectorCommandExecution.State);
        Assert.Equal(futureRecordedAt, futureControlPlane.ManagedConnectorCommandExecution.RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, futureControlPlane.ManagedConnectorCommandExecution.RequestedOperationId);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable, observeOnly.ManagedConnectorCommandExecution.State);
        Assert.False(observeOnly.ManagedConnectorCommandExecution.IsUnrecorded);
        Assert.False(observeOnly.ManagedConnectorCommandExecution.HasRecordedOutcome);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, outOfPolicy.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, waiting.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, blocked.ManagedConnectorCommandExecution.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, pauseSatisfied.ManagedConnectorCommandExecution.State);

        var pauseHistory = runtimeCatalog.GetManagedConnectorCommandExecutionHistory(PauseRequiredRuntimeId);
        Assert.Equal(2, pauseHistory.Count);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, pauseHistory[0].State);
        Assert.Equal(pauseAdaptedRecordedAt, pauseHistory[0].RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, pauseHistory[1].State);
        Assert.Equal(pauseBlockedRecordedAt, pauseHistory[1].RecordedAtUtc);

        var deleteHistory = runtimeCatalog.GetManagedConnectorCommandExecutionHistory(DeleteRequiredRuntimeId);
        Assert.Equal(2, deleteHistory.Count);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, deleteHistory[0].State);
        Assert.Equal(deleteAdaptedRecordedAt, deleteHistory[0].RecordedAtUtc);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, deleteHistory[1].State);
        Assert.Equal(deleteBlockedRecordedAt, deleteHistory[1].RecordedAtUtc);

        var readyHistory = runtimeCatalog.GetManagedConnectorCommandExecutionHistory(ReadyRuntimeId);
        Assert.Single(readyHistory);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, readyHistory[0].State);
        Assert.Equal(readyRecordedAt, readyHistory[0].RecordedAtUtc);

        var futureHistory = runtimeCatalog.GetManagedConnectorCommandExecutionHistory(FutureControlPlaneRuntimeId);
        Assert.Single(futureHistory);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly, futureHistory[0].State);
        Assert.Equal(futureRecordedAt, futureHistory[0].RecordedAtUtc);

        Assert.Empty(runtimeCatalog.GetManagedConnectorCommandExecutionHistory(ObserveOnlyRuntimeId));

        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandExecutionState(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandExecutionState(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Empty(runtimeCatalog
            .GetByManagedConnectorCommandExecutionState(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked));
        Assert.Equal([ReadyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandExecutionState(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandExecutionState(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandExecutionState(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandExecutionOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandExecutionOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandExecutionOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable, observeOnly.ManagedConnectorCommandRetry.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotNeeded, ready.ManagedConnectorCommandRetry.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Cooldown, pauseRequired.ManagedConnectorCommandRetry.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Cooldown, deleteRequired.ManagedConnectorCommandRetry.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.OperatorOnly, futureControlPlane.ManagedConnectorCommandRetry.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, ready.ManagedConnectorCommandRetry.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause, pauseRequired.ManagedConnectorCommandRetry.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete, deleteRequired.ManagedConnectorCommandRetry.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, futureControlPlane.ManagedConnectorCommandRetry.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.CooldownActive, pauseRequired.ManagedConnectorCommandRetry.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.CooldownActive, deleteRequired.ManagedConnectorCommandRetry.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ControlPlaneOwnershipGap, futureControlPlane.ManagedConnectorCommandRetry.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.NoExecutionNeeded, ready.ManagedConnectorCommandRetry.CategoryIds);
        Assert.True(pauseRequired.ManagedConnectorCommandRetry.HasMatchingRetryFingerprint);
        Assert.True(deleteRequired.ManagedConnectorCommandRetry.HasMatchingRetryFingerprint);
        Assert.True(futureControlPlane.ManagedConnectorCommandRetry.IsOperatorOnly);
        Assert.True(ready.ManagedConnectorCommandRetry.IsNotNeeded);
        Assert.False(ready.ManagedConnectorCommandRetry.CanRetry);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable, observeOnly.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded, ready.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown, pauseRequired.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown, deleteRequired.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.OperatorOnly, futureControlPlane.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Reconcile, ready.ManagedConnectorRetryExecutionPolicy.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Pause, pauseRequired.ManagedConnectorRetryExecutionPolicy.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Delete, deleteRequired.ManagedConnectorRetryExecutionPolicy.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Reconcile, futureControlPlane.ManagedConnectorRetryExecutionPolicy.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.CooldownActive, pauseRequired.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.CooldownActive, deleteRequired.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ControlPlaneOwnershipGap, futureControlPlane.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.NoExecutionNeeded, ready.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.True(futureControlPlane.ManagedConnectorRetryExecutionPolicy.IsOperatorOnly);
        Assert.True(ready.ManagedConnectorRetryExecutionPolicy.IsNotNeeded);
        Assert.False(ready.ManagedConnectorRetryExecutionPolicy.CanExecuteRetryThroughPolicy);
        Assert.False(pauseRequired.ManagedConnectorRetryExecutionPolicy.IsAutomaticRetryEnabled);
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandRetryState(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandRetryState(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Cooldown)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandRetryState(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandRetryState(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotNeeded)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandRetryCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.CooldownActive)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandRetryCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ControlPlaneOwnershipGap)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandRetryOperationId(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandRetryOperationId(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorRetryExecutionPolicyState(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyState(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorRetryExecutionPolicyState(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyState(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyCategory(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.CooldownActive)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorRetryExecutionPolicyCategory(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ControlPlaneOwnershipGap)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyOperationId(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([DeleteRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorRetryExecutionPolicyOperationId(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Delete)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable, observeOnly.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Empty, outOfPolicy.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Empty, waiting.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Empty, blocked.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Bounded, ready.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive, pauseRequired.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive, deleteRequired.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Empty, pauseSatisfied.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.InsufficientForAutomation, futureControlPlane.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Reconcile, ready.ManagedConnectorCommandJournal.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Pause, pauseRequired.ManagedConnectorCommandJournal.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Delete, deleteRequired.ManagedConnectorCommandJournal.OperationId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Reconcile, futureControlPlane.ManagedConnectorCommandJournal.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ObserveOnlyMode, observeOnly.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.NoRecordedCommand, outOfPolicy.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.BoundedRetention, ready.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.CooldownActive, pauseRequired.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.CooldownActive, deleteRequired.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ControlPlaneOwnershipGap, futureControlPlane.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.OperatorOnly, futureControlPlane.ManagedConnectorCommandJournal.CategoryIds);
        Assert.True(ready.ManagedConnectorCommandJournal.IsBounded);
        Assert.True(pauseRequired.ManagedConnectorCommandJournal.IsCooldownActive);
        Assert.True(deleteRequired.ManagedConnectorCommandJournal.IsCooldownActive);
        Assert.True(futureControlPlane.ManagedConnectorCommandJournal.IsInsufficientForAutomation);
        Assert.True(ready.ManagedConnectorCommandJournal.HasRecordedCommandHistory);
        Assert.False(outOfPolicy.ManagedConnectorCommandJournal.HasRecordedCommandHistory);
        Assert.Equal(1, ready.ManagedConnectorCommandJournal.TotalRecordedEntryCount);
        Assert.Equal(2, pauseRequired.ManagedConnectorCommandJournal.TotalRecordedEntryCount);
        Assert.Equal(2, deleteRequired.ManagedConnectorCommandJournal.TotalRecordedEntryCount);
        Assert.Equal([ObserveOnlyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandJournalState(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandJournalState(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Empty)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([ReadyRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandJournalState(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Bounded)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandJournalState(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandJournalState(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.InsufficientForAutomation)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, WaitingRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandJournalCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.NoRecordedCommand)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandJournalCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.CooldownActive)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorCommandJournalCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ControlPlaneOwnershipGap)
            .Select(static runtime => runtime.Id)
            .ToArray());

        timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-23T06:11:10Z", CultureInfo.InvariantCulture));
        pauseRequired = runtimeCatalog.GetById(PauseRequiredRuntimeId);
        deleteRequired = runtimeCatalog.GetById(DeleteRequiredRuntimeId);

        Assert.NotNull(pauseRequired);
        Assert.NotNull(deleteRequired);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Duplicate, pauseRequired.ManagedConnectorCommandRetry.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Duplicate, deleteRequired.ManagedConnectorCommandRetry.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DuplicateCommand, pauseRequired.ManagedConnectorCommandRetry.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DuplicateCommand, deleteRequired.ManagedConnectorCommandRetry.CategoryIds);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded, pauseRequired.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded, deleteRequired.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DuplicateCommand, pauseRequired.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DuplicateCommand, deleteRequired.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandRetryState(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Duplicate)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandRetryCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DuplicateCommand)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyCategory(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DuplicateCommand)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent, pauseRequired.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent, deleteRequired.ManagedConnectorCommandJournal.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.DuplicateCommand, pauseRequired.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.DuplicateCommand, deleteRequired.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandJournalState(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandJournalCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.DuplicateCommand)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorRetryExecutionPolicyCatalogElevatesManualApprovalAndPolicyBlockedPostCooldown()
    {
        const string policyBlockedRuntimeId = "inventory-policy-blocked-connector";
        const string policyBlockedCaptureId = "inventory-policy-blocked-cdc";

        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: policyBlockedRuntimeId,
                    captureId: policyBlockedCaptureId,
                    displayName: "Inventory Policy-Blocked Connector",
                    captureDisplayName: "Inventory Policy-Blocked CDC",
                    captureDescription: "Declares a drifted write-path mode while omitting connect-cluster governance truth so retry remains policy-blocked.",
                    connectClusterId: null,
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-policy-blocked",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 2,
                    taskIds: ["0", "1"]));
                options.Connectors.Add(CreateConnector(
                    runtimeId: PauseRequiredRuntimeId,
                    captureId: PauseRequiredCaptureId,
                    displayName: "Inventory Pause-Required Connector",
                    captureDisplayName: "Inventory Returns CDC",
                    captureDescription: "Needs a shared pause action because the connector is still running.",
                    connectClusterId: "connect-cluster-f",
                    connectorClass: "io.debezium.connector.mysql.MySqlConnector",
                    sourceProviderId: "mysql",
                    topicPrefix: "inventory-returns",
                    managementMode: "pause",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var commandExecutor = provider.GetRequiredService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>();

        await reportSink.ReportAsync(
            policyBlockedRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: policyBlockedCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-policy-blocked-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-policy",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-policy")
            ]);

        await reportSink.ReportAsync(
            PauseRequiredRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: PauseRequiredCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:09:50Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-pause-required-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-f",
                        ["connectorClass"] = "io.debezium.connector.mysql.MySqlConnector",
                        ["sourceProviderId"] = "mysql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-f")
            ]);

        timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-23T06:10:31Z", CultureInfo.InvariantCulture));
        var policyBlockedCommand = await commandExecutor.ExecuteAsync(
            policyBlockedRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile);

        timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-23T06:10:32Z", CultureInfo.InvariantCulture));
        var pauseBlockedCommand = await commandExecutor.ExecuteAsync(
            PauseRequiredRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause);

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, policyBlockedCommand.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, pauseBlockedCommand.State);

        var policyBlockedDuringCooldown = runtimeCatalog.GetById(policyBlockedRuntimeId);
        var pauseRequiredDuringCooldown = runtimeCatalog.GetById(PauseRequiredRuntimeId);

        Assert.NotNull(policyBlockedDuringCooldown);
        Assert.NotNull(pauseRequiredDuringCooldown);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown, policyBlockedDuringCooldown.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown, pauseRequiredDuringCooldown.ManagedConnectorRetryExecutionPolicy.State);

        timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-23T06:11:10Z", CultureInfo.InvariantCulture));

        var policyBlocked = runtimeCatalog.GetById(policyBlockedRuntimeId);
        var pauseRequired = runtimeCatalog.GetById(PauseRequiredRuntimeId);

        Assert.NotNull(policyBlocked);
        Assert.NotNull(pauseRequired);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.PolicyBlocked, policyBlocked.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.ManualApproval, pauseRequired.ManagedConnectorRetryExecutionPolicy.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.CommandRetry, policyBlocked.ManagedConnectorRetryExecutionPolicy.SourceId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.ExecutionApproval, pauseRequired.ManagedConnectorRetryExecutionPolicy.SourceId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ProviderExecutionBlocked, policyBlocked.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.GovernanceOutOfPolicy, policyBlocked.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ManualApprovalReady, pauseRequired.ManagedConnectorRetryExecutionPolicy.CategoryIds);
        Assert.Equal(
            [policyBlockedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyState(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.PolicyBlocked)
                .Select(static runtime => runtime.Id)
                .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyState(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.ManualApproval)
                .Select(static runtime => runtime.Id)
                .ToArray());
        Assert.Equal(
            [policyBlockedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyCategory(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.GovernanceOutOfPolicy)
                .Select(static runtime => runtime.Id)
                .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyCategory(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ManualApprovalReady)
                .Select(static runtime => runtime.Id)
                .ToArray());
        Assert.Equal(
            [policyBlockedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyOperationId(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorRetryExecutionPolicyOperationId(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Pause)
                .Select(static runtime => runtime.Id)
                .ToArray());
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorCommandJournalCatalogMarksTruncatedBoundedHistory()
    {
        const string truncatedRuntimeId = "inventory-journal-truncated-connector";
        const string truncatedCaptureId = "inventory-journal-truncated-cdc";

        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-24T02:00:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
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
                options.Connectors.Add(CreateConnector(
                    runtimeId: truncatedRuntimeId,
                    captureId: truncatedCaptureId,
                    displayName: "Inventory Journal Truncated Connector",
                    captureDisplayName: "Inventory Journal Truncated CDC",
                    captureDescription: "Records enough no-op reconcile outcomes to truncate the bounded shared command journal.",
                    connectClusterId: "connect-cluster-journal",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-journal-truncated",
                    managementMode: "apply-and-reconcile",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var commandExecutor = provider.GetRequiredService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>();

        await reportSink.ReportAsync(
            truncatedRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: truncatedCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-24T01:59:30Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-journal-truncated-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-journal",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-journal")
            ]);

        for (var index = 0; index < 25; index++)
        {
            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-24T02:00:00Z", CultureInfo.InvariantCulture).AddSeconds(index + 1));
            var result = await commandExecutor.ExecuteAsync(
                truncatedRuntimeId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile);

            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, result.State);
        }

        var truncatedRuntime = runtimeCatalog.GetById(truncatedRuntimeId);

        Assert.NotNull(truncatedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Truncated, truncatedRuntime.ManagedConnectorCommandJournal.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Reconcile, truncatedRuntime.ManagedConnectorCommandJournal.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.HistoryTruncated, truncatedRuntime.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.BoundedRetention, truncatedRuntime.ManagedConnectorCommandJournal.CategoryIds);
        Assert.Equal(25, truncatedRuntime.ManagedConnectorCommandJournal.TotalRecordedEntryCount);
        Assert.Equal(20, truncatedRuntime.ManagedConnectorCommandJournal.RetainedEntryCount);
        Assert.Equal(20, truncatedRuntime.ManagedConnectorCommandJournal.MaximumRetainedEntryCount);
        Assert.True(truncatedRuntime.ManagedConnectorCommandJournal.IsTruncated);
        Assert.True(truncatedRuntime.ManagedConnectorCommandJournal.HasTruncatedHistory);
        Assert.False(string.IsNullOrWhiteSpace(truncatedRuntime.ManagedConnectorCommandJournal.LatestAttemptId));
        Assert.False(string.IsNullOrWhiteSpace(truncatedRuntime.ManagedConnectorCommandJournal.OldestRetainedAttemptId));
        Assert.Equal(
            [truncatedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandJournalState(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Truncated)
                .Select(static runtime => runtime.Id)
                .ToArray());
        Assert.Equal(
            [truncatedRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorCommandJournalCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.HistoryTruncated)
                .Select(static runtime => runtime.Id)
                .ToArray());
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorCommandJournalDurabilityCatalogRecoversPersistedHistoryAcrossProviderRestart()
    {
        const string persistedRuntimeId = "inventory-journal-durable-connector";
        const string persistedCaptureId = "inventory-journal-durable-cdc";
        var baseRecordedAtUtc = DateTimeOffset.Parse("2026-04-24T04:00:00Z", CultureInfo.InvariantCulture);
        var timeProvider = new MutableTimeProvider(baseRecordedAtUtc);
        var persistenceDirectory = Path.Combine(Path.GetTempPath(), $"cephalon-eng-186-{Guid.NewGuid():N}");
        var persistencePath = Path.Combine(persistenceDirectory, "managed-connector-command-journal.json");

        ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TimeProvider>(timeProvider);
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "ModularVerticalSlice",
                    patterns: ["CQRS"]));
                engine.AddModule(new PlatformTestModule());
                engine.AddModule(new Phase8CatalogModule());
                engine.AddData(options =>
                {
                    options.ManagedConnectorCommandJournalPersistencePath = persistencePath;
                });
                engine.AddDebeziumData(options =>
                {
                    options.Connectors.Add(CreateConnector(
                        runtimeId: persistedRuntimeId,
                        captureId: persistedCaptureId,
                        displayName: "Inventory Durable Journal Connector",
                        captureDisplayName: "Inventory Durable Journal CDC",
                        captureDescription: "Persists bounded managed-connector command history so retry evidence can survive provider restart.",
                        connectClusterId: "connect-cluster-durable",
                        connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                        sourceProviderId: "postgresql",
                        topicPrefix: "inventory-journal-durable",
                        managementMode: "apply-and-reconcile",
                        expectedTaskCount: 1,
                        taskIds: ["0"]));
                });
            });

            return services.BuildServiceProvider();
        }

        CdcCaptureRuntimeObservation CreateObservation(string reportId) =>
            new(
                cdcCaptureId: persistedCaptureId,
                outcome: CdcCaptureRuntimeOutcomes.Captured,
                observedAtUtc: baseRecordedAtUtc.AddSeconds(-15),
                reportId: reportId,
                metadata: new Dictionary<string, string>
                {
                    ["connectorState"] = "RUNNING",
                    ["connectClusterId"] = "connect-cluster-durable",
                    ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                    ["sourceProviderId"] = "postgresql",
                    ["reportedTaskIds"] = "0",
                    ["activeTaskIds"] = "0"
                },
                reporterId: "connect-worker-durable");

        try
        {
            string initialAttemptId;

            using (var initialProvider = CreateProvider())
            {
                var reportSink = initialProvider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
                var runtimeCatalog = initialProvider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
                var commandExecutor = initialProvider.GetRequiredService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>();

                await reportSink.ReportAsync(
                    persistedRuntimeId,
                    [CreateObservation("debezium-report-journal-durable-001")]);

                timeProvider.SetUtcNow(baseRecordedAtUtc.AddSeconds(1));
                var execution = await commandExecutor.ExecuteAsync(
                    persistedRuntimeId,
                    CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile);

                Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, execution.State);

                var persistedRuntime = runtimeCatalog.GetById(persistedRuntimeId);

                Assert.NotNull(persistedRuntime);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.Persisted,
                    persistedRuntime.ManagedConnectorCommandJournalDurability.State);
                Assert.True(persistedRuntime.ManagedConnectorCommandJournalDurability.HasDurableStoreConfigured);
                Assert.True(persistedRuntime.ManagedConnectorCommandJournalDurability.HasPersistedSnapshot);
                Assert.True(persistedRuntime.ManagedConnectorCommandJournalDurability.HasPersistedRecordedHistory);
                Assert.False(persistedRuntime.ManagedConnectorCommandJournalDurability.HasRecoveredPersistedHistory);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.PersistenceHealthy,
                    persistedRuntime.ManagedConnectorCommandJournalDurability.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.PersistedRecordedHistory,
                    persistedRuntime.ManagedConnectorCommandJournalDurability.CategoryIds);
                Assert.False(string.IsNullOrWhiteSpace(persistedRuntime.ManagedConnectorCommandJournalDurability.PersistencePath));
                Assert.False(string.IsNullOrWhiteSpace(persistedRuntime.ManagedConnectorCommandJournal.LatestAttemptId));

                initialAttemptId = persistedRuntime.ManagedConnectorCommandJournal.LatestAttemptId;
            }

            timeProvider.SetUtcNow(baseRecordedAtUtc.AddMinutes(1));

            using var recoveredProvider = CreateProvider();
            var recoveredReportSink = recoveredProvider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
            var recoveredRuntimeCatalog = recoveredProvider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

            await recoveredReportSink.ReportAsync(
                persistedRuntimeId,
                [CreateObservation("debezium-report-journal-durable-002")]);

            var recoveredRuntime = recoveredRuntimeCatalog.GetById(persistedRuntimeId);
            var recoveredHistory = recoveredRuntimeCatalog.GetManagedConnectorCommandExecutionHistory(persistedRuntimeId);

            Assert.NotNull(recoveredRuntime);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.Recovered,
                recoveredRuntime.ManagedConnectorCommandJournalDurability.State);
            Assert.True(recoveredRuntime.ManagedConnectorCommandJournalDurability.HasDurableStoreConfigured);
            Assert.True(recoveredRuntime.ManagedConnectorCommandJournalDurability.HasPersistedSnapshot);
            Assert.True(recoveredRuntime.ManagedConnectorCommandJournalDurability.HasPersistedRecordedHistory);
            Assert.True(recoveredRuntime.ManagedConnectorCommandJournalDurability.HasRecoveredPersistedHistory);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.RecoveredHistory,
                recoveredRuntime.ManagedConnectorCommandJournalDurability.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.PersistenceHealthy,
                recoveredRuntime.ManagedConnectorCommandJournalDurability.CategoryIds);
            Assert.Equal(initialAttemptId, recoveredRuntime.ManagedConnectorCommandJournal.LatestAttemptId);
            Assert.Single(recoveredHistory);
            Assert.Equal(initialAttemptId, recoveredHistory[0].AttemptId);
            Assert.Equal(
                [persistedRuntimeId],
                recoveredRuntimeCatalog
                    .GetByManagedConnectorCommandJournalDurabilityState(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.Recovered)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [persistedRuntimeId],
                recoveredRuntimeCatalog
                    .GetByManagedConnectorCommandJournalDurabilityCategory(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.RecoveredHistory)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
        }
        finally
        {
            if (Directory.Exists(persistenceDirectory))
            {
                Directory.Delete(persistenceDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorAutomaticRetryExecutionCatalogRunsBoundedBackgroundRetryOnEligibleRuntime()
    {
        const string automaticRetryRuntimeId = "inventory-automatic-retry-connector";
        const string automaticRetryCaptureId = "inventory-automatic-retry-cdc";

        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-24T03:10:30Z", CultureInfo.InvariantCulture));
        var persistenceDirectory = Path.Combine(Path.GetTempPath(), $"cephalon-eng-187-{Guid.NewGuid():N}");
        var persistencePath = Path.Combine(persistenceDirectory, "managed-connector-command-journal.json");

        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<TimeProvider>(timeProvider);
            services.AddSingleton<ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter>(
                new AutomaticRetryTestExecutionAdapter(automaticRetryRuntimeId));
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "ModularVerticalSlice",
                    patterns: ["CQRS"]));
                engine.AddModule(new PlatformTestModule());
                engine.AddModule(new Phase8CatalogModule());
                engine.AddData(options =>
                {
                    options.EnableManagedConnectorAutomaticRetryExecution = true;
                    options.ManagedConnectorAutomaticRetryPollingIntervalSeconds = 1;
                    options.ManagedConnectorAutomaticRetryCoordinationOwnerId = "connect-worker-auto";
                    options.ManagedConnectorCommandJournalPersistencePath = persistencePath;
                });
                engine.AddDebeziumData(options =>
                {
                    options.Connectors.Add(CreateConnector(
                        runtimeId: automaticRetryRuntimeId,
                        captureId: automaticRetryCaptureId,
                        displayName: "Inventory Automatic Retry Connector",
                        captureDisplayName: "Inventory Automatic Retry CDC",
                        captureDescription: "Uses bounded shared retry truth to trigger one automatic restart retry after cooldown.",
                        connectClusterId: "connect-cluster-auto",
                        connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                        sourceProviderId: "postgresql",
                        topicPrefix: "inventory-automatic-retry",
                        managementMode: "restart",
                        expectedTaskCount: 1,
                        taskIds: ["0"]));
                });
            });

            using var provider = services.BuildServiceProvider();
            var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
            var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
            var commandExecutor = provider.GetRequiredService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>();
            var hostedServices = provider.GetServices<IHostedService>().ToArray();

            await reportSink.ReportAsync(
                automaticRetryRuntimeId,
                [
                    new CdcCaptureRuntimeObservation(
                        cdcCaptureId: automaticRetryCaptureId,
                        outcome: CdcCaptureRuntimeOutcomes.Captured,
                        observedAtUtc: DateTimeOffset.Parse("2026-04-24T03:10:00Z", CultureInfo.InvariantCulture),
                        reportId: "debezium-report-automatic-retry-001",
                        metadata: new Dictionary<string, string>
                        {
                            ["connectorState"] = "RUNNING",
                            ["connectClusterId"] = "connect-cluster-auto",
                            ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                            ["sourceProviderId"] = "postgresql",
                            ["reportedTaskIds"] = "0",
                            ["activeTaskIds"] = "0"
                        },
                        reporterId: "connect-worker-auto")
                ]);

            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-24T03:10:31Z", CultureInfo.InvariantCulture));
            var blockedCommand = await commandExecutor.ExecuteAsync(
                automaticRetryRuntimeId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart,
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
                {
                    Approve = true
                });

            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, blockedCommand.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.OperatorRequest,
                blockedCommand.InvocationSourceId);
            Assert.True(blockedCommand.ApprovalApplied);

            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-24T03:11:10Z", CultureInfo.InvariantCulture));

            var eligibleRuntime = runtimeCatalog.GetById(automaticRetryRuntimeId);

            Assert.NotNull(eligibleRuntime);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.RetryReady,
                eligibleRuntime.ManagedConnectorRetryExecutionPolicy.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Eligible,
                eligibleRuntime.ManagedConnectorAutomaticRetryExecution.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld,
                eligibleRuntime.ManagedConnectorAutomaticRetryCoordination.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.IdempotentSafe,
                eligibleRuntime.ManagedConnectorDistributedRetryLease.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.IdempotentSafe,
                eligibleRuntime.ManagedConnectorCrossNodeIdempotencyHardening.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Scheduled,
                eligibleRuntime.ManagedConnectorDistributedRetryOrchestration.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseExecutable,
                eligibleRuntime.ManagedConnectorMultiNodeLeaseExecution.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.Scheduled,
                eligibleRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.ExecutionHardened,
                eligibleRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderBlocked,
                eligibleRuntime.ManagedConnectorProviderOwnedWritePathExecution.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationReady,
                eligibleRuntime.ManagedConnectorProviderExecutionOrchestration.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipPartial,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningPartial,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates.ApplyAndReconcileReady,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.State);
            Assert.True(eligibleRuntime.ManagedConnectorRetryExecutionPolicy.CanReuseApprovalFromMatchingHistory);
            Assert.True(eligibleRuntime.ManagedConnectorAutomaticRetryExecution.CanReuseApprovalFromMatchingHistory);
            Assert.True(eligibleRuntime.ManagedConnectorAutomaticRetryExecution.LatestMatchingApprovalApplied);
            Assert.True(eligibleRuntime.ManagedConnectorAutomaticRetryCoordination.CanExecuteOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorDistributedRetryLease.CanExecuteAutomaticRetryOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CanExecuteAutomaticRetryOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorDistributedRetryOrchestration.CanScheduleAutomaticRetryOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorMultiNodeLeaseExecution.CanExecuteAutomaticRetryOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CanScheduleAutomaticRetryOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CanExecuteAutomaticRetryOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorProviderOwnedWritePathExecution.CanExecuteProviderOwnedWritePathOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorProviderExecutionOrchestration.CanOrchestrateProviderExecutionOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CanExerciseProviderOwnedControlPlaneOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.RequiresExplicitApproval);
            Assert.False(eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CanProvisionProviderOwnedControlPlaneOnCurrentNode);
            Assert.True(eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CanExecuteProviderOwnedControlPlaneApplyAndReconcileOnCurrentNode);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningSources.DurableSharedSchedulerOrchestration,
                eligibleRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart,
                eligibleRuntime.ManagedConnectorProviderOwnedWritePathExecution.OperationId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionSources.CommandExecution,
                eligibleRuntime.ManagedConnectorProviderOwnedWritePathExecution.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationSources.ProviderOwnedWritePathExecution,
                eligibleRuntime.ManagedConnectorProviderExecutionOrchestration.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipSources.ProviderExecutionOrchestration,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningSources.ProviderOwnedControlPlaneMutationReconcile,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionSources.CommandExecution,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.SourceId);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.OwnerMatch,
                eligibleRuntime.ManagedConnectorAutomaticRetryCoordination.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.ActiveLeaseHeld,
                eligibleRuntime.ManagedConnectorAutomaticRetryCoordination.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.RetryReady,
                eligibleRuntime.ManagedConnectorAutomaticRetryExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.CrossNodeIdempotentSafe,
                eligibleRuntime.ManagedConnectorDistributedRetryLease.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.PersistedHistory,
                eligibleRuntime.ManagedConnectorDistributedRetryLease.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.IdempotentSafe,
                eligibleRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.MatchingRetryHistory,
                eligibleRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.CurrentNodeSchedulable,
                eligibleRuntime.ManagedConnectorDistributedRetryOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.LeaseExecutable,
                eligibleRuntime.ManagedConnectorMultiNodeLeaseExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.CurrentNodeExecutable,
                eligibleRuntime.ManagedConnectorMultiNodeLeaseExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.Scheduled,
                eligibleRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.CurrentNodeSchedulable,
                eligibleRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.ExecutionHardened,
                eligibleRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.CurrentNodeExecutable,
                eligibleRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.SchedulerScheduled,
                eligibleRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderBlocked,
                eligibleRuntime.ManagedConnectorProviderOwnedWritePathExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.CurrentNodeExecutable,
                eligibleRuntime.ManagedConnectorProviderOwnedWritePathExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandBlocked,
                eligibleRuntime.ManagedConnectorProviderOwnedWritePathExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationReady,
                eligibleRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.CurrentNodeOrchestratable,
                eligibleRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderBlocked,
                eligibleRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.SchedulerScheduled,
                eligibleRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ExecutionHardened,
                eligibleRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipPartial,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.CurrentNodeExecutable,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderExecutionReady,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderBlocked,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.SchedulerScheduled,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ApprovalRequired,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningPartial,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.MutationBlocked,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ApprovalRequired,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CurrentNodeBlocked,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApplyAndReconcileReady,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApprovalRequired,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.CurrentNodeExecutable,
                eligibleRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CategoryIds);
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderExecutionOrchestrationState(CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationReady)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderExecutionOrchestrationCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationReady)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderExecutionOrchestrationOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneOwnershipState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipPartial)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneOwnershipCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipPartial)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneOwnershipOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneProvisioningState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningPartial)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneProvisioningCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ApprovalRequired)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneProvisioningOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates.ApplyAndReconcileReady)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApprovalRequired)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.False(string.IsNullOrWhiteSpace(eligibleRuntime.ManagedConnectorCrossNodeIdempotencyHardening.RetryFingerprint));

            try
            {
                foreach (var hostedService in hostedServices)
                {
                    await hostedService.StartAsync(CancellationToken.None);
                }

                CdcCaptureExecutionRuntimeDescriptor? completedRuntime = null;
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    completedRuntime = runtimeCatalog.GetById(automaticRetryRuntimeId);
                    if (completedRuntime?.ManagedConnectorAutomaticRetryExecution.IsCompleted == true)
                    {
                        break;
                    }

                    await Task.Delay(50);
                }

                Assert.NotNull(completedRuntime);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Completed,
                    completedRuntime.ManagedConnectorAutomaticRetryExecution.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart,
                    completedRuntime.ManagedConnectorAutomaticRetryExecution.OperationId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted,
                    completedRuntime.ManagedConnectorAutomaticRetryExecution.LatestAutomaticRetryState);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.AutomaticRetry,
                    completedRuntime.ManagedConnectorAutomaticRetryExecution.LatestCommandExecutionInvocationSourceId);
                Assert.True(completedRuntime.ManagedConnectorAutomaticRetryExecution.HasAutomaticRetryAttempt);
                Assert.True(completedRuntime.ManagedConnectorAutomaticRetryExecution.HasMatchingAutomaticRetryAttempt);
                Assert.True(completedRuntime.ManagedConnectorAutomaticRetryExecution.CanReuseApprovalFromMatchingHistory);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.AutomaticAttemptRecorded,
                    completedRuntime.ManagedConnectorAutomaticRetryExecution.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionAdapted,
                    completedRuntime.ManagedConnectorAutomaticRetryExecution.CategoryIds);
                Assert.False(string.IsNullOrWhiteSpace(completedRuntime.ManagedConnectorAutomaticRetryExecution.LatestAutomaticRetryAttemptId));
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld,
                    completedRuntime.ManagedConnectorAutomaticRetryCoordination.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.IdempotentSafe,
                    completedRuntime.ManagedConnectorDistributedRetryLease.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.IdempotentSafe,
                    completedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Cooldown,
                    completedRuntime.ManagedConnectorDistributedRetryOrchestration.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseBlocked,
                    completedRuntime.ManagedConnectorMultiNodeLeaseExecution.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.Unscheduled,
                    completedRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.ExecutionHardened,
                    completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderOwnedExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedWritePathExecution.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationExecuting,
                    completedRuntime.ManagedConnectorProviderExecutionOrchestration.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipActive,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates.ApplyAndReconcileExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.ApplyAndReconcileHardened,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates.MutationHardened,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerExecuting,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.State);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.DependencyReady,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.State);
                Assert.True(completedRuntime.ManagedConnectorAutomaticRetryCoordination.CanExecuteOnCurrentNode);
                Assert.True(completedRuntime.ManagedConnectorDistributedRetryLease.CanExecuteAutomaticRetryOnCurrentNode);
                Assert.True(completedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CanExecuteAutomaticRetryOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorDistributedRetryOrchestration.CanScheduleAutomaticRetryOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorMultiNodeLeaseExecution.CanExecuteAutomaticRetryOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CanScheduleAutomaticRetryOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CanExecuteAutomaticRetryOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderOwnedWritePathExecution.CanExecuteProviderOwnedWritePathOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderExecutionOrchestration.CanOrchestrateProviderExecutionOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CanExerciseProviderOwnedControlPlaneOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CanProvisionProviderOwnedControlPlaneOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CanExecuteProviderOwnedControlPlaneApplyAndReconcileOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CanExecuteDependencyAwareApplyAndReconcileOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CanExecuteDependencyAwareProvisioningAndMutationOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CanExecuteDependencyAwareProvisioningOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CanExecuteDependencyAwareMutationOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CanUseProviderSpecificControlPlaneMaterializerOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CanExecuteDependencyAwareTeardownAndMutationExecutionOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CanExecuteDependencyAwareTeardownOnCurrentNode);
                Assert.False(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CanExecuteDependencyAwareMutationExecutionOnCurrentNode);
                Assert.True(completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.IsMutationOperation);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.IsMutationExecutionOperation);
                Assert.False(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.IsTeardownOperation);
                Assert.Equal(DebeziumDataOptions.ProviderId, completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.ProviderId);
                Assert.Equal(DebeziumDataOptions.ProviderId, completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.ProviderId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.MaterializerId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.MaterializerId);
                Assert.Equal("http-rest", completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.TransportKind);
                Assert.Equal("http-rest", completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.TransportKind);
                Assert.Equal("debezium-kafka-connect-rest", completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.ProviderSurfaceId);
                Assert.Equal("debezium-kafka-connect-rest", completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.ProviderSurfaceId);
                Assert.Equal(automaticRetryRuntimeId, completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.ConnectorId);
                Assert.Equal(automaticRetryRuntimeId, completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.ConnectorId);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.HasProviderIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.HasMaterializerIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.HasTransportIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.HasProviderSurfaceIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.HasConnectorIdentity);
                Assert.False(completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.HasWorkerIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.HasProviderIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.HasMaterializerIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.HasTransportIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.HasProviderSurfaceIdentity);
                Assert.True(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.HasConnectorIdentity);
                Assert.False(completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.HasWorkerIdentity);
                Assert.Equal("connect-worker-auto", completedRuntime.ManagedConnectorAutomaticRetryCoordination.CoordinationOwnerId);
                Assert.Equal("connect-worker-auto", completedRuntime.ManagedConnectorDistributedRetryLease.CoordinationOwnerId);
                Assert.Equal("connect-worker-auto", completedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CoordinationOwnerId);
                Assert.Equal("connect-worker-auto", completedRuntime.ManagedConnectorDistributedRetryOrchestration.CoordinationOwnerId);
                Assert.Equal("connect-worker-auto", completedRuntime.ManagedConnectorMultiNodeLeaseExecution.CoordinationOwnerId);
                Assert.Equal("connect-worker-auto", completedRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CoordinationOwnerId);
                Assert.Equal("connect-worker-auto", completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CoordinationOwnerId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId,
                    completedRuntime.ManagedConnectorDistributedRetryOrchestration.SchedulerId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId,
                    completedRuntime.ManagedConnectorMultiNodeLeaseExecution.SchedulerId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId,
                    completedRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.SchedulerId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId,
                    completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.SchedulerId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningSources.CommandExecution,
                    completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart,
                    completedRuntime.ManagedConnectorProviderOwnedWritePathExecution.OperationId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionSources.CommandExecution,
                    completedRuntime.ManagedConnectorProviderOwnedWritePathExecution.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationSources.ProviderOwnedWritePathExecution,
                    completedRuntime.ManagedConnectorProviderExecutionOrchestration.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipSources.ProviderExecutionOrchestration,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningSources.ProviderOwnedControlPlaneMutationReconcile,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionSources.CommandExecution,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningSources.ProviderOwnedControlPlaneApplyAndReconcileExecution,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningSources.ProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerSources.ProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningSources.ProviderSpecificControlPlaneMaterializer,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.SourceId);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted,
                    completedRuntime.ManagedConnectorProviderOwnedWritePathExecution.LatestCommandExecutionState);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.AutomaticRetry,
                    completedRuntime.ManagedConnectorProviderOwnedWritePathExecution.LatestCommandExecutionInvocationSourceId);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.IdempotentSafe,
                    completedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.MatchingAutomaticRetryAttempt,
                    completedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.CooldownWindow,
                    completedRuntime.ManagedConnectorDistributedRetryOrchestration.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.AutomaticRetryAttemptRecorded,
                    completedRuntime.ManagedConnectorDistributedRetryOrchestration.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.LeaseBlocked,
                    completedRuntime.ManagedConnectorMultiNodeLeaseExecution.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.CooldownWindow,
                    completedRuntime.ManagedConnectorMultiNodeLeaseExecution.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.CurrentNodeBlocked,
                    completedRuntime.ManagedConnectorMultiNodeLeaseExecution.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.Unscheduled,
                    completedRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipActive,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderExecutionExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderOwnedExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderCommandAdapted,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.MutationOperation,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderCommandAdapted,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileHardened,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.CurrentNodeBlocked,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.MutationHardened,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.ApplyAndReconcileHardened,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.ProvisioningExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.MutationExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.CurrentNodeMutationBlocked,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.ProviderCommandAdapted,
                    completedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerExecuting,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderIdentityReady,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerIdentityReady,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.TransportIdentityReady,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderSurfaceReady,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ConnectorIdentityReady,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.WorkerIdentityUnavailable,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.CurrentNodeBlocked,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderCommandAdapted,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.DependencyReady,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.MaterializerExecuting,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.MutationExecutionOperation,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.CurrentNodeBlocked,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.CurrentNodeMutationExecutionBlocked,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.ProviderCommandAdapted,
                    completedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.CooldownWindow,
                    completedRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.ExecutionHardened,
                    completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.AutomaticRetryAttemptRecorded,
                    completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.MatchingAutomaticRetryAttempt,
                    completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.LatestAutomaticExecutionAdapted,
                    completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderOwnedExecuting,
                    completedRuntime.ManagedConnectorProviderOwnedWritePathExecution.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandAdapted,
                    completedRuntime.ManagedConnectorProviderOwnedWritePathExecution.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationExecuting,
                    completedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderOwnedExecuting,
                    completedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.SchedulerUnscheduled,
                    completedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ExecutionHardened,
                    completedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
                Assert.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderCommandAdapted,
                    completedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);

                var history = runtimeCatalog.GetManagedConnectorCommandExecutionHistory(automaticRetryRuntimeId);

                Assert.Equal(2, history.Count);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.AutomaticRetry,
                    history[0].InvocationSourceId);
                Assert.True(history[0].IsAutomaticRetryInvocation);
                Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, history[0].State);
                Assert.True(history[0].ApprovalApplied);
                Assert.Equal(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.OperatorRequest,
                    history[1].InvocationSourceId);
                Assert.True(history[1].IsOperatorRequestInvocation);
                Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, history[1].State);
                Assert.True(history[1].ApprovalApplied);

                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorAutomaticRetryExecutionState(CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Completed)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorAutomaticRetryExecutionCategory(CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.AutomaticAttemptRecorded)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorAutomaticRetryExecutionOperationId(CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds.Restart)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorAutomaticRetryCoordinationState(CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorAutomaticRetryCoordinationCategory(CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.OwnerMatch)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorAutomaticRetryCoordinationOwnerId("connect-worker-auto")
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDistributedRetryLeaseState(CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.IdempotentSafe)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDistributedRetryLeaseCategory(CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.CrossNodeIdempotentSafe)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDistributedRetryLeaseOwnerId("connect-worker-auto")
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorCrossNodeIdempotencyHardeningState(CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.IdempotentSafe)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorCrossNodeIdempotencyHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.IdempotentSafe)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorCrossNodeIdempotencyHardeningOwnerId("connect-worker-auto")
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorCrossNodeIdempotencyHardeningRetryFingerprint(completedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.RetryFingerprint)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDistributedRetryOrchestrationState(CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Cooldown)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDistributedRetryOrchestrationCategory(CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.CooldownWindow)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDistributedRetryOrchestrationOwnerId("connect-worker-auto")
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorMultiNodeLeaseExecutionState(CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseBlocked)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorMultiNodeLeaseExecutionCategory(CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.CurrentNodeBlocked)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorMultiNodeLeaseExecutionOwnerId("connect-worker-auto")
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDurableSharedSchedulerOrchestrationState(CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.Unscheduled)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDurableSharedSchedulerOrchestrationCategory(CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.CooldownWindow)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorDurableSharedSchedulerOrchestrationOwnerId("connect-worker-auto")
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorSchedulerRecoveryExecutionHardeningState(CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.ExecutionHardened)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorSchedulerRecoveryExecutionHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.ExecutionHardened)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorSchedulerRecoveryExecutionHardeningOwnerId("connect-worker-auto")
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorSchedulerRecoveryExecutionHardeningRetryFingerprint(completedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.RetryFingerprint)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorProviderOwnedWritePathExecutionState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderOwnedExecuting)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorProviderOwnedWritePathExecutionCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandAdapted)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorProviderOwnedWritePathExecutionOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.ApplyAndReconcileHardened)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.CurrentNodeBlocked)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates.MutationHardened)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
                Assert.Equal(
                    [automaticRetryRuntimeId],
                    runtimeCatalog
                        .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.CurrentNodeMutationBlocked)
                        .Select(static runtime => runtime.Id)
                        .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerState(CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerExecuting)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderCommandAdapted)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerProviderId(DebeziumDataOptions.ProviderId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerProviderSurfaceId("debezium-kafka-connect-rest")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerConnectorId(automaticRetryRuntimeId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningState(CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.DependencyReady)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.ProviderCommandAdapted)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningProviderId(DebeziumDataOptions.ProviderId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningProviderSurfaceId("debezium-kafka-connect-rest")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningMaterializerId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningConnectorId(automaticRetryRuntimeId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            }
            finally
            {
                foreach (var hostedService in hostedServices.Reverse())
                {
                    await hostedService.StopAsync(CancellationToken.None);
                }
            }
        }
        finally
        {
            if (Directory.Exists(persistenceDirectory))
            {
                Directory.Delete(persistenceDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AddDebeziumData_ManagedConnectorAutomaticRetryExecutionCatalogBlocksBackgroundRetryWhenCrossNodeIdempotencyRemainsInMemoryOnly()
    {
        const string automaticRetryRuntimeId = "inventory-automatic-retry-risk-connector";
        const string automaticRetryCaptureId = "inventory-automatic-retry-risk-cdc";

        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-24T03:30:30Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddSingleton<ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter>(
            new AutomaticRetryTestExecutionAdapter(automaticRetryRuntimeId));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableManagedConnectorAutomaticRetryExecution = true;
                options.ManagedConnectorAutomaticRetryPollingIntervalSeconds = 1;
                options.ManagedConnectorAutomaticRetryCoordinationOwnerId = "connect-worker-risk";
            });
            engine.AddDebeziumData(options =>
            {
                options.Connectors.Add(CreateConnector(
                    runtimeId: automaticRetryRuntimeId,
                    captureId: automaticRetryCaptureId,
                    displayName: "Inventory Automatic Retry Risk Connector",
                    captureDisplayName: "Inventory Automatic Retry Risk CDC",
                    captureDescription: "Keeps automatic retry eligible but blocks background execution when cross-node idempotency still depends on in-memory command history only.",
                    connectClusterId: "connect-cluster-auto-risk",
                    connectorClass: "io.debezium.connector.postgresql.PostgresConnector",
                    sourceProviderId: "postgresql",
                    topicPrefix: "inventory-automatic-retry-risk",
                    managementMode: "restart",
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var commandExecutor = provider.GetRequiredService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();

        await reportSink.ReportAsync(
            automaticRetryRuntimeId,
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: automaticRetryCaptureId,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-24T03:30:00Z", CultureInfo.InvariantCulture),
                    reportId: "debezium-report-automatic-retry-risk-001",
                    metadata: new Dictionary<string, string>
                    {
                        ["connectorState"] = "RUNNING",
                        ["connectClusterId"] = "connect-cluster-auto-risk",
                        ["connectorClass"] = "io.debezium.connector.postgresql.PostgresConnector",
                        ["sourceProviderId"] = "postgresql",
                        ["reportedTaskIds"] = "0",
                        ["activeTaskIds"] = "0"
                    },
                    reporterId: "connect-worker-risk")
            ]);

        timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-24T03:30:31Z", CultureInfo.InvariantCulture));
        var blockedCommand = await commandExecutor.ExecuteAsync(
            automaticRetryRuntimeId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart,
            new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
            {
                Approve = true
            });

        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, blockedCommand.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.OperatorRequest,
            blockedCommand.InvocationSourceId);
        Assert.True(blockedCommand.ApprovalApplied);

        timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-24T03:31:10Z", CultureInfo.InvariantCulture));

        var riskRuntime = runtimeCatalog.GetById(automaticRetryRuntimeId);

        Assert.NotNull(riskRuntime);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Eligible,
            riskRuntime.ManagedConnectorAutomaticRetryExecution.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld,
            riskRuntime.ManagedConnectorAutomaticRetryCoordination.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.IdempotencyRisk,
            riskRuntime.ManagedConnectorDistributedRetryLease.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.ReplayWindowRisk,
            riskRuntime.ManagedConnectorCrossNodeIdempotencyHardening.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Blocked,
            riskRuntime.ManagedConnectorDistributedRetryOrchestration.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseBlocked,
            riskRuntime.ManagedConnectorMultiNodeLeaseExecution.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.RecoveryNeeded,
            riskRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.RecoveryBlocked,
            riskRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderOwnedRisk,
            riskRuntime.ManagedConnectorProviderOwnedWritePathExecution.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipRisk,
            riskRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.State);
        Assert.False(riskRuntime.ManagedConnectorDistributedRetryLease.CanExecuteAutomaticRetryOnCurrentNode);
        Assert.False(riskRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CanExecuteAutomaticRetryOnCurrentNode);
        Assert.False(riskRuntime.ManagedConnectorDistributedRetryOrchestration.CanScheduleAutomaticRetryOnCurrentNode);
        Assert.False(riskRuntime.ManagedConnectorMultiNodeLeaseExecution.CanExecuteAutomaticRetryOnCurrentNode);
        Assert.False(riskRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CanScheduleAutomaticRetryOnCurrentNode);
        Assert.False(riskRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CanExecuteAutomaticRetryOnCurrentNode);
        Assert.False(riskRuntime.ManagedConnectorProviderOwnedWritePathExecution.CanExecuteProviderOwnedWritePathOnCurrentNode);
        Assert.False(riskRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CanExerciseProviderOwnedControlPlaneOnCurrentNode);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.InMemoryJournalOnly,
            riskRuntime.ManagedConnectorDistributedRetryLease.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.CrossNodeIdempotencyRisk,
            riskRuntime.ManagedConnectorDistributedRetryLease.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.InMemoryJournalOnly,
            riskRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.ReplayWindowRisk,
            riskRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.InMemoryJournalOnly,
            riskRuntime.ManagedConnectorDistributedRetryOrchestration.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.CrossNodeIdempotencyRisk,
            riskRuntime.ManagedConnectorDistributedRetryOrchestration.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.LeaseBlocked,
            riskRuntime.ManagedConnectorMultiNodeLeaseExecution.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.CrossNodeIdempotencyRisk,
            riskRuntime.ManagedConnectorMultiNodeLeaseExecution.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.CurrentNodeBlocked,
            riskRuntime.ManagedConnectorMultiNodeLeaseExecution.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.RecoveryNeeded,
            riskRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.InMemoryJournalOnly,
            riskRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.RecoveryBlocked,
            riskRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.InMemoryJournalOnly,
            riskRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.CurrentNodeBlocked,
            riskRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CategoryIds);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningSources.CommandJournalDurability,
            riskRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.SourceId);
        Assert.Equal(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionSources.CommandExecution,
            riskRuntime.ManagedConnectorProviderOwnedWritePathExecution.SourceId);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderOwnedRisk,
            riskRuntime.ManagedConnectorProviderOwnedWritePathExecution.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.CurrentNodeBlocked,
            riskRuntime.ManagedConnectorProviderOwnedWritePathExecution.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipRisk,
            riskRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.CurrentNodeBlocked,
            riskRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.RecoveryBlocked,
            riskRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
        Assert.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderOwnedRisk,
            riskRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);

        try
        {
            foreach (var hostedService in hostedServices)
            {
                await hostedService.StartAsync(CancellationToken.None);
            }

            await Task.Delay(1400);

            var refreshedRuntime = runtimeCatalog.GetById(automaticRetryRuntimeId);
            var history = runtimeCatalog.GetManagedConnectorCommandExecutionHistory(automaticRetryRuntimeId);

            Assert.NotNull(refreshedRuntime);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Eligible,
                refreshedRuntime.ManagedConnectorAutomaticRetryExecution.State);
            Assert.False(refreshedRuntime.ManagedConnectorAutomaticRetryExecution.HasAutomaticRetryAttempt);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.IdempotencyRisk,
                refreshedRuntime.ManagedConnectorDistributedRetryLease.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.ReplayWindowRisk,
                refreshedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Blocked,
                refreshedRuntime.ManagedConnectorDistributedRetryOrchestration.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseBlocked,
                refreshedRuntime.ManagedConnectorMultiNodeLeaseExecution.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.RecoveryNeeded,
                refreshedRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.RecoveryBlocked,
                refreshedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderOwnedRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedWritePathExecution.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationRisk,
                refreshedRuntime.ManagedConnectorProviderExecutionOrchestration.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates.ApplyAndReconcileRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.DependencyRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates.DependencyRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerRisk,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.DependencyRisk,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.State);
            Assert.False(refreshedRuntime.ManagedConnectorDistributedRetryLease.HasMatchingAutomaticRetryAttempt);
            Assert.False(refreshedRuntime.ManagedConnectorDistributedRetryLease.CanExecuteAutomaticRetryOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.CanExecuteAutomaticRetryOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorDistributedRetryOrchestration.CanScheduleAutomaticRetryOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorMultiNodeLeaseExecution.CanExecuteAutomaticRetryOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorDurableSharedSchedulerOrchestration.CanScheduleAutomaticRetryOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.CanExecuteAutomaticRetryOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderOwnedWritePathExecution.CanExecuteProviderOwnedWritePathOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderExecutionOrchestration.CanOrchestrateProviderExecutionOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CanExerciseProviderOwnedControlPlaneOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CanProvisionProviderOwnedControlPlaneOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CanExecuteProviderOwnedControlPlaneApplyAndReconcileOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CanExecuteDependencyAwareApplyAndReconcileOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CanExecuteDependencyAwareProvisioningAndMutationOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CanExecuteDependencyAwareProvisioningOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CanExecuteDependencyAwareMutationOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CanUseProviderSpecificControlPlaneMaterializerOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CanExecuteDependencyAwareTeardownAndMutationExecutionOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CanExecuteDependencyAwareTeardownOnCurrentNode);
            Assert.False(refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CanExecuteDependencyAwareMutationExecutionOnCurrentNode);
            Assert.Equal(DebeziumDataOptions.ProviderId, refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.ProviderId);
            Assert.Equal(DebeziumDataOptions.ProviderId, refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.ProviderId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.MaterializerId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.MaterializerId);
            Assert.Equal("http-rest", refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.TransportKind);
            Assert.Equal("http-rest", refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.TransportKind);
            Assert.Equal("debezium-kafka-connect-rest", refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.ProviderSurfaceId);
            Assert.Equal("debezium-kafka-connect-rest", refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.ProviderSurfaceId);
            Assert.Equal(automaticRetryRuntimeId, refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.ConnectorId);
            Assert.Equal(automaticRetryRuntimeId, refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.ConnectorId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationSources.SchedulerRecoveryExecutionHardening,
                refreshedRuntime.ManagedConnectorProviderExecutionOrchestration.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipSources.SchedulerRecoveryExecutionHardening,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningSources.ProviderOwnedControlPlaneMutationReconcile,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionSources.CommandExecution,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerSources.ProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.SourceId);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningSources.CommandExecution,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.SourceId);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationRisk,
                refreshedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderOwnedRisk,
                refreshedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.RecoveryBlocked,
                refreshedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.CurrentNodeBlocked,
                refreshedRuntime.ManagedConnectorProviderExecutionOrchestration.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.CurrentNodeBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.RecoveryBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneOwnership.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CurrentNodeBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.MutationRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApplyAndReconcileRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.CurrentNodeBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ProviderCommandBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DependencyRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.CurrentNodeBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.DependencyRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.ProvisioningRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.MutationRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.ApplyAndReconcileRisk,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.CurrentNodeMutationBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.ProviderCommandBlocked,
                refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerRisk,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.CurrentNodeBlocked,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderCommandBlocked,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.DependencyRisk,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.MaterializerRisk,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.CurrentNodeBlocked,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.CurrentNodeMutationExecutionBlocked,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);
            Assert.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.ProviderCommandBlocked,
                refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.CategoryIds);

            Assert.Single(history);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.OperatorRequest,
                history[0].InvocationSourceId);
            Assert.True(history[0].IsOperatorRequestInvocation);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, history[0].State);

            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorCrossNodeIdempotencyHardeningState(CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.ReplayWindowRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorCrossNodeIdempotencyHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.ReplayWindowRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorCrossNodeIdempotencyHardeningOwnerId("connect-worker-risk")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorCrossNodeIdempotencyHardeningRetryFingerprint(refreshedRuntime.ManagedConnectorCrossNodeIdempotencyHardening.RetryFingerprint)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDistributedRetryLeaseState(CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.IdempotencyRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDistributedRetryLeaseCategory(CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.InMemoryJournalOnly)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDistributedRetryLeaseOwnerId("connect-worker-risk")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDistributedRetryOrchestrationState(CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Blocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDistributedRetryOrchestrationCategory(CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.InMemoryJournalOnly)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDistributedRetryOrchestrationOwnerId("connect-worker-risk")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorMultiNodeLeaseExecutionState(CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorMultiNodeLeaseExecutionCategory(CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.CurrentNodeBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorMultiNodeLeaseExecutionOwnerId("connect-worker-risk")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDurableSharedSchedulerOrchestrationState(CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.RecoveryNeeded)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDurableSharedSchedulerOrchestrationCategory(CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.RecoveryNeeded)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorDurableSharedSchedulerOrchestrationOwnerId("connect-worker-risk")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorSchedulerRecoveryExecutionHardeningState(CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.RecoveryBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorSchedulerRecoveryExecutionHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.InMemoryJournalOnly)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorSchedulerRecoveryExecutionHardeningOwnerId("connect-worker-risk")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorSchedulerRecoveryExecutionHardeningRetryFingerprint(refreshedRuntime.ManagedConnectorSchedulerRecoveryExecutionHardening.RetryFingerprint)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedWritePathExecutionState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderOwnedRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedWritePathExecutionCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderOwnedRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedWritePathExecutionOperationId(refreshedRuntime.ManagedConnectorProviderOwnedWritePathExecution.OperationId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderExecutionOrchestrationState(CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderExecutionOrchestrationCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.RecoveryBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderExecutionOrchestrationOperationId(refreshedRuntime.ManagedConnectorProviderExecutionOrchestration.OperationId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneProvisioningState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneProvisioningCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneProvisioningOperationId(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneProvisioning.OperationId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates.ApplyAndReconcileRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ProviderCommandBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionOperationId(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.OperationId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.DependencyRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningOperationId(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening.OperationId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningState(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates.DependencyRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.ProviderCommandBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningOperationId(refreshedRuntime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening.OperationId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerState(CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderCommandBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerProviderId(DebeziumDataOptions.ProviderId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerProviderSurfaceId("debezium-kafka-connect-rest")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerConnectorId(automaticRetryRuntimeId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneMaterializerOperationId(refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneMaterializer.OperationId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningState(CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.DependencyRisk)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategory(CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories.ProviderCommandBlocked)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningProviderId(DebeziumDataOptions.ProviderId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningProviderSurfaceId("debezium-kafka-connect-rest")
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningMaterializerId(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningConnectorId(automaticRetryRuntimeId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
            Assert.Equal(
                [automaticRetryRuntimeId],
                runtimeCatalog
                    .GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningOperationId(refreshedRuntime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening.OperationId)
                    .Select(static runtime => runtime.Id)
                    .ToArray());
        }
        finally
        {
            foreach (var hostedService in hostedServices.Reverse())
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
        }
    }

    private sealed class AutomaticRetryTestExecutionAdapter(string runtimeId)
        : ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter
    {
        public string AdapterId => "cephalon-test-automatic-retry";

        public bool CanHandle(CdcCaptureExecutionRuntimeDescriptor runtime)
        {
            ArgumentNullException.ThrowIfNull(runtime);

            return string.Equals(runtime.Id, runtimeId, StringComparison.OrdinalIgnoreCase);
        }

        public ValueTask<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> ExecuteAsync(
            CdcCaptureExecutionRuntimeDescriptor runtime,
            string operationId,
            CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(runtime);
            ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

            cancellationToken.ThrowIfCancellationRequested();

            var normalizedOperationId = operationId.Trim();
            var executionAdapter = runtime.ManagedConnectorExecutionAdapter;
            var state = runtime.ManagedConnectorCommandExecution.IsBlocked &&
                        runtime.ManagedConnectorCommandExecution.IsOperatorRequestInvocation
                ? CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted
                : CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked;
            var description = string.Equals(
                state,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted,
                StringComparison.OrdinalIgnoreCase)
                ? "The automatic retry execution lane reused the approved restart intent and translated one provider command after the transient block cleared."
                : "The provider adapter intentionally simulates one transient restart block so the shared automatic retry lane can recover it after cooldown.";
            var transportKind = string.Equals(
                state,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted,
                StringComparison.OrdinalIgnoreCase)
                ? "http-rest"
                : null;
            var httpMethod = string.Equals(
                state,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted,
                StringComparison.OrdinalIgnoreCase)
                ? "POST"
                : null;
            var relativePath = string.Equals(
                state,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted,
                StringComparison.OrdinalIgnoreCase)
                ? $"/connectors/{Uri.EscapeDataString(runtime.Id)}/restart?includeTasks=true&onlyFailed=false"
                : null;

            return ValueTask.FromResult(new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult(state, description)
            {
                ExecutionRuntimeId = runtime.Id,
                RequestedOperationId = normalizedOperationId,
                ResolvedOperationId = executionAdapter.OperationId,
                ExecutionAdapterState = executionAdapter.State,
                CommandIssuanceState = executionAdapter.CommandIssuanceState,
                CommandEnvelopeState = executionAdapter.CommandEnvelopeState,
                AdapterId = AdapterId,
                ProviderId = runtime.Metadata.TryGetValue("provider", out var providerId) && !string.IsNullOrWhiteSpace(providerId)
                    ? providerId.Trim()
                    : DebeziumDataOptions.ProviderId,
                TransportKind = transportKind,
                HttpMethod = httpMethod,
                RelativePath = relativePath,
                ConnectClusterId = executionAdapter.ConnectClusterId,
                ConnectorId = runtime.Id,
                ConnectorClass = executionAdapter.ConnectorClass,
                SourceProviderId = executionAdapter.SourceProviderId,
                ManagementMode = executionAdapter.ManagementMode,
                SourceId = executionAdapter.SourceId,
                CommandFingerprint = executionAdapter.CommandFingerprint,
                IssuanceFingerprint = executionAdapter.IssuanceFingerprint,
                AdapterFingerprint = executionAdapter.AdapterFingerprint,
                ExecutionFingerprint = string.Join(
                    "|",
                    [
                        "cephalon-test-automatic-retry-execution/v1",
                        $"runtime={runtime.Id}",
                        $"operation={normalizedOperationId}",
                        $"state={state}",
                        $"approvalApplied={(request?.Approve == true).ToString().ToLowerInvariant()}",
                        $"destructiveApplied={(request?.AllowDestructive == true).ToString().ToLowerInvariant()}"
                    ]),
                RequiresExplicitApproval = executionAdapter.RequiresExplicitApproval,
                ApprovalApplied = request?.Approve == true,
                IsDestructiveOperation = executionAdapter.IsDestructiveOperation,
                DestructiveAllowanceApplied = request?.AllowDestructive == true,
                WouldApplyChanges = executionAdapter.WouldApplyChanges
            });
        }
    }

    private static DebeziumConnectorOptions CreateConnector(
        string runtimeId,
        string captureId,
        string displayName,
        string captureDisplayName,
        string captureDescription,
        string? connectClusterId,
        string connectorClass,
        string sourceProviderId,
        string topicPrefix,
        string managementMode,
        int? expectedTaskCount,
        IReadOnlyList<string> taskIds,
        IReadOnlyList<string>? edgeNodeIds = null)
    {
        var connector = new DebeziumConnectorOptions
        {
            Id = runtimeId,
            DisplayName = displayName,
            Description = $"Represents Debezium-managed connector '{runtimeId}' and projects its external runtime observations into the shared Cephalon CDC runtime catalog.",
            ConnectClusterId = connectClusterId ?? string.Empty,
            ConnectorClass = connectorClass,
            SourceProviderId = sourceProviderId,
            TopicPrefix = topicPrefix,
            ExecutionOwnership = "external-managed",
            ExecutionTopology = "managed-connector",
            AcknowledgementMode = "connector-offset-commit",
            ManagementMode = managementMode,
            ObservationStaleAfterSeconds = 180,
            ReporterLeaseSeconds = 120,
            RejectConflictingReporterIds = true,
            ExpectedTaskCount = expectedTaskCount
        };

        foreach (var taskId in taskIds)
        {
            connector.TaskIds.Add(taskId);
        }

        foreach (var edgeNodeId in edgeNodeIds ?? [])
        {
            connector.EdgeNodeIds.Add(edgeNodeId);
        }

        connector.CdcCaptures.Add(new DebeziumCaptureOptions
        {
            Id = captureId,
            DisplayName = captureDisplayName,
            Description = captureDescription,
            SourceModuleId = "phase8-runtime-catalogs",
            OutboxId = "tenant-event-outbox",
            TopicName = $"inventory.public.{captureId.Replace("-cdc", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("inventory-", string.Empty, StringComparison.OrdinalIgnoreCase)}",
            SnapshotMode = "initial"
        });

        return connector;
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset utcNow = now;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void SetUtcNow(DateTimeOffset value)
        {
            utcNow = value;
        }
    }
}
