namespace Cephalon.Tests.Tooling;

public sealed class DocumentationCoverageTests
{
    private static readonly Dictionary<string, string> ComponentDocByProject =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Cephalon.Abstractions"] = "abstractions.md",
            ["Cephalon.Agentics"] = "agentics.md",
            ["Cephalon.AspNetCore"] = "aspnetcore.md",
            ["Cephalon.AspNetCore.GraphQL"] = "aspnetcore-graphql.md",
            ["Cephalon.AspNetCore.Grpc"] = "aspnetcore-grpc.md",
            ["Cephalon.AspNetCore.JsonRpc"] = "aspnetcore-jsonrpc.md",
            ["Cephalon.Cli"] = "cli.md",
            ["Cephalon.Edge"] = "edge.md",
            ["Cephalon.Engine"] = "engine.md",
            ["Cephalon.Eventing"] = "eventing.md",
            ["Cephalon.Observability"] = "observability.md",
            ["Cephalon.Observability.CassandraDependencies"] = "observability-cassandra-dependencies.md",
            ["Cephalon.Observability.ClickHouseDependencies"] = "observability-clickhouse-dependencies.md",
            ["Cephalon.Observability.ConsulDependencies"] = "observability-consul-dependencies.md",
            ["Cephalon.Observability.ElasticsearchDependencies"] = "observability-elasticsearch-dependencies.md",
            ["Cephalon.Observability.HttpDependencies"] = "observability-http-dependencies.md",
            ["Cephalon.Observability.KafkaDependencies"] = "observability-kafka-dependencies.md",
            ["Cephalon.Observability.MemcachedDependencies"] = "observability-memcached-dependencies.md",
            ["Cephalon.Observability.MongoDbDependencies"] = "observability-mongodb-dependencies.md",
            ["Cephalon.Observability.MqttDependencies"] = "observability-mqtt-dependencies.md",
            ["Cephalon.Observability.MySqlDependencies"] = "observability-mysql-dependencies.md",
            ["Cephalon.Observability.NatsDependencies"] = "observability-nats-dependencies.md",
            ["Cephalon.Observability.Neo4jDependencies"] = "observability-neo4j-dependencies.md",
            ["Cephalon.Observability.OpenSearchDependencies"] = "observability-opensearch-dependencies.md",
            ["Cephalon.Observability.OracleDependencies"] = "observability-oracle-dependencies.md",
            ["Cephalon.Observability.PostgresDependencies"] = "observability-postgres-dependencies.md",
            ["Cephalon.Observability.RabbitMqDependencies"] = "observability-rabbitmq-dependencies.md",
            ["Cephalon.Observability.RedisDependencies"] = "observability-redis-dependencies.md",
            ["Cephalon.Observability.SqlServerDependencies"] = "observability-sqlserver-dependencies.md",
            ["Cephalon.Observability.OpenTelemetry"] = "observability-opentelemetry.md",
            ["Cephalon.Observability.Serilog"] = "observability-serilog.md",
            ["Cephalon.ReferenceDocs"] = "reference-docs.md",
            ["Cephalon.Retrieval"] = "retrieval.md",
            ["Cephalon.Scaffolding"] = "scaffolding.md",
            ["Cephalon.Worker"] = "worker.md",
        };

    [Fact]
    public void EveryShippedSourceProjectHasAComponentDocumentAndCatalogEntry()
    {
        var repositoryRoot = GetRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");
        var componentCatalog = File.ReadAllText(Path.Combine(componentDocsRoot, "README.md"));

        var projectDirectories = Directory
            .GetDirectories(sourceRoot, "Cephalon.*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(projectDirectories.Length, ComponentDocByProject.Count);

        foreach (var projectName in projectDirectories)
        {
            Assert.True(
                ComponentDocByProject.TryGetValue(projectName!, out var componentDocFileName),
                $"Add a component-doc mapping for '{projectName}' in {nameof(DocumentationCoverageTests)}.");

            var componentDocPath = Path.Combine(componentDocsRoot, componentDocFileName);
            Assert.True(
                File.Exists(componentDocPath),
                $"Expected a component doc for '{projectName}' at '{componentDocPath}'.");

            var componentDocContents = File.ReadAllText(componentDocPath);
            Assert.Contains(projectName!, componentDocContents, StringComparison.Ordinal);
            Assert.Contains(
                $"[{projectName}]({componentDocFileName})",
                componentCatalog,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DocsHubLinksToCoreDocumentationSurfaces()
    {
        var docsReadme = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "docs", "README.md"));

        Assert.Contains("[Architecture](architecture.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Component catalog](components/README.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[App models](app-models.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Module authoring](module-authoring.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Technology packs](technology-packs.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Operations](operations.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Runtime failure policy](runtime-failure-policy.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Benchmarking](benchmarking.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Reference docs publishing](reference-docs.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Engine roadmap](engine-roadmap.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Engine backlog](engine-backlog.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Reference landing page](reference/README.md)", docsReadme, StringComparison.Ordinal);
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
}
