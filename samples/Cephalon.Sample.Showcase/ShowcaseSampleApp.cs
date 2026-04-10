using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Tenancy;
using Cephalon.AspNetCore.GraphQL.Hosting;
using Cephalon.AspNetCore.Grpc.Hosting;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.Audit.EntityFramework.Registration;
using Cephalon.Audit.Registration;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Messaging.Hosting;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Data.EntityFramework.Configuration;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.MongoDB.Registration;
using Cephalon.Data.Redis.Configuration;
using Cephalon.Data.Redis.Registration;
using Cephalon.Data.Registration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Wolverine.Registration;
using Cephalon.Identity.Registration;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Observability.Hosting;
using Cephalon.Observability.OpenTelemetry.Hosting;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Sample.Showcase;

/// <summary>
/// Builds the comprehensive showcase sample that exercises every architecture pattern,
/// behavior pattern, and transport available in CephalonEngine.
/// </summary>
/// <remarks>
/// <para>
/// The showcase models an e-commerce domain with five bounded contexts, each mapped to a
/// distinct behavior pattern:
/// </para>
/// <list type="bullet">
///   <item><description><b>Catalog</b> - Direct pattern via REST, GraphQL, gRPC, JSON-RPC</description></item>
///   <item><description><b>Cart</b> - CQRS pattern (event-sourced) via REST, WebSocket, GraphQL, SSE</description></item>
///   <item><description><b>Orders</b> - Event-driven pattern via REST, RabbitMQ, Kafka, GraphQL-WS, SSE, GraphQL-SSE</description></item>
///   <item><description><b>Inventory</b> - Saga-step pattern via REST, RabbitMQ, in-memory</description></item>
///   <item><description><b>Shipping</b> - Process-manager pattern via Kafka, RabbitMQ, in-memory, REST, gRPC</description></item>
/// </list>
/// <para>
/// Infrastructure services can run through Docker Compose (compose.yaml): PostgreSQL, MongoDB,
/// Redis, RabbitMQ, Kafka, and OpenTelemetry Collector. When Docker mode is not enabled, the
/// sample falls back to in-memory Entity Framework stores for write, read, and audit-history
/// roles so the showcase stays fully bootable and introspectable without external dependencies.
/// </para>
/// </remarks>
public static class ShowcaseSampleApp
{
    /// <summary>
    /// Builds the showcase sample application with the full Cephalon wiring.
    /// </summary>
    /// <param name="args">Optional command-line arguments for the sample host.</param>
    /// <param name="configureBuilder">Optional hook to customize the builder after the base configuration sources are loaded and before the host is built.</param>
    /// <returns>The configured showcase sample application.</returns>
    public static WebApplication Build(
        string[]? args = null,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var contentRoot = Path.GetDirectoryName(typeof(ShowcaseSampleApp).Assembly.Location)
            ?? AppContext.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(ShowcaseSampleApp).Assembly.FullName,
            ContentRootPath = contentRoot,
            EnvironmentName = Environments.Development
        });

        builder.Configuration.AddJsonFile("showcase.settings.json", optional: false, reloadOnChange: false);
        builder.Configuration.AddEnvironmentVariables("SHOWCASE_");
        configureBuilder?.Invoke(builder);

        var config = builder.Configuration;

        // Docker infrastructure toggle — set SHOWCASE_DOCKER=true to connect to Docker services
        var dockerMode = string.Equals(
            Environment.GetEnvironmentVariable("SHOWCASE_DOCKER"), "true",
            StringComparison.OrdinalIgnoreCase);
        var fallbackDatabaseSuffix = Guid.NewGuid().ToString("N");
        if (!dockerMode)
        {
            ApplyNonDockerDatabaseOverrides(builder.Configuration, fallbackDatabaseSuffix);
        }

        builder.AddCephalon(engine =>
        {
            // --- ID generation ---
            engine.AddSfidIds();

            // --- Data layer ---
            engine.AddData();

            // --- Entity Framework data and durable audit history ---
            // Docker mode uses PostgreSQL. Local/test mode falls back to in-memory EF stores so
            // the showcase still exercises database-role, migration, and audit-history surfaces.
            engine.AddEntityFrameworkData<ShowcaseReadDbContext, ShowcaseWriteDbContext>(
                configureDbContext: (role, opts) => ConfigureShowcaseDatabaseRole(role, opts, dockerMode),
                configure: efOpts =>
                {
                    efOpts.RegisterOutbox = true;
                    efOpts.RegisterInbox = true;
                });

            engine.AddEntityFrameworkAuditHistory<ShowcaseAuditHistoryDbContext>(
                configureDbContext: (role, opts) => ConfigureShowcaseDatabaseRole(role, opts, dockerMode));

            // --- MongoDB document store (Docker mode only) ---
            if (dockerMode)
            {
                engine.AddMongoDbData(opts =>
                {
                    config.GetSection(MongoDbDataOptions.SectionPath).Bind(opts);
                    opts.ConnectionStringName ??= "MongoDB";
                    opts.RegisterOutbox = true;
                    opts.RegisterInbox = true;
                });
            }

            // --- Redis cache (Docker mode only) ---
            if (dockerMode)
            {
                engine.AddRedisData(opts =>
                {
                    config.GetSection(RedisDataOptions.SectionPath).Bind(opts);
                    opts.ConnectionStringName ??= "Redis";
                });
            }

            // --- Event-driven integration ---
            engine.AddEventing();
            engine.AddWolverineEventing(options =>
            {
                options.EnableDispatchLoop = true;
                options.DispatchBatchSize = 10;
                options.DispatchPollingIntervalSeconds = 3;
                options.RetryDelaySeconds = 15;
            });

            // --- Cross-cutting: identity, tenancy, audit ---
            engine.AddIdentityAccess();
            engine.AddMultiTenancy(opts =>
            {
                opts.Tenants.Add(new TenantContext(
                    tenantId: "tenant-alpha",
                    displayName: "Alpha Store",
                    domains: ["alpha.showcase.local"]));
                opts.Tenants.Add(new TenantContext(
                    tenantId: "tenant-beta",
                    displayName: "Beta Store",
                    domains: ["beta.showcase.local"]));
                opts.DefaultTenantId = "tenant-alpha";
            });
            engine.AddAudit();

            // --- Behaviors: all five patterns + generic non-REST transports ---
            // This sample uses explicit module-owned behavior registration for public REST and
            // internal ownership. Hosts can still opt into assembly auto-registration separately
            // through Engine:Behaviors:AutoRegister when they want the fallback scan path.
            engine.AddBehaviors(behaviors =>
            {
                // Register pattern execution strategies
                behaviors.AddBehaviorPatterns();

                // Register generic HTTP behavior bindings (JSON-RPC, GraphQL, GraphQL-SSE/WS, SSE, WS)
                behaviors.AddHttpBehaviorBindings();

                // Register messaging transport bindings — auto-bind from Engine:Messaging config
                behaviors.AddMessagingBehaviorBindings(config)
                    .AddInMemory()
                    .AddRabbitMq()
                    .AddKafka();
            });
        });

        // --- Transport mappers ---
        builder.AddGraphQLTransport();
        builder.AddGrpcTransport();
        builder.AddJsonRpcTransport();

        // --- Observability ---
        builder.Services.AddCephalonObservability(builder.Configuration);
        builder.AddCephalonOpenTelemetry();
        builder.Services.AddSingleton<IEventStore, ShowcaseInMemoryEventStore>();

        var app = builder.Build();
        var apiRoutes = ApiRoutesOptions.FromConfiguration(app.Configuration);
        app.UseExceptionHandler();
        app.UseStaticFiles();
        app.MapGet("/", () => TypedResults.Ok(ShowcaseSummary.Instance)).ExcludeFromDescription();
        app.MapGet("/showcase/client-config.js", () =>
        {
            var payload = JsonSerializer.Serialize(new
            {
                restApiBase = $"{apiRoutes.RestPrefix}/v1/showcase",
                restApiVersion = "v1",
                behaviorVersion = apiRoutes.DefaultBehaviorDocumentName,
                behaviorRoutes = new
                {
                    rest = apiRoutes.RestPrefix,
                    graphql = apiRoutes.GraphQLPrefix,
                    jsonRpc = apiRoutes.JsonRpcPrefix,
                    grpc = apiRoutes.GrpcPrefix,
                    ws = apiRoutes.WsPrefix,
                    sse = apiRoutes.SsePrefix,
                    graphQLWs = apiRoutes.GraphQLWsPrefix,
                    graphQLSse = apiRoutes.GraphQLSsePrefix
                }
            });

            return Results.Content($"window.CEPHALON_SHOWCASE = {payload};", "application/javascript");
        }).ExcludeFromDescription();
        app.MapCephalon();

        // --- Database initialization ---
        InitializeDatabase(app);

        return app;
    }

    private static void ApplyNonDockerDatabaseOverrides(
        ConfigurationManager configuration,
        string databaseSuffix)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseSuffix);

        var normalizedSuffix = databaseSuffix.Trim();
        OverrideInMemoryRole(configuration, "Write", $"showcase-write-{normalizedSuffix}");
        OverrideInMemoryRole(configuration, "Read", $"showcase-read-{normalizedSuffix}");
        OverrideInMemoryRole(configuration, "History", $"showcase-history-{normalizedSuffix}");
    }

    private static void OverrideInMemoryRole(
        ConfigurationManager configuration,
        string roleSectionName,
        string databaseName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleSectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        configuration[$"Engine:Databases:{roleSectionName}:Provider"] = "InMemory";
        configuration[$"Engine:Databases:{roleSectionName}:ConnectionStringName"] = string.Empty;
        configuration[$"Engine:Databases:{roleSectionName}:ConnectionString"] = databaseName.Trim();
    }

    private static void ConfigureShowcaseDatabaseRole(
        EntityFrameworkDatabaseRoleContext role,
        DbContextOptionsBuilder optionsBuilder,
        bool dockerMode)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        if (!dockerMode)
        {
            optionsBuilder.UseInMemoryDatabase(role.ConnectionString);
            return;
        }

        optionsBuilder.UseNpgsql(
            role.ConnectionString,
            npgsql =>
            {
                if (role.Runtime.CommandTimeoutSeconds is { } commandTimeoutSeconds)
                {
                    npgsql.CommandTimeout(commandTimeoutSeconds);
                }

                if (role.Runtime.MaxBatchSize is { } maxBatchSize)
                {
                    npgsql.MaxBatchSize(maxBatchSize);
                }

                if (role.Runtime.EnableRetryOnFailure == true)
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: role.Runtime.MaxRetryCount ?? 6,
                        maxRetryDelay: role.Runtime.MaxRetryDelaySeconds is { } seconds
                            ? TimeSpan.FromSeconds(seconds)
                            : TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                }
            });
    }

    private static void InitializeDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var writeDb = scope.ServiceProvider.GetService<ShowcaseWriteDbContext>();
        var readDb = scope.ServiceProvider.GetService<ShowcaseReadDbContext>();
        var historyDb = scope.ServiceProvider.GetService<ShowcaseAuditHistoryDbContext>();

        writeDb?.Database.EnsureCreated();
        readDb?.Database.EnsureCreated();
        historyDb?.Database.EnsureCreated();

        if (writeDb is not null)
        {
            SeedCommerceReferenceData(writeDb);
        }

        if (readDb is not null)
        {
            SeedCommerceReferenceData(readDb);
        }
    }

    private static void SeedCommerceReferenceData(ShowcaseCommerceDbContextBase db)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (!db.Products.Any())
        {
            foreach (var product in ShowcaseDataStore.Products.Values)
            {
                db.Products.Add(new ShowcaseProductEntity
                {
                    Id = product.Id,
                    Sku = product.Sku,
                    Name = product.Name,
                    Description = product.Description,
                    Category = product.Category,
                    PriceInCents = product.PriceInCents,
                    Currency = product.Currency,
                    IsActive = product.IsActive,
                    TagsJson = JsonSerializer.Serialize(product.Tags),
                    CreatedAtUtc = product.CreatedAtUtc,
                    UpdatedAtUtc = product.UpdatedAtUtc
                });
            }

            db.SaveChanges();
        }

        if (!db.InventoryItems.Any())
        {
            foreach (var item in ShowcaseDataStore.Inventory.Values)
            {
                db.InventoryItems.Add(new ShowcaseInventoryEntity
                {
                    ProductId = item.ProductId,
                    QuantityOnHand = item.QuantityOnHand,
                    QuantityReserved = item.QuantityReserved,
                    WarehouseCode = item.WarehouseCode,
                    LastUpdatedAtUtc = item.LastUpdatedAtUtc
                });
            }

            db.SaveChanges();
        }
    }
}

