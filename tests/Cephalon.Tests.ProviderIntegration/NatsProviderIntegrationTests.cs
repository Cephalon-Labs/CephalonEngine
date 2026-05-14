using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.Nats.Hosting;
using Cephalon.EventSourcing.Services;
using Cephalon.Tests.ProviderIntegration.ExternalServices;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace Cephalon.Tests.ProviderIntegration;

public sealed class NatsProviderIntegrationTests : IAsyncLifetime, IDisposable
{
    private readonly ExternalProviderTestcontainerRuntime _testcontainerRuntime = new();
    private bool _disposed;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _testcontainerRuntime.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _testcontainerRuntime.DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Nats)]
    public async Task NatsProvider_ReplaysEventStreamSnapshotsAgainstLiveJetStream()
    {
        var service = await _testcontainerRuntime.ResolveNatsAsync(ExternalProviderServiceGate.FromEnvironment()).ConfigureAwait(false);
        var uniqueId = Guid.NewGuid().ToString("N");
        var bucketName = $"cephalon-it-{uniqueId}-events";
        var streamId = $"orders-{uniqueId}";

        var services = new ServiceCollection();
        services.AddCephalonEventSourcing(options =>
        {
            options.DefaultProvider = "nats";
            options.EnableSnapshots = true;
        });
        services.AddCephalonEventType<NatsProviderEvent>("tests.nats-provider-event");
        services.AddSingleton<RecordingNatsProjection>();
        services.AddSingleton<IProjection<IDomainEvent>>(serviceProvider =>
            serviceProvider.GetRequiredService<RecordingNatsProjection>());
        services.AddCephalonNatsEventSourcing(service.Uri, bucketName);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "EventSourcing"],
                technologies: ["EventDrivenIntegration"]));
            engine.AddModule(new ProviderIntegrationModule());
        });

        await using var provider = services.BuildServiceProvider();
        try
        {
            var eventStream = Assert.Single(provider.GetRequiredService<IEventStoreCatalog>().All);
            Assert.Equal("nats-event-store", eventStream.Id);
            Assert.Equal("nats", eventStream.Provider);
            Assert.Equal(bucketName, eventStream.Metadata["bucketName"]);
            Assert.Equal(bucketName, eventStream.Metadata["snapshotBucketName"]);
            Assert.Equal("snapshots/", eventStream.Metadata["snapshotKeyPrefix"]);
            Assert.Equal("NATS JetStream KV latest-snapshot", eventStream.Metadata["snapshotStorage"]);
            Assert.Equal("provider-durable", eventStream.Metadata["snapshotLifecycle"]);
            Assert.Equal("revision-compare-and-set", eventStream.Metadata["snapshotConcurrency"]);

            await using var scope = provider.CreateAsyncScope();
            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
            var snapshotStore = scope.ServiceProvider.GetRequiredService<ISnapshotStore>();
            var replayWorker = scope.ServiceProvider.GetRequiredService<IEventStreamReplayWorker>();
            var projection = scope.ServiceProvider.GetRequiredService<RecordingNatsProjection>();

            Assert.Equal(-1, await eventStore.GetVersionAsync(streamId).ConfigureAwait(false));
            await eventStore.AppendAsync(
                    streamId,
                    [
                        new NatsProviderEvent(streamId, 0, new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc)),
                        new NatsProviderEvent(streamId, 1, new DateTime(2026, 5, 15, 0, 1, 0, DateTimeKind.Utc))
                    ],
                    -1)
                .ConfigureAwait(false);

            await snapshotStore.SaveSnapshotAsync(streamId, 1, 2).ConfigureAwait(false);
            var initialSnapshot = await snapshotStore.LoadSnapshotAsync<int>(streamId).ConfigureAwait(false);
            Assert.Equal(2, initialSnapshot.State);
            Assert.Equal(1, initialSnapshot.Version);

            await eventStore.AppendAsync(
                    streamId,
                    [
                        new NatsProviderEvent(streamId, 2, new DateTime(2026, 5, 15, 0, 2, 0, DateTimeKind.Utc)),
                        new NatsProviderEvent(streamId, 3, new DateTime(2026, 5, 15, 0, 3, 0, DateTimeKind.Utc))
                    ],
                    1)
                .ConfigureAwait(false);

            var replayed = new List<IDomainEvent>();
            await foreach (var evt in eventStore.ReadStreamAsync(streamId).ConfigureAwait(false))
            {
                replayed.Add(evt);
            }

            Assert.Collection(
                replayed,
                static evt => Assert.Equal(0, evt.StreamVersion),
                static evt => Assert.Equal(1, evt.StreamVersion),
                static evt => Assert.Equal(2, evt.StreamVersion),
                static evt => Assert.Equal(3, evt.StreamVersion));
            Assert.All(replayed, static evt => Assert.IsType<NatsProviderEvent>(evt));
            Assert.Equal(3, await eventStore.GetVersionAsync(streamId).ConfigureAwait(false));

            var replay = await replayWorker
                .ReplayAggregateAsync<NatsProviderAggregate, int>(
                    eventStore,
                    new EventStreamReplayRequest(streamId))
                .ConfigureAwait(false);

            Assert.Equal(4, replay.State);
            Assert.Equal("passed", replay.Report.Status);
            Assert.True(replay.Report.UsedSnapshot);
            Assert.Equal(1, replay.Report.SnapshotVersion);
            Assert.Equal(2, replay.Report.ReplayFromVersion);
            Assert.Equal(2, replay.Report.ReplayedEventCount);
            Assert.Equal(1, replay.Report.ProjectionCount);
            Assert.Equal(2, replay.Report.ProjectedEventCount);
            Assert.Equal(3, replay.Report.LastReplayedVersion);
            Assert.True(replay.Report.SnapshotSaved);
            Assert.Equal([2L, 3L], projection.ProjectedVersions);

            var finalSnapshot = await snapshotStore.LoadSnapshotAsync<int>(streamId).ConfigureAwait(false);
            Assert.Equal(4, finalSnapshot.State);
            Assert.Equal(3, finalSnapshot.Version);

            var nats = provider.GetRequiredService<INatsConnection>();
            var kv = await GetStoreAsync(nats, bucketName).ConfigureAwait(false);
            var snapshotKey = CreateSnapshotKey<int>(streamId);
            var rawSnapshot = await kv.TryGetEntryAsync<byte[]>(snapshotKey).ConfigureAwait(false);
            Assert.True(rawSnapshot.Success);
            Assert.NotNull(rawSnapshot.Value.Value);
            using var document = JsonDocument.Parse(rawSnapshot.Value.Value);
            Assert.Equal(streamId, document.RootElement.GetProperty("StreamId").GetString());
            Assert.Equal(3, document.RootElement.GetProperty("StreamVersion").GetInt64());
            Assert.Equal("4", document.RootElement.GetProperty("Payload").GetString());

            var runtimeSurface = scope.ServiceProvider
                .GetServices<ITechnologyRuntimeContributor>()
                .Select(static contributor => contributor.DescribeRuntimeSurface())
                .Single(static surface => surface.TechnologyId == "event-sourcing");
            var summary = Assert.Single(runtimeSurface.Entries, static entry => entry.Id == "event-sourcing-runtime");
            Assert.Equal("provider-durable", summary.Metadata["snapshotLifecycle"]);
            Assert.Equal("nats", summary.Metadata["providerDurableSnapshotProviders"]);
            Assert.Equal("passed", summary.Metadata["latestReplayStatus"]);
            Assert.Equal("true", summary.Metadata["latestReplayUsedSnapshot"]);
            Assert.Equal("1", summary.Metadata["latestReplaySnapshotVersion"]);
            Assert.Equal("2", summary.Metadata["latestReplayFromVersion"]);
            Assert.Equal("3", summary.Metadata["latestReplayLastVersion"]);

            var replayEntry = Assert.Single(runtimeSurface.Entries, static entry => entry.Id == "event-sourcing-managed-replay-worker");
            Assert.Equal("provider-durable", replayEntry.Metadata["snapshotAssistedReplay"]);
            Assert.Equal("claimed", replayEntry.Metadata["providerDurableSnapshots"]);
            Assert.Equal("nats", replayEntry.Metadata["providerDurableSnapshotProviders"]);

            var natsEntry = Assert.Single(runtimeSurface.Entries, entry =>
                entry.Metadata.TryGetValue("provider", out var providerId) &&
                providerId == "nats");
            Assert.Equal("provider-durable", natsEntry.Metadata["provider.snapshotLifecycle"]);
            Assert.Equal("NATS JetStream KV latest-snapshot", natsEntry.Metadata["provider.snapshotStorage"]);
            Assert.Equal("snapshots/", natsEntry.Metadata["provider.snapshotKeyPrefix"]);
            Assert.Equal("revision-compare-and-set", natsEntry.Metadata["provider.snapshotConcurrency"]);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    snapshotStore.SaveSnapshotAsync(streamId, 2, 3))
                .ConfigureAwait(false);

            var exception = await Assert.ThrowsAsync<EventStreamConcurrencyException>(() =>
                    eventStore.AppendAsync(
                        streamId,
                        [new NatsProviderEvent(streamId, 4, new DateTime(2026, 5, 15, 0, 4, 0, DateTimeKind.Utc))],
                        1))
                .ConfigureAwait(false);

            Assert.Equal(streamId, exception.StreamId);
            Assert.Equal(1, exception.ExpectedVersion);
            Assert.Equal(3, exception.ActualVersion);
        }
        finally
        {
            await DeleteStoreIfCreatedAsync(provider, bucketName).ConfigureAwait(false);
        }
    }

    private static async Task<INatsKVStore> GetStoreAsync(INatsConnection nats, string bucketName)
    {
        var js = new NatsJSContext(nats);
        var kvCtx = new NatsKVContext(js);
        return await kvCtx.GetStoreAsync(bucketName).ConfigureAwait(false);
    }

    private static async Task DeleteStoreIfCreatedAsync(IServiceProvider provider, string bucketName)
    {
        var nats = provider.GetService<INatsConnection>();
        if (nats is null)
        {
            return;
        }

        try
        {
            var js = new NatsJSContext(nats);
            var kvCtx = new NatsKVContext(js);
            await kvCtx.DeleteStoreAsync(bucketName).ConfigureAwait(false);
        }
        catch (NatsKVException)
        {
        }
    }

    private static string CreateSnapshotKey<TState>(string streamId)
    {
        var type = typeof(TState);
        var assemblyName = type.Assembly.GetName().Name;
        var typeName = type.FullName ?? type.Name;
        var stateType = string.IsNullOrWhiteSpace(assemblyName)
            ? typeName
            : string.Concat(assemblyName, ":", typeName);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(stateType));
        var keyComponent = encoded.TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"snapshots/{streamId}/{keyComponent}";
    }

    private sealed record NatsProviderEvent(
        string StreamId,
        long StreamVersion,
        DateTime OccurredAtUtc) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

    private sealed class NatsProviderAggregate : IAggregate<int>
    {
        public int Apply(int current, IDomainEvent evt) =>
            evt is NatsProviderEvent ? current + 1 : current;
    }

    private sealed class RecordingNatsProjection : IProjection<IDomainEvent>
    {
        public List<long> ProjectedVersions { get; } = [];

        public ValueTask ProjectAsync(IDomainEvent message, CancellationToken cancellationToken = default)
        {
            ProjectedVersions.Add(message.StreamVersion);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ProviderIntegrationModule : ModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "provider-integration-test",
            displayName: "Provider Integration Test",
            description: "Test module used to bind provider-integration runtime composition.",
            tags: ["test", "provider-integration"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;
    }
}
