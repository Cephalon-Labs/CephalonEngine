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
        hasDomainOwnershipContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(ITenantDomainOwnershipContributor));
        hasGovernanceActionContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(ITenantGovernanceActionContributor));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ILogger<TenantMembershipEvaluator>>(NullLogger<TenantMembershipEvaluator>.Instance);
        services.TryAddSingleton<ILogger<TenantInvitationValidator>>(NullLogger<TenantInvitationValidator>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipValidator>>(NullLogger<TenantDomainOwnershipValidator>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipVerificationWorkflow>>(NullLogger<TenantDomainOwnershipVerificationWorkflow>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofEvaluator>>(NullLogger<TenantDomainOwnershipProofEvaluator>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofChallengeIssuer>>(NullLogger<TenantDomainOwnershipProofChallengeIssuer>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofPublicationPlanner>>(NullLogger<TenantDomainOwnershipProofPublicationPlanner>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipHttpProofCollector>>(NullLogger<TenantDomainOwnershipHttpProofCollector>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipDnsTxtProofCollector>>(NullLogger<TenantDomainOwnershipDnsTxtProofCollector>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofVerificationRunner>>(NullLogger<TenantDomainOwnershipProofVerificationRunner>.Instance);
        services.TryAddSingleton<ILogger<TenantDomainOwnershipProofPollingRunner>>(NullLogger<TenantDomainOwnershipProofPollingRunner>.Instance);
        services.TryAddSingleton<ILogger<TenantGovernanceActionDecider>>(NullLogger<TenantGovernanceActionDecider>.Instance);
        services.TryAddSingleton<ILogger<TenantGovernanceActionWorkflow>>(NullLogger<TenantGovernanceActionWorkflow>.Instance);
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
        services.TryAddSingleton<ITenantDomainOwnershipStore>(
            static serviceProvider => TenantDomainOwnershipStores.Create(serviceProvider.GetRequiredService<MultiTenancyGovernanceOptions>()));
        services.TryAddSingleton<ITenantGovernanceActionStore>(
            static serviceProvider => TenantGovernanceActionStores.Create(serviceProvider.GetRequiredService<MultiTenancyGovernanceOptions>()));
        services.TryAddSingleton<ITenantMembershipCatalog, TenantMembershipCatalog>();
        services.TryAddSingleton<ITenantInvitationCatalog, TenantInvitationCatalog>();
        services.TryAddSingleton<ITenantDomainOwnershipCatalog, TenantDomainOwnershipCatalog>();
        services.TryAddSingleton<ITenantGovernanceActionCatalog, TenantGovernanceActionCatalog>();

        if (options.EnableMembershipEvaluation)
        {
            services.TryAddSingleton<ITenantMembershipEvaluator, TenantMembershipEvaluator>();
        }

        if (options.EnableInvitationValidation)
        {
            services.TryAddSingleton<ITenantInvitationValidator, TenantInvitationValidator>();
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

        if (options.EnableGovernanceActionDecision)
        {
            services.TryAddSingleton<ITenantGovernanceActionDecider, TenantGovernanceActionDecider>();
        }

        if (options.EnableGovernanceActionWorkflow)
        {
            services.TryAddSingleton<ITenantGovernanceActionWorkflow, TenantGovernanceActionWorkflow>();
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceInvitationRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceDomainRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceActionRuntimeSurfaceContributor>());
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
        var dnsTxtProofCollectionEnabled = options.EnableDomainOwnershipDnsTxtProofCollection &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow;
        var dnsTxtProofCollectionConfigured = dnsTxtProofCollectionEnabled &&
            options.DomainOwnershipDnsTxtProofResolverEndpoint is not null;
        var proofVerificationRunnerEnabled = options.EnableDomainOwnershipProofVerificationRunner &&
            options.EnableDomainOwnershipProofChallengeIssuance &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow;
        var proofPollingRunnerEnabled = options.EnableDomainOwnershipProofPollingRunner &&
            proofVerificationRunnerEnabled;
        var dnsTxtProofCollectionOwnership = dnsTxtProofCollectionConfigured ? "cephalon-managed" : "not-configured";
        var dnsHttpProofCollectionOwnership = httpProofCollectionEnabled && dnsTxtProofCollectionConfigured
            ? "cephalon-managed"
            : httpProofCollectionEnabled || dnsTxtProofCollectionConfigured
                ? "mixed"
                : "application-managed";
        var proofPollingRunnerOwnership = proofPollingRunnerEnabled ? "cephalon-managed" : "not-configured";
        var externalProofPollingOwnership = proofPollingRunnerEnabled ? "cephalon-managed" : "application-managed";
        const string backgroundProofPollingOwnership = "application-managed";

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
                ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.MembershipStoreFilePath) ? "application-managed" : "cephalon-managed"
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
                ["hasInvitationContributors"] = hasInvitationContributors.ToString().ToLowerInvariant()
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
                ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.InvitationStoreFilePath) ? "application-managed" : "cephalon-managed"
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
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
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
                    ["proofPublicationOwnership"] = "application-managed",
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
                    ["proofPublicationOwnership"] = "application-managed",
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
                    ["httpProofCollectionOwnership"] = "cephalon-managed",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["proofPublicationOwnership"] = "application-managed",
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
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["proofPublicationOwnership"] = "application-managed",
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
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["proofPublicationOwnership"] = "application-managed",
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
                    ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
                    ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionOwnership,
                    ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
                    ["externalProofPollingOwnership"] = externalProofPollingOwnership,
                    ["backgroundProofPollingOwnership"] = backgroundProofPollingOwnership,
                    ["proofPublicationOwnership"] = "application-managed",
                    ["durableStoreOwnership"] = string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath) ? "application-managed" : "cephalon-managed",
                    ["defaultBatchLimit"] = (options.DomainOwnershipProofPollingMaxItems <= 0 ? 50 : options.DomainOwnershipProofPollingMaxItems).ToString(CultureInfo.InvariantCulture),
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
