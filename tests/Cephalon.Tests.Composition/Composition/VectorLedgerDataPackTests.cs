using Cephalon.Abstractions.Data;
using Cephalon.Data.Nats.Configuration;
using Cephalon.Data.Nats.Registration;
using Cephalon.Data.Qdrant.Configuration;
using Cephalon.Data.Qdrant.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class VectorLedgerDataPackTests
{
    // === Qdrant ===

    [Fact]
    public void AddQdrantData_RegistersExpectedCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS"], data: new DataSettings(provider: "Qdrant")));
            engine.AddModule(new PlatformTestModule());
            engine.AddQdrantData("localhost");
        });
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.qdrant");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.vector-store");
    }

    [Fact]
    public void AddQdrantData_WithOutbox_RegistersOutboxCapabilityAndDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS", "Outbox"], data: new DataSettings(provider: "Qdrant", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddQdrantData("localhost", configure: o => { o.RegisterOutbox = true; o.RegisterInbox = true; });
        });
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.outbox.qdrant");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.inbox.qdrant");
    }

    [Fact]
    public void AddQdrantData_OutboxDescriptor_HasExpectedMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS", "Outbox"], data: new DataSettings(provider: "Qdrant", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddQdrantData("localhost", configure: o => o.RegisterOutbox = true);
        });
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IOutboxCatalog>();
        var descriptor = Assert.Single(catalog.Outboxes);
        Assert.Equal("qdrant-outbox", descriptor.Id);
        Assert.Equal(QdrantDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("vector-collection", descriptor.Mode);
    }

    [Fact]
    public void AddQdrantData_InboxDescriptor_HasExpectedMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS"], data: new DataSettings(provider: "Qdrant")));
            engine.AddModule(new PlatformTestModule());
            engine.AddQdrantData("localhost", configure: o => o.RegisterInbox = true);
        });
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IInboxCatalog>();
        var descriptor = Assert.Single(catalog.Inboxes);
        Assert.Equal("qdrant-inbox", descriptor.Id);
        Assert.Equal(QdrantDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("point-id", descriptor.Metadata["idempotency"]);
    }

    [Fact]
    public void AddQdrantData_WithEventDrivenIntegration_RegistersConsumerManagedDispatchStore()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                data: new DataSettings(provider: "Qdrant", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "test-events",
                    displayName: "Test Events",
                    description: "Dispatch-store test channel."));
            });
            engine.AddQdrantData("localhost", configure: options =>
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
        Assert.Equal("qdrant-outbox", Assert.Single(dispatchStore.OutboxIds));
    }

    // === NATS ===

    [Fact]
    public void AddNatsData_RegistersExpectedCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS"], data: new DataSettings(provider: "Nats")));
            engine.AddModule(new PlatformTestModule());
            engine.AddNatsData("nats://localhost:4222");
        });
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.nats");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.ledger-store");
    }

    [Fact]
    public void AddNatsData_WithOutbox_RegistersOutboxCapabilityAndDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS", "Outbox"], data: new DataSettings(provider: "Nats", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddNatsData("nats://localhost:4222", o => { o.RegisterOutbox = true; o.RegisterInbox = true; });
        });
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.outbox.nats");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.inbox.nats");
    }

    [Fact]
    public void AddNatsData_OutboxDescriptor_HasExpectedMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS", "Outbox"], data: new DataSettings(provider: "Nats", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddNatsData("nats://localhost:4222", o => o.RegisterOutbox = true);
        });
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IOutboxCatalog>();
        var descriptor = Assert.Single(catalog.Outboxes);
        Assert.Equal("nats-outbox", descriptor.Id);
        Assert.Equal(NatsDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("jetstream-kv", descriptor.Mode);
    }

    [Fact]
    public void AddNatsData_InboxDescriptor_HasExpectedMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS"], data: new DataSettings(provider: "Nats")));
            engine.AddModule(new PlatformTestModule());
            engine.AddNatsData("nats://localhost:4222", o => o.RegisterInbox = true);
        });
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IInboxCatalog>();
        var descriptor = Assert.Single(catalog.Inboxes);
        Assert.Equal("nats-inbox", descriptor.Id);
        Assert.Equal(NatsDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("kv-create", descriptor.Metadata["idempotency"]);
    }

    [Fact]
    public async Task AddNatsData_UsesNamedUriFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uris:Messaging"] = "nats://configured:4222"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice", patterns: ["CQRS"], data: new DataSettings(provider: "Nats")));
            engine.AddModule(new PlatformTestModule());
            engine.AddNatsData(options =>
            {
                options.UriName = "Messaging";
            });
        });
        await using var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<NATS.Client.Core.INatsConnection>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var capability = Assert.Single(runtime.Manifest.Capabilities, c => c.Key == "data.nats");

        Assert.NotNull(connection);
        Assert.Equal("Messaging", capability.Metadata["uriName"]);
    }

    [Fact]
    public async Task AddNatsData_WithEventDrivenIntegration_RegistersConsumerManagedDispatchStore()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                data: new DataSettings(provider: "Nats", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "test-events",
                    displayName: "Test Events",
                    description: "Dispatch-store test channel."));
            });
            engine.AddNatsData("nats://localhost:4222", options =>
            {
                options.RegisterOutbox = true;
            });
        });

        await using var provider = services.BuildServiceProvider();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();
        var descriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("consumer-managed", descriptor.DispatchPolicy.PolicyId);
        Assert.Equal("consumer-managed", descriptor.DispatchPolicy.ExecutionMode);

        await using var scope = provider.CreateAsyncScope();
        var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        Assert.NotNull(dispatchStore);
        Assert.Equal("nats-outbox", Assert.Single(dispatchStore.OutboxIds));
    }
}
