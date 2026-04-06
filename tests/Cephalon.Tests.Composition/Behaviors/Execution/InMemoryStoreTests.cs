using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Stores;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="InMemorySagaStateStore"/> and <see cref="InMemoryProcessCheckpointStore"/>.</summary>
public sealed class InMemoryStoreTests
{
    // ─── InMemorySagaStateStore ───────────────────────────────────────────────

    [Fact]
    public async Task InMemorySagaStateStore_GetAsync_ReturnsNull_WhenKeyNotFound()
    {
        var store = new InMemorySagaStateStore();

        var result = await store.GetAsync<string>("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task InMemorySagaStateStore_SaveAsync_IsUpsert()
    {
        var store = new InMemorySagaStateStore();

        await store.SaveAsync("key1", "first");
        await store.SaveAsync("key1", "second");

        var result = await store.GetAsync<string>("key1");
        Assert.Equal("second", result);
    }

    [Fact]
    public async Task InMemorySagaStateStore_GetSaveDelete_RoundTrip()
    {
        var store = new InMemorySagaStateStore();

        await store.SaveAsync("k", "state-value");
        var found = await store.GetAsync<string>("k");
        Assert.Equal("state-value", found);

        await store.DeleteAsync("k");
        var afterDelete = await store.GetAsync<string>("k");
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task InMemorySagaStateStore_DeleteAsync_NonExistentKey_DoesNotThrow()
    {
        var store = new InMemorySagaStateStore();

        // Should not throw for missing keys.
        await store.DeleteAsync("nope");
    }

    [Fact]
    public async Task InMemorySagaStateStore_SerializesComplexTypes()
    {
        var store = new InMemorySagaStateStore();
        var data = new SagaData { Step = "step-2", Counter = 42 };

        await store.SaveAsync("complex", data);
        var loaded = await store.GetAsync<SagaData>("complex");

        Assert.NotNull(loaded);
        Assert.Equal("step-2", loaded.Step);
        Assert.Equal(42, loaded.Counter);
    }

    private sealed class SagaData
    {
        public string Step { get; set; } = string.Empty;
        public int Counter { get; set; }
    }

    // ─── InMemoryProcessCheckpointStore ──────────────────────────────────────

    [Fact]
    public async Task InMemoryProcessCheckpointStore_GetAsync_ReturnsNull_WhenKeyNotFound()
    {
        var store = new InMemoryProcessCheckpointStore();

        var result = await store.GetAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task InMemoryProcessCheckpointStore_SaveAsync_IsUpsert()
    {
        var store = new InMemoryProcessCheckpointStore();
        var first = MakeCheckpoint("p1", "step-a");
        var second = MakeCheckpoint("p1", "step-b");

        await store.SaveAsync("p1", first);
        await store.SaveAsync("p1", second);

        var result = await store.GetAsync("p1");
        Assert.Equal("step-b", result!.CurrentStep);
    }

    [Fact]
    public async Task InMemoryProcessCheckpointStore_GetSaveDelete_RoundTrip()
    {
        var store = new InMemoryProcessCheckpointStore();
        var cp = MakeCheckpoint("p2", "step-1");

        await store.SaveAsync("p2", cp);
        var found = await store.GetAsync("p2");
        Assert.NotNull(found);
        Assert.Equal("step-1", found.CurrentStep);

        await store.DeleteAsync("p2");
        var afterDelete = await store.GetAsync("p2");
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task InMemoryProcessCheckpointStore_DeleteAsync_NonExistentKey_DoesNotThrow()
    {
        var store = new InMemoryProcessCheckpointStore();

        // Should not throw for missing keys.
        await store.DeleteAsync("nope");
    }

    private static ProcessCheckpoint MakeCheckpoint(string id, string step) =>
        new()
        {
            ProcessId = id,
            CurrentStep = step,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
