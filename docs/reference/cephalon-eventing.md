# Cephalon.Eventing

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Eventing)
## Namespaces

- `Cephalon.Eventing.Configuration`
- `Cephalon.Eventing.Hosting`
- `Cephalon.Eventing.Registration`
- `Cephalon.Eventing.Services`

<a id="namespace-cephalon-eventing-configuration"></a>

## Namespace Cephalon.Eventing.Configuration

<a id="type-cephalon-eventing-configuration-eventingoptions"></a>

### `EventingOptions`

Configures the built-in eventing runtime pack.

Remarks: These options seed the host-owned part of the eventing runtime. Installed modules can still contribute additional channels through `IEventChannelContributor` and additional subscription, contract, serializer, schema registry, upcaster, and context policy descriptors through `IEventSubscriptionContributor`, `IEventContractContributor`, `IEventSerializerContributor`, `IEventSchemaRegistryContributor`, `IEventUpcasterContributor`, and `IEventContextPolicyContributor`.

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

<a id="member-p-cephalon-eventing-configuration-eventingoptions-contextpolicies"></a>

##### `ContextPolicies`

```csharp
IList<EventContextPolicyDescriptor> ContextPolicies { get; }
```

Gets the host-defined event context policy descriptors that should be available to the eventing runtime.

Remarks: These descriptors are code-owned tenant, correlation, causation, baggage, and message-header policy metadata. They do not make publish or subscription execution perform context propagation lookups on the hot path.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-continueinprocesssubscriptionexecutionafterfailure"></a>

##### `ContinueInProcessSubscriptionExecutionAfterFailure`

```csharp
bool ContinueInProcessSubscriptionExecutionAfterFailure { get; set; }
```

Gets or sets a value indicating whether the in-process publisher should continue executing later subscriptions on the same channel after one subscription fails.

Remarks: The publisher still reports failed subscriptions and throws after the publication attempt finishes. This setting only controls whether independent subscriptions on the same channel get a chance to run before the failure is returned to the caller.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-contracts"></a>

##### `Contracts`

```csharp
IList<EventContractDescriptor> Contracts { get; }
```

Gets the host-defined event contract descriptors that should be available to the eventing runtime.

Remarks: These descriptors are code-owned contract metadata. They do not make publish or subscription execution perform config lookups on the hot path.

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

<a id="member-p-cephalon-eventing-configuration-eventingoptions-enablepublicationrouting"></a>

##### `EnablePublicationRouting`

```csharp
bool EnablePublicationRouting { get; set; }
```

Gets or sets a value indicating whether publication requests can resolve an effective channel from the configured event-type routing table before the active publisher runs.

Remarks: The routing table is provider-neutral and runs inside the Cephalon eventing dispatcher. It can route requests that use `PublicationRoutingAutoChannelId` as the requested channel, and it can validate explicit channels against declared event-type ownership. Broker topology, queues, exchanges, topics, and subscriptions still belong to the selected transport or companion package.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-enablepublicationscheduling"></a>

##### `EnablePublicationScheduling`

```csharp
bool EnablePublicationScheduling { get; set; }
```

Gets or sets a value indicating whether publication requests can be delayed by the native eventing pack.

Remarks: Delayed publications are held in the current process until their due time and then handed to the active publisher. This is a lightweight Wolverine-free scheduling baseline, not a durable or distributed scheduler.

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

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionidempotencystore"></a>

##### `InProcessSubscriptionIdempotencyStore`

```csharp
string InProcessSubscriptionIdempotencyStore { get; set; }
```

Gets or sets the store used by the direct in-process publisher for completed subscription-execution idempotency.

Remarks: The default value is `process-local`, which records completed executions in the current process only. Set the value to `inbox` to use exactly one registered `IInbox` as the duplicate-suppression store while keeping the same direct in-process execution path.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionmaxattempts"></a>

##### `InProcessSubscriptionMaxAttempts`

```csharp
int InProcessSubscriptionMaxAttempts { get; set; }
```

Gets or sets the maximum number of direct in-process execution attempts per matching subscription.

Remarks: The default value of `1` preserves the no-retry baseline. Values greater than `1` enable a bounded, process-local retry loop; this still does not provide durable broker, inbox, or distributed retry guarantees.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionretrybackoff"></a>

##### `InProcessSubscriptionRetryBackoff`

```csharp
string InProcessSubscriptionRetryBackoff { get; set; }
```

Gets or sets the process-local backoff strategy used between failed direct subscription attempts.

Remarks: Supported values are `fixed` and `exponential`. The default `fixed` value preserves the original bounded in-process retry behavior.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionretrybackoffmultiplier"></a>

##### `InProcessSubscriptionRetryBackoffMultiplier`

```csharp
int InProcessSubscriptionRetryBackoffMultiplier { get; set; }
```

Gets or sets the exponential retry-delay multiplier used by the direct in-process publisher.

Remarks: The value is used only when `InProcessSubscriptionRetryBackoff` is `exponential`.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionretrydelaymilliseconds"></a>

##### `InProcessSubscriptionRetryDelayMilliseconds`

```csharp
int InProcessSubscriptionRetryDelayMilliseconds { get; set; }
```

Gets or sets the delay in milliseconds before the direct in-process publisher retries a failed subscription attempt.

Remarks: The delay is applied only when `InProcessSubscriptionMaxAttempts` is greater than `1`. The default value of `0` retries immediately and is useful for tests and lightweight process-local remediation paths.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionretryjitterpercent"></a>

##### `InProcessSubscriptionRetryJitterPercent`

```csharp
int InProcessSubscriptionRetryJitterPercent { get; set; }
```

Gets or sets the deterministic retry jitter percentage applied by the direct in-process publisher.

Remarks: Jitter is derived from publication id, subscription id, and attempt number so it stays deterministic for a message while still spreading retry timings across different messages.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-inprocesssubscriptionretrymaxdelaymilliseconds"></a>

##### `InProcessSubscriptionRetryMaxDelayMilliseconds`

```csharp
int InProcessSubscriptionRetryMaxDelayMilliseconds { get; set; }
```

Gets or sets the maximum retry delay, in milliseconds, accepted by the direct in-process publisher.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-publicationroutes"></a>

##### `PublicationRoutes`

```csharp
IDictionary<string, string> PublicationRoutes { get; }
```

Gets the provider-neutral event-type route table used by the Cephalon dispatcher.

Remarks: Keys are event-type patterns. Exact keys match first; keys ending with `*` match by case-insensitive prefix. Values are effective event channel identifiers.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-publicationroutingautochannelid"></a>

##### `PublicationRoutingAutoChannelId`

```csharp
string PublicationRoutingAutoChannelId { get; set; }
```

Gets or sets the requested channel identifier that tells the dispatcher to resolve the effective channel from `PublicationRoutes`.

Remarks: The default value is `auto`. Hosts can keep application code stable by allowing callers to publish with this logical channel while routing remains configuration-owned.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-publicationroutingrejectmismatchedexplicitchannel"></a>

##### `PublicationRoutingRejectMismatchedExplicitChannel`

```csharp
bool PublicationRoutingRejectMismatchedExplicitChannel { get; set; }
```

Gets or sets a value indicating whether an explicitly requested channel that disagrees with a matched event-type route should be rejected before publishing.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-publicationroutingrequirematchedroute"></a>

##### `PublicationRoutingRequireMatchedRoute`

```csharp
bool PublicationRoutingRequireMatchedRoute { get; set; }
```

Gets or sets a value indicating whether every routed publication must match a configured event-type route.

Remarks: This guard is useful when a host wants central routing governance for all events. Requests that use the auto channel always require a match because the dispatcher has no effective channel without one.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-publicationschedulingmaxdelaymilliseconds"></a>

##### `PublicationSchedulingMaxDelayMilliseconds`

```csharp
int PublicationSchedulingMaxDelayMilliseconds { get; set; }
```

Gets or sets the maximum delay, in milliseconds, accepted by the process-local publication scheduler.

Remarks: The default value is `86,400,000` milliseconds, or twenty-four hours.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-publicationschedulingmaxpendingcount"></a>

##### `PublicationSchedulingMaxPendingCount`

```csharp
int PublicationSchedulingMaxPendingCount { get; set; }
```

Gets or sets the maximum number of delayed publications retained by the process-local scheduler.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-remediationcommandhistorylimit"></a>

##### `RemediationCommandHistoryLimit`

```csharp
int RemediationCommandHistoryLimit { get; set; }
```

Gets or sets the maximum number of event-dispatch remediation command results retained in memory for operator reads.

Remarks: The default value is `256`. Set the value to `0` to disable the process-local remediation command history while keeping the command dispatcher itself available. The catalog is an operator-audit read model, not a durable compliance store; hosts that need long-term retention should also persist command results.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-schemaregistries"></a>

##### `SchemaRegistries`

```csharp
IList<EventSchemaRegistryDescriptor> SchemaRegistries { get; }
```

Gets the host-defined event schema registry descriptors that should be available to the eventing runtime.

Remarks: These descriptors are code-owned registry availability metadata. They do not make publish or subscription execution perform registry lookups on the hot path.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-serializers"></a>

##### `Serializers`

```csharp
IList<EventSerializerDescriptor> Serializers { get; }
```

Gets the host-defined event serializer descriptors that should be available to the eventing runtime.

Remarks: These descriptors are code-owned serializer availability metadata. They do not make publish or subscription execution perform config lookups on the hot path.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-subscriptions"></a>

##### `Subscriptions`

```csharp
IList<EventSubscriptionDescriptor> Subscriptions { get; }
```

Gets the host-defined event subscription descriptors that should be available to the eventing runtime.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-upcasters"></a>

##### `Upcasters`

```csharp
IList<EventUpcasterDescriptor> Upcasters { get; }
```

Gets the host-defined event upcaster descriptors that should be available to the eventing runtime.

Remarks: These descriptors are code-owned version-transition metadata. They do not make publish or subscription execution perform upcaster lookups on the hot path.

<a id="namespace-cephalon-eventing-hosting"></a>

## Namespace Cephalon.Eventing.Hosting

<a id="type-cephalon-eventing-hosting-eventingservicecollectionextensions"></a>

### `EventingServiceCollectionExtensions`

Registers code-first native eventing services used by Cephalon hosts and modules.

#### Declaration
```csharp
public static class EventingServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-eventing-hosting-eventingservicecollectionextensions-addcephaloneventsubscriptionexecutionmiddleware-1-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `AddCephalonEventSubscriptionExecutionMiddleware`

```csharp
IServiceCollection AddCephalonEventSubscriptionExecutionMiddleware<TMiddleware>(this IServiceCollection services)
```

Registers a direct in-process subscription execution middleware step with the native eventing pack.

Remarks: Middleware steps are registered as singleton `IEventSubscriptionExecutionMiddleware` contributions and run in dependency-injection registration order before the final subscription executor. This keeps cross-cutting subscription policy typed and code-owned instead of binding it from configuration.

Returns: The same service collection for fluent registration.

Type parameters:
- `TMiddleware`: The concrete middleware implementation type.

Parameters:
- `services`: The service collection to extend.

<a id="member-m-cephalon-eventing-hosting-eventingservicecollectionextensions-addcephaloneventsubscriptionexecutor-1-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `AddCephalonEventSubscriptionExecutor`

```csharp
IServiceCollection AddCephalonEventSubscriptionExecutor<TExecutor>(this IServiceCollection services)
```

Registers a direct in-process event subscription executor with the native eventing pack.

Remarks: The executor is registered as a singleton `IEventSubscriptionExecutor` contribution. If the implementation is annotated with `EventSubscriptionAttribute` or implements `IEventSubscriptionDescriptorProvider`, the native in-process lane can discover the matching subscription descriptor from the same code-owned type.

Returns: The same service collection for fluent registration.

Type parameters:
- `TExecutor`: The concrete executor implementation type.

Parameters:
- `services`: The service collection to extend.

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

<a id="member-m-cephalon-eventing-registration-eventingenginebuilderextensions-addeventingfromconfiguration-cephalon-engine-composition-enginebuilder-microsoft-extensions-configuration-iconfiguration"></a>

##### `AddEventingFromConfiguration`

```csharp
EngineBuilder AddEventingFromConfiguration(this EngineBuilder builder, IConfiguration configuration)
```

Adds the eventing runtime pack to the engine and reads host-owned native eventing settings from configuration.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configuration`: The host configuration that contains the `Engine:Messaging` section, including optional `Channels`, `InProcessSubscriptions`, and publication policy settings.

