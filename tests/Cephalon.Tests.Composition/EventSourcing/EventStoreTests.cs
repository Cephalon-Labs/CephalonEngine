using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.EntityFramework;
using Cephalon.EventSourcing.EntityFramework.Hosting;
using Cephalon.EventSourcing.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Cephalon.Tests.EventSourcing;

public sealed class EventStoreTests
{
    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public async Task AppendAndReadStream_RoundTrips()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var services = BuildServices(connection);
        using var scope = services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        await eventStore.AppendAsync("cart-1", [new TestEvent("cart-1", 0, new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc))], -1);
        await eventStore.AppendAsync("cart-1", [new TestEvent("cart-1", 1, new DateTime(2026, 4, 6, 0, 1, 0, DateTimeKind.Utc))], 0);
        await eventStore.AppendAsync("cart-1", [new TestEvent("cart-1", 2, new DateTime(2026, 4, 6, 0, 2, 0, DateTimeKind.Utc))], 1);

        var results = new List<IDomainEvent>();
        await foreach (var evt in eventStore.ReadStreamAsync("cart-1", 0))
        {
            results.Add(evt);
        }

        Assert.Collection(
            results,
            static evt => Assert.Equal(0, evt.StreamVersion),
            static evt => Assert.Equal(1, evt.StreamVersion),
            static evt => Assert.Equal(2, evt.StreamVersion));
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public async Task StreamVersion_IsMonotonicallyIncremented()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var services = BuildServices(connection);
        using var scope = services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        await eventStore.AppendAsync("cart-2", [new TestEvent("cart-2", 0, new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc))], -1);
        await eventStore.AppendAsync("cart-2", [new TestEvent("cart-2", 1, new DateTime(2026, 4, 6, 0, 1, 0, DateTimeKind.Utc))], 0);

        var version = await eventStore.GetVersionAsync("cart-2");
        Assert.Equal(1, version);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public async Task OptimisticConcurrency_ThrowsOnVersionMismatch()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var services = BuildServices(connection);
        using var scope = services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        var exception = await Assert.ThrowsAsync<EventStreamConcurrencyException>(() =>
            eventStore.AppendAsync("cart-3", [new TestEvent("cart-3", 6, new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc))], 5));

        Assert.Equal("cart-3", exception.StreamId);
        Assert.Equal(5, exception.ExpectedVersion);
        Assert.Equal(-1, exception.ActualVersion);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public async Task AggregateHydrator_AppliesEventsInOrder()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var services = BuildServices(connection);
        using var scope = services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var hydrator = new AggregateHydrator<TestAggregate, int>();

        await eventStore.AppendAsync(
            "cart-4",
            [
                new TestEvent("cart-4", 0, new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc)),
                new TestEvent("cart-4", 1, new DateTime(2026, 4, 6, 0, 1, 0, DateTimeKind.Utc)),
                new TestEvent("cart-4", 2, new DateTime(2026, 4, 6, 0, 2, 0, DateTimeKind.Utc)),
                new TestEvent("cart-4", 3, new DateTime(2026, 4, 6, 0, 3, 0, DateTimeKind.Utc)),
                new TestEvent("cart-4", 4, new DateTime(2026, 4, 6, 0, 4, 0, DateTimeKind.Utc))
            ],
            -1);

        var (state, version) = await hydrator.HydrateAsync(eventStore, "cart-4");
        Assert.Equal(5, state);
        Assert.Equal(4, version);
    }

    private static ServiceProvider BuildServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestEventContext>(options => options.UseSqlite(connection));
        services.AddCephalonEntityFrameworkEventSourcing<TestEventContext>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestEventContext>();
        context.Database.EnsureCreated();
        return provider;
    }

    private sealed class TestEventContext : DbContext, IEntityFrameworkEventContext
    {
        public TestEventContext(DbContextOptions<TestEventContext> options)
            : base(options)
        {
        }

        public DbSet<EntityFrameworkEventEntry> Events => Set<EntityFrameworkEventEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            EntityFrameworkEventSourcingConfiguration.ConfigureCephalonEvents(modelBuilder);
        }
    }

    private sealed record TestEvent(
        string StreamId,
        long StreamVersion,
        DateTime OccurredAtUtc) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

    private sealed class TestAggregate : IAggregate<int>
    {
        public int Apply(int current, IDomainEvent evt)
        {
            ArgumentNullException.ThrowIfNull(evt);
            return current + 1;
        }
    }
}
