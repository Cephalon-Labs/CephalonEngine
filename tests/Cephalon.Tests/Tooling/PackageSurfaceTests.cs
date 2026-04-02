using System.Reflection;
using Cephalon.Cli;
using Cephalon.ReferenceDocs;

namespace Cephalon.Tests.Tooling;

public sealed class PackageSurfaceTests
{
    [Fact]
    public void CliAssemblyExposesOnlyTheTopLevelApplicationEntryPoint()
    {
        AssertExportedTypes(
            typeof(CliApplication).Assembly,
            typeof(CliApplication));
    }

    [Fact]
    public void ReferenceDocsAssemblyExposesOnlyTheDocumentedLibrarySurface()
    {
        AssertExportedTypes(
            typeof(ReferenceDocsApplication).Assembly,
            typeof(global::Cephalon.ReferenceDocs.Generation.ReferenceDocFile),
            typeof(global::Cephalon.ReferenceDocs.Generation.ReferenceDocsGenerator),
            typeof(global::Cephalon.ReferenceDocs.Generation.ReferenceDocsRequest),
            typeof(global::Cephalon.ReferenceDocs.Generation.RenderedReferenceDocs),
            typeof(global::Cephalon.ReferenceDocs.IO.ReferenceDocsWriter),
            typeof(ReferenceDocsApplication));
    }