<a id="member-m-cephalon-eventing-registration-eventingenginebuilderextensions-addeventingfromconfiguration-cephalon-engine-composition-enginebuilder-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-eventing-configuration-eventingoptions"></a>

##### `AddEventingFromConfiguration`

```csharp
EngineBuilder AddEventingFromConfiguration(this EngineBuilder builder, IConfiguration configuration, Action<EventingOptions> configure)
```

Adds the eventing runtime pack to the engine and reads host-owned native eventing settings from configuration.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configuration`: The host configuration that contains the `Engine:Messaging` section, including optional `Channels`, `InProcessSubscriptions`, and publication policy settings.
- `configure`: A callback that can add channels, subscriptions, or deliberate code-owned overrides after configuration is read.

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

<a id="type-cephalon-eventing-services-eventconsumercontextextractor"></a>

### `EventConsumerContextExtractor`

Extracts Cephalon event context from a publication that is about to be delivered to a consumer.

Remarks: The extractor normalizes the publication fields and stable Cephalon headers into one consumer-side context-header set. It does not claim provider-side persistence, delivery completion, or cross-node handoff.

#### Declaration
```csharp
public static class EventConsumerContextExtractor
```

#### Methods

<a id="member-m-cephalon-eventing-services-eventconsumercontextextractor-applymetadata-system-collections-generic-idictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ApplyMetadata`

```csharp
void ApplyMetadata(IDictionary<string, string> metadata, IReadOnlyDictionary<string, string> consumerContextHeaders)
```

Adds consumer-side context extraction evidence to subscription execution metadata.

Parameters:
- `metadata`: The subscription execution metadata dictionary to enrich.
- `consumerContextHeaders`: The consumer-visible context headers extracted from the publication.

<a id="member-m-cephalon-eventing-services-eventconsumercontextextractor-createheaders-cephalon-eventing-services-eventpublication"></a>

##### `CreateHeaders`

```csharp
Dictionary<string, string> CreateHeaders(EventPublication publication)
```

Creates a deterministic set of consumer-visible Cephalon context headers for a publication.

Returns: A case-insensitive dictionary of extracted consumer context headers.

Parameters:
- `publication`: The publication being delivered to a subscription executor.

<a id="type-cephalon-eventing-services-eventcontexthandoffmetadatakeys"></a>

### `EventContextHandoffMetadataKeys`

Provides stable metadata keys used when Cephalon stages validated event context into an outbox handoff.

#### Declaration
```csharp
public static class EventContextHandoffMetadataKeys
```

#### Fields

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-baggagecontextpropagation"></a>

##### `BaggageContextPropagation`

```csharp
const string BaggageContextPropagation
```

Records how baggage context was represented on the staged publication.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-causationcontextpropagation"></a>

##### `CausationContextPropagation`

```csharp
const string CausationContextPropagation
```

Records how causation context was represented on the staged publication.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-contexthandoff"></a>

##### `ContextHandoff`

```csharp
const string ContextHandoff
```

Identifies how event context was handed off to the next runtime boundary.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-contextvalidation"></a>

##### `ContextValidation`

```csharp
const string ContextValidation
```

Identifies the validation posture used before the handoff was staged.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-correlationcontextpropagation"></a>

##### `CorrelationContextPropagation`

```csharp
const string CorrelationContextPropagation
```

Records how correlation context was represented on the staged publication.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-presentheadercount"></a>

##### `PresentHeaderCount`

```csharp
const string PresentHeaderCount
```

Records the number of required context headers present on the staged publication.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-presentheaders"></a>

##### `PresentHeaders`

```csharp
const string PresentHeaders
```

Lists the required context header names present on the staged publication.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-requiredheadercount"></a>

##### `RequiredHeaderCount`

```csharp
const string RequiredHeaderCount
```

Records the number of required context headers declared by the active policy set.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-requiredheaders"></a>

##### `RequiredHeaders`

```csharp
const string RequiredHeaders
```

Lists the required context header names declared by the active policy set.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-tenantcontextpropagation"></a>

##### `TenantContextPropagation`

```csharp
const string TenantContextPropagation
```

Records how tenant context was represented on the staged publication.

<a id="member-f-cephalon-eventing-services-eventcontexthandoffmetadatakeys-wolverinerequired"></a>

##### `WolverineRequired`

```csharp
const string WolverineRequired
```

Records whether the context handoff required the optional Wolverine companion.

<a id="type-cephalon-eventing-services-eventcontextheadernames"></a>

### `EventContextHeaderNames`

Defines the stable Cephalon event context header names used by provider-neutral eventing policies.

#### Declaration
```csharp
public static class EventContextHeaderNames
```

#### Fields

<a id="member-f-cephalon-eventing-services-eventcontextheadernames-baggage"></a>

##### `Baggage`

```csharp
const string Baggage
```

Carries provider-neutral baggage associated with an event publication.

<a id="member-f-cephalon-eventing-services-eventcontextheadernames-causationid"></a>

##### `CausationId`

```csharp
const string CausationId
```

Identifies the causation id associated with an event publication.

<a id="member-f-cephalon-eventing-services-eventcontextheadernames-correlationid"></a>

##### `CorrelationId`

```csharp
const string CorrelationId
```

Identifies the correlation id associated with an event publication.

<a id="member-f-cephalon-eventing-services-eventcontextheadernames-messageid"></a>

##### `MessageId`

```csharp
const string MessageId
```

Identifies the source event message when a policy needs a header-level message identity.

<a id="member-f-cephalon-eventing-services-eventcontextheadernames-tenantid"></a>

##### `TenantId`

```csharp
const string TenantId
```

Identifies the tenant associated with an event publication.

<a id="type-cephalon-eventing-services-eventcontextpolicydescriptor"></a>

### `EventContextPolicyDescriptor`

Describes provider-neutral event context policy metadata and publisher validation hints for tenant, correlation, causation, baggage, and header propagation.

Remarks: The descriptor is intentionally code-first. It lets hosts and modules expose context-policy ownership, require stable publication headers, and project validation evidence without putting publish/subscribe binding behind string configuration.

#### Declaration
```csharp
public sealed class EventContextPolicyDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventcontextpolicydescriptor-ctor-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventContextPolicyDescriptor`

```csharp
EventContextPolicyDescriptor(string id, string displayName, string description, string runtimeKind, bool declaresTenantContext, bool declaresCorrelationId, bool declaresCausationId, bool declaresBaggage, bool validatesMessageHeaders, IReadOnlyList<string> headerNames, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new event context policy descriptor.

Parameters:
- `id`: The stable context policy identifier.
- `displayName`: The operator-facing context policy name.
- `description`: The human-readable context policy description.
- `runtimeKind`: The runtime implementation kind, such as `code-first` or `provider-managed`.
- `declaresTenantContext`: Whether the policy declares tenant-context propagation metadata.
- `declaresCorrelationId`: Whether the policy declares correlation-id propagation metadata.
- `declaresCausationId`: Whether the policy declares causation-id propagation metadata.
- `declaresBaggage`: Whether the policy declares baggage propagation metadata.
- `validatesMessageHeaders`: Whether the policy declares message-header validation metadata.
- `headerNames`: Optional stable message-header names covered by the policy.
- `tags`: Optional tags that classify the context policy.
- `metadata`: Optional context policy metadata.

#### Properties

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-declaresbaggage"></a>

##### `DeclaresBaggage`

```csharp
bool DeclaresBaggage { get; }
```

Gets a value indicating whether the policy declares baggage propagation metadata.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-declarescausationid"></a>

##### `DeclaresCausationId`

```csharp
bool DeclaresCausationId { get; }
```

Gets a value indicating whether the policy declares causation-id propagation metadata.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-declarescorrelationid"></a>

##### `DeclaresCorrelationId`

```csharp
bool DeclaresCorrelationId { get; }
```

Gets a value indicating whether the policy declares correlation-id propagation metadata.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-declarestenantcontext"></a>

##### `DeclaresTenantContext`

```csharp
bool DeclaresTenantContext { get; }
```

Gets a value indicating whether the policy declares tenant-context propagation metadata.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable context policy description.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the context policy.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-headernames"></a>

##### `HeaderNames`

```csharp
IReadOnlyList<string> HeaderNames { get; }
```

Gets the normalized message-header names covered by the policy.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable context policy identifier.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets normalized metadata associated with the context policy.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-runtimekind"></a>

##### `RuntimeKind`

```csharp
string RuntimeKind { get; }
```

Gets the context policy runtime implementation kind.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the context policy.

<a id="member-p-cephalon-eventing-services-eventcontextpolicydescriptor-validatesmessageheaders"></a>

##### `ValidatesMessageHeaders`

```csharp
bool ValidatesMessageHeaders { get; }
```

Gets a value indicating whether the policy declares message-header validation metadata.

<a id="type-cephalon-eventing-services-eventcontractdescriptor"></a>

### `EventContractDescriptor`

Describes the provider-neutral contract metadata for one event type and version.

Remarks: The descriptor is intentionally code-first and runtime-neutral. Modules, source generators, and hosts can register event contract truth without putting serializer or handler lookup on the publication hot path.

#### Declaration
```csharp
public sealed class EventContractDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventcontractdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventContractDescriptor`

```csharp
EventContractDescriptor(string id, string eventType, string displayName, string description, string version, string contentType, string serializerId, string envelopeSchema, string compatibilityPolicy, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new event contract descriptor.

Parameters:
- `id`: The stable contract identifier.
- `eventType`: The logical event type identifier.
- `displayName`: The operator-facing event contract name.
- `description`: The human-readable event contract description.
- `version`: The event contract version.
- `contentType`: The wire content type expected for the event payload.
- `serializerId`: The provider-neutral serializer identifier selected by the contract.
- `envelopeSchema`: The event envelope schema identifier used by the contract.
- `compatibilityPolicy`: The compatibility policy declared for the contract.
- `tags`: Optional tags that classify the contract.
- `metadata`: Optional contract metadata.

#### Properties

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-compatibilitypolicy"></a>

##### `CompatibilityPolicy`

```csharp
string CompatibilityPolicy { get; }
```

Gets the declared compatibility policy for the event contract.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; }
```

Gets the wire content type expected for the event payload.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable contract description.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the contract.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-envelopeschema"></a>

##### `EnvelopeSchema`

```csharp
string EnvelopeSchema { get; }
```

Gets the event envelope schema identifier used by the contract.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-eventtype"></a>

##### `EventType`

```csharp
string EventType { get; }
```

Gets the logical event type identifier.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable contract identifier.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets normalized metadata associated with the contract.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-serializerid"></a>

##### `SerializerId`

```csharp
string SerializerId { get; }
```

Gets the provider-neutral serializer identifier selected by the contract.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the contract.

<a id="member-p-cephalon-eventing-services-eventcontractdescriptor-version"></a>

##### `Version`

```csharp
string Version { get; }
```

Gets the event contract version.

<a id="type-cephalon-eventing-services-eventdispatchcontextreportmetadata"></a>

### `EventDispatchContextReportMetadata`

Builds provider-neutral metadata that carries staged Cephalon event context into dispatch runtime reports.

Remarks: The helper preserves the dispatch item's staged metadata and adds conservative runtime boundary markers. Provider and broker context claims remain `not-claimed` until a provider package reports executable proof.

#### Declaration
```csharp
public static class EventDispatchContextReportMetadata
```

#### Methods

<a id="member-m-cephalon-eventing-services-eventdispatchcontextreportmetadata-create-cephalon-eventing-services-eventdispatchitem-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Create`

