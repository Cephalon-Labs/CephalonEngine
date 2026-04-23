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
                    expectedTaskCount: 1,
                    taskIds: ["0"]));
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
    public async Task AddDebeziumData_ManagedConnectorDryRunAndExecutionIntentCatalogsExposeSharedStateCategoryAndOperationFilters()
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

        var observeOnly = runtimeCatalog.GetById(ObserveOnlyRuntimeId);
        var futureControlPlane = runtimeCatalog.GetById(FutureControlPlaneRuntimeId);
        var outOfPolicy = runtimeCatalog.GetById(OutOfPolicyRuntimeId);
        var waiting = runtimeCatalog.GetById(WaitingRuntimeId);
        var blocked = runtimeCatalog.GetById(BlockedRuntimeId);
        var ready = runtimeCatalog.GetById(ReadyRuntimeId);
        var pauseRequired = runtimeCatalog.GetById(PauseRequiredRuntimeId);
        var pauseSatisfied = runtimeCatalog.GetById(PauseSatisfiedRuntimeId);

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

        Assert.NotNull(outOfPolicy);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked, outOfPolicy.ManagedConnectorDryRun.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None, outOfPolicy.ManagedConnectorDryRun.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorDryRun.CategoryIds);
        Assert.False(outOfPolicy.ManagedConnectorDryRun.WouldApplyChanges);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked, outOfPolicy.ManagedConnectorExecutionIntent.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None, outOfPolicy.ManagedConnectorExecutionIntent.OperationId);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.GovernanceOutOfPolicy, outOfPolicy.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.True(outOfPolicy.ManagedConnectorExecutionIntent.IsBlocked);

        Assert.NotNull(waiting);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked, waiting.ManagedConnectorDryRun.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.IncompleteReportingCoverage, waiting.ManagedConnectorDryRun.CategoryIds);
        Assert.False(waiting.ManagedConnectorDryRun.WouldApplyChanges);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked, waiting.ManagedConnectorExecutionIntent.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.IncompleteReportingCoverage, waiting.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.RuntimeTruthIncomplete, waiting.ManagedConnectorExecutionIntent.CategoryIds);

        Assert.NotNull(blocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked, blocked.ManagedConnectorDryRun.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.BlockingRemediation, blocked.ManagedConnectorDryRun.CategoryIds);
        Assert.True(blocked.ManagedConnectorDryRun.IsBlocked);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked, blocked.ManagedConnectorExecutionIntent.State);
        Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.BlockingRemediation, blocked.ManagedConnectorExecutionIntent.CategoryIds);
        Assert.True(blocked.ManagedConnectorExecutionIntent.IsBlocked);

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
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorDryRunState(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.WouldChange)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [FutureControlPlaneRuntimeId, PauseRequiredRuntimeId],
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
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionIntentState(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.RequiresApproval)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseSatisfiedRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentState(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.ReadyToExecute)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal([PauseRequiredRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionIntentCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ApprovalRequired)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal([FutureControlPlaneRuntimeId], runtimeCatalog
            .GetByManagedConnectorExecutionIntentCategory(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.OperatorOnly)
            .Select(static runtime => runtime.Id)
            .ToArray());
        Assert.Equal(
            [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId],
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
        Assert.Equal(
            [FutureControlPlaneRuntimeId, ReadyRuntimeId],
            runtimeCatalog
                .GetByManagedConnectorExecutionIntentOperationId(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile)
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray());
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
