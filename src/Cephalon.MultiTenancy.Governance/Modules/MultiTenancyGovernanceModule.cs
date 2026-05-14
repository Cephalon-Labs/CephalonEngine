using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Configuration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using System.Net.Http;

namespace Cephalon.MultiTenancy.Governance.Modules;

internal sealed class MultiTenancyGovernanceModule(MultiTenancyGovernanceOptions options)
    : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "multi-tenancy-governance",
        displayName: "Multi-Tenancy Governance",
        description: "Tenant membership, invitation, declared domain-ownership, and approval/remediation action governance runtime for Cephalon multi-tenancy workloads.",
        tags: ["tenant", "membership", "invitation", "domain-ownership", "governance", "approval", "remediation"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["technology"] = "multi-tenancy",
            ["basePackage"] = "Cephalon.MultiTenancy"
        });

    private bool hasMembershipContributors;
    private bool hasInvitationContributors;
    private bool hasInvitationDeliverySenders;
    private bool hasDomainOwnershipContributors;
    private bool hasGovernanceActionContributors;

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MultiTenancyGovernanceDiagnosticsConventionContributor>());
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("multi-tenancy"))
        {
            return;
        }

        hasMembershipContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(ITenantMembershipContributor));
        hasInvitationContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(ITenantInvitationContributor));
        hasInvitationDeliverySenders = services.Any(static descriptor => descriptor.ServiceType == typeof(ITenantInvitationDeliverySender));
        hasDomainOwnershipContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(ITenantDomainOwnershipContributor));
        hasGovernanceActionContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(ITenantGovernanceActionContributor));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ILogger<TenantMembershipEvaluator>>(NullLogger<TenantMembershipEvaluator>.Instance);
        services.TryAddSingleton<ILogger<TenantInvitationValidator>>(NullLogger<TenantInvitationValidator>.Instance);
        services.TryAddSingleton<ILogger<TenantInvitationDeliveryDispatcher>>(NullLogger<TenantInvitationDeliveryDispatcher>.Instance);
        services.TryAddSingleton<ILogger<TenantInvitationDeliveryRetryRunner>>(NullLogger<TenantInvitationDeliveryRetryRunner>.Instance);
        services.TryAddSingleton<ILogger<TenantInvitationDeliveryRetryHostedService>>(NullLogger<TenantInvitationDeliveryRetryHostedService>.Instance);
        services.TryAddSingleton<ILogger<TenantInvitationDeliveryStatusReconciler>>(NullLogger<TenantInvitationDeliveryStatusReconciler>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipValidator>>(NullLogger<TenantDomainOwnershipValidator>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipVerificationWorkflow>>(NullLogger<TenantDomainOwnershipVerificationWorkflow>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofEvaluator>>(NullLogger<TenantDomainOwnershipProofEvaluator>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofChallengeIssuer>>(NullLogger<TenantDomainOwnershipProofChallengeIssuer>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofPublicationPlanner>>(NullLogger<TenantDomainOwnershipProofPublicationPlanner>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipHttpProofPublisher>>(NullLogger<TenantDomainOwnershipHttpProofPublisher>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipHttpProofCollector>>(NullLogger<TenantDomainOwnershipHttpProofCollector>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipDnsTxtProofCollector>>(NullLogger<TenantDomainOwnershipDnsTxtProofCollector>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofVerificationRunner>>(NullLogger<TenantDomainOwnershipProofVerificationRunner>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofPollingRunner>>(NullLogger<TenantDomainOwnershipProofPollingRunner>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofPollingHostedService>>(NullLogger<TenantDomainOwnershipProofPollingHostedService>.Instance);
        services.TryAddSingleton<ILogger<TenantGovernanceActionDecider>>(NullLogger<TenantGovernanceActionDecider>.Instance);
        services.TryAddSingleton<ILogger<TenantGovernanceActionWorkflow>>(NullLogger<TenantGovernanceActionWorkflow>.Instance);
        services.TryAddSingleton<ILogger<TenantAdministrationWorkflow>>(NullLogger<TenantAdministrationWorkflow>.Instance);
        services.TryAddSingleton(static _ => new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        })
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
        services.TryAddSingleton<ITenantMembershipStore>(
            static serviceProvider => TenantMembershipStores.Create(serviceProvider.GetRequiredService<MultiTenancyGovernanceOptions>()));
        services.TryAddSingleton<ITenantInvitationStore>(
            static serviceProvider => TenantInvitationStores.Create(serviceProvider.GetRequiredService<MultiTenancyGovernanceOptions>()));
        services.TryAddSingleton<ITenantInvitationDeliveryStatusObservationStore>(
            static serviceProvider => TenantInvitationDeliveryStatusObservationStores.Create(serviceProvider.GetRequiredService<MultiTenancyGovernanceOptions>()));
        services.TryAddSingleton<ITenantInvitationDeliveryRetryStore>(
            static serviceProvider => TenantInvitationDeliveryRetryQueueStores.Create(serviceProvider.GetRequiredService<MultiTenancyGovernanceOptions>()));
        services.TryAddSingleton<ITenantDomainOwnershipStore>(
            static serviceProvider => TenantDomainOwnershipStores.Create(serviceProvider.GetRequiredService<MultiTenancyGovernanceOptions>()));
        services.TryAddSingleton<ITenantGovernanceActionStore>(
            static serviceProvider => TenantGovernanceActionStores.Create(serviceProvider.GetRequiredService<MultiTenancyGovernanceOptions>()));
        services.TryAddSingleton<TenantDomainOwnershipProofPollingRuntimeReporter>();
        services.TryAddSingleton<ITenantDomainOwnershipProofPollingRuntimeCatalog>(
            static serviceProvider => serviceProvider.GetRequiredService<TenantDomainOwnershipProofPollingRuntimeReporter>());
        services.TryAddSingleton<TenantInvitationDeliveryRetryRuntimeReporter>();
        services.TryAddSingleton<ITenantInvitationDeliveryRetryRuntimeCatalog>(
            static serviceProvider => serviceProvider.GetRequiredService<TenantInvitationDeliveryRetryRuntimeReporter>());
        services.TryAddSingleton<TenantInvitationDeliveryRetryExecutionCoordinator>();
        services.TryAddSingleton<ITenantInvitationDeliveryRetryExecutionCoordinationCatalog>(
            static serviceProvider => serviceProvider.GetRequiredService<TenantInvitationDeliveryRetryExecutionCoordinator>());
        services.TryAddSingleton<TenantInvitationDeliveryRunReporter>();
        services.TryAddSingleton<ITenantInvitationDeliveryRunCatalog>(
            static serviceProvider => serviceProvider.GetRequiredService<TenantInvitationDeliveryRunReporter>());
        services.TryAddSingleton<ITenantMembershipCatalog, TenantMembershipCatalog>();
        services.TryAddSingleton<ITenantInvitationCatalog, TenantInvitationCatalog>();
        services.TryAddSingleton<ITenantDomainOwnershipCatalog, TenantDomainOwnershipCatalog>();
        services.TryAddSingleton<ITenantDomainOwnershipHttpProofPublicationCatalog, TenantDomainOwnershipHttpProofPublicationCatalog>();
        services.TryAddSingleton<ITenantGovernanceActionCatalog, TenantGovernanceActionCatalog>();

        if (options.EnableMembershipEvaluation)
        {
            services.TryAddSingleton<ITenantMembershipEvaluator, TenantMembershipEvaluator>();
        }

        if (options.EnableInvitationValidation)
        {
            services.TryAddSingleton<ITenantInvitationValidator, TenantInvitationValidator>();
        }

        if (options.EnableInvitationDeliveryDispatch)
        {
            services.TryAddSingleton<ITenantInvitationDeliveryDispatcher, TenantInvitationDeliveryDispatcher>();
        }

        if (TenantInvitationDeliveryRetryConfiguration.IsRetryRunnerEnabled(options))
        {
            services.TryAddSingleton<ITenantInvitationDeliveryRetryRunner, TenantInvitationDeliveryRetryRunner>();
        }

        if (TenantInvitationDeliveryRetryConfiguration.IsBackgroundSchedulingEnabled(options))
        {
            services.TryAddSingleton<TenantInvitationDeliveryRetryHostedService>();
            services.AddHostedService(static serviceProvider =>
                serviceProvider.GetRequiredService<TenantInvitationDeliveryRetryHostedService>());
        }

        if (options.EnableInvitationDeliveryStatusReconciliation)
        {
            services.TryAddSingleton<ITenantInvitationDeliveryStatusReconciler, TenantInvitationDeliveryStatusReconciler>();
        }

        if (options.EnableDomainOwnershipValidation)
        {
            services.TryAddSingleton<ITenantDomainOwnershipValidator, TenantDomainOwnershipValidator>();
        }

        if (options.EnableDomainOwnershipVerificationWorkflow)
        {
            services.TryAddSingleton<ITenantDomainOwnershipVerificationWorkflow, TenantDomainOwnershipVerificationWorkflow>();
        }

        if (options.EnableDomainOwnershipProofEvaluation && options.EnableDomainOwnershipVerificationWorkflow)
        {
            services.TryAddSingleton<ITenantDomainOwnershipProofEvaluator, TenantDomainOwnershipProofEvaluator>();
        }

        if (options.EnableDomainOwnershipProofChallengeIssuance)
        {
            services.TryAddSingleton<ITenantDomainOwnershipProofChallengeIssuer, TenantDomainOwnershipProofChallengeIssuer>();
        }

        if (options.EnableDomainOwnershipProofPublicationPlanning)
        {
            services.TryAddSingleton<ITenantDomainOwnershipProofPublicationPlanner, TenantDomainOwnershipProofPublicationPlanner>();
        }

        if (options.EnableDomainOwnershipHttpProofPublication &&
            options.EnableDomainOwnershipProofPublicationPlanning)
        {
            services.TryAddSingleton<ITenantDomainOwnershipHttpProofPublisher, TenantDomainOwnershipHttpProofPublisher>();
        }

        if (options.EnableDomainOwnershipHttpProofCollection &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow)
        {
            services.TryAddSingleton<ITenantDomainOwnershipHttpProofCollector, TenantDomainOwnershipHttpProofCollector>();
        }

        if (options.EnableDomainOwnershipDnsTxtProofCollection &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow)
        {
            services.TryAddSingleton<ITenantDomainOwnershipDnsTxtProofCollector, TenantDomainOwnershipDnsTxtProofCollector>();
        }

        if (options.EnableDomainOwnershipProofVerificationRunner &&
            options.EnableDomainOwnershipProofChallengeIssuance &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow)
        {
            services.TryAddSingleton<ITenantDomainOwnershipProofVerificationRunner, TenantDomainOwnershipProofVerificationRunner>();
        }

        if (options.EnableDomainOwnershipProofPollingRunner &&
            options.EnableDomainOwnershipProofVerificationRunner &&
            options.EnableDomainOwnershipProofChallengeIssuance &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow)
        {
            services.TryAddSingleton<ITenantDomainOwnershipProofPollingRunner, TenantDomainOwnershipProofPollingRunner>();
        }

        if (TenantDomainOwnershipProofPollingConfiguration.IsBackgroundPollingEnabled(options))
        {
            services.TryAddSingleton<TenantDomainOwnershipProofPollingHostedService>();
            services.AddHostedService(static serviceProvider =>
                serviceProvider.GetRequiredService<TenantDomainOwnershipProofPollingHostedService>());
        }

        if (options.EnableGovernanceActionDecision)
        {
            services.TryAddSingleton<ITenantGovernanceActionDecider, TenantGovernanceActionDecider>();
        }

        if (options.EnableGovernanceActionWorkflow)
        {
            services.TryAddSingleton<ITenantGovernanceActionWorkflow, TenantGovernanceActionWorkflow>();
        }

        if (options.EnableTenantAdministrationWorkflow)
        {
            services.TryAddSingleton<ITenantAdministrationWorkflow, TenantAdministrationWorkflow>();
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceInvitationRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceDomainRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceActionRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceAdministrationRuntimeSurfaceContributor>());
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("multi-tenancy"))
        {
            return;
        }

        var httpProofCollectionEnabled = options.EnableDomainOwnershipHttpProofCollection &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow;
        var httpProofPublicationEnabled = options.EnableDomainOwnershipHttpProofPublication &&
            options.EnableDomainOwnershipProofPublicationPlanning;
        var dnsTxtProofCollectionEnabled = options.EnableDomainOwnershipDnsTxtProofCollection &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow;
        var dnsTxtProofCollectionConfigured = dnsTxtProofCollectionEnabled &&
            options.DomainOwnershipDnsTxtProofResolverEndpoint is not null;
        var proofVerificationRunnerEnabled = TenantDomainOwnershipProofPollingConfiguration.IsProofVerificationRunnerEnabled(options);
        var proofPollingRunnerEnabled = TenantDomainOwnershipProofPollingConfiguration.IsProofPollingRunnerEnabled(options);
        var backgroundProofPollingEnabled = TenantDomainOwnershipProofPollingConfiguration.IsBackgroundPollingEnabled(options);
        var backgroundProofPollingIntervalSeconds = TenantDomainOwnershipProofPollingConfiguration.ResolveBackgroundPollingIntervalSeconds(options);
        var backgroundProofPollingBatchLimit = TenantDomainOwnershipProofPollingConfiguration.ResolveBatchLimit(options);
        var dnsTxtProofCollectionOwnership = dnsTxtProofCollectionConfigured ? "cephalon-managed" : "not-configured";
        var dnsHttpProofCollectionOwnership = httpProofCollectionEnabled && dnsTxtProofCollectionConfigured
            ? "cephalon-managed"
            : httpProofCollectionEnabled || dnsTxtProofCollectionConfigured
                ? "mixed"
                : "application-managed";
        var proofPollingRunnerOwnership = proofPollingRunnerEnabled ? "cephalon-managed" : "not-configured";
        var externalProofPollingOwnership = proofPollingRunnerEnabled ? "cephalon-managed" : "application-managed";
        var backgroundProofPollingOwnership = TenantDomainOwnershipProofPollingConfiguration.ResolveBackgroundPollingOwnership(options);
        var httpProofPublicationOwnership = httpProofPublicationEnabled ? "cephalon-managed" : "not-configured";
        var proofPublicationOwnership = httpProofPublicationEnabled ? "mixed" : "application-managed";
        var administrationWorkflowOwnership = options.EnableTenantAdministrationWorkflow ? "cephalon-managed" : "not-configured";
        var invitationDeliveryDispatchOwnership = options.EnableInvitationDeliveryDispatch ? "cephalon-managed" : "not-configured";
        var invitationDeliveryStatusReconciliationOwnership = options.EnableInvitationDeliveryStatusReconciliation ? "cephalon-managed" : "not-configured";
        var invitationDeliveryStatusObservationStoreOwnership = options.EnableInvitationDeliveryStatusObservationStore ? "cephalon-managed" : "not-configured";
        var invitationDeliveryStatusObservationStoreDurable = !string.IsNullOrWhiteSpace(options.InvitationDeliveryStatusObservationStoreFilePath);
        var invitationDeliveryStatusObservationStoreKind = invitationDeliveryStatusObservationStoreDurable ? "file" : "in-memory";
        var invitationDeliveryStatusObservationHistoryLimit =
            TenantInvitationDeliveryStatusObservationStores.ResolveHistoryLimit(options);
        var invitationDeliveryRetryQueueOwnership = options.EnableInvitationDeliveryRetryQueue ? "cephalon-managed" : "not-configured";
        var invitationDeliveryRetryQueueDurable = !string.IsNullOrWhiteSpace(options.InvitationDeliveryRetryQueueFilePath);
        var invitationDeliveryRetryQueueKind = invitationDeliveryRetryQueueDurable ? "file" : "in-memory";
        var invitationDeliveryRetryMaxAttempts = TenantInvitationDeliveryRetryQueueStores.ResolveMaxAttempts(options);
        var invitationDeliveryRetryDelaySeconds = TenantInvitationDeliveryRetryQueueStores.ResolveRetryDelaySeconds(options);
        var invitationDeliveryRetryMaxItems = TenantInvitationDeliveryRetryQueueStores.ResolveMaxItems(options);
        var invitationDeliveryRetryRunnerEnabled = TenantInvitationDeliveryRetryConfiguration.IsRetryRunnerEnabled(options);
        var invitationDeliveryRetryBackgroundEnabled = TenantInvitationDeliveryRetryConfiguration.IsBackgroundSchedulingEnabled(options);
        var invitationDeliveryRetryBackgroundOwnership = TenantInvitationDeliveryRetryConfiguration.ResolveBackgroundSchedulingOwnership(options);
        var invitationDeliveryRetryBackgroundIntervalSeconds =
            TenantInvitationDeliveryRetryConfiguration.ResolveBackgroundSchedulingIntervalSeconds(options);
        var invitationDeliveryRetryExecutionCoordinationEnabled =
            TenantInvitationDeliveryRetryConfiguration.IsExecutionCoordinationEnabled(options);
        var invitationDeliveryRetryExecutionCoordinationOwnership =
            TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationOwnership(options);
        var invitationDeliveryRetryExecutionCoordinationScope =
            TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationScope(options);
        var invitationDeliveryRetryExecutionCoordinationMode =
            TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationMode(options);
        var invitationDeliverySenderOwnership = hasInvitationDeliverySenders ? "provider-managed" : "not-configured";
        var invitationExternalDeliveryOwnership = hasInvitationDeliverySenders ? "provider-managed" : "application-managed";
        var invitationExternalDeliveryStatusOwnership = options.EnableInvitationDeliveryStatusReconciliation ? "provider-managed" : "application-managed";
        var invitationDeliveryOwnership = options.EnableInvitationDeliveryDispatch && hasInvitationDeliverySenders
            ? "mixed"
            : "application-managed";

        capabilities.Add(new Capability(
            key: "tenancy.membership.catalog",
            displayName: "Tenant Membership Catalog",
            description: "Exposes tenant membership descriptors through the multi-tenancy governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-memberships",
                ["configuredMembershipCount"] = options.Memberships.Count.ToString(CultureInfo.InvariantCulture),
                ["hasMembershipContributors"] = hasMembershipContributors.ToString().ToLowerInvariant()
            }));

        capabilities.Add(new Capability(
            key: "tenancy.membership.store",
            displayName: "Tenant Membership Store",
            description: "Stores runtime tenant memberships managed by the multi-tenancy governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-memberships",
                ["storeKind"] = string.IsNullOrWhiteSpace(options.MembershipStoreFilePath) ? "in-memory" : "file",
                ["storeDurable"] = (!string.IsNullOrWhiteSpace(options.MembershipStoreFilePath)).ToString().ToLowerInvariant(),
                ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.MembershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                ["administrationWorkflowOwnership"] = administrationWorkflowOwnership
            }));

        if (options.EnableMembershipEvaluation)
        {
            capabilities.Add(new Capability(
                key: "tenancy.membership.evaluation",
                displayName: "Tenant Membership Evaluation",
                description: "Evaluates whether a principal has active tenant membership and required tenant roles.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["runtimeSurface"] = "tenant-memberships"
                }));
        }

        capabilities.Add(new Capability(
            key: "tenancy.invitation.catalog",
            displayName: "Tenant Invitation Catalog",
            description: "Exposes tenant invitation descriptors through the multi-tenancy governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-invitations",
                ["configuredInvitationCount"] = options.Invitations.Count.ToString(CultureInfo.InvariantCulture),
                ["hasInvitationContributors"] = hasInvitationContributors.ToString().ToLowerInvariant(),
                ["deliveryDispatchOwnership"] = invitationDeliveryDispatchOwnership,
                ["deliveryStatusReconciliationOwnership"] = invitationDeliveryStatusReconciliationOwnership,
                ["deliveryStatusObservationStoreOwnership"] = invitationDeliveryStatusObservationStoreOwnership,
                ["deliveryRetryQueueOwnership"] = invitationDeliveryRetryQueueOwnership,
                ["deliveryRetryQueueStoreKind"] = invitationDeliveryRetryQueueKind,
                ["deliveryRetryQueueStoreDurable"] = invitationDeliveryRetryQueueDurable.ToString().ToLowerInvariant(),
                ["deliveryRetryMaxAttempts"] = invitationDeliveryRetryMaxAttempts.ToString(CultureInfo.InvariantCulture),
                ["deliveryRetryDelaySeconds"] = invitationDeliveryRetryDelaySeconds.ToString(CultureInfo.InvariantCulture),
                ["deliveryRetryBackgroundEnabled"] = invitationDeliveryRetryBackgroundEnabled.ToString().ToLowerInvariant(),
                ["deliveryRetryBackgroundOwnership"] = invitationDeliveryRetryBackgroundOwnership,
                ["deliveryRetryBackgroundIntervalSeconds"] = invitationDeliveryRetryBackgroundIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                ["deliveryRetryBackgroundRunOnStartup"] = options.InvitationDeliveryRetryBackgroundRunOnStartup.ToString().ToLowerInvariant(),
                ["deliveryRetryExecutionCoordinationEnabled"] = invitationDeliveryRetryExecutionCoordinationEnabled.ToString().ToLowerInvariant(),
                ["deliveryRetryExecutionCoordinationOwnership"] = invitationDeliveryRetryExecutionCoordinationOwnership,
                ["deliveryRetryExecutionCoordinationScope"] = invitationDeliveryRetryExecutionCoordinationScope,
                ["deliveryRetryExecutionCoordinationMode"] = invitationDeliveryRetryExecutionCoordinationMode,
                ["deliverySenderOwnership"] = invitationDeliverySenderOwnership
            }));

        capabilities.Add(new Capability(
            key: "tenancy.invitation.store",
            displayName: "Tenant Invitation Store",
            description: "Stores runtime tenant invitations managed by the multi-tenancy governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-invitations",
                ["storeKind"] = string.IsNullOrWhiteSpace(options.InvitationStoreFilePath) ? "in-memory" : "file",
                ["storeDurable"] = (!string.IsNullOrWhiteSpace(options.InvitationStoreFilePath)).ToString().ToLowerInvariant(),
                ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.InvitationStoreFilePath) ? "application-managed" : "cephalon-managed",
                ["administrationWorkflowOwnership"] = administrationWorkflowOwnership,
                ["deliveryDispatchOwnership"] = invitationDeliveryDispatchOwnership,
                ["deliveryStatusReconciliationOwnership"] = invitationDeliveryStatusReconciliationOwnership,
                ["deliveryStatusObservationStoreOwnership"] = invitationDeliveryStatusObservationStoreOwnership,
                ["deliveryRetryQueueOwnership"] = invitationDeliveryRetryQueueOwnership,
                ["deliveryRetryQueueStoreKind"] = invitationDeliveryRetryQueueKind,
                ["deliveryRetryQueueStoreDurable"] = invitationDeliveryRetryQueueDurable.ToString().ToLowerInvariant(),
                ["deliveryRetryMaxAttempts"] = invitationDeliveryRetryMaxAttempts.ToString(CultureInfo.InvariantCulture),
                ["deliveryRetryDelaySeconds"] = invitationDeliveryRetryDelaySeconds.ToString(CultureInfo.InvariantCulture),
                ["deliveryRetryBackgroundEnabled"] = invitationDeliveryRetryBackgroundEnabled.ToString().ToLowerInvariant(),
                ["deliveryRetryBackgroundOwnership"] = invitationDeliveryRetryBackgroundOwnership,
                ["deliveryRetryBackgroundIntervalSeconds"] = invitationDeliveryRetryBackgroundIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                ["deliveryRetryBackgroundRunOnStartup"] = options.InvitationDeliveryRetryBackgroundRunOnStartup.ToString().ToLowerInvariant(),
                ["deliveryRetryExecutionCoordinationEnabled"] = invitationDeliveryRetryExecutionCoordinationEnabled.ToString().ToLowerInvariant(),
                ["deliveryRetryExecutionCoordinationOwnership"] = invitationDeliveryRetryExecutionCoordinationOwnership,
                ["deliveryRetryExecutionCoordinationScope"] = invitationDeliveryRetryExecutionCoordinationScope,
                ["deliveryRetryExecutionCoordinationMode"] = invitationDeliveryRetryExecutionCoordinationMode,
                ["deliverySenderOwnership"] = invitationDeliverySenderOwnership
            }));

        if (options.EnableInvitationValidation)
        {
            capabilities.Add(new Capability(
                key: "tenancy.invitation.validation",
                displayName: "Tenant Invitation Validation",
                description: "Validates whether a tenant invitation is pending, unexpired, invitee-matched, and role-compatible.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["runtimeSurface"] = "tenant-invitations"
                }));
        }

        if (options.EnableInvitationDeliveryDispatch)
        {
            capabilities.Add(new Capability(
                key: "tenancy.invitation.delivery-dispatch",
                displayName: "Tenant Invitation Delivery Dispatch",
                description: "Dispatches pending tenant invitations through registered delivery senders and records dispatch outcomes without owning provider-specific delivery channels.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["deliveryDispatchOwnership"] = invitationDeliveryDispatchOwnership,
                    ["deliverySenderOwnership"] = invitationDeliverySenderOwnership,
                    ["externalDeliveryOwnership"] = invitationExternalDeliveryOwnership,
                    ["deliveryRetryQueueOwnership"] = invitationDeliveryRetryQueueOwnership,
                    ["deliveryRetryQueueStoreKind"] = invitationDeliveryRetryQueueKind,
                    ["deliveryRetryQueueStoreDurable"] = invitationDeliveryRetryQueueDurable.ToString().ToLowerInvariant(),
                    ["deliveryRetryMaxAttempts"] = invitationDeliveryRetryMaxAttempts.ToString(CultureInfo.InvariantCulture),
                    ["deliveryRetryDelaySeconds"] = invitationDeliveryRetryDelaySeconds.ToString(CultureInfo.InvariantCulture),
                    ["deliveryRetryBackgroundEnabled"] = invitationDeliveryRetryBackgroundEnabled.ToString().ToLowerInvariant(),
                    ["deliveryRetryBackgroundOwnership"] = invitationDeliveryRetryBackgroundOwnership,
                    ["deliveryRetryBackgroundIntervalSeconds"] = invitationDeliveryRetryBackgroundIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["deliveryRetryBackgroundRunOnStartup"] = options.InvitationDeliveryRetryBackgroundRunOnStartup.ToString().ToLowerInvariant(),
                    ["deliveryRetryExecutionCoordinationEnabled"] = invitationDeliveryRetryExecutionCoordinationEnabled.ToString().ToLowerInvariant(),
                    ["deliveryRetryExecutionCoordinationOwnership"] = invitationDeliveryRetryExecutionCoordinationOwnership,
                    ["deliveryRetryExecutionCoordinationScope"] = invitationDeliveryRetryExecutionCoordinationScope,
                    ["deliveryRetryExecutionCoordinationMode"] = invitationDeliveryRetryExecutionCoordinationMode,
                    ["senderConfigured"] = hasInvitationDeliverySenders.ToString().ToLowerInvariant(),
                    ["runtimeSurface"] = "tenant-invitations",
                    ["runHistoryLimit"] = Math.Max(1, options.InvitationDeliveryRunHistoryLimit).ToString(CultureInfo.InvariantCulture)
                }));
        }

        if (invitationDeliveryRetryRunnerEnabled)
        {
            capabilities.Add(new Capability(
                key: "tenancy.invitation.delivery-retry-queue",
                displayName: "Tenant Invitation Delivery Retry Queue",
                description: "Queues retryable tenant invitation sender failures and exposes a bounded retry runner; background scheduling is opt-in and distributed queues or provider-specific delivery stay outside this proof.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["ownership"] = "cephalon-managed",
                    ["executionOwnership"] = "cephalon-managed",
                    ["deliveryRetryQueueOwnership"] = invitationDeliveryRetryQueueOwnership,
                    ["deliveryRetryQueueStoreKind"] = invitationDeliveryRetryQueueKind,
                    ["deliveryRetryQueueStoreDurable"] = invitationDeliveryRetryQueueDurable.ToString().ToLowerInvariant(),
                    ["deliveryRetryMaxAttempts"] = invitationDeliveryRetryMaxAttempts.ToString(CultureInfo.InvariantCulture),
                    ["deliveryRetryDelaySeconds"] = invitationDeliveryRetryDelaySeconds.ToString(CultureInfo.InvariantCulture),
                    ["deliveryRetryMaxItems"] = invitationDeliveryRetryMaxItems.ToString(CultureInfo.InvariantCulture),
                    ["deliveryDispatchOwnership"] = invitationDeliveryDispatchOwnership,
                    ["deliverySenderOwnership"] = invitationDeliverySenderOwnership,
                    ["externalDeliveryOwnership"] = invitationExternalDeliveryOwnership,
                    ["backgroundRetryEnabled"] = invitationDeliveryRetryBackgroundEnabled.ToString().ToLowerInvariant(),
                    ["backgroundRetryOwnership"] = invitationDeliveryRetryBackgroundOwnership,
                    ["backgroundRetryIntervalSeconds"] = invitationDeliveryRetryBackgroundIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundRetryRunOnStartup"] = options.InvitationDeliveryRetryBackgroundRunOnStartup.ToString().ToLowerInvariant(),
                    ["executionCoordinationEnabled"] = invitationDeliveryRetryExecutionCoordinationEnabled.ToString().ToLowerInvariant(),
                    ["executionCoordinationOwnership"] = invitationDeliveryRetryExecutionCoordinationOwnership,
                    ["executionCoordinationScope"] = invitationDeliveryRetryExecutionCoordinationScope,
                    ["executionCoordinationMode"] = invitationDeliveryRetryExecutionCoordinationMode,
                    ["distributedRetryOwnership"] = "application-managed",
                    ["exactlyOnceOwnership"] = "application-managed",
                    ["providerSpecificSenderOwnership"] = "application-managed",
                    ["runtimeSurface"] = "tenant-invitations"
                }));

            if (invitationDeliveryRetryExecutionCoordinationEnabled)
            {
                capabilities.Add(new Capability(
                    key: "tenancy.invitation.delivery-retry-execution-coordination",
                    displayName: "Tenant Invitation Delivery Retry Execution Coordination",
                    description: "Coordinates bounded tenant invitation delivery retry runner passes inside one host process and skips overlapping executions instead of dispatching the same local retry queue concurrently.",
                    metadata: new Dictionary<string, string>
                    {
                        ["technology"] = "multi-tenancy",
                        ["package"] = "Cephalon.MultiTenancy.Governance",
                        ["ownership"] = "cephalon-managed",
                        ["executionOwnership"] = "cephalon-managed",
                        ["deliveryRetryQueueOwnership"] = invitationDeliveryRetryQueueOwnership,
                        ["executionCoordinationEnabled"] = invitationDeliveryRetryExecutionCoordinationEnabled.ToString().ToLowerInvariant(),
                        ["executionCoordinationOwnership"] = invitationDeliveryRetryExecutionCoordinationOwnership,
                        ["executionCoordinationScope"] = invitationDeliveryRetryExecutionCoordinationScope,
                        ["executionCoordinationMode"] = invitationDeliveryRetryExecutionCoordinationMode,
                        ["distributedRetryOwnership"] = "application-managed",
                        ["crossNodeLeaseOwnership"] = "application-managed",
                        ["exactlyOnceOwnership"] = "application-managed",
                        ["runtimeSurface"] = "tenant-invitations"
                    }));
            }
        }

        if (invitationDeliveryRetryBackgroundEnabled)
        {
            capabilities.Add(new Capability(
                key: "tenancy.invitation.delivery-retry-background-scheduling",
                displayName: "Tenant Invitation Delivery Retry Background Scheduling",
                description: "Runs automatic background tenant invitation delivery retry scheduling through the generic host and delegates each pass to the bounded retry runner.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["ownership"] = "cephalon-managed",
                    ["executionOwnership"] = "cephalon-managed",
                    ["deliveryRetryQueueOwnership"] = invitationDeliveryRetryQueueOwnership,
                    ["deliveryRetryQueueStoreKind"] = invitationDeliveryRetryQueueKind,
                    ["deliveryRetryQueueStoreDurable"] = invitationDeliveryRetryQueueDurable.ToString().ToLowerInvariant(),
                    ["deliveryRetryMaxAttempts"] = invitationDeliveryRetryMaxAttempts.ToString(CultureInfo.InvariantCulture),
                    ["deliveryRetryDelaySeconds"] = invitationDeliveryRetryDelaySeconds.ToString(CultureInfo.InvariantCulture),
                    ["deliveryRetryMaxItems"] = invitationDeliveryRetryMaxItems.ToString(CultureInfo.InvariantCulture),
                    ["deliveryDispatchOwnership"] = invitationDeliveryDispatchOwnership,
                    ["deliverySenderOwnership"] = invitationDeliverySenderOwnership,
                    ["externalDeliveryOwnership"] = invitationExternalDeliveryOwnership,
                    ["backgroundRetryEnabled"] = invitationDeliveryRetryBackgroundEnabled.ToString().ToLowerInvariant(),
                    ["backgroundRetryOwnership"] = invitationDeliveryRetryBackgroundOwnership,
                    ["backgroundRetryIntervalSeconds"] = invitationDeliveryRetryBackgroundIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundRetryRunOnStartup"] = options.InvitationDeliveryRetryBackgroundRunOnStartup.ToString().ToLowerInvariant(),
                    ["executionCoordinationEnabled"] = invitationDeliveryRetryExecutionCoordinationEnabled.ToString().ToLowerInvariant(),
                    ["executionCoordinationOwnership"] = invitationDeliveryRetryExecutionCoordinationOwnership,
                    ["executionCoordinationScope"] = invitationDeliveryRetryExecutionCoordinationScope,
                    ["executionCoordinationMode"] = invitationDeliveryRetryExecutionCoordinationMode,
                    ["distributedRetryOwnership"] = "application-managed",
                    ["exactlyOnceOwnership"] = "application-managed",
                    ["providerSpecificSenderOwnership"] = "application-managed",
                    ["runtimeSurface"] = "tenant-invitations"
                }));
        }

        if (options.EnableInvitationDeliveryStatusReconciliation)
        {
            capabilities.Add(new Capability(
                key: "tenancy.invitation.delivery-status-reconciliation",
                displayName: "Tenant Invitation Delivery Status Reconciliation",
                description: "Reconciles provider or receiver tenant invitation delivery status observations into Cephalon-managed invitation metadata without owning provider webhook mapping or provider APIs.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["deliveryStatusReconciliationOwnership"] = invitationDeliveryStatusReconciliationOwnership,
                    ["externalDeliveryStatusOwnership"] = invitationExternalDeliveryStatusOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.InvitationStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["deliveryStatusObservationStoreOwnership"] = invitationDeliveryStatusObservationStoreOwnership,
                    ["deliveryStatusObservationStoreKind"] = invitationDeliveryStatusObservationStoreKind,
                    ["deliveryStatusObservationStoreDurable"] = invitationDeliveryStatusObservationStoreDurable.ToString().ToLowerInvariant(),
                    ["deliveryStatusObservationHistoryLimit"] = invitationDeliveryStatusObservationHistoryLimit.ToString(CultureInfo.InvariantCulture),
                    ["runtimeSurface"] = "tenant-invitations"
                }));
        }

        if (options.EnableInvitationDeliveryStatusObservationStore)
        {
            capabilities.Add(new Capability(
                key: "tenancy.invitation.delivery-status-observation-store",
                displayName: "Tenant Invitation Delivery Status Observation Store",
                description: "Records normalized tenant invitation delivery status reconciliation observations for audit and operator review without owning provider-specific callbacks or polling.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["ownership"] = "cephalon-managed",
                    ["executionOwnership"] = "cephalon-managed",
                    ["deliveryStatusReconciliationOwnership"] = invitationDeliveryStatusReconciliationOwnership,
                    ["deliveryStatusObservationStoreOwnership"] = invitationDeliveryStatusObservationStoreOwnership,
                    ["deliveryStatusObservationStoreKind"] = invitationDeliveryStatusObservationStoreKind,
                    ["deliveryStatusObservationStoreDurable"] = invitationDeliveryStatusObservationStoreDurable.ToString().ToLowerInvariant(),
                    ["deliveryStatusObservationHistoryLimit"] = invitationDeliveryStatusObservationHistoryLimit.ToString(CultureInfo.InvariantCulture),
                    ["externalDeliveryStatusOwnership"] = invitationExternalDeliveryStatusOwnership,
                    ["providerSpecificCallbackTranslationOwnership"] = "application-managed",
                    ["providerSpecificSignatureVerificationOwnership"] = "application-managed",
                    ["providerPollingOwnership"] = "application-managed",
                    ["crossNodeReplayProtectionOwnership"] = "application-managed",
                    ["runtimeSurface"] = "tenant-invitations"
                }));
        }

        if (options.EnableTenantAdministrationWorkflow)
        {
            capabilities.Add(new Capability(
                key: "tenancy.administration.workflow",
                displayName: "Tenant Administration Workflow",
                description: "Applies host-driven tenant administration commands over Cephalon-managed membership and invitation stores without claiming public onboarding, provider-specific notification delivery, tenant-admin endpoints, or identity-provider synchronization.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["membershipAdministrationOwnership"] = "cephalon-managed",
                    ["invitationAdministrationOwnership"] = "cephalon-managed",
                    ["membershipStoreKind"] = string.IsNullOrWhiteSpace(options.MembershipStoreFilePath) ? "in-memory" : "file",
                    ["membershipStoreDurable"] = (!string.IsNullOrWhiteSpace(options.MembershipStoreFilePath)).ToString().ToLowerInvariant(),
                    ["invitationStoreKind"] = string.IsNullOrWhiteSpace(options.InvitationStoreFilePath) ? "in-memory" : "file",
                    ["invitationStoreDurable"] = (!string.IsNullOrWhiteSpace(options.InvitationStoreFilePath)).ToString().ToLowerInvariant(),
                    ["publicOnboardingOwnership"] = "application-managed",
                    ["tenantAdminEndpointOwnership"] = "application-managed",
                    ["invitationDeliveryOwnership"] = invitationDeliveryOwnership,
                    ["invitationDeliveryDispatchOwnership"] = invitationDeliveryDispatchOwnership,
                    ["invitationDeliveryStatusReconciliationOwnership"] = invitationDeliveryStatusReconciliationOwnership,
                    ["invitationDeliverySenderOwnership"] = invitationDeliverySenderOwnership,
                    ["externalInvitationDeliveryOwnership"] = invitationExternalDeliveryOwnership,
                    ["externalInvitationDeliveryStatusOwnership"] = invitationExternalDeliveryStatusOwnership,
                    ["identityProviderSyncOwnership"] = "application-managed",
                    ["runtimeSurface"] = "tenant-administration"
                }));
        }

        capabilities.Add(new Capability(
            key: "tenancy.domain-ownership.catalog",
            displayName: "Tenant Domain Ownership Catalog",
            description: "Exposes declared tenant-domain ownership descriptors through the multi-tenancy governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-domain-ownership",
                ["configuredDomainOwnershipCount"] = options.DomainOwnerships.Count.ToString(CultureInfo.InvariantCulture),
                ["hasDomainOwnershipContributors"] = hasDomainOwnershipContributors.ToString().ToLowerInvariant()
            }));

        capabilities.Add(new Capability(
            key: "tenancy.domain-ownership.store",
            displayName: "Tenant Domain Ownership Store",
            description: "Stores runtime tenant-domain ownership declarations managed by the multi-tenancy governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-domain-ownership",
                ["storeKind"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "in-memory" : "file",
                ["storeDurable"] = (!string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath)).ToString().ToLowerInvariant(),
                ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed"
            }));

        if (options.EnableDomainOwnershipValidation)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.validation",
                displayName: "Tenant Domain Ownership Validation",
                description: "Validates whether a declared tenant domain belongs to the requested tenant and is verified, unexpired, and active.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (options.EnableDomainOwnershipVerificationWorkflow)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.workflow",
                displayName: "Tenant Domain Ownership Verification Workflow",
                description: "Executes in-process tenant-domain ownership verification status transitions.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
                    ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["dnsHttpProofCollectionOwnership"] = dnsHttpProofCollectionOwnership,
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (options.EnableDomainOwnershipProofEvaluation && options.EnableDomainOwnershipVerificationWorkflow)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.proof-evaluation",
                displayName: "Tenant Domain Ownership Proof Evaluation",
                description: "Evaluates reported tenant-domain ownership proof evidence and applies verified or rejected workflow transitions.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["proofCollectionOwnership"] = dnsHttpProofCollectionOwnership,
                    ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
                    ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["dnsHttpProofCollectionOwnership"] = dnsHttpProofCollectionOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (options.EnableDomainOwnershipProofChallengeIssuance)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.proof-challenge",
                displayName: "Tenant Domain Ownership Proof Challenge",
                description: "Issues tenant-domain ownership proof challenges and records expected proof values for later evaluation.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["challengeGenerationOwnership"] = "cephalon-managed",
                    ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
                    ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["dnsHttpProofCollectionOwnership"] = dnsHttpProofCollectionOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (options.EnableDomainOwnershipProofPublicationPlanning)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.proof-publication-plan",
                displayName: "Tenant Domain Ownership Proof Publication Plan",
                description: "Builds DNS TXT or HTTP file publication instructions from issued tenant-domain ownership proof challenges.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["publicationPlanningOwnership"] = "cephalon-managed",
                    ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
                    ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["dnsHttpProofCollectionOwnership"] = dnsHttpProofCollectionOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (httpProofPublicationEnabled)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.http-proof-publication",
                displayName: "Tenant Domain Ownership HTTP Proof Publication",
                description: "Materializes and records HTTP proof-file publication state so host adapters can serve tenant-domain ownership proof files.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["httpProofPublicationOwnership"] = "cephalon-managed",
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["dnsProofPublicationOwnership"] = "application-managed",
                    ["providerMutationOwnership"] = "application-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (httpProofCollectionEnabled)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.http-proof-collection",
                displayName: "Tenant Domain Ownership HTTP Proof Collection",
                description: "Collects HTTP file domain-ownership proof content and evaluates it through the governance workflow.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
                    ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["httpProofCollectionOwnership"] = "cephalon-managed",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (dnsTxtProofCollectionEnabled)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.dns-txt-proof-collection",
                displayName: "Tenant Domain Ownership DNS TXT Proof Collection",
                description: "Collects DNS TXT domain-ownership proof content through an explicitly configured DNS-over-HTTPS resolver and evaluates it through the governance workflow.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = dnsTxtProofCollectionConfigured ? "cephalon-managed" : "not-configured",
                    ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
                    ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (proofVerificationRunnerEnabled)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.proof-verification-runner",
                displayName: "Tenant Domain Ownership Proof Verification Runner",
                description: "Runs tenant-domain ownership proof challenge, publication planning, reported-proof evaluation, HTTP proof collection, and configured DNS TXT proof collection paths through one governance entry point.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["proofVerificationRunnerOwnership"] = "cephalon-managed",
                    ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (proofPollingRunnerEnabled)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.proof-polling-runner",
                displayName: "Tenant Domain Ownership Proof Polling Runner",
                description: "Runs bounded on-demand tenant-domain ownership proof polling over pending or rejected declarations and delegates each attempt to the proof verification runner.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["proofPollingRunnerOwnership"] = "cephalon-managed",
                    ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["defaultBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        if (backgroundProofPollingEnabled)
        {
            capabilities.Add(new Capability(
                key: "tenancy.domain-ownership.proof-background-polling",
                displayName: "Tenant Domain Ownership Proof Background Polling",
                description: "Runs automatic background tenant-domain ownership proof polling through the generic host and delegates each pass to the bounded proof polling runner.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["backgroundProofPollingEnabled"] = backgroundProofPollingEnabled.ToString().ToLowerInvariant(),
                    ["intervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingIntervalSeconds"] = backgroundProofPollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["runOnStartup"] = options.DomainOwnershipProofBackgroundPollingRunOnStartup.ToString().ToLowerInvariant(),
                    ["defaultBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["backgroundProofPollingBatchLimit"] = backgroundProofPollingBatchLimit.ToString(CultureInfo.InvariantCulture),
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["proofPublicationOwnership"] = proofPublicationOwnership,
                    ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["runtimeSurface"] = "tenant-domain-ownership"
                }));
        }

        capabilities.Add(new Capability(
            key: "tenancy.governance-action.catalog",
            displayName: "Tenant Governance Action Catalog",
            description: "Exposes tenant approval and remediation action descriptors through the multi-tenancy governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-governance-actions",
                ["configuredActionCount"] = options.GovernanceActions.Count.ToString(CultureInfo.InvariantCulture),
                ["hasGovernanceActionContributors"] = hasGovernanceActionContributors.ToString().ToLowerInvariant()
            }));

        capabilities.Add(new Capability(
            key: "tenancy.governance-action.store",
            displayName: "Tenant Governance Action Store",
            description: "Stores runtime approval and remediation actions created or transitioned by the governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-governance-actions",
                ["storeKind"] = string.IsNullOrWhiteSpace(options.GovernanceActionStoreFilePath) ? "in-memory" : "file",
                ["storeDurable"] = (!string.IsNullOrWhiteSpace(options.GovernanceActionStoreFilePath)).ToString().ToLowerInvariant(),
                ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.GovernanceActionStoreFilePath) ? "application-managed" : "cephalon-managed"
            }));

        if (options.EnableGovernanceActionDecision)
        {
            capabilities.Add(new Capability(
                key: "tenancy.governance-action.decision",
                displayName: "Tenant Governance Action Decision",
                description: "Decides whether declared tenant approval and remediation actions are allowed, pending, rejected, expired, or remediation-blocked.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["runtimeSurface"] = "tenant-governance-actions"
                }));
        }

        if (options.EnableGovernanceActionWorkflow)
        {
            capabilities.Add(new Capability(
                key: "tenancy.governance-action.workflow",
                displayName: "Tenant Governance Action Workflow",
                description: "Executes in-process approval and remediation status transitions for tenant-governance actions.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.GovernanceActionStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["notificationDeliveryOwnership"] = "application-managed",
                    ["runtimeSurface"] = "tenant-governance-actions"
                }));
        }
    }
}
