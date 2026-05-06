using System.Data;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Data.SqlServer.Configuration;
using Cephalon.Data.SqlServer.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.CdcIntegration.ExternalServices;
using Cephalon.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;

namespace Cephalon.Tests.CdcIntegration;

public sealed class SqlServerCdcIntegrationTests : IAsyncLifetime
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string SqlRuntimeId = "sqlserver-cdc-capture-pump";
    private const string CaptureId = "sql-orders-cdc";
    private const string CaptureInstance = "dbo_orders";
    private const string DatabasePrefix = "cephalon_cdc_";
    private const string MsSqlImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";
    private MsSqlContainer? container;
    private string? serverConnectionString;
    private string? databaseName;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (databaseName is not null && serverConnectionString is not null)
        {
            await DropDatabaseAsync(serverConnectionString, databaseName).ConfigureAwait(false);
        }

        if (container is not null)
        {
            await container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [ExternalCdcServiceFact(ExternalCdcServiceProvider.SqlServer)]
    public async Task SqlServerCdc_StagesOutboxAndPersistsCheckpointAgainstLiveDatabase()
    {
        var gate = ExternalCdcServiceGate.FromEnvironment();
        serverConnectionString = await ResolveServerConnectionStringAsync(gate).ConfigureAwait(false);
        databaseName = CreateDatabaseName();
        var databaseConnectionString = await CreateCdcDatabaseAsync(serverConnectionString, databaseName).ConfigureAwait(false);

        var executionState = new TestCdcExecutionState();
        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddScoped<IOutbox, TestOutbox>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new PlatformEventingTestModule());
            engine.AddData(options =>
            {
                options.EnableCdcExecution = true;
                options.CdcPollingIntervalSeconds = 600;
            });
            engine.AddSqlServerData(
                connectionString: databaseConnectionString,
                databaseName: databaseName,
                configure: options =>
                {
                    options.CdcCaptures.Add(new SqlServerCdcCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "SQL Orders CDC",
                        Description = "Captures SQL Server order changes through a live provider-native CDC runner.",
                        SourceModuleId = "platform",
                        CaptureInstance = CaptureInstance,
                        TableSchema = "dbo",
                        TableName = "orders",
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.sql.changed",
                        InitialPosition = "earliest-available",
                        PollingIntervalSeconds = 1,
                        MaxChangesPerPoll = 64
                    });
                });
        });

        using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        try
        {
            await WaitForAsync(
                    () => Task.FromResult(provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>().GetById(CaptureId)),
                    static state => state is not null && state.StartedCount > 0,
                    TimeSpan.FromSeconds(30))
                .ConfigureAwait(false);

            await InsertOrderAsync(databaseConnectionString, "order-001", "created").ConfigureAwait(false);
            using var stagedMessageTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await executionState.WaitForStagedMessageAsync(stagedMessageTimeout.Token)
                .ConfigureAwait(false);

            var stateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
            var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
            var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
            var state = await WaitForAsync(
                    () => Task.FromResult(stateCatalog.GetById(CaptureId)),
                    static current => current is not null && current.LastOutcome == CdcCaptureRuntimeOutcomes.Captured,
                    TimeSpan.FromSeconds(30))
                .ConfigureAwait(false);

            Assert.NotNull(state);
            Assert.Equal(SqlRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.Equal(1, state.LastCapturedChangeCount);
            Assert.Equal(1, state.LastProducedMessageCount);
            Assert.True(state.TotalCapturedChangeCount >= 1);
            Assert.True(state.TotalProducedMessageCount >= 1);
            Assert.False(string.IsNullOrWhiteSpace(state.LastCheckpoint));
            Assert.False(string.IsNullOrWhiteSpace(state.LastChangeId));
            Assert.Equal("sqlserver-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(SqlRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal("insert", state.Metadata["lastOperationType"]);
            Assert.Equal("dbo.cephalon_cdc_checkpoints", state.Metadata["checkpointStore"]);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            var stagedMessage = Assert.Single(executionState.StagedMessages);
            Assert.Equal("orders", stagedMessage.ChannelId);
            Assert.Equal("orders.sql.changed", stagedMessage.MessageType);
            Assert.Equal("application/vnd.cephalon.sqlserver.cdc+json", stagedMessage.ContentType);
            Assert.Equal(SqlServerDataOptions.ProviderId, stagedMessage.Headers["provider"]);
            Assert.Equal(CaptureId, stagedMessage.Headers["cdcCaptureId"]);
            Assert.Equal(databaseName, stagedMessage.Headers["databaseName"]);
            Assert.Equal("dbo", stagedMessage.Headers["schemaName"]);
            Assert.Equal("orders", stagedMessage.Headers["tableName"]);
            Assert.Equal(CaptureInstance, stagedMessage.Headers["captureInstance"]);
            Assert.Equal("insert", stagedMessage.Headers["operation"]);

            using var payload = JsonDocument.Parse(stagedMessage.Payload);
            Assert.Equal("order-001", payload.RootElement.GetProperty("data").GetProperty("order_id").GetString());
            Assert.Equal("created", payload.RootElement.GetProperty("data").GetProperty("status").GetString());

            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(SqlRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("sqlserver-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal(CaptureInstance, capture.Metadata["captureInstance"]);

            Assert.Empty(runtimeCatalog.GetById(SharedRuntimeId)!.CdcCaptureIds);
            var sqlRuntime = runtimeCatalog.GetById(SqlRuntimeId);
            Assert.NotNull(sqlRuntime);
            Assert.Equal("host-managed", sqlRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", sqlRuntime.ExecutionTopology);
            Assert.Equal("provider-native", sqlRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], sqlRuntime.CdcCaptureIds);
            Assert.True(sqlRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, sqlRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, sqlRuntime.Summary.LastOutcome);
            Assert.True(sqlRuntime.Summary.TotalCapturedChangeCount >= 1);
            Assert.True(sqlRuntime.Summary.TotalProducedMessageCount >= 1);
            Assert.Equal("provider-native", sqlRuntime.Summary.LastAcknowledgement);

            var checkpoint = await ReadCheckpointAsync(databaseConnectionString).ConfigureAwait(false);
            Assert.NotNull(checkpoint);
            Assert.Equal(state.LastCheckpoint, checkpoint);
        }
        finally
        {
            foreach (var hostedService in hostedServices.Reverse())
            {
                await hostedService.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async Task<string> ResolveServerConnectionStringAsync(ExternalCdcServiceGate gate)
    {
        return gate.ResolveSqlServerMode() switch
        {
            ExternalCdcServiceMode.PreProvisionedConnectionString => CreateServerConnectionString(gate.SqlServerConnectionString!),
            ExternalCdcServiceMode.Testcontainers => await StartContainerAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalCdcServiceGate.SkipReason)
        };
    }

    private async Task<string> StartContainerAsync()
    {
        container = new MsSqlBuilder(MsSqlImage)
            .WithEnvironment("MSSQL_AGENT_ENABLED", "true")
            .Build();

        await container.StartAsync().ConfigureAwait(false);
        return CreateServerConnectionString(container.GetConnectionString());
    }

    private static async Task<string> CreateCdcDatabaseAsync(string masterConnectionString, string databaseName)
    {
        await ExecuteNonQueryAsync(
                masterConnectionString,
                $"""
CREATE DATABASE [{EscapeIdentifier(databaseName)}];
""")
            .ConfigureAwait(false);

        var databaseConnectionString = CreateDatabaseConnectionString(masterConnectionString, databaseName);
        await ExecuteNonQueryAsync(
                databaseConnectionString,
                """
EXEC sys.sp_cdc_enable_db;

CREATE TABLE dbo.orders
(
    id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_orders PRIMARY KEY,
    order_id nvarchar(64) NOT NULL,
    status nvarchar(32) NOT NULL,
    created_at_utc datetime2 NOT NULL CONSTRAINT DF_orders_created_at_utc DEFAULT SYSUTCDATETIME()
);

EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'orders',
    @role_name = NULL,
    @capture_instance = N'dbo_orders',
    @supports_net_changes = 0;
""")
            .ConfigureAwait(false);

        return databaseConnectionString;
    }

    private static async Task InsertOrderAsync(string connectionString, string orderId, string status)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO dbo.orders (order_id, status)
VALUES (@orderId, @status);
""";
        command.Parameters.Add(new SqlParameter("@orderId", SqlDbType.NVarChar, 64) { Value = orderId });
        command.Parameters.Add(new SqlParameter("@status", SqlDbType.NVarChar, 32) { Value = status });
        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<string?> ReadCheckpointAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT TOP (1) CheckpointToken
FROM dbo.cephalon_cdc_checkpoints
WHERE CdcCaptureId = @cdcCaptureId;
""";
        command.Parameters.Add(new SqlParameter("@cdcCaptureId", SqlDbType.NVarChar, 128) { Value = CaptureId });
        var scalar = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return scalar as string;
    }

    private static async Task DropDatabaseAsync(string masterConnectionString, string databaseName)
    {
        try
        {
            await ExecuteNonQueryAsync(
                    masterConnectionString,
                    $"""
IF DB_ID(N'{EscapeSqlLiteral(databaseName)}') IS NOT NULL
BEGIN
    ALTER DATABASE [{EscapeIdentifier(databaseName)}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{EscapeIdentifier(databaseName)}];
END;
""")
                .ConfigureAwait(false);
        }
        catch (SqlException)
        {
            // Cleanup is best-effort because external pre-provisioned services may restrict database-level permissions.
        }
    }

    private static async Task ExecuteNonQueryAsync(string connectionString, string commandText)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 120;
        command.CommandText = commandText;
        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
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

            await Task.Delay(500, cancellationTokenSource.Token).ConfigureAwait(false);
        }

        throw new TimeoutException("Timed out while waiting for the expected SQL Server CDC integration condition.");
    }

    private static string CreateServerConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }

    private static string CreateDatabaseConnectionString(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }

    private static string CreateDatabaseName()
    {
        return $"{DatabasePrefix}{Guid.NewGuid():N}"[..30];
    }

    private static string EscapeIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().Replace("]", "]]", StringComparison.Ordinal);
    }

    private static string EscapeSqlLiteral(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().Replace("'", "''", StringComparison.Ordinal);
    }
}
