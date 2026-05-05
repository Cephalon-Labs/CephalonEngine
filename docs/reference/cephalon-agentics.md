# Cephalon.Agentics

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Agentics)
## Namespaces

- `Cephalon.Agentics.Configuration`
- `Cephalon.Agentics.Registration`
- `Cephalon.Agentics.Services`

<a id="namespace-cephalon-agentics-configuration"></a>

## Namespace Cephalon.Agentics.Configuration

<a id="type-cephalon-agentics-configuration-agenticruntimeoptions"></a>

### `AgenticRuntimeOptions`

Configures the built-in agentic runtime pack.

Remarks: These options seed the host-owned part of the agentic runtime. Installed modules can still contribute additional tools through `IAgentToolContributor`.

#### Declaration
```csharp
public sealed class AgenticRuntimeOptions
```

#### Constructors

<a id="member-m-cephalon-agentics-configuration-agenticruntimeoptions-ctor"></a>

##### `AgenticRuntimeOptions`

```csharp
AgenticRuntimeOptions()
```

Creates agentic runtime options with the default host-owned features enabled.

#### Properties

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-enableexecution"></a>

##### `EnableExecution`

```csharp
bool EnableExecution { get; set; }
```

Gets or sets a value indicating whether Cephalon-managed tool dispatch and run-state features are enabled.

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-enableexecutionidempotency"></a>

##### `EnableExecutionIdempotency`

```csharp
bool EnableExecutionIdempotency { get; set; }
```

Gets or sets a value indicating whether duplicate completed run ids should be skipped inside the current process.

Remarks: This is a bounded, process-local idempotency posture. It suppresses duplicate completed tool runs observed by the in-memory run catalog without claiming durable inbox storage, cross-node deduplication, or distributed exactly-once execution.

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-enablememory"></a>

##### `EnableMemory`

```csharp
bool EnableMemory { get; set; }
```

Gets or sets a value indicating whether agent memory features are enabled.

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-executionidempotencyretentionminutes"></a>

##### `ExecutionIdempotencyRetentionMinutes`

```csharp
int ExecutionIdempotencyRetentionMinutes { get; set; }
```

Gets or sets the process-local retention window, in minutes, for completed run-id suppression.

Remarks: Values less than `1` are normalized to one minute by the dispatcher.

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-executionmaxattempts"></a>

##### `ExecutionMaxAttempts`

```csharp
int ExecutionMaxAttempts { get; set; }
```

Gets or sets the maximum number of process-local attempts for one managed tool execution.

Remarks: The default value preserves single-attempt execution. Values greater than `1` enable bounded in-process retry for executor failures without claiming durable retry queues or distributed coordination.

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-executionretrydelaymilliseconds"></a>

##### `ExecutionRetryDelayMilliseconds`

```csharp
int ExecutionRetryDelayMilliseconds { get; set; }
```

