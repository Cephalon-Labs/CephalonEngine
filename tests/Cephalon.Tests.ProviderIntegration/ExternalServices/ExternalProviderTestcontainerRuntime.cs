using System.Net;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Cephalon.Tests.ProviderIntegration.ExternalServices;

internal sealed class ExternalProviderTestcontainerRuntime : IAsyncDisposable
{
    private const string CassandraImage = "cassandra:4.1";
    private const string ClickHouseImage = "clickhouse/clickhouse-server:24.8-alpine";
    private const string ElasticsearchImage = "docker.elastic.co/elasticsearch/elasticsearch:8.17.0";
    private const string NatsImage = "nats:2.10-alpine";
    private const string Neo4jImage = "neo4j:5-community";
    private const string OpenSearchImage = "opensearchproject/opensearch:2.18.0";
    private const string QdrantImage = "qdrant/qdrant:v1.12.5";
    private const string Neo4jUsername = "neo4j";
    private const string Neo4jPassword = "cephalon-test-password";
    private static readonly TimeSpan HttpReadyDelay = TimeSpan.FromMilliseconds(500);
    private readonly List<IContainer> _containers = [];

    public async ValueTask DisposeAsync()
    {
        if (_containers.Count == 0)
        {
            return;
        }

        foreach (var container in _containers.AsEnumerable().Reverse())
        {
            await container.DisposeAsync().ConfigureAwait(false);
        }

        _containers.Clear();
    }

