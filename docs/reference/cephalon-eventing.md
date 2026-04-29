# Cephalon.Eventing

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Eventing)
## Namespaces

- `Cephalon.Eventing.Configuration`
- `Cephalon.Eventing.Registration`
- `Cephalon.Eventing.Services`

<a id="namespace-cephalon-eventing-configuration"></a>

## Namespace Cephalon.Eventing.Configuration

<a id="type-cephalon-eventing-configuration-eventingoptions"></a>

### `EventingOptions`

Configures the built-in eventing runtime pack.

Remarks: These options seed the host-owned part of the eventing runtime. Installed modules can still contribute additional channels through `IEventChannelContributor` and additional subscription descriptors through `IEventSubscriptionContributor`.

#### Declaration
```csharp
public sealed class EventingOptions
```

#### Constructors

<a id="member-m-cephalon-eventing-configuration-eventingoptions-ctor"></a>

##### `EventingOptions`

```csharp
EventingOptions()
```

Creates eventing options with the default host-owned features enabled.

#### Properties

<a id="member-p-cephalon-eventing-configuration-eventingoptions-channels"></a>

##### `Channels`

```csharp
IList<EventChannelDescriptor> Channels { get; }
```

Gets the host-defined event channels that should be available to the eventing runtime.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-continueinprocesssubscriptionexecutionafterfailure"></a>

##### `ContinueInProcessSubscriptionExecutionAfterFailure`

```csharp
bool ContinueInProcessSubscriptionExecutionAfterFailure { get; set; }
```

Gets or sets a value indicating whether the in-process publisher should continue executing later subscriptions on the same channel after one subscription fails.

Remarks: The publisher still reports failed subscriptions and throws after the publication attempt finishes. This setting only controls whether independent subscriptions on the same channel get a chance to run before the failure is returned to the caller.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-enableinprocesssubscriptionexecution"></a>

##### `EnableInProcessSubscriptionExecution`

```csharp
bool EnableInProcessSubscriptionExecution { get; set; }
```

Gets or sets a value indicating whether the core eventing pack should execute matching subscription executors directly inside the current process when a publication is accepted.

Remarks: This is an opt-in managed execution baseline for lightweight hosts and tests. It is not a durable broker, inbox, or retry runtime; companion packs should still own those richer delivery guarantees when they are selected.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-enableinprocesssubscriptionidempotency"></a>

##### `EnableInProcessSubscriptionIdempotency`

```csharp
bool EnableInProcessSubscriptionIdempotency { get; set; }
```

Gets or sets a value indicating whether the direct in-process publisher should suppress duplicate completed subscription executions for the same publication identifier.

Remarks: This is a bounded process-local guard for lightweight hosts. It records only successful direct executions in memory and skips later duplicate `subscriptionId + publicationId` pairs while the entry remains in the retention window. It is not a durable inbox, cross-node idempotency store, or broker-owned exactly-once guarantee.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-enablepublishing"></a>

##### `EnablePublishing`

```csharp
bool EnablePublishing { get; set; }
```

Gets or sets a value indicating whether publishing features are enabled.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-enablesubscriptions"></a>

##### `EnableSubscriptions`

```csharp
bool EnableSubscriptions { get; set; }
```

Gets or sets a value indicating whether subscription features are enabled.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionidempotencyretentionminutes"></a>

##### `InProcessSubscriptionIdempotencyRetentionMinutes`

```csharp
int InProcessSubscriptionIdempotencyRetentionMinutes { get; set; }
```

Gets or sets the number of minutes that successful direct in-process subscription executions remain eligible for duplicate suppression.

Remarks: The default value is `60` minutes. The value is used only when `EnableInProcessSubscriptionIdempotency` is enabled.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionmaxattempts"></a>

##### `InProcessSubscriptionMaxAttempts`

```csharp
int InProcessSubscriptionMaxAttempts { get; set; }
```

Gets or sets the maximum number of direct in-process execution attempts per matching subscription.

Remarks: The default value of `1` preserves the no-retry baseline. Values greater than `1` enable a bounded, process-local retry loop; this still does not provide durable broker, inbox, or distributed retry guarantees.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionretrydelaymilliseconds"></a>

##### `InProcessSubscriptionRetryDelayMilliseconds`

```csharp
int InProcessSubscriptionRetryDelayMilliseconds { get; set; }
```

Gets or sets the delay in milliseconds before the direct in-process publisher retries a failed subscription attempt.

Remarks: The delay is applied only when `InProcessSubscriptionMaxAttempts` is greater than `1`. The default value of `0` retries immediately and is useful for tests and lightweight process-local remediation paths.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-subscriptions"></a>

##### `Subscriptions`

```csharp
IList<EventSubscriptionDescriptor> Subscriptions { get; }
```

Gets the host-defined event subscription descriptors that should be available to the eventing runtime.

<a id="namespace-cephalon-eventing-registration"></a>

## Namespace Cephalon.Eventing.Registration

<a id="type-cephalon-eventing-registration-eventingenginebuilderextensions"></a>

### `EventingEngineBuilderExtensions`

Registers the built-in eventing runtime pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class EventingEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-eventing-registration-eventingenginebuilderextensions-addeventing-cephalon-engine-composition-enginebuilder-system-action-cephalon-eventing-configuration-eventingoptions"></a>

##### `AddEventing`

```csharp
EngineBuilder AddEventing(this EngineBuilder builder, Action<EventingOptions> configure)
```

Adds the eventing runtime pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned eventing options.

<a id="namespace-cephalon-eventing-services"></a>

## Namespace Cephalon.Eventing.Services

<a id="type-cephalon-eventing-services-eventchanneldescriptor"></a>

### `EventChannelDescriptor`

Describes an event channel that can be surfaced through the eventing runtime pack.

