namespace Cephalon.Tests.ProviderIntegration.ExternalServices;

internal sealed record ExternalProviderServiceGate(
    bool ExternalServicesEnabled,
    bool TestcontainersEnabled,
    string? RedisConnectionString,
    string? CassandraContactPoints,
    int? CassandraPort,
    string? CassandraKeyspace,
    string? ClickHouseHost,
    int? ClickHousePort,
    string? ClickHouseDatabase,
    string? ClickHouseUsername,
    string? ClickHousePassword,
    string? ElasticsearchUri,
    string? ElasticsearchUsername,
    string? ElasticsearchPassword,
    string? NatsUri,
    string? Neo4jUri,
    string? Neo4jUsername,
    string? Neo4jPassword,
    string? OpenSearchUri,
    string? OpenSearchUsername,
    string? OpenSearchPassword,
    string? QdrantHost,
    int? QdrantPort,
    string? QdrantApiKey,
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpApiUri)
{
    internal const string ExternalServicesVariable = "CEPHALON_PROVIDER_EXTERNAL_SERVICES";
    internal const string ExternalServicesAliasVariable = "CEPHALON_PROVIDER_INTEGRATION";
    internal const string TestcontainersVariable = "CEPHALON_PROVIDER_TESTCONTAINERS";
    internal const string CassandraContactPointsVariable = "CEPHALON_PROVIDER_CASSANDRA_CONTACT_POINTS";
    internal const string CassandraPortVariable = "CEPHALON_PROVIDER_CASSANDRA_PORT";
    internal const string CassandraKeyspaceVariable = "CEPHALON_PROVIDER_CASSANDRA_KEYSPACE";
    internal const string ClickHouseHostVariable = "CEPHALON_PROVIDER_CLICKHOUSE_HOST";
    internal const string ClickHousePortVariable = "CEPHALON_PROVIDER_CLICKHOUSE_PORT";
    internal const string ClickHouseDatabaseVariable = "CEPHALON_PROVIDER_CLICKHOUSE_DATABASE";
    internal const string ClickHouseUsernameVariable = "CEPHALON_PROVIDER_CLICKHOUSE_USERNAME";
    internal const string ClickHousePasswordVariable = "CEPHALON_PROVIDER_CLICKHOUSE_PASSWORD";
    internal const string ElasticsearchUriVariable = "CEPHALON_PROVIDER_ELASTICSEARCH_URI";
    internal const string ElasticsearchUsernameVariable = "CEPHALON_PROVIDER_ELASTICSEARCH_USERNAME";
    internal const string ElasticsearchPasswordVariable = "CEPHALON_PROVIDER_ELASTICSEARCH_PASSWORD";
    internal const string NatsUriVariable = "CEPHALON_PROVIDER_NATS_URI";
    internal const string Neo4jUriVariable = "CEPHALON_PROVIDER_NEO4J_URI";
    internal const string Neo4jUsernameVariable = "CEPHALON_PROVIDER_NEO4J_USERNAME";
    internal const string Neo4jPasswordVariable = "CEPHALON_PROVIDER_NEO4J_PASSWORD";
    internal const string OpenSearchUriVariable = "CEPHALON_PROVIDER_OPENSEARCH_URI";
    internal const string OpenSearchUsernameVariable = "CEPHALON_PROVIDER_OPENSEARCH_USERNAME";
    internal const string OpenSearchPasswordVariable = "CEPHALON_PROVIDER_OPENSEARCH_PASSWORD";
    internal const string QdrantHostVariable = "CEPHALON_PROVIDER_QDRANT_HOST";
    internal const string QdrantPortVariable = "CEPHALON_PROVIDER_QDRANT_PORT";
    internal const string QdrantApiKeyVariable = "CEPHALON_PROVIDER_QDRANT_API_KEY";
    internal const string SmtpHostVariable = "CEPHALON_PROVIDER_SMTP_HOST";
    internal const string SmtpPortVariable = "CEPHALON_PROVIDER_SMTP_PORT";
    internal const string SmtpApiUriVariable = "CEPHALON_PROVIDER_SMTP_API_URI";
    internal const string RedisConnectionStringVariable = "CEPHALON_PROVIDER_REDIS_CONNECTION_STRING";
    internal const string RedisConnectionStringAliasVariable = "CEPHALON_REDIS_CONNECTION_STRING";

    internal static string SkipReason =>
        $"External provider service tests are disabled. Set {ExternalServicesVariable}=1 and either {TestcontainersVariable}=1 or the provider-specific service variables to run this lane.";

    internal int CassandraPortOrDefault => CassandraPort ?? 9042;

    internal int ClickHousePortOrDefault => ClickHousePort ?? 8123;

    internal string ClickHouseUsernameOrDefault => ClickHouseUsername ?? "default";

    internal string ClickHousePasswordOrDefault => ClickHousePassword ?? string.Empty;

    internal int QdrantPortOrDefault => QdrantPort ?? 6334;

    internal int SmtpPortOrDefault => SmtpPort ?? 1025;

    internal static ExternalProviderServiceGate FromEnvironment()
    {
        return FromResolver(Environment.GetEnvironmentVariable);
    }

    internal static ExternalProviderServiceGate FromValues(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return FromResolver(name => values.TryGetValue(name, out var value) ? value : null);
    }

    internal ExternalProviderServiceMode ResolveRedisMode()
    {
        return ResolveProviderMode(!string.IsNullOrWhiteSpace(RedisConnectionString), allowTestcontainers: true);
    }

    internal ExternalProviderServiceMode ResolveCassandraMode()
    {
        return ResolveProviderMode(
            !string.IsNullOrWhiteSpace(CassandraContactPoints) &&
            !string.IsNullOrWhiteSpace(CassandraKeyspace),
            allowTestcontainers: true);
    }

    internal ExternalProviderServiceMode ResolveClickHouseMode()
    {
        return ResolveProviderMode(
            !string.IsNullOrWhiteSpace(ClickHouseHost) &&
            !string.IsNullOrWhiteSpace(ClickHouseDatabase),
            allowTestcontainers: true);
    }

    internal ExternalProviderServiceMode ResolveElasticsearchMode()
    {
        return ResolveProviderMode(!string.IsNullOrWhiteSpace(ElasticsearchUri), allowTestcontainers: true);
    }

    internal ExternalProviderServiceMode ResolveNatsMode()
    {
        return ResolveProviderMode(!string.IsNullOrWhiteSpace(NatsUri), allowTestcontainers: true);
    }

    internal ExternalProviderServiceMode ResolveNeo4jMode()
    {
        return ResolveProviderMode(
            !string.IsNullOrWhiteSpace(Neo4jUri) &&
            !string.IsNullOrWhiteSpace(Neo4jUsername) &&
            !string.IsNullOrWhiteSpace(Neo4jPassword),
            allowTestcontainers: true);
    }

    internal ExternalProviderServiceMode ResolveOpenSearchMode()
    {
        return ResolveProviderMode(!string.IsNullOrWhiteSpace(OpenSearchUri), allowTestcontainers: true);
    }

    internal ExternalProviderServiceMode ResolveQdrantMode()
    {
        return ResolveProviderMode(!string.IsNullOrWhiteSpace(QdrantHost), allowTestcontainers: true);
    }

    internal ExternalProviderServiceMode ResolveSmtpMode()
    {
        return ResolveProviderMode(
            !string.IsNullOrWhiteSpace(SmtpHost) &&
            !string.IsNullOrWhiteSpace(SmtpApiUri),
            allowTestcontainers: true);
    }

    internal string? GetSkipReason(ExternalProviderServiceProvider provider)
    {
        if (!ExternalServicesEnabled)
        {
            return SkipReason;
        }

        return provider switch
        {
            ExternalProviderServiceProvider.Any => null,
            ExternalProviderServiceProvider.Cassandra when ResolveCassandraMode() == ExternalProviderServiceMode.Disabled =>
                $"{ProviderName(provider)} external-provider tests are disabled. Set {CassandraContactPointsVariable} and {CassandraKeyspaceVariable}, or set {TestcontainersVariable}=1, with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.Cassandra => null,
            ExternalProviderServiceProvider.ClickHouse when ResolveClickHouseMode() == ExternalProviderServiceMode.Disabled =>
                $"{ProviderName(provider)} external-provider tests are disabled. Set {ClickHouseHostVariable} and {ClickHouseDatabaseVariable}, or set {TestcontainersVariable}=1, with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.ClickHouse => null,
            ExternalProviderServiceProvider.Elasticsearch when ResolveElasticsearchMode() == ExternalProviderServiceMode.Disabled =>
                $"{ProviderName(provider)} external-provider tests are disabled. Set {ElasticsearchUriVariable}, or set {TestcontainersVariable}=1, with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.Elasticsearch => null,
            ExternalProviderServiceProvider.Nats when ResolveNatsMode() == ExternalProviderServiceMode.Disabled =>
                $"{ProviderName(provider)} external-provider tests are disabled. Set {NatsUriVariable}, or set {TestcontainersVariable}=1, with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.Nats => null,
            ExternalProviderServiceProvider.Neo4j when ResolveNeo4jMode() == ExternalProviderServiceMode.Disabled =>
                $"{ProviderName(provider)} external-provider tests are disabled. Set {Neo4jUriVariable}, {Neo4jUsernameVariable}, and {Neo4jPasswordVariable}, or set {TestcontainersVariable}=1, with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.Neo4j => null,
            ExternalProviderServiceProvider.OpenSearch when ResolveOpenSearchMode() == ExternalProviderServiceMode.Disabled =>
                $"{ProviderName(provider)} external-provider tests are disabled. Set {OpenSearchUriVariable}, or set {TestcontainersVariable}=1, with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.OpenSearch => null,
            ExternalProviderServiceProvider.Qdrant when ResolveQdrantMode() == ExternalProviderServiceMode.Disabled =>
                $"{ProviderName(provider)} external-provider tests are disabled. Set {QdrantHostVariable}, or set {TestcontainersVariable}=1, with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.Qdrant => null,
            ExternalProviderServiceProvider.Smtp when ResolveSmtpMode() == ExternalProviderServiceMode.Disabled =>
                $"{ProviderName(provider)} external-provider tests are disabled. Set {SmtpHostVariable} and {SmtpApiUriVariable}, or set {TestcontainersVariable}=1, with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.Smtp => null,
            ExternalProviderServiceProvider.Redis when ResolveRedisMode() == ExternalProviderServiceMode.Disabled =>
                $"Redis external-provider tests are disabled. Set {RedisConnectionStringVariable} or set {TestcontainersVariable}=1 with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.Redis => null,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown external provider service provider.")
        };
    }

    private static ExternalProviderServiceGate FromResolver(Func<string, string?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        var externalServicesEnabled =
            IsTruthy(resolver(ExternalServicesVariable)) ||
            IsTruthy(resolver(ExternalServicesAliasVariable));
        var testcontainersEnabled = externalServicesEnabled && IsTruthy(resolver(TestcontainersVariable));
        var redisConnectionString = FirstNonEmpty(
            resolver(RedisConnectionStringVariable),
            resolver(RedisConnectionStringAliasVariable));

        return new ExternalProviderServiceGate(
            externalServicesEnabled,
            testcontainersEnabled,
            redisConnectionString,
            FirstNonEmpty(resolver(CassandraContactPointsVariable)),
            ParsePositiveInt(resolver(CassandraPortVariable)),
            FirstNonEmpty(resolver(CassandraKeyspaceVariable)),
            FirstNonEmpty(resolver(ClickHouseHostVariable)),
            ParsePositiveInt(resolver(ClickHousePortVariable)),
            FirstNonEmpty(resolver(ClickHouseDatabaseVariable)),
            FirstNonEmpty(resolver(ClickHouseUsernameVariable)),
            FirstNonEmpty(resolver(ClickHousePasswordVariable)),
            FirstNonEmpty(resolver(ElasticsearchUriVariable)),
            FirstNonEmpty(resolver(ElasticsearchUsernameVariable)),
            FirstNonEmpty(resolver(ElasticsearchPasswordVariable)),
            FirstNonEmpty(resolver(NatsUriVariable)),
            FirstNonEmpty(resolver(Neo4jUriVariable)),
            FirstNonEmpty(resolver(Neo4jUsernameVariable)),
            FirstNonEmpty(resolver(Neo4jPasswordVariable)),
            FirstNonEmpty(resolver(OpenSearchUriVariable)),
            FirstNonEmpty(resolver(OpenSearchUsernameVariable)),
            FirstNonEmpty(resolver(OpenSearchPasswordVariable)),
            FirstNonEmpty(resolver(QdrantHostVariable)),
            ParsePositiveInt(resolver(QdrantPortVariable)),
            FirstNonEmpty(resolver(QdrantApiKeyVariable)),
            FirstNonEmpty(resolver(SmtpHostVariable)),
            ParsePositiveInt(resolver(SmtpPortVariable)),
            FirstNonEmpty(resolver(SmtpApiUriVariable)));
    }

    private ExternalProviderServiceMode ResolveProviderMode(
        bool preProvisionedServiceConfigured,
        bool allowTestcontainers = false)
    {
        if (!ExternalServicesEnabled)
        {
            return ExternalProviderServiceMode.Disabled;
        }

        if (preProvisionedServiceConfigured)
        {
            return ExternalProviderServiceMode.PreProvisionedConnectionString;
        }

        return allowTestcontainers && TestcontainersEnabled
            ? ExternalProviderServiceMode.Testcontainers
            : ExternalProviderServiceMode.Disabled;
    }

    private static bool IsTruthy(string? value)
    {
        return value is not null
            && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static int? ParsePositiveInt(string? value)
    {
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }

    private static string ProviderName(ExternalProviderServiceProvider provider)
    {
        return provider switch
        {
            ExternalProviderServiceProvider.Cassandra => "Cassandra",
            ExternalProviderServiceProvider.ClickHouse => "ClickHouse",
            ExternalProviderServiceProvider.Elasticsearch => "Elasticsearch",
            ExternalProviderServiceProvider.Nats => "NATS",
            ExternalProviderServiceProvider.Neo4j => "Neo4j",
            ExternalProviderServiceProvider.OpenSearch => "OpenSearch",
            ExternalProviderServiceProvider.Qdrant => "Qdrant",
            ExternalProviderServiceProvider.Smtp => "SMTP",
            ExternalProviderServiceProvider.Redis => "Redis",
            ExternalProviderServiceProvider.Any => "Provider",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown external provider service provider.")
        };
    }
}
