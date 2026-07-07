using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.CassandraDependencies.Configuration;
using Cephalon.Observability.CassandraDependencies.Hosting;
using Cephalon.Observability.CassandraDependencies.Services;
using Cephalon.Observability.ClickHouseDependencies.Configuration;
using Cephalon.Observability.ClickHouseDependencies.Hosting;
using Cephalon.Observability.ClickHouseDependencies.Services;
using Cephalon.Observability.ConsulDependencies.Configuration;
using Cephalon.Observability.ConsulDependencies.Hosting;
using Cephalon.Observability.ConsulDependencies.Services;
using Cephalon.Observability.ElasticsearchDependencies.Configuration;
using Cephalon.Observability.ElasticsearchDependencies.Hosting;
using Cephalon.Observability.ElasticsearchDependencies.Services;
using Cephalon.Observability.HttpDependencies.Configuration;
using Cephalon.Observability.HttpDependencies.Hosting;
using Cephalon.Observability.KafkaDependencies.Configuration;
using Cephalon.Observability.KafkaDependencies.Hosting;
using Cephalon.Observability.KafkaDependencies.Services;
using Cephalon.Observability.MemcachedDependencies.Configuration;
using Cephalon.Observability.MemcachedDependencies.Hosting;
using Cephalon.Observability.MongoDbDependencies.Configuration;
using Cephalon.Observability.MongoDbDependencies.Hosting;
using Cephalon.Observability.MongoDbDependencies.Services;
using Cephalon.Observability.MqttDependencies.Configuration;
using Cephalon.Observability.MqttDependencies.Hosting;
using Cephalon.Observability.MqttDependencies.Services;
using Cephalon.Observability.MySqlDependencies.Configuration;
using Cephalon.Observability.MySqlDependencies.Hosting;
using Cephalon.Observability.MySqlDependencies.Services;
using Cephalon.Observability.NatsDependencies.Configuration;
using Cephalon.Observability.NatsDependencies.Hosting;
using Cephalon.Observability.NatsDependencies.Services;
using Cephalon.Observability.Neo4jDependencies.Configuration;
using Cephalon.Observability.Neo4jDependencies.Hosting;
using Cephalon.Observability.Neo4jDependencies.Services;
using Cephalon.Observability.OpenSearchDependencies.Configuration;
using Cephalon.Observability.OpenSearchDependencies.Hosting;
using Cephalon.Observability.OpenSearchDependencies.Services;
using Cephalon.Observability.OracleDependencies.Configuration;
using Cephalon.Observability.OracleDependencies.Hosting;
using Cephalon.Observability.OracleDependencies.Services;
using Cephalon.Observability.PostgresDependencies.Configuration;
using Cephalon.Observability.PostgresDependencies.Hosting;
using Cephalon.Observability.PostgresDependencies.Services;
using Cephalon.Observability.RabbitMqDependencies.Configuration;
using Cephalon.Observability.RabbitMqDependencies.Hosting;
using Cephalon.Observability.RabbitMqDependencies.Services;
using Cephalon.Observability.RedisDependencies.Configuration;
using Cephalon.Observability.RedisDependencies.Hosting;
using Cephalon.Observability.SqlServerDependencies.Configuration;
using Cephalon.Observability.SqlServerDependencies.Hosting;
using Cephalon.Observability.SqlServerDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class ObservabilityDependencyHealthProviderInvariantTests
{
    private static readonly ProviderManifest Manifest = LoadProviderManifest();

    private static readonly string[] RegisteredExtensionMethods =
    [
        "AddCephalonCassandraDependencyHealth",
        "AddCephalonClickHouseDependencyHealth",
        "AddCephalonConsulDependencyHealth",
        "AddCephalonElasticsearchDependencyHealth",
        "AddCephalonHttpDependencyHealth",
        "AddCephalonKafkaDependencyHealth",
        "AddCephalonMemcachedDependencyHealth",
        "AddCephalonMongoDbDependencyHealth",
        "AddCephalonMqttDependencyHealth",
        "AddCephalonMySqlDependencyHealth",
        "AddCephalonNatsDependencyHealth",
        "AddCephalonNeo4jDependencyHealth",
        "AddCephalonOpenSearchDependencyHealth",
        "AddCephalonOracleDependencyHealth",
        "AddCephalonPostgresDependencyHealth",
        "AddCephalonRabbitMqDependencyHealth",
        "AddCephalonRedisDependencyHealth",
        "AddCephalonSqlServerDependencyHealth"
    ];

    private static ProviderExpectation[] ProviderExpectations => Manifest.Providers;

    [Fact]
    public void DependencyHealthProviderManifestMatchesRegisteredInvariantProviders()
    {
        Assert.Equal("1.0.0", Manifest.SchemaVersion);
        Assert.Equal(Manifest.ProviderCount, ProviderExpectations.Length);
        Assert.Equal(RegisteredExtensionMethods, ProviderExpectations.Select(static provider => provider.ExtensionMethod));
        Assert.Equal(
            ProviderExpectations.Length,
            ProviderExpectations.Select(static provider => provider.Source).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task DependencyHealthProvidersShareRuntimeHealthAndDiagnosticsInvariants()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.AddCephalon();
        RegisterAllRequiredValidationFailures(builder.Services);

        using var host = builder.Build();
        await host.StartAsync();

        try
        {
            var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
            var dependencies = evaluator.EvaluateDependencies();
            var readiness = evaluator.EvaluateReadiness();
            var diagnosticsCatalog = host.Services.GetRequiredService<IRuntimeDiagnosticsCatalog>();
            var snapshot = host.Services.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();
            var dependencyHealthSection = Assert.Single(
                snapshot.ExtensionSections,
                static section => section.Id == "dependency-health");

            Assert.Equal(ProviderExpectations.Length, dependencies.Length);
            Assert.Equal(
                ProviderExpectations.Select(static provider => provider.Source),
                dependencies.Select(static dependency => dependency.Source));
            Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
            Assert.Equal(ProviderExpectations.Length, readiness.Dependencies.Count);
            Assert.Equal("1.0.0", dependencyHealthSection.SchemaVersion);
            Assert.Equal(ProviderExpectations.Length, dependencyHealthSection.Entries.Count);

            foreach (var provider in ProviderExpectations)
            {
                var dependency = Assert.Single(dependencies, report => report.Source == provider.Source);
                Assert.Equal(provider.Id, dependency.Id);
                Assert.Equal(provider.DisplayName, dependency.DisplayName);
                Assert.True(dependency.Required);
                Assert.Equal(HealthState.Unhealthy, dependency.State);
                Assert.False(string.IsNullOrWhiteSpace(dependency.Description));
                Assert.NotNull(dependency.CheckedAtUtc);
                Assert.True(dependency.ProbeDurationMilliseconds >= 0);
                Assert.Equal(1, dependency.ConsecutiveFailureCount);

                var operatorEntry = Assert.Single(
                    dependencyHealthSection.Entries,
                    entry => entry.Id == dependency.Id);
                Assert.Equal("healthy", operatorEntry.DesiredState);
                Assert.Equal("unhealthy", operatorEntry.ObservedState);
                Assert.Equal("false", Assert.Single(operatorEntry.Conditions).Status);
                Assert.Equal("1", operatorEntry.Metadata["consecutiveFailureCount"]);
                Assert.Equal(provider.Source, operatorEntry.Metadata["source"]);

                var convention = Assert.Single(diagnosticsCatalog.GetBySource(provider.Source));
                Assert.Contains(convention.Events, static entry => entry.Name == "ProbeTimedOut");
                Assert.Contains(convention.Events, static entry => entry.Name == "ProbeFailed");
                Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == provider.Source);
            }
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task DependencyHealthProvidersExecuteSuccessfulLiveProbeRuntime()
    {
        await using var httpServer = new DependencyHealthHttpServer(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["/dependency-health"] = 200
        });
        await using var memcachedServer = new FakeMemcachedServer();
        await using var redisServer = new FakeRedisServer();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.AddCephalon();
        var probeClient = RegisterSuccessfulProbeClients(builder.Services);
        RegisterAllSuccessfulProbeRuntime(
            builder.Services,
            httpServer.GetUrl("/dependency-health"),
            memcachedServer.Port,
            redisServer.Port);

        using var host = builder.Build();
        await host.StartAsync();

        try
        {
            var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
            var dependencies = evaluator.EvaluateDependencies();
            var readiness = evaluator.EvaluateReadiness();

            Assert.Equal(ProviderExpectations.Length, dependencies.Length);
            Assert.Equal(RuntimeHealthState.Healthy, readiness.State);
            Assert.Equal(ProviderExpectations.Length, readiness.Dependencies.Count);

            foreach (var provider in ProviderExpectations)
            {
                var dependency = Assert.Single(dependencies, report => report.Source == provider.Source);
                Assert.Equal(provider.Id, dependency.Id);
                Assert.Equal(provider.DisplayName, dependency.DisplayName);
                Assert.True(dependency.Required);
                Assert.Equal(HealthState.Healthy, dependency.State);
                Assert.False(string.IsNullOrWhiteSpace(dependency.Description));
                Assert.NotNull(dependency.CheckedAtUtc);
                Assert.True(dependency.ProbeDurationMilliseconds >= 0);
                Assert.Equal(0, dependency.ConsecutiveFailureCount);
            }

            Assert.Equal(
                ProviderExpectations
                    .Select(static provider => provider.Provider)
                    .Except(["HTTP", "Memcached", "Redis"], StringComparer.Ordinal)
                    .OrderBy(static provider => provider, StringComparer.Ordinal),
                probeClient.ObservedProviders.OrderBy(static provider => provider, StringComparer.Ordinal));
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static SuccessfulDependencyProbeClient RegisterSuccessfulProbeClients(IServiceCollection services)
    {
        var probeClient = new SuccessfulDependencyProbeClient();
        services.AddSingleton(probeClient);
        services.AddSingleton<ICassandraDependencyProbeClient>(probeClient);
        services.AddSingleton<IClickHouseDependencyProbeClient>(probeClient);
        services.AddSingleton<IConsulDependencyProbeClient>(probeClient);
        services.AddSingleton<IElasticsearchDependencyProbeClient>(probeClient);
        services.AddSingleton<IKafkaDependencyProbeClient>(probeClient);
        services.AddSingleton<IMongoDbDependencyProbeClient>(probeClient);
        services.AddSingleton<IMqttDependencyProbeClient>(probeClient);
        services.AddSingleton<IMySqlDependencyProbeClient>(probeClient);
        services.AddSingleton<INatsDependencyProbeClient>(probeClient);
        services.AddSingleton<INeo4jDependencyProbeClient>(probeClient);
        services.AddSingleton<IOpenSearchDependencyProbeClient>(probeClient);
        services.AddSingleton<IOracleDependencyProbeClient>(probeClient);
        services.AddSingleton<IPostgresDependencyProbeClient>(probeClient);
        services.AddSingleton<IRabbitMqDependencyProbeClient>(probeClient);
        services.AddSingleton<ISqlServerDependencyProbeClient>(probeClient);

        return probeClient;
    }

    private static void RegisterAllSuccessfulProbeRuntime(
        IServiceCollection services,
        string httpEndpoint,
        int memcachedPort,
        int redisPort)
    {
        services.AddCephalonCassandraDependencyHealth(options =>
            options.Dependencies = [new CassandraDependencyDefinition { Id = "required-cassandra", DisplayName = "Required Cassandra", Required = true, TimeoutSeconds = 5, ContactPoints = ["127.0.0.1"] }]);
        services.AddCephalonClickHouseDependencyHealth(options =>
            options.Dependencies = [new ClickHouseDependencyDefinition { Id = "required-clickhouse", DisplayName = "Required ClickHouse", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
        services.AddCephalonConsulDependencyHealth(options =>
            options.Dependencies = [new ConsulDependencyDefinition { Id = "required-consul", DisplayName = "Required Consul", Required = true, TimeoutSeconds = 5, Endpoint = "http://127.0.0.1:8500/v1/status/leader" }]);
        services.AddCephalonElasticsearchDependencyHealth(options =>
            options.Dependencies = [new ElasticsearchDependencyDefinition { Id = "required-elasticsearch", DisplayName = "Required Elasticsearch", Required = true, TimeoutSeconds = 5, Endpoint = "http://127.0.0.1:9200" }]);
        services.AddCephalonHttpDependencyHealth(options =>
            options.Dependencies = [new HttpDependencyDefinition { Id = "required-http", DisplayName = "Required HTTP", Required = true, TimeoutSeconds = 5, Endpoint = httpEndpoint }]);
        services.AddCephalonKafkaDependencyHealth(options =>
            options.Dependencies = [new KafkaDependencyDefinition { Id = "required-kafka", DisplayName = "Required Kafka", Required = true, TimeoutSeconds = 5, BootstrapServers = "127.0.0.1:9092" }]);
        services.AddCephalonMemcachedDependencyHealth(options =>
            options.Dependencies = [new MemcachedDependencyDefinition { Id = "required-memcached", DisplayName = "Required Memcached", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1", Port = memcachedPort }]);
        services.AddCephalonMongoDbDependencyHealth(options =>
            options.Dependencies = [new MongoDbDependencyDefinition { Id = "required-mongodb", DisplayName = "Required MongoDB", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
        services.AddCephalonMqttDependencyHealth(options =>
            options.Dependencies = [new MqttDependencyDefinition { Id = "required-mqtt", DisplayName = "Required MQTT", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
        services.AddCephalonMySqlDependencyHealth(options =>
            options.Dependencies = [new MySqlDependencyDefinition { Id = "required-mysql", DisplayName = "Required MySQL", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
        services.AddCephalonNatsDependencyHealth(options =>
            options.Dependencies = [new NatsDependencyDefinition { Id = "required-nats", DisplayName = "Required NATS", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
        services.AddCephalonNeo4jDependencyHealth(options =>
            options.Dependencies = [new Neo4jDependencyDefinition { Id = "required-neo4j", DisplayName = "Required Neo4j", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
        services.AddCephalonOpenSearchDependencyHealth(options =>
            options.Dependencies = [new OpenSearchDependencyDefinition { Id = "required-opensearch", DisplayName = "Required OpenSearch", Required = true, TimeoutSeconds = 5, Endpoint = "http://127.0.0.1:9200" }]);
        services.AddCephalonOracleDependencyHealth(options =>
            options.Dependencies = [new OracleDependencyDefinition { Id = "required-oracle", DisplayName = "Required Oracle", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1", ServiceName = "XEPDB1" }]);
        services.AddCephalonPostgresDependencyHealth(options =>
            options.Dependencies = [new PostgresDependencyDefinition { Id = "required-postgres", DisplayName = "Required Postgres", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
        services.AddCephalonRabbitMqDependencyHealth(options =>
            options.Dependencies = [new RabbitMqDependencyDefinition { Id = "required-rabbitmq", DisplayName = "Required RabbitMQ", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
        services.AddCephalonRedisDependencyHealth(options =>
            options.Dependencies = [new RedisDependencyDefinition { Id = "required-redis", DisplayName = "Required Redis", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1", Port = redisPort }]);
        services.AddCephalonSqlServerDependencyHealth(options =>
            options.Dependencies = [new SqlServerDependencyDefinition { Id = "required-sqlserver", DisplayName = "Required SQL Server", Required = true, TimeoutSeconds = 5, Host = "127.0.0.1" }]);
    }

    private static void RegisterAllRequiredValidationFailures(IServiceCollection services)
    {
        services.AddCephalonCassandraDependencyHealth(options =>
            options.Dependencies = [new CassandraDependencyDefinition { Id = " required-cassandra ", DisplayName = " Required Cassandra ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonClickHouseDependencyHealth(options =>
            options.Dependencies = [new ClickHouseDependencyDefinition { Id = " required-clickhouse ", DisplayName = " Required ClickHouse ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonConsulDependencyHealth(options =>
            options.Dependencies = [new ConsulDependencyDefinition { Id = " required-consul ", DisplayName = " Required Consul ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonElasticsearchDependencyHealth(options =>
            options.Dependencies = [new ElasticsearchDependencyDefinition { Id = " required-elasticsearch ", DisplayName = " Required Elasticsearch ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonHttpDependencyHealth(options =>
            options.Dependencies = [new HttpDependencyDefinition { Id = " required-http ", DisplayName = " Required HTTP ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonKafkaDependencyHealth(options =>
            options.Dependencies = [new KafkaDependencyDefinition { Id = " required-kafka ", DisplayName = " Required Kafka ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonMemcachedDependencyHealth(options =>
            options.Dependencies = [new MemcachedDependencyDefinition { Id = " required-memcached ", DisplayName = " Required Memcached ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonMongoDbDependencyHealth(options =>
            options.Dependencies = [new MongoDbDependencyDefinition { Id = " required-mongodb ", DisplayName = " Required MongoDB ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonMqttDependencyHealth(options =>
            options.Dependencies = [new MqttDependencyDefinition { Id = " required-mqtt ", DisplayName = " Required MQTT ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonMySqlDependencyHealth(options =>
            options.Dependencies = [new MySqlDependencyDefinition { Id = " required-mysql ", DisplayName = " Required MySQL ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonNatsDependencyHealth(options =>
            options.Dependencies = [new NatsDependencyDefinition { Id = " required-nats ", DisplayName = " Required NATS ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonNeo4jDependencyHealth(options =>
            options.Dependencies = [new Neo4jDependencyDefinition { Id = " required-neo4j ", DisplayName = " Required Neo4j ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonOpenSearchDependencyHealth(options =>
            options.Dependencies = [new OpenSearchDependencyDefinition { Id = " required-opensearch ", DisplayName = " Required OpenSearch ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonOracleDependencyHealth(options =>
            options.Dependencies = [new OracleDependencyDefinition { Id = " required-oracle ", DisplayName = " Required Oracle ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonPostgresDependencyHealth(options =>
            options.Dependencies = [new PostgresDependencyDefinition { Id = " required-postgres ", DisplayName = " Required Postgres ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonRabbitMqDependencyHealth(options =>
            options.Dependencies = [new RabbitMqDependencyDefinition { Id = " required-rabbitmq ", DisplayName = " Required RabbitMQ ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonRedisDependencyHealth(options =>
            options.Dependencies = [new RedisDependencyDefinition { Id = " required-redis ", DisplayName = " Required Redis ", Required = true, TimeoutSeconds = 1 }]);
        services.AddCephalonSqlServerDependencyHealth(options =>
            options.Dependencies = [new SqlServerDependencyDefinition { Id = " required-sqlserver ", DisplayName = " Required SQL Server ", Required = true, TimeoutSeconds = 1 }]);
    }

    private static ProviderManifest LoadProviderManifest()
    {
        var manifestPath = Path.Combine(GetRepositoryRoot(), "scripts", "observability-dependency-health-providers.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = manifest.RootElement;
        var providers = root.GetProperty("providers").EnumerateArray()
            .Select(provider => new ProviderExpectation(
                ReadRequiredString(provider, "provider"),
                ReadRequiredString(provider, "source"),
                ReadRequiredString(provider, "id"),
                ReadRequiredString(provider, "displayName"),
                ReadRequiredString(provider, "extensionMethod")))
            .ToArray();

        return new ProviderManifest(
            ReadRequiredString(root, "schemaVersion"),
            root.GetProperty("providerCount").GetInt32(),
            providers);
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        var value = element.GetProperty(propertyName).GetString();
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(
                $"Dependency-health provider manifest property '{propertyName}' must be populated.")
            : value;
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
    }

    private sealed record ProviderManifest(string SchemaVersion, int ProviderCount, ProviderExpectation[] Providers);

    private sealed record ProviderExpectation(
        string Provider,
        string Source,
        string Id,
        string DisplayName,
        string ExtensionMethod);

    private sealed class SuccessfulDependencyProbeClient :
        ICassandraDependencyProbeClient,
        IClickHouseDependencyProbeClient,
        IConsulDependencyProbeClient,
        IElasticsearchDependencyProbeClient,
        IKafkaDependencyProbeClient,
        IMongoDbDependencyProbeClient,
        IMqttDependencyProbeClient,
        IMySqlDependencyProbeClient,
        INatsDependencyProbeClient,
        INeo4jDependencyProbeClient,
        IOpenSearchDependencyProbeClient,
        IOracleDependencyProbeClient,
        IPostgresDependencyProbeClient,
        IRabbitMqDependencyProbeClient,
        ISqlServerDependencyProbeClient
    {
        private readonly ConcurrentDictionary<string, int> observedProviders = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> ObservedProviders => observedProviders.Keys.ToArray();

        public ValueTask<string> ProbeAsync(CassandraDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("Cassandra", dependency.Id);

        public ValueTask<string> ProbeAsync(ClickHouseDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("ClickHouse", dependency.Id);

        public ValueTask<string> ProbeAsync(ConsulDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("Consul", dependency.Id);

        public ValueTask<string> ProbeAsync(ElasticsearchDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("Elasticsearch", dependency.Id);

        public ValueTask<string> ProbeAsync(KafkaDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("Kafka", dependency.Id);

        public ValueTask<string> ProbeAsync(MongoDbDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("MongoDB", dependency.Id);

        public ValueTask<string> ProbeAsync(MqttDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("MQTT", dependency.Id);

        public ValueTask<string> ProbeAsync(MySqlDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("MySQL", dependency.Id);

        public ValueTask<string> ProbeAsync(NatsDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("NATS", dependency.Id);

        public ValueTask<string> ProbeAsync(Neo4jDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("Neo4j", dependency.Id);

        public ValueTask<string> ProbeAsync(OpenSearchDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("OpenSearch", dependency.Id);

        public ValueTask<string> ProbeAsync(OracleDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("Oracle", dependency.Id);

        public ValueTask<string> ProbeAsync(PostgresDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("Postgres", dependency.Id);

        public ValueTask<string> ProbeAsync(RabbitMqDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("RabbitMQ", dependency.Id);

        public ValueTask<string> ProbeAsync(SqlServerDependencyDefinition dependency, CancellationToken cancellationToken) =>
            RecordAsync("SQL Server", dependency.Id);

        private ValueTask<string> RecordAsync(string provider, string dependencyId)
        {
            observedProviders.AddOrUpdate(provider, 1, static (_, count) => count + 1);
            return ValueTask.FromResult($"{provider} dependency '{dependencyId}' executed the managed dependency-health probe path.");
        }
    }

    private sealed class DependencyHealthHttpServer : IAsyncDisposable
    {
        private readonly HttpListener listener = new();
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly IReadOnlyDictionary<string, int> responseCodes;
        private readonly Task listenLoop;

        public DependencyHealthHttpServer(IReadOnlyDictionary<string, int> responseCodes)
        {
            ArgumentNullException.ThrowIfNull(responseCodes);

            this.responseCodes = responseCodes;
            var port = ReservePort();
            BaseUrl = $"http://127.0.0.1:{port}";
            listener.Prefixes.Add($"{BaseUrl}/");
            listener.Start();
            listenLoop = Task.Run(ListenAsync);
        }

        public string BaseUrl { get; }

        public string GetUrl(string path) => $"{BaseUrl}/{path.TrimStart('/')}";

        public async ValueTask DisposeAsync()
        {
            cancellationSource.Cancel();
            listener.Stop();
            listener.Close();

            try
            {
                await listenLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpListenerException)
            {
            }

            cancellationSource.Dispose();
        }

        private async Task ListenAsync()
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (HttpListenerException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }

                var rawPath = context.Request.Url?.AbsolutePath ?? "/";
                context.Response.StatusCode = responseCodes.TryGetValue(rawPath, out var statusCode) ? statusCode : 404;
                context.Response.Close();
            }
        }
    }

    private sealed class FakeMemcachedServer : IAsyncDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly Task acceptLoop;

        public FakeMemcachedServer()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            acceptLoop = Task.Run(AcceptLoopAsync);
        }

        public int Port { get; }

        public async ValueTask DisposeAsync()
        {
            cancellationSource.Cancel();
            listener.Stop();

            try
            {
                await acceptLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }

            cancellationSource.Dispose();
        }

        private async Task AcceptLoopAsync()
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, cancellationSource.Token), CancellationToken.None);
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var ownedClient = client;
            await using var stream = ownedClient.GetStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                string line;
                try
                {
                    line = await ReadLineAsync(stream, cancellationToken);
                }
                catch (IOException)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                await WriteLineAsync(
                    stream,
                    string.Equals(line, "version", StringComparison.OrdinalIgnoreCase) ? "VERSION 1.6.31" : "ERROR",
                    cancellationToken);
            }
        }
    }

    private sealed class FakeRedisServer : IAsyncDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly Task acceptLoop;

        public FakeRedisServer()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            acceptLoop = Task.Run(AcceptLoopAsync);
        }

        public int Port { get; }

        public async ValueTask DisposeAsync()
        {
            cancellationSource.Cancel();
            listener.Stop();

            try
            {
                await acceptLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }

            cancellationSource.Dispose();
        }

        private async Task AcceptLoopAsync()
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, cancellationSource.Token), CancellationToken.None);
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var ownedClient = client;
            await using var stream = ownedClient.GetStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                string[] command;
                try
                {
                    command = await ReadRedisCommandAsync(stream, cancellationToken);
                }
                catch (IOException)
                {
                    break;
                }

                if (command.Length == 0)
                {
                    break;
                }

                await WriteRedisFrameAsync(
                    stream,
                    string.Equals(command[0], "PING", StringComparison.OrdinalIgnoreCase) ? "+PONG\r\n" : "-ERR unsupported command\r\n",
                    cancellationToken);
            }
        }
    }

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        var buffer = new byte[1];
        var sawCarriageReturn = false;

        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                throw new IOException("Unexpected end of stream while reading a line.");
            }

            var current = (char)buffer[0];
            if (sawCarriageReturn)
            {
                if (current == '\n')
                {
                    break;
                }

                builder.Append('\r');
                sawCarriageReturn = false;
            }

            if (current == '\r')
            {
                sawCarriageReturn = true;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static async Task WriteLineAsync(NetworkStream stream, string value, CancellationToken cancellationToken)
    {
        var bytes = Encoding.ASCII.GetBytes($"{value}\r\n");
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<string[]> ReadRedisCommandAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var header = await ReadLineAsync(stream, cancellationToken);
        if (string.IsNullOrWhiteSpace(header))
        {
            return [];
        }

        if (!header.StartsWith('*') || !int.TryParse(header[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count < 0)
        {
            throw new IOException($"Unexpected Redis array header '{header}'.");
        }

        var parts = new string[count];
        for (var index = 0; index < count; index++)
        {
            var bulkHeader = await ReadLineAsync(stream, cancellationToken);
            if (!bulkHeader.StartsWith('$') || !int.TryParse(bulkHeader[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length < 0)
            {
                throw new IOException($"Unexpected Redis bulk header '{bulkHeader}'.");
            }

            parts[index] = await ReadFixedStringAsync(stream, length, cancellationToken);
            await ReadLineAsync(stream, cancellationToken);
        }

        return parts;
    }

    private static async Task<string> ReadFixedStringAsync(NetworkStream stream, int length, CancellationToken cancellationToken)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
            {
                throw new IOException("Unexpected end of stream while reading a fixed string.");
            }

            offset += read;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    private static async Task WriteRedisFrameAsync(NetworkStream stream, string value, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
