using System.Globalization;
using System.Net.Http.Json;
using Cephalon.Abstractions.Data;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.Configuration;
using Cephalon.Data.Debezium.Configuration;
using Cephalon.Data.Debezium.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class DebeziumDataCdcHostingTests
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
    public async Task MapCephalonExposesDebeziumManagedConnectorReportingRouteWithoutBaseOptInFlag()
    {
        var builder = CreateBuilder(
            options =>
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
            },
            new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T05:01:30Z", CultureInfo.InvariantCulture)));

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();
            var response = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{RuntimeId}/reports",
                new[]
                {
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
                });
            response.EnsureSuccessStatusCode();

            var runtime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{RuntimeId}");
            var capture = await client.GetFromJsonAsync<CdcCaptureDescriptor>($"/engine/cdc-captures/{CaptureId}");
            var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>($"/engine/cdc-captures/runtime/{CaptureId}");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(runtime);
            Assert.Equal("external-managed", runtime.ExecutionOwnership);
            Assert.Equal("managed-connector", runtime.ExecutionTopology);
            Assert.Equal("connector-offset-commit", runtime.AcknowledgementMode);
            Assert.Equal([CaptureId], runtime.CdcCaptureIds);
            Assert.True(runtime.Summary.HasReports);
            Assert.Equal(CaptureId, runtime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, runtime.Summary.LastOutcome);
            Assert.Equal("debezium-report-001", runtime.Summary.LastReportId);
            Assert.Equal("connect-worker-a", runtime.Summary.LastReporterId);
            Assert.Equal(["edge-bkk-01"], runtime.Summary.ObservedEdgeNodeIds);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, runtime.ManagedConnectorGovernance.State);
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
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted, runtime.ManagedConnectorDrift.State);
            Assert.Equal("observe-only", runtime.ManagedConnectorDrift.ManagementMode);
            Assert.Equal("connect-cluster-a", runtime.ManagedConnectorDrift.DeclaredConnectClusterId);
            Assert.Equal("connect-cluster-b", runtime.ManagedConnectorDrift.ReportedConnectClusterId);
            Assert.Equal(["1"], runtime.ManagedConnectorDrift.MissingDeclaredTaskIds);
            Assert.Equal(["2"], runtime.ManagedConnectorDrift.UnexpectedReportedTaskIds);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingDeclaredTaskReports, runtime.ManagedConnectorDrift.CategoryIds);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.UnexpectedReportedTasks, runtime.ManagedConnectorDrift.CategoryIds);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectClusterMismatch, runtime.ManagedConnectorDrift.CategoryIds);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.InvestigateDrift, runtime.ManagedConnectorDrift.RecommendedActionId);
            Assert.True(runtime.ManagedConnectorDrift.RequiresAttention);
            Assert.True(runtime.ManagedConnectorDrift.CanEvaluateDrift);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired, runtime.ManagedConnectorActionPlan.State);
            Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift], runtime.ManagedConnectorActionPlan.ActionIds);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift, runtime.ManagedConnectorActionPlan.PrimaryActionId);
            Assert.Equal(CdcCaptureExecutionRuntimeRemediationStates.Ready, runtime.ManagedConnectorActionPlan.RemediationState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, runtime.ManagedConnectorActionPlan.GovernanceState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted, runtime.ManagedConnectorActionPlan.DriftState);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.DriftDetected, runtime.ManagedConnectorActionPlan.CategoryIds);
            Assert.True(runtime.ManagedConnectorActionPlan.RequiresAction);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady, runtime.ManagedConnectorWritePathReadiness.State);
            Assert.Equal("observe-only", runtime.ManagedConnectorWritePathReadiness.ManagementMode);
            Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported, runtime.ManagedConnectorWritePathReadiness.ReportingCoverageState);
            Assert.Equal(CdcCaptureExecutionRuntimeRemediationStates.Ready, runtime.ManagedConnectorWritePathReadiness.RemediationState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, runtime.ManagedConnectorWritePathReadiness.GovernanceState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted, runtime.ManagedConnectorWritePathReadiness.DriftState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired, runtime.ManagedConnectorWritePathReadiness.ActionPlanState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift, runtime.ManagedConnectorWritePathReadiness.PrimaryActionId);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.DriftDetected, runtime.ManagedConnectorWritePathReadiness.CategoryIds);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.ObserveOnlyMode, runtime.ManagedConnectorWritePathReadiness.CategoryIds);
            Assert.True(runtime.ManagedConnectorWritePathReadiness.RequiresAttention);

            Assert.NotNull(capture);
            Assert.Equal(CaptureId, capture.Id);
            Assert.Equal(DebeziumDataOptions.ProviderId, capture.Provider);
            Assert.Equal(RuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("external-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("managed-connector", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("inventory.public.customers", capture.Metadata["topicName"]);
            Assert.Equal("debezium-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal("observe-only", capture.Metadata["debeziumManagementMode"]);
            Assert.Equal("2", capture.Metadata["debeziumExpectedTaskCount"]);
            Assert.Equal("0,1", capture.Metadata["debeziumDeclaredTaskIds"]);

            Assert.NotNull(state);
            Assert.Equal(RuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.Equal("debezium-report-001", state.LastReportId);
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
            Assert.Equal("connect-cluster-a", state.Metadata["managedConnectorDeclaredConnectClusterId"]);
            Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", state.Metadata["managedConnectorDeclaredConnectorClass"]);
            Assert.Equal("postgresql", state.Metadata["managedConnectorDeclaredSourceProviderId"]);
            Assert.Equal("connect-cluster-b", state.Metadata["managedConnectorReportedConnectClusterId"]);
            Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", state.Metadata["managedConnectorReportedConnectorClass"]);
            Assert.Equal("postgresql", state.Metadata["managedConnectorReportedSourceProviderId"]);
            Assert.Equal("2", state.Metadata["managedConnectorExpectedTaskCount"]);
            Assert.Equal("0,1", state.Metadata["managedConnectorDeclaredTaskIds"]);
            Assert.Equal("0,2", state.Metadata["managedConnectorReportedTaskIds"]);
            Assert.Equal("0,2", state.Metadata["managedConnectorActiveTaskIds"]);
            Assert.Equal("task-mismatch", state.Metadata["managedConnectorTaskReconciliationState"]);
            Assert.Equal("task-mismatch", state.Metadata["managedConnectorReconciliationState"]);
            Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", state.Metadata["managedConnectorReconciliationReason"]);
            Assert.Equal("connect-cluster-a", state.Metadata["debeziumDeclaredConnectClusterId"]);
            Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", state.Metadata["debeziumDeclaredConnectorClass"]);
            Assert.Equal("postgresql", state.Metadata["debeziumDeclaredSourceProviderId"]);
            Assert.Equal("connect-cluster-b", state.Metadata["debeziumReportedConnectClusterId"]);
            Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", state.Metadata["debeziumReportedConnectorClass"]);
            Assert.Equal("postgresql", state.Metadata["debeziumReportedSourceProviderId"]);
            Assert.Equal("0,1", state.Metadata["debeziumDeclaredTaskIds"]);
            Assert.Equal("0,2", state.Metadata["debeziumReportedTaskIds"]);
            Assert.Equal("0,2", state.Metadata["debeziumActiveTaskIds"]);
            Assert.Equal("RUNNING:2", state.Metadata["debeziumTaskStateSummary"]);
            Assert.Equal("42", state.Metadata["debeziumConnectorGeneration"]);
            Assert.Equal("connect-worker-a-1", state.Metadata["debeziumWorkerId"]);

            Assert.Equal("observe-only", runtime.Metadata["managedConnectorManagementMode"]);
            Assert.Equal("2", runtime.Metadata["managedConnectorExpectedTaskCount"]);
            Assert.Equal("0,1", runtime.Metadata["managedConnectorDeclaredTaskIds"]);
            Assert.Equal("task-mismatch", runtime.Metadata["managedConnectorTaskReconciliationState"]);
            Assert.Equal("task-mismatch", runtime.Metadata["managedConnectorReconciliationState"]);
            Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", runtime.Metadata["managedConnectorReconciliationReason"]);
            Assert.Equal("0,2", runtime.Metadata["managedConnectorReportedTaskIds"]);
            Assert.Equal("0,2", runtime.Metadata["managedConnectorActiveTaskIds"]);
            Assert.Equal("connect-cluster-a", runtime.Metadata["managedConnectorDeclaredConnectClusterId"]);
            Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.Metadata["managedConnectorDeclaredConnectorClass"]);
            Assert.Equal("postgresql", runtime.Metadata["managedConnectorDeclaredSourceProviderId"]);
            Assert.Equal("connect-cluster-b", runtime.Metadata["managedConnectorReportedConnectClusterId"]);
            Assert.Equal("io.debezium.connector.postgresql.PostgresConnector", runtime.Metadata["managedConnectorReportedConnectorClass"]);
            Assert.Equal("postgresql", runtime.Metadata["managedConnectorReportedSourceProviderId"]);
            Assert.Equal("observe-only", runtime.Metadata["debeziumManagementMode"]);
            Assert.Equal("2", runtime.Metadata["debeziumExpectedTaskCount"]);
            Assert.Equal("0,1", runtime.Metadata["debeziumDeclaredTaskIds"]);
            Assert.Equal("running", runtime.Metadata["debeziumConnectorState"]);
            Assert.Equal("running", runtime.Metadata["debeziumConnectorLifecycleState"]);
            Assert.Equal("task-mismatch", runtime.Metadata["debeziumTaskReconciliationState"]);
            Assert.Equal("task-mismatch", runtime.Metadata["debeziumReconciliationState"]);
            Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", runtime.Metadata["debeziumReconciliationReason"]);
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

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptures, item => item.Id == CaptureId &&
                item.ExecutionBinding.EffectiveExecutionRuntimeId == RuntimeId);
            Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == CaptureId &&
                item.LastOutcome == CdcCaptureRuntimeOutcomes.Captured &&
                item.Metadata["cdcCaptureReporterId"] == "connect-worker-a" &&
                item.Metadata["debeziumReconciliationState"] == "task-mismatch");
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == RuntimeId &&
                item.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Captured &&
                item.Summary.LastReporterId == "connect-worker-a" &&
                item.ManagedConnectorGovernance.State == CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly &&
                item.ManagedConnectorDrift.State == CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted &&
                item.ManagedConnectorActionPlan.State == CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired &&
                item.ManagedConnectorWritePathReadiness.State == CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady &&
                item.ManagedConnectorGovernance.ReconciliationState == "task-mismatch" &&
                item.Metadata["debeziumReconciliationState"] == "task-mismatch");
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorGovernanceRoutesOnSharedCdcRuntimeSurface()
    {
        var builder = CreateBuilder(options =>
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

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();
            var observeOnly = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/governance/observe-only");
            var futureControlPlane = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/governance/future-control-plane");
            var outOfPolicy = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/governance/categories/missing-connect-cluster-id");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(observeOnly);
            Assert.Equal([ObserveOnlyRuntimeId], observeOnly.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, observeOnly[0].ManagedConnectorGovernance.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.KeepObserveOnly, observeOnly[0].ManagedConnectorGovernance.RecommendedActionId);

            Assert.NotNull(futureControlPlane);
            Assert.Equal([FutureControlPlaneRuntimeId], futureControlPlane.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.FutureControlPlane, futureControlPlane[0].ManagedConnectorGovernance.State);
            Assert.Equal("apply-and-reconcile", futureControlPlane[0].ManagedConnectorGovernance.ManagementMode);
            Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.FutureControlPlaneMode], futureControlPlane[0].ManagedConnectorGovernance.CategoryIds);

            Assert.NotNull(outOfPolicy);
            Assert.Equal([OutOfPolicyRuntimeId], outOfPolicy.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.OutOfPolicy, outOfPolicy[0].ManagedConnectorGovernance.State);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectClusterId, outOfPolicy[0].ManagedConnectorGovernance.CategoryIds);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.CompleteGovernanceDeclaration, outOfPolicy[0].ManagedConnectorGovernance.RecommendedActionId);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorGovernance.State == CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorGovernance.State == CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.FutureControlPlane &&
                item.ManagedConnectorGovernance.RequiresControlPlaneSupport);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OutOfPolicyRuntimeId &&
                item.ManagedConnectorGovernance.State == CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.OutOfPolicy &&
                item.ManagedConnectorGovernance.IsOutOfPolicy);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorDriftRoutesOnSharedCdcRuntimeSurface()
    {
        var builder = CreateBuilder(options =>
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

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();
            var inSyncResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ObserveOnlyRuntimeId}/reports",
                new[]
                {
                    new CdcCaptureRuntimeObservation(
                        cdcCaptureId: ObserveOnlyCaptureId,
                        outcome: CdcCaptureRuntimeOutcomes.Captured,
                        observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:00:00Z", CultureInfo.InvariantCulture),
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
                });
            inSyncResponse.EnsureSuccessStatusCode();

            var driftedResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{FutureControlPlaneRuntimeId}/reports",
                new[]
                {
                    new CdcCaptureRuntimeObservation(
                        cdcCaptureId: FutureControlPlaneCaptureId,
                        outcome: CdcCaptureRuntimeOutcomes.Captured,
                        observedAtUtc: DateTimeOffset.Parse("2026-04-23T06:05:00Z", CultureInfo.InvariantCulture),
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
                });
            driftedResponse.EnsureSuccessStatusCode();

            var inSync = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/drift/in-sync");
            var drifted = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/drift/drifted");
            var unknown = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/drift/unknown");
            var taskCountMismatch = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/drift/categories/task-count-mismatch");
            var reportUnavailable = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/drift/categories/reported-task-topology-unavailable");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(inSync);
            Assert.Equal([ObserveOnlyRuntimeId], inSync.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.InSync, inSync[0].ManagedConnectorDrift.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.None, inSync[0].ManagedConnectorDrift.RecommendedActionId);

            Assert.NotNull(drifted);
            Assert.Equal([FutureControlPlaneRuntimeId], drifted.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted, drifted[0].ManagedConnectorDrift.State);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.TaskCountMismatch, drifted[0].ManagedConnectorDrift.CategoryIds);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingDeclaredTaskReports, drifted[0].ManagedConnectorDrift.CategoryIds);

            Assert.NotNull(unknown);
            Assert.Equal([OutOfPolicyRuntimeId], unknown.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown, unknown[0].ManagedConnectorDrift.State);
            Assert.Equal([CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskTopologyUnavailable], unknown[0].ManagedConnectorDrift.CategoryIds);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.WaitForRuntimeReport, unknown[0].ManagedConnectorDrift.RecommendedActionId);

            Assert.NotNull(taskCountMismatch);
            Assert.Equal([FutureControlPlaneRuntimeId], taskCountMismatch.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(reportUnavailable);
            Assert.Equal([OutOfPolicyRuntimeId], reportUnavailable.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorDrift.State == CdcCaptureExecutionRuntimeManagedConnectorDriftStates.InSync);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorDrift.State == CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted &&
                item.ManagedConnectorDrift.RequiresAttention);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OutOfPolicyRuntimeId &&
                item.ManagedConnectorDrift.State == CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown &&
                !item.ManagedConnectorDrift.CanEvaluateDrift);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorActionPlanRoutesOnSharedCdcRuntimeSurface()
    {
        var builder = CreateBuilder(
            options =>
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
            },
            new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture)));

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();

            var observeOnlyReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ObserveOnlyRuntimeId}/reports",
                new[]
                {
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
                });
            observeOnlyReport.EnsureSuccessStatusCode();

            var futureControlPlaneReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{FutureControlPlaneRuntimeId}/reports",
                new[]
                {
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
                });
            futureControlPlaneReport.EnsureSuccessStatusCode();

            var blockedReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{BlockedRuntimeId}/reports",
                new[]
                {
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
                });
            blockedReport.EnsureSuccessStatusCode();

            var observe = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/action-plans/observe");
            var actionRequired = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/action-plans/action-required");
            var waiting = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/action-plans/waiting");
            var blocked = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/action-plans/blocked");
            var keepObserveOnly = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/actions/keep-observe-only");
            var completeGovernanceDeclaration = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/actions/complete-governance-declaration");
            var investigateDrift = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/actions/investigate-drift");
            var resolveRuntimeRemediation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/actions/resolve-runtime-remediation");
            var deferControlPlane = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/actions/defer-control-plane");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(observe);
            Assert.Equal([ObserveOnlyRuntimeId], observe.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Observe, observe[0].ManagedConnectorActionPlan.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly, observe[0].ManagedConnectorActionPlan.PrimaryActionId);

            Assert.NotNull(actionRequired);
            Assert.Equal([FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId], actionRequired.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired, actionRequired[0].ManagedConnectorActionPlan.State);

            Assert.NotNull(waiting);
            Assert.Equal([WaitingRuntimeId], waiting.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Waiting, waiting[0].ManagedConnectorActionPlan.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.WaitForRuntimeReport, waiting[0].ManagedConnectorActionPlan.PrimaryActionId);

            Assert.NotNull(blocked);
            Assert.Equal([BlockedRuntimeId], blocked.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Blocked, blocked[0].ManagedConnectorActionPlan.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.ResolveRuntimeRemediation, blocked[0].ManagedConnectorActionPlan.PrimaryActionId);

            Assert.NotNull(keepObserveOnly);
            Assert.Equal([ObserveOnlyRuntimeId], keepObserveOnly.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(completeGovernanceDeclaration);
            Assert.Equal([OutOfPolicyRuntimeId], completeGovernanceDeclaration.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(investigateDrift);
            Assert.Equal([FutureControlPlaneRuntimeId], investigateDrift.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(resolveRuntimeRemediation);
            Assert.Equal([BlockedRuntimeId], resolveRuntimeRemediation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(deferControlPlane);
            Assert.Equal([FutureControlPlaneRuntimeId], deferControlPlane.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorActionPlan.State == CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Observe);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorActionPlan.PrimaryActionId == CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift &&
                item.ManagedConnectorActionPlan.ActionIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.DeferControlPlane, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OutOfPolicyRuntimeId &&
                item.ManagedConnectorActionPlan.PrimaryActionId == CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.CompleteGovernanceDeclaration);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == WaitingRuntimeId &&
                item.ManagedConnectorActionPlan.State == CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Waiting);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == BlockedRuntimeId &&
                item.ManagedConnectorActionPlan.State == CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Blocked);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorWritePathReadinessRoutesOnSharedCdcRuntimeSurface()
    {
        var builder = CreateBuilder(
            options =>
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
            },
            new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture)));

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();

            var observeOnlyReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ObserveOnlyRuntimeId}/reports",
                new[]
                {
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
                });
            observeOnlyReport.EnsureSuccessStatusCode();

            var futureControlPlaneReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{FutureControlPlaneRuntimeId}/reports",
                new[]
                {
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
                });
            futureControlPlaneReport.EnsureSuccessStatusCode();

            var blockedReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{BlockedRuntimeId}/reports",
                new[]
                {
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
                });
            blockedReport.EnsureSuccessStatusCode();

            var readyReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ReadyRuntimeId}/reports",
                new[]
                {
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
                });
            readyReport.EnsureSuccessStatusCode();

            var deferred = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/deferred");
            var ready = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/ready");
            var notReady = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/not-ready");
            var blocked = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/blocked");
            var observeOnlyMode = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/categories/observe-only-mode");
            var writePathRequested = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/categories/write-path-requested");
            var writePathReady = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/categories/write-path-ready");
            var governanceOutOfPolicy = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/categories/governance-out-of-policy");
            var runtimeTruthIncomplete = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/categories/runtime-truth-incomplete");
            var blockingRemediation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/write-path-readiness/categories/blocking-remediation");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(deferred);
            Assert.Equal([ObserveOnlyRuntimeId], deferred.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Deferred, deferred[0].ManagedConnectorWritePathReadiness.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly, deferred[0].ManagedConnectorWritePathReadiness.PrimaryActionId);

            Assert.NotNull(ready);
            Assert.Equal([ReadyRuntimeId], ready.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Ready, ready[0].ManagedConnectorWritePathReadiness.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.DeferControlPlane, ready[0].ManagedConnectorWritePathReadiness.PrimaryActionId);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathReady, ready[0].ManagedConnectorWritePathReadiness.CategoryIds);

            Assert.NotNull(notReady);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                notReady.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(blocked);
            Assert.Equal([BlockedRuntimeId], blocked.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Blocked, blocked[0].ManagedConnectorWritePathReadiness.State);

            Assert.NotNull(observeOnlyMode);
            Assert.Equal(
                [BlockedRuntimeId, ObserveOnlyRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                observeOnlyMode.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(writePathRequested);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                writePathRequested.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(writePathReady);
            Assert.Equal([ReadyRuntimeId], writePathReady.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(governanceOutOfPolicy);
            Assert.Equal([OutOfPolicyRuntimeId], governanceOutOfPolicy.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(runtimeTruthIncomplete);
            Assert.Equal(
                [OutOfPolicyRuntimeId, WaitingRuntimeId],
                runtimeTruthIncomplete.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(blockingRemediation);
            Assert.Equal([BlockedRuntimeId], blockingRemediation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorWritePathReadiness.State == CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Deferred &&
                item.ManagedConnectorWritePathReadiness.IsDeferred);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorWritePathReadiness.State == CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady &&
                item.ManagedConnectorWritePathReadiness.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.DriftDetected, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OutOfPolicyRuntimeId &&
                item.ManagedConnectorWritePathReadiness.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.GovernanceOutOfPolicy, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == WaitingRuntimeId &&
                item.ManagedConnectorWritePathReadiness.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == BlockedRuntimeId &&
                item.ManagedConnectorWritePathReadiness.State == CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Blocked);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorWritePathReadiness.State == CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Ready &&
                item.ManagedConnectorWritePathReadiness.IsReady);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorPreflightRoutesOnSharedCdcRuntimeSurface()
    {
        var builder = CreateBuilder(
            options =>
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
            },
            new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture)));

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();

            var observeOnlyReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ObserveOnlyRuntimeId}/reports",
                new[]
                {
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
                });
            observeOnlyReport.EnsureSuccessStatusCode();

            var futureControlPlaneReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{FutureControlPlaneRuntimeId}/reports",
                new[]
                {
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
                });
            futureControlPlaneReport.EnsureSuccessStatusCode();

            var blockedReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{BlockedRuntimeId}/reports",
                new[]
                {
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
                });
            blockedReport.EnsureSuccessStatusCode();

            var readyReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ReadyRuntimeId}/reports",
                new[]
                {
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
                });
            readyReport.EnsureSuccessStatusCode();

            var deferred = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/deferred");
            var ready = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/ready");
            var notReady = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/not-ready");
            var blocked = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/blocked");
            var observeOnlyMode = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/categories/observe-only-mode");
            var reconcileIntent = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/categories/reconcile-intent");
            var preflightReady = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/categories/preflight-ready");
            var governanceOutOfPolicy = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/categories/governance-out-of-policy");
            var runtimeTruthIncomplete = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/categories/runtime-truth-incomplete");
            var blockingRemediation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/categories/blocking-remediation");
            var noOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/operations/none");
            var reconcile = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/preflight/operations/reconcile");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(deferred);
            Assert.Equal([ObserveOnlyRuntimeId], deferred.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Deferred, deferred[0].ManagedConnectorPreflight.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None, deferred[0].ManagedConnectorPreflight.OperationId);

            Assert.NotNull(ready);
            Assert.Equal([ReadyRuntimeId], ready.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Ready, ready[0].ManagedConnectorPreflight.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Reconcile, ready[0].ManagedConnectorPreflight.OperationId);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.PreflightReady, ready[0].ManagedConnectorPreflight.CategoryIds);

            Assert.NotNull(notReady);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                notReady.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(blocked);
            Assert.Equal([BlockedRuntimeId], blocked.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Blocked, blocked[0].ManagedConnectorPreflight.State);

            Assert.NotNull(observeOnlyMode);
            Assert.Equal(
                [BlockedRuntimeId, ObserveOnlyRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                observeOnlyMode.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(reconcileIntent);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                reconcileIntent.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(preflightReady);
            Assert.Equal([ReadyRuntimeId], preflightReady.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(governanceOutOfPolicy);
            Assert.Equal([OutOfPolicyRuntimeId], governanceOutOfPolicy.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(runtimeTruthIncomplete);
            Assert.Equal(
                [OutOfPolicyRuntimeId, WaitingRuntimeId],
                runtimeTruthIncomplete.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(blockingRemediation);
            Assert.Equal([BlockedRuntimeId], blockingRemediation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(noOperation);
            Assert.Equal(
                [BlockedRuntimeId, ObserveOnlyRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                noOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(reconcile);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                reconcile.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorPreflight.State == CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Deferred &&
                item.ManagedConnectorPreflight.IsDeferred);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorPreflight.State == CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady &&
                item.ManagedConnectorPreflight.OperationId == CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OutOfPolicyRuntimeId &&
                item.ManagedConnectorPreflight.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.GovernanceOutOfPolicy, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == WaitingRuntimeId &&
                item.ManagedConnectorPreflight.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == BlockedRuntimeId &&
                item.ManagedConnectorPreflight.State == CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Blocked);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorPreflight.State == CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Ready &&
                item.ManagedConnectorPreflight.IsReady);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorDryRunExecutionIntentApprovalCommandEnvelopeCommandIssuanceExecutionAdapterAndCommandExecutionRoutesOnSharedCdcRuntimeSurface()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture));
        var builder = CreateBuilder(
            options =>
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
            },
            timeProvider);

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();

            var observeOnlyReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ObserveOnlyRuntimeId}/reports",
                new[]
                {
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
                });
            observeOnlyReport.EnsureSuccessStatusCode();

            var futureControlPlaneReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{FutureControlPlaneRuntimeId}/reports",
                new[]
                {
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
                });
            futureControlPlaneReport.EnsureSuccessStatusCode();

            var blockedReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{BlockedRuntimeId}/reports",
                new[]
                {
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
                });
            blockedReport.EnsureSuccessStatusCode();

            var readyReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ReadyRuntimeId}/reports",
                new[]
                {
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
                });
            readyReport.EnsureSuccessStatusCode();

            var pauseRequiredReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{PauseRequiredRuntimeId}/reports",
                new[]
                {
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
                });
            pauseRequiredReport.EnsureSuccessStatusCode();

            var pauseSatisfiedReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{PauseSatisfiedRuntimeId}/reports",
                new[]
                {
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
                });
            pauseSatisfiedReport.EnsureSuccessStatusCode();

            var deleteRequiredReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{DeleteRequiredRuntimeId}/reports",
                new[]
                {
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
                });
            deleteRequiredReport.EnsureSuccessStatusCode();

            var deferred = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/deferred");
            var blocked = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/blocked");
            var noOp = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/no-op");
            var wouldChange = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/would-change");
            var changePlanned = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/categories/change-planned");
            var noChangesRequired = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/categories/no-changes-required");
            var taskTopologyChange = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/categories/task-topology-change");
            var pauseOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/operations/pause");
            var deleteOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/operations/delete");
            var reconcileOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/dry-runs/operations/reconcile");
            var deferredIntent = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/deferred");
            var blockedIntent = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/blocked");
            var operatorActionIntent = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/operator-action");
            var approvalRequiredIntent = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/requires-approval");
            var readyToExecuteIntent = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/ready-to-execute");
            var approvalRequiredCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/categories/approval-required");
            var operatorOnlyCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/categories/operator-only");
            var engineExecutionCandidateCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/categories/engine-execution-candidate");
            var noExecutionNeededCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/categories/no-execution-needed");
            var executionIntentPauseOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/operations/pause");
            var executionIntentDeleteOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/operations/delete");
            var executionIntentReconcileOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-intents/operations/reconcile");
            var notApplicableApproval = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/not-applicable");
            var autoBlockedApproval = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/auto-blocked");
            var policyBlockedApproval = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/policy-blocked");
            var approvalRequiredApproval = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/approval-required");
            var approvalReadyApproval = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/approval-ready");
            var autoEligibleApproval = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/auto-eligible");
            var observeOnlyApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/observe-only-mode");
            var blockingRemediationApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/blocking-remediation");
            var governanceApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/governance-out-of-policy");
            var controlPlaneApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/control-plane-ownership-gap");
            var destructiveApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/destructive-operation");
            var approvalRequiredApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/approval-required");
            var approvalReadyApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/approval-ready");
            var autoEligibleApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/auto-eligible");
            var noExecutionNeededApprovalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/categories/no-execution-needed");
            var approvalPauseOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/operations/pause");
            var approvalDeleteOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/operations/delete");
            var approvalReconcileOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-approvals/operations/reconcile");
            var notApplicableCommandEnvelope = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/not-applicable");
            var blockedCommandEnvelope = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/blocked");
            var operatorOnlyCommandEnvelope = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/operator-only");
            var approvalGatedCommandEnvelope = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/approval-gated");
            var engineReadyCommandEnvelope = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/engine-ready");
            var observeOnlyCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/observe-only-mode");
            var controlPlaneCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/control-plane-ownership-gap");
            var governanceCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/governance-out-of-policy");
            var blockingRemediationCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/blocking-remediation");
            var approvalGatedCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/approval-gated");
            var approvalReadyCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/approval-ready");
            var approvalRequiredCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/approval-required");
            var engineReadyCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/engine-ready");
            var destructiveCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/destructive-operation");
            var noExecutionNeededCommandEnvelopeCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/categories/no-execution-needed");
            var commandEnvelopePauseOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/operations/pause");
            var commandEnvelopeDeleteOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/operations/delete");
            var commandEnvelopeReconcileOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-envelopes/operations/reconcile");
            var notApplicableCommandIssuance = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/not-applicable");
            var blockedCommandIssuance = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/blocked");
            var operatorOnlyCommandIssuance = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/operator-only");
            var acceptedCommandIssuance = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/accepted");
            var rejectedCommandIssuance = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/rejected");
            var issuedCommandIssuance = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/issued");
            var observeOnlyCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/observe-only-mode");
            var controlPlaneCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/control-plane-ownership-gap");
            var operatorOnlyCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/operator-only");
            var governanceCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/governance-out-of-policy");
            var blockingRemediationCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/blocking-remediation");
            var changePlannedCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/change-planned");
            var approvalGatedCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/approval-gated");
            var approvalReadyCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/approval-ready");
            var approvalRequiredCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/approval-required");
            var acceptedCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/accepted");
            var rejectedCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/rejected");
            var noExecutionNeededCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/no-execution-needed");
            var issuedCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/issued");
            var destructiveCommandIssuanceCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/categories/destructive-operation");
            var commandIssuancePauseOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/operations/pause");
            var commandIssuanceDeleteOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/operations/delete");
            var commandIssuanceReconcileOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-issuances/operations/reconcile");
            var notApplicableExecutionAdapter = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/not-applicable");
            var blockedExecutionAdapter = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/blocked");
            var operatorOnlyExecutionAdapter = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/operator-only");
            var unavailableExecutionAdapter = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/unavailable");
            var readyExecutionAdapter = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/ready");
            var observeOnlyExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/observe-only-mode");
            var controlPlaneExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/control-plane-ownership-gap");
            var governanceExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/governance-out-of-policy");
            var blockingRemediationExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/blocking-remediation");
            var adapterReadyExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/adapter-ready");
            var adapterUnavailableExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/adapter-unavailable");
            var noExecutionNeededExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/no-execution-needed");
            var approvalReadyExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/approval-ready");
            var approvalRequiredExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/approval-required");
            var destructiveExecutionAdapterCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/categories/destructive-operation");
            var executionAdapterPauseOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/operations/pause");
            var executionAdapterDeleteOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/operations/delete");
            var executionAdapterReconcileOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/execution-adapters/operations/reconcile");
            var readyReconcileCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{ReadyRuntimeId}/commands/reconcile",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest());
            var pauseRequiredBlockedCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{PauseRequiredRuntimeId}/commands/pause",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest());
            var pauseRequiredApprovedCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{PauseRequiredRuntimeId}/commands/pause",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
                {
                    Approve = true
                });
            var deleteRequiredApprovalOnlyCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{DeleteRequiredRuntimeId}/commands/delete",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
                {
                    Approve = true
                });
            var deleteRequiredApprovedCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{DeleteRequiredRuntimeId}/commands/delete",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
                {
                    Approve = true,
                    AllowDestructive = true
                });
            var futureControlPlaneCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{FutureControlPlaneRuntimeId}/commands/reconcile",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest());
            var notApplicableCommandExecution = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/not-applicable");
            var unrecordedCommandExecution = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/unrecorded");
            var blockedCommandExecution = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/blocked");
            var noOpCommandExecution = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/no-op");
            var adaptedCommandExecution = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/adapted");
            var operatorOnlyCommandExecution = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/operator-only");
            var pauseCommandExecutionOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/operations/pause");
            var deleteCommandExecutionOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/operations/delete");
            var reconcileCommandExecutionOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-executions/operations/reconcile");
            var observeOnlyCommandExecutionHistory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[]>($"/engine/cdc-capture-runtimes/{ObserveOnlyRuntimeId}/command-executions");
            var readyCommandExecutionHistory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[]>($"/engine/cdc-capture-runtimes/{ReadyRuntimeId}/command-executions");
            var pauseRequiredCommandExecutionHistory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[]>($"/engine/cdc-capture-runtimes/{PauseRequiredRuntimeId}/command-executions");
            var deleteRequiredCommandExecutionHistory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[]>($"/engine/cdc-capture-runtimes/{DeleteRequiredRuntimeId}/command-executions");
            var futureControlPlaneCommandExecutionHistory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[]>($"/engine/cdc-capture-runtimes/{FutureControlPlaneRuntimeId}/command-executions");
            var notApplicableCommandRetry = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/not-applicable");
            var notNeededCommandRetry = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/not-needed");
            var cooldownCommandRetry = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/cooldown");
            var operatorOnlyCommandRetry = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/operator-only");
            var cooldownCommandRetryCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/categories/cooldown-active");
            var controlPlaneCommandRetryCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/categories/control-plane-ownership-gap");
            var pauseCommandRetryOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/operations/pause");
            var deleteCommandRetryOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/operations/delete");
            var notApplicableRetryExecutionPolicy = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/not-applicable");
            var notNeededRetryExecutionPolicy = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/not-needed");
            var cooldownRetryExecutionPolicy = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/cooldown");
            var operatorOnlyRetryExecutionPolicy = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/operator-only");
            var cooldownRetryExecutionPolicyCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/categories/cooldown-active");
            var controlPlaneRetryExecutionPolicyCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/categories/control-plane-ownership-gap");
            var pauseRetryExecutionPolicyOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/operations/pause");
            var deleteRetryExecutionPolicyOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/operations/delete");
            var notApplicableCommandJournal = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/not-applicable");
            var emptyCommandJournal = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/empty");
            var boundedCommandJournal = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/bounded");
            var cooldownCommandJournal = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/cooldown-active");
            var insufficientCommandJournal = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/insufficient-for-automation");
            var noRecordedCommandJournalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/categories/no-recorded-command");
            var controlPlaneCommandJournalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/categories/control-plane-ownership-gap");
            var readyCommandJournal = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus>($"/engine/cdc-capture-runtimes/{ReadyRuntimeId}/command-journal");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
            var readyReconcileCommand = await readyReconcileCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();
            var pauseRequiredBlockedCommand = await pauseRequiredBlockedCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();
            var pauseRequiredApprovedCommand = await pauseRequiredApprovedCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();
            var deleteRequiredApprovalOnlyCommand = await deleteRequiredApprovalOnlyCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();
            var deleteRequiredApprovedCommand = await deleteRequiredApprovedCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();
            var futureControlPlaneCommand = await futureControlPlaneCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();

            Assert.NotNull(deferred);
            Assert.Equal([ObserveOnlyRuntimeId], deferred.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred, deferred[0].ManagedConnectorDryRun.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None, deferred[0].ManagedConnectorDryRun.OperationId);

            Assert.NotNull(blocked);
            Assert.Equal(
                [BlockedRuntimeId, FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                blocked.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(noOp);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                noOp.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());
            Assert.Contains(noOp, runtime => runtime.Id == ReadyRuntimeId &&
                runtime.ManagedConnectorDryRun.OperationId == CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile);
            Assert.Contains(noOp, runtime => runtime.Id == PauseSatisfiedRuntimeId &&
                runtime.ManagedConnectorDryRun.OperationId == CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause);

            Assert.NotNull(wouldChange);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                wouldChange.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());
            Assert.Contains(wouldChange, runtime => runtime.Id == PauseRequiredRuntimeId &&
                runtime.ManagedConnectorDryRun.OperationId == CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause);
            Assert.Contains(wouldChange, runtime => runtime.Id == DeleteRequiredRuntimeId &&
                runtime.ManagedConnectorDryRun.OperationId == CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete);

            Assert.NotNull(changePlanned);
            Assert.Equal(
                [DeleteRequiredRuntimeId, FutureControlPlaneRuntimeId, PauseRequiredRuntimeId],
                changePlanned.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(noChangesRequired);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                noChangesRequired.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(taskTopologyChange);
            Assert.Equal([FutureControlPlaneRuntimeId], taskTopologyChange.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(pauseOperation);
            Assert.Equal(
                [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
                pauseOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(deleteOperation);
            Assert.Equal([DeleteRequiredRuntimeId], deleteOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(reconcileOperation);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                reconcileOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(deferredIntent);
            Assert.Equal([ObserveOnlyRuntimeId], deferredIntent.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Deferred, deferredIntent[0].ManagedConnectorExecutionIntent.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None, deferredIntent[0].ManagedConnectorExecutionIntent.OperationId);

            Assert.NotNull(blockedIntent);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                blockedIntent.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(operatorActionIntent);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorActionIntent.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile, operatorActionIntent[0].ManagedConnectorExecutionIntent.OperationId);

            Assert.NotNull(approvalRequiredIntent);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                approvalRequiredIntent.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());
            Assert.Contains(approvalRequiredIntent, runtime => runtime.Id == PauseRequiredRuntimeId &&
                runtime.ManagedConnectorExecutionIntent.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Pause);
            Assert.Contains(approvalRequiredIntent, runtime => runtime.Id == DeleteRequiredRuntimeId &&
                runtime.ManagedConnectorExecutionIntent.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Delete);

            Assert.NotNull(readyToExecuteIntent);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                readyToExecuteIntent.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());
            Assert.Contains(readyToExecuteIntent, runtime => runtime.Id == ReadyRuntimeId &&
                runtime.ManagedConnectorExecutionIntent.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile);
            Assert.Contains(readyToExecuteIntent, runtime => runtime.Id == PauseSatisfiedRuntimeId &&
                runtime.ManagedConnectorExecutionIntent.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Pause);

            Assert.NotNull(approvalRequiredCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                approvalRequiredCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(operatorOnlyCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorOnlyCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(engineExecutionCandidateCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId],
                engineExecutionCandidateCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(noExecutionNeededCategory);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                noExecutionNeededCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(executionIntentPauseOperation);
            Assert.Equal(
                [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
                executionIntentPauseOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(executionIntentDeleteOperation);
            Assert.Equal([DeleteRequiredRuntimeId], executionIntentDeleteOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(executionIntentReconcileOperation);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                executionIntentReconcileOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(notApplicableApproval);
            Assert.Equal([ObserveOnlyRuntimeId], notApplicableApproval.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable, notApplicableApproval[0].ManagedConnectorExecutionApproval.State);

            Assert.NotNull(autoBlockedApproval);
            Assert.Equal(
                [BlockedRuntimeId, WaitingRuntimeId],
                autoBlockedApproval.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(policyBlockedApproval);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, OutOfPolicyRuntimeId],
                policyBlockedApproval.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(approvalRequiredApproval);
            Assert.Equal([DeleteRequiredRuntimeId], approvalRequiredApproval.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Delete, approvalRequiredApproval[0].ManagedConnectorExecutionApproval.OperationId);

            Assert.NotNull(approvalReadyApproval);
            Assert.Equal([PauseRequiredRuntimeId], approvalReadyApproval.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Pause, approvalReadyApproval[0].ManagedConnectorExecutionApproval.OperationId);

            Assert.NotNull(autoEligibleApproval);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                autoEligibleApproval.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(observeOnlyApprovalCategory);
            Assert.Equal([ObserveOnlyRuntimeId], observeOnlyApprovalCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(blockingRemediationApprovalCategory);
            Assert.Equal([BlockedRuntimeId], blockingRemediationApprovalCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(governanceApprovalCategory);
            Assert.Equal([OutOfPolicyRuntimeId], governanceApprovalCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(controlPlaneApprovalCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], controlPlaneApprovalCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(destructiveApprovalCategory);
            Assert.Equal([DeleteRequiredRuntimeId], destructiveApprovalCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(approvalRequiredApprovalCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                approvalRequiredApprovalCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(approvalReadyApprovalCategory);
            Assert.Equal([PauseRequiredRuntimeId], approvalReadyApprovalCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(autoEligibleApprovalCategory);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                autoEligibleApprovalCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(noExecutionNeededApprovalCategory);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                noExecutionNeededApprovalCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(approvalPauseOperation);
            Assert.Equal(
                [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
                approvalPauseOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(approvalDeleteOperation);
            Assert.Equal([DeleteRequiredRuntimeId], approvalDeleteOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(approvalReconcileOperation);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                approvalReconcileOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(notApplicableCommandEnvelope);
            Assert.Equal([ObserveOnlyRuntimeId], notApplicableCommandEnvelope.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable, notApplicableCommandEnvelope[0].ManagedConnectorCommandEnvelope.State);

            Assert.NotNull(blockedCommandEnvelope);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                blockedCommandEnvelope.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(operatorOnlyCommandEnvelope);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorOnlyCommandEnvelope.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Reconcile, operatorOnlyCommandEnvelope[0].ManagedConnectorCommandEnvelope.OperationId);

            Assert.NotNull(approvalGatedCommandEnvelope);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                approvalGatedCommandEnvelope.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(engineReadyCommandEnvelope);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                engineReadyCommandEnvelope.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(observeOnlyCommandEnvelopeCategory);
            Assert.Equal([ObserveOnlyRuntimeId], observeOnlyCommandEnvelopeCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(controlPlaneCommandEnvelopeCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], controlPlaneCommandEnvelopeCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(governanceCommandEnvelopeCategory);
            Assert.Equal([OutOfPolicyRuntimeId], governanceCommandEnvelopeCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(blockingRemediationCommandEnvelopeCategory);
            Assert.Equal([BlockedRuntimeId], blockingRemediationCommandEnvelopeCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(approvalGatedCommandEnvelopeCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                approvalGatedCommandEnvelopeCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(approvalReadyCommandEnvelopeCategory);
            Assert.Equal([PauseRequiredRuntimeId], approvalReadyCommandEnvelopeCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(approvalRequiredCommandEnvelopeCategory);
            Assert.Equal([DeleteRequiredRuntimeId], approvalRequiredCommandEnvelopeCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(engineReadyCommandEnvelopeCategory);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                engineReadyCommandEnvelopeCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(destructiveCommandEnvelopeCategory);
            Assert.Equal([DeleteRequiredRuntimeId], destructiveCommandEnvelopeCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(noExecutionNeededCommandEnvelopeCategory);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                noExecutionNeededCommandEnvelopeCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(commandEnvelopePauseOperation);
            Assert.Equal(
                [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
                commandEnvelopePauseOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(commandEnvelopeDeleteOperation);
            Assert.Equal([DeleteRequiredRuntimeId], commandEnvelopeDeleteOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(commandEnvelopeReconcileOperation);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                commandEnvelopeReconcileOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(notApplicableCommandIssuance);
            Assert.Equal([ObserveOnlyRuntimeId], notApplicableCommandIssuance.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable, notApplicableCommandIssuance[0].ManagedConnectorCommandIssuance.State);

            Assert.NotNull(blockedCommandIssuance);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                blockedCommandIssuance.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(operatorOnlyCommandIssuance);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorOnlyCommandIssuance.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Reconcile, operatorOnlyCommandIssuance[0].ManagedConnectorCommandIssuance.OperationId);

            Assert.NotNull(acceptedCommandIssuance);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                acceptedCommandIssuance.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(rejectedCommandIssuance);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                rejectedCommandIssuance.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(issuedCommandIssuance);
            Assert.Empty(issuedCommandIssuance);

            Assert.NotNull(observeOnlyCommandIssuanceCategory);
            Assert.Equal([ObserveOnlyRuntimeId], observeOnlyCommandIssuanceCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(controlPlaneCommandIssuanceCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], controlPlaneCommandIssuanceCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(operatorOnlyCommandIssuanceCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorOnlyCommandIssuanceCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(governanceCommandIssuanceCategory);
            Assert.Equal([OutOfPolicyRuntimeId], governanceCommandIssuanceCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(blockingRemediationCommandIssuanceCategory);
            Assert.Equal([BlockedRuntimeId], blockingRemediationCommandIssuanceCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(changePlannedCommandIssuanceCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, FutureControlPlaneRuntimeId, PauseRequiredRuntimeId],
                changePlannedCommandIssuanceCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(approvalGatedCommandIssuanceCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                approvalGatedCommandIssuanceCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(approvalReadyCommandIssuanceCategory);
            Assert.Equal([PauseRequiredRuntimeId], approvalReadyCommandIssuanceCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(approvalRequiredCommandIssuanceCategory);
            Assert.Equal([DeleteRequiredRuntimeId], approvalRequiredCommandIssuanceCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(acceptedCommandIssuanceCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                acceptedCommandIssuanceCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(rejectedCommandIssuanceCategory);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                rejectedCommandIssuanceCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(noExecutionNeededCommandIssuanceCategory);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                noExecutionNeededCommandIssuanceCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(issuedCommandIssuanceCategory);
            Assert.Empty(issuedCommandIssuanceCategory);

            Assert.NotNull(destructiveCommandIssuanceCategory);
            Assert.Equal([DeleteRequiredRuntimeId], destructiveCommandIssuanceCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(commandIssuancePauseOperation);
            Assert.Equal(
                [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
                commandIssuancePauseOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(commandIssuanceDeleteOperation);
            Assert.Equal([DeleteRequiredRuntimeId], commandIssuanceDeleteOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(commandIssuanceReconcileOperation);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                commandIssuanceReconcileOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(notApplicableExecutionAdapter);
            Assert.Equal([ObserveOnlyRuntimeId], notApplicableExecutionAdapter.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable, notApplicableExecutionAdapter[0].ManagedConnectorExecutionAdapter.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest, notApplicableExecutionAdapter[0].ManagedConnectorExecutionAdapter.AdapterId);

            Assert.NotNull(blockedExecutionAdapter);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, WaitingRuntimeId],
                blockedExecutionAdapter.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(operatorOnlyExecutionAdapter);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorOnlyExecutionAdapter.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, operatorOnlyExecutionAdapter[0].ManagedConnectorExecutionAdapter.OperationId);

            Assert.NotNull(unavailableExecutionAdapter);
            Assert.Empty(unavailableExecutionAdapter);

            Assert.NotNull(readyExecutionAdapter);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId],
                readyExecutionAdapter.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(observeOnlyExecutionAdapterCategory);
            Assert.Equal([ObserveOnlyRuntimeId], observeOnlyExecutionAdapterCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(controlPlaneExecutionAdapterCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], controlPlaneExecutionAdapterCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(governanceExecutionAdapterCategory);
            Assert.Equal([OutOfPolicyRuntimeId], governanceExecutionAdapterCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(blockingRemediationExecutionAdapterCategory);
            Assert.Equal([BlockedRuntimeId], blockingRemediationExecutionAdapterCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(adapterReadyExecutionAdapterCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId],
                adapterReadyExecutionAdapterCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(adapterUnavailableExecutionAdapterCategory);
            Assert.Empty(adapterUnavailableExecutionAdapterCategory);

            Assert.NotNull(noExecutionNeededExecutionAdapterCategory);
            Assert.Equal(
                [PauseSatisfiedRuntimeId, ReadyRuntimeId],
                noExecutionNeededExecutionAdapterCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(approvalReadyExecutionAdapterCategory);
            Assert.Equal([PauseRequiredRuntimeId], approvalReadyExecutionAdapterCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(approvalRequiredExecutionAdapterCategory);
            Assert.Equal([DeleteRequiredRuntimeId], approvalRequiredExecutionAdapterCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(destructiveExecutionAdapterCategory);
            Assert.Equal([DeleteRequiredRuntimeId], destructiveExecutionAdapterCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(executionAdapterPauseOperation);
            Assert.Equal(
                [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
                executionAdapterPauseOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(executionAdapterDeleteOperation);
            Assert.Equal([DeleteRequiredRuntimeId], executionAdapterDeleteOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(executionAdapterReconcileOperation);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                executionAdapterReconcileOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            readyReconcileCommandResponse.EnsureSuccessStatusCode();
            Assert.NotNull(readyReconcileCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, readyReconcileCommand.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, readyReconcileCommand.ExecutionAdapterState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, readyReconcileCommand.ResolvedOperationId);
            Assert.True(readyReconcileCommand.IsNoOp);
            Assert.True(readyReconcileCommand.HasRecordedOutcome);
            Assert.False(string.IsNullOrWhiteSpace(readyReconcileCommand.AttemptId));
            Assert.NotNull(readyReconcileCommand.RecordedAtUtc);
            Assert.False(readyReconcileCommand.HasProviderCommand);

            pauseRequiredBlockedCommandResponse.EnsureSuccessStatusCode();
            Assert.NotNull(pauseRequiredBlockedCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, pauseRequiredBlockedCommand.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, pauseRequiredBlockedCommand.ExecutionAdapterState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause, pauseRequiredBlockedCommand.ResolvedOperationId);
            Assert.True(pauseRequiredBlockedCommand.IsBlocked);
            Assert.True(pauseRequiredBlockedCommand.HasRecordedOutcome);
            Assert.False(string.IsNullOrWhiteSpace(pauseRequiredBlockedCommand.AttemptId));
            Assert.NotNull(pauseRequiredBlockedCommand.RecordedAtUtc);
            Assert.True(pauseRequiredBlockedCommand.RequiresExplicitApproval);
            Assert.False(pauseRequiredBlockedCommand.HasProviderCommand);

            pauseRequiredApprovedCommandResponse.EnsureSuccessStatusCode();
            Assert.NotNull(pauseRequiredApprovedCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, pauseRequiredApprovedCommand.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, pauseRequiredApprovedCommand.ExecutionAdapterState);
            Assert.Equal("http-rest", pauseRequiredApprovedCommand.TransportKind);
            Assert.Equal("PUT", pauseRequiredApprovedCommand.HttpMethod);
            Assert.Equal($"/connectors/{PauseRequiredRuntimeId}/pause", pauseRequiredApprovedCommand.RelativePath);
            Assert.True(pauseRequiredApprovedCommand.HasRecordedOutcome);
            Assert.False(string.IsNullOrWhiteSpace(pauseRequiredApprovedCommand.AttemptId));
            Assert.NotNull(pauseRequiredApprovedCommand.RecordedAtUtc);
            Assert.True(pauseRequiredApprovedCommand.HasProviderCommand);
            Assert.True(pauseRequiredApprovedCommand.IsAdapted);

            deleteRequiredApprovalOnlyCommandResponse.EnsureSuccessStatusCode();
            Assert.NotNull(deleteRequiredApprovalOnlyCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, deleteRequiredApprovalOnlyCommand.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete, deleteRequiredApprovalOnlyCommand.ResolvedOperationId);
            Assert.True(deleteRequiredApprovalOnlyCommand.IsBlocked);
            Assert.True(deleteRequiredApprovalOnlyCommand.HasRecordedOutcome);
            Assert.False(string.IsNullOrWhiteSpace(deleteRequiredApprovalOnlyCommand.AttemptId));
            Assert.NotNull(deleteRequiredApprovalOnlyCommand.RecordedAtUtc);
            Assert.True(deleteRequiredApprovalOnlyCommand.IsDestructiveOperation);
            Assert.False(deleteRequiredApprovalOnlyCommand.HasProviderCommand);

            deleteRequiredApprovedCommandResponse.EnsureSuccessStatusCode();
            Assert.NotNull(deleteRequiredApprovedCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, deleteRequiredApprovedCommand.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, deleteRequiredApprovedCommand.ExecutionAdapterState);
            Assert.Equal("http-rest", deleteRequiredApprovedCommand.TransportKind);
            Assert.Equal("DELETE", deleteRequiredApprovedCommand.HttpMethod);
            Assert.Equal($"/connectors/{DeleteRequiredRuntimeId}", deleteRequiredApprovedCommand.RelativePath);
            Assert.True(deleteRequiredApprovedCommand.HasRecordedOutcome);
            Assert.False(string.IsNullOrWhiteSpace(deleteRequiredApprovedCommand.AttemptId));
            Assert.NotNull(deleteRequiredApprovedCommand.RecordedAtUtc);
            Assert.True(deleteRequiredApprovedCommand.IsAdapted);
            Assert.True(deleteRequiredApprovedCommand.IsDestructiveOperation);
            Assert.True(deleteRequiredApprovedCommand.HasProviderCommand);

            futureControlPlaneCommandResponse.EnsureSuccessStatusCode();
            Assert.NotNull(futureControlPlaneCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly, futureControlPlaneCommand.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.OperatorOnly, futureControlPlaneCommand.ExecutionAdapterState);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile, futureControlPlaneCommand.ResolvedOperationId);
            Assert.True(futureControlPlaneCommand.IsOperatorOnly);
            Assert.True(futureControlPlaneCommand.HasRecordedOutcome);
            Assert.False(string.IsNullOrWhiteSpace(futureControlPlaneCommand.AttemptId));
            Assert.NotNull(futureControlPlaneCommand.RecordedAtUtc);
            Assert.False(futureControlPlaneCommand.HasProviderCommand);

            Assert.NotNull(notApplicableCommandExecution);
            Assert.Equal([ObserveOnlyRuntimeId], notApplicableCommandExecution.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(unrecordedCommandExecution);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, WaitingRuntimeId],
                unrecordedCommandExecution.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(blockedCommandExecution);
            Assert.Empty(blockedCommandExecution);

            Assert.NotNull(noOpCommandExecution);
            Assert.Equal([ReadyRuntimeId], noOpCommandExecution.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(adaptedCommandExecution);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                adaptedCommandExecution.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(operatorOnlyCommandExecution);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorOnlyCommandExecution.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(pauseCommandExecutionOperation);
            Assert.Equal([PauseRequiredRuntimeId], pauseCommandExecutionOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(deleteCommandExecutionOperation);
            Assert.Equal([DeleteRequiredRuntimeId], deleteCommandExecutionOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(reconcileCommandExecutionOperation);
            Assert.Equal(
                [FutureControlPlaneRuntimeId, ReadyRuntimeId],
                reconcileCommandExecutionOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(observeOnlyCommandExecutionHistory);
            Assert.Empty(observeOnlyCommandExecutionHistory);

            Assert.NotNull(readyCommandExecutionHistory);
            Assert.Single(readyCommandExecutionHistory);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, readyCommandExecutionHistory[0].State);
            Assert.Equal(readyReconcileCommand.AttemptId, readyCommandExecutionHistory[0].AttemptId);

            Assert.NotNull(pauseRequiredCommandExecutionHistory);
            Assert.Equal(2, pauseRequiredCommandExecutionHistory.Length);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, pauseRequiredCommandExecutionHistory[0].State);
            Assert.Equal(pauseRequiredApprovedCommand.AttemptId, pauseRequiredCommandExecutionHistory[0].AttemptId);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, pauseRequiredCommandExecutionHistory[1].State);
            Assert.Equal(pauseRequiredBlockedCommand.AttemptId, pauseRequiredCommandExecutionHistory[1].AttemptId);

            Assert.NotNull(deleteRequiredCommandExecutionHistory);
            Assert.Equal(2, deleteRequiredCommandExecutionHistory.Length);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, deleteRequiredCommandExecutionHistory[0].State);
            Assert.Equal(deleteRequiredApprovedCommand.AttemptId, deleteRequiredCommandExecutionHistory[0].AttemptId);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, deleteRequiredCommandExecutionHistory[1].State);
            Assert.Equal(deleteRequiredApprovalOnlyCommand.AttemptId, deleteRequiredCommandExecutionHistory[1].AttemptId);

            Assert.NotNull(futureControlPlaneCommandExecutionHistory);
            Assert.Single(futureControlPlaneCommandExecutionHistory);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly, futureControlPlaneCommandExecutionHistory[0].State);
            Assert.Equal(futureControlPlaneCommand.AttemptId, futureControlPlaneCommandExecutionHistory[0].AttemptId);

            Assert.NotNull(notApplicableCommandRetry);
            Assert.Equal([ObserveOnlyRuntimeId], notApplicableCommandRetry.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable, notApplicableCommandRetry[0].ManagedConnectorCommandRetry.State);

            Assert.NotNull(notNeededCommandRetry);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId, WaitingRuntimeId],
                notNeededCommandRetry.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(cooldownCommandRetry);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                cooldownCommandRetry.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());
            Assert.Contains(cooldownCommandRetry, runtime => runtime.Id == PauseRequiredRuntimeId &&
                runtime.ManagedConnectorCommandRetry.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Pause);
            Assert.Contains(cooldownCommandRetry, runtime => runtime.Id == DeleteRequiredRuntimeId &&
                runtime.ManagedConnectorCommandRetry.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Delete &&
                runtime.ManagedConnectorCommandRetry.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DestructiveOperation, StringComparer.OrdinalIgnoreCase));

            Assert.NotNull(operatorOnlyCommandRetry);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorOnlyCommandRetry.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Reconcile, operatorOnlyCommandRetry[0].ManagedConnectorCommandRetry.OperationId);

            Assert.NotNull(cooldownCommandRetryCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                cooldownCommandRetryCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(controlPlaneCommandRetryCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], controlPlaneCommandRetryCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(pauseCommandRetryOperation);
            Assert.Equal(
                [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
                pauseCommandRetryOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(deleteCommandRetryOperation);
            Assert.Equal([DeleteRequiredRuntimeId], deleteCommandRetryOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(notApplicableRetryExecutionPolicy);
            Assert.Equal([ObserveOnlyRuntimeId], notApplicableRetryExecutionPolicy.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable, notApplicableRetryExecutionPolicy[0].ManagedConnectorRetryExecutionPolicy.State);

            Assert.NotNull(notNeededRetryExecutionPolicy);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, ReadyRuntimeId, WaitingRuntimeId],
                notNeededRetryExecutionPolicy.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(cooldownRetryExecutionPolicy);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                cooldownRetryExecutionPolicy.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());
            Assert.Contains(cooldownRetryExecutionPolicy, runtime => runtime.Id == PauseRequiredRuntimeId &&
                runtime.ManagedConnectorRetryExecutionPolicy.OperationId == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Pause);
            Assert.Contains(cooldownRetryExecutionPolicy, runtime => runtime.Id == DeleteRequiredRuntimeId &&
                runtime.ManagedConnectorRetryExecutionPolicy.OperationId == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Delete &&
                runtime.ManagedConnectorRetryExecutionPolicy.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DestructiveOperation, StringComparer.OrdinalIgnoreCase));

            Assert.NotNull(operatorOnlyRetryExecutionPolicy);
            Assert.Equal([FutureControlPlaneRuntimeId], operatorOnlyRetryExecutionPolicy.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Reconcile, operatorOnlyRetryExecutionPolicy[0].ManagedConnectorRetryExecutionPolicy.OperationId);

            Assert.NotNull(cooldownRetryExecutionPolicyCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                cooldownRetryExecutionPolicyCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(controlPlaneRetryExecutionPolicyCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], controlPlaneRetryExecutionPolicyCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(pauseRetryExecutionPolicyOperation);
            Assert.Equal(
                [PauseRequiredRuntimeId, PauseSatisfiedRuntimeId],
                pauseRetryExecutionPolicyOperation.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(deleteRetryExecutionPolicyOperation);
            Assert.Equal([DeleteRequiredRuntimeId], deleteRetryExecutionPolicyOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(notApplicableCommandJournal);
            Assert.Equal([ObserveOnlyRuntimeId], notApplicableCommandJournal.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable, notApplicableCommandJournal[0].ManagedConnectorCommandJournal.State);

            Assert.NotNull(emptyCommandJournal);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, WaitingRuntimeId],
                emptyCommandJournal.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(boundedCommandJournal);
            Assert.Equal([ReadyRuntimeId], boundedCommandJournal.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Bounded, boundedCommandJournal[0].ManagedConnectorCommandJournal.State);

            Assert.NotNull(cooldownCommandJournal);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                cooldownCommandJournal.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());
            Assert.Contains(cooldownCommandJournal, runtime => runtime.Id == PauseRequiredRuntimeId &&
                runtime.ManagedConnectorCommandJournal.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Pause);
            Assert.Contains(cooldownCommandJournal, runtime => runtime.Id == DeleteRequiredRuntimeId &&
                runtime.ManagedConnectorCommandJournal.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Delete);

            Assert.NotNull(insufficientCommandJournal);
            Assert.Equal([FutureControlPlaneRuntimeId], insufficientCommandJournal.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.InsufficientForAutomation, insufficientCommandJournal[0].ManagedConnectorCommandJournal.State);

            Assert.NotNull(noRecordedCommandJournalCategory);
            Assert.Equal(
                [BlockedRuntimeId, OutOfPolicyRuntimeId, PauseSatisfiedRuntimeId, WaitingRuntimeId],
                noRecordedCommandJournalCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(controlPlaneCommandJournalCategory);
            Assert.Equal([FutureControlPlaneRuntimeId], controlPlaneCommandJournalCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(readyCommandJournal);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Bounded, readyCommandJournal.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Reconcile, readyCommandJournal.OperationId);
            Assert.Equal(1, readyCommandJournal.TotalRecordedEntryCount);
            Assert.Equal(1, readyCommandJournal.RetainedEntryCount);
            Assert.Equal(20, readyCommandJournal.MaximumRetainedEntryCount);
            Assert.True(readyCommandJournal.IsBounded);
            Assert.True(readyCommandJournal.HasRecordedCommandHistory);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.BoundedRetention, readyCommandJournal.CategoryIds);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorDryRun.State == CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred &&
                item.ManagedConnectorDryRun.IsDeferred);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorDryRun.State == CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked &&
                item.ManagedConnectorDryRun.WouldApplyChanges &&
                item.ManagedConnectorDryRun.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.TaskTopologyChange, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorDryRun.State == CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NoOp &&
                item.ManagedConnectorDryRun.OperationId == CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorDryRun.State == CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.WouldChange &&
                item.ManagedConnectorDryRun.OperationId == CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorDryRun.State == CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.WouldChange &&
                item.ManagedConnectorDryRun.OperationId == CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseSatisfiedRuntimeId &&
                item.ManagedConnectorDryRun.State == CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NoOp &&
                item.ManagedConnectorDryRun.OperationId == CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorExecutionIntent.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Deferred &&
                item.ManagedConnectorExecutionIntent.IsDeferred);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorExecutionIntent.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.OperatorAction &&
                item.ManagedConnectorExecutionIntent.IsOperatorAction &&
                item.ManagedConnectorExecutionIntent.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.OperatorOnly, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorExecutionIntent.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.ReadyToExecute &&
                item.ManagedConnectorExecutionIntent.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorExecutionIntent.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.RequiresApproval &&
                item.ManagedConnectorExecutionIntent.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorExecutionIntent.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.RequiresApproval &&
                item.ManagedConnectorExecutionIntent.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Delete);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseSatisfiedRuntimeId &&
                item.ManagedConnectorExecutionIntent.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.ReadyToExecute &&
                item.ManagedConnectorExecutionIntent.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorExecutionApproval.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable &&
                item.ManagedConnectorExecutionApproval.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ObserveOnlyMode, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorExecutionApproval.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.PolicyBlocked &&
                item.ManagedConnectorExecutionApproval.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorExecutionApproval.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoEligible &&
                item.ManagedConnectorExecutionApproval.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorExecutionApproval.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalReady &&
                item.ManagedConnectorExecutionApproval.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorExecutionApproval.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalRequired &&
                item.ManagedConnectorExecutionApproval.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Delete);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseSatisfiedRuntimeId &&
                item.ManagedConnectorExecutionApproval.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoEligible &&
                item.ManagedConnectorExecutionApproval.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorCommandEnvelope.State == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable &&
                item.ManagedConnectorCommandEnvelope.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ObserveOnlyMode, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorCommandEnvelope.State == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.OperatorOnly &&
                item.ManagedConnectorCommandEnvelope.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OutOfPolicyRuntimeId &&
                item.ManagedConnectorCommandEnvelope.State == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked &&
                item.ManagedConnectorCommandEnvelope.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.GovernanceOutOfPolicy, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == BlockedRuntimeId &&
                item.ManagedConnectorCommandEnvelope.State == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked &&
                item.ManagedConnectorCommandEnvelope.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.BlockingRemediation, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorCommandEnvelope.State == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.EngineReady &&
                item.ManagedConnectorCommandEnvelope.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Reconcile &&
                item.ManagedConnectorCommandEnvelope.ConnectClusterId == "connect-cluster-e");
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorCommandEnvelope.State == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.ApprovalGated &&
                item.ManagedConnectorCommandEnvelope.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorCommandEnvelope.State == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.ApprovalGated &&
                item.ManagedConnectorCommandEnvelope.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Delete &&
                item.ManagedConnectorCommandEnvelope.IsDestructiveOperation);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseSatisfiedRuntimeId &&
                item.ManagedConnectorCommandEnvelope.State == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.EngineReady &&
                item.ManagedConnectorCommandEnvelope.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorCommandIssuance.State == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable &&
                item.ManagedConnectorCommandIssuance.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ObserveOnlyMode, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorCommandIssuance.State == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.OperatorOnly &&
                item.ManagedConnectorCommandIssuance.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OutOfPolicyRuntimeId &&
                item.ManagedConnectorCommandIssuance.State == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked &&
                item.ManagedConnectorCommandIssuance.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.GovernanceOutOfPolicy, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == BlockedRuntimeId &&
                item.ManagedConnectorCommandIssuance.State == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked &&
                item.ManagedConnectorCommandIssuance.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.BlockingRemediation, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorCommandIssuance.State == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Rejected &&
                item.ManagedConnectorCommandIssuance.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Reconcile &&
                item.ManagedConnectorCommandIssuance.ConnectClusterId == "connect-cluster-e");
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorCommandIssuance.State == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Accepted &&
                item.ManagedConnectorCommandIssuance.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorCommandIssuance.State == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Accepted &&
                item.ManagedConnectorCommandIssuance.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Delete &&
                item.ManagedConnectorCommandIssuance.IsDestructiveOperation);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseSatisfiedRuntimeId &&
                item.ManagedConnectorCommandIssuance.State == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Rejected &&
                item.ManagedConnectorCommandIssuance.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorExecutionAdapter.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable &&
                item.ManagedConnectorExecutionAdapter.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ObserveOnlyMode, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorExecutionAdapter.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.OperatorOnly &&
                item.ManagedConnectorExecutionAdapter.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OutOfPolicyRuntimeId &&
                item.ManagedConnectorExecutionAdapter.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Blocked &&
                item.ManagedConnectorExecutionAdapter.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.GovernanceOutOfPolicy, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == BlockedRuntimeId &&
                item.ManagedConnectorExecutionAdapter.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Blocked &&
                item.ManagedConnectorExecutionAdapter.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.BlockingRemediation, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorExecutionAdapter.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready &&
                item.ManagedConnectorExecutionAdapter.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile &&
                item.ManagedConnectorExecutionAdapter.ConnectClusterId == "connect-cluster-e");
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorExecutionAdapter.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready &&
                item.ManagedConnectorExecutionAdapter.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorExecutionAdapter.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready &&
                item.ManagedConnectorExecutionAdapter.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete &&
                item.ManagedConnectorExecutionAdapter.IsDestructiveOperation);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseSatisfiedRuntimeId &&
                item.ManagedConnectorExecutionAdapter.State == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready &&
                item.ManagedConnectorExecutionAdapter.OperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause &&
                item.ManagedConnectorExecutionAdapter.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.NoExecutionNeeded, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorCommandExecution.State == CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable &&
                !item.ManagedConnectorCommandExecution.IsUnrecorded);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorCommandExecution.State == CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp &&
                item.ManagedConnectorCommandExecution.AttemptId == readyReconcileCommand.AttemptId &&
                item.ManagedConnectorCommandExecution.HasRecordedOutcome);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorCommandExecution.State == CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted &&
                item.ManagedConnectorCommandExecution.AttemptId == pauseRequiredApprovedCommand.AttemptId &&
                item.ManagedConnectorCommandExecution.RequestedOperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorCommandExecution.State == CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted &&
                item.ManagedConnectorCommandExecution.AttemptId == deleteRequiredApprovedCommand.AttemptId &&
                item.ManagedConnectorCommandExecution.RequestedOperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorCommandExecution.State == CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly &&
                item.ManagedConnectorCommandExecution.AttemptId == futureControlPlaneCommand.AttemptId &&
                item.ManagedConnectorCommandExecution.RequestedOperationId == CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseSatisfiedRuntimeId &&
                item.ManagedConnectorCommandExecution.State == CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded &&
                !item.ManagedConnectorCommandExecution.HasRecordedOutcome);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded &&
                !item.ManagedConnectorRetryExecutionPolicy.CanExecuteRetryThroughPolicy);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown &&
                item.ManagedConnectorRetryExecutionPolicy.OperationId == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown &&
                item.ManagedConnectorRetryExecutionPolicy.OperationId == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Delete);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.OperatorOnly &&
                item.ManagedConnectorRetryExecutionPolicy.OperationId == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Reconcile);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ObserveOnlyRuntimeId &&
                item.ManagedConnectorCommandJournal.State == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable &&
                item.ManagedConnectorCommandJournal.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ObserveOnlyMode, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == ReadyRuntimeId &&
                item.ManagedConnectorCommandJournal.State == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Bounded &&
                item.ManagedConnectorCommandJournal.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Reconcile &&
                item.ManagedConnectorCommandJournal.TotalRecordedEntryCount == 1);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorCommandJournal.State == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive &&
                item.ManagedConnectorCommandJournal.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Pause);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorCommandJournal.State == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive &&
                item.ManagedConnectorCommandJournal.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Delete);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == FutureControlPlaneRuntimeId &&
                item.ManagedConnectorCommandJournal.State == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.InsufficientForAutomation &&
                item.ManagedConnectorCommandJournal.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ControlPlaneOwnershipGap, StringComparer.OrdinalIgnoreCase));

            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-23T06:11:10Z", CultureInfo.InvariantCulture));
            var duplicateCommandRetry = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/duplicate");
            var duplicateCommandRetryCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-retries/categories/duplicate-command");
            var duplicateRetryExecutionPolicyCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/categories/duplicate-command");
            var duplicateCommandJournal = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/duplicate-evidence-present");
            var duplicateCommandJournalCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/categories/duplicate-command");
            var snapshotAfterRetryCooldown = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(duplicateCommandRetry);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                duplicateCommandRetry.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(duplicateCommandRetryCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                duplicateCommandRetryCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(duplicateRetryExecutionPolicyCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                duplicateRetryExecutionPolicyCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(duplicateCommandJournal);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                duplicateCommandJournal.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(duplicateCommandJournalCategory);
            Assert.Equal(
                [DeleteRequiredRuntimeId, PauseRequiredRuntimeId],
                duplicateCommandJournalCategory.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            Assert.NotNull(snapshotAfterRetryCooldown);
            Assert.Contains(snapshotAfterRetryCooldown.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorCommandRetry.State == CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Duplicate &&
                item.ManagedConnectorCommandRetry.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Pause);
            Assert.Contains(snapshotAfterRetryCooldown.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorCommandRetry.State == CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Duplicate &&
                item.ManagedConnectorCommandRetry.OperationId == CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Delete);
            Assert.Contains(snapshotAfterRetryCooldown.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded &&
                item.ManagedConnectorRetryExecutionPolicy.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DuplicateCommand, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshotAfterRetryCooldown.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded &&
                item.ManagedConnectorRetryExecutionPolicy.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DuplicateCommand, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshotAfterRetryCooldown.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorCommandJournal.State == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent &&
                item.ManagedConnectorCommandJournal.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.DuplicateCommand, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshotAfterRetryCooldown.CdcCaptureExecutionRuntimes, item => item.Id == DeleteRequiredRuntimeId &&
                item.ManagedConnectorCommandJournal.State == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent &&
                item.ManagedConnectorCommandJournal.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.DuplicateCommand, StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorRetryExecutionPolicyRoutesForManualApprovalAndPolicyBlockedPostCooldown()
    {
        const string policyBlockedRuntimeId = "inventory-policy-blocked-connector";
        const string policyBlockedCaptureId = "inventory-policy-blocked-cdc";

        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T06:10:30Z", CultureInfo.InvariantCulture));
        var builder = CreateBuilder(
            options =>
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
            },
            timeProvider);

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();

            var policyBlockedReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{policyBlockedRuntimeId}/reports",
                new[]
                {
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
                });
            policyBlockedReport.EnsureSuccessStatusCode();

            var pauseRequiredReport = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{PauseRequiredRuntimeId}/reports",
                new[]
                {
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
                });
            pauseRequiredReport.EnsureSuccessStatusCode();

            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-23T06:10:31Z", CultureInfo.InvariantCulture));
            var policyBlockedCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{policyBlockedRuntimeId}/commands/reconcile",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest());

            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-23T06:10:32Z", CultureInfo.InvariantCulture));
            var pauseBlockedCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{PauseRequiredRuntimeId}/commands/pause",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest());

            policyBlockedCommandResponse.EnsureSuccessStatusCode();
            pauseBlockedCommandResponse.EnsureSuccessStatusCode();

            var policyBlockedCommand = await policyBlockedCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();
            var pauseBlockedCommand = await pauseBlockedCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();

            Assert.NotNull(policyBlockedCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, policyBlockedCommand.State);
            Assert.NotNull(pauseBlockedCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, pauseBlockedCommand.State);

            var cooldownPolicies = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/cooldown");

            Assert.NotNull(cooldownPolicies);
            Assert.Equal(
                [PauseRequiredRuntimeId, policyBlockedRuntimeId],
                cooldownPolicies.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray());

            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-23T06:11:10Z", CultureInfo.InvariantCulture));
            var policyBlocked = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/policy-blocked");
            var manualApproval = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/manual-approval");
            var governanceOutOfPolicyCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/categories/governance-out-of-policy");
            var manualApprovalReadyCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/categories/manual-approval-ready");
            var reconcilePolicyOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/operations/reconcile");
            var pausePolicyOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/retry-execution-policies/operations/pause");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(policyBlocked);
            Assert.Equal([policyBlockedRuntimeId], policyBlocked.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.PolicyBlocked, policyBlocked[0].ManagedConnectorRetryExecutionPolicy.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.CommandRetry, policyBlocked[0].ManagedConnectorRetryExecutionPolicy.SourceId);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ProviderExecutionBlocked, policyBlocked[0].ManagedConnectorRetryExecutionPolicy.CategoryIds);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.GovernanceOutOfPolicy, policyBlocked[0].ManagedConnectorRetryExecutionPolicy.CategoryIds);

            Assert.NotNull(manualApproval);
            Assert.Equal([PauseRequiredRuntimeId], manualApproval.Select(static runtime => runtime.Id).ToArray());
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.ManualApproval, manualApproval[0].ManagedConnectorRetryExecutionPolicy.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.ExecutionApproval, manualApproval[0].ManagedConnectorRetryExecutionPolicy.SourceId);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ManualApprovalReady, manualApproval[0].ManagedConnectorRetryExecutionPolicy.CategoryIds);

            Assert.NotNull(governanceOutOfPolicyCategory);
            Assert.Equal([policyBlockedRuntimeId], governanceOutOfPolicyCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(manualApprovalReadyCategory);
            Assert.Equal([PauseRequiredRuntimeId], manualApprovalReadyCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(reconcilePolicyOperation);
            Assert.Equal([policyBlockedRuntimeId], reconcilePolicyOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(pausePolicyOperation);
            Assert.Equal([PauseRequiredRuntimeId], pausePolicyOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == policyBlockedRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.PolicyBlocked &&
                item.ManagedConnectorRetryExecutionPolicy.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.GovernanceOutOfPolicy, StringComparer.OrdinalIgnoreCase));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PauseRequiredRuntimeId &&
                item.ManagedConnectorRetryExecutionPolicy.State == CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.ManualApproval &&
                item.ManagedConnectorRetryExecutionPolicy.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ManualApprovalReady, StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorCommandJournalRoutesForTruncatedBoundedHistory()
    {
        const string truncatedRuntimeId = "inventory-journal-truncated-connector";
        const string truncatedCaptureId = "inventory-journal-truncated-cdc";
        var baseRecordedAtUtc = DateTimeOffset.Parse("2026-04-24T02:00:00Z", CultureInfo.InvariantCulture);

        var timeProvider = new MutableTimeProvider(baseRecordedAtUtc);
        var builder = CreateBuilder(
            options =>
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
            },
            timeProvider);

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();
            var reportResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{truncatedRuntimeId}/reports",
                new[]
                {
                    new CdcCaptureRuntimeObservation(
                        cdcCaptureId: truncatedCaptureId,
                        outcome: CdcCaptureRuntimeOutcomes.Captured,
                        observedAtUtc: baseRecordedAtUtc.AddSeconds(-30),
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
                });
            reportResponse.EnsureSuccessStatusCode();

            for (var index = 0; index < 25; index++)
            {
                timeProvider.SetUtcNow(baseRecordedAtUtc.AddSeconds(index + 1));
                var commandResponse = await client.PostAsJsonAsync(
                    $"/engine/cdc-capture-runtimes/{truncatedRuntimeId}/commands/reconcile",
                    new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest());

                commandResponse.EnsureSuccessStatusCode();
                var command = await commandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();
                Assert.NotNull(command);
                Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, command.State);
            }

            var truncatedJournal = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus>($"/engine/cdc-capture-runtimes/{truncatedRuntimeId}/command-journal");
            var truncatedByState = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/truncated");
            var truncatedByCategory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/command-journals/categories/history-truncated");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(truncatedJournal);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Truncated, truncatedJournal.State);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.Reconcile, truncatedJournal.OperationId);
            Assert.Equal(25, truncatedJournal.TotalRecordedEntryCount);
            Assert.Equal(20, truncatedJournal.RetainedEntryCount);
            Assert.Equal(20, truncatedJournal.MaximumRetainedEntryCount);
            Assert.True(truncatedJournal.IsTruncated);
            Assert.True(truncatedJournal.HasTruncatedHistory);
            Assert.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.HistoryTruncated, truncatedJournal.CategoryIds);
            Assert.False(string.IsNullOrWhiteSpace(truncatedJournal.LatestAttemptId));
            Assert.False(string.IsNullOrWhiteSpace(truncatedJournal.OldestRetainedAttemptId));

            Assert.NotNull(truncatedByState);
            Assert.Equal([truncatedRuntimeId], truncatedByState.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(truncatedByCategory);
            Assert.Equal([truncatedRuntimeId], truncatedByCategory.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == truncatedRuntimeId &&
                item.ManagedConnectorCommandJournal.State == CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Truncated &&
                item.ManagedConnectorCommandJournal.TotalRecordedEntryCount == 25 &&
                item.ManagedConnectorCommandJournal.RetainedEntryCount == 20 &&
                item.ManagedConnectorCommandJournal.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.HistoryTruncated, StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesManagedConnectorAutomaticRetryRoutesAfterBoundedBackgroundRetryRuns()
    {
        const string automaticRetryRuntimeId = "inventory-automatic-retry-connector";
        const string automaticRetryCaptureId = "inventory-automatic-retry-cdc";

        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-24T03:10:30Z", CultureInfo.InvariantCulture));
        var builder = CreateBuilder(
            options =>
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
            },
            timeProvider,
            dataOptions =>
            {
                dataOptions.EnableManagedConnectorAutomaticRetryExecution = true;
                dataOptions.ManagedConnectorAutomaticRetryPollingIntervalSeconds = 1;
            },
            services => services.AddSingleton<ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter>(
                new AutomaticRetryTestExecutionAdapter(automaticRetryRuntimeId)));

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();

            var reportResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{automaticRetryRuntimeId}/reports",
                new[]
                {
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
                });
            reportResponse.EnsureSuccessStatusCode();

            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-24T03:10:31Z", CultureInfo.InvariantCulture));
            var blockedCommandResponse = await client.PostAsJsonAsync(
                $"/engine/cdc-capture-runtimes/{automaticRetryRuntimeId}/commands/restart",
                new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
                {
                    Approve = true
                });
            blockedCommandResponse.EnsureSuccessStatusCode();

            var blockedCommand = await blockedCommandResponse.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult>();

            Assert.NotNull(blockedCommand);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, blockedCommand.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.OperatorRequest,
                blockedCommand.InvocationSourceId);
            Assert.True(blockedCommand.ApprovalApplied);

            timeProvider.SetUtcNow(DateTimeOffset.Parse("2026-04-24T03:11:10Z", CultureInfo.InvariantCulture));

            CdcCaptureExecutionRuntimeDescriptor? completedRuntime = null;
            for (var attempt = 0; attempt < 40; attempt++)
            {
                completedRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{automaticRetryRuntimeId}");
                if (completedRuntime?.ManagedConnectorAutomaticRetryExecution.IsCompleted == true)
                {
                    break;
                }

                await Task.Delay(100);
            }

            var completedByState = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/automatic-retries/completed");
            var automaticAttemptRecorded = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/automatic-retries/categories/automatic-attempt-recorded");
            var restartOperation = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/automatic-retries/operations/restart");
            var commandHistory = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[]>($"/engine/cdc-capture-runtimes/{automaticRetryRuntimeId}/command-executions");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(completedRuntime);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown,
                completedRuntime.ManagedConnectorRetryExecutionPolicy.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Completed,
                completedRuntime.ManagedConnectorAutomaticRetryExecution.State);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds.Restart,
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

            Assert.NotNull(completedByState);
            Assert.Equal([automaticRetryRuntimeId], completedByState.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(automaticAttemptRecorded);
            Assert.Equal([automaticRetryRuntimeId], automaticAttemptRecorded.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(restartOperation);
            Assert.Equal([automaticRetryRuntimeId], restartOperation.Select(static runtime => runtime.Id).ToArray());

            Assert.NotNull(commandHistory);
            Assert.Equal(2, commandHistory.Length);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.AutomaticRetry,
                commandHistory[0].InvocationSourceId);
            Assert.True(commandHistory[0].IsAutomaticRetryInvocation);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, commandHistory[0].State);
            Assert.True(commandHistory[0].ApprovalApplied);
            Assert.Equal(
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.OperatorRequest,
                commandHistory[1].InvocationSourceId);
            Assert.True(commandHistory[1].IsOperatorRequestInvocation);
            Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, commandHistory[1].State);
            Assert.True(commandHistory[1].ApprovalApplied);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == automaticRetryRuntimeId &&
                item.ManagedConnectorAutomaticRetryExecution.State == CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Completed &&
                item.ManagedConnectorAutomaticRetryExecution.HasMatchingAutomaticRetryAttempt &&
                item.ManagedConnectorAutomaticRetryExecution.CategoryIds.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.AutomaticAttemptRecorded,
                    StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static WebApplicationBuilder CreateBuilder(
        Action<DebeziumDataOptions> configure,
        TimeProvider? timeProvider = null,
        Action<DataRuntimeOptions>? configureData = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        if (timeProvider is not null)
        {
            builder.Services.AddSingleton<TimeProvider>(timeProvider);
        }

        configureServices?.Invoke(builder.Services);

        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(configureData);
            cephalon.AddDebeziumData(configure);
        });

        return builder;
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
