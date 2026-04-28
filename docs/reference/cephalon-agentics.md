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

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-enablememory"></a>

##### `EnableMemory`

```csharp
bool EnableMemory { get; set; }
```

Gets or sets a value indicating whether agent memory features are enabled.

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

<a id="type-cephalon-agentics-services-agenttoolexecutionoutcomes"></a>

### `AgentToolExecutionOutcomes`

Defines stable outcome identifiers for agent-tool execution observations.

#### Declaration
```csharp
public static class AgentToolExecutionOutcomes
```

#### Fields

<a id="member-f-cephalon-agentics-services-agenttoolexecutionoutcomes-approvalrequired"></a>

##### `ApprovalRequired`

```csharp
const string ApprovalRequired
```

Gets the outcome identifier used when a tool run needs an approval step before execution.

<a id="member-f-cephalon-agentics-services-agenttoolexecutionoutcomes-denied"></a>

##### `Denied`

```csharp
const string Denied
```

Gets the outcome identifier used when a policy denies a tool run.

<a id="member-f-cephalon-agentics-services-agenttoolexecutionoutcomes-failed"></a>

##### `Failed`

```csharp
const string Failed
```

Gets the outcome identifier used when a tool run fails.

<a id="member-f-cephalon-agentics-services-agenttoolexecutionoutcomes-skipped"></a>

##### `Skipped`

```csharp
const string Skipped
```

Gets the outcome identifier used when a tool run is intentionally skipped.

<a id="member-f-cephalon-agentics-services-agenttoolexecutionoutcomes-started"></a>

##### `Started`

```csharp
const string Started
```

Gets the outcome identifier used when a tool run begins.

<a id="member-f-cephalon-agentics-services-agenttoolexecutionoutcomes-succeeded"></a>

##### `Succeeded`

```csharp
const string Succeeded
```

Gets the outcome identifier used when a tool run completes successfully.

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

<a id="type-cephalon-agentics-services-agenttoolexecutionrequest"></a>

### `AgentToolExecutionRequest`

Describes one request to execute an agent tool through the Cephalon-managed agentics runtime.

#### Declaration
```csharp
public sealed class AgentToolExecutionRequest
```

#### Constructors

<a id="member-m-cephalon-agentics-services-agenttoolexecutionrequest-ctor-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-int32-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AgentToolExecutionRequest`

```csharp
AgentToolExecutionRequest(string toolId, string runId, IReadOnlyDictionary<string, string> arguments, string actorId, string correlationId, int attempt, IReadOnlyDictionary<string, string> metadata)
```

Creates a new agent-tool execution request.

Parameters:
- `toolId`: The stable tool identifier to execute.
- `runId`: The stable run identifier. A generated identifier is used when omitted.
- `arguments`: Optional string arguments supplied to the tool executor.
- `actorId`: The optional actor identifier responsible for the request.
- `correlationId`: The optional correlation identifier for the request.
- `attempt`: The execution attempt number.
- `metadata`: Optional operator-facing metadata associated with the request.

#### Properties

<a id="member-p-cephalon-agentics-services-agenttoolexecutionrequest-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the optional actor identifier responsible for the request.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionrequest-arguments"></a>

##### `Arguments`

```csharp
IReadOnlyDictionary<string, string> Arguments { get; }
```

Gets optional string arguments supplied to the tool executor.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionrequest-attempt"></a>

##### `Attempt`

```csharp
int Attempt { get; }
```

Gets the execution attempt number.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the request.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata associated with the request.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionrequest-runid"></a>

##### `RunId`

```csharp
string RunId { get; }
```

Gets the stable run identifier for this execution.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionrequest-toolid"></a>

##### `ToolId`

```csharp
string ToolId { get; }
```

Gets the stable tool identifier to execute.

<a id="type-cephalon-agentics-services-agenttoolexecutionresult"></a>

### `AgentToolExecutionResult`

Describes the result returned by one managed agent-tool executor.

#### Declaration
```csharp
public sealed class AgentToolExecutionResult
```

#### Constructors

<a id="member-m-cephalon-agentics-services-agenttoolexecutionresult-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AgentToolExecutionResult`

```csharp
AgentToolExecutionResult(string outcome, string outputSummary, string error, IReadOnlyDictionary<string, string> metadata)
```

Creates a new agent-tool execution result.

Parameters:
- `outcome`: The stable execution outcome identifier.
- `outputSummary`: The optional operator-facing output summary.
- `error`: The optional operator-facing error summary.
- `metadata`: Optional metadata captured by the executor.

#### Properties

<a id="member-p-cephalon-agentics-services-agenttoolexecutionresult-error"></a>

##### `Error`

```csharp
string Error { get; }
```

