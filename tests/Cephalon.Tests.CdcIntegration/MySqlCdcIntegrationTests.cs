using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.Registration;
using Cephalon.Data.MySql.SciSharpReplication.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.CdcIntegration.ExternalServices;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;
using Testcontainers.MySql;

namespace Cephalon.Tests.CdcIntegration;

public sealed class MySqlCdcIntegrationTests : IAsyncLifetime
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string MySqlRuntimeId = "mysql-binlog-capture-pump";
    private const string CaptureId = "mysql-orders-cdc";
    private const string DatabaseName = "cephalon_cdc";
    private const string TableName = "orders";
    private const string CheckpointTableName = "cephalon_cdc_checkpoints";
    private const string MySqlImage = "mysql:8.4";
    private const string ContainerUsername = "cephalon";
    private const string ContainerPassword = "cephalon";
    private MySqlContainer? container;
    private string? databaseConnectionString;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (databaseConnectionString is not null)
        {
            await DropCdcObjectsAsync(databaseConnectionString).ConfigureAwait(false);
        }

        if (container is not null)
        {
            await container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [ExternalCdcServiceFact(ExternalCdcServiceProvider.MySql)]
    public async Task MySqlCdc_StagesOutboxAndCommitsCheckpointAgainstLiveBinlog()
    {
        var gate = ExternalCdcServiceGate.FromEnvironment();
        databaseConnectionString = await ResolveDatabaseConnectionStringAsync(gate).ConfigureAwait(false);
        var databaseName = ResolveDatabaseName(databaseConnectionString);
        await CreateCdcObjectsAsync(databaseConnectionString).ConfigureAwait(false);

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
            engine.AddMySqlData(
                connectionString: databaseConnectionString,
                databaseName: databaseName,
                configure: options =>
                {
                    options.CheckpointTableName = CheckpointTableName;
                    options.CdcCaptures.Add(new MySqlBinlogCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "MySQL Orders CDC",
                        Description = "Captures MySQL order changes through the live SciSharp-backed binlog adapter.",
                        SourceModuleId = "platform",
                        TableSchema = databaseName,
                        TableName = TableName,
                        ServerId = 700201,
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.mysql.changed",
                        InitialPosition = "latest-available",
                        PollingIntervalSeconds = 1,
                        MaxChangesPerRead = 64,
                        MaxAwaitTimeSeconds = 30
                    });
                });
            engine.AddSciSharpMySqlBinlogReplication();
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
                    TimeSpan.FromSeconds(45))
                .ConfigureAwait(false);

            await InsertOrderAsync(databaseConnectionString, "order-001", "created").ConfigureAwait(false);
            using var stagedMessageTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await executionState.WaitForStagedMessageAsync(stagedMessageTimeout.Token)
                .ConfigureAwait(false);

            var stateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
            var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
            var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
            var state = await WaitForAsync(
                    () => Task.FromResult(stateCatalog.GetById(CaptureId)),
                    static current => current is not null && current.LastOutcome == CdcCaptureRuntimeOutcomes.Captured,
                    TimeSpan.FromSeconds(45))
                .ConfigureAwait(false);

            Assert.NotNull(state);
            Assert.Equal(MySqlRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.Equal(1, state.LastCapturedChangeCount);
            Assert.Equal(1, state.LastProducedMessageCount);
            Assert.True(state.TotalCapturedChangeCount >= 1);
            Assert.True(state.TotalProducedMessageCount >= 1);
            Assert.False(string.IsNullOrWhiteSpace(state.LastCheckpoint));
            Assert.False(string.IsNullOrWhiteSpace(state.LastChangeId));
            Assert.Equal("mysql-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(MySqlRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal("insert", state.Metadata["lastOperationType"]);
            Assert.Equal($"{databaseName}.{CheckpointTableName}", state.Metadata["checkpointStore"]);
            Assert.Equal("cephalon-checkpoint-table", state.Metadata["binlogCheckpointSource"]);
            Assert.Equal("latest-available", state.Metadata["binlogResumeMode"]);
            Assert.Equal("available", state.Metadata["binlogLifecycleState"]);
            Assert.Equal("start", state.Metadata["binlogLifecycleAction"]);
            Assert.Equal("ROW", state.Metadata["binlogFormat"]);
            Assert.Equal("FULL", state.Metadata["binlogRowImage"]);
            Assert.Equal("true", state.Metadata["binaryLoggingEnabled"]);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            var stagedMessage = Assert.Single(executionState.StagedMessages);
            Assert.Equal("orders", stagedMessage.ChannelId);
            Assert.Equal("orders.mysql.changed", stagedMessage.MessageType);
            Assert.Equal("application/vnd.cephalon.mysql.binlog+json", stagedMessage.ContentType);
            Assert.Equal(MySqlDataOptions.ProviderId, stagedMessage.Headers["provider"]);
            Assert.Equal(CaptureId, stagedMessage.Headers["cdcCaptureId"]);
            Assert.Equal(databaseName, stagedMessage.Headers["databaseName"]);
            Assert.Equal(databaseName, stagedMessage.Headers["schemaName"]);
            Assert.Equal(TableName, stagedMessage.Headers["tableName"]);
            Assert.Equal("700201", stagedMessage.Headers["serverId"]);
            Assert.Equal("insert", stagedMessage.Headers["operation"]);

            using var payload = JsonDocument.Parse(stagedMessage.Payload);
            Assert.Equal("order-001", payload.RootElement.GetProperty("row").GetProperty("order_id").GetString());
            Assert.Equal("created", payload.RootElement.GetProperty("row").GetProperty("status").GetString());

            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(MySqlRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("mysql-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal(databaseName, capture.Metadata["tableSchema"]);
            Assert.Equal(TableName, capture.Metadata["tableName"]);
            Assert.Equal("checkpoint-or-initial-position", capture.Metadata["binlogResumeMode"]);
            Assert.Equal("checkpoint-validation", capture.Metadata["binlogLifecyclePolicy"]);
            Assert.Equal("observe-only", capture.Metadata["sourceServerIdentityMode"]);

            Assert.Empty(runtimeCatalog.GetById(SharedRuntimeId)!.CdcCaptureIds);
            var mySqlRuntime = runtimeCatalog.GetById(MySqlRuntimeId);
            Assert.NotNull(mySqlRuntime);
            Assert.Equal("host-managed", mySqlRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", mySqlRuntime.ExecutionTopology);
            Assert.Equal("provider-native", mySqlRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], mySqlRuntime.CdcCaptureIds);
            Assert.True(mySqlRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, mySqlRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, mySqlRuntime.Summary.LastOutcome);
            Assert.True(mySqlRuntime.Summary.TotalCapturedChangeCount >= 1);
            Assert.True(mySqlRuntime.Summary.TotalProducedMessageCount >= 1);
            Assert.Equal("provider-native", mySqlRuntime.Summary.LastAcknowledgement);

            var checkpoint = await WaitForAsync(
                    () => ReadCheckpointTokenAsync(databaseConnectionString),
                    value => string.Equals(value, state.LastCheckpoint, StringComparison.OrdinalIgnoreCase),
                    TimeSpan.FromSeconds(30))
                .ConfigureAwait(false);
            Assert.Equal(state.LastCheckpoint, checkpoint, ignoreCase: true);
        }
        finally
        {
            foreach (var hostedService in hostedServices.Reverse())
            {
                await hostedService.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async Task<string> ResolveDatabaseConnectionStringAsync(ExternalCdcServiceGate gate)
    {
        return gate.ResolveMySqlMode() switch
        {
            ExternalCdcServiceMode.PreProvisionedConnectionString => NormalizeConnectionString(gate.MySqlConnectionString!),
            ExternalCdcServiceMode.Testcontainers => await StartContainerAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalCdcServiceGate.SkipReason)
        };
    }

    private async Task<string> StartContainerAsync()
    {
        container = new MySqlBuilder(MySqlImage)
            .WithDatabase(DatabaseName)
            .WithUsername(ContainerUsername)
            .WithPassword(ContainerPassword)
            .WithCommand(
                "--server-id=700200",
                "--log-bin=mysql-bin",
                "--binlog-format=ROW",
                "--binlog-row-image=FULL",
                "--gtid-mode=ON",
                "--enforce-gtid-consistency=ON")
            .Build();

        await container.StartAsync().ConfigureAwait(false);
        var connectionString = NormalizeConnectionString(container.GetConnectionString());
        await GrantReplicationPrivilegesAsync(CreateRootConnectionString(connectionString)).ConfigureAwait(false);
        return connectionString;
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            builder.Database = DatabaseName;
        }

        builder.SslMode = MySqlSslMode.Disabled;
        builder.AllowPublicKeyRetrieval = true;
        return builder.ConnectionString;
    }

    private static string CreateRootConnectionString(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            UserID = "root",
            Password = ContainerPassword,
            Database = DatabaseName,
            SslMode = MySqlSslMode.Disabled,
            AllowPublicKeyRetrieval = true
        };

        return builder.ConnectionString;
    }

    private static async Task GrantReplicationPrivilegesAsync(string rootConnectionString)
    {
        await ExecuteNonQueryAsync(
                rootConnectionString,
                $"""
GRANT SELECT, RELOAD, REPLICATION SLAVE, REPLICATION CLIENT ON *.* TO '{EscapeStringLiteral(ContainerUsername)}'@'%';
FLUSH PRIVILEGES;
""")
            .ConfigureAwait(false);
    }

    private static async Task CreateCdcObjectsAsync(string connectionString)
    {
        await ExecuteNonQueryAsync(
                connectionString,
                $"""
CREATE TABLE IF NOT EXISTS `{EscapeIdentifier(TableName)}`
(
    `id` int NOT NULL AUTO_INCREMENT,
    `order_id` varchar(64) NOT NULL,
    `status` varchar(64) NOT NULL,
    `created_at_utc` timestamp(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`)
) ENGINE=InnoDB;
""")
            .ConfigureAwait(false);
    }

    private static async Task InsertOrderAsync(
        string connectionString,
        string orderId,
        string status)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
INSERT INTO `{EscapeIdentifier(TableName)}` (`order_id`, `status`)
VALUES (@orderId, @status);
""";
        command.Parameters.AddWithValue("@orderId", orderId);
        command.Parameters.AddWithValue("@status", status);
        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<string?> ReadCheckpointTokenAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT `CheckpointToken`
FROM `{EscapeIdentifier(CheckpointTableName)}`
WHERE `CdcCaptureId` = @cdcCaptureId;
""";
        command.Parameters.AddWithValue("@cdcCaptureId", CaptureId);
        return await command.ExecuteScalarAsync().ConfigureAwait(false) as string;
    }

    private static async Task DropCdcObjectsAsync(string connectionString)
    {
        await TryCleanupAsync(() => ExecuteNonQueryAsync(
                connectionString,
                $"DROP TABLE IF EXISTS `{EscapeIdentifier(TableName)}`;"))
            .ConfigureAwait(false);
        await TryCleanupAsync(() => ExecuteNonQueryAsync(
                connectionString,
                $"DROP TABLE IF EXISTS `{EscapeIdentifier(CheckpointTableName)}`;"))
            .ConfigureAwait(false);
    }

    private static async Task TryCleanupAsync(Func<Task> cleanup)
    {
        try
        {
            await cleanup().ConfigureAwait(false);
        }
        catch (MySqlException)
        {
            // Cleanup is best-effort because external pre-provisioned services may restrict DDL permissions.
        }
    }

    private static async Task ExecuteNonQueryAsync(string connectionString, string commandText)
    {
        await using var connection = new MySqlConnection(connectionString);
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
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var current = await producer().ConfigureAwait(false);
            if (predicate(current))
            {
                return current;
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException("Timed out while waiting for the expected MySQL CDC integration condition.");
    }

    private static string ResolveDatabaseName(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        return string.IsNullOrWhiteSpace(builder.Database) ? DatabaseName : builder.Database;
    }

    private static string EscapeIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().Replace("`", "``", StringComparison.Ordinal);
    }

    private static string EscapeStringLiteral(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "''", StringComparison.Ordinal);
    }
}
