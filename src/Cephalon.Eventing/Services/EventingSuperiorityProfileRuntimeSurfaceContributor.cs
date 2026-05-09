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
                    description: "Projects channels, subscriptions, publication state, dispatch state, readiness, and claim maturity through Cephalon introspection.",
                    status: "claimed",
                    evidence: "event-channels,event-subscriptions,event-publishers,event-dispatches,event-dispatch-runtimes,eventing-superiority-profile",
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
                        ? $"in-process retry={InProcessEventingRetryPolicy.GetPolicyId(options)} attempts={InProcessEventingRetryPolicy.GetMaxAttempts(options).ToString(CultureInfo.InvariantCulture)}"
                        : topology.HasDispatchRuntimeContributors ? "dispatch runtime contributors are configured." : "no managed retry runtime is active.",
                    advantage: "Retry truth is emitted as stable Cephalon metadata rather than hidden in a provider-specific error queue or middleware pipeline.",
                    nextGap: "Add provider-neutral replay/dead-letter operator commands before claiming full remediation parity."),
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
                    nextGap: "Move replay, poison-message quarantine, and delayed eligibility into shared operator contracts."),
                CreateEntry(
                    id: "mediator-style-in-process-low-ceremony",
                    displayName: "Mediator-style In-process Low Ceremony",
                    description: "Provides a lightweight direct execution lane for apps that need local notifications without a durable bus dependency.",
                    status: topology.HasInProcessSubscriptionExecutionPath ? "claimed" : "not-claimed",
                    evidence: topology.HasInProcessSubscriptionExecutionPath
                        ? "EnableInProcessSubscriptionExecution=true with registered IEventSubscriptionExecutor services."
                        : "in-process subscription execution is not enabled.",
                    advantage: "Teams get MediatR-style local dispatch while preserving the same event catalog, readiness, diagnostics, and future provider handoff seams.",
                    nextGap: "Add source-generated or descriptor-compiled handler discovery when the native path needs lower ceremony at larger scale."),
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
                    description: "Distinguishes terminal failures from retryable failures before claiming replay operations.",
                    status: topology.HasDispatchRuntimeContributors ? "partial" : "not-claimed",
                    evidence: topology.HasDispatchRuntimeContributors
                        ? "dispatch runtime metadata can report terminal failures, but replay commands are not yet provider-neutral."
                        : "no dispatch runtime is active.",
                    advantage: "The engine refuses to claim replay support until the operator command surface exists across providers.",
                    nextGap: "Ship provider-neutral dead-letter, retry-later, replay, skip, and quarantine commands with audit evidence."),
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