Gets or sets the optional delay, in milliseconds, before a process-local retry attempt.

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; }
```

Gets arbitrary metadata that can be attached to the agentic runtime configuration.

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-tools"></a>

##### `Tools`

```csharp
IList<AgentToolDescriptor> Tools { get; }
```

Gets the host-defined tool descriptors that should be available to the agentic runtime.

<a id="namespace-cephalon-agentics-registration"></a>

## Namespace Cephalon.Agentics.Registration

<a id="type-cephalon-agentics-registration-agenticenginebuilderextensions"></a>

### `AgenticEngineBuilderExtensions`

Registers the built-in agentic runtime pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class AgenticEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-agentics-registration-agenticenginebuilderextensions-addagentics-cephalon-engine-composition-enginebuilder-system-action-cephalon-agentics-configuration-agenticruntimeoptions"></a>

##### `AddAgentics`

```csharp
EngineBuilder AddAgentics(this EngineBuilder builder, Action<AgenticRuntimeOptions> configure)
```

Adds the agentic runtime pack to the engine.

Remarks: The pack activates only when the matching technology profile is selected, but registering it here makes its services, capabilities, and runtime surfaces available when that selection is active.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned agentic runtime options.

<a id="namespace-cephalon-agentics-services"></a>

## Namespace Cephalon.Agentics.Services

<a id="type-cephalon-agentics-services-agenticsdiagnostics"></a>

### `AgenticsDiagnostics`

Defines the stable activity source, meter, activity, counter, and tag names emitted by the agentics companion runtime. Names are sourced from `Agentics` and `Agentics` so the agentics pack and observability companion packs share one canonical name set with the rest of the engine.

#### Declaration
```csharp
public static class AgenticsDiagnostics
```

#### Fields

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-activitysourcename"></a>

##### `ActivitySourceName`

```csharp
const string ActivitySourceName
```

Gets the stable activity-source name emitted by the agentics runtime.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-actoridtag"></a>

##### `ActorIdTag`

```csharp
const string ActorIdTag
```

Stable Cephalon-prefix tag carrying the optional actor identifier emitted on the activity.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-attempttag"></a>

##### `AttemptTag`

```csharp
const string AttemptTag
```

Stable Cephalon-prefix tag carrying the requested execution attempt number emitted on the activity.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-correlationidtag"></a>

##### `CorrelationIdTag`

```csharp
const string CorrelationIdTag
```

Stable Cephalon-prefix tag carrying the optional correlation identifier emitted on the activity.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-dispatcheridtag"></a>

##### `DispatcherIdTag`

```csharp
const string DispatcherIdTag
```

Stable Cephalon-prefix tag carrying the dispatcher identifier responsible for the run.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-executionoutcometag"></a>

##### `ExecutionOutcomeTag`

```csharp
const string ExecutionOutcomeTag
```

Stable Cephalon-prefix tag carrying the terminal execution outcome emitted on the activity (succeeded, failed, skipped, approval-required, or denied).

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-metername"></a>

##### `MeterName`

```csharp
const string MeterName
```

Gets the stable meter name emitted by the agentics runtime.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-runidtag"></a>

##### `RunIdTag`

```csharp
const string RunIdTag
```

Stable Cephalon-prefix tag carrying the agent-tool run identifier emitted on the activity.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-tooldispatchactivityname"></a>

##### `ToolDispatchActivityName`

```csharp
const string ToolDispatchActivityName
```

Gets the stable activity name emitted around one managed agent-tool dispatch.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-tooldispatchcountername"></a>

##### `ToolDispatchCounterName`

```csharp
const string ToolDispatchCounterName
```

Gets the stable counter name for completed agent-tool dispatches.

<a id="member-f-cephalon-agentics-services-agenticsdiagnostics-toolidtag"></a>

##### `ToolIdTag`

```csharp
const string ToolIdTag
```

Stable Cephalon-prefix tag carrying the agent-tool identifier emitted on the activity.

<a id="type-cephalon-agentics-services-agenttooldescriptor"></a>

### `AgentToolDescriptor`

Describes a tool that can be surfaced through the agentic runtime pack.

#### Declaration
```csharp
public sealed class AgentToolDescriptor
```

#### Constructors

<a id="member-m-cephalon-agentics-services-agenttooldescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AgentToolDescriptor`

```csharp
AgentToolDescriptor(string id, string displayName, string description, IReadOnlyList<string> tags, IReadOnlyList<string> capabilityKeys, string executionGraphId, string hostedExecutionId, IReadOnlyDictionary<string, string> metadata)
```

Creates a new agent tool descriptor.

Parameters:
- `id`: The stable tool identifier.
- `displayName`: The operator-facing tool name.
- `description`: The human-readable description of the tool.
- `tags`: Optional tags that classify the tool.
- `capabilityKeys`: Optional capability keys that the tool expects to use through the active runtime.
- `executionGraphId`: The related execution-graph identifier when the tool coordinates a published orchestration flow.
- `hostedExecutionId`: The related hosted-execution identifier when the tool coordinates one host-managed background surface.
- `metadata`: Optional operator-facing metadata that should flow through the runtime surface.

#### Properties

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-capabilitykeys"></a>

##### `CapabilityKeys`

```csharp
IReadOnlyList<string> CapabilityKeys { get; }
```

Gets the capability keys that the tool expects to use through the active runtime.

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the tool.

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the tool.

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-executiongraphid"></a>

##### `ExecutionGraphId`

```csharp
string ExecutionGraphId { get; }
```

Gets the related execution-graph identifier when one is declared.

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-hostedexecutionid"></a>

##### `HostedExecutionId`

```csharp
string HostedExecutionId { get; }
```

Gets the related hosted-execution identifier when one is declared.

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable tool identifier.

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional operator-facing metadata associated with the tool.

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the tool.