#### Declaration
```csharp
public sealed class EventChannelDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventchanneldescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `EventChannelDescriptor`

```csharp
EventChannelDescriptor(string id, string displayName, string description, IReadOnlyList<string> tags)
```

Creates a new event channel descriptor.

Parameters:
- `id`: The stable channel identifier.
- `displayName`: The operator-facing channel name.
- `description`: The human-readable description of the channel.
- `tags`: Optional tags that classify the channel.

#### Properties

<a id="member-p-cephalon-eventing-services-eventchanneldescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the channel.

<a id="member-p-cephalon-eventing-services-eventchanneldescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the channel.

<a id="member-p-cephalon-eventing-services-eventchanneldescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable channel identifier.

<a id="member-p-cephalon-eventing-services-eventchanneldescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the channel.

<a id="type-cephalon-eventing-services-eventdispatchexecutionoutcomes"></a>

### `EventDispatchExecutionOutcomes`

Defines the stable outcome identifiers used when reporting durable event-dispatch activity.

#### Declaration
```csharp
public static class EventDispatchExecutionOutcomes
```

#### Fields

<a id="member-f-cephalon-eventing-services-eventdispatchexecutionoutcomes-failed"></a>

##### `Failed`

```csharp
const string Failed
```

Gets the outcome identifier used when dispatch fails for one staged message.

<a id="member-f-cephalon-eventing-services-eventdispatchexecutionoutcomes-retryscheduled"></a>

##### `RetryScheduled`

```csharp
const string RetryScheduled
```

Gets the outcome identifier used when dispatch schedules or expects another retry attempt.

<a id="member-f-cephalon-eventing-services-eventdispatchexecutionoutcomes-skipped"></a>

##### `Skipped`

```csharp
const string Skipped
```

Gets the outcome identifier used when dispatch intentionally skips one staged message.

<a id="member-f-cephalon-eventing-services-eventdispatchexecutionoutcomes-started"></a>

##### `Started`

```csharp
const string Started
```

Gets the outcome identifier used when dispatch begins for one staged message.

<a id="member-f-cephalon-eventing-services-eventdispatchexecutionoutcomes-succeeded"></a>

##### `Succeeded`

```csharp
const string Succeeded
```

Gets the outcome identifier used when dispatch completes successfully for one staged message.

<a id="type-cephalon-eventing-services-eventdispatchexecutionreport"></a>

### `EventDispatchExecutionReport`

Describes one reported dispatch observation for a durable event publication path.

#### Declaration
```csharp
public sealed class EventDispatchExecutionReport
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventdispatchexecutionreport-ctor-system-string-system-string-system-string-system-datetimeoffset-system-string-system-int32-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventDispatchExecutionReport`

```csharp
EventDispatchExecutionReport(string outboxId, string channelId, string outcome, DateTimeOffset observedAtUtc, string messageId, int attempt, string error, IReadOnlyDictionary<string, string> metadata)
```

Creates a new dispatch observation report.

Parameters:
- `outboxId`: The stable outbox identifier that owns the dispatch path.
- `channelId`: The stable channel identifier for the dispatched event.
- `outcome`: The stable outcome identifier, such as `started` or `retry-scheduled`.
- `observedAtUtc`: The UTC timestamp when the observation occurred.
- `messageId`: The stable outbound message identifier when available.
- `attempt`: The dispatch attempt number for this observation.
- `error`: The operator-facing error summary when the observation represents a failure.
- `metadata`: Optional operator-facing metadata captured alongside the observation.

#### Properties

<a id="member-p-cephalon-eventing-services-eventdispatchexecutionreport-attempt"></a>

##### `Attempt`

```csharp
int Attempt { get; }
```

Gets the dispatch attempt number associated with this observation.

<a id="member-p-cephalon-eventing-services-eventdispatchexecutionreport-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; }
```

Gets the stable channel identifier for the dispatched event.

<a id="member-p-cephalon-eventing-services-eventdispatchexecutionreport-error"></a>

##### `Error`

```csharp
string Error { get; }
```

Gets the operator-facing error summary when the observation represents a failure.

<a id="member-p-cephalon-eventing-services-eventdispatchexecutionreport-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; }
```

Gets the stable outbound message identifier when one was reported.

<a id="member-p-cephalon-eventing-services-eventdispatchexecutionreport-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata captured alongside the observation.

<a id="member-p-cephalon-eventing-services-eventdispatchexecutionreport-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; }
```

Gets the UTC timestamp when the observation occurred.

<a id="member-p-cephalon-eventing-services-eventdispatchexecutionreport-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; }
```

Gets the stable outbox identifier that owns the dispatch path.

<a id="member-p-cephalon-eventing-services-eventdispatchexecutionreport-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable outcome identifier for the observed dispatch activity.

<a id="type-cephalon-eventing-services-eventdispatchitem"></a>

### `EventDispatchItem`

Describes one staged outbound event that is waiting for dispatch through the active eventing runtime.

#### Declaration
```csharp
public sealed class EventDispatchItem
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventdispatchitem-ctor-system-string-system-string-system-string-system-string-system-string-system-datetimeoffset-system-datetimeoffset-system-int32-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventDispatchItem`

```csharp
EventDispatchItem(string outboxId, string messageId, string channelId, string eventType, string payload, DateTimeOffset occurredAtUtc, DateTimeOffset createdAtUtc, int dispatchAttemptCount, string contentType, string correlationId, string tenantId, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> metadata)
```

Creates a new dispatch item.

Parameters:
- `outboxId`: The stable outbox identifier that owns the staged message.
- `messageId`: The stable outbound message identifier.
- `channelId`: The logical channel or destination identifier.
- `eventType`: The logical event type identifier.
- `payload`: The serialized payload that should be dispatched.
- `occurredAtUtc`: The time at which the event became visible to the outbox.
- `createdAtUtc`: The time at which the durable outbox row was created.
- `dispatchAttemptCount`: The number of dispatch attempts already recorded for this message.
- `contentType`: The payload content type when one is known.
- `correlationId`: The correlation identifier associated with the message.
- `tenantId`: The tenant identifier associated with the message.
- `headers`: Optional message headers associated with the dispatch item.
- `metadata`: Optional message metadata associated with the dispatch item.

#### Properties

<a id="member-p-cephalon-eventing-services-eventdispatchitem-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; }
```

