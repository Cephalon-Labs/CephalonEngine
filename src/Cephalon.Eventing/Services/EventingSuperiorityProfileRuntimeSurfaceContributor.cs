using Cephalon.Abstractions.Technologies;
using Cephalon.Eventing.Configuration;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingSuperiorityProfileRuntimeSurfaceContributor(
    EventingOptions options,
    EventingRuntimeTopology topology) : ITechnologyRuntimeContributor
{
    private const string ReferenceFrameworks = "MassTransit,NServiceBus,Wolverine,MediatR";
    private const string ClaimPolicy = "claimed-only-with-runtime-evidence";

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var routeCount = options.PublicationRoutes.Count.ToString(CultureInfo.InvariantCulture);
        var routingStatus = options.EnablePublicationRouting && options.PublicationRoutes.Count > 0
            ? "claimed"
            : options.EnablePublicationRouting ? "partial" : "not-claimed";
        var routingEvidence = options.EnablePublicationRouting
            ? $"policy={EventPublicationRoutingPolicy.GetPolicyId(options)} routes={routeCount} autoChannel={EventPublicationRoutingPolicy.GetAutoChannelId(options)}"
            : "publication routing is not enabled.";

        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "eventing-superiority-profile",
            displayName: "Eventing Superiority Profile",
            description: "Runtime evidence for the Cephalon-native eventing target compared with established .NET messaging and mediator frameworks.",
            entries:
            [
                CreateEntry(
                    id: "configuration-first-provider-neutrality",
                    displayName: "Configuration-first Provider Neutrality",
                    description: "Keeps the app authoring surface on Cephalon configuration and runtime contracts instead of external bus APIs.",
                    status: "claimed",
                    evidence: "Engine:Technologies=EventDrivenIntegration; provider adapters remain explicit opt-ins.",
                    advantage: "Consumers can switch native, in-process, outbox, or provider-managed paths without rewriting application handlers around a vendor API.",
                    nextGap: "Keep adding provider-specific companion packs only behind explicit Engine:Messaging choices."),
                CreateEntry(
                    id: "routing-and-provider-portability",
                    displayName: "Routing And Provider Portability",
                    description: "Routes event publications through a Cephalon-owned event-type map before any in-process, outbox, or provider-managed publisher receives the message.",
                    status: routingStatus,
                    evidence: routingEvidence,
                    advantage: "Application code can publish against a stable Cephalon route contract while channel ownership moves through configuration instead of Wolverine, MassTransit, NServiceBus, or MediatR APIs.",
                    nextGap: options.EnablePublicationRouting && options.PublicationRoutes.Count > 0
                        ? "Extend this same route truth into future broker-topology and generated contract validation surfaces."
                        : "Configure Engine:Messaging:Publications:Routing:Routes before claiming routing evidence."),
                CreateEntry(
                    id: "native-wolverine-free-baseline",
                    displayName: "Native Wolverine-free Baseline",
                    description: "Shows whether the active runtime has a Cephalon-owned eventing path without the Wolverine companion.",
                    status: topology.HasPublishingPath && !topology.HasExternalManagedSubscriptionExecutionBindings ? "claimed" : "partial",
                    evidence: topology.HasPublishingPath
                        ? "A native publication path is active; Wolverine remains optional."
                        : "Eventing descriptors are active, but no publication path is currently configured.",
                    advantage: "The core pack can remain host-agnostic and package-light while companion packs stay replaceable.",
                    nextGap: topology.HasPublishingPath
                        ? "Promote more durable operator actions into provider-neutral contracts."
                        : "Configure in-process execution or an outbox-backed publication path to activate runtime-owned publication."),
                CreateEntry(
                    id: "runtime-truth-and-operator-surfaces",
                    displayName: "Runtime Truth And Operator Surfaces",
                    description: "Projects channels, subscriptions, publication state, dispatch state, remediation advice, readiness, and claim maturity through Cephalon introspection.",
                    status: "claimed",
                    evidence: "event-channels,event-subscriptions,event-publishers,event-dispatches,event-dispatch-remediations,event-dispatch-remediation-commands,event-dispatch-runtimes,eventing-superiority-profile",
                    advantage: "Operators get one Cephalon snapshot instead of reverse-engineering the selected bus, mediator, hosted service, and outbox combination.",
                    nextGap: "Keep any new eventing claim paired with a runtime surface and snapshot field before documenting it as supported."),
                CreateEntry(
                    id: "recoverability-and-terminal-failure-posture",
                    displayName: "Recoverability And Terminal Failure Posture",
                    description: "Captures retry, retry exhaustion, skipped work, and terminal-failure posture through provider-neutral metadata.",
                    status: topology.HasDispatchRuntimeContributors || topology.HasInProcessSubscriptionExecutionPath
                        ? "claimed"
                        : topology.HasPublishingPath ? "partial" : "not-claimed",
                    evidence: topology.HasInProcessSubscriptionExecutionPath
                        ? $"in-process retry={InProcessEventingRetryPolicy.GetPolicyId(options)} attempts={InProcessEventingRetryPolicy.GetMaxAttempts(options).ToString(CultureInfo.InvariantCulture)} backoff={InProcessEventingRetryPolicy.GetBackoff(options)} jitter={InProcessEventingRetryPolicy.GetJitterPercent(options).ToString(CultureInfo.InvariantCulture)}"
                        : topology.HasDispatchRuntimeContributors ? "dispatch runtime contributors are configured." : "no managed retry runtime is active.",
                    advantage: "Retry truth is emitted as stable Cephalon metadata rather than hidden in a provider-specific error queue or middleware pipeline.",
                    nextGap: "Add provider-neutral broker dead-letter ownership before claiming full remediation parity."),
                CreateEntry(
                    id: "durability-and-outbox-portability",
                    displayName: "Durability And Outbox Portability",
                    description: "Separates staged publication, dispatch-store ownership, and provider-managed dispatch loops.",
                    status: topology.HasDispatchStore && topology.HasDispatchRuntimeContributors
                        ? "claimed"
                        : topology.HasDispatchStore ? "partial" : "not-claimed",
                    evidence: topology.HasDispatchStore
                        ? topology.HasDispatchRuntimeContributors ? "dispatch store and dispatch runtime contributor are active." : "dispatch store is available without a managed dispatch runtime."
                        : "no dispatch store is configured.",
                    advantage: "Cephalon can make outbox ownership truthful per provider instead of assuming one bus owns every delivery path.",
                    nextGap: "Broaden broker dead-letter, replay, and audit evidence only when a package owns those paths."),
                CreateEntry(
                    id: "mediator-style-in-process-low-ceremony",
                    displayName: "Mediator-style In-process Low Ceremony",
                    description: "Provides a lightweight direct execution lane for apps that need local notifications without a durable bus dependency.",
                    status: topology.HasInProcessSubscriptionExecutionPath ? "claimed" : "not-claimed",
                    evidence: topology.HasInProcessSubscriptionExecutionPath
                        ? topology.HasInProcessSubscriptionDescriptorDiscovery
                            ? "EnableInProcessSubscriptionExecution=true with registered IEventSubscriptionExecutor services and code-first descriptor discovery."
                            : "EnableInProcessSubscriptionExecution=true with registered IEventSubscriptionExecutor services."
                        : "in-process subscription execution is not enabled.",
                    advantage: "Teams get MediatR-style local dispatch while preserving the same event catalog, readiness, diagnostics, and future provider handoff seams.",
                    nextGap: topology.HasInProcessSubscriptionDescriptorDiscovery
                        ? "Add source-generated registration helpers and benchmark evidence before promoting larger-scale generated handler discovery."
                        : "Use EventSubscriptionAttribute or IEventSubscriptionDescriptorProvider on registered executors when the native path needs lower ceremony at larger scale."),
                CreateEntry(
                    id: "code-first-subscription-execution-pipeline",
                    displayName: "Code-first Subscription Execution Pipeline",
                    description: "Lets modules and hosts add type-safe middleware around native in-process subscription execution without string configuration.",
                    status: topology.SubscriptionExecutionMiddlewareCount > 0
                        ? "claimed"
                        : topology.HasInProcessSubscriptionExecutionPath ? "partial" : "not-claimed",
                    evidence: topology.HasInProcessSubscriptionExecutionPath
                        ? $"pipeline={topology.SubscriptionExecutionPipeline} middlewareCount={topology.SubscriptionExecutionMiddlewareCount.ToString(CultureInfo.InvariantCulture)}"
                        : "in-process subscription execution is not enabled.",
                    advantage: "Teams can add validation, tenancy, auditing, short-circuiting, or policy checks with MassTransit/NServiceBus-style filters while keeping publish/subscribe code-first and Wolverine-free.",
                    nextGap: topology.SubscriptionExecutionMiddlewareCount > 0
                        ? "Attach benchmark evidence and source-generated registration helpers before promoting lower-ceremony large-scale handler discovery."
                        : "Register IEventSubscriptionExecutionMiddleware services in code before claiming pipeline execution evidence."),
                CreateEntry(
                    id: "workflow-choreography-and-sagas",
                    displayName: "Workflow Choreography And Sagas",
                    description: "Keeps saga and process-manager ideas as Cephalon-owned choreography surfaces rather than bus-specific state-machine APIs.",
                    status: topology.HasSubscriptionContributors || topology.HasManagedSubscriptionExecutionBindings ? "partial" : "not-claimed",
                    evidence: topology.HasSubscriptionContributors
                        ? "declared subscriptions are available for choreography bridges."
                        : "no declared subscription contributors are active.",
                    advantage: "Choreography can move through Cephalon.Behaviors and event publication seams without forcing one saga runtime into the core pack.",
                    nextGap: "Add provider-neutral process-manager state, timeout, compensation, and correlation contracts before claiming full saga superiority."),
                CreateEntry(
                    id: "dead-letter-replay-and-remediation",
                    displayName: "Dead-letter Replay And Remediation",
                    description: "Distinguishes terminal failures from retryable failures and exposes bounded dispatch-store dead-letter intent before claiming broker queue ownership.",
                    status: topology.HasOutboxPublishingPath ? "partial" : "not-claimed",
                    evidence: topology.HasOutboxPublishingPath
                        ? "event-dispatch-remediations derives retry-pending, skipped, failed, and terminal-failure posture from reported dispatch state; supported dispatch stores expose retry-now, retry-later, skip, quarantine, and dispatch-store dead-letter commands plus bounded command-result reads."
                        : "no outbox-backed dispatch reporting path is active.",
                    advantage: "The engine can explain remediation posture without depending on Wolverine, MassTransit, NServiceBus, or a broker-specific dead-letter API.",
                    nextGap: "Ship broker dead-letter queue ownership only when a provider companion can prove that path with audit evidence."),
                CreateEntry(
                    id: "observability-compliance-and-auditability",
                    displayName: "Observability Compliance And Auditability",
                    description: "Publishes stable diagnostics, redaction-aware activity tags, runtime states, and claim maturity for review.",
                    status: "claimed",
                    evidence: "Cephalon.Eventing diagnostics convention; eventing publication activity tags; runtime metadata; claim statuses.",
                    advantage: "Audit and operations teams can see what is supported, partial, or unclaimed without reading provider internals.",
                    nextGap: "Attach benchmark results and release-scorecard evidence to each promoted claim."),
                CreateEntry(
                    id: "testability-and-benchmark-evidence",
                    displayName: "Testability And Benchmark Evidence",
                    description: "Requires every promoted capability to carry focused tests, docs, and later benchmark evidence.",
                    status: "partial",
                    evidence: "composition and hosting tests prove current runtime surfaces; benchmark evidence is still a later promotion gate.",
                    advantage: "Cephalon separates tested runtime truth from aspirational roadmap items.",
                    nextGap: "Promote repeatable performance and cold-start benchmarks for native and provider-managed eventing paths.")
            ]);
    }

    private static TechnologyRuntimeEntry CreateEntry(
        string id,
        string displayName,
        string description,
        string status,
        string evidence,
        string advantage,
        string nextGap)
    {
        return new TechnologyRuntimeEntry(
            id: id,
            displayName: displayName,
            description: description,
            metadata: new Dictionary<string, string>
            {
                ["status"] = status,
                ["referenceFrameworks"] = ReferenceFrameworks,
                ["claimPolicy"] = ClaimPolicy,
                ["runtimeEvidence"] = evidence,
                ["cephalonAdvantage"] = advantage,
                ["nextGap"] = nextGap
            });
    }
}
