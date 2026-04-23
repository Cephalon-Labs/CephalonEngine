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

    [Fact]
    public async Task MapCephalonExposesDebeziumManagedConnectorReportingRouteWithoutBaseOptInFlag()
    {
        var builder = CreateBuilder(options =>
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
            Assert.Equal("2", state.Metadata["managedConnectorExpectedTaskCount"]);
            Assert.Equal("0,1", state.Metadata["managedConnectorDeclaredTaskIds"]);
            Assert.Equal("0,2", state.Metadata["managedConnectorReportedTaskIds"]);
            Assert.Equal("0,2", state.Metadata["managedConnectorActiveTaskIds"]);
            Assert.Equal("task-mismatch", state.Metadata["managedConnectorTaskReconciliationState"]);
            Assert.Equal("task-mismatch", state.Metadata["managedConnectorReconciliationState"]);
            Assert.Equal("The Debezium connector declared tasks '0,1' but last reported '0,2'.", state.Metadata["managedConnectorReconciliationReason"]);
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

    private static WebApplicationBuilder CreateBuilder(Action<DebeziumDataOptions> configure)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
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
}