Gets the logical channel or destination identifier.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; }
```

Gets the payload content type when one is known.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier associated with the message.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-createdatutc"></a>

##### `CreatedAtUtc`

```csharp
DateTimeOffset CreatedAtUtc { get; }
```

Gets the time at which the durable outbox row was created.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-dispatchattemptcount"></a>

##### `DispatchAttemptCount`

```csharp
int DispatchAttemptCount { get; }
```

Gets the number of dispatch attempts already recorded for this message.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-eventtype"></a>

##### `EventType`

```csharp
string EventType { get; }
```

Gets the logical event type identifier.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; }
```

Gets optional message headers associated with the dispatch item.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; }
```

Gets the stable outbound message identifier.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional message metadata associated with the dispatch item.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; }
```

Gets the time at which the event became visible to the outbox.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; }
```

Gets the stable outbox identifier that owns the staged message.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-payload"></a>

##### `Payload`

```csharp
string Payload { get; }
```

Gets the serialized payload that should be dispatched.

<a id="member-p-cephalon-eventing-services-eventdispatchitem-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the message.

<a id="type-cephalon-eventing-services-eventpublication"></a>

### `EventPublication`

Describes one integration event publication request handled by the active eventing runtime.

#### Declaration
```csharp
public sealed class EventPublication
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventpublication-ctor-system-string-system-string-system-string-system-string-system-datetimeoffset-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventPublication`

```csharp
EventPublication(string id, string channelId, string eventType, string payload, DateTimeOffset occurredAtUtc, string contentType, string correlationId, string tenantId, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> metadata)
```

Creates a new event publication request.

Parameters:
- `id`: The stable publication identifier.
- `channelId`: The logical channel or destination identifier.
- `eventType`: The logical event type identifier.
- `payload`: The serialized event payload.
- `occurredAtUtc`: The time at which the event occurred.
- `contentType`: The payload content type when one is known.
- `correlationId`: The correlation identifier associated with the event.
- `tenantId`: The tenant identifier associated with the event.
- `headers`: Optional event headers.
- `metadata`: Optional event metadata.

#### Properties

<a id="member-p-cephalon-eventing-services-eventpublication-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; }
```

Gets the logical channel or destination identifier.

<a id="member-p-cephalon-eventing-services-eventpublication-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; }
```

Gets the payload content type when one is known.

<a id="member-p-cephalon-eventing-services-eventpublication-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier associated with the event.

<a id="member-p-cephalon-eventing-services-eventpublication-eventtype"></a>

##### `EventType`

```csharp
string EventType { get; }
```

Gets the logical event type identifier.

<a id="member-p-cephalon-eventing-services-eventpublication-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; }
```

Gets the event headers associated with the publication.

<a id="member-p-cephalon-eventing-services-eventpublication-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable publication identifier.

<a id="member-p-cephalon-eventing-services-eventpublication-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets the event metadata associated with the publication.