```csharp
Dictionary<string, string> Create(EventDispatchItem dispatchItem, IReadOnlyDictionary<string, string> additionalMetadata)
```

Creates dispatch report metadata from a pending dispatch item and optional runtime-specific metadata.

Returns: A case-insensitive metadata dictionary suitable for `Metadata`.

Parameters:
- `dispatchItem`: The pending dispatch item whose staged context should be carried into the report.
- `additionalMetadata`: Optional runtime-specific metadata to add after the provider-neutral context markers.

<a id="type-cephalon-eventing-services-eventdispatchcrossnodecontexthandoffmetadata"></a>

### `EventDispatchCrossNodeContextHandoffMetadata`

Builds provider-reported cross-node context-handoff metadata for dispatch reports.

Remarks: Cross-node handoff is only marked when provider-side context persistence has already been proven on the dispatch metadata, consumer-side extraction metadata is present, and the reported producer and consumer node ids are distinct. This keeps local dispatch-store persistence and local subscription extraction from being mistaken for a distributed handoff.

#### Declaration
```csharp
public static class EventDispatchCrossNodeContextHandoffMetadata
```

#### Methods

<a id="member-m-cephalon-eventing-services-eventdispatchcrossnodecontexthandoffmetadata-createmetadata-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-string"></a>

##### `CreateMetadata`

```csharp
Dictionary<string, string> CreateMetadata(IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, string> consumerContextMetadata, string source, string producerNodeId, string consumerNodeId)
```

Creates a metadata copy enriched with provider-reported cross-node handoff proof when the inputs support it.

Returns: A case-insensitive metadata dictionary containing the original values plus handoff proof when applicable.

Parameters:
- `metadata`: The provider-side dispatch report metadata to copy.
- `consumerContextMetadata`: The consumer-side context extraction metadata observed by the provider or runtime.
- `source`: The stable provider or runtime source that observed the cross-node handoff.
- `producerNodeId`: The producer-side node identifier.
- `consumerNodeId`: The consumer-side node identifier.

<a id="member-m-cephalon-eventing-services-eventdispatchcrossnodecontexthandoffmetadata-createreport-cephalon-eventing-services-eventdispatchexecutionreport-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-string"></a>

##### `CreateReport`

```csharp
EventDispatchExecutionReport CreateReport(EventDispatchExecutionReport report, IReadOnlyDictionary<string, string> consumerContextMetadata, string source, string producerNodeId, string consumerNodeId)
```

Creates a dispatch report copy enriched with provider-reported cross-node handoff proof when the inputs support it.

Returns: A dispatch report containing the original metadata plus cross-node handoff proof when applicable.

Parameters:
- `report`: The provider-side dispatch report to copy.
- `consumerContextMetadata`: The consumer-side context extraction metadata observed by the provider or runtime.
- `source`: The stable provider or runtime source that observed the cross-node handoff.
- `producerNodeId`: The producer-side node identifier.
- `consumerNodeId`: The consumer-side node identifier.

<a id="member-m-cephalon-eventing-services-eventdispatchcrossnodecontexthandoffmetadata-ishandoffproven-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `IsHandoffProven`

```csharp
bool IsHandoffProven(IReadOnlyDictionary<string, string> metadata)
```

Gets a value indicating whether the metadata contains provider-reported cross-node context handoff proof.

Returns: `true` when provider-reported cross-node handoff proof is present; otherwise, `false`.

Parameters:
- `metadata`: The dispatch metadata dictionary to inspect.

<a id="member-m-cephalon-eventing-services-eventdispatchcrossnodecontexthandoffmetadata-tryapplymetadata-system-collections-generic-idictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-string"></a>

##### `TryApplyMetadata`

```csharp
bool TryApplyMetadata(IDictionary<string, string> metadata, IReadOnlyDictionary<string, string> consumerContextMetadata, string source, string producerNodeId, string consumerNodeId)
```

Applies provider-reported cross-node handoff proof to an existing dispatch metadata dictionary when the inputs support it.

Returns: `true` when cross-node handoff proof was applied; otherwise, `false`.

Parameters:
- `metadata`: The provider-side dispatch metadata dictionary to enrich.
- `consumerContextMetadata`: The consumer-side context extraction metadata observed by the provider or runtime.
- `source`: The stable provider or runtime source that observed the cross-node handoff.
- `producerNodeId`: The producer-side node identifier.
- `consumerNodeId`: The consumer-side node identifier.

<a id="type-cephalon-eventing-services-eventdispatchdeliverycompletionmetadata"></a>

### `EventDispatchDeliveryCompletionMetadata`

Builds provider-reported downstream delivery-completion metadata for successful dispatch reports.

Remarks: Dispatch success only says the active dispatcher completed its local work. This helper records a stronger, provider-reported downstream completion proof only when the report outcome is `succeeded` and the provider supplies an explicit delivery receipt id. Subscriber acknowledgement and destination commit evidence are recorded independently so the engine does not imply exactly-once delivery from a generic provider receipt.

#### Declaration
```csharp
public static class EventDispatchDeliveryCompletionMetadata
```

#### Methods

<a id="member-m-cephalon-eventing-services-eventdispatchdeliverycompletionmetadata-createmetadata-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `CreateMetadata`

```csharp
Dictionary<string, string> CreateMetadata(IReadOnlyDictionary<string, string> metadata, string outcome, string source, string providerReceiptId, string subscriberAcknowledgementId, string destinationCommitId)
```

Creates a metadata copy enriched with provider-reported delivery-completion proof when the inputs support it.

Returns: A case-insensitive metadata dictionary containing the original values plus delivery-completion proof when applicable.

Parameters:
- `metadata`: The dispatch report metadata to copy.
- `outcome`: The dispatch report outcome associated with the metadata.
- `source`: The stable provider or runtime source that reported delivery completion.
- `providerReceiptId`: The provider delivery receipt id.
- `subscriberAcknowledgementId`: An optional subscriber acknowledgement id.
- `destinationCommitId`: An optional destination commit id.

<a id="member-m-cephalon-eventing-services-eventdispatchdeliverycompletionmetadata-createreport-cephalon-eventing-services-eventdispatchexecutionreport-system-string-system-string-system-string-system-string"></a>

##### `CreateReport`

```csharp
EventDispatchExecutionReport CreateReport(EventDispatchExecutionReport report, string source, string providerReceiptId, string subscriberAcknowledgementId, string destinationCommitId)
```

Creates a dispatch report copy enriched with provider-reported delivery-completion proof when the inputs support it.

Returns: A dispatch report containing the original metadata plus delivery-completion proof when applicable.

Parameters:
- `report`: The successful dispatch report to copy.
- `source`: The stable provider or runtime source that reported delivery completion.
- `providerReceiptId`: The provider delivery receipt id.
- `subscriberAcknowledgementId`: An optional subscriber acknowledgement id.
- `destinationCommitId`: An optional destination commit id.

<a id="member-m-cephalon-eventing-services-eventdispatchdeliverycompletionmetadata-iscompleted-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `IsCompleted`

```csharp
bool IsCompleted(IReadOnlyDictionary<string, string> metadata)
```

Gets a value indicating whether the metadata contains provider-reported downstream delivery-completion proof.

Returns: `true` when provider-reported delivery-completion proof is present; otherwise, `false`.

Parameters:
- `metadata`: The dispatch metadata dictionary to inspect.

<a id="member-m-cephalon-eventing-services-eventdispatchdeliverycompletionmetadata-tryapplymetadata-system-collections-generic-idictionary-system-string-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `TryApplyMetadata`

```csharp
bool TryApplyMetadata(IDictionary<string, string> metadata, string outcome, string source, string providerReceiptId, string subscriberAcknowledgementId, string destinationCommitId)
```

Applies provider-reported delivery-completion proof to an existing dispatch metadata dictionary when the inputs support it.

Returns: `true` when delivery-completion proof was applied; otherwise, `false`.

Parameters:
- `metadata`: The dispatch metadata dictionary to enrich.
- `outcome`: The dispatch report outcome associated with the metadata.
- `source`: The stable provider or runtime source that reported delivery completion.
- `providerReceiptId`: The provider delivery receipt id.
- `subscriberAcknowledgementId`: An optional subscriber acknowledgement id.
- `destinationCommitId`: An optional destination commit id.

<a id="type-cephalon-eventing-services-eventdispatchexactlyoncedeliveryproofmetadata"></a>

### `EventDispatchExactlyOnceDeliveryProofMetadata`

Builds provider-reported exactly-once delivery proof metadata for successful dispatch reports.

Remarks: Exactly-once delivery is a stronger claim than dispatch success, provider receipt, subscriber acknowledgement, or destination commit by themselves. This helper records the claim only when a provider/runtime supplies all completion evidence plus an explicit exactly-once proof id. Cephalon carries the proof without making any provider package, including Wolverine, part of the core authoring surface.

#### Declaration
```csharp
public static class EventDispatchExactlyOnceDeliveryProofMetadata
```

#### Methods

<a id="member-m-cephalon-eventing-services-eventdispatchexactlyoncedeliveryproofmetadata-createmetadata-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `CreateMetadata`

```csharp
Dictionary<string, string> CreateMetadata(IReadOnlyDictionary<string, string> metadata, string outcome, string source, string providerReceiptId, string subscriberAcknowledgementId, string destinationCommitId, string exactlyOnceProofId, string strategy)
```

Creates a metadata copy enriched with provider-reported exactly-once delivery proof when the inputs support it.

Returns: A case-insensitive metadata dictionary containing the original values plus exactly-once proof when applicable.

Parameters:
- `metadata`: The dispatch report metadata to copy.
- `outcome`: The dispatch report outcome associated with the metadata.
- `source`: The stable provider or runtime source that reported the exactly-once proof.
- `providerReceiptId`: The provider delivery receipt id.
- `subscriberAcknowledgementId`: The subscriber acknowledgement id.
- `destinationCommitId`: The destination commit id.
- `exactlyOnceProofId`: The provider exactly-once delivery proof id.
- `strategy`: An optional provider strategy name for the exactly-once proof.

<a id="member-m-cephalon-eventing-services-eventdispatchexactlyoncedeliveryproofmetadata-createreport-cephalon-eventing-services-eventdispatchexecutionreport-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `CreateReport`

```csharp
EventDispatchExecutionReport CreateReport(EventDispatchExecutionReport report, string source, string providerReceiptId, string subscriberAcknowledgementId, string destinationCommitId, string exactlyOnceProofId, string strategy)
```

Creates a dispatch report copy enriched with provider-reported exactly-once delivery proof when the inputs support it.

Returns: A dispatch report containing the original metadata plus exactly-once proof when applicable.

Parameters:
- `report`: The successful dispatch report to copy.
- `source`: The stable provider or runtime source that reported the exactly-once proof.
- `providerReceiptId`: The provider delivery receipt id.
- `subscriberAcknowledgementId`: The subscriber acknowledgement id.
- `destinationCommitId`: The destination commit id.
- `exactlyOnceProofId`: The provider exactly-once delivery proof id.
- `strategy`: An optional provider strategy name for the exactly-once proof.

