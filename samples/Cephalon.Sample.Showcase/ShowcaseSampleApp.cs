using Cephalon.Abstractions.Tenancy;
using Cephalon.AspNetCore.GraphQL.Hosting;
using Cephalon.AspNetCore.Grpc.Hosting;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.Audit.Registration;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Messaging.Hosting;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Data.MongoDB.Registration;
using Cephalon.Data.Redis.Registration;
using Cephalon.Data.Registration;
using Cephalon.Eventing.Registration;
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
///   <item><description><b>Orders</b> - Event-driven pattern via RabbitMQ, Kafka, GraphQL-WS, SSE, GraphQL-SSE</description></item>
///   <item><description><b>Inventory</b> - Saga-step pattern via RabbitMQ, in-memory</description></item>
///   <item><description><b>Shipping</b> - Process-manager pattern via Kafka, RabbitMQ, in-memory, REST, gRPC</description></item>
/// </list>
/// <para>
/// Infrastructure services are connected via Docker Compose (compose.yaml):
/// PostgreSQL, MongoDB, Redis, RabbitMQ, Kafka, and OpenTelemetry Collector.
/// </para>
/// </remarks>
public static class ShowcaseSampleApp
{
    /// <summary>
    /// Builds the showcase sample application with the full Cephalon wiring.
    /// </summary>
    /// <param name="args">Optional command-line arguments for the sample host.</param>
    /// <param name="configureBuilder">Optional hook to customize the builder before the host is built.</param>
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

        configureBuilder?.Invoke(builder);
        builder.Configuration.AddJsonFile("showcase.settings.json", optional: false, reloadOnChange: false);
        builder.Configuration.AddEnvironmentVariables("SHOWCASE_");

        var config = builder.Configuration;

        // Docker infrastructure toggle — set SHOWCASE_DOCKER=true to connect to Docker services
        var dockerMode = string.Equals(
            Environment.GetEnvironmentVariable("SHOWCASE_DOCKER"), "true",
            StringComparison.OrdinalIgnoreCase);

        builder.AddCephalon(engine =>
        {
            // --- ID generation ---
            engine.AddSfidIds();

            // --- Data layer ---
            engine.AddData();

            // --- PostgreSQL via Entity Framework (Docker mode only) ---
            if (dockerMode)
            {
                var pgConn = config.GetConnectionString("PostgreSQL")
                    ?? "Host=localhost;Port=5432;Database=showcase_db;Username=showcase;Password=showcase_secret";
                engine.AddEntityFrameworkData<ShowcaseDbContext>(
                    configureDbContext: opts => opts.UseNpgsql(pgConn),
                    configure: efOpts =>
                    {
                        efOpts.RegisterOutbox = true;
                        efOpts.RegisterInbox = true;
                    });
            }

            // --- MongoDB document store (Docker mode only) ---
            if (dockerMode)
            {
                var mongoConn = config.GetConnectionString("MongoDB")
                    ?? "mongodb://showcase:showcase_secret@localhost:27017";
                var mongoDb = config.GetValue("Engine:Data:MongoDB:DatabaseName", "showcase_db")!;
                engine.AddMongoDbData(mongoConn, mongoDb, opts =>
                {
                    config.GetSection("Engine:Data:MongoDB").Bind(opts);
                    opts.RegisterOutbox = true;
                    opts.RegisterInbox = true;
                });
            }

            // --- Redis cache (Docker mode only) ---
            if (dockerMode)
            {
                var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
                engine.AddRedisData(redisConn, opts =>
                {
                    config.GetSection("Engine:Data:Redis").Bind(opts);
                });
            }

            // --- Event-driven integration ---
            engine.AddEventing();

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

            // --- Behaviors: all five patterns + all transports ---
            // Behaviors are auto-registered from loaded assemblies (AutoRegister = true by default).
            // Each behavior's topology is declared via its static ConfigureTopology() method.
            engine.AddBehaviors(behaviors =>
            {
                // Register pattern execution strategies
                behaviors.AddBehaviorPatterns();

                // Register HTTP transport bindings (REST, GraphQL, gRPC, JSON-RPC, SSE, WS)
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

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStaticFiles();
        app.MapGet("/", () => TypedResults.Ok(ShowcaseSummary.Instance)).ExcludeFromDescription();
        app.MapCephalon();

        // --- Database initialization (Docker mode) ---
        if (dockerMode)
        {
            InitializeDatabase(app);
        }

        return app;
    }

    private static void InitializeDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetService<ShowcaseDbContext>();
        if (db is null) return;

        // Create tables if not exist
        db.Database.EnsureCreated();

        // Seed products from in-memory store if database is empty
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

        // Seed inventory from in-memory store if database is empty
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
    private static readonly string[] OrdersTransports = ["rabbitmq", "kafka", "http.graphql-ws", "http.sse", "http.graphql-sse"];
    private static readonly string[] InventoryTransports = ["rabbitmq", "in-memory"];
    private static readonly string[] ShippingTransports = ["kafka", "rabbitmq", "in-memory", "http.rest", "grpc"];
}