<a id="member-p-cephalon-eventing-services-eventpublication-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; }
```

Gets the time at which the event occurred.

<a id="member-p-cephalon-eventing-services-eventpublication-payload"></a>

##### `Payload`

```csharp
string Payload { get; }
```

Gets the serialized event payload.

<a id="member-p-cephalon-eventing-services-eventpublication-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the event.

<a id="type-cephalon-eventing-services-eventsubscriptiondescriptor"></a>

### `EventSubscriptionDescriptor`

Describes one declared event subscription available to the active eventing runtime.

#### Declaration
```csharp
public sealed class EventSubscriptionDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventsubscriptiondescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventSubscriptionDescriptor`

```csharp
EventSubscriptionDescriptor(string id, string displayName, string description, string channelId, string handlerId, string deliveryMode, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new event subscription descriptor.

Parameters:
- `id`: The stable subscription identifier.
- `displayName`: The operator-facing subscription name.
- `description`: The human-readable description of the subscription.
- `channelId`: The logical event channel that the subscription consumes.
- `handlerId`: The logical handler or consumer identifier that receives the event.
- `deliveryMode`: The declared delivery mode for the subscription.
- `tags`: Optional tags that classify the subscription.
- `metadata`: Optional subscription metadata.

#### Properties

<a id="member-p-cephalon-eventing-services-eventsubscriptiondescriptor-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; }
```

Gets the logical event channel that the subscription consumes.

<a id="member-p-cephalon-eventing-services-eventsubscriptiondescriptor-deliverymode"></a>

##### `DeliveryMode`

```csharp
string DeliveryMode { get; }
```

Gets the declared delivery mode for the subscription.

<a id="member-p-cephalon-eventing-services-eventsubscriptiondescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the subscription.

<a id="member-p-cephalon-eventing-services-eventsubscriptiondescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the subscription.

<a id="member-p-cephalon-eventing-services-eventsubscriptiondescriptor-handlerid"></a>

##### `HandlerId`

```csharp
string HandlerId { get; }
```

Gets the logical handler or consumer identifier that receives the event.

<a id="member-p-cephalon-eventing-services-eventsubscriptiondescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable subscription identifier.

<a id="member-p-cephalon-eventing-services-eventsubscriptiondescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets normalized metadata associated with the subscription.

<a id="member-p-cephalon-eventing-services-eventsubscriptiondescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the subscription.

<a id="type-cephalon-eventing-services-eventsubscriptionexecutionbindingdescriptor"></a>

### `EventSubscriptionExecutionBindingDescriptor`

Describes how a declared event subscription binds to a managed execution runtime.

#### Declaration
```csharp
public sealed class EventSubscriptionExecutionBindingDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventsubscriptionexecutionbindingdescriptor-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventSubscriptionExecutionBindingDescriptor`

```csharp
EventSubscriptionExecutionBindingDescriptor(string subscriptionId, string executionRuntimeId, string executionOwnership, string executionMode, IReadOnlyDictionary<string, string> metadata)
```

Creates a new managed execution binding descriptor for a declared event subscription.

Parameters:
- `subscriptionId`: The stable declared subscription identifier.
- `executionRuntimeId`: The operator-facing managed execution-runtime identifier.
- `executionOwnership`: The operator-facing ownership mode for the execution runtime.
- `executionMode`: The operator-facing execution mode for the binding.
- `metadata`: Optional operator-facing metadata associated with the binding.

#### Properties

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionbindingdescriptor-executionmode"></a>

##### `ExecutionMode`

```csharp
string ExecutionMode { get; }
```

Gets the operator-facing execution mode for the binding.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionbindingdescriptor-executionownership"></a>

##### `ExecutionOwnership`

```csharp
string ExecutionOwnership { get; }
```

Gets the operator-facing ownership mode for the managed execution runtime.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionbindingdescriptor-executionruntimeid"></a>

##### `ExecutionRuntimeId`

```csharp
string ExecutionRuntimeId { get; }
```

Gets the operator-facing managed execution-runtime identifier.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionbindingdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the binding.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionbindingdescriptor-subscriptionid"></a>

##### `SubscriptionId`

```csharp
string SubscriptionId { get; }
```

Gets the stable declared subscription identifier.

<a id="type-cephalon-eventing-services-eventsubscriptionexecutioncontext"></a>

### `EventSubscriptionExecutionContext`

Describes the host-agnostic execution context delivered to a managed event-subscription executor.

#### Declaration
```csharp
public sealed class EventSubscriptionExecutionContext
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventsubscriptionexecutioncontext-ctor-cephalon-eventing-services-eventsubscriptiondescriptor-cephalon-eventing-services-eventpublication-system-int32-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventSubscriptionExecutionContext`

```csharp
EventSubscriptionExecutionContext(EventSubscriptionDescriptor subscription, EventPublication publication, int attempt, IReadOnlyDictionary<string, string> metadata)
```

Creates a new managed event-subscription execution context.

Parameters:
- `subscription`: The declared subscription that is being executed.
- `publication`: The staged publication being delivered to the subscription.
- `attempt`: The current managed execution attempt.
- `metadata`: Optional operator-facing metadata associated with the current execution attempt.

#### Properties

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutioncontext-attempt"></a>

##### `Attempt`

```csharp
int Attempt { get; }
```

Gets the current managed execution attempt number.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutioncontext-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the current execution attempt.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutioncontext-publication"></a>

##### `Publication`

```csharp
EventPublication Publication { get; }
```

Gets the staged publication being delivered to the managed subscription.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutioncontext-subscription"></a>

##### `Subscription`

```csharp
EventSubscriptionDescriptor Subscription { get; }
```

Gets the declared subscription that is currently being executed.

<a id="type-cephalon-eventing-services-eventsubscriptionexecutionoutcomes"></a>

### `EventSubscriptionExecutionOutcomes`

Defines the stable outcome identifiers used when reporting declared event-subscription activity.

#### Declaration
```csharp
public static class EventSubscriptionExecutionOutcomes
```

#### Fields

<a id="member-f-cephalon-eventing-services-eventsubscriptionexecutionoutcomes-failed"></a>

##### `Failed`

```csharp
const string Failed
```

Gets the outcome identifier used when subscription handling fails for one message.

<a id="member-f-cephalon-eventing-services-eventsubscriptionexecutionoutcomes-retryscheduled"></a>

##### `RetryScheduled`

```csharp
const string RetryScheduled
```

Gets the outcome identifier used when subscription handling schedules or expects another retry attempt.

<a id="member-f-cephalon-eventing-services-eventsubscriptionexecutionoutcomes-skipped"></a>

##### `Skipped`

```csharp
const string Skipped
```

Gets the outcome identifier used when subscription handling intentionally skips one message.

<a id="member-f-cephalon-eventing-services-eventsubscriptionexecutionoutcomes-started"></a>

##### `Started`

```csharp
const string Started
```

Gets the outcome identifier used when subscription handling begins for one message.

<a id="member-f-cephalon-eventing-services-eventsubscriptionexecutionoutcomes-succeeded"></a>

##### `Succeeded`

```csharp
const string Succeeded
```

Gets the outcome identifier used when subscription handling completes successfully for one message.

<a id="type-cephalon-eventing-services-eventsubscriptionexecutionreport"></a>

### `EventSubscriptionExecutionReport`

Describes one application-managed execution observation for a declared event subscription.

#### Declaration
```csharp
public sealed class EventSubscriptionExecutionReport
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventsubscriptionexecutionreport-ctor-system-string-system-string-system-datetimeoffset-system-string-system-int32-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventSubscriptionExecutionReport`

```csharp
EventSubscriptionExecutionReport(string subscriptionId, string outcome, DateTimeOffset observedAtUtc, string messageId, int attempt, string error, IReadOnlyDictionary<string, string> metadata)
```

Creates a new execution report for a declared event subscription.

Parameters:
- `subscriptionId`: The stable declared subscription identifier.
- `outcome`: The stable outcome identifier, such as `started` or `retry-scheduled`.
- `observedAtUtc`: The UTC timestamp when the observation occurred.
- `messageId`: The stable inbound message identifier when available.
- `attempt`: The application-managed attempt number for this observation.
- `error`: The operator-facing error summary when the observation represents a failure.
- `metadata`: Optional operator-facing metadata captured alongside the observation.

#### Properties

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionreport-attempt"></a>

##### `Attempt`

```csharp
int Attempt { get; }
```

Gets the application-managed attempt number associated with this observation.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionreport-error"></a>

##### `Error`

```csharp
string Error { get; }
```

Gets the operator-facing error summary when the observation represents a failure.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionreport-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; }
```

Gets the stable inbound message identifier when one was reported.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionreport-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata captured alongside the observation.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionreport-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; }
```

Gets the UTC timestamp when the observation occurred.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionreport-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable outcome identifier for the observed subscription activity.

<a id="member-p-cephalon-eventing-services-eventsubscriptionexecutionreport-subscriptionid"></a>

##### `SubscriptionId`

```csharp
string SubscriptionId { get; }
```

Gets the stable declared subscription identifier.

<a id="type-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys"></a>

### `EventSubscriptionRuntimeMetadataKeys`

Defines stable metadata keys used by the event-subscriptions runtime surface.

Remarks: These keys appear in the `event-subscriptions` technology runtime surface so operators and companion packs can distinguish descriptor-only, application-managed, hosted-execution-linked, and runtime-bound subscription paths without parsing provider-specific metadata.

#### Declaration
```csharp
public static class EventSubscriptionRuntimeMetadataKeys
```

#### Fields

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-bindingmetadatakeys"></a>

##### `BindingMetadataKeys`

```csharp
const string BindingMetadataKeys
```

Identifies the comma-separated metadata keys contributed by the managed execution binding.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-bindingmetadataprefix"></a>

##### `BindingMetadataPrefix`

```csharp
const string BindingMetadataPrefix
```

Prefix for individual managed execution-binding metadata entries.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-channelid"></a>

##### `ChannelId`

```csharp
const string ChannelId
```

Identifies the logical event channel consumed by the declared subscription.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-deliverymode"></a>

##### `DeliveryMode`

```csharp
const string DeliveryMode
```

Identifies the declared delivery mode for the subscription.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-dispatchruntime"></a>

##### `DispatchRuntime`

```csharp
const string DispatchRuntime
```

Identifies who owns the dispatch path feeding subscription execution.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-executiongraphid"></a>

##### `ExecutionGraphId`

```csharp
const string ExecutionGraphId
```

Identifies the execution graph linked to the subscription's hosted execution.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-executionmode"></a>

##### `ExecutionMode`

```csharp
const string ExecutionMode
```

Identifies the execution mode used by the managed subscription binding.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-executionownership"></a>

##### `ExecutionOwnership`

```csharp
const string ExecutionOwnership
```

Identifies who owns the real subscription execution path.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-executionpath"></a>

##### `ExecutionPath`

```csharp
const string ExecutionPath
```

Identifies whether an execution path is currently bound, linked, or observed.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-executionreadiness"></a>

##### `ExecutionReadiness`

```csharp
const string ExecutionReadiness
```

Identifies the execution-readiness state derived from managed bindings, hosted execution links, or runtime observations.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-executionreadinessreasons"></a>

##### `ExecutionReadinessReasons`

```csharp
const string ExecutionReadinessReasons
```

Identifies the comma-separated reasons that explain the execution-readiness state.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-executionruntimeid"></a>

##### `ExecutionRuntimeId`

```csharp
const string ExecutionRuntimeId
```

Identifies the managed execution-runtime identifier bound to the subscription.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-handlerid"></a>

##### `HandlerId`

```csharp
const string HandlerId
```

Identifies the logical handler or consumer declared for the subscription.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-hostedexecutionid"></a>

##### `HostedExecutionId`

```csharp
const string HostedExecutionId
```

Identifies the single hosted execution linked to the declared subscription when exactly one exists.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-hostedexecutionids"></a>

##### `HostedExecutionIds`

```csharp
const string HostedExecutionIds
```

Identifies all hosted executions linked to the declared subscription.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-inbox"></a>

##### `Inbox`

```csharp
const string Inbox
```

Identifies whether an inbox is available for the subscription's channel.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-inboxids"></a>

##### `InboxIds`

```csharp
const string InboxIds
```

Identifies the linked inbox identifiers that can observe the subscription channel.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-inboxlink"></a>

##### `InboxLink`

```csharp
const string InboxLink
```

Identifies who owns the inbox linkage for the subscription's channel.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-lastoutcome"></a>

##### `LastOutcome`

```csharp
const string LastOutcome
```

Identifies the latest reported subscription execution outcome.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-reportedmetadataprefix"></a>

##### `ReportedMetadataPrefix`

```csharp
const string ReportedMetadataPrefix
```

Prefix for individual runtime-observation metadata entries.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-retrypending"></a>

##### `RetryPending`

```csharp
const string RetryPending
```

Identifies whether the latest runtime observation says a retry is pending.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-runtimestate"></a>

##### `RuntimeState`

```csharp
const string RuntimeState
```

Identifies whether runtime observations have been reported for the subscription.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-subscriptionruntime"></a>

##### `SubscriptionRuntime`

```csharp
const string SubscriptionRuntime
```

Identifies the subscription execution posture, such as application-managed or runtime-bound.

<a id="type-cephalon-eventing-services-eventsubscriptionruntimestate"></a>

### `EventSubscriptionRuntimeState`

Describes the latest operator-facing runtime state reported for one declared event subscription.

#### Declaration
```csharp
public sealed class EventSubscriptionRuntimeState
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventsubscriptionruntimestate-ctor-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventSubscriptionRuntimeState`

```csharp
EventSubscriptionRuntimeState(string SubscriptionId, string LastOutcome, DateTimeOffset? LastObservedAtUtc, string LastMessageId, int LastAttempt, int StartedCount, int SucceededCount, int FailedCount, int RetryScheduledCount, int SkippedCount, string LastError, IReadOnlyDictionary<string, string> Metadata)
```

Describes the latest operator-facing runtime state reported for one declared event subscription.

Parameters:
- `SubscriptionId`: The stable declared subscription identifier.
- `LastOutcome`: The last reported outcome identifier when one exists.
- `LastObservedAtUtc`: The UTC timestamp when the last observation was reported.
- `LastMessageId`: The last stable inbound message identifier when one was reported.
- `LastAttempt`: The last reported application-managed attempt number.
- `StartedCount`: The number of `started` observations reported so far.
- `SucceededCount`: The number of `succeeded` observations reported so far.
- `FailedCount`: The number of `failed` observations reported so far.
- `RetryScheduledCount`: The number of `retry-scheduled` observations reported so far.
- `SkippedCount`: The number of `skipped` observations reported so far.
- `LastError`: The last operator-facing error summary when a failure was reported.
- `Metadata`: The operator-facing metadata captured by the latest report.

#### Properties

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; set; }
```