Gets the optional operator-facing error summary.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional metadata captured by the executor.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable execution outcome identifier.

<a id="member-p-cephalon-agentics-services-agenttoolexecutionresult-outputsummary"></a>

##### `OutputSummary`

```csharp
string OutputSummary { get; }
```

Gets the optional operator-facing output summary.

#### Methods

<a id="member-m-cephalon-agentics-services-agenttoolexecutionresult-approvalrequired-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ApprovalRequired`

```csharp
AgentToolExecutionResult ApprovalRequired(string outputSummary, IReadOnlyDictionary<string, string> metadata)
```

Creates an approval-required execution result.

Returns: An approval-required execution result.

Parameters:
- `outputSummary`: The optional operator-facing output summary.
- `metadata`: Optional metadata captured by the policy layer.

<a id="member-m-cephalon-agentics-services-agenttoolexecutionresult-denied-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Denied`

```csharp
AgentToolExecutionResult Denied(string error, IReadOnlyDictionary<string, string> metadata)
```

Creates a denied execution result.

Returns: A denied execution result.

Parameters:
- `error`: The operator-facing denial reason.
- `metadata`: Optional metadata captured by the policy layer.

<a id="member-m-cephalon-agentics-services-agenttoolexecutionresult-failed-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Failed`

```csharp
AgentToolExecutionResult Failed(string error, IReadOnlyDictionary<string, string> metadata)
```

Creates a failed execution result.

Returns: A failed execution result.

Parameters:
- `error`: The operator-facing error summary.
- `metadata`: Optional metadata captured by the executor.

<a id="member-m-cephalon-agentics-services-agenttoolexecutionresult-skipped-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Skipped`

```csharp
AgentToolExecutionResult Skipped(string outputSummary, IReadOnlyDictionary<string, string> metadata)
```

Creates a skipped execution result.

Returns: A skipped execution result.

Parameters:
- `outputSummary`: The optional operator-facing output summary.
- `metadata`: Optional metadata captured by the executor.

<a id="member-m-cephalon-agentics-services-agenttoolexecutionresult-succeeded-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Succeeded`

```csharp
AgentToolExecutionResult Succeeded(string outputSummary, IReadOnlyDictionary<string, string> metadata)
```

Creates a successful execution result.

Returns: A successful execution result.

Parameters:
- `outputSummary`: The optional operator-facing output summary.
- `metadata`: Optional metadata captured by the executor.

<a id="type-cephalon-agentics-services-agenttoolrunstate"></a>

### `AgentToolRunState`

Describes the latest operator-facing runtime state reported for one agent-tool run.

#### Declaration
```csharp
public sealed class AgentToolRunState
```

#### Constructors

<a id="member-m-cephalon-agentics-services-agenttoolrunstate-ctor-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AgentToolRunState`

```csharp
AgentToolRunState(string ToolId, string RunId, string LastOutcome, DateTimeOffset? LastObservedAtUtc, string LastActorId, string LastCorrelationId, int LastAttempt, int StartedCount, int SucceededCount, int FailedCount, int SkippedCount, int ApprovalRequiredCount, int DeniedCount, string LastOutputSummary, string LastError, IReadOnlyDictionary<string, string> Metadata)
```

Describes the latest operator-facing runtime state reported for one agent-tool run.

Parameters:
- `ToolId`: The stable tool identifier.
- `RunId`: The stable run identifier.
- `LastOutcome`: The last reported outcome identifier when one exists.
- `LastObservedAtUtc`: The UTC timestamp when the last observation was reported.
- `LastActorId`: The actor identifier from the latest observation when one was reported.
- `LastCorrelationId`: The correlation identifier from the latest observation when one was reported.
- `LastAttempt`: The last reported execution attempt number.
- `StartedCount`: The number of `started` observations reported so far.
- `SucceededCount`: The number of `succeeded` observations reported so far.
- `FailedCount`: The number of `failed` observations reported so far.
- `SkippedCount`: The number of `skipped` observations reported so far.
- `ApprovalRequiredCount`: The number of `approval-required` observations reported so far.
- `DeniedCount`: The number of `denied` observations reported so far.
- `LastOutputSummary`: The latest operator-facing output summary when one was reported.
- `LastError`: The latest operator-facing error summary when one was reported.
- `Metadata`: The operator-facing metadata captured by the latest report.

#### Properties

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-approvalrequiredcount"></a>

##### `ApprovalRequiredCount`

```csharp
int ApprovalRequiredCount { get; set; }
```

The number of `approval-required` observations reported so far.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-deniedcount"></a>

##### `DeniedCount`

```csharp
int DeniedCount { get; set; }
```

