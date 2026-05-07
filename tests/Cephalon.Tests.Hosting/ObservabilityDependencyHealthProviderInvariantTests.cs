using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.CassandraDependencies.Configuration;
using Cephalon.Observability.CassandraDependencies.Hosting;
using Cephalon.Observability.ClickHouseDependencies.Configuration;
using Cephalon.Observability.ClickHouseDependencies.Hosting;
using Cephalon.Observability.ConsulDependencies.Configuration;
using Cephalon.Observability.ConsulDependencies.Hosting;
using Cephalon.Observability.ElasticsearchDependencies.Configuration;
using Cephalon.Observability.ElasticsearchDependencies.Hosting;
using Cephalon.Observability.HttpDependencies.Configuration;
using Cephalon.Observability.HttpDependencies.Hosting;
using Cephalon.Observability.KafkaDependencies.Configuration;
using Cephalon.Observability.KafkaDependencies.Hosting;
using Cephalon.Observability.MemcachedDependencies.Configuration;
using Cephalon.Observability.MemcachedDependencies.Hosting;
using Cephalon.Observability.MongoDbDependencies.Configuration;
using Cephalon.Observability.MongoDbDependencies.Hosting;
using Cephalon.Observability.MqttDependencies.Configuration;
using Cephalon.Observability.MqttDependencies.Hosting;
using Cephalon.Observability.MySqlDependencies.Configuration;
using Cephalon.Observability.MySqlDependencies.Hosting;
using Cephalon.Observability.NatsDependencies.Configuration;
using Cephalon.Observability.NatsDependencies.Hosting;
using Cephalon.Observability.Neo4jDependencies.Configuration;
using Cephalon.Observability.Neo4jDependencies.Hosting;
using Cephalon.Observability.OpenSearchDependencies.Configuration;
using Cephalon.Observability.OpenSearchDependencies.Hosting;
using Cephalon.Observability.OracleDependencies.Configuration;
using Cephalon.Observability.OracleDependencies.Hosting;
using Cephalon.Observability.PostgresDependencies.Configuration;
using Cephalon.Observability.PostgresDependencies.Hosting;
using Cephalon.Observability.RabbitMqDependencies.Configuration;
using Cephalon.Observability.RabbitMqDependencies.Hosting;
using Cephalon.Observability.RedisDependencies.Configuration;
using Cephalon.Observability.RedisDependencies.Hosting;
using Cephalon.Observability.SqlServerDependencies.Configuration;
using Cephalon.Observability.SqlServerDependencies.Hosting;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class ObservabilityDependencyHealthProviderInvariantTests
{
    private static readonly ProviderExpectation[] ProviderExpectations =
    [
        new("Cassandra", "Cephalon.Observability.CassandraDependencies", "required-cassandra", "Required Cassandra"),
        new("ClickHouse", "Cephalon.Observability.ClickHouseDependencies", "required-clickhouse", "Required ClickHouse"),
        new("Consul", "Cephalon.Observability.ConsulDependencies", "required-consul", "Required Consul"),
        new("Elasticsearch", "Cephalon.Observability.ElasticsearchDependencies", "required-elasticsearch", "Required Elasticsearch"),
        new("HTTP", "Cephalon.Observability.HttpDependencies", "required-http", "Required HTTP"),
        new("Kafka", "Cephalon.Observability.KafkaDependencies", "required-kafka", "Required Kafka"),
        new("Memcached", "Cephalon.Observability.MemcachedDependencies", "required-memcached", "Required Memcached"),
        new("MongoDB", "Cephalon.Observability.MongoDbDependencies", "required-mongodb", "Required MongoDB"),
        new("MQTT", "Cephalon.Observability.MqttDependencies", "required-mqtt", "Required MQTT"),
        new("MySQL", "Cephalon.Observability.MySqlDependencies", "required-mysql", "Required MySQL"),
        new("NATS", "Cephalon.Observability.NatsDependencies", "required-nats", "Required NATS"),
        new("Neo4j", "Cephalon.Observability.Neo4jDependencies", "required-neo4j", "Required Neo4j"),
        new("OpenSearch", "Cephalon.Observability.OpenSearchDependencies", "required-opensearch", "Required OpenSearch"),
        new("Oracle", "Cephalon.Observability.OracleDependencies", "required-oracle", "Required Oracle"),
        new("Postgres", "Cephalon.Observability.PostgresDependencies", "required-postgres", "Required Postgres"),
        new("RabbitMQ", "Cephalon.Observability.RabbitMqDependencies", "required-rabbitmq", "Required RabbitMQ"),
        new("Redis", "Cephalon.Observability.RedisDependencies", "required-redis", "Required Redis"),
        new("SQL Server", "Cephalon.Observability.SqlServerDependencies", "required-sqlserver", "Required SQL Server")
    ];

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

            Assert.Equal(ProviderExpectations.Length, dependencies.Length);
            Assert.Equal(
                ProviderExpectations.Select(static provider => provider.Source),
                dependencies.Select(static dependency => dependency.Source));
            Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
            Assert.Equal(ProviderExpectations.Length, readiness.Dependencies.Count);

            foreach (var provider in ProviderExpectations)
            {
                var dependency = Assert.Single(dependencies, report => report.Source == provider.Source);
                Assert.Equal(provider.Id, dependency.Id);
                Assert.Equal(provider.DisplayName, dependency.DisplayName);
                Assert.True(dependency.Required);
                Assert.Equal(HealthState.Unhealthy, dependency.State);
                Assert.False(string.IsNullOrWhiteSpace(dependency.Description));

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

    private sealed record ProviderExpectation(string Provider, string Source, string Id, string DisplayName);
}
