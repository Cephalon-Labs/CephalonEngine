using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.EntityFramework.Modeling;
using Cephalon.Data.Registration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SfidNet;

namespace Cephalon.Tests.Support;

internal sealed class EntityFrameworkSingleContextTestModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "entity-framework-single-context-tests",
        displayName: "Entity Framework Single Context Tests",
        description: "Registers test handlers that exercise the shared DbContext Entity Framework pack path.",
        tags: ["data", "entity-framework", "tests"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddCephalonDataCommand<CreateSingleCatalogItemCommand>();
        services.AddCephalonDataQuery<CountSingleCatalogItemsQuery, int>();
        services.AddScoped<ICommandHandler<CreateSingleCatalogItemCommand>, CreateSingleCatalogItemCommandHandler>();
        services.AddScoped<IQueryHandler<CountSingleCatalogItemsQuery, int>, CountSingleCatalogItemsQueryHandler>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}

internal sealed class EntityFrameworkSplitContextTestModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "entity-framework-split-context-tests",
        displayName: "Entity Framework Split Context Tests",
        description: "Registers test handlers that exercise split read/write DbContext Entity Framework pack paths.",
        tags: ["data", "entity-framework", "tests"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddCephalonDataCommand<CreateSplitWriteCatalogItemCommand>();
        services.AddCephalonDataQuery<CountSplitReadCatalogItemsQuery, int>();
        services.AddScoped<ICommandHandler<CreateSplitWriteCatalogItemCommand>, CreateSplitWriteCatalogItemCommandHandler>();
        services.AddScoped<IQueryHandler<CountSplitReadCatalogItemsQuery, int>, CountSplitReadCatalogItemsQueryHandler>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}

internal sealed class EntityFrameworkOutboxTestModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "entity-framework-outbox-tests",
        displayName: "Entity Framework Outbox Tests",
        description: "Registers test handlers that exercise the Entity Framework-backed outbox path.",
        tags: ["data", "entity-framework", "outbox", "tests"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddCephalonDataCommand<CreateOutboxCatalogItemCommand>();
        services.AddScoped<ICommandHandler<CreateOutboxCatalogItemCommand>, CreateOutboxCatalogItemCommandHandler>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}

internal sealed class EntityFrameworkSfidTestModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "entity-framework-sfid-tests",
        displayName: "Entity Framework Sfid Tests",
        description: "Registers test handlers that exercise official Sfid.EntityFramework integration.",
        tags: ["data", "entity-framework", "sfid", "tests"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddCephalonDataCommand<CreateSfidCatalogItemCommand, Sfid>();
        services.AddScoped<ICommandHandler<CreateSfidCatalogItemCommand, Sfid>, CreateSfidCatalogItemCommandHandler>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}

internal sealed record CreateSingleCatalogItemCommand(string Id, string Name) : ICommand;

internal sealed record CountSingleCatalogItemsQuery() : IQuery<int>;

internal sealed record CreateSplitWriteCatalogItemCommand(string Id, string Name) : ICommand;

internal sealed record CountSplitReadCatalogItemsQuery() : IQuery<int>;

internal sealed record CreateOutboxCatalogItemCommand(string Id, string Name) : ICommand;

internal sealed record CreateSfidCatalogItemCommand(string Name) : ICommand<Sfid>;

internal sealed class SingleCatalogDbContext(DbContextOptions<SingleCatalogDbContext> options) : DbContext(options)
{
    public DbSet<SingleCatalogItem> CatalogItems => Set<SingleCatalogItem>();
}

internal sealed class SplitCatalogReadDbContext(DbContextOptions<SplitCatalogReadDbContext> options) : DbContext(options)
{
    public DbSet<SplitCatalogReadItem> CatalogItems => Set<SplitCatalogReadItem>();
}

internal sealed class SplitCatalogWriteDbContext(DbContextOptions<SplitCatalogWriteDbContext> options) : DbContext(options)
{
    public DbSet<SplitCatalogWriteItem> CatalogItems => Set<SplitCatalogWriteItem>();
}

