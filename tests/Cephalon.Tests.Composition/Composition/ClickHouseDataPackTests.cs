using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Data.ClickHouse.Configuration;
using Cephalon.Data.ClickHouse.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.EventSourcing.ClickHouse;
using Cephalon.EventSourcing.ClickHouse.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

/// <summary>
/// Composition and capability tests for the ClickHouse data companion pack and ClickHouse event-store provider.
/// <c>ClickHouseConnection</c> creates a lazy ADO.NET connection — no socket is opened until
/// <c>OpenAsync()</c> is called. This allows service registration and resolution tests to run
/// without a live ClickHouse server.
/// </summary>
public sealed class ClickHouseDataPackTests
{
    private const string OfflineHost = "localhost";
    private const string OfflineDatabase = "default";

    [Fact]
    public void AddClickHouseData_RegistersOutbox_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "ClickHouse", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddClickHouseData(OfflineHost, OfflineDatabase, configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        Assert.NotNull(outbox);
        Assert.Contains("ClickHouseOutbox", outbox.GetType().Name, StringComparison.Ordinal);
    }

    [Fact]
    public void AddClickHouseData_RegistersInbox_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "ClickHouse")));
            engine.AddModule(new PlatformTestModule());
            engine.AddClickHouseData(OfflineHost, OfflineDatabase, configure: options =>
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
    public void AddClickHouseData_WithoutOutboxOrInbox_DoesNotRegisterOutboxOrInbox()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "ClickHouse")));
            engine.AddModule(new PlatformTestModule());
            engine.AddClickHouseData(OfflineHost, OfflineDatabase);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetService<IOutbox>();
        var inbox = scope.ServiceProvider.GetService<IInbox>();
        Assert.Null(outbox);
        Assert.Null(inbox);
    }

    [Fact]
    public void ClickHouseDataModule_ExposesCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "ClickHouse", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddClickHouseData(OfflineHost, OfflineDatabase, configure: options =>
            {
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.clickhouse");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.analytics-store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.outbox.clickhouse");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox.clickhouse");
    }

    [Fact]
    public void ClickHouseDataModule_RegistersOutboxDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "ClickHouse", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddClickHouseData(OfflineHost, OfflineDatabase, configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();

        var descriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("clickhouse-outbox", descriptor.Id);
        Assert.Equal(ClickHouseDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("replacing-merge-tree", descriptor.Mode);
    }

    [Fact]
    public void ClickHouseDataModule_RegistersInboxDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "ClickHouse")));
            engine.AddModule(new PlatformTestModule());
            engine.AddClickHouseData(OfflineHost, OfflineDatabase, configure: options =>
            {
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var inboxCatalog = provider.GetRequiredService<IInboxCatalog>();

        var descriptor = Assert.Single(inboxCatalog.Inboxes);
        Assert.Equal("clickhouse-inbox", descriptor.Id);
        Assert.Equal(ClickHouseDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("replacing-merge-tree", descriptor.Mode);
        Assert.Equal("replacing-merge-tree-eventual", descriptor.Metadata["idempotency"]);
    }

    [Fact]
    public void AddCephalonClickHouseEventSourcing_RegistersEventStore()
    {
        var services = new ServiceCollection();
        services.AddCephalonClickHouseEventSourcing(OfflineHost, OfflineDatabase);

        using var provider = services.BuildServiceProvider();
        var eventStore = provider.GetRequiredService<IEventStore>();
        Assert.NotNull(eventStore);
        Assert.IsType<ClickHouseEventStore>(eventStore);
    }

    [Fact]
    public void ClickHouseDataModule_WithoutOutboxOrInbox_PublishesBaseCapabilitiesOnly()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "ClickHouse")));
            engine.AddModule(new PlatformTestModule());
            engine.AddClickHouseData(OfflineHost, OfflineDatabase);
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.clickhouse");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.analytics-store");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, c => c.Key == "data.outbox.clickhouse");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, c => c.Key == "data.inbox.clickhouse");
    }
}