The number of `failed` observations reported so far.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-lastattempt"></a>

##### `LastAttempt`

```csharp
int LastAttempt { get; set; }
```

The last reported application-managed attempt number.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; set; }
```

The last operator-facing error summary when a failure was reported.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-lastmessageid"></a>

##### `LastMessageId`

```csharp
string LastMessageId { get; set; }
```

The last stable inbound message identifier when one was reported.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the last observation was reported.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; set; }
```

The last reported outcome identifier when one exists.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

The operator-facing metadata captured by the latest report.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-retrypending"></a>

##### `RetryPending`

```csharp
bool RetryPending { get; }
```

Gets a value indicating whether the latest report says another retry attempt is pending.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-retryscheduledcount"></a>

##### `RetryScheduledCount`

```csharp
int RetryScheduledCount { get; set; }
```

The number of `retry-scheduled` observations reported so far.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-skippedcount"></a>

##### `SkippedCount`

```csharp
int SkippedCount { get; set; }
```

The number of `skipped` observations reported so far.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-startedcount"></a>

##### `StartedCount`

```csharp
int StartedCount { get; set; }
```

The number of `started` observations reported so far.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-subscriptionid"></a>

##### `SubscriptionId`

```csharp
string SubscriptionId { get; set; }
```

The stable declared subscription identifier.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-succeededcount"></a>

