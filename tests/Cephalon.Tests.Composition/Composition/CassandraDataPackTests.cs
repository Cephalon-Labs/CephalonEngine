using Cassandra;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Data.Cassandra.Configuration;
using Cephalon.Data.Cassandra.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.EventSourcing.Cassandra;
using Cephalon.EventSourcing.Cassandra.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

/// <summary>
/// Composition and capability tests for the Cassandra data companion pack and Cassandra event-store provider.
/// The CassandraCSharpDriver creates <see cref="ICluster" /> via <c>Cluster.Builder()...Build()</c> without
/// opening any socket — the connection is deferred until <c>cluster.ConnectAsync()</c> is called for the
/// first time. This allows service registration and resolution tests to run without a live Cassandra server.
/// </summary>
public sealed class CassandraDataPackTests
{
    private const string OfflineContactPoints = "localhost";
    private const string OfflineKeyspace = "cephalon_test";

    [Fact]
    public void AddCassandraData_RegistersCluster()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Cassandra")));
            engine.AddModule(new PlatformTestModule());
            engine.AddCassandraData(OfflineContactPoints, OfflineKeyspace);
        });

        using var provider = services.BuildServiceProvider();
        var cluster = provider.GetRequiredService<ICluster>();
        Assert.NotNull(cluster);
    }

    [Fact]
    public void AddCassandraData_RegistersOutbox_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Cassandra", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddCassandraData(OfflineContactPoints, OfflineKeyspace, configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        Assert.NotNull(outbox);
    }

    [Fact]
    public void AddCassandraData_RegistersInbox_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Cassandra")));
            engine.AddModule(new PlatformTestModule());
            engine.AddCassandraData(OfflineContactPoints, OfflineKeyspace, configure: options =>
            {
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        Assert.NotNull(inbox);
    }

    [Fact]
    public void CassandraDataModule_ExposesCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Cassandra", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddCassandraData(OfflineContactPoints, OfflineKeyspace, configure: options =>
            {
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.cassandra");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.wide-column-store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.outbox.cassandra");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox.cassandra");
    }

    [Fact]
    public void CassandraDataModule_RegistersOutboxDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Cassandra", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddCassandraData(OfflineContactPoints, OfflineKeyspace, configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();

        var descriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("cassandra-outbox", descriptor.Id);
        Assert.Equal(CassandraDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("wide-column-lwt", descriptor.Mode);
    }

    [Fact]
    public void CassandraDataModule_RegistersInboxDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Cassandra")));
            engine.AddModule(new PlatformTestModule());
            engine.AddCassandraData(OfflineContactPoints, OfflineKeyspace, configure: options =>
            {
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var inboxCatalog = provider.GetRequiredService<IInboxCatalog>();

        var descriptor = Assert.Single(inboxCatalog.Inboxes);
        Assert.Equal("cassandra-inbox", descriptor.Id);
        Assert.Equal(CassandraDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("wide-column-lwt", descriptor.Mode);
        Assert.Equal("lwt-if-not-exists", descriptor.Metadata["idempotency"]);
    }

    [Fact]
    public void AddCephalonCassandraEventSourcing_RegistersEventStore()
    {
        var services = new ServiceCollection();
        services.AddCephalonCassandraEventSourcing(OfflineContactPoints, OfflineKeyspace);

        using var provider = services.BuildServiceProvider();
        var eventStore = provider.GetRequiredService<IEventStore>();
        Assert.NotNull(eventStore);
        Assert.IsType<CassandraEventStore>(eventStore);
    }

    [Fact]
    public void CassandraDataModule_WithoutOutboxOrInbox_PublishesBaseCapabilitiesOnly()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Cassandra")));
            engine.AddModule(new PlatformTestModule());
            engine.AddCassandraData(OfflineContactPoints, OfflineKeyspace);
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.cassandra");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.wide-column-store");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, c => c.Key == "data.outbox.cassandra");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, c => c.Key == "data.inbox.cassandra");
    }
}