<a id="member-m-cephalon-eventing-services-eventdispatchexactlyoncedeliveryproofmetadata-isproviderproven-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `IsProviderProven`

```csharp
bool IsProviderProven(IReadOnlyDictionary<string, string> metadata)
```

Gets a value indicating whether the metadata contains provider-proven exactly-once delivery proof.

Returns: `true` when provider-proven exactly-once proof is present; otherwise, `false`.

Parameters:
- `metadata`: The dispatch metadata dictionary to inspect.

<a id="member-m-cephalon-eventing-services-eventdispatchexactlyoncedeliveryproofmetadata-tryapplymetadata-system-collections-generic-idictionary-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `TryApplyMetadata`

```csharp
bool TryApplyMetadata(IDictionary<string, string> metadata, string outcome, string source, string providerReceiptId, string subscriberAcknowledgementId, string destinationCommitId, string exactlyOnceProofId, string strategy)
```

Applies provider-reported exactly-once delivery proof to an existing dispatch metadata dictionary when the inputs support it.

Returns: `true` when exactly-once proof was applied; otherwise, `false`.

Parameters:
- `metadata`: The dispatch metadata dictionary to enrich.
- `outcome`: The dispatch report outcome associated with the metadata.
- `source`: The stable provider or runtime source that reported the exactly-once proof.
- `providerReceiptId`: The provider delivery receipt id.
- `subscriberAcknowledgementId`: The subscriber acknowledgement id.
- `destinationCommitId`: The destination commit id.
- `exactlyOnceProofId`: The provider exactly-once delivery proof id.
- `strategy`: An optional provider strategy name for the exactly-once proof.

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

<a id="type-cephalon-eventing-services-eventdispatchproviderbrokercontextheaders"></a>

### `EventDispatchProviderBrokerContextHeaders`

Projects staged Cephalon event context into provider-neutral headers before a dispatch runtime hands a message to a provider or broker.

Remarks: The helper only creates stable Cephalon context headers from an `EventDispatchItem`. It does not claim that a provider persisted the headers, that a consumer extracted them, or that cross-node handoff has completed.

#### Declaration
```csharp
public static class EventDispatchProviderBrokerContextHeaders
```

#### Methods

<a id="member-m-cephalon-eventing-services-eventdispatchproviderbrokercontextheaders-applyreportmetadata-system-collections-generic-idictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ApplyReportMetadata`

```csharp
void ApplyReportMetadata(IDictionary<string, string> metadata, IReadOnlyDictionary<string, string> providerBrokerHeaders)
```

Adds conservative dispatch-report metadata for a projected provider or broker context-header set.

Parameters:
- `metadata`: The dispatch report metadata dictionary to enrich.
- `providerBrokerHeaders`: The headers created for the provider or broker handoff.

<a id="member-m-cephalon-eventing-services-eventdispatchproviderbrokercontextheaders-create-cephalon-eventing-services-eventdispatchitem"></a>

##### `Create`

```csharp
Dictionary<string, string> Create(EventDispatchItem dispatchItem)
```

Creates a deterministic set of Cephalon context headers for a pending dispatch item.

Returns: A case-insensitive dictionary of provider-neutral context headers.

Parameters:
- `dispatchItem`: The pending dispatch item whose staged context should be projected.

<a id="type-cephalon-eventing-services-eventdispatchprovidercontextpersistencemetadata"></a>

### `EventDispatchProviderContextPersistenceMetadata`

Builds truthful provider-side context-persistence metadata for dispatch reports that were durably applied by a dispatch store.

Remarks: The helper only claims provider-side persistence when provider or broker context headers were already projected with Cephalon propagation headers. It keeps cross-node handoff unclaimed because persistence inside a dispatch store does not prove that a different node consumed the propagated context.

#### Declaration
```csharp
public static class EventDispatchProviderContextPersistenceMetadata
```

#### Methods

<a id="member-m-cephalon-eventing-services-eventdispatchprovidercontextpersistencemetadata-createmetadata-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string"></a>

##### `CreateMetadata`

```csharp
Dictionary<string, string> CreateMetadata(IReadOnlyDictionary<string, string> metadata, string source)
```

Creates a metadata copy enriched with provider-side context-persistence proof when the source metadata supports it.

Returns: A case-insensitive metadata dictionary containing the original values plus persistence proof when applicable.

Parameters:
- `metadata`: The dispatch report metadata to copy.
- `source`: The stable dispatch-store or provider identifier that persisted the context proof.

<a id="member-m-cephalon-eventing-services-eventdispatchprovidercontextpersistencemetadata-createreport-cephalon-eventing-services-eventdispatchexecutionreport-system-string"></a>

##### `CreateReport`

```csharp
EventDispatchExecutionReport CreateReport(EventDispatchExecutionReport report, string source)
```

Creates a dispatch report copy enriched with provider-side context-persistence proof when the source metadata supports it.

Returns: A dispatch report containing the original metadata plus provider-side persistence proof when applicable.

Parameters:
- `report`: The dispatch report that was already applied by a capable provider-side dispatch store.
- `source`: The stable dispatch-store or provider identifier that persisted the context proof.

<a id="member-m-cephalon-eventing-services-eventdispatchprovidercontextpersistencemetadata-ispersisted-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `IsPersisted`

```csharp
bool IsPersisted(IReadOnlyDictionary<string, string> metadata)
```

Gets a value indicating whether the metadata contains provider-side context-persistence proof.

Returns: `true` when the provider-side persistence proof is present; otherwise, `false`.

Parameters:
- `metadata`: The metadata dictionary to inspect.

<a id="member-m-cephalon-eventing-services-eventdispatchprovidercontextpersistencemetadata-tryapplymetadata-system-collections-generic-idictionary-system-string-system-string-system-string"></a>

##### `TryApplyMetadata`

```csharp
bool TryApplyMetadata(IDictionary<string, string> metadata, string source)
```

Applies provider-side context-persistence proof to an existing metadata dictionary when projected context headers are present.

Returns: `true` when provider-side persistence metadata was applied; otherwise, `false`.

Parameters:
- `metadata`: The metadata dictionary to enrich.
- `source`: The stable dispatch-store or provider identifier that persisted the context proof.

<a id="type-cephalon-eventing-services-eventdispatchremediationmetadatakeys"></a>

### `EventDispatchRemediationMetadataKeys`

Defines stable metadata keys used by event-dispatch remediation command results.

Remarks: These keys appear in command-result records, remediation runtime surfaces, and rejected duplicate command responses so operators can reason about command idempotency without parsing provider-specific metadata.

#### Declaration
```csharp
public static class EventDispatchRemediationMetadataKeys
```

#### Fields

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-commandidempotencypolicy"></a>

##### `CommandIdempotencyPolicy`

```csharp
const string CommandIdempotencyPolicy
```

Identifies the command idempotency policy enforced by the active remediation dispatcher.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-commandreservationduplicatepolicy"></a>

##### `CommandReservationDuplicatePolicy`

```csharp
const string CommandReservationDuplicatePolicy
```

Identifies how duplicate reservations are handled by the active command journal.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-commandreservationindoubtoutcome"></a>

##### `CommandReservationInDoubtOutcome`

```csharp
const string CommandReservationInDoubtOutcome
```

Identifies the command outcome used when a reservation exists before finalization.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-commandreservationowner"></a>

##### `CommandReservationOwner`

```csharp
const string CommandReservationOwner
```

Identifies the component that owns command reservations.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-commandreservationpolicy"></a>

##### `CommandReservationPolicy`

```csharp
const string CommandReservationPolicy
```

Identifies the reservation policy enforced before dispatch-store mutation.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-commandreservationstate"></a>

##### `CommandReservationState`

```csharp
const string CommandReservationState
```

Identifies the current command-reservation state visible in command metadata.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-commandreservationtiming"></a>

##### `CommandReservationTiming`

```csharp
const string CommandReservationTiming
```

Identifies when the command identifier is reserved relative to dispatch-store mutation.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-duplicatecommand"></a>

##### `DuplicateCommand`

```csharp
const string DuplicateCommand
```

Identifies whether the current response describes a duplicate command request.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-duplicatecommandpolicy"></a>

##### `DuplicateCommandPolicy`

```csharp
const string DuplicateCommandPolicy
```

Identifies how the active remediation dispatcher handles a duplicate command identifier.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-existingcommandobservedatutc"></a>

##### `ExistingCommandObservedAtUtc`

```csharp
const string ExistingCommandObservedAtUtc
```

Identifies the UTC timestamp recorded for the first command that used the duplicate command identifier.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-existingcommandoperationid"></a>

##### `ExistingCommandOperationId`

```csharp
const string ExistingCommandOperationId
```

Identifies the operation recorded for the first command that used the duplicate command identifier.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-existingcommandoutboxid"></a>

##### `ExistingCommandOutboxId`

```csharp
const string ExistingCommandOutboxId
```

Identifies the outbox recorded for the first command that used the duplicate command identifier.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-existingcommandoutcome"></a>

##### `ExistingCommandOutcome`

```csharp
const string ExistingCommandOutcome
```

Identifies the outcome recorded for the first command that used the duplicate command identifier.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-operatoractorid"></a>

##### `OperatorActorId`

```csharp
const string OperatorActorId
```

Identifies the operator actor that requested the remediation command.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-operatorcommandreason"></a>

##### `OperatorCommandReason`

```csharp
const string OperatorCommandReason
```

Identifies the operator-facing reason attached to the remediation command.

<a id="member-f-cephalon-eventing-services-eventdispatchremediationmetadatakeys-operatorcorrelationid"></a>

##### `OperatorCorrelationId`

```csharp
const string OperatorCorrelationId
```

Identifies the operator correlation identifier attached to the remediation command.

<a id="type-cephalon-eventing-services-eventdispatchruntimemetadatakeys"></a>

### `EventDispatchRuntimeMetadataKeys`

Defines stable metadata keys used by event-dispatch runtime observations.

Remarks: These keys appear in dispatch runtime reports and the derived event-dispatch runtime surfaces so operators and dispatch stores can distinguish retryable failures from terminal failures without parsing provider-specific metadata.

#### Declaration
```csharp
public static class EventDispatchRuntimeMetadataKeys
```

#### Fields

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-brokerdeadletter"></a>

##### `BrokerDeadLetter`

```csharp
const string BrokerDeadLetter
```

Identifies whether the dead-letter decision is owned by a broker-specific dead-letter queue.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-consumercontextextraction"></a>

##### `ConsumerContextExtraction`

```csharp
const string ConsumerContextExtraction
```

Identifies whether consumer-side extraction has been proven for the dispatched context.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-crossnodecontexthandoff"></a>

##### `CrossNodeContextHandoff`

```csharp
const string CrossNodeContextHandoff
```

Identifies whether cross-node context handoff has been proven for the dispatched context.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-crossnodecontexthandoffconsumernodeid"></a>

##### `CrossNodeContextHandoffConsumerNodeId`

```csharp
const string CrossNodeContextHandoffConsumerNodeId
```

Identifies the consumer-side node observed for a proven cross-node context handoff.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-crossnodecontexthandoffheadercount"></a>

##### `CrossNodeContextHandoffHeaderCount`

```csharp
const string CrossNodeContextHandoffHeaderCount
```

Identifies the number of Cephalon context headers observed during a proven cross-node handoff.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-crossnodecontexthandoffheadernames"></a>

##### `CrossNodeContextHandoffHeaderNames`

```csharp
const string CrossNodeContextHandoffHeaderNames
```

Identifies the comma-separated Cephalon context header names observed during a proven cross-node handoff.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-crossnodecontexthandoffproducernodeid"></a>

##### `CrossNodeContextHandoffProducerNodeId`

```csharp
const string CrossNodeContextHandoffProducerNodeId
```