    [Fact]
    public void AspNetCoreAssemblyExposesOnlyTheDocumentedHostContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.AspNetCore.Hosting.EngineWebApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.AspNetCore.Diagnostics.DiagnosticsSurface),
            typeof(global::Cephalon.AspNetCore.Documentation.ReferenceDocsHostingOptions),
            typeof(global::Cephalon.AspNetCore.Documentation.ReferenceDocsSurface),
            typeof(global::Cephalon.AspNetCore.Hosting.EngineWebApplicationBuilderExtensions),
            typeof(global::Cephalon.AspNetCore.Hosting.EngineWebApplicationExtensions),
            typeof(global::Cephalon.AspNetCore.Hosting.HttpRequestResponseLoggingOptions),
            typeof(global::Cephalon.AspNetCore.Hosting.ITransportRouteMapper),
            typeof(global::Cephalon.AspNetCore.Modules.IEndpointModule),
            typeof(global::Cephalon.AspNetCore.Transformers.XmlCommentsDocumentTransformer),
            typeof(global::Cephalon.AspNetCore.Transports.Rest.IRestModule),
            typeof(global::Cephalon.AspNetCore.Transports.Rest.RestEndpointConventionBuilderExtensions),
            typeof(global::Cephalon.AspNetCore.Transports.ServerSentEvents.IServerSentEventsModule),
            typeof(global::Cephalon.AspNetCore.Transports.WebSockets.IWebSocketModule));
    }

    [Fact]
    public void GraphQLAssemblyExposesOnlyTheDocumentedHostContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.AspNetCore.GraphQL.Hosting.GraphQLTransportServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.AspNetCore.GraphQL.Hosting.GraphQLTransportServiceCollectionExtensions),
            typeof(global::Cephalon.AspNetCore.GraphQL.Modules.IGraphQLModule));
    }

    [Fact]
    public void JsonRpcAssemblyExposesOnlyTheDocumentedHostContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.AspNetCore.JsonRpc.Hosting.JsonRpcTransportServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.AspNetCore.JsonRpc.Hosting.JsonRpcTransportServiceCollectionExtensions),
            typeof(global::Cephalon.AspNetCore.JsonRpc.Modules.IJsonRpcModule));
    }

    [Fact]
    public void GrpcAssemblyExposesOnlyTheTransportContractsAndGeneratedDiscoverySurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.AspNetCore.Grpc.Hosting.GrpcTransportServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.AspNetCore.Grpc.Contracts.Discovery.DiscoveryReflection),
            typeof(global::Cephalon.AspNetCore.Grpc.Contracts.Discovery.DiscoveryService),
            typeof(global::Cephalon.AspNetCore.Grpc.Contracts.Discovery.DiscoveryService.DiscoveryServiceBase),
            typeof(global::Cephalon.AspNetCore.Grpc.Contracts.Discovery.DiscoveryService.DiscoveryServiceClient),
            typeof(global::Cephalon.AspNetCore.Grpc.Contracts.Discovery.HelloReply),
            typeof(global::Cephalon.AspNetCore.Grpc.Contracts.Discovery.HelloRequest),
            typeof(global::Cephalon.AspNetCore.Grpc.Contracts.Discovery.PrincipleReply),
            typeof(global::Cephalon.AspNetCore.Grpc.Contracts.Discovery.PrinciplesRequest),
            typeof(global::Cephalon.AspNetCore.Grpc.Hosting.GrpcTransportServiceCollectionExtensions),
            typeof(global::Cephalon.AspNetCore.Grpc.Modules.IGrpcModule));
    }

    [Fact]
    public void WorkerAssemblyExposesOnlyTheDocumentedHostingExtensions()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Worker.Hosting.WorkerHostApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.Worker.Hosting.WorkerHostApplicationBuilderExtensions),
            typeof(global::Cephalon.Worker.Hosting.WorkerServiceCollectionExtensions));
    }

    [Fact]
    public void ScaffoldingAssemblyExposesOnlyTheDocumentedRenderedOutputSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Scaffolding.Generation.ScaffoldGenerator).Assembly,
            typeof(global::Cephalon.Scaffolding.Generation.RenderedFile),
            typeof(global::Cephalon.Scaffolding.Generation.RenderedFolder),
            typeof(global::Cephalon.Scaffolding.Generation.RenderedProject),
            typeof(global::Cephalon.Scaffolding.Generation.RenderedScaffold),
            typeof(global::Cephalon.Scaffolding.Generation.ScaffoldGenerator),
            typeof(global::Cephalon.Scaffolding.Generation.ScaffoldRequest),
            typeof(global::Cephalon.Scaffolding.IO.FileSystemScaffoldWriter));
    }

    [Fact]
    public void ObservabilityAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.Hosting.ObservabilityServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.Configuration.ObservabilityOptions),
            typeof(global::Cephalon.Observability.Configuration.TelemetryExportOptions),
            typeof(global::Cephalon.Observability.Hosting.ObservabilityServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityConsulDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.ConsulDependencies.Hosting.ConsulDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.ConsulDependencies.Configuration.ConsulDependencyDefinition),
            typeof(global::Cephalon.Observability.ConsulDependencies.Configuration.ConsulDependencyHealthOptions),
            typeof(global::Cephalon.Observability.ConsulDependencies.Hosting.ConsulDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityCassandraDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.CassandraDependencies.Hosting.CassandraDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.CassandraDependencies.Configuration.CassandraDependencyDefinition),
            typeof(global::Cephalon.Observability.CassandraDependencies.Configuration.CassandraDependencyHealthOptions),
            typeof(global::Cephalon.Observability.CassandraDependencies.Hosting.CassandraDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityNeo4jDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.Neo4jDependencies.Hosting.Neo4jDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.Neo4jDependencies.Configuration.Neo4jDependencyDefinition),
            typeof(global::Cephalon.Observability.Neo4jDependencies.Configuration.Neo4jDependencyHealthOptions),
            typeof(global::Cephalon.Observability.Neo4jDependencies.Hosting.Neo4jDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityOpenTelemetryAssemblyExposesOnlyTheDocumentedRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.OpenTelemetry.Hosting.OpenTelemetryHostApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.Observability.OpenTelemetry.Hosting.OpenTelemetryHostApplicationBuilderExtensions));
    }

    [Fact]
    public void ObservabilitySerilogAssemblyExposesOnlyTheDocumentedRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.Serilog.Hosting.SerilogHostApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.Observability.Serilog.Hosting.SerilogHostApplicationBuilderExtensions));
    }

    [Fact]
    public void ObservabilityHttpDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.HttpDependencies.Hosting.HttpDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.HttpDependencies.Configuration.HttpDependencyDefinition),
            typeof(global::Cephalon.Observability.HttpDependencies.Configuration.HttpDependencyHealthOptions),
            typeof(global::Cephalon.Observability.HttpDependencies.Hosting.HttpDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityElasticsearchDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.ElasticsearchDependencies.Hosting.ElasticsearchDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.ElasticsearchDependencies.Configuration.ElasticsearchDependencyDefinition),
            typeof(global::Cephalon.Observability.ElasticsearchDependencies.Configuration.ElasticsearchDependencyHealthOptions),
            typeof(global::Cephalon.Observability.ElasticsearchDependencies.Hosting.ElasticsearchDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityKafkaDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.KafkaDependencies.Hosting.KafkaDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.KafkaDependencies.Configuration.KafkaDependencyDefinition),
            typeof(global::Cephalon.Observability.KafkaDependencies.Configuration.KafkaDependencyHealthOptions),
            typeof(global::Cephalon.Observability.KafkaDependencies.Hosting.KafkaDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityMemcachedDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.MemcachedDependencies.Hosting.MemcachedDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.MemcachedDependencies.Configuration.MemcachedDependencyDefinition),
            typeof(global::Cephalon.Observability.MemcachedDependencies.Configuration.MemcachedDependencyHealthOptions),
            typeof(global::Cephalon.Observability.MemcachedDependencies.Hosting.MemcachedDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityRedisDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.RedisDependencies.Hosting.RedisDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.RedisDependencies.Configuration.RedisDependencyDefinition),
            typeof(global::Cephalon.Observability.RedisDependencies.Configuration.RedisDependencyHealthOptions),
            typeof(global::Cephalon.Observability.RedisDependencies.Hosting.RedisDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityPostgresDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.PostgresDependencies.Hosting.PostgresDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.PostgresDependencies.Configuration.PostgresDependencyDefinition),
            typeof(global::Cephalon.Observability.PostgresDependencies.Configuration.PostgresDependencyHealthOptions),
            typeof(global::Cephalon.Observability.PostgresDependencies.Hosting.PostgresDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityMySqlDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.MySqlDependencies.Hosting.MySqlDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.MySqlDependencies.Configuration.MySqlDependencyDefinition),
            typeof(global::Cephalon.Observability.MySqlDependencies.Configuration.MySqlDependencyHealthOptions),
            typeof(global::Cephalon.Observability.MySqlDependencies.Hosting.MySqlDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityNatsDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.NatsDependencies.Hosting.NatsDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.NatsDependencies.Configuration.NatsDependencyDefinition),
            typeof(global::Cephalon.Observability.NatsDependencies.Configuration.NatsDependencyHealthOptions),
            typeof(global::Cephalon.Observability.NatsDependencies.Hosting.NatsDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityOracleDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.OracleDependencies.Hosting.OracleDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.OracleDependencies.Configuration.OracleDependencyDefinition),
            typeof(global::Cephalon.Observability.OracleDependencies.Configuration.OracleDependencyHealthOptions),
            typeof(global::Cephalon.Observability.OracleDependencies.Hosting.OracleDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityMongoDbDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.MongoDbDependencies.Hosting.MongoDbDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.MongoDbDependencies.Configuration.MongoDbDependencyDefinition),
            typeof(global::Cephalon.Observability.MongoDbDependencies.Configuration.MongoDbDependencyHealthOptions),
            typeof(global::Cephalon.Observability.MongoDbDependencies.Hosting.MongoDbDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityMqttDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.MqttDependencies.Hosting.MqttDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.MqttDependencies.Configuration.MqttDependencyDefinition),
            typeof(global::Cephalon.Observability.MqttDependencies.Configuration.MqttDependencyHealthOptions),
            typeof(global::Cephalon.Observability.MqttDependencies.Hosting.MqttDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityRabbitMqDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.RabbitMqDependencies.Hosting.RabbitMqDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.RabbitMqDependencies.Configuration.RabbitMqDependencyDefinition),
            typeof(global::Cephalon.Observability.RabbitMqDependencies.Configuration.RabbitMqDependencyHealthOptions),
            typeof(global::Cephalon.Observability.RabbitMqDependencies.Hosting.RabbitMqDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilitySqlServerDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.SqlServerDependencies.Hosting.SqlServerDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.SqlServerDependencies.Configuration.SqlServerDependencyDefinition),
            typeof(global::Cephalon.Observability.SqlServerDependencies.Configuration.SqlServerDependencyHealthOptions),
            typeof(global::Cephalon.Observability.SqlServerDependencies.Hosting.SqlServerDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void AgenticsAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Agentics.Registration.AgenticEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Agentics.Configuration.AgenticRuntimeOptions),
            typeof(global::Cephalon.Agentics.Registration.AgenticEngineBuilderExtensions),
            typeof(global::Cephalon.Agentics.Services.AgentToolDescriptor),
            typeof(global::Cephalon.Agentics.Services.IAgentToolCatalog),
            typeof(global::Cephalon.Agentics.Services.IAgentToolContributor),
            typeof(global::Cephalon.Agentics.Services.IAgentToolRegistry));
    }

    [Fact]
    public void EventingAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Eventing.Registration.EventingEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Eventing.Configuration.EventingOptions),
            typeof(global::Cephalon.Eventing.Registration.EventingEngineBuilderExtensions),
            typeof(global::Cephalon.Eventing.Services.EventChannelDescriptor),
            typeof(global::Cephalon.Eventing.Services.IEventChannelCatalog),
            typeof(global::Cephalon.Eventing.Services.IEventChannelContributor),
            typeof(global::Cephalon.Eventing.Services.IEventChannelRegistry));
    }

    [Fact]
    public void RetrievalAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Retrieval.Registration.RetrievalEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Retrieval.Configuration.RetrievalOptions),
            typeof(global::Cephalon.Retrieval.Registration.RetrievalEngineBuilderExtensions),
            typeof(global::Cephalon.Retrieval.Services.IKnowledgeCatalog),
            typeof(global::Cephalon.Retrieval.Services.IKnowledgeCollectionContributor),
            typeof(global::Cephalon.Retrieval.Services.IKnowledgeCollectionRegistry),
            typeof(global::Cephalon.Retrieval.Services.KnowledgeCollectionDescriptor));
    }

    [Fact]
    public void EdgeAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Edge.Registration.EdgeEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Edge.Configuration.EdgeRuntimeOptions),
            typeof(global::Cephalon.Edge.Registration.EdgeEngineBuilderExtensions),
            typeof(global::Cephalon.Edge.Services.EdgeNodeDescriptor),
            typeof(global::Cephalon.Edge.Services.IEdgeNodeCatalog),
            typeof(global::Cephalon.Edge.Services.IEdgeNodeContributor),
            typeof(global::Cephalon.Edge.Services.IEdgeNodeRegistry));
    }

    private static void AssertExportedTypes(Assembly assembly, params Type[] expectedTypes)
    {
        var exportedTypes = assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var expected = expectedTypes
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, exportedTypes);
    }
}