internal sealed class OutboxCatalogDbContext(DbContextOptions<OutboxCatalogDbContext> options) : DbContext(options), IEntityFrameworkOutboxContext
{
    public DbSet<OutboxCatalogItem> CatalogItems => Set<OutboxCatalogItem>();

    public DbSet<EntityFrameworkOutboxEntry> OutboxMessages => Set<EntityFrameworkOutboxEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ConfigureCephalonOutbox();
    }
}

internal sealed class InboxCatalogDbContext(DbContextOptions<InboxCatalogDbContext> options) : DbContext(options), IEntityFrameworkInboxContext
{
    public DbSet<EntityFrameworkInboxEntry> InboxMessages => Set<EntityFrameworkInboxEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ConfigureCephalonInbox();
    }
}

internal sealed class SfidCatalogDbContext(DbContextOptions<SfidCatalogDbContext> options) : DbContext(options)
{
    public DbSet<SfidCatalogItem> CatalogItems => Set<SfidCatalogItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<SfidCatalogItem>(entity =>
        {
            entity.HasKey(item => item.Id);
        });
    }
}

internal sealed class SingleCatalogItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

internal sealed class SplitCatalogReadItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

internal sealed class SplitCatalogWriteItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

internal sealed class OutboxCatalogItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

internal sealed class SfidCatalogItem
{
    public Sfid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

internal sealed class CreateSingleCatalogItemCommandHandler(SingleCatalogDbContext dbContext) : ICommandHandler<CreateSingleCatalogItemCommand>
{
    public async ValueTask HandleAsync(
        CreateSingleCatalogItemCommand command,
        CancellationToken cancellationToken = default)
    {
        dbContext.CatalogItems.Add(new SingleCatalogItem
        {
            Id = command.Id,
            Name = command.Name
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class CountSingleCatalogItemsQueryHandler(SingleCatalogDbContext dbContext) : IQueryHandler<CountSingleCatalogItemsQuery, int>
{
    public ValueTask<int> HandleAsync(
        CountSingleCatalogItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<int>(dbContext.CatalogItems.CountAsync(cancellationToken));
    }
}

internal sealed class CreateSplitWriteCatalogItemCommandHandler(SplitCatalogWriteDbContext dbContext) : ICommandHandler<CreateSplitWriteCatalogItemCommand>
{
    public async ValueTask HandleAsync(
        CreateSplitWriteCatalogItemCommand command,
        CancellationToken cancellationToken = default)
    {
        dbContext.CatalogItems.Add(new SplitCatalogWriteItem
        {
            Id = command.Id,
            Name = command.Name
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class CountSplitReadCatalogItemsQueryHandler(SplitCatalogReadDbContext dbContext) : IQueryHandler<CountSplitReadCatalogItemsQuery, int>
{
    public ValueTask<int> HandleAsync(
        CountSplitReadCatalogItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<int>(dbContext.CatalogItems.CountAsync(cancellationToken));
    }
}

internal sealed class CreateOutboxCatalogItemCommandHandler(
    OutboxCatalogDbContext dbContext,
    IOutbox outbox) : ICommandHandler<CreateOutboxCatalogItemCommand>
{
    public async ValueTask HandleAsync(
        CreateOutboxCatalogItemCommand command,
        CancellationToken cancellationToken = default)
    {
        dbContext.CatalogItems.Add(new OutboxCatalogItem
        {
            Id = command.Id,
            Name = command.Name
        });

        await outbox.EnqueueAsync(
            new OutboxMessage(
                id: $"outbox-{command.Id}",
                channelId: "catalog-events",
                messageType: "catalog.item.created",
                payload: $$"""{"id":"{{command.Id}}","name":"{{command.Name}}"}""",
                occurredAtUtc: DateTimeOffset.UtcNow,
                contentType: "application/json",
                correlationId: command.Id),
            cancellationToken);
    }
}

internal sealed class CreateSfidCatalogItemCommandHandler(SfidCatalogDbContext dbContext) : ICommandHandler<CreateSfidCatalogItemCommand, Sfid>
{
    public async ValueTask<Sfid> HandleAsync(
        CreateSfidCatalogItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var entity = new SfidCatalogItem
        {
            Name = command.Name
        };

        dbContext.CatalogItems.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