Identifies the producer-side node observed for a proven cross-node context handoff.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-crossnodecontexthandoffsource"></a>

##### `CrossNodeContextHandoffSource`

```csharp
const string CrossNodeContextHandoffSource
```

Identifies the provider or runtime observation source that proved cross-node context handoff.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-deadletterdurability"></a>

##### `DeadLetterDurability`

```csharp
const string DeadLetterDurability
```

Identifies where the dead-letter decision is persisted.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-deadletteroutcome"></a>

##### `DeadLetterOutcome`

```csharp
const string DeadLetterOutcome
```

Identifies the dead-letter decision represented by the latest operator observation.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-deadletterscope"></a>

##### `DeadLetterScope`

```csharp
const string DeadLetterScope
```

Identifies the scope that owns the dead-letter decision.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-destinationcommit"></a>

##### `DestinationCommit`

```csharp
const string DestinationCommit
```

Identifies whether destination commit proof was reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-destinationcommitid"></a>

##### `DestinationCommitId`

```csharp
const string DestinationCommitId
```

Identifies the destination commit id reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-dispatchcontextheadercount"></a>

##### `DispatchContextHeaderCount`

```csharp
const string DispatchContextHeaderCount
```

Identifies the number of context-capable headers present on the dispatch item used by the report.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-dispatchcontextmetadata"></a>

##### `DispatchContextMetadata`

```csharp
const string DispatchContextMetadata
```

Identifies whether the latest dispatch runtime observation includes Cephalon context metadata.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-dispatchcontextmetadatacount"></a>

##### `DispatchContextMetadataCount`

```csharp
const string DispatchContextMetadataCount
```

Identifies the number of context metadata entries carried from the dispatch item into the report.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-downstreamdeliverycompletion"></a>

##### `DownstreamDeliveryCompletion`

```csharp
const string DownstreamDeliveryCompletion
```

Identifies whether provider-reported downstream delivery completion has been proven for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-downstreamdeliverycompletionsource"></a>

##### `DownstreamDeliveryCompletionSource`

```csharp
const string DownstreamDeliveryCompletionSource
```

Identifies the provider or runtime source that reported downstream delivery completion.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-durabledispatchcontextpropagation"></a>

##### `DurableDispatchContextPropagation`

```csharp
const string DurableDispatchContextPropagation
```

Identifies the durable dispatch context propagation boundary proven by the latest runtime observation.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-exactlyoncedelivery"></a>

##### `ExactlyOnceDelivery`

```csharp
const string ExactlyOnceDelivery
```

Identifies whether exactly-once delivery proof was reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-exactlyoncedeliveryproofid"></a>

##### `ExactlyOnceDeliveryProofId`

```csharp
const string ExactlyOnceDeliveryProofId
```

Identifies the provider exactly-once delivery proof id reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-exactlyoncedeliverysource"></a>

##### `ExactlyOnceDeliverySource`

```csharp
const string ExactlyOnceDeliverySource
```

Identifies the provider or runtime source that reported exactly-once delivery proof.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-exactlyoncedeliverystrategy"></a>

##### `ExactlyOnceDeliveryStrategy`

```csharp
const string ExactlyOnceDeliveryStrategy
```

Identifies the provider exactly-once delivery strategy reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-nextretryatutc"></a>

##### `NextRetryAtUtc`

```csharp
const string NextRetryAtUtc
```

Identifies the next UTC time when a retryable dispatch failure should become eligible again.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providerbrokercontextheadercount"></a>

##### `ProviderBrokerContextHeaderCount`

```csharp
const string ProviderBrokerContextHeaderCount
```

Identifies the number of Cephalon context headers projected toward the provider or broker boundary.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providerbrokercontextheadernames"></a>

##### `ProviderBrokerContextHeaderNames`

```csharp
const string ProviderBrokerContextHeaderNames
```

Identifies the comma-separated Cephalon context header names projected toward the provider or broker boundary.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providerbrokercontextheaderprojection"></a>

##### `ProviderBrokerContextHeaderProjection`

```csharp
const string ProviderBrokerContextHeaderProjection
```

Identifies the provider-neutral projection used for provider or broker context headers.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providerbrokercontextheaders"></a>

##### `ProviderBrokerContextHeaders`

```csharp
const string ProviderBrokerContextHeaders
```

Identifies whether provider or broker headers carry the same context beyond Cephalon dispatch metadata.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providerdeliveryreceipt"></a>

##### `ProviderDeliveryReceipt`

```csharp
const string ProviderDeliveryReceipt
```

Identifies whether a provider delivery receipt was reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providerdeliveryreceiptid"></a>

##### `ProviderDeliveryReceiptId`

```csharp
const string ProviderDeliveryReceiptId
```

Identifies the provider delivery receipt id reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providersidecontextpersistence"></a>

##### `ProviderSideContextPersistence`

```csharp
const string ProviderSideContextPersistence
```

Identifies whether the active provider-side dispatch store persisted the projected Cephalon context proof.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providersidecontextpersistenceheadercount"></a>

##### `ProviderSideContextPersistenceHeaderCount`

```csharp
const string ProviderSideContextPersistenceHeaderCount
```

Identifies the number of Cephalon context headers persisted by the provider-side dispatch store proof.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providersidecontextpersistenceheadernames"></a>

##### `ProviderSideContextPersistenceHeaderNames`

```csharp
const string ProviderSideContextPersistenceHeaderNames
```

Identifies the comma-separated Cephalon context header names persisted by the provider-side dispatch store proof.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-providersidecontextpersistencesource"></a>

##### `ProviderSideContextPersistenceSource`

```csharp
const string ProviderSideContextPersistenceSource
```

Identifies the provider-side dispatch store that persisted the projected Cephalon context proof.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-retrydelayseconds"></a>

##### `RetryDelaySeconds`

```csharp
const string RetryDelaySeconds
```

Identifies the retry delay in seconds when the active dispatch runtime uses a delayed retry policy.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-retrydurability"></a>

##### `RetryDurability`

```csharp
const string RetryDurability
```

Identifies where retry eligibility is persisted.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-retryexhausted"></a>

##### `RetryExhausted`

```csharp
const string RetryExhausted
```

Identifies whether the retry budget was exhausted for the latest observation.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-retrymaxattempts"></a>

##### `RetryMaxAttempts`

```csharp
const string RetryMaxAttempts
```

Identifies the maximum number of dispatch attempts allowed for one staged message.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-retryoutcome"></a>

##### `RetryOutcome`

```csharp
const string RetryOutcome
```

Identifies the retry decision represented by the latest observation.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-retrypolicy"></a>

##### `RetryPolicy`

```csharp
const string RetryPolicy
```

Identifies the retry policy applied by the active dispatch runtime.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-retryscope"></a>

##### `RetryScope`

```csharp
const string RetryScope
```

Identifies who owns the retry policy.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-subscriberacknowledgement"></a>

##### `SubscriberAcknowledgement`

```csharp
const string SubscriberAcknowledgement
```

Identifies whether subscriber acknowledgement was reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-subscriberacknowledgementid"></a>

##### `SubscriberAcknowledgementId`

```csharp
const string SubscriberAcknowledgementId
```

Identifies the subscriber acknowledgement id reported for the dispatch.

<a id="member-f-cephalon-eventing-services-eventdispatchruntimemetadatakeys-terminalfailure"></a>

##### `TerminalFailure`

```csharp
const string TerminalFailure
```

Identifies whether the latest failure should stop re-entering pending-dispatch reads.

#### Methods

<a id="member-m-cephalon-eventing-services-eventdispatchruntimemetadatakeys-isterminalfailure-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `IsTerminalFailure`

```csharp
bool IsTerminalFailure(IReadOnlyDictionary<string, string> metadata)
```

Gets a value indicating whether the supplied metadata describes a terminal failure.

Returns: `true` when either `TerminalFailure` or `RetryExhausted` is set to `true`; otherwise, `false`.

Parameters:
- `metadata`: The dispatch observation metadata to inspect.

<a id="type-cephalon-eventing-services-eventingdiagnostics"></a>

### `EventingDiagnostics`

Defines the stable activity source, meter, activity, counter, and tag names emitted by the eventing companion runtime. Names are sourced from `Eventing` and `Eventing` so the eventing pack and observability companion packs share one canonical name set with the rest of the engine.

#### Declaration
```csharp
public static class EventingDiagnostics
```

#### Fields

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-activitysourcename"></a>

##### `ActivitySourceName`

```csharp
const string ActivitySourceName
```

Gets the stable activity-source name emitted by the eventing runtime.

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-channelidtag"></a>

##### `ChannelIdTag`

```csharp
const string ChannelIdTag
```

Stable Cephalon-prefix tag carrying the channel identifier emitted on the activity.

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-eventtypetag"></a>

##### `EventTypeTag`

```csharp
const string EventTypeTag
```

Stable Cephalon-prefix tag carrying the event type emitted on the activity.

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-matchedsubscriptioncounttag"></a>

##### `MatchedSubscriptionCountTag`

```csharp
const string MatchedSubscriptionCountTag
```

Stable Cephalon-prefix tag carrying the count of subscriptions matched for the publication.

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-metername"></a>

##### `MeterName`

```csharp
const string MeterName
```

Gets the stable meter name emitted by the eventing runtime.

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-publicationdispatchactivityname"></a>

##### `PublicationDispatchActivityName`

```csharp
const string PublicationDispatchActivityName
```

Gets the stable activity name emitted around one in-process event publication dispatch.

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-publicationdispatchcountername"></a>

##### `PublicationDispatchCounterName`

```csharp
const string PublicationDispatchCounterName
```

Gets the stable counter name for completed in-process publication dispatches.

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-publicationidtag"></a>

##### `PublicationIdTag`

```csharp
const string PublicationIdTag
```

Stable Cephalon-prefix tag carrying the publication identifier emitted on the activity.

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-publicationoutcometag"></a>

##### `PublicationOutcomeTag`

```csharp
const string PublicationOutcomeTag
```

Stable Cephalon-prefix tag carrying the publication outcome emitted on the activity (succeeded, skipped, or failed).

<a id="member-f-cephalon-eventing-services-eventingdiagnostics-publisheridtag"></a>

##### `PublisherIdTag`

```csharp
const string PublisherIdTag
```

Stable Cephalon-prefix tag carrying the publisher identifier responsible for the dispatch.

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

<a id="type-cephalon-eventing-services-eventschemaregistrydescriptor"></a>

### `EventSchemaRegistryDescriptor`

Describes provider-neutral schema registry availability for event serializers.

Remarks: The descriptor is intentionally metadata-only. It lets hosts and modules expose schema registry availability without putting schema lookup, payload serialization, or compatibility validation on the publication hot path.

#### Declaration
```csharp
public sealed class EventSchemaRegistryDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventschemaregistrydescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-boolean-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventSchemaRegistryDescriptor`

