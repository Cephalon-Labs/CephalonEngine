using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Data.Redis.Configuration;
using Cephalon.Data.Redis.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.EventSourcing.Redis;
using Cephalon.EventSourcing.Redis.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Cephalon.Tests.Composition;

/// <summary>
/// Composition and capability tests for the Redis data companion pack and Redis event-store provider.
/// StackExchange.Redis connects lazily, so <c>abortConnect=false</c> allows service resolution tests to run
/// without a live Redis server — operations are deferred until the first actual Redis command is issued.
/// </summary>
public sealed class RedisDataPackTests
{
    // abortConnect=false: StackExchange.Redis will not throw during Connect() if Redis is unreachable.
    // Services can be registered and resolved — actual network I/O is deferred to the first command.
    private const string OfflineConnectionString = "localhost:6379,abortConnect=false";

    [Fact]
    public void AddRedisData_RegistersConnectionMultiplexer()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Redis")));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(OfflineConnectionString);
        });

        using var provider = services.BuildServiceProvider();
        var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
        Assert.NotNull(multiplexer);
    }

    [Fact]
    public void AddRedisData_WithConnectionStringName_ResolvesFromConnectionStrings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RedisPrimary"] = OfflineConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Redis")));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(options =>
            {
                options.ConnectionStringName = "RedisPrimary";
            });
        });

        using var provider = services.BuildServiceProvider();
        var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();

        Assert.NotNull(multiplexer);
    }

    [Fact]
    public void AddRedisData_WithConnectionStringAndName_FailsFast()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RedisPrimary"] = OfflineConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Redis")));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(options =>
            {
                options.ConnectionStringName = "RedisPrimary";
                options.ConnectionString = OfflineConnectionString;
            });
        });

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IConnectionMultiplexer>());

        Assert.Contains("either ConnectionStringName or ConnectionString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRedisData_RegistersOutbox_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Redis", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(OfflineConnectionString, configure: options =>
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
    public void AddRedisData_WithEventDrivenIntegration_RegistersConsumerManagedDispatchStore()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                data: new DataSettings(provider: "Redis", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "test-events",
                    displayName: "Test Events",
                    description: "Dispatch-store test channel."));
            });
            engine.AddRedisData(OfflineConnectionString, configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();
        var descriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("consumer-managed", descriptor.DispatchPolicy.PolicyId);
        Assert.Equal("consumer-managed", descriptor.DispatchPolicy.ExecutionMode);

        using var scope = provider.CreateScope();
        var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        Assert.NotNull(dispatchStore);
        Assert.Equal("redis-outbox", Assert.Single(dispatchStore.OutboxIds));
    }

    [Fact]
    public void AddRedisData_RegistersInbox_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Redis")));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(OfflineConnectionString, configure: options =>
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
    public void RedisDataModule_ExposesCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Redis", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(OfflineConnectionString, configure: options =>
            {
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.redis");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.key-value-store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.outbox.redis");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox.redis");
    }

    [Fact]
    public void RedisDataModule_RegistersOutboxDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Redis", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(OfflineConnectionString, configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();

        var outboxDescriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("redis-outbox", outboxDescriptor.Id);
        Assert.Equal(RedisDataOptions.ProviderId, outboxDescriptor.Provider);
        Assert.Equal("sorted-set", outboxDescriptor.Mode);
    }

    [Fact]
    public void RedisDataModule_RegistersInboxDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Redis")));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(OfflineConnectionString, configure: options =>
            {
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var inboxCatalog = provider.GetRequiredService<IInboxCatalog>();

        var inboxDescriptor = Assert.Single(inboxCatalog.Inboxes);
        Assert.Equal("redis-inbox", inboxDescriptor.Id);
        Assert.Equal(RedisDataOptions.ProviderId, inboxDescriptor.Provider);
        Assert.Equal("set", inboxDescriptor.Mode);
        Assert.Equal("message-id", inboxDescriptor.Metadata["idempotency"]);
    }

    [Fact]
    public void AddCephalonRedisEventSourcing_RegistersEventStore()
    {
        var services = new ServiceCollection();
        services.AddCephalonRedisEventSourcing(OfflineConnectionString);

        using var provider = services.BuildServiceProvider();
        var eventStore = provider.GetRequiredService<IEventStore>();
        var snapshotStore = provider.GetRequiredService<ISnapshotStore>();
        Assert.NotNull(eventStore);
        Assert.NotNull(snapshotStore);
        Assert.IsType<RedisEventStore>(eventStore);
    }

    [Fact]
    public void AddRedisData_WithCustomKeyPrefix_PropagatesPrefix()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Redis")));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(OfflineConnectionString, configure: options =>
            {
                options.KeyPrefix = "myapp:";
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();

        var descriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("myapp:", descriptor.Metadata["keyPrefix"]);
    }

    [Fact]
    public void RedisDataModule_WithoutOutboxOrInbox_PublishesBaseCapabilitiesOnly()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Redis")));
            engine.AddModule(new PlatformTestModule());
            engine.AddRedisData(OfflineConnectionString);
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.redis");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.key-value-store");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, c => c.Key == "data.outbox.redis");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, c => c.Key == "data.inbox.redis");
    }
}