The number of `denied` observations reported so far.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; set; }
```

The number of `failed` observations reported so far.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-isterminal"></a>

##### `IsTerminal`

```csharp
bool IsTerminal { get; }
```

Gets a value indicating whether the latest report represents a terminal outcome for this run.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-lastactorid"></a>

##### `LastActorId`

```csharp
string LastActorId { get; set; }
```

The actor identifier from the latest observation when one was reported.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-lastattempt"></a>

##### `LastAttempt`

```csharp
int LastAttempt { get; set; }
```

The last reported execution attempt number.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-lastcorrelationid"></a>

##### `LastCorrelationId`

```csharp
string LastCorrelationId { get; set; }
```

The correlation identifier from the latest observation when one was reported.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; set; }
```

The latest operator-facing error summary when one was reported.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the last observation was reported.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; set; }
```

The last reported outcome identifier when one exists.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-lastoutputsummary"></a>

##### `LastOutputSummary`

```csharp
string LastOutputSummary { get; set; }
```

The latest operator-facing output summary when one was reported.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

The operator-facing metadata captured by the latest report.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-requiresapproval"></a>

##### `RequiresApproval`

```csharp
bool RequiresApproval { get; }
```

Gets a value indicating whether the latest report says explicit approval is required before execution can continue.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-runid"></a>

##### `RunId`

```csharp
string RunId { get; set; }
```

The stable run identifier.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-skippedcount"></a>

##### `SkippedCount`

```csharp
int SkippedCount { get; set; }
```

The number of `skipped` observations reported so far.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-startedcount"></a>

##### `StartedCount`

```csharp
int StartedCount { get; set; }
```

The number of `started` observations reported so far.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-succeededcount"></a>

##### `SucceededCount`

```csharp
int SucceededCount { get; set; }
```

The number of `succeeded` observations reported so far.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-toolid"></a>

##### `ToolId`

```csharp
string ToolId { get; set; }
```

The stable tool identifier.

<a id="member-p-cephalon-agentics-services-agenttoolrunstate-totalreports"></a>

##### `TotalReports`

```csharp
int TotalReports { get; }
```

Gets the total number of observations reported for this run.

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

<a id="type-cephalon-agentics-services-iagenttooldispatcher"></a>

### `IAgentToolDispatcher`

Dispatches registered agent tools through Cephalon-managed execution, policy, and run-state services.

#### Declaration
```csharp
public interface IAgentToolDispatcher
```

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttooldispatcher-executeasync-cephalon-agentics-services-agenttoolexecutionrequest-system-threading-cancellationtoken"></a>

##### `ExecuteAsync`

```csharp
ValueTask<AgentToolExecutionResult> ExecuteAsync(AgentToolExecutionRequest request, CancellationToken cancellationToken)
```

Executes one registered agent tool.

Returns: The result produced by the managed execution path.

Parameters:
- `request`: The execution request to dispatch.
- `cancellationToken`: The cancellation token for the current execution attempt.

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

<a id="type-cephalon-agentics-services-iagenttoolruncatalog"></a>

### `IAgentToolRunCatalog`

Exposes the latest reported runtime state for agent-tool runs.

#### Declaration
```csharp
public interface IAgentToolRunCatalog
```

#### Properties

<a id="member-p-cephalon-agentics-services-iagenttoolruncatalog-runs"></a>

##### `Runs`

```csharp
IReadOnlyList<AgentToolRunState> Runs { get; }
```

Gets the currently known agent-tool run states ordered by tool identifier and run identifier.

#### Methods

<a id="member-m-cephalon-agentics-services-iagenttoolruncatalog-getbyrunid-system-string"></a>

##### `GetByRunId`

```csharp
AgentToolRunState GetByRunId(string runId)
```

Looks up one reported run-state entry by run identifier.

Returns: The current run state when one has been reported; otherwise, `null`.

Parameters:
- `runId`: The stable run identifier to resolve.

<a id="member-m-cephalon-agentics-services-iagenttoolruncatalog-getbytoolid-system-string"></a>

##### `GetByToolId`

```csharp
IReadOnlyList<AgentToolRunState> GetByToolId(string toolId)
```

Gets all reported run-state entries for one tool.

Returns: The run states reported for the tool.

Parameters:
- `toolId`: The stable tool identifier to resolve.

<a id="member-m-cephalon-agentics-services-iagenttoolruncatalog-tryget-system-string-cephalon-agentics-services-agenttoolrunstate"></a>

##### `TryGet`

```csharp
bool TryGet(string runId, out AgentToolRunState state)
```

Attempts to look up one reported run-state entry by run identifier.

Returns: `true` when one run-state entry is available; otherwise, `false`.

Parameters:
- `runId`: The stable run identifier to resolve.
- `state`: The current run state when one has been reported.

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