```csharp
EventSchemaRegistryDescriptor(string id, string displayName, string description, string provider, string endpointKind, string runtimeKind, bool canReadSchemas, bool canWriteSchemas, bool validatesCompatibility, IReadOnlyList<string> supportedFormats, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new event schema registry descriptor.

Parameters:
- `id`: The stable schema registry identifier used by serializers.
- `displayName`: The operator-facing schema registry name.
- `description`: The human-readable schema registry description.
- `provider`: The provider or product family for the registry.
- `endpointKind`: The endpoint kind, such as `managed`, `embedded`, or `external`.
- `runtimeKind`: The runtime implementation kind, such as `code-first` or `provider-managed`.
- `canReadSchemas`: Whether the runtime can read schemas from the registry.
- `canWriteSchemas`: Whether the runtime can write schemas to the registry.
- `validatesCompatibility`: Whether the runtime validates schema compatibility.
- `supportedFormats`: Optional serialization formats supported by the registry.
- `tags`: Optional tags that classify the registry.
- `metadata`: Optional schema registry metadata.

#### Properties

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-canreadschemas"></a>

##### `CanReadSchemas`

```csharp
bool CanReadSchemas { get; }
```

Gets a value indicating whether schemas can be read from the registry.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-canwriteschemas"></a>

##### `CanWriteSchemas`

```csharp
bool CanWriteSchemas { get; }
```

Gets a value indicating whether schemas can be written to the registry.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable registry description.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the registry.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-endpointkind"></a>

##### `EndpointKind`

```csharp
string EndpointKind { get; }
```

Gets the endpoint kind exposed by the registry.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable schema registry identifier used by serializers.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets normalized metadata associated with the registry.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the provider or product family for the registry.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-runtimekind"></a>

##### `RuntimeKind`

```csharp
string RuntimeKind { get; }
```

Gets the registry runtime implementation kind.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-supportedformats"></a>

##### `SupportedFormats`

```csharp
IReadOnlyList<string> SupportedFormats { get; }
```

Gets the normalized serialization formats supported by the registry.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the registry.

<a id="member-p-cephalon-eventing-services-eventschemaregistrydescriptor-validatescompatibility"></a>

##### `ValidatesCompatibility`

```csharp
bool ValidatesCompatibility { get; }
```

Gets a value indicating whether the registry runtime validates compatibility.

<a id="type-cephalon-eventing-services-eventserializerdescriptor"></a>

### `EventSerializerDescriptor`

Describes a provider-neutral serializer runtime that can be selected by event contracts.

Remarks: The descriptor is intentionally code-first. It lets modules, source generators, or hosts declare serializer availability without forcing publish or subscription handler selection through string configuration.

#### Declaration
```csharp
public sealed class EventSerializerDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventserializerdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-boolean-system-boolean-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventSerializerDescriptor`

```csharp
EventSerializerDescriptor(string id, string displayName, string description, string contentType, string format, string runtimeKind, bool canRead, bool canWrite, bool requiresSchemaRegistry, string schemaRegistryId, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new event serializer descriptor.

Parameters:
- `id`: The stable serializer identifier used by event contracts.
- `displayName`: The operator-facing serializer name.
- `description`: The human-readable serializer description.
- `contentType`: The primary wire content type produced or consumed by the serializer.
- `format`: The provider-neutral serialization format, such as `json`, `protobuf`, or `avro`.
- `runtimeKind`: The runtime implementation kind, such as `source-generated` or `custom`.
- `canRead`: Whether the serializer can deserialize payloads.
- `canWrite`: Whether the serializer can serialize payloads.
- `requiresSchemaRegistry`: Whether the serializer requires a schema registry before it can be used safely.
- `schemaRegistryId`: The optional schema registry identifier required by the serializer.
- `tags`: Optional tags that classify the serializer.
- `metadata`: Optional serializer metadata.

#### Properties

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-canread"></a>

##### `CanRead`

```csharp
bool CanRead { get; }
```

Gets a value indicating whether the serializer can deserialize payloads.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-canwrite"></a>

##### `CanWrite`

```csharp
bool CanWrite { get; }
```

Gets a value indicating whether the serializer can serialize payloads.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; }
```

Gets the primary wire content type produced or consumed by the serializer.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable serializer description.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the serializer.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-format"></a>

##### `Format`

```csharp
string Format { get; }
```

Gets the provider-neutral serialization format.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable serializer identifier used by event contracts.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets normalized metadata associated with the serializer.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-requiresschemaregistry"></a>

##### `RequiresSchemaRegistry`

```csharp
bool RequiresSchemaRegistry { get; }
```

Gets a value indicating whether the serializer needs schema-registry support before use.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-runtimekind"></a>

##### `RuntimeKind`

```csharp
string RuntimeKind { get; }
```

Gets the serializer runtime implementation kind.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-schemaregistryid"></a>

##### `SchemaRegistryId`

```csharp
string SchemaRegistryId { get; }
```

Gets the optional schema registry identifier required by the serializer.

<a id="member-p-cephalon-eventing-services-eventserializerdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the serializer.

<a id="type-cephalon-eventing-services-eventsubscriptionattribute"></a>

### `EventSubscriptionAttribute`

Declares the subscription descriptor owned by an in-process event subscription executor.

Remarks: Apply this attribute to a concrete `IEventSubscriptionExecutor` implementation when the subscription metadata is static and can be discovered from code at startup. Richer dynamic descriptor metadata can still be supplied by implementing `IEventSubscriptionDescriptorProvider` directly.

#### Declaration
```csharp
public sealed class EventSubscriptionAttribute
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventsubscriptionattribute-ctor-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `EventSubscriptionAttribute`

```csharp
EventSubscriptionAttribute(string id, string displayName, string description, string channelId, string handlerId, string deliveryMode)
```

Creates a new event subscription descriptor attribute.

Parameters:
- `id`: The stable subscription identifier.
- `displayName`: The operator-facing subscription name.
- `description`: The human-readable description of the subscription.
- `channelId`: The logical event channel that the subscription consumes.
- `handlerId`: The logical handler or consumer identifier that receives the event.
- `deliveryMode`: The declared delivery mode for the subscription.

#### Properties

