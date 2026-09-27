using System.Data.Common;
using Cephalon.Abstractions.Data;
using Cephalon.Data.EntityFramework.Modeling;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class EntityFrameworkJournalCursorTests
{
    private static readonly string[] CursorColumns = ["command_id", "observed_at_utc"];

    [Fact]
    public async Task LatestCursorReadsOnlyItsColumnsAndObservesOtherScopesWithoutTrackingJournalEntries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var reads = new CursorReadObserver();
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options => options.Channels.Add(new EventChannelDescriptor(
                id: "cursor-events", displayName: "Cursor events", description: "Relational journal cursor proof.")));
            engine.AddEntityFrameworkData<CursorDbContext>(
                options => options.UseSqlite(connection).AddInterceptors(reads),
                options => options.RegisterOutbox = true);
        });
        await using var provider = services.BuildServiceProvider();
        using var readerScope = provider.CreateScope();
        var readerContext = readerScope.ServiceProvider.GetRequiredService<CursorDbContext>();
        await readerContext.Database.EnsureCreatedAsync();
        var catalog = (IEventDispatchRemediationCommandReplayCursorCatalog)readerScope.ServiceProvider
            .GetRequiredService<IEventDispatchRemediationCommandJournal>();
        reads.Columns.Clear();

        Assert.Null(catalog.LatestReplayCursor);
        var timestamp = new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero);
        using (var writerScope = provider.CreateScope())
        {
            var context = writerScope.ServiceProvider.GetRequiredService<CursorDbContext>();
            context.EventDispatchRemediationCommandJournalEntries.AddRange(
                Entry("cmd-a", timestamp), Entry("cmd-z", timestamp), Entry("cmd-zz", timestamp.AddSeconds(-1)));
            await context.SaveChangesAsync();
        }

        Assert.Equal(new EventDispatchRemediationCommandReplayCursor(timestamp, "cmd-z"), catalog.LatestReplayCursor);
        using (var writerScope = provider.CreateScope())
        {
            var context = writerScope.ServiceProvider.GetRequiredService<CursorDbContext>();
            context.EventDispatchRemediationCommandJournalEntries.Add(Entry("cmd-new", timestamp.AddSeconds(1)));
            await context.SaveChangesAsync();
        }

        Assert.Equal(new EventDispatchRemediationCommandReplayCursor(timestamp.AddSeconds(1), "cmd-new"), catalog.LatestReplayCursor);
        Assert.Empty(readerContext.ChangeTracker.Entries());
        // Synchronous readers here are only the three public latest-cursor reads;
        // schema creation and writes above use EF's async path.
        Assert.Equal(3, reads.Columns.Count);
        Assert.All(reads.Columns, columns =>
            Assert.Equal(CursorColumns, columns.Order(StringComparer.Ordinal)));
    }

    private static EntityFrameworkEventDispatchRemediationCommandEntry Entry(string commandId, DateTimeOffset timestamp) => new()
    {
        CommandId = commandId,
        ObservedAtUtc = timestamp,
        MetadataJson = "{\"payload\":\"" + new string('x', 64 * 1024) + "\"}"
    };

    private sealed class CursorReadObserver : DbCommandInterceptor
    {
        public List<string[]> Columns { get; } = [];

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            Columns.Add(Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToArray());
            return result;
        }
    }

    private sealed class CursorDbContext(DbContextOptions<CursorDbContext> options) : DbContext(options),
        IEntityFrameworkOutboxContext, IEntityFrameworkEventDispatchRemediationCommandJournalContext
    {
        public DbSet<EntityFrameworkOutboxEntry> OutboxMessages => Set<EntityFrameworkOutboxEntry>();
        public DbSet<EntityFrameworkEventDispatchRemediationCommandEntry> EventDispatchRemediationCommandJournalEntries =>
            Set<EntityFrameworkEventDispatchRemediationCommandEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureCephalonOutbox();
            modelBuilder.ConfigureCephalonEventDispatchRemediationCommandJournal();
            // SQLite cannot order DateTimeOffset directly. This test-host mapping
            // preserves UTC ordering; it is not a new default provider mapping.
            modelBuilder.Entity<EntityFrameworkEventDispatchRemediationCommandEntry>()
                .Property(entry => entry.ObservedAtUtc)
                .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        }
    }
}
