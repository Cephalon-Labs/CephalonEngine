using System.Net.Http.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Data.SqlServer.Configuration;
using Cephalon.Data.SqlServer.Registration;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

[Collection(ProviderNativeCdcHostingCollectionDefinition.Name)]
public sealed class SqlServerDataCdcHostingTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string SqlRuntimeId = "sqlserver-cdc-capture-pump";
    private const string CaptureId = "sql-orders-cdc";
    private static readonly TimeSpan ProviderNativeCdcTimeout = TimeSpan.FromSeconds(90);

    [Fact]
    public async Task MapCephalonExposesSqlServerProviderNativeCdcRuntimeSurfaces()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new SqlServerCdcTestHarness();
        var batch = new SqlServerCdcTestBatch();
        batch.Metadata["checkpointStore"] = "dbo.cephalon_cdc_checkpoints";
        batch.Changes.Add(new SqlServerCdcTestChange
        {
            StartLsn = "0x00000000000000000001",
            SequenceValue = "0x0000000000000000000A",
            Operation = 2,
            ChangeId = "lsn-0001",
            Payload = """{"orderId":"order-001","status":"created"}"""
        });
        harness.EnqueueBatch(batch);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(executionState);
        builder.Services.AddSqlServerCdcTestHarness(harness);
        builder.Services.AddScoped<IOutbox, TestOutbox>();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new PlatformEventingTestModule());
            cephalon.AddData(options =>
            {
                options.EnableCdcExecution = true;
                options.CdcPollingIntervalSeconds = 600;
            });
            cephalon.AddSqlServerData(
                connectionString: "Server=(local);Database=cephalon;Integrated Security=true;TrustServerCertificate=true",
                databaseName: "cephalon",
                configure: options =>
                {
                    options.CdcCaptures.Add(new SqlServerCdcCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "SQL Orders CDC",
                        Description = "Captures SQL Server order changes through a provider-native CDC runner.",
                        SourceModuleId = "platform",
                        CaptureInstance = "dbo_orders",
                        TableSchema = "dbo",
                        TableName = "orders",
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.sql.changed",
                        InitialPosition = "earliest-available",
                        PollingIntervalSeconds = 600,
                        MaxChangesPerPoll = 64
                    });
                });
        });

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            await executionState.WaitForStagedMessageAsync();

            var client = app.GetTestClient();
            var cdcState = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureRuntimeState>($"/engine/cdc-captures/runtime/{CaptureId}")!,
                static state => HasObservedInsertedChange(state),
                ProviderNativeCdcTimeout);

            var cdcCaptureRuntimes = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes");
            var sqlRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{SqlRuntimeId}");
            var capturesByRuntime = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>($"/engine/cdc-captures/execution-runtimes/{SqlRuntimeId}");
            var captureStatesByRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>($"/engine/cdc-captures/runtime/execution-runtimes/{SqlRuntimeId}");
            var hostedExecutions = await client.GetFromJsonAsync<HostedExecutionDescriptor[]>("/engine/hosted-executions");
            var executionGraphs = await client.GetFromJsonAsync<ExecutionGraphDescriptor[]>("/engine/execution-graphs");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(cdcCaptureRuntimes);
            Assert.NotNull(sqlRuntime);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == SqlRuntimeId);

            var sharedRuntime = Assert.Single(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            Assert.Equal("host-managed", sqlRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", sqlRuntime.ExecutionTopology);
            Assert.Equal("provider-native", sqlRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], sqlRuntime.CdcCaptureIds);
            Assert.True(sqlRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, sqlRuntime.Summary.LastCdcCaptureId);
            Assert.True(IsCapturedOrIdle(sqlRuntime.Summary.LastOutcome));
            Assert.True(sqlRuntime.Summary.CapturedCount > 0);
            Assert.Equal(1, sqlRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, sqlRuntime.Summary.TotalProducedMessageCount);

            var capture = Assert.Single(capturesByRuntime!);
            Assert.Equal(CaptureId, capture.Id);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(SqlRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("sqlserver-data", capture.Metadata["contributorModuleId"]);

            var captureState = Assert.Single(captureStatesByRuntime!);
            Assert.Equal(CaptureId, captureState.CdcCaptureId);
            Assert.Equal(SqlRuntimeId, captureState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(captureState.LastOutcome));
            Assert.True(captureState.CapturedCount > 0);
            Assert.Equal(1, captureState.TotalCapturedChangeCount);
            Assert.Equal(1, captureState.TotalProducedMessageCount);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, captureState.Publication.State);

            Assert.NotNull(cdcState);
            Assert.Equal(SqlRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(cdcState.LastOutcome));
            Assert.True(cdcState.CapturedCount > 0);
            Assert.Equal(1, cdcState.TotalCapturedChangeCount);
            Assert.Equal(1, cdcState.TotalProducedMessageCount);
            Assert.Equal("sqlserver-provider-native-runtime", cdcState.Metadata["captureExecution"]);
            Assert.Equal(SqlRuntimeId, cdcState.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", cdcState.Metadata["acknowledgement"]);
            Assert.Equal("dbo.cephalon_cdc_checkpoints", cdcState.Metadata["checkpointStore"]);

            var hostedExecution = Assert.Single(hostedExecutions!, item => item.Id == SqlRuntimeId);
            Assert.Equal("sqlserver-data", hostedExecution.SourceModuleId);
            Assert.Equal("background-service", hostedExecution.Kind);

            var executionGraph = Assert.Single(executionGraphs!, item => item.Id == "sqlserver-cdc-capture-flow");
            Assert.Equal("sqlserver-data", executionGraph.SourceModuleId);
            Assert.Equal("resolve-sqlserver-cdc-captures", executionGraph.EntryNodeId);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptures, item => item.Id == CaptureId &&
                item.ExecutionBinding.EffectiveExecutionRuntimeId == SqlRuntimeId);
            Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == CaptureId &&
                HasObservedInsertedChange(item) &&
                item.Publication.State == CdcCapturePublicationStates.PendingPublication);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == SqlRuntimeId &&
                HasObservedInsertedChange(item));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static async Task<T> WaitForAsync<T>(
        Func<Task<T>> producer,
        Func<T, bool> predicate,
        TimeSpan timeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var current = await producer().ConfigureAwait(false);
            if (predicate(current))
            {
                return current;
            }

            try
            {
                await Task.Delay(200, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException("Timed out while waiting for the expected SQL Server CDC hosting condition.");
    }

    private static bool HasObservedInsertedChange(CdcCaptureRuntimeState? state)
    {
        return state is not null &&
            IsCapturedOrIdle(state.LastOutcome) &&
            state.CapturedCount > 0 &&
            state.TotalCapturedChangeCount == 1 &&
            state.TotalProducedMessageCount == 1;
    }

    private static bool HasObservedInsertedChange(CdcCaptureExecutionRuntimeDescriptor? runtime)
    {
        return runtime is not null &&
            IsCapturedOrIdle(runtime.Summary.LastOutcome) &&
            runtime.Summary.CapturedCount > 0 &&
            runtime.Summary.TotalCapturedChangeCount == 1 &&
            runtime.Summary.TotalProducedMessageCount == 1;
    }

    private static bool IsCapturedOrIdle(string? outcome)
    {
        return string.Equals(outcome, CdcCaptureRuntimeOutcomes.Captured, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outcome, CdcCaptureRuntimeOutcomes.Idle, StringComparison.OrdinalIgnoreCase);
    }
}