<a id="member-p-cephalon-eventing-services-eventsubscriptionattribute-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; }
```

Gets the logical event channel that the subscription consumes.

<a id="member-p-cephalon-eventing-services-eventsubscriptionattribute-deliverymode"></a>

##### `DeliveryMode`

```csharp
string DeliveryMode { get; }
```

Gets the declared delivery mode for the subscription.

<a id="member-p-cephalon-eventing-services-eventsubscriptionattribute-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the subscription.

<a id="member-p-cephalon-eventing-services-eventsubscriptionattribute-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the subscription.

<a id="member-p-cephalon-eventing-services-eventsubscriptionattribute-handlerid"></a>

##### `HandlerId`

```csharp
string HandlerId { get; }
```

Gets the logical handler or consumer identifier that receives the event.

<a id="member-p-cephalon-eventing-services-eventsubscriptionattribute-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable subscription identifier.

<a id="member-p-cephalon-eventing-services-eventsubscriptionattribute-tags"></a>

##### `Tags`

```csharp
string[] Tags { get; set; }
```

Gets or sets optional tags that classify the subscription.

<a id="type-cephalon-eventing-services-eventsubscriptionbrokerinboundconsumptionmetadata"></a>

### `EventSubscriptionBrokerInboundConsumptionMetadata`

Builds provider-reported broker inbound-consumption proof metadata for successful subscription reports.

Remarks: Declared subscriptions, direct in-process execution, hosted execution bindings, and optional provider adapters do not automatically prove that Cephalon owns a broker consumer loop. This helper records that stronger claim only when a provider/runtime reports a successful subscription observation with consumer-loop, acknowledgement, lease, retry/poison, and offset-checkpoint proof.

#### Declaration
```csharp
public static class EventSubscriptionBrokerInboundConsumptionMetadata
```

#### Methods

<a id="member-m-cephalon-eventing-services-eventsubscriptionbrokerinboundconsumptionmetadata-createmetadata-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `CreateMetadata`

```csharp
Dictionary<string, string> CreateMetadata(IReadOnlyDictionary<string, string> metadata, string outcome, string source, string consumerLoopId, string acknowledgementId, string leaseId, string retryPolicy, string poisonMessageHandling, string offsetCheckpointId)
```

Creates a metadata copy enriched with provider-reported broker inbound-consumption proof when the inputs support it.

Returns: A case-insensitive metadata dictionary containing the original values plus broker inbound-consumption proof when applicable.

Parameters:
- `metadata`: The subscription execution metadata to copy.
- `outcome`: The subscription execution outcome associated with the metadata.
- `source`: The stable provider or runtime source that reported broker inbound consumption.
- `consumerLoopId`: The provider consumer-loop proof id.
- `acknowledgementId`: The inbound acknowledgement proof id.
- `leaseId`: The consumer lease or ownership-token proof id.
- `retryPolicy`: The provider retry policy reported for inbound consumption.
- `poisonMessageHandling`: The poison-message handling posture reported by the provider.
- `offsetCheckpointId`: The consumer offset-checkpoint proof id.

<a id="member-m-cephalon-eventing-services-eventsubscriptionbrokerinboundconsumptionmetadata-createreport-cephalon-eventing-services-eventsubscriptionexecutionreport-system-string-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `CreateReport`

```csharp
EventSubscriptionExecutionReport CreateReport(EventSubscriptionExecutionReport report, string source, string consumerLoopId, string acknowledgementId, string leaseId, string retryPolicy, string poisonMessageHandling, string offsetCheckpointId)
```

Creates a subscription report copy enriched with provider-reported broker inbound-consumption proof when the inputs support it.

Returns: A subscription report containing the original metadata plus broker inbound-consumption proof when applicable.

Parameters:
- `report`: The successful subscription execution report to copy.
- `source`: The stable provider or runtime source that reported broker inbound consumption.
- `consumerLoopId`: The provider consumer-loop proof id.
- `acknowledgementId`: The inbound acknowledgement proof id.
- `leaseId`: The consumer lease or ownership-token proof id.
- `retryPolicy`: The provider retry policy reported for inbound consumption.
- `poisonMessageHandling`: The poison-message handling posture reported by the provider.
- `offsetCheckpointId`: The consumer offset-checkpoint proof id.

<a id="member-m-cephalon-eventing-services-eventsubscriptionbrokerinboundconsumptionmetadata-isbrokerconsumed-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `IsBrokerConsumed`

```csharp
bool IsBrokerConsumed(IReadOnlyDictionary<string, string> metadata)
```

Gets a value indicating whether the metadata contains complete provider-reported broker inbound-consumption proof.

Returns: `true` when complete broker inbound-consumption proof is present; otherwise, `false`.

Parameters:
- `metadata`: The subscription metadata dictionary to inspect.

<a id="member-m-cephalon-eventing-services-eventsubscriptionbrokerinboundconsumptionmetadata-tryapplymetadata-system-collections-generic-idictionary-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `TryApplyMetadata`

```csharp
bool TryApplyMetadata(IDictionary<string, string> metadata, string outcome, string source, string consumerLoopId, string acknowledgementId, string leaseId, string retryPolicy, string poisonMessageHandling, string offsetCheckpointId)
```

Applies provider-reported broker inbound-consumption proof to an existing subscription metadata dictionary when the inputs support it.

Returns: `true` when broker inbound-consumption proof was applied; otherwise, `false`.

Parameters:
- `metadata`: The subscription metadata dictionary to enrich.
- `outcome`: The subscription execution outcome associated with the metadata.
- `source`: The stable provider or runtime source that reported broker inbound consumption.
- `consumerLoopId`: The provider consumer-loop proof id.
- `acknowledgementId`: The inbound acknowledgement proof id.
- `leaseId`: The consumer lease or ownership-token proof id.
- `retryPolicy`: The provider retry policy reported for inbound consumption.
- `poisonMessageHandling`: The poison-message handling posture reported by the provider.
- `offsetCheckpointId`: The consumer offset-checkpoint proof id.

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

<a id="type-cephalon-eventing-services-eventsubscriptionexecutionstep"></a>

### `EventSubscriptionExecutionStep`

Represents the next step in the code-owned event subscription execution pipeline.

#### Declaration
```csharp
public sealed class EventSubscriptionExecutionStep
```

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

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-brokerconsumerloop"></a>

##### `BrokerConsumerLoop`

```csharp
const string BrokerConsumerLoop
```

Identifies whether a provider-owned broker consumer loop was reported.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-brokerconsumerloopid"></a>

##### `BrokerConsumerLoopId`

```csharp
const string BrokerConsumerLoopId
```

Identifies the provider consumer-loop proof id reported for broker inbound consumption.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-brokerinboundconsumption"></a>

##### `BrokerInboundConsumption`

```csharp
const string BrokerInboundConsumption
```

Identifies whether a provider or runtime reports ownership of inbound broker consumption.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-brokerinboundconsumptionsource"></a>

##### `BrokerInboundConsumptionSource`

```csharp
const string BrokerInboundConsumptionSource
```

Identifies the provider or runtime source that reported inbound broker consumption.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-channelid"></a>

##### `ChannelId`

```csharp
const string ChannelId
```

Identifies the logical event channel consumed by the declared subscription.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-consumercontextextraction"></a>

##### `ConsumerContextExtraction`

```csharp
const string ConsumerContextExtraction
```

Identifies whether the subscription runtime extracted Cephalon context before executing the consumer.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-consumercontextextractionsource"></a>

##### `ConsumerContextExtractionSource`

```csharp
const string ConsumerContextExtractionSource
```

Identifies the source used for consumer-side Cephalon context extraction.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-consumercontextheadercount"></a>

##### `ConsumerContextHeaderCount`

```csharp
const string ConsumerContextHeaderCount
```

Identifies the number of Cephalon context headers extracted before executing the consumer.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-consumercontextheadernames"></a>

##### `ConsumerContextHeaderNames`

```csharp
const string ConsumerContextHeaderNames
```

Identifies the comma-separated Cephalon context header names extracted before executing the consumer.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-consumerlease"></a>

##### `ConsumerLease`

```csharp
const string ConsumerLease
```

Identifies whether a consumer lease or ownership token was reported for broker consumption.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-consumerleaseid"></a>

##### `ConsumerLeaseId`

```csharp
const string ConsumerLeaseId
```

Identifies the consumer lease or ownership-token proof id reported for broker consumption.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-consumeroffsetcheckpoint"></a>

##### `ConsumerOffsetCheckpoint`

```csharp
const string ConsumerOffsetCheckpoint
```

Identifies whether a consumer offset checkpoint was reported for broker consumption.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-consumeroffsetcheckpointid"></a>

##### `ConsumerOffsetCheckpointId`

```csharp
const string ConsumerOffsetCheckpointId
```

Identifies the consumer offset-checkpoint proof id reported for broker consumption.

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

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-inboundacknowledgement"></a>

##### `InboundAcknowledgement`

```csharp
const string InboundAcknowledgement
```

Identifies whether inbound acknowledgement proof was reported for broker consumption.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-inboundacknowledgementid"></a>

##### `InboundAcknowledgementId`

```csharp
const string InboundAcknowledgementId
```

Identifies the inbound acknowledgement proof id reported for broker consumption.

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-inboundretrypolicy"></a>

##### `InboundRetryPolicy`

```csharp
const string InboundRetryPolicy
```

Identifies the provider retry policy reported for inbound broker consumption.

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

<a id="member-f-cephalon-eventing-services-eventsubscriptionruntimemetadatakeys-poisonmessagehandling"></a>

##### `PoisonMessageHandling`

```csharp
const string PoisonMessageHandling
```

Identifies the poison-message handling posture reported for inbound broker consumption.

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

<a id="type-cephalon-eventing-services-eventupcasterdescriptor"></a>

### `EventUpcasterDescriptor`

Describes provider-neutral event upcaster availability for one event type version transition.

Remarks: The descriptor is intentionally metadata-only. It lets hosts and modules expose version transition ownership without putting payload deserialization, schema lookup, or upcaster execution on the publication or subscription hot path.

#### Declaration
```csharp
public sealed class EventUpcasterDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventupcasterdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventUpcasterDescriptor`

```csharp
EventUpcasterDescriptor(string id, string eventType, string displayName, string description, string fromVersion, string toVersion, string runtimeKind, bool canUpcast, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new event upcaster descriptor.

Parameters:
- `id`: The stable upcaster identifier.
- `eventType`: The logical event type identifier handled by the upcaster.
- `displayName`: The operator-facing upcaster name.
- `description`: The human-readable upcaster description.
- `fromVersion`: The source event contract version.
- `toVersion`: The target event contract version.
- `runtimeKind`: The runtime implementation kind, such as `code-first` or `provider-managed`.
- `canUpcast`: Whether the runtime declares that this transition can be upcast.
- `tags`: Optional tags that classify the upcaster.
- `metadata`: Optional upcaster metadata.

#### Properties

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-canupcast"></a>

##### `CanUpcast`

```csharp
bool CanUpcast { get; }
```

Gets a value indicating whether the runtime declares that this transition can be upcast.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable upcaster description.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the upcaster.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-eventtype"></a>

##### `EventType`

```csharp
string EventType { get; }
```

Gets the logical event type identifier handled by the upcaster.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-fromversion"></a>

##### `FromVersion`

```csharp
string FromVersion { get; }
```

Gets the source event contract version.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable upcaster identifier.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets normalized metadata associated with the upcaster.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-runtimekind"></a>

##### `RuntimeKind`

```csharp
string RuntimeKind { get; }
```

Gets the upcaster runtime implementation kind.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the upcaster.

<a id="member-p-cephalon-eventing-services-eventupcasterdescriptor-toversion"></a>

##### `ToVersion`

```csharp
string ToVersion { get; }
```

Gets the target event contract version.

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

<a id="type-cephalon-eventing-services-ieventcontextpolicycatalog"></a>

### `IEventContextPolicyCatalog`

Provides read access to provider-neutral event context policy descriptors.

#### Declaration
```csharp
public interface IEventContextPolicyCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventcontextpolicycatalog-policies"></a>

##### `Policies`

```csharp
IReadOnlyList<EventContextPolicyDescriptor> Policies { get; }
```

Gets all context policy descriptors available to the active eventing runtime.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventcontextpolicycatalog-getbyheadername-system-string"></a>

##### `GetByHeaderName`

```csharp
IReadOnlyList<EventContextPolicyDescriptor> GetByHeaderName(string headerName)
```

Gets the context policies that cover a specific message-header name.

Returns: The matching context policies, or an empty list when no policy covers the header.

Parameters:
- `headerName`: The message-header name to match.

<a id="member-m-cephalon-eventing-services-ieventcontextpolicycatalog-tryget-system-string-cephalon-eventing-services-eventcontextpolicydescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string policyId, out EventContextPolicyDescriptor policy)
```

Tries to get a context policy by identifier.

Returns: `true` when the context policy was found; otherwise `false`.

Parameters:
- `policyId`: The context policy identifier.
- `policy`: When this method returns, contains the matched context policy.

<a id="type-cephalon-eventing-services-ieventcontextpolicycontributor"></a>

### `IEventContextPolicyContributor`

Allows a module to contribute event context policy metadata into the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventContextPolicyContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventcontextpolicycontributor-registereventcontextpolicies-cephalon-eventing-services-ieventcontextpolicyregistry"></a>

##### `RegisterEventContextPolicies`

```csharp
void RegisterEventContextPolicies(IEventContextPolicyRegistry policies)
```

Registers one or more event context policy descriptors with the supplied registry.

Parameters:
- `policies`: The registry that collects contributed event context policy descriptors.

<a id="type-cephalon-eventing-services-ieventcontextpolicyregistry"></a>

### `IEventContextPolicyRegistry`

Collects event context policy descriptors contributed by modules or hosts.

#### Declaration
```csharp
public interface IEventContextPolicyRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventcontextpolicyregistry-add-cephalon-eventing-services-eventcontextpolicydescriptor"></a>

##### `Add`

```csharp
void Add(EventContextPolicyDescriptor policy)
```

Adds an event context policy descriptor to the registry.

Parameters:
- `policy`: The context policy descriptor to add.

<a id="type-cephalon-eventing-services-ieventcontractcatalog"></a>

### `IEventContractCatalog`

Provides the merged event contract descriptors visible to the active eventing runtime.

#### Declaration
```csharp
public interface IEventContractCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventcontractcatalog-contracts"></a>

##### `Contracts`

```csharp
IReadOnlyList<EventContractDescriptor> Contracts { get; }
```

Gets all registered event contract descriptors.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventcontractcatalog-getbyeventtype-system-string"></a>

##### `GetByEventType`

```csharp
IReadOnlyList<EventContractDescriptor> GetByEventType(string eventType)
```

Gets all contract descriptors registered for the supplied event type.

Returns: The matching contract descriptors ordered by version.

Parameters:
- `eventType`: The logical event type identifier.

<a id="member-m-cephalon-eventing-services-ieventcontractcatalog-tryget-system-string-cephalon-eventing-services-eventcontractdescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string contractId, out EventContractDescriptor contract)
```

Attempts to resolve a contract by its stable identifier.

Returns: `true` when a contract with the identifier exists.

Parameters:
- `contractId`: The stable contract identifier.
- `contract`: When found, the matching contract descriptor.

<a id="member-m-cephalon-eventing-services-ieventcontractcatalog-trygetversion-system-string-system-string-cephalon-eventing-services-eventcontractdescriptor"></a>

##### `TryGetVersion`

```csharp
bool TryGetVersion(string eventType, string version, out EventContractDescriptor contract)
```

Attempts to resolve a specific contract version for an event type.

Returns: `true` when a contract with the event type and version exists.

Parameters:
- `eventType`: The logical event type identifier.
- `version`: The event contract version.
- `contract`: When found, the matching contract descriptor.

<a id="type-cephalon-eventing-services-ieventcontractcontributor"></a>

### `IEventContractContributor`

Allows a module to contribute event contract metadata into the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventContractContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventcontractcontributor-registereventcontracts-cephalon-eventing-services-ieventcontractregistry"></a>

##### `RegisterEventContracts`

```csharp
void RegisterEventContracts(IEventContractRegistry contracts)
```

Registers one or more event contract descriptors with the supplied registry.

Parameters:
- `contracts`: The registry that collects contributed event contract descriptors.

<a id="type-cephalon-eventing-services-ieventcontractregistry"></a>

### `IEventContractRegistry`

Collects event contract descriptors contributed by a host or module.

#### Declaration
```csharp
public interface IEventContractRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventcontractregistry-add-cephalon-eventing-services-eventcontractdescriptor"></a>

##### `Add`

```csharp
void Add(EventContractDescriptor contract)
```

Adds one event contract descriptor to the active eventing catalog.

Parameters:
- `contract`: The contract descriptor to add.

<a id="type-cephalon-eventing-services-ieventdispatchprovidercontextpersistencestore"></a>

### `IEventDispatchProviderContextPersistenceStore`

Marks an event dispatch store that can report persisted provider-side Cephalon context proof after applying a dispatch report.

Remarks: Dispatch runtimes should call `CreatePersistedContextReport` only after `ApplyReportAsync` succeeds. Implementations must not use this contract to claim consumer extraction, downstream delivery completion, or cross-node handoff.

#### Declaration
```csharp
public interface IEventDispatchProviderContextPersistenceStore
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventdispatchprovidercontextpersistencestore-createpersistedcontextreport-cephalon-eventing-services-eventdispatchexecutionreport"></a>

##### `CreatePersistedContextReport`

```csharp
EventDispatchExecutionReport CreatePersistedContextReport(EventDispatchExecutionReport report)
```

Creates a dispatch report copy containing provider-side context-persistence proof for a report the store already applied.

Returns: A dispatch report enriched with provider-side context-persistence metadata when the original report supports it.

Parameters:
- `report`: The dispatch report that was already applied by the dispatch store.

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

<a id="type-cephalon-eventing-services-ieventschemaregistrycatalog"></a>

### `IEventSchemaRegistryCatalog`

Provides the merged event schema registry descriptors visible to the active eventing runtime.

#### Declaration
```csharp
public interface IEventSchemaRegistryCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventschemaregistrycatalog-registries"></a>

##### `Registries`

```csharp
IReadOnlyList<EventSchemaRegistryDescriptor> Registries { get; }
```

Gets all registered event schema registry descriptors.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventschemaregistrycatalog-getbyformat-system-string"></a>

##### `GetByFormat`

```csharp
IReadOnlyList<EventSchemaRegistryDescriptor> GetByFormat(string format)
```

Gets all schema registries registered for the supplied serialization format.

Returns: The matching schema registries ordered by identifier.

Parameters:
- `format`: The serialization format.

<a id="member-m-cephalon-eventing-services-ieventschemaregistrycatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<EventSchemaRegistryDescriptor> GetByProvider(string provider)
```

Gets all schema registries registered for the supplied provider.

Returns: The matching schema registries ordered by identifier.

Parameters:
- `provider`: The provider or product family.

<a id="member-m-cephalon-eventing-services-ieventschemaregistrycatalog-tryget-system-string-cephalon-eventing-services-eventschemaregistrydescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string schemaRegistryId, out EventSchemaRegistryDescriptor registry)
```

Attempts to resolve a schema registry by its stable identifier.

Returns: `true` when a registry with the identifier exists.

Parameters:
- `schemaRegistryId`: The stable schema registry identifier.
- `registry`: When found, the matching schema registry descriptor.

<a id="member-m-cephalon-eventing-services-ieventschemaregistrycatalog-trygetforserializer-cephalon-eventing-services-eventserializerdescriptor-cephalon-eventing-services-eventschemaregistrydescriptor"></a>

##### `TryGetForSerializer`

```csharp
bool TryGetForSerializer(EventSerializerDescriptor serializer, out EventSchemaRegistryDescriptor registry)
```

Attempts to resolve the schema registry selected by an event serializer.

Returns: `true` when the serializer's schema registry identifier is available.

Parameters:
- `serializer`: The event serializer descriptor.
- `registry`: When found, the matching schema registry descriptor.

<a id="type-cephalon-eventing-services-ieventschemaregistrycontributor"></a>

### `IEventSchemaRegistryContributor`

Allows a module to contribute event schema registry metadata into the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventSchemaRegistryContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventschemaregistrycontributor-registereventschemaregistries-cephalon-eventing-services-ieventschemaregistryregistry"></a>

##### `RegisterEventSchemaRegistries`

```csharp
void RegisterEventSchemaRegistries(IEventSchemaRegistryRegistry registries)
```

Registers one or more event schema registry descriptors with the supplied registry.

Parameters:
- `registries`: The registry that collects contributed event schema registry descriptors.

<a id="type-cephalon-eventing-services-ieventschemaregistryregistry"></a>

### `IEventSchemaRegistryRegistry`

Collects event schema registry descriptors contributed by a host or module.

#### Declaration
```csharp
public interface IEventSchemaRegistryRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventschemaregistryregistry-add-cephalon-eventing-services-eventschemaregistrydescriptor"></a>

##### `Add`

```csharp
void Add(EventSchemaRegistryDescriptor registry)
```

Adds one event schema registry descriptor to the active eventing catalog.

Parameters:
- `registry`: The schema registry descriptor to add.

<a id="type-cephalon-eventing-services-ieventserializercatalog"></a>

### `IEventSerializerCatalog`

Provides the merged event serializer descriptors visible to the active eventing runtime.

#### Declaration
```csharp
public interface IEventSerializerCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventserializercatalog-serializers"></a>

##### `Serializers`

```csharp
IReadOnlyList<EventSerializerDescriptor> Serializers { get; }
```

Gets all registered event serializer descriptors.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventserializercatalog-getbycontenttype-system-string"></a>

##### `GetByContentType`

```csharp
IReadOnlyList<EventSerializerDescriptor> GetByContentType(string contentType)
```

Gets all serializer descriptors registered for the supplied content type.

Returns: The matching serializer descriptors ordered by identifier.

Parameters:
- `contentType`: The wire content type.

<a id="member-m-cephalon-eventing-services-ieventserializercatalog-tryget-system-string-cephalon-eventing-services-eventserializerdescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string serializerId, out EventSerializerDescriptor serializer)
```

Attempts to resolve a serializer by its stable identifier.

Returns: `true` when a serializer with the identifier exists.

Parameters:
- `serializerId`: The stable serializer identifier.
- `serializer`: When found, the matching serializer descriptor.

<a id="member-m-cephalon-eventing-services-ieventserializercatalog-trygetforcontract-cephalon-eventing-services-eventcontractdescriptor-cephalon-eventing-services-eventserializerdescriptor"></a>

##### `TryGetForContract`

```csharp
bool TryGetForContract(EventContractDescriptor contract, out EventSerializerDescriptor serializer)
```

Attempts to resolve the serializer selected by an event contract.

Returns: `true` when the contract's serializer identifier is available.

Parameters:
- `contract`: The event contract descriptor.
- `serializer`: When found, the matching serializer descriptor.

<a id="type-cephalon-eventing-services-ieventserializercontributor"></a>

### `IEventSerializerContributor`

Allows a module to contribute event serializer metadata into the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventSerializerContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventserializercontributor-registereventserializers-cephalon-eventing-services-ieventserializerregistry"></a>

##### `RegisterEventSerializers`

```csharp
void RegisterEventSerializers(IEventSerializerRegistry serializers)
```

Registers one or more event serializer descriptors with the supplied registry.

Parameters:
- `serializers`: The registry that collects contributed event serializer descriptors.

<a id="type-cephalon-eventing-services-ieventserializerregistry"></a>

### `IEventSerializerRegistry`

Collects event serializer descriptors contributed by a host or module.

#### Declaration
```csharp
public interface IEventSerializerRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventserializerregistry-add-cephalon-eventing-services-eventserializerdescriptor"></a>

##### `Add`

```csharp
void Add(EventSerializerDescriptor serializer)
```

Adds one event serializer descriptor to the active eventing catalog.

Parameters:
- `serializer`: The serializer descriptor to add.

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

<a id="type-cephalon-eventing-services-ieventsubscriptiondescriptorprovider"></a>

### `IEventSubscriptionDescriptorProvider`

Allows an in-process subscription executor to provide its declared subscription descriptor.

Remarks: Implement this optional interface on an `IEventSubscriptionExecutor` when the executor owns enough code-first metadata for the native eventing pack to register its descriptor automatically. This keeps subscription authoring typed and dependency-injection owned without binding handlers from configuration.

#### Declaration
```csharp
public interface IEventSubscriptionDescriptorProvider
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventsubscriptiondescriptorprovider-subscriptiondescriptor"></a>

##### `SubscriptionDescriptor`

```csharp
EventSubscriptionDescriptor SubscriptionDescriptor { get; }
```

Gets the code-owned subscription descriptor associated with the executor.

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

<a id="type-cephalon-eventing-services-ieventsubscriptionexecutionmiddleware"></a>

### `IEventSubscriptionExecutionMiddleware`

Adds a code-owned middleware step around direct in-process event subscription execution.

Remarks: Register implementations through dependency injection when a host or module needs a type-safe subscription execution pipeline. This contract is deliberately not configuration driven so publish/subscribe hot paths stay code-owned and avoid string-based handler binding.

#### Declaration
```csharp
public interface IEventSubscriptionExecutionMiddleware
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventsubscriptionexecutionmiddleware-invokeasync-cephalon-eventing-services-eventsubscriptionexecutioncontext-cephalon-eventing-services-eventsubscriptionexecutionstep-system-threading-cancellationtoken"></a>

##### `InvokeAsync`

```csharp
ValueTask InvokeAsync(EventSubscriptionExecutionContext context, EventSubscriptionExecutionStep nextStep, CancellationToken cancellationToken)
```

Invokes this middleware step and optionally forwards execution to the next step.

Returns: A task that completes when this middleware step finishes.

Parameters:
- `context`: The managed subscription execution context for the current attempt.
- `nextStep`: The next middleware or subscription executor in the pipeline.
- `cancellationToken`: The token that cancels the execution attempt.

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

<a id="type-cephalon-eventing-services-ieventupcastercatalog"></a>

### `IEventUpcasterCatalog`

Provides the merged event upcaster descriptors visible to the active eventing runtime.

#### Declaration
```csharp
public interface IEventUpcasterCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventupcastercatalog-upcasters"></a>

##### `Upcasters`

```csharp
IReadOnlyList<EventUpcasterDescriptor> Upcasters { get; }
```

Gets all registered event upcaster descriptors.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventupcastercatalog-getbyeventtype-system-string"></a>

##### `GetByEventType`

```csharp
IReadOnlyList<EventUpcasterDescriptor> GetByEventType(string eventType)
```

Gets all upcaster descriptors registered for the supplied event type.

Returns: The matching upcaster descriptors ordered by source version, target version, and identifier.

Parameters:
- `eventType`: The logical event type identifier.

<a id="member-m-cephalon-eventing-services-ieventupcastercatalog-getbysourceversion-system-string-system-string"></a>

##### `GetBySourceVersion`

```csharp
IReadOnlyList<EventUpcasterDescriptor> GetBySourceVersion(string eventType, string fromVersion)
```

Gets all upcaster descriptors registered for the supplied event type and source version.

Returns: The matching upcaster descriptors ordered by target version and identifier.

Parameters:
- `eventType`: The logical event type identifier.
- `fromVersion`: The source event contract version.

<a id="member-m-cephalon-eventing-services-ieventupcastercatalog-tryget-system-string-cephalon-eventing-services-eventupcasterdescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string upcasterId, out EventUpcasterDescriptor upcaster)
```

Attempts to resolve an upcaster by its stable identifier.

Returns: `true` when an upcaster with the identifier exists.

Parameters:
- `upcasterId`: The stable upcaster identifier.
- `upcaster`: When found, the matching upcaster descriptor.

<a id="member-m-cephalon-eventing-services-ieventupcastercatalog-trygettransition-system-string-system-string-system-string-cephalon-eventing-services-eventupcasterdescriptor"></a>

##### `TryGetTransition`

```csharp
bool TryGetTransition(string eventType, string fromVersion, string toVersion, out EventUpcasterDescriptor upcaster)
```

Attempts to resolve an upcaster for a specific event type version transition.

Returns: `true` when an upcaster with the event type and version transition exists.

Parameters:
- `eventType`: The logical event type identifier.
- `fromVersion`: The source event contract version.
- `toVersion`: The target event contract version.
- `upcaster`: When found, the matching upcaster descriptor.

<a id="type-cephalon-eventing-services-ieventupcastercontributor"></a>

### `IEventUpcasterContributor`

Allows a module to contribute event upcaster metadata into the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventUpcasterContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventupcastercontributor-registereventupcasters-cephalon-eventing-services-ieventupcasterregistry"></a>

##### `RegisterEventUpcasters`

```csharp
void RegisterEventUpcasters(IEventUpcasterRegistry upcasters)
```

Registers one or more event upcaster descriptors with the supplied registry.

Parameters:
- `upcasters`: The registry that collects contributed event upcaster descriptors.

<a id="type-cephalon-eventing-services-ieventupcasterregistry"></a>

### `IEventUpcasterRegistry`

Collects event upcaster descriptors contributed by a host or module.

#### Declaration
```csharp
public interface IEventUpcasterRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventupcasterregistry-add-cephalon-eventing-services-eventupcasterdescriptor"></a>

##### `Add`

```csharp
void Add(EventUpcasterDescriptor upcaster)
```

Adds one event upcaster descriptor to the active eventing catalog.

Parameters:
- `upcaster`: The upcaster descriptor to add.