##### `SucceededCount`

```csharp
int SucceededCount { get; set; }
```

The number of `succeeded` observations reported so far.

<a id="member-p-cephalon-eventing-services-eventsubscriptionruntimestate-totalreports"></a>

##### `TotalReports`

```csharp
int TotalReports { get; }
```

Gets the total number of observations reported for this subscription.

<a id="type-cephalon-eventing-services-ieventchannelcatalog"></a>

### `IEventChannelCatalog`

Exposes the merged set of event channels available to the active eventing runtime.

#### Declaration
```csharp
public interface IEventChannelCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventchannelcatalog-channels"></a>

##### `Channels`

```csharp
IReadOnlyList<EventChannelDescriptor> Channels { get; }
```

Gets the effective channel set after host options and module contributors have both been applied.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventchannelcatalog-tryget-system-string-cephalon-eventing-services-eventchanneldescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string channelId, out EventChannelDescriptor channel)
```

Attempts to resolve an event channel descriptor by identifier.

Returns: `true` when the channel exists; otherwise `false`.

Parameters:
- `channelId`: The channel identifier to resolve.
- `channel`: When this method returns, contains the resolved channel if found.

<a id="type-cephalon-eventing-services-ieventchannelcontributor"></a>

### `IEventChannelContributor`

Allows a module to contribute event channels into the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventChannelContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventchannelcontributor-registerchannels-cephalon-eventing-services-ieventchannelregistry"></a>

##### `RegisterChannels`

```csharp
void RegisterChannels(IEventChannelRegistry channels)
```

Registers one or more event channel descriptors with the supplied registry.

Parameters:
- `channels`: The registry that collects contributed channel descriptors.

<a id="type-cephalon-eventing-services-ieventchannelregistry"></a>

### `IEventChannelRegistry`

Collects event channel descriptors contributed to the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventChannelRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventchannelregistry-add-cephalon-eventing-services-eventchanneldescriptor"></a>

##### `Add`

```csharp
void Add(EventChannelDescriptor channel)
```

Adds an event channel descriptor to the registry.

Parameters:
- `channel`: The channel descriptor to contribute.

<a id="type-cephalon-eventing-services-ieventdispatchruntimecontributor"></a>

### `IEventDispatchRuntimeContributor`

Contributes one or more operator-facing durable dispatch runtimes to the active eventing technology.

#### Declaration
```csharp
public interface IEventDispatchRuntimeContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventdispatchruntimecontributor-registerdispatchruntimes-cephalon-eventing-services-ieventdispatchruntimeregistry"></a>

##### `RegisterDispatchRuntimes`

```csharp
void RegisterDispatchRuntimes(IEventDispatchRuntimeRegistry dispatchRuntimes)
```

Registers one or more dispatch-runtime descriptors owned by the contributor.

Parameters:
- `dispatchRuntimes`: The dispatch-runtime registry receiving contributed descriptors.

<a id="type-cephalon-eventing-services-ieventdispatchruntimeregistry"></a>

### `IEventDispatchRuntimeRegistry`

Receives operator-facing durable dispatch-runtime descriptors contributed by active eventing packs.

#### Declaration
```csharp
public interface IEventDispatchRuntimeRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventdispatchruntimeregistry-add-cephalon-abstractions-data-eventdispatchruntimedescriptor"></a>

