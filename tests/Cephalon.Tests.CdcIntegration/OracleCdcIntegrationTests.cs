using System.Data;
using System.Globalization;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Oracle.Configuration;
using Cephalon.Data.Oracle.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.CdcIntegration.ExternalServices;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;

namespace Cephalon.Tests.CdcIntegration;

public sealed class OracleCdcIntegrationTests : IAsyncLifetime
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string OracleRuntimeId = "oracle-logminer-capture-pump";
    private const string CaptureId = "oracle-orders-cdc";
    private const string DatabaseName = "XEPDB1";
    private const string TableName = "ORDERS";
    private const string CheckpointTableName = "CEPHALON_CDC_CHECKPOINTS";
    private const string OracleImage = "gvenzl/oracle-xe:21.3.0-slim-faststart";
    private const string ContainerUsername = "CEPHALON_CDC";
    private const string ContainerPassword = "cephalon";
    private OracleContainer? container;
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

    [ExternalCdcServiceFact(ExternalCdcServiceProvider.Oracle)]
    public async Task OracleCdc_StagesOutboxAndCommitsCheckpointAgainstLiveLogMiner()
    {
        var gate = ExternalCdcServiceGate.FromEnvironment();
        databaseConnectionString = await ResolveDatabaseConnectionStringAsync(gate).ConfigureAwait(false);
        var databaseName = await ResolveDatabaseNameAsync(databaseConnectionString).ConfigureAwait(false);
        var schemaName = await ResolveCurrentSchemaAsync(databaseConnectionString).ConfigureAwait(false);
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
            engine.AddOracleData(
                connectionString: databaseConnectionString,
                databaseName: databaseName,
                configure: options =>
                {
                    options.CheckpointTableName = CheckpointTableName;
                    options.CdcCaptures.Add(new OracleLogMinerCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "Oracle Orders CDC",
                        Description = "Captures Oracle order changes through a live provider-native LogMiner runner.",
                        SourceModuleId = "platform",
                        TableSchema = schemaName,
                        TableName = TableName,
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.oracle.changed",
                        InitialPosition = "earliest-available",
                        PollingIntervalSeconds = 1,
                        MaxChangesPerRead = 64,
                        MaxAwaitTimeSeconds = 20
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
                    TimeSpan.FromSeconds(90))
                .ConfigureAwait(false);

            await InsertOrderAsync(databaseConnectionString, "order-001", "created").ConfigureAwait(false);
            using var stagedMessageTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(180));
            await executionState.WaitForStagedMessageAsync(stagedMessageTimeout.Token)
                .ConfigureAwait(false);

            var stateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
            var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
            var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
            var state = await WaitForAsync(
                    () => Task.FromResult(stateCatalog.GetById(CaptureId)),
                    static current => current is not null && current.LastOutcome == CdcCaptureRuntimeOutcomes.Captured,
                    TimeSpan.FromSeconds(90))
                .ConfigureAwait(false);

            Assert.NotNull(state);
            Assert.Equal(OracleRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.Equal(1, state.LastCapturedChangeCount);
            Assert.Equal(1, state.LastProducedMessageCount);
            Assert.True(state.TotalCapturedChangeCount >= 1);
            Assert.True(state.TotalProducedMessageCount >= 1);
            Assert.False(string.IsNullOrWhiteSpace(state.LastCheckpoint));
            Assert.False(string.IsNullOrWhiteSpace(state.LastChangeId));
            Assert.Equal("oracle-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(OracleRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal("INSERT", state.Metadata["lastOperationType"], ignoreCase: true);
            Assert.Equal("1", state.Metadata["lastOperationCode"]);
            Assert.Equal(CheckpointTableName, state.Metadata["checkpointStore"]);
            Assert.Equal("cephalon-checkpoint-table", state.Metadata["checkpointSource"]);
            Assert.Equal("earliest-available", state.Metadata["resumeMode"]);
            Assert.Equal("available", state.Metadata["archiveLogLifecycleState"]);
            Assert.Equal("start", state.Metadata["archiveLogLifecycleAction"]);
            Assert.Equal("ARCHIVELOG", state.Metadata["archiveLogMode"], ignoreCase: true);
            Assert.Equal("observed", state.Metadata["databaseIdentityState"]);
            Assert.Equal("accept", state.Metadata["databaseIdentityAction"]);
            Assert.False(string.IsNullOrWhiteSpace(state.Metadata["databaseId"]));
            Assert.False(string.IsNullOrWhiteSpace(state.Metadata["currentScn"]));
            Assert.False(string.IsNullOrWhiteSpace(state.Metadata["earliestAvailableScn"]));
            Assert.False(string.IsNullOrWhiteSpace(state.Metadata["startScn"]));
            Assert.False(string.IsNullOrWhiteSpace(state.Metadata["endScn"]));
            Assert.True(int.Parse(state.Metadata["logFileCount"], CultureInfo.InvariantCulture) >= 1);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            var stagedMessage = Assert.Single(executionState.StagedMessages);
            Assert.Equal("orders", stagedMessage.ChannelId);
            Assert.Equal("orders.oracle.changed", stagedMessage.MessageType);
            Assert.Equal("application/vnd.cephalon.oracle.logminer+json", stagedMessage.ContentType);
            Assert.Equal(OracleDataOptions.ProviderId, stagedMessage.Headers["provider"]);
            Assert.Equal(CaptureId, stagedMessage.Headers["cdcCaptureId"]);
            Assert.Equal(databaseName, stagedMessage.Headers["databaseName"]);
            Assert.Equal(schemaName, stagedMessage.Headers["schemaName"], ignoreCase: true);
            Assert.Equal(TableName, stagedMessage.Headers["tableName"], ignoreCase: true);
            Assert.Equal("INSERT", stagedMessage.Headers["operation"], ignoreCase: true);
            Assert.Equal("1", stagedMessage.Headers["operationCode"]);
            Assert.False(string.IsNullOrWhiteSpace(stagedMessage.Headers["commitScn"]));
            Assert.False(string.IsNullOrWhiteSpace(stagedMessage.Headers["changeScn"]));
            Assert.False(string.IsNullOrWhiteSpace(stagedMessage.Headers["recordSetId"]));
            Assert.False(string.IsNullOrWhiteSpace(stagedMessage.Headers["sqlSequenceNumber"]));
            Assert.Equal(state.LastCheckpoint, stagedMessage.Metadata["checkpointToken"]);
            Assert.Equal("oracle-logminer-redo-event", stagedMessage.Metadata["eventFormat"]);

            using var payload = JsonDocument.Parse(stagedMessage.Payload);
            Assert.Equal(OracleDataOptions.ProviderId, payload.RootElement.GetProperty("provider").GetString());
            Assert.Equal("INSERT", payload.RootElement.GetProperty("operation").GetString(), ignoreCase: true);
            Assert.Equal("1", payload.RootElement.GetProperty("operationCode").GetInt32().ToString(CultureInfo.InvariantCulture));
            Assert.Contains("order-001", payload.RootElement.GetProperty("sqlRedo").GetString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("created", payload.RootElement.GetProperty("sqlRedo").GetString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(OracleRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("oracle", capture.Provider);
            Assert.Equal("logminer", capture.Mode);
            Assert.Equal("oracle-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal(databaseName, capture.Metadata["databaseName"]);
            Assert.Equal(schemaName, capture.Metadata["tableSchema"], ignoreCase: true);
            Assert.Equal(TableName, capture.Metadata["tableName"], ignoreCase: true);
            Assert.Equal(CheckpointTableName, capture.Metadata["checkpointStore"]);
            Assert.Equal("online-catalog", capture.Metadata["logMinerDictionary"]);
            Assert.Equal("committed-only", capture.Metadata["logMinerMode"]);
            Assert.Equal("commit-scn|change-scn|rs-id|ssn", capture.Metadata["redoCursor"]);
            Assert.Equal("fail-when-checkpoint-unavailable", capture.Metadata["archiveLogLifecyclePolicy"]);

            Assert.Empty(runtimeCatalog.GetById(SharedRuntimeId)!.CdcCaptureIds);
            var oracleRuntime = runtimeCatalog.GetById(OracleRuntimeId);
            Assert.NotNull(oracleRuntime);
            Assert.Equal("host-managed", oracleRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", oracleRuntime.ExecutionTopology);
            Assert.Equal("provider-native", oracleRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], oracleRuntime.CdcCaptureIds);
            Assert.True(oracleRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, oracleRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, oracleRuntime.Summary.LastOutcome);
            Assert.True(oracleRuntime.Summary.TotalCapturedChangeCount >= 1);
            Assert.True(oracleRuntime.Summary.TotalProducedMessageCount >= 1);
            Assert.Equal("provider-native", oracleRuntime.Summary.LastAcknowledgement);

            var checkpointParts = state.LastCheckpoint.Split('|', StringSplitOptions.TrimEntries);
            Assert.Equal(4, checkpointParts.Length);
            var checkpoint = await WaitForAsync(
                    () => ReadCheckpointAsync(databaseConnectionString),
                    value => value is not null && string.Equals(value.CheckpointToken, state.LastCheckpoint, StringComparison.Ordinal),
                    TimeSpan.FromSeconds(60))
                .ConfigureAwait(false);
            Assert.NotNull(checkpoint);
            Assert.Equal(CaptureId, checkpoint.CdcCaptureId);
            Assert.Equal(state.LastCheckpoint, checkpoint.CheckpointToken);
            Assert.Equal(decimal.Parse(checkpointParts[0], CultureInfo.InvariantCulture), checkpoint.CommitScn);
            Assert.Equal(decimal.Parse(checkpointParts[1], CultureInfo.InvariantCulture), checkpoint.ChangeScn);
            Assert.Equal(checkpointParts[2], checkpoint.RecordSetId);
            Assert.Equal(long.Parse(checkpointParts[3], CultureInfo.InvariantCulture), checkpoint.SqlSequenceNumber);
            Assert.True(checkpoint.DatabaseId > 0);
            Assert.Equal("ARCHIVELOG", checkpoint.ArchiveLogMode, ignoreCase: true);
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
        return gate.ResolveOracleMode() switch
        {
            ExternalCdcServiceMode.PreProvisionedConnectionString => NormalizeConnectionString(gate.OracleConnectionString!),
            ExternalCdcServiceMode.Testcontainers => await StartContainerAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalCdcServiceGate.SkipReason)
        };
    }

    private async Task<string> StartContainerAsync()
    {
        container = new OracleBuilder(OracleImage)
            .WithDatabase(DatabaseName)
            .WithUsername(ContainerUsername)
            .WithPassword(ContainerPassword)
            .Build();

        await container.StartAsync().ConfigureAwait(false);
        await EnableArchiveLogModeAsync(container).ConfigureAwait(false);
        await GrantLogMinerPrivilegesAsync(container).ConfigureAwait(false);
        return NormalizeConnectionString(container.GetConnectionString());
    }

    private static async Task EnableArchiveLogModeAsync(OracleContainer oracleContainer)
    {
        await ExecuteContainerSqlPlusAsSysDbaAsync(
                oracleContainer,
                """
                WHENEVER SQLERROR EXIT SQL.SQLCODE
                SHUTDOWN IMMEDIATE;
                STARTUP MOUNT;
                ALTER DATABASE ARCHIVELOG;
                ALTER DATABASE OPEN;
                ALTER PLUGGABLE DATABASE ALL OPEN;
                ALTER DATABASE ADD SUPPLEMENTAL LOG DATA;
                ALTER SYSTEM SWITCH LOGFILE;
                EXIT;
                """)
            .ConfigureAwait(false);
    }

    private static async Task GrantLogMinerPrivilegesAsync(OracleContainer oracleContainer)
    {
        await ExecuteContainerSqlPlusAsSysDbaAsync(
                oracleContainer,
                $"""
                WHENEVER SQLERROR EXIT SQL.SQLCODE
                ALTER SESSION SET CONTAINER = {DatabaseName};
                GRANT CREATE SESSION, CREATE TABLE, UNLIMITED TABLESPACE TO {ContainerUsername};
                GRANT LOGMINING TO {ContainerUsername};
                GRANT SELECT_CATALOG_ROLE TO {ContainerUsername};
                GRANT EXECUTE_CATALOG_ROLE TO {ContainerUsername};
                GRANT SELECT ON SYS.V_$DATABASE TO {ContainerUsername};
                GRANT SELECT ON SYS.V_$ARCHIVED_LOG TO {ContainerUsername};
                GRANT SELECT ON SYS.V_$LOG TO {ContainerUsername};
                GRANT SELECT ON SYS.V_$LOGFILE TO {ContainerUsername};
                GRANT SELECT ON SYS.V_$LOGMNR_CONTENTS TO {ContainerUsername};
                GRANT EXECUTE ON SYS.DBMS_LOGMNR TO {ContainerUsername};
                GRANT EXECUTE ON SYS.DBMS_LOGMNR_D TO {ContainerUsername};
                EXIT;
                """)
            .ConfigureAwait(false);
    }

    private static async Task ExecuteContainerSqlPlusAsSysDbaAsync(
        OracleContainer oracleContainer,
        string script)
    {
        var normalizedScript = script.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var shellCommand = string.Create(
            CultureInfo.InvariantCulture,
            $"cat <<'SQL' | sqlplus -S / as sysdba\n{normalizedScript}\nSQL");

        await oracleContainer.ExecAsync(["/bin/sh", "-c", shellCommand]).ConfigureAwait(false);
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return connectionString.Trim();
    }

    private static async Task<string> ResolveDatabaseNameAsync(string connectionString)
    {
        return await ExecuteScalarStringAsync(
                connectionString,
                "SELECT SYS_CONTEXT('USERENV', 'CON_NAME') FROM dual")
            .ConfigureAwait(false) ?? DatabaseName;
    }

    private static async Task<string> ResolveCurrentSchemaAsync(string connectionString)
    {
        return await ExecuteScalarStringAsync(
                connectionString,
                "SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM dual")
            .ConfigureAwait(false) ?? ContainerUsername;
    }

    private static async Task CreateCdcObjectsAsync(string connectionString)
    {
        await DropCdcObjectsAsync(connectionString).ConfigureAwait(false);
        await ExecuteNonQueryAsync(
                connectionString,
                $"""
                CREATE TABLE {EscapeIdentifier(TableName)}
                (
                    ID NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ORDER_ID VARCHAR2(64) NOT NULL,
                    STATUS VARCHAR2(64) NOT NULL,
                    CREATED_AT_UTC TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
                )
                """)
            .ConfigureAwait(false);
        await ExecuteNonQueryAsync(
                connectionString,
                $"ALTER TABLE {EscapeIdentifier(TableName)} ADD SUPPLEMENTAL LOG DATA (ALL) COLUMNS")
            .ConfigureAwait(false);
    }

    private static async Task InsertOrderAsync(
        string connectionString,
        string orderId,
        string status)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText =
            $"""
            INSERT INTO {EscapeIdentifier(TableName)} (ORDER_ID, STATUS)
            VALUES (:orderId, :status)
            """;
        command.Parameters.Add("orderId", OracleDbType.Varchar2, orderId, ParameterDirection.Input);
        command.Parameters.Add("status", OracleDbType.Varchar2, status, ParameterDirection.Input);
        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<OracleCheckpointSnapshot?> ReadCheckpointAsync(string connectionString)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText =
            $"""
            SELECT
                CdcCaptureId,
                CommitScn,
                ChangeScn,
                RecordSetId,
                SqlSequenceNumber,
                CheckpointToken,
                DatabaseId,
                DatabaseUniqueName,
                ResetLogsChangeNumber,
                ArchiveLogMode,
                SupplementalLogDataMin
            FROM {EscapeIdentifier(CheckpointTableName)}
            WHERE CdcCaptureId = :captureId
            """;
        command.Parameters.Add("captureId", OracleDbType.Varchar2, CaptureId, ParameterDirection.Input);

        using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
        {
            return null;
        }

        return new OracleCheckpointSnapshot(
            ReadString(reader, 0),
            ReadDecimal(reader, 1),
            ReadDecimal(reader, 2),
            ReadString(reader, 3),
            ReadInt64(reader, 4),
            ReadString(reader, 5),
            ReadNullableDecimal(reader, 6),
            ReadNullableString(reader, 7),
            ReadNullableDecimal(reader, 8),
            ReadNullableString(reader, 9),
            ReadNullableString(reader, 10));
    }

    private static async Task DropCdcObjectsAsync(string connectionString)
    {
        await TryCleanupAsync(() => ExecuteNonQueryAsync(
                connectionString,
                $"DROP TABLE {EscapeIdentifier(TableName)} PURGE"))
            .ConfigureAwait(false);
        await TryCleanupAsync(() => ExecuteNonQueryAsync(
                connectionString,
                $"DROP TABLE {EscapeIdentifier(CheckpointTableName)} PURGE"))
            .ConfigureAwait(false);
    }

    private static async Task TryCleanupAsync(Func<Task> cleanup)
    {
        try
        {
            await cleanup().ConfigureAwait(false);
        }
        catch (OracleException)
        {
            // Cleanup is best-effort because external pre-provisioned services may restrict DDL permissions.
        }
    }

    private static async Task ExecuteNonQueryAsync(string connectionString, string commandText)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandTimeout = 180;
        command.CommandText = commandText;
        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<string?> ExecuteScalarStringAsync(string connectionString, string commandText)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var scalar = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return scalar is null || scalar is DBNull
            ? null
            : Convert.ToString(scalar, CultureInfo.InvariantCulture)?.Trim();
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

        throw new TimeoutException("Timed out while waiting for the expected Oracle CDC integration condition.");
    }

    private static string EscapeIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    private static decimal ReadDecimal(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? 0m
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static decimal? ReadNullableDecimal(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static long ReadInt64(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? 0L
            : Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static string ReadString(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? string.Empty
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static string? ReadNullableString(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim();
    }

    private sealed record OracleCheckpointSnapshot(
        string CdcCaptureId,
        decimal CommitScn,
        decimal ChangeScn,
        string RecordSetId,
        long SqlSequenceNumber,
        string CheckpointToken,
        decimal? DatabaseId,
        string? DatabaseUniqueName,
        decimal? ResetLogsChangeNumber,
        string? ArchiveLogMode,
        string? SupplementalLogDataMin);
}