<a id="type-cephalon-agentics-services-agenttoolexecutioncontext"></a>

### `AgentToolExecutionContext`

Describes the host-agnostic execution context delivered to one managed agent-tool executor.

#### Declaration
```csharp
public sealed class AgentToolExecutionContext
```

#### Constructors

<a id="member-m-cephalon-agentics-services-agenttoolexecutioncontext-ctor-cephalon-agentics-services-agenttooldescriptor-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-int32-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AgentToolExecutionContext`

```csharp
AgentToolExecutionContext(AgentToolDescriptor tool, string runId, IReadOnlyDictionary<string, string> arguments, string actorId, string correlationId, int attempt, IReadOnlyDictionary<string, string> metadata)
```

Creates a new agent-tool execution context.

Parameters:
- `tool`: The resolved tool descriptor being executed.
- `runId`: The stable run identifier for this execution.
- `arguments`: Optional string arguments supplied to the tool executor.
- `actorId`: The optional actor identifier responsible for the request.
- `correlationId`: The optional correlation identifier for the request.
- `attempt`: The execution attempt number.
- `metadata`: Optional operator-facing metadata associated with the request.

#### Properties

<a id="member-p-cephalon-agentics-services-agenttoolexecutioncontext-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the optional actor identifier responsible for the request.

<a id="member-p-cephalon-agentics-services-agenttoolexecutioncontext-arguments"></a>

##### `Arguments`

```csharp
IReadOnlyDictionary<string, string> Arguments { get; }
```

Gets optional string arguments supplied to the tool executor.

<a id="member-p-cephalon-agentics-services-agenttoolexecutioncontext-attempt"></a>

##### `Attempt`

```csharp
int Attempt { get; }
```

Gets the execution attempt number.

<a id="member-p-cephalon-agentics-services-agenttoolexecutioncontext-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the request.

<a id="member-p-cephalon-agentics-services-agenttoolexecutioncontext-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata associated with the request.

<a id="member-p-cephalon-agentics-services-agenttoolexecutioncontext-runid"></a>

##### `RunId`

```csharp
string RunId { get; }
```

Gets the stable run identifier for this execution.

<a id="member-p-cephalon-agentics-services-agenttoolexecutioncontext-tool"></a>

##### `Tool`

```csharp
AgentToolDescriptor Tool { get; }
```

Gets the resolved tool descriptor being executed.

<a id="type-cephalon-agentics-services-agenttoolexecutiondecision"></a>

### `AgentToolExecutionDecision`

Describes a policy decision for one agent-tool execution request.

#### Declaration
```csharp
public sealed class AgentToolExecutionDecision
```

#### Constructors

<a id="member-m-cephalon-agentics-services-agenttoolexecutiondecision-ctor-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AgentToolExecutionDecision`

```csharp
AgentToolExecutionDecision(string kind, string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a new agent-tool execution decision.

Parameters:
- `kind`: The stable decision identifier.
- `reason`: The operator-facing reason associated with the decision.
- `metadata`: Optional metadata captured with the decision.

#### Properties

<a id="member-p-cephalon-agentics-services-agenttoolexecutiondecision-kind"></a>

##### `Kind`

```csharp
string Kind { get; }
```

Gets the stable decision identifier.

<a id="member-p-cephalon-agentics-services-agenttoolexecutiondecision-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional metadata captured with the decision.

<a id="member-p-cephalon-agentics-services-agenttoolexecutiondecision-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing reason associated with the decision.

#### Methods

<a id="member-m-cephalon-agentics-services-agenttoolexecutiondecision-allow-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Allow`

```csharp
AgentToolExecutionDecision Allow(string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates an allow decision.

Returns: An allow decision.

Parameters:
- `reason`: The optional operator-facing reason for the decision.
- `metadata`: Optional metadata captured with the decision.

<a id="member-m-cephalon-agentics-services-agenttoolexecutiondecision-deny-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Deny`

