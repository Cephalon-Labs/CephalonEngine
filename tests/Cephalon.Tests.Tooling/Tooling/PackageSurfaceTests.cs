using System.Reflection;
using System.Text.Json;
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
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKind),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKindExtensions),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingFallbackMode),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingFallbackModeExtensions),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSourceExtensions),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasisExtensions),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSelectionBasisSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideActionKindSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKind),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKindExtensions),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingModeExtensions),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointAuthoringPolicyRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointCandidateRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointPublicationGroupRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointCandidateRuntimeRegistry),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointOverrideRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointSuppressionRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IRestEndpointRuntimeRegistry),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateProjectionDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatusExtensions),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicyDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor),
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
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSourceExtensions),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethodExtensions),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileAttribute),
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor),
            typeof(global::Cephalon.Behaviors.Http.Hosting.BehaviorRestEndpointGroup),
            typeof(global::Cephalon.Behaviors.Http.Hosting.BehaviorRestEndpointRouteBuilderExtensions),
            typeof(global::Cephalon.Behaviors.Http.Hosting.HttpBehaviorBindingExtensions),
            typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder),
            typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorModuleBuilder),
            typeof(global::Cephalon.Behaviors.Http.Hosting.RestBehaviorEngineBuilderExtensions),
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
    public void BehaviorsHttpRestBehaviorEndpointGroupBuilderExposesOpenApiDocumentNameOverrideMethod()
    {
        var methods = typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(methods, static method =>
            method.Name == "WithOpenApiDocumentName" &&
            method.GetParameters() is [{ ParameterType: { } parameterType }] &&
            parameterType == typeof(string));
    }

    [Fact]
    public void BehaviorsHttpRestBehaviorEndpointGroupBuilderExposesHostGovernanceOptInMethod()
    {
        var methods = typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(methods, static method =>
            method.Name == "AllowHostGovernance" &&
            method.GetParameters().Length == 0);
    }

    [Fact]
    public void BehaviorsHttpRestBehaviorModuleBuilderExposesDerivedGeneratedGroupMethod()
    {
        var methods = typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorModuleBuilder)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(methods, static method =>
            method.Name == "GroupFromBehaviorIdPrefix" &&
            method.GetParameters() is [{ ParameterType: { } parameterType }] &&
            parameterType == typeof(string));
    }

    [Fact]
    public void BehaviorsHttpRestProfileContractsExposePreservedImplicitQueryFallback()
    {
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileAttribute)
            .GetProperty("PreserveImplicitQueryFallback", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor)
            .GetProperty("PreserveImplicitQueryFallback", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void BehaviorsHttpRestMethodExposesStableWireNameHelpers()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod),
            "Unspecified"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod),
            "Get"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod),
            "Post"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod),
            "Put"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod),
            "Patch"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod),
            "Delete"));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethodExtensions)
            .GetMethod("GetWireName", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethodExtensions)
            .GetMethod("TryParseWireName", BindingFlags.Static | BindingFlags.Public));
    }

    [Theory]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod.Unspecified, "unspecified")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod.Get, "get")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod.Post, "post")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod.Put, "put")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod.Patch, "patch")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod.Delete, "delete")]
    public void BehaviorsHttpRestMethodWireNamesStayAlignedWithJsonSerialization(
        global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod method,
        string expectedWireName)
    {
        Assert.Equal(
            expectedWireName,
            global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethodExtensions.GetWireName(method));
        Assert.True(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethodExtensions.TryParseWireName(expectedWireName, out var parsed));
        Assert.Equal(method, parsed);
        Assert.Equal($"\"{expectedWireName}\"", JsonSerializer.Serialize(method));
    }

    [Fact]
    public void BehaviorsHttpRestBindingSourceExposesStableWireNameHelpers()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource),
            "Unspecified"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource),
            "Route"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource),
            "Query"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource),
            "Header"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource),
            "Body"));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSourceExtensions)
            .GetMethod("GetWireName", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSourceExtensions)
            .GetMethod("TryParseWireName", BindingFlags.Static | BindingFlags.Public));
    }

    [Theory]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource.Unspecified, "unspecified")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource.Route, "route")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource.Query, "query")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource.Header, "header")]
    [InlineData(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource.Body, "body")]
    public void BehaviorsHttpRestBindingSourceWireNamesStayAlignedWithJsonSerialization(
        global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource source,
        string expectedWireName)
    {
        Assert.Equal(
            expectedWireName,
            global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSourceExtensions.GetWireName(source));
        Assert.True(global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSourceExtensions.TryParseWireName(expectedWireName, out var parsed));
        Assert.Equal(source, parsed);
        Assert.Equal($"\"{expectedWireName}\"", JsonSerializer.Serialize(source));
    }

    [Fact]
    public void BehaviorsHttpAssemblyExposesInlineRestBehaviorModuleEngineBuilderMethods()
    {
        var methods = typeof(global::Cephalon.Behaviors.Http.Hosting.RestBehaviorEngineBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static);

        Assert.Contains(methods, static method =>
            method.Name == "AddRestBehaviorModule" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } descriptorType },
                { ParameterType: { } configureType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            descriptorType == typeof(global::Cephalon.Abstractions.Modules.ModuleDescriptor) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorModuleBuilder>));
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModule" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } descriptorType },
                { ParameterType: { } configureType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            descriptorType == typeof(global::Cephalon.Abstractions.Modules.ModuleDescriptor) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>));
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModule" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } descriptorType },
                { ParameterType: { } prefixType },
                { ParameterType: { } configureType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            descriptorType == typeof(global::Cephalon.Abstractions.Modules.ModuleDescriptor) &&
            prefixType == typeof(string) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeCandidateIdSelectors()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeDocumentAndTagSelectors()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("OpenApiDocumentNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("TagNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("OpenApiDocumentNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("TagNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions)
            .GetProperty("OpenApiDocumentNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions)
            .GetProperty("TagNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("OpenApiDocumentNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("TagNames", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeBindingFallbackSelectors()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("BindingFallbackModes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("BindingFallbackModes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions)
            .GetProperty("BindingFallbackModes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("BindingFallbackModes", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeTargetBindingSelectors()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("TargetBindings", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("TargetBindings", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions)
            .GetProperty("TargetBindings", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("TargetBindings", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeEndpointMetadataOverrides()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("EndpointName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("Summary", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("Description", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("ClearEndpointName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("ClearSummary", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("ClearDescription", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("EndpointName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("Summary", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("Description", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("ClearEndpointName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("ClearSummary", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("ClearDescription", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeTagNameOverrides()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("TagName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("TagName", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeOpenApiDocumentNameOverrides()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("OpenApiDocumentName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("OpenApiDocumentName", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeOverrideActionKinds()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("ActionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("ActionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKindExtensions)
            .GetMethod("GetWireName", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKindExtensions)
            .GetMethod("TryParseWireName", BindingFlags.Static | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeRuleCentricRuntimeEffects()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("MatchedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("SuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("SkippedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("SelectionBases", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("SelectionBasisSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("MatchedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("SelectedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("AppliedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("SkippedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("SelectionBases", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("SelectionBasisSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("SelectedActionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("SelectedActionKindSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("AppliedActionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("AppliedActionKindSummaries", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeCapabilityBoundaryOverrides()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("RequiredCapabilityKey", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("ClearRequiredCapability", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("RequiredCapabilityKey", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("ClearRequiredCapability", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeClearBindingsOverrides()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("ClearBindings", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("ClearBindings", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointOverrideBindingModeExposesStableModes()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode),
            "Unspecified"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode),
            "ReplaceExplicit"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode),
            "MergeExplicit"));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingModeExtensions)
            .GetMethod("GetWireName", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingModeExtensions)
            .GetMethod("TryParseWireName", BindingFlags.Static | BindingFlags.Public));
    }

    [Theory]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode.Unspecified, "unspecified")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode.ReplaceExplicit, "replace-explicit")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode.MergeExplicit, "merge-explicit")]
    public void RestEndpointOverrideBindingModeWireNamesStayAlignedWithJsonSerialization(
        global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingMode bindingMode,
        string expectedWireName)
    {
        Assert.Equal(
            expectedWireName,
            global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingModeExtensions.GetWireName(bindingMode));
        Assert.True(global::Cephalon.Abstractions.Transports.RestEndpointOverrideBindingModeExtensions.TryParseWireName(expectedWireName, out var parsed));
        Assert.Equal(bindingMode, parsed);
        Assert.Equal($"\"{expectedWireName}\"", JsonSerializer.Serialize(bindingMode));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeBindingFallbackMode()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("BindingFallbackMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateProjectionDescriptor)
            .GetProperty("BindingFallbackMode", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointBindingSourceExposesStableSources()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource),
            "Unspecified"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource),
            "Route"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource),
            "Query"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource),
            "Header"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource),
            "Body"));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSourceExtensions)
            .GetMethod("GetWireName", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingSourceExtensions)
            .GetMethod("TryParseWireName", BindingFlags.Static | BindingFlags.Public));
    }

    [Theory]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource.Unspecified, "unspecified")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource.Route, "route")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource.Query, "query")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource.Header, "header")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointBindingSource.Body, "body")]
    public void RestEndpointBindingSourceWireNamesStayAlignedWithJsonSerialization(
        global::Cephalon.Abstractions.Transports.RestEndpointBindingSource source,
        string expectedWireName)
    {
        Assert.Equal(
            expectedWireName,
            global::Cephalon.Abstractions.Transports.RestEndpointBindingSourceExtensions.GetWireName(source));
        Assert.True(global::Cephalon.Abstractions.Transports.RestEndpointBindingSourceExtensions.TryParseWireName(expectedWireName, out var parsed));
        Assert.Equal(source, parsed);
        Assert.Equal($"\"{expectedWireName}\"", JsonSerializer.Serialize(source));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeOriginalProjectionHostGovernanceIntent()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateProjectionDescriptor)
            .GetProperty("AllowsHostGovernance", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointCandidateStatusExposesStableStatuses()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus),
            "Unspecified"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus),
            "Published"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus),
            "Suppressed"));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatusExtensions)
            .GetMethod("GetWireName", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatusExtensions)
            .GetMethod("TryParseWireName", BindingFlags.Static | BindingFlags.Public));
    }

    [Theory]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus.Unspecified, "unspecified")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus.Published, "published")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus.Suppressed, "suppressed")]
    public void RestEndpointCandidateStatusWireNamesStayAlignedWithJsonSerialization(
        global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatus status,
        string expectedWireName)
    {
        Assert.Equal(
            expectedWireName,
            global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatusExtensions.GetWireName(status));
        Assert.True(global::Cephalon.Abstractions.Transports.RestEndpointCandidateStatusExtensions.TryParseWireName(expectedWireName, out var parsed));
        Assert.Equal(status, parsed);
        Assert.Equal($"\"{expectedWireName}\"", JsonSerializer.Serialize(status));
    }

    [Fact]
    public void RestEndpointBindingFallbackModeExposesRemainingBodyFallbackValue()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointBindingFallbackMode),
            "PreserveRemainingBodyFallback"));
    }

    [Theory]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback, "preserve-source-implicit-fallback")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback, "preserve-remaining-body-fallback")]
    public void RestEndpointBindingFallbackModeWireNamesStayAlignedWithJsonSerialization(
        global::Cephalon.Abstractions.Transports.RestEndpointBindingFallbackMode mode,
        string expectedWireName)
    {
        Assert.Equal(
            expectedWireName,
            global::Cephalon.Abstractions.Transports.RestEndpointBindingFallbackModeExtensions.GetWireName(mode));
        Assert.True(global::Cephalon.Abstractions.Transports.RestEndpointBindingFallbackModeExtensions.TryParseWireName(expectedWireName, out var parsed));
        Assert.Equal(mode, parsed);
        Assert.Equal($"\"{expectedWireName}\"", JsonSerializer.Serialize(mode));
    }

    [Fact]
    public void RestEndpointAuthoringPolicySuppressionKindExposesPreferredValue()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKind),
            "PreferredAuthoringStyleSelected"));
    }

    [Theory]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle, "disallowed-authoring-style")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle, "not-allowed-authoring-style")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected, "preferred-authoring-style-selected")]
    public void RestEndpointAuthoringPolicySuppressionKindWireNamesStayAlignedWithJsonSerialization(
        global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKind kind,
        string expectedWireName)
    {
        Assert.Equal(
            expectedWireName,
            global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKindExtensions.GetWireName(kind));
        Assert.True(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionKindExtensions.TryParseWireName(expectedWireName, out var parsed));
        Assert.Equal(kind, parsed);
        Assert.Equal($"\"{expectedWireName}\"", JsonSerializer.Serialize(kind));
    }

    [Fact]
    public void RestEndpointGovernanceRuleSelectionBasisExposesStableSpecificityValues()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis),
            "SingleMatch"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis),
            "CandidateTargeting"));
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis),
            "StableRuleId"));
    }

    [Theory]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.SingleMatch, "single-match")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.CandidateTargeting, "candidate-targeting")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.NarrowerCandidateSet, "narrower-candidate-set")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.MoreTargetDimensions, "more-target-dimensions")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.BehaviorTargeting, "behavior-targeting")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.NarrowerAuthoringStyleScope, "narrower-authoring-style-scope")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.FewerTargetValues, "fewer-target-values")]
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.StableRuleId, "stable-rule-id")]
    public void RestEndpointGovernanceRuleSelectionBasisWireNamesStayAlignedWithJsonSerialization(
        global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis basis,
        string expectedWireName)
    {
        Assert.Equal(
            expectedWireName,
            global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasisExtensions.GetWireName(basis));
        Assert.True(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasisExtensions.TryParseWireName(expectedWireName, out var parsed));
        Assert.Equal(basis, parsed);
        Assert.Equal($"\"{expectedWireName}\"", JsonSerializer.Serialize(basis));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeOriginalProjectionTagName()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateProjectionDescriptor)
            .GetProperty("TagName", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupContractsExposeAuthoringStyleSummaries()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor)
            .GetProperty("SuppressedByAuthoringPolicyKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor)
            .GetProperty("SelectedOverrideId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor)
            .GetProperty("SelectedOverrideActionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor)
            .GetProperty("SuppressionSelectionBasis", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor)
            .GetProperty("OverrideSelectionBasis", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor)
            .GetProperty("AppliedOverrideActionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor)
            .GetProperty("SkippedSuppressionIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateRuntimeDescriptor)
            .GetProperty("SkippedOverrideIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("AuthoringStyleSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("AuthoringPolicy", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("AuthoringPolicySuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("AuthoringPolicySuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("GovernanceSuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("GovernanceOverrideSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("SkippedSuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("SkippedOverrideSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("HostGovernanceEligibleCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("HostGovernanceIneligibleCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("SkippedSuppressionIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupDescriptor)
            .GetProperty("SkippedOverrideIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupAuthoringPolicyContractsExposeConfiguredIntent()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicyDescriptor)
            .GetProperty("BehaviorId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicyDescriptor)
            .GetProperty("IsConfigured", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicyDescriptor)
            .GetProperty("AllowMultiplePublishedCandidates", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicyDescriptor)
            .GetProperty("PreferredAuthoringStyle", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicyDescriptor)
            .GetProperty("AllowedAuthoringStyles", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicyDescriptor)
            .GetProperty("DisallowedAuthoringStyles", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointAuthoringPolicyRuntimeContractsExposeIntentAndRuntimeBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.IRestEndpointAuthoringPolicyRuntimeCatalog)
            .GetProperty("Policies", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.IRestEndpointAuthoringPolicyRuntimeCatalog)
            .GetMethod("GetByBehaviorId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("BehaviorId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("IsConfigured", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("AllowMultiplePublishedCandidates", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("PreferredAuthoringStyle", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("AllowedAuthoringStyles", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("DisallowedAuthoringStyles", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("RetainedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("PublishedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("PrecedenceSuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("GovernanceSuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("SuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("SuppressionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("SuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("RestEndpointAuthoringPolicies", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointAuthoringPolicySuppressionSummaryContractsExposeKindAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionSummaryDescriptor)
            .GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicySuppressionSummaryDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupAuthoringStyleContractsExposeOutcomeBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("AuthoringStyle", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("SourceModuleIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("PrecedenceRanks", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("PublishedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("PrecedenceSuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("GovernanceSuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("AuthoringPolicySuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("AuthoringPolicySuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("GovernanceSuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("GovernanceOverrideSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("SkippedSuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("SkippedOverrideSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("HostGovernanceEligibleCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("HostGovernanceIneligibleCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("SkippedSuppressionIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringStyleDescriptor)
            .GetProperty("SkippedOverrideIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupAuthoringPolicySuppressionContractsExposeKindAndCandidateIds()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor)
            .GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupGovernanceSuppressionSummaryContractsExposeRuleAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor)
            .GetProperty("RuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor)
            .GetProperty("MatchedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor)
            .GetProperty("SuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor)
            .GetProperty("SelectionBasisSummaries", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupGovernanceOverrideSummaryContractsExposeRuleAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor)
            .GetProperty("RuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor)
            .GetProperty("MatchedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor)
            .GetProperty("SelectedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor)
            .GetProperty("AppliedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor)
            .GetProperty("SelectionBasisSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor)
            .GetProperty("SelectedActionKindSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor)
            .GetProperty("AppliedActionKindSummaries", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupGovernanceSelectionBasisSummaryContractsExposeBasisAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor)
            .GetProperty("SelectionBasis", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceSelectionBasisSummaryContractsExposeBasisAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSelectionBasisSummaryDescriptor)
            .GetProperty("SelectionBasis", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSelectionBasisSummaryDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryContractsExposeActionKindAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor)
            .GetProperty("ActionKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceOverrideActionKindSummaryContractsExposeActionKindAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideActionKindSummaryDescriptor)
            .GetProperty("ActionKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideActionKindSummaryDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryContractsExposeRuleAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor)
            .GetProperty("RuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryContractsExposeRuleAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor)
            .GetProperty("RuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeRouteGroupPrefixAndRelativePattern()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("RouteGroupPrefix", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("RelativePattern", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeBehaviorType()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("BehaviorType", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeSourceId()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeAuthoringStyle()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("AuthoringStyle", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeCandidateId()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("CandidateId", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeRequiredCapabilityKey()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("RequiredCapabilityKey", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeCapabilityProvenance()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("OriginalRequiredCapabilityKey", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("AppliedOverrideId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("SelectedOverrideId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("SelectedOverrideActionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("OverrideSelectionBasis", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("AppliedOverrideActionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("MatchedOverrideIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("SkippedSuppressionIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("SkippedOverrideIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeOriginalProjection()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("OriginalProjection", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointRuntimeContractsExposeOriginalEndpointMetadata()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("OriginalEndpointName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("OriginalSummary", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("OriginalDescription", BindingFlags.Instance | BindingFlags.Public));
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