##### `Add`

```csharp
void Add(EventDispatchRuntimeDescriptor dispatchRuntime)
```

Adds one dispatch runtime to the current eventing technology composition.

Parameters:
- `dispatchRuntime`: The dispatch runtime to register.

<a id="type-cephalon-eventing-services-ieventdispatchruntimereporter"></a>

### `IEventDispatchRuntimeReporter`

Accepts operator-facing dispatch runtime observations for durable event publication paths.

#### Declaration
```csharp
public interface IEventDispatchRuntimeReporter
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventdispatchruntimereporter-reportasync-cephalon-eventing-services-eventdispatchexecutionreport-system-threading-cancellationtoken"></a>

##### `ReportAsync`

```csharp
ValueTask ReportAsync(EventDispatchExecutionReport report, CancellationToken cancellationToken)
```

Reports one dispatch observation for the active eventing runtime.

Returns: A task that completes when the observation has been recorded.

Parameters:
- `report`: The dispatch observation to capture.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-eventing-services-ieventdispatchstore"></a>

### `IEventDispatchStore`

Reads pending staged events and applies durable dispatch outcomes for the active eventing runtime.

Remarks: This contract stays runtime-neutral on purpose. It does not claim broker ownership, delivery guarantees, or concurrency semantics beyond what the active outbox implementation actually provides.

#### Declaration
```csharp
public interface IEventDispatchStore
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventdispatchstore-outboxids"></a>

##### `OutboxIds`

```csharp
IReadOnlyList<string> OutboxIds { get; }
```

Gets the outbox identifiers explicitly owned by the dispatch store.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventdispatchstore-applyreportasync-cephalon-eventing-services-eventdispatchexecutionreport-system-threading-cancellationtoken"></a>

##### `ApplyReportAsync`

```csharp
ValueTask ApplyReportAsync(EventDispatchExecutionReport report, CancellationToken cancellationToken)
```

Applies one durable dispatch outcome to the active staged-event store.

Returns: A task that completes when the durable store has applied the dispatch observation.

Parameters:
- `report`: The dispatch observation to apply.
- `cancellationToken`: The token that cancels the operation.

<a id="member-m-cephalon-eventing-services-ieventdispatchstore-readpendingasync-system-int32-system-threading-cancellationtoken"></a>

##### `ReadPendingAsync`

```csharp
ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(int maximumCount, CancellationToken cancellationToken)
```

Reads pending staged events that are eligible for dispatch.

Returns: The pending staged events that are currently eligible for dispatch.

Parameters:
- `maximumCount`: The maximum number of staged events to read.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-eventing-services-ieventpublisher"></a>

### `IEventPublisher`

Accepts integration events for the active eventing runtime.

#### Declaration
```csharp
public interface IEventPublisher
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventpublisher-publishasync-cephalon-eventing-services-eventpublication-system-threading-cancellationtoken"></a>

##### `PublishAsync`

```csharp
ValueTask PublishAsync(EventPublication publication, CancellationToken cancellationToken)
```

Publishes one integration event through the active eventing runtime.

Returns: A task that completes when the publication has been accepted by the runtime.

Parameters:
- `publication`: The publication request to handle.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-eventing-services-ieventsubscriptioncatalog"></a>

### `IEventSubscriptionCatalog`

Exposes the merged set of declared event subscriptions available to the active eventing runtime.

#### Declaration
```csharp
public interface IEventSubscriptionCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventsubscriptioncatalog-subscriptions"></a>

##### `Subscriptions`

```csharp
IReadOnlyList<EventSubscriptionDescriptor> Subscriptions { get; }
```

Gets the effective subscription set after host options and module contributors have both been applied.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptioncatalog-getbychannelid-system-string"></a>

##### `GetByChannelId`

```csharp
IReadOnlyList<EventSubscriptionDescriptor> GetByChannelId(string channelId)
```

Gets the subscriptions currently bound to one event channel identifier.

Returns: The subscriptions declared for the requested channel.

Parameters:
- `channelId`: The event channel identifier to filter by.

<a id="member-m-cephalon-eventing-services-ieventsubscriptioncatalog-tryget-system-string-cephalon-eventing-services-eventsubscriptiondescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string subscriptionId, out EventSubscriptionDescriptor subscription)
```

Attempts to resolve a subscription descriptor by identifier.

Returns: `true` when the subscription exists; otherwise `false`.

Parameters:
- `subscriptionId`: The subscription identifier to resolve.
- `subscription`: When this method returns, contains the resolved subscription if found.

<a id="type-cephalon-eventing-services-ieventsubscriptioncontributor"></a>

### `IEventSubscriptionContributor`

Allows a module to contribute declared event subscriptions into the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventSubscriptionContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptioncontributor-registersubscriptions-cephalon-eventing-services-ieventsubscriptionregistry"></a>

##### `RegisterSubscriptions`

```csharp
void RegisterSubscriptions(IEventSubscriptionRegistry subscriptions)
```

Registers one or more event subscription descriptors with the supplied registry.

Parameters:
- `subscriptions`: The registry that collects contributed subscription descriptors.

<a id="type-cephalon-eventing-services-ieventsubscriptionexecutionbindingcatalog"></a>

### `IEventSubscriptionExecutionBindingCatalog`

Exposes managed execution bindings for declared event subscriptions.

Remarks: The catalog is a host-agnostic read contract for companion packs that bind declared subscriptions to a real execution runtime. An empty catalog is a valid answer and means the active eventing pack is still descriptor-first or application-managed for subscription execution.

#### Declaration
```csharp
public interface IEventSubscriptionExecutionBindingCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventsubscriptionexecutionbindingcatalog-bindings"></a>

