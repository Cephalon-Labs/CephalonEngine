namespace Cephalon.Tests.ProviderIntegration.ExternalServices;

public sealed class ExternalProviderServiceGateTests
{
    [Fact]
    public void FromValues_DisablesExternalServicesByDefault()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>());

        Assert.False(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveRedisMode());
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveCassandraMode());
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveNatsMode());
        Assert.Contains(ExternalProviderServiceGate.ExternalServicesVariable, ExternalProviderServiceGate.SkipReason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("yes")]
    [InlineData("on")]
    public void FromValues_EnablesExternalServicesForTruthyValues(string value)
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = value
        });

        Assert.True(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveRedisMode());
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveCassandraMode());
    }

    [Fact]
    public void FromValues_AllowsAliasToEnableExternalServices()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesAliasVariable] = "1"
        });

        Assert.True(gate.ExternalServicesEnabled);
    }

    [Fact]
    public void FromValues_RequiresExternalServicesBeforeTestcontainersModeCanRun()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.TestcontainersVariable] = "1"
        });

        Assert.False(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveRedisMode());
    }

    [Fact]
    public void FromValues_UsesTestcontainersWhenExternalServicesAndTestcontainersAreEnabled()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1",
            [ExternalProviderServiceGate.TestcontainersVariable] = "1"
        });

        Assert.True(gate.ExternalServicesEnabled);
        Assert.True(gate.TestcontainersEnabled);
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveRedisMode());
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveCassandraMode());
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveClickHouseMode());
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveElasticsearchMode());
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveNatsMode());
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveNeo4jMode());
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveOpenSearchMode());
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveQdrantMode());
    }

    [Fact]
    public void FromValues_KeepsProviderTestsSkippedUntilAProviderModeResolves()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1"
        });

        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.Any));
        Assert.Contains(
            ExternalProviderServiceGate.CassandraContactPointsVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.Cassandra)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalProviderServiceGate.ClickHouseHostVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.ClickHouse)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalProviderServiceGate.ElasticsearchUriVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.Elasticsearch)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalProviderServiceGate.NatsUriVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.Nats)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalProviderServiceGate.Neo4jUriVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.Neo4j)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalProviderServiceGate.OpenSearchUriVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.OpenSearch)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalProviderServiceGate.QdrantHostVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.Qdrant)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalProviderServiceGate.RedisConnectionStringVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.Redis)!,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FromValues_ResolvesPreProvisionedDataProviderModes()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1",
            [ExternalProviderServiceGate.CassandraContactPointsVariable] = " cassandra.local ",
            [ExternalProviderServiceGate.CassandraPortVariable] = "9043",
            [ExternalProviderServiceGate.CassandraKeyspaceVariable] = " cephalon_test ",
            [ExternalProviderServiceGate.ClickHouseHostVariable] = " clickhouse.local ",
            [ExternalProviderServiceGate.ClickHousePortVariable] = "8124",
            [ExternalProviderServiceGate.ClickHouseDatabaseVariable] = " cephalon_click ",
            [ExternalProviderServiceGate.ClickHouseUsernameVariable] = " cephalon ",
            [ExternalProviderServiceGate.ClickHousePasswordVariable] = " click-secret ",
            [ExternalProviderServiceGate.ElasticsearchUriVariable] = " http://elastic.local:9200 ",
            [ExternalProviderServiceGate.ElasticsearchUsernameVariable] = " elastic ",
            [ExternalProviderServiceGate.ElasticsearchPasswordVariable] = " elastic-secret ",
            [ExternalProviderServiceGate.NatsUriVariable] = " nats://nats.local:4222 ",
            [ExternalProviderServiceGate.Neo4jUriVariable] = " bolt://neo4j.local:7687 ",
            [ExternalProviderServiceGate.Neo4jUsernameVariable] = " neo4j ",
            [ExternalProviderServiceGate.Neo4jPasswordVariable] = " neo-secret ",
            [ExternalProviderServiceGate.OpenSearchUriVariable] = " http://opensearch.local:9200 ",
            [ExternalProviderServiceGate.OpenSearchUsernameVariable] = " admin ",
            [ExternalProviderServiceGate.OpenSearchPasswordVariable] = " open-secret ",
            [ExternalProviderServiceGate.QdrantHostVariable] = " qdrant.local ",
            [ExternalProviderServiceGate.QdrantPortVariable] = "6335",
            [ExternalProviderServiceGate.QdrantApiKeyVariable] = " qdrant-secret "
        });

        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveCassandraMode());
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveClickHouseMode());
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveElasticsearchMode());
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveNatsMode());
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveNeo4jMode());
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveOpenSearchMode());
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveQdrantMode());
        Assert.Equal("cassandra.local", gate.CassandraContactPoints);
        Assert.Equal(9043, gate.CassandraPortOrDefault);
        Assert.Equal("cephalon_test", gate.CassandraKeyspace);
        Assert.Equal("clickhouse.local", gate.ClickHouseHost);
        Assert.Equal(8124, gate.ClickHousePortOrDefault);
        Assert.Equal("cephalon_click", gate.ClickHouseDatabase);
        Assert.Equal("cephalon", gate.ClickHouseUsernameOrDefault);
        Assert.Equal("click-secret", gate.ClickHousePasswordOrDefault);
        Assert.Equal("http://elastic.local:9200", gate.ElasticsearchUri);
        Assert.Equal("elastic", gate.ElasticsearchUsername);
        Assert.Equal("elastic-secret", gate.ElasticsearchPassword);
        Assert.Equal("nats://nats.local:4222", gate.NatsUri);
        Assert.Equal("bolt://neo4j.local:7687", gate.Neo4jUri);
        Assert.Equal("neo4j", gate.Neo4jUsername);
        Assert.Equal("neo-secret", gate.Neo4jPassword);
        Assert.Equal("http://opensearch.local:9200", gate.OpenSearchUri);
        Assert.Equal("admin", gate.OpenSearchUsername);
        Assert.Equal("open-secret", gate.OpenSearchPassword);
        Assert.Equal("qdrant.local", gate.QdrantHost);
        Assert.Equal(6335, gate.QdrantPortOrDefault);
        Assert.Equal("qdrant-secret", gate.QdrantApiKey);
        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.Cassandra));
        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.ClickHouse));
        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.Elasticsearch));
        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.Nats));
        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.Neo4j));
        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.OpenSearch));
        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.Qdrant));
    }

    [Fact]
    public void FromValues_PrefersPreProvisionedDataProviderSettingsOverTestcontainersAndKeepsDefaultPorts()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1",
            [ExternalProviderServiceGate.TestcontainersVariable] = "1",
            [ExternalProviderServiceGate.CassandraContactPointsVariable] = "cassandra.local",
            [ExternalProviderServiceGate.CassandraKeyspaceVariable] = "cephalon_test",
            [ExternalProviderServiceGate.ClickHouseHostVariable] = "clickhouse.local",
            [ExternalProviderServiceGate.ClickHouseDatabaseVariable] = "cephalon",
            [ExternalProviderServiceGate.QdrantHostVariable] = "qdrant.local"
        });

        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveCassandraMode());
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveClickHouseMode());
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveQdrantMode());
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveNatsMode());
        Assert.Equal(9042, gate.CassandraPortOrDefault);
        Assert.Equal(8123, gate.ClickHousePortOrDefault);
        Assert.Equal("default", gate.ClickHouseUsernameOrDefault);
        Assert.Equal(string.Empty, gate.ClickHousePasswordOrDefault);
        Assert.Equal(6334, gate.QdrantPortOrDefault);
    }

    [Fact]
    public void FromValues_PrefersPreProvisionedConnectionStringOverTestcontainers()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1",
            [ExternalProviderServiceGate.TestcontainersVariable] = "1",
            [ExternalProviderServiceGate.RedisConnectionStringVariable] = " localhost:6379 "
        });

        Assert.Equal("localhost:6379", gate.RedisConnectionString);
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveRedisMode());
    }

    [Fact]
    public void FromValues_UsesRedisConnectionStringAlias()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1",
            [ExternalProviderServiceGate.RedisConnectionStringAliasVariable] = " redis.local:6379 "
        });

        Assert.Equal("redis.local:6379", gate.RedisConnectionString);
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveRedisMode());
    }

    [ExternalProviderServiceFact]
    public void ExternalProviderServiceFact_RemainsDiscoverableWhenTheLaneIsEnabled()
    {
        Assert.True(ExternalProviderServiceGate.FromEnvironment().ExternalServicesEnabled);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Redis)]
    public void RedisExternalProviderServiceFact_RemainsDiscoverableWhenAProviderModeIsEnabled()
    {
        Assert.NotEqual(ExternalProviderServiceMode.Disabled, ExternalProviderServiceGate.FromEnvironment().ResolveRedisMode());
    }
}
