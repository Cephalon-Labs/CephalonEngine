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
