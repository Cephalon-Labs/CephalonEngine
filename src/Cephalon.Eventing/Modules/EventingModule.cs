using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Eventing.Configuration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;

namespace Cephalon.Eventing.Modules;

internal sealed class EventingModule : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "eventing-runtime",
        displayName: "Eventing Runtime",
        description: "Companion runtime services for event-driven integration workloads.",
        tags: ["technology", "eventing"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "technology-pack",
            ["technology"] = "event-driven-integration"
        });

    private readonly EventingOptions options;
    private bool hasChannelContributors;
    private bool hasDispatchStore;
    private bool hasDispatchRuntimeContributors;
    private bool hasExternalManagedSubscriptionExecutionBindings;
    private bool hasInboxPath;
    private bool hasInProcessSubscriptionDescriptorDiscovery;
    private bool hasInProcessSubscriptionExecutionPath;
    private bool hasManagedSubscriptionExecutionBindings;
    private bool hasOutboxPublishingPath;
    private bool hasSubscriptionContributors;
    private bool hasSubscriptionExecutionPipeline;
    private bool hasSubscriptionExecutors;
    private bool hasPublishingPath;
    private string inProcessSubscriptionDescriptorDiscoveryMode = "none";
    private int subscriptionExecutionMiddlewareCount;

    public EventingModule(EventingOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration"))
        {
            return;
        }

        hasChannelContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventChannelContributor));
        hasDispatchStore = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventDispatchStore));
        hasDispatchRuntimeContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventDispatchRuntimeContributor));
        hasExternalManagedSubscriptionExecutionBindings = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventSubscriptionExecutionBindingContributor));
        var inboxRegistrationCount = services.Count(static descriptor => descriptor.ServiceType == typeof(IInbox));
        hasInboxPath = inboxRegistrationCount > 0;
        subscriptionExecutionMiddlewareCount = services.Count(static descriptor => descriptor.ServiceType == typeof(IEventSubscriptionExecutionMiddleware));
        hasSubscriptionExecutionPipeline = subscriptionExecutionMiddlewareCount > 0;
        hasSubscriptionExecutors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventSubscriptionExecutor));
        hasInProcessSubscriptionDescriptorDiscovery = options.EnableSubscriptions &&
            options.EnableInProcessSubscriptionExecution &&
            hasSubscriptionExecutors;
        inProcessSubscriptionDescriptorDiscoveryMode = hasInProcessSubscriptionDescriptorDiscovery
            ? GetInProcessSubscriptionDescriptorDiscoveryMode(services)
            : "none";
        if (hasInProcessSubscriptionDescriptorDiscovery)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventSubscriptionContributor, InProcessEventSubscriptionDescriptorContributor>());
        }

        hasSubscriptionContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventSubscriptionContributor));
        hasInProcessSubscriptionExecutionPath = options.EnableInProcessSubscriptionExecution && hasSubscriptionExecutors;
        hasManagedSubscriptionExecutionBindings = hasExternalManagedSubscriptionExecutionBindings || hasInProcessSubscriptionExecutionPath;
        services.TryAddSingleton(options);
        services.TryAddSingleton(new EventSubscriptionExecutionPipelineDescriptor(subscriptionExecutionMiddlewareCount));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, EventingDiagnosticsConventionContributor>());
        services.TryAddSingleton<IEventChannelCatalog, EventChannelCatalog>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingRuntimeSurfaceContributor>());
        if (options.EnableSubscriptions)
        {
            services.TryAddSingleton<IEventSubscriptionCatalog, EventSubscriptionCatalog>();
            services.TryAddSingleton<EventSubscriptionExecutionBindingCatalog>();
            services.TryAddSingleton<IEventSubscriptionExecutionBindingCatalog>(
                static provider => provider.GetRequiredService<EventSubscriptionExecutionBindingCatalog>());
            services.TryAddSingleton<EventSubscriptionExecutionReadinessCatalog>();
            services.TryAddSingleton<IEventSubscriptionExecutionReadinessCatalog>(
                static provider => provider.GetRequiredService<EventSubscriptionExecutionReadinessCatalog>());
            services.TryAddSingleton<EventSubscriptionRuntimeCatalog>();
            services.TryAddSingleton<IEventSubscriptionRuntimeCatalog>(static provider => provider.GetRequiredService<EventSubscriptionRuntimeCatalog>());
            services.TryAddSingleton<IEventSubscriptionRuntimeReporter>(static provider => provider.GetRequiredService<EventSubscriptionRuntimeCatalog>());
            if (hasInProcessSubscriptionExecutionPath)
            {
                services.TryAddSingleton<InProcessEventSubscriptionExecutorCatalog>();
                services.TryAddSingleton<InProcessEventSubscriptionIdempotencyTracker>();
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventSubscriptionExecutionBindingContributor, InProcessEventSubscriptionExecutorCatalog>());
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingSubscriptionRuntimeSurfaceContributor>());
        }

        if (options.EnableInProcessSubscriptionExecution)
        {
            if (!options.EnablePublishing)
            {
                throw new InvalidOperationException(
                    "In-process event subscription execution requires publishing to be enabled because the built-in direct publisher is the execution trigger.");
            }

            if (!options.EnableSubscriptions)
            {
                throw new InvalidOperationException(
                    "In-process event subscription execution requires subscriptions to be enabled.");
            }

            if (!hasSubscriptionExecutors)
            {
                throw new InvalidOperationException(
                    "In-process event subscription execution requires at least one IEventSubscriptionExecutor. Register a managed executor before enabling EnableInProcessSubscriptionExecution.");
            }

            if (hasExternalManagedSubscriptionExecutionBindings)
            {
                throw new InvalidOperationException(
                    "In-process event subscription execution cannot be combined with another managed subscription-execution binding contributor. Select one execution owner for each eventing flow.");
            }

            if (hasDispatchRuntimeContributors)
            {
                throw new InvalidOperationException(
                    "In-process event subscription execution cannot be combined with an event dispatch runtime contributor. Select either the built-in direct in-process path or a dispatch-runtime-backed companion path for this host.");
            }

            if (options.EnableInProcessSubscriptionIdempotency &&
                InProcessEventingIdempotencyPolicy.UsesInbox(options))
            {
                if (inboxRegistrationCount == 0)
                {
                    throw new InvalidOperationException(
                        "Inbox-backed in-process event subscription idempotency requires exactly one IInbox registration. Register an inbox store before setting Engine:Messaging:InProcessSubscriptions:Idempotency:Store to 'inbox'.");
                }

                if (inboxRegistrationCount > 1)
                {
                    throw new InvalidOperationException(
                        "Inbox-backed in-process event subscription idempotency requires exactly one IInbox registration. Multiple inbox registrations would make duplicate suppression nondeterministic.");
                }
            }
        }
        else if (options.EnableInProcessSubscriptionIdempotency &&
            InProcessEventingIdempotencyPolicy.UsesInbox(options))
        {
            throw new InvalidOperationException(
                "Inbox-backed in-process event subscription idempotency requires EnableInProcessSubscriptionExecution to be enabled because it protects the built-in direct subscription executor path.");
        }

        hasOutboxPublishingPath = options.EnablePublishing &&
            !hasInProcessSubscriptionExecutionPath &&
            services.Any(static descriptor => descriptor.ServiceType == typeof(IOutbox));
        hasPublishingPath = hasInProcessSubscriptionExecutionPath || hasOutboxPublishingPath;
        services.TryAddSingleton(new EventingRuntimeTopology(
            HasChannelContributors: hasChannelContributors,
            HasDispatchStore: hasDispatchStore,
            HasDispatchRuntimeContributors: hasDispatchRuntimeContributors,
            HasExternalManagedSubscriptionExecutionBindings: hasExternalManagedSubscriptionExecutionBindings,
            HasInboxPath: hasInboxPath,
            HasInProcessSubscriptionDescriptorDiscovery: hasInProcessSubscriptionDescriptorDiscovery,
            InProcessSubscriptionDescriptorDiscoveryMode: inProcessSubscriptionDescriptorDiscoveryMode,
            HasInProcessSubscriptionExecutionPath: hasInProcessSubscriptionExecutionPath,
            HasManagedSubscriptionExecutionBindings: hasManagedSubscriptionExecutionBindings,
            HasOutboxPublishingPath: hasOutboxPublishingPath,
            HasPublishingPath: hasPublishingPath,
            HasSubscriptionContributors: hasSubscriptionContributors,
            HasSubscriptionExecutors: hasSubscriptionExecutors,
            SubscriptionExecutionMiddlewareCount: subscriptionExecutionMiddlewareCount));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingSuperiorityProfileRuntimeSurfaceContributor>());
        if (hasInProcessSubscriptionExecutionPath)
        {
            services.TryAddScoped<IEventPublisher, InProcessEventPublisher>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingInProcessPublishingRuntimeSurfaceContributor>());
        }

        if (hasOutboxPublishingPath)
        {
            services.TryAddSingleton<EventDispatchRuntimeDescriptorCatalog>();
            services.TryAddSingleton<EventDispatchRuntimeCatalog>();
            services.TryAddSingleton<IEventDispatchRuntimeDescriptorCatalog>(static provider => provider.GetRequiredService<EventDispatchRuntimeDescriptorCatalog>());
            services.TryAddSingleton<IEventDispatchRuntimeCatalog>(static provider => provider.GetRequiredService<EventDispatchRuntimeCatalog>());
            services.TryAddSingleton<IEventDispatchRuntimeReporter>(static provider => provider.GetRequiredService<EventDispatchRuntimeCatalog>());
            services.TryAddSingleton<IOutboxDispatchPolicyCatalog, OutboxDispatchPolicyCatalog>();
            services.TryAddScoped<IEventPublisher, OutboxBackedEventPublisher>();
            if (hasDispatchStore)
            {
                services.TryAddSingleton<EventDispatchRemediationRuntimeCatalog>();
                services.TryAddSingleton<IEventDispatchRemediationRuntimeCatalog>(static provider => provider.GetRequiredService<EventDispatchRemediationRuntimeCatalog>());
                services.TryAddScoped<IEventDispatchRemediationDispatcher, EventDispatchRemediationDispatcher>();
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingPublishingRuntimeSurfaceContributor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingDispatchRuntimeSurfaceContributor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingDispatchRemediationRuntimeSurfaceContributor>());
            if (hasDispatchStore)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingDispatchRemediationCommandRuntimeSurfaceContributor>());
            }

            if (hasDispatchRuntimeContributors)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingDispatchRuntimeCatalogSurfaceContributor>());
            }
        }

        if (hasPublishingPath)
        {
            services.TryAddSingleton<EventPublicationScheduleQueue>();
            services.TryAddSingleton<EventPublicationRuntimeCatalog>();
            services.TryAddSingleton<IEventPublicationRuntimeCatalog>(static provider => provider.GetRequiredService<EventPublicationRuntimeCatalog>());
            services.TryAddSingleton<IEventPublicationRuntimeReporter>(static provider => provider.GetRequiredService<EventPublicationRuntimeCatalog>());
            services.TryAddScoped<IEventPublicationDispatcher, EventPublicationDispatcher>();
        }
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration"))
        {
            return;
        }

        if (options.EnablePublishing && hasPublishingPath)
        {
            var inProcessRetryPolicy = InProcessEventingRetryPolicy.GetPolicyId(options);
            var inProcessRetryMaxAttempts = InProcessEventingRetryPolicy.GetMaxAttempts(options).ToString(CultureInfo.InvariantCulture);
            var inProcessRetryDelayMilliseconds = InProcessEventingRetryPolicy.GetRetryDelayMilliseconds(options).ToString(CultureInfo.InvariantCulture);
            var inProcessRetryBackoff = InProcessEventingRetryPolicy.GetBackoff(options);
            var inProcessRetryBackoffMultiplier = InProcessEventingRetryPolicy.GetBackoffMultiplier(options).ToString(CultureInfo.InvariantCulture);
            var inProcessRetryMaxDelayMilliseconds = InProcessEventingRetryPolicy.GetMaxDelayMilliseconds(options).ToString(CultureInfo.InvariantCulture);
            var inProcessRetryJitterPercent = InProcessEventingRetryPolicy.GetJitterPercent(options).ToString(CultureInfo.InvariantCulture);
            var inProcessIdempotencyPolicy = InProcessEventingIdempotencyPolicy.GetPolicyId(options);
            var inProcessIdempotencyKey = InProcessEventingIdempotencyPolicy.GetKeyShape(options);
            var inProcessIdempotencyStore = InProcessEventingIdempotencyPolicy.GetStore(options);
            var inProcessIdempotencyScope = InProcessEventingIdempotencyPolicy.GetScope(options);
            var inProcessIdempotencyDurability = InProcessEventingIdempotencyPolicy.GetDurability(options);
            var inProcessIdempotencyRetentionMinutes = InProcessEventingIdempotencyPolicy.GetRetentionMinutes(options).ToString(CultureInfo.InvariantCulture);
            var publicationSchedulingPolicy = EventPublicationSchedulingPolicy.GetPolicyId(options);
            var publicationSchedulingScope = EventPublicationSchedulingPolicy.GetScope(options);
            var publicationSchedulingDurability = EventPublicationSchedulingPolicy.GetDurability(options);
            var publicationSchedulingMaxDelayMilliseconds = options.PublicationSchedulingMaxDelayMilliseconds.ToString(CultureInfo.InvariantCulture);
            var publicationSchedulingMaxPendingCount = options.PublicationSchedulingMaxPendingCount.ToString(CultureInfo.InvariantCulture);
            var publicationRoutingPolicy = EventPublicationRoutingPolicy.GetPolicyId(options);
            var publicationRoutingRouteCount = EventPublicationRoutingPolicy.GetRouteCount(options);
            var publicationRoutingAutoChannelId = EventPublicationRoutingPolicy.GetAutoChannelId(options);
            var publicationRoutingRequireMatchedRoute = options.PublicationRoutingRequireMatchedRoute.ToString().ToLowerInvariant();
            var publicationRoutingRejectMismatchedExplicitChannel = options.PublicationRoutingRejectMismatchedExplicitChannel.ToString().ToLowerInvariant();
            var publishMetadata = hasInProcessSubscriptionExecutionPath
                ? new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["handoff"] = "in-process",
                    ["dispatchRuntime"] = "cephalon-managed",
                    ["dispatchStore"] = "not-configured",
                    ["subscriptionExecution"] = "cephalon-managed",
                    ["subscriptionDescriptorDiscovery"] = inProcessSubscriptionDescriptorDiscoveryMode,
                    ["subscriptionExecutionPipeline"] = hasSubscriptionExecutionPipeline ? "code-first" : "none",
                    ["subscriptionExecutionMiddlewareCount"] = subscriptionExecutionMiddlewareCount.ToString(CultureInfo.InvariantCulture),
                    ["executionRuntimeId"] = InProcessEventingRuntimeIds.SubscriptionExecutionRuntimeId,
                    ["triggerRuntimeId"] = InProcessEventingRuntimeIds.PublisherId,
                    ["publicationDispatcher"] = "available",
                    ["publicationRuntimeState"] = "available",
                    ["retryPolicy"] = inProcessRetryPolicy,
                    ["retryMaxAttempts"] = inProcessRetryMaxAttempts,
                    ["retryDelayMilliseconds"] = inProcessRetryDelayMilliseconds,
                    ["retryBackoff"] = inProcessRetryBackoff,
                    ["retryBackoffMultiplier"] = inProcessRetryBackoffMultiplier,
                    ["retryMaxDelayMilliseconds"] = inProcessRetryMaxDelayMilliseconds,
                    ["retryJitterPercent"] = inProcessRetryJitterPercent,
                    ["retryDurability"] = "none",
                    ["retryScope"] = "process-local",
                    ["idempotencyPolicy"] = inProcessIdempotencyPolicy,
                    ["idempotencyKey"] = inProcessIdempotencyKey,
                    ["idempotencyStore"] = inProcessIdempotencyStore,
                    ["idempotencyRetentionMinutes"] = inProcessIdempotencyRetentionMinutes,
                    ["idempotencyDurability"] = inProcessIdempotencyDurability,
                    ["idempotencyScope"] = inProcessIdempotencyScope,
                    ["inbox"] = hasInboxPath ? "available" : "not-configured",
                    ["publicationSchedulingPolicy"] = publicationSchedulingPolicy,
                    ["publicationSchedulingScope"] = publicationSchedulingScope,
                    ["publicationSchedulingDurability"] = publicationSchedulingDurability,
                    ["publicationSchedulingMaxDelayMilliseconds"] = publicationSchedulingMaxDelayMilliseconds,
                    ["publicationSchedulingMaxPendingCount"] = publicationSchedulingMaxPendingCount,
                    ["publicationRoutingPolicy"] = publicationRoutingPolicy,
                    ["publicationRoutingRouteCount"] = publicationRoutingRouteCount,
                    ["publicationRoutingAutoChannelId"] = publicationRoutingAutoChannelId,
                    ["publicationRoutingRequireMatchedRoute"] = publicationRoutingRequireMatchedRoute,
                    ["publicationRoutingRejectMismatchedExplicitChannel"] = publicationRoutingRejectMismatchedExplicitChannel,
                    ["runtimeState"] = "available"
                }
                : new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["handoff"] = "outbox",
                    ["dispatchRuntime"] = hasDispatchRuntimeContributors ? "configured" : "not-configured",
                    ["dispatchStore"] = hasDispatchStore ? "available" : "not-configured",
                    ["publicationDispatcher"] = "available",
                    ["publicationRuntimeState"] = "available",
                    ["publicationSchedulingPolicy"] = publicationSchedulingPolicy,
                    ["publicationSchedulingScope"] = publicationSchedulingScope,
                    ["publicationSchedulingDurability"] = publicationSchedulingDurability,
                    ["publicationSchedulingMaxDelayMilliseconds"] = publicationSchedulingMaxDelayMilliseconds,
                    ["publicationSchedulingMaxPendingCount"] = publicationSchedulingMaxPendingCount,
                    ["publicationRoutingPolicy"] = publicationRoutingPolicy,
                    ["publicationRoutingRouteCount"] = publicationRoutingRouteCount,
                    ["publicationRoutingAutoChannelId"] = publicationRoutingAutoChannelId,
                    ["publicationRoutingRequireMatchedRoute"] = publicationRoutingRequireMatchedRoute,
                    ["publicationRoutingRejectMismatchedExplicitChannel"] = publicationRoutingRejectMismatchedExplicitChannel,
                    ["runtimeState"] = "available"
                };

            capabilities.Add(new Capability(
                key: "eventing.publish",
                displayName: "Event Publishing",
                description: hasInProcessSubscriptionExecutionPath
                    ? "Accepts integration events for configured event channels and directly invokes matching in-process subscription executors."
                    : "Accepts integration events for configured event channels and stages them through the active outbox path.",
                metadata: publishMetadata));
        }

        capabilities.Add(new Capability(
            key: "eventing.superiority-profile",
            displayName: "Eventing Superiority Profile",
            description: "Projects runtime evidence for how the native Cephalon eventing path compares with established .NET messaging and mediator framework capabilities.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "event-driven-integration",
                ["surfaceId"] = "eventing-superiority-profile",
                ["referenceFrameworks"] = "MassTransit,NServiceBus,Wolverine,MediatR",
                ["claimPolicy"] = "claimed-only-with-runtime-evidence",
                ["wolverineOptional"] = "true",
                ["runtimeState"] = "available"
            }));

        if (options.EnableSubscriptions && (options.Subscriptions.Count > 0 || hasSubscriptionContributors))
        {
            capabilities.Add(new Capability(
                key: "eventing.subscriptions",
                displayName: "Event Subscription Descriptors",
                description: "Exposes declared event subscription descriptors to the runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["dispatchRuntime"] = hasManagedSubscriptionExecutionBindings ? "configured" : "not-configured",
                    ["inbox"] = hasInboxPath ? "available" : "not-configured",
                    ["subscriptionDescriptorDiscovery"] = inProcessSubscriptionDescriptorDiscoveryMode,
                    ["runtimeState"] = "available"
                }));
        }

        if (options.EnableSubscriptions && hasInProcessSubscriptionExecutionPath)
        {
            var retryPolicy = InProcessEventingRetryPolicy.GetPolicyId(options);
            capabilities.Add(new Capability(
                key: "eventing.subscribe",
                displayName: "Managed Event Subscription Execution",
                description: "Executes declared event subscriptions through the built-in in-process direct publisher with optional bounded process-local retries and process-local or inbox-backed duplicate suppression.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["adapter"] = "none",
                    ["executionOwnership"] = "cephalon-managed",
                    ["executionMode"] = "in-process-direct",
                    ["executionRuntimeId"] = InProcessEventingRuntimeIds.SubscriptionExecutionRuntimeId,
                    ["triggerRuntimeId"] = InProcessEventingRuntimeIds.PublisherId,
                    ["subscriptionDescriptorDiscovery"] = inProcessSubscriptionDescriptorDiscoveryMode,
                    ["subscriptionExecutionPipeline"] = hasSubscriptionExecutionPipeline ? "code-first" : "none",
                    ["subscriptionExecutionMiddlewareCount"] = subscriptionExecutionMiddlewareCount.ToString(CultureInfo.InvariantCulture),
                    ["retryPolicy"] = retryPolicy,
                    ["retryMaxAttempts"] = InProcessEventingRetryPolicy.GetMaxAttempts(options).ToString(CultureInfo.InvariantCulture),
                    ["retryDelayMilliseconds"] = InProcessEventingRetryPolicy.GetRetryDelayMilliseconds(options).ToString(CultureInfo.InvariantCulture),
                    ["retryBackoff"] = InProcessEventingRetryPolicy.GetBackoff(options),
                    ["retryBackoffMultiplier"] = InProcessEventingRetryPolicy.GetBackoffMultiplier(options).ToString(CultureInfo.InvariantCulture),
                    ["retryMaxDelayMilliseconds"] = InProcessEventingRetryPolicy.GetMaxDelayMilliseconds(options).ToString(CultureInfo.InvariantCulture),
                    ["retryJitterPercent"] = InProcessEventingRetryPolicy.GetJitterPercent(options).ToString(CultureInfo.InvariantCulture),
                    ["retryDurability"] = "none",
                    ["retryScope"] = "process-local",
                    ["idempotencyPolicy"] = InProcessEventingIdempotencyPolicy.GetPolicyId(options),
                    ["idempotencyKey"] = InProcessEventingIdempotencyPolicy.GetKeyShape(options),
                    ["idempotencyStore"] = InProcessEventingIdempotencyPolicy.GetStore(options),
                    ["idempotencyRetentionMinutes"] = InProcessEventingIdempotencyPolicy.GetRetentionMinutes(options).ToString(CultureInfo.InvariantCulture),
                    ["idempotencyDurability"] = InProcessEventingIdempotencyPolicy.GetDurability(options),
                    ["idempotencyScope"] = InProcessEventingIdempotencyPolicy.GetScope(options),
                    ["inbox"] = hasInboxPath ? "available" : "not-configured"
                }));
        }

        if (options.EnablePublishing && hasOutboxPublishingPath && hasDispatchStore)
        {
            capabilities.Add(new Capability(
                key: "eventing.dispatch-remediation",
                displayName: "Event Dispatch Remediation",
                description: "Runs bounded provider-neutral operator commands against the active dispatch store without requiring Wolverine or another bus package.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["surfaceId"] = "event-dispatch-remediations",
                    ["commandRoute"] = "/engine/event-dispatches/{outboxId}/commands/{operationId}",
                    ["commandResultRoute"] = "/engine/event-dispatch-remediation-commands/{commandId}",
                    ["operationIds"] = string.Join(
                        ",",
                        EventDispatchRemediationOperationIds.RetryNow,
                        EventDispatchRemediationOperationIds.RetryLater,
                        EventDispatchRemediationOperationIds.Skip,
                        EventDispatchRemediationOperationIds.Quarantine,
                        EventDispatchRemediationOperationIds.DeadLetter),
                    ["commandScope"] = "dispatch-store",
                    ["retryCommand"] = "ready",
                    ["retryLaterCommand"] = "ready",
                    ["skipCommand"] = "ready",
                    ["quarantineCommand"] = "ready",
                    ["deadLetterCommand"] = "dispatch-store-ready",
                    ["brokerDeadLetterCommand"] = "not-claimed",
                    [EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy] = "unique-command-id",
                    [EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy] = "reject-without-mutation",
                    ["commandRuntimeState"] = "available",
                    ["commandHistoryLimit"] = options.RemediationCommandHistoryLimit.ToString(CultureInfo.InvariantCulture),
                    ["wolverineRequired"] = "false",
                    ["runtimeState"] = "available"
                }));
        }

        if (options.Channels.Count > 0 || hasChannelContributors)
        {
            capabilities.Add(new Capability(
                key: "eventing.channels",
                displayName: "Event Channels",
                description: "Exposes configured event channel descriptors to the runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["channelCount"] = options.Channels.Count.ToString(CultureInfo.InvariantCulture)
                }));
        }
    }

    private static string GetInProcessSubscriptionDescriptorDiscoveryMode(IServiceCollection services)
    {
        var hasAttributeDescriptor = false;
        var hasProviderDescriptor = false;
        var hasUnknownDescriptorShape = false;

        foreach (var descriptor in services.Where(static descriptor => descriptor.ServiceType == typeof(IEventSubscriptionExecutor)))
        {
            var executorType = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
            if (executorType is null)
            {
                hasUnknownDescriptorShape = true;
                continue;
            }

            if (executorType.IsDefined(typeof(EventSubscriptionAttribute), inherit: false))
            {
                hasAttributeDescriptor = true;
            }

            if (typeof(IEventSubscriptionDescriptorProvider).IsAssignableFrom(executorType))
            {
                hasProviderDescriptor = true;
            }
        }

        if (hasAttributeDescriptor && !hasProviderDescriptor && !hasUnknownDescriptorShape)
        {
            return "code-first-attribute";
        }

        if (hasProviderDescriptor && !hasAttributeDescriptor && !hasUnknownDescriptorShape)
        {
            return "code-first-executor";
        }

        if (hasAttributeDescriptor || hasProviderDescriptor)
        {
            return "code-first-mixed";
        }

        return hasUnknownDescriptorShape ? "code-first-executor" : "none";
    }
}
