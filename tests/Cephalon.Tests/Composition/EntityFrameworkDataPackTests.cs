using Cephalon.Abstractions.Data;
using Cephalon.Data.EntityFramework.Configuration;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Data.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SfidNet;

namespace Cephalon.Tests.Composition;

public sealed class EntityFrameworkDataPackTests
{
    [Fact]
    public async Task AddEntityFrameworkDataRegistersSharedDbContextForReadAndWriteHandlers()
    {
        var databaseName = $"cephalon-data-ef-single-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "EntityFramework")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSingleContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName));
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();

        await writeStore.ExecuteAsync(new CreateSingleCatalogItemCommand("item-001", "Ada"));
        var count = await readStore.ExecuteAsync(new CountSingleCatalogItemsQuery());
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Equal(1, count);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.entity-framework");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.dbcontext.read");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.dbcontext.write");
    }

    [Fact]
    public async Task AddEntityFrameworkDataSupportsSeparateReadAndWriteDbContexts()
    {
        var readDatabaseName = $"cephalon-data-ef-read-{Guid.NewGuid():N}";
        var writeDatabaseName = $"cephalon-data-ef-write-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    readWriteSplit: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSplitContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SplitCatalogReadDbContext, SplitCatalogWriteDbContext>(
                configureReadDbContext: options => options.UseInMemoryDatabase(readDatabaseName),
                configureWriteDbContext: options => options.UseInMemoryDatabase(writeDatabaseName));
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();
        var readDbContext = scope.ServiceProvider.GetRequiredService<SplitCatalogReadDbContext>();
        var writeDbContext = scope.ServiceProvider.GetRequiredService<SplitCatalogWriteDbContext>();

        await writeStore.ExecuteAsync(new CreateSplitWriteCatalogItemCommand("write-001", "Grace"));
        readDbContext.CatalogItems.Add(new SplitCatalogReadItem
        {
            Id = "read-001",
            Name = "Grace"
        });
        await readDbContext.SaveChangesAsync();

        var readCount = await readStore.ExecuteAsync(new CountSplitReadCatalogItemsQuery());
        var writeCount = await writeDbContext.CatalogItems.CountAsync();

        Assert.Equal(1, readCount);
        Assert.Equal(1, writeCount);
        Assert.NotEqual(readDbContext.GetType(), writeDbContext.GetType());
    }

    [Fact]
    public void AddEntityFrameworkDataPublishesProviderAndDbContextMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    readWriteSplit: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddEntityFrameworkData<SplitCatalogReadDbContext, SplitCatalogWriteDbContext>(
                configureReadDbContext: options => options.UseInMemoryDatabase($"cephalon-data-ef-read-meta-{Guid.NewGuid():N}"),
                configureWriteDbContext: options => options.UseInMemoryDatabase($"cephalon-data-ef-write-meta-{Guid.NewGuid():N}"));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var providerCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.entity-framework");
        var readCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.dbcontext.read");
        var writeCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.dbcontext.write");

        Assert.Equal(EntityFrameworkDataOptions.ProviderId, providerCapability.Metadata["provider"]);
        Assert.Equal("true", providerCapability.Metadata["readWriteSplit"]);
        Assert.Equal(typeof(SplitCatalogReadDbContext).FullName, providerCapability.Metadata["readDbContext"]);
        Assert.Equal(typeof(SplitCatalogWriteDbContext).FullName, providerCapability.Metadata["writeDbContext"]);
        Assert.Equal("read", readCapability.Metadata["role"]);
        Assert.Equal(typeof(SplitCatalogReadDbContext).FullName, readCapability.Metadata["dbContext"]);
        Assert.Equal("write", writeCapability.Metadata["role"]);
        Assert.Equal(typeof(SplitCatalogWriteDbContext).FullName, writeCapability.Metadata["dbContext"]);
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanRegisterAnEfBackedOutbox()
    {
        var databaseName = $"cephalon-data-ef-outbox-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkOutboxTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxCatalogDbContext>();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        await writeStore.ExecuteAsync(new CreateOutboxCatalogItemCommand("item-100", "Lin"));

        var catalogItem = await dbContext.CatalogItems.SingleAsync();
        var outboxEntry = await dbContext.OutboxMessages.SingleAsync();
        var outboxDescriptor = Assert.Single(outboxCatalog.Outboxes);

        Assert.Equal("item-100", catalogItem.Id);
        Assert.Equal("catalog-events", outboxEntry.ChannelId);
        Assert.Equal("catalog.item.created", outboxEntry.MessageType);
        Assert.Equal("entity-framework-outbox", outboxDescriptor.Id);
        Assert.Equal("entity-framework", outboxDescriptor.Provider);
        Assert.Equal("transactional-table", outboxDescriptor.Mode);
        Assert.Equal("not-configured", outboxDescriptor.Metadata["dispatchRuntime"]);
        Assert.Equal("false", outboxDescriptor.Metadata["eventingLinked"]);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.outbox");
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanRegisterAnEfBackedInbox()
    {
        var databaseName = $"cephalon-data-ef-inbox-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "EntityFramework")));
            engine.AddModule(new PlatformTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<InboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterInbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        var dbContext = scope.ServiceProvider.GetRequiredService<InboxCatalogDbContext>();
        var inboxCatalog = provider.GetRequiredService<IInboxCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var message = new InboxMessage(
            id: "evt-100",
            channelId: "catalog-events",
            messageType: "catalog.item.created",
            payload: "{\"id\":\"item-100\"}",
            receivedAtUtc: DateTimeOffset.UtcNow,
            contentType: "application/json",
            correlationId: "corr-100",
            tenantId: "tenant-100");

        Assert.False(await inbox.HasProcessedAsync("evt-100"));

        await inbox.MarkProcessedAsync(message);
        await inbox.MarkProcessedAsync(message);

        var inboxEntry = await dbContext.InboxMessages.SingleAsync();
        var inboxDescriptor = Assert.Single(inboxCatalog.Inboxes);

        Assert.True(await inbox.HasProcessedAsync("evt-100"));
        Assert.Equal("evt-100", inboxEntry.Id);
        Assert.Equal("catalog-events", inboxEntry.ChannelId);
        Assert.Equal("catalog.item.created", inboxEntry.MessageType);
        Assert.Equal("corr-100", inboxEntry.CorrelationId);
        Assert.Equal("entity-framework-inbox", inboxDescriptor.Id);
        Assert.Equal("entity-framework", inboxDescriptor.Provider);
        Assert.Equal("processed-message-table", inboxDescriptor.Mode);
        Assert.Equal("message-id", inboxDescriptor.Metadata["idempotency"]);
        Assert.Equal("dynamic", inboxDescriptor.Metadata["channelMode"]);
        Assert.Equal("not-configured", inboxDescriptor.Metadata["subscriptionRuntime"]);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox");
    }

    [Fact]
    public void AddEntityFrameworkDataThrowsWhenOutboxIsEnabledWithoutOutboxContext()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "ModularMonolith",
                    patterns: ["Outbox"],
                    data: new DataSettings(
                        provider: "EntityFramework",
                        outboxEnabled: true)));
                engine.AddModule(new PlatformTestModule());
                engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                    options => options.UseInMemoryDatabase($"cephalon-data-ef-invalid-outbox-{Guid.NewGuid():N}"),
                    configure: options => options.RegisterOutbox = true);
            }));

        Assert.Contains(nameof(Cephalon.Data.EntityFramework.Modeling.IEntityFrameworkOutboxContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddEntityFrameworkDataThrowsWhenInboxIsEnabledWithoutInboxContext()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "ModularMonolith",
                    patterns: ["CQRS"],
                    data: new DataSettings(provider: "EntityFramework")));
                engine.AddModule(new PlatformTestModule());
                engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                    options => options.UseInMemoryDatabase($"cephalon-data-ef-invalid-inbox-{Guid.NewGuid():N}"),
                    configure: options => options.RegisterInbox = true);
            }));

        Assert.Contains(nameof(Cephalon.Data.EntityFramework.Modeling.IEntityFrameworkInboxContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanUseOfficialSfidEntityFrameworkIntegration()
    {
        var databaseName = $"cephalon-data-ef-sfid-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    idGenerator: "Sfid")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSfidTestModule());
            engine.AddData();
            engine.AddSfidIds(options =>
            {
                options.DatacenterId = 4;
                options.WorkerId = 12;
            });
            engine.AddEntityFrameworkData<SfidCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.EnableSfidIdentifiers = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SfidCatalogDbContext>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        var id = await writeStore.ExecuteAsync(new CreateSfidCatalogItemCommand("Turing"));
        var entity = await dbContext.CatalogItems.SingleAsync();
        var providerCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.entity-framework");

        Assert.True(id.Value > 0);
        Assert.Equal(id, entity.Id);
        Assert.Equal("sfid", providerCapability.Metadata["idStrategy"]);
    }

    [Fact]
    public void AddEntityFrameworkDataRejectsSfidIntegrationWhenConfiguredIdStrategyDoesNotMatch()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    idGenerator: "Ulid")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSfidTestModule());
            engine.AddSfidIds(options =>
            {
                options.DatacenterId = 4;
                options.WorkerId = 12;
            });
            engine.AddEntityFrameworkData<SfidCatalogDbContext>(
                options => options.UseInMemoryDatabase($"cephalon-data-ef-sfid-mismatch-{Guid.NewGuid():N}"),
                configure: options => options.EnableSfidIdentifiers = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<SfidCatalogDbContext>());

        Assert.Contains("Ulid", exception.Message, StringComparison.Ordinal);
        Assert.Contains("EnableSfidIdentifiers", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddEntityFrameworkDataProjectsItsOutboxThroughEventDrivenTechnologySurfaces()
    {
        var databaseName = $"cephalon-data-ef-outbox-surface-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkOutboxTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");

        Assert.Equal(4, eventingSurfaces.Count);
        var outboxSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "outbox-producers");
        var outboxEntry = Assert.Single(outboxSurface.Entries);
        var publishSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-publishers");
        var publisherEntry = Assert.Single(publishSurface.Entries);
        var dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var dispatchEntry = Assert.Single(dispatchSurface.Entries);
        Assert.Equal("entity-framework-outbox", outboxEntry.Id);
        Assert.Equal("entity-framework", outboxEntry.Metadata["provider"]);
        Assert.Equal("transactional-table", outboxEntry.Metadata["mode"]);
        Assert.Equal("entity-framework-data", outboxEntry.Metadata["sourceModuleId"]);
        Assert.Equal("dynamic", outboxEntry.Metadata["channelMode"]);
        Assert.Equal("outbox-backed-publisher", publisherEntry.Id);
        Assert.Equal("outbox", publisherEntry.Metadata["handoff"]);
        Assert.Equal("not-configured", publisherEntry.Metadata["dispatchRuntime"]);
        Assert.Equal("available", publisherEntry.Metadata["dispatchStore"]);
        Assert.Equal("catalog-events", publisherEntry.Metadata["channelIds"]);
        Assert.Equal("entity-framework-outbox", dispatchEntry.Id);
        Assert.Equal("available", dispatchEntry.Metadata["dispatchStore"]);
        Assert.Equal("not-reported", dispatchEntry.Metadata["runtimeState"]);
        Assert.Equal("entity-framework", dispatchEntry.Metadata["provider"]);
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.publish" && capability.Metadata["runtimeState"] == "available");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.publish" && capability.Metadata["dispatchStore"] == "available");
    }

    [Fact]
    public void AddEntityFrameworkDataProjectsItsInboxThroughEventDrivenTechnologySurfaces()
    {
        var databaseName = $"cephalon-data-ef-inbox-surface-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(provider: "EntityFramework"),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
                options.Subscriptions.Add(new EventSubscriptionDescriptor(
                    id: "catalog-projector",
                    displayName: "Catalog Projector",
                    description: "Projects catalog integration events into read models.",
                    channelId: "catalog-events",
                    handlerId: "catalog-read-model-projector",
                    deliveryMode: "background-service"));
            });
            engine.AddEntityFrameworkData<InboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterInbox = true);
        });

        using var provider = services.BuildServiceProvider();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");

        Assert.Equal(3, eventingSurfaces.Count);
        var inboxSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "inbox-stores");
        var inboxEntry = Assert.Single(inboxSurface.Entries);
        var subscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        var subscriptionEntry = Assert.Single(subscriptionSurface.Entries);
        Assert.Equal("entity-framework-inbox", inboxEntry.Id);
        Assert.Equal("entity-framework", inboxEntry.Metadata["provider"]);
        Assert.Equal("processed-message-table", inboxEntry.Metadata["mode"]);
        Assert.Equal("entity-framework-data", inboxEntry.Metadata["sourceModuleId"]);
        Assert.Equal("dynamic", inboxEntry.Metadata["channelMode"]);
        Assert.Equal("message-id", inboxEntry.Metadata["idempotency"]);
        Assert.Equal("not-configured", inboxEntry.Metadata["subscriptionRuntime"]);
        Assert.Equal("available", subscriptionEntry.Metadata["inbox"]);
        Assert.Equal("application-managed", subscriptionEntry.Metadata["inboxLink"]);
        Assert.Equal("entity-framework-inbox", subscriptionEntry.Metadata["inboxIds"]);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.subscriptions" && capability.Metadata["inbox"] == "available");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.publish");
    }

    [Fact]
    public async Task AddEventingCanStagePublicationsThroughOutboxBackedPublisher()
    {
        var databaseName = $"cephalon-data-ef-event-publisher-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxCatalogDbContext>();

        await publisher.PublishAsync(new EventPublication(
            id: "evt-001",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-001\"}",
            occurredAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-001"));

        var outboxEntry = await dbContext.OutboxMessages.SingleAsync();

        Assert.Equal("evt-001", outboxEntry.Id);
        Assert.Equal("catalog-events", outboxEntry.ChannelId);
        Assert.Equal("catalog.item.created", outboxEntry.MessageType);
        Assert.Equal("corr-001", outboxEntry.CorrelationId);
    }

    [Fact]
    public async Task AddEventingCanReportDispatchRuntimeStateThroughEventingTechnologySurfaces()
    {
        var databaseName = $"cephalon-data-ef-event-dispatch-runtime-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var reporter = provider.GetRequiredService<IEventDispatchRuntimeReporter>();
        var runtimeCatalog = provider.GetRequiredService<IEventDispatchRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();

        await reporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Started,
            observedAtUtc: new DateTimeOffset(2026, 04, 04, 12, 0, 0, TimeSpan.Zero),
            messageId: "evt-020",
            attempt: 1,
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = "outbox-backed-publisher"
            }));
        await reporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.RetryScheduled,
            observedAtUtc: new DateTimeOffset(2026, 04, 04, 12, 1, 0, TimeSpan.Zero),
            messageId: "evt-020",
            attempt: 2,
            error: "Broker temporarily unavailable",
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = "outbox-backed-publisher",
                ["nextRetryAtUtc"] = "2026-04-04T12:06:00.0000000+00:00",
                ["retryPolicy"] = "exponential"
            }));

        var publisherState = Assert.Single(runtimeCatalog.States);
        Assert.Equal("entity-framework-outbox", publisherState.OutboxId);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, publisherState.LastOutcome);
        Assert.Equal("catalog-events", publisherState.LastChannelId);
        Assert.Equal("evt-020", publisherState.LastMessageId);
        Assert.Equal(2, publisherState.LastAttempt);
        Assert.Equal(1, publisherState.StartedCount);
        Assert.Equal(1, publisherState.RetryScheduledCount);
        Assert.Equal(2, publisherState.TotalReports);
        Assert.True(publisherState.RetryPending);

        var eventingConvention = Assert.Single(diagnosticsCatalog.Conventions, convention => convention.Source == "Cephalon.Eventing");
        Assert.Equal(4210, eventingConvention.MaximumEventId);
        Assert.Contains(eventingConvention.Events, entry => entry.Id == 4209 && entry.Name == "EventPublicationDispatchRetryScheduled");

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var dispatchEntry = Assert.Single(dispatchSurface.Entries, entry => entry.Id == "entity-framework-outbox");
        Assert.Equal("reported", dispatchEntry.Metadata["runtimeState"]);
        Assert.Equal("available", dispatchEntry.Metadata["dispatchStore"]);
        Assert.Equal("retry-scheduled", dispatchEntry.Metadata["lastOutcome"]);
        Assert.Equal("catalog-events", dispatchEntry.Metadata["lastChannelId"]);
        Assert.Equal("evt-020", dispatchEntry.Metadata["lastMessageId"]);
        Assert.Equal("2", dispatchEntry.Metadata["lastAttempt"]);
        Assert.Equal("1", dispatchEntry.Metadata["retryScheduledCount"]);
        Assert.Equal("2", dispatchEntry.Metadata["totalReports"]);
        Assert.Equal("true", dispatchEntry.Metadata["retryPending"]);
        Assert.Equal("outbox-backed-publisher", dispatchEntry.Metadata["reported.publisherId"]);
        Assert.Equal("2026-04-04T12:06:00.0000000+00:00", dispatchEntry.Metadata["reported.nextRetryAtUtc"]);
        Assert.Equal("Broker temporarily unavailable", dispatchEntry.Metadata["lastError"]);
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanPersistDispatchStoreStateForPendingMessages()
    {
        var databaseName = $"cephalon-data-ef-event-dispatch-store-{Guid.NewGuid():N}";
        var nextRetryAtUtc = DateTimeOffset.UtcNow.AddMinutes(5);
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxCatalogDbContext>();

        await publisher.PublishAsync(new EventPublication(
            id: "evt-200",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-200\"}",
            occurredAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-200"));

        var pendingDispatches = await dispatchStore.ReadPendingAsync(10);
        var pendingDispatch = Assert.Single(pendingDispatches);
        Assert.Equal("entity-framework-outbox", pendingDispatch.OutboxId);
        Assert.Equal("evt-200", pendingDispatch.MessageId);
        Assert.Equal("catalog-events", pendingDispatch.ChannelId);
        Assert.Equal(0, pendingDispatch.DispatchAttemptCount);

        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Started,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: "evt-200",
            attempt: 1));
        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.RetryScheduled,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: "evt-200",
            attempt: 1,
            error: "Broker temporarily unavailable",
            metadata: new Dictionary<string, string>
            {
                ["nextRetryAtUtc"] = nextRetryAtUtc.ToString("O")
            }));

        var pendingDuringBackoff = await dispatchStore.ReadPendingAsync(10);
        Assert.Empty(pendingDuringBackoff);

        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Succeeded,
            observedAtUtc: DateTimeOffset.UtcNow.AddMinutes(10),
            messageId: "evt-200",
            attempt: 2));

        var outboxEntry = await dbContext.OutboxMessages.SingleAsync();
        Assert.Equal(2, outboxEntry.DispatchAttemptCount);
        Assert.NotNull(outboxEntry.DispatchedAtUtc);
        Assert.Null(outboxEntry.NextAttemptAtUtc);
        Assert.Empty(await dispatchStore.ReadPendingAsync(10));
    }
}