##### `Bindings`

```csharp
IReadOnlyList<EventSubscriptionExecutionBindingDescriptor> Bindings { get; }
```

Gets the currently active managed execution bindings ordered by subscription identifier.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptionexecutionbindingcatalog-getbysubscriptionid-system-string"></a>

##### `GetBySubscriptionId`

```csharp
EventSubscriptionExecutionBindingDescriptor GetBySubscriptionId(string subscriptionId)
```

Looks up the managed execution binding for one declared subscription.

Returns: The managed execution binding when one is active; otherwise, `null`.

Parameters:
- `subscriptionId`: The stable declared subscription identifier.

<a id="member-m-cephalon-eventing-services-ieventsubscriptionexecutionbindingcatalog-tryget-system-string-cephalon-eventing-services-eventsubscriptionexecutionbindingdescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string subscriptionId, out EventSubscriptionExecutionBindingDescriptor binding)
```

Attempts to resolve the managed execution binding for one declared subscription.

Returns: `true` when a binding exists; otherwise, `false`.

Parameters:
- `subscriptionId`: The stable declared subscription identifier.
- `binding`: When this method returns, contains the resolved binding when one is active.

<a id="type-cephalon-eventing-services-ieventsubscriptionexecutionbindingcontributor"></a>

### `IEventSubscriptionExecutionBindingContributor`

Contributes one or more managed execution bindings for declared event subscriptions.

#### Declaration
```csharp
public interface IEventSubscriptionExecutionBindingContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptionexecutionbindingcontributor-getexecutionbindings"></a>

##### `GetExecutionBindings`

```csharp
IReadOnlyList<EventSubscriptionExecutionBindingDescriptor> GetExecutionBindings()
```

Returns the managed execution bindings owned by the contributor.

Returns: The managed execution bindings for declared subscriptions.

<a id="type-cephalon-eventing-services-ieventsubscriptionexecutor"></a>

### `IEventSubscriptionExecutor`

Executes one declared event subscription through a pack-owned managed runtime.

#### Declaration
```csharp
public interface IEventSubscriptionExecutor
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventsubscriptionexecutor-subscriptionid"></a>

##### `SubscriptionId`

```csharp
string SubscriptionId { get; }
```

Gets the stable declared subscription identifier owned by this executor.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptionexecutor-executeasync-cephalon-eventing-services-eventsubscriptionexecutioncontext-system-threading-cancellationtoken"></a>

##### `ExecuteAsync`

```csharp
ValueTask ExecuteAsync(EventSubscriptionExecutionContext context, CancellationToken cancellationToken)
```

Executes the managed subscription against the supplied publication context.

Returns: A task that completes when the managed subscription attempt finishes.

Parameters:
- `context`: The host-agnostic execution context for the current subscription attempt.
- `cancellationToken`: The cancellation token for the current execution attempt.

<a id="type-cephalon-eventing-services-ieventsubscriptionregistry"></a>

### `IEventSubscriptionRegistry`

Collects event subscription descriptors contributed to the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventSubscriptionRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptionregistry-add-cephalon-eventing-services-eventsubscriptiondescriptor"></a>

##### `Add`

```csharp
void Add(EventSubscriptionDescriptor subscription)
```

Adds an event subscription descriptor to the registry.

Parameters:
- `subscription`: The subscription descriptor to contribute.

<a id="type-cephalon-eventing-services-ieventsubscriptionruntimecatalog"></a>

### `IEventSubscriptionRuntimeCatalog`

Exposes the latest reported runtime state for declared event subscriptions.

#### Declaration
```csharp
public interface IEventSubscriptionRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventsubscriptionruntimecatalog-states"></a>

##### `States`

```csharp
IReadOnlyList<EventSubscriptionRuntimeState> States { get; }
```

Gets the currently known runtime-state entries ordered by subscription identifier.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptionruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
EventSubscriptionRuntimeState GetById(string subscriptionId)
```

Looks up one reported runtime-state entry by declared subscription identifier.

Returns: The current runtime state when one has been reported; otherwise, `null`.

Parameters:
- `subscriptionId`: The stable declared subscription identifier.

<a id="member-m-cephalon-eventing-services-ieventsubscriptionruntimecatalog-tryget-system-string-cephalon-eventing-services-eventsubscriptionruntimestate"></a>

##### `TryGet`

```csharp
bool TryGet(string subscriptionId, out EventSubscriptionRuntimeState state)
```

Attempts to look up one reported runtime-state entry by declared subscription identifier.

Returns: `true` when one runtime-state entry is available; otherwise, `false`.

Parameters:
- `subscriptionId`: The stable declared subscription identifier.
- `state`: The current runtime state when one has been reported.

<a id="type-cephalon-eventing-services-ieventsubscriptionruntimereporter"></a>

### `IEventSubscriptionRuntimeReporter`

Records application-managed runtime observations for declared event subscriptions.

#### Declaration
```csharp
public interface IEventSubscriptionRuntimeReporter
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptionruntimereporter-reportasync-cephalon-eventing-services-eventsubscriptionexecutionreport-system-threading-cancellationtoken"></a>

##### `ReportAsync`

```csharp
ValueTask ReportAsync(EventSubscriptionExecutionReport report, CancellationToken cancellationToken)
```

Records one application-managed execution observation for a declared event subscription.

Returns: A task that completes when the observation has been recorded.

Parameters:
- `report`: The runtime observation to record.
- `cancellationToken`: The token that cancels the operation.