/// <summary>
/// Static summary object returned by the root endpoint to describe the showcase configuration.
/// </summary>
internal static class ShowcaseSummary
{
    /// <summary>
    /// Gets the singleton summary instance.
    /// </summary>
    public static readonly object Instance = new
    {
        sample = "Showcase",
        blueprint = "ModularMonolith",
        description = "Comprehensive CephalonEngine showcase exercising all patterns and transports",
        domains = DomainDescriptors,
        infrastructure = new
        {
            postgres = "localhost:5432",
            mongodb = "localhost:27017",
            redis = "localhost:6379",
            rabbitmq = "localhost:5672",
            kafka = "localhost:9092",
            otelCollector = "localhost:4317"
        }
    };

    private static readonly object[] DomainDescriptors =
    [
        new { name = "Catalog", pattern = "direct", transports = CatalogTransports },
        new { name = "Cart", pattern = "cqrs", transports = CartTransports },
        new { name = "Orders", pattern = "event-driven", transports = OrdersTransports },
        new { name = "Inventory", pattern = "saga-step", transports = InventoryTransports },
        new { name = "Shipping", pattern = "process-manager", transports = ShippingTransports }
    ];

    private static readonly string[] CatalogTransports = ["http.rest", "http.graphql", "grpc", "http.jsonrpc"];
    private static readonly string[] CartTransports = ["http.rest", "http.ws", "http.graphql", "http.sse"];
    private static readonly string[] OrdersTransports = ["http.rest", "rabbitmq", "kafka", "http.graphql-ws", "http.sse", "http.graphql-sse"];
    private static readonly string[] InventoryTransports = ["http.rest", "rabbitmq", "in-memory"];
    private static readonly string[] ShippingTransports = ["kafka", "rabbitmq", "in-memory", "http.rest", "grpc"];
}