```csharp
AgentToolExecutionDecision Deny(string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates a deny decision.

Returns: A deny decision.

Parameters:
- `reason`: The optional operator-facing reason for the decision.
- `metadata`: Optional metadata captured with the decision.

<a id="member-m-cephalon-agentics-services-agenttoolexecutiondecision-requireapproval-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `RequireApproval`

```csharp
AgentToolExecutionDecision RequireApproval(string reason, IReadOnlyDictionary<string, string> metadata)
```

Creates an approval-required decision.

Returns: An approval-required decision.

Parameters:
- `reason`: The optional operator-facing reason for the decision.
- `metadata`: Optional metadata captured with the decision.

<a id="type-cephalon-agentics-services-agenttoolexecutiondecisionkinds"></a>

### `AgentToolExecutionDecisionKinds`

Defines stable decision identifiers returned by agent-tool execution policies.

#### Declaration
```csharp
public static class AgentToolExecutionDecisionKinds
```

#### Fields

<a id="member-f-cephalon-agentics-services-agenttoolexecutiondecisionkinds-allow"></a>

##### `Allow`

```csharp
const string Allow
```

Gets the decision identifier used when execution can continue.

<a id="member-f-cephalon-agentics-services-agenttoolexecutiondecisionkinds-approvalrequired"></a>

##### `ApprovalRequired`

```csharp
const string ApprovalRequired
```

Gets the decision identifier used when execution must wait for explicit approval.

<a id="member-f-cephalon-agentics-services-agenttoolexecutiondecisionkinds-deny"></a>

##### `Deny`

```csharp
const string Deny
```

Gets the decision identifier used when execution is denied by policy.

<a id="type-cephalon-agentics-services-agenttoolexecutionreport"></a>

### `AgentToolExecutionReport`

Describes one runtime observation for an agent-tool run.

#### Declaration
```csharp
public sealed class AgentToolExecutionReport
```

#### Constructors

<a id="member-m-cephalon-agentics-services-agenttoolexecutionreport-ctor-system-string-system-string-system-string-system-datetimeoffset-system-string-system-string-system-int32-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AgentToolExecutionReport`

```csharp
AgentToolExecutionReport(string toolId, string runId, string outcome, DateTimeOffset observedAtUtc, string actorId, string correlationId, int attempt, string outputSummary, string error, IReadOnlyDictionary<string, string> metadata)
```

Creates a new runtime observation for an agent-tool run.

Parameters:
- `toolId`: The stable tool identifier.
- `runId`: The stable run identifier.
- `outcome`: The stable outcome identifier.
- `observedAtUtc`: The UTC timestamp when the observation occurred.
- `actorId`: The optional actor identifier responsible for the run.
- `correlationId`: The optional correlation identifier associated with the run.
- `attempt`: The execution attempt number.
- `outputSummary`: The optional operator-facing output summary.
- `error`: The optional operator-facing error summary.
- `metadata`: Optional operator-facing metadata captured alongside the observation.

#### Properties

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the optional actor identifier responsible for the run.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-attempt"></a>

##### `Attempt`

```csharp
int Attempt { get; }
```

Gets the execution attempt number.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier associated with the run.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-error"></a>

##### `Error`

```csharp
string Error { get; }
```

Gets the optional operator-facing error summary.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata captured alongside the observation.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; }
```

Gets the UTC timestamp when the observation occurred.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable outcome identifier.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-outputsummary"></a>

##### `OutputSummary`

```csharp
string OutputSummary { get; }
```

Gets the optional operator-facing output summary.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-runid"></a>

##### `RunId`

```csharp
string RunId { get; }
```

Gets the stable run identifier.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionreport-toolid"></a>

##### `ToolId`

```csharp
string ToolId { get; }
```

Gets the stable tool identifier.

<a id="type-cephalon-agentics-services-iagenttoolcatalog"></a>

### `IAgentToolCatalog`

Exposes the merged set of tools available to the active agentic runtime.

#### Declaration
```csharp
public interface IAgentToolCatalog
```

#### Properties

<a id="member-p-cephalon-agentics-services-iagenttoolcatalog-tools"></a>

##### `Tools`

```csharp
IReadOnlyList<AgentToolDescriptor> Tools { get; }
```

Gets the effective tool set after host options and module contributors have both been applied.

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttoolcatalog-tryget-system-string-cephalon-agentics-services-agenttooldescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string toolId, out AgentToolDescriptor tool)
```

Attempts to resolve a tool descriptor by identifier.

Returns: `true` when the tool exists; otherwise `false`.

Parameters:
- `toolId`: The tool identifier to resolve.
- `tool`: When this method returns, contains the resolved tool if found.

<a id="type-cephalon-agentics-services-iagenttoolcontributor"></a>

