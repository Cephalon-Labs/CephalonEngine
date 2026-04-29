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
            typeof(global::Cephalon.Abstractions.Agentics.AgentToolExecutionOutcomes),
            typeof(global::Cephalon.Abstractions.Agentics.AgentToolRunState),
            typeof(global::Cephalon.Abstractions.Agentics.IAgentToolRunCatalog),
            typeof(global::Cephalon.Abstractions.Retrieval.IKnowledgeIndexCatalog),
            typeof(global::Cephalon.Abstractions.Retrieval.IKnowledgeIndexer),
            typeof(global::Cephalon.Abstractions.Retrieval.KnowledgeIndexFreshnessStates),
            typeof(global::Cephalon.Abstractions.Retrieval.KnowledgeIndexingOutcomes),
            typeof(global::Cephalon.Abstractions.Retrieval.KnowledgeIndexingRequest),
            typeof(global::Cephalon.Abstractions.Retrieval.KnowledgeIndexingResult),
            typeof(global::Cephalon.Abstractions.Retrieval.KnowledgeIndexState),
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
            typeof(global::Cephalon.Abstractions.Behaviors.BehaviorFeatureDisabledException),
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
            typeof(global::Cephalon.Abstractions.Data.EventSubscriptionExecutionReadinessDescriptor),
            typeof(global::Cephalon.Abstractions.Data.EventSubscriptionExecutionReadinessStates),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseMigrationCatalog),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseMigrationContributor),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseMigrationOperationalPlaybookProvider),
            typeof(global::Cephalon.Abstractions.Data.IDatabaseTopologyOperationalSnapshotProvider),
            typeof(global::Cephalon.Abstractions.Data.DatabaseRoleRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Data.ICommand),
            typeof(global::Cephalon.Abstractions.Data.ICommand<>),
            typeof(global::Cephalon.Abstractions.Data.ICommandHandler<>),
            typeof(global::Cephalon.Abstractions.Data.ICommandHandler<,>),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionAcknowledgement),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionBindingDescriptor),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandRetrySources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationCategories),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReportingCoverageStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReportingCoverageStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReporterCoordinationRollup),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureDescriptor),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionResult),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeObservation),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureAcknowledger),
            typeof(global::Cephalon.Abstractions.Data.ICdcCapture),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureContributor),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeReportSink),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRegistry),
            typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureFreshnessStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureFreshnessStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureLagStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureLagStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCapturePublicationStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCapturePublicationStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationBreakdownEntry),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationIssueReasons),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterParticipantRoles),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterParticipantStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterTakeoverStates),
            typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState),
            typeof(global::Cephalon.Abstractions.Data.DataProductDescriptor),
            typeof(global::Cephalon.Abstractions.Data.IDataProduct<>),
            typeof(global::Cephalon.Abstractions.Data.IDataProductCatalog),
            typeof(global::Cephalon.Abstractions.Data.IDataProductContributor),
            typeof(global::Cephalon.Abstractions.Data.IDataProductRegistry),
            typeof(global::Cephalon.Abstractions.Data.IEventDispatchRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Data.IEventDispatchRuntimeDescriptorCatalog),
            typeof(global::Cephalon.Abstractions.Data.IEventSubscriptionExecutionReadinessCatalog),
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
            typeof(global::Cephalon.Abstractions.Execution.DurableExecutionCompensationAction),
            typeof(global::Cephalon.Abstractions.Execution.DurableExecutionPendingSignal),
            typeof(global::Cephalon.Abstractions.Execution.DurableExecutionPendingTimer),
            typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState),
            typeof(global::Cephalon.Abstractions.Execution.ExecutionGraphDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.ExecutionGraphEdgeDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.ExecutionGraphNodeDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.HostedExecutionDescriptor),
            typeof(global::Cephalon.Abstractions.Execution.IDurableExecutionRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Execution.IDurableExecutionRuntimeStateCatalog),
            typeof(global::Cephalon.Abstractions.Execution.IExecutionGraphContributor),
            typeof(global::Cephalon.Abstractions.Execution.IExecutionGraphRegistry),
            typeof(global::Cephalon.Abstractions.Execution.IExecutionRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Execution.IHostedExecutionContributor),
            typeof(global::Cephalon.Abstractions.Execution.IHostedExecutionRegistry),
            typeof(global::Cephalon.Abstractions.Execution.IHostedExecutionRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog),
            typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyPublicationRuntimeState),
            typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Features.FeatureFlagDescriptor),
            typeof(global::Cephalon.Abstractions.Features.FeatureFlagEvaluationContext),
            typeof(global::Cephalon.Abstractions.Features.FeatureFlagEvaluationResult),
            typeof(global::Cephalon.Abstractions.Features.FeatureFlagProviderBindingDescriptor),
            typeof(global::Cephalon.Abstractions.Features.FeatureFlagProviderEvaluationResult),
            typeof(global::Cephalon.Abstractions.Features.FeatureFlagSourceKind),
            typeof(global::Cephalon.Abstractions.Features.FeatureFlagTargetingDescriptor),
            typeof(global::Cephalon.Abstractions.Features.IFeatureFlagContributor),
            typeof(global::Cephalon.Abstractions.Features.IFeatureFlagProvider),
            typeof(global::Cephalon.Abstractions.Features.IFeatureFlagRegistry),
            typeof(global::Cephalon.Abstractions.Features.IFeatureFlagRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Features.IFeatureToggle),
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
            typeof(global::Cephalon.Abstractions.Patterns.BackendForFrontendBehaviorFilterDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.BackendForFrontendClientBindingDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.IBackendForFrontendClientBindingContributor),
            typeof(global::Cephalon.Abstractions.Patterns.IBackendForFrontendClientBindingRegistry),
            typeof(global::Cephalon.Abstractions.Patterns.IBackendForFrontendRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigIngressRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigRouteContributor),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigMigrationRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigRouteRegistry),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigRouter),
            typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigIngressRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigMigrationRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.PatternDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.PatternKind),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigRequest),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigRouteDescriptor),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigRouteResolution),
            typeof(global::Cephalon.Abstractions.Patterns.StranglerFigTarget),
            typeof(global::Cephalon.Abstractions.Technologies.CellBoundaryDescriptor),
            typeof(global::Cephalon.Abstractions.Technologies.CellHealthIsolationDescriptor),
            typeof(global::Cephalon.Abstractions.Technologies.CellRouteDescriptor),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionCategories),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDimensions),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionSeverities),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionStates),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationResult),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationStates),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDependencyStates),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDriftStates),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationResult),
            typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationStates),
            typeof(global::Cephalon.Abstractions.Technologies.ICellBoundaryCatalog),
            typeof(global::Cephalon.Abstractions.Technologies.ICellBoundaryContributor),
            typeof(global::Cephalon.Abstractions.Technologies.ICellBoundaryRegistry),
            typeof(global::Cephalon.Abstractions.Technologies.ICellHealthIsolationCatalog),
            typeof(global::Cephalon.Abstractions.Technologies.ICellHealthIsolationContributor),
            typeof(global::Cephalon.Abstractions.Technologies.ICellHealthIsolationRegistry),
            typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationEdgeMaterializer),
            typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationMaterializationReportSink),
            typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationProviderMaterializer),
            typeof(global::Cephalon.Abstractions.Technologies.ICellRouteCatalog),
            typeof(global::Cephalon.Abstractions.Technologies.ICellRouteContributor),
            typeof(global::Cephalon.Abstractions.Technologies.ICellRouteRegistry),
            typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog),
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
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSuppressionSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSkippedSuppressionSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSkippedOverrideSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSelectionBasisSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideActionKindSummaryDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor),
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
            typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestEndpointRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.IBackendForFrontendRestDocumentRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.IBackendForFrontendRestRuntimeCatalog),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor),
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeMetadataKeys),
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
            typeof(global::Cephalon.Agentics.Services.AgentToolExecutionContext),
            typeof(global::Cephalon.Agentics.Services.AgentToolExecutionDecision),
            typeof(global::Cephalon.Agentics.Services.AgentToolExecutionDecisionKinds),
            typeof(global::Cephalon.Agentics.Services.AgentToolExecutionReport),
            typeof(global::Cephalon.Agentics.Services.AgentToolExecutionRequest),
            typeof(global::Cephalon.Agentics.Services.AgentToolExecutionResult),
            typeof(global::Cephalon.Agentics.Services.IAgentToolDispatcher),
            typeof(global::Cephalon.Agentics.Services.IAgentToolCatalog),
            typeof(global::Cephalon.Agentics.Services.IAgentToolContributor),
            typeof(global::Cephalon.Agentics.Services.IAgentToolExecutionObserver),
            typeof(global::Cephalon.Agentics.Services.IAgentToolExecutionPolicy),
            typeof(global::Cephalon.Agentics.Services.IAgentToolExecutor),
            typeof(global::Cephalon.Agentics.Services.IAgentToolRunReporter),
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
            typeof(global::Cephalon.Eventing.Services.EventSubscriptionExecutionBindingDescriptor),
            typeof(global::Cephalon.Eventing.Services.EventSubscriptionExecutionContext),
            typeof(global::Cephalon.Eventing.Services.EventSubscriptionRuntimeMetadataKeys),
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
            typeof(global::Cephalon.Eventing.Services.IEventSubscriptionExecutionBindingCatalog),
            typeof(global::Cephalon.Eventing.Services.IEventSubscriptionExecutionBindingContributor),
            typeof(global::Cephalon.Eventing.Services.IEventSubscriptionExecutor),
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
            typeof(global::Cephalon.Eventing.Wolverine.Services.WolverineManagedEventSubscriptionExecutionHandler),
            typeof(global::Cephalon.Eventing.Wolverine.Services.WolverineManagedEventSubscriptionExecutionRequest),
            typeof(global::Cephalon.Eventing.Wolverine.Registration.WolverineEventingEngineBuilderExtensions));
    }

    [Fact]
    public void BehaviorEventingBridgeAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Eventing.Behaviors.Registration.BehaviorEventingEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Eventing.Behaviors.Registration.BehaviorEventingEngineBuilderExtensions));
    }

    [Fact]
    public void DataAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.Registration.DataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.Configuration.CdcCaptureExecutionRuntimeOptions),
            typeof(global::Cephalon.Data.Configuration.DataRuntimeOptions),
            typeof(global::Cephalon.Data.Services.CdcCaptureExecutionReport),
            typeof(global::Cephalon.Data.Services.CdcCaptureRuntimeOutcomes),
            typeof(global::Cephalon.Data.Services.ICdcCaptureRuntimeReporter),
            typeof(global::Cephalon.Data.Services.ICdcCaptureExecutionRuntimeContributor),
            typeof(global::Cephalon.Data.Services.ICdcCaptureExecutionRuntimeRegistry),
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
    public void DataMongoDbAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.MongoDB.Registration.MongoDbDataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.MongoDB.Configuration.MongoDbDataOptions),
            typeof(global::Cephalon.Data.MongoDB.Configuration.MongoDbChangeStreamCaptureOptions),
            typeof(global::Cephalon.Data.MongoDB.Registration.MongoDbDataEngineBuilderExtensions));
    }

    [Fact]
    public void DataSqlServerAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.SqlServer.Registration.SqlServerDataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerDataOptions),
            typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerCdcCaptureOptions),
            typeof(global::Cephalon.Data.SqlServer.Registration.SqlServerDataEngineBuilderExtensions));
    }

    [Fact]
    public void DataMySqlAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.MySql.Registration.MySqlDataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.MySql.Configuration.MySqlDataOptions),
            typeof(global::Cephalon.Data.MySql.Configuration.MySqlBinlogCaptureOptions),
            typeof(global::Cephalon.Data.MySql.Registration.MySqlDataEngineBuilderExtensions));
    }

    [Fact]
    public void DataOracleAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.Oracle.Registration.OracleDataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.Oracle.Configuration.OracleDataOptions),
            typeof(global::Cephalon.Data.Oracle.Configuration.OracleLogMinerCaptureOptions),
            typeof(global::Cephalon.Data.Oracle.Registration.OracleDataEngineBuilderExtensions));
    }

    [Fact]
    public void DataPostgresAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.Postgres.Registration.PostgresDataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.Postgres.Configuration.PostgresDataOptions),
            typeof(global::Cephalon.Data.Postgres.Configuration.PostgresLogicalReplicationCaptureOptions),
            typeof(global::Cephalon.Data.Postgres.Registration.PostgresDataEngineBuilderExtensions));
    }

    [Fact]
    public void DataDebeziumAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Data.Debezium.Registration.DebeziumDataEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumDataOptions),
            typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumConnectorOptions),
            typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumCaptureOptions),
            typeof(global::Cephalon.Data.Debezium.Registration.DebeziumDataEngineBuilderExtensions));
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
            typeof(global::Cephalon.Retrieval.Services.IKnowledgeDocumentProvider),
            typeof(global::Cephalon.Retrieval.Services.IKnowledgeQueryEngine),
            typeof(global::Cephalon.Retrieval.Services.KnowledgeCollectionDescriptor),
            typeof(global::Cephalon.Retrieval.Services.KnowledgeDocument),
            typeof(global::Cephalon.Retrieval.Services.KnowledgeDocumentProviderContext),
            typeof(global::Cephalon.Retrieval.Services.KnowledgeQueryMatch),
            typeof(global::Cephalon.Retrieval.Services.KnowledgeQueryRequest),
            typeof(global::Cephalon.Retrieval.Services.KnowledgeQueryResult));
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
    public void EdgeKubernetesGatewayAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Edge.KubernetesGateway.Registration.KubernetesGatewayEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficMaterializerOptions),
            typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationModes),
            typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationOptions),
            typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficRouteOptions),
            typeof(global::Cephalon.Edge.KubernetesGateway.Registration.KubernetesGatewayEngineBuilderExtensions));
    }

    [Fact]
    public void EdgeTraefikAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Edge.Traefik.Registration.TraefikEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikIngressRouteOptions),
            typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikMiddlewareReferenceOptions),
            typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficMaterializerOptions),
            typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationModes),
            typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationOptions),
            typeof(global::Cephalon.Edge.Traefik.Registration.TraefikEngineBuilderExtensions));
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
    public void MultiTenancyGovernanceAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.MultiTenancy.Governance.Registration.MultiTenancyGovernanceEngineBuilderExtensions).Assembly,
            typeof(global::Cephalon.MultiTenancy.Governance.Configuration.MultiTenancyGovernanceOptions),
            typeof(global::Cephalon.MultiTenancy.Governance.Registration.MultiTenancyGovernanceEngineBuilderExtensions),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantGovernanceActionCatalog),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantGovernanceActionContributor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantGovernanceActionDecider),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantGovernanceActionRegistry),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantGovernanceActionStore),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantGovernanceActionWorkflow),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantAdministrationWorkflow),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipCatalog),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipContributor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipHttpProofPublicationCatalog),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipHttpProofPublisher),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipRegistry),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipStore),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipValidator),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipVerificationWorkflow),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipProofEvaluator),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipProofChallengeIssuer),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipProofPublicationPlanner),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipHttpProofCollector),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipDnsTxtProofCollector),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipProofVerificationRunner),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipProofPollingRunner),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantDomainOwnershipProofPollingRuntimeCatalog),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationCatalog),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationContributor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationRegistry),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationStore),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationValidator),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantMembershipCatalog),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantMembershipContributor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantMembershipEvaluator),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantMembershipRegistry),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantMembershipStore),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipDescriptor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofChallengeMetadataKeys),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofChallengeOutcomes),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofChallengeRequest),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofChallengeResult),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPublicationPlanMetadataKeys),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPublicationPlanOutcomes),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPublicationPlanRequest),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPublicationPlanResult),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofPublicationDescriptor),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofPublicationMetadataKeys),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofPublicationOutcomes),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofPublicationRequest),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofPublicationResult),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofCollectionMetadataKeys),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofCollectionOutcomes),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofCollectionRequest),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipHttpProofCollectionResult),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipDnsTxtProofCollectionOutcomes),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipDnsTxtProofCollectionRequest),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipDnsTxtProofCollectionResult),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofVerificationMetadataKeys),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofVerificationOutcomes),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofVerificationRequest),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofVerificationResult),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPollingMetadataKeys),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPollingOutcomes),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPollingRequest),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPollingResult),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofPollingRuntimeSnapshot),
              typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofEvaluationOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofEvaluationRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofEvaluationResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipProofMetadataKeys),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipStatuses),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipValidationOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipValidationRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipValidationResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipVerificationWorkflowCommands),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipVerificationWorkflowOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipVerificationWorkflowRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainOwnershipVerificationWorkflowResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantDomainVerificationMethods),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionDecisionOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionDecisionRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionDecisionResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionDescriptor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionKinds),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionStatuses),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionWorkflowCommands),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionWorkflowOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionWorkflowRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantGovernanceActionWorkflowResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantAdministrationWorkflowCommands),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantAdministrationWorkflowMetadataKeys),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantAdministrationWorkflowOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantAdministrationWorkflowRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantAdministrationWorkflowResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDescriptor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryContext),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryMetadataKeys),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryRunDescriptor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliverySenderResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryStatuses),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryStatusReconciliationOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryStatusReconciliationRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationDeliveryStatusReconciliationResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationStatuses),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationValidationOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationValidationRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantInvitationValidationResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationDeliveryDispatcher),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationDeliveryRunCatalog),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationDeliverySender),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.ITenantInvitationDeliveryStatusReconciler),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantMembershipDescriptor),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantMembershipEvaluationOutcomes),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantMembershipEvaluationRequest),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantMembershipEvaluationResult),
            typeof(global::Cephalon.MultiTenancy.Governance.Services.TenantMembershipStatuses));
    }

    [Fact]
    public void MultiTenancyGovernanceAspNetCoreAssemblyExposesOnlyTheDocumentedHostContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.MultiTenancy.Governance.AspNetCore.Hosting.MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.MultiTenancy.Governance.AspNetCore.Configuration.MultiTenancyGovernanceAspNetCoreOptions),
            typeof(global::Cephalon.MultiTenancy.Governance.AspNetCore.Hosting.MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions),
            typeof(global::Cephalon.MultiTenancy.Governance.AspNetCore.Hosting.MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions),
            typeof(global::Cephalon.MultiTenancy.Governance.AspNetCore.Hosting.TenantAdministrationEndpointRouteBuilderExtensions),
            typeof(global::Cephalon.MultiTenancy.Governance.AspNetCore.Hosting.TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions));
    }

    [Fact]
    public void MultiTenancyGovernanceHttpDeliveryAssemblyExposesOnlyTheDocumentedPackContracts()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting.HttpInvitationDeliveryServiceCollectionExtensions).Assembly,
            typeof(global::Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration.HttpInvitationDeliveryOptions),
            typeof(global::Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting.HttpInvitationDeliveryServiceCollectionExtensions),
            typeof(global::Cephalon.MultiTenancy.Governance.HttpDelivery.Services.HttpInvitationDeliveryPayload));
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
    public void BehaviorsHttpRestBehaviorEndpointGroupBuilderExposesHostGovernanceScopeMethod()
    {
        var methods = typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(methods, static method =>
            method.Name == "WithHostGovernanceScope" &&
            method.GetParameters() is [{ ParameterType: { } parameterType }] &&
            parameterType == typeof(string));
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
    public void BehaviorsHttpRestBehaviorModuleBuilderExposesGeneratedProfileGroupMethods()
    {
        var methods = typeof(global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorModuleBuilder)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(methods, static method =>
            method.Name == "MapGeneratedProfileGroups" &&
            method.GetParameters() is [{ ParameterType: { } prefixType }] &&
            prefixType == typeof(string));
        Assert.Contains(methods, static method =>
            method.Name == "MapGeneratedProfileGroups" &&
            method.GetParameters() is
            [
                { ParameterType: { } prefixType },
                { ParameterType: { } configureType }
            ] &&
            prefixType == typeof(string) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>));
        Assert.Contains(methods, static method =>
            method.Name == "MapGeneratedProfileGroups" &&
            method.GetParameters() is
            [
                { ParameterType: { } prefixType },
                { ParameterType: { } configureType }
            ] &&
            prefixType == typeof(string) &&
            configureType == typeof(Action<string, global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>));
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
            method.Name == "AddRestBehaviorModule" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } moduleIdType },
                { ParameterType: { } displayNameType },
                { ParameterType: { } descriptionType },
                { ParameterType: { } configureType },
                { ParameterType: { } versionType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            moduleIdType == typeof(string) &&
            displayNameType == typeof(string) &&
            descriptionType == typeof(string) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorModuleBuilder>) &&
            versionType == typeof(string));
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
                { ParameterType: { } moduleIdType },
                { ParameterType: { } displayNameType },
                { ParameterType: { } descriptionType },
                { ParameterType: { } configureType },
                { ParameterType: { } versionType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            moduleIdType == typeof(string) &&
            displayNameType == typeof(string) &&
            descriptionType == typeof(string) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>) &&
            versionType == typeof(string));
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
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModule" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } moduleIdType },
                { ParameterType: { } displayNameType },
                { ParameterType: { } descriptionType },
                { ParameterType: { } prefixType },
                { ParameterType: { } configureType },
                { ParameterType: { } versionType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            moduleIdType == typeof(string) &&
            displayNameType == typeof(string) &&
            descriptionType == typeof(string) &&
            prefixType == typeof(string) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>) &&
            versionType == typeof(string));
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModuleGroups" &&
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
            method.Name == "AddGeneratedRestBehaviorModuleGroups" &&
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
            configureType == typeof(Action<string, global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>));
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModuleGroups" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } moduleIdType },
                { ParameterType: { } displayNameType },
                { ParameterType: { } descriptionType },
                { ParameterType: { } configureType },
                { ParameterType: { } versionType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            moduleIdType == typeof(string) &&
            displayNameType == typeof(string) &&
            descriptionType == typeof(string) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>) &&
            versionType == typeof(string));
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModuleGroups" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } moduleIdType },
                { ParameterType: { } displayNameType },
                { ParameterType: { } descriptionType },
                { ParameterType: { } configureType },
                { ParameterType: { } versionType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            moduleIdType == typeof(string) &&
            displayNameType == typeof(string) &&
            descriptionType == typeof(string) &&
            configureType == typeof(Action<string, global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>) &&
            versionType == typeof(string));
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModuleGroups" &&
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
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModuleGroups" &&
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
            configureType == typeof(Action<string, global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>));
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModuleGroups" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } moduleIdType },
                { ParameterType: { } displayNameType },
                { ParameterType: { } descriptionType },
                { ParameterType: { } prefixType },
                { ParameterType: { } configureType },
                { ParameterType: { } versionType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            moduleIdType == typeof(string) &&
            displayNameType == typeof(string) &&
            descriptionType == typeof(string) &&
            prefixType == typeof(string) &&
            configureType == typeof(Action<global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>) &&
            versionType == typeof(string));
        Assert.Contains(methods, static method =>
            method.Name == "AddGeneratedRestBehaviorModuleGroups" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            method.GetParameters() is
            [
                { ParameterType: { } engineType },
                { ParameterType: { } moduleIdType },
                { ParameterType: { } displayNameType },
                { ParameterType: { } descriptionType },
                { ParameterType: { } prefixType },
                { ParameterType: { } configureType },
                { ParameterType: { } versionType }
            ] &&
            engineType == typeof(global::Cephalon.Engine.Composition.EngineBuilder) &&
            moduleIdType == typeof(string) &&
            displayNameType == typeof(string) &&
            descriptionType == typeof(string) &&
            prefixType == typeof(string) &&
            configureType == typeof(Action<string, global::Cephalon.Behaviors.Http.Hosting.IRestBehaviorEndpointGroupBuilder>) &&
            versionType == typeof(string));
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
    public void RestEndpointGovernanceContractsExposeBehaviorIdPrefixSelectors()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("BehaviorIdPrefixes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("BehaviorIdPrefixes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions)
            .GetProperty("BehaviorIdPrefixes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("BehaviorIdPrefixes", BindingFlags.Instance | BindingFlags.Public));
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
    public void RestEndpointGovernanceContractsExposeEndpointNameSelectors()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("EndpointNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("EndpointNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions)
            .GetProperty("EndpointNames", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("EndpointNames", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceContractsExposeHostGovernanceScopeSelectors()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointSuppressionDescriptor)
            .GetProperty("HostGovernanceScopes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("HostGovernanceScopes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointSuppressionOptions)
            .GetProperty("HostGovernanceScopes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("HostGovernanceScopes", BindingFlags.Instance | BindingFlags.Public));
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
    public void RestEndpointOverrideActionKindsExposePreserveImplicitQueryFallback()
    {
        Assert.True(Enum.IsDefined(
            typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKind),
            "PreserveImplicitQueryFallback"));
        Assert.Equal(
            "preserve-implicit-query-fallback",
            global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKindExtensions.GetWireName(
                global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKind.PreserveImplicitQueryFallback));
        Assert.True(global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKindExtensions.TryParseWireName(
            "preserve-implicit-query-fallback",
            out var parsed));
        Assert.Equal(
            global::Cephalon.Abstractions.Transports.RestEndpointOverrideActionKind.PreserveImplicitQueryFallback,
            parsed);
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
    public void RestEndpointGovernanceContractsExposeFeatureBoundaryOverrides()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("RequiredFeatureFlagIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("ClearRequiredFeatureFlags", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("RequiredFeatureFlagIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("ClearRequiredFeatureFlags", BindingFlags.Instance | BindingFlags.Public));
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
    public void RestEndpointGovernanceContractsExposePreserveImplicitQueryFallbackOverrides()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointOverrideDescriptor)
            .GetProperty("PreserveImplicitQueryFallback", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.AspNetCore.Hosting.RestEndpointOverrideOptions)
            .GetProperty("PreserveImplicitQueryFallback", BindingFlags.Instance | BindingFlags.Public));
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
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointCandidateProjectionDescriptor)
            .GetProperty("HostGovernanceScope", BindingFlags.Instance | BindingFlags.Public));
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
            "NarrowerBehaviorScope"));
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
    [InlineData(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope, "narrower-behavior-scope")]
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
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("HostGovernanceEligibleCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("HostGovernanceIneligibleCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("SkippedSuppressionIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("SkippedOverrideIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("GovernanceSuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("GovernanceOverrideSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("SkippedSuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("SkippedOverrideSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyDescriptor)
            .GetProperty("AuthoringStyleSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("RestEndpointAuthoringPolicies", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void BackendForFrontendRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.BackendForFrontendClientBindingDescriptor)
            .GetProperty("ClientId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.BackendForFrontendClientBindingDescriptor)
            .GetProperty("TransportId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.BackendForFrontendClientBindingDescriptor)
            .GetProperty("EntryPoint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.BackendForFrontendClientBindingDescriptor)
            .GetProperty("BehaviorFilter", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("BackendForFrontendBindings", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void BackendForFrontendRestRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestEndpointRuntimeDescriptor)
            .GetProperty("Binding", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestEndpointRuntimeDescriptor)
            .GetProperty("Endpoint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestEndpointRuntimeDescriptor)
            .GetProperty("MatchedByDefault", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestEndpointRuntimeDescriptor)
            .GetProperty("MatchedBehaviorIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestEndpointRuntimeDescriptor)
            .GetProperty("MatchedCapabilityKeys", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestEndpointRuntimeDescriptor)
            .GetProperty("MatchedTags", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("BackendForFrontendRestEndpoints", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void BackendForFrontendRestDocumentRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("ScopeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("ClientId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("DocumentName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("OpenApiPath", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("ScalarPath", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("BindingIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("RuntimeEndpointIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.BackendForFrontendRestDocumentRuntimeDescriptor)
            .GetProperty("RestEndpointIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("BackendForFrontendRestDocuments", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointAuthoringPolicyAuthoringStyleSummaryContractsExposeRuntimeBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("AuthoringStyle", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("RetainedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("PublishedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("PrecedenceSuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("GovernanceSuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("SuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("SuppressionKinds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("SuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("HostGovernanceEligibleCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("HostGovernanceIneligibleCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("SkippedSuppressionIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("SkippedOverrideIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("GovernanceSuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("GovernanceOverrideSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("SkippedSuppressionSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointAuthoringPolicyAuthoringStyleDescriptor)
            .GetProperty("SkippedOverrideSummaries", BindingFlags.Instance | BindingFlags.Public));
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
    public void RestEndpointGovernanceSuppressionSummaryContractsExposeRuleAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSuppressionSummaryDescriptor)
            .GetProperty("RuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSuppressionSummaryDescriptor)
            .GetProperty("MatchedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSuppressionSummaryDescriptor)
            .GetProperty("SuppressedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSuppressionSummaryDescriptor)
            .GetProperty("SelectionBasisSummaries", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceOverrideSummaryContractsExposeRuleAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideSummaryDescriptor)
            .GetProperty("RuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideSummaryDescriptor)
            .GetProperty("MatchedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideSummaryDescriptor)
            .GetProperty("SelectedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideSummaryDescriptor)
            .GetProperty("AppliedCandidateIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideSummaryDescriptor)
            .GetProperty("SelectionBasisSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideSummaryDescriptor)
            .GetProperty("SelectedActionKindSummaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceOverrideSummaryDescriptor)
            .GetProperty("AppliedActionKindSummaries", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceSkippedSuppressionSummaryContractsExposeRuleAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSkippedSuppressionSummaryDescriptor)
            .GetProperty("RuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSkippedSuppressionSummaryDescriptor)
            .GetProperty("CandidateIds", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void RestEndpointGovernanceSkippedOverrideSummaryContractsExposeRuleAndCandidateBuckets()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSkippedOverrideSummaryDescriptor)
            .GetProperty("RuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointGovernanceSkippedOverrideSummaryDescriptor)
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
            .GetProperty("RequiredFeatureFlagIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Transports.RestEndpointRuntimeDescriptor)
            .GetProperty("OriginalRequiredFeatureFlagIds", BindingFlags.Instance | BindingFlags.Public));
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
    public void RestEndpointRuntimeMetadataKeysExposeOwnershipTruth()
    {
        Assert.Equal(
            "restPublicationActivationOwnership",
            global::Cephalon.Abstractions.Transports.RestEndpointRuntimeMetadataKeys.RestPublicationActivationOwnership);
        Assert.Equal(
            "restMaterializationOwnership",
            global::Cephalon.Abstractions.Transports.RestEndpointRuntimeMetadataKeys.RestMaterializationOwnership);
        Assert.Equal(
            "restProfileMetadataOwnership",
            global::Cephalon.Abstractions.Transports.RestEndpointRuntimeMetadataKeys.RestProfileMetadataOwnership);
        Assert.Equal(
            "restPublicationActivationMode",
            global::Cephalon.Abstractions.Transports.RestEndpointRuntimeMetadataKeys.RestPublicationActivationMode);
    }

    [Fact]
    public void DurableExecutionRuntimeStateContractsExposePendingCoordination()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState)
            .GetProperty("PendingTimers", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState)
            .GetProperty("PendingSignals", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState)
            .GetProperty("CompensationActions", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState)
            .GetProperty("HasPendingTimers", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState)
            .GetProperty("HasPendingSignals", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState)
            .GetProperty("HasCompensationActions", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState)
            .GetProperty("NextTimerDueAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.DurableExecutionRuntimeState)
            .GetProperty("CoordinationPending", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void DurableExecutionRuntimeStateCatalogContractsExposePendingCoordinationFilters()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.IDurableExecutionRuntimeStateCatalog)
            .GetMethod("GetWithPendingTimers", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.IDurableExecutionRuntimeStateCatalog)
            .GetMethod("GetWithPendingSignals", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.IDurableExecutionRuntimeStateCatalog)
            .GetMethod("GetWithCompensationActions", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.IDurableExecutionRuntimeStateCatalog)
            .GetMethod("GetByPendingTimerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.IDurableExecutionRuntimeStateCatalog)
            .GetMethod("GetByPendingSignalId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.IDurableExecutionRuntimeStateCatalog)
            .GetMethod("GetByCompensationActionId", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void DurableExecutionStepResultContractsExposePendingCoordination()
    {
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.DurableExecutionStepResult<>)
            .GetProperty("PendingTimers", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.DurableExecutionStepResult<>)
            .GetProperty("PendingSignals", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.DurableExecutionStepResult<>)
            .GetProperty("CompensationActions", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void SagaChoreographyHelperContractsExposeReactorAndTypedResultSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ISagaEventReactor<>)
            .GetMethod("ReactAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ISagaEventReactor<,>)
            .GetMethod("ReactAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyStepResult<>)
            .GetProperty("Output", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyStepResult<>)
            .GetProperty("Publications", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyPublication)
            .GetMethod("CreateJson", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(global::Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyPublication)
            .GetMethod("CreateCompensationJson", BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void SagaChoreographyRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyRuntimeCatalog)
            .GetProperty("SagaChoreographies", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyRuntimeCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyRuntimeCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyRuntimeCatalog)
            .GetMethod("GetByTransportId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyRuntimeDescriptor)
            .GetProperty("ResultType", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyRuntimeDescriptor)
            .GetProperty("LocalOutputType", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyRuntimeDescriptor)
            .GetProperty("SuccessStatusCodes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("SagaChoreographies", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void SagaChoreographyPublicationRuntimeStateContractsExposeRuntimeSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetProperty("States", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("GetByBehaviorId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("GetByTransportId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("GetByChannelId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("GetByCorrelationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("GetCompensationPublications", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("GetFailedPublications", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.ISagaChoreographyPublicationRuntimeStateCatalog)
            .GetMethod("TryGetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyPublicationRuntimeState)
            .GetProperty("PublicationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyPublicationRuntimeState)
            .GetProperty("CorrelationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyPublicationRuntimeState)
            .GetProperty("IsCompensation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyPublicationRuntimeState)
            .GetProperty("AcceptedCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyPublicationRuntimeState)
            .GetProperty("FailedCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Execution.SagaChoreographyPublicationRuntimeState)
            .GetProperty("LastPublisherType", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("SagaChoreographyPublicationStates", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void StranglerFigIngressRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigIngressRuntimeCatalog)
            .GetProperty("Routes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigIngressRuntimeCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.IStranglerFigIngressRuntimeCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.StranglerFigIngressRuntimeDescriptor)
            .GetProperty("SelectedEndpointKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.StranglerFigIngressRuntimeDescriptor)
            .GetProperty("IngressMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.StranglerFigIngressRuntimeDescriptor)
            .GetProperty("CanMaterialize", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.StranglerFigIngressRuntimeDescriptor)
            .GetProperty("TargetPathPrefix", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Patterns.StranglerFigIngressRuntimeDescriptor)
            .GetProperty("TargetUri", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("StranglerFigIngressRoutes", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void DataProductRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IDataProduct<>)
            .GetMethod("QueryAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IDataProductCatalog)
            .GetProperty("DataProducts", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IDataProductCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IDataProductCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IDataProductCatalog)
            .GetMethod("GetByDomainId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IDataProductCatalog)
            .GetMethod("GetByContractId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IDataProductContributor)
            .GetMethod("RegisterDataProducts", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IDataProductRegistry)
            .GetMethod("Add", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.DataProductDescriptor)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.DataProductDescriptor)
            .GetProperty("DomainId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.DataProductDescriptor)
            .GetProperty("ContractId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.DataProductDescriptor)
            .GetProperty("Mode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("DataProducts", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void CdcCaptureRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureAcknowledger)
            .GetMethod("AcknowledgeAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCapture)
            .GetProperty("CdcCaptureId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCapture)
            .GetMethod("CaptureAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionAcknowledgement)
            .GetProperty("CdcCaptureId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionAcknowledgement)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionAcknowledgement)
            .GetProperty("Messages", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionAcknowledgement)
            .GetProperty("StagedMessageCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionAcknowledgement)
            .GetProperty("Checkpoint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetProperty("Runtimes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByEdgeNodeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByReporterCoordinationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByReporterCoordinationIssueReason", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByRemediationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByRemediationCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorGovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorGovernanceCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDriftCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorWritePathReadinessState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorWritePathReadinessCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorPreflightState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorPreflightCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorPreflightOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDryRunState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDryRunCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDryRunOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionIntentState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionIntentCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionIntentOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionApprovalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionApprovalCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionApprovalOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandEnvelopeState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandEnvelopeCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandEnvelopeOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandIssuanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandIssuanceCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandIssuanceOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionAdapterState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionAdapterCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorExecutionAdapterOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandExecutionOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetManagedConnectorCommandExecutionHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandJournalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandJournalCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandJournalDurabilityState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCommandJournalDurabilityCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorAutomaticRetryExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorAutomaticRetryExecutionCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorAutomaticRetryExecutionOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorAutomaticRetryCoordinationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorAutomaticRetryCoordinationCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorAutomaticRetryCoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDistributedRetryLeaseState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDistributedRetryLeaseCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDistributedRetryLeaseOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCrossNodeIdempotencyHardeningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCrossNodeIdempotencyHardeningCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCrossNodeIdempotencyHardeningOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorCrossNodeIdempotencyHardeningRetryFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDistributedRetryOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDistributedRetryOrchestrationCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDistributedRetryOrchestrationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDurableSharedSchedulerOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDurableSharedSchedulerOrchestrationCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorDurableSharedSchedulerOrchestrationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorMultiNodeLeaseExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorMultiNodeLeaseExecutionCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorMultiNodeLeaseExecutionOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorSchedulerRecoveryExecutionHardeningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorSchedulerRecoveryExecutionHardeningCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorSchedulerRecoveryExecutionHardeningOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorSchedulerRecoveryExecutionHardeningRetryFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedWritePathExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedWritePathExecutionCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedWritePathExecutionOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderExecutionOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderExecutionOrchestrationCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderExecutionOrchestrationOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneOwnershipState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneOwnershipCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneOwnershipOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneMutationReconcileState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneMutationReconcileCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneMutationReconcileOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneProvisioningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneProvisioningCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneProvisioningOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderSpecificControlPlaneMaterializerState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderSpecificControlPlaneMaterializerCategory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderSpecificControlPlaneMaterializerProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderSpecificControlPlaneMaterializerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog)
            .GetMethod("GetByManagedConnectorProviderSpecificControlPlaneMaterializerOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor)
            .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter)
            .GetMethod("CanHandle", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter)
            .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ExecutionOwnership", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ExecutionTopology", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("AcknowledgementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("HostedExecutionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ExecutionGraphId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("Summary", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ObservationStaleAfterSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("RejectOutOfOrderReports", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorGovernance", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorDrift", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorActionPlan", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorWritePathReadiness", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorPreflight", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorDryRun", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorExecutionIntent", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorExecutionApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorCommandEnvelope", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorCommandIssuance", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorExecutionAdapter", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorCommandExecution", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorCommandJournal", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorCommandJournalDurability", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorAutomaticRetryExecution", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorAutomaticRetryCoordination", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorDistributedRetryLease", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorCrossNodeIdempotencyHardening", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorDistributedRetryOrchestration", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorDurableSharedSchedulerOrchestration", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorMultiNodeLeaseExecution", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorSchedulerRecoveryExecutionHardening", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderOwnedWritePathExecution", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderExecutionOrchestration", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderOwnedControlPlaneOwnership", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderOwnedControlPlaneMutationReconcile", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderOwnedControlPlaneProvisioning", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeDescriptor)
            .GetProperty("ManagedConnectorProviderSpecificControlPlaneMaterializer", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("ReportedCdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("LastAcknowledgement", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("TotalCapturedChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("LastReportId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("ObservationFreshness", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("ReporterCoordination", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("ReporterCoordinationRollup", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("ReportingCoverage", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("Remediation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("HasUnreportedDeclaredCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("HasFullCaptureCoverage", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("RequiresRemediation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeSummary)
            .GetProperty("HasBlockingRemediation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionBindingDescriptor)
            .GetProperty("ExecutionTopology", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionBindingDescriptor)
            .GetProperty("RequestedExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionBindingDescriptor)
            .GetProperty("EffectiveExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureDescriptor)
            .GetProperty("ExecutionBinding", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("ExecutionBinding", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionResult)
            .GetProperty("Messages", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionResult)
            .GetProperty("CapturedChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionResult)
            .GetProperty("ProducedMessageCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionResult)
            .GetProperty("Checkpoint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.IOutbox)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog)
            .GetProperty("CdcCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog)
            .GetMethod("GetByProvider", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog)
            .GetMethod("GetByOutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog)
            .GetMethod("GetBySourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog)
            .GetMethod("GetByExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureCatalog)
            .GetMethod("GetByResourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetProperty("States", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetByProvider", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetByOutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetBySourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetByExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetByReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetByEdgeNodeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetByReporterCoordinationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetByReporterCoordinationIssueReason", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRuntimeStateCatalog)
            .GetMethod("GetByResourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureContributor)
            .GetMethod("RegisterCdcCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureRegistry)
            .GetMethod("Add", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureDescriptor)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureDescriptor)
            .GetProperty("Provider", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureDescriptor)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureDescriptor)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureDescriptor)
            .GetProperty("Mode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureDescriptor)
            .GetProperty("EventFormat", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("Freshness", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("LastReportId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("ObservationFreshness", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("ReporterCoordination", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("Lag", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("Publication", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStates)
            .GetField("Active", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStates)
            .GetField("LeaseExpired", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationIssueReasons)
            .GetField("AwaitingTakeover", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationIssueReasons)
            .GetField("RejectedReporterConflict", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationIssueReasons)
            .GetField("MultipleActiveReporters", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterTakeoverStates)
            .GetField("AwaitingTakeover", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterTakeoverStates)
            .GetField("Completed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("ActiveReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("PreviousReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("LastTakeoverObservedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("LastConflictingReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("TakeoverState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("DegradedReason", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("RequiresTakeover", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("HasCompletedTakeover", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureFreshnessStates)
            .GetField("Mixed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureFreshnessStatus)
            .GetProperty("FreshUntilUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureLagStatus)
            .GetProperty("PendingChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCapturePublicationStatus)
            .GetProperty("PendingPublicationCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterParticipantRoles)
            .GetField("Active", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterParticipantRoles)
            .GetField("Standby", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterParticipantRoles)
            .GetField("Rejected", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterParticipantStatus)
            .GetProperty("ReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterParticipantStatus)
            .GetProperty("Role", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterParticipantStatus)
            .GetProperty("LastObservedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("ReporterParticipants", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("HasStandbyReporters", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationStatus)
            .GetProperty("HasRejectedReporters", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationBreakdownEntry)
            .GetProperty("Id", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureReporterCoordinationBreakdownEntry)
            .GetProperty("Count", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReporterCoordinationRollup)
            .GetProperty("CoordinationStateBreakdown", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReporterCoordinationRollup)
            .GetProperty("DegradedReasonBreakdown", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReporterCoordinationRollup)
            .GetProperty("ActiveReporterIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReporterCoordinationRollup)
            .GetProperty("StandbyReporterIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReporterCoordinationRollup)
            .GetProperty("RejectedReporterIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReporterCoordinationRollup)
            .GetProperty("DegradedCdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReportingCoverageStatus)
            .GetProperty("DeclaredCaptureCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReportingCoverageStatus)
            .GetProperty("ReportedCaptureCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReportingCoverageStatus)
            .GetProperty("UnreportedCdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeReportingCoverageStatus)
            .GetProperty("HasFullCoverage", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationCategories)
            .GetField("UnreportedCdcCaptures", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationCategories)
            .GetField("StaleObservations", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationCategories)
            .GetField("FailedCdcCaptures", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationCategories)
            .GetField("ReporterCoordinationIssues", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStates)
            .GetField("Ready", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStates)
            .GetField("Attention", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus)
            .GetProperty("AffectedCdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus)
            .GetProperty("UnreportedCdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus)
            .GetProperty("StaleCdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus)
            .GetProperty("FailedCdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus)
            .GetProperty("ReporterCoordinationIssueCdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus)
            .GetProperty("RequiresRemediation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeRemediationStatus)
            .GetProperty("IsBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("SourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ExpectedTaskCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ReportedTaskCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("DeclaredTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ReportedTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ActiveTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ConnectorLifecycleState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("TaskReconciliationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ReconciliationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("ReconciliationReason", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("RecommendedActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("IsObserveOnly", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("RequiresControlPlaneSupport", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("IsOutOfPolicy", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds)
            .GetField("CompleteTaskBaseline", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds)
            .GetField("WaitForRuntimeReport", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds)
            .GetField("InvestigateDrift", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("MissingTaskBaseline", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("ReportedTaskTopologyUnavailable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("ReportedTaskIdentityUnavailable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("TaskCountMismatch", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("MissingDeclaredTaskReports", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("UnexpectedReportedTasks", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("ConnectClusterMismatch", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("ConnectorClassMismatch", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftCategories)
            .GetField("SourceProviderMismatch", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStates)
            .GetField("InSync", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStates)
            .GetField("Drifted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("DeclaredConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ReportedConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("DeclaredConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ReportedConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("DeclaredSourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ReportedSourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ExpectedTaskCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ReportedTaskCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("DeclaredTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ReportedTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ActiveTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("MissingDeclaredTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("UnexpectedReportedTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ConnectorLifecycleState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("TaskReconciliationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ReconciliationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("ReconciliationReason", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("RecommendedActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("CanEvaluateDrift", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("IsInSync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("IsDrifted", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDriftStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("BlockingRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("RuntimeRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("IncompleteReportingCoverage", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("GovernanceOutOfPolicy", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("RuntimeTruthIncomplete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("DriftDetected", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("ObserveOnlyMode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("WritePathRequested", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories)
            .GetField("WritePathReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates)
            .GetField("Deferred", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates)
            .GetField("NotReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates)
            .GetField("Ready", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("ReportingCoverageState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("RemediationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("GovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("DriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("ActionPlanState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("PrimaryActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("IsDeferred", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("IsReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("IsBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("BlockingRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("RuntimeRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("IncompleteReportingCoverage", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("GovernanceOutOfPolicy", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("RuntimeTruthIncomplete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("DriftDetected", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("ObserveOnlyMode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("ReconcileIntent", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories)
            .GetField("PreflightReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds)
            .GetField("None", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds)
            .GetField("Reconcile", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds)
            .GetField("Pause", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds)
            .GetField("Resume", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds)
            .GetField("Restart", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds)
            .GetField("Delete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStates)
            .GetField("Deferred", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStates)
            .GetField("NotReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStates)
            .GetField("Ready", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("ReportingCoverageState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("RemediationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("GovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("DriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("ActionPlanState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("WritePathReadinessState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("PrimaryActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("IsDeferred", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("IsReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("IsBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("BlockingRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("RuntimeRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("IncompleteReportingCoverage", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("GovernanceOutOfPolicy", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("RuntimeTruthIncomplete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("ObserveOnlyMode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("NoChangesRequired", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("ChangePlanned", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("LifecycleChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("ConnectClusterChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("ConnectorClassChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("SourceProviderChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories)
            .GetField("TaskTopologyChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds)
            .GetField("None", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds)
            .GetField("Reconcile", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds)
            .GetField("Pause", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds)
            .GetField("Resume", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds)
            .GetField("Restart", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds)
            .GetField("Delete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStates)
            .GetField("Deferred", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStates)
            .GetField("NoOp", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStates)
            .GetField("WouldChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("ReportingCoverageState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("RemediationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("GovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("DriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("ActionPlanState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("WritePathReadinessState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("PreflightState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("PrimaryActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("ConnectorLifecycleState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("ReconciliationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("MissingDeclaredTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("UnexpectedReportedTaskIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("PotentialChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("IsDeferred", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("IsBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("IsNoOp", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("IsWouldChange", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("BlockingRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("RuntimeRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("IncompleteReportingCoverage", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("GovernanceOutOfPolicy", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("RuntimeTruthIncomplete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("ObserveOnlyMode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("FutureControlPlane", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("ApprovalRequired", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("EngineExecutionCandidate", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("NoExecutionNeeded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("ChangePlanned", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories)
            .GetField("LifecycleChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds)
            .GetField("None", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds)
            .GetField("Reconcile", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds)
            .GetField("Pause", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds)
            .GetField("Resume", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds)
            .GetField("Restart", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds)
            .GetField("Delete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources)
            .GetField("Unknown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources)
            .GetField("ActionPlan", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources)
            .GetField("WritePathReadiness", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources)
            .GetField("Preflight", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources)
            .GetField("DryRun", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates)
            .GetField("Deferred", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates)
            .GetField("OperatorAction", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates)
            .GetField("RequiresApproval", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates)
            .GetField("ReadyToExecute", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("ReportingCoverageState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("RemediationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("GovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("DriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("ActionPlanState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("WritePathReadinessState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("PreflightState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("DryRunState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("PrimaryActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("ConfidenceSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("PotentialChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("IsDeferred", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("IsBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("IsOperatorAction", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("IsApprovalRequired", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("CanExecuteThroughEngine", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("IsReadyToExecute", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("IsOperatorOnly", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("ObserveOnlyMode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("BlockingRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("StaleObservation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("IncompleteReportingCoverage", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("RuntimeTruthIncomplete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("GovernanceOutOfPolicy", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("ControlPlaneOwnershipGap", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("DriftRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("LifecycleChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("DestructiveOperation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("ApprovalRequired", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("ApprovalReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("AutoEligible", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories)
            .GetField("NoExecutionNeeded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds)
            .GetField("None", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds)
            .GetField("Reconcile", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds)
            .GetField("Pause", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds)
            .GetField("Resume", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds)
            .GetField("Restart", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds)
            .GetField("Delete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources)
            .GetField("Unknown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources)
            .GetField("Remediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources)
            .GetField("Governance", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources)
            .GetField("DryRun", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources)
            .GetField("ExecutionIntent", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates)
            .GetField("AutoBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates)
            .GetField("PolicyBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates)
            .GetField("ApprovalRequired", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates)
            .GetField("ApprovalReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates)
            .GetField("AutoEligible", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("ReportingCoverageState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("RemediationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("GovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("DriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("ActionPlanState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("WritePathReadinessState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("PreflightState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("DryRunState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("ExecutionIntentState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("PrimaryActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("ExecutionIntentConfidenceSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("PotentialChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("RequiresExplicitApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("IsAutoBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("IsPolicyBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("IsApprovalRequired", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("IsApprovalReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("IsAutoEligible", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("CanRequestApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("CanAutoExecuteThroughEngine", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("HasSafetyGateClearance", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("ObserveOnlyMode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("BlockingRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("StaleObservation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("IncompleteReportingCoverage", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("RuntimeTruthIncomplete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("GovernanceOutOfPolicy", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("ControlPlaneOwnershipGap", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("ChangePlanned", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("LifecycleChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("DestructiveOperation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("ApprovalRequired", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("ApprovalReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("ApprovalGated", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("EngineReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories)
            .GetField("NoExecutionNeeded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds)
            .GetField("None", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds)
            .GetField("Reconcile", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds)
            .GetField("Pause", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds)
            .GetField("Resume", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds)
            .GetField("Restart", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds)
            .GetField("Delete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources)
            .GetField("Unknown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources)
            .GetField("ExecutionApproval", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources)
            .GetField("ExecutionIntent", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources)
            .GetField("DryRun", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates)
            .GetField("ApprovalGated", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates)
            .GetField("EngineReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ReportingCoverageState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("RemediationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("GovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("DriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ActionPlanState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("WritePathReadinessState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("PreflightState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("DryRunState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ExecutionIntentState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ExecutionApprovalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("PrimaryActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ExecutionIntentConfidenceSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ExecutionApprovalSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("PotentialChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("RequiresExplicitApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("IsDestructiveOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("ConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("SourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("CommandFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("IsBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("IsOperatorOnly", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("IsApprovalGated", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("IsEngineReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("HasCommandTarget", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("CanPrepareFutureExecutionLane", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("ObserveOnlyMode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("BlockingRemediation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("StaleObservation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("IncompleteReportingCoverage", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("RuntimeTruthIncomplete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("GovernanceOutOfPolicy", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("ControlPlaneOwnershipGap", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("ChangePlanned", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("LifecycleChange", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("DestructiveOperation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("ApprovalRequired", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("ApprovalReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("ApprovalGated", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("Accepted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("Rejected", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("Issued", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories)
            .GetField("NoExecutionNeeded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds)
            .GetField("None", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds)
            .GetField("Reconcile", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds)
            .GetField("Pause", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds)
            .GetField("Resume", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds)
            .GetField("Restart", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds)
            .GetField("Delete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources)
            .GetField("Unknown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources)
            .GetField("CommandEnvelope", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources)
            .GetField("ExecutionApproval", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources)
            .GetField("ExecutionIntent", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates)
            .GetField("Accepted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates)
            .GetField("Rejected", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates)
            .GetField("Issued", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories)
            .GetField("AdapterReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories)
            .GetField("AdapterUnavailable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds)
            .GetField("None", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds)
            .GetField("DebeziumKafkaConnectRest", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates)
            .GetField("Unavailable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates)
            .GetField("Ready", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates)
            .GetField("Empty", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates)
            .GetField("Bounded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates)
            .GetField("Truncated", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates)
            .GetField("CooldownActive", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates)
            .GetField("DuplicateEvidencePresent", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates)
            .GetField("InsufficientForAutomation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates)
            .GetField("InMemoryOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates)
            .GetField("Persisted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates)
            .GetField("Recovered", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates)
            .GetField("RecoveryFailed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates)
            .GetField("PersistenceFailed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates)
            .GetField("Disabled", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates)
            .GetField("Eligible", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates)
            .GetField("Completed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates)
            .GetField("SingleNode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates)
            .GetField("Uncoordinated", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates)
            .GetField("LeaseHeld", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates)
            .GetField("LeaseMissing", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates)
            .GetField("Conflicted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates)
            .GetField("SingleNode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates)
            .GetField("LeaseHeld", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates)
            .GetField("LeaseMissing", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates)
            .GetField("LeaseConflicted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates)
            .GetField("IdempotentSafe", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates)
            .GetField("IdempotencyRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates)
            .GetField("IdempotentSafe", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates)
            .GetField("StaleOwnerRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates)
            .GetField("DuplicateLineageRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates)
            .GetField("ReplayWindowRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates)
            .GetField("Disabled", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates)
            .GetField("Cooldown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates)
            .GetField("Scheduled", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates)
            .GetField("Completed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates)
            .GetField("Disabled", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates)
            .GetField("Unscheduled", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates)
            .GetField("Scheduled", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates)
            .GetField("LeaseBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates)
            .GetField("RecoveryNeeded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates)
            .GetField("SchedulerConflicted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates)
            .GetField("SingleNode", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates)
            .GetField("LeaseExecutable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates)
            .GetField("LeaseBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates)
            .GetField("LeaseConflicted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates)
            .GetField("StaleLeaseRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates)
            .GetField("RecoveryReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates)
            .GetField("RecoveryBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates)
            .GetField("Replaying", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates)
            .GetField("ExecutionHardened", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates)
            .GetField("ExecutionRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates)
            .GetField("ProviderExecutable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates)
            .GetField("ProviderBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates)
            .GetField("ProviderOwnedExecuting", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates)
            .GetField("ProviderOwnedCompleted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates)
            .GetField("ProviderOwnedRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates)
            .GetField("OrchestrationReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates)
            .GetField("OrchestrationBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates)
            .GetField("OrchestrationExecuting", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates)
            .GetField("OrchestrationCompleted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates)
            .GetField("OrchestrationRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("Unrecorded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("Unrecorded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("Blocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("Unavailable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("NoOp", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("Adapted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates)
            .GetField("Failed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ReportingCoverageState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("RemediationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("GovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("DriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ActionPlanState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("WritePathReadinessState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("PreflightState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("DryRunState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ExecutionIntentState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ExecutionApprovalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("CommandEnvelopeState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("PrimaryActionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("CommandEnvelopeSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ExecutionIntentConfidenceSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ExecutionApprovalSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("PotentialChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("RequiresExplicitApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("IsDestructiveOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("ConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("SourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("CommandFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("IssuanceFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("IsBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("IsOperatorOnly", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("IsAccepted", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("IsRejected", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("IsIssued", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("HasIssuableCommand", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("CanEnterFutureIssuanceLane", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus)
            .GetProperty("RequiresAttention", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("CommandIssuanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("AdapterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("AdapterFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("IsReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("IsUnavailable", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("HasAdaptableCommand", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus)
            .GetProperty("CanUseProviderExecutionAdapter", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest)
            .GetProperty("Approve", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest)
            .GetProperty("AllowDestructive", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("AttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("RecordedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("RequestedOperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("ExecutionAdapterState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("TransportKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("HttpMethod", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("RelativePath", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("ExecutionFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("HasProviderCommand", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("IsUnrecorded", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("HasRecordedOutcome", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("IsAdapted", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("IsNoOp", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("ApprovalApplied", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("DestructiveAllowanceApplied", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult)
            .GetProperty("InvocationSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus)
            .GetProperty("TotalRecordedEntryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus)
            .GetProperty("RetainedEntryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus)
            .GetProperty("MaximumRetainedEntryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus)
            .GetProperty("LatestAttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus)
            .GetProperty("OldestRetainedAttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus)
            .GetProperty("HasTruncatedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("PersistencePath", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("LastRecoveredAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("LastPersistedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("LastRecoveryError", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("LastPersistenceError", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("HasDurableStoreConfigured", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("HasPersistedSnapshot", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("HasPersistedRecordedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus)
            .GetProperty("HasRecoveredPersistedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus)
            .GetProperty("LatestAutomaticRetryState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus)
            .GetProperty("LatestAutomaticRetryAttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus)
            .GetProperty("LatestAutomaticRetryRecordedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus)
            .GetProperty("IsAutomaticRetryEnabled", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus)
            .GetProperty("LatestCommandExecutionInvocationSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus)
            .GetProperty("ActiveReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus)
            .GetProperty("CanExecuteOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus)
            .GetProperty("ActiveReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus)
            .GetProperty("HasDurableStoreConfigured", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus)
            .GetProperty("HasMatchingRetryFingerprintHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus)
            .GetProperty("HasDuplicateAutomaticRetryAttempts", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus)
            .GetProperty("CanExecuteAutomaticRetryOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus)
            .GetProperty("RetryFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus)
            .GetProperty("MatchingCommandLineageEntryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus)
            .GetProperty("HasDuplicateCommandLineage", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus)
            .GetProperty("CanExecuteAutomaticRetryOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus)
            .GetProperty("SchedulerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus)
            .GetProperty("PollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus)
            .GetProperty("CanScheduleAutomaticRetryOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("ActiveReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("AutomaticRetryCoordinationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("CommandJournalDurabilityState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("DistributedRetryOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("MultiNodeLeaseExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("SchedulerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("SchedulerKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("PollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("RetryFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("LatestAutomaticRetryAttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("HasDurableStoreConfigured", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("HasPersistedRecordedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("HasRecoveredPersistedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus)
            .GetProperty("CanScheduleAutomaticRetryOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStatus)
            .GetProperty("SchedulerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStatus)
            .GetProperty("PollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStatus)
            .GetProperty("CanExecuteAutomaticRetryOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("ExecutionOwnership", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("ExecutionTopology", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("ActiveReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("ActiveReporterLeaseExpiresAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("DurableSharedSchedulerOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("MultiNodeLeaseExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("DistributedRetryOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("CommandJournalDurabilityState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("LatestCommandExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("LatestCommandExecutionInvocationSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("SchedulerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("SchedulerKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("PollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("RetryFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("LatestAutomaticRetryAttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("LatestAutomaticRetryExecutionFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("LatestCommandExecutionFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("LatestAutomaticRetryRecordedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("HasDurableStoreConfigured", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("HasPersistedRecordedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("HasRecoveredPersistedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("HasMatchingAutomaticRetryAttempt", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("CanExecuteAutomaticRetryOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("HasActiveReporterLease", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("IsOperatorOnly", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("IsRecoveryReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("IsRecoveryBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("IsReplaying", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("IsExecutionHardened", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus)
            .GetProperty("IsExecutionRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("ExecutionOwnership", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("ExecutionTopology", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("ExecutionAdapterState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("LatestCommandExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("LatestCommandExecutionInvocationSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("RetryExecutionPolicyState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("AutomaticRetryExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("DistributedRetryLeaseState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("DurableSharedSchedulerOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("SchedulerRecoveryExecutionHardeningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("AdapterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("ProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("ConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("ConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("SourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("CommandFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("AdapterFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("LatestExecutionFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("RetryFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("PotentialChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("LatestAttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("LatestRecordedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("RequiresExplicitApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("IsDestructiveOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("CanExecuteProviderOwnedWritePathOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("IsOperatorOnly", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("IsProviderExecutable", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("IsProviderBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("IsProviderOwnedExecuting", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("IsProviderOwnedCompleted", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus)
            .GetProperty("IsProviderOwnedRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ExecutionOwnership", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ExecutionTopology", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ProviderOwnedWritePathExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ExecutionAdapterState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("LatestCommandExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("LatestCommandExecutionInvocationSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("RetryExecutionPolicyState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("CommandJournalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("DurableSharedSchedulerOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("SchedulerRecoveryExecutionHardeningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("AdapterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("SourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("CommandFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("AdapterFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("LatestExecutionFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("RetryFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("PotentialChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("LatestAttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("LatestRecordedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ActiveReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("ActiveReporterLeaseExpiresAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("SchedulerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("SchedulerKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("PollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("RequiresExplicitApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("IsDestructiveOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("HasCommandJournalEvidence", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("HasDurableStoreConfigured", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("HasPersistedRecordedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("HasRecoveredPersistedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("CanOrchestrateProviderExecutionOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("HasActiveReporterLease", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("IsOperatorOnly", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("IsOrchestrationReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("IsOrchestrationBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("IsOrchestrationExecuting", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("IsOrchestrationCompleted", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus)
            .GetProperty("IsOrchestrationRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ExecutionOwnership", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ExecutionTopology", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ManagementMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ProviderExecutionOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ProviderOwnedWritePathExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ExecutionAdapterState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("LatestCommandExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("LatestCommandExecutionInvocationSourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("RetryExecutionPolicyState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("CommandJournalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("DurableSharedSchedulerOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("SchedulerRecoveryExecutionHardeningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("AdapterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("SourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("CommandFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("AdapterFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("LatestExecutionFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("RetryFingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("PotentialChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("LatestAttemptId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("LatestRecordedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("CoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ActiveReporterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("ActiveReporterLeaseExpiresAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("SchedulerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("SchedulerKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("PollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("RequiresExplicitApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("IsDestructiveOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("HasCommandJournalEvidence", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("HasDurableStoreConfigured", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("HasPersistedRecordedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("HasRecoveredPersistedHistory", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("CanExerciseProviderOwnedControlPlaneOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("CategoryCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("HasActiveReporterLease", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("AppliesToManagedConnector", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("IsOperatorOnly", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("IsOwnershipReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("IsOwnershipBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("IsOwnershipActive", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("IsOwnershipPartial", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus)
            .GetProperty("IsOwnershipRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("ProviderOwnedControlPlaneOwnershipState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("CommandEnvelopeState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("CommandIssuanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("LatestCommandExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("CommandRetryState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("RetryExecutionPolicyState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("CommandJournalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("HasTargetOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("IsMutationOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("IsReconcileOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("CanMutateOrReconcileOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("IsMutationReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("IsReconcileReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("IsMutationBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("IsReconcileBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("IsMutationExecuting", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStatus)
            .GetProperty("IsMutationRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("ProviderOwnedControlPlaneMutationReconcileState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("ProviderOwnedControlPlaneOwnershipState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("ProviderExecutionOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("ProviderOwnedWritePathExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("CommandEnvelopeState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("CommandIssuanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("LatestCommandExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("CommandRetryState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("RetryExecutionPolicyState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("CommandJournalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("RequiresExplicitApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("HasTargetOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("CanProvisionProviderOwnedControlPlaneOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("IsProvisioningReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("IsProvisioningBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("IsProvisioningExecuting", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("IsProvisioningPartial", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus)
            .GetProperty("IsProvisioningRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates)
            .GetField("ProvisioningReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates)
            .GetField("ProvisioningBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates)
            .GetField("ProvisioningExecuting", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates)
            .GetField("ProvisioningPartial", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates)
            .GetField("ProvisioningRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("ProviderOwnedControlPlaneProvisioningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("ProviderOwnedControlPlaneMutationReconcileState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("ProviderOwnedControlPlaneOwnershipState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("ProviderExecutionOrchestrationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("ProviderOwnedWritePathExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("CommandEnvelopeState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("CommandIssuanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("LatestCommandExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("CommandRetryState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("RetryExecutionPolicyState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("CommandJournalState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("WouldApplyChanges", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("RequiresExplicitApproval", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("HasTargetOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("CanExecuteProviderOwnedControlPlaneApplyAndReconcileOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("IsApplyAndReconcileReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("IsApplyAndReconcileBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("IsApplyAndReconcileExecuting", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("IsApplyAndReconcileCompleted", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStatus)
            .GetProperty("IsApplyAndReconcileRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates)
            .GetField("ApplyAndReconcileReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates)
            .GetField("ApplyAndReconcileBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates)
            .GetField("ApplyAndReconcileExecuting", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates)
            .GetField("ApplyAndReconcileCompleted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates)
            .GetField("ApplyAndReconcileRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("ProviderOwnedControlPlaneApplyAndReconcileExecutionState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("GovernanceState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("DriftState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("HasDeclaredDependencyIdentity", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("HasReportedTaskTopology", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("CanExecuteDependencyAwareApplyAndReconcileOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("IsDependencyReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("IsDependencyBlocked", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("IsDependencyDegraded", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("IsApplyAndReconcileHardened", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus)
            .GetProperty("IsDependencyRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates)
            .GetField("DependencyReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates)
            .GetField("DependencyBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates)
            .GetField("DependencyDegraded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates)
            .GetField("ApplyAndReconcileHardened", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates)
            .GetField("DependencyRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("ProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("ProviderOwnedControlPlaneProvisioningState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("ProviderOwnedControlPlaneMutationReconcileState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("CanExecuteDependencyAwareProvisioningAndMutationOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("CanExecuteDependencyAwareProvisioningOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("CanExecuteDependencyAwareMutationOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("IsMutationOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("IsProvisioningHardened", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStatus)
            .GetProperty("IsMutationHardened", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("DependencyReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("ProvisioningBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("MutationBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("DependencyDegraded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("ProvisioningHardened", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("MutationHardened", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates)
            .GetField("DependencyRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("ProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("MaterializerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("TransportKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("ProviderSurfaceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("HasSelectedMaterializer", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("CanUseProviderSpecificControlPlaneMaterializerOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("IsMaterializerReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("IsMaterializerSelected", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("IsMaterializerExecuting", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus)
            .GetProperty("IsMaterializerRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates)
            .GetField("MaterializerUnavailable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates)
            .GetField("MaterializerReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates)
            .GetField("MaterializerSelected", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates)
            .GetField("MaterializerExecuting", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates)
            .GetField("MaterializerRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("CategoryIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("ExecutionRuntimeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("OperationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("SourceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("ProviderSpecificControlPlaneMaterializerState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("ProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("MaterializerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("TransportKind", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("ProviderSurfaceId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("CanExecuteDependencyAwareTeardownAndMutationExecutionOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("CanExecuteDependencyAwareTeardownOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("CanExecuteDependencyAwareMutationExecutionOnCurrentNode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("IsTeardownOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("IsMutationExecutionOperation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("IsDependencyReady", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("IsTeardownHardened", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("IsMutationExecutionHardened", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus)
            .GetProperty("IsDependencyRisk", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("NotApplicable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("OperatorOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("DependencyReady", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("TeardownBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("MutationExecutionBlocked", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("DependencyDegraded", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("TeardownHardened", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("MutationExecutionHardened", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates)
            .GetField("DependencyRisk", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("OutboxDispatchState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("HasPendingPublications", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeState)
            .GetProperty("TotalCapturedChangeCount", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("CdcCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("CdcCaptureStates", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("CdcCaptureExecutionRuntimes", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void DataRuntimeOptionsExposeConfiguredCdcExecutionRuntimeDeclarations()
    {
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.DataRuntimeOptions)
            .GetProperty("CdcExecutionRuntimes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.DataRuntimeOptions)
            .GetProperty("EnableExternalCdcRuntimeReporting", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.DataRuntimeOptions)
            .GetProperty("EnableManagedConnectorAutomaticRetryExecution", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.DataRuntimeOptions)
            .GetProperty("ManagedConnectorAutomaticRetryPollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.DataRuntimeOptions)
            .GetProperty("ManagedConnectorAutomaticRetryCoordinationOwnerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.DataRuntimeOptions)
            .GetProperty("ManagedConnectorCommandJournalPersistencePath", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.CdcCaptureExecutionRuntimeOptions)
            .GetProperty("ExecutionOwnership", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.CdcCaptureExecutionRuntimeOptions)
            .GetProperty("ExecutionTopology", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.CdcCaptureExecutionRuntimeOptions)
            .GetProperty("CdcCaptureIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.CdcCaptureExecutionRuntimeOptions)
            .GetProperty("Metadata", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.CdcCaptureExecutionRuntimeOptions)
            .GetProperty("ObservationStaleAfterSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Configuration.CdcCaptureExecutionRuntimeOptions)
            .GetProperty("RejectOutOfOrderReports", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void CdcExecutionRuntimeReportContractsExposeObservationSink()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeObservation)
            .GetProperty("ObservedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeObservation)
            .GetProperty("ReportId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.CdcCaptureRuntimeObservation)
            .GetProperty("Metadata", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Data.ICdcCaptureExecutionRuntimeReportSink)
            .GetMethod("ReportAsync", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void MongoDbDataOptionsExposeProviderNativeChangeStreamCaptureDeclarations()
    {
        Assert.NotNull(typeof(global::Cephalon.Data.MongoDB.Configuration.MongoDbDataOptions)
            .GetProperty("ChangeStreamCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MongoDB.Configuration.MongoDbChangeStreamCaptureOptions)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MongoDB.Configuration.MongoDbChangeStreamCaptureOptions)
            .GetProperty("CollectionName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MongoDB.Configuration.MongoDbChangeStreamCaptureOptions)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MongoDB.Configuration.MongoDbChangeStreamCaptureOptions)
            .GetProperty("FullDocumentMode", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void SqlServerDataOptionsExposeProviderNativeCdcCaptureDeclarations()
    {
        Assert.NotNull(typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerDataOptions)
            .GetProperty("CdcCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerDataOptions)
            .GetProperty("DatabaseName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerCdcCaptureOptions)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerCdcCaptureOptions)
            .GetProperty("CaptureInstance", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerCdcCaptureOptions)
            .GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerCdcCaptureOptions)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.SqlServer.Configuration.SqlServerCdcCaptureOptions)
            .GetProperty("InitialPosition", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void MySqlDataOptionsExposeProviderNativeBinlogCaptureDeclarations()
    {
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlDataOptions)
            .GetProperty("CdcCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlDataOptions)
            .GetProperty("DatabaseName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlDataOptions)
            .GetProperty("CheckpointTableName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlBinlogCaptureOptions)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlBinlogCaptureOptions)
            .GetProperty("TableSchema", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlBinlogCaptureOptions)
            .GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlBinlogCaptureOptions)
            .GetProperty("ServerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlBinlogCaptureOptions)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.MySql.Configuration.MySqlBinlogCaptureOptions)
            .GetProperty("InitialPosition", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void OracleDataOptionsExposeProviderNativeLogMinerCaptureDeclarations()
    {
        Assert.NotNull(typeof(global::Cephalon.Data.Oracle.Configuration.OracleDataOptions)
            .GetProperty("CdcCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Oracle.Configuration.OracleDataOptions)
            .GetProperty("DatabaseName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Oracle.Configuration.OracleDataOptions)
            .GetProperty("CheckpointTableName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Oracle.Configuration.OracleLogMinerCaptureOptions)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Oracle.Configuration.OracleLogMinerCaptureOptions)
            .GetProperty("TableSchema", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Oracle.Configuration.OracleLogMinerCaptureOptions)
            .GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Oracle.Configuration.OracleLogMinerCaptureOptions)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Oracle.Configuration.OracleLogMinerCaptureOptions)
            .GetProperty("InitialPosition", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void PostgresDataOptionsExposeProviderNativeLogicalReplicationCaptureDeclarations()
    {
        Assert.NotNull(typeof(global::Cephalon.Data.Postgres.Configuration.PostgresDataOptions)
            .GetProperty("CdcCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Postgres.Configuration.PostgresDataOptions)
            .GetProperty("ConnectionString", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Postgres.Configuration.PostgresLogicalReplicationCaptureOptions)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Postgres.Configuration.PostgresLogicalReplicationCaptureOptions)
            .GetProperty("PublicationName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Postgres.Configuration.PostgresLogicalReplicationCaptureOptions)
            .GetProperty("SlotName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Postgres.Configuration.PostgresLogicalReplicationCaptureOptions)
            .GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Postgres.Configuration.PostgresLogicalReplicationCaptureOptions)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Postgres.Configuration.PostgresLogicalReplicationCaptureOptions)
            .GetProperty("InitialPosition", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void DebeziumDataOptionsExposeManagedConnectorCaptureDeclarations()
    {
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumDataOptions)
            .GetProperty("Connectors", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumConnectorOptions)
            .GetProperty("ConnectClusterId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumConnectorOptions)
            .GetProperty("ConnectorClass", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumConnectorOptions)
            .GetProperty("SourceProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumConnectorOptions)
            .GetProperty("CdcCaptures", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumCaptureOptions)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumCaptureOptions)
            .GetProperty("OutboxId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumCaptureOptions)
            .GetProperty("TopicName", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Data.Debezium.Configuration.DebeziumCaptureOptions)
            .GetProperty("SnapshotMode", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void CellBoundaryRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellBoundaryCatalog)
            .GetProperty("CellBoundaries", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellBoundaryCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellBoundaryCatalog)
            .GetMethod("GetByModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellBoundaryDescriptor)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellBoundaryDescriptor)
            .GetProperty("BlastRadius", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellBoundaryDescriptor)
            .GetProperty("RoutingStrategy", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellBoundaryDescriptor)
            .GetProperty("ModuleIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("CellBoundaries", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void CellRouteRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellRouteCatalog)
            .GetProperty("Routes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellRouteCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellRouteCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellRouteCatalog)
            .GetMethod("GetBySourceCellId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellRouteCatalog)
            .GetMethod("GetByTargetCellId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellRouteDescriptor)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellRouteDescriptor)
            .GetProperty("SourceCellId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellRouteDescriptor)
            .GetProperty("TargetCellId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellRouteDescriptor)
            .GetProperty("RoutingStrategy", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellRouteDescriptor)
            .GetProperty("GovernanceMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellRouteDescriptor)
            .GetProperty("TransportIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellRouteDescriptor)
            .GetProperty("RequiredCapabilityKey", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("CellRoutes", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void CellHealthIsolationRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Engine.Composition.EngineBuilder)
            .GetMethod("AddCellHealthIsolation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Composition.EngineBuilder)
            .GetMethod("AddCellHealthIsolations", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellHealthIsolationCatalog)
            .GetProperty("HealthIsolations", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellHealthIsolationCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellHealthIsolationCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellHealthIsolationCatalog)
            .GetMethod("GetByCellId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellHealthIsolationCatalog)
            .GetMethod("GetByDependencyId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellHealthIsolationDescriptor)
            .GetProperty("SourceModuleId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellHealthIsolationDescriptor)
            .GetProperty("CellId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellHealthIsolationDescriptor)
            .GetProperty("FailureIsolationMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellHealthIsolationDescriptor)
            .GetProperty("ReadinessScope", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellHealthIsolationDescriptor)
            .GetProperty("RestartScope", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellHealthIsolationDescriptor)
            .GetProperty("DependencyIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("CellHealthIsolations", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void CellTrafficAutomationRuntimeContractsExposeSnapshotSurface()
    {
        Assert.NotNull(typeof(global::Cephalon.Engine.Composition.EngineBuilder)
            .GetMethod("UseCellSettings", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Configuration.CellSettings)
            .GetProperty("TrafficAutomation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Configuration.CellTrafficAutomationSettings)
            .GetProperty("DefaultProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Configuration.CellTrafficAutomationSettings)
            .GetProperty("DefaultEdgeNodeIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Configuration.CellTrafficAutomationSettings)
            .GetProperty("Routes", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Configuration.CellTrafficAutomationRouteSettings)
            .GetProperty("RouteId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Configuration.CellTrafficAutomationRouteSettings)
            .GetProperty("ProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Configuration.CellTrafficAutomationRouteSettings)
            .GetProperty("EdgeNodeIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetProperty("Automations", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetMethod("GetByRouteId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetMethod("GetBySourceModule", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetMethod("GetBySourceCellId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetMethod("GetByTargetCellId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetMethod("GetByProvider", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetMethod("GetByEdgeNodeId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationRuntimeCatalog)
            .GetMethod("GetByHealthIsolationId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationMaterializationReportSink)
            .GetMethod("ReportProviderAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationMaterializationReportSink)
            .GetMethod("ReportEdgeAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationProviderMaterializer)
            .GetProperty("MaterializerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationProviderMaterializer)
            .GetProperty("ProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationProviderMaterializer)
            .GetProperty("Priority", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationProviderMaterializer)
            .GetMethod("CanMaterialize", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationProviderMaterializer)
            .GetMethod("MaterializeAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationEdgeMaterializer)
            .GetProperty("MaterializerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationEdgeMaterializer)
            .GetProperty("Priority", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationEdgeMaterializer)
            .GetMethod("CanMaterialize", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.ICellTrafficAutomationEdgeMaterializer)
            .GetMethod("MaterializeAsync", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationResult)
            .GetProperty("State", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationResult)
            .GetProperty("ObservedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationResult)
            .GetProperty("Error", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationResult)
            .GetProperty("Metadata", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationResult)
            .GetProperty("Conditions", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor)
            .GetProperty("Dimension", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor)
            .GetProperty("Category", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor)
            .GetProperty("ConditionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor)
            .GetProperty("State", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor)
            .GetProperty("Severity", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor)
            .GetProperty("Reason", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor)
            .GetProperty("Description", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDescriptor)
            .GetProperty("Metadata", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDimensions)
            .GetField("Provider", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionDimensions)
            .GetField("Edge", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionCategories)
            .GetField("Readiness", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionCategories)
            .GetField("Dependency", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionCategories)
            .GetField("Ownership", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionCategories)
            .GetField("Drift", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionCategories)
            .GetField("Lifecycle", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionCategories)
            .GetField("Observation", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionStates)
            .GetField("Met", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionStates)
            .GetField("Unmet", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionStates)
            .GetField("Pending", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionStates)
            .GetField("Unknown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionSeverities)
            .GetField("Info", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionSeverities)
            .GetField("Warning", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationConditionSeverities)
            .GetField("Error", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationStates)
            .GetField("Unavailable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationStates)
            .GetField("Pending", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationStates)
            .GetField("Applied", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationStates)
            .GetField("Partial", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationMaterializationStates)
            .GetField("Failed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates)
            .GetField("Requested", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates)
            .GetField("Owned", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates)
            .GetField("OwnershipConflict", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates)
            .GetField("Orphaned", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates)
            .GetField("Pruned", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates)
            .GetField("Transferred", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates)
            .GetField("Unknown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationOwnershipStates)
            .GetField("Mixed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDependencyStates)
            .GetField("Satisfied", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDependencyStates)
            .GetField("Missing", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDependencyStates)
            .GetField("Unknown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDependencyStates)
            .GetField("Mixed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDriftStates)
            .GetField("InSync", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDriftStates)
            .GetField("Reconciling", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDriftStates)
            .GetField("Drifted", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDriftStates)
            .GetField("Unknown", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationDriftStates)
            .GetField("Mixed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions)
            .GetField("Project", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions)
            .GetField("Observe", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions)
            .GetField("Reconcile", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions)
            .GetField("Create", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions)
            .GetField("Replace", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions)
            .GetField("Delete", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions)
            .GetField("Prune", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationLifecycleActions)
            .GetField("Transfer", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationResult)
            .GetProperty("State", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationResult)
            .GetProperty("ObservedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationResult)
            .GetProperty("Error", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationResult)
            .GetProperty("Metadata", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationResult)
            .GetProperty("Conditions", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationStates)
            .GetField("Unavailable", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationStates)
            .GetField("Pending", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationStates)
            .GetField("Applied", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationProviderMaterializationStates)
            .GetField("Failed", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("RouteId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("AutomationMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("TriggerMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("ActionMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("MaterializationMode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("ProviderId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("EdgeNodeIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("MaterializationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("MaterializationObservedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("MaterializationError", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("EdgeMaterializerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("EdgeMaterializationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("EdgeMaterializationObservedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("EdgeMaterializationError", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("ProviderMaterializerId", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("ProviderMaterializationState", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("ProviderMaterializationObservedAtUtc", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("ProviderMaterializationError", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("PolicySource", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("SourceHealthIsolationIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("TargetHealthIsolationIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("DependencyIds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Technologies.CellTrafficAutomationRuntimeDescriptor)
            .GetProperty("MaterializationConditions", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot)
            .GetProperty("CellTrafficAutomations", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficMaterializerOptions)
            .GetProperty("Observation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationOptions)
            .GetProperty("Mode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationOptions)
            .GetProperty("UseInClusterConfiguration", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationOptions)
            .GetProperty("KubeConfigPath", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationOptions)
            .GetProperty("KubeContext", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationOptions)
            .GetProperty("MasterUrl", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationOptions)
            .GetProperty("PollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationOptions)
            .GetProperty("StaleAfterSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationModes)
            .GetField("ConfiguredIntent", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationModes)
            .GetField("ObserveOnly", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.KubernetesGateway.Configuration.KubernetesGatewayTrafficObservationModes)
            .GetField("ApplyAndReconcile", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficMaterializerOptions)
            .GetProperty("Observation", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationOptions)
            .GetProperty("Mode", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationOptions)
            .GetProperty("UseInClusterConfiguration", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationOptions)
            .GetProperty("KubeConfigPath", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationOptions)
            .GetProperty("KubeContext", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationOptions)
            .GetProperty("MasterUrl", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationOptions)
            .GetProperty("PollingIntervalSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationOptions)
            .GetProperty("StaleAfterSeconds", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationModes)
            .GetField("ConfiguredIntent", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(typeof(global::Cephalon.Edge.Traefik.Configuration.TraefikTrafficObservationModes)
            .GetField("ObserveOnly", BindingFlags.Static | BindingFlags.Public));
    }

    [Fact]
    public void BehaviorExecutionResilienceSelectionContractsExposeRateLimiting()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.Resilience.BehaviorExecutionResilienceSelection)
            .GetProperty("RateLimiting", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void BehaviorExecutionResilienceOverrideSelectionContractsExposeRateLimiting()
    {
        Assert.NotNull(typeof(global::Cephalon.Abstractions.AppModel.BehaviorExecutionResilienceOverrideSelection)
            .GetProperty("RateLimiting", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void BehaviorsPatternsAssemblyExposesOnlyTheDocumentedContractSurface()
    {
        AssertExportedTypes(
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.IBehaviorExecutionStrategy).Assembly,
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.BehaviorExecutionContext),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.BehaviorExecutionResult),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.DurableExecutionState<>),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.DurableExecutionStepResult<>),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.IBehaviorExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.IDurableExecution<>),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.IDurableExecution<,,>),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ISagaChoreographyStepResult),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ISagaChoreographyPublisher),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ISagaEventReactor<>),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ISagaEventReactor<,>),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.IProcessCheckpointStore),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ISagaStateStore),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.ProcessCheckpoint),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyPublication),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyStepResult),
            typeof(global::Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyStepResult<>),
            typeof(global::Cephalon.Behaviors.Patterns.Hosting.PatternBehaviorExtensions),
            typeof(global::Cephalon.Behaviors.Patterns.Publishers.InMemorySagaChoreographyPublisher),
            typeof(global::Cephalon.Behaviors.Patterns.Registry.ExecutionStrategyRegistry),
            typeof(global::Cephalon.Behaviors.Patterns.Stores.InMemoryProcessCheckpointStore),
            typeof(global::Cephalon.Behaviors.Patterns.Stores.InMemorySagaStateStore),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.ChoreographySagaExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.CqrsExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.DirectExecutionStrategy),
            typeof(global::Cephalon.Behaviors.Patterns.Strategies.DurableExecutionStrategy),
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
