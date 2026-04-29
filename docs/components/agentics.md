# Cephalon.Agentics

`Cephalon.Agentics` is the baseline technology pack for agentic workloads.

## What it owns

- agentic runtime options
- module and registration entry points for the `AgenticWorkloads` technology
- tool descriptors, registries, and catalogs
- orchestration-link validation for tool descriptors that point back to capabilities, execution graphs, or hosted executions
- Cephalon-managed agent-tool dispatch through registered executors
- bounded process-local retry for executor failures when explicitly configured
- process-local duplicate-completed run suppression when explicitly configured
- run-state reporting for tool executions, including retry-scheduled posture
- policy and observer hooks for approval, denial, audit, and operational projection
- runtime-surface contribution for introspection

## Main surfaces

- `Configuration/AgenticRuntimeOptions.cs`
- `Modules/AgenticsModule.cs`
- `Registration/AgenticEngineBuilderExtensions.cs`
- `Services/AgentToolDescriptor.cs`
- `Services/AgentToolRegistry.cs`
- `Services/AgentToolCatalog.cs`
- `Services/IAgentToolContributor.cs`
- `Services/IAgentToolCatalog.cs`
- `Services/AgentToolDispatcher.cs`
- `Services/IAgentToolExecutor.cs`
- `Services/IAgentToolExecutionPolicy.cs`
- `Services/IAgentToolExecutionObserver.cs`
- `Services/IAgentToolRunReporter.cs`
- `Services/AgentToolExecutionContext.cs`
- `Services/AgentToolExecutionReport.cs`
- `Services/AgenticsRuntimeSurfaceContributor.cs`

Shared read/action contracts:

- `Cephalon.Abstractions.Agentics/AgentToolExecutionOutcomes.cs`
- `Cephalon.Abstractions.Agentics/AgentToolExecutionRequest.cs`
- `Cephalon.Abstractions.Agentics/AgentToolExecutionResult.cs`
- `Cephalon.Abstractions.Agentics/AgentToolRunState.cs`
- `Cephalon.Abstractions.Agentics/IAgentToolDispatcher.cs`
- `Cephalon.Abstractions.Agentics/IAgentToolRunCatalog.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack is the reference pattern for future AI or agent runtime behavior in Cephalon. Modules can contribute tools without forcing the host to own the entire catalog, and those tools can stay grounded in the existing runtime model by linking back to published capability keys, execution graphs, and hosted executions.

When `AgenticRuntimeOptions.EnableExecution` is enabled, the pack also owns a narrow managed execution lane:

- the abstraction-level `IAgentToolDispatcher` contract resolves the selected `AgentToolDescriptor`, reports a `started` observation, evaluates `IAgentToolExecutionPolicy` hooks, invokes the matching `IAgentToolExecutor`, and reports the final outcome through the `Cephalon.Agentics` implementation.
- when `AgenticRuntimeOptions.ExecutionMaxAttempts` is greater than `1`, the dispatcher retries failed executor attempts inside the same process, reports `retry-scheduled` observations, and publishes `retryPolicy = bounded-in-process`, max-attempt, delay, durability, scope, and next-attempt metadata without claiming durable retry queues.
- when `AgenticRuntimeOptions.EnableExecutionIdempotency` is enabled, the dispatcher suppresses duplicate `toolId + runId` executions that already completed successfully inside the current process, reports the duplicate as `skipped`, and publishes `idempotencyPolicy = completed-run`, `idempotencyKey = tool-run`, retention, durability, scope, and `idempotencyOutcome = duplicate-skipped` metadata without claiming a durable inbox, broker deduplication, cross-node exactly-once delivery, or provider AI orchestration.
- `IAgentToolRunCatalog` exposes the latest run-state truth for each tool run through `Cephalon.Abstractions.Agentics`, including outcome counts, retry-scheduled counts, skipped counts, actor/correlation details, approval-required posture, retry-pending posture, duplicate-completed posture, terminal-state posture, and the latest operator metadata.
- `IAgentToolExecutionObserver` receives every report after it is recorded so modules can attach audit, telemetry, or projection behavior without replacing the dispatcher.

The resulting operator-facing answer flows through `/engine/technology-surfaces`,
`/engine/agent-tool-runs`, `/engine/agent-tool-runs/retry-pending`,
`/engine/agent-tool-runs/idempotency-duplicates`, `/engine/agent-tool-runs/{runId}`,
`/engine/agent-tool-runs/by-tool/{toolId}`, `POST /engine/agent-tools/{toolId}/runs`, and
`snapshot.AgentToolRuns`. Tool entries now include
execution readiness (`executionEnabled`, `executionOwnership`, `executorConfigured`,
`executorCount`) and run-state metadata (`runtimeState`, `runCount`, `lastOutcome`,
`retryPolicy`, `retryMaxAttempts`, `retryDelayMilliseconds`, `retryScheduledCount`,
`retryPending`, `idempotencyPolicy`, `idempotencyKey`, `idempotencyRetentionMinutes`,
`idempotencyDurability`, `idempotencyScope`, `duplicateCompleted`, `totalReports`,
approval/denial counters, and `reported.*` metadata). A tool without a registered executor remains
truthful as `awaiting-executor` rather than being described as fully managed.

ASP.NET Core hosts can trigger one bounded managed run through
`POST /engine/agent-tools/{toolId}/runs` without referencing `Cephalon.Agentics` implementation
types. The route accepts optional run id, actor id, correlation id, attempt, string arguments, and
metadata, adds safe `trigger` and `route` metadata, and returns `404` when the active runtime does
not expose the dispatcher or the selected tool does not exist.

This is still intentionally not an autonomous agent planner, durable retry queue, durable inbox,
memory store, distributed scheduler, cross-node exactly-once layer, or provider-specific AI
orchestration layer. Those should land only when a package owns those runtime paths explicitly.

The showcase sample now includes `ShowcaseAgenticsModule`, which contributes a catalog-inspection tool, a matching executor, an approval policy, and an observer hook so the dispatcher/run-state loop is proven end to end in an adoption-quality host.

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
