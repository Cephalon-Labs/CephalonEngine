using System.Globalization;
using System.Net.Http.Json;
using Cephalon.Abstractions.Data;
using Cephalon.AspNetCore.Hosting;
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

    private static WebApplicationBuilder CreateBuilder(
        Action<DebeziumDataOptions> configure,
        TimeProvider? timeProvider = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        if (timeProvider is not null)
        {
            builder.Services.AddSingleton<TimeProvider>(timeProvider);
        }

        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData();
            cephalon.AddDebeziumData(configure);
        });

        return builder;
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