    internal async Task<CassandraProviderService> ResolveCassandraAsync(
        ExternalProviderServiceGate gate,
        string uniqueId)
    {
        return gate.ResolveCassandraMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => new CassandraProviderService(
                gate.CassandraContactPoints!,
                gate.CassandraPortOrDefault,
                gate.CassandraKeyspace!),
            ExternalProviderServiceMode.Testcontainers => await StartCassandraAsync(uniqueId).ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    internal async Task<ClickHouseProviderService> ResolveClickHouseAsync(
        ExternalProviderServiceGate gate,
        string uniqueId)
    {
        return gate.ResolveClickHouseMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => new ClickHouseProviderService(
                gate.ClickHouseHost!,
                gate.ClickHousePortOrDefault,
                gate.ClickHouseDatabase!,
                gate.ClickHouseUsernameOrDefault,
                gate.ClickHousePasswordOrDefault),
            ExternalProviderServiceMode.Testcontainers => await StartClickHouseAsync(uniqueId).ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    internal async Task<ElasticsearchProviderService> ResolveElasticsearchAsync(
        ExternalProviderServiceGate gate)
    {
        return gate.ResolveElasticsearchMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => new ElasticsearchProviderService(
                gate.ElasticsearchUri!,
                gate.ElasticsearchUsername,
                gate.ElasticsearchPassword),
            ExternalProviderServiceMode.Testcontainers => await StartElasticsearchAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    internal async Task<NatsProviderService> ResolveNatsAsync(
        ExternalProviderServiceGate gate)
    {
        return gate.ResolveNatsMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => new NatsProviderService(gate.NatsUri!),
            ExternalProviderServiceMode.Testcontainers => await StartNatsAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    internal async Task<Neo4jProviderService> ResolveNeo4jAsync(
        ExternalProviderServiceGate gate)
    {
        return gate.ResolveNeo4jMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => new Neo4jProviderService(
                gate.Neo4jUri!,
                gate.Neo4jUsername!,
                gate.Neo4jPassword!),
            ExternalProviderServiceMode.Testcontainers => await StartNeo4jAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    internal async Task<OpenSearchProviderService> ResolveOpenSearchAsync(
        ExternalProviderServiceGate gate)
    {
        return gate.ResolveOpenSearchMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => new OpenSearchProviderService(
                gate.OpenSearchUri!,
                gate.OpenSearchUsername,
                gate.OpenSearchPassword),
            ExternalProviderServiceMode.Testcontainers => await StartOpenSearchAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    internal async Task<QdrantProviderService> ResolveQdrantAsync(
        ExternalProviderServiceGate gate)
    {
        return gate.ResolveQdrantMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => new QdrantProviderService(
                gate.QdrantHost!,
                gate.QdrantPortOrDefault,
                gate.QdrantApiKey),
            ExternalProviderServiceMode.Testcontainers => await StartQdrantAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    private async Task<CassandraProviderService> StartCassandraAsync(string uniqueId)
    {
        var container = await StartContainerAsync(new ContainerBuilder(CassandraImage)
                .WithPortBinding(9042, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9042).UntilExternalTcpPortIsAvailable(9042))
                .Build())
            .ConfigureAwait(false);

        return new CassandraProviderService(container.Hostname, container.GetMappedPublicPort(9042), $"cephalon_it_{uniqueId}");
    }

    private async Task<ClickHouseProviderService> StartClickHouseAsync(string uniqueId)
    {
        var database = $"cephalon_it_{uniqueId}";
        var container = await StartContainerAsync(new ContainerBuilder(ClickHouseImage)
                .WithEnvironment("CLICKHOUSE_DB", database)
                .WithPortBinding(8123, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8123).UntilExternalTcpPortIsAvailable(8123))
                .Build())
            .ConfigureAwait(false);

        var service = new ClickHouseProviderService(container.Hostname, container.GetMappedPublicPort(8123), database, "default", string.Empty);
        await WaitForHttpReadyAsync($"http://{service.Host}:{service.Port}/ping").ConfigureAwait(false);
        return service;
    }

    private async Task<ElasticsearchProviderService> StartElasticsearchAsync()
    {
        var container = await StartContainerAsync(new ContainerBuilder(ElasticsearchImage)
                .WithEnvironment("discovery.type", "single-node")
                .WithEnvironment("xpack.security.enabled", "false")
                .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
                .WithPortBinding(9200, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9200).UntilExternalTcpPortIsAvailable(9200))
                .Build())
            .ConfigureAwait(false);

        var uri = $"http://{container.Hostname}:{container.GetMappedPublicPort(9200)}";
        await WaitForHttpReadyAsync(uri).ConfigureAwait(false);
        return new ElasticsearchProviderService(uri, null, null);
    }

    private async Task<NatsProviderService> StartNatsAsync()
    {
        var container = await StartContainerAsync(new ContainerBuilder(NatsImage)
                .WithCommand("-js")
                .WithPortBinding(4222, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(4222).UntilExternalTcpPortIsAvailable(4222))
                .Build())
            .ConfigureAwait(false);

        return new NatsProviderService($"nats://{container.Hostname}:{container.GetMappedPublicPort(4222)}");
    }

    private async Task<Neo4jProviderService> StartNeo4jAsync()
    {
        var container = await StartContainerAsync(new ContainerBuilder(Neo4jImage)
                .WithEnvironment("NEO4J_AUTH", $"{Neo4jUsername}/{Neo4jPassword}")
                .WithPortBinding(7687, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(7687).UntilExternalTcpPortIsAvailable(7687))
                .Build())
            .ConfigureAwait(false);

        return new Neo4jProviderService($"bolt://{container.Hostname}:{container.GetMappedPublicPort(7687)}", Neo4jUsername, Neo4jPassword);
    }

    private async Task<OpenSearchProviderService> StartOpenSearchAsync()
    {
        var container = await StartContainerAsync(new ContainerBuilder(OpenSearchImage)
                .WithEnvironment("discovery.type", "single-node")
                .WithEnvironment("plugins.security.disabled", "true")
                .WithEnvironment("DISABLE_SECURITY_PLUGIN", "true")
                .WithEnvironment("OPENSEARCH_INITIAL_ADMIN_PASSWORD", "cephalon-test-password")
                .WithEnvironment("OPENSEARCH_JAVA_OPTS", "-Xms512m -Xmx512m")
                .WithPortBinding(9200, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9200).UntilExternalTcpPortIsAvailable(9200))
                .Build())
            .ConfigureAwait(false);

        var uri = $"http://{container.Hostname}:{container.GetMappedPublicPort(9200)}";
        await WaitForHttpReadyAsync(uri).ConfigureAwait(false);
        return new OpenSearchProviderService(uri, null, null);
    }

    private async Task<QdrantProviderService> StartQdrantAsync()
    {
        var container = await StartContainerAsync(new ContainerBuilder(QdrantImage)
                .WithPortBinding(6334, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(6334).UntilExternalTcpPortIsAvailable(6334))
                .Build())
            .ConfigureAwait(false);

        return new QdrantProviderService(container.Hostname, container.GetMappedPublicPort(6334), null);
    }

    private async Task<IContainer> StartContainerAsync(IContainer container)
    {
        _containers.Add(container);
        await container.StartAsync().ConfigureAwait(false);
        return container;
    }

    private static async Task WaitForHttpReadyAsync(string uri)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (var attempt = 0; attempt < 120; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(uri).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.ServiceUnavailable &&
                    response.StatusCode != HttpStatusCode.BadGateway &&
                    response.StatusCode != HttpStatusCode.GatewayTimeout)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Service ports often open before the HTTP layer finishes booting.
            }
            catch (TaskCanceledException)
            {
                // Keep polling until the bounded readiness window expires.
            }

            await Task.Delay(HttpReadyDelay).ConfigureAwait(false);
        }

        throw new TimeoutException($"Timed out waiting for {uri} to become ready.");
    }
}

internal sealed record CassandraProviderService(string ContactPoints, int Port, string Keyspace);

internal sealed record ClickHouseProviderService(string Host, int Port, string Database, string Username, string Password);

internal sealed record ElasticsearchProviderService(string Uri, string? Username, string? Password);

internal sealed record NatsProviderService(string Uri);

internal sealed record Neo4jProviderService(string Uri, string Username, string Password);

internal sealed record OpenSearchProviderService(string Uri, string? Username, string? Password);

internal sealed record QdrantProviderService(string Host, int Port, string? ApiKey);
