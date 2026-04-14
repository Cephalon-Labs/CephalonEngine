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
    public void AbstractionsAssemblyExposesOnlyTheDocumentedContractSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Abstractions.AppModel.AppBlueprint).Assembly,
            typeof(global::Cephalon.Abstractions.AppModel.AppBlueprint),
            typeof(global::Cephalon.Abstractions.AppModel.AppProfile),
            typeof(global::Cephalon.Abstractions.AppModel.AuditHistoryExportSelection),
            typeof(global::Cephalon.Abstractions.AppModel.AuditHistoryRetentionSelection),
            typeof(global::Cephalon.Abstractions.AppModel.AuditHistorySelection),
            typeof(global::Cephalon.Abstractions.AppModel.AuditSelection),
            typeof(global::Cephalon.Abstractions.AppModel.BehaviorExecutionResilienceOverrideSelection),
            typeof(global::Cephalon.Abstractions.AppModel.BulkheadSelection),
            typeof(global::Cephalon.Abstractions.AppModel.CircuitBreakerSelection),
            typeof(global::Cephalon.Abstractions.AppModel.DataSelection),
            typeof(global::Cephalon.Abstractions.AppModel.DatabaseMigrationsSelection),
            typeof(global::Cephalon.Abstractions.AppModel.DatabaseRuntimeSelection),
            typeof(global::Cephalon.Abstractions.AppModel.DatabaseTargetSelection),
            typeof(global::Cephalon.Abstractions.AppModel.DatabaseTopologySelection),
            typeof(global::Cephalon.Abstractions.AppModel.IdentitySelection),
            typeof(global::Cephalon.Abstractions.AppModel.MessagingSelection),
            typeof(global::Cephalon.Abstractions.AppModel.RateLimitingOverrideSelection),
            typeof(global::Cephalon.Abstractions.AppModel.RateLimitingSelection),
            typeof(global::Cephalon.Abstractions.AppModel.ResilienceSelection),
            typeof(global::Cephalon.Abstractions.AppModel.RetrySelection),
            typeof(global::Cephalon.Abstractions.AppModel.Scaffolding.ProjectRoles),
            typeof(global::Cephalon.Abstractions.AppModel.Scaffolding.ScaffoldFolder),
            typeof(global::Cephalon.Abstractions.AppModel.Scaffolding.ScaffoldPlan),
            typeof(global::Cephalon.Abstractions.AppModel.Scaffolding.ScaffoldProject),
            typeof(global::Cephalon.Abstractions.AppModel.Scaffolding.ScaffoldScopes),
            typeof(global::Cephalon.Abstractions.AppModel.Scaffolding.SuiteScaffoldPlan),
            typeof(global::Cephalon.Abstractions.AppModel.Scaffolding.SuiteScaffoldService),
            typeof(global::Cephalon.Abstractions.AppModel.SuiteBlueprint),
            typeof(global::Cephalon.Abstractions.AppModel.TenancySelection),
            typeof(global::Cephalon.Abstractions.AppModel.TimeoutSelection),
            typeof(global::Cephalon.Abstractions.Resilience.BehaviorResilienceExceptionContext),
            typeof(global::Cephalon.Abstractions.Resilience.BehaviorResilienceExceptionHandling),
            typeof(global::Cephalon.Abstractions.Resilience.BehaviorExecutionResilienceSelection),
            typeof(global::Cephalon.Abstractions.Resilience.IBehaviorResilienceExceptionClassifier),
            typeof(global::Cephalon.Abstractions.Resilience.IBehaviorResilienceRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Resilience.IRateLimitingRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Resilience.BehaviorResilienceRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Resilience.RateLimitingRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Audit.AuditActor),
            typeof(global::Cephalon.Abstractions.Audit.AuditChange),
            typeof(global::Cephalon.Abstractions.Audit.AuditEntry),
            typeof(global::Cephalon.Abstractions.Audit.AuditHistoryExportRequest),
            typeof(global::Cephalon.Abstractions.Audit.AuditHistoryEntry),
            typeof(global::Cephalon.Abstractions.Audit.AuditHistoryQuery),
            typeof(global::Cephalon.Abstractions.Audit.AuditHistoryQueryResult),
            typeof(global::Cephalon.Abstractions.Audit.AuditOutcome),
            typeof(global::Cephalon.Abstractions.Audit.IAuditHistoryExporter),
            typeof(global::Cephalon.Abstractions.Audit.IAuditHistoryReader),
            typeof(global::Cephalon.Abstractions.Audit.AuditStoreDescriptor),
            typeof(global::Cephalon.Abstractions.Audit.IAuditStoreCatalog),
            typeof(global::Cephalon.Abstractions.Audit.IAuditStoreContributor),
            typeof(global::Cephalon.Abstractions.Audit.IAuditStoreRuntimeContributor),
            typeof(global::Cephalon.Abstractions.Audit.IAuditStoreRegistry),
            typeof(global::Cephalon.Abstractions.Audit.IAuditWriter),
            typeof(global::Cephalon.Abstractions.Authorization.AuthorizationContext),
            typeof(global::Cephalon.Abstractions.Authorization.AuthorizationDecision),
            typeof(global::Cephalon.Abstractions.Authorization.AuthorizationMode),
            typeof(global::Cephalon.Abstractions.Authorization.AuthorizationPolicyDescriptor),
            typeof(global::Cephalon.Abstractions.Authorization.AuthorizationResource),
            typeof(global::Cephalon.Abstractions.Authorization.AuthorizationSubject),
            typeof(global::Cephalon.Abstractions.Authorization.IAuthorizationEvaluator),
            typeof(global::Cephalon.Abstractions.Authorization.IAuthorizationPolicyCatalog),
            typeof(global::Cephalon.Abstractions.Authorization.IAuthorizationPolicyContributor),
            typeof(global::Cephalon.Abstractions.Authorization.IAuthorizationPolicyRegistry),
            typeof(global::Cephalon.Abstractions.Behaviors.AppBehaviorAttribute),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorAdvisorySeverity),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorAllowedPatternsAttribute),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorAllowedTransportsAttribute),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorApiSurfaceDescriptor),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorCompatibilityViolation),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorFault),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorFaultSeverity),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorIdempotencyAttribute),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorIdempotencyMode),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorNotFoundException),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorResult),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorResultDescriptor),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorResult<>),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorResultStatus),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorSecurityException),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorTopologyDescriptor),
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorTopologyOptions),
            typeof(global::Cephalon.Abstractions.Behaviors.ContainsBehaviorsAttribute),
            typeof(global::Cephalon.Abstractions.Behaviors.CompatibilitySeverity),
            typeof(global::Cephalon.Abstractions.Behaviors.IAppBehavior<,>),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorAdvisoryCatalog),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorAdvisory),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorAdvisoryContributor),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorCatalog),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorCompatibilityRule),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorContext),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorModuleBuilder),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorOwnerModule),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorResult),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorContributor),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorRegistry),
            typeof(global::Cephalon.Abstractions.Behaviors.IBehaviorTopologyBuilder),
            typeof(global::Cephalon.Abstractions.Behaviors.IProcessCompletion),
            typeof(global::Cephalon.Abstractions.Behaviors.OwnedBehaviorRegistration),
            typeof(global::Cephalon.Abstractions.Behaviors.Result),
            typeof(global::Cephalon.Abstractions.Behaviors.Result<>),
            typeof(global::Cephalon.Abstractions.Capabilities.Capability),
            typeof(global::Cephalon.Abstractions.Capabilities.CapabilityAccess),
            typeof(global::Cephalon.Abstractions.Capabilities.ICapabilityRegistry),
            typeof(global::Cephalon.Abstractions.Data.DatabaseRoleDescriptor),
            typeof(global::Cephalon.Abstractions.Data.DatabaseRoleProbeDescriptor),
            typeof(global::Cephalon.Abstractions.Data.DatabaseMigrationOperationalPlaybook),
            typeof(global::Cephalon.Abstractions.Data.DatabaseMigrationOperationalExecutionGroup),
            typeof(global::Cephalon.Abstractions.Data.DatabaseMigrationOperationalExecutionGroupCommandBatch),
            typeof(global::Cephalon.Abstractions.Data.DatabaseMigrationOperationalExecutionGroupCommand),
            typeof(global::Cephalon.Abstractions.Data.DatabaseMigrationOperationalStep),
            typeof(global::Cephalon.Abstractions.Data.DatabaseTopologyOperationalAction),
            typeof(global::Cephalon.Abstractions.Data.DatabaseTopologyOperationalActionPlan),
            typeof(global::Cephalon.Abstractions.Data.DatabaseTopologyOperationalAdvisory),
            typeof(global::Cephalon.Abstractions.Data.DatabaseTopologyOperationalSnapshot),
            typeof(global::Cephalon.Abstractions.Data.DatabaseTopologyOperationalSummary),
            typeof(global::Cephalon.Abstractions.Data.DatabaseMigrationCommandDescriptor),
            typeof(global::Cephalon.Abstractions.Data.DatabaseMigrationDescriptor),
            typeof(global::Cephalon.Abstractions.Data.DatabaseMigrationStatus),
            typeof(global::Cephalon.Abstractions.Data.EventDispatchRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Data.EventDispatchRuntimeSummary),
            typeof(global::Cephalon.Abstractions.Data.EventDispatchRuntimeState),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseMigrationCatalog),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseMigrationContributor),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseMigrationOperationalPlaybookProvider),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseTopologyOperationalSnapshotProvider),
            typeof(global::Cephalon.Abstractions.Data.DatabaseRoleRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Data.ICommand),
            typeof(global::Cephalon.Abstractions.Data.ICommand<>),
            typeof(global::Cephalon.Abstractions.Data.ICommandHandler<>),
            typeof(global::Cephalon.Abstractions.Data.ICommandHandler<,>),
            typeof(global::Cephalon.Abstractions.Data.IEventDispatchRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Data.IEventDispatchRuntimeDescriptorCatalog),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseRoleCatalog),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseRoleRuntimeContributor),
            typeof(global::Cephalon.Abstractions.Data.IInbox),
            typeof(global::Cephalon.Abstractions.Data.IInboxCatalog),
            typeof(global::Cephalon.Abstractions.Data.IInboxContributor),
            typeof(global::Cephalon.Abstractions.Data.IInboxRegistry),
            typeof(global::Cephalon.Abstractions.Data.InboxDescriptor),
            typeof(global::Cephalon.Abstractions.Data.InboxMessage),
            typeof(global::Cephalon.Abstractions.Data.IOutboxDispatchPolicyCatalog),
            typeof(global::Cephalon.Abstractions.Data.IOutbox),
            typeof(global::Cephalon.Abstractions.Data.IOutboxCatalog),
            typeof(global::Cephalon.Abstractions.Data.IOutboxContributor),
            typeof(global::Cephalon.Abstractions.Data.IOutboxRegistry),
            typeof(global::Cephalon.Abstractions.Data.OutboxDispatchPolicyDescriptor),
            typeof(global::Cephalon.Abstractions.Data.IProjection<>),
            typeof(global::Cephalon.Abstractions.Data.IProjectionCatalog),
            typeof(global::Cephalon.Abstractions.Data.IProjectionContributor),
            typeof(global::Cephalon.Abstractions.Data.IProjectionRegistry),
            typeof(global::Cephalon.Abstractions.Data.IQuery<>),
            typeof(global::Cephalon.Abstractions.Data.IQueryHandler<,>),
            typeof(global::Cephalon.Abstractions.Data.IReadStore),
            typeof(global::Cephalon.Abstractions.Data.IWriteStore),
            typeof(global::Cephalon.Abstractions.Data.OutboxDescriptor),
            typeof(global::Cephalon.Abstractions.Data.OutboxMessage),
            typeof(global::Cephalon.Abstractions.Data.ProjectionDescriptor),
            typeof(global::Cephalon.Abstractions.EventSourcing.DomainEvent),
            typeof(global::Cephalon.Abstractions.EventSourcing.EventStreamConcurrencyException),
            typeof(global::Cephalon.Abstractions.EventSourcing.EventStreamDescriptor),
            typeof(global::Cephalon.Abstractions.EventSourcing.IAggregate<>),
            typeof(global::Cephalon.Abstractions.EventSourcing.IDomainEvent),
            typeof(global::Cephalon.Abstractions.EventSourcing.IEventStore),
            typeof(global::Cephalon.Abstractions.EventSourcing.IEventStoreCatalog),
            typeof(global::Cephalon.Abstractions.EventSourcing.IEventStoreContributor),
            typeof(global::Cephalon.Abstractions.EventSourcing.IEventStoreRegistry),
            typeof(global::Cephalon.Abstractions.EventSourcing.ISnapshotStore),
            typeof(global::Cephalon.Abstractions.Execution.ExecutionGraphDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.ExecutionGraphEdgeDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.ExecutionGraphNodeDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.HostedExecutionDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.IExecutionGraphContributor),
            typeof(global::Cephalon.Abstractions.Execution.IExecutionGraphRegistry),
            typeof(global::Cephalon.Abstractions.Execution.IExecutionRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Execution.IHostedExecutionContributor),
            typeof(global::Cephalon.Abstractions.Execution.IHostedExecutionRegistry),
            typeof(global::Cephalon.Abstractions.Execution.IHostedExecutionRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Health.DependencyHealthReport),
            typeof(global::Cephalon.Abstractions.Health.HealthState),
            typeof(global::Cephalon.Abstractions.Health.IDependencyHealthContributor),
            typeof(global::Cephalon.Abstractions.Ids.IdGenerationRequest),
            typeof(global::Cephalon.Abstractions.Ids.IIdGenerator),
            typeof(global::Cephalon.Abstractions.Localization.ILocalizedResourceContributor),
            typeof(global::Cephalon.Abstractions.Localization.ILocalizedResourceRegistry),
            typeof(global::Cephalon.Abstractions.Localization.ILocalizedTextCatalog),
            typeof(global::Cephalon.Abstractions.Localization.LocalizedResourcesSnapshot),
            typeof(global::Cephalon.Abstractions.Modules.IModule),
            typeof(global::Cephalon.Abstractions.Modules.IModuleLifecycle),
            typeof(global::Cephalon.Abstractions.Modules.ModuleBase),
            typeof(global::Cephalon.Abstractions.Modules.ModuleContext),
            typeof(global::Cephalon.Abstractions.Modules.ModuleDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigRouteContributor),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigRouteRegistry),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigRouter),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Patterns.PatternDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.PatternKind),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigRequest),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigRouteDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigRouteResolution),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigTarget),
            typeof(global::Cephalon.Abstractions.Technologies.ITechnologyCapabilityContributor),
            typeof(global::Cephalon.Abstractions.Technologies.ITechnologyContributor),
            typeof(global::Cephalon.Abstractions.Technologies.ITechnologyRegistry),
            typeof(global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeContributor),
            typeof(global::Cephalon.Abstractions.Technologies.ITechnologyServiceContributor),
            typeof(global::Cephalon.Abstractions.Technologies.TechnologyDescriptor),
            typeof(global::Cephalon.Abstractions.Technologies.TechnologyKind),
            typeof(global::Cephalon.Abstractions.Technologies.TechnologyRuntimeEntry),
            typeof(global::Cephalon.Abstractions.Technologies.TechnologyRuntimeSurface),
            typeof(global::Cephalon.Abstractions.Technologies.TechnologySelection),
            typeof(global::Cephalon.Abstractions.Tenancy.ITenantContextAccessor),
            typeof(global::Cephalon.Abstractions.Tenancy.ITenantResolver),
            typeof(global::Cephalon.Abstractions.Tenancy.TenantContext),
            typeof(global::Cephalon.Abstractions.Tenancy.TenantResolutionRequest),
            typeof(global::Cephalon.Abstractions.Tenancy.TenantResolutionResult),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointCandidateRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointCandidateRuntimeRegistry),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointOverrideRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointSuppressionRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointRuntimeRegistry),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateProjectionDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.TransportDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.TransportFeatures));
    }

    [Fact]
    public void AspNetCoreAssemblyExposesOnlyTheDocumentedHostContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.AspNetCore.Hosting.EngineWebApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.AspNetCore.Diagnostics.DiagnosticsSurface),
            typeof(global::Cephalon.AspNetCore.Documentation.OpenApiEndpointOptions),
            typeof(global::Cephalon.AspNetCore.Documentation.OpenApiTagMetadata),
            typeof(global::Cephalon.AspNetCore.Documentation.ReferenceDocsHostingOptions),
            typeof(global::Cephalon.AspNetCore.Documentation.ReferenceDocsSurface),
            typeof(global::Cephalon.AspNetCore.Hosting.AuditHistoryExportHttpResponseExtensions),
            typeof(global::Cephalon.AspNetCore.Hosting.ApiRoutesOptions),
            typeof(global::Cephalon.AspNetCore.Hosting.CephalonRateLimitingEndpointConventionBuilderExtensions),
            typeof(global::Cephalon.AspNetCore.Hosting.EngineWebApplicationBuilderExtensions),
            typeof(global::Cephalon.AspNetCore.Hosting.EngineWebApplicationExtensions),
            typeof(global::Cephalon.AspNetCore.Hosting.HttpRequestResponseLoggingOptions),
            typeof(global::Cephalon.AspNetCore.Hosting.ITransportRouteMapper),
            typeof(global::Cephalon.AspNetCore.Hosting.RestApiGovernanceOptions),
            typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions),
            typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions),
            typeof(global::Cephalon.AspNetCore.Modules.IEndpointModule),
            typeof(global::Cephalon.AspNetCore.Transformers.XmlCommentsDocumentTransformer),
            typeof(global::Cephalon.AspNetCore.Transports.Rest.IRestModule),
            typeof(global::Cephalon.AspNetCore.Transports.Rest.ResultModel<>),
            typeof(global::Cephalon.AspNetCore.Transports.Rest.ResultModelError),
            typeof(global::Cephalon.AspNetCore.Transports.Rest.ResultModelErrorDetail),
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
    public void ObservabilityClickHouseDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.ClickHouseDependencies.Hosting.ClickHouseDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.ClickHouseDependencies.Configuration.ClickHouseDependencyDefinition),
            typeof(global::Cephalon.Observability.ClickHouseDependencies.Configuration.ClickHouseDependencyHealthOptions),
            typeof(global::Cephalon.Observability.ClickHouseDependencies.Hosting.ClickHouseDependencyHealthServiceCollectionExtensions));
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
    public void ObservabilityOpenSearchDependenciesAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.OpenSearchDependencies.Hosting.OpenSearchDependencyHealthServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Observability.OpenSearchDependencies.Configuration.OpenSearchDependencyDefinition),
            typeof(global::Cephalon.Observability.OpenSearchDependencies.Configuration.OpenSearchDependencyHealthOptions),
            typeof(global::Cephalon.Observability.OpenSearchDependencies.Hosting.OpenSearchDependencyHealthServiceCollectionExtensions));
    }

    [Fact]
    public void ObservabilityOpenTelemetryAssemblyExposesOnlyTheDocumentedRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.OpenTelemetry.Hosting.OpenTelemetryHostApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.Observability.OpenTelemetry.Hosting.OpenTelemetryHostApplicationBuilderExtensions));
    }

    [Fact]
    public void ObservabilityOracleCloudAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.OracleCloud.Hosting.OracleCloudHostApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.Observability.OracleCloud.Configuration.OracleCloudTelemetryExportOptions),
            typeof(global::Cephalon.Observability.OracleCloud.Hosting.OracleCloudHostApplicationBuilderExtensions));
    }

    [Fact]
    public void ObservabilityGrafanaCloudAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.GrafanaCloud.Hosting.GrafanaCloudHostApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.Observability.GrafanaCloud.Configuration.GrafanaCloudTelemetryExportOptions),
            typeof(global::Cephalon.Observability.GrafanaCloud.Hosting.GrafanaCloudHostApplicationBuilderExtensions));
    }

    [Fact]
    public void ObservabilityNewRelicAssemblyExposesOnlyTheDocumentedConfigurationAndRegistrationSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Observability.NewRelic.Hosting.NewRelicHostApplicationBuilderExtensions).Assembly,
            typeof(global::Cephalon.Observability.NewRelic.Configuration.NewRelicTelemetryExportOptions),
            typeof(global::Cephalon.Observability.NewRelic.Hosting.NewRelicHostApplicationBuilderExtensions));
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
            typeof(global::Cephalon.Eventing.Services.EventDispatchItem),
            typeof(global::Cephalon.Eventing.Services.EventDispatchExecutionOutcomes),
            typeof(global::Cephalon.Eventing.Services.EventDispatchExecutionReport),
            typeof(global::Cephalon.Eventing.Services.EventPublication),
            typeof(global::Cephalon.Eventing.Registration.EventingEngineBuilderExtensions),
            typeof(global::Cephalon.Eventing.Services.EventChannelDescriptor),
            typeof(global::Cephalon.Eventing.Services.EventSubscriptionExecutionOutcomes),
            typeof(global::Cephalon.Eventing.Services.EventSubscriptionExecutionReport),
            typeof(global::Cephalon.Eventing.Services.EventSubscriptionDescriptor),
            typeof(global::Cephalon.Eventing.Services.IEventChannelCatalog),
            typeof(global::Cephalon.Eventing.Services.IEventChannelContributor),
            typeof(global::Cephalon.Eventing.Services.IEventChannelRegistry),
            typeof(global::Cephalon.Eventing.Services.IEventDispatchStore),
            typeof(global::Cephalon.Eventing.Services.IEventDispatchRuntimeContributor),
            typeof(global::Cephalon.Eventing.Services.IEventDispatchRuntimeRegistry),
            typeof(global::Cephalon.Eventing.Services.IEventDispatchRuntimeReporter),
            typeof(global::Cephalon.Eventing.Services.IEventSubscriptionCatalog),
            typeof(global::Cephalon.Eventing.Services.IEventSubscriptionContributor),
            typeof(global::Cephalon.Eventing.Services.IEventSubscriptionRegistry),
            typeof(global::Cephalon.Eventing.Services.IEventPublisher),
            typeof(global::Cephalon.Eventing.Services.IEventSubscriptionRuntimeCatalog),
            typeof(global::Cephalon.Eventing.Services.IEventSubscriptionRuntimeReporter),
            typeof(global::Cephalon.Eventing.Services.EventSubscriptionRuntimeState));
    }

    [Fact]
    public void EventSourcingAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.EventSourcing.Registration.EventSourcingEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.EventSourcing.Configuration.EventSourcingOptions),
            typeof(global::Cephalon.EventSourcing.Hosting.EventSourcingServiceCollectionExtensions),
            typeof(global::Cephalon.EventSourcing.Registration.EventSourcingEngineBuilderExtensions),
            typeof(global::Cephalon.EventSourcing.Services.AggregateHydrator<,>),
            typeof(global::Cephalon.EventSourcing.Services.EventStreamCatalog),
            typeof(global::Cephalon.EventSourcing.Services.EventStreamRegistry));
    }

    [Fact]
    public void EventSourcingEntityFrameworkAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.EventSourcing.EntityFramework.Registration.EntityFrameworkEventSourcingEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.EventSourcing.EntityFramework.EntityFrameworkEventEntry),
            typeof(global::Cephalon.EventSourcing.EntityFramework.EntityFrameworkEventSourcingConfiguration),
            typeof(global::Cephalon.EventSourcing.EntityFramework.Hosting.EntityFrameworkEventSourcingServiceCollectionExtensions),
            typeof(global::Cephalon.EventSourcing.EntityFramework.IEntityFrameworkEventContext),
            typeof(global::Cephalon.EventSourcing.EntityFramework.Registration.EntityFrameworkEventSourcingEngineBuilderExtensions));
    }

    [Fact]
    public void WolverineEventingAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Eventing.Wolverine.Registration.WolverineEventingEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Eventing.Wolverine.Configuration.WolverineEventingOptions),
            typeof(global::Cephalon.Eventing.Wolverine.Registration.WolverineEventingEngineBuilderExtensions));
    }

    [Fact]
    public void DataAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.Registration.DataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.Configuration.DataRuntimeOptions),
            typeof(global::Cephalon.Data.Registration.DataEngineBuilderExtensions));
    }

    [Fact]
    public void DataEntityFrameworkAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.EntityFramework.Registration.EntityFrameworkDataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.EntityFramework.Configuration.EntityFrameworkDataOptions),
            typeof(global::Cephalon.Data.EntityFramework.Configuration.EntityFrameworkDatabaseRoleResolver),
            typeof(global::Cephalon.Data.EntityFramework.Configuration.EntityFrameworkDatabaseRoleContext),
            typeof(global::Cephalon.Data.EntityFramework.Modeling.EntityFrameworkInboxEntry),
            typeof(global::Cephalon.Data.EntityFramework.Modeling.EntityFrameworkModelBuilderExtensions),
            typeof(global::Cephalon.Data.EntityFramework.Modeling.IEntityFrameworkInboxContext),
            typeof(global::Cephalon.Data.EntityFramework.Modeling.EntityFrameworkOutboxEntry),
            typeof(global::Cephalon.Data.EntityFramework.Modeling.IEntityFrameworkOutboxContext),
            typeof(global::Cephalon.Data.EntityFramework.Services.EntityFrameworkDatabaseMigrationHostedService),
            typeof(global::Cephalon.Data.EntityFramework.Services.EntityFrameworkDatabaseMigrationRegistration),
            typeof(global::Cephalon.Data.EntityFramework.Registration.EntityFrameworkDataEngineBuilderExtensions));
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

    [Fact]
    public void SfidIdsAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Ids.Sfid.Registration.SfidEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Ids.Sfid.Configuration.SfidIdOptions),
            typeof(global::Cephalon.Ids.Sfid.Registration.SfidEngineBuilderExtensions));
    }

    [Fact]
    public void IdentityAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Identity.Registration.IdentityEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Identity.Configuration.IdentityRuntimeOptions),
            typeof(global::Cephalon.Identity.Policies.IdentityPolicyMetadataKeys),
            typeof(global::Cephalon.Identity.Registration.IdentityEngineBuilderExtensions));
    }

    [Fact]
    public void IdentityAspNetCoreAssemblyExposesOnlyTheDocumentedHostContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Identity.AspNetCore.Hosting.IdentityAspNetCoreServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.Identity.AspNetCore.Configuration.IdentityAspNetCoreOptions),
            typeof(global::Cephalon.Identity.AspNetCore.Hosting.IdentityAspNetCoreServiceCollectionExtensions),
            typeof(global::Cephalon.Identity.AspNetCore.Hosting.IdentityAspNetCoreWebApplicationBuilderExtensions),
            typeof(global::Cephalon.Identity.AspNetCore.Transports.Rest.IdentityEndpointConventionBuilderExtensions),
            typeof(global::Cephalon.Identity.AspNetCore.Transports.Rest.RequireCephalonAuthorizationAttribute));
    }

    [Fact]
    public void MultiTenancyAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.MultiTenancy.Registration.MultiTenancyEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.MultiTenancy.Configuration.MultiTenancyRuntimeOptions),
            typeof(global::Cephalon.MultiTenancy.Registration.MultiTenancyEngineBuilderExtensions));
    }

    [Fact]
    public void AuditAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Audit.Registration.AuditEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Audit.Configuration.AuditRuntimeOptions),
            typeof(global::Cephalon.Audit.Conventions.AuditMetadataKeys),
            typeof(global::Cephalon.Audit.Registration.AuditEngineBuilderExtensions),
            typeof(global::Cephalon.Audit.Services.AuditRecordRequest),
            typeof(global::Cephalon.Audit.Services.IAuditActorAccessor),
            typeof(global::Cephalon.Audit.Services.IAuditRecorder));
    }

    [Fact]
    public void AuditEntityFrameworkAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Audit.EntityFramework.Registration.EntityFrameworkAuditHistoryEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Audit.EntityFramework.Configuration.EntityFrameworkAuditHistoryOptions),
            typeof(global::Cephalon.Audit.EntityFramework.EntityFrameworkAuditHistoryEntry),
            typeof(global::Cephalon.Audit.EntityFramework.IEntityFrameworkAuditHistoryContext),
            typeof(global::Cephalon.Audit.EntityFramework.Modeling.EntityFrameworkAuditHistoryModelBuilderExtensions),
            typeof(global::Cephalon.Audit.EntityFramework.Registration.EntityFrameworkAuditHistoryEngineBuilderExtensions));
    }

    [Fact]
    public void BehaviorsHttpAssemblyExposesOnlyTheDocumentedContractSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Behaviors.Http.Bindings.JsonRpcHttpBehaviorBinding).Assembly,
            typeof(global::Cephalon.Behaviors.Http.Abstractions.IHttpBehaviorBinding),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.IHttpBehaviorBindingRegistry),
            typeof(global::Cephalon.Behaviors.Http.Bindings.JsonRpcRequest),
            typeof(global::Cephalon.Behaviors.Http.Bindings.JsonRpcSuccessResponse),
            typeof(global::Cephalon.Behaviors.Http.Bindings.JsonRpcErrorResponse),
            typeof(global::Cephalon.Behaviors.Http.Bindings.JsonRpcError),
            typeof(global::Cephalon.Behaviors.Http.Bindings.JsonRpcHttpBehaviorBinding),
            typeof(global::Cephalon.Behaviors.Http.Bindings.GraphqlRequest),
            typeof(global::Cephalon.Behaviors.Http.Bindings.GraphqlHttpBehaviorBinding),
            typeof(global::Cephalon.Behaviors.Http.Bindings.GraphqlSseBehaviorBinding),
            typeof(global::Cephalon.Behaviors.Http.Bindings.GraphqlWsBehaviorBinding),
            typeof(global::Cephalon.Behaviors.Http.Bindings.SseBehaviorBinding),
            typeof(global::Cephalon.Behaviors.Http.Bindings.WebSocketBehaviorBinding),
            typeof(global::Cephalon.Behaviors.Http.Registry.HttpBehaviorBindingRegistry),
            typeof(global::Cephalon.Behaviors.Http.LazyTransportBinding),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingAttribute),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingDescriptor),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileAttribute),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor),
            typeof(global::Cephalon.Behaviors.Http.Hosting.BehaviorRestEndpointGroup),
            typeof(global::Cephalon.Behaviors.Http.Hosting.BehaviorRestEndpointRouteBuilderExtensions),
            typeof(global::Cephalon.Behaviors.Http.Hosting.HttpBehaviorBindingExtensions),
            typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder),
            typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorModuleBuilder),
            typeof(global::Cephalon.Behaviors.Http.Hosting.RestBehaviorModuleBase));
    }

    [Fact]
    public void BehaviorsHttpRestBehaviorEndpointGroupBuilderExposesGeneratedProjectionMethods()
    {
        var methods = typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(methods, static method =>
            method.Name == "MapGeneratedProfiles" &&
            method.GetParameters().Length == 0);
        Assert.Contains(methods, static method =>
            method.Name == "MapGeneratedProfiles" &&
            method.GetParameters() is [{ ParameterType: { } parameterType }] &&
            parameterType == typeof(string));
    }

    [Fact]
    public void BehaviorsPatternsAssemblyExposesOnlyTheDocumentedContractSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.IBehaviorExecutionStrategy).Assembly,
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.BehaviorExecutionContext),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.BehaviorExecutionResult),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.IBehaviorExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.IProcessCheckpointStore),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ISagaStateStore),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ProcessCheckpoint),
            typeof(global::Cephalon.Behaviors.Patterns.Hosting.PatternBehaviorExtensions),
            typeof(global::Cephalon.Behaviors.Patterns.Registry.ExecutionStrategyRegistry),
            typeof(global::Cephalon.Behaviors.Patterns.Stores.InMemoryProcessCheckpointStore),
            typeof(global::Cephalon.Behaviors.Patterns.Stores.InMemorySagaStateStore),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.CqrsExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.DirectExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.EventDrivenExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.ProcessManagerExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.SagaExecutionStrategy));
    }

    [Fact]
    public void BehaviorsMessagingAssemblyExposesOnlyTheDocumentedContractSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Behaviors.Messaging.Abstractions.IMessagingBehaviorBinding).Assembly,
            typeof(global::Cephalon.Behaviors.Messaging.Abstractions.IMessagingBehaviorBinding),
            typeof(global::Cephalon.Behaviors.Messaging.Abstractions.IMessagingBehaviorBindingRegistry),
            typeof(global::Cephalon.Behaviors.Messaging.Bindings.InMemoryTransportBinding),
            typeof(global::Cephalon.Behaviors.Messaging.Bindings.KafkaTransportBinding),
            typeof(global::Cephalon.Behaviors.Messaging.Bindings.RabbitMqTransportBinding),
            typeof(global::Cephalon.Behaviors.Messaging.Hosting.MessagingBehaviorBindingsBuilder),
            typeof(global::Cephalon.Behaviors.Messaging.Hosting.MessagingBehaviorBindingServiceCollectionExtensions),
            typeof(global::Cephalon.Behaviors.Messaging.Options.InMemoryTransportOptions),
            typeof(global::Cephalon.Behaviors.Messaging.Options.KafkaTransportOptions),
            typeof(global::Cephalon.Behaviors.Messaging.Options.RabbitMqTransportOptions),
            typeof(global::Cephalon.Behaviors.Messaging.Registry.MessagingBehaviorBindingRegistry));
    }

    [Fact]
    public void BehaviorsSourceGenAssemblyExposesOnlyTheDocumentedContractSurface()
    {
        var asm = typeof(Cephalon.Behaviors.SourceGen.BehaviorSourceGenerator).Assembly;
        AssertExportedTypes(asm,
            typeof(Cephalon.Behaviors.SourceGen.BehaviorSourceGenerator));
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