### `IAgentToolContributor`

Allows a module to contribute tools into the active agentic runtime pack.

#### Declaration
```csharp
public interface IAgentToolContributor
```

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttoolcontributor-registertools-cephalon-agentics-services-iagenttoolregistry"></a>

##### `RegisterTools`

```csharp
void RegisterTools(IAgentToolRegistry tools)
```

Registers one or more tool descriptors with the supplied registry.

Parameters:
- `tools`: The registry that collects contributed tool descriptors.

<a id="type-cephalon-agentics-services-iagenttoolexecutionobserver"></a>

### `IAgentToolExecutionObserver`

Observes agent-tool execution reports for audit, telemetry, or host-specific projection.

#### Declaration
```csharp
public interface IAgentToolExecutionObserver
```

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttoolexecutionobserver-observeasync-cephalon-agentics-services-agenttoolexecutionreport-system-threading-cancellationtoken"></a>

##### `ObserveAsync`

```csharp
ValueTask ObserveAsync(AgentToolExecutionReport report, CancellationToken cancellationToken)
```

Observes one execution report after it has been accepted by the runtime catalog.

Returns: A task that completes when the report has been observed.

Parameters:
- `report`: The execution report to observe.
- `cancellationToken`: The cancellation token for the observation.

<a id="type-cephalon-agentics-services-iagenttoolexecutionpolicy"></a>

### `IAgentToolExecutionPolicy`

Evaluates whether an agent-tool execution request can continue.

#### Declaration
```csharp
public interface IAgentToolExecutionPolicy
```

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttoolexecutionpolicy-evaluateasync-cephalon-agentics-services-agenttoolexecutioncontext-system-threading-cancellationtoken"></a>

##### `EvaluateAsync`

```csharp
ValueTask<AgentToolExecutionDecision> EvaluateAsync(AgentToolExecutionContext context, CancellationToken cancellationToken)
```

Evaluates one resolved agent-tool execution context.

Returns: The policy decision for this execution request.

Parameters:
- `context`: The execution context to evaluate.
- `cancellationToken`: The cancellation token for the evaluation.

<a id="type-cephalon-agentics-services-iagenttoolexecutor"></a>

### `IAgentToolExecutor`

Executes one registered agent tool through the Cephalon-managed agentics runtime.

#### Declaration
```csharp
public interface IAgentToolExecutor
```

#### Properties

<a id="member-p-cephalon-agentics-services-iagenttoolexecutor-toolid"></a>

##### `ToolId`

```csharp
string ToolId { get; }
```

Gets the stable tool identifier owned by this executor.

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttoolexecutor-executeasync-cephalon-agentics-services-agenttoolexecutioncontext-system-threading-cancellationtoken"></a>

##### `ExecuteAsync`

```csharp
ValueTask<AgentToolExecutionResult> ExecuteAsync(AgentToolExecutionContext context, CancellationToken cancellationToken)
```

Executes the tool against the supplied context.

Returns: The result reported by the executor.

Parameters:
- `context`: The host-agnostic execution context for the current tool run.
- `cancellationToken`: The cancellation token for the current execution attempt.

<a id="type-cephalon-agentics-services-iagenttoolregistry"></a>

### `IAgentToolRegistry`

Collects tool descriptors contributed to the active agentic runtime pack.

#### Declaration
```csharp
public interface IAgentToolRegistry
```

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttoolregistry-add-cephalon-agentics-services-agenttooldescriptor"></a>

##### `Add`

```csharp
void Add(AgentToolDescriptor tool)
```

Adds a tool descriptor to the registry.

Parameters:
- `tool`: The tool descriptor to contribute.

<a id="type-cephalon-agentics-services-iagenttoolrunreporter"></a>

### `IAgentToolRunReporter`

Records runtime observations for agent-tool runs.

#### Declaration
```csharp
public interface IAgentToolRunReporter
```

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttoolrunreporter-reportasync-cephalon-agentics-services-agenttoolexecutionreport-system-threading-cancellationtoken"></a>

##### `ReportAsync`

```csharp
ValueTask ReportAsync(AgentToolExecutionReport report, CancellationToken cancellationToken)
```

Records one runtime observation for an agent-tool run.

Returns: A task that completes when the observation has been recorded.

Parameters:
- `report`: The runtime observation to record.
- `cancellationToken`: The token that cancels the operation.
