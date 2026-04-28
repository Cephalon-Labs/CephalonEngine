using Cephalon.Abstractions.EventSourcing;
using Cephalon.AspNetCore.GraphQL.Hosting;
using Cephalon.AspNetCore.Grpc.Hosting;
using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.Agentics.Registration;
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
using Cephalon.Observability.Serilog.Hosting;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Diagnostics;
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
/// Redis, RabbitMQ, Kafka, and OpenTelemetry Collector. The showcase host now treats the split
/// configuration files as the source of truth for provider selection, so Local and Development
/// profiles can point directly at Docker Desktop-backed infrastructure while tests or alternate
/// environments can still override the same configuration keys to use in-memory stores.
/// </para>
/// </remarks>
public static class ShowcaseSampleApp
{
    /// <summary>
    /// Builds the showcase sample application with the full Cephalon wiring.
    /// </summary>
    /// <param name="args">Optional command-line arguments for the sample host.</param>
    /// <param name="configureBuilder">Optional hook to customize the builder after the base configuration sources are loaded and before the host is built.</param>
    /// <param name="contentRootPath">Optional explicit content root used to load split showcase configuration during tests or other non-default hosting scenarios.</param>
    /// <returns>The configured showcase sample application.</returns>
    public static WebApplication Build(
        string[]? args = null,
        Action<WebApplicationBuilder>? configureBuilder = null,
        string? contentRootPath = null)
    {
        var contentRoot = contentRootPath
            ?? Path.GetDirectoryName(typeof(ShowcaseSampleApp).Assembly.Location)
            ?? AppContext.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(ShowcaseSampleApp).Assembly.FullName,
            ContentRootPath = contentRoot,
            EnvironmentName = ResolveEnvironmentName()
        });

        builder.AddCephalonProjectConfigurations();
        builder.Configuration.AddEnvironmentVariables("SHOWCASE_");
        configureBuilder?.Invoke(builder);
        if (builder.Configuration.GetSection("Serilog").Exists())
        {
            builder.Logging.ClearProviders();
        }

        builder.AddCephalonSerilog();

        var config = builder.Configuration;
        var registerMongoDbData = HasConfiguredValue(
            config,
            $"{MongoDbDataOptions.SectionPath}:ConnectionStringName",
            $"{MongoDbDataOptions.SectionPath}:ConnectionString");
        var registerRedisData = HasConfiguredValue(
            config,
            $"{RedisDataOptions.SectionPath}:ConnectionStringName",
            $"{RedisDataOptions.SectionPath}:ConnectionString");
        var registerRabbitMqMessaging = HasConfiguredValue(
            config,
            "Engine:Messaging:RabbitMQ:ConnectionString",
            "Engine:Messaging:RabbitMQ:HostName");
        var registerKafkaMessaging = HasConfiguredValue(
            config,
            "Engine:Messaging:Kafka:BootstrapServers");

        builder.AddCephalon(engine =>
        {
            // --- ID generation ---
            engine.AddSfidIds();

            // --- Data layer ---
            engine.AddData();

            // --- Entity Framework data and durable audit history ---
            // The active provider comes from Engine:Databases:* so the sample host does not need
            // hard-coded Docker-vs-in-memory branches.
            engine.AddEntityFrameworkData<ShowcaseReadDbContext, ShowcaseWriteDbContext>(
                configureDbContext: ConfigureShowcaseDatabaseRole,
                configure: efOpts =>
                {
                    efOpts.RegisterOutbox = true;
                    efOpts.RegisterInbox = true;
                });

            engine.AddEntityFrameworkAuditHistory<ShowcaseAuditHistoryDbContext>(
                configureDbContext: ConfigureShowcaseDatabaseRole);

            // --- MongoDB document store ---
            if (registerMongoDbData)
            {
                engine.AddMongoDbData(opts =>
                {
                    config.GetSection(MongoDbDataOptions.SectionPath).Bind(opts);
                });
            }

            // --- Redis cache ---
            if (registerRedisData)
            {
                engine.AddRedisData(opts =>
                {
                    config.GetSection(RedisDataOptions.SectionPath).Bind(opts);
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

            // --- Agentic workload proof ---
            engine.AddAgentics();

            // --- Cross-cutting: identity, tenancy, audit ---
            engine.AddIdentityAccess();
            engine.AddMultiTenancy();
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
                var messagingBindings = behaviors.AddMessagingBehaviorBindings(config)
                    .AddInMemory();

                if (registerRabbitMqMessaging)
                {
                    messagingBindings.AddRabbitMq();
                }

                if (registerKafkaMessaging)
                {
                    messagingBindings.AddKafka();
                }
            });
        });

        // --- Transport mappers ---
        builder.AddGraphQLTransport();
        builder.AddGrpcTransport();
        builder.AddJsonRpcTransport();

        // --- Observability ---
        builder.Services.AddCephalonObservability(builder.Configuration);
        builder.AddCephalonOpenTelemetry();
        builder.Services.AddSingleton<ShowcaseInMemoryEventStore>();
        builder.Services.AddSingleton<IEventStore>(serviceProvider =>
            serviceProvider.GetRequiredService<ShowcaseInMemoryEventStore>());
        builder.Services.AddSingleton<ShowcaseActivityFeed>();
        builder.Services.AddScoped<ShowcaseSystemProjectionService>();
        builder.Services.AddScoped<ShowcaseReadModelProjector>();
        builder.Services.AddScoped<ShowcaseReadModelSyncService>();
        builder.Services.AddScoped<ShowcaseResetService>();
        builder.Services.AddHostedService<ShowcaseDatabaseSeedHostedService>();
        builder.Services.AddHostedService<ShowcaseReadModelProjectionHostedService>();

        var app = builder.Build();
        var apiRoutes = ApiRoutesOptions.FromConfiguration(app.Configuration);
        var openApiOptions = OpenApiEndpointOptions.FromConfiguration(app.Configuration);
        var defaultOpenApiDocumentName = ResolveDefaultOpenApiDocumentName(app.Configuration);
        app.UseExceptionHandler();
        app.UseStaticFiles();
        app.Use(async (context, next) =>
        {
            if (!ShouldCaptureShowcaseActivity(context.Request.Path))
            {
                await next().ConfigureAwait(false);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            await next().ConfigureAwait(false);
            stopwatch.Stop();

            var activityFeed = context.RequestServices.GetRequiredService<ShowcaseActivityFeed>();
            var classification = ClassifyShowcaseActivity(context.Request.Path, apiRoutes, openApiOptions);
            activityFeed.Record(
                classification.Area,
                classification.Transport,
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                title: classification.Title);
        });
        app.MapGet("/", () => TypedResults.Ok(ShowcaseSummary.Instance)).ExcludeFromDescription();
        app.MapGet("/showcase", () => Results.Redirect("/showcase.html")).ExcludeFromDescription();
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
                },
                docs = new
                {
                    openApiJson = BuildOpenApiDocumentPath(openApiOptions.RoutePattern, defaultOpenApiDocumentName),
                    scalar = $"{openApiOptions.ScalarRoutePrefix.TrimEnd('/')}/{defaultOpenApiDocumentName}"
                },
                engine = new
                {
                    databases = "/engine/databases",
                    databaseTopology = "/engine/database-topology",
                    databaseRoles = "/engine/database-roles",
                    databaseMigrations = "/engine/database-migrations",
                    databaseMigrationPlaybook = "/engine/database-migration-playbook",
                    snapshot = "/engine/snapshot",
                    runtimeStory = "/engine/runtime-story",
                    diagnostics = "/engine/diagnostics",
                    modules = "/engine/modules",
                    capabilities = "/engine/capabilities",
                    packages = "/engine/packages",
                    packagePolicy = "/engine/package-policy",
                    trustPolicy = "/engine/trust-policy",
                    authorizationPolicies = "/engine/authorization-policies",
                    technologySurfaces = "/engine/technology-surfaces"
                }
            });

            return Results.Content($"window.CEPHALON_SHOWCASE = {payload};", "application/javascript");
        }).ExcludeFromDescription();
        app.MapCephalon();

        return app;
    }

    private static string ResolveEnvironmentName()
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return string.IsNullOrWhiteSpace(environmentName)
            ? Environments.Development
            : environmentName.Trim();
    }

    private static bool HasConfiguredValue(
        ConfigurationManager configuration,
        params string[] configurationPaths)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configurationPaths);

        return configurationPaths.Any(path => !string.IsNullOrWhiteSpace(configuration[path]));
    }

    private static void ConfigureShowcaseDatabaseRole(
        EntityFrameworkDatabaseRoleContext role,
        DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        if (string.IsNullOrWhiteSpace(role.Provider))
        {
            throw new InvalidOperationException(
                $"Database role '{role.Role}' must declare Engine:Databases:{role.Role}:Provider so the showcase host can select the correct EF provider.");
        }

        var provider = role.Provider.Trim().ToUpperInvariant();

        switch (provider)
        {
            case "INMEMORY":
                optionsBuilder.UseInMemoryDatabase(role.ConnectionString);
                return;

            case "POSTGRESQL":
            case "POSTGRES":
            case "NPGSQL":
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
                return;

            default:
                throw new InvalidOperationException(
                    $"Database role '{role.Role}' uses unsupported provider '{role.Provider}'. The showcase host currently supports InMemory and PostgreSql for EF-backed roles.");
        }
    }

    private static bool ShouldCaptureShowcaseActivity(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Equals("/", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/showcase", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/showcase/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value.Contains("/showcase/system/activity", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("/showcase/system/reset", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/showcase/client-config.js", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return value.StartsWith("/engine", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("/showcase/", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/graphql", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/graphql-ws", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/graphql-sse", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/json-rpc", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/ws", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/sse", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/grpc", StringComparison.OrdinalIgnoreCase);
    }

    private static (string Area, string Transport, string Title) ClassifyShowcaseActivity(
        PathString path,
        ApiRoutesOptions apiRoutes,
        OpenApiEndpointOptions openApiOptions)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith("/engine", StringComparison.OrdinalIgnoreCase))
        {
            return ("engine", "engine", "Engine introspection");
        }

        if (value.StartsWith(openApiOptions.ScalarRoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("docs", "scalar", "Scalar documentation");
        }

        if (value.StartsWith(ExtractFixedRoutePrefix(openApiOptions.RoutePattern), StringComparison.OrdinalIgnoreCase))
        {
            return ("docs", "openapi", "OpenAPI document");
        }

        if (value.Contains("/showcase/system/", StringComparison.OrdinalIgnoreCase))
        {
            return ("system", "rest-api", "Showcase system projection");
        }

        if (value.Contains("/showcase/", StringComparison.OrdinalIgnoreCase))
        {
            return ("business", "rest-api", "Showcase business API");
        }

        if (value.StartsWith(apiRoutes.GraphQLWsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("transport", "graphql-ws", "GraphQL WebSocket transport");
        }

        if (value.StartsWith(apiRoutes.GraphQLSsePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("transport", "graphql-sse", "GraphQL SSE transport");
        }

        if (value.StartsWith(apiRoutes.GraphQLPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("transport", "graphql", "GraphQL transport");
        }

        if (value.StartsWith(apiRoutes.JsonRpcPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("transport", "json-rpc", "JSON-RPC transport");
        }

        if (value.StartsWith(apiRoutes.WsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("transport", "ws", "WebSocket transport");
        }

        if (value.StartsWith(apiRoutes.SsePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("transport", "sse", "Server-sent events transport");
        }

        if (value.StartsWith(apiRoutes.GrpcPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("transport", "grpc", "gRPC transport");
        }

        return ("host", "http", "Host request");
    }

    private static string ExtractFixedRoutePrefix(string routePattern)
    {
        var tokenIndex = routePattern.IndexOf("/{documentName}", StringComparison.OrdinalIgnoreCase);
        if (tokenIndex >= 0)
        {
            return routePattern[..tokenIndex];
        }

        tokenIndex = routePattern.IndexOf("{documentName}", StringComparison.OrdinalIgnoreCase);
        return tokenIndex >= 0
            ? routePattern[..tokenIndex].TrimEnd('/')
            : routePattern;
    }

    private static string BuildOpenApiDocumentPath(string routePattern, string documentName)
    {
        return routePattern.Replace("{documentName}", documentName, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDefaultOpenApiDocumentName(IConfiguration configuration)
    {
        var defaultVersion = NormalizeVersionDocumentName(configuration["OpenApi:DefaultVersion"]);
        if (defaultVersion is not null)
        {
            return defaultVersion;
        }

        var defaultDocument = configuration["OpenApi:DefaultDocument"]?.Trim();
        if (!string.IsNullOrWhiteSpace(defaultDocument))
        {
            return defaultDocument;
        }

        var enabledVersions = configuration.GetSection("OpenApi:EnabledVersions").Get<string[]>()
            ?? configuration.GetSection("OpenApi:EnableVersions").Get<string[]>();
        var firstVersion = enabledVersions?
            .Select(NormalizeVersionDocumentName)
            .FirstOrDefault(static candidate => !string.IsNullOrWhiteSpace(candidate));

        return firstVersion ?? "v1";
    }

    private static string? NormalizeVersionDocumentName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        return int.TryParse(normalized, out var major) && major > 0
            ? $"v{major}"
            : null;
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
