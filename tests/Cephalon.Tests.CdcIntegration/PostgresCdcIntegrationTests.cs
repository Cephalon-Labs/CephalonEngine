using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Postgres.Configuration;
using Cephalon.Data.Postgres.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.CdcIntegration.ExternalServices;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Cephalon.Tests.CdcIntegration;

public sealed class PostgresCdcIntegrationTests : IAsyncLifetime
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string PostgresRuntimeId = "postgresql-logical-replication-capture-pump";
    private const string CaptureId = "pg-orders-cdc";
    private const string TableName = "orders";
    private const string PostgresImage = "postgres:16-alpine";
    private PostgreSqlContainer? container;
    private string? databaseConnectionString;
    private string? schemaName;
    private string? publicationName;
    private string? slotName;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (databaseConnectionString is not null)
        {
            await DropCdcObjectsAsync(databaseConnectionString, schemaName, publicationName, slotName).ConfigureAwait(false);
        }

        if (container is not null)
        {
            await container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [ExternalCdcServiceFact(ExternalCdcServiceProvider.Postgres)]
    public async Task PostgresCdc_StagesOutboxAndConfirmsSlotCheckpointAgainstLiveDatabase()
    {
        var gate = ExternalCdcServiceGate.FromEnvironment();
        databaseConnectionString = await ResolveDatabaseConnectionStringAsync(gate).ConfigureAwait(false);
        var databaseName = ResolveDatabaseName(databaseConnectionString);
        var suffix = Guid.NewGuid().ToString("N")[..12];
        schemaName = $"cephalon_cdc_{suffix}";
        publicationName = $"cephalon_cdc_pub_{suffix}";
        slotName = $"cephalon_cdc_slot_{suffix}";
        await CreateCdcObjectsAsync(databaseConnectionString, schemaName, publicationName).ConfigureAwait(false);

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
            engine.AddPostgresData(
                connectionString: databaseConnectionString,
                databaseName: databaseName,
                configure: options =>
                {
                    options.CdcCaptures.Add(new PostgresLogicalReplicationCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "PostgreSQL Orders CDC",
                        Description = "Captures PostgreSQL order changes through a live provider-native logical-replication runner.",
                        SourceModuleId = "platform",
                        PublicationName = publicationName,
                        SlotName = slotName,
                        TableSchema = schemaName,
                        TableName = TableName,
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.postgresql.changed",
                        InitialPosition = "slot-consistent-point",
                        CreateSlotIfMissing = true,
                        RecreateSlotIfInvalidated = true,
                        PollingIntervalSeconds = 1,
                        MaxChangesPerRead = 64,
                        MaxAwaitTimeSeconds = 10
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
            await WaitForAsync(
                    () => SlotExistsAsync(databaseConnectionString, slotName),
                    static exists => exists,
                    TimeSpan.FromSeconds(30))
                .ConfigureAwait(false);

            await InsertOrderAsync(databaseConnectionString, schemaName, "order-001", "created").ConfigureAwait(false);
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
            Assert.Equal(PostgresRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.Equal(1, state.LastCapturedChangeCount);
            Assert.Equal(1, state.LastProducedMessageCount);
            Assert.True(state.TotalCapturedChangeCount >= 1);
            Assert.True(state.TotalProducedMessageCount >= 1);
            Assert.False(string.IsNullOrWhiteSpace(state.LastCheckpoint));
            Assert.False(string.IsNullOrWhiteSpace(state.LastChangeId));
            Assert.Equal("postgresql-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(PostgresRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal("insert", state.Metadata["lastOperationType"]);
            Assert.Equal(publicationName, state.Metadata["publicationName"]);
            Assert.Equal(slotName, state.Metadata["slotName"]);
            Assert.Equal("slot-confirmed-flush-lsn", state.Metadata["replicationCheckpointSource"]);
            Assert.Equal("created", state.Metadata["slotLifecycleState"]);
            Assert.Equal("create", state.Metadata["slotLifecycleAction"]);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            var stagedMessage = Assert.Single(executionState.StagedMessages);
            Assert.Equal("orders", stagedMessage.ChannelId);
            Assert.Equal("orders.postgresql.changed", stagedMessage.MessageType);
            Assert.Equal("application/vnd.cephalon.postgresql.logical-replication+json", stagedMessage.ContentType);
            Assert.Equal(PostgresDataOptions.ProviderId, stagedMessage.Headers["provider"]);
            Assert.Equal(CaptureId, stagedMessage.Headers["cdcCaptureId"]);
            Assert.Equal(databaseName, stagedMessage.Headers["databaseName"]);
            Assert.Equal(schemaName, stagedMessage.Headers["schemaName"]);
            Assert.Equal(TableName, stagedMessage.Headers["tableName"]);
            Assert.Equal(publicationName, stagedMessage.Headers["publicationName"]);
            Assert.Equal(slotName, stagedMessage.Headers["slotName"]);
            Assert.Equal("insert", stagedMessage.Headers["operation"]);

            using var payload = JsonDocument.Parse(stagedMessage.Payload);
            Assert.Equal("order-001", payload.RootElement.GetProperty("data").GetProperty("order_id").GetString());
            Assert.Equal("created", payload.RootElement.GetProperty("data").GetProperty("status").GetString());

            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(PostgresRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("postgres-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal(publicationName, capture.Metadata["publicationName"]);
            Assert.Equal(slotName, capture.Metadata["slotName"]);
            Assert.Equal("true", capture.Metadata["createSlotIfMissing"]);
            Assert.Equal("true", capture.Metadata["recreateSlotIfInvalidated"]);

            Assert.Empty(runtimeCatalog.GetById(SharedRuntimeId)!.CdcCaptureIds);
            var postgresRuntime = runtimeCatalog.GetById(PostgresRuntimeId);
            Assert.NotNull(postgresRuntime);
            Assert.Equal("host-managed", postgresRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", postgresRuntime.ExecutionTopology);
            Assert.Equal("provider-native", postgresRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], postgresRuntime.CdcCaptureIds);
            Assert.True(postgresRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, postgresRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, postgresRuntime.Summary.LastOutcome);
            Assert.True(postgresRuntime.Summary.TotalCapturedChangeCount >= 1);
            Assert.True(postgresRuntime.Summary.TotalProducedMessageCount >= 1);
            Assert.Equal("provider-native", postgresRuntime.Summary.LastAcknowledgement);

            var checkpointParts = state.LastCheckpoint.Split('|', StringSplitOptions.TrimEntries);
            Assert.Equal(3, checkpointParts.Length);
            Assert.Equal(slotName, checkpointParts[0]);
            var confirmedFlushLsn = await WaitForAsync(
                    () => ReadConfirmedFlushLsnAsync(databaseConnectionString, slotName),
                    value => string.Equals(value, checkpointParts[2], StringComparison.OrdinalIgnoreCase),
                    TimeSpan.FromSeconds(30))
                .ConfigureAwait(false);
            Assert.Equal(checkpointParts[2], confirmedFlushLsn, ignoreCase: true);
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
        return gate.ResolvePostgresMode() switch
        {
            ExternalCdcServiceMode.PreProvisionedConnectionString => NormalizeConnectionString(gate.PostgresConnectionString!),
            ExternalCdcServiceMode.Testcontainers => await StartContainerAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalCdcServiceGate.SkipReason)
        };
    }

    private async Task<string> StartContainerAsync()
    {
        container = new PostgreSqlBuilder(PostgresImage)
            .WithDatabase("cephalon_cdc")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCommand(
                "-c",
                "wal_level=logical",
                "-c",
                "max_wal_senders=10",
                "-c",
                "max_replication_slots=10")
            .Build();

        await container.StartAsync().ConfigureAwait(false);
        return NormalizeConnectionString(container.GetConnectionString());
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            builder.Database = string.IsNullOrWhiteSpace(builder.Username) ? "postgres" : builder.Username;
        }

        return builder.ConnectionString;
    }

    private static async Task CreateCdcObjectsAsync(string connectionString, string schemaName, string publicationName)
    {
        await ExecuteNonQueryAsync(
                connectionString,
                $"""
CREATE SCHEMA {EscapeIdentifier(schemaName)};

CREATE TABLE {EscapeIdentifier(schemaName)}.{EscapeIdentifier(TableName)}
(
    id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    order_id text NOT NULL,
    status text NOT NULL,
    created_at_utc timestamptz NOT NULL DEFAULT now()
);

CREATE PUBLICATION {EscapeIdentifier(publicationName)}
FOR TABLE {EscapeIdentifier(schemaName)}.{EscapeIdentifier(TableName)};
""")
            .ConfigureAwait(false);
    }

    private static async Task InsertOrderAsync(
        string connectionString,
        string schemaName,
        string orderId,
        string status)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
INSERT INTO {EscapeIdentifier(schemaName)}.{EscapeIdentifier(TableName)} (order_id, status)
VALUES (@orderId, @status);
""";
        command.Parameters.AddWithValue("orderId", orderId);
        command.Parameters.AddWithValue("status", status);
        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<bool> SlotExistsAsync(string connectionString, string slotName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_replication_slots
    WHERE slot_name = @slotName
);
""";
        command.Parameters.AddWithValue("slotName", slotName);
        return (bool)(await command.ExecuteScalarAsync().ConfigureAwait(false) ?? false);
    }

    private static async Task<string?> ReadConfirmedFlushLsnAsync(string connectionString, string slotName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT confirmed_flush_lsn::text
FROM pg_catalog.pg_replication_slots
WHERE slot_name = @slotName;
""";
        command.Parameters.AddWithValue("slotName", slotName);
        return await command.ExecuteScalarAsync().ConfigureAwait(false) as string;
    }

    private static async Task DropCdcObjectsAsync(
        string connectionString,
        string? schemaName,
        string? publicationName,
        string? slotName)
    {
        if (!string.IsNullOrWhiteSpace(slotName))
        {
            await TryCleanupAsync(() => DropReplicationSlotAsync(connectionString, slotName)).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(publicationName))
        {
            await TryCleanupAsync(() => ExecuteNonQueryAsync(
                    connectionString,
                    $"DROP PUBLICATION IF EXISTS {EscapeIdentifier(publicationName)};"))
                .ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            await TryCleanupAsync(() => ExecuteNonQueryAsync(
                    connectionString,
                    $"DROP SCHEMA IF EXISTS {EscapeIdentifier(schemaName)} CASCADE;"))
                .ConfigureAwait(false);
        }
    }

    private static async Task TryCleanupAsync(Func<Task> cleanup)
    {
        try
        {
            await cleanup().ConfigureAwait(false);
        }
        catch (NpgsqlException)
        {
            // Cleanup is best-effort because external pre-provisioned services may restrict publication or slot permissions.
        }
    }

    private static async Task DropReplicationSlotAsync(string connectionString, string slotName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT pg_drop_replication_slot(slot_name)
FROM pg_catalog.pg_replication_slots
WHERE slot_name = @slotName
  AND active = false;
""";
        command.Parameters.AddWithValue("slotName", slotName);
        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task ExecuteNonQueryAsync(string connectionString, string commandText)
    {
        await using var connection = new NpgsqlConnection(connectionString);
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

        throw new TimeoutException("Timed out while waiting for the expected PostgreSQL CDC integration condition.");
    }

    private static string ResolveDatabaseName(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(builder.Database))
        {
            return builder.Database;
        }

        return string.IsNullOrWhiteSpace(builder.Username) ? "postgres" : builder.Username;
    }

    private static string EscapeIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return "\"" + value.Trim().Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
