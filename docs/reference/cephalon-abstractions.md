# Cephalon.Abstractions

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Abstractions)
## Namespaces

- `Cephalon.Abstractions.AppModel`
- `Cephalon.Abstractions.AppModel.Scaffolding`
- `Cephalon.Abstractions.Audit`
- `Cephalon.Abstractions.Authorization`
- `Cephalon.Abstractions.Behaviors`
- `Cephalon.Abstractions.Capabilities`
- `Cephalon.Abstractions.Data`
- `Cephalon.Abstractions.EventSourcing`
- `Cephalon.Abstractions.Execution`
- `Cephalon.Abstractions.Features`
- `Cephalon.Abstractions.Health`
- `Cephalon.Abstractions.Ids`
- `Cephalon.Abstractions.Localization`
- `Cephalon.Abstractions.Modules`
- `Cephalon.Abstractions.Patterns`
- `Cephalon.Abstractions.Resilience`
- `Cephalon.Abstractions.Technologies`
- `Cephalon.Abstractions.Tenancy`
- `Cephalon.Abstractions.Transports`

<a id="namespace-cephalon-abstractions-appmodel"></a>

## Namespace Cephalon.Abstractions.AppModel

<a id="type-cephalon-abstractions-appmodel-appblueprint"></a>

### `AppBlueprint`

Describes a shipped Cephalon blueprint together with its baseline patterns and scaffold shape.

#### Declaration
```csharp
public sealed class AppBlueprint
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-appblueprint-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-patterns-patterndescriptor-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AppBlueprint`

```csharp
AppBlueprint(string id, string displayName, string description, IReadOnlyList<PatternDescriptor> patterns, IReadOnlyDictionary<string, string> metadata)
```

Creates a blueprint without scaffold metadata.

Parameters:
- `id`: The stable blueprint identifier.
- `displayName`: The human-readable blueprint name.
- `description`: The blueprint description.
- `patterns`: The baseline patterns implied by the blueprint.
- `metadata`: Optional blueprint metadata.

<a id="member-m-cephalon-abstractions-appmodel-appblueprint-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-patterns-patterndescriptor-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AppBlueprint`

```csharp
AppBlueprint(string id, string displayName, string description, IReadOnlyList<PatternDescriptor> patterns, ScaffoldPlan scaffold, IReadOnlyDictionary<string, string> metadata)
```

Creates a blueprint with optional scaffold metadata.

Parameters:
- `id`: The stable blueprint identifier.
- `displayName`: The human-readable blueprint name.
- `description`: The blueprint description.
- `patterns`: The baseline patterns implied by the blueprint.
- `scaffold`: The scaffold plan associated with the blueprint.
- `metadata`: Optional blueprint metadata.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the blueprint description.

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable blueprint name.

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable blueprint identifier.

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional blueprint metadata.

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-patterns"></a>

##### `Patterns`

```csharp
IReadOnlyList<PatternDescriptor> Patterns { get; }
```

Gets the baseline patterns implied by the blueprint.

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-scaffold"></a>

##### `Scaffold`

```csharp
ScaffoldPlan Scaffold { get; }
```

Gets the scaffold plan associated with the blueprint, when one is defined.

<a id="type-cephalon-abstractions-appmodel-appprofile"></a>

### `AppProfile`

Describes the resolved runtime profile selected for a Cephalon app.

#### Declaration
```csharp
public sealed class AppProfile
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-appprofile-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-patterns-patterndescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-transportdescriptor-cephalon-abstractions-appmodel-dataselection-cephalon-abstractions-appmodel-databasetopologyselection-cephalon-abstractions-appmodel-identityselection-cephalon-abstractions-appmodel-tenancyselection-cephalon-abstractions-appmodel-auditselection-cephalon-abstractions-appmodel-messagingselection-cephalon-abstractions-appmodel-resilienceselection"></a>

##### `AppProfile`

```csharp
AppProfile(string blueprintId, string blueprintDisplayName, string blueprintDescription, IReadOnlyList<PatternDescriptor> patterns, IReadOnlyList<TechnologyDescriptor> technologies, IReadOnlyList<TransportDescriptor> transports, DataSelection data, DatabaseTopologySelection databases, IdentitySelection identity, TenancySelection tenancy, AuditSelection audit, MessagingSelection messaging, ResilienceSelection resilience)
```

Creates an app profile without scaffold metadata.

Parameters:
- `blueprintId`: The selected blueprint identifier.
- `blueprintDisplayName`: The selected blueprint display name.
- `blueprintDescription`: The selected blueprint description.
- `patterns`: The patterns active for the app.
- `technologies`: The selected technology profiles.
- `transports`: The selected transports.
- `data`: The selected data inputs.
- `databases`: The selected database topology inputs.
- `identity`: The selected identity and authorization inputs.
- `tenancy`: The selected multi-tenancy inputs.
- `audit`: The selected audit inputs.
- `messaging`: The selected messaging inputs.
- `resilience`: The selected resilience-policy inputs.

<a id="member-m-cephalon-abstractions-appmodel-appprofile-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-patterns-patterndescriptor-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-transportdescriptor-cephalon-abstractions-appmodel-dataselection-cephalon-abstractions-appmodel-databasetopologyselection-cephalon-abstractions-appmodel-identityselection-cephalon-abstractions-appmodel-tenancyselection-cephalon-abstractions-appmodel-auditselection-cephalon-abstractions-appmodel-messagingselection-cephalon-abstractions-appmodel-resilienceselection"></a>

##### `AppProfile`

```csharp
AppProfile(string blueprintId, string blueprintDisplayName, string blueprintDescription, IReadOnlyList<PatternDescriptor> patterns, ScaffoldPlan scaffold, IReadOnlyList<TechnologyDescriptor> technologies, IReadOnlyList<TransportDescriptor> transports, DataSelection data, DatabaseTopologySelection databases, IdentitySelection identity, TenancySelection tenancy, AuditSelection audit, MessagingSelection messaging, ResilienceSelection resilience)
```

Creates an app profile with optional scaffold metadata.

Parameters:
- `blueprintId`: The selected blueprint identifier.
- `blueprintDisplayName`: The selected blueprint display name.
- `blueprintDescription`: The selected blueprint description.
- `patterns`: The patterns active for the app.
- `scaffold`: The scaffold plan associated with the app shape.
- `technologies`: The selected technology profiles.
- `transports`: The selected transports.
- `data`: The selected data inputs.
- `databases`: The selected database topology inputs.
- `identity`: The selected identity and authorization inputs.
- `tenancy`: The selected multi-tenancy inputs.
- `audit`: The selected audit inputs.
- `messaging`: The selected messaging inputs.
- `resilience`: The selected resilience-policy inputs.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-appprofile-audit"></a>

##### `Audit`

```csharp
AuditSelection Audit { get; }
```

Gets the selected audit inputs.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-blueprintdescription"></a>

##### `BlueprintDescription`

```csharp
string BlueprintDescription { get; }
```

Gets the selected blueprint description.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-blueprintdisplayname"></a>

##### `BlueprintDisplayName`

```csharp
string BlueprintDisplayName { get; }
```

Gets the selected blueprint display name.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-blueprintid"></a>

##### `BlueprintId`

```csharp
string BlueprintId { get; }
```

Gets the selected blueprint identifier.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-data"></a>

##### `Data`

```csharp
DataSelection Data { get; }
```

Gets the selected data inputs.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-databases"></a>

##### `Databases`

```csharp
DatabaseTopologySelection Databases { get; }
```

Gets the selected database topology inputs.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-identity"></a>

##### `Identity`

```csharp
IdentitySelection Identity { get; }
```

Gets the selected identity and authorization inputs.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-messaging"></a>

##### `Messaging`

```csharp
MessagingSelection Messaging { get; }
```

Gets the selected messaging inputs.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-patterns"></a>

##### `Patterns`

```csharp
IReadOnlyList<PatternDescriptor> Patterns { get; }
```

Gets the active patterns for the app.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-resilience"></a>

##### `Resilience`

```csharp
ResilienceSelection Resilience { get; }
```

Gets the selected resilience-policy inputs.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-scaffold"></a>

##### `Scaffold`

```csharp
ScaffoldPlan Scaffold { get; }
```

Gets the scaffold plan associated with the app shape, when one is defined.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-technologies"></a>

##### `Technologies`

```csharp
IReadOnlyList<TechnologyDescriptor> Technologies { get; }
```

Gets the selected technology profiles.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-tenancy"></a>

##### `Tenancy`

```csharp
TenancySelection Tenancy { get; }
```

Gets the selected multi-tenancy inputs.

<a id="member-p-cephalon-abstractions-appmodel-appprofile-transports"></a>

##### `Transports`

```csharp
IReadOnlyList<TransportDescriptor> Transports { get; }
```

Gets the selected transports.

<a id="type-cephalon-abstractions-appmodel-audithistoryexportselection"></a>

### `AuditHistoryExportSelection`

Describes the durable audit-history export inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class AuditHistoryExportSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-audithistoryexportselection-ctor-system-nullable-system-boolean-system-nullable-system-int32"></a>

##### `AuditHistoryExportSelection`

```csharp
AuditHistoryExportSelection(bool? enabled, int? maxEntries)
```

Initializes a new instance of the `AuditHistoryExportSelection` class.

Parameters:
- `enabled`: Whether audit-history export was explicitly enabled.
- `maxEntries`: The configured maximum number of entries that one export may stream.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-audithistoryexportselection-empty"></a>

##### `Empty`

```csharp
AuditHistoryExportSelection Empty { get; }
```

Gets an empty audit-history export-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryexportselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether audit-history export was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryexportselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any audit-history export inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryexportselection-maxentries"></a>

##### `MaxEntries`

```csharp
int? MaxEntries { get; }
```

Gets the configured maximum number of entries that one export may stream.

<a id="type-cephalon-abstractions-appmodel-audithistoryretentionselection"></a>

### `AuditHistoryRetentionSelection`

Describes the durable audit-history retention inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class AuditHistoryRetentionSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-audithistoryretentionselection-ctor-system-nullable-system-boolean-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-boolean-system-nullable-system-int32"></a>

##### `AuditHistoryRetentionSelection`

```csharp
AuditHistoryRetentionSelection(bool? enabled, int? maxAgeDays, int? deleteBatchSize, bool? applyOnStartup, int? runIntervalMinutes)
```

Initializes a new instance of the `AuditHistoryRetentionSelection` class.

Parameters:
- `enabled`: Whether retention was explicitly enabled.
- `maxAgeDays`: The maximum age, in days, to retain durable audit rows.
- `deleteBatchSize`: The maximum number of rows deleted per retention batch.
- `applyOnStartup`: Whether one retention pass should run during host startup.
- `runIntervalMinutes`: The optional recurring retention interval in minutes.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-audithistoryretentionselection-applyonstartup"></a>

##### `ApplyOnStartup`

```csharp
bool? ApplyOnStartup { get; }
```

Gets a value indicating whether one retention pass should run during host startup.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryretentionselection-deletebatchsize"></a>

##### `DeleteBatchSize`

```csharp
int? DeleteBatchSize { get; }
```

Gets the maximum number of rows deleted per retention batch.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryretentionselection-empty"></a>

##### `Empty`

```csharp
AuditHistoryRetentionSelection Empty { get; }
```

Gets an empty audit-history retention selection instance.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryretentionselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether retention was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryretentionselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any durable audit-history retention inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryretentionselection-maxagedays"></a>

##### `MaxAgeDays`

```csharp
int? MaxAgeDays { get; }
```

Gets the maximum age, in days, to retain durable audit rows.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryretentionselection-runintervalminutes"></a>

##### `RunIntervalMinutes`

```csharp
int? RunIntervalMinutes { get; }
```

Gets the optional recurring retention interval in minutes.

<a id="type-cephalon-abstractions-appmodel-audithistoryselection"></a>

### `AuditHistorySelection`

Describes the durable audit-history inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class AuditHistorySelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-audithistoryselection-ctor-system-nullable-system-boolean-system-string-system-string-cephalon-abstractions-appmodel-audithistoryexportselection-cephalon-abstractions-appmodel-audithistoryretentionselection"></a>

##### `AuditHistorySelection`

```csharp
AuditHistorySelection(bool? enabled, string provider, string databaseRole, AuditHistoryExportSelection export, AuditHistoryRetentionSelection retention)
```

Initializes a new instance of the `AuditHistorySelection` class.

Parameters:
- `enabled`: Whether durable audit history was explicitly enabled.
- `provider`: The selected durable history provider identifier.
- `databaseRole`: The selected database role used by the durable history path.
- `export`: The resolved export inputs for durable audit history.
- `retention`: The resolved retention inputs for durable audit history.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-audithistoryselection-databaserole"></a>

##### `DatabaseRole`

```csharp
string DatabaseRole { get; }
```

Gets the selected database role used by the durable history path.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryselection-empty"></a>

##### `Empty`

```csharp
AuditHistorySelection Empty { get; }
```

Gets an empty audit-history selection instance.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether durable audit history was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryselection-export"></a>

##### `Export`

```csharp
AuditHistoryExportSelection Export { get; }
```

Gets the resolved export inputs for durable audit history.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any durable audit-history inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryselection-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the selected durable history provider identifier.

<a id="member-p-cephalon-abstractions-appmodel-audithistoryselection-retention"></a>

##### `Retention`

```csharp
AuditHistoryRetentionSelection Retention { get; }
```

Gets the resolved retention inputs for durable audit history.

<a id="type-cephalon-abstractions-appmodel-auditselection"></a>

### `AuditSelection`

Describes the active audit and history inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class AuditSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-auditselection-ctor-system-nullable-system-boolean-cephalon-abstractions-appmodel-audithistoryselection"></a>

##### `AuditSelection`

```csharp
AuditSelection(bool? enabled, AuditHistorySelection history)
```

Initializes a new instance of the `AuditSelection` class.

Parameters:
- `enabled`: Whether audit support was explicitly enabled.
- `history`: The durable audit-history inputs resolved for the app.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-auditselection-empty"></a>

##### `Empty`

```csharp
AuditSelection Empty { get; }
```

Gets an empty audit-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-auditselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether audit support was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-auditselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any audit-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-auditselection-history"></a>

##### `History`

```csharp
AuditHistorySelection History { get; }
```

Gets the durable audit-history inputs resolved for the app.

<a id="type-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection"></a>

### `BehaviorExecutionResilienceOverrideSelection`

Describes one named behavior-execution resilience override requested for a subset of behaviors or transports.

#### Declaration
```csharp
public sealed class BehaviorExecutionResilienceOverrideSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-cephalon-abstractions-appmodel-retryselection-cephalon-abstractions-appmodel-timeoutselection-cephalon-abstractions-appmodel-circuitbreakerselection-cephalon-abstractions-appmodel-bulkheadselection-cephalon-abstractions-appmodel-ratelimitingselection"></a>

##### `BehaviorExecutionResilienceOverrideSelection`

```csharp
BehaviorExecutionResilienceOverrideSelection(string id, IReadOnlyList<string> behaviorIds, IReadOnlyList<string> transportIds, RetrySelection retry, TimeoutSelection timeout, CircuitBreakerSelection circuitBreaker, BulkheadSelection bulkhead, RateLimitingSelection rateLimiting)
```

Initializes a new instance of the `BehaviorExecutionResilienceOverrideSelection` class.

Parameters:
- `id`: The stable override identifier.
- `behaviorIds`: The targeted behavior identifiers.
- `transportIds`: The targeted transport identifiers.
- `retry`: The retry override requested for the targeted surface.
- `timeout`: The timeout override requested for the targeted surface.
- `circuitBreaker`: The circuit-breaker override requested for the targeted surface.
- `bulkhead`: The bulkhead override requested for the targeted surface.
- `rateLimiting`: The rate-limiting override requested for the targeted surface.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-behaviorids"></a>

##### `BehaviorIds`

```csharp
IReadOnlyList<string> BehaviorIds { get; }
```

Gets the behavior identifiers targeted by this override.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-bulkhead"></a>

##### `Bulkhead`

```csharp
BulkheadSelection Bulkhead { get; }
```

Gets the bulkhead override requested for the targeted surface.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-circuitbreaker"></a>

##### `CircuitBreaker`

```csharp
CircuitBreakerSelection CircuitBreaker { get; }
```

Gets the circuit-breaker override requested for the targeted surface.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-hasstrategyvalues"></a>

##### `HasStrategyValues`

```csharp
bool HasStrategyValues { get; }
```

Gets a value indicating whether any strategy-level override values were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any override values were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable override identifier.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-ratelimiting"></a>

##### `RateLimiting`

```csharp
RateLimitingSelection RateLimiting { get; }
```

Gets the rate-limiting override requested for the targeted surface.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-retry"></a>

##### `Retry`

```csharp
RetrySelection Retry { get; }
```

Gets the retry override requested for the targeted surface.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-timeout"></a>

##### `Timeout`

```csharp
TimeoutSelection Timeout { get; }
```

Gets the timeout override requested for the targeted surface.

<a id="member-p-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; }
```

Gets the transport identifiers targeted by this override.

<a id="type-cephalon-abstractions-appmodel-bulkheadselection"></a>

### `BulkheadSelection`

Describes the bulkhead-isolation inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class BulkheadSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-bulkheadselection-ctor-system-nullable-system-boolean-system-nullable-system-int32-system-nullable-system-int32"></a>

##### `BulkheadSelection`

```csharp
BulkheadSelection(bool? enabled, int? maxConcurrentExecutions, int? maxQueuedActions)
```

Initializes a new instance of the `BulkheadSelection` class.

Parameters:
- `enabled`: Whether bulkhead isolation was explicitly enabled.
- `maxConcurrentExecutions`: The maximum concurrent executions allowed inside the bulkhead.
- `maxQueuedActions`: The maximum queued actions allowed before rejection.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-bulkheadselection-empty"></a>

##### `Empty`

```csharp
BulkheadSelection Empty { get; }
```

Gets an empty bulkhead-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-bulkheadselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether bulkhead isolation was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-bulkheadselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any bulkhead-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-bulkheadselection-maxconcurrentexecutions"></a>

##### `MaxConcurrentExecutions`

```csharp
int? MaxConcurrentExecutions { get; }
```

Gets the maximum concurrent executions allowed inside the bulkhead.

<a id="member-p-cephalon-abstractions-appmodel-bulkheadselection-maxqueuedactions"></a>

##### `MaxQueuedActions`

```csharp
int? MaxQueuedActions { get; }
```

Gets the maximum queued actions allowed before rejection.

<a id="type-cephalon-abstractions-appmodel-circuitbreakerselection"></a>

### `CircuitBreakerSelection`

Describes the circuit-breaker inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class CircuitBreakerSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-circuitbreakerselection-ctor-system-nullable-system-boolean-system-nullable-system-decimal-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32"></a>

##### `CircuitBreakerSelection`

```csharp
CircuitBreakerSelection(bool? enabled, decimal? failureRatio, int? minimumThroughput, int? samplingDurationSeconds, int? breakDurationSeconds)
```

Initializes a new instance of the `CircuitBreakerSelection` class.

Parameters:
- `enabled`: Whether circuit-breaker support was explicitly enabled.
- `failureRatio`: The failure ratio threshold requested for opening the breaker.
- `minimumThroughput`: The minimum throughput required before the breaker evaluates failures.
- `samplingDurationSeconds`: The sampling duration in seconds used by the breaker.
- `breakDurationSeconds`: The break duration in seconds requested for the open state.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-circuitbreakerselection-breakdurationseconds"></a>

##### `BreakDurationSeconds`

```csharp
int? BreakDurationSeconds { get; }
```

Gets the break duration in seconds requested for the open state.

<a id="member-p-cephalon-abstractions-appmodel-circuitbreakerselection-empty"></a>

##### `Empty`

```csharp
CircuitBreakerSelection Empty { get; }
```

Gets an empty circuit-breaker-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-circuitbreakerselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether circuit-breaker support was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-circuitbreakerselection-failureratio"></a>

##### `FailureRatio`

```csharp
decimal? FailureRatio { get; }
```

Gets the failure ratio threshold requested for opening the breaker.

<a id="member-p-cephalon-abstractions-appmodel-circuitbreakerselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any circuit-breaker-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-circuitbreakerselection-minimumthroughput"></a>

##### `MinimumThroughput`

```csharp
int? MinimumThroughput { get; }
```

Gets the minimum throughput required before the breaker evaluates failures.

<a id="member-p-cephalon-abstractions-appmodel-circuitbreakerselection-samplingdurationseconds"></a>

##### `SamplingDurationSeconds`

```csharp
int? SamplingDurationSeconds { get; }
```

Gets the sampling duration in seconds used by the breaker.

<a id="type-cephalon-abstractions-appmodel-databasemigrationsselection"></a>

### `DatabaseMigrationsSelection`

Describes the active database-migration inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class DatabaseMigrationsSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-databasemigrationsselection-ctor-system-nullable-system-boolean-system-nullable-system-boolean-system-collections-generic-ireadonlylist-system-string"></a>

##### `DatabaseMigrationsSelection`

```csharp
DatabaseMigrationsSelection(bool? applyOnStartup, bool? exitAfterApply, IReadOnlyList<string> targets)
```

Initializes a new instance of the `DatabaseMigrationsSelection` class.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-databasemigrationsselection-applyonstartup"></a>

##### `ApplyOnStartup`

```csharp
bool? ApplyOnStartup { get; }
```

Gets a value indicating whether migrations should be applied during host startup.

<a id="member-p-cephalon-abstractions-appmodel-databasemigrationsselection-empty"></a>

##### `Empty`

```csharp
DatabaseMigrationsSelection Empty { get; }
```

Gets an empty database-migrations selection instance.

<a id="member-p-cephalon-abstractions-appmodel-databasemigrationsselection-exitafterapply"></a>

##### `ExitAfterApply`

```csharp
bool? ExitAfterApply { get; }
```

Gets a value indicating whether the host should exit after applying migrations.

<a id="member-p-cephalon-abstractions-appmodel-databasemigrationsselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any migration-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-databasemigrationsselection-targets"></a>

##### `Targets`

```csharp
IReadOnlyList<string> Targets { get; }
```

Gets the logical migration targets selected for the app.

<a id="type-cephalon-abstractions-appmodel-databaseruntimeselection"></a>

### `DatabaseRuntimeSelection`

Describes the active database runtime tuning inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class DatabaseRuntimeSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-databaseruntimeselection-ctor-system-nullable-system-boolean-system-nullable-system-boolean-system-nullable-system-boolean-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32"></a>

##### `DatabaseRuntimeSelection`

```csharp
DatabaseRuntimeSelection(bool? enableDetailedErrors, bool? enableSensitiveDataLogging, bool? enableRetryOnFailure, int? maxRetryCount, int? maxRetryDelaySeconds, int? commandTimeoutSeconds, int? maxBatchSize, int? roleProbeFreshnessSeconds)
```

Initializes a new instance of the `DatabaseRuntimeSelection` class.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-commandtimeoutseconds"></a>

##### `CommandTimeoutSeconds`

```csharp
int? CommandTimeoutSeconds { get; }
```

Gets the command timeout in seconds when one was configured.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-empty"></a>

##### `Empty`

```csharp
DatabaseRuntimeSelection Empty { get; }
```

Gets an empty database-runtime selection instance.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-enabledetailederrors"></a>

##### `EnableDetailedErrors`

```csharp
bool? EnableDetailedErrors { get; }
```

Gets a value indicating whether detailed provider errors were explicitly selected.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-enableretryonfailure"></a>

##### `EnableRetryOnFailure`

```csharp
bool? EnableRetryOnFailure { get; }
```

Gets a value indicating whether transient-failure retries were explicitly selected.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-enablesensitivedatalogging"></a>

##### `EnableSensitiveDataLogging`

```csharp
bool? EnableSensitiveDataLogging { get; }
```

Gets a value indicating whether sensitive-data logging was explicitly selected.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any database-runtime selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-maxbatchsize"></a>

##### `MaxBatchSize`

```csharp
int? MaxBatchSize { get; }
```

Gets the maximum provider batch size when one was configured.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-maxretrycount"></a>

##### `MaxRetryCount`

```csharp
int? MaxRetryCount { get; }
```

Gets the maximum retry count when transient-failure retries were configured.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-maxretrydelayseconds"></a>

##### `MaxRetryDelaySeconds`

```csharp
int? MaxRetryDelaySeconds { get; }
```

Gets the maximum retry delay in seconds when transient-failure retries were configured.

<a id="member-p-cephalon-abstractions-appmodel-databaseruntimeselection-roleprobefreshnessseconds"></a>

##### `RoleProbeFreshnessSeconds`

```csharp
int? RoleProbeFreshnessSeconds { get; }
```

Gets the freshness window in seconds for cached database-role probes when one was selected. A value of `0` disables probe-result caching.

<a id="type-cephalon-abstractions-appmodel-databasetargetselection"></a>

### `DatabaseTargetSelection`

Describes one database role target resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class DatabaseTargetSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-databasetargetselection-ctor-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-appmodel-databaseruntimeselection"></a>

##### `DatabaseTargetSelection`

```csharp
DatabaseTargetSelection(string provider, string connectionStringName, string connectionString, string useRole, string schema, DatabaseRuntimeSelection runtime)
```

Initializes a new instance of the `DatabaseTargetSelection` class.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-databasetargetselection-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; }
```

Gets the inline connection string selected for this database role.

<a id="member-p-cephalon-abstractions-appmodel-databasetargetselection-connectionstringname"></a>

##### `ConnectionStringName`

```csharp
string ConnectionStringName { get; }
```

Gets the root connection-string name selected for this database role.

<a id="member-p-cephalon-abstractions-appmodel-databasetargetselection-empty"></a>

##### `Empty`

```csharp
DatabaseTargetSelection Empty { get; }
```

Gets an empty database-target selection instance.

<a id="member-p-cephalon-abstractions-appmodel-databasetargetselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any target-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-databasetargetselection-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the selected logical provider identifier.

<a id="member-p-cephalon-abstractions-appmodel-databasetargetselection-runtime"></a>

##### `Runtime`

```csharp
DatabaseRuntimeSelection Runtime { get; }
```

Gets the role-specific runtime overrides for this database target.

<a id="member-p-cephalon-abstractions-appmodel-databasetargetselection-schema"></a>

##### `Schema`

```csharp
string Schema { get; }
```

Gets the schema override selected for this database role.

<a id="member-p-cephalon-abstractions-appmodel-databasetargetselection-userole"></a>

##### `UseRole`

```csharp
string UseRole { get; }
```

Gets the referenced concrete database role that supplies the physical connection target.

<a id="type-cephalon-abstractions-appmodel-databasetopologyselection"></a>

### `DatabaseTopologySelection`

Describes the active database topology inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class DatabaseTopologySelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-databasetopologyselection-ctor-cephalon-abstractions-appmodel-databaseruntimeselection-cephalon-abstractions-appmodel-databasetargetselection-cephalon-abstractions-appmodel-databasetargetselection-cephalon-abstractions-appmodel-databasetargetselection-cephalon-abstractions-appmodel-databasetargetselection-cephalon-abstractions-appmodel-databasemigrationsselection"></a>

##### `DatabaseTopologySelection`

```csharp
DatabaseTopologySelection(DatabaseRuntimeSelection runtime, DatabaseTargetSelection write, DatabaseTargetSelection read, DatabaseTargetSelection outbox, DatabaseTargetSelection history, DatabaseMigrationsSelection migrations)
```

Initializes a new instance of the `DatabaseTopologySelection` class.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-databasetopologyselection-empty"></a>

##### `Empty`

```csharp
DatabaseTopologySelection Empty { get; }
```

Gets an empty database-topology selection instance.

<a id="member-p-cephalon-abstractions-appmodel-databasetopologyselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any database-topology inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-databasetopologyselection-history"></a>

##### `History`

```csharp
DatabaseTargetSelection History { get; }
```

Gets the audit-history database target selection.

<a id="member-p-cephalon-abstractions-appmodel-databasetopologyselection-migrations"></a>

##### `Migrations`

```csharp
DatabaseMigrationsSelection Migrations { get; }
```

Gets the database-migration selection.

<a id="member-p-cephalon-abstractions-appmodel-databasetopologyselection-outbox"></a>

##### `Outbox`

```csharp
DatabaseTargetSelection Outbox { get; }
```

Gets the outbox database target selection.

<a id="member-p-cephalon-abstractions-appmodel-databasetopologyselection-read"></a>

##### `Read`

```csharp
DatabaseTargetSelection Read { get; }
```

Gets the read-side database target selection.

<a id="member-p-cephalon-abstractions-appmodel-databasetopologyselection-runtime"></a>

##### `Runtime`

```csharp
DatabaseRuntimeSelection Runtime { get; }
```

Gets the shared runtime tuning selected for database roles.

<a id="member-p-cephalon-abstractions-appmodel-databasetopologyselection-write"></a>

##### `Write`

```csharp
DatabaseTargetSelection Write { get; }
```

Gets the write-side database target selection.

<a id="type-cephalon-abstractions-appmodel-dataselection"></a>

### `DataSelection`

Describes the active data-selection inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class DataSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-dataselection-ctor-system-string-system-nullable-system-boolean-system-nullable-system-boolean-system-string"></a>

##### `DataSelection`

```csharp
DataSelection(string provider, bool? readWriteSplit, bool? outboxEnabled, string idGenerator)
```

Initializes a new instance of the `DataSelection` class.

Parameters:
- `provider`: The selected primary data-provider family or implementation identifier.
- `readWriteSplit`: Whether distinct read and write paths were explicitly selected.
- `outboxEnabled`: Whether the outbox pattern was explicitly enabled.
- `idGenerator`: The selected identifier-generation strategy.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-dataselection-empty"></a>

##### `Empty`

```csharp
DataSelection Empty { get; }
```

Gets an empty data-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-dataselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any data-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-dataselection-idgenerator"></a>

##### `IdGenerator`

```csharp
string IdGenerator { get; }
```

Gets the selected identifier-generation strategy.

<a id="member-p-cephalon-abstractions-appmodel-dataselection-outboxenabled"></a>

##### `OutboxEnabled`

```csharp
bool? OutboxEnabled { get; }
```

Gets a value indicating whether the outbox pattern was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-dataselection-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the selected primary data-provider family or implementation identifier.

<a id="member-p-cephalon-abstractions-appmodel-dataselection-readwritesplit"></a>

##### `ReadWriteSplit`

```csharp
bool? ReadWriteSplit { get; }
```

Gets a value indicating whether distinct read and write paths were explicitly selected.

<a id="type-cephalon-abstractions-appmodel-identityselection"></a>

### `IdentitySelection`

Describes the active identity and authorization inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class IdentitySelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-identityselection-ctor-system-nullable-system-boolean-system-collections-generic-ireadonlylist-system-string"></a>

##### `IdentitySelection`

```csharp
IdentitySelection(bool? enabled, IReadOnlyList<string> authorizationModes)
```

Initializes a new instance of the `IdentitySelection` class.

Parameters:
- `enabled`: Whether identity and authorization support was explicitly enabled.
- `authorizationModes`: The selected authorization modes.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-identityselection-authorizationmodes"></a>

##### `AuthorizationModes`

```csharp
IReadOnlyList<string> AuthorizationModes { get; }
```

Gets the selected authorization modes.

<a id="member-p-cephalon-abstractions-appmodel-identityselection-empty"></a>

##### `Empty`

```csharp
IdentitySelection Empty { get; }
```

Gets an empty identity-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-identityselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether identity and authorization support was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-identityselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any identity-selection inputs were explicitly supplied.

<a id="type-cephalon-abstractions-appmodel-messagingselection"></a>

### `MessagingSelection`

Describes the active messaging inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class MessagingSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-messagingselection-ctor-system-string"></a>

##### `MessagingSelection`

```csharp
MessagingSelection(string provider)
```

Initializes a new instance of the `MessagingSelection` class.

Parameters:
- `provider`: The selected messaging provider or runtime adapter.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-messagingselection-empty"></a>

##### `Empty`

```csharp
MessagingSelection Empty { get; }
```

Gets an empty messaging-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-messagingselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any messaging-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-messagingselection-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the selected messaging provider or runtime adapter.

<a id="type-cephalon-abstractions-appmodel-ratelimitingoverrideselection"></a>

### `RateLimitingOverrideSelection`

Describes one named rate-limiting override requested for a subset of transports or behaviors.

#### Declaration
```csharp
public sealed class RateLimitingOverrideSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-ratelimitingoverrideselection-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-nullable-system-boolean-system-string-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32"></a>

##### `RateLimitingOverrideSelection`

```csharp
RateLimitingOverrideSelection(string id, IReadOnlyList<string> behaviorIds, IReadOnlyList<string> transportIds, bool? enabled, string algorithm, int? permitLimit, int? queueLimit, int? windowSeconds, int? segmentsPerWindow)
```

Initializes a new instance of the `RateLimitingOverrideSelection` class.

Parameters:
- `id`: The stable override identifier.
- `behaviorIds`: The targeted behavior identifiers.
- `transportIds`: The targeted transport identifiers.
- `enabled`: Whether the override explicitly enables or disables rate limiting for the targeted surface.
- `algorithm`: The requested rate-limiting algorithm, such as `FixedWindow` or `TokenBucket`.
- `permitLimit`: The maximum permits available per limiter window or bucket.
- `queueLimit`: The maximum queued requests allowed before rejection.
- `windowSeconds`: The limiter window duration in seconds when the selected algorithm uses windows.
- `segmentsPerWindow`: The number of segments per window when sliding windows are used.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-algorithm"></a>

##### `Algorithm`

```csharp
string Algorithm { get; }
```

Gets the requested rate-limiting algorithm, such as `FixedWindow` or `TokenBucket`.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-behaviorids"></a>

##### `BehaviorIds`

```csharp
IReadOnlyList<string> BehaviorIds { get; }
```

Gets the behavior identifiers targeted by this override.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether rate limiting was explicitly enabled or disabled for the targeted surface.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any override values were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable override identifier.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-permitlimit"></a>

##### `PermitLimit`

```csharp
int? PermitLimit { get; }
```

Gets the maximum permits available per limiter window or bucket.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-queuelimit"></a>

##### `QueueLimit`

```csharp
int? QueueLimit { get; }
```

Gets the maximum queued requests allowed before rejection.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-segmentsperwindow"></a>

##### `SegmentsPerWindow`

```csharp
int? SegmentsPerWindow { get; }
```

Gets the number of segments per window when sliding windows are used.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; }
```

Gets the transport identifiers targeted by this override.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingoverrideselection-windowseconds"></a>

##### `WindowSeconds`

```csharp
int? WindowSeconds { get; }
```

Gets the limiter window duration in seconds when the selected algorithm uses windows.

<a id="type-cephalon-abstractions-appmodel-ratelimitingselection"></a>

### `RateLimitingSelection`

Describes the rate-limiting inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class RateLimitingSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-ratelimitingselection-ctor-system-nullable-system-boolean-system-string-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32-system-collections-generic-ireadonlylist-cephalon-abstractions-appmodel-ratelimitingoverrideselection"></a>

##### `RateLimitingSelection`

```csharp
RateLimitingSelection(bool? enabled, string algorithm, int? permitLimit, int? queueLimit, int? windowSeconds, int? segmentsPerWindow, IReadOnlyList<RateLimitingOverrideSelection> overrides)
```

Initializes a new instance of the `RateLimitingSelection` class.

Parameters:
- `enabled`: Whether rate limiting was explicitly enabled.
- `algorithm`: The requested rate-limiting algorithm, such as `FixedWindow` or `TokenBucket`.
- `permitLimit`: The maximum permits available per limiter window or bucket.
- `queueLimit`: The maximum queued requests allowed before rejection.
- `windowSeconds`: The limiter window duration in seconds when the selected algorithm uses windows.
- `segmentsPerWindow`: The number of segments per window when sliding windows are used.
- `overrides`: The named override policies targeted at specific transports or behaviors.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-algorithm"></a>

##### `Algorithm`

```csharp
string Algorithm { get; }
```

Gets the requested rate-limiting algorithm, such as `FixedWindow` or `TokenBucket`.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-empty"></a>

##### `Empty`

```csharp
RateLimitingSelection Empty { get; }
```

Gets an empty rate-limiting-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether rate limiting was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any rate-limiting-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-overrides"></a>

##### `Overrides`

```csharp
IReadOnlyList<RateLimitingOverrideSelection> Overrides { get; }
```

Gets the named override policies targeted at specific transports or behaviors.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-permitlimit"></a>

##### `PermitLimit`

```csharp
int? PermitLimit { get; }
```

Gets the maximum permits available per limiter window or bucket.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-queuelimit"></a>

##### `QueueLimit`

```csharp
int? QueueLimit { get; }
```

Gets the maximum queued requests allowed before rejection.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-segmentsperwindow"></a>

##### `SegmentsPerWindow`

```csharp
int? SegmentsPerWindow { get; }
```

Gets the number of segments per window when sliding windows are used.

<a id="member-p-cephalon-abstractions-appmodel-ratelimitingselection-windowseconds"></a>

##### `WindowSeconds`

```csharp
int? WindowSeconds { get; }
```

Gets the limiter window duration in seconds when the selected algorithm uses windows.

<a id="type-cephalon-abstractions-appmodel-resilienceselection"></a>

### `ResilienceSelection`

Describes the resilience-policy inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class ResilienceSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-resilienceselection-ctor-cephalon-abstractions-appmodel-retryselection-cephalon-abstractions-appmodel-timeoutselection-cephalon-abstractions-appmodel-circuitbreakerselection-cephalon-abstractions-appmodel-bulkheadselection-cephalon-abstractions-appmodel-ratelimitingselection-system-collections-generic-ireadonlylist-cephalon-abstractions-appmodel-behaviorexecutionresilienceoverrideselection"></a>

##### `ResilienceSelection`

```csharp
ResilienceSelection(RetrySelection retry, TimeoutSelection timeout, CircuitBreakerSelection circuitBreaker, BulkheadSelection bulkhead, RateLimitingSelection rateLimiting, IReadOnlyList<BehaviorExecutionResilienceOverrideSelection> behaviorExecutionOverrides)
```

Initializes a new instance of the `ResilienceSelection` class.

Parameters:
- `retry`: The retry policy resolved for the app.
- `timeout`: The timeout policy resolved for the app.
- `circuitBreaker`: The circuit-breaker policy resolved for the app.
- `bulkhead`: The bulkhead policy resolved for the app.
- `rateLimiting`: The rate-limiting policy resolved for the app.
- `behaviorExecutionOverrides`: The named behavior-execution override policies targeted at specific behaviors or transports.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-resilienceselection-behaviorexecutionoverrides"></a>

##### `BehaviorExecutionOverrides`

```csharp
IReadOnlyList<BehaviorExecutionResilienceOverrideSelection> BehaviorExecutionOverrides { get; }
```

Gets the named behavior-execution override policies targeted at specific behaviors or transports.

<a id="member-p-cephalon-abstractions-appmodel-resilienceselection-bulkhead"></a>

##### `Bulkhead`

```csharp
BulkheadSelection Bulkhead { get; }
```

Gets the bulkhead policy resolved for the app.

<a id="member-p-cephalon-abstractions-appmodel-resilienceselection-circuitbreaker"></a>

##### `CircuitBreaker`

```csharp
CircuitBreakerSelection CircuitBreaker { get; }
```

Gets the circuit-breaker policy resolved for the app.

<a id="member-p-cephalon-abstractions-appmodel-resilienceselection-empty"></a>

##### `Empty`

```csharp
ResilienceSelection Empty { get; }
```

Gets an empty resilience-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-resilienceselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any resilience-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-resilienceselection-ratelimiting"></a>

##### `RateLimiting`

```csharp
RateLimitingSelection RateLimiting { get; }
```

Gets the rate-limiting policy resolved for the app.

<a id="member-p-cephalon-abstractions-appmodel-resilienceselection-retry"></a>

##### `Retry`

```csharp
RetrySelection Retry { get; }
```

Gets the retry policy resolved for the app.

<a id="member-p-cephalon-abstractions-appmodel-resilienceselection-timeout"></a>

##### `Timeout`

```csharp
TimeoutSelection Timeout { get; }
```

Gets the timeout policy resolved for the app.

<a id="type-cephalon-abstractions-appmodel-retryselection"></a>

### `RetrySelection`

Describes the retry-policy inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class RetrySelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-retryselection-ctor-system-nullable-system-boolean-system-nullable-system-int32-system-string-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-boolean"></a>

##### `RetrySelection`

```csharp
RetrySelection(bool? enabled, int? maxAttempts, string backoff, int? baseDelayMilliseconds, int? maxDelayMilliseconds, bool? useJitter)
```

Initializes a new instance of the `RetrySelection` class.

Parameters:
- `enabled`: Whether retry support was explicitly enabled.
- `maxAttempts`: The maximum retry attempts requested for the policy.
- `backoff`: The requested backoff mode, such as `Exponential` or `Linear`.
- `baseDelayMilliseconds`: The base delay in milliseconds used by the retry policy.
- `maxDelayMilliseconds`: The maximum delay in milliseconds the retry policy may apply.
- `useJitter`: Whether jitter was explicitly requested for retry delays.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-retryselection-backoff"></a>

##### `Backoff`

```csharp
string Backoff { get; }
```

Gets the requested backoff mode, such as `Exponential` or `Linear`.

<a id="member-p-cephalon-abstractions-appmodel-retryselection-basedelaymilliseconds"></a>

##### `BaseDelayMilliseconds`

```csharp
int? BaseDelayMilliseconds { get; }
```

Gets the base delay in milliseconds used by the retry policy.

<a id="member-p-cephalon-abstractions-appmodel-retryselection-empty"></a>

##### `Empty`

```csharp
RetrySelection Empty { get; }
```

Gets an empty retry-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-retryselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether retry support was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-retryselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any retry-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-retryselection-maxattempts"></a>

##### `MaxAttempts`

```csharp
int? MaxAttempts { get; }
```

Gets the maximum retry attempts requested for the policy.

<a id="member-p-cephalon-abstractions-appmodel-retryselection-maxdelaymilliseconds"></a>

##### `MaxDelayMilliseconds`

```csharp
int? MaxDelayMilliseconds { get; }
```

Gets the maximum delay in milliseconds the retry policy may apply.

<a id="member-p-cephalon-abstractions-appmodel-retryselection-usejitter"></a>

##### `UseJitter`

```csharp
bool? UseJitter { get; }
```

Gets a value indicating whether jitter was explicitly requested for retry delays.

<a id="type-cephalon-abstractions-appmodel-suiteblueprint"></a>

### `SuiteBlueprint`

Describes a suite-level Cephalon blueprint composed from existing app-level contracts.

#### Declaration
```csharp
public sealed class SuiteBlueprint
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-suiteblueprint-ctor-system-string-system-string-system-string-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `SuiteBlueprint`

```csharp
SuiteBlueprint(string id, string displayName, string description, SuiteScaffoldPlan scaffold, IReadOnlyDictionary<string, string> metadata)
```

Creates a suite blueprint.

Parameters:
- `id`: The stable suite-blueprint identifier.
- `displayName`: The human-readable suite-blueprint name.
- `description`: The suite-blueprint description.
- `scaffold`: The suite-scaffold plan associated with the suite blueprint.
- `metadata`: Optional suite-blueprint metadata.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-suiteblueprint-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the suite-blueprint description.

<a id="member-p-cephalon-abstractions-appmodel-suiteblueprint-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable suite-blueprint name.

<a id="member-p-cephalon-abstractions-appmodel-suiteblueprint-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable suite-blueprint identifier.

<a id="member-p-cephalon-abstractions-appmodel-suiteblueprint-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional suite-blueprint metadata.

<a id="member-p-cephalon-abstractions-appmodel-suiteblueprint-scaffold"></a>

##### `Scaffold`

```csharp
SuiteScaffoldPlan Scaffold { get; }
```

Gets the suite-scaffold plan associated with the suite blueprint.

<a id="type-cephalon-abstractions-appmodel-tenancyselection"></a>

### `TenancySelection`

Describes the active multi-tenancy inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class TenancySelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-tenancyselection-ctor-system-nullable-system-boolean-system-string"></a>

##### `TenancySelection`

```csharp
TenancySelection(bool? enabled, string mode)
```

Initializes a new instance of the `TenancySelection` class.

Parameters:
- `enabled`: Whether multi-tenancy was explicitly enabled.
- `mode`: The selected tenancy mode.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-tenancyselection-empty"></a>

##### `Empty`

```csharp
TenancySelection Empty { get; }
```

Gets an empty tenancy-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-tenancyselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether multi-tenancy was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-tenancyselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any tenancy-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-tenancyselection-mode"></a>

##### `Mode`

```csharp
string Mode { get; }
```

Gets the selected tenancy mode.

<a id="type-cephalon-abstractions-appmodel-timeoutselection"></a>

### `TimeoutSelection`

Describes the timeout-policy inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class TimeoutSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-timeoutselection-ctor-system-nullable-system-boolean-system-nullable-system-int32-system-nullable-system-int32"></a>

##### `TimeoutSelection`

```csharp
TimeoutSelection(bool? enabled, int? totalTimeoutSeconds, int? attemptTimeoutSeconds)
```

Initializes a new instance of the `TimeoutSelection` class.

Parameters:
- `enabled`: Whether timeout support was explicitly enabled.
- `totalTimeoutSeconds`: The overall timeout in seconds requested for an execution.
- `attemptTimeoutSeconds`: The per-attempt timeout in seconds requested for an execution.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-timeoutselection-attempttimeoutseconds"></a>

##### `AttemptTimeoutSeconds`

```csharp
int? AttemptTimeoutSeconds { get; }
```

Gets the per-attempt timeout in seconds requested for an execution.

<a id="member-p-cephalon-abstractions-appmodel-timeoutselection-empty"></a>

##### `Empty`

```csharp
TimeoutSelection Empty { get; }
```

Gets an empty timeout-selection instance.

<a id="member-p-cephalon-abstractions-appmodel-timeoutselection-enabled"></a>

##### `Enabled`

```csharp
bool? Enabled { get; }
```

Gets a value indicating whether timeout support was explicitly enabled.

<a id="member-p-cephalon-abstractions-appmodel-timeoutselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any timeout-selection inputs were explicitly supplied.

<a id="member-p-cephalon-abstractions-appmodel-timeoutselection-totaltimeoutseconds"></a>

##### `TotalTimeoutSeconds`

```csharp
int? TotalTimeoutSeconds { get; }
```

Gets the overall timeout in seconds requested for an execution.

<a id="namespace-cephalon-abstractions-appmodel-scaffolding"></a>

## Namespace Cephalon.Abstractions.AppModel.Scaffolding

<a id="type-cephalon-abstractions-appmodel-scaffolding-projectroles"></a>

### `ProjectRoles`

Defines the canonical project-role identifiers used by scaffold plans.

#### Declaration
```csharp
public static class ProjectRoles
```

#### Fields

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-contracts"></a>

##### `Contracts`

```csharp
const string Contracts
```

Identifies the contracts project.

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-foundation"></a>

##### `Foundation`

```csharp
const string Foundation
```

Identifies the shared foundation project.

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-host"></a>

##### `Host`

```csharp
const string Host
```

Identifies the host project.

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-module"></a>

##### `Module`

```csharp
const string Module
```

Identifies a module project.

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-tests"></a>

##### `Tests`

```csharp
const string Tests
```

Identifies a test project.

<a id="type-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder"></a>

### `ScaffoldFolder`

Describes a folder that should exist in a scaffolded app shape.

#### Declaration
```csharp
public sealed class ScaffoldFolder
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ScaffoldFolder`

```csharp
ScaffoldFolder(string pathTemplate, string purpose, string scope, string projectId, IReadOnlyDictionary<string, string> metadata)
```

Creates a scaffold-folder description.

Parameters:
- `pathTemplate`: The folder path template.
- `purpose`: The human-readable folder purpose.
- `scope`: The scaffold scope that owns the folder.
- `projectId`: The owning project identifier when the folder belongs to a project.
- `metadata`: Optional folder metadata.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional folder metadata.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-pathtemplate"></a>

##### `PathTemplate`

```csharp
string PathTemplate { get; }
```

Gets the folder path template.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-projectid"></a>

##### `ProjectId`

```csharp
string ProjectId { get; }
```

Gets the owning project identifier when the folder belongs to a project.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-purpose"></a>

##### `Purpose`

```csharp
string Purpose { get; }
```

Gets the human-readable purpose of the folder.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-scope"></a>

##### `Scope`

```csharp
string Scope { get; }
```

Gets the scaffold scope that owns the folder.

<a id="type-cephalon-abstractions-appmodel-scaffolding-scaffoldplan"></a>

### `ScaffoldPlan`

Describes the blueprint-driven scaffold plan for an app shape.

#### Declaration
```csharp
public sealed class ScaffoldPlan
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-system-collections-generic-ireadonlylist-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ScaffoldPlan`

```csharp
ScaffoldPlan(string id, string displayName, string description, IReadOnlyList<ScaffoldProject> projects, IReadOnlyList<ScaffoldFolder> folders, IReadOnlyList<string> conventions, IReadOnlyDictionary<string, string> metadata)
```

Creates a scaffold plan.

Parameters:
- `id`: The stable scaffold-plan identifier.
- `displayName`: The human-readable scaffold-plan name.
- `description`: The scaffold-plan description.
- `projects`: The projects emitted by the scaffold.
- `folders`: The folders emitted by the scaffold.
- `conventions`: The conventions implied by the scaffold.
- `metadata`: Optional scaffold metadata.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-conventions"></a>

##### `Conventions`

```csharp
IReadOnlyList<string> Conventions { get; }
```

Gets the conventions implied by the scaffold.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the scaffold-plan description.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable scaffold-plan name.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-folders"></a>

##### `Folders`

```csharp
IReadOnlyList<ScaffoldFolder> Folders { get; }
```

Gets the folders emitted by the scaffold.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable scaffold-plan identifier.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional scaffold metadata.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-projects"></a>

##### `Projects`

```csharp
IReadOnlyList<ScaffoldProject> Projects { get; }
```

Gets the projects emitted by the scaffold.

<a id="type-cephalon-abstractions-appmodel-scaffolding-scaffoldproject"></a>

### `ScaffoldProject`

Describes one project emitted by a scaffold plan.

#### Declaration
```csharp
public sealed class ScaffoldProject
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ScaffoldProject`

```csharp
ScaffoldProject(string id, string nameTemplate, string pathTemplate, string scope, string role, string template, IReadOnlyList<string> dependsOn, IReadOnlyList<string> packages, IReadOnlyDictionary<string, string> metadata)
```

Creates a scaffold-project description.

Parameters:
- `id`: The stable project identifier.
- `nameTemplate`: The project-name template.
- `pathTemplate`: The project-path template.
- `scope`: The scaffold scope that owns the project.
- `role`: The canonical project role.
- `template`: The template used to create the project.
- `dependsOn`: The project identifiers this project depends on.
- `packages`: The package hints associated with the project.
- `metadata`: Optional project metadata.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-dependson"></a>

##### `DependsOn`

```csharp
IReadOnlyList<string> DependsOn { get; }
```

Gets the project identifiers this project depends on.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable project identifier.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional project metadata.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-nametemplate"></a>

##### `NameTemplate`

```csharp
string NameTemplate { get; }
```

Gets the project-name template.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-packages"></a>

##### `Packages`

```csharp
IReadOnlyList<string> Packages { get; }
```

Gets the package hints associated with the project.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-pathtemplate"></a>

##### `PathTemplate`

```csharp
string PathTemplate { get; }
```

Gets the project-path template.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-role"></a>

##### `Role`

```csharp
string Role { get; }
```

Gets the canonical project role.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-scope"></a>

##### `Scope`

```csharp
string Scope { get; }
```

Gets the scaffold scope that owns the project.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-template"></a>

##### `Template`

```csharp
string Template { get; }
```

Gets the template used to create the project.

<a id="type-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes"></a>

### `ScaffoldScopes`

Defines the canonical scaffold-scope identifiers used by scaffold plans.

#### Declaration
```csharp
public static class ScaffoldScopes
```

#### Fields

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes-feature"></a>

##### `Feature`

```csharp
const string Feature
```

Identifies a feature-level scaffold scope.

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes-module"></a>

##### `Module`

```csharp
const string Module
```

Identifies a module-level scaffold scope.

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes-solution"></a>

##### `Solution`

```csharp
const string Solution
```

Identifies a solution-level scaffold scope.

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes-suite"></a>

##### `Suite`

```csharp
const string Suite
```

Identifies a suite-level scaffold scope.

<a id="type-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan"></a>

### `SuiteScaffoldPlan`

Describes a suite-level scaffold plan for coordinated multi-service Cephalon solutions.

#### Declaration
```csharp
public sealed class SuiteScaffoldPlan
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-system-collections-generic-ireadonlylist-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-system-collections-generic-ireadonlylist-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `SuiteScaffoldPlan`

```csharp
SuiteScaffoldPlan(string id, string displayName, string description, IReadOnlyList<SuiteScaffoldService> services, IReadOnlyList<ScaffoldProject> sharedProjects, IReadOnlyList<ScaffoldFolder> sharedFolders, IReadOnlyList<string> conventions, IReadOnlyDictionary<string, string> metadata)
```

Creates a suite-level scaffold plan.

Parameters:
- `id`: The stable suite-scaffold identifier.
- `displayName`: The human-readable suite-scaffold name.
- `description`: The suite-scaffold description.
- `services`: The service slots emitted by the suite scaffold.
- `sharedProjects`: The shared projects emitted outside individual services.
- `sharedFolders`: The shared folders emitted outside individual services.
- `conventions`: The conventions implied by the suite scaffold.
- `metadata`: Optional suite-scaffold metadata.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-conventions"></a>

##### `Conventions`

```csharp
IReadOnlyList<string> Conventions { get; }
```

Gets the conventions implied by the suite scaffold.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the suite-scaffold description.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable suite-scaffold name.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable suite-scaffold identifier.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional suite-scaffold metadata.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-services"></a>

##### `Services`

```csharp
IReadOnlyList<SuiteScaffoldService> Services { get; }
```

Gets the service slots emitted by the suite scaffold.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-sharedfolders"></a>

##### `SharedFolders`

```csharp
IReadOnlyList<ScaffoldFolder> SharedFolders { get; }
```

Gets the shared folders emitted outside individual services.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldplan-sharedprojects"></a>

##### `SharedProjects`

```csharp
IReadOnlyList<ScaffoldProject> SharedProjects { get; }
```

Gets the shared projects emitted outside individual services.

<a id="type-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice"></a>

### `SuiteScaffoldService`

Describes one service slot inside a suite-level scaffold plan.

#### Declaration
```csharp
public sealed class SuiteScaffoldService
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `SuiteScaffoldService`

```csharp
SuiteScaffoldService(string id, string displayName, string description, string blueprintId, string nameTemplate, string pathTemplate, IReadOnlyList<string> dependsOn, IReadOnlyDictionary<string, string> metadata)
```

Creates a suite-scaffold service description.

Parameters:
- `id`: The stable service-slot identifier.
- `displayName`: The human-readable service-slot name.
- `description`: The service-slot description.
- `blueprintId`: The app blueprint identifier used for the service.
- `nameTemplate`: The generated app-name template for the service.
- `pathTemplate`: The generated root-path template for the service.
- `dependsOn`: The service or shared-project identifiers this service depends on.
- `metadata`: Optional service metadata.

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-blueprintid"></a>

##### `BlueprintId`

```csharp
string BlueprintId { get; }
```

Gets the app blueprint identifier used for the service.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-dependson"></a>

##### `DependsOn`

```csharp
IReadOnlyList<string> DependsOn { get; }
```

Gets the service or shared-project identifiers this service depends on.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the service-slot description.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable service-slot name.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable service-slot identifier.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional service metadata.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-nametemplate"></a>

##### `NameTemplate`

```csharp
string NameTemplate { get; }
```

Gets the generated app-name template for the service.

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-suitescaffoldservice-pathtemplate"></a>

##### `PathTemplate`

```csharp
string PathTemplate { get; }
```

Gets the generated root-path template for the service.

<a id="namespace-cephalon-abstractions-audit"></a>

## Namespace Cephalon.Abstractions.Audit

<a id="type-cephalon-abstractions-audit-auditactor"></a>

### `AuditActor`

Describes the actor responsible for one audited operation.

#### Declaration
```csharp
public sealed class AuditActor
```

#### Constructors

<a id="member-m-cephalon-abstractions-audit-auditactor-ctor-system-string-system-string-system-string-system-boolean-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuditActor`

```csharp
AuditActor(string actorId, string displayName, string actorType, bool isSystem, IReadOnlyDictionary<string, string> attributes)
```

Creates a new audit actor.

Parameters:
- `actorId`: The stable actor identifier.
- `displayName`: The human-readable actor name when one is known.
- `actorType`: The logical actor type such as `user`, `service`, or `system`.
- `isSystem`: Whether the actor represents system-owned automation.
- `attributes`: Optional actor attributes.

#### Properties

<a id="member-p-cephalon-abstractions-audit-auditactor-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the stable actor identifier.

<a id="member-p-cephalon-abstractions-audit-auditactor-actortype"></a>

##### `ActorType`

```csharp
string ActorType { get; }
```

Gets the logical actor type when one is known.

<a id="member-p-cephalon-abstractions-audit-auditactor-attributes"></a>

##### `Attributes`

```csharp
IReadOnlyDictionary<string, string> Attributes { get; }
```

Gets the actor attributes.

<a id="member-p-cephalon-abstractions-audit-auditactor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable actor name when one is known.

<a id="member-p-cephalon-abstractions-audit-auditactor-issystem"></a>

##### `IsSystem`

```csharp
bool IsSystem { get; }
```

Gets a value indicating whether the actor represents system-owned automation.

<a id="type-cephalon-abstractions-audit-auditchange"></a>

### `AuditChange`

Describes one field-level change captured by an audit entry.

#### Declaration
```csharp
public sealed class AuditChange
```

#### Constructors

<a id="member-m-cephalon-abstractions-audit-auditchange-ctor-system-string-system-string-system-string"></a>

##### `AuditChange`

```csharp
AuditChange(string fieldName, string oldValue, string newValue)
```

Creates a new audit change.

Parameters:
- `fieldName`: The logical field or property name that changed.
- `oldValue`: The previous serialized value when one is known.
- `newValue`: The new serialized value when one is known.

#### Properties

<a id="member-p-cephalon-abstractions-audit-auditchange-fieldname"></a>

##### `FieldName`

```csharp
string FieldName { get; }
```

Gets the logical field or property name that changed.

<a id="member-p-cephalon-abstractions-audit-auditchange-newvalue"></a>

##### `NewValue`

```csharp
string NewValue { get; }
```

Gets the new serialized value when one is known.

<a id="member-p-cephalon-abstractions-audit-auditchange-oldvalue"></a>

##### `OldValue`

```csharp
string OldValue { get; }
```

Gets the previous serialized value when one is known.

<a id="type-cephalon-abstractions-audit-auditentry"></a>

### `AuditEntry`

Describes one auditable operation recorded by the active audit implementation.

#### Declaration
```csharp
public sealed class AuditEntry
```

#### Constructors

<a id="member-m-cephalon-abstractions-audit-auditentry-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-datetimeoffset-cephalon-abstractions-audit-auditactor-cephalon-abstractions-audit-auditoutcome-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-audit-auditchange-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuditEntry`

```csharp
AuditEntry(string id, string category, string action, string summary, string subjectType, string subjectId, DateTimeOffset occurredAtUtc, AuditActor actor, AuditOutcome outcome, string tenantId, string correlationId, IReadOnlyList<AuditChange> changes, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new audit entry.

Parameters:
- `id`: The stable audit-entry identifier.
- `category`: The logical audit category such as `identity`, `tenant`, or `billing`.
- `action`: The logical action identifier associated with the audit event.
- `summary`: The human-readable audit summary.
- `subjectType`: The logical subject type associated with the entry.
- `subjectId`: The stable subject identifier associated with the entry when one is known.
- `occurredAtUtc`: The time at which the audited operation occurred.
- `actor`: The actor responsible for the audited operation.
- `outcome`: The outcome recorded for the audited operation.
- `tenantId`: The tenant identifier associated with the audited operation.
- `correlationId`: The correlation identifier associated with the audited operation.
- `changes`: Optional field-level changes captured for the operation.
- `tags`: Optional descriptive tags associated with the entry.
- `metadata`: Optional audit metadata.

#### Properties

<a id="member-p-cephalon-abstractions-audit-auditentry-action"></a>

##### `Action`

```csharp
string Action { get; }
```

Gets the logical action identifier associated with the audit event.

<a id="member-p-cephalon-abstractions-audit-auditentry-actor"></a>

##### `Actor`

```csharp
AuditActor Actor { get; }
```

Gets the actor responsible for the audited operation.

<a id="member-p-cephalon-abstractions-audit-auditentry-category"></a>

##### `Category`

```csharp
string Category { get; }
```

Gets the logical audit category.

<a id="member-p-cephalon-abstractions-audit-auditentry-changes"></a>

##### `Changes`

```csharp
IReadOnlyList<AuditChange> Changes { get; }
```

Gets the field-level changes captured for the operation.

<a id="member-p-cephalon-abstractions-audit-auditentry-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier associated with the audited operation.

<a id="member-p-cephalon-abstractions-audit-auditentry-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable audit-entry identifier.

<a id="member-p-cephalon-abstractions-audit-auditentry-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets audit metadata associated with the entry.

<a id="member-p-cephalon-abstractions-audit-auditentry-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; }
```

Gets the time at which the audited operation occurred.

<a id="member-p-cephalon-abstractions-audit-auditentry-outcome"></a>

##### `Outcome`

```csharp
AuditOutcome Outcome { get; }
```

Gets the outcome recorded for the audited operation.

<a id="member-p-cephalon-abstractions-audit-auditentry-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the stable subject identifier associated with the entry when one is known.

<a id="member-p-cephalon-abstractions-audit-auditentry-subjecttype"></a>

##### `SubjectType`

```csharp
string SubjectType { get; }
```

Gets the logical subject type associated with the entry.

<a id="member-p-cephalon-abstractions-audit-auditentry-summary"></a>

##### `Summary`

```csharp
string Summary { get; }
```

Gets the human-readable audit summary.

<a id="member-p-cephalon-abstractions-audit-auditentry-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the entry.

<a id="member-p-cephalon-abstractions-audit-auditentry-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the audited operation.

<a id="type-cephalon-abstractions-audit-audithistoryentry"></a>

### `AuditHistoryEntry`

Represents one audit entry returned from a durable or queryable audit-history store.

#### Declaration
```csharp
public sealed class AuditHistoryEntry
```

#### Constructors

<a id="member-m-cephalon-abstractions-audit-audithistoryentry-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-datetimeoffset-system-datetimeoffset-cephalon-abstractions-audit-auditactor-cephalon-abstractions-audit-auditoutcome-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-audit-auditchange-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuditHistoryEntry`

```csharp
AuditHistoryEntry(string id, string category, string action, string summary, string subjectType, string subjectId, DateTimeOffset occurredAtUtc, DateTimeOffset persistedAtUtc, AuditActor actor, AuditOutcome outcome, string tenantId, string correlationId, IReadOnlyList<AuditChange> changes, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new audit-history entry.

Parameters:
- `id`: The stable audit-entry identifier.
- `category`: The logical audit category such as `identity`, `tenant`, or `billing`.
- `action`: The logical action identifier associated with the audit event.
- `summary`: The human-readable audit summary.
- `subjectType`: The logical subject type associated with the entry.
- `subjectId`: The stable subject identifier associated with the entry when one is known.
- `occurredAtUtc`: The time at which the audited operation occurred.
- `persistedAtUtc`: The time at which the audit entry was durably persisted.
- `actor`: The actor responsible for the audited operation.
- `outcome`: The outcome recorded for the audited operation.
- `tenantId`: The tenant identifier associated with the audited operation.
- `correlationId`: The correlation identifier associated with the audited operation.
- `changes`: Optional field-level changes captured for the operation.
- `tags`: Optional descriptive tags associated with the entry.
- `metadata`: Optional audit metadata.

#### Properties

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-action"></a>

##### `Action`

```csharp
string Action { get; }
```

Gets the logical action identifier associated with the audit event.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-actor"></a>

##### `Actor`

```csharp
AuditActor Actor { get; }
```

Gets the actor responsible for the audited operation.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-category"></a>

##### `Category`

```csharp
string Category { get; }
```

Gets the logical audit category.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-changes"></a>

##### `Changes`

```csharp
IReadOnlyList<AuditChange> Changes { get; }
```

Gets the field-level changes captured for the operation.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier associated with the audited operation.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable audit-entry identifier.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets audit metadata associated with the entry.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; }
```

Gets the time at which the audited operation occurred.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-outcome"></a>

##### `Outcome`

```csharp
AuditOutcome Outcome { get; }
```

Gets the outcome recorded for the audited operation.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-persistedatutc"></a>

##### `PersistedAtUtc`

```csharp
DateTimeOffset PersistedAtUtc { get; }
```

Gets the time at which the audit entry was durably persisted.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the stable subject identifier associated with the entry when one is known.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-subjecttype"></a>

##### `SubjectType`

```csharp
string SubjectType { get; }
```

Gets the logical subject type associated with the entry.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-summary"></a>

##### `Summary`

```csharp
string Summary { get; }
```

Gets the human-readable audit summary.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the entry.

<a id="member-p-cephalon-abstractions-audit-audithistoryentry-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the audited operation.

<a id="type-cephalon-abstractions-audit-audithistoryexportrequest"></a>

### `AuditHistoryExportRequest`

Describes a host-agnostic audit-history export request against the active audit-history exporter.

#### Declaration
```csharp
public sealed class AuditHistoryExportRequest
```

#### Constructors

<a id="member-m-cephalon-abstractions-audit-audithistoryexportrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-cephalon-abstractions-audit-auditoutcome-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-int32"></a>

##### `AuditHistoryExportRequest`

```csharp
AuditHistoryExportRequest(string category, string action, string subjectType, string subjectId, string actorId, string tenantId, string correlationId, AuditOutcome? outcome, DateTimeOffset? occurredFromUtc, DateTimeOffset? occurredToUtc, int maxEntries)
```

Creates a new audit-history export request.

Parameters:
- `category`: An optional logical audit category filter.
- `action`: An optional logical action identifier filter.
- `subjectType`: An optional logical subject-type filter.
- `subjectId`: An optional stable subject identifier filter.
- `actorId`: An optional stable actor identifier filter.
- `tenantId`: An optional tenant identifier filter.
- `correlationId`: An optional correlation identifier filter.
- `outcome`: An optional audit-outcome filter.
- `occurredFromUtc`: An optional inclusive lower occurrence bound.
- `occurredToUtc`: An optional inclusive upper occurrence bound.
- `maxEntries`: The maximum number of exported entries.

#### Fields

<a id="member-f-cephalon-abstractions-audit-audithistoryexportrequest-defaultmaxentries"></a>

##### `DefaultMaxEntries`

```csharp
const int DefaultMaxEntries
```

Gets the default maximum number of audit-history entries exported when the caller does not supply one.

#### Properties

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-action"></a>

##### `Action`

```csharp
string Action { get; }
```

Gets the optional logical action identifier filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the optional stable actor identifier filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-category"></a>

##### `Category`

```csharp
string Category { get; }
```

Gets the optional logical audit category filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-maxentries"></a>

##### `MaxEntries`

```csharp
int MaxEntries { get; }
```

Gets the maximum number of entries to export.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-occurredfromutc"></a>

##### `OccurredFromUtc`

```csharp
DateTimeOffset? OccurredFromUtc { get; }
```

Gets the optional inclusive lower occurrence bound.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-occurredtoutc"></a>

##### `OccurredToUtc`

```csharp
DateTimeOffset? OccurredToUtc { get; }
```

Gets the optional inclusive upper occurrence bound.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-outcome"></a>

##### `Outcome`

```csharp
AuditOutcome? Outcome { get; }
```

Gets the optional audit-outcome filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the optional stable subject identifier filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-subjecttype"></a>

##### `SubjectType`

```csharp
string SubjectType { get; }
```

Gets the optional logical subject-type filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryexportrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the optional tenant identifier filter.

<a id="type-cephalon-abstractions-audit-audithistoryquery"></a>

### `AuditHistoryQuery`

Describes a host-agnostic audit-history query against the active audit-history reader.

#### Declaration
```csharp
public sealed class AuditHistoryQuery
```

#### Constructors

<a id="member-m-cephalon-abstractions-audit-audithistoryquery-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-cephalon-abstractions-audit-auditoutcome-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-int32-system-int32"></a>

##### `AuditHistoryQuery`

```csharp
AuditHistoryQuery(string category, string action, string subjectType, string subjectId, string actorId, string tenantId, string correlationId, AuditOutcome? outcome, DateTimeOffset? occurredFromUtc, DateTimeOffset? occurredToUtc, int offset, int limit)
```

Creates a new audit-history query.

Parameters:
- `category`: An optional logical audit category filter.
- `action`: An optional logical action identifier filter.
- `subjectType`: An optional logical subject-type filter.
- `subjectId`: An optional stable subject identifier filter.
- `actorId`: An optional stable actor identifier filter.
- `tenantId`: An optional tenant identifier filter.
- `correlationId`: An optional correlation identifier filter.
- `outcome`: An optional audit-outcome filter.
- `occurredFromUtc`: An optional inclusive lower occurrence bound.
- `occurredToUtc`: An optional inclusive upper occurrence bound.
- `offset`: The zero-based query offset.
- `limit`: The requested page size, clamped to the supported range.

#### Fields

<a id="member-f-cephalon-abstractions-audit-audithistoryquery-defaultlimit"></a>

##### `DefaultLimit`

```csharp
const int DefaultLimit
```

Gets the default number of entries returned by a query when the caller does not supply one.

<a id="member-f-cephalon-abstractions-audit-audithistoryquery-maxlimit"></a>

##### `MaxLimit`

```csharp
const int MaxLimit
```

Gets the maximum number of entries returned by one query.

#### Properties

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-action"></a>

##### `Action`

```csharp
string Action { get; }
```

Gets the optional logical action identifier filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the optional stable actor identifier filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-category"></a>

##### `Category`

```csharp
string Category { get; }
```

Gets the optional logical audit category filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-limit"></a>

##### `Limit`

```csharp
int Limit { get; }
```

Gets the requested page size after it has been normalized to the supported range.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-occurredfromutc"></a>

##### `OccurredFromUtc`

```csharp
DateTimeOffset? OccurredFromUtc { get; }
```

Gets the optional inclusive lower occurrence bound.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-occurredtoutc"></a>

##### `OccurredToUtc`

```csharp
DateTimeOffset? OccurredToUtc { get; }
```

Gets the optional inclusive upper occurrence bound.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-offset"></a>

##### `Offset`

```csharp
int Offset { get; }
```

Gets the zero-based query offset.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-outcome"></a>

##### `Outcome`

```csharp
AuditOutcome? Outcome { get; }
```

Gets the optional audit-outcome filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the optional stable subject identifier filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-subjecttype"></a>

##### `SubjectType`

```csharp
string SubjectType { get; }
```

Gets the optional logical subject-type filter.

<a id="member-p-cephalon-abstractions-audit-audithistoryquery-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the optional tenant identifier filter.

<a id="type-cephalon-abstractions-audit-audithistoryqueryresult"></a>

### `AuditHistoryQueryResult`

Represents one page of audit-history results returned by an `IAuditHistoryReader`.

#### Declaration
```csharp
public sealed class AuditHistoryQueryResult
```

#### Constructors

<a id="member-m-cephalon-abstractions-audit-audithistoryqueryresult-ctor-system-collections-generic-ireadonlylist-cephalon-abstractions-audit-audithistoryentry-system-int32-system-int32-system-int32"></a>

##### `AuditHistoryQueryResult`

```csharp
AuditHistoryQueryResult(IReadOnlyList<AuditHistoryEntry> entries, int offset, int limit, int totalCount)
```

Creates a new audit-history query result.

Parameters:
- `entries`: The returned audit-history entries.
- `offset`: The zero-based query offset that produced this page.
- `limit`: The normalized page size used for the query.
- `totalCount`: The total number of matching entries before paging was applied.

#### Properties

<a id="member-p-cephalon-abstractions-audit-audithistoryqueryresult-entries"></a>

##### `Entries`

```csharp
IReadOnlyList<AuditHistoryEntry> Entries { get; }
```

Gets the returned audit-history entries.

<a id="member-p-cephalon-abstractions-audit-audithistoryqueryresult-hasmore"></a>

##### `HasMore`

```csharp
bool HasMore { get; }
```

Gets a value indicating whether more entries remain beyond this page.

<a id="member-p-cephalon-abstractions-audit-audithistoryqueryresult-limit"></a>

##### `Limit`

```csharp
int Limit { get; }
```

Gets the normalized page size used for the query.

<a id="member-p-cephalon-abstractions-audit-audithistoryqueryresult-offset"></a>

##### `Offset`

```csharp
int Offset { get; }
```

Gets the zero-based query offset that produced this page.

<a id="member-p-cephalon-abstractions-audit-audithistoryqueryresult-totalcount"></a>

##### `TotalCount`

```csharp
int TotalCount { get; }
```

Gets the total number of matching entries before paging was applied.

<a id="type-cephalon-abstractions-audit-auditoutcome"></a>

### `AuditOutcome`

Identifies the outcome recorded for an audit entry.

#### Declaration
```csharp
public enum AuditOutcome
```

#### Fields

<a id="member-f-cephalon-abstractions-audit-auditoutcome-failed"></a>

##### `Failed`

```csharp
const AuditOutcome Failed
```

Indicates the operation failed.

<a id="member-f-cephalon-abstractions-audit-auditoutcome-succeeded"></a>

##### `Succeeded`

```csharp
const AuditOutcome Succeeded
```

Indicates the operation completed successfully.

<a id="member-f-cephalon-abstractions-audit-auditoutcome-unknown"></a>

##### `Unknown`

```csharp
const AuditOutcome Unknown
```

Indicates the operation outcome was not explicitly classified.

<a id="type-cephalon-abstractions-audit-auditstoredescriptor"></a>

### `AuditStoreDescriptor`

Describes one audit store surface contributed to the active runtime.

#### Declaration
```csharp
public sealed class AuditStoreDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-audit-auditstoredescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuditStoreDescriptor`

```csharp
AuditStoreDescriptor(string id, string displayName, string description, string sourceModuleId, string provider, string mode, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new audit-store descriptor.

Parameters:
- `id`: The stable audit-store identifier.
- `displayName`: The operator-facing audit-store name.
- `description`: The human-readable audit-store description.
- `sourceModuleId`: The module identifier that owns the audit-store surface.
- `provider`: The logical provider identifier that backs the audit-store surface.
- `mode`: The audit-store mode such as `volatile-buffer` or `transactional-table`.
- `tags`: Optional descriptive tags associated with the audit store.
- `metadata`: Optional operator-facing metadata associated with the audit store.

#### Properties

<a id="member-p-cephalon-abstractions-audit-auditstoredescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable audit-store description.

<a id="member-p-cephalon-abstractions-audit-auditstoredescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing audit-store name.

<a id="member-p-cephalon-abstractions-audit-auditstoredescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable audit-store identifier.

<a id="member-p-cephalon-abstractions-audit-auditstoredescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the audit store.

<a id="member-p-cephalon-abstractions-audit-auditstoredescriptor-mode"></a>

##### `Mode`

```csharp
string Mode { get; }
```

Gets the audit-store mode.

<a id="member-p-cephalon-abstractions-audit-auditstoredescriptor-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the logical provider identifier that backs the audit-store surface.

<a id="member-p-cephalon-abstractions-audit-auditstoredescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that owns the audit-store surface.

<a id="member-p-cephalon-abstractions-audit-auditstoredescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the audit store.

<a id="type-cephalon-abstractions-audit-iaudithistoryexporter"></a>

### `IAuditHistoryExporter`

Streams persisted audit-history entries for export-oriented operator or application flows.

#### Declaration
```csharp
public interface IAuditHistoryExporter
```

#### Methods

<a id="member-m-cephalon-abstractions-audit-iaudithistoryexporter-exportasync-cephalon-abstractions-audit-audithistoryexportrequest-system-threading-cancellationtoken"></a>

##### `ExportAsync`

```csharp
IAsyncEnumerable<AuditHistoryEntry> ExportAsync(AuditHistoryExportRequest request, CancellationToken cancellationToken)
```

Streams audit-history entries that match the supplied export request in stable export order.

Returns: The matching audit-history entries.

Parameters:
- `request`: The export request to execute.
- `cancellationToken`: The token that cancels the export stream.

<a id="type-cephalon-abstractions-audit-iaudithistoryreader"></a>

### `IAuditHistoryReader`

Reads persisted audit-history entries from the active runtime.

#### Declaration
```csharp
public interface IAuditHistoryReader
```

#### Methods

<a id="member-m-cephalon-abstractions-audit-iaudithistoryreader-getbyidasync-system-string-system-threading-cancellationtoken"></a>

##### `GetByIdAsync`

```csharp
ValueTask<AuditHistoryEntry> GetByIdAsync(string auditEntryId, CancellationToken cancellationToken)
```

Resolves one audit-history entry by its stable identifier.

Returns: The matching audit-history entry when one exists; otherwise `null`.

Parameters:
- `auditEntryId`: The stable audit-entry identifier to resolve.
- `cancellationToken`: The token that cancels the operation.

<a id="member-m-cephalon-abstractions-audit-iaudithistoryreader-queryasync-cephalon-abstractions-audit-audithistoryquery-system-threading-cancellationtoken"></a>

##### `QueryAsync`

```csharp
ValueTask<AuditHistoryQueryResult> QueryAsync(AuditHistoryQuery query, CancellationToken cancellationToken)
```

Queries audit-history entries using the supplied host-agnostic filter set.

Returns: The resulting page of audit-history entries.

Parameters:
- `query`: The query to execute.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-audit-iauditstorecatalog"></a>

### `IAuditStoreCatalog`

Exposes the audit-store surfaces visible to the current runtime.

#### Declaration
```csharp
public interface IAuditStoreCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-audit-iauditstorecatalog-auditstores"></a>

##### `AuditStores`

```csharp
IReadOnlyList<AuditStoreDescriptor> AuditStores { get; }
```

Gets all audit-store surfaces visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-audit-iauditstorecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
AuditStoreDescriptor GetById(string auditStoreId)
```

Gets one audit store by its stable identifier.

Returns: The matching audit store, or `null` when it is not active.

Parameters:
- `auditStoreId`: The audit-store identifier to resolve.

<a id="member-m-cephalon-abstractions-audit-iauditstorecatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<AuditStoreDescriptor> GetByProvider(string provider)
```

Gets all audit stores backed by the requested provider identifier.

Returns: The matching audit stores, or an empty list when the provider contributes none.

Parameters:
- `provider`: The provider identifier to filter by.

<a id="member-m-cephalon-abstractions-audit-iauditstorecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<AuditStoreDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all audit stores contributed by the requested module.

Returns: The matching audit stores, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="type-cephalon-abstractions-audit-iauditstorecontributor"></a>

### `IAuditStoreContributor`

Contributes one or more audit-store descriptors to the active runtime.

#### Declaration
```csharp
public interface IAuditStoreContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-audit-iauditstorecontributor-registerauditstores-cephalon-abstractions-audit-iauditstoreregistry"></a>

##### `RegisterAuditStores`

```csharp
void RegisterAuditStores(IAuditStoreRegistry auditStores)
```

Registers one or more audit-store descriptors with the supplied registry.

Parameters:
- `auditStores`: The registry that collects contributed audit-store descriptors.

<a id="type-cephalon-abstractions-audit-iauditstoreregistry"></a>

### `IAuditStoreRegistry`

Receives audit-store descriptors contributed by active modules or packages.

#### Declaration
```csharp
public interface IAuditStoreRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-audit-iauditstoreregistry-add-cephalon-abstractions-audit-auditstoredescriptor"></a>

##### `Add`

```csharp
void Add(AuditStoreDescriptor auditStore)
```

Adds an audit store to the current runtime composition.

Parameters:
- `auditStore`: The audit-store descriptor to register.

<a id="type-cephalon-abstractions-audit-iauditstoreruntimecontributor"></a>

### `IAuditStoreRuntimeContributor`

Contributes runtime-resolved audit-store descriptors to the active Cephalon audit surface.

#### Declaration
```csharp
public interface IAuditStoreRuntimeContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-audit-iauditstoreruntimecontributor-describeauditstores"></a>

##### `DescribeAuditStores`

```csharp
IReadOnlyList<AuditStoreDescriptor> DescribeAuditStores()
```

Describes the audit stores that should appear in the active runtime after configuration, topology, and provider-specific options have been resolved.

Returns: The audit-store descriptors that should appear in the active runtime.

<a id="type-cephalon-abstractions-audit-iauditwriter"></a>

### `IAuditWriter`

Persists audit entries for the current runtime.

#### Declaration
```csharp
public interface IAuditWriter
```

#### Methods

<a id="member-m-cephalon-abstractions-audit-iauditwriter-writeasync-cephalon-abstractions-audit-auditentry-system-threading-cancellationtoken"></a>

##### `WriteAsync`

```csharp
ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
```

Writes one audit entry.

Returns: A task that completes when the audit entry has been written.

Parameters:
- `entry`: The audit entry to write.
- `cancellationToken`: The token that cancels the operation.

<a id="namespace-cephalon-abstractions-authorization"></a>

## Namespace Cephalon.Abstractions.Authorization

<a id="type-cephalon-abstractions-authorization-authorizationcontext"></a>

### `AuthorizationContext`

Describes the operation-specific context supplied to an authorization evaluation.

#### Declaration
```csharp
public sealed class AuthorizationContext
```

#### Constructors

<a id="member-m-cephalon-abstractions-authorization-authorizationcontext-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuthorizationContext`

```csharp
AuthorizationContext(string action, string policyId, string tenantId, string correlationId, IReadOnlyDictionary<string, string> attributes)
```

Creates a new authorization context.

Parameters:
- `action`: The action being requested, such as `read`, `write`, or `approve`.
- `policyId`: The explicit policy identifier requested by the caller when one is known.
- `tenantId`: The tenant identifier associated with the current operation.
- `correlationId`: The correlation identifier associated with the current operation.
- `attributes`: Optional operation-specific attributes.

#### Properties

<a id="member-p-cephalon-abstractions-authorization-authorizationcontext-action"></a>

##### `Action`

```csharp
string Action { get; }
```

Gets the action being requested.

<a id="member-p-cephalon-abstractions-authorization-authorizationcontext-attributes"></a>

##### `Attributes`

```csharp
IReadOnlyDictionary<string, string> Attributes { get; }
```

Gets the operation-specific attributes.

<a id="member-p-cephalon-abstractions-authorization-authorizationcontext-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier associated with the current operation.

<a id="member-p-cephalon-abstractions-authorization-authorizationcontext-policyid"></a>

##### `PolicyId`

```csharp
string PolicyId { get; }
```

Gets the explicit policy identifier requested by the caller when one is known.

<a id="member-p-cephalon-abstractions-authorization-authorizationcontext-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the current operation.

<a id="type-cephalon-abstractions-authorization-authorizationdecision"></a>

### `AuthorizationDecision`

Describes the outcome of one authorization evaluation.

#### Declaration
```csharp
public sealed class AuthorizationDecision
```

#### Constructors

<a id="member-m-cephalon-abstractions-authorization-authorizationdecision-ctor-system-boolean-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-authorization-authorizationmode-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuthorizationDecision`

```csharp
AuthorizationDecision(bool isAllowed, string policyId, string reason, IReadOnlyList<AuthorizationMode> modes, IReadOnlyDictionary<string, string> metadata)
```

Creates a new authorization decision.

Parameters:
- `isAllowed`: Whether access was allowed.
- `policyId`: The policy identifier that produced the decision when one is known.
- `reason`: The human-readable reason associated with the decision.
- `modes`: The authorization modes that participated in the decision.
- `metadata`: Optional decision metadata.

#### Properties

<a id="member-p-cephalon-abstractions-authorization-authorizationdecision-isallowed"></a>

##### `IsAllowed`

```csharp
bool IsAllowed { get; }
```

Gets a value indicating whether access was allowed.

<a id="member-p-cephalon-abstractions-authorization-authorizationdecision-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional decision metadata.

<a id="member-p-cephalon-abstractions-authorization-authorizationdecision-modes"></a>

##### `Modes`

```csharp
IReadOnlyList<AuthorizationMode> Modes { get; }
```

Gets the authorization modes that participated in the decision.

<a id="member-p-cephalon-abstractions-authorization-authorizationdecision-policyid"></a>

##### `PolicyId`

```csharp
string PolicyId { get; }
```

Gets the policy identifier that produced the decision when one is known.

<a id="member-p-cephalon-abstractions-authorization-authorizationdecision-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the human-readable reason associated with the decision.

#### Methods

<a id="member-m-cephalon-abstractions-authorization-authorizationdecision-allow-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-authorization-authorizationmode-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Allow`

```csharp
AuthorizationDecision Allow(string policyId, string reason, IReadOnlyList<AuthorizationMode> modes, IReadOnlyDictionary<string, string> metadata)
```

Creates an allowed authorization decision.

Returns: An allowed authorization decision.

Parameters:
- `policyId`: The policy identifier that produced the decision when one is known.
- `reason`: The human-readable reason associated with the decision.
- `modes`: The authorization modes that participated in the decision.
- `metadata`: Optional decision metadata.

<a id="member-m-cephalon-abstractions-authorization-authorizationdecision-deny-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-authorization-authorizationmode-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Deny`

```csharp
AuthorizationDecision Deny(string policyId, string reason, IReadOnlyList<AuthorizationMode> modes, IReadOnlyDictionary<string, string> metadata)
```

Creates a denied authorization decision.

Returns: A denied authorization decision.

Parameters:
- `policyId`: The policy identifier that produced the decision when one is known.
- `reason`: The human-readable reason associated with the decision.
- `modes`: The authorization modes that participated in the decision.
- `metadata`: Optional decision metadata.

<a id="type-cephalon-abstractions-authorization-authorizationmode"></a>

### `AuthorizationMode`

Identifies one authorization approach active inside a policy evaluation.

#### Declaration
```csharp
public enum AuthorizationMode
```

#### Fields

<a id="member-f-cephalon-abstractions-authorization-authorizationmode-abac"></a>

##### `Abac`

```csharp
const AuthorizationMode Abac
```

Indicates an attribute-based access-control evaluation.

<a id="member-f-cephalon-abstractions-authorization-authorizationmode-policy"></a>

##### `Policy`

```csharp
const AuthorizationMode Policy
```

Indicates a policy-driven authorization evaluation.

<a id="member-f-cephalon-abstractions-authorization-authorizationmode-rbac"></a>

##### `Rbac`

```csharp
const AuthorizationMode Rbac
```

Indicates a role-based access-control evaluation.

<a id="type-cephalon-abstractions-authorization-authorizationpolicydescriptor"></a>

### `AuthorizationPolicyDescriptor`

Describes one authorization policy surface contributed to the active runtime.

#### Declaration
```csharp
public sealed class AuthorizationPolicyDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-authorization-authorizationpolicydescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-authorization-authorizationmode-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuthorizationPolicyDescriptor`

```csharp
AuthorizationPolicyDescriptor(string id, string displayName, string description, IReadOnlyList<AuthorizationMode> modes, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new authorization policy descriptor.

Parameters:
- `id`: The stable authorization-policy identifier.
- `displayName`: The operator-facing authorization-policy name.
- `description`: The human-readable authorization-policy description.
- `modes`: The authorization modes supported by the policy.
- `tags`: Optional descriptive tags associated with the policy.
- `metadata`: Optional operator-facing metadata associated with the policy.

#### Properties

<a id="member-p-cephalon-abstractions-authorization-authorizationpolicydescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable authorization-policy description.

<a id="member-p-cephalon-abstractions-authorization-authorizationpolicydescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing authorization-policy name.

<a id="member-p-cephalon-abstractions-authorization-authorizationpolicydescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable authorization-policy identifier.

<a id="member-p-cephalon-abstractions-authorization-authorizationpolicydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the policy.

<a id="member-p-cephalon-abstractions-authorization-authorizationpolicydescriptor-modes"></a>

##### `Modes`

```csharp
IReadOnlyList<AuthorizationMode> Modes { get; }
```

Gets the authorization modes supported by the policy.

<a id="member-p-cephalon-abstractions-authorization-authorizationpolicydescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the policy.

<a id="type-cephalon-abstractions-authorization-authorizationresource"></a>

### `AuthorizationResource`

Describes the protected resource being evaluated by an authorization policy.

#### Declaration
```csharp
public sealed class AuthorizationResource
```

#### Constructors

<a id="member-m-cephalon-abstractions-authorization-authorizationresource-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuthorizationResource`

```csharp
AuthorizationResource(string resourceType, string resourceId, string tenantId, string ownerSubjectId, IReadOnlyDictionary<string, string> attributes)
```

Creates a new authorization resource.

Parameters:
- `resourceType`: The logical resource type identifier.
- `resourceId`: The stable resource identifier when one is known.
- `tenantId`: The tenant identifier associated with the resource.
- `ownerSubjectId`: The owning subject identifier when one is known.
- `attributes`: Optional attributes associated with the resource.

#### Properties

<a id="member-p-cephalon-abstractions-authorization-authorizationresource-attributes"></a>

##### `Attributes`

```csharp
IReadOnlyDictionary<string, string> Attributes { get; }
```

Gets the attributes associated with the resource.

<a id="member-p-cephalon-abstractions-authorization-authorizationresource-ownersubjectid"></a>

##### `OwnerSubjectId`

```csharp
string OwnerSubjectId { get; }
```

Gets the owning subject identifier when one is known.

<a id="member-p-cephalon-abstractions-authorization-authorizationresource-resourceid"></a>

##### `ResourceId`

```csharp
string ResourceId { get; }
```

Gets the stable resource identifier when one is known.

<a id="member-p-cephalon-abstractions-authorization-authorizationresource-resourcetype"></a>

##### `ResourceType`

```csharp
string ResourceType { get; }
```

Gets the logical resource type identifier.

<a id="member-p-cephalon-abstractions-authorization-authorizationresource-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the resource.

<a id="type-cephalon-abstractions-authorization-authorizationsubject"></a>

### `AuthorizationSubject`

Describes the caller or actor being evaluated by an authorization policy.

#### Declaration
```csharp
public sealed class AuthorizationSubject
```

#### Constructors

<a id="member-m-cephalon-abstractions-authorization-authorizationsubject-ctor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuthorizationSubject`

```csharp
AuthorizationSubject(string subjectId, string displayName, IReadOnlyList<string> roles, IReadOnlyList<string> tenantIds, IReadOnlyDictionary<string, string> attributes)
```

Creates a new authorization subject.

Parameters:
- `subjectId`: The stable subject identifier.
- `displayName`: The human-readable subject name when one is known.
- `roles`: Optional roles assigned to the subject.
- `tenantIds`: Optional tenant identifiers associated with the subject.
- `attributes`: Optional attributes associated with the subject.

#### Properties

<a id="member-p-cephalon-abstractions-authorization-authorizationsubject-attributes"></a>

##### `Attributes`

```csharp
IReadOnlyDictionary<string, string> Attributes { get; }
```

Gets the attributes associated with the subject.

<a id="member-p-cephalon-abstractions-authorization-authorizationsubject-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable subject name when one is known.

<a id="member-p-cephalon-abstractions-authorization-authorizationsubject-roles"></a>

##### `Roles`

```csharp
IReadOnlyList<string> Roles { get; }
```

Gets the roles assigned to the subject.

<a id="member-p-cephalon-abstractions-authorization-authorizationsubject-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the stable subject identifier.

<a id="member-p-cephalon-abstractions-authorization-authorizationsubject-tenantids"></a>

##### `TenantIds`

```csharp
IReadOnlyList<string> TenantIds { get; }
```

Gets the tenant identifiers associated with the subject.

<a id="type-cephalon-abstractions-authorization-iauthorizationevaluator"></a>

### `IAuthorizationEvaluator`

Evaluates access decisions for the current authorization runtime.

#### Declaration
```csharp
public interface IAuthorizationEvaluator
```

#### Methods

<a id="member-m-cephalon-abstractions-authorization-iauthorizationevaluator-evaluateasync-cephalon-abstractions-authorization-authorizationsubject-cephalon-abstractions-authorization-authorizationresource-cephalon-abstractions-authorization-authorizationcontext-system-threading-cancellationtoken"></a>

##### `EvaluateAsync`

```csharp
ValueTask<AuthorizationDecision> EvaluateAsync(AuthorizationSubject subject, AuthorizationResource resource, AuthorizationContext context, CancellationToken cancellationToken)
```

Evaluates one authorization request.

Returns: A task that completes with the resulting authorization decision.

Parameters:
- `subject`: The subject requesting access.
- `resource`: The protected resource being accessed.
- `context`: The operation-specific authorization context.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-authorization-iauthorizationpolicycatalog"></a>

### `IAuthorizationPolicyCatalog`

Exposes the authorization policies visible to the current runtime.

#### Declaration
```csharp
public interface IAuthorizationPolicyCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-authorization-iauthorizationpolicycatalog-policies"></a>

##### `Policies`

```csharp
IReadOnlyList<AuthorizationPolicyDescriptor> Policies { get; }
```

Gets all authorization policies visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-authorization-iauthorizationpolicycatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
AuthorizationPolicyDescriptor GetById(string policyId)
```

Gets one authorization policy by its stable identifier.

Returns: The matching policy, or `null` when it is not active.

Parameters:
- `policyId`: The authorization-policy identifier to resolve.

<a id="member-m-cephalon-abstractions-authorization-iauthorizationpolicycatalog-getbymode-cephalon-abstractions-authorization-authorizationmode"></a>

##### `GetByMode`

```csharp
IReadOnlyList<AuthorizationPolicyDescriptor> GetByMode(AuthorizationMode mode)
```

Gets all authorization policies that support the requested mode.

Returns: The matching policies, or an empty list when none support the requested mode.

Parameters:
- `mode`: The authorization mode to filter by.

<a id="type-cephalon-abstractions-authorization-iauthorizationpolicycontributor"></a>

### `IAuthorizationPolicyContributor`

Contributes one or more authorization-policy descriptors to the active runtime.

#### Declaration
```csharp
public interface IAuthorizationPolicyContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-authorization-iauthorizationpolicycontributor-registerpolicies-cephalon-abstractions-authorization-iauthorizationpolicyregistry"></a>

##### `RegisterPolicies`

```csharp
void RegisterPolicies(IAuthorizationPolicyRegistry policies)
```

Registers one or more authorization-policy descriptors with the supplied registry.

Parameters:
- `policies`: The registry that collects contributed authorization-policy descriptors.

<a id="type-cephalon-abstractions-authorization-iauthorizationpolicyregistry"></a>

### `IAuthorizationPolicyRegistry`

Receives authorization-policy descriptors contributed by active modules or packages.

#### Declaration
```csharp
public interface IAuthorizationPolicyRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-authorization-iauthorizationpolicyregistry-add-cephalon-abstractions-authorization-authorizationpolicydescriptor"></a>

##### `Add`

```csharp
void Add(AuthorizationPolicyDescriptor policy)
```

Adds an authorization policy to the current runtime composition.

Parameters:
- `policy`: The authorization-policy descriptor to register.

<a id="namespace-cephalon-abstractions-behaviors"></a>

## Namespace Cephalon.Abstractions.Behaviors

<a id="type-cephalon-abstractions-behaviors-appbehaviorattribute"></a>

### `AppBehaviorAttribute`

Marks a class as a registered application behavior and assigns its stable identifier. This attribute is required on all types registered via `IBehaviorCollectionBuilder.Register<TBehavior>()`.

#### Declaration
```csharp
public sealed class AppBehaviorAttribute
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-appbehaviorattribute-ctor-system-string"></a>

##### `AppBehaviorAttribute`

```csharp
AppBehaviorAttribute(string id)
```

Initializes the attribute with the behavior's stable identifier.

Parameters:
- `id`: The stable, unique behavior identifier used for dispatch and configuration lookup.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-appbehaviorattribute-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the stable behavior identifier.

Remarks: Alias for `Id` retained for source compatibility.

<a id="member-p-cephalon-abstractions-behaviors-appbehaviorattribute-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable behavior identifier.

<a id="type-cephalon-abstractions-behaviors-behavioradvisoryseverity"></a>

### `BehaviorAdvisorySeverity`

Severity levels for behavior advisories.

#### Declaration
```csharp
public enum BehaviorAdvisorySeverity
```

#### Fields

<a id="member-f-cephalon-abstractions-behaviors-behavioradvisoryseverity-critical"></a>

##### `Critical`

```csharp
const BehaviorAdvisorySeverity Critical
```

Critical — immediate attention recommended.

<a id="member-f-cephalon-abstractions-behaviors-behavioradvisoryseverity-info"></a>

##### `Info`

```csharp
const BehaviorAdvisorySeverity Info
```

Informational — no action required.

<a id="member-f-cephalon-abstractions-behaviors-behavioradvisoryseverity-warning"></a>

##### `Warning`

```csharp
const BehaviorAdvisorySeverity Warning
```

Warning — review recommended.

<a id="type-cephalon-abstractions-behaviors-behaviorallowedpatternsattribute"></a>

### `BehaviorAllowedPatternsAttribute`

Restricts which patterns may activate for this behavior and can also provide an attribute-only runtime baseline when exactly one pattern is declared.

Remarks: When a behavior has no explicit topology from `ConfigureTopology(...)`, fluent registration, or configuration overrides, a single declared pattern becomes the runtime baseline. Multiple declared patterns remain an allowlist and require another topology source to choose one.

#### Declaration
```csharp
public sealed class BehaviorAllowedPatternsAttribute
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviorallowedpatternsattribute-ctor-system-string"></a>

##### `BehaviorAllowedPatternsAttribute`

```csharp
BehaviorAllowedPatternsAttribute(string[] patterns)
```

Initializes a new instance of `BehaviorAllowedPatternsAttribute`.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviorallowedpatternsattribute-patterns"></a>

##### `Patterns`

```csharp
IReadOnlyList<string> Patterns { get; }
```

Gets the set of allowed pattern identifiers.

<a id="type-cephalon-abstractions-behaviors-behaviorallowedtransportsattribute"></a>

### `BehaviorAllowedTransportsAttribute`

Restricts which transports may activate for this behavior and can also provide an attribute-only runtime transport baseline when no explicit topology exists.

Remarks: Declared transports remain a transport allowlist for config and topology validation. When a behavior has no explicit topology, the declared transports become the runtime transport baseline. Public REST is module-owned and must not be declared through behavior transport allowlists. For author-facing allowlists, `http.grpc` is accepted and normalized to canonical `grpc`.

#### Declaration
```csharp
public sealed class BehaviorAllowedTransportsAttribute
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviorallowedtransportsattribute-ctor-system-string"></a>

##### `BehaviorAllowedTransportsAttribute`

```csharp
BehaviorAllowedTransportsAttribute(string[] transports)
```

Initializes a new instance of `BehaviorAllowedTransportsAttribute`.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviorallowedtransportsattribute-transports"></a>

##### `Transports`

```csharp
IReadOnlyList<string> Transports { get; }
```

Gets the set of allowed transport identifiers.

<a id="type-cephalon-abstractions-behaviors-behaviorapisurfacedescriptor"></a>

### `BehaviorApiSurfaceDescriptor`

Describes the logical public API surface projected by a behavior across transport adapters.

Remarks: This descriptor stays transport-agnostic. Route-shaped non-REST adapters such as JSON-RPC, GraphQL-over-SSE, GraphQL-over-WebSocket, Server-Sent Events, and WebSocket can project canonical routes from the same logical surface without forcing transport-specific path details into behavior identifiers. Public REST stays module-owned.

#### Declaration
```csharp
public sealed class BehaviorApiSurfaceDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviorapisurfacedescriptor-ctor-system-string-system-string"></a>

##### `BehaviorApiSurfaceDescriptor`

```csharp
BehaviorApiSurfaceDescriptor(string groupPath, string operationPath)
```

Initializes a new `BehaviorApiSurfaceDescriptor`.

Parameters:
- `groupPath`: The logical group path, such as `cart` or `orders/status`.
- `operationPath`: The logical operation path, such as `get` or `remove-item`.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviorapisurfacedescriptor-grouppath"></a>

##### `GroupPath`

```csharp
string GroupPath { get; }
```

Gets the logical group path shared by transport-specific projections.

<a id="member-p-cephalon-abstractions-behaviors-behaviorapisurfacedescriptor-operationpath"></a>

##### `OperationPath`

```csharp
string OperationPath { get; }
```

Gets the logical operation path shared by transport-specific projections.

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-behaviorapisurfacedescriptor-createdefault-system-string"></a>

##### `CreateDefault`

```csharp
BehaviorApiSurfaceDescriptor CreateDefault(string behaviorId)
```

Creates a default API surface descriptor from the supplied behavior identifier.

Remarks: Behavior identifiers such as `cart.get` become group `cart` plus operation `get`. Identifiers with more than two segments join all but the final segment into the group path.

Returns: The default logical API surface derived from the identifier.

Parameters:
- `behaviorId`: The stable behavior identifier.

<a id="type-cephalon-abstractions-behaviors-behaviorcompatibilityviolation"></a>

### `BehaviorCompatibilityViolation`

Represents a compatibility rule violation for a behavior topology.

#### Declaration
```csharp
public sealed class BehaviorCompatibilityViolation
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviorcompatibilityviolation-ctor-system-string-system-string-cephalon-abstractions-behaviors-compatibilityseverity-system-string"></a>

##### `BehaviorCompatibilityViolation`

```csharp
BehaviorCompatibilityViolation(string ruleId, string behaviorId, CompatibilitySeverity severity, string message)
```

Initializes a new instance of `BehaviorCompatibilityViolation`.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviorcompatibilityviolation-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the behavior identifier that triggered the violation.

<a id="member-p-cephalon-abstractions-behaviors-behaviorcompatibilityviolation-message"></a>

##### `Message`

```csharp
string Message { get; }
```

Gets the violation message.

<a id="member-p-cephalon-abstractions-behaviors-behaviorcompatibilityviolation-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the rule identifier that was violated.

<a id="member-p-cephalon-abstractions-behaviors-behaviorcompatibilityviolation-severity"></a>

##### `Severity`

```csharp
CompatibilitySeverity Severity { get; }
```

Gets the violation severity.

<a id="type-cephalon-abstractions-behaviors-behaviorfault"></a>

### `BehaviorFault`

Represents a structured fault from a behavior execution.

#### Declaration
```csharp
public sealed class BehaviorFault
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviorfault-ctor"></a>

##### `BehaviorFault`

```csharp
BehaviorFault()
```

Initializes a new instance of `BehaviorFault`.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviorfault-code"></a>

##### `Code`

```csharp
string Code { get; set; }
```

Gets or sets the fault code.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfault-details"></a>

##### `Details`

```csharp
string Details { get; set; }
```

Gets or sets additional fault details.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfault-innerfaults"></a>

##### `InnerFaults`

```csharp
IReadOnlyList<BehaviorFault> InnerFaults { get; set; }
```

Gets or sets nested faults.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfault-message"></a>

##### `Message`

```csharp
string Message { get; set; }
```

Gets or sets the fault message.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfault-severity"></a>

##### `Severity`

```csharp
BehaviorFaultSeverity Severity { get; set; }
```

Gets or sets the fault severity.

<a id="type-cephalon-abstractions-behaviors-behaviorfaultseverity"></a>

### `BehaviorFaultSeverity`

Severity levels for structured behavior faults.

#### Declaration
```csharp
public enum BehaviorFaultSeverity
```

#### Fields

<a id="member-f-cephalon-abstractions-behaviors-behaviorfaultseverity-critical"></a>

##### `Critical`

```csharp
const BehaviorFaultSeverity Critical
```

Critical fault details.

<a id="member-f-cephalon-abstractions-behaviors-behaviorfaultseverity-error"></a>

##### `Error`

```csharp
const BehaviorFaultSeverity Error
```

Error-level fault details.

<a id="member-f-cephalon-abstractions-behaviors-behaviorfaultseverity-info"></a>

##### `Info`

```csharp
const BehaviorFaultSeverity Info
```

Informational fault details.

<a id="member-f-cephalon-abstractions-behaviors-behaviorfaultseverity-warning"></a>

##### `Warning`

```csharp
const BehaviorFaultSeverity Warning
```

Warning-level fault details.

<a id="type-cephalon-abstractions-behaviors-behaviorfeaturedisabledexception"></a>

### `BehaviorFeatureDisabledException`

Thrown when a behavior declares one or more required feature flags and the active runtime evaluation context does not satisfy one of them.

#### Declaration
```csharp
public sealed class BehaviorFeatureDisabledException
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviorfeaturedisabledexception-ctor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-nullable-cephalon-abstractions-features-featureflagsourcekind-system-string"></a>

##### `BehaviorFeatureDisabledException`

```csharp
BehaviorFeatureDisabledException(string behaviorId, string featureFlagId, IReadOnlyList<string> requiredFeatureFlagIds, string reason, FeatureFlagSourceKind? sourceKind, string sourceModuleId)
```

Initializes a new instance of `BehaviorFeatureDisabledException`.

Parameters:
- `behaviorId`: The behavior identifier that was blocked.
- `featureFlagId`: The specific required feature flag that evaluated to disabled.
- `requiredFeatureFlagIds`: The full ordered set of required feature flags.
- `reason`: The evaluation reason returned by the feature-toggle runtime.
- `sourceKind`: The ownership kind of the resolved feature flag when one exists.
- `sourceModuleId`: The owning module identifier when the resolved feature flag is module-owned.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviorfeaturedisabledexception-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the behavior identifier that was blocked.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfeaturedisabledexception-featureflagid"></a>

##### `FeatureFlagId`

```csharp
string FeatureFlagId { get; }
```

Gets the required feature flag that evaluated to disabled for the active runtime context.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfeaturedisabledexception-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the operator-facing evaluation reason that explained why the feature flag was not available for the current runtime context.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfeaturedisabledexception-requiredfeatureflagids"></a>

##### `RequiredFeatureFlagIds`

```csharp
IReadOnlyList<string> RequiredFeatureFlagIds { get; }
```

Gets the full ordered set of required feature flags declared by the behavior.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfeaturedisabledexception-sourcekind"></a>

##### `SourceKind`

```csharp
FeatureFlagSourceKind? SourceKind { get; }
```

Gets the ownership kind of the resolved feature flag when one exists.

<a id="member-p-cephalon-abstractions-behaviors-behaviorfeaturedisabledexception-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the owning module identifier when the resolved feature flag is module-owned.

<a id="type-cephalon-abstractions-behaviors-behavioridempotencyattribute"></a>

### `BehaviorIdempotencyAttribute`

Declares whether a behavior execution is safe to replay automatically.

Remarks: Cephalon uses this behavior-authored contract when resilience features need to decide whether transient failures should stay fail-fast only or can later participate in automatic retry.

#### Declaration
```csharp
public sealed class BehaviorIdempotencyAttribute
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behavioridempotencyattribute-ctor"></a>

##### `BehaviorIdempotencyAttribute`

```csharp
BehaviorIdempotencyAttribute()
```

Initializes a new instance of the `BehaviorIdempotencyAttribute` class and marks the behavior as idempotent.

<a id="member-m-cephalon-abstractions-behaviors-behavioridempotencyattribute-ctor-cephalon-abstractions-behaviors-behavioridempotencymode"></a>

##### `BehaviorIdempotencyAttribute`

```csharp
BehaviorIdempotencyAttribute(BehaviorIdempotencyMode mode)
```

Initializes a new instance of the `BehaviorIdempotencyAttribute` class.

Parameters:
- `mode`: The declared idempotency mode.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behavioridempotencyattribute-mode"></a>

##### `Mode`

```csharp
BehaviorIdempotencyMode Mode { get; }
```

Gets the declared idempotency mode.

<a id="type-cephalon-abstractions-behaviors-behavioridempotencymode"></a>

### `BehaviorIdempotencyMode`

Describes whether a behavior execution is safe to replay automatically.

#### Declaration
```csharp
public enum BehaviorIdempotencyMode
```

#### Fields

<a id="member-f-cephalon-abstractions-behaviors-behavioridempotencymode-idempotent"></a>

##### `Idempotent`

```csharp
const BehaviorIdempotencyMode Idempotent
```

Replaying the same logical behavior execution is expected to be safe.

<a id="member-f-cephalon-abstractions-behaviors-behavioridempotencymode-nonidempotent"></a>

##### `NonIdempotent`

```csharp
const BehaviorIdempotencyMode NonIdempotent
```

Replaying the same logical behavior execution is not expected to be safe.

<a id="member-f-cephalon-abstractions-behaviors-behavioridempotencymode-unknown"></a>

##### `Unknown`

```csharp
const BehaviorIdempotencyMode Unknown
```

No explicit idempotency contract was declared for the behavior.

<a id="type-cephalon-abstractions-behaviors-behaviornotfoundexception"></a>

### `BehaviorNotFoundException`

Thrown when a behavior cannot be located in the active runtime's behavior catalog.

#### Declaration
```csharp
public sealed class BehaviorNotFoundException
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviornotfoundexception-ctor-system-string"></a>

##### `BehaviorNotFoundException`

```csharp
BehaviorNotFoundException(string behaviorId)
```

Initializes the exception for the given behavior identifier.

Parameters:
- `behaviorId`: The behavior identifier that could not be resolved.

<a id="member-m-cephalon-abstractions-behaviors-behaviornotfoundexception-ctor-system-string-system-exception"></a>

##### `BehaviorNotFoundException`

```csharp
BehaviorNotFoundException(string behaviorId, Exception innerException)
```

Initializes the exception for the given behavior identifier with an inner exception.

Parameters:
- `behaviorId`: The behavior identifier that could not be resolved.
- `innerException`: The exception that caused this exception.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviornotfoundexception-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the behavior identifier that could not be resolved.

<a id="type-cephalon-abstractions-behaviors-behaviorresult"></a>

### `BehaviorResult`

Provides legacy factory helpers for creating transport-neutral behavior results.

Remarks: Prefer `Result` for new authoring code when the shorter name is a better fit. This type remains available as a compatibility alias.

#### Declaration
```csharp
public static class BehaviorResult
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-accepted-1-0-system-string-system-string"></a>

##### `Accepted`

```csharp
BehaviorResult<T> Accepted<T>(T value, string message, string code)
```

Creates an accepted result with an optional payload value.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-conflict-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Conflict`

```csharp
BehaviorResultDescriptor Conflict(string code, string message, BehaviorFault fault)
```

Creates a conflict result.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-conflict-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Conflict`

```csharp
BehaviorResult<T> Conflict<T>(string code, string message, BehaviorFault fault)
```

Creates a conflict result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-created-1-0-system-string-system-string"></a>

##### `Created`

```csharp
BehaviorResult<T> Created<T>(T value, string message, string code)
```

Creates a created result with a payload value.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-forbidden-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Forbidden`

```csharp
BehaviorResultDescriptor Forbidden(string code, string message, BehaviorFault fault)
```

Creates a forbidden result.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-forbidden-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Forbidden`

```csharp
BehaviorResult<T> Forbidden<T>(string code, string message, BehaviorFault fault)
```

Creates a forbidden result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-invalid-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Invalid`

```csharp
BehaviorResultDescriptor Invalid(string code, string message, BehaviorFault fault)
```

Creates an invalid-request result.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-invalid-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Invalid`

```csharp
BehaviorResult<T> Invalid<T>(string code, string message, BehaviorFault fault)
```

Creates an invalid-request result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-nocontent-system-string-system-string"></a>

##### `NoContent`

```csharp
BehaviorResultDescriptor NoContent(string message, string code)
```

Creates a no-content result.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-nocontent-1-system-string-system-string"></a>

##### `NoContent`

```csharp
BehaviorResult<T> NoContent<T>(string message, string code)
```

Creates a no-content result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-notfound-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `NotFound`

```csharp
BehaviorResultDescriptor NotFound(string code, string message, BehaviorFault fault)
```

Creates a not-found result.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-notfound-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `NotFound`

```csharp
BehaviorResult<T> NotFound<T>(string code, string message, BehaviorFault fault)
```

Creates a not-found result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-ok-1-0-system-string-system-string"></a>

##### `Ok`

```csharp
BehaviorResult<T> Ok<T>(T value, string message, string code)
```

Creates a successful result with a payload value.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-unauthorized-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Unauthorized`

```csharp
BehaviorResultDescriptor Unauthorized(string code, string message, BehaviorFault fault)
```

Creates an unauthorized result.

<a id="member-m-cephalon-abstractions-behaviors-behaviorresult-unauthorized-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Unauthorized`

```csharp
BehaviorResult<T> Unauthorized<T>(string code, string message, BehaviorFault fault)
```

Creates an unauthorized result for the specified payload type.

<a id="type-cephalon-abstractions-behaviors-behaviorresultdescriptor"></a>

### `BehaviorResultDescriptor`

Represents a transport-neutral behavior outcome descriptor that does not carry a payload value.

#### Declaration
```csharp
public struct BehaviorResultDescriptor
```

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviorresultdescriptor-code"></a>

##### `Code`

```csharp
string Code { get; }
```

Gets the stable outcome code when one was supplied.

<a id="member-p-cephalon-abstractions-behaviors-behaviorresultdescriptor-fault"></a>

##### `Fault`

```csharp
BehaviorFault Fault { get; }
```

Gets the structured fault details when the outcome is not successful.

<a id="member-p-cephalon-abstractions-behaviors-behaviorresultdescriptor-message"></a>

##### `Message`

```csharp
string Message { get; }
```

Gets the human-readable outcome message.

<a id="member-p-cephalon-abstractions-behaviors-behaviorresultdescriptor-status"></a>

##### `Status`

```csharp
BehaviorResultStatus Status { get; }
```

Gets the transport-neutral outcome status.

<a id="type-cephalon-abstractions-behaviors-behaviorresultstatus"></a>

### `BehaviorResultStatus`

Represents a transport-neutral behavior outcome.

#### Declaration
```csharp
public enum BehaviorResultStatus
```

#### Fields

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-accepted"></a>

##### `Accepted`

```csharp
const BehaviorResultStatus Accepted
```

The behavior accepted the request for asynchronous work.

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-conflict"></a>

##### `Conflict`

```csharp
const BehaviorResultStatus Conflict
```

The request conflicts with the current state of the target resource.

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-created"></a>

##### `Created`

```csharp
const BehaviorResultStatus Created
```

The behavior created a new resource or record.

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-forbidden"></a>

##### `Forbidden`

```csharp
const BehaviorResultStatus Forbidden
```

The caller is authenticated but not allowed to perform the requested action.

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-invalid"></a>

##### `Invalid`

```csharp
const BehaviorResultStatus Invalid
```

The request was invalid for the target behavior.

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-nocontent"></a>

##### `NoContent`

```csharp
const BehaviorResultStatus NoContent
```

The behavior completed successfully without a response payload.

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-notfound"></a>

##### `NotFound`

```csharp
const BehaviorResultStatus NotFound
```

The requested resource or target was not found.

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-ok"></a>

##### `Ok`

```csharp
const BehaviorResultStatus Ok
```

The behavior completed successfully and returned a value.

<a id="member-f-cephalon-abstractions-behaviors-behaviorresultstatus-unauthorized"></a>

##### `Unauthorized`

```csharp
const BehaviorResultStatus Unauthorized
```

The caller is not authenticated for the requested behavior.

<a id="type-cephalon-abstractions-behaviors-behaviorresult-t"></a>

### `BehaviorResult<T>`

Represents a legacy transport-neutral behavior outcome with an optional payload value.

Remarks: Prefer `Result<T>` for new authoring code when the shorter name is a better fit. This type remains available as a compatibility alias.

#### Declaration
```csharp
public sealed class BehaviorResult<T>
```

<a id="type-cephalon-abstractions-behaviors-behaviorsecurityexception"></a>

### `BehaviorSecurityException`

Thrown when a behavior's resolved topology violates an allowlist constraint declared via `BehaviorAllowedPatternsAttribute` or `BehaviorAllowedTransportsAttribute`.

#### Declaration
```csharp
public sealed class BehaviorSecurityException
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviorsecurityexception-ctor-system-string-system-string"></a>

##### `BehaviorSecurityException`

```csharp
BehaviorSecurityException(string behaviorId, string message)
```

Initializes the exception with the behavior identifier and a descriptive message.

Parameters:
- `behaviorId`: The behavior identifier that triggered the violation.
- `message`: A human-readable description of the security violation.

<a id="member-m-cephalon-abstractions-behaviors-behaviorsecurityexception-ctor-system-string-system-string-system-exception"></a>

##### `BehaviorSecurityException`

```csharp
BehaviorSecurityException(string behaviorId, string message, Exception innerException)
```

Initializes the exception with the behavior identifier, a descriptive message, and an inner exception.

Parameters:
- `behaviorId`: The behavior identifier that triggered the violation.
- `message`: A human-readable description of the security violation.
- `innerException`: The exception that caused this exception.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviorsecurityexception-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the behavior identifier that triggered the security violation.

<a id="type-cephalon-abstractions-behaviors-behaviortopologydescriptor"></a>

### `BehaviorTopologyDescriptor`

Describes the resolved topology for a single behavior, including its pattern, transports, feature flags, and shared logical API surface.

#### Declaration
```csharp
public sealed class BehaviorTopologyDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviortopologydescriptor-ctor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-boolean-system-boolean-system-boolean-cephalon-abstractions-behaviors-behaviorapisurfacedescriptor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `BehaviorTopologyDescriptor`

```csharp
BehaviorTopologyDescriptor(string id, string pattern, IReadOnlyList<string> transportIds, bool inboxEnabled, bool outboxEnabled, bool eventSourcingEnabled, BehaviorApiSurfaceDescriptor apiSurface, string displayName, string description, IReadOnlyList<string> requiredFeatureFlagIds, string sourceModuleId, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of `BehaviorTopologyDescriptor`.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-apisurface"></a>

##### `ApiSurface`

```csharp
BehaviorApiSurfaceDescriptor ApiSurface { get; }
```

Gets the logical public API surface projected by route-shaped transport adapters.

Remarks: When no explicit API surface is supplied, the descriptor derives one from the behavior identifier so route-shaped transports can project canonical paths without hard-coding the behavior id into every transport binding.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the optional description.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional display name.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-eventsourcingenabled"></a>

##### `EventSourcingEnabled`

```csharp
bool EventSourcingEnabled { get; }
```

Gets a value indicating whether event sourcing is wired into the behavior context.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the behavior identifier.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-inboxenabled"></a>

##### `InboxEnabled`

```csharp
bool InboxEnabled { get; }
```

Gets a value indicating whether inbox deduplication is enabled.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional metadata.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-outboxenabled"></a>

##### `OutboxEnabled`

```csharp
bool OutboxEnabled { get; }
```

Gets a value indicating whether outbox staging is enabled.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-pattern"></a>

##### `Pattern`

```csharp
string Pattern { get; }
```

Gets the pattern identifier (e.g. "cqrs", "event-driven", "saga-step", "saga-choreography", "process-manager", "durable-execution", "direct").

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-requiredfeatureflagids"></a>

##### `RequiredFeatureFlagIds`

```csharp
IReadOnlyList<string> RequiredFeatureFlagIds { get; }
```

Gets the ordered feature-flag identifiers that must resolve to enabled before the behavior can execute.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module identifier that owns this behavior when ownership is known at runtime.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologydescriptor-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; }
```

Gets the transport identifiers configured for this behavior.

<a id="type-cephalon-abstractions-behaviors-behaviortopologyoptions"></a>

### `BehaviorTopologyOptions`

Optional feature flags for a behavior topology entry.

#### Declaration
```csharp
public sealed class BehaviorTopologyOptions
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviortopologyoptions-ctor"></a>

##### `BehaviorTopologyOptions`

```csharp
BehaviorTopologyOptions()
```

Initializes a new instance of `BehaviorTopologyOptions`.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologyoptions-eventsourcingenabled"></a>

##### `EventSourcingEnabled`

```csharp
bool EventSourcingEnabled { get; set; }
```

Gets or sets a value indicating whether event sourcing is wired into the behavior context.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologyoptions-inboxenabled"></a>

##### `InboxEnabled`

```csharp
bool InboxEnabled { get; set; }
```

Gets or sets a value indicating whether inbox deduplication is enabled.

<a id="member-p-cephalon-abstractions-behaviors-behaviortopologyoptions-outboxenabled"></a>

##### `OutboxEnabled`

```csharp
bool OutboxEnabled { get; set; }
```

Gets or sets a value indicating whether outbox staging is enabled.

<a id="type-cephalon-abstractions-behaviors-compatibilityseverity"></a>

### `CompatibilitySeverity`

Severity level of a behavior compatibility rule violation.

#### Declaration
```csharp
public enum CompatibilitySeverity
```

#### Fields

<a id="member-f-cephalon-abstractions-behaviors-compatibilityseverity-advisory"></a>

##### `Advisory`

```csharp
const CompatibilitySeverity Advisory
```

The violation is informational only.

<a id="member-f-cephalon-abstractions-behaviors-compatibilityseverity-error"></a>

##### `Error`

```csharp
const CompatibilitySeverity Error
```

The violation prevents application startup.

<a id="member-f-cephalon-abstractions-behaviors-compatibilityseverity-warning"></a>

##### `Warning`

```csharp
const CompatibilitySeverity Warning
```

The violation may cause runtime issues but does not prevent startup.

<a id="type-cephalon-abstractions-behaviors-containsbehaviorsattribute"></a>

### `ContainsBehaviorsAttribute`

Assembly-level marker indicating that the assembly contains auto-discovered behavior types. When present, the engine uses the source-generated registration class instead of runtime reflection scanning, resulting in zero-reflection startup.

Remarks: This attribute is automatically emitted by the `Cephalon.Behaviors.SourceGen` source generator when it discovers one or more `[AppBehavior]` classes in the assembly. You do not need to add it manually.

The `RegistrationType` property points to the generated class that provides compile-time registration and topology descriptors, enabling the engine to skip the expensive `DefinedTypes` / `GetCustomAttribute` reflection scan.

#### Declaration
```csharp
public sealed class ContainsBehaviorsAttribute
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-containsbehaviorsattribute-ctor-system-type"></a>

##### `ContainsBehaviorsAttribute`

```csharp
ContainsBehaviorsAttribute(Type registrationType)
```

Initializes a new instance of `ContainsBehaviorsAttribute` pointing to the generated registration class.

Parameters:
- `registrationType`: The generated type that contains static `Register` and `GetTopologyDescriptors` methods.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-containsbehaviorsattribute-registrationtype"></a>

##### `RegistrationType`

```csharp
Type RegistrationType { get; }
```

Gets the generated registration class type emitted by the source generator.

<a id="type-cephalon-abstractions-behaviors-iappbehavior-tin-tout"></a>

### `IAppBehavior<TIn, TOut>`

Single interface for all behavior patterns. Developers implement this once; pattern and transport are config-driven.

#### Declaration
```csharp
public interface IAppBehavior<TIn, TOut>
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-iappbehavior-2-configuretopology-cephalon-abstractions-behaviors-ibehaviortopologybuilder"></a>

##### `ConfigureTopology`

```csharp
void ConfigureTopology(IBehaviorTopologyBuilder builder)
```

Optional author-intent topology declaration. Called by source generator at build time. Override to declare pattern/transport defaults in code.

<a id="member-m-cephalon-abstractions-behaviors-iappbehavior-2-handleasync-0-cephalon-abstractions-behaviors-ibehaviorcontext-system-threading-cancellationtoken"></a>

##### `HandleAsync`

```csharp
Task<TOut> HandleAsync(TIn input, IBehaviorContext context, CancellationToken ct)
```

Handles the behavior input and returns the output.

<a id="type-cephalon-abstractions-behaviors-ibehavioradvisory"></a>

### `IBehaviorAdvisory`

Represents a runtime advisory that describes a recommendation or observation about behavior topology. Advisories are informational — they do not block dispatch.

#### Declaration
```csharp
public interface IBehaviorAdvisory
```

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-ibehavioradvisory-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the behavior identifier this advisory applies to, or `null` if global.

<a id="member-p-cephalon-abstractions-behaviors-ibehavioradvisory-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the advisory description.

<a id="member-p-cephalon-abstractions-behaviors-ibehavioradvisory-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the display name shown in runtime surfaces.

<a id="member-p-cephalon-abstractions-behaviors-ibehavioradvisory-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable advisory identifier.

<a id="member-p-cephalon-abstractions-behaviors-ibehavioradvisory-severity"></a>

##### `Severity`

```csharp
BehaviorAdvisorySeverity Severity { get; }
```

Gets the severity of this advisory.

<a id="type-cephalon-abstractions-behaviors-ibehavioradvisorycatalog"></a>

### `IBehaviorAdvisoryCatalog`

Provides read access to all active behavior advisories.

#### Declaration
```csharp
public interface IBehaviorAdvisoryCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-ibehavioradvisorycatalog-all"></a>

##### `All`

```csharp
IReadOnlyList<IBehaviorAdvisory> All { get; }
```

Gets all active advisories.

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehavioradvisorycatalog-getbybehavior-system-string"></a>

##### `GetByBehavior`

```csharp
IReadOnlyList<IBehaviorAdvisory> GetByBehavior(string behaviorId)
```

Gets advisories for a specific behavior identifier.

<a id="member-m-cephalon-abstractions-behaviors-ibehavioradvisorycatalog-getbyseverity-cephalon-abstractions-behaviors-behavioradvisoryseverity"></a>

##### `GetBySeverity`

```csharp
IReadOnlyList<IBehaviorAdvisory> GetBySeverity(BehaviorAdvisorySeverity minimumSeverity)
```

Gets advisories at or above the specified severity.

<a id="type-cephalon-abstractions-behaviors-ibehavioradvisorycontributor"></a>

### `IBehaviorAdvisoryContributor`

Contributes behavior advisories to the active runtime's advisory catalog. Implementations are collected via dependency injection enumeration.

#### Declaration
```csharp
public interface IBehaviorAdvisoryContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehavioradvisorycontributor-contribute"></a>

##### `Contribute`

```csharp
IReadOnlyList<IBehaviorAdvisory> Contribute()
```

Contributes advisories for the current runtime state.

Returns: The advisories contributed by this instance.

<a id="type-cephalon-abstractions-behaviors-ibehaviorcatalog"></a>

### `IBehaviorCatalog`

Provides read access to all registered behavior topology descriptors.

#### Declaration
```csharp
public interface IBehaviorCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorcatalog-all"></a>

##### `All`

```csharp
IReadOnlyList<BehaviorTopologyDescriptor> All { get; }
```

Gets all registered behavior topology descriptors.

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehaviorcatalog-findbyid-system-string"></a>

##### `FindById`

```csharp
BehaviorTopologyDescriptor FindById(string behaviorId)
```

Finds a behavior by identifier (case-insensitive), or returns `null` if not found.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviorcatalog-getbypattern-system-string"></a>

##### `GetByPattern`

```csharp
IReadOnlyList<BehaviorTopologyDescriptor> GetByPattern(string pattern)
```

Gets all behaviors registered with the specified pattern.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviorcatalog-getbytransport-system-string"></a>

##### `GetByTransport`

```csharp
IReadOnlyList<BehaviorTopologyDescriptor> GetByTransport(string transportId)
```

Gets all behaviors registered with the specified transport.

<a id="type-cephalon-abstractions-behaviors-ibehaviorcompatibilityrule"></a>

### `IBehaviorCompatibilityRule`

Validates a behavior topology descriptor against a compatibility constraint.

#### Declaration
```csharp
public interface IBehaviorCompatibilityRule
```

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorcompatibilityrule-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets a human-readable description of the rule.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorcompatibilityrule-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the unique rule identifier (e.g. "ABT-001").

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehaviorcompatibilityrule-check-cephalon-abstractions-behaviors-behaviortopologydescriptor"></a>

##### `Check`

```csharp
BehaviorCompatibilityViolation Check(BehaviorTopologyDescriptor descriptor)
```

Checks the descriptor and returns a violation if the rule is violated, or `null` if valid.

<a id="type-cephalon-abstractions-behaviors-ibehaviorcontext"></a>

### `IBehaviorContext`

Provides ambient context to a behavior during its execution. The context exposes reply semantics, metadata, and cancellation.

Remarks: When the behavior topology uses the `direct` pattern, `ReplyAsync` is not supported and will throw `NotSupportedException`. Use the behavior's return value to communicate results in the direct pattern.

#### Declaration
```csharp
public interface IBehaviorContext
```

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorcontext-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the stable identifier of the behavior being executed.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorcontext-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier for the current execution, or `null` if not provided.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorcontext-eventstore"></a>

##### `EventStore`

```csharp
IEventStore EventStore { get; }
```

Gets the event store for the current behavior context, or `null` if event sourcing is not configured for this behavior.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorcontext-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets ambient metadata associated with the current execution (e.g. correlation id, tenant id).

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehaviorcontext-replyasync-system-object-system-threading-cancellationtoken"></a>

##### `ReplyAsync`

```csharp
Task ReplyAsync(object reply, CancellationToken cancellationToken)
```

Sends a reply message back to the caller through the active transport.

Returns: A task that completes when the reply has been dispatched.

Parameters:
- `reply`: The reply object to send.
- `cancellationToken`: A token that cancels the reply.

<a id="type-cephalon-abstractions-behaviors-ibehaviorcontributor"></a>

### `IBehaviorContributor`

Contributes behavior topology descriptors to the active runtime's catalog. Implementations are collected via dependency injection enumeration.

#### Declaration
```csharp
public interface IBehaviorContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehaviorcontributor-contribute"></a>

##### `Contribute`

```csharp
IReadOnlyList<BehaviorTopologyDescriptor> Contribute()
```

Returns the behavior topology descriptors contributed by this instance.

Returns: The contributed descriptors.

<a id="type-cephalon-abstractions-behaviors-ibehaviormodulebuilder"></a>

### `IBehaviorModuleBuilder`

Collects behavior ownership declarations contributed by a Cephalon module.

Remarks: This builder is host-agnostic and only declares which behaviors a module owns. Public REST exposure stays in host adapters such as ASP.NET Core.

#### Declaration
```csharp
public interface IBehaviorModuleBuilder
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehaviormodulebuilder-add-1"></a>

##### `Add`

```csharp
IBehaviorModuleBuilder Add<TBehavior>()
```

Declares that the current module owns the specified behavior.

Returns: The same builder for fluent ownership registration.

Type parameters:
- `TBehavior`: The concrete behavior type owned by the module.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviormodulebuilder-add-1-system-action-cephalon-abstractions-behaviors-ibehaviortopologybuilder"></a>

##### `Add`

```csharp
IBehaviorModuleBuilder Add<TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
```

Declares that the current module owns the specified behavior and supplies an explicit topology override.

Returns: The same builder for fluent ownership registration.

Type parameters:
- `TBehavior`: The concrete behavior type owned by the module.

Parameters:
- `configureTopology`: The callback that selects the resolved behavior topology when attribute-only synthesis is not enough.

<a id="type-cephalon-abstractions-behaviors-ibehaviorownermodule"></a>

### `IBehaviorOwnerModule`

Declares that a module explicitly owns one or more Cephalon behaviors.

Remarks: Modules can use this contract to keep behavior ownership deterministic without relying only on assembly scanning. A module may still choose to expose only some of its owned behaviors through a host adapter such as ASP.NET Core REST.

#### Declaration
```csharp
public interface IBehaviorOwnerModule
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehaviorownermodule-configurebehaviors-cephalon-abstractions-behaviors-ibehaviormodulebuilder"></a>

##### `ConfigureBehaviors`

```csharp
void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
```

Registers the behaviors owned by the current module.

Parameters:
- `behaviors`: The builder that collects module-owned behavior registrations.

<a id="type-cephalon-abstractions-behaviors-ibehaviorregistry"></a>

### `IBehaviorRegistry`

Receives behavior topology descriptors from contributors.

#### Declaration
```csharp
public interface IBehaviorRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehaviorregistry-add-cephalon-abstractions-behaviors-behaviortopologydescriptor"></a>

##### `Add`

```csharp
void Add(BehaviorTopologyDescriptor descriptor)
```

Adds a behavior topology descriptor to the registry.

<a id="type-cephalon-abstractions-behaviors-ibehaviorresult"></a>

### `IBehaviorResult`

Describes a structured behavior outcome that can be projected into transport-specific responses.

#### Declaration
```csharp
public interface IBehaviorResult
```

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorresult-code"></a>

##### `Code`

```csharp
string Code { get; }
```

Gets the stable outcome code when one was supplied.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorresult-fault"></a>

##### `Fault`

```csharp
BehaviorFault Fault { get; }
```

Gets the structured fault details when the outcome is not successful.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorresult-hasvalue"></a>

##### `HasValue`

```csharp
bool HasValue { get; }
```

Gets a value indicating whether the result carries a payload value.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorresult-issuccess"></a>

##### `IsSuccess`

```csharp
bool IsSuccess { get; }
```

Gets a value indicating whether the result represents a successful outcome.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorresult-message"></a>

##### `Message`

```csharp
string Message { get; }
```

Gets the human-readable outcome message.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorresult-status"></a>

##### `Status`

```csharp
BehaviorResultStatus Status { get; }
```

Gets the transport-neutral outcome status.

<a id="member-p-cephalon-abstractions-behaviors-ibehaviorresult-value"></a>

##### `Value`

```csharp
object Value { get; }
```

Gets the boxed payload value when one was supplied.

<a id="type-cephalon-abstractions-behaviors-ibehaviortopologybuilder"></a>

### `IBehaviorTopologyBuilder`

Fluent builder for declaring behavior topology: pattern, transports, and feature options.

#### Declaration
```csharp
public interface IBehaviorTopologyBuilder
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-ascqrs"></a>

##### `AsCqrs`

```csharp
IBehaviorTopologyBuilder AsCqrs()
```

Declares this behavior as CQRS-shaped (command/query split, 200/202 HTTP semantics).

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-asdirect"></a>

##### `AsDirect`

```csharp
IBehaviorTopologyBuilder AsDirect()
```

Declares this behavior as direct (no architectural pattern — input → handler → output, 200/204 HTTP).

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-asdurableexecution"></a>

##### `AsDurableExecution`

```csharp
IBehaviorTopologyBuilder AsDurableExecution()
```

Declares this behavior as a durable execution workflow with event-store replay semantics.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-aseventdriven"></a>

##### `AsEventDriven`

```csharp
IBehaviorTopologyBuilder AsEventDriven()
```

Declares this behavior as event-driven (fire-and-forget, 202 HTTP, fanout).

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-asprocessmanager"></a>

##### `AsProcessManager`

```csharp
IBehaviorTopologyBuilder AsProcessManager()
```

Declares this behavior as a process manager step (long-running, durable checkpoint).

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-assaga"></a>

##### `AsSaga`

```csharp
IBehaviorTopologyBuilder AsSaga()
```

Declares this behavior as a saga step (stateful, compensation chain).

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-assagachoreography"></a>

##### `AsSagaChoreography`

```csharp
IBehaviorTopologyBuilder AsSagaChoreography()
```

Declares this behavior as a choreography-based saga step (event-reaction coordination).

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-build-system-string"></a>

##### `Build`

```csharp
BehaviorTopologyDescriptor Build(string behaviorId)
```

Builds the final descriptor. Called internally by the engine — do not call directly.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-requirefeatureflag-system-string"></a>

##### `RequireFeatureFlag`

```csharp
IBehaviorTopologyBuilder RequireFeatureFlag(string featureFlagId)
```

Requires one Cephalon feature flag to be enabled before the behavior can execute.

Returns: The same builder for fluent chaining.

Parameters:
- `featureFlagId`: The feature-flag identifier that must resolve to enabled.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-requirefeatureflags-system-string"></a>

##### `RequireFeatureFlags`

```csharp
IBehaviorTopologyBuilder RequireFeatureFlags(string[] featureFlagIds)
```

Requires all requested Cephalon feature flags to be enabled before the behavior can execute.

Returns: The same builder for fluent chaining.

Parameters:
- `featureFlagIds`: The feature-flag identifiers that must resolve to enabled.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viagrpc"></a>

##### `ViaGrpc`

```csharp
IBehaviorTopologyBuilder ViaGrpc()
```

Adds the gRPC transport.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viahttpgraphql"></a>

##### `ViaHttpGraphQl`

```csharp
IBehaviorTopologyBuilder ViaHttpGraphQl()
```

Adds the GraphQL over HTTP transport (queries and mutations).

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viahttpgraphqlsse"></a>

##### `ViaHttpGraphQlSse`

```csharp
IBehaviorTopologyBuilder ViaHttpGraphQlSse()
```

Adds the GraphQL subscriptions via Server-Sent Events transport.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viahttpgraphqlws"></a>

##### `ViaHttpGraphQlWs`

```csharp
IBehaviorTopologyBuilder ViaHttpGraphQlWs()
```

Adds the GraphQL subscriptions via WebSocket transport.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viahttpjsonrpc"></a>

##### `ViaHttpJsonRpc`

```csharp
IBehaviorTopologyBuilder ViaHttpJsonRpc()
```

Adds the JSON-RPC 2.0 over HTTP transport.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viahttpsse"></a>

##### `ViaHttpSse`

```csharp
IBehaviorTopologyBuilder ViaHttpSse()
```

Adds the raw Server-Sent Events push transport.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viainmemory"></a>

##### `ViaInMemory`

```csharp
IBehaviorTopologyBuilder ViaInMemory()
```

Adds the in-memory transport (zero-infra, for tests and local dev).

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viakafka"></a>

##### `ViaKafka`

```csharp
IBehaviorTopologyBuilder ViaKafka()
```

Adds the Kafka transport.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viarabbitmq"></a>

##### `ViaRabbitMq`

```csharp
IBehaviorTopologyBuilder ViaRabbitMq()
```

Adds the RabbitMQ (AMQP) transport.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viawebsocket"></a>

##### `ViaWebSocket`

```csharp
IBehaviorTopologyBuilder ViaWebSocket()
```

Adds the raw WebSocket bi-directional transport.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-withapisurface-system-string-system-string"></a>

##### `WithApiSurface`

```csharp
IBehaviorTopologyBuilder WithApiSurface(string groupPath, string operationPath)
```

Overrides the logical API surface projected by route-shaped transport adapters.

Remarks: This primarily affects the shared generic behavior HTTP transport surface used by JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, Server-Sent Events, and WebSocket bindings. Public REST endpoints are module-owned and should be mapped through `RestBehaviorModuleBase.ConfigureRestBehaviors(...)`, with `MapAdditionalEndpoints(...)` plus `MapBehaviorRestGroup(...)` reserved for the advanced manual-route escape hatch, instead of behavior topology.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-withmetadata-system-string-system-string"></a>

##### `WithMetadata`

```csharp
IBehaviorTopologyBuilder WithMetadata(string key, string value)
```

Adds or replaces arbitrary topology metadata for companion packs that need extra routing or runtime hints.

Returns: The same builder for fluent chaining.

Parameters:
- `key`: The stable metadata key.
- `value`: The metadata value. Pass `null` to remove the key from the topology descriptor.

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-withoptions-system-action-cephalon-abstractions-behaviors-behaviortopologyoptions"></a>

##### `WithOptions`

```csharp
IBehaviorTopologyBuilder WithOptions(Action<BehaviorTopologyOptions> configure)
```

Configures optional feature flags for this behavior (outbox, inbox, event sourcing).

<a id="type-cephalon-abstractions-behaviors-iprocesscompletion"></a>

### `IProcessCompletion`

Marker interface that signals a process manager behavior has reached its final step.

#### Declaration
```csharp
public interface IProcessCompletion
```

<a id="type-cephalon-abstractions-behaviors-ownedbehaviorregistration"></a>

### `OwnedBehaviorRegistration`

Describes one explicit module-owned behavior registration collected during engine composition.

#### Declaration
```csharp
public sealed class OwnedBehaviorRegistration
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-ownedbehaviorregistration-ctor-system-string-system-string-system-type-system-action-cephalon-abstractions-behaviors-ibehaviortopologybuilder"></a>

##### `OwnedBehaviorRegistration`

```csharp
OwnedBehaviorRegistration(string sourceModuleId, string behaviorId, Type behaviorType, Action<IBehaviorTopologyBuilder> configureTopology)
```

Initializes a new `OwnedBehaviorRegistration`.

Parameters:
- `sourceModuleId`: The stable module identifier that owns the behavior.
- `behaviorId`: The stable behavior identifier.
- `behaviorType`: The concrete behavior implementation type.
- `configureTopology`: An optional topology callback used when the owning module needs to select an explicit behavior topology.

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-ownedbehaviorregistration-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the stable behavior identifier.

<a id="member-p-cephalon-abstractions-behaviors-ownedbehaviorregistration-behaviortype"></a>

##### `BehaviorType`

```csharp
Type BehaviorType { get; }
```

Gets the concrete behavior implementation type.

<a id="member-p-cephalon-abstractions-behaviors-ownedbehaviorregistration-configuretopology"></a>

##### `ConfigureTopology`

```csharp
Action<IBehaviorTopologyBuilder> ConfigureTopology { get; }
```

Gets the optional topology callback supplied by the owning module.

<a id="member-p-cephalon-abstractions-behaviors-ownedbehaviorregistration-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the stable module identifier that owns the behavior.

<a id="type-cephalon-abstractions-behaviors-result"></a>

### `Result`

Provides concise factory helpers for creating transport-neutral behavior results.

Remarks: Prefer this type for new behavior authoring code when the longer `BehaviorResult` naming does not add clarity.

#### Declaration
```csharp
public static class Result
```

#### Methods

<a id="member-m-cephalon-abstractions-behaviors-result-accepted-1-0-system-string-system-string"></a>

##### `Accepted`

```csharp
Result<T> Accepted<T>(T value, string message, string code)
```

Creates an accepted result with a payload value.

<a id="member-m-cephalon-abstractions-behaviors-result-conflict-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Conflict`

```csharp
BehaviorResultDescriptor Conflict(string code, string message, BehaviorFault fault)
```

Creates a conflict result.

<a id="member-m-cephalon-abstractions-behaviors-result-conflict-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Conflict`

```csharp
Result<T> Conflict<T>(string code, string message, BehaviorFault fault)
```

Creates a conflict result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-result-created-1-0-system-string-system-string"></a>

##### `Created`

```csharp
Result<T> Created<T>(T value, string message, string code)
```

Creates a created result with a payload value.

<a id="member-m-cephalon-abstractions-behaviors-result-forbidden-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Forbidden`

```csharp
BehaviorResultDescriptor Forbidden(string code, string message, BehaviorFault fault)
```

Creates a forbidden result.

<a id="member-m-cephalon-abstractions-behaviors-result-forbidden-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Forbidden`

```csharp
Result<T> Forbidden<T>(string code, string message, BehaviorFault fault)
```

Creates a forbidden result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-result-invalid-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Invalid`

```csharp
BehaviorResultDescriptor Invalid(string code, string message, BehaviorFault fault)
```

Creates an invalid-request result.

<a id="member-m-cephalon-abstractions-behaviors-result-invalid-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Invalid`

```csharp
Result<T> Invalid<T>(string code, string message, BehaviorFault fault)
```

Creates an invalid-request result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-result-nocontent-system-string-system-string"></a>

##### `NoContent`

```csharp
BehaviorResultDescriptor NoContent(string message, string code)
```

Creates a no-content result.

<a id="member-m-cephalon-abstractions-behaviors-result-nocontent-1-system-string-system-string"></a>

##### `NoContent`

```csharp
Result<T> NoContent<T>(string message, string code)
```

Creates a no-content result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-result-notfound-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `NotFound`

```csharp
BehaviorResultDescriptor NotFound(string code, string message, BehaviorFault fault)
```

Creates a not-found result.

<a id="member-m-cephalon-abstractions-behaviors-result-notfound-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `NotFound`

```csharp
Result<T> NotFound<T>(string code, string message, BehaviorFault fault)
```

Creates a not-found result for the specified payload type.

<a id="member-m-cephalon-abstractions-behaviors-result-ok-1-0-system-string-system-string"></a>

##### `Ok`

```csharp
Result<T> Ok<T>(T value, string message, string code)
```

Creates a successful result with a payload value.

<a id="member-m-cephalon-abstractions-behaviors-result-unauthorized-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Unauthorized`

```csharp
BehaviorResultDescriptor Unauthorized(string code, string message, BehaviorFault fault)
```

Creates an unauthorized result.

<a id="member-m-cephalon-abstractions-behaviors-result-unauthorized-1-system-string-system-string-cephalon-abstractions-behaviors-behaviorfault"></a>

##### `Unauthorized`

```csharp
Result<T> Unauthorized<T>(string code, string message, BehaviorFault fault)
```

Creates an unauthorized result for the specified payload type.

<a id="type-cephalon-abstractions-behaviors-result-t"></a>

### `Result<T>`

Represents a concise transport-neutral behavior outcome with an optional payload value.

#### Declaration
```csharp
public class Result<T>
```

#### Properties

<a id="member-p-cephalon-abstractions-behaviors-result-1-code"></a>

##### `Code`

```csharp
string Code { get; }
```

Gets the stable outcome code when one was supplied.

<a id="member-p-cephalon-abstractions-behaviors-result-1-fault"></a>

##### `Fault`

```csharp
BehaviorFault Fault { get; }
```

Gets the structured fault details when the outcome is not successful.

<a id="member-p-cephalon-abstractions-behaviors-result-1-hasvalue"></a>

##### `HasValue`

```csharp
bool HasValue { get; }
```

Gets a value indicating whether the result carries a payload value.

<a id="member-p-cephalon-abstractions-behaviors-result-1-issuccess"></a>

##### `IsSuccess`

```csharp
bool IsSuccess { get; }
```

Gets a value indicating whether the result represents a successful outcome.

<a id="member-p-cephalon-abstractions-behaviors-result-1-message"></a>

##### `Message`

```csharp
string Message { get; }
```

Gets the human-readable outcome message.

<a id="member-p-cephalon-abstractions-behaviors-result-1-status"></a>

##### `Status`

```csharp
BehaviorResultStatus Status { get; }
```

Gets the transport-neutral outcome status.

<a id="member-p-cephalon-abstractions-behaviors-result-1-value"></a>

##### `Value`

```csharp
T Value { get; }
```

Gets the typed payload value when one was supplied.

<a id="namespace-cephalon-abstractions-capabilities"></a>

## Namespace Cephalon.Abstractions.Capabilities

<a id="type-cephalon-abstractions-capabilities-capability"></a>

### `Capability`

Describes a capability contributed by a module or package.

#### Declaration
```csharp
public sealed class Capability
```

#### Constructors

<a id="member-m-cephalon-abstractions-capabilities-capability-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Capability`

```csharp
Capability(string key, string displayName, string description, IReadOnlyDictionary<string, string> metadata)
```

Creates a capability descriptor.

Parameters:
- `key`: The stable capability key.
- `displayName`: The human-readable capability name.
- `description`: The capability description.
- `metadata`: Optional capability metadata.

#### Properties

<a id="member-p-cephalon-abstractions-capabilities-capability-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the capability description.

<a id="member-p-cephalon-abstractions-capabilities-capability-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable capability name.

<a id="member-p-cephalon-abstractions-capabilities-capability-key"></a>

##### `Key`

```csharp
string Key { get; }
```

Gets the stable capability key.

<a id="member-p-cephalon-abstractions-capabilities-capability-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional capability metadata.

<a id="type-cephalon-abstractions-capabilities-capabilityaccess"></a>

### `CapabilityAccess`

Describes how a capability may be consumed under the active trust policy.

#### Declaration
```csharp
public enum CapabilityAccess
```

#### Fields

<a id="member-f-cephalon-abstractions-capabilities-capabilityaccess-allowed"></a>

##### `Allowed`

```csharp
const CapabilityAccess Allowed
```

Indicates the capability can be used without additional trust requirements.

<a id="member-f-cephalon-abstractions-capabilities-capabilityaccess-denied"></a>

##### `Denied`

```csharp
const CapabilityAccess Denied
```

Indicates the capability is denied.

<a id="member-f-cephalon-abstractions-capabilities-capabilityaccess-trustedonly"></a>

##### `TrustedOnly`

```csharp
const CapabilityAccess TrustedOnly
```

Indicates the capability can be used only by trusted modules or packages.

<a id="type-cephalon-abstractions-capabilities-icapabilityregistry"></a>

### `ICapabilityRegistry`

Registers capabilities exposed by modules and packages.

#### Declaration
```csharp
public interface ICapabilityRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-capabilities-icapabilityregistry-add-cephalon-abstractions-capabilities-capability"></a>

##### `Add`

```csharp
void Add(Capability capability)
```

Adds a capability to the registry.

Parameters:
- `capability`: The capability to register.

<a id="namespace-cephalon-abstractions-data"></a>

## Namespace Cephalon.Abstractions.Data

<a id="type-cephalon-abstractions-data-cdccapturedescriptor"></a>

### `CdcCaptureDescriptor`

Describes one change-data-capture surface contributed to the active runtime.

#### Declaration
```csharp
public sealed class CdcCaptureDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccapturedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureDescriptor`

```csharp
CdcCaptureDescriptor(string id, string displayName, string description, string sourceModuleId, string provider, string sourceId, string outboxId, string mode, string eventFormat, IReadOnlyList<string> resourceIds, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new CDC capture descriptor.

Parameters:
- `id`: The stable CDC capture identifier.
- `displayName`: The operator-facing CDC capture name.
- `description`: The human-readable CDC capture description.
- `sourceModuleId`: The module identifier that owns the CDC capture.
- `provider`: The logical provider identifier that supplies the change feed.
- `sourceId`: The logical source stream, database, or feed identifier.
- `outboxId`: The outbox identifier that receives captured publications.
- `mode`: The capture mode such as `wal`, `change-stream`, or `table-tail`.
- `eventFormat`: The emitted change-event format such as `debezium-envelope`.
- `resourceIds`: Optional resource identifiers such as tables, collections, or topics observed by the capture.
- `tags`: Optional descriptive tags associated with the CDC capture.
- `metadata`: Optional operator-facing metadata associated with the CDC capture.

<a id="member-m-cephalon-abstractions-data-cdccapturedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureDescriptor`

```csharp
CdcCaptureDescriptor(string id, string displayName, string description, string sourceModuleId, string provider, string sourceId, string outboxId, CdcCaptureExecutionBindingDescriptor executionBinding, string mode, string eventFormat, IReadOnlyList<string> resourceIds, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new CDC capture descriptor.

Parameters:
- `id`: The stable CDC capture identifier.
- `displayName`: The operator-facing CDC capture name.
- `description`: The human-readable CDC capture description.
- `sourceModuleId`: The module identifier that owns the CDC capture.
- `provider`: The logical provider identifier that supplies the change feed.
- `sourceId`: The logical source stream, database, or feed identifier.
- `outboxId`: The outbox identifier that receives captured publications.
- `executionBinding`: The authored or effective execution-binding answer for the CDC capture. When omitted, the capture starts unbound.
- `mode`: The capture mode such as `wal`, `change-stream`, or `table-tail`.
- `eventFormat`: The emitted change-event format such as `debezium-envelope`.
- `resourceIds`: Optional resource identifiers such as tables, collections, or topics observed by the capture.
- `tags`: Optional descriptive tags associated with the CDC capture.
- `metadata`: Optional operator-facing metadata associated with the CDC capture.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable CDC capture description.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing CDC capture name.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-eventformat"></a>

##### `EventFormat`

```csharp
string EventFormat { get; }
```

Gets the emitted change-event format.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-executionbinding"></a>

##### `ExecutionBinding`

```csharp
CdcCaptureExecutionBindingDescriptor ExecutionBinding { get; set; }
```

Gets the authored or effective execution-binding answer for the CDC capture.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable CDC capture identifier.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the CDC capture.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-mode"></a>

##### `Mode`

```csharp
string Mode { get; }
```

Gets the capture mode.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; }
```

Gets the outbox identifier that receives captured publications.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the logical provider identifier that supplies the change feed.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-resourceids"></a>

##### `ResourceIds`

```csharp
IReadOnlyList<string> ResourceIds { get; }
```

Gets the resource identifiers observed by the capture.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-sourceid"></a>

##### `SourceId`

```csharp
string SourceId { get; }
```

Gets the logical source stream, database, or feed identifier.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that owns the CDC capture.

<a id="member-p-cephalon-abstractions-data-cdccapturedescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the CDC capture.

#### Methods

<a id="member-m-cephalon-abstractions-data-cdccapturedescriptor-withexecutionbinding-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor"></a>

##### `WithExecutionBinding`

```csharp
CdcCaptureDescriptor WithExecutionBinding(CdcCaptureExecutionBindingDescriptor executionBinding)
```

Creates a copy of the CDC capture descriptor with a different execution-binding answer.

Returns: A new CDC capture descriptor with the requested execution binding.

Parameters:
- `executionBinding`: The execution-binding answer to apply.

<a id="type-cephalon-abstractions-data-cdccaptureexecutionacknowledgement"></a>

### `CdcCaptureExecutionAcknowledgement`

Describes one CDC batch that the shared runtime has already staged through the linked outbox and is now safe to acknowledge durably.

#### Declaration
```csharp
public sealed class CdcCaptureExecutionAcknowledgement
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-ctor-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-data-outboxmessage-system-nullable-system-int32-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureExecutionAcknowledgement`

```csharp
CdcCaptureExecutionAcknowledgement(string cdcCaptureId, string outboxId, IReadOnlyList<OutboxMessage> messages, int? capturedChangeCount, string changeId, string checkpoint, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of the `CdcCaptureExecutionAcknowledgement` class.

Parameters:
- `cdcCaptureId`: The stable CDC capture identifier that owns the staged batch.
- `outboxId`: The stable outbox identifier that already accepted the staged publications.
- `messages`: The outbox publications that the shared runtime staged successfully.
- `capturedChangeCount`: The number of source changes observed by the staged batch. When omitted, Cephalon uses the staged-message count as the default captured-change answer.
- `changeId`: The latest provider-facing change identifier when one is available.
- `checkpoint`: The latest provider-facing checkpoint or cursor when one is available.
- `metadata`: Optional operator-facing metadata captured alongside the staged batch.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-capturedchangecount"></a>

##### `CapturedChangeCount`

```csharp
int CapturedChangeCount { get; }
```

Gets the number of source changes observed by the staged batch.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-cdccaptureid"></a>

##### `CdcCaptureId`

```csharp
string CdcCaptureId { get; }
```

Gets the stable CDC capture identifier that owns the staged batch.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-changeid"></a>

##### `ChangeId`

```csharp
string ChangeId { get; }
```

Gets the latest provider-facing change identifier when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-checkpoint"></a>

##### `Checkpoint`

```csharp
string Checkpoint { get; }
```

Gets the latest provider-facing checkpoint or cursor when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-messages"></a>

##### `Messages`

```csharp
IReadOnlyList<OutboxMessage> Messages { get; }
```

Gets the outbox publications that the shared runtime staged successfully.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata captured alongside the staged batch.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; }
```

Gets the stable outbox identifier that already accepted the staged publications.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-stagedmessagecount"></a>

##### `StagedMessageCount`

```csharp
int StagedMessageCount { get; }
```

Gets the number of publications that the shared runtime staged successfully.

<a id="type-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor"></a>

### `CdcCaptureExecutionBindingDescriptor`

Describes how a CDC capture binds to an operator-facing execution runtime.

#### Declaration
```csharp
public sealed class CdcCaptureExecutionBindingDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureExecutionBindingDescriptor`

```csharp
CdcCaptureExecutionBindingDescriptor(string cdcCaptureId, string authoredExecutionRuntimeId, string requestedExecutionRuntimeId, string effectiveExecutionRuntimeId, string executionOwnership, string resolutionMode, IReadOnlyDictionary<string, string> metadata)
```

Creates a new CDC capture execution binding descriptor.

Parameters:
- `cdcCaptureId`: The stable CDC capture identifier.
- `authoredExecutionRuntimeId`: The execution-runtime identifier authored directly on the CDC capture when one was declared.
- `requestedExecutionRuntimeId`: The execution-runtime identifier requested for the CDC capture after any additive overrides are applied.
- `effectiveExecutionRuntimeId`: The execution-runtime identifier that currently owns execution for the CDC capture.
- `executionOwnership`: The operator-facing ownership mode for the effective execution runtime, such as `host-managed` or `external-managed`.
- `resolutionMode`: The operator-facing reason that explains how the effective execution-runtime binding was selected.
- `metadata`: Optional operator-facing metadata for the resolved binding.

<a id="member-m-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureExecutionBindingDescriptor`

```csharp
CdcCaptureExecutionBindingDescriptor(string cdcCaptureId, string authoredExecutionRuntimeId, string requestedExecutionRuntimeId, string effectiveExecutionRuntimeId, string executionOwnership, string executionTopology, string resolutionMode, IReadOnlyDictionary<string, string> metadata)
```

Creates a new CDC capture execution binding descriptor with a first-class topology classification.

Parameters:
- `cdcCaptureId`: The stable CDC capture identifier.
- `authoredExecutionRuntimeId`: The execution-runtime identifier authored directly on the CDC capture when one was declared.
- `requestedExecutionRuntimeId`: The execution-runtime identifier requested for the CDC capture after any additive overrides are applied.
- `effectiveExecutionRuntimeId`: The execution-runtime identifier that currently owns execution for the CDC capture.
- `executionOwnership`: The operator-facing ownership mode for the effective execution runtime, such as `host-managed` or `external-managed`.
- `executionTopology`: The operator-facing topology classification for the effective execution runtime, such as `shared-in-process-polling` or `provider-native`.
- `resolutionMode`: The operator-facing reason that explains how the effective execution-runtime binding was selected.
- `metadata`: Optional operator-facing metadata for the resolved binding.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-authoredexecutionruntimeid"></a>

##### `AuthoredExecutionRuntimeId`

```csharp
string AuthoredExecutionRuntimeId { get; }
```

Gets the execution-runtime identifier authored directly on the CDC capture when one was declared.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-cdccaptureid"></a>

##### `CdcCaptureId`

```csharp
string CdcCaptureId { get; }
```

Gets the stable CDC capture identifier.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-effectiveexecutionruntimeid"></a>

##### `EffectiveExecutionRuntimeId`

```csharp
string EffectiveExecutionRuntimeId { get; }
```

Gets the execution-runtime identifier that currently owns execution for the CDC capture.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-executionownership"></a>

##### `ExecutionOwnership`

```csharp
string ExecutionOwnership { get; }
```

Gets the operator-facing ownership mode for the effective execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-executiontopology"></a>

##### `ExecutionTopology`

```csharp
string ExecutionTopology { get; }
```

Gets the operator-facing topology classification for the effective execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-isbound"></a>

##### `IsBound`

```csharp
bool IsBound { get; }
```

Gets a value indicating whether the CDC capture currently resolves to an active execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata for the resolved binding.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-requestedexecutionruntimeid"></a>

##### `RequestedExecutionRuntimeId`

```csharp
string RequestedExecutionRuntimeId { get; }
```

Gets the execution-runtime identifier requested for the CDC capture after additive overrides are applied.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-resolutionmode"></a>

##### `ResolutionMode`

```csharp
string ResolutionMode { get; }
```

Gets the operator-facing explanation for how the effective execution-runtime binding was selected.

#### Methods

<a id="member-m-cephalon-abstractions-data-cdccaptureexecutionbindingdescriptor-unbound-system-string"></a>

##### `Unbound`

```csharp
CdcCaptureExecutionBindingDescriptor Unbound(string cdcCaptureId)
```

Creates the default unbound execution-binding descriptor for the requested CDC capture.

Returns: An unbound execution-binding descriptor.

Parameters:
- `cdcCaptureId`: The CDC capture identifier to bind.

<a id="type-cephalon-abstractions-data-cdccaptureexecutionresult"></a>

### `CdcCaptureExecutionResult`

Describes one bounded CDC capture batch and the outbox publications it produced.

#### Declaration
```csharp
public sealed class CdcCaptureExecutionResult
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccaptureexecutionresult-ctor-system-collections-generic-ireadonlylist-cephalon-abstractions-data-outboxmessage-system-nullable-system-int32-system-string-system-string-cephalon-abstractions-data-cdccapturefreshnessstatus-cephalon-abstractions-data-cdccapturelagstatus-cephalon-abstractions-data-cdccapturepublicationstatus-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureExecutionResult`

```csharp
CdcCaptureExecutionResult(IReadOnlyList<OutboxMessage> messages, int? capturedChangeCount, string changeId, string checkpoint, CdcCaptureFreshnessStatus freshness, CdcCaptureLagStatus lag, CdcCapturePublicationStatus publication, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of the `CdcCaptureExecutionResult` class.

Parameters:
- `messages`: The outbox publications produced by the capture batch.
- `capturedChangeCount`: The number of source changes observed by the capture batch. When omitted, Cephalon uses the produced-message count as the default captured-change answer for the batch.
- `changeId`: The latest provider-facing change identifier when one is available.
- `checkpoint`: The latest provider-facing checkpoint or cursor when one is available.
- `freshness`: The optional typed freshness answer reported by the capture implementation.
- `lag`: The optional typed lag answer reported by the capture implementation.
- `publication`: The optional typed publication-posture answer reported by the capture implementation before any linked outbox-dispatch overlay is applied.
- `metadata`: Optional operator-facing metadata captured alongside the batch.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-capturedchangecount"></a>

##### `CapturedChangeCount`

```csharp
int CapturedChangeCount { get; }
```

Gets the number of source changes observed by the capture batch.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-changeid"></a>

##### `ChangeId`

```csharp
string ChangeId { get; }
```

Gets the latest provider-facing change identifier when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-checkpoint"></a>

##### `Checkpoint`

```csharp
string Checkpoint { get; }
```

Gets the latest provider-facing checkpoint or cursor when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-freshness"></a>

##### `Freshness`

```csharp
CdcCaptureFreshnessStatus Freshness { get; }
```

Gets the typed freshness answer reported by the capture implementation when one was supplied.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-lag"></a>

##### `Lag`

```csharp
CdcCaptureLagStatus Lag { get; }
```

Gets the typed lag answer reported by the capture implementation when one was supplied.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-messages"></a>

##### `Messages`

```csharp
IReadOnlyList<OutboxMessage> Messages { get; }
```

Gets the outbox publications produced by the capture batch.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata captured alongside the batch.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-producedmessagecount"></a>

##### `ProducedMessageCount`

```csharp
int ProducedMessageCount { get; }
```

Gets the number of outbox publications produced by the capture batch.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionresult-publication"></a>

##### `Publication`

```csharp
CdcCapturePublicationStatus Publication { get; }
```

Gets the typed publication-posture answer reported by the capture implementation when one was supplied.

<a id="type-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor"></a>

### `CdcCaptureExecutionRuntimeDescriptor`

Describes one operator-facing CDC capture execution runtime visible to the current Cephalon runtime.

#### Declaration
```csharp
public sealed class CdcCaptureExecutionRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlylist-system-string-cephalon-abstractions-data-cdccaptureexecutionruntimesummary"></a>

##### `CdcCaptureExecutionRuntimeDescriptor`

```csharp
CdcCaptureExecutionRuntimeDescriptor(string id, string displayName, string description, IReadOnlyDictionary<string, string> metadata, IReadOnlyList<string> cdcCaptureIds, CdcCaptureExecutionRuntimeSummary summary)
```

Creates a new CDC capture execution runtime descriptor.

Parameters:
- `id`: The stable execution-runtime identifier.
- `displayName`: The operator-facing execution-runtime name.
- `description`: The human-readable execution-runtime description.
- `metadata`: Optional operator-facing metadata for the execution runtime.
- `cdcCaptureIds`: Optional CDC capture identifiers explicitly owned by the execution runtime when ownership is bounded to a known capture set.
- `summary`: Optional aggregate runtime summary describing the latest reported operator-facing state for the execution runtime.

<a id="member-m-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlylist-system-string-cephalon-abstractions-data-cdccaptureexecutionruntimesummary"></a>

##### `CdcCaptureExecutionRuntimeDescriptor`

```csharp
CdcCaptureExecutionRuntimeDescriptor(string id, string displayName, string description, string executionOwnership, string executionTopology, string acknowledgementMode, string hostedExecutionId, string executionGraphId, IReadOnlyDictionary<string, string> metadata, IReadOnlyList<string> cdcCaptureIds, CdcCaptureExecutionRuntimeSummary summary)
```

Creates a new CDC capture execution runtime descriptor with first-class ownership and topology semantics.

Parameters:
- `id`: The stable execution-runtime identifier.
- `displayName`: The operator-facing execution-runtime name.
- `description`: The human-readable execution-runtime description.
- `executionOwnership`: The operator-facing execution-ownership mode, such as `host-managed` or `external-managed`.
- `executionTopology`: The operator-facing execution-topology classification, such as `shared-in-process-polling` or `provider-native`.
- `acknowledgementMode`: The operator-facing acknowledgement mode when the runtime reports one, such as `post-stage-provider`.
- `hostedExecutionId`: The stable hosted-execution identifier when the runtime is backed by a Cephalon hosted execution.
- `executionGraphId`: The stable execution-graph identifier when the runtime is backed by a Cephalon execution graph.
- `metadata`: Optional operator-facing metadata for the execution runtime.
- `cdcCaptureIds`: Optional CDC capture identifiers explicitly owned by the execution runtime when ownership is bounded to a known capture set.
- `summary`: Optional aggregate runtime summary describing the latest reported operator-facing state for the execution runtime.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-acknowledgementmode"></a>

##### `AcknowledgementMode`

```csharp
string AcknowledgementMode { get; }
```

Gets the operator-facing acknowledgement mode for the runtime when one was declared.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-cdccaptureids"></a>

##### `CdcCaptureIds`

```csharp
IReadOnlyList<string> CdcCaptureIds { get; }
```

Gets the CDC capture identifiers explicitly owned by the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable execution-runtime description.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing execution-runtime name.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-executiongraphid"></a>

##### `ExecutionGraphId`

```csharp
string ExecutionGraphId { get; }
```

Gets the linked execution-graph identifier when the runtime is backed by a Cephalon execution graph.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-executionownership"></a>

##### `ExecutionOwnership`

```csharp
string ExecutionOwnership { get; }
```

Gets the operator-facing execution-ownership mode for the runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-executiontopology"></a>

##### `ExecutionTopology`

```csharp
string ExecutionTopology { get; }
```

Gets the operator-facing execution-topology classification for the runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-hostedexecutionid"></a>

##### `HostedExecutionId`

```csharp
string HostedExecutionId { get; }
```

Gets the linked hosted-execution identifier when the runtime is backed by a Cephalon hosted execution.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable execution-runtime identifier.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor-summary"></a>

##### `Summary`

```csharp
CdcCaptureExecutionRuntimeSummary Summary { get; }
```

Gets the latest aggregate runtime summary reported for the execution runtime.

<a id="type-cephalon-abstractions-data-cdccaptureexecutionruntimesummary"></a>

### `CdcCaptureExecutionRuntimeSummary`

Describes the latest aggregate operator-facing runtime summary for one CDC capture execution runtime.

#### Declaration
```csharp
public sealed class CdcCaptureExecutionRuntimeSummary
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-ctor-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-string-system-int32-system-int32-system-int32-system-int32-system-int64-system-int64-system-string-system-string"></a>

##### `CdcCaptureExecutionRuntimeSummary`

```csharp
CdcCaptureExecutionRuntimeSummary(IReadOnlyList<string> ReportedCdcCaptureIds, string LastCdcCaptureId, string LastOutcome, DateTimeOffset? LastObservedAtUtc, string LastChangeId, string LastCheckpoint, int StartedCount, int CapturedCount, int IdleCount, int FailedCount, long TotalCapturedChangeCount, long TotalProducedMessageCount, string LastAcknowledgement, string LastError)
```

Describes the latest aggregate operator-facing runtime summary for one CDC capture execution runtime.

Parameters:
- `ReportedCdcCaptureIds`: The CDC capture identifiers that have reported runtime state for the execution runtime.
- `LastCdcCaptureId`: The CDC capture identifier that produced the latest runtime observation.
- `LastOutcome`: The latest reported capture outcome visible for the execution runtime.
- `LastObservedAtUtc`: The UTC timestamp when the latest runtime observation was reported.
- `LastChangeId`: The latest provider-facing change identifier visible for the execution runtime.
- `LastCheckpoint`: The latest provider-facing checkpoint visible for the execution runtime.
- `StartedCount`: The total number of `started` observations visible for the execution runtime.
- `CapturedCount`: The total number of `captured` observations visible for the execution runtime.
- `IdleCount`: The total number of `idle` observations visible for the execution runtime.
- `FailedCount`: The total number of `failed` observations visible for the execution runtime.
- `TotalCapturedChangeCount`: The total number of source changes reported for the execution runtime.
- `TotalProducedMessageCount`: The total number of produced outbox messages reported for the execution runtime.
- `LastAcknowledgement`: The latest acknowledgement posture reported for the execution runtime when one is known.
- `LastError`: The latest operator-facing error summary visible for the execution runtime.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-capturedcount"></a>

##### `CapturedCount`

```csharp
int CapturedCount { get; set; }
```

The total number of `captured` observations visible for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-empty"></a>

##### `Empty`

```csharp
CdcCaptureExecutionRuntimeSummary Empty { get; }
```

Gets an empty summary for execution runtimes that have not reported state yet.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; set; }
```

The total number of `failed` observations visible for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-hasreports"></a>

##### `HasReports`

```csharp
bool HasReports { get; }
```

Gets a value indicating whether the execution runtime has reported any runtime observations yet.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-idlecount"></a>

##### `IdleCount`

```csharp
int IdleCount { get; set; }
```

The total number of `idle` observations visible for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-lastacknowledgement"></a>

##### `LastAcknowledgement`

```csharp
string LastAcknowledgement { get; set; }
```

The latest acknowledgement posture reported for the execution runtime when one is known.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-lastcdccaptureid"></a>

##### `LastCdcCaptureId`

```csharp
string LastCdcCaptureId { get; set; }
```

The CDC capture identifier that produced the latest runtime observation.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-lastchangeid"></a>

##### `LastChangeId`

```csharp
string LastChangeId { get; set; }
```

The latest provider-facing change identifier visible for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-lastcheckpoint"></a>

##### `LastCheckpoint`

```csharp
string LastCheckpoint { get; set; }
```

The latest provider-facing checkpoint visible for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; set; }
```

The latest operator-facing error summary visible for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the latest runtime observation was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; set; }
```

The latest reported capture outcome visible for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-reportedcapturecount"></a>

##### `ReportedCaptureCount`

```csharp
int ReportedCaptureCount { get; }
```

Gets the number of distinct CDC captures that have reported runtime state for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-reportedcdccaptureids"></a>

##### `ReportedCdcCaptureIds`

```csharp
IReadOnlyList<string> ReportedCdcCaptureIds { get; set; }
```

The CDC capture identifiers that have reported runtime state for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-startedcount"></a>

##### `StartedCount`

```csharp
int StartedCount { get; set; }
```

The total number of `started` observations visible for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-totalcapturedchangecount"></a>

##### `TotalCapturedChangeCount`

```csharp
long TotalCapturedChangeCount { get; set; }
```

The total number of source changes reported for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-totalproducedmessagecount"></a>

##### `TotalProducedMessageCount`

```csharp
long TotalProducedMessageCount { get; set; }
```

The total number of produced outbox messages reported for the execution runtime.

<a id="member-p-cephalon-abstractions-data-cdccaptureexecutionruntimesummary-totalreports"></a>

##### `TotalReports`

```csharp
int TotalReports { get; }
```

Gets the total number of runtime observations visible for the execution runtime.

<a id="type-cephalon-abstractions-data-cdccapturefreshnessstates"></a>

### `CdcCaptureFreshnessStates`

Defines the recommended stable freshness-state identifiers for CDC runtime reporting.

#### Declaration
```csharp
public static class CdcCaptureFreshnessStates
```

#### Fields

<a id="member-f-cephalon-abstractions-data-cdccapturefreshnessstates-fresh"></a>

##### `Fresh`

```csharp
const string Fresh
```

Indicates that the provider reports the capture as fresh.

<a id="member-f-cephalon-abstractions-data-cdccapturefreshnessstates-stale"></a>

##### `Stale`

```csharp
const string Stale
```

Indicates that the provider reports the capture as stale.

<a id="member-f-cephalon-abstractions-data-cdccapturefreshnessstates-unknown"></a>

##### `Unknown`

```csharp
const string Unknown
```

Indicates that the active runtime does not yet have a freshness answer.

<a id="type-cephalon-abstractions-data-cdccapturefreshnessstatus"></a>

### `CdcCaptureFreshnessStatus`

Describes the provider-facing freshness posture currently visible for one CDC capture.

#### Declaration
```csharp
public sealed class CdcCaptureFreshnessStatus
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccapturefreshnessstatus-ctor-system-string-system-nullable-system-datetimeoffset-system-string"></a>

##### `CdcCaptureFreshnessStatus`

```csharp
CdcCaptureFreshnessStatus(string state, DateTimeOffset? freshUntilUtc, string description)
```

Creates a new CDC freshness status.

Parameters:
- `state`: The stable freshness-state identifier, such as `unknown`, `fresh`, or `stale`.
- `freshUntilUtc`: The UTC timestamp until which the active runtime expects the current observation to remain fresh when one is known.
- `description`: An optional operator-facing freshness summary.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccapturefreshnessstatus-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets an optional operator-facing freshness summary.

<a id="member-p-cephalon-abstractions-data-cdccapturefreshnessstatus-freshuntilutc"></a>

##### `FreshUntilUtc`

```csharp
DateTimeOffset? FreshUntilUtc { get; }
```

Gets the UTC timestamp until which the current capture observation remains fresh when one is known.

<a id="member-p-cephalon-abstractions-data-cdccapturefreshnessstatus-haswindow"></a>

##### `HasWindow`

```csharp
bool HasWindow { get; }
```

Gets a value indicating whether a freshness window is currently known.

<a id="member-p-cephalon-abstractions-data-cdccapturefreshnessstatus-state"></a>

##### `State`

```csharp
string State { get; }
```

Gets the stable freshness-state identifier.

<a id="type-cephalon-abstractions-data-cdccapturelagstates"></a>

### `CdcCaptureLagStates`

Defines the recommended stable lag-state identifiers for CDC runtime reporting.

#### Declaration
```csharp
public static class CdcCaptureLagStates
```

#### Fields

<a id="member-f-cephalon-abstractions-data-cdccapturelagstates-backfilling"></a>

##### `Backfilling`

```csharp
const string Backfilling
```

Indicates that the provider reports the capture as intentionally backfilling older changes.

<a id="member-f-cephalon-abstractions-data-cdccapturelagstates-current"></a>

##### `Current`

```csharp
const string Current
```

Indicates that the provider reports the capture as caught up.

<a id="member-f-cephalon-abstractions-data-cdccapturelagstates-lagging"></a>

##### `Lagging`

```csharp
const string Lagging
```

Indicates that the provider reports the capture as lagging behind the source stream.

<a id="member-f-cephalon-abstractions-data-cdccapturelagstates-unknown"></a>

##### `Unknown`

```csharp
const string Unknown
```

Indicates that the active runtime does not yet have a lag answer.

<a id="type-cephalon-abstractions-data-cdccapturelagstatus"></a>

### `CdcCaptureLagStatus`

Describes the provider-facing lag posture currently visible for one CDC capture.

#### Declaration
```csharp
public sealed class CdcCaptureLagStatus
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccapturelagstatus-ctor-system-string-system-nullable-system-int64-system-string"></a>

##### `CdcCaptureLagStatus`

```csharp
CdcCaptureLagStatus(string state, long? pendingChangeCount, string description)
```

Creates a new CDC lag status.

Parameters:
- `state`: The stable lag-state identifier, such as `unknown`, `current`, `lagging`, or `backfilling`.
- `pendingChangeCount`: The number of source-side changes still pending capture when the provider can report that answer.
- `description`: An optional operator-facing lag summary.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccapturelagstatus-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets an optional operator-facing lag summary.

<a id="member-p-cephalon-abstractions-data-cdccapturelagstatus-haspendingchanges"></a>

##### `HasPendingChanges`

```csharp
bool HasPendingChanges { get; }
```

Gets a value indicating whether the capture still has pending source-side changes.

<a id="member-p-cephalon-abstractions-data-cdccapturelagstatus-pendingchangecount"></a>

##### `PendingChangeCount`

```csharp
long? PendingChangeCount { get; }
```

Gets the number of source-side changes still pending capture when the provider reports that answer.

<a id="member-p-cephalon-abstractions-data-cdccapturelagstatus-state"></a>

##### `State`

```csharp
string State { get; }
```

Gets the stable lag-state identifier.

<a id="type-cephalon-abstractions-data-cdccapturepublicationstates"></a>

### `CdcCapturePublicationStates`

Defines the recommended stable publication-state identifiers for CDC runtime reporting.

#### Declaration
```csharp
public static class CdcCapturePublicationStates
```

#### Fields

<a id="member-f-cephalon-abstractions-data-cdccapturepublicationstates-capturefailed"></a>

##### `CaptureFailed`

```csharp
const string CaptureFailed
```

Indicates that the capture itself last reported a failure before publication completed.

<a id="member-f-cephalon-abstractions-data-cdccapturepublicationstates-current"></a>

##### `Current`

```csharp
const string Current
```

Indicates that the capture is current through the linked publication path.

<a id="member-f-cephalon-abstractions-data-cdccapturepublicationstates-dispatchfailed"></a>

##### `DispatchFailed`

```csharp
const string DispatchFailed
```

Indicates that the linked outbox dispatch runtime last reported a failure.

<a id="member-f-cephalon-abstractions-data-cdccapturepublicationstates-dispatching"></a>

##### `Dispatching`

```csharp
const string Dispatching
```

Indicates that the linked outbox dispatch runtime is actively dispatching publications.

<a id="member-f-cephalon-abstractions-data-cdccapturepublicationstates-dispatchretrypending"></a>

##### `DispatchRetryPending`

```csharp
const string DispatchRetryPending
```

Indicates that the linked outbox dispatch runtime has a retry pending.

<a id="member-f-cephalon-abstractions-data-cdccapturepublicationstates-pendingpublication"></a>

##### `PendingPublication`

```csharp
const string PendingPublication
```

Indicates that the capture still has pending publications to push into or through the outbox.

<a id="member-f-cephalon-abstractions-data-cdccapturepublicationstates-unknown"></a>

##### `Unknown`

```csharp
const string Unknown
```

Indicates that the active runtime does not yet have a publication answer.

<a id="type-cephalon-abstractions-data-cdccapturepublicationstatus"></a>

### `CdcCapturePublicationStatus`

Describes the publication posture currently visible for one CDC capture.

#### Declaration
```csharp
public sealed class CdcCapturePublicationStatus
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccapturepublicationstatus-ctor-system-string-system-nullable-system-int64-system-string"></a>

##### `CdcCapturePublicationStatus`

```csharp
CdcCapturePublicationStatus(string state, long? pendingPublicationCount, string description)
```

Creates a new CDC publication status.

Parameters:
- `state`: The stable publication-state identifier, such as `unknown`, `pending-publication`, or `dispatch-retry-pending`.
- `pendingPublicationCount`: The number of pending publications still waiting to flow through the linked outbox path when the provider can report that answer.
- `description`: An optional operator-facing publication summary.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccapturepublicationstatus-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets an optional operator-facing publication summary.

<a id="member-p-cephalon-abstractions-data-cdccapturepublicationstatus-haspendingpublications"></a>

##### `HasPendingPublications`

```csharp
bool HasPendingPublications { get; }
```

Gets a value indicating whether the capture still has pending publications.

<a id="member-p-cephalon-abstractions-data-cdccapturepublicationstatus-pendingpublicationcount"></a>

##### `PendingPublicationCount`

```csharp
long? PendingPublicationCount { get; }
```

Gets the number of pending publications still waiting to flow through the linked outbox path when one is known.

<a id="member-p-cephalon-abstractions-data-cdccapturepublicationstatus-state"></a>

##### `State`

```csharp
string State { get; }
```

Gets the stable publication-state identifier.

<a id="type-cephalon-abstractions-data-cdccaptureruntimeobservation"></a>

### `CdcCaptureRuntimeObservation`

Describes one operator-facing runtime observation reported for a CDC capture by an execution runtime.

#### Declaration
```csharp
public sealed class CdcCaptureRuntimeObservation
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccaptureruntimeobservation-ctor-system-string-system-string-system-datetimeoffset-system-int32-system-int32-system-string-system-string-system-string-cephalon-abstractions-data-cdccapturefreshnessstatus-cephalon-abstractions-data-cdccapturelagstatus-cephalon-abstractions-data-cdccapturepublicationstatus-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureRuntimeObservation`

```csharp
CdcCaptureRuntimeObservation(string cdcCaptureId, string outcome, DateTimeOffset observedAtUtc, int capturedChangeCount, int producedMessageCount, string changeId, string checkpoint, string error, CdcCaptureFreshnessStatus freshness, CdcCaptureLagStatus lag, CdcCapturePublicationStatus publication, IReadOnlyDictionary<string, string> metadata)
```

Creates a new CDC capture runtime observation.

Parameters:
- `cdcCaptureId`: The stable CDC capture identifier that produced the observation.
- `outcome`: The stable outcome identifier, such as `started`, `captured`, `idle`, or `failed`.
- `observedAtUtc`: The UTC timestamp when the observation occurred.
- `capturedChangeCount`: The number of source changes observed by this report.
- `producedMessageCount`: The number of outbox messages produced by this report.
- `changeId`: The latest provider-facing change identifier when available.
- `checkpoint`: The latest provider-facing checkpoint or cursor when available.
- `error`: The operator-facing error summary when the observation represents a failure.
- `freshness`: An optional typed freshness answer reported by the active provider/runtime.
- `lag`: An optional typed lag answer reported by the active provider/runtime.
- `publication`: An optional typed publication-posture answer reported by the active provider/runtime.
- `metadata`: Optional operator-facing metadata captured alongside the observation.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-capturedchangecount"></a>

##### `CapturedChangeCount`

```csharp
int CapturedChangeCount { get; }
```

Gets the number of source changes observed by this report.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-cdccaptureid"></a>

##### `CdcCaptureId`

```csharp
string CdcCaptureId { get; }
```

Gets the stable CDC capture identifier that produced the observation.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-changeid"></a>

##### `ChangeId`

```csharp
string ChangeId { get; }
```

Gets the latest provider-facing change identifier when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-checkpoint"></a>

##### `Checkpoint`

```csharp
string Checkpoint { get; }
```

Gets the latest provider-facing checkpoint or cursor when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-error"></a>

##### `Error`

```csharp
string Error { get; }
```

Gets the operator-facing error summary when the observation represents a failure.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-freshness"></a>

##### `Freshness`

```csharp
CdcCaptureFreshnessStatus Freshness { get; }
```

Gets the typed freshness answer reported by the active provider/runtime when one was supplied.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-lag"></a>

##### `Lag`

```csharp
CdcCaptureLagStatus Lag { get; }
```

Gets the typed lag answer reported by the active provider/runtime when one was supplied.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata captured alongside the observation.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; }
```

Gets the UTC timestamp when the observation occurred.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable outcome identifier for the observed CDC activity.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-producedmessagecount"></a>

##### `ProducedMessageCount`

```csharp
int ProducedMessageCount { get; }
```

Gets the number of outbox messages produced by this report.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimeobservation-publication"></a>

##### `Publication`

```csharp
CdcCapturePublicationStatus Publication { get; }
```

Gets the typed publication-posture answer reported by the active provider/runtime when one was supplied.

<a id="type-cephalon-abstractions-data-cdccaptureruntimestate"></a>

### `CdcCaptureRuntimeState`

Describes the latest operator-facing runtime state visible for one active CDC capture.

#### Declaration
```csharp
public sealed class CdcCaptureRuntimeState
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-cdccaptureruntimestate-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-nullable-system-datetimeoffset-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-int64-system-int64-system-string-system-string-system-string-cephalon-abstractions-data-cdccapturefreshnessstatus-cephalon-abstractions-data-cdccapturelagstatus-cephalon-abstractions-data-cdccapturepublicationstatus-cephalon-abstractions-data-eventdispatchruntimestate-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureRuntimeState`

```csharp
CdcCaptureRuntimeState(string CdcCaptureId, string SourceModuleId, string Provider, string SourceId, string OutboxId, string Mode, string EventFormat, IReadOnlyList<string> ResourceIds, string LastOutcome, DateTimeOffset? LastObservedAtUtc, int LastCapturedChangeCount, int LastProducedMessageCount, int StartedCount, int CapturedCount, int IdleCount, int FailedCount, long TotalCapturedChangeCount, long TotalProducedMessageCount, string LastChangeId, string LastCheckpoint, string LastError, CdcCaptureFreshnessStatus Freshness, CdcCaptureLagStatus Lag, CdcCapturePublicationStatus Publication, EventDispatchRuntimeState OutboxDispatchState, IReadOnlyDictionary<string, string> Metadata)
```

Describes the latest operator-facing runtime state visible for one active CDC capture.

Parameters:
- `CdcCaptureId`: The stable CDC capture identifier.
- `SourceModuleId`: The identifier of the module that owns the CDC capture.
- `Provider`: The logical provider identifier that supplies the change feed.
- `SourceId`: The logical source stream, database, or feed identifier.
- `OutboxId`: The outbox identifier that receives captured publications.
- `Mode`: The capture mode such as `wal`, `change-stream`, or `table-tail`.
- `EventFormat`: The emitted change-event format such as `debezium-envelope`.
- `ResourceIds`: The resource identifiers observed by the capture.
- `LastOutcome`: The latest reported capture outcome identifier when one exists.
- `LastObservedAtUtc`: The UTC timestamp when the latest capture observation was reported.
- `LastCapturedChangeCount`: The number of source changes observed in the latest report.
- `LastProducedMessageCount`: The number of outbox messages produced by the latest report.
- `StartedCount`: The number of `started` observations reported so far.
- `CapturedCount`: The number of `captured` observations reported so far.
- `IdleCount`: The number of `idle` observations reported so far.
- `FailedCount`: The number of `failed` observations reported so far.
- `TotalCapturedChangeCount`: The total number of source changes reported so far.
- `TotalProducedMessageCount`: The total number of outbox messages produced so far.
- `LastChangeId`: The latest provider-facing change identifier when one was reported.
- `LastCheckpoint`: The latest provider-facing checkpoint or cursor when one was reported.
- `LastError`: The latest operator-facing error summary when one was reported.
- `Freshness`: The latest provider-facing freshness answer reported for the capture.
- `Lag`: The latest provider-facing lag answer reported for the capture.
- `Publication`: The latest publication posture answer reported for the capture.
- `OutboxDispatchState`: The latest linked outbox dispatch state when the active runtime also reports publication posture for the capture's outbox.
- `Metadata`: The operator-facing metadata captured by the latest report.

#### Properties

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-capturedcount"></a>

##### `CapturedCount`

```csharp
int CapturedCount { get; set; }
```

The number of `captured` observations reported so far.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-cdccaptureid"></a>

##### `CdcCaptureId`

```csharp
string CdcCaptureId { get; set; }
```

The stable CDC capture identifier.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-eventformat"></a>

##### `EventFormat`

```csharp
string EventFormat { get; set; }
```

The emitted change-event format such as `debezium-envelope`.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-executionbinding"></a>

##### `ExecutionBinding`

```csharp
CdcCaptureExecutionBindingDescriptor ExecutionBinding { get; set; }
```

Gets the authored or effective execution-binding answer for the CDC capture.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; set; }
```

The number of `failed` observations reported so far.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-freshness"></a>

##### `Freshness`

```csharp
CdcCaptureFreshnessStatus Freshness { get; set; }
```

The latest provider-facing freshness answer reported for the capture.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-hasdispatchreports"></a>

##### `HasDispatchReports`

```csharp
bool HasDispatchReports { get; }
```

Gets a value indicating whether the linked outbox dispatch path has reported runtime state.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-hasfreshnesswindow"></a>

##### `HasFreshnessWindow`

```csharp
bool HasFreshnessWindow { get; }
```

Gets a value indicating whether the capture still has a provider-reported freshness window.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-haspendingchanges"></a>

##### `HasPendingChanges`

```csharp
bool HasPendingChanges { get; }
```

Gets a value indicating whether the capture still has provider-reported pending source changes.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-haspendingpublications"></a>

##### `HasPendingPublications`

```csharp
bool HasPendingPublications { get; }
```

Gets a value indicating whether the capture still has provider-reported pending publications.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-hasreports"></a>

##### `HasReports`

```csharp
bool HasReports { get; }
```

Gets a value indicating whether the capture has reported any runtime observations yet.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-idlecount"></a>

##### `IdleCount`

```csharp
int IdleCount { get; set; }
```

The number of `idle` observations reported so far.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-isfailed"></a>

##### `IsFailed`

```csharp
bool IsFailed { get; }
```

Gets a value indicating whether the latest reported capture posture is failed.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-lag"></a>

##### `Lag`

```csharp
CdcCaptureLagStatus Lag { get; set; }
```

The latest provider-facing lag answer reported for the capture.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-lastcapturedchangecount"></a>

##### `LastCapturedChangeCount`

```csharp
int LastCapturedChangeCount { get; set; }
```

The number of source changes observed in the latest report.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-lastchangeid"></a>

##### `LastChangeId`

```csharp
string LastChangeId { get; set; }
```

The latest provider-facing change identifier when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-lastcheckpoint"></a>

##### `LastCheckpoint`

```csharp
string LastCheckpoint { get; set; }
```

The latest provider-facing checkpoint or cursor when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; set; }
```

The latest operator-facing error summary when one was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the latest capture observation was reported.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; set; }
```

The latest reported capture outcome identifier when one exists.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-lastproducedmessagecount"></a>

##### `LastProducedMessageCount`

```csharp
int LastProducedMessageCount { get; set; }
```

The number of outbox messages produced by the latest report.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

The operator-facing metadata captured by the latest report.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-mode"></a>

##### `Mode`

```csharp
string Mode { get; set; }
```

The capture mode such as `wal`, `change-stream`, or `table-tail`.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-outboxdispatchstate"></a>

##### `OutboxDispatchState`

```csharp
EventDispatchRuntimeState OutboxDispatchState { get; set; }
```

The latest linked outbox dispatch state when the active runtime also reports publication posture for the capture's outbox.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; set; }
```

The outbox identifier that receives captured publications.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-provider"></a>

##### `Provider`

```csharp
string Provider { get; set; }
```

The logical provider identifier that supplies the change feed.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-publication"></a>

##### `Publication`

```csharp
CdcCapturePublicationStatus Publication { get; set; }
```

The latest publication posture answer reported for the capture.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-resourceids"></a>

##### `ResourceIds`

```csharp
IReadOnlyList<string> ResourceIds { get; set; }
```

The resource identifiers observed by the capture.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-sourceid"></a>

##### `SourceId`

```csharp
string SourceId { get; set; }
```

The logical source stream, database, or feed identifier.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

The identifier of the module that owns the CDC capture.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-startedcount"></a>

##### `StartedCount`

```csharp
int StartedCount { get; set; }
```

The number of `started` observations reported so far.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-totalcapturedchangecount"></a>

##### `TotalCapturedChangeCount`

```csharp
long TotalCapturedChangeCount { get; set; }
```

The total number of source changes reported so far.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-totalproducedmessagecount"></a>

##### `TotalProducedMessageCount`

```csharp
long TotalProducedMessageCount { get; set; }
```

The total number of outbox messages produced so far.

<a id="member-p-cephalon-abstractions-data-cdccaptureruntimestate-totalreports"></a>

##### `TotalReports`

```csharp
int TotalReports { get; }
```

Gets the total number of capture observations reported for the CDC capture.

<a id="type-cephalon-abstractions-data-databasemigrationcommanddescriptor"></a>

### `DatabaseMigrationCommandDescriptor`

Describes one operator-facing command template for executing a database-migration target.

#### Declaration
```csharp
public sealed class DatabaseMigrationCommandDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasemigrationcommanddescriptor-ctor-system-string-system-string-system-string-system-string-system-boolean-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-string"></a>

##### `DatabaseMigrationCommandDescriptor`

```csharp
DatabaseMigrationCommandDescriptor(string id, string displayName, string description, string commandTemplate, bool recommendedForProduction, IReadOnlyDictionary<string, string> metadata, string toolId, string executionCategory, string workingDirectoryHint)
```

Creates a new database-migration command descriptor.

Parameters:
- `id`: The stable command identifier such as `bundle`, `script`, or `update`.
- `displayName`: The operator-facing command name.
- `description`: The human-readable command description.
- `commandTemplate`: The command template that operators can adapt for their environment.
- `recommendedForProduction`: Whether this command is recommended for production use.
- `metadata`: Optional operator-facing metadata associated with the command.
- `toolId`: An optional stable tool identifier such as `dotnet-ef`.
- `executionCategory`: An optional execution category such as `deploy-time` or `manual`.
- `workingDirectoryHint`: An optional working-directory hint for where the command is typically run.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-commandtemplate"></a>

##### `CommandTemplate`

```csharp
string CommandTemplate { get; }
```

Gets the command template that operators can adapt for their environment.

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable command description.

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing command name.

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-executioncategory"></a>

##### `ExecutionCategory`

```csharp
string ExecutionCategory { get; }
```

Gets the execution category when the provider can distinguish deploy-time, manual, or other command paths.

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable command identifier.

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata associated with the command.

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-recommendedforproduction"></a>

##### `RecommendedForProduction`

```csharp
bool RecommendedForProduction { get; }
```

Gets a value indicating whether this command is recommended for production use.

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-toolid"></a>

##### `ToolId`

```csharp
string ToolId { get; }
```

Gets the stable operator tool identifier when the provider can name one.

<a id="member-p-cephalon-abstractions-data-databasemigrationcommanddescriptor-workingdirectoryhint"></a>

##### `WorkingDirectoryHint`

```csharp
string WorkingDirectoryHint { get; }
```

Gets the provider-published working-directory hint when one is known.

<a id="type-cephalon-abstractions-data-databasemigrationdescriptor"></a>

### `DatabaseMigrationDescriptor`

Describes one logical database-migration target visible to the active Cephalon runtime.

#### Declaration
```csharp
public sealed class DatabaseMigrationDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasemigrationdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-data-databasemigrationstatus-system-boolean-system-boolean-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-data-databasemigrationcommanddescriptor-system-collections-generic-ireadonlydictionary-system-string-system-string-system-nullable-system-int32-system-nullable-cephalon-abstractions-health-healthstate-system-string-system-string-system-string-system-nullable-system-datetimeoffset"></a>

##### `DatabaseMigrationDescriptor`

```csharp
DatabaseMigrationDescriptor(string id, string displayName, string description, string requestedRoleId, string resolvedRoleId, string executionMode, DatabaseMigrationStatus status, bool applyOnStartup, bool exitAfterApply, string provider, string dbContextType, string mechanism, DateTimeOffset? startedAtUtc, DateTimeOffset? completedAtUtc, string lastError, IReadOnlyList<DatabaseMigrationCommandDescriptor> commands, IReadOnlyDictionary<string, string> metadata, int? recommendedExecutionOrder, HealthState? roleHealthState, string roleHealthDescription, string roleMigrationState, string roleMigrationDescription, DateTimeOffset? roleObservedAtUtc)
```

Creates a new database-migration descriptor.

Parameters:
- `id`: The stable logical migration-target identifier.
- `displayName`: The operator-facing migration-target name.
- `description`: The human-readable migration-target description.
- `requestedRoleId`: The logical database role requested by migration policy.
- `resolvedRoleId`: The concrete database role that backs the target.
- `executionMode`: The execution mode such as `startup-hosted-service` or `manual-or-deploy-time`.
- `status`: The current execution status of the migration target.
- `applyOnStartup`: Whether startup execution is enabled for this target.
- `exitAfterApply`: Whether the host exits after startup execution completes.
- `provider`: The effective provider identifier when known.
- `dbContextType`: The DbContext type that can execute the target when known.
- `mechanism`: The execution mechanism such as `migrate` or `ensure-created`.
- `startedAtUtc`: The latest start time observed for this target.
- `completedAtUtc`: The latest completion time observed for this target.
- `lastError`: The latest error observed for this target.
- `commands`: Optional operator-facing command templates for executing this target outside startup apply.
- `metadata`: Optional operator-facing metadata associated with the migration target.
- `recommendedExecutionOrder`: An optional positive ordinal that operator surfaces can use when presenting a recommended migration sequence.
- `roleHealthState`: The current runtime health state reported for the resolved role behind this target, when available.
- `roleHealthDescription`: The operator-facing health description reported for the resolved role behind this target, when available.
- `roleMigrationState`: The current migration execution state reported for the resolved role behind this target, when available.
- `roleMigrationDescription`: The operator-facing migration description reported for the resolved role behind this target, when available.
- `roleObservedAtUtc`: The UTC timestamp when resolved-role runtime state was last observed for this target, when available.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-applyonstartup"></a>

##### `ApplyOnStartup`

```csharp
bool ApplyOnStartup { get; }
```

Gets a value indicating whether startup execution is enabled for this target.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-commands"></a>

##### `Commands`

```csharp
IReadOnlyList<DatabaseMigrationCommandDescriptor> Commands { get; }
```

Gets optional operator-facing command templates for executing this target outside startup apply.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-completedatutc"></a>

##### `CompletedAtUtc`

```csharp
DateTimeOffset? CompletedAtUtc { get; }
```

Gets the latest completion time observed for this target.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-dbcontexttype"></a>

##### `DbContextType`

```csharp
string DbContextType { get; }
```

Gets the DbContext type that can execute the target when known.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable migration-target description.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing migration-target name.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-executionmode"></a>

##### `ExecutionMode`

```csharp
string ExecutionMode { get; }
```

Gets the runtime execution mode for this target.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-exitafterapply"></a>

##### `ExitAfterApply`

```csharp
bool ExitAfterApply { get; }
```

Gets a value indicating whether the host exits after startup execution completes.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable logical migration-target identifier.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; }
```

Gets the latest error observed for this target.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-mechanism"></a>

##### `Mechanism`

```csharp
string Mechanism { get; }
```

Gets the execution mechanism such as `migrate` or `ensure-created`.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata associated with the migration target.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the effective provider identifier when known.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-recommendedexecutionorder"></a>

##### `RecommendedExecutionOrder`

```csharp
int? RecommendedExecutionOrder { get; }
```

Gets the recommended positive ordinal for operator-facing migration playbooks when the provider can publish one.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-requestedroleid"></a>

##### `RequestedRoleId`

```csharp
string RequestedRoleId { get; }
```

Gets the logical database role requested by migration policy.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-resolvedroleid"></a>

##### `ResolvedRoleId`

```csharp
string ResolvedRoleId { get; }
```

Gets the concrete database role that backs the target.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-rolehealthdescription"></a>

##### `RoleHealthDescription`

```csharp
string RoleHealthDescription { get; }
```

Gets the operator-facing health description reported for the resolved role behind this target, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-rolehealthstate"></a>

##### `RoleHealthState`

```csharp
HealthState? RoleHealthState { get; }
```

Gets the current runtime health state reported for the resolved role behind this target, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-rolemigrationdescription"></a>

##### `RoleMigrationDescription`

```csharp
string RoleMigrationDescription { get; }
```

Gets the operator-facing migration description reported for the resolved role behind this target, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-rolemigrationstate"></a>

##### `RoleMigrationState`

```csharp
string RoleMigrationState { get; }
```

Gets the current migration execution state reported for the resolved role behind this target, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-roleobservedatutc"></a>

##### `RoleObservedAtUtc`

```csharp
DateTimeOffset? RoleObservedAtUtc { get; }
```

Gets the UTC timestamp when resolved-role runtime state was last observed for this target, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-startedatutc"></a>

##### `StartedAtUtc`

```csharp
DateTimeOffset? StartedAtUtc { get; }
```

Gets the latest start time observed for this target.

<a id="member-p-cephalon-abstractions-data-databasemigrationdescriptor-status"></a>

##### `Status`

```csharp
DatabaseMigrationStatus Status { get; }
```

Gets the current execution status of the migration target.

<a id="type-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup"></a>

### `DatabaseMigrationOperationalExecutionGroup`

Describes one engine-owned execution group in the database-migration playbook.

#### Declaration
```csharp
public sealed class DatabaseMigrationOperationalExecutionGroup
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-ctor-system-int32-system-string-system-string-cephalon-abstractions-data-databasemigrationstatus-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-int32-system-int32-system-int32-system-collections-generic-ireadonlylist-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand-system-collections-generic-ireadonlylist-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand-system-string"></a>

##### `DatabaseMigrationOperationalExecutionGroup`

```csharp
DatabaseMigrationOperationalExecutionGroup(int order, string physicalTargetId, string physicalTargetDisplayName, DatabaseMigrationStatus status, IReadOnlyList<string> databaseMigrationIds, IReadOnlyList<string> requestedRoleIds, IReadOnlyList<string> resolvedRoleIds, int productionReadyTargetCount, int manualPathTargetCount, int applyOnStartupTargetCount, IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand> productionCommands, IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand> manualCommands, string coordinationHint)
```

Creates a new database-migration execution group.

Parameters:
- `order`: The positive execution-group order in the playbook.
- `physicalTargetId`: The stable physical-target identifier that anchors this execution group. When the runtime cannot resolve a physical database identity, the engine uses a logical fallback identifier instead of leaving the group anonymous.
- `physicalTargetDisplayName`: The operator-facing description of the physical target that anchors this group.
- `status`: The aggregate execution status across the logical migration targets in this group.
- `databaseMigrationIds`: The logical migration targets that belong to this execution group.
- `requestedRoleIds`: The logical requested role ids represented in this group.
- `resolvedRoleIds`: The concrete resolved role ids represented in this group.
- `productionReadyTargetCount`: The number of targets in this group that publish a production-recommended command.
- `manualPathTargetCount`: The number of targets in this group that publish a direct or manual command path.
- `applyOnStartupTargetCount`: The number of targets in this group that are configured for startup execution.
- `productionCommands`: The selected production-recommended commands grouped for this physical-target batch.
- `manualCommands`: The selected direct or manual commands grouped for this physical-target batch.
- `coordinationHint`: The operator-facing coordination guidance for shared physical targets, when available.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-applyonstartuptargetcount"></a>

##### `ApplyOnStartupTargetCount`

```csharp
int ApplyOnStartupTargetCount { get; }
```

Gets the number of targets in this group that are configured for startup execution.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-coordinationhint"></a>

##### `CoordinationHint`

```csharp
string CoordinationHint { get; }
```

Gets the operator-facing coordination guidance for shared physical targets, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-databasemigrationids"></a>

##### `DatabaseMigrationIds`

```csharp
IReadOnlyList<string> DatabaseMigrationIds { get; }
```

Gets the logical migration targets that belong to this execution group.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-hasmanualcommandsforalltargets"></a>

##### `HasManualCommandsForAllTargets`

```csharp
bool HasManualCommandsForAllTargets { get; }
```

Gets a value indicating whether every target in this group publishes a direct or manual command path.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-hasproductionrecommendedcommandsforalltargets"></a>

##### `HasProductionRecommendedCommandsForAllTargets`

```csharp
bool HasProductionRecommendedCommandsForAllTargets { get; }
```

Gets a value indicating whether every target in this group publishes a production-recommended command.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-manualcommandbatch"></a>

##### `ManualCommandBatch`

```csharp
DatabaseMigrationOperationalExecutionGroupCommandBatch ManualCommandBatch { get; }
```

Gets the combined manual command-batch template for this physical-target batch, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-manualcommands"></a>

##### `ManualCommands`

```csharp
IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand> ManualCommands { get; }
```

Gets the selected direct or manual commands grouped for this physical-target batch.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-manualpathtargetcount"></a>

##### `ManualPathTargetCount`

```csharp
int ManualPathTargetCount { get; }
```

Gets the number of targets in this group that publish a direct or manual command path.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-order"></a>

##### `Order`

```csharp
int Order { get; }
```

Gets the positive execution-group order in the playbook.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-physicaltargetdisplayname"></a>

##### `PhysicalTargetDisplayName`

```csharp
string PhysicalTargetDisplayName { get; }
```

Gets the operator-facing description of the physical target that anchors this group.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-physicaltargetid"></a>

##### `PhysicalTargetId`

```csharp
string PhysicalTargetId { get; }
```

Gets the stable physical-target identifier that anchors this execution group.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-productioncommandbatch"></a>

##### `ProductionCommandBatch`

```csharp
DatabaseMigrationOperationalExecutionGroupCommandBatch ProductionCommandBatch { get; }
```

Gets the combined production command-batch template for this physical-target batch, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-productioncommands"></a>

##### `ProductionCommands`

```csharp
IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand> ProductionCommands { get; }
```

Gets the selected production-recommended commands grouped for this physical-target batch.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-productionreadytargetcount"></a>

##### `ProductionReadyTargetCount`

```csharp
int ProductionReadyTargetCount { get; }
```

Gets the number of targets in this group that publish a production-recommended command.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-requestedroleids"></a>

##### `RequestedRoleIds`

```csharp
IReadOnlyList<string> RequestedRoleIds { get; }
```

Gets the logical requested role ids represented in this group.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-requiresphysicaltargetcoordination"></a>

##### `RequiresPhysicalTargetCoordination`

```csharp
bool RequiresPhysicalTargetCoordination { get; }
```

Gets a value indicating whether this execution group spans multiple logical migration targets on one physical database target.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-resolvedroleids"></a>

##### `ResolvedRoleIds`

```csharp
IReadOnlyList<string> ResolvedRoleIds { get; }
```

Gets the concrete resolved role ids represented in this group.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-status"></a>

##### `Status`

```csharp
DatabaseMigrationStatus Status { get; }
```

Gets the aggregate execution status across the logical migration targets in this group.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup-targetcount"></a>

##### `TargetCount`

```csharp
int TargetCount { get; }
```

Gets the number of logical migration targets represented in this group.

<a id="type-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand"></a>

### `DatabaseMigrationOperationalExecutionGroupCommand`

Describes one selected command path for a logical migration target inside an engine-owned execution group.

#### Declaration
```csharp
public sealed class DatabaseMigrationOperationalExecutionGroupCommand
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand-ctor-system-int32-system-string-system-string-system-string-cephalon-abstractions-data-databasemigrationcommanddescriptor"></a>

##### `DatabaseMigrationOperationalExecutionGroupCommand`

```csharp
DatabaseMigrationOperationalExecutionGroupCommand(int order, string databaseMigrationId, string requestedRoleId, string resolvedRoleId, DatabaseMigrationCommandDescriptor command)
```

Creates a new execution-group command entry.

Parameters:
- `order`: The positive playbook order of the logical migration target that owns this command.
- `databaseMigrationId`: The logical migration target identifier that owns this command.
- `requestedRoleId`: The logical requested role id represented by this command.
- `resolvedRoleId`: The concrete resolved role id represented by this command.
- `command`: The selected command descriptor for this execution-group entry.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand-command"></a>

##### `Command`

```csharp
DatabaseMigrationCommandDescriptor Command { get; }
```

Gets the selected command descriptor for this execution-group entry.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand-databasemigrationid"></a>

##### `DatabaseMigrationId`

```csharp
string DatabaseMigrationId { get; }
```

Gets the logical migration target identifier that owns this command.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand-order"></a>

##### `Order`

```csharp
int Order { get; }
```

Gets the positive playbook order of the logical migration target that owns this command.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand-requestedroleid"></a>

##### `RequestedRoleId`

```csharp
string RequestedRoleId { get; }
```

Gets the logical requested role id represented by this command.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommand-resolvedroleid"></a>

##### `ResolvedRoleId`

```csharp
string ResolvedRoleId { get; }
```

Gets the concrete resolved role id represented by this command.

<a id="type-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch"></a>

### `DatabaseMigrationOperationalExecutionGroupCommandBatch`

Describes one combined command-batch template derived from the selected command path of an engine-owned execution group.

#### Declaration
```csharp
public sealed class DatabaseMigrationOperationalExecutionGroupCommandBatch
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-ctor-system-string-system-string-system-string-system-string-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `DatabaseMigrationOperationalExecutionGroupCommandBatch`

```csharp
DatabaseMigrationOperationalExecutionGroupCommandBatch(string id, string displayName, string description, string commandTemplate, int commandCount, IReadOnlyList<string> databaseMigrationIds, IReadOnlyList<string> commandIds, IReadOnlyList<string> toolIds, IReadOnlyList<string> workingDirectoryHints)
```

Creates a new execution-group command-batch template.

Parameters:
- `id`: The stable batch identifier such as `production` or `manual`.
- `displayName`: The operator-facing batch name.
- `description`: The human-readable batch description.
- `commandTemplate`: The ordered combined command template for this execution-group path.
- `commandCount`: The number of command entries represented in this batch.
- `databaseMigrationIds`: The logical migration targets represented in this batch, in execution order.
- `commandIds`: The stable command identifiers represented in this batch, in encounter order.
- `toolIds`: The stable operator tool identifiers represented in this batch, in encounter order.
- `workingDirectoryHints`: The working-directory hints represented in this batch, in encounter order.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-commandcount"></a>

##### `CommandCount`

```csharp
int CommandCount { get; }
```

Gets the number of command entries represented in this batch.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-commandids"></a>

##### `CommandIds`

```csharp
IReadOnlyList<string> CommandIds { get; }
```

Gets the stable command identifiers represented in this batch, in encounter order.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-commandtemplate"></a>

##### `CommandTemplate`

```csharp
string CommandTemplate { get; }
```

Gets the ordered combined command template for this execution-group path.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-databasemigrationids"></a>

##### `DatabaseMigrationIds`

```csharp
IReadOnlyList<string> DatabaseMigrationIds { get; }
```

Gets the logical migration targets represented in this batch, in execution order.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable batch description.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing batch name.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable batch identifier such as `production` or `manual`.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-primarytoolid"></a>

##### `PrimaryToolId`

```csharp
string PrimaryToolId { get; }
```

Gets the single stable operator tool identifier when the batch uses only one tool.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-primaryworkingdirectoryhint"></a>

##### `PrimaryWorkingDirectoryHint`

```csharp
string PrimaryWorkingDirectoryHint { get; }
```

Gets the single working-directory hint when every command in the batch uses the same working directory.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-toolids"></a>

##### `ToolIds`

```csharp
IReadOnlyList<string> ToolIds { get; }
```

Gets the stable operator tool identifiers represented in this batch, in encounter order.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalexecutiongroupcommandbatch-workingdirectoryhints"></a>

##### `WorkingDirectoryHints`

```csharp
IReadOnlyList<string> WorkingDirectoryHints { get; }
```

Gets the working-directory hints represented in this batch, in encounter order.

<a id="type-cephalon-abstractions-data-databasemigrationoperationalplaybook"></a>

### `DatabaseMigrationOperationalPlaybook`

Describes the engine-owned ordered operator playbook for database migration targets.

#### Declaration
```csharp
public sealed class DatabaseMigrationOperationalPlaybook
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasemigrationoperationalplaybook-ctor-system-datetimeoffset-system-collections-generic-ireadonlylist-cephalon-abstractions-data-databasemigrationoperationalstep-system-collections-generic-ireadonlylist-cephalon-abstractions-data-databasemigrationoperationalexecutiongroup"></a>

##### `DatabaseMigrationOperationalPlaybook`

```csharp
DatabaseMigrationOperationalPlaybook(DateTimeOffset generatedAtUtc, IReadOnlyList<DatabaseMigrationOperationalStep> steps, IReadOnlyList<DatabaseMigrationOperationalExecutionGroup> executionGroups)
```

Creates a new database-migration operational playbook.

Parameters:
- `generatedAtUtc`: The UTC timestamp when the playbook was created.
- `steps`: The ordered operator steps derived from the current migration catalog.
- `executionGroups`: The ordered physical-target execution groups derived from the current migration catalog and shared-target topology truth.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-applyonstartuptargetcount"></a>

##### `ApplyOnStartupTargetCount`

```csharp
int ApplyOnStartupTargetCount { get; }
```

Gets the number of targets that are configured for startup execution.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-coordinationrequiredgroupcount"></a>

##### `CoordinationRequiredGroupCount`

```csharp
int CoordinationRequiredGroupCount { get; }
```

Gets the number of physical-target execution groups that span multiple logical migration targets.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-coordinationrequiredtargetcount"></a>

##### `CoordinationRequiredTargetCount`

```csharp
int CoordinationRequiredTargetCount { get; }
```

Gets the number of targets that share one physical database target with another migration target.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-executiongroupcount"></a>

##### `ExecutionGroupCount`

```csharp
int ExecutionGroupCount { get; }
```

Gets the total number of physical-target execution groups in the playbook.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-executiongroups"></a>

##### `ExecutionGroups`

```csharp
IReadOnlyList<DatabaseMigrationOperationalExecutionGroup> ExecutionGroups { get; }
```

Gets the ordered physical-target execution groups derived from the current migration catalog.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-generatedatutc"></a>

##### `GeneratedAtUtc`

```csharp
DateTimeOffset GeneratedAtUtc { get; }
```

Gets the UTC timestamp when the playbook was created.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-manualpathtargetcount"></a>

##### `ManualPathTargetCount`

```csharp
int ManualPathTargetCount { get; }
```

Gets the number of targets that publish a direct or manual command path.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-productionreadytargetcount"></a>

##### `ProductionReadyTargetCount`

```csharp
int ProductionReadyTargetCount { get; }
```

Gets the number of targets that publish a production-recommended command.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-steps"></a>

##### `Steps`

```csharp
IReadOnlyList<DatabaseMigrationOperationalStep> Steps { get; }
```

Gets the ordered operator steps derived from the current migration catalog.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalplaybook-targetcount"></a>

##### `TargetCount`

```csharp
int TargetCount { get; }
```

Gets the total number of migration targets in the playbook.

<a id="type-cephalon-abstractions-data-databasemigrationoperationalstep"></a>

### `DatabaseMigrationOperationalStep`

Describes one ordered operator step in the engine-owned database-migration playbook.

#### Declaration
```csharp
public sealed class DatabaseMigrationOperationalStep
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasemigrationoperationalstep-ctor-system-int32-system-string-system-string-system-string-cephalon-abstractions-data-databasemigrationstatus-system-string-system-boolean-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-cephalon-abstractions-data-databasemigrationcommanddescriptor-cephalon-abstractions-data-databasemigrationcommanddescriptor"></a>

##### `DatabaseMigrationOperationalStep`

```csharp
DatabaseMigrationOperationalStep(int order, string databaseMigrationId, string requestedRoleId, string resolvedRoleId, DatabaseMigrationStatus status, string executionMode, bool applyOnStartup, string physicalTargetId, string physicalTargetDisplayName, IReadOnlyList<string> coordinatedMigrationIds, string coordinationHint, DatabaseMigrationCommandDescriptor productionCommand, DatabaseMigrationCommandDescriptor manualCommand)
```

Creates a new database-migration operational step.

Parameters:
- `order`: The positive playbook order for this step.
- `databaseMigrationId`: The logical database-migration target identifier for this step.
- `requestedRoleId`: The logical database role requested by migration policy.
- `resolvedRoleId`: The concrete database role that backs this step.
- `status`: The current execution status for this step.
- `executionMode`: The execution mode such as `startup-hosted-service` or `manual-or-deploy-time`.
- `applyOnStartup`: Whether startup execution is enabled for this step.
- `physicalTargetId`: The stable physical-target identifier that backs this step when known.
- `physicalTargetDisplayName`: The operator-facing description of the physical target that backs this step when known.
- `coordinatedMigrationIds`: Other logical migration targets that share the same physical database target.
- `coordinationHint`: The operator-facing coordination guidance for shared physical targets, when available.
- `productionCommand`: The primary production-recommended command selected for this step when available.
- `manualCommand`: The primary direct or manual command selected for this step when available.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-applyonstartup"></a>

##### `ApplyOnStartup`

```csharp
bool ApplyOnStartup { get; }
```

Gets a value indicating whether startup execution is enabled for this step.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-coordinatedmigrationids"></a>

##### `CoordinatedMigrationIds`

```csharp
IReadOnlyList<string> CoordinatedMigrationIds { get; }
```

Gets the other logical migration targets that share the same physical database target.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-coordinationhint"></a>

##### `CoordinationHint`

```csharp
string CoordinationHint { get; }
```

Gets the operator-facing coordination guidance for shared physical targets, when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-databasemigrationid"></a>

##### `DatabaseMigrationId`

```csharp
string DatabaseMigrationId { get; }
```

Gets the logical database-migration target identifier for this step.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-executionmode"></a>

##### `ExecutionMode`

```csharp
string ExecutionMode { get; }
```

Gets the execution mode for this step.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-hasproductionrecommendedcommand"></a>

##### `HasProductionRecommendedCommand`

```csharp
bool HasProductionRecommendedCommand { get; }
```

Gets a value indicating whether this step publishes a production-recommended command.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-manualcommand"></a>

##### `ManualCommand`

```csharp
DatabaseMigrationCommandDescriptor ManualCommand { get; }
```

Gets the primary direct or manual command selected for this step when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-order"></a>

##### `Order`

```csharp
int Order { get; }
```

Gets the positive playbook order for this step.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-physicaltargetdisplayname"></a>

##### `PhysicalTargetDisplayName`

```csharp
string PhysicalTargetDisplayName { get; }
```

Gets the operator-facing description of the physical target that backs this step when known.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-physicaltargetid"></a>

##### `PhysicalTargetId`

```csharp
string PhysicalTargetId { get; }
```

Gets the stable physical-target identifier that backs this step when known.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-productioncommand"></a>

##### `ProductionCommand`

```csharp
DatabaseMigrationCommandDescriptor ProductionCommand { get; }
```

Gets the primary production-recommended command selected for this step when available.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-requestedroleid"></a>

##### `RequestedRoleId`

```csharp
string RequestedRoleId { get; }
```

Gets the logical database role requested by migration policy.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-requiresphysicaltargetcoordination"></a>

##### `RequiresPhysicalTargetCoordination`

```csharp
bool RequiresPhysicalTargetCoordination { get; }
```

Gets a value indicating whether this step needs shared-physical-target coordination.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-resolvedroleid"></a>

##### `ResolvedRoleId`

```csharp
string ResolvedRoleId { get; }
```

Gets the concrete database role that backs this step.

<a id="member-p-cephalon-abstractions-data-databasemigrationoperationalstep-status"></a>

##### `Status`

```csharp
DatabaseMigrationStatus Status { get; }
```

Gets the current execution status for this step.

<a id="type-cephalon-abstractions-data-databasemigrationstatus"></a>

### `DatabaseMigrationStatus`

Describes the current execution state of one logical database-migration target.

#### Declaration
```csharp
public enum DatabaseMigrationStatus
```

#### Fields

<a id="member-f-cephalon-abstractions-data-databasemigrationstatus-failed"></a>

##### `Failed`

```csharp
const DatabaseMigrationStatus Failed
```

The migration target failed during execution.

<a id="member-f-cephalon-abstractions-data-databasemigrationstatus-planned"></a>

##### `Planned`

```csharp
const DatabaseMigrationStatus Planned
```

The migration target is known to the runtime but has not started executing yet.

<a id="member-f-cephalon-abstractions-data-databasemigrationstatus-running"></a>

##### `Running`

```csharp
const DatabaseMigrationStatus Running
```

The migration target is currently executing.

<a id="member-f-cephalon-abstractions-data-databasemigrationstatus-succeeded"></a>

##### `Succeeded`

```csharp
const DatabaseMigrationStatus Succeeded
```

The migration target completed successfully.

<a id="member-f-cephalon-abstractions-data-databasemigrationstatus-unsupported"></a>

##### `Unsupported`

```csharp
const DatabaseMigrationStatus Unsupported
```

The runtime cannot execute the configured migration target with the active provider-pack registrations.

<a id="type-cephalon-abstractions-data-databaseroledescriptor"></a>

### `DatabaseRoleDescriptor`

Describes one logical database role resolved for the active Cephalon runtime.

#### Declaration
```csharp
public sealed class DatabaseRoleDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databaseroledescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-appmodel-databaseruntimeselection-system-boolean-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-nullable-cephalon-abstractions-health-healthstate-system-string-system-string-system-string-system-nullable-system-datetimeoffset-cephalon-abstractions-data-databaseroleprobedescriptor-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `DatabaseRoleDescriptor`

```csharp
DatabaseRoleDescriptor(string id, string displayName, string description, string provider, string requestedRoleId, string resolvedRoleId, string resolutionMode, DatabaseRuntimeSelection runtime, bool usesRoleReference, string useRole, string connectionMode, string connectionStringName, string schema, IReadOnlyList<string> consumers, IReadOnlyList<string> referencedByRoles, IReadOnlyList<string> coLocatedRoles, string physicalTargetId, string physicalTargetDisplayName, IReadOnlyList<string> physicalCoLocatedRoles, IReadOnlyDictionary<string, string> metadata, HealthState? healthState, string healthDescription, string migrationState, string migrationDescription, DateTimeOffset? observedAtUtc, DatabaseRoleProbeDescriptor probe, IReadOnlyDictionary<string, string> runtimeMetadata)
```

Creates a new database-role descriptor.

Parameters:
- `id`: The stable logical database-role identifier.
- `displayName`: The operator-facing database-role name.
- `description`: The human-readable database-role description.
- `provider`: The logical provider identifier that backs the effective target.
- `requestedRoleId`: The logical role that was requested by configuration or runtime selection.
- `resolvedRoleId`: The concrete role that ultimately backs the physical target.
- `resolutionMode`: The runtime resolution mode such as `direct` or `role-reference`.
- `runtime`: The effective runtime tuning resolved for this database role.
- `usesRoleReference`: Whether the logical role resolves through `UseRole`.
- `useRole`: The referenced role supplied through `UseRole`, when present.
- `connectionMode`: The effective connection mode such as `named` or `inline`.
- `connectionStringName`: The effective named connection-string reference, when used.
- `schema`: The effective schema override, when configured.
- `consumers`: The logical engine features that explicitly target this role.
- `referencedByRoles`: Other logical roles that explicitly reference this role through `UseRole`.
- `coLocatedRoles`: Other logical roles that resolve to the same concrete role target.
- `physicalTargetId`: The stable physical-target identifier used to group logical roles that share one physical database target.
- `physicalTargetDisplayName`: The operator-facing description of the physical target that backs this role.
- `physicalCoLocatedRoles`: Other logical roles that share the same physical database target.
- `metadata`: Optional operator-facing metadata associated with the database role.
- `healthState`: The current runtime health state reported for the database role, when available.
- `healthDescription`: The operator-facing health description reported for the database role, when available.
- `migrationState`: The current migration execution state reported for the database role, when available.
- `migrationDescription`: The operator-facing migration description reported for the database role, when available.
- `observedAtUtc`: The UTC timestamp when runtime state was last observed for the database role, when available.
- `probe`: The stable probe-freshness answer reported for the database role, when available.
- `runtimeMetadata`: Optional runtime metadata associated with the database role.

#### Properties

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-colocatedroles"></a>

##### `CoLocatedRoles`

```csharp
IReadOnlyList<string> CoLocatedRoles { get; }
```

Gets the logical roles that resolve to the same concrete role target.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-connectionmode"></a>

##### `ConnectionMode`

```csharp
string ConnectionMode { get; }
```

Gets the effective connection mode.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-connectionstringname"></a>

##### `ConnectionStringName`

```csharp
string ConnectionStringName { get; }
```

Gets the effective named connection-string reference, when used.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-consumers"></a>

##### `Consumers`

```csharp
IReadOnlyList<string> Consumers { get; }
```

Gets the logical engine features that explicitly target this role.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable database-role description.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing database-role name.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-healthdescription"></a>

##### `HealthDescription`

```csharp
string HealthDescription { get; }
```

Gets the operator-facing health description reported for the database role, when available.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-healthstate"></a>

##### `HealthState`

```csharp
HealthState? HealthState { get; }
```

Gets the current runtime health state reported for the database role, when available.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable logical database-role identifier.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata associated with the database role.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-migrationdescription"></a>

##### `MigrationDescription`

```csharp
string MigrationDescription { get; }
```

Gets the operator-facing migration description reported for the database role, when available.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-migrationstate"></a>

##### `MigrationState`

```csharp
string MigrationState { get; }
```

Gets the current migration execution state reported for the database role, when available.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset? ObservedAtUtc { get; }
```

Gets the UTC timestamp when runtime state was last observed for the database role, when available.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-physicalcolocatedroles"></a>

##### `PhysicalCoLocatedRoles`

```csharp
IReadOnlyList<string> PhysicalCoLocatedRoles { get; }
```

Gets the logical roles that share the same physical database target.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-physicaltargetdisplayname"></a>

##### `PhysicalTargetDisplayName`

```csharp
string PhysicalTargetDisplayName { get; }
```

Gets the operator-facing description of the physical target that backs this role.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-physicaltargetid"></a>

##### `PhysicalTargetId`

```csharp
string PhysicalTargetId { get; }
```

Gets the stable physical-target identifier used to group logical roles that share one physical database target.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-probe"></a>

##### `Probe`

```csharp
DatabaseRoleProbeDescriptor Probe { get; }
```

Gets the stable probe-freshness answer reported for the database role, when available.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the logical provider identifier that backs the effective target.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-referencedbyroles"></a>

##### `ReferencedByRoles`

```csharp
IReadOnlyList<string> ReferencedByRoles { get; }
```

Gets the logical roles that explicitly reference this role through `UseRole`.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-requestedroleid"></a>

##### `RequestedRoleId`

```csharp
string RequestedRoleId { get; }
```

Gets the logical role requested by configuration or runtime selection.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-resolutionmode"></a>

##### `ResolutionMode`

```csharp
string ResolutionMode { get; }
```

Gets the runtime resolution mode.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-resolvedroleid"></a>

##### `ResolvedRoleId`

```csharp
string ResolvedRoleId { get; }
```

Gets the concrete role that ultimately backs the physical target.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-runtime"></a>

##### `Runtime`

```csharp
DatabaseRuntimeSelection Runtime { get; }
```

Gets the effective runtime tuning resolved for this database role.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-runtimemetadata"></a>

##### `RuntimeMetadata`

```csharp
IReadOnlyDictionary<string, string> RuntimeMetadata { get; }
```

Gets optional runtime metadata associated with the database role.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-schema"></a>

##### `Schema`

```csharp
string Schema { get; }
```

Gets the effective schema override, when configured.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-userole"></a>

##### `UseRole`

```csharp
string UseRole { get; }
```

Gets the referenced role supplied through `UseRole`, when present.

<a id="member-p-cephalon-abstractions-data-databaseroledescriptor-usesrolereference"></a>

##### `UsesRoleReference`

```csharp
bool UsesRoleReference { get; }
```

Gets a value indicating whether this role resolves through `UseRole`.

<a id="type-cephalon-abstractions-data-databaseroleprobedescriptor"></a>

### `DatabaseRoleProbeDescriptor`

Describes the stable probe-freshness runtime state published for one database role.

#### Declaration
```csharp
public sealed class DatabaseRoleProbeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databaseroleprobedescriptor-ctor-system-boolean-system-int32-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-int32"></a>

##### `DatabaseRoleProbeDescriptor`

```csharp
DatabaseRoleProbeDescriptor(bool cacheEnabled, int freshnessSeconds, string freshnessOrigin, string source, DateTimeOffset? freshUntilUtc, int? ageSeconds)
```

Creates a new database-role probe descriptor.

Parameters:
- `cacheEnabled`: Whether cached probe answers are enabled for the role.
- `freshnessSeconds`: The configured or default freshness window in seconds.
- `freshnessOrigin`: The source of the effective freshness window, such as `configured` or `default`.
- `source`: The source of the current answer, such as `live` or `cache`.
- `freshUntilUtc`: The UTC timestamp until which the current answer remains fresh, when known.
- `ageSeconds`: The age in seconds of the current answer, when known.

#### Properties

<a id="member-p-cephalon-abstractions-data-databaseroleprobedescriptor-ageseconds"></a>

##### `AgeSeconds`

```csharp
int? AgeSeconds { get; }
```

Gets the age in seconds of the current answer, when known.

<a id="member-p-cephalon-abstractions-data-databaseroleprobedescriptor-cacheenabled"></a>

##### `CacheEnabled`

```csharp
bool CacheEnabled { get; }
```

Gets a value indicating whether cached probe answers are enabled for the role.

<a id="member-p-cephalon-abstractions-data-databaseroleprobedescriptor-freshnessorigin"></a>

##### `FreshnessOrigin`

```csharp
string FreshnessOrigin { get; }
```

Gets the source of the effective freshness window, when known.

<a id="member-p-cephalon-abstractions-data-databaseroleprobedescriptor-freshnessseconds"></a>

##### `FreshnessSeconds`

```csharp
int FreshnessSeconds { get; }
```

Gets the configured or default freshness window in seconds.

<a id="member-p-cephalon-abstractions-data-databaseroleprobedescriptor-freshuntilutc"></a>

##### `FreshUntilUtc`

```csharp
DateTimeOffset? FreshUntilUtc { get; }
```

Gets the UTC timestamp until which the current answer remains fresh, when known.

<a id="member-p-cephalon-abstractions-data-databaseroleprobedescriptor-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source of the current answer, such as `live` or `cache`, when known.

<a id="type-cephalon-abstractions-data-databaseroleruntimedescriptor"></a>

### `DatabaseRoleRuntimeDescriptor`

Describes additive runtime state projected for one logical database role.

#### Declaration
```csharp
public sealed class DatabaseRoleRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databaseroleruntimedescriptor-ctor-system-string-system-nullable-cephalon-abstractions-health-healthstate-system-string-system-string-system-string-system-nullable-system-datetimeoffset-cephalon-abstractions-data-databaseroleprobedescriptor-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `DatabaseRoleRuntimeDescriptor`

```csharp
DatabaseRoleRuntimeDescriptor(string databaseRoleId, HealthState? healthState, string healthDescription, string migrationState, string migrationDescription, DateTimeOffset? observedAtUtc, DatabaseRoleProbeDescriptor probe, IReadOnlyDictionary<string, string> metadata)
```

Creates a new database-role runtime descriptor.

Parameters:
- `databaseRoleId`: The logical database-role identifier that this runtime state applies to.
- `healthState`: The current runtime health state for the role, when known.
- `healthDescription`: The operator-facing health description for the role, when known.
- `migrationState`: The current migration execution state for the role, when known.
- `migrationDescription`: The operator-facing migration description for the role, when known.
- `observedAtUtc`: The UTC timestamp when this runtime state was last observed.
- `probe`: The stable probe-freshness answer for the role, when known.
- `metadata`: Optional runtime metadata associated with the role.

#### Properties

<a id="member-p-cephalon-abstractions-data-databaseroleruntimedescriptor-databaseroleid"></a>

##### `DatabaseRoleId`

```csharp
string DatabaseRoleId { get; }
```

Gets the logical database-role identifier that this runtime state applies to.

<a id="member-p-cephalon-abstractions-data-databaseroleruntimedescriptor-healthdescription"></a>

##### `HealthDescription`

```csharp
string HealthDescription { get; }
```

Gets the operator-facing health description for the role, when known.

<a id="member-p-cephalon-abstractions-data-databaseroleruntimedescriptor-healthstate"></a>

##### `HealthState`

```csharp
HealthState? HealthState { get; }
```

Gets the current runtime health state for the role, when known.

<a id="member-p-cephalon-abstractions-data-databaseroleruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional runtime metadata associated with the role.

<a id="member-p-cephalon-abstractions-data-databaseroleruntimedescriptor-migrationdescription"></a>

##### `MigrationDescription`

```csharp
string MigrationDescription { get; }
```

Gets the operator-facing migration description for the role, when known.

<a id="member-p-cephalon-abstractions-data-databaseroleruntimedescriptor-migrationstate"></a>

##### `MigrationState`

```csharp
string MigrationState { get; }
```

Gets the current migration execution state for the role, when known.

<a id="member-p-cephalon-abstractions-data-databaseroleruntimedescriptor-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset? ObservedAtUtc { get; }
```

Gets the UTC timestamp when this runtime state was last observed.

<a id="member-p-cephalon-abstractions-data-databaseroleruntimedescriptor-probe"></a>

##### `Probe`

```csharp
DatabaseRoleProbeDescriptor Probe { get; }
```

Gets the stable probe-freshness answer for the role, when known.

<a id="type-cephalon-abstractions-data-databasetopologyoperationalaction"></a>

### `DatabaseTopologyOperationalAction`

Describes one engine-owned operator action for the current database-topology posture.

#### Declaration
```csharp
public sealed class DatabaseTopologyOperationalAction
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasetopologyoperationalaction-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `DatabaseTopologyOperationalAction`

```csharp
DatabaseTopologyOperationalAction(string id, string category, string tone, string title, string detail, string completionSignal, string actionLabel, string actionPath, IReadOnlyList<string> sourceRoleIds, IReadOnlyList<string> sourceMigrationIds)
```

Creates a new database-topology operator action.

Parameters:
- `id`: The stable action identifier.
- `category`: The stable machine-readable remediation category.
- `tone`: The operator-facing tone such as `Success`, `Warning`, or `Error`.
- `title`: The human-readable action title.
- `detail`: The operator-facing action detail.
- `completionSignal`: The operator-facing signal that the action is complete.
- `actionLabel`: The suggested operator action label.
- `actionPath`: The suggested operator action path.
- `sourceRoleIds`: Optional logical database-role identifiers that contributed to the action.
- `sourceMigrationIds`: Optional logical migration-target identifiers that contributed to the action.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-actionlabel"></a>

##### `ActionLabel`

```csharp
string ActionLabel { get; }
```

Gets the suggested operator action label.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-actionpath"></a>

##### `ActionPath`

```csharp
string ActionPath { get; }
```

Gets the suggested operator action path.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-category"></a>

##### `Category`

```csharp
string Category { get; }
```

Gets the stable machine-readable remediation category.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-completionsignal"></a>

##### `CompletionSignal`

```csharp
string CompletionSignal { get; }
```

Gets the operator-facing signal that the action is complete.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-detail"></a>

##### `Detail`

```csharp
string Detail { get; }
```

Gets the operator-facing action detail.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable action identifier.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-sourcemigrationids"></a>

##### `SourceMigrationIds`

```csharp
IReadOnlyList<string> SourceMigrationIds { get; }
```

Gets the logical migration-target identifiers that contributed to the action.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-sourceroleids"></a>

##### `SourceRoleIds`

```csharp
IReadOnlyList<string> SourceRoleIds { get; }
```

Gets the logical database-role identifiers that contributed to the action.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-title"></a>

##### `Title`

```csharp
string Title { get; }
```

Gets the human-readable action title.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalaction-tone"></a>

##### `Tone`

```csharp
string Tone { get; }
```

Gets the operator-facing action tone.

<a id="type-cephalon-abstractions-data-databasetopologyoperationalactionplan"></a>

### `DatabaseTopologyOperationalActionPlan`

Describes the engine-owned ordered operator action plan for the current database-topology posture.

#### Declaration
```csharp
public sealed class DatabaseTopologyOperationalActionPlan
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasetopologyoperationalactionplan-ctor-system-datetimeoffset-system-collections-generic-ireadonlylist-cephalon-abstractions-data-databasetopologyoperationalaction"></a>

##### `DatabaseTopologyOperationalActionPlan`

```csharp
DatabaseTopologyOperationalActionPlan(DateTimeOffset generatedAtUtc, IReadOnlyList<DatabaseTopologyOperationalAction> actions)
```

Creates a new database-topology action plan.

Parameters:
- `generatedAtUtc`: The UTC timestamp when the plan was created.
- `actions`: The ordered operator actions derived from the current topology posture.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalactionplan-actions"></a>

##### `Actions`

```csharp
IReadOnlyList<DatabaseTopologyOperationalAction> Actions { get; }
```

Gets the ordered operator actions derived from the current topology posture.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalactionplan-attentionactioncount"></a>

##### `AttentionActionCount`

```csharp
int AttentionActionCount { get; }
```

Gets the number of attention-level actions in the plan.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalactionplan-blockingactioncount"></a>

##### `BlockingActionCount`

```csharp
int BlockingActionCount { get; }
```

Gets the number of blocking actions in the plan.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalactionplan-generatedatutc"></a>

##### `GeneratedAtUtc`

```csharp
DateTimeOffset GeneratedAtUtc { get; }
```

Gets the UTC timestamp when the plan was created.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalactionplan-readyactioncount"></a>

##### `ReadyActionCount`

```csharp
int ReadyActionCount { get; }
```

Gets the number of ready-state actions in the plan.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalactionplan-totalactioncount"></a>

##### `TotalActionCount`

```csharp
int TotalActionCount { get; }
```

Gets the total number of operator actions in the plan.

<a id="type-cephalon-abstractions-data-databasetopologyoperationaladvisory"></a>

### `DatabaseTopologyOperationalAdvisory`

Describes one reusable operator-facing advisory for the current database-topology posture.

#### Declaration
```csharp
public sealed class DatabaseTopologyOperationalAdvisory
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasetopologyoperationaladvisory-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `DatabaseTopologyOperationalAdvisory`

```csharp
DatabaseTopologyOperationalAdvisory(string id, string tone, string title, string detail, string actionLabel, string actionPath, IReadOnlyList<string> sourceRoleIds, IReadOnlyList<string> sourceMigrationIds)
```

Creates a new database-topology advisory.

Parameters:
- `id`: The stable advisory identifier.
- `tone`: The operator-facing tone such as `Success`, `Warning`, or `Error`.
- `title`: The human-readable advisory title.
- `detail`: The operator-facing advisory detail.
- `actionLabel`: The suggested operator action label.
- `actionPath`: The suggested operator action path.
- `sourceRoleIds`: Optional logical database-role identifiers that contributed to the advisory.
- `sourceMigrationIds`: Optional logical migration-target identifiers that contributed to the advisory.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationaladvisory-actionlabel"></a>

##### `ActionLabel`

```csharp
string ActionLabel { get; }
```

Gets the suggested operator action label.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationaladvisory-actionpath"></a>

##### `ActionPath`

```csharp
string ActionPath { get; }
```

Gets the suggested operator action path.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationaladvisory-detail"></a>

##### `Detail`

```csharp
string Detail { get; }
```

Gets the operator-facing advisory detail.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationaladvisory-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable advisory identifier.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationaladvisory-sourcemigrationids"></a>

##### `SourceMigrationIds`

```csharp
IReadOnlyList<string> SourceMigrationIds { get; }
```

Gets the logical migration-target identifiers that contributed to the advisory.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationaladvisory-sourceroleids"></a>

##### `SourceRoleIds`

```csharp
IReadOnlyList<string> SourceRoleIds { get; }
```

Gets the logical database-role identifiers that contributed to the advisory.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationaladvisory-title"></a>

##### `Title`

```csharp
string Title { get; }
```

Gets the human-readable advisory title.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationaladvisory-tone"></a>

##### `Tone`

```csharp
string Tone { get; }
```

Gets the operator-facing advisory tone.

<a id="type-cephalon-abstractions-data-databasetopologyoperationalsnapshot"></a>

### `DatabaseTopologyOperationalSnapshot`

Combines the engine-owned operator-facing database-topology posture into one reusable payload.

#### Declaration
```csharp
public sealed class DatabaseTopologyOperationalSnapshot
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasetopologyoperationalsnapshot-ctor-system-datetimeoffset-cephalon-abstractions-data-databasetopologyoperationalsummary-system-collections-generic-ireadonlylist-cephalon-abstractions-data-databasetopologyoperationaladvisory-cephalon-abstractions-data-databasetopologyoperationalactionplan"></a>

##### `DatabaseTopologyOperationalSnapshot`

```csharp
DatabaseTopologyOperationalSnapshot(DateTimeOffset generatedAtUtc, DatabaseTopologyOperationalSummary summary, IReadOnlyList<DatabaseTopologyOperationalAdvisory> advisories, DatabaseTopologyOperationalActionPlan actionPlan)
```

Creates a new database-topology operational snapshot.

Parameters:
- `generatedAtUtc`: The UTC timestamp when the snapshot was created.
- `summary`: The aggregate operator-facing topology summary.
- `advisories`: The reusable operator-facing advisories derived from the current topology state.
- `actionPlan`: The ordered engine-owned operator action plan derived from the current topology state.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsnapshot-actionplan"></a>

##### `ActionPlan`

```csharp
DatabaseTopologyOperationalActionPlan ActionPlan { get; }
```

Gets the ordered engine-owned operator action plan derived from the current topology state.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsnapshot-advisories"></a>

##### `Advisories`

```csharp
IReadOnlyList<DatabaseTopologyOperationalAdvisory> Advisories { get; }
```

Gets the reusable operator-facing advisories derived from the current topology state.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsnapshot-generatedatutc"></a>

##### `GeneratedAtUtc`

```csharp
DateTimeOffset GeneratedAtUtc { get; }
```

Gets the UTC timestamp when the snapshot was created.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsnapshot-summary"></a>

##### `Summary`

```csharp
DatabaseTopologyOperationalSummary Summary { get; }
```

Gets the aggregate operator-facing topology summary.

<a id="type-cephalon-abstractions-data-databasetopologyoperationalsummary"></a>

### `DatabaseTopologyOperationalSummary`

Describes the aggregate operator-facing posture for the current database topology.

#### Declaration
```csharp
public sealed class DatabaseTopologyOperationalSummary
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-databasetopologyoperationalsummary-ctor-system-string-system-string-system-string-system-string-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32"></a>

##### `DatabaseTopologyOperationalSummary`

```csharp
DatabaseTopologyOperationalSummary(string status, string headline, string detail, string actionLabel, string actionPath, int roleCount, int healthyRoleCount, int degradedRoleCount, int unhealthyRoleCount, int migrationTargetCount, int succeededMigrationTargetCount, int failedMigrationTargetCount, int pendingMigrationTargetCount, int productionReadyMigrationTargetCount)
```

Creates a new database-topology operational summary.

Parameters:
- `status`: The aggregate topology status such as `Ready`, `Attention`, or `Blocked`.
- `headline`: The operator-facing summary headline.
- `detail`: The operator-facing summary detail.
- `actionLabel`: The suggested operator action label.
- `actionPath`: The suggested operator action path.
- `roleCount`: The total number of configured logical database roles.
- `healthyRoleCount`: The number of roles currently reporting healthy runtime state.
- `degradedRoleCount`: The number of roles currently reporting degraded runtime state.
- `unhealthyRoleCount`: The number of roles currently reporting unhealthy runtime state.
- `migrationTargetCount`: The total number of visible logical migration targets.
- `succeededMigrationTargetCount`: The number of migration targets currently reporting `Succeeded`.
- `failedMigrationTargetCount`: The number of migration targets currently reporting `Failed`.
- `pendingMigrationTargetCount`: The number of migration targets that are not yet `Succeeded`.
- `productionReadyMigrationTargetCount`: The number of migration targets that publish production-recommended guidance.

#### Properties

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-actionlabel"></a>

##### `ActionLabel`

```csharp
string ActionLabel { get; }
```

Gets the suggested operator action label.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-actionpath"></a>

##### `ActionPath`

```csharp
string ActionPath { get; }
```

Gets the suggested operator action path.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-degradedrolecount"></a>

##### `DegradedRoleCount`

```csharp
int DegradedRoleCount { get; }
```

Gets the number of roles currently reporting degraded runtime state.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-detail"></a>

##### `Detail`

```csharp
string Detail { get; }
```

Gets the operator-facing summary detail.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-failedmigrationtargetcount"></a>

##### `FailedMigrationTargetCount`

```csharp
int FailedMigrationTargetCount { get; }
```

Gets the number of migration targets currently reporting `Failed`.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-headline"></a>

##### `Headline`

```csharp
string Headline { get; }
```

Gets the operator-facing summary headline.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-healthyrolecount"></a>

##### `HealthyRoleCount`

```csharp
int HealthyRoleCount { get; }
```

Gets the number of roles currently reporting healthy runtime state.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-migrationtargetcount"></a>

##### `MigrationTargetCount`

```csharp
int MigrationTargetCount { get; }
```

Gets the total number of visible logical migration targets.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-pendingmigrationtargetcount"></a>

##### `PendingMigrationTargetCount`

```csharp
int PendingMigrationTargetCount { get; }
```

Gets the number of migration targets that are not yet `Succeeded`.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-productionreadymigrationtargetcount"></a>

##### `ProductionReadyMigrationTargetCount`

```csharp
int ProductionReadyMigrationTargetCount { get; }
```

Gets the number of migration targets that publish production-recommended guidance.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-rolecount"></a>

##### `RoleCount`

```csharp
int RoleCount { get; }
```

Gets the total number of configured logical database roles.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-status"></a>

##### `Status`

```csharp
string Status { get; }
```

Gets the aggregate topology status.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-succeededmigrationtargetcount"></a>

##### `SucceededMigrationTargetCount`

```csharp
int SucceededMigrationTargetCount { get; }
```

Gets the number of migration targets currently reporting `Succeeded`.

<a id="member-p-cephalon-abstractions-data-databasetopologyoperationalsummary-unhealthyrolecount"></a>

##### `UnhealthyRoleCount`

```csharp
int UnhealthyRoleCount { get; }
```

Gets the number of roles currently reporting unhealthy runtime state.

<a id="type-cephalon-abstractions-data-dataproductdescriptor"></a>

### `DataProductDescriptor`

Describes one module-owned data product surface contributed to the active runtime.

#### Declaration
```csharp
public sealed class DataProductDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-dataproductdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `DataProductDescriptor`

```csharp
DataProductDescriptor(string id, string displayName, string description, string sourceModuleId, string domainId, string contractId, string mode, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new data product descriptor.

Parameters:
- `id`: The stable data product identifier.
- `displayName`: The operator-facing data product name.
- `description`: The human-readable data product description.
- `sourceModuleId`: The module identifier that owns the data product.
- `domainId`: The stable domain or bounded-context identifier for the data product.
- `contractId`: The stable query or contract identifier exposed by the data product.
- `mode`: The access mode such as `query`, `snapshot`, or `feed`.
- `tags`: Optional descriptive tags associated with the data product.
- `metadata`: Optional operator-facing metadata associated with the data product.

#### Properties

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-contractid"></a>

##### `ContractId`

```csharp
string ContractId { get; }
```

Gets the stable query or contract identifier exposed by the data product.

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable data product description.

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing data product name.

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-domainid"></a>

##### `DomainId`

```csharp
string DomainId { get; }
```

Gets the stable domain or bounded-context identifier for the data product.

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable data product identifier.

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the data product.

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-mode"></a>

##### `Mode`

```csharp
string Mode { get; }
```

Gets the declared access mode for the data product.

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that owns the data product.

<a id="member-p-cephalon-abstractions-data-dataproductdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the data product.

<a id="type-cephalon-abstractions-data-eventdispatchruntimedescriptor"></a>

### `EventDispatchRuntimeDescriptor`

Describes one operator-facing durable event-dispatch runtime available to the active Cephalon runtime.

#### Declaration
```csharp
public sealed class EventDispatchRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-eventdispatchruntimedescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlylist-system-string-cephalon-abstractions-data-eventdispatchruntimesummary"></a>

##### `EventDispatchRuntimeDescriptor`

```csharp
EventDispatchRuntimeDescriptor(string id, string displayName, string description, IReadOnlyDictionary<string, string> metadata, IReadOnlyList<string> outboxIds, EventDispatchRuntimeSummary summary)
```

Creates a new event-dispatch runtime descriptor.

Parameters:
- `id`: The stable dispatch-runtime identifier.
- `displayName`: The operator-facing dispatch-runtime name.
- `description`: The human-readable dispatch-runtime description.
- `metadata`: Optional operator-facing metadata for the dispatch runtime.
- `outboxIds`: Optional outbox identifiers explicitly owned by the dispatch runtime when execution ownership is bounded to specific outboxes.
- `summary`: Optional aggregate runtime summary describing the latest reported operator-facing state for the dispatch runtime.

#### Properties

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable dispatch-runtime description.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing dispatch-runtime name.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable dispatch-runtime identifier.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata for the dispatch runtime.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimedescriptor-outboxids"></a>

##### `OutboxIds`

```csharp
IReadOnlyList<string> OutboxIds { get; }
```

Gets the outbox identifiers explicitly owned by the dispatch runtime.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimedescriptor-summary"></a>

##### `Summary`

```csharp
EventDispatchRuntimeSummary Summary { get; }
```

Gets the latest aggregate runtime summary reported for the dispatch runtime.

<a id="type-cephalon-abstractions-data-eventdispatchruntimestate"></a>

### `EventDispatchRuntimeState`

Describes the latest operator-facing runtime state reported for one durable event-dispatch path.

#### Declaration
```csharp
public sealed class EventDispatchRuntimeState
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-eventdispatchruntimestate-ctor-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventDispatchRuntimeState`

```csharp
EventDispatchRuntimeState(string OutboxId, string LastChannelId, string LastOutcome, DateTimeOffset? LastObservedAtUtc, string LastMessageId, int LastAttempt, int StartedCount, int SucceededCount, int FailedCount, int RetryScheduledCount, int SkippedCount, string LastError, IReadOnlyDictionary<string, string> Metadata)
```

Describes the latest operator-facing runtime state reported for one durable event-dispatch path.

Parameters:
- `OutboxId`: The stable outbox identifier that owns the dispatch path.
- `LastChannelId`: The last stable channel identifier reported for this dispatch path.
- `LastOutcome`: The last reported outcome identifier when one exists.
- `LastObservedAtUtc`: The UTC timestamp when the last observation was reported.
- `LastMessageId`: The last stable outbound message identifier when one was reported.
- `LastAttempt`: The last reported dispatch attempt number.
- `StartedCount`: The number of `started` observations reported so far.
- `SucceededCount`: The number of `succeeded` observations reported so far.
- `FailedCount`: The number of `failed` observations reported so far.
- `RetryScheduledCount`: The number of `retry-scheduled` observations reported so far.
- `SkippedCount`: The number of `skipped` observations reported so far.
- `LastError`: The last operator-facing error summary when a failure was reported.
- `Metadata`: The operator-facing metadata captured by the latest report.

#### Properties

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; set; }
```

The number of `failed` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-lastattempt"></a>

##### `LastAttempt`

```csharp
int LastAttempt { get; set; }
```

The last reported dispatch attempt number.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-lastchannelid"></a>

##### `LastChannelId`

```csharp
string LastChannelId { get; set; }
```

The last stable channel identifier reported for this dispatch path.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; set; }
```

The last operator-facing error summary when a failure was reported.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-lastmessageid"></a>

##### `LastMessageId`

```csharp
string LastMessageId { get; set; }
```

The last stable outbound message identifier when one was reported.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the last observation was reported.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; set; }
```

The last reported outcome identifier when one exists.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

The operator-facing metadata captured by the latest report.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; set; }
```

The stable outbox identifier that owns the dispatch path.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-retrypending"></a>

##### `RetryPending`

```csharp
bool RetryPending { get; }
```

Gets a value indicating whether the latest report says another retry attempt is pending.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-retryscheduledcount"></a>

##### `RetryScheduledCount`

```csharp
int RetryScheduledCount { get; set; }
```

The number of `retry-scheduled` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-skippedcount"></a>

##### `SkippedCount`

```csharp
int SkippedCount { get; set; }
```

The number of `skipped` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-startedcount"></a>

##### `StartedCount`

```csharp
int StartedCount { get; set; }
```

The number of `started` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-succeededcount"></a>

##### `SucceededCount`

```csharp
int SucceededCount { get; set; }
```

The number of `succeeded` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimestate-totalreports"></a>

##### `TotalReports`

```csharp
int TotalReports { get; }
```

Gets the total number of observations reported for this dispatch path.

<a id="type-cephalon-abstractions-data-eventdispatchruntimesummary"></a>

### `EventDispatchRuntimeSummary`

Describes the latest aggregate operator-facing state reported for one durable event-dispatch runtime.

#### Declaration
```csharp
public sealed class EventDispatchRuntimeSummary
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-eventdispatchruntimesummary-ctor-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-int32-system-string"></a>

##### `EventDispatchRuntimeSummary`

```csharp
EventDispatchRuntimeSummary(IReadOnlyList<string> reportedOutboxIds, string lastOutboxId, string lastChannelId, string lastOutcome, DateTimeOffset? lastObservedAtUtc, string lastMessageId, int lastAttempt, int startedCount, int succeededCount, int failedCount, int retryScheduledCount, int skippedCount, int retryPendingCount, string lastError)
```

Creates a new aggregate runtime summary.

Parameters:
- `reportedOutboxIds`: The outbox identifiers that have reported state for the runtime.
- `lastOutboxId`: The outbox identifier that produced the latest observation.
- `lastChannelId`: The latest reported channel identifier.
- `lastOutcome`: The latest reported dispatch outcome identifier.
- `lastObservedAtUtc`: The UTC timestamp when the latest observation was reported.
- `lastMessageId`: The latest outbound message identifier when one was reported.
- `lastAttempt`: The latest reported dispatch attempt number.
- `startedCount`: The total number of `started` observations reported so far.
- `succeededCount`: The total number of `succeeded` observations reported so far.
- `failedCount`: The total number of `failed` observations reported so far.
- `retryScheduledCount`: The total number of `retry-scheduled` observations reported so far.
- `skippedCount`: The total number of `skipped` observations reported so far.
- `retryPendingCount`: The number of owned outboxes whose latest report still says another retry is pending.
- `lastError`: The latest operator-facing error summary when one was reported.

#### Properties

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-empty"></a>

##### `Empty`

```csharp
EventDispatchRuntimeSummary Empty { get; }
```

Gets an empty runtime summary when no dispatch observations have been reported yet.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; }
```

Gets the total number of `failed` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-hasreports"></a>

##### `HasReports`

```csharp
bool HasReports { get; }
```

Gets a value indicating whether the dispatch runtime has reported any observations yet.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-lastattempt"></a>

##### `LastAttempt`

```csharp
int LastAttempt { get; }
```

Gets the latest reported dispatch attempt number.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-lastchannelid"></a>

##### `LastChannelId`

```csharp
string LastChannelId { get; }
```

Gets the latest reported channel identifier when one exists.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; }
```

Gets the latest operator-facing error summary when one was reported.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-lastmessageid"></a>

##### `LastMessageId`

```csharp
string LastMessageId { get; }
```

Gets the latest outbound message identifier when one was reported.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; }
```

Gets the UTC timestamp when the latest observation was reported.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-lastoutboxid"></a>

##### `LastOutboxId`

```csharp
string LastOutboxId { get; }
```

Gets the outbox identifier that produced the latest observation when one exists.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; }
```

Gets the latest reported dispatch outcome identifier when one exists.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-reportedoutboxcount"></a>

##### `ReportedOutboxCount`

```csharp
int ReportedOutboxCount { get; }
```

Gets the number of outboxes that have reported runtime state for this dispatch runtime.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-reportedoutboxids"></a>

##### `ReportedOutboxIds`

```csharp
IReadOnlyList<string> ReportedOutboxIds { get; }
```

Gets the outbox identifiers that have reported runtime state for the dispatch runtime.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-retrypendingcount"></a>

##### `RetryPendingCount`

```csharp
int RetryPendingCount { get; }
```

Gets the number of owned outboxes whose latest report still says another retry is pending.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-retryscheduledcount"></a>

##### `RetryScheduledCount`

```csharp
int RetryScheduledCount { get; }
```

Gets the total number of `retry-scheduled` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-skippedcount"></a>

##### `SkippedCount`

```csharp
int SkippedCount { get; }
```

Gets the total number of `skipped` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-startedcount"></a>

##### `StartedCount`

```csharp
int StartedCount { get; }
```

Gets the total number of `started` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-succeededcount"></a>

##### `SucceededCount`

```csharp
int SucceededCount { get; }
```

Gets the total number of `succeeded` observations reported so far.

<a id="member-p-cephalon-abstractions-data-eventdispatchruntimesummary-totalreports"></a>

##### `TotalReports`

```csharp
int TotalReports { get; }
```

Gets the total number of reported observations across all owned outboxes.

<a id="type-cephalon-abstractions-data-icdccapture"></a>

### `ICdcCapture`

Captures database changes for one stable CDC surface and shapes them into outbox-ready publications.

#### Declaration
```csharp
public interface ICdcCapture
```

#### Properties

<a id="member-p-cephalon-abstractions-data-icdccapture-cdccaptureid"></a>

##### `CdcCaptureId`

```csharp
string CdcCaptureId { get; }
```

Gets the stable CDC capture identifier owned by this implementation.

#### Methods

<a id="member-m-cephalon-abstractions-data-icdccapture-captureasync-system-threading-cancellationtoken"></a>

##### `CaptureAsync`

```csharp
ValueTask<CdcCaptureExecutionResult> CaptureAsync(CancellationToken cancellationToken)
```

Reads one bounded capture batch and returns the resulting outbox publications plus any provider-facing execution metadata.

Returns: The captured batch result for the active CDC surface.

Parameters:
- `cancellationToken`: The token that cancels the capture stream.

<a id="type-cephalon-abstractions-data-icdccaptureacknowledger"></a>

### `ICdcCaptureAcknowledger`

Allows an active `ICdcCapture` implementation to acknowledge durable progress only after the shared runtime stages the linked outbox publications successfully.

#### Declaration
```csharp
public interface ICdcCaptureAcknowledger
```

#### Methods

<a id="member-m-cephalon-abstractions-data-icdccaptureacknowledger-acknowledgeasync-cephalon-abstractions-data-cdccaptureexecutionacknowledgement-system-threading-cancellationtoken"></a>

##### `AcknowledgeAsync`

```csharp
ValueTask AcknowledgeAsync(CdcCaptureExecutionAcknowledgement acknowledgement, CancellationToken cancellationToken)
```

Commits or acknowledges provider-facing progress for one staged CDC batch.

Returns: A task that completes when the provider-facing acknowledgement has finished.

Parameters:
- `acknowledgement`: The staged batch that is now safe to acknowledge durably.
- `cancellationToken`: The token that cancels the acknowledgement operation.

<a id="type-cephalon-abstractions-data-icdccapturecatalog"></a>

### `ICdcCaptureCatalog`

Exposes the CDC capture surfaces visible to the current runtime.

#### Declaration
```csharp
public interface ICdcCaptureCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-icdccapturecatalog-cdccaptures"></a>

##### `CdcCaptures`

```csharp
IReadOnlyList<CdcCaptureDescriptor> CdcCaptures { get; }
```

Gets all CDC capture surfaces visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-icdccapturecatalog-getbyexecutionruntimeid-system-string"></a>

##### `GetByExecutionRuntimeId`

```csharp
IReadOnlyList<CdcCaptureDescriptor> GetByExecutionRuntimeId(string executionRuntimeId)
```

Gets all CDC captures currently owned by the requested execution runtime.

Returns: The matching CDC captures, or an empty list when the runtime owns none.

Parameters:
- `executionRuntimeId`: The execution-runtime identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccapturecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
CdcCaptureDescriptor GetById(string cdcCaptureId)
```

Gets one CDC capture by its stable identifier.

Returns: The matching CDC capture, or `null` when it is not active.

Parameters:
- `cdcCaptureId`: The CDC capture identifier to resolve.

<a id="member-m-cephalon-abstractions-data-icdccapturecatalog-getbyoutboxid-system-string"></a>

##### `GetByOutboxId`

```csharp
IReadOnlyList<CdcCaptureDescriptor> GetByOutboxId(string outboxId)
```

Gets all CDC captures that publish through the requested outbox.

Returns: The matching CDC captures, or an empty list when no capture uses that outbox.

Parameters:
- `outboxId`: The outbox identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccapturecatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<CdcCaptureDescriptor> GetByProvider(string provider)
```

Gets all CDC captures backed by the requested provider identifier.

Returns: The matching CDC captures, or an empty list when the provider contributes none.

Parameters:
- `provider`: The provider identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccapturecatalog-getbyresourceid-system-string"></a>

##### `GetByResourceId`

```csharp
IReadOnlyList<CdcCaptureDescriptor> GetByResourceId(string resourceId)
```

Gets all CDC captures that explicitly observe the requested resource identifier.

Returns: The matching CDC captures, or an empty list when no capture declares that resource.

Parameters:
- `resourceId`: The resource identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccapturecatalog-getbysourceid-system-string"></a>

##### `GetBySourceId`

```csharp
IReadOnlyList<CdcCaptureDescriptor> GetBySourceId(string sourceId)
```

Gets all CDC captures that observe the requested logical source identifier.

Returns: The matching CDC captures, or an empty list when no capture uses that source.

Parameters:
- `sourceId`: The source identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccapturecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<CdcCaptureDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all CDC captures contributed by the requested module.

Returns: The matching CDC captures, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="type-cephalon-abstractions-data-icdccapturecontributor"></a>

### `ICdcCaptureContributor`

Contributes one or more CDC capture descriptors to the active runtime.

#### Declaration
```csharp
public interface ICdcCaptureContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-data-icdccapturecontributor-registercdccaptures-cephalon-abstractions-data-icdccaptureregistry"></a>

##### `RegisterCdcCaptures`

```csharp
void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
```

Registers one or more CDC capture descriptors with the supplied registry.

Parameters:
- `cdcCaptures`: The registry that collects contributed CDC capture descriptors.

<a id="type-cephalon-abstractions-data-icdccaptureexecutionruntimecatalog"></a>

### `ICdcCaptureExecutionRuntimeCatalog`

Exposes the configured CDC capture execution runtimes visible to the current runtime.

#### Declaration
```csharp
public interface ICdcCaptureExecutionRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-icdccaptureexecutionruntimecatalog-runtimes"></a>

##### `Runtimes`

```csharp
IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> Runtimes { get; }
```

Gets the configured CDC capture execution runtimes visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-icdccaptureexecutionruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
CdcCaptureExecutionRuntimeDescriptor GetById(string executionRuntimeId)
```

Gets one CDC capture execution runtime by its stable identifier.

Returns: The matching execution-runtime descriptor, or `null` when none exists.

Parameters:
- `executionRuntimeId`: The stable execution-runtime identifier to resolve.

<a id="type-cephalon-abstractions-data-icdccaptureexecutionruntimereportsink"></a>

### `ICdcCaptureExecutionRuntimeReportSink`

Accepts operator-facing CDC runtime observations that are reported on behalf of one execution runtime.

#### Declaration
```csharp
public interface ICdcCaptureExecutionRuntimeReportSink
```

#### Methods

<a id="member-m-cephalon-abstractions-data-icdccaptureexecutionruntimereportsink-reportasync-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-data-cdccaptureruntimeobservation-system-threading-cancellationtoken"></a>

##### `ReportAsync`

```csharp
ValueTask ReportAsync(string executionRuntimeId, IReadOnlyList<CdcCaptureRuntimeObservation> observations, CancellationToken cancellationToken)
```

Reports one or more CDC runtime observations for the supplied execution runtime.

Parameters:
- `executionRuntimeId`: The stable execution-runtime identifier that owns the reported captures.
- `observations`: The capture observations to merge into the active runtime-state catalog.
- `cancellationToken`: The token used to observe cancellation.

<a id="type-cephalon-abstractions-data-icdccaptureregistry"></a>

### `ICdcCaptureRegistry`

Receives CDC capture descriptors contributed by active modules or packages.

#### Declaration
```csharp
public interface ICdcCaptureRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-data-icdccaptureregistry-add-cephalon-abstractions-data-cdccapturedescriptor"></a>

##### `Add`

```csharp
void Add(CdcCaptureDescriptor cdcCapture)
```

Adds a CDC capture to the current runtime composition.

Parameters:
- `cdcCapture`: The CDC capture descriptor to register.

<a id="type-cephalon-abstractions-data-icdccaptureruntimestatecatalog"></a>

### `ICdcCaptureRuntimeStateCatalog`

Exposes the operator-facing CDC runtime state currently visible for the active runtime.

#### Declaration
```csharp
public interface ICdcCaptureRuntimeStateCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-icdccaptureruntimestatecatalog-states"></a>

##### `States`

```csharp
IReadOnlyList<CdcCaptureRuntimeState> States { get; }
```

Gets the CDC runtime-state entries visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-icdccaptureruntimestatecatalog-getbyexecutionruntimeid-system-string"></a>

##### `GetByExecutionRuntimeId`

```csharp
IReadOnlyList<CdcCaptureRuntimeState> GetByExecutionRuntimeId(string executionRuntimeId)
```

Gets the CDC runtime-state entries currently owned by the requested execution runtime.

Returns: The matching runtime states, or an empty list when the runtime owns none.

Parameters:
- `executionRuntimeId`: The execution-runtime identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccaptureruntimestatecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
CdcCaptureRuntimeState GetById(string cdcCaptureId)
```

Gets one CDC runtime-state entry by its stable capture identifier.

Returns: The matching runtime state, or `null` when that capture is not active.

Parameters:
- `cdcCaptureId`: The CDC capture identifier to resolve.

<a id="member-m-cephalon-abstractions-data-icdccaptureruntimestatecatalog-getbyoutboxid-system-string"></a>

##### `GetByOutboxId`

```csharp
IReadOnlyList<CdcCaptureRuntimeState> GetByOutboxId(string outboxId)
```

Gets the CDC runtime-state entries that publish through the requested outbox.

Returns: The matching runtime states, or an empty list when no capture uses that outbox.

Parameters:
- `outboxId`: The outbox identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccaptureruntimestatecatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<CdcCaptureRuntimeState> GetByProvider(string provider)
```

Gets the CDC runtime-state entries backed by the requested provider identifier.

Returns: The matching runtime states, or an empty list when the provider contributes none.

Parameters:
- `provider`: The provider identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccaptureruntimestatecatalog-getbyresourceid-system-string"></a>

##### `GetByResourceId`

```csharp
IReadOnlyList<CdcCaptureRuntimeState> GetByResourceId(string resourceId)
```

Gets the CDC runtime-state entries that explicitly observe the requested resource identifier.

Returns: The matching runtime states, or an empty list when no capture declares that resource.

Parameters:
- `resourceId`: The resource identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccaptureruntimestatecatalog-getbysourceid-system-string"></a>

##### `GetBySourceId`

```csharp
IReadOnlyList<CdcCaptureRuntimeState> GetBySourceId(string sourceId)
```

Gets the CDC runtime-state entries that observe the requested logical source identifier.

Returns: The matching runtime states, or an empty list when no capture uses that source.

Parameters:
- `sourceId`: The source identifier to filter by.

<a id="member-m-cephalon-abstractions-data-icdccaptureruntimestatecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<CdcCaptureRuntimeState> GetBySourceModule(string sourceModuleId)
```

Gets the CDC runtime-state entries contributed by the requested module.

Returns: The matching runtime states, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="type-cephalon-abstractions-data-icommand"></a>

### `ICommand`

Marks a request that should execute on the write side of a Cephalon application.

#### Declaration
```csharp
public interface ICommand
```

<a id="type-cephalon-abstractions-data-icommandhandler-tcommand"></a>

### `ICommandHandler<TCommand>`

Handles a write-side request that does not return a result value.

#### Declaration
```csharp
public interface ICommandHandler<TCommand>
```

#### Methods

<a id="member-m-cephalon-abstractions-data-icommandhandler-1-handleasync-0-system-threading-cancellationtoken"></a>

##### `HandleAsync`

```csharp
ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken)
```

Handles the supplied command.

Returns: A task that completes when the command has finished running.

Parameters:
- `command`: The command to execute.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-icommandhandler-tcommand-tresult"></a>

### `ICommandHandler<TCommand, TResult>`

Handles a write-side request that returns a result value.

#### Declaration
```csharp
public interface ICommandHandler<TCommand, TResult>
```

#### Methods

<a id="member-m-cephalon-abstractions-data-icommandhandler-2-handleasync-0-system-threading-cancellationtoken"></a>

##### `HandleAsync`

```csharp
ValueTask<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
```

Handles the supplied command.

Returns: A task that completes with the result produced by the command.

Parameters:
- `command`: The command to execute.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-icommand-tresult"></a>

### `ICommand<TResult>`

Marks a write-side request that returns a value when it completes.

#### Declaration
```csharp
public interface ICommand<TResult>
```

<a id="type-cephalon-abstractions-data-idatabasemigrationcatalog"></a>

### `IDatabaseMigrationCatalog`

Exposes the active runtime database-migration catalog for the current Cephalon host.

#### Declaration
```csharp
public interface IDatabaseMigrationCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-idatabasemigrationcatalog-databasemigrations"></a>

##### `DatabaseMigrations`

```csharp
IReadOnlyList<DatabaseMigrationDescriptor> DatabaseMigrations { get; }
```

Gets every migration target visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-idatabasemigrationcatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
DatabaseMigrationDescriptor GetById(string databaseMigrationId)
```

Gets one migration target by its logical identifier.

Returns: The matching migration-target descriptor, or `null` when none exists.

Parameters:
- `databaseMigrationId`: The logical migration-target identifier.

<a id="type-cephalon-abstractions-data-idatabasemigrationcontributor"></a>

### `IDatabaseMigrationContributor`

Contributes one or more database-migration descriptors to the active Cephalon runtime.

#### Declaration
```csharp
public interface IDatabaseMigrationContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-data-idatabasemigrationcontributor-describedatabasemigrations"></a>

##### `DescribeDatabaseMigrations`

```csharp
IReadOnlyList<DatabaseMigrationDescriptor> DescribeDatabaseMigrations()
```

Describes the database-migration targets that should appear in the active runtime catalog.

Returns: The migration descriptors contributed by the current provider or module pack.

<a id="type-cephalon-abstractions-data-idatabasemigrationoperationalplaybookprovider"></a>

### `IDatabaseMigrationOperationalPlaybookProvider`

Creates the engine-owned ordered operator playbook for the current database-migration catalog.

#### Declaration
```csharp
public interface IDatabaseMigrationOperationalPlaybookProvider
```

#### Methods

<a id="member-m-cephalon-abstractions-data-idatabasemigrationoperationalplaybookprovider-createplaybook"></a>

##### `CreatePlaybook`

```csharp
DatabaseMigrationOperationalPlaybook CreatePlaybook()
```

Creates the current database-migration playbook.

Returns: The current ordered operator playbook for database migrations.

<a id="type-cephalon-abstractions-data-idatabaserolecatalog"></a>

### `IDatabaseRoleCatalog`

Exposes the active engine-owned database-role catalog for the current runtime.

#### Declaration
```csharp
public interface IDatabaseRoleCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-idatabaserolecatalog-databaseroles"></a>

##### `DatabaseRoles`

```csharp
IReadOnlyList<DatabaseRoleDescriptor> DatabaseRoles { get; }
```

Gets every database role visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-idatabaserolecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
DatabaseRoleDescriptor GetById(string databaseRoleId)
```

Gets one database role by its logical identifier.

Returns: The matching database-role descriptor, or `null` when none exists.

Parameters:
- `databaseRoleId`: The logical database-role identifier.

<a id="member-m-cephalon-abstractions-data-idatabaserolecatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<DatabaseRoleDescriptor> GetByProvider(string provider)
```

Gets every database role backed by the supplied provider identifier.

Returns: The matching database-role descriptors.

Parameters:
- `provider`: The provider identifier to match.

<a id="member-m-cephalon-abstractions-data-idatabaserolecatalog-getbyresolvedrole-system-string"></a>

##### `GetByResolvedRole`

```csharp
IReadOnlyList<DatabaseRoleDescriptor> GetByResolvedRole(string resolvedRoleId)
```

Gets every database role that resolves to the supplied concrete role identifier.

Returns: The matching database-role descriptors.

Parameters:
- `resolvedRoleId`: The resolved concrete database-role identifier.

<a id="type-cephalon-abstractions-data-idatabaseroleruntimecontributor"></a>

### `IDatabaseRoleRuntimeContributor`

Contributes additive runtime state for one or more logical database roles.

#### Declaration
```csharp
public interface IDatabaseRoleRuntimeContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-data-idatabaseroleruntimecontributor-describedatabaseroleruntime"></a>

##### `DescribeDatabaseRoleRuntime`

```csharp
IReadOnlyList<DatabaseRoleRuntimeDescriptor> DescribeDatabaseRoleRuntime()
```

Describes the runtime state that should be merged into the active database-role catalog.

Returns: The runtime descriptors that should enrich the active database-role catalog.

<a id="type-cephalon-abstractions-data-idatabasetopologyoperationalsnapshotprovider"></a>

### `IDatabaseTopologyOperationalSnapshotProvider`

Creates the engine-owned operator-facing database-topology posture snapshot for the current runtime.

#### Declaration
```csharp
public interface IDatabaseTopologyOperationalSnapshotProvider
```

#### Methods

<a id="member-m-cephalon-abstractions-data-idatabasetopologyoperationalsnapshotprovider-createsnapshot"></a>

##### `CreateSnapshot`

```csharp
DatabaseTopologyOperationalSnapshot CreateSnapshot()
```

Creates the current database-topology posture snapshot.

Returns: The current operator-facing database-topology posture snapshot.

<a id="type-cephalon-abstractions-data-idataproductcatalog"></a>

### `IDataProductCatalog`

Exposes the data products visible to the current runtime.

#### Declaration
```csharp
public interface IDataProductCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-idataproductcatalog-dataproducts"></a>

##### `DataProducts`

```csharp
IReadOnlyList<DataProductDescriptor> DataProducts { get; }
```

Gets all data products visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-idataproductcatalog-getbycontractid-system-string"></a>

##### `GetByContractId`

```csharp
IReadOnlyList<DataProductDescriptor> GetByContractId(string contractId)
```

Gets all data products that expose the requested contract identifier.

Returns: The matching data products, or an empty list when no active data product exposes the contract.

Parameters:
- `contractId`: The contract identifier to filter by.

<a id="member-m-cephalon-abstractions-data-idataproductcatalog-getbydomainid-system-string"></a>

##### `GetByDomainId`

```csharp
IReadOnlyList<DataProductDescriptor> GetByDomainId(string domainId)
```

Gets all data products that belong to the requested domain.

Returns: The matching data products, or an empty list when the domain contributed none.

Parameters:
- `domainId`: The domain identifier to filter by.

<a id="member-m-cephalon-abstractions-data-idataproductcatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
DataProductDescriptor GetById(string dataProductId)
```

Gets one data product by its stable identifier.

Returns: The matching data product, or `null` when it is not active.

Parameters:
- `dataProductId`: The data product identifier to resolve.

<a id="member-m-cephalon-abstractions-data-idataproductcatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<DataProductDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all data products contributed by the requested module.

Returns: The matching data products, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="type-cephalon-abstractions-data-idataproductcontributor"></a>

### `IDataProductContributor`

Contributes one or more data product descriptors to the active runtime.

#### Declaration
```csharp
public interface IDataProductContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-data-idataproductcontributor-registerdataproducts-cephalon-abstractions-data-idataproductregistry"></a>

##### `RegisterDataProducts`

```csharp
void RegisterDataProducts(IDataProductRegistry dataProducts)
```

Registers one or more data product descriptors with the supplied registry.

Parameters:
- `dataProducts`: The registry that collects contributed data product descriptors.

<a id="type-cephalon-abstractions-data-idataproductregistry"></a>

### `IDataProductRegistry`

Receives data product descriptors contributed by active modules or packages.

#### Declaration
```csharp
public interface IDataProductRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-data-idataproductregistry-add-cephalon-abstractions-data-dataproductdescriptor"></a>

##### `Add`

```csharp
void Add(DataProductDescriptor dataProduct)
```

Adds a data product to the current runtime composition.

Parameters:
- `dataProduct`: The data product descriptor to register.

<a id="type-cephalon-abstractions-data-idataproduct-t"></a>

### `IDataProduct<T>`

Exposes a module-owned queryable data product.

#### Declaration
```csharp
public interface IDataProduct<T>
```

#### Methods

<a id="member-m-cephalon-abstractions-data-idataproduct-1-queryasync-system-threading-cancellationtoken"></a>

##### `QueryAsync`

```csharp
ValueTask<T> QueryAsync(CancellationToken cancellationToken)
```

Queries the current value of the data product.

Returns: A task that completes with the current data product value.

Parameters:
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-ieventdispatchruntimecatalog"></a>

### `IEventDispatchRuntimeCatalog`

Exposes the operator-facing dispatch runtime state currently reported for durable event publication paths.

#### Declaration
```csharp
public interface IEventDispatchRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-ieventdispatchruntimecatalog-states"></a>

##### `States`

```csharp
IReadOnlyList<EventDispatchRuntimeState> States { get; }
```

Gets the reported dispatch state entries visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-ieventdispatchruntimecatalog-getbyoutboxid-system-string"></a>

##### `GetByOutboxId`

```csharp
EventDispatchRuntimeState GetByOutboxId(string outboxId)
```

Gets the latest reported dispatch state for one outbox-backed publication path.

Returns: The latest reported state, or `null` when that path has not reported runtime state.

Parameters:
- `outboxId`: The stable outbox identifier to resolve.

<a id="member-m-cephalon-abstractions-data-ieventdispatchruntimecatalog-tryget-system-string-cephalon-abstractions-data-eventdispatchruntimestate"></a>

##### `TryGet`

```csharp
bool TryGet(string outboxId, out EventDispatchRuntimeState state)
```

Tries to get the latest reported dispatch state for one outbox-backed publication path.

Returns: `true` when a reported state exists; otherwise, `false`.

Parameters:
- `outboxId`: The stable outbox identifier to resolve.
- `state`: Receives the latest reported state when the path has reported one.

<a id="type-cephalon-abstractions-data-ieventdispatchruntimedescriptorcatalog"></a>

### `IEventDispatchRuntimeDescriptorCatalog`

Exposes the configured operator-facing durable dispatch runtimes visible to the current runtime.

#### Declaration
```csharp
public interface IEventDispatchRuntimeDescriptorCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-ieventdispatchruntimedescriptorcatalog-runtimes"></a>

##### `Runtimes`

```csharp
IReadOnlyList<EventDispatchRuntimeDescriptor> Runtimes { get; }
```

Gets the configured dispatch runtimes visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-ieventdispatchruntimedescriptorcatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
EventDispatchRuntimeDescriptor GetById(string dispatchRuntimeId)
```

Gets one dispatch runtime by its stable identifier.

Returns: The matching dispatch-runtime descriptor, or `null` when none exists.

Parameters:
- `dispatchRuntimeId`: The stable dispatch-runtime identifier to resolve.

<a id="type-cephalon-abstractions-data-iinbox"></a>

### `IInbox`

Tracks inbound messages so consumer pipelines can enforce idempotent handling.

#### Declaration
```csharp
public interface IInbox
```

#### Methods

<a id="member-m-cephalon-abstractions-data-iinbox-hasprocessedasync-system-string-system-threading-cancellationtoken"></a>

##### `HasProcessedAsync`

```csharp
ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken)
```

Determines whether the requested message identifier has already been recorded as processed.

Returns: `true` when the message has already been processed; otherwise, `false`.

Parameters:
- `messageId`: The stable inbound message identifier.
- `cancellationToken`: The token that cancels the operation.

<a id="member-m-cephalon-abstractions-data-iinbox-markprocessedasync-cephalon-abstractions-data-inboxmessage-system-threading-cancellationtoken"></a>

##### `MarkProcessedAsync`

```csharp
ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken)
```

Records one inbound message as processed.

Returns: A task that completes when the inbox has persisted the processed-message record.

Parameters:
- `message`: The inbound message that completed processing.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-iinboxcatalog"></a>

### `IInboxCatalog`

Exposes the inbox surfaces visible to the current runtime.

#### Declaration
```csharp
public interface IInboxCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-iinboxcatalog-inboxes"></a>

##### `Inboxes`

```csharp
IReadOnlyList<InboxDescriptor> Inboxes { get; }
```

Gets all inbox surfaces visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-iinboxcatalog-getbychannelid-system-string"></a>

##### `GetByChannelId`

```csharp
IReadOnlyList<InboxDescriptor> GetByChannelId(string channelId)
```

Gets all inboxes that explicitly declare the requested channel identifier.

Returns: The matching inboxes, or an empty list when no inbox declares that channel.

Parameters:
- `channelId`: The channel identifier to filter by.

<a id="member-m-cephalon-abstractions-data-iinboxcatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
InboxDescriptor GetById(string inboxId)
```

Gets one inbox by its stable identifier.

Returns: The matching inbox, or `null` when it is not active.

Parameters:
- `inboxId`: The inbox identifier to resolve.

<a id="member-m-cephalon-abstractions-data-iinboxcatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<InboxDescriptor> GetByProvider(string provider)
```

Gets all inboxes backed by the requested provider identifier.

Returns: The matching inboxes, or an empty list when the provider contributes none.

Parameters:
- `provider`: The provider identifier to filter by.

<a id="member-m-cephalon-abstractions-data-iinboxcatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<InboxDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all inboxes contributed by the requested module.

Returns: The matching inboxes, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="type-cephalon-abstractions-data-iinboxcontributor"></a>

### `IInboxContributor`

Contributes one or more inbox descriptors to the active runtime.

#### Declaration
```csharp
public interface IInboxContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-data-iinboxcontributor-registerinboxes-cephalon-abstractions-data-iinboxregistry"></a>

##### `RegisterInboxes`

```csharp
void RegisterInboxes(IInboxRegistry inboxes)
```

Registers one or more inbox descriptors with the supplied registry.

Parameters:
- `inboxes`: The registry that collects contributed inbox descriptors.

<a id="type-cephalon-abstractions-data-iinboxregistry"></a>

### `IInboxRegistry`

Receives inbox descriptors contributed by active modules or packages.

#### Declaration
```csharp
public interface IInboxRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-data-iinboxregistry-add-cephalon-abstractions-data-inboxdescriptor"></a>

##### `Add`

```csharp
void Add(InboxDescriptor inbox)
```

Adds an inbox to the current runtime composition.

Parameters:
- `inbox`: The inbox descriptor to register.

<a id="type-cephalon-abstractions-data-inboxdescriptor"></a>

### `InboxDescriptor`

Describes one inbox surface contributed to the active runtime.

#### Declaration
```csharp
public sealed class InboxDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-inboxdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `InboxDescriptor`

```csharp
InboxDescriptor(string id, string displayName, string description, string sourceModuleId, string provider, string mode, IReadOnlyList<string> channelIds, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new inbox descriptor.

Parameters:
- `id`: The stable inbox identifier.
- `displayName`: The operator-facing inbox name.
- `description`: The human-readable inbox description.
- `sourceModuleId`: The module identifier that owns the inbox surface.
- `provider`: The logical provider identifier that backs the inbox.
- `mode`: The inbox mode such as `processed-message-table` or `durable-log`.
- `channelIds`: Optional channel identifiers that this inbox is explicitly scoped to.
- `tags`: Optional descriptive tags associated with the inbox.
- `metadata`: Optional operator-facing metadata associated with the inbox.

#### Properties

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-channelids"></a>

##### `ChannelIds`

```csharp
IReadOnlyList<string> ChannelIds { get; }
```

Gets the optional channel identifiers that this inbox is explicitly scoped to.

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable inbox description.

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing inbox name.

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable inbox identifier.

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the inbox.

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-mode"></a>

##### `Mode`

```csharp
string Mode { get; }
```

Gets the inbox mode.

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the logical provider identifier that backs the inbox.

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that owns the inbox surface.

<a id="member-p-cephalon-abstractions-data-inboxdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the inbox.

<a id="type-cephalon-abstractions-data-inboxmessage"></a>

### `InboxMessage`

Describes one inbound message tracked by an inbox implementation for idempotency or replay control.

#### Declaration
```csharp
public sealed class InboxMessage
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-inboxmessage-ctor-system-string-system-string-system-string-system-string-system-datetimeoffset-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `InboxMessage`

```csharp
InboxMessage(string id, string channelId, string messageType, string payload, DateTimeOffset receivedAtUtc, string contentType, string correlationId, string tenantId, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> metadata)
```

Creates a new inbox message.

Parameters:
- `id`: The stable inbound message identifier.
- `channelId`: The logical channel or source identifier.
- `messageType`: The logical message type identifier.
- `payload`: The serialized payload that was received.
- `receivedAtUtc`: The time at which the message was received.
- `contentType`: The payload content type when one is known.
- `correlationId`: The correlation identifier associated with the message.
- `tenantId`: The tenant identifier associated with the message.
- `headers`: Optional message headers.
- `metadata`: Optional message metadata.

#### Properties

<a id="member-p-cephalon-abstractions-data-inboxmessage-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; }
```

Gets the logical channel or source identifier.

<a id="member-p-cephalon-abstractions-data-inboxmessage-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; }
```

Gets the payload content type when one is known.

<a id="member-p-cephalon-abstractions-data-inboxmessage-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier associated with the message.

<a id="member-p-cephalon-abstractions-data-inboxmessage-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; }
```

Gets message headers associated with the message.

<a id="member-p-cephalon-abstractions-data-inboxmessage-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable inbound message identifier.

<a id="member-p-cephalon-abstractions-data-inboxmessage-messagetype"></a>

##### `MessageType`

```csharp
string MessageType { get; }
```

Gets the logical message type identifier.

<a id="member-p-cephalon-abstractions-data-inboxmessage-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets message metadata associated with the message.

<a id="member-p-cephalon-abstractions-data-inboxmessage-payload"></a>

##### `Payload`

```csharp
string Payload { get; }
```

Gets the serialized payload that was received.

<a id="member-p-cephalon-abstractions-data-inboxmessage-receivedatutc"></a>

##### `ReceivedAtUtc`

```csharp
DateTimeOffset ReceivedAtUtc { get; }
```

Gets the time at which the message was received.

<a id="member-p-cephalon-abstractions-data-inboxmessage-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the message.

<a id="type-cephalon-abstractions-data-ioutbox"></a>

### `IOutbox`

Stages messages for durable delivery after the current write-side operation completes.

#### Declaration
```csharp
public interface IOutbox
```

#### Properties

<a id="member-p-cephalon-abstractions-data-ioutbox-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; }
```

Gets the stable outbox identifier owned by this implementation.

#### Methods

<a id="member-m-cephalon-abstractions-data-ioutbox-enqueueasync-cephalon-abstractions-data-outboxmessage-system-threading-cancellationtoken"></a>

##### `EnqueueAsync`

```csharp
ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken)
```

Enqueues one message for later delivery.

Returns: A task that completes when the message has been persisted to the outbox.

Parameters:
- `message`: The message to stage for later delivery.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-ioutboxcatalog"></a>

### `IOutboxCatalog`

Exposes the outbox surfaces visible to the current runtime.

#### Declaration
```csharp
public interface IOutboxCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-ioutboxcatalog-outboxes"></a>

##### `Outboxes`

```csharp
IReadOnlyList<OutboxDescriptor> Outboxes { get; }
```

Gets all outbox surfaces visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-ioutboxcatalog-getbychannelid-system-string"></a>

##### `GetByChannelId`

```csharp
IReadOnlyList<OutboxDescriptor> GetByChannelId(string channelId)
```

Gets all outboxes that explicitly declare the requested channel identifier.

Returns: The matching outboxes, or an empty list when no outbox declares that channel.

Parameters:
- `channelId`: The channel identifier to filter by.

<a id="member-m-cephalon-abstractions-data-ioutboxcatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
OutboxDescriptor GetById(string outboxId)
```

Gets one outbox by its stable identifier.

Returns: The matching outbox, or `null` when it is not active.

Parameters:
- `outboxId`: The outbox identifier to resolve.

<a id="member-m-cephalon-abstractions-data-ioutboxcatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<OutboxDescriptor> GetByProvider(string provider)
```

Gets all outboxes backed by the requested provider identifier.

Returns: The matching outboxes, or an empty list when the provider contributes none.

Parameters:
- `provider`: The provider identifier to filter by.

<a id="member-m-cephalon-abstractions-data-ioutboxcatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<OutboxDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all outboxes contributed by the requested module.

Returns: The matching outboxes, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="type-cephalon-abstractions-data-ioutboxcontributor"></a>

### `IOutboxContributor`

Contributes one or more outbox descriptors to the active runtime.

#### Declaration
```csharp
public interface IOutboxContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-data-ioutboxcontributor-registeroutboxes-cephalon-abstractions-data-ioutboxregistry"></a>

##### `RegisterOutboxes`

```csharp
void RegisterOutboxes(IOutboxRegistry outboxes)
```

Registers one or more outbox descriptors with the supplied registry.

Parameters:
- `outboxes`: The registry that collects contributed outbox descriptors.

<a id="type-cephalon-abstractions-data-ioutboxdispatchpolicycatalog"></a>

### `IOutboxDispatchPolicyCatalog`

Exposes the effective dispatch-execution policies visible to the current runtime for active outbox surfaces.

#### Declaration
```csharp
public interface IOutboxDispatchPolicyCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-ioutboxdispatchpolicycatalog-policies"></a>

##### `Policies`

```csharp
IReadOnlyList<OutboxDispatchPolicyDescriptor> Policies { get; }
```

Gets the effective dispatch policies visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-ioutboxdispatchpolicycatalog-getbyoutboxid-system-string"></a>

##### `GetByOutboxId`

```csharp
OutboxDispatchPolicyDescriptor GetByOutboxId(string outboxId)
```

Gets the effective dispatch policy for one outbox by its stable identifier.

Returns: The matching dispatch policy, or `null` when the outbox is not active.

Parameters:
- `outboxId`: The stable outbox identifier to resolve.

<a id="type-cephalon-abstractions-data-ioutboxregistry"></a>

### `IOutboxRegistry`

Receives outbox descriptors contributed by active modules or packages.

#### Declaration
```csharp
public interface IOutboxRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-data-ioutboxregistry-add-cephalon-abstractions-data-outboxdescriptor"></a>

##### `Add`

```csharp
void Add(OutboxDescriptor outbox)
```

Adds an outbox to the current runtime composition.

Parameters:
- `outbox`: The outbox descriptor to register.

<a id="type-cephalon-abstractions-data-iprojectioncatalog"></a>

### `IProjectionCatalog`

Exposes the projections visible to the current runtime.

#### Declaration
```csharp
public interface IProjectionCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-data-iprojectioncatalog-projections"></a>

##### `Projections`

```csharp
IReadOnlyList<ProjectionDescriptor> Projections { get; }
```

Gets all projections visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-data-iprojectioncatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
ProjectionDescriptor GetById(string projectionId)
```

Gets one projection by its stable identifier.

Returns: The matching projection, or `null` when it is not active.

Parameters:
- `projectionId`: The projection identifier to resolve.

<a id="member-m-cephalon-abstractions-data-iprojectioncatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<ProjectionDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all projections contributed by the requested module.

Returns: The matching projections, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="member-m-cephalon-abstractions-data-iprojectioncatalog-getbytargetstore-system-string"></a>

##### `GetByTargetStore`

```csharp
IReadOnlyList<ProjectionDescriptor> GetByTargetStore(string targetStoreId)
```

Gets all projections that target the requested store identifier.

Returns: The matching projections, or an empty list when no projection targets the store.

Parameters:
- `targetStoreId`: The target store identifier to filter by.

<a id="type-cephalon-abstractions-data-iprojectioncontributor"></a>

### `IProjectionContributor`

Contributes one or more projection descriptors to the active runtime.

#### Declaration
```csharp
public interface IProjectionContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-data-iprojectioncontributor-registerprojections-cephalon-abstractions-data-iprojectionregistry"></a>

##### `RegisterProjections`

```csharp
void RegisterProjections(IProjectionRegistry projections)
```

Registers one or more projection descriptors with the supplied registry.

Parameters:
- `projections`: The registry that collects contributed projection descriptors.

<a id="type-cephalon-abstractions-data-iprojectionregistry"></a>

### `IProjectionRegistry`

Receives projection descriptors contributed by active modules or packages.

#### Declaration
```csharp
public interface IProjectionRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-data-iprojectionregistry-add-cephalon-abstractions-data-projectiondescriptor"></a>

##### `Add`

```csharp
void Add(ProjectionDescriptor projection)
```

Adds a projection to the current runtime composition.

Parameters:
- `projection`: The projection descriptor to register.

<a id="type-cephalon-abstractions-data-iprojection-tmessage"></a>

### `IProjection<TMessage>`

Applies one message, event, or record to a projection target.

#### Declaration
```csharp
public interface IProjection<TMessage>
```

#### Methods

<a id="member-m-cephalon-abstractions-data-iprojection-1-projectasync-0-system-threading-cancellationtoken"></a>

##### `ProjectAsync`

```csharp
ValueTask ProjectAsync(TMessage message, CancellationToken cancellationToken)
```

Projects the supplied message into the target read model or data view.

Returns: A task that completes when the projection has finished applying the message.

Parameters:
- `message`: The message to project.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-iqueryhandler-tquery-tresult"></a>

### `IQueryHandler<TQuery, TResult>`

Handles a read-side request and returns the requested result.

#### Declaration
```csharp
public interface IQueryHandler<TQuery, TResult>
```

#### Methods

<a id="member-m-cephalon-abstractions-data-iqueryhandler-2-handleasync-0-system-threading-cancellationtoken"></a>

##### `HandleAsync`

```csharp
ValueTask<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
```

Handles the supplied query.

Returns: A task that completes with the result produced by the query.

Parameters:
- `query`: The query to execute.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-iquery-tresult"></a>

### `IQuery<TResult>`

Marks a request that should execute on the read side of a Cephalon application.

#### Declaration
```csharp
public interface IQuery<TResult>
```

<a id="type-cephalon-abstractions-data-ireadstore"></a>

### `IReadStore`

Executes read-side requests against the active data implementation.

#### Declaration
```csharp
public interface IReadStore
```

#### Methods

<a id="member-m-cephalon-abstractions-data-ireadstore-executeasync-1-cephalon-abstractions-data-iquery-0-system-threading-cancellationtoken"></a>

##### `ExecuteAsync`

```csharp
ValueTask<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
```

Executes the supplied query on the read side.

Returns: A task that completes with the requested result.

Type parameters:
- `TResult`: The result type returned by the query.

Parameters:
- `query`: The query to execute.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-iwritestore"></a>

### `IWriteStore`

Executes write-side requests against the active data implementation.

#### Declaration
```csharp
public interface IWriteStore
```

#### Methods

<a id="member-m-cephalon-abstractions-data-iwritestore-executeasync-cephalon-abstractions-data-icommand-system-threading-cancellationtoken"></a>

##### `ExecuteAsync`

```csharp
ValueTask ExecuteAsync(ICommand command, CancellationToken cancellationToken)
```

Executes the supplied command on the write side.

Returns: A task that completes when the command has finished running.

Parameters:
- `command`: The command to execute.
- `cancellationToken`: The token that cancels the operation.

<a id="member-m-cephalon-abstractions-data-iwritestore-executeasync-1-cephalon-abstractions-data-icommand-0-system-threading-cancellationtoken"></a>

##### `ExecuteAsync`

```csharp
ValueTask<TResult> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
```

Executes the supplied command on the write side and returns the resulting value.

Returns: A task that completes with the result produced by the command.

Type parameters:
- `TResult`: The result type returned by the command.

Parameters:
- `command`: The command to execute.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-data-outboxdescriptor"></a>

### `OutboxDescriptor`

Describes one outbox surface contributed to the active runtime.

#### Declaration
```csharp
public sealed class OutboxDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-outboxdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-cephalon-abstractions-data-outboxdispatchpolicydescriptor"></a>

##### `OutboxDescriptor`

```csharp
OutboxDescriptor(string id, string displayName, string description, string sourceModuleId, string provider, string mode, IReadOnlyList<string> channelIds, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata, OutboxDispatchPolicyDescriptor dispatchPolicy)
```

Creates a new outbox descriptor.

Parameters:
- `id`: The stable outbox identifier.
- `displayName`: The operator-facing outbox name.
- `description`: The human-readable outbox description.
- `sourceModuleId`: The module identifier that owns the outbox surface.
- `provider`: The logical provider identifier that backs the outbox.
- `mode`: The outbox mode such as `transactional-table` or `append-only-log`.
- `channelIds`: Optional channel identifiers that this outbox is explicitly scoped to.
- `tags`: Optional descriptive tags associated with the outbox.
- `metadata`: Optional operator-facing metadata associated with the outbox.
- `dispatchPolicy`: The optional effective dispatch-execution policy. When omitted, Cephalon defaults the outbox to a disabled dispatch policy.

#### Properties

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-channelids"></a>

##### `ChannelIds`

```csharp
IReadOnlyList<string> ChannelIds { get; }
```

Gets the optional channel identifiers that this outbox is explicitly scoped to.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable outbox description.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-dispatchpolicy"></a>

##### `DispatchPolicy`

```csharp
OutboxDispatchPolicyDescriptor DispatchPolicy { get; }
```

Gets the effective dispatch-execution policy for the outbox.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing outbox name.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable outbox identifier.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the outbox.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-mode"></a>

##### `Mode`

```csharp
string Mode { get; }
```

Gets the outbox mode.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the logical provider identifier that backs the outbox.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that owns the outbox surface.

<a id="member-p-cephalon-abstractions-data-outboxdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the outbox.

#### Methods

<a id="member-m-cephalon-abstractions-data-outboxdescriptor-withdispatchpolicy-cephalon-abstractions-data-outboxdispatchpolicydescriptor"></a>

##### `WithDispatchPolicy`

```csharp
OutboxDescriptor WithDispatchPolicy(OutboxDispatchPolicyDescriptor dispatchPolicy)
```

Creates a copy of the outbox descriptor with a different dispatch policy.

Returns: A new outbox descriptor with the requested dispatch policy.

Parameters:
- `dispatchPolicy`: The effective dispatch policy to apply.

<a id="type-cephalon-abstractions-data-outboxdispatchpolicydescriptor"></a>

### `OutboxDispatchPolicyDescriptor`

Describes the active dispatch-execution policy for one durable outbox surface.

#### Declaration
```csharp
public sealed class OutboxDispatchPolicyDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-outboxdispatchpolicydescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `OutboxDispatchPolicyDescriptor`

```csharp
OutboxDispatchPolicyDescriptor(string outboxId, string policyId, string displayName, string description, string executionMode, string runtimeId, IReadOnlyDictionary<string, string> metadata)
```

Creates a new outbox dispatch-policy descriptor.

Parameters:
- `outboxId`: The stable outbox identifier that the policy applies to.
- `policyId`: The stable dispatch-policy identifier.
- `displayName`: The operator-facing dispatch-policy name.
- `description`: The human-readable dispatch-policy description.
- `executionMode`: The execution ownership mode, such as `disabled`, `consumer-managed`, or `runtime-managed`.
- `runtimeId`: The optional dispatch-runtime identifier that explicitly owns execution for the outbox when the policy is runtime-managed.
- `metadata`: Optional operator-facing metadata associated with the dispatch policy.

#### Properties

<a id="member-p-cephalon-abstractions-data-outboxdispatchpolicydescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable dispatch-policy description.

<a id="member-p-cephalon-abstractions-data-outboxdispatchpolicydescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing dispatch-policy name.

<a id="member-p-cephalon-abstractions-data-outboxdispatchpolicydescriptor-executionmode"></a>

##### `ExecutionMode`

```csharp
string ExecutionMode { get; }
```

Gets the execution ownership mode for the outbox.

<a id="member-p-cephalon-abstractions-data-outboxdispatchpolicydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the dispatch policy.

<a id="member-p-cephalon-abstractions-data-outboxdispatchpolicydescriptor-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; }
```

Gets the stable outbox identifier that the policy applies to.

<a id="member-p-cephalon-abstractions-data-outboxdispatchpolicydescriptor-policyid"></a>

##### `PolicyId`

```csharp
string PolicyId { get; }
```

Gets the stable dispatch-policy identifier.

<a id="member-p-cephalon-abstractions-data-outboxdispatchpolicydescriptor-runtimeid"></a>

##### `RuntimeId`

```csharp
string RuntimeId { get; }
```

Gets the optional dispatch-runtime identifier that explicitly owns execution for the outbox.

#### Methods

<a id="member-m-cephalon-abstractions-data-outboxdispatchpolicydescriptor-disabled-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Disabled`

```csharp
OutboxDispatchPolicyDescriptor Disabled(string outboxId, IReadOnlyDictionary<string, string> metadata)
```

Creates the default disabled dispatch policy for an outbox.

Returns: The default disabled dispatch policy descriptor.

Parameters:
- `outboxId`: The stable outbox identifier.
- `metadata`: Optional operator-facing metadata associated with the policy.

<a id="member-m-cephalon-abstractions-data-outboxdispatchpolicydescriptor-unsupported-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Unsupported`

```csharp
OutboxDispatchPolicyDescriptor Unsupported(string outboxId, string description, IReadOnlyDictionary<string, string> metadata)
```

Creates an explicit unsupported dispatch policy for an outbox that can stage messages but does not currently support Cephalon-managed mutable dispatch-state ownership.

Returns: The explicit unsupported dispatch policy descriptor.

Parameters:
- `outboxId`: The stable outbox identifier.
- `description`: An optional operator-facing description explaining why the current provider intentionally remains outside the managed-dispatch contract.
- `metadata`: Optional operator-facing metadata associated with the policy.

<a id="type-cephalon-abstractions-data-outboxmessage"></a>

### `OutboxMessage`

Describes one message staged for later delivery through an outbox implementation.

#### Declaration
```csharp
public sealed class OutboxMessage
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-outboxmessage-ctor-system-string-system-string-system-string-system-string-system-datetimeoffset-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `OutboxMessage`

```csharp
OutboxMessage(string id, string channelId, string messageType, string payload, DateTimeOffset occurredAtUtc, string contentType, string correlationId, string tenantId, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> metadata)
```

Creates a new outbox message.

Parameters:
- `id`: The stable outbox message identifier.
- `channelId`: The logical channel or destination identifier.
- `messageType`: The logical message type identifier.
- `payload`: The serialized payload that should be delivered later.
- `occurredAtUtc`: The time at which the message became visible to the outbox.
- `contentType`: The payload content type when one is known.
- `correlationId`: The correlation identifier associated with the message.
- `tenantId`: The tenant identifier associated with the message.
- `headers`: Optional message headers.
- `metadata`: Optional message metadata.

#### Properties

<a id="member-p-cephalon-abstractions-data-outboxmessage-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; }
```

Gets the logical channel or destination identifier.

<a id="member-p-cephalon-abstractions-data-outboxmessage-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; }
```

Gets the payload content type when one is known.

<a id="member-p-cephalon-abstractions-data-outboxmessage-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier associated with the message.

<a id="member-p-cephalon-abstractions-data-outboxmessage-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; }
```

Gets message headers associated with the message.

<a id="member-p-cephalon-abstractions-data-outboxmessage-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable outbox message identifier.

<a id="member-p-cephalon-abstractions-data-outboxmessage-messagetype"></a>

##### `MessageType`

```csharp
string MessageType { get; }
```

Gets the logical message type identifier.

<a id="member-p-cephalon-abstractions-data-outboxmessage-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets message metadata associated with the message.

<a id="member-p-cephalon-abstractions-data-outboxmessage-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; }
```

Gets the time at which the message became visible to the outbox.

<a id="member-p-cephalon-abstractions-data-outboxmessage-payload"></a>

##### `Payload`

```csharp
string Payload { get; }
```

Gets the serialized payload that should be delivered later.

<a id="member-p-cephalon-abstractions-data-outboxmessage-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the message.

<a id="type-cephalon-abstractions-data-projectiondescriptor"></a>

### `ProjectionDescriptor`

Describes one projection surface contributed to the active runtime.

#### Declaration
```csharp
public sealed class ProjectionDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-data-projectiondescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ProjectionDescriptor`

```csharp
ProjectionDescriptor(string id, string displayName, string description, string sourceModuleId, string targetStoreId, string mode, IReadOnlyList<string> sourceContracts, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new projection descriptor.

Parameters:
- `id`: The stable projection identifier.
- `displayName`: The operator-facing projection name.
- `description`: The human-readable projection description.
- `sourceModuleId`: The module identifier that owns the projection.
- `targetStoreId`: The logical target store or read-model identifier populated by the projection.
- `mode`: The projection mode such as `synchronous`, `asynchronous`, or `rebuild`.
- `sourceContracts`: Optional source contracts that can feed the projection.
- `tags`: Optional descriptive tags associated with the projection.
- `metadata`: Optional operator-facing metadata associated with the projection.

#### Properties

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable projection description.

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing projection name.

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable projection identifier.

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the projection.

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-mode"></a>

##### `Mode`

```csharp
string Mode { get; }
```

Gets the projection mode.

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-sourcecontracts"></a>

##### `SourceContracts`

```csharp
IReadOnlyList<string> SourceContracts { get; }
```

Gets the optional source contracts that can feed the projection.

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that owns the projection.

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the projection.

<a id="member-p-cephalon-abstractions-data-projectiondescriptor-targetstoreid"></a>

##### `TargetStoreId`

```csharp
string TargetStoreId { get; }
```

Gets the logical target store or read-model identifier populated by the projection.

<a id="namespace-cephalon-abstractions-eventsourcing"></a>

## Namespace Cephalon.Abstractions.EventSourcing

<a id="type-cephalon-abstractions-eventsourcing-domainevent"></a>

### `DomainEvent`

Provides a minimal record base for immutable domain events.

#### Declaration
```csharp
public abstract class DomainEvent
```

#### Properties

<a id="member-p-cephalon-abstractions-eventsourcing-domainevent-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTime OccurredAtUtc { get; set; }
```

Gets the time at which the event occurred in UTC.

<a id="member-p-cephalon-abstractions-eventsourcing-domainevent-streamid"></a>

##### `StreamId`

```csharp
string StreamId { get; set; }
```

Gets the stable stream identifier that owns the event.

<a id="member-p-cephalon-abstractions-eventsourcing-domainevent-streamversion"></a>

##### `StreamVersion`

```csharp
long StreamVersion { get; set; }
```

Gets the optimistic stream version assigned to the event.

<a id="type-cephalon-abstractions-eventsourcing-eventstreamconcurrencyexception"></a>

### `EventStreamConcurrencyException`

Represents an optimistic concurrency failure while appending events to a stream.

#### Declaration
```csharp
public sealed class EventStreamConcurrencyException
```

#### Constructors

<a id="member-m-cephalon-abstractions-eventsourcing-eventstreamconcurrencyexception-ctor-system-string-system-int64-system-int64"></a>

##### `EventStreamConcurrencyException`

```csharp
EventStreamConcurrencyException(string streamId, long expectedVersion, long actualVersion)
```

Initializes a new instance of the `EventStreamConcurrencyException` class.

Parameters:
- `streamId`: The stream identifier that failed the concurrency check.
- `expectedVersion`: The version that the caller expected.
- `actualVersion`: The version that currently exists in the store.

#### Properties

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamconcurrencyexception-actualversion"></a>

##### `ActualVersion`

```csharp
long ActualVersion { get; }
```

Gets the version that currently exists in the store.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamconcurrencyexception-expectedversion"></a>

##### `ExpectedVersion`

```csharp
long ExpectedVersion { get; }
```

Gets the version that the caller expected.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamconcurrencyexception-streamid"></a>

##### `StreamId`

```csharp
string StreamId { get; }
```

Gets the stream identifier that failed the concurrency check.

<a id="type-cephalon-abstractions-eventsourcing-eventstreamdescriptor"></a>

### `EventStreamDescriptor`

Describes one logical event stream visible to the current runtime.

#### Declaration
```csharp
public sealed class EventStreamDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-eventsourcing-eventstreamdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `EventStreamDescriptor`

```csharp
EventStreamDescriptor(string id, string displayName, string description, string sourceModuleId, string provider, string mode, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of the `EventStreamDescriptor` class.

Parameters:
- `id`: The stable event-stream identifier.
- `displayName`: The operator-facing event-stream name.
- `description`: The human-readable event-stream description.
- `sourceModuleId`: The module identifier that owns the event stream.
- `provider`: The provider identifier that persists the stream.
- `mode`: The stream persistence mode. The default is `append-only`.
- `tags`: The descriptive tags associated with the stream.
- `metadata`: The provider-specific metadata associated with the stream.

#### Properties

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the normalized human-readable description.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the normalized operator-facing name.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the normalized event-stream identifier.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets the normalized provider-specific metadata associated with the stream.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamdescriptor-mode"></a>

##### `Mode`

```csharp
string Mode { get; }
```

Gets the normalized stream persistence mode.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamdescriptor-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the normalized provider identifier.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the normalized source module identifier.

<a id="member-p-cephalon-abstractions-eventsourcing-eventstreamdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized descriptive tags associated with the stream.

<a id="type-cephalon-abstractions-eventsourcing-iaggregate-tstate"></a>

### `IAggregate<TState>`

Applies domain events to an aggregate state projection.

#### Declaration
```csharp
public interface IAggregate<TState>
```

#### Methods

<a id="member-m-cephalon-abstractions-eventsourcing-iaggregate-1-apply-0-cephalon-abstractions-eventsourcing-idomainevent"></a>

##### `Apply`

```csharp
TState Apply(TState current, IDomainEvent evt)
```

Applies one event to the current state and returns the next state snapshot.

Returns: The updated aggregate state.

Parameters:
- `current`: The current aggregate state.
- `evt`: The event to apply.

<a id="type-cephalon-abstractions-eventsourcing-idomainevent"></a>

### `IDomainEvent`

Represents one immutable domain event stored in an append-only stream.

#### Declaration
```csharp
public interface IDomainEvent
```

#### Properties

<a id="member-p-cephalon-abstractions-eventsourcing-idomainevent-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTime OccurredAtUtc { get; }
```

Gets the time at which the event occurred in UTC.

<a id="member-p-cephalon-abstractions-eventsourcing-idomainevent-streamid"></a>

##### `StreamId`

```csharp
string StreamId { get; }
```

Gets the stable stream identifier that owns the event.

<a id="member-p-cephalon-abstractions-eventsourcing-idomainevent-streamversion"></a>

##### `StreamVersion`

```csharp
long StreamVersion { get; }
```

Gets the optimistic stream version assigned to the event.

<a id="type-cephalon-abstractions-eventsourcing-ieventstore"></a>

### `IEventStore`

Appends and replays immutable domain events for one logical event store.

#### Declaration
```csharp
public interface IEventStore
```

#### Methods

<a id="member-m-cephalon-abstractions-eventsourcing-ieventstore-appendasync-system-string-system-collections-generic-ireadonlycollection-cephalon-abstractions-eventsourcing-idomainevent-system-int64-system-threading-cancellationtoken"></a>

##### `AppendAsync`

```csharp
Task AppendAsync(string streamId, IReadOnlyCollection<IDomainEvent> events, long expectedVersion, CancellationToken cancellationToken)
```

Appends one or more events to the requested stream after checking the expected version.

Returns: A task that completes when the append finishes.

Parameters:
- `streamId`: The stable stream identifier.
- `events`: The events to append.
- `expectedVersion`: The current stream version expected by the caller. Use `-1` to require a brand-new stream.
- `cancellationToken`: The token that cancels the operation.

<a id="member-m-cephalon-abstractions-eventsourcing-ieventstore-getversionasync-system-string-system-threading-cancellationtoken"></a>

##### `GetVersionAsync`

```csharp
Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken)
```

Gets the latest version known for the requested stream.

Returns: A task that returns the current stream version, or `-1` when the stream does not exist.

Parameters:
- `streamId`: The stable stream identifier.
- `cancellationToken`: The token that cancels the operation.

<a id="member-m-cephalon-abstractions-eventsourcing-ieventstore-readstreamasync-system-string-system-int64-system-threading-cancellationtoken"></a>

##### `ReadStreamAsync`

```csharp
IAsyncEnumerable<IDomainEvent> ReadStreamAsync(string streamId, long fromVersion, CancellationToken cancellationToken)
```

Reads the requested stream from the supplied version onward.

Returns: An async sequence of domain events in ascending stream-version order.

Parameters:
- `streamId`: The stable stream identifier.
- `fromVersion`: The first stream version to include. The default is `0`.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-eventsourcing-ieventstorecatalog"></a>

### `IEventStoreCatalog`

Exposes the event-stream surfaces visible to the current runtime.

#### Declaration
```csharp
public interface IEventStoreCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-eventsourcing-ieventstorecatalog-all"></a>

##### `All`

```csharp
IReadOnlyList<EventStreamDescriptor> All { get; }
```

Gets all event-stream descriptors visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-eventsourcing-ieventstorecatalog-findbyid-system-string"></a>

##### `FindById`

```csharp
EventStreamDescriptor FindById(string id)
```

Finds one event stream by its stable identifier.

Returns: The matching event stream, or `null` when it is not active.

Parameters:
- `id`: The event-stream identifier to resolve.

<a id="member-m-cephalon-abstractions-eventsourcing-ieventstorecatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<EventStreamDescriptor> GetByProvider(string provider)
```

Gets all event streams backed by the requested provider identifier.

Returns: The matching event streams, or an empty list when the provider contributes none.

Parameters:
- `provider`: The provider identifier to filter by.

<a id="type-cephalon-abstractions-eventsourcing-ieventstorecontributor"></a>

### `IEventStoreContributor`

Contributes one or more event-stream descriptors to the active runtime.

#### Declaration
```csharp
public interface IEventStoreContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-eventsourcing-ieventstorecontributor-contribute"></a>

##### `Contribute`

```csharp
IReadOnlyList<EventStreamDescriptor> Contribute()
```

Returns the event-stream descriptors contributed by the current module or package.

Returns: The contributed event-stream descriptors.

<a id="type-cephalon-abstractions-eventsourcing-ieventstoreregistry"></a>

### `IEventStoreRegistry`

Receives event-stream descriptors contributed by active modules or packages.

#### Declaration
```csharp
public interface IEventStoreRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-eventsourcing-ieventstoreregistry-register-cephalon-abstractions-eventsourcing-eventstreamdescriptor"></a>

##### `Register`

```csharp
void Register(EventStreamDescriptor descriptor)
```

Registers one event stream with the current runtime composition.

Parameters:
- `descriptor`: The event-stream descriptor to register.

<a id="type-cephalon-abstractions-eventsourcing-isnapshotstore"></a>

### `ISnapshotStore`

Persists and rehydrates optional aggregate snapshots for event-sourced workloads.

#### Declaration
```csharp
public interface ISnapshotStore
```

#### Methods

<a id="member-m-cephalon-abstractions-eventsourcing-isnapshotstore-loadsnapshotasync-1-system-string-system-threading-cancellationtoken"></a>

##### `LoadSnapshotAsync`

```csharp
Task<ValueTuple<TState, long>> LoadSnapshotAsync<TState>(string streamId, CancellationToken cancellationToken)
```

Loads the latest snapshot for the requested stream.

Returns: A task that returns the snapshot state and version, or the default state and `-1` when none exists.

Type parameters:
- `TState`: The aggregate state type.

Parameters:
- `streamId`: The stable stream identifier.
- `cancellationToken`: The token that cancels the operation.

<a id="member-m-cephalon-abstractions-eventsourcing-isnapshotstore-savesnapshotasync-1-system-string-system-int64-0-system-threading-cancellationtoken"></a>

##### `SaveSnapshotAsync`

```csharp
Task SaveSnapshotAsync<TState>(string streamId, long version, TState state, CancellationToken cancellationToken)
```

Saves one snapshot for the requested stream.

Returns: A task that completes when the snapshot has been persisted.

Type parameters:
- `TState`: The aggregate state type.

Parameters:
- `streamId`: The stable stream identifier.
- `version`: The stream version represented by the snapshot.
- `state`: The state payload to persist.
- `cancellationToken`: The token that cancels the operation.

<a id="namespace-cephalon-abstractions-execution"></a>

## Namespace Cephalon.Abstractions.Execution

<a id="type-cephalon-abstractions-execution-durableexecutioncompensationaction"></a>

### `DurableExecutionCompensationAction`

Describes one operator-facing durable-execution compensation action available for a workflow stream.

#### Declaration
```csharp
public sealed class DurableExecutionCompensationAction
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-durableexecutioncompensationaction-ctor-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `DurableExecutionCompensationAction`

```csharp
DurableExecutionCompensationAction(string id, string displayName, string description, string triggerKind, string compensationBehaviorId, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of the `DurableExecutionCompensationAction` class.

Parameters:
- `id`: The stable compensation-action identifier within the durable workflow.
- `displayName`: The operator-facing compensation-action name.
- `description`: A human-readable description of what the compensation action does.
- `triggerKind`: The operator-facing trigger kind for the compensation action, such as `manual` or `on-failure`.
- `compensationBehaviorId`: The stable behavior identifier to invoke when the compensation action maps to another Cephalon behavior.
- `metadata`: Additional operator-facing metadata describing the compensation action.

#### Properties

<a id="member-p-cephalon-abstractions-execution-durableexecutioncompensationaction-compensationbehaviorid"></a>

##### `CompensationBehaviorId`

```csharp
string CompensationBehaviorId { get; }
```

Gets the stable behavior identifier to invoke when the compensation action maps to another Cephalon behavior.

<a id="member-p-cephalon-abstractions-execution-durableexecutioncompensationaction-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable compensation-action description when one was supplied.

<a id="member-p-cephalon-abstractions-execution-durableexecutioncompensationaction-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing compensation-action name.

<a id="member-p-cephalon-abstractions-execution-durableexecutioncompensationaction-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable compensation-action identifier within the durable workflow.

<a id="member-p-cephalon-abstractions-execution-durableexecutioncompensationaction-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional operator-facing metadata describing the compensation action.

<a id="member-p-cephalon-abstractions-execution-durableexecutioncompensationaction-triggerkind"></a>

##### `TriggerKind`

```csharp
string TriggerKind { get; }
```

Gets the operator-facing trigger kind for the compensation action.

<a id="type-cephalon-abstractions-execution-durableexecutionpendingsignal"></a>

### `DurableExecutionPendingSignal`

Describes one durable-execution signal that is currently pending for a workflow stream.

#### Declaration
```csharp
public sealed class DurableExecutionPendingSignal
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-durableexecutionpendingsignal-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `DurableExecutionPendingSignal`

```csharp
DurableExecutionPendingSignal(string id, string displayName, string description, string payloadType, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of the `DurableExecutionPendingSignal` class.

Parameters:
- `id`: The stable signal identifier within the durable workflow.
- `displayName`: The operator-facing signal name.
- `description`: A human-readable description of why the signal is awaited.
- `payloadType`: The expected payload type name for the awaited signal when one is known.
- `metadata`: Additional operator-facing metadata describing the signal.

#### Properties

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingsignal-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable signal description when one was supplied.

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingsignal-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing signal name.

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingsignal-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable signal identifier within the durable workflow.

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingsignal-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional operator-facing metadata describing the signal.

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingsignal-payloadtype"></a>

##### `PayloadType`

```csharp
string PayloadType { get; }
```

Gets the expected payload type name when the awaited signal declares one.

<a id="type-cephalon-abstractions-execution-durableexecutionpendingtimer"></a>

### `DurableExecutionPendingTimer`

Describes one durable-execution timer that is currently pending for a workflow stream.

#### Declaration
```csharp
public sealed class DurableExecutionPendingTimer
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-durableexecutionpendingtimer-ctor-system-string-system-datetimeoffset-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `DurableExecutionPendingTimer`

```csharp
DurableExecutionPendingTimer(string id, DateTimeOffset dueAtUtc, string displayName, string description, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of the `DurableExecutionPendingTimer` class.

Parameters:
- `id`: The stable timer identifier within the durable workflow.
- `dueAtUtc`: The UTC timestamp when the timer is next due.
- `displayName`: The operator-facing timer name.
- `description`: A human-readable description of why the timer is pending.
- `metadata`: Additional operator-facing metadata describing the timer.

#### Properties

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingtimer-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable timer description when one was supplied.

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingtimer-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing timer name.

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingtimer-dueatutc"></a>

##### `DueAtUtc`

```csharp
DateTimeOffset DueAtUtc { get; }
```

Gets the UTC timestamp when the timer is next due.

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingtimer-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable timer identifier within the durable workflow.

<a id="member-p-cephalon-abstractions-execution-durableexecutionpendingtimer-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional operator-facing metadata describing the timer.

<a id="type-cephalon-abstractions-execution-durableexecutionruntimedescriptor"></a>

### `DurableExecutionRuntimeDescriptor`

Describes one active durable-execution workflow visible to the current runtime.

Remarks: This runtime-facing surface keeps durable workflow truth derived from the shared behavior topology and registered implementation types instead of inventing a host-only workflow registry. It is intentionally static and operator-facing: it describes the active durable contract shape, ownership, transports, and replay semantics rather than per-invocation state.

#### Declaration
```csharp
public sealed class DurableExecutionRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-durableexecutionruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-boolean-system-boolean-system-collections-generic-ireadonlylist-system-int32-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `DurableExecutionRuntimeDescriptor`

```csharp
DurableExecutionRuntimeDescriptor(string id, string displayName, string description, string behaviorType, string inputType, string stateType, string outputType, string executionMode, string sourceModuleId, IReadOnlyList<string> transportIds, IReadOnlyList<string> requiredFeatureFlagIds, bool eventSourcingEnabled, bool requiresEventStore, IReadOnlyList<int> successStatusCodes, IReadOnlyDictionary<string, string> metadata)
```

Creates a durable-execution runtime descriptor.

Parameters:
- `id`: The stable durable behavior identifier.
- `displayName`: The operator-facing durable workflow name.
- `description`: A human-readable description of the durable workflow.
- `behaviorType`: The concrete durable behavior implementation type name.
- `inputType`: The durable workflow input type name.
- `stateType`: The durable workflow replay-state type name.
- `outputType`: The durable workflow local output type name.
- `executionMode`: The replay mode used by the runtime, such as `event-store-replay`.
- `sourceModuleId`: The owning module identifier when the workflow came from an explicit module-owned behavior.
- `transportIds`: The transport identifiers that expose the durable workflow.
- `requiredFeatureFlagIds`: The ordered feature-flag identifiers that must resolve to enabled before the workflow can execute.
- `eventSourcingEnabled`: Indicates whether the authored behavior topology explicitly enables event sourcing for the workflow.
- `requiresEventStore`: Indicates whether the runtime contract requires an `IEventStore` to execute truthfully.
- `successStatusCodes`: The HTTP success status codes the shared durable execution strategy can return for local output, continuation-only work, pending timer/signal coordination, or completion without output.
- `metadata`: Additional operator-facing metadata describing replay semantics.

#### Properties

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-behaviortype"></a>

##### `BehaviorType`

```csharp
string BehaviorType { get; }
```

Gets the concrete durable behavior implementation type name.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable durable workflow description.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing durable workflow name.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-eventsourcingenabled"></a>

##### `EventSourcingEnabled`

```csharp
bool EventSourcingEnabled { get; }
```

Gets a value indicating whether the authored behavior topology explicitly enables event sourcing.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-executionmode"></a>

##### `ExecutionMode`

```csharp
string ExecutionMode { get; }
```

Gets the replay mode used by the active runtime.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable durable behavior identifier.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-inputtype"></a>

##### `InputType`

```csharp
string InputType { get; }
```

Gets the durable workflow input type name.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional operator-facing metadata describing replay semantics.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-outputtype"></a>

##### `OutputType`

```csharp
string OutputType { get; }
```

Gets the durable workflow local output type name.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-requiredfeatureflagids"></a>

##### `RequiredFeatureFlagIds`

```csharp
IReadOnlyList<string> RequiredFeatureFlagIds { get; }
```

Gets the ordered feature-flag identifiers that gate workflow execution.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-requireseventstore"></a>

##### `RequiresEventStore`

```csharp
bool RequiresEventStore { get; }
```

Gets a value indicating whether the runtime contract requires an `IEventStore`.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the owning module identifier when one is known at runtime.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-statetype"></a>

##### `StateType`

```csharp
string StateType { get; }
```

Gets the durable workflow replay-state type name.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-successstatuscodes"></a>

##### `SuccessStatusCodes`

```csharp
IReadOnlyList<int> SuccessStatusCodes { get; }
```

Gets the HTTP success status codes the shared durable strategy can return.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimedescriptor-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; }
```

Gets the transport identifiers that expose the durable workflow.

<a id="type-cephalon-abstractions-execution-durableexecutionruntimestate"></a>

### `DurableExecutionRuntimeState`

Describes the latest operator-facing runtime state reported for one durable-execution stream.

#### Declaration
```csharp
public sealed class DurableExecutionRuntimeState
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-durableexecutionruntimestate-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-int64-system-nullable-system-int64-system-nullable-system-int32-system-int32-system-boolean-system-boolean-system-int32-system-int32-system-int32-system-int32-system-int32-system-collections-generic-ireadonlylist-cephalon-abstractions-execution-durableexecutionpendingtimer-system-collections-generic-ireadonlylist-cephalon-abstractions-execution-durableexecutionpendingsignal-system-collections-generic-ireadonlylist-cephalon-abstractions-execution-durableexecutioncompensationaction-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `DurableExecutionRuntimeState`

```csharp
DurableExecutionRuntimeState(string BehaviorId, string StreamId, string SourceModuleId, IReadOnlyList<string> TransportIds, string LastOutcome, string LastStage, DateTimeOffset? LastObservedAtUtc, long? LastReplayedVersion, long? LastKnownVersion, int? LastHttpStatusCode, int LastAppendedEventCount, bool LastStepProducedOutput, bool LastStepCompleted, int StartedCount, int SucceededCount, int ContinuationCount, int CompletedCount, int FailedCount, IReadOnlyList<DurableExecutionPendingTimer> PendingTimers, IReadOnlyList<DurableExecutionPendingSignal> PendingSignals, IReadOnlyList<DurableExecutionCompensationAction> CompensationActions, string LastError, IReadOnlyDictionary<string, string> Metadata)
```

Describes the latest operator-facing runtime state reported for one durable-execution stream.

Parameters:
- `BehaviorId`: The stable durable behavior identifier that owns the stream.
- `StreamId`: The stable event-stream identifier reported by the durable workflow.
- `SourceModuleId`: The owning module identifier when one is known at runtime.
- `TransportIds`: The transport identifiers that expose the durable workflow.
- `LastOutcome`: The last reported durable-execution outcome identifier when one exists.
- `LastStage`: The last reported durable-execution stage identifier when one exists.
- `LastObservedAtUtc`: The UTC timestamp when the latest observation was reported.
- `LastReplayedVersion`: The latest stream version that was fully replayed before the durable step executed.
- `LastKnownVersion`: The latest stream version known after the reported durable step finished or failed.
- `LastHttpStatusCode`: The latest HTTP success status code returned by the durable execution strategy when one was reported.
- `LastAppendedEventCount`: The number of domain events appended by the latest successful durable step.
- `LastStepProducedOutput`: Indicates whether the latest successful durable step produced local output.
- `LastStepCompleted`: Indicates whether the latest reported durable step declared the workflow completed.
- `StartedCount`: The number of `started` observations reported so far.
- `SucceededCount`: The number of `succeeded` observations reported so far.
- `ContinuationCount`: The number of `continuation-staged` observations reported so far.
- `CompletedCount`: The number of `completed` observations reported so far.
- `FailedCount`: The number of `failed` observations reported so far.
- `PendingTimers`: The durable timers that are currently pending for this stream.
- `PendingSignals`: The durable signals that are currently awaited for this stream.
- `CompensationActions`: The operator-facing compensation actions currently available for this stream.
- `LastError`: The latest operator-facing error summary when the durable step reported a failure.
- `Metadata`: The operator-facing metadata captured by the latest report.

#### Properties

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; set; }
```

The stable durable behavior identifier that owns the stream.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-compensationactions"></a>

##### `CompensationActions`

```csharp
IReadOnlyList<DurableExecutionCompensationAction> CompensationActions { get; set; }
```

The operator-facing compensation actions currently available for this stream.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-completedcount"></a>

##### `CompletedCount`

```csharp
int CompletedCount { get; set; }
```

The number of `completed` observations reported so far.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-continuationcount"></a>

##### `ContinuationCount`

```csharp
int ContinuationCount { get; set; }
```

The number of `continuation-staged` observations reported so far.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-continuationpending"></a>

##### `ContinuationPending`

```csharp
bool ContinuationPending { get; }
```

Gets a value indicating whether the latest report says the workflow still has continuation work pending.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-coordinationpending"></a>

##### `CoordinationPending`

```csharp
bool CoordinationPending { get; }
```

Gets a value indicating whether the latest runtime state still has pending continuation, timer, or signal work.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; set; }
```

The number of `failed` observations reported so far.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-hascompensationactions"></a>

##### `HasCompensationActions`

```csharp
bool HasCompensationActions { get; }
```

Gets a value indicating whether one or more operator-facing compensation actions are currently available for the stream.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-haspendingsignals"></a>

##### `HasPendingSignals`

```csharp
bool HasPendingSignals { get; }
```

Gets a value indicating whether one or more durable signals are currently awaited for the stream.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-haspendingtimers"></a>

##### `HasPendingTimers`

```csharp
bool HasPendingTimers { get; }
```

Gets a value indicating whether one or more durable timers are currently pending for the stream.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-isfailed"></a>

##### `IsFailed`

```csharp
bool IsFailed { get; }
```

Gets a value indicating whether the latest report says the durable stream is currently in a failed posture.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-lastappendedeventcount"></a>

##### `LastAppendedEventCount`

```csharp
int LastAppendedEventCount { get; set; }
```

The number of domain events appended by the latest successful durable step.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; set; }
```

The latest operator-facing error summary when the durable step reported a failure.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-lasthttpstatuscode"></a>

##### `LastHttpStatusCode`

```csharp
int? LastHttpStatusCode { get; set; }
```

The latest HTTP success status code returned by the durable execution strategy when one was reported.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-lastknownversion"></a>

##### `LastKnownVersion`

```csharp
long? LastKnownVersion { get; set; }
```

The latest stream version known after the reported durable step finished or failed.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the latest observation was reported.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; set; }
```

The last reported durable-execution outcome identifier when one exists.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-lastreplayedversion"></a>

##### `LastReplayedVersion`

```csharp
long? LastReplayedVersion { get; set; }
```

The latest stream version that was fully replayed before the durable step executed.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-laststage"></a>

##### `LastStage`

```csharp
string LastStage { get; set; }
```

The last reported durable-execution stage identifier when one exists.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-laststepcompleted"></a>

##### `LastStepCompleted`

```csharp
bool LastStepCompleted { get; set; }
```

Indicates whether the latest reported durable step declared the workflow completed.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-laststepproducedoutput"></a>

##### `LastStepProducedOutput`

```csharp
bool LastStepProducedOutput { get; set; }
```

Indicates whether the latest successful durable step produced local output.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

The operator-facing metadata captured by the latest report.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-nexttimerdueatutc"></a>

##### `NextTimerDueAtUtc`

```csharp
DateTimeOffset? NextTimerDueAtUtc { get; }
```

Gets the earliest UTC due timestamp across the currently pending timers when one exists.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-pendingsignals"></a>

##### `PendingSignals`

```csharp
IReadOnlyList<DurableExecutionPendingSignal> PendingSignals { get; set; }
```

The durable signals that are currently awaited for this stream.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-pendingtimers"></a>

##### `PendingTimers`

```csharp
IReadOnlyList<DurableExecutionPendingTimer> PendingTimers { get; set; }
```

The durable timers that are currently pending for this stream.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

The owning module identifier when one is known at runtime.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-startedcount"></a>

##### `StartedCount`

```csharp
int StartedCount { get; set; }
```

The number of `started` observations reported so far.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-streamid"></a>

##### `StreamId`

```csharp
string StreamId { get; set; }
```

The stable event-stream identifier reported by the durable workflow.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-succeededcount"></a>

##### `SucceededCount`

```csharp
int SucceededCount { get; set; }
```

The number of `succeeded` observations reported so far.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-totalreports"></a>

##### `TotalReports`

```csharp
int TotalReports { get; }
```

Gets the total number of observations reported for this durable-execution stream.

<a id="member-p-cephalon-abstractions-execution-durableexecutionruntimestate-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; set; }
```

The transport identifiers that expose the durable workflow.

<a id="type-cephalon-abstractions-execution-executiongraphdescriptor"></a>

### `ExecutionGraphDescriptor`

Describes one operator-facing execution graph contributed by an active module.

#### Declaration
```csharp
public sealed class ExecutionGraphDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-executiongraphdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-execution-executiongraphnodedescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-execution-executiongraphedgedescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ExecutionGraphDescriptor`

```csharp
ExecutionGraphDescriptor(string id, string displayName, string description, string sourceModuleId, string entryNodeId, IReadOnlyList<ExecutionGraphNodeDescriptor> nodes, IReadOnlyList<ExecutionGraphEdgeDescriptor> edges, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new execution graph descriptor.

Parameters:
- `id`: The stable execution-graph identifier.
- `displayName`: The operator-facing execution-graph name.
- `description`: A human-readable description of the graph.
- `sourceModuleId`: The module identifier that owns the graph.
- `entryNodeId`: The node identifier where execution should begin.
- `nodes`: The nodes that participate in the graph.
- `edges`: The directed edges that connect the graph nodes.
- `tags`: Optional descriptive tags associated with the graph.
- `metadata`: Optional operator-facing metadata associated with the graph.

#### Properties

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the graph.

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing execution-graph name.

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-edges"></a>

##### `Edges`

```csharp
IReadOnlyList<ExecutionGraphEdgeDescriptor> Edges { get; }
```

Gets the directed edges that connect graph nodes.

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-entrynodeid"></a>

##### `EntryNodeId`

```csharp
string EntryNodeId { get; }
```

Gets the node identifier where execution should begin.

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable execution-graph identifier.

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the graph.

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-nodes"></a>

##### `Nodes`

```csharp
IReadOnlyList<ExecutionGraphNodeDescriptor> Nodes { get; }
```

Gets the nodes that participate in the graph.

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that contributed the graph.

<a id="member-p-cephalon-abstractions-execution-executiongraphdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the graph.

<a id="type-cephalon-abstractions-execution-executiongraphedgedescriptor"></a>

### `ExecutionGraphEdgeDescriptor`

Describes one directed edge within an execution graph.

#### Declaration
```csharp
public sealed class ExecutionGraphEdgeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-executiongraphedgedescriptor-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ExecutionGraphEdgeDescriptor`

```csharp
ExecutionGraphEdgeDescriptor(string fromNodeId, string toNodeId, string displayName, string condition, IReadOnlyDictionary<string, string> metadata)
```

Creates a new execution-graph edge descriptor.

Parameters:
- `fromNodeId`: The source node identifier.
- `toNodeId`: The destination node identifier.
- `displayName`: An optional operator-facing label for the edge.
- `condition`: An optional condition or routing hint associated with the edge.
- `metadata`: Optional operator-facing metadata associated with the edge.

#### Properties

<a id="member-p-cephalon-abstractions-execution-executiongraphedgedescriptor-condition"></a>

##### `Condition`

```csharp
string Condition { get; }
```

Gets the optional condition or routing hint for the edge.

<a id="member-p-cephalon-abstractions-execution-executiongraphedgedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the optional operator-facing label for the edge.

<a id="member-p-cephalon-abstractions-execution-executiongraphedgedescriptor-fromnodeid"></a>

##### `FromNodeId`

```csharp
string FromNodeId { get; }
```

Gets the source node identifier.

<a id="member-p-cephalon-abstractions-execution-executiongraphedgedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata associated with the edge.

<a id="member-p-cephalon-abstractions-execution-executiongraphedgedescriptor-tonodeid"></a>

##### `ToNodeId`

```csharp
string ToNodeId { get; }
```

Gets the destination node identifier.

<a id="type-cephalon-abstractions-execution-executiongraphnodedescriptor"></a>

### `ExecutionGraphNodeDescriptor`

Describes one node within an execution graph.

#### Declaration
```csharp
public sealed class ExecutionGraphNodeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-executiongraphnodedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ExecutionGraphNodeDescriptor`

```csharp
ExecutionGraphNodeDescriptor(string id, string displayName, string description, string kind, string moduleId, string capabilityKey, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new execution-graph node descriptor.

Parameters:
- `id`: The stable node identifier within the graph.
- `displayName`: The operator-facing node name.
- `description`: A human-readable description of the node.
- `kind`: The node kind, such as `activity`, `decision`, or `wait`.
- `moduleId`: The module identifier that primarily owns the node, when different from the graph source.
- `capabilityKey`: The capability key the node intends to drive, when it maps to an existing capability contract.
- `tags`: Optional descriptive tags associated with the node.
- `metadata`: Optional operator-facing metadata associated with the node.

#### Properties

<a id="member-p-cephalon-abstractions-execution-executiongraphnodedescriptor-capabilitykey"></a>

##### `CapabilityKey`

```csharp
string CapabilityKey { get; }
```

Gets the capability key the node intends to drive, when one was declared.

<a id="member-p-cephalon-abstractions-execution-executiongraphnodedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the node.

<a id="member-p-cephalon-abstractions-execution-executiongraphnodedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing node name.

<a id="member-p-cephalon-abstractions-execution-executiongraphnodedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable node identifier within the graph.

<a id="member-p-cephalon-abstractions-execution-executiongraphnodedescriptor-kind"></a>

##### `Kind`

```csharp
string Kind { get; }
```

Gets the node kind.

<a id="member-p-cephalon-abstractions-execution-executiongraphnodedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the node.

<a id="member-p-cephalon-abstractions-execution-executiongraphnodedescriptor-moduleid"></a>

##### `ModuleId`

```csharp
string ModuleId { get; }
```

Gets the module identifier that primarily owns the node, when one was declared.

<a id="member-p-cephalon-abstractions-execution-executiongraphnodedescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the node.

<a id="type-cephalon-abstractions-execution-hostedexecutiondescriptor"></a>

### `HostedExecutionDescriptor`

Describes one operator-facing hosted or background execution surface contributed by an active module.

#### Declaration
```csharp
public sealed class HostedExecutionDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-hostedexecutiondescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `HostedExecutionDescriptor`

```csharp
HostedExecutionDescriptor(string id, string displayName, string description, string sourceModuleId, string kind, string executionGraphId, bool startsWithHost, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new hosted execution descriptor.

Parameters:
- `id`: The stable hosted-execution identifier.
- `displayName`: The operator-facing hosted-execution name.
- `description`: A human-readable description of the hosted execution.
- `sourceModuleId`: The module identifier that owns the hosted execution.
- `kind`: The operator-facing hosted-execution kind such as `background-service`, `timer`, or `listener`.
- `executionGraphId`: The related execution-graph identifier when this hosted execution drives one graph directly.
- `startsWithHost`: A value indicating whether the hosted execution is expected to become active when the runtime host starts.
- `tags`: Optional descriptive tags associated with the hosted execution.
- `metadata`: Optional operator-facing metadata associated with the hosted execution.

#### Properties

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the hosted execution.

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing hosted-execution name.

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-executiongraphid"></a>

##### `ExecutionGraphId`

```csharp
string ExecutionGraphId { get; }
```

Gets the related execution-graph identifier when one is declared.

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable hosted-execution identifier.

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-kind"></a>

##### `Kind`

```csharp
string Kind { get; }
```

Gets the operator-facing hosted-execution kind.

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata associated with the hosted execution.

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that contributed the hosted execution.

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-startswithhost"></a>

##### `StartsWithHost`

```csharp
bool StartsWithHost { get; }
```

Gets a value indicating whether the hosted execution is expected to become active when the runtime host starts.

<a id="member-p-cephalon-abstractions-execution-hostedexecutiondescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the hosted execution.

<a id="type-cephalon-abstractions-execution-idurableexecutionruntimecatalog"></a>

### `IDurableExecutionRuntimeCatalog`

Exposes the active durable-execution workflows visible to the current runtime.

#### Declaration
```csharp
public interface IDurableExecutionRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-execution-idurableexecutionruntimecatalog-durableexecutions"></a>

##### `DurableExecutions`

```csharp
IReadOnlyList<DurableExecutionRuntimeDescriptor> DurableExecutions { get; }
```

Gets all active durable-execution workflows visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
DurableExecutionRuntimeDescriptor GetById(string behaviorId)
```

Gets one durable-execution workflow by its stable behavior identifier.

Returns: The matching durable workflow descriptor, or `null` when it is not active.

Parameters:
- `behaviorId`: The durable behavior identifier to resolve.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<DurableExecutionRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all durable-execution workflows contributed by the requested module.

Returns: The matching durable workflows, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimecatalog-getbytransportid-system-string"></a>

##### `GetByTransportId`

```csharp
IReadOnlyList<DurableExecutionRuntimeDescriptor> GetByTransportId(string transportId)
```

Gets all durable-execution workflows exposed over the requested transport.

Returns: The matching durable workflows, or an empty list when none expose that transport.

Parameters:
- `transportId`: The stable transport identifier to filter by.

<a id="type-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog"></a>

### `IDurableExecutionRuntimeStateCatalog`

Exposes the operator-facing durable-execution runtime state currently reported for active streams.

#### Declaration
```csharp
public interface IDurableExecutionRuntimeStateCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-states"></a>

##### `States`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> States { get; }
```

Gets the reported durable-execution state entries visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getbybehaviorid-system-string"></a>

##### `GetByBehaviorId`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetByBehaviorId(string behaviorId)
```

Gets the reported durable-execution state entries for one durable behavior.

Returns: The matching state entries, or an empty list when the behavior has not reported runtime state.

Parameters:
- `behaviorId`: The stable durable behavior identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getbycompensationactionid-system-string"></a>

##### `GetByCompensationActionId`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetByCompensationActionId(string compensationActionId)
```

Gets the reported durable-execution state entries that currently include the requested compensation action.

Returns: The matching state entries, or an empty list when no stream currently reports that compensation action.

Parameters:
- `compensationActionId`: The stable compensation-action identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getbypendingsignalid-system-string"></a>

##### `GetByPendingSignalId`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetByPendingSignalId(string signalId)
```

Gets the reported durable-execution state entries that currently include the requested pending signal.

Returns: The matching state entries, or an empty list when no stream currently reports that signal.

Parameters:
- `signalId`: The stable signal identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getbypendingtimerid-system-string"></a>

##### `GetByPendingTimerId`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetByPendingTimerId(string timerId)
```

Gets the reported durable-execution state entries that currently include the requested pending timer.

Returns: The matching state entries, or an empty list when no stream currently reports that timer.

Parameters:
- `timerId`: The stable timer identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetBySourceModule(string sourceModuleId)
```

Gets the reported durable-execution state entries contributed by one source module.

Returns: The matching state entries, or an empty list when the module has not reported runtime state.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getbystreamid-system-string"></a>

##### `GetByStreamId`

```csharp
DurableExecutionRuntimeState GetByStreamId(string streamId)
```

Gets the latest reported durable-execution state for one stream.

Returns: The latest reported state, or `null` when that stream has not reported runtime state.

Parameters:
- `streamId`: The stable stream identifier to resolve.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getbytransportid-system-string"></a>

##### `GetByTransportId`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetByTransportId(string transportId)
```

Gets the reported durable-execution state entries exposed over one transport.

Returns: The matching state entries, or an empty list when none reported runtime state for that transport.

Parameters:
- `transportId`: The stable transport identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getwithcompensationactions"></a>

##### `GetWithCompensationActions`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetWithCompensationActions()
```

Gets the reported durable-execution state entries that currently expose one or more compensation actions.

Returns: The matching state entries, or an empty list when no stream currently reports compensation actions.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getwithpendingsignals"></a>

##### `GetWithPendingSignals`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetWithPendingSignals()
```

Gets the reported durable-execution state entries that currently have one or more pending signals.

Returns: The matching state entries, or an empty list when no stream currently reports pending signals.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-getwithpendingtimers"></a>

##### `GetWithPendingTimers`

```csharp
IReadOnlyList<DurableExecutionRuntimeState> GetWithPendingTimers()
```

Gets the reported durable-execution state entries that currently have one or more pending timers.

Returns: The matching state entries, or an empty list when no stream currently reports pending timers.

<a id="member-m-cephalon-abstractions-execution-idurableexecutionruntimestatecatalog-trygetbystreamid-system-string-cephalon-abstractions-execution-durableexecutionruntimestate"></a>

##### `TryGetByStreamId`

```csharp
bool TryGetByStreamId(string streamId, out DurableExecutionRuntimeState state)
```

Tries to get the latest reported durable-execution state for one stream.

Returns: `true` when a reported state exists; otherwise, `false`.

Parameters:
- `streamId`: The stable stream identifier to resolve.
- `state`: Receives the latest reported state when the stream has reported one.

<a id="type-cephalon-abstractions-execution-iexecutiongraphcontributor"></a>

### `IExecutionGraphContributor`

Contributes one or more execution graphs to the active runtime.

#### Declaration
```csharp
public interface IExecutionGraphContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-execution-iexecutiongraphcontributor-registerexecutiongraphs-cephalon-abstractions-execution-iexecutiongraphregistry"></a>

##### `RegisterExecutionGraphs`

```csharp
void RegisterExecutionGraphs(IExecutionGraphRegistry graphs)
```

Registers one or more execution graphs owned by the contributor.

Parameters:
- `graphs`: The execution-graph registry receiving graph descriptors.

<a id="type-cephalon-abstractions-execution-iexecutiongraphregistry"></a>

### `IExecutionGraphRegistry`

Receives execution graphs contributed by active modules.

#### Declaration
```csharp
public interface IExecutionGraphRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-execution-iexecutiongraphregistry-add-cephalon-abstractions-execution-executiongraphdescriptor"></a>

##### `Add`

```csharp
void Add(ExecutionGraphDescriptor graph)
```

Adds an execution graph to the current runtime composition.

Parameters:
- `graph`: The execution graph to register.

<a id="type-cephalon-abstractions-execution-iexecutionruntimecatalog"></a>

### `IExecutionRuntimeCatalog`

Exposes the execution graphs visible to the current runtime.

#### Declaration
```csharp
public interface IExecutionRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-execution-iexecutionruntimecatalog-graphs"></a>

##### `Graphs`

```csharp
IReadOnlyList<ExecutionGraphDescriptor> Graphs { get; }
```

Gets all execution graphs visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-execution-iexecutionruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
ExecutionGraphDescriptor GetById(string graphId)
```

Gets one execution graph by its stable identifier.

Returns: The matching graph, or `null` when it is not active.

Parameters:
- `graphId`: The execution-graph identifier to resolve.

<a id="member-m-cephalon-abstractions-execution-iexecutionruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<ExecutionGraphDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all execution graphs contributed by the requested module.

Returns: The matching execution graphs, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="type-cephalon-abstractions-execution-ihostedexecutioncontributor"></a>

### `IHostedExecutionContributor`

Contributes one or more hosted or background execution descriptors to the active runtime.

#### Declaration
```csharp
public interface IHostedExecutionContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-execution-ihostedexecutioncontributor-registerhostedexecutions-cephalon-abstractions-execution-ihostedexecutionregistry"></a>

##### `RegisterHostedExecutions`

```csharp
void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
```

Registers one or more hosted execution descriptors owned by the contributor.

Parameters:
- `hostedExecutions`: The hosted-execution registry receiving hosted-execution descriptors.

<a id="type-cephalon-abstractions-execution-ihostedexecutionregistry"></a>

### `IHostedExecutionRegistry`

Receives hosted-execution descriptors contributed by active modules.

#### Declaration
```csharp
public interface IHostedExecutionRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-execution-ihostedexecutionregistry-add-cephalon-abstractions-execution-hostedexecutiondescriptor"></a>

##### `Add`

```csharp
void Add(HostedExecutionDescriptor hostedExecution)
```

Adds a hosted execution to the current runtime composition.

Parameters:
- `hostedExecution`: The hosted execution to register.

<a id="type-cephalon-abstractions-execution-ihostedexecutionruntimecatalog"></a>

### `IHostedExecutionRuntimeCatalog`

Exposes the hosted executions visible to the current runtime.

#### Declaration
```csharp
public interface IHostedExecutionRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-execution-ihostedexecutionruntimecatalog-hostedexecutions"></a>

##### `HostedExecutions`

```csharp
IReadOnlyList<HostedExecutionDescriptor> HostedExecutions { get; }
```

Gets all hosted executions visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-execution-ihostedexecutionruntimecatalog-getbyexecutiongraph-system-string"></a>

##### `GetByExecutionGraph`

```csharp
IReadOnlyList<HostedExecutionDescriptor> GetByExecutionGraph(string executionGraphId)
```

Gets all hosted executions linked to one execution graph.

Returns: The matching hosted executions, or an empty list when none link to that graph.

Parameters:
- `executionGraphId`: The execution-graph identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-ihostedexecutionruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
HostedExecutionDescriptor GetById(string hostedExecutionId)
```

Gets one hosted execution by its stable identifier.

Returns: The matching hosted execution, or `null` when it is not active.

Parameters:
- `hostedExecutionId`: The hosted-execution identifier to resolve.

<a id="member-m-cephalon-abstractions-execution-ihostedexecutionruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<HostedExecutionDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all hosted executions contributed by the requested module.

Returns: The matching hosted executions, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="type-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog"></a>

### `ISagaChoreographyPublicationRuntimeStateCatalog`

Exposes the operator-facing live saga-choreography publication state currently reported for the active runtime.

#### Declaration
```csharp
public interface ISagaChoreographyPublicationRuntimeStateCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-states"></a>

##### `States`

```csharp
IReadOnlyList<SagaChoreographyPublicationRuntimeState> States { get; }
```

Gets the reported choreography publication-state entries visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-getbybehaviorid-system-string"></a>

##### `GetByBehaviorId`

```csharp
IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByBehaviorId(string behaviorId)
```

Gets the reported publication-state entries for one choreography behavior.

Returns: The matching publication-state entries, or an empty list when the behavior has not reported live publication state.

Parameters:
- `behaviorId`: The stable choreography behavior identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-getbychannelid-system-string"></a>

##### `GetByChannelId`

```csharp
IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByChannelId(string channelId)
```

Gets the reported publication-state entries that targeted the requested channel.

Returns: The matching publication-state entries, or an empty list when no publication targeted that channel.

Parameters:
- `channelId`: The logical channel identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-getbycorrelationid-system-string"></a>

##### `GetByCorrelationId`

```csharp
IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByCorrelationId(string correlationId)
```

Gets the reported publication-state entries associated with one correlation identifier.

Returns: The matching publication-state entries, or an empty list when none reported that correlation.

Parameters:
- `correlationId`: The correlation identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
SagaChoreographyPublicationRuntimeState GetById(string id)
```

Gets the latest reported publication state for one choreography publication path.

Returns: The latest reported publication state, or `null` when that identifier has not reported choreography runtime state.

Parameters:
- `id`: The stable runtime-state identifier to resolve.

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetBySourceModule(string sourceModuleId)
```

Gets the reported publication-state entries contributed by one source module.

Returns: The matching publication-state entries, or an empty list when the module has not reported live publication state.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-getbytransportid-system-string"></a>

##### `GetByTransportId`

```csharp
IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByTransportId(string transportId)
```

Gets the reported publication-state entries exposed over one transport.

Returns: The matching publication-state entries, or an empty list when none reported live publication state for that transport.

Parameters:
- `transportId`: The stable transport identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-getcompensationpublications"></a>

##### `GetCompensationPublications`

```csharp
IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetCompensationPublications()
```

Gets the reported publication-state entries that currently represent compensation work.

Returns: The matching compensation publication-state entries, or an empty list when none reported compensation posture.

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-getfailedpublications"></a>

##### `GetFailedPublications`

```csharp
IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetFailedPublications()
```

Gets the reported publication-state entries whose latest observation is failed.

Returns: The matching failed publication-state entries, or an empty list when none currently report a failed posture.

<a id="member-m-cephalon-abstractions-execution-isagachoreographypublicationruntimestatecatalog-trygetbyid-system-string-cephalon-abstractions-execution-sagachoreographypublicationruntimestate"></a>

##### `TryGetById`

```csharp
bool TryGetById(string id, out SagaChoreographyPublicationRuntimeState state)
```

Tries to get the latest reported publication state for one choreography publication path.

Returns: `true` when a reported state exists; otherwise, `false`.

Parameters:
- `id`: The stable runtime-state identifier to resolve.
- `state`: Receives the latest reported publication state when that identifier has reported one.

<a id="type-cephalon-abstractions-execution-isagachoreographyruntimecatalog"></a>

### `ISagaChoreographyRuntimeCatalog`

Exposes the active saga-choreography behaviors visible to the current runtime.

#### Declaration
```csharp
public interface ISagaChoreographyRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-execution-isagachoreographyruntimecatalog-sagachoreographies"></a>

##### `SagaChoreographies`

```csharp
IReadOnlyList<SagaChoreographyRuntimeDescriptor> SagaChoreographies { get; }
```

Gets all active saga-choreography behaviors visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-execution-isagachoreographyruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
SagaChoreographyRuntimeDescriptor GetById(string behaviorId)
```

Gets one saga-choreography behavior by its stable behavior identifier.

Returns: The matching choreography descriptor, or `null` when it is not active.

Parameters:
- `behaviorId`: The choreography behavior identifier to resolve.

<a id="member-m-cephalon-abstractions-execution-isagachoreographyruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<SagaChoreographyRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all saga-choreography behaviors contributed by the requested module.

Returns: The matching choreographies, or an empty list when the module contributed none.

Parameters:
- `sourceModuleId`: The source module identifier to filter by.

<a id="member-m-cephalon-abstractions-execution-isagachoreographyruntimecatalog-getbytransportid-system-string"></a>

##### `GetByTransportId`

```csharp
IReadOnlyList<SagaChoreographyRuntimeDescriptor> GetByTransportId(string transportId)
```

Gets all saga-choreography behaviors exposed over the requested transport.

Returns: The matching choreographies, or an empty list when none expose that transport.

Parameters:
- `transportId`: The stable transport identifier to filter by.

<a id="type-cephalon-abstractions-execution-sagachoreographypublicationruntimestate"></a>

### `SagaChoreographyPublicationRuntimeState`

Describes the latest operator-facing runtime state reported for one live saga-choreography publication path.

#### Declaration
```csharp
public sealed class SagaChoreographyPublicationRuntimeState
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-ctor-system-string-system-string-system-string-system-string-system-string-system-datetimeoffset-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-string-system-boolean-system-string-system-nullable-system-datetimeoffset-system-string-system-int32-system-int32-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `SagaChoreographyPublicationRuntimeState`

```csharp
SagaChoreographyPublicationRuntimeState(string Id, string BehaviorId, string PublicationId, string ChannelId, string EventType, DateTimeOffset OccurredAtUtc, string SourceModuleId, IReadOnlyList<string> TransportIds, string CorrelationId, string TenantId, string ContentType, bool IsCompensation, string LastOutcome, DateTimeOffset? LastObservedAtUtc, string LastPublisherType, int AcceptedCount, int FailedCount, string LastError, IReadOnlyDictionary<string, string> Metadata)
```

Describes the latest operator-facing runtime state reported for one live saga-choreography publication path.

Parameters:
- `Id`: The stable runtime-state identifier for this observed choreography publication path.
- `BehaviorId`: The stable choreography behavior identifier that produced the publication.
- `PublicationId`: The stable publication identifier declared by the choreography step.
- `ChannelId`: The logical channel or destination identifier used by the publication.
- `EventType`: The logical event type identifier used by the publication.
- `OccurredAtUtc`: The UTC timestamp carried by the observed publication itself.
- `SourceModuleId`: The owning module identifier when one is known at runtime.
- `TransportIds`: The transport identifiers that expose the owning choreography behavior.
- `CorrelationId`: The correlation identifier associated with the publication when one exists.
- `TenantId`: The tenant identifier associated with the publication when one exists.
- `ContentType`: The payload content type when one is known.
- `IsCompensation`: Indicates whether the publication represents compensation work.
- `LastOutcome`: The last reported publication outcome identifier when one exists.
- `LastObservedAtUtc`: The UTC timestamp when the latest publication observation was reported.
- `LastPublisherType`: The last concrete publisher implementation type that accepted or rejected the publication when one was reported.
- `AcceptedCount`: The number of `accepted` observations reported so far.
- `FailedCount`: The number of `failed` observations reported so far.
- `LastError`: The latest operator-facing error summary when the publication handoff reported a failure.
- `Metadata`: The operator-facing metadata captured by the latest observation.

#### Properties

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-acceptedcount"></a>

##### `AcceptedCount`

```csharp
int AcceptedCount { get; set; }
```

The number of `accepted` observations reported so far.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; set; }
```

The stable choreography behavior identifier that produced the publication.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

The logical channel or destination identifier used by the publication.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; set; }
```

The payload content type when one is known.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

The correlation identifier associated with the publication when one exists.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-eventtype"></a>

##### `EventType`

```csharp
string EventType { get; set; }
```

The logical event type identifier used by the publication.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; set; }
```

The number of `failed` observations reported so far.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

The stable runtime-state identifier for this observed choreography publication path.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-isaccepted"></a>

##### `IsAccepted`

```csharp
bool IsAccepted { get; }
```

Gets a value indicating whether the latest report says the publication handoff succeeded.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-iscompensation"></a>

##### `IsCompensation`

```csharp
bool IsCompensation { get; set; }
```

Indicates whether the publication represents compensation work.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-isfailed"></a>

##### `IsFailed`

```csharp
bool IsFailed { get; }
```

Gets a value indicating whether the latest report says the publication handoff failed.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; set; }
```

The latest operator-facing error summary when the publication handoff reported a failure.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the latest publication observation was reported.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; set; }
```

The last reported publication outcome identifier when one exists.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-lastpublishertype"></a>

##### `LastPublisherType`

```csharp
string LastPublisherType { get; set; }
```

The last concrete publisher implementation type that accepted or rejected the publication when one was reported.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

The operator-facing metadata captured by the latest observation.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; set; }
```

The UTC timestamp carried by the observed publication itself.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-publicationid"></a>

##### `PublicationId`

```csharp
string PublicationId { get; set; }
```

The stable publication identifier declared by the choreography step.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

The owning module identifier when one is known at runtime.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; set; }
```

The tenant identifier associated with the publication when one exists.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-totalreports"></a>

##### `TotalReports`

```csharp
int TotalReports { get; }
```

Gets the total number of publication observations reported for this runtime-state entry.

<a id="member-p-cephalon-abstractions-execution-sagachoreographypublicationruntimestate-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; set; }
```

The transport identifiers that expose the owning choreography behavior.

<a id="type-cephalon-abstractions-execution-sagachoreographyruntimedescriptor"></a>

### `SagaChoreographyRuntimeDescriptor`

Describes one active saga-choreography behavior visible to the current runtime.

Remarks: This runtime-facing surface keeps choreography truth derived from the shared behavior topology and registered implementation types instead of inventing a host-only choreography registry. It is intentionally static and operator-facing: it describes the active choreography contract shape, ownership, transports, and publication semantics rather than per-invocation state.

#### Declaration
```csharp
public sealed class SagaChoreographyRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-int32-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `SagaChoreographyRuntimeDescriptor`

```csharp
SagaChoreographyRuntimeDescriptor(string id, string displayName, string description, string behaviorType, string inputType, string resultType, string localOutputType, string sourceModuleId, IReadOnlyList<string> transportIds, IReadOnlyList<string> requiredFeatureFlagIds, IReadOnlyList<int> successStatusCodes, IReadOnlyDictionary<string, string> metadata)
```

Creates a saga-choreography runtime descriptor.

Parameters:
- `id`: The stable choreography behavior identifier.
- `displayName`: The operator-facing choreography name.
- `description`: A human-readable description of the choreography behavior.
- `behaviorType`: The concrete choreography behavior implementation type name.
- `inputType`: The choreography input type name.
- `resultType`: The behavior result-contract type name.
- `localOutputType`: The typed local output carried inside the shared choreography result contract when one is known.
- `sourceModuleId`: The owning module identifier when the choreography came from an explicit module-owned behavior.
- `transportIds`: The transport identifiers that expose the choreography.
- `requiredFeatureFlagIds`: The ordered feature-flag identifiers that must resolve to enabled before the choreography can execute.
- `successStatusCodes`: The HTTP success status codes the shared choreography strategy can return for local output, publication-only work, or completion without output.
- `metadata`: Additional operator-facing metadata describing choreography semantics.

#### Properties

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-behaviortype"></a>

##### `BehaviorType`

```csharp
string BehaviorType { get; }
```

Gets the concrete choreography behavior implementation type name.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable choreography description.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing choreography name.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable choreography behavior identifier.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-inputtype"></a>

##### `InputType`

```csharp
string InputType { get; }
```

Gets the choreography input type name.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-localoutputtype"></a>

##### `LocalOutputType`

```csharp
string LocalOutputType { get; }
```

Gets the typed local output carried inside the choreography result contract when one is known.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional operator-facing metadata describing choreography semantics.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-requiredfeatureflagids"></a>

##### `RequiredFeatureFlagIds`

```csharp
IReadOnlyList<string> RequiredFeatureFlagIds { get; }
```

Gets the ordered feature-flag identifiers that gate choreography execution.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-resulttype"></a>

##### `ResultType`

```csharp
string ResultType { get; }
```

Gets the behavior result-contract type name.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the owning module identifier when one is known at runtime.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-successstatuscodes"></a>

##### `SuccessStatusCodes`

```csharp
IReadOnlyList<int> SuccessStatusCodes { get; }
```

Gets the HTTP success status codes the shared choreography strategy can return.

<a id="member-p-cephalon-abstractions-execution-sagachoreographyruntimedescriptor-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; }
```

Gets the transport identifiers that expose the choreography.

<a id="namespace-cephalon-abstractions-features"></a>

## Namespace Cephalon.Abstractions.Features

<a id="type-cephalon-abstractions-features-featureflagdescriptor"></a>

### `FeatureFlagDescriptor`

Describes one feature flag visible to the active Cephalon runtime.

#### Declaration
```csharp
public sealed class FeatureFlagDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-features-featureflagdescriptor-ctor-system-string-system-string-system-string-system-boolean-cephalon-abstractions-features-featureflagsourcekind-system-string-cephalon-abstractions-features-featureflagtargetingdescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-features-featureflagproviderbindingdescriptor-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `FeatureFlagDescriptor`

```csharp
FeatureFlagDescriptor(string id, string displayName, string description, bool enabled, FeatureFlagSourceKind sourceKind, string sourceModuleId, FeatureFlagTargetingDescriptor targeting, IReadOnlyList<FeatureFlagProviderBindingDescriptor> providerBindings, IReadOnlyDictionary<string, string> metadata)
```

Creates a feature-flag descriptor.

Parameters:
- `id`: The stable feature-flag identifier.
- `displayName`: The operator-facing feature-flag name.
- `description`: The human-readable description of the gated behavior.
- `enabled`: Indicates whether the feature flag is enabled before any targeting constraints are applied.
- `sourceKind`: Identifies whether the feature flag is host-owned or module-owned.
- `sourceModuleId`: The module identifier that owns this feature flag when `sourceKind` is `Module`.
- `targeting`: The optional targeting constraints attached to the feature flag.
- `providerBindings`: Optional external provider bindings that can further gate the Cephalon-owned feature flag without replacing the local runtime descriptor as the source of truth.
- `metadata`: Optional operator-facing metadata.

#### Properties

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the gated behavior.

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing feature-flag name.

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; }
```

Gets a value indicating whether the feature flag is enabled before targeting is applied.

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable feature-flag identifier.

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets operator-facing metadata for the feature flag.

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-providerbindings"></a>

##### `ProviderBindings`

```csharp
IReadOnlyList<FeatureFlagProviderBindingDescriptor> ProviderBindings { get; }
```

Gets the optional external provider bindings attached to the feature flag.

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-sourcekind"></a>

##### `SourceKind`

```csharp
FeatureFlagSourceKind SourceKind { get; }
```

Gets the ownership kind for this feature flag.

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the owning module identifier when the feature flag is module-owned.

<a id="member-p-cephalon-abstractions-features-featureflagdescriptor-targeting"></a>

##### `Targeting`

```csharp
FeatureFlagTargetingDescriptor Targeting { get; }
```

Gets the optional targeting constraints attached to the feature flag.

<a id="type-cephalon-abstractions-features-featureflagevaluationcontext"></a>

### `FeatureFlagEvaluationContext`

Supplies contextual information for evaluating a feature flag at runtime.

#### Declaration
```csharp
public sealed class FeatureFlagEvaluationContext
```

#### Constructors

<a id="member-m-cephalon-abstractions-features-featureflagevaluationcontext-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `FeatureFlagEvaluationContext`

```csharp
FeatureFlagEvaluationContext(string environmentName, string moduleId, string behaviorId, string capabilityKey, string transportId, string tenantId, string subjectId, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a feature-flag evaluation context.

Parameters:
- `environmentName`: The active hosting environment name.
- `moduleId`: The current module identifier when one is known.
- `behaviorId`: The current behavior identifier when one is known.
- `capabilityKey`: The current capability key when one is known.
- `transportId`: The active transport identifier when one is known.
- `tenantId`: The current tenant identifier when one is known.
- `subjectId`: The current subject identifier when one is known.
- `tags`: The descriptive tags associated with the current request or workload.
- `metadata`: Additional evaluation metadata.

#### Properties

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the current behavior identifier when one is known.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-capabilitykey"></a>

##### `CapabilityKey`

```csharp
string CapabilityKey { get; }
```

Gets the current capability key when one is known.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-empty"></a>

##### `Empty`

```csharp
FeatureFlagEvaluationContext Empty { get; }
```

Gets an empty feature-flag evaluation context.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-environmentname"></a>

##### `EnvironmentName`

```csharp
string EnvironmentName { get; }
```

Gets the active hosting environment name.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional evaluation metadata.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-moduleid"></a>

##### `ModuleId`

```csharp
string ModuleId { get; }
```

Gets the current module identifier when one is known.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the current subject identifier when one is known.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the descriptive tags associated with the current request or workload.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the current tenant identifier when one is known.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationcontext-transportid"></a>

##### `TransportId`

```csharp
string TransportId { get; }
```

Gets the active transport identifier when one is known.

<a id="type-cephalon-abstractions-features-featureflagevaluationresult"></a>

### `FeatureFlagEvaluationResult`

Describes the result of evaluating a feature flag for a specific runtime context.

#### Declaration
```csharp
public sealed class FeatureFlagEvaluationResult
```

#### Constructors

<a id="member-m-cephalon-abstractions-features-featureflagevaluationresult-ctor-system-string-system-boolean-system-boolean-system-boolean-system-string-system-nullable-cephalon-abstractions-features-featureflagsourcekind-system-string"></a>

##### `FeatureFlagEvaluationResult`

```csharp
FeatureFlagEvaluationResult(string FeatureId, bool IsDefined, bool IsEnabled, bool Matched, string Reason, FeatureFlagSourceKind? SourceKind, string SourceModuleId)
```

Describes the result of evaluating a feature flag for a specific runtime context.

Parameters:
- `FeatureId`: The evaluated feature-flag identifier.
- `IsDefined`: Indicates whether the feature flag exists in the active runtime.
- `IsEnabled`: Indicates whether the feature flag resolved to enabled.
- `Matched`: Indicates whether the supplied evaluation context matched the targeting constraints for the feature flag.
- `Reason`: The operator-facing explanation for the evaluation result.
- `SourceKind`: The ownership kind for the resolved feature flag when one exists.
- `SourceModuleId`: The owning module identifier when the feature flag is module-owned.

#### Properties

<a id="member-p-cephalon-abstractions-features-featureflagevaluationresult-featureid"></a>

##### `FeatureId`

```csharp
string FeatureId { get; set; }
```

The evaluated feature-flag identifier.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationresult-isdefined"></a>

##### `IsDefined`

```csharp
bool IsDefined { get; set; }
```

Indicates whether the feature flag exists in the active runtime.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationresult-isenabled"></a>

##### `IsEnabled`

```csharp
bool IsEnabled { get; set; }
```

Indicates whether the feature flag resolved to enabled.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationresult-matched"></a>

##### `Matched`

```csharp
bool Matched { get; set; }
```

Indicates whether the supplied evaluation context matched the targeting constraints for the feature flag.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationresult-providerresults"></a>

##### `ProviderResults`

```csharp
IReadOnlyList<FeatureFlagProviderEvaluationResult> ProviderResults { get; set; }
```

Gets the external provider evaluation results that participated in the final answer.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; set; }
```

The operator-facing explanation for the evaluation result.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationresult-sourcekind"></a>

##### `SourceKind`

```csharp
FeatureFlagSourceKind? SourceKind { get; set; }
```

The ownership kind for the resolved feature flag when one exists.

<a id="member-p-cephalon-abstractions-features-featureflagevaluationresult-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

The owning module identifier when the feature flag is module-owned.

<a id="type-cephalon-abstractions-features-featureflagproviderbindingdescriptor"></a>

### `FeatureFlagProviderBindingDescriptor`

Describes one external provider binding attached to a Cephalon-owned feature flag.

#### Declaration
```csharp
public sealed class FeatureFlagProviderBindingDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-features-featureflagproviderbindingdescriptor-ctor-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `FeatureFlagProviderBindingDescriptor`

```csharp
FeatureFlagProviderBindingDescriptor(string providerId, string providerFeatureId, IReadOnlyDictionary<string, string> metadata)
```

Creates a feature-flag provider binding.

Parameters:
- `providerId`: The stable external provider identifier.
- `providerFeatureId`: The provider-specific feature identifier. When omitted, the owning Cephalon feature-flag id is used.
- `metadata`: Optional provider-specific binding metadata.

#### Properties

<a id="member-p-cephalon-abstractions-features-featureflagproviderbindingdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets provider-specific binding metadata.

<a id="member-p-cephalon-abstractions-features-featureflagproviderbindingdescriptor-providerfeatureid"></a>

##### `ProviderFeatureId`

```csharp
string ProviderFeatureId { get; }
```

Gets the provider-specific feature identifier when one was supplied.

<a id="member-p-cephalon-abstractions-features-featureflagproviderbindingdescriptor-providerid"></a>

##### `ProviderId`

```csharp
string ProviderId { get; }
```

Gets the stable external provider identifier.

#### Methods

<a id="member-m-cephalon-abstractions-features-featureflagproviderbindingdescriptor-resolveproviderfeatureid-system-string"></a>

##### `ResolveProviderFeatureId`

```csharp
string ResolveProviderFeatureId(string featureFlagId)
```

Resolves the provider-side feature identifier for the supplied Cephalon feature flag.

Returns: The provider-side feature identifier.

Parameters:
- `featureFlagId`: The owning Cephalon feature-flag identifier.

<a id="type-cephalon-abstractions-features-featureflagproviderevaluationresult"></a>

### `FeatureFlagProviderEvaluationResult`

Describes the result of evaluating one feature-flag provider binding.

#### Declaration
```csharp
public sealed class FeatureFlagProviderEvaluationResult
```

#### Constructors

<a id="member-m-cephalon-abstractions-features-featureflagproviderevaluationresult-ctor-system-string-system-string-system-boolean-system-boolean-system-string"></a>

##### `FeatureFlagProviderEvaluationResult`

```csharp
FeatureFlagProviderEvaluationResult(string ProviderId, string ProviderFeatureId, bool IsDefined, bool IsEnabled, string Reason)
```

Describes the result of evaluating one feature-flag provider binding.

Parameters:
- `ProviderId`: The external provider identifier.
- `ProviderFeatureId`: The provider-side feature identifier.
- `IsDefined`: Indicates whether the provider recognizes the requested feature.
- `IsEnabled`: Indicates whether the provider resolved the feature to enabled.
- `Reason`: The operator-facing explanation for the provider evaluation result.

#### Properties

<a id="member-p-cephalon-abstractions-features-featureflagproviderevaluationresult-isdefined"></a>

##### `IsDefined`

```csharp
bool IsDefined { get; set; }
```

Indicates whether the provider recognizes the requested feature.

<a id="member-p-cephalon-abstractions-features-featureflagproviderevaluationresult-isenabled"></a>

##### `IsEnabled`

```csharp
bool IsEnabled { get; set; }
```

Indicates whether the provider resolved the feature to enabled.

<a id="member-p-cephalon-abstractions-features-featureflagproviderevaluationresult-providerfeatureid"></a>

##### `ProviderFeatureId`

```csharp
string ProviderFeatureId { get; set; }
```

The provider-side feature identifier.

<a id="member-p-cephalon-abstractions-features-featureflagproviderevaluationresult-providerid"></a>

##### `ProviderId`

```csharp
string ProviderId { get; set; }
```

The external provider identifier.

<a id="member-p-cephalon-abstractions-features-featureflagproviderevaluationresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; set; }
```

The operator-facing explanation for the provider evaluation result.

<a id="type-cephalon-abstractions-features-featureflagsourcekind"></a>

### `FeatureFlagSourceKind`

Identifies who owns a feature flag visible to the active runtime.

#### Declaration
```csharp
public enum FeatureFlagSourceKind
```

#### Fields

<a id="member-f-cephalon-abstractions-features-featureflagsourcekind-host"></a>

##### `Host`

```csharp
const FeatureFlagSourceKind Host
```

Indicates the feature flag is host-owned and was configured directly by the app.

<a id="member-f-cephalon-abstractions-features-featureflagsourcekind-module"></a>

##### `Module`

```csharp
const FeatureFlagSourceKind Module
```

Indicates the feature flag is module-owned and was contributed by a Cephalon module.

<a id="type-cephalon-abstractions-features-featureflagtargetingdescriptor"></a>

### `FeatureFlagTargetingDescriptor`

Describes the optional targeting constraints that govern when a feature flag is considered active for a given runtime evaluation context.

#### Declaration
```csharp
public sealed class FeatureFlagTargetingDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-features-featureflagtargetingdescriptor-ctor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `FeatureFlagTargetingDescriptor`

```csharp
FeatureFlagTargetingDescriptor(IReadOnlyList<string> includedModuleIds, IReadOnlyList<string> excludedModuleIds, IReadOnlyList<string> includedBehaviorIds, IReadOnlyList<string> excludedBehaviorIds, IReadOnlyList<string> includedCapabilityKeys, IReadOnlyList<string> excludedCapabilityKeys, IReadOnlyList<string> includedTransportIds, IReadOnlyList<string> excludedTransportIds, IReadOnlyList<string> includedEnvironmentNames, IReadOnlyList<string> excludedEnvironmentNames, IReadOnlyList<string> includedTenantIds, IReadOnlyList<string> excludedTenantIds, IReadOnlyList<string> includedSubjectIds, IReadOnlyList<string> excludedSubjectIds, IReadOnlyList<string> includedTags, IReadOnlyList<string> excludedTags)
```

Creates feature-flag targeting constraints.

Parameters:
- `includedModuleIds`: The module identifiers that are explicitly included in the targeted audience.
- `excludedModuleIds`: The module identifiers that are explicitly excluded from the targeted audience.
- `includedBehaviorIds`: The behavior identifiers that are explicitly included in the targeted audience.
- `excludedBehaviorIds`: The behavior identifiers that are explicitly excluded from the targeted audience.
- `includedCapabilityKeys`: The capability keys that are explicitly included in the targeted audience.
- `excludedCapabilityKeys`: The capability keys that are explicitly excluded from the targeted audience.
- `includedTransportIds`: The transport identifiers that are explicitly included in the targeted audience.
- `excludedTransportIds`: The transport identifiers that are explicitly excluded from the targeted audience.
- `includedEnvironmentNames`: The environment names that are explicitly included in the targeted audience.
- `excludedEnvironmentNames`: The environment names that are explicitly excluded from the targeted audience.
- `includedTenantIds`: The tenant identifiers that are explicitly included in the targeted audience.
- `excludedTenantIds`: The tenant identifiers that are explicitly excluded from the targeted audience.
- `includedSubjectIds`: The subject identifiers that are explicitly included in the targeted audience.
- `excludedSubjectIds`: The subject identifiers that are explicitly excluded from the targeted audience.
- `includedTags`: The descriptive tags that are explicitly included in the targeted audience.
- `excludedTags`: The descriptive tags that are explicitly excluded from the targeted audience.

#### Properties

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-empty"></a>

##### `Empty`

```csharp
FeatureFlagTargetingDescriptor Empty { get; }
```

Gets an empty targeting descriptor with no constraints.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-excludedbehaviorids"></a>

##### `ExcludedBehaviorIds`

```csharp
IReadOnlyList<string> ExcludedBehaviorIds { get; }
```

Gets the explicitly excluded behavior identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-excludedcapabilitykeys"></a>

##### `ExcludedCapabilityKeys`

```csharp
IReadOnlyList<string> ExcludedCapabilityKeys { get; }
```

Gets the explicitly excluded capability keys.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-excludedenvironmentnames"></a>

##### `ExcludedEnvironmentNames`

```csharp
IReadOnlyList<string> ExcludedEnvironmentNames { get; }
```

Gets the explicitly excluded environment names.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-excludedmoduleids"></a>

##### `ExcludedModuleIds`

```csharp
IReadOnlyList<string> ExcludedModuleIds { get; }
```

Gets the explicitly excluded module identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-excludedsubjectids"></a>

##### `ExcludedSubjectIds`

```csharp
IReadOnlyList<string> ExcludedSubjectIds { get; }
```

Gets the explicitly excluded subject identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-excludedtags"></a>

##### `ExcludedTags`

```csharp
IReadOnlyList<string> ExcludedTags { get; }
```

Gets the explicitly excluded descriptive tags.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-excludedtenantids"></a>

##### `ExcludedTenantIds`

```csharp
IReadOnlyList<string> ExcludedTenantIds { get; }
```

Gets the explicitly excluded tenant identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-excludedtransportids"></a>

##### `ExcludedTransportIds`

```csharp
IReadOnlyList<string> ExcludedTransportIds { get; }
```

Gets the explicitly excluded transport identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any targeting constraint was supplied.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-includedbehaviorids"></a>

##### `IncludedBehaviorIds`

```csharp
IReadOnlyList<string> IncludedBehaviorIds { get; }
```

Gets the explicitly included behavior identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-includedcapabilitykeys"></a>

##### `IncludedCapabilityKeys`

```csharp
IReadOnlyList<string> IncludedCapabilityKeys { get; }
```

Gets the explicitly included capability keys.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-includedenvironmentnames"></a>

##### `IncludedEnvironmentNames`

```csharp
IReadOnlyList<string> IncludedEnvironmentNames { get; }
```

Gets the explicitly included environment names.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-includedmoduleids"></a>

##### `IncludedModuleIds`

```csharp
IReadOnlyList<string> IncludedModuleIds { get; }
```

Gets the explicitly included module identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-includedsubjectids"></a>

##### `IncludedSubjectIds`

```csharp
IReadOnlyList<string> IncludedSubjectIds { get; }
```

Gets the explicitly included subject identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-includedtags"></a>

##### `IncludedTags`

```csharp
IReadOnlyList<string> IncludedTags { get; }
```

Gets the explicitly included descriptive tags.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-includedtenantids"></a>

##### `IncludedTenantIds`

```csharp
IReadOnlyList<string> IncludedTenantIds { get; }
```

Gets the explicitly included tenant identifiers.

<a id="member-p-cephalon-abstractions-features-featureflagtargetingdescriptor-includedtransportids"></a>

##### `IncludedTransportIds`

```csharp
IReadOnlyList<string> IncludedTransportIds { get; }
```

Gets the explicitly included transport identifiers.

<a id="type-cephalon-abstractions-features-ifeatureflagcontributor"></a>

### `IFeatureFlagContributor`

Allows a module to contribute feature flags to the active runtime.

#### Declaration
```csharp
public interface IFeatureFlagContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-features-ifeatureflagcontributor-registerfeatureflags-cephalon-abstractions-features-ifeatureflagregistry"></a>

##### `RegisterFeatureFlags`

```csharp
void RegisterFeatureFlags(IFeatureFlagRegistry registry)
```

Registers the feature flags owned by the contributing module.

Parameters:
- `registry`: The registry that receives feature-flag descriptors.

<a id="type-cephalon-abstractions-features-ifeatureflagprovider"></a>

### `IFeatureFlagProvider`

Evaluates Cephalon feature-flag provider bindings against external or provider-owned state.

Remarks: Implementations are expected to answer from cached or in-memory provider state rather than performing network I/O on the hot execution path.

#### Declaration
```csharp
public interface IFeatureFlagProvider
```

#### Properties

<a id="member-p-cephalon-abstractions-features-ifeatureflagprovider-providerid"></a>

##### `ProviderId`

```csharp
string ProviderId { get; }
```

Gets the stable provider identifier used by feature-flag bindings.

#### Methods

<a id="member-m-cephalon-abstractions-features-ifeatureflagprovider-evaluate-cephalon-abstractions-features-featureflagproviderbindingdescriptor-cephalon-abstractions-features-featureflagdescriptor-cephalon-abstractions-features-featureflagevaluationcontext"></a>

##### `Evaluate`

```csharp
FeatureFlagProviderEvaluationResult Evaluate(FeatureFlagProviderBindingDescriptor binding, FeatureFlagDescriptor featureFlag, FeatureFlagEvaluationContext context)
```

Evaluates one provider binding for the supplied Cephalon feature flag and runtime context.

Returns: The provider evaluation result.

Parameters:
- `binding`: The provider binding attached to the Cephalon feature flag.
- `featureFlag`: The owning Cephalon feature-flag descriptor.
- `context`: The optional runtime context used for evaluation.

<a id="type-cephalon-abstractions-features-ifeatureflagregistry"></a>

### `IFeatureFlagRegistry`

Collects feature flags contributed to the active runtime.

#### Declaration
```csharp
public interface IFeatureFlagRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-features-ifeatureflagregistry-add-cephalon-abstractions-features-featureflagdescriptor"></a>

##### `Add`

```csharp
void Add(FeatureFlagDescriptor featureFlag)
```

Adds a feature-flag descriptor to the current runtime composition.

Parameters:
- `featureFlag`: The feature-flag descriptor to register.

<a id="type-cephalon-abstractions-features-ifeatureflagruntimecatalog"></a>

### `IFeatureFlagRuntimeCatalog`

Exposes the feature flags visible to the current runtime.

#### Declaration
```csharp
public interface IFeatureFlagRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-features-ifeatureflagruntimecatalog-featureflags"></a>

##### `FeatureFlags`

```csharp
IReadOnlyList<FeatureFlagDescriptor> FeatureFlags { get; }
```

Gets all feature flags visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-features-ifeatureflagruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
FeatureFlagDescriptor GetById(string featureFlagId)
```

Gets one feature flag by its stable identifier.

Returns: The matching feature flag, or `null` when it is not active.

Parameters:
- `featureFlagId`: The feature-flag identifier to resolve.

<a id="member-m-cephalon-abstractions-features-ifeatureflagruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<FeatureFlagDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all module-owned feature flags contributed by the requested source module.

Returns: The matching feature flags, or an empty list when none were contributed.

Parameters:
- `sourceModuleId`: The source-module identifier to filter by.

<a id="member-m-cephalon-abstractions-features-ifeatureflagruntimecatalog-getdisabled"></a>

##### `GetDisabled`

```csharp
IReadOnlyList<FeatureFlagDescriptor> GetDisabled()
```

Gets all feature flags that are disabled before targeting is applied.

Returns: The disabled feature flags.

<a id="member-m-cephalon-abstractions-features-ifeatureflagruntimecatalog-getenabled"></a>

##### `GetEnabled`

```csharp
IReadOnlyList<FeatureFlagDescriptor> GetEnabled()
```

Gets all feature flags that are enabled before targeting is applied.

Returns: The enabled feature flags.

<a id="type-cephalon-abstractions-features-ifeaturetoggle"></a>

### `IFeatureToggle`

Evaluates runtime feature flags against an optional evaluation context.

#### Declaration
```csharp
public interface IFeatureToggle
```

#### Methods

<a id="member-m-cephalon-abstractions-features-ifeaturetoggle-evaluate-system-string-cephalon-abstractions-features-featureflagevaluationcontext"></a>

##### `Evaluate`

```csharp
FeatureFlagEvaluationResult Evaluate(string featureFlagId, FeatureFlagEvaluationContext context)
```

Evaluates the requested feature flag and returns a richer operator-facing result.

Returns: The full evaluation result.

Parameters:
- `featureFlagId`: The stable feature-flag identifier to evaluate.
- `context`: The optional runtime context used for targeting evaluation.

<a id="member-m-cephalon-abstractions-features-ifeaturetoggle-isenabled-system-string-cephalon-abstractions-features-featureflagevaluationcontext"></a>

##### `IsEnabled`

```csharp
bool IsEnabled(string featureFlagId, FeatureFlagEvaluationContext context)
```

Evaluates whether the requested feature flag is enabled for the supplied context.

Returns: `true` when the feature flag resolves to enabled; otherwise `false`.

Parameters:
- `featureFlagId`: The stable feature-flag identifier to evaluate.
- `context`: The optional runtime context used for targeting evaluation.

<a id="namespace-cephalon-abstractions-health"></a>

## Namespace Cephalon.Abstractions.Health

<a id="type-cephalon-abstractions-health-dependencyhealthreport"></a>

### `DependencyHealthReport`

Describes the health state of one dependency surfaced by the runtime.

#### Declaration
```csharp
public sealed class DependencyHealthReport
```

#### Constructors

<a id="member-m-cephalon-abstractions-health-dependencyhealthreport-ctor-system-string-system-string-cephalon-abstractions-health-healthstate-system-string-system-boolean-system-string"></a>

##### `DependencyHealthReport`

```csharp
DependencyHealthReport(string Id, string DisplayName, HealthState State, string Description, bool Required, string Source)
```

Describes the health state of one dependency surfaced by the runtime.

Parameters:
- `Id`: The stable dependency identifier.
- `DisplayName`: The human-readable dependency name.
- `State`: The current health state.
- `Description`: The operator-facing health description.
- `Required`: Whether the dependency is required for readiness.
- `Source`: The contributor or subsystem that reported the dependency.

#### Properties

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

The operator-facing health description.

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

The human-readable dependency name.

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

The stable dependency identifier.

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Whether the dependency is required for readiness.

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-source"></a>

##### `Source`

```csharp
string Source { get; set; }
```

The contributor or subsystem that reported the dependency.

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-state"></a>

##### `State`

```csharp
HealthState State { get; set; }
```

The current health state.

<a id="type-cephalon-abstractions-health-healthstate"></a>

### `HealthState`

Describes the runtime health state of a dependency or probe.

#### Declaration
```csharp
public enum HealthState
```

#### Fields

<a id="member-f-cephalon-abstractions-health-healthstate-degraded"></a>

##### `Degraded`

```csharp
const HealthState Degraded
```

Indicates the dependency is degraded but still available.

<a id="member-f-cephalon-abstractions-health-healthstate-healthy"></a>

##### `Healthy`

```csharp
const HealthState Healthy
```

Indicates the dependency is healthy.

<a id="member-f-cephalon-abstractions-health-healthstate-unhealthy"></a>

##### `Unhealthy`

```csharp
const HealthState Unhealthy
```

Indicates the dependency is unhealthy.

<a id="type-cephalon-abstractions-health-idependencyhealthcontributor"></a>

### `IDependencyHealthContributor`

Contributes dependency-health information to the runtime.

#### Declaration
```csharp
public interface IDependencyHealthContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-health-idependencyhealthcontributor-getdependencyhealth"></a>

##### `GetDependencyHealth`

```csharp
IReadOnlyList<DependencyHealthReport> GetDependencyHealth()
```

Returns the dependency-health reports currently known to the contributor.

Returns: The contributed dependency-health reports.

<a id="namespace-cephalon-abstractions-ids"></a>

## Namespace Cephalon.Abstractions.Ids

<a id="type-cephalon-abstractions-ids-idgenerationrequest"></a>

### `IdGenerationRequest`

Describes optional hints supplied to an identifier generator.

#### Declaration
```csharp
public sealed class IdGenerationRequest
```

#### Constructors

<a id="member-m-cephalon-abstractions-ids-idgenerationrequest-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `IdGenerationRequest`

```csharp
IdGenerationRequest(string kind, string scope, string tenantId, IReadOnlyDictionary<string, string> attributes)
```

Creates a new identifier-generation request.

Parameters:
- `kind`: The logical identifier kind or entity category when one is known.
- `scope`: The logical generation scope when one is known.
- `tenantId`: The tenant identifier associated with the requested identifier when one is known.
- `attributes`: Optional generation hints supplied by the caller.

#### Properties

<a id="member-p-cephalon-abstractions-ids-idgenerationrequest-attributes"></a>

##### `Attributes`

```csharp
IReadOnlyDictionary<string, string> Attributes { get; }
```

Gets optional generation hints supplied by the caller.

<a id="member-p-cephalon-abstractions-ids-idgenerationrequest-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any generation hints were explicitly supplied.

<a id="member-p-cephalon-abstractions-ids-idgenerationrequest-kind"></a>

##### `Kind`

```csharp
string Kind { get; }
```

Gets the logical identifier kind or entity category when one is known.

<a id="member-p-cephalon-abstractions-ids-idgenerationrequest-scope"></a>

##### `Scope`

```csharp
string Scope { get; }
```

Gets the logical generation scope when one is known.

<a id="member-p-cephalon-abstractions-ids-idgenerationrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the requested identifier when one is known.

<a id="type-cephalon-abstractions-ids-iidgenerator"></a>

### `IIdGenerator`

Generates stable textual identifiers for Cephalon workloads.

#### Declaration
```csharp
public interface IIdGenerator
```

#### Properties

<a id="member-p-cephalon-abstractions-ids-iidgenerator-strategyid"></a>

##### `StrategyId`

```csharp
string StrategyId { get; }
```

Gets the stable identifier-generation strategy identifier.

#### Methods

<a id="member-m-cephalon-abstractions-ids-iidgenerator-generateasync-cephalon-abstractions-ids-idgenerationrequest-system-threading-cancellationtoken"></a>

##### `GenerateAsync`

```csharp
ValueTask<string> GenerateAsync(IdGenerationRequest request, CancellationToken cancellationToken)
```

Generates one identifier.

Returns: A task that completes with the generated identifier.

Parameters:
- `request`: Optional generation hints supplied by the caller.
- `cancellationToken`: The token that cancels the operation.

<a id="namespace-cephalon-abstractions-localization"></a>

## Namespace Cephalon.Abstractions.Localization

<a id="type-cephalon-abstractions-localization-ilocalizedresourcecontributor"></a>

### `ILocalizedResourceContributor`

Contributes localized resources to the runtime localization catalog.

#### Declaration
```csharp
public interface ILocalizedResourceContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-localization-ilocalizedresourcecontributor-registerresources-cephalon-abstractions-localization-ilocalizedresourceregistry"></a>

##### `RegisterResources`

```csharp
void RegisterResources(ILocalizedResourceRegistry resources)
```

Registers the contributor's localized resources.

Parameters:
- `resources`: The registry that accepts localized resources.

<a id="type-cephalon-abstractions-localization-ilocalizedresourceregistry"></a>

### `ILocalizedResourceRegistry`

Registers localized resources by culture and key.

#### Declaration
```csharp
public interface ILocalizedResourceRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-localization-ilocalizedresourceregistry-add-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `Add`

```csharp
void Add(string culture, IReadOnlyDictionary<string, string> resources)
```

Adds a batch of localized text values for one culture.

Parameters:
- `culture`: The culture the values belong to.
- `resources`: The localized resources to register.

<a id="member-m-cephalon-abstractions-localization-ilocalizedresourceregistry-add-system-string-system-string-system-string"></a>

##### `Add`

```csharp
void Add(string culture, string key, string value)
```

Adds one localized text value.

Parameters:
- `culture`: The culture the value belongs to.
- `key`: The localized resource key.
- `value`: The localized text value.

<a id="type-cephalon-abstractions-localization-ilocalizedtextcatalog"></a>

### `ILocalizedTextCatalog`

Reads localized text resolved by the runtime.

#### Declaration
```csharp
public interface ILocalizedTextCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-localization-ilocalizedtextcatalog-defaultculture"></a>

##### `DefaultCulture`

```csharp
string DefaultCulture { get; }
```

Gets the default culture used by the catalog.

<a id="member-p-cephalon-abstractions-localization-ilocalizedtextcatalog-supportedcultures"></a>

##### `SupportedCultures`

```csharp
IReadOnlyList<string> SupportedCultures { get; }
```

Gets the cultures currently available in the catalog.

#### Methods

<a id="member-m-cephalon-abstractions-localization-ilocalizedtextcatalog-createsnapshot-system-string"></a>

##### `CreateSnapshot`

```csharp
LocalizedResourcesSnapshot CreateSnapshot(string culture)
```

Creates an introspectable snapshot of the currently resolved localized resources.

Returns: The localized-resource snapshot.

Parameters:
- `culture`: The preferred culture, or `null` to use the default resolution flow.

<a id="member-m-cephalon-abstractions-localization-ilocalizedtextcatalog-getresources-system-string"></a>

##### `GetResources`

```csharp
IReadOnlyDictionary<string, string> GetResources(string culture)
```

Returns the localized resources visible for one culture.

Returns: The localized resources visible for the requested culture.

Parameters:
- `culture`: The preferred culture, or `null` to use the default resolution flow.

<a id="member-m-cephalon-abstractions-localization-ilocalizedtextcatalog-resolvetext-system-string-system-string-system-string"></a>

##### `ResolveText`

```csharp
string ResolveText(string key, string culture, string fallback)
```

Resolves one localized text value with an optional fallback.

Returns: The resolved localized text value.

Parameters:
- `key`: The resource key to resolve.
- `culture`: The preferred culture, or `null` to use the default resolution flow.
- `fallback`: The fallback value to use when the key cannot be resolved.

<a id="member-m-cephalon-abstractions-localization-ilocalizedtextcatalog-tryget-system-string-system-string-system-string"></a>

##### `TryGet`

```csharp
bool TryGet(string key, string culture, out string value)
```

Attempts to resolve one localized text value.

Returns: `true` when the value was resolved; otherwise `false`.

Parameters:
- `key`: The resource key to resolve.
- `culture`: The preferred culture, or `null` to use the default resolution flow.
- `value`: The resolved text value when one is found.

<a id="type-cephalon-abstractions-localization-localizedresourcessnapshot"></a>

### `LocalizedResourcesSnapshot`

Captures the resolved localization state visible to the runtime.

#### Declaration
```csharp
public sealed class LocalizedResourcesSnapshot
```

#### Constructors

<a id="member-m-cephalon-abstractions-localization-localizedresourcessnapshot-ctor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `LocalizedResourcesSnapshot`

```csharp
LocalizedResourcesSnapshot(string defaultCulture, string resolvedCulture, IReadOnlyList<string> supportedCultures, IReadOnlyDictionary<string, string> resources)
```

Creates a localization snapshot.

Parameters:
- `defaultCulture`: The default catalog culture.
- `resolvedCulture`: The culture actually resolved for the snapshot.
- `supportedCultures`: The cultures currently supported by the catalog.
- `resources`: The localized resources visible to the snapshot.

#### Properties

<a id="member-p-cephalon-abstractions-localization-localizedresourcessnapshot-defaultculture"></a>

##### `DefaultCulture`

```csharp
string DefaultCulture { get; }
```

Gets the default catalog culture.

<a id="member-p-cephalon-abstractions-localization-localizedresourcessnapshot-resolvedculture"></a>

##### `ResolvedCulture`

```csharp
string ResolvedCulture { get; }
```

Gets the culture actually resolved for the snapshot.

<a id="member-p-cephalon-abstractions-localization-localizedresourcessnapshot-resources"></a>

##### `Resources`

```csharp
IReadOnlyDictionary<string, string> Resources { get; }
```

Gets the localized resources visible to the snapshot.

<a id="member-p-cephalon-abstractions-localization-localizedresourcessnapshot-supportedcultures"></a>

##### `SupportedCultures`

```csharp
IReadOnlyList<string> SupportedCultures { get; }
```

Gets the cultures currently supported by the catalog.

<a id="namespace-cephalon-abstractions-modules"></a>

## Namespace Cephalon.Abstractions.Modules

<a id="type-cephalon-abstractions-modules-imodule"></a>

### `IModule`

Defines the host-agnostic contract that every Cephalon module implements.

#### Declaration
```csharp
public interface IModule
```

#### Properties

<a id="member-p-cephalon-abstractions-modules-imodule-descriptor"></a>

##### `Descriptor`

```csharp
ModuleDescriptor Descriptor { get; }
```

Gets the module descriptor used for discovery, ordering, and manifest output.

#### Methods

<a id="member-m-cephalon-abstractions-modules-imodule-configureservices-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `ConfigureServices`

```csharp
void ConfigureServices(IServiceCollection services)
```

Configures services required by the module.

Parameters:
- `services`: The service collection receiving module services.

<a id="member-m-cephalon-abstractions-modules-imodule-registercapabilities-cephalon-abstractions-capabilities-icapabilityregistry"></a>

##### `RegisterCapabilities`

```csharp
void RegisterCapabilities(ICapabilityRegistry capabilities)
```

Registers capabilities exposed by the module.

Parameters:
- `capabilities`: The capability registry receiving module capabilities.

<a id="type-cephalon-abstractions-modules-imodulelifecycle"></a>

### `IModuleLifecycle`

Defines the deterministic lifecycle hooks managed by the host runtime.

#### Declaration
```csharp
public interface IModuleLifecycle
```

#### Methods

<a id="member-m-cephalon-abstractions-modules-imodulelifecycle-initializeasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `InitializeAsync`

```csharp
Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
```

Initializes the module before the runtime starts serving work.

Returns: A task that completes when initialization finishes.

Parameters:
- `context`: The module runtime context.
- `cancellationToken`: A token that cancels initialization.

<a id="member-m-cephalon-abstractions-modules-imodulelifecycle-startasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `StartAsync`

```csharp
Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
```

Starts the module after initialization has completed.

Returns: A task that completes when startup finishes.

Parameters:
- `context`: The module runtime context.
- `cancellationToken`: A token that cancels startup.

<a id="member-m-cephalon-abstractions-modules-imodulelifecycle-stopasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `StopAsync`

```csharp
Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
```

Stops the module during runtime shutdown.

Returns: A task that completes when shutdown finishes.

Parameters:
- `context`: The module runtime context.
- `cancellationToken`: A token that cancels shutdown.

<a id="type-cephalon-abstractions-modules-modulebase"></a>

### `ModuleBase`

Provides default no-op implementations for module and lifecycle contracts.

#### Declaration
```csharp
public abstract class ModuleBase
```

#### Properties

<a id="member-p-cephalon-abstractions-modules-modulebase-descriptor"></a>

##### `Descriptor`

```csharp
ModuleDescriptor Descriptor { get; }
```

Gets the module descriptor used for discovery, ordering, and manifest output.

#### Methods

<a id="member-m-cephalon-abstractions-modules-modulebase-configureservices-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `ConfigureServices`

```csharp
void ConfigureServices(IServiceCollection services)
```

Configures services required by the module.

Parameters:
- `services`: The service collection receiving module services.

<a id="member-m-cephalon-abstractions-modules-modulebase-initializeasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `InitializeAsync`

```csharp
Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
```

Initializes the module before the runtime starts serving work.

Returns: A task that completes when initialization finishes.

Parameters:
- `context`: The module runtime context.
- `cancellationToken`: A token that cancels initialization.

<a id="member-m-cephalon-abstractions-modules-modulebase-registercapabilities-cephalon-abstractions-capabilities-icapabilityregistry"></a>

##### `RegisterCapabilities`

```csharp
void RegisterCapabilities(ICapabilityRegistry capabilities)
```

Registers capabilities exposed by the module.

Parameters:
- `capabilities`: The capability registry receiving module capabilities.

<a id="member-m-cephalon-abstractions-modules-modulebase-startasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `StartAsync`

```csharp
Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
```

Starts the module after initialization has completed.

Returns: A task that completes when startup finishes.

Parameters:
- `context`: The module runtime context.
- `cancellationToken`: A token that cancels startup.

<a id="member-m-cephalon-abstractions-modules-modulebase-stopasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `StopAsync`

```csharp
Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
```

Stops the module during runtime shutdown.

Returns: A task that completes when shutdown finishes.

Parameters:
- `context`: The module runtime context.
- `cancellationToken`: A token that cancels shutdown.

<a id="type-cephalon-abstractions-modules-modulecontext"></a>

### `ModuleContext`

Provides runtime services shared with module lifecycle hooks.

#### Declaration
```csharp
public sealed class ModuleContext
```

#### Constructors

<a id="member-m-cephalon-abstractions-modules-modulecontext-ctor-system-iserviceprovider"></a>

##### `ModuleContext`

```csharp
ModuleContext(IServiceProvider services)
```

Creates a module runtime context.

Parameters:
- `services`: The root service provider for the runtime.

#### Properties

<a id="member-p-cephalon-abstractions-modules-modulecontext-services"></a>

##### `Services`

```csharp
IServiceProvider Services { get; }
```

Gets the root service provider for the runtime.

<a id="type-cephalon-abstractions-modules-moduledescriptor"></a>

### `ModuleDescriptor`

Describes a module for discovery, ordering, manifest generation, and diagnostics.

#### Declaration
```csharp
public sealed class ModuleDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-modules-moduledescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ienumerable-system-type-system-collections-generic-ienumerable-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `ModuleDescriptor`

```csharp
ModuleDescriptor(string id, string displayName, string description, IEnumerable<Type> dependsOn, IEnumerable<string> tags, string version, IReadOnlyDictionary<string, string> metadata)
```

Creates a module descriptor.

Parameters:
- `id`: The stable module identifier.
- `displayName`: The human-readable module name.
- `description`: The module description.
- `dependsOn`: The module types this module depends on.
- `tags`: The tags associated with the module.
- `version`: The declared module version.
- `metadata`: Optional module metadata.

#### Properties

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-dependson"></a>

##### `DependsOn`

```csharp
IReadOnlyList<Type> DependsOn { get; }
```

Gets the module types this module depends on.

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the module description.

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable module name.

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable module identifier.

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional module metadata.

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the tags associated with the module.

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-version"></a>

##### `Version`

```csharp
string Version { get; }
```

Gets the declared module version, when one is available.

<a id="namespace-cephalon-abstractions-patterns"></a>

## Namespace Cephalon.Abstractions.Patterns

<a id="type-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor"></a>

### `BackendForFrontendBehaviorFilterDescriptor`

Describes behavior, capability, and tag-selection hints for one backend-for-frontend binding.

#### Declaration
```csharp
public sealed class BackendForFrontendBehaviorFilterDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-ctor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `BackendForFrontendBehaviorFilterDescriptor`

```csharp
BackendForFrontendBehaviorFilterDescriptor(IReadOnlyList<string> includedBehaviorIds, IReadOnlyList<string> excludedBehaviorIds, IReadOnlyList<string> includedCapabilityKeys, IReadOnlyList<string> excludedCapabilityKeys, IReadOnlyList<string> includedTags, IReadOnlyList<string> excludedTags)
```

Creates a backend-for-frontend behavior filter descriptor.

Parameters:
- `includedBehaviorIds`: The explicit behavior identifiers that should stay visible to the client.
- `excludedBehaviorIds`: The explicit behavior identifiers that should be hidden from the client.
- `includedCapabilityKeys`: The explicit capability keys that should stay visible to the client.
- `excludedCapabilityKeys`: The explicit capability keys that should be hidden from the client.
- `includedTags`: The behavior or endpoint tags that should stay visible to the client.
- `excludedTags`: The behavior or endpoint tags that should be hidden from the client.

#### Properties

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-empty"></a>

##### `Empty`

```csharp
BackendForFrontendBehaviorFilterDescriptor Empty { get; }
```

Gets an empty backend-for-frontend behavior filter.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-excludedbehaviorids"></a>

##### `ExcludedBehaviorIds`

```csharp
IReadOnlyList<string> ExcludedBehaviorIds { get; }
```

Gets the explicit behavior identifiers that should be hidden from the client.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-excludedcapabilitykeys"></a>

##### `ExcludedCapabilityKeys`

```csharp
IReadOnlyList<string> ExcludedCapabilityKeys { get; }
```

Gets the explicit capability keys that should be hidden from the client.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-excludedtags"></a>

##### `ExcludedTags`

```csharp
IReadOnlyList<string> ExcludedTags { get; }
```

Gets the behavior or endpoint tags that should be hidden from the client.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any behavior-filter hints were explicitly supplied.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-includedbehaviorids"></a>

##### `IncludedBehaviorIds`

```csharp
IReadOnlyList<string> IncludedBehaviorIds { get; }
```

Gets the explicit behavior identifiers that should stay visible to the client.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-includedcapabilitykeys"></a>

##### `IncludedCapabilityKeys`

```csharp
IReadOnlyList<string> IncludedCapabilityKeys { get; }
```

Gets the explicit capability keys that should stay visible to the client.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-includedtags"></a>

##### `IncludedTags`

```csharp
IReadOnlyList<string> IncludedTags { get; }
```

Gets the behavior or endpoint tags that should stay visible to the client.

<a id="type-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor"></a>

### `BackendForFrontendClientBindingDescriptor`

Describes one client-specific transport binding owned by a Cephalon module.

#### Declaration
```csharp
public sealed class BackendForFrontendClientBindingDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-patterns-backendforfrontendbehaviorfilterdescriptor-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `BackendForFrontendClientBindingDescriptor`

```csharp
BackendForFrontendClientBindingDescriptor(string id, string clientId, string sourceModuleId, string displayName, string description, string transportId, string entryPoint, BackendForFrontendBehaviorFilterDescriptor behaviorFilter, IReadOnlyDictionary<string, string> metadata)
```

Creates a backend-for-frontend client binding descriptor.

Parameters:
- `id`: The stable binding identifier.
- `clientId`: The stable client identifier, such as `mobile` or `storefront`.
- `sourceModuleId`: The Cephalon module that owns this binding.
- `displayName`: The operator-facing binding name.
- `description`: The human-readable description of the client-specific surface.
- `transportId`: The transport identifier used by this client surface.
- `entryPoint`: The transport-specific entry point, route prefix, or endpoint handle when one is known.
- `behaviorFilter`: The behavior, capability, and tag-selection hints attached to this client binding.
- `metadata`: Optional binding metadata.

#### Properties

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-behaviorfilter"></a>

##### `BehaviorFilter`

```csharp
BackendForFrontendBehaviorFilterDescriptor BehaviorFilter { get; }
```

Gets the behavior, capability, and tag-selection hints attached to this client binding.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-clientid"></a>

##### `ClientId`

```csharp
string ClientId { get; }
```

Gets the stable client identifier.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the client-specific surface.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing binding name.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-entrypoint"></a>

##### `EntryPoint`

```csharp
string EntryPoint { get; }
```

Gets the transport-specific entry point, route prefix, or endpoint handle when one is known.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable binding identifier.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional binding metadata.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that owns this client-specific binding.

<a id="member-p-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-transportid"></a>

##### `TransportId`

```csharp
string TransportId { get; }
```

Gets the transport identifier used by this client-specific surface.

<a id="type-cephalon-abstractions-patterns-ibackendforfrontendclientbindingcontributor"></a>

### `IBackendForFrontendClientBindingContributor`

Allows a module to contribute backend-for-frontend client bindings into the active runtime.

#### Declaration
```csharp
public interface IBackendForFrontendClientBindingContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-patterns-ibackendforfrontendclientbindingcontributor-registerclientbindings-cephalon-abstractions-patterns-ibackendforfrontendclientbindingregistry"></a>

##### `RegisterClientBindings`

```csharp
void RegisterClientBindings(IBackendForFrontendClientBindingRegistry bindings)
```

Registers one or more backend-for-frontend client bindings with the supplied registry.

Parameters:
- `bindings`: The registry that collects contributed client-binding descriptors.

<a id="type-cephalon-abstractions-patterns-ibackendforfrontendclientbindingregistry"></a>

### `IBackendForFrontendClientBindingRegistry`

Collects backend-for-frontend client binding descriptors contributed to the active runtime.

#### Declaration
```csharp
public interface IBackendForFrontendClientBindingRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-patterns-ibackendforfrontendclientbindingregistry-add-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor"></a>

##### `Add`

```csharp
void Add(BackendForFrontendClientBindingDescriptor binding)
```

Adds a backend-for-frontend client binding descriptor to the current runtime composition.

Parameters:
- `binding`: The client binding descriptor to register.

<a id="type-cephalon-abstractions-patterns-ibackendforfrontendruntimecatalog"></a>

### `IBackendForFrontendRuntimeCatalog`

Exposes the backend-for-frontend client bindings visible to the current runtime.

#### Declaration
```csharp
public interface IBackendForFrontendRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-patterns-ibackendforfrontendruntimecatalog-bindings"></a>

##### `Bindings`

```csharp
IReadOnlyList<BackendForFrontendClientBindingDescriptor> Bindings { get; }
```

Gets all backend-for-frontend client bindings visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-patterns-ibackendforfrontendruntimecatalog-getbyclientid-system-string"></a>

##### `GetByClientId`

```csharp
IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetByClientId(string clientId)
```

Gets all backend-for-frontend client bindings owned by the requested client identifier.

Returns: The matching client binding descriptors, or an empty list when none are active.

Parameters:
- `clientId`: The client identifier to filter by.

<a id="member-m-cephalon-abstractions-patterns-ibackendforfrontendruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
BackendForFrontendClientBindingDescriptor GetById(string bindingId)
```

Gets one backend-for-frontend client binding by its stable identifier.

Returns: The matching client binding descriptor, or `null` when it is not active.

Parameters:
- `bindingId`: The binding identifier to resolve.

<a id="member-m-cephalon-abstractions-patterns-ibackendforfrontendruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all backend-for-frontend client bindings owned by the requested module.

Returns: The matching client binding descriptors, or an empty list when none are active.

Parameters:
- `sourceModuleId`: The module identifier to filter by.

<a id="member-m-cephalon-abstractions-patterns-ibackendforfrontendruntimecatalog-getbytransportid-system-string"></a>

##### `GetByTransportId`

```csharp
IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetByTransportId(string transportId)
```

Gets all backend-for-frontend client bindings that target the requested transport.

Returns: The matching client binding descriptors, or an empty list when none are active.

Parameters:
- `transportId`: The transport identifier to filter by.

<a id="type-cephalon-abstractions-patterns-istranglerfigingressruntimecatalog"></a>

### `IStranglerFigIngressRuntimeCatalog`

Exposes the normalized strangler-fig ingress materialization answers visible to the current runtime.

#### Declaration
```csharp
public interface IStranglerFigIngressRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-patterns-istranglerfigingressruntimecatalog-routes"></a>

##### `Routes`

```csharp
IReadOnlyList<StranglerFigIngressRuntimeDescriptor> Routes { get; }
```

Gets all effective strangler-fig ingress answers visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-patterns-istranglerfigingressruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
StranglerFigIngressRuntimeDescriptor GetById(string routeId)
```

Gets one effective strangler-fig ingress answer by its stable route identifier.

Returns: The matching runtime descriptor, or `null` when it is not active.

Parameters:
- `routeId`: The route identifier to resolve.

<a id="member-m-cephalon-abstractions-patterns-istranglerfigingressruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<StranglerFigIngressRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all effective strangler-fig ingress answers owned by the requested module.

Returns: The matching runtime descriptors, or an empty list when none are active.

Parameters:
- `sourceModuleId`: The module identifier to filter by.

<a id="type-cephalon-abstractions-patterns-istranglerfigmigrationruntimecatalog"></a>

### `IStranglerFigMigrationRuntimeCatalog`

Exposes the effective strangler-fig migration policy visible to the current runtime.

#### Declaration
```csharp
public interface IStranglerFigMigrationRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-patterns-istranglerfigmigrationruntimecatalog-routes"></a>

##### `Routes`

```csharp
IReadOnlyList<StranglerFigMigrationRuntimeDescriptor> Routes { get; }
```

Gets all effective strangler-fig migration-policy answers visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-patterns-istranglerfigmigrationruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
StranglerFigMigrationRuntimeDescriptor GetById(string routeId)
```

Gets one effective strangler-fig migration-policy answer by its stable route identifier.

Returns: The matching runtime descriptor, or `null` when it is not active.

Parameters:
- `routeId`: The route identifier to resolve.

<a id="member-m-cephalon-abstractions-patterns-istranglerfigmigrationruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<StranglerFigMigrationRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all effective strangler-fig migration-policy answers owned by the requested module.

Returns: The matching runtime descriptors, or an empty list when none are active.

Parameters:
- `sourceModuleId`: The module identifier to filter by.

<a id="type-cephalon-abstractions-patterns-istranglerfigroutecontributor"></a>

### `IStranglerFigRouteContributor`

Allows a module to contribute strangler-fig routes into the active runtime.

#### Declaration
```csharp
public interface IStranglerFigRouteContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-patterns-istranglerfigroutecontributor-registerroutes-cephalon-abstractions-patterns-istranglerfigrouteregistry"></a>

##### `RegisterRoutes`

```csharp
void RegisterRoutes(IStranglerFigRouteRegistry routes)
```

Registers one or more strangler-fig route descriptors with the supplied registry.

Parameters:
- `routes`: The registry that collects contributed route descriptors.

<a id="type-cephalon-abstractions-patterns-istranglerfigrouter"></a>

### `IStranglerFigRouter`

Resolves requests against the active strangler-fig migration routes.

#### Declaration
```csharp
public interface IStranglerFigRouter
```

#### Methods

<a id="member-m-cephalon-abstractions-patterns-istranglerfigrouter-resolveasync-cephalon-abstractions-patterns-stranglerfigrequest-system-threading-cancellationtoken"></a>

##### `ResolveAsync`

```csharp
ValueTask<StranglerFigRouteResolution> ResolveAsync(StranglerFigRequest request, CancellationToken cancellationToken)
```

Resolves the migration boundary that should receive the supplied request.

Returns: The matched route resolution when the request is covered by an active strangler-fig route; otherwise, `null`.

Parameters:
- `request`: The request to evaluate.
- `cancellationToken`: The token that cancels the evaluation.

<a id="type-cephalon-abstractions-patterns-istranglerfigrouteregistry"></a>

### `IStranglerFigRouteRegistry`

Collects strangler-fig route descriptors contributed to the active runtime.

#### Declaration
```csharp
public interface IStranglerFigRouteRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-patterns-istranglerfigrouteregistry-add-cephalon-abstractions-patterns-stranglerfigroutedescriptor"></a>

##### `Add`

```csharp
void Add(StranglerFigRouteDescriptor route)
```

Adds a strangler-fig route descriptor to the current runtime composition.

Parameters:
- `route`: The route descriptor to register.

<a id="type-cephalon-abstractions-patterns-istranglerfigruntimecatalog"></a>

### `IStranglerFigRuntimeCatalog`

Exposes the strangler-fig routes visible to the current runtime.

#### Declaration
```csharp
public interface IStranglerFigRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-patterns-istranglerfigruntimecatalog-routes"></a>

##### `Routes`

```csharp
IReadOnlyList<StranglerFigRouteDescriptor> Routes { get; }
```

Gets all strangler-fig routes visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-patterns-istranglerfigruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
StranglerFigRouteDescriptor GetById(string routeId)
```

Gets one strangler-fig route by its stable identifier.

Returns: The matching route descriptor, or `null` when it is not active.

Parameters:
- `routeId`: The route identifier to resolve.

<a id="member-m-cephalon-abstractions-patterns-istranglerfigruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<StranglerFigRouteDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all strangler-fig routes owned by the requested module.

Returns: The matching route descriptors, or an empty list when none are active.

Parameters:
- `sourceModuleId`: The module identifier to filter by.

<a id="type-cephalon-abstractions-patterns-patterndescriptor"></a>

### `PatternDescriptor`

Describes one pattern that can shape a Cephalon app.

#### Declaration
```csharp
public sealed class PatternDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-patterndescriptor-ctor-system-string-system-string-system-string-cephalon-abstractions-patterns-patternkind-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `PatternDescriptor`

```csharp
PatternDescriptor(string id, string displayName, string description, PatternKind kind, IReadOnlyList<string> aliases, IReadOnlyList<string> tags, IReadOnlyList<string> requires, IReadOnlyList<string> conflictsWith, IReadOnlyDictionary<string, string> metadata)
```

Creates a pattern descriptor.

Parameters:
- `id`: The stable pattern identifier.
- `displayName`: The human-readable pattern name.
- `description`: The pattern description.
- `kind`: The category of the pattern.
- `aliases`: Optional aliases that can resolve to the same pattern.
- `tags`: The tags associated with the pattern.
- `requires`: The pattern identifiers required by this pattern.
- `conflictsWith`: The pattern identifiers that conflict with this pattern.
- `metadata`: Optional pattern metadata.

#### Properties

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-aliases"></a>

##### `Aliases`

```csharp
IReadOnlyList<string> Aliases { get; }
```

Gets optional aliases that can resolve to the same pattern.

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-conflictswith"></a>

##### `ConflictsWith`

```csharp
IReadOnlyList<string> ConflictsWith { get; }
```

Gets the pattern identifiers that conflict with this pattern.

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the pattern description.

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable pattern name.

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable pattern identifier.

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-kind"></a>

##### `Kind`

```csharp
PatternKind Kind { get; }
```

Gets the category of the pattern.

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional pattern metadata.

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-requires"></a>

##### `Requires`

```csharp
IReadOnlyList<string> Requires { get; }
```

Gets the pattern identifiers required by this pattern.

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the tags associated with the pattern.

<a id="type-cephalon-abstractions-patterns-patternkind"></a>

### `PatternKind`

Categorizes the role a pattern plays in an app shape.

#### Declaration
```csharp
public enum PatternKind
```

#### Fields

<a id="member-f-cephalon-abstractions-patterns-patternkind-architecture"></a>

##### `Architecture`

```csharp
const PatternKind Architecture
```

Identifies an architecture-shaping pattern.

<a id="member-f-cephalon-abstractions-patterns-patternkind-composition"></a>

##### `Composition`

```csharp
const PatternKind Composition
```

Identifies a composition pattern.

<a id="member-f-cephalon-abstractions-patterns-patternkind-data"></a>

##### `Data`

```csharp
const PatternKind Data
```

Identifies a data or persistence pattern.

<a id="member-f-cephalon-abstractions-patterns-patternkind-deployment"></a>

##### `Deployment`

```csharp
const PatternKind Deployment
```

Identifies a deployment-topology pattern.

<a id="member-f-cephalon-abstractions-patterns-patternkind-design"></a>

##### `Design`

```csharp
const PatternKind Design
```

Identifies a design pattern.

<a id="member-f-cephalon-abstractions-patterns-patternkind-domain"></a>

##### `Domain`

```csharp
const PatternKind Domain
```

Identifies a domain-modeling pattern.

<a id="member-f-cephalon-abstractions-patterns-patternkind-foundation"></a>

##### `Foundation`

```csharp
const PatternKind Foundation
```

Identifies a foundation pattern.

<a id="member-f-cephalon-abstractions-patterns-patternkind-organization"></a>

##### `Organization`

```csharp
const PatternKind Organization
```

Identifies an organization pattern.

<a id="type-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor"></a>

### `StranglerFigIngressRuntimeDescriptor`

Describes the effective strangler-fig ingress materialization answer for one route.

#### Declaration
```csharp
public sealed class StranglerFigIngressRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-patterns-stranglerfigtarget-cephalon-abstractions-patterns-stranglerfigtarget-system-string-system-string-system-string-system-string-system-string-system-boolean-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-int32-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `StranglerFigIngressRuntimeDescriptor`

```csharp
StranglerFigIngressRuntimeDescriptor(string routeId, string sourceModuleId, string displayName, string description, string pathPrefix, StranglerFigTarget requestedTarget, StranglerFigTarget effectiveTarget, string requestedTargetSource, string selectionMode, string selectedEndpoint, string selectedEndpointKind, string ingressMode, bool canMaterialize, string targetPathPrefix, string targetQuery, string targetUri, IReadOnlyList<string> methods, string progressState, int progressPercent, IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, string> runtimeMetadata)
```

Creates a strangler-fig ingress runtime descriptor.

Parameters:
- `routeId`: The stable route identifier.
- `sourceModuleId`: The Cephalon module that owns the modern boundary for this route.
- `displayName`: The operator-facing route name.
- `description`: The human-readable description of the migration boundary.
- `pathPrefix`: The rooted public path prefix that this route matches.
- `requestedTarget`: The target requested after applying migration-policy overlays.
- `effectiveTarget`: The target that will actually receive traffic after endpoint fallback is considered.
- `requestedTargetSource`: The source of the requested target, such as `authored-route` or `migration-route`.
- `selectionMode`: The runtime selection result, such as `requested-target` or `fallback-target`.
- `selectedEndpoint`: The concrete endpoint or boundary identifier that will receive traffic.
- `selectedEndpointKind`: The normalized selected-endpoint kind, such as `local-path`, `absolute-uri`, or `opaque`.
- `ingressMode`: The normalized ingress follow-through mode, such as `pass-through`, `rewrite-local-path`, `proxy-absolute-uri`, or `opaque-endpoint`.
- `canMaterialize`: Indicates whether a generic ingress or traffic manager can materialize this selected endpoint directly.
- `targetPathPrefix`: The normalized path prefix that traffic should land on when the selected endpoint is path-shaped.
- `targetQuery`: The normalized base query string that should flow with the selected endpoint when one exists.
- `targetUri`: The normalized absolute URI that traffic should target when the selected endpoint is absolute.
- `methods`: Optional request methods that this route matches.
- `progressState`: The normalized migration-progress state for the route.
- `progressPercent`: The normalized migration-progress percentage for the route.
- `metadata`: The original authored route metadata.
- `runtimeMetadata`: Additional runtime-only metadata such as notes or overlay provenance.

#### Properties

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-canmaterialize"></a>

##### `CanMaterialize`

```csharp
bool CanMaterialize { get; }
```

Gets a value indicating whether a generic ingress or traffic manager can materialize this selected endpoint directly.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the migration boundary.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing route name.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-effectivetarget"></a>

##### `EffectiveTarget`

```csharp
StranglerFigTarget EffectiveTarget { get; }
```

Gets the target that will actually receive traffic after endpoint fallback is considered.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-ingressmode"></a>

##### `IngressMode`

```csharp
string IngressMode { get; }
```

Gets the normalized ingress follow-through mode, such as `pass-through`, `rewrite-local-path`, `proxy-absolute-uri`, or `opaque-endpoint`.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets the original authored route metadata.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-methods"></a>

##### `Methods`

```csharp
IReadOnlyList<string> Methods { get; }
```

Gets the normalized request methods that this route matches.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-pathprefix"></a>

##### `PathPrefix`

```csharp
string PathPrefix { get; }
```

Gets the rooted public path prefix that matches this route.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-progresspercent"></a>

##### `ProgressPercent`

```csharp
int ProgressPercent { get; }
```

Gets the normalized migration-progress percentage for the route.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-progressstate"></a>

##### `ProgressState`

```csharp
string ProgressState { get; }
```

Gets the normalized migration-progress state for the route.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-requestedtarget"></a>

##### `RequestedTarget`

```csharp
StranglerFigTarget RequestedTarget { get; }
```

Gets the target requested after applying migration-policy overlays.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-requestedtargetsource"></a>

##### `RequestedTargetSource`

```csharp
string RequestedTargetSource { get; }
```

Gets the source of the requested target, such as `authored-route`, `migration-default`, or `migration-route`.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-routeid"></a>

##### `RouteId`

```csharp
string RouteId { get; }
```

Gets the stable route identifier.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-runtimemetadata"></a>

##### `RuntimeMetadata`

```csharp
IReadOnlyDictionary<string, string> RuntimeMetadata { get; }
```

Gets runtime-only metadata such as notes or overlay provenance.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-selectedendpoint"></a>

##### `SelectedEndpoint`

```csharp
string SelectedEndpoint { get; }
```

Gets the concrete endpoint or boundary identifier that will receive traffic.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-selectedendpointkind"></a>

##### `SelectedEndpointKind`

```csharp
string SelectedEndpointKind { get; }
```

Gets the normalized selected-endpoint kind, such as `local-path`, `absolute-uri`, or `opaque`.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-selectionmode"></a>

##### `SelectionMode`

```csharp
string SelectionMode { get; }
```

Gets the runtime selection result, such as `requested-target` or `fallback-target`.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that owns the modern Cephalon boundary for this route.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-targetpathprefix"></a>

##### `TargetPathPrefix`

```csharp
string TargetPathPrefix { get; }
```

Gets the normalized path prefix that traffic should land on when the selected endpoint is path-shaped.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-targetquery"></a>

##### `TargetQuery`

```csharp
string TargetQuery { get; }
```

Gets the normalized base query string that should flow with the selected endpoint when one exists.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigingressruntimedescriptor-targeturi"></a>

##### `TargetUri`

```csharp
string TargetUri { get; }
```

Gets the normalized absolute URI that traffic should target when the selected endpoint is absolute.

<a id="type-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor"></a>

### `StranglerFigMigrationRuntimeDescriptor`

Describes the effective runtime migration policy for one strangler-fig route.

#### Declaration
```csharp
public sealed class StranglerFigMigrationRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-patterns-stranglerfigtarget-cephalon-abstractions-patterns-stranglerfigtarget-cephalon-abstractions-patterns-stranglerfigtarget-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-int32-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `StranglerFigMigrationRuntimeDescriptor`

```csharp
StranglerFigMigrationRuntimeDescriptor(string routeId, string sourceModuleId, string displayName, string description, string pathPrefix, StranglerFigTarget authoredTarget, StranglerFigTarget requestedTarget, StranglerFigTarget effectiveTarget, string requestedTargetSource, string selectionMode, string selectedEndpoint, string legacyEndpoint, string modernEndpoint, IReadOnlyList<string> methods, string progressState, int progressPercent, IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, string> runtimeMetadata)
```

Creates a strangler-fig runtime migration descriptor.

Parameters:
- `routeId`: The stable route identifier.
- `sourceModuleId`: The Cephalon module that owns the modern boundary for this route.
- `displayName`: The operator-facing route name.
- `description`: The human-readable description of the migration boundary.
- `pathPrefix`: The rooted path prefix that this route matches.
- `authoredTarget`: The target preferred by the authored route descriptor.
- `requestedTarget`: The target requested after applying migration-policy overlays.
- `effectiveTarget`: The target that will actually receive traffic after endpoint fallback is considered.
- `requestedTargetSource`: The source of the requested target, such as `authored-route` or `migration-route`.
- `selectionMode`: The runtime selection result, such as `requested-target` or `fallback-target`.
- `selectedEndpoint`: The concrete endpoint or boundary identifier that will receive traffic.
- `legacyEndpoint`: The legacy boundary identifier or endpoint when one is configured.
- `modernEndpoint`: The modern Cephalon boundary identifier or endpoint when one is configured.
- `methods`: Optional request methods that this route matches.
- `progressState`: The normalized migration-progress state for the route.
- `progressPercent`: The normalized migration-progress percentage for the route.
- `metadata`: The original authored route metadata.
- `runtimeMetadata`: Additional runtime-only metadata such as notes or overlay provenance.

#### Properties

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-authoredtarget"></a>

##### `AuthoredTarget`

```csharp
StranglerFigTarget AuthoredTarget { get; }
```

Gets the target preferred by the authored route descriptor.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the migration boundary.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing route name.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-effectivetarget"></a>

##### `EffectiveTarget`

```csharp
StranglerFigTarget EffectiveTarget { get; }
```

Gets the target that will actually receive traffic after endpoint fallback is considered.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-legacyendpoint"></a>

##### `LegacyEndpoint`

```csharp
string LegacyEndpoint { get; }
```

Gets the legacy boundary identifier or endpoint when one is configured.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets the original authored route metadata.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-methods"></a>

##### `Methods`

```csharp
IReadOnlyList<string> Methods { get; }
```

Gets the normalized request methods that this route matches.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-modernendpoint"></a>

##### `ModernEndpoint`

```csharp
string ModernEndpoint { get; }
```

Gets the modern Cephalon boundary identifier or endpoint when one is configured.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-pathprefix"></a>

##### `PathPrefix`

```csharp
string PathPrefix { get; }
```

Gets the rooted path prefix that matches this route.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-progresspercent"></a>

##### `ProgressPercent`

```csharp
int ProgressPercent { get; }
```

Gets the normalized migration-progress percentage for the route.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-progressstate"></a>

##### `ProgressState`

```csharp
string ProgressState { get; }
```

Gets the normalized migration-progress state for the route.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-requestedtarget"></a>

##### `RequestedTarget`

```csharp
StranglerFigTarget RequestedTarget { get; }
```

Gets the target requested after applying migration-policy overlays.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-requestedtargetsource"></a>

##### `RequestedTargetSource`

```csharp
string RequestedTargetSource { get; }
```

Gets the source of the requested target, such as `authored-route`, `migration-default`, or `migration-route`.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-routeid"></a>

##### `RouteId`

```csharp
string RouteId { get; }
```

Gets the stable route identifier.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-runtimemetadata"></a>

##### `RuntimeMetadata`

```csharp
IReadOnlyDictionary<string, string> RuntimeMetadata { get; }
```

Gets runtime-only metadata such as notes or overlay provenance.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-selectedendpoint"></a>

##### `SelectedEndpoint`

```csharp
string SelectedEndpoint { get; }
```

Gets the concrete endpoint or boundary identifier that will receive traffic.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-selectionmode"></a>

##### `SelectionMode`

```csharp
string SelectionMode { get; }
```

Gets the runtime selection result, such as `requested-target` or `fallback-target`.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigmigrationruntimedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that owns the modern Cephalon boundary for this route.

<a id="type-cephalon-abstractions-patterns-stranglerfigrequest"></a>

### `StranglerFigRequest`

Describes one request that should be evaluated by a strangler-fig router.

#### Declaration
```csharp
public sealed class StranglerFigRequest
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-stranglerfigrequest-ctor-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `StranglerFigRequest`

```csharp
StranglerFigRequest(string path, string method, IReadOnlyDictionary<string, string> metadata)
```

Creates a new strangler-fig routing request.

Parameters:
- `path`: The request path or absolute URI that should be evaluated.
- `method`: The request method to evaluate, such as `GET` or `POST`.
- `metadata`: Optional host-specific metadata that can accompany the request.

#### Properties

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional host-specific metadata that accompanied the request.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrequest-method"></a>

##### `Method`

```csharp
string Method { get; }
```

Gets the normalized request method.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrequest-path"></a>

##### `Path`

```csharp
string Path { get; }
```

Gets the request path or absolute URI that should be evaluated.

<a id="type-cephalon-abstractions-patterns-stranglerfigroutedescriptor"></a>

### `StranglerFigRouteDescriptor`

Describes one strangler-fig route owned by a Cephalon module.

#### Declaration
```csharp
public sealed class StranglerFigRouteDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-stranglerfigroutedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-patterns-stranglerfigtarget-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `StranglerFigRouteDescriptor`

```csharp
StranglerFigRouteDescriptor(string id, string sourceModuleId, string displayName, string description, string pathPrefix, StranglerFigTarget preferredTarget, string legacyEndpoint, string modernEndpoint, IReadOnlyList<string> methods, IReadOnlyDictionary<string, string> metadata)
```

Creates a strangler-fig route descriptor.

Parameters:
- `id`: The stable route identifier.
- `sourceModuleId`: The Cephalon module that owns the modern boundary for this route.
- `displayName`: The operator-facing route name.
- `description`: The human-readable description of the migration boundary.
- `pathPrefix`: The rooted path prefix that this route should match.
- `preferredTarget`: The preferred boundary for matched requests.
- `legacyEndpoint`: The legacy boundary identifier or endpoint.
- `modernEndpoint`: The modern Cephalon boundary identifier or endpoint.
- `methods`: Optional request methods that this route should match.
- `metadata`: Optional route metadata.

#### Properties

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the migration boundary.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing route name.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable route identifier.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-legacyendpoint"></a>

##### `LegacyEndpoint`

```csharp
string LegacyEndpoint { get; }
```

Gets the legacy boundary identifier or endpoint when one is configured.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional route metadata.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-methods"></a>

##### `Methods`

```csharp
IReadOnlyList<string> Methods { get; }
```

Gets the normalized request methods that this route should match.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-modernendpoint"></a>

##### `ModernEndpoint`

```csharp
string ModernEndpoint { get; }
```

Gets the modern Cephalon boundary identifier or endpoint when one is configured.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-pathprefix"></a>

##### `PathPrefix`

```csharp
string PathPrefix { get; }
```

Gets the rooted path prefix that should match this route.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-preferredtarget"></a>

##### `PreferredTarget`

```csharp
StranglerFigTarget PreferredTarget { get; }
```

Gets the preferred boundary for matched requests.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigroutedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that owns the modern Cephalon boundary for this route.

<a id="type-cephalon-abstractions-patterns-stranglerfigrouteresolution"></a>

### `StranglerFigRouteResolution`

Describes the strangler-fig routing decision made for one request.

#### Declaration
```csharp
public sealed class StranglerFigRouteResolution
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-stranglerfigrouteresolution-ctor-system-string-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-patterns-stranglerfigtarget-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `StranglerFigRouteResolution`

```csharp
StranglerFigRouteResolution(string RouteId, string RouteDisplayName, string SourceModuleId, string RequestedPath, string RequestedMethod, string MatchedPathPrefix, StranglerFigTarget SelectedTarget, string SelectedEndpoint, string LegacyEndpoint, string ModernEndpoint, string ResolutionMode, IReadOnlyDictionary<string, string> Metadata)
```

Describes the strangler-fig routing decision made for one request.

Parameters:
- `RouteId`: The matched route identifier.
- `RouteDisplayName`: The operator-facing route name.
- `SourceModuleId`: The Cephalon module that owns the modern boundary.
- `RequestedPath`: The normalized request path that was evaluated.
- `RequestedMethod`: The normalized request method that was evaluated.
- `MatchedPathPrefix`: The normalized route prefix that matched the request.
- `SelectedTarget`: The migration boundary chosen for the request.
- `SelectedEndpoint`: The concrete endpoint or boundary identifier that should receive the request.
- `LegacyEndpoint`: The configured legacy endpoint when one exists.
- `ModernEndpoint`: The configured modern endpoint when one exists.
- `ResolutionMode`: The reason the boundary was chosen, such as `preferred-target` or `fallback-target`.
- `Metadata`: Additional route metadata that traveled with the decision.

#### Properties

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-legacyendpoint"></a>

##### `LegacyEndpoint`

```csharp
string LegacyEndpoint { get; set; }
```

The configured legacy endpoint when one exists.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-matchedpathprefix"></a>

##### `MatchedPathPrefix`

```csharp
string MatchedPathPrefix { get; set; }
```

The normalized route prefix that matched the request.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Additional route metadata that traveled with the decision.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-modernendpoint"></a>

##### `ModernEndpoint`

```csharp
string ModernEndpoint { get; set; }
```

The configured modern endpoint when one exists.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-requestedmethod"></a>

##### `RequestedMethod`

```csharp
string RequestedMethod { get; set; }
```

The normalized request method that was evaluated.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-requestedpath"></a>

##### `RequestedPath`

```csharp
string RequestedPath { get; set; }
```

The normalized request path that was evaluated.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-resolutionmode"></a>

##### `ResolutionMode`

```csharp
string ResolutionMode { get; set; }
```

The reason the boundary was chosen, such as `preferred-target` or `fallback-target`.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-routedisplayname"></a>

##### `RouteDisplayName`

```csharp
string RouteDisplayName { get; set; }
```

The operator-facing route name.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-routeid"></a>

##### `RouteId`

```csharp
string RouteId { get; set; }
```

The matched route identifier.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-selectedendpoint"></a>

##### `SelectedEndpoint`

```csharp
string SelectedEndpoint { get; set; }
```

The concrete endpoint or boundary identifier that should receive the request.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-selectedtarget"></a>

##### `SelectedTarget`

```csharp
StranglerFigTarget SelectedTarget { get; set; }
```

The migration boundary chosen for the request.

<a id="member-p-cephalon-abstractions-patterns-stranglerfigrouteresolution-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

The Cephalon module that owns the modern boundary.

<a id="type-cephalon-abstractions-patterns-stranglerfigtarget"></a>

### `StranglerFigTarget`

Identifies which side of a strangler-fig migration boundary currently owns traffic.

#### Declaration
```csharp
public enum StranglerFigTarget
```

#### Fields

<a id="member-f-cephalon-abstractions-patterns-stranglerfigtarget-legacy"></a>

##### `Legacy`

```csharp
const StranglerFigTarget Legacy
```

Routes traffic to the legacy boundary.

<a id="member-f-cephalon-abstractions-patterns-stranglerfigtarget-modern"></a>

##### `Modern`

```csharp
const StranglerFigTarget Modern
```

Routes traffic to the modern Cephalon boundary.

<a id="namespace-cephalon-abstractions-resilience"></a>

## Namespace Cephalon.Abstractions.Resilience

<a id="type-cephalon-abstractions-resilience-behaviorexecutionresilienceselection"></a>

### `BehaviorExecutionResilienceSelection`

Describes the subset of resilience policy selections that apply to behavior execution pipelines.

#### Declaration
```csharp
public sealed class BehaviorExecutionResilienceSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-ctor-cephalon-abstractions-appmodel-retryselection-cephalon-abstractions-appmodel-timeoutselection-cephalon-abstractions-appmodel-circuitbreakerselection-cephalon-abstractions-appmodel-bulkheadselection-cephalon-abstractions-appmodel-ratelimitingselection"></a>

##### `BehaviorExecutionResilienceSelection`

```csharp
BehaviorExecutionResilienceSelection(RetrySelection retry, TimeoutSelection timeout, CircuitBreakerSelection circuitBreaker, BulkheadSelection bulkhead, RateLimitingSelection rateLimiting)
```

Initializes a new instance of the `BehaviorExecutionResilienceSelection` class.

Parameters:
- `retry`: The retry selection that applies to behavior execution.
- `timeout`: The timeout selection that applies to behavior execution.
- `circuitBreaker`: The circuit-breaker selection that applies to behavior execution.
- `bulkhead`: The bulkhead selection that applies to behavior execution.
- `rateLimiting`: The rate-limiting selection that applies to behavior execution.

#### Properties

<a id="member-p-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-bulkhead"></a>

##### `Bulkhead`

```csharp
BulkheadSelection Bulkhead { get; }
```

Gets the bulkhead selection that applies to behavior execution.

<a id="member-p-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-circuitbreaker"></a>

##### `CircuitBreaker`

```csharp
CircuitBreakerSelection CircuitBreaker { get; }
```

Gets the circuit-breaker selection that applies to behavior execution.

<a id="member-p-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-empty"></a>

##### `Empty`

```csharp
BehaviorExecutionResilienceSelection Empty { get; }
```

Gets an empty behavior-execution resilience selection.

<a id="member-p-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any behavior-execution resilience inputs were supplied.

<a id="member-p-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-ratelimiting"></a>

##### `RateLimiting`

```csharp
RateLimitingSelection RateLimiting { get; }
```

Gets the rate-limiting selection that applies to behavior execution.

<a id="member-p-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-retry"></a>

##### `Retry`

```csharp
RetrySelection Retry { get; }
```

Gets the retry selection that applies to behavior execution.

<a id="member-p-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-timeout"></a>

##### `Timeout`

```csharp
TimeoutSelection Timeout { get; }
```

Gets the timeout selection that applies to behavior execution.

<a id="type-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext"></a>

### `BehaviorResilienceExceptionContext`

Describes one behavior-execution exception being evaluated by the resilience pipeline.

#### Declaration
```csharp
public sealed class BehaviorResilienceExceptionContext
```

#### Constructors

<a id="member-m-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-exception-cephalon-abstractions-behaviors-behavioridempotencymode"></a>

##### `BehaviorResilienceExceptionContext`

```csharp
BehaviorResilienceExceptionContext(string policyId, string behaviorId, string transportId, IReadOnlyList<string> targetedBehaviorIds, IReadOnlyList<string> targetedTransportIds, Exception exception, BehaviorIdempotencyMode behaviorIdempotency)
```

Initializes a new instance of the `BehaviorResilienceExceptionContext` class.

Parameters:
- `policyId`: The stable resilience-policy identifier handling the exception.
- `behaviorId`: The stable behavior identifier being executed.
- `transportId`: The active transport identifier when one is known.
- `targetedBehaviorIds`: The behavior identifiers targeted by the active policy.
- `targetedTransportIds`: The transport identifiers targeted by the active policy.
- `exception`: The exception being classified.
- `behaviorIdempotency`: The declared behavior idempotency mode when one is known.

#### Properties

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the stable behavior identifier being executed.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext-behavioridempotency"></a>

##### `BehaviorIdempotency`

```csharp
BehaviorIdempotencyMode BehaviorIdempotency { get; }
```

Gets the declared behavior idempotency mode when one is known.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext-exception"></a>

##### `Exception`

```csharp
Exception Exception { get; }
```

Gets the exception being classified.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext-policyid"></a>

##### `PolicyId`

```csharp
string PolicyId { get; }
```

Gets the stable resilience-policy identifier handling the exception.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext-targetedbehaviorids"></a>

##### `TargetedBehaviorIds`

```csharp
IReadOnlyList<string> TargetedBehaviorIds { get; }
```

Gets the behavior identifiers targeted by the active policy.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext-targetedtransportids"></a>

##### `TargetedTransportIds`

```csharp
IReadOnlyList<string> TargetedTransportIds { get; }
```

Gets the transport identifiers targeted by the active policy.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext-transportid"></a>

##### `TransportId`

```csharp
string TransportId { get; }
```

Gets the active transport identifier when one is known.

<a id="type-cephalon-abstractions-resilience-behaviorresilienceexceptionhandling"></a>

### `BehaviorResilienceExceptionHandling`

Describes how a behavior-execution exception should participate in resilience handling.

#### Declaration
```csharp
public enum BehaviorResilienceExceptionHandling
```

#### Fields

<a id="member-f-cephalon-abstractions-resilience-behaviorresilienceexceptionhandling-ignore"></a>

##### `Ignore`

```csharp
const BehaviorResilienceExceptionHandling Ignore
```

Ignore the exception for resilience accounting.

<a id="member-f-cephalon-abstractions-resilience-behaviorresilienceexceptionhandling-retryandtrip"></a>

##### `RetryAndTrip`

```csharp
const BehaviorResilienceExceptionHandling RetryAndTrip
```

Count the exception for circuit-breaker accounting and treat it as eligible for future retry handling.

<a id="member-f-cephalon-abstractions-resilience-behaviorresilienceexceptionhandling-triponly"></a>

##### `TripOnly`

```csharp
const BehaviorResilienceExceptionHandling TripOnly
```

Count the exception for circuit-breaker style failure accounting, but do not automatically retry it.

<a id="type-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor"></a>

### `BehaviorResilienceRuntimeDescriptor`

Describes one effective behavior-execution resilience policy exposed by the current runtime.

#### Declaration
```csharp
public sealed class BehaviorResilienceRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-cephalon-abstractions-resilience-behaviorexecutionresilienceselection-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `BehaviorResilienceRuntimeDescriptor`

```csharp
BehaviorResilienceRuntimeDescriptor(string Id, string DisplayName, string Description, string ExecutionMode, string Scope, IReadOnlyList<string> BehaviorIds, IReadOnlyList<string> TransportIds, BehaviorExecutionResilienceSelection Requested, BehaviorExecutionResilienceSelection Effective, IReadOnlyDictionary<string, string> Metadata)
```

Describes one effective behavior-execution resilience policy exposed by the current runtime.

Parameters:
- `Id`: The stable runtime policy identifier.
- `DisplayName`: The human-readable policy name.
- `Description`: The human-readable policy description.
- `ExecutionMode`: The enforcement mode used by the active runtime, such as `behavior-dispatch-middleware` or `contract-only`.
- `Scope`: The runtime scope covered by the policy, such as `all-behavior-executions`.
- `BehaviorIds`: The behavior identifiers covered by the policy when it is scoped to a behavior subset.
- `TransportIds`: The transport identifiers covered by the policy when it is scoped to a transport subset.
- `Requested`: The requested behavior-execution resilience contract.
- `Effective`: The effective behavior-execution resilience contract after runtime normalization.
- `Metadata`: Additional runtime-specific metadata describing the policy.

#### Properties

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-behaviorids"></a>

##### `BehaviorIds`

```csharp
IReadOnlyList<string> BehaviorIds { get; set; }
```

The behavior identifiers covered by the policy when it is scoped to a behavior subset.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

The human-readable policy description.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

The human-readable policy name.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-effective"></a>

##### `Effective`

```csharp
BehaviorExecutionResilienceSelection Effective { get; set; }
```

The effective behavior-execution resilience contract after runtime normalization.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-executionmode"></a>

##### `ExecutionMode`

```csharp
string ExecutionMode { get; set; }
```

The enforcement mode used by the active runtime, such as `behavior-dispatch-middleware` or `contract-only`.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

The stable runtime policy identifier.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Additional runtime-specific metadata describing the policy.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-requested"></a>

##### `Requested`

```csharp
BehaviorExecutionResilienceSelection Requested { get; set; }
```

The requested behavior-execution resilience contract.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-scope"></a>

##### `Scope`

```csharp
string Scope { get; set; }
```

The runtime scope covered by the policy, such as `all-behavior-executions`.

<a id="member-p-cephalon-abstractions-resilience-behaviorresilienceruntimedescriptor-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; set; }
```

The transport identifiers covered by the policy when it is scoped to a transport subset.

<a id="type-cephalon-abstractions-resilience-ibehaviorresilienceexceptionclassifier"></a>

### `IBehaviorResilienceExceptionClassifier`

Classifies behavior-execution exceptions for resilience handling.

Remarks: This contract lets hosts or companion packs decide which failures should count toward circuit-breaker style failure accounting and which ones should stay outside resilience automation because they represent business or validation outcomes.

#### Declaration
```csharp
public interface IBehaviorResilienceExceptionClassifier
```

#### Methods

<a id="member-m-cephalon-abstractions-resilience-ibehaviorresilienceexceptionclassifier-classify-cephalon-abstractions-resilience-behaviorresilienceexceptioncontext"></a>

##### `Classify`

```csharp
BehaviorResilienceExceptionHandling Classify(BehaviorResilienceExceptionContext context)
```

Classifies one behavior-execution exception.

Returns: The resilience-handling mode that should apply.

Parameters:
- `context`: The exception context being evaluated.

<a id="type-cephalon-abstractions-resilience-ibehaviorresilienceruntimecatalog"></a>

### `IBehaviorResilienceRuntimeCatalog`

Exposes the active behavior-execution resilience policies visible to the current runtime.

Remarks: This runtime-facing surface reports what the behavior dispatch pipeline actually enforces after defaults and implementation limits have been applied. It complements the requested contract projected through `AppProfile.Resilience`.

#### Declaration
```csharp
public interface IBehaviorResilienceRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-resilience-ibehaviorresilienceruntimecatalog-policies"></a>

##### `Policies`

```csharp
IReadOnlyList<BehaviorResilienceRuntimeDescriptor> Policies { get; }
```

Gets all behavior-execution resilience policies visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-resilience-ibehaviorresilienceruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
BehaviorResilienceRuntimeDescriptor GetById(string policyId)
```

Gets one behavior-execution resilience policy by its stable identifier.

Returns: The matching policy descriptor, or `null` when it is not active.

Parameters:
- `policyId`: The stable policy identifier to resolve.

<a id="member-m-cephalon-abstractions-resilience-ibehaviorresilienceruntimecatalog-resolve-system-string-system-string"></a>

##### `Resolve`

```csharp
BehaviorResilienceRuntimeDescriptor Resolve(string behaviorId, string transportId)
```

Resolves the effective behavior-execution resilience policy for one behavior and optional transport.

Returns: The matched policy descriptor, including explicit disable overrides when one suppresses the default policy; otherwise `null` when no behavior-execution policy applies.

Parameters:
- `behaviorId`: The stable behavior identifier to resolve.
- `transportId`: The stable transport identifier when one is known.

<a id="type-cephalon-abstractions-resilience-iratelimitingruntimecatalog"></a>

### `IRateLimitingRuntimeCatalog`

Exposes the active HTTP rate-limiting policies visible to the current runtime.

Remarks: Implementations describe the effective policy applied by the active host adapter, such as ASP.NET Core middleware-based request limiting. This surface is runtime-facing rather than app-model-facing because it reflects what the host actually enforces after defaults and host exclusions have been applied.

#### Declaration
```csharp
public interface IRateLimitingRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-resilience-iratelimitingruntimecatalog-policies"></a>

##### `Policies`

```csharp
IReadOnlyList<RateLimitingRuntimeDescriptor> Policies { get; }
```

Gets all rate-limiting policies visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-resilience-iratelimitingruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
RateLimitingRuntimeDescriptor GetById(string policyId)
```

Gets one rate-limiting policy by its stable identifier.

Returns: The matching policy descriptor, or `null` when it is not active.

Parameters:
- `policyId`: The policy identifier to resolve.

<a id="member-m-cephalon-abstractions-resilience-iratelimitingruntimecatalog-getbytransportid-system-string"></a>

##### `GetByTransportId`

```csharp
IReadOnlyList<RateLimitingRuntimeDescriptor> GetByTransportId(string transportId)
```

Gets all rate-limiting policies that apply to the requested transport identifier.

Returns: The matching policies, or an empty list when none target the transport.

Parameters:
- `transportId`: The stable transport identifier to filter by.

<a id="type-cephalon-abstractions-resilience-ratelimitingruntimedescriptor"></a>

### `RateLimitingRuntimeDescriptor`

Describes one effective HTTP rate-limiting policy exposed by the current runtime.

#### Declaration
```csharp
public sealed class RateLimitingRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-cephalon-abstractions-appmodel-ratelimitingselection-cephalon-abstractions-appmodel-ratelimitingselection-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `RateLimitingRuntimeDescriptor`

```csharp
RateLimitingRuntimeDescriptor(string Id, string DisplayName, string Description, string ExecutionMode, string Scope, int RejectionStatusCode, IReadOnlyList<string> TransportIds, IReadOnlyList<string> ExcludedPathPrefixes, RateLimitingSelection Requested, RateLimitingSelection Effective, IReadOnlyDictionary<string, string> Metadata)
```

Describes one effective HTTP rate-limiting policy exposed by the current runtime.

Parameters:
- `Id`: The stable runtime policy identifier.
- `DisplayName`: The human-readable policy name.
- `Description`: The human-readable policy description.
- `ExecutionMode`: The enforcement mode used by the active host, such as `aspnetcore-global-middleware` or `disabled`.
- `Scope`: The runtime scope covered by the policy, such as `public-http-endpoints`.
- `RejectionStatusCode`: The HTTP status code returned when the limiter rejects a request.
- `TransportIds`: The transport identifiers whose HTTP surfaces are covered by the policy.
- `ExcludedPathPrefixes`: The rooted path prefixes intentionally excluded from enforcement, such as operator or documentation routes.
- `Requested`: The requested app-model selection that asked for rate limiting.
- `Effective`: The effective policy values after host defaults and adapter-specific normalization have been applied.
- `Metadata`: Additional host-specific metadata describing the policy.

#### Properties

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

The human-readable policy description.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

The human-readable policy name.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-effective"></a>

##### `Effective`

```csharp
RateLimitingSelection Effective { get; set; }
```

The effective policy values after host defaults and adapter-specific normalization have been applied.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-excludedpathprefixes"></a>

##### `ExcludedPathPrefixes`

```csharp
IReadOnlyList<string> ExcludedPathPrefixes { get; set; }
```

The rooted path prefixes intentionally excluded from enforcement, such as operator or documentation routes.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-executionmode"></a>

##### `ExecutionMode`

```csharp
string ExecutionMode { get; set; }
```

The enforcement mode used by the active host, such as `aspnetcore-global-middleware` or `disabled`.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

The stable runtime policy identifier.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Additional host-specific metadata describing the policy.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-rejectionstatuscode"></a>

##### `RejectionStatusCode`

```csharp
int RejectionStatusCode { get; set; }
```

The HTTP status code returned when the limiter rejects a request.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-requested"></a>

##### `Requested`

```csharp
RateLimitingSelection Requested { get; set; }
```

The requested app-model selection that asked for rate limiting.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-scope"></a>

##### `Scope`

```csharp
string Scope { get; set; }
```

The runtime scope covered by the policy, such as `public-http-endpoints`.

<a id="member-p-cephalon-abstractions-resilience-ratelimitingruntimedescriptor-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; set; }
```

The transport identifiers whose HTTP surfaces are covered by the policy.

<a id="namespace-cephalon-abstractions-technologies"></a>

## Namespace Cephalon.Abstractions.Technologies

<a id="type-cephalon-abstractions-technologies-cellboundarydescriptor"></a>

### `CellBoundaryDescriptor`

Describes one module-owned cell boundary visible to the active Cephalon runtime.

#### Declaration
```csharp
public sealed class CellBoundaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-cellboundarydescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CellBoundaryDescriptor`

```csharp
CellBoundaryDescriptor(string id, string sourceModuleId, string displayName, string description, string blastRadius, string routingStrategy, IReadOnlyList<string> moduleIds, IReadOnlyDictionary<string, string> metadata)
```

Creates a cell-boundary descriptor.

Parameters:
- `id`: The stable cell identifier.
- `sourceModuleId`: The Cephalon module that owns this cell boundary.
- `displayName`: The operator-facing cell name.
- `description`: The human-readable description of the cell boundary.
- `blastRadius`: The operator-facing blast-radius posture for this cell.
- `routingStrategy`: The operator-facing routing strategy applied to this cell.
- `moduleIds`: The module identifiers that belong to this cell boundary.
- `metadata`: Optional operator-facing metadata.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-cellboundarydescriptor-blastradius"></a>

##### `BlastRadius`

```csharp
string BlastRadius { get; }
```

Gets the operator-facing blast-radius posture for this cell.

<a id="member-p-cephalon-abstractions-technologies-cellboundarydescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the cell boundary.

<a id="member-p-cephalon-abstractions-technologies-cellboundarydescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing cell name.

<a id="member-p-cephalon-abstractions-technologies-cellboundarydescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable cell identifier.

<a id="member-p-cephalon-abstractions-technologies-cellboundarydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata for this cell boundary.

<a id="member-p-cephalon-abstractions-technologies-cellboundarydescriptor-moduleids"></a>

##### `ModuleIds`

```csharp
IReadOnlyList<string> ModuleIds { get; }
```

Gets the module identifiers that belong to this cell boundary.

<a id="member-p-cephalon-abstractions-technologies-cellboundarydescriptor-routingstrategy"></a>

##### `RoutingStrategy`

```csharp
string RoutingStrategy { get; }
```

Gets the operator-facing routing strategy for this cell.

<a id="member-p-cephalon-abstractions-technologies-cellboundarydescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that owns this cell boundary.

<a id="type-cephalon-abstractions-technologies-cellhealthisolationdescriptor"></a>

### `CellHealthIsolationDescriptor`

Describes one module-owned cell health-isolation answer visible to the active runtime.

#### Declaration
```csharp
public sealed class CellHealthIsolationDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-cellhealthisolationdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CellHealthIsolationDescriptor`

```csharp
CellHealthIsolationDescriptor(string id, string sourceModuleId, string cellId, string displayName, string description, string failureIsolationMode, string readinessScope, string restartScope, IReadOnlyList<string> dependencyIds, IReadOnlyDictionary<string, string> metadata)
```

Creates a cell health-isolation descriptor.

Parameters:
- `id`: The stable health-isolation identifier.
- `sourceModuleId`: The Cephalon module that owns this health-isolation answer.
- `cellId`: The cell identifier governed by this health-isolation answer.
- `displayName`: The operator-facing health-isolation name.
- `description`: The human-readable description of the health-isolation posture.
- `failureIsolationMode`: The operator-facing failure-isolation mode for this cell.
- `readinessScope`: The operator-facing readiness scope used for this cell.
- `restartScope`: The operator-facing restart scope used for this cell.
- `dependencyIds`: Optional dependency identifiers associated with this health-isolation answer.
- `metadata`: Optional operator-facing metadata.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-cellid"></a>

##### `CellId`

```csharp
string CellId { get; }
```

Gets the cell identifier governed by this health-isolation answer.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-dependencyids"></a>

##### `DependencyIds`

```csharp
IReadOnlyList<string> DependencyIds { get; }
```

Gets the normalized dependency identifiers associated with this health-isolation answer.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the health-isolation posture.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing health-isolation name.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-failureisolationmode"></a>

##### `FailureIsolationMode`

```csharp
string FailureIsolationMode { get; }
```

Gets the operator-facing failure-isolation mode for this cell.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable health-isolation identifier.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata for this health-isolation answer.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-readinessscope"></a>

##### `ReadinessScope`

```csharp
string ReadinessScope { get; }
```

Gets the operator-facing readiness scope for this cell.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-restartscope"></a>

##### `RestartScope`

```csharp
string RestartScope { get; }
```

Gets the operator-facing restart scope for this cell.

<a id="member-p-cephalon-abstractions-technologies-cellhealthisolationdescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that owns this health-isolation answer.

<a id="type-cephalon-abstractions-technologies-cellroutedescriptor"></a>

### `CellRouteDescriptor`

Describes one module-owned cell-to-cell routing and governance answer visible to the active runtime.

#### Declaration
```csharp
public sealed class CellRouteDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-cellroutedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CellRouteDescriptor`

```csharp
CellRouteDescriptor(string id, string sourceModuleId, string sourceCellId, string targetCellId, string displayName, string description, string routingStrategy, string governanceMode, IReadOnlyList<string> transportIds, string requiredCapabilityKey, IReadOnlyDictionary<string, string> metadata)
```

Creates a cell-route descriptor.

Parameters:
- `id`: The stable cell-route identifier.
- `sourceModuleId`: The Cephalon module that owns this cell route.
- `sourceCellId`: The source cell identifier.
- `targetCellId`: The target cell identifier.
- `displayName`: The operator-facing route name.
- `description`: The human-readable description of the cell route.
- `routingStrategy`: The operator-facing routing strategy used for this route.
- `governanceMode`: The operator-facing governance posture applied to this route.
- `transportIds`: Optional transport identifiers associated with this route.
- `requiredCapabilityKey`: An optional capability key required to use this route.
- `metadata`: Optional operator-facing metadata.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the cell route.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing route name.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-governancemode"></a>

##### `GovernanceMode`

```csharp
string GovernanceMode { get; }
```

Gets the operator-facing governance posture applied to this route.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable cell-route identifier.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata for this route.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-requiredcapabilitykey"></a>

##### `RequiredCapabilityKey`

```csharp
string RequiredCapabilityKey { get; }
```

Gets the optional capability key required to use this route.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-routingstrategy"></a>

##### `RoutingStrategy`

```csharp
string RoutingStrategy { get; }
```

Gets the operator-facing routing strategy used for this route.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-sourcecellid"></a>

##### `SourceCellId`

```csharp
string SourceCellId { get; }
```

Gets the source cell identifier.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that owns this cell route.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-targetcellid"></a>

##### `TargetCellId`

```csharp
string TargetCellId { get; }
```

Gets the target cell identifier.

<a id="member-p-cephalon-abstractions-technologies-cellroutedescriptor-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; }
```

Gets the normalized transport identifiers associated with this route.

<a id="type-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationresult"></a>

### `CellTrafficAutomationProviderMaterializationResult`

Describes one provider-managed materialization result for a cell traffic automation answer.

#### Declaration
```csharp
public sealed class CellTrafficAutomationProviderMaterializationResult
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationresult-ctor-system-string-system-datetimeoffset-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CellTrafficAutomationProviderMaterializationResult`

```csharp
CellTrafficAutomationProviderMaterializationResult(string state, DateTimeOffset observedAtUtc, string error, IReadOnlyDictionary<string, string> metadata)
```

Creates a provider-managed materialization result.

Parameters:
- `state`: The stable provider-materialization state, such as `applied` or `failed`.
- `observedAtUtc`: The UTC timestamp when the result was observed.
- `error`: The operator-facing error summary when the materialization failed.
- `metadata`: Optional provider-facing metadata captured alongside the result.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationresult-error"></a>

##### `Error`

```csharp
string Error { get; }
```

Gets the operator-facing error summary when the materialization failed.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional provider-facing metadata captured alongside the result.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationresult-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; }
```

Gets the UTC timestamp when the result was observed.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationresult-state"></a>

##### `State`

```csharp
string State { get; }
```

Gets the stable provider-materialization state.

<a id="type-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationstates"></a>

### `CellTrafficAutomationProviderMaterializationStates`

Defines the stable provider-materialization states for cell traffic automation runtime answers.

#### Declaration
```csharp
public static class CellTrafficAutomationProviderMaterializationStates
```

#### Fields

<a id="member-f-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationstates-applied"></a>

##### `Applied`

```csharp
const string Applied
```

The automation was reconciled successfully by the selected provider materializer.

<a id="member-f-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationstates-failed"></a>

##### `Failed`

```csharp
const string Failed
```

The selected provider materializer last reported a failure while reconciling the automation.

<a id="member-f-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationstates-pending"></a>

##### `Pending`

```csharp
const string Pending
```

The automation targets a provider and an active materializer is expected to reconcile it.

<a id="member-f-cephalon-abstractions-technologies-celltrafficautomationprovidermaterializationstates-unavailable"></a>

##### `Unavailable`

```csharp
const string Unavailable
```

The automation targets a provider but no active materializer can apply it.

<a id="type-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor"></a>

### `CellTrafficAutomationRuntimeDescriptor`

Describes the effective runtime traffic-automation answer for one governed cell route.

#### Declaration
```csharp
public sealed class CellTrafficAutomationRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CellTrafficAutomationRuntimeDescriptor`

```csharp
CellTrafficAutomationRuntimeDescriptor(string id, string routeId, string sourceModuleId, string sourceCellId, string targetCellId, string displayName, string description, string routingStrategy, string governanceMode, string automationMode, string triggerMode, string actionMode, string materializationMode, string policySource, IReadOnlyList<string> transportIds, string requiredCapabilityKey, IReadOnlyList<string> sourceHealthIsolationIds, IReadOnlyList<string> targetHealthIsolationIds, IReadOnlyList<string> dependencyIds, IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, string> runtimeMetadata)
```

Creates a cell traffic-automation runtime descriptor.

Parameters:
- `id`: The stable traffic-automation identifier.
- `routeId`: The stable governed cell-route identifier that this automation answer applies to.
- `sourceModuleId`: The Cephalon module that owns the governed route.
- `sourceCellId`: The source cell identifier.
- `targetCellId`: The target cell identifier.
- `displayName`: The operator-facing traffic-automation name.
- `description`: The human-readable description of the traffic-automation posture.
- `routingStrategy`: The operator-facing routing strategy inherited from the governed route.
- `governanceMode`: The operator-facing governance posture inherited from the governed route.
- `automationMode`: The normalized automation posture, such as `advisory` or `automatic`.
- `triggerMode`: The normalized trigger posture, such as `source-health` or `source-or-target-health`.
- `actionMode`: The normalized action posture, such as `quarantine-route` or `shed-load`.
- `materializationMode`: The normalized materialization posture, such as `runtime-catalog-only` or `provider-managed`.
- `policySource`: The source of the effective automation policy, such as `cell-default` or `cell-route`.
- `transportIds`: Optional transport identifiers inherited from the governed route.
- `requiredCapabilityKey`: An optional capability key inherited from the governed route.
- `sourceHealthIsolationIds`: The normalized health-isolation identifiers attached to the source cell.
- `targetHealthIsolationIds`: The normalized health-isolation identifiers attached to the target cell.
- `dependencyIds`: The normalized dependency identifiers observed across the related health-isolation answers.
- `metadata`: The original authored route metadata.
- `runtimeMetadata`: Additional runtime-only metadata such as policy notes or overlay provenance.

<a id="member-m-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `CellTrafficAutomationRuntimeDescriptor`

```csharp
CellTrafficAutomationRuntimeDescriptor(string id, string routeId, string sourceModuleId, string sourceCellId, string targetCellId, string displayName, string description, string routingStrategy, string governanceMode, string automationMode, string triggerMode, string actionMode, string materializationMode, string policySource, IReadOnlyList<string> transportIds, string requiredCapabilityKey, IReadOnlyList<string> sourceHealthIsolationIds, IReadOnlyList<string> targetHealthIsolationIds, IReadOnlyList<string> dependencyIds, IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, string> runtimeMetadata, string providerId, IReadOnlyList<string> edgeNodeIds)
```

Creates a cell traffic-automation runtime descriptor with provider and edge targeting.

Parameters:
- `id`: The stable traffic-automation identifier.
- `routeId`: The stable governed cell-route identifier that this automation answer applies to.
- `sourceModuleId`: The Cephalon module that owns the governed route.
- `sourceCellId`: The source cell identifier.
- `targetCellId`: The target cell identifier.
- `displayName`: The operator-facing traffic-automation name.
- `description`: The human-readable description of the traffic-automation posture.
- `routingStrategy`: The operator-facing routing strategy inherited from the governed route.
- `governanceMode`: The operator-facing governance posture inherited from the governed route.
- `automationMode`: The normalized automation posture, such as `advisory` or `automatic`.
- `triggerMode`: The normalized trigger posture, such as `source-health` or `source-or-target-health`.
- `actionMode`: The normalized action posture, such as `quarantine-route` or `shed-load`.
- `materializationMode`: The normalized materialization posture, such as `runtime-catalog-only`, `provider-managed`, or `edge-managed`.
- `policySource`: The source of the effective automation policy, such as `cell-default` or `cell-route`.
- `transportIds`: Optional transport identifiers inherited from the governed route.
- `requiredCapabilityKey`: An optional capability key inherited from the governed route.
- `sourceHealthIsolationIds`: The normalized health-isolation identifiers attached to the source cell.
- `targetHealthIsolationIds`: The normalized health-isolation identifiers attached to the target cell.
- `dependencyIds`: The normalized dependency identifiers observed across the related health-isolation answers.
- `metadata`: The original authored route metadata.
- `runtimeMetadata`: Additional runtime-only metadata such as policy notes or overlay provenance.
- `providerId`: The optional external provider or control-plane identifier that materializes this automation.
- `edgeNodeIds`: The optional edge-node identifiers associated with this automation answer.

<a id="member-m-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-string"></a>

##### `CellTrafficAutomationRuntimeDescriptor`

```csharp
CellTrafficAutomationRuntimeDescriptor(string id, string routeId, string sourceModuleId, string sourceCellId, string targetCellId, string displayName, string description, string routingStrategy, string governanceMode, string automationMode, string triggerMode, string actionMode, string materializationMode, string policySource, IReadOnlyList<string> transportIds, string requiredCapabilityKey, IReadOnlyList<string> sourceHealthIsolationIds, IReadOnlyList<string> targetHealthIsolationIds, IReadOnlyList<string> dependencyIds, IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, string> runtimeMetadata, string providerId, IReadOnlyList<string> edgeNodeIds, string providerMaterializerId, string providerMaterializationState, DateTimeOffset? providerMaterializationObservedAtUtc, string providerMaterializationError)
```

Creates a cell traffic-automation runtime descriptor with provider, edge, and provider-materialization state.

Parameters:
- `id`: The stable traffic-automation identifier.
- `routeId`: The stable governed cell-route identifier that this automation answer applies to.
- `sourceModuleId`: The Cephalon module that owns the governed route.
- `sourceCellId`: The source cell identifier.
- `targetCellId`: The target cell identifier.
- `displayName`: The operator-facing traffic-automation name.
- `description`: The human-readable description of the traffic-automation posture.
- `routingStrategy`: The operator-facing routing strategy inherited from the governed route.
- `governanceMode`: The operator-facing governance posture inherited from the governed route.
- `automationMode`: The normalized automation posture, such as `advisory` or `automatic`.
- `triggerMode`: The normalized trigger posture, such as `source-health` or `source-or-target-health`.
- `actionMode`: The normalized action posture, such as `quarantine-route` or `shed-load`.
- `materializationMode`: The normalized materialization posture, such as `runtime-catalog-only`, `provider-managed`, or `edge-managed`.
- `policySource`: The source of the effective automation policy, such as `cell-default` or `cell-route`.
- `transportIds`: Optional transport identifiers inherited from the governed route.
- `requiredCapabilityKey`: An optional capability key inherited from the governed route.
- `sourceHealthIsolationIds`: The normalized health-isolation identifiers attached to the source cell.
- `targetHealthIsolationIds`: The normalized health-isolation identifiers attached to the target cell.
- `dependencyIds`: The normalized dependency identifiers observed across the related health-isolation answers.
- `metadata`: The original authored route metadata.
- `runtimeMetadata`: Additional runtime-only metadata such as policy notes or overlay provenance.
- `providerId`: The optional external provider or control-plane identifier that materializes this automation.
- `edgeNodeIds`: The optional edge-node identifiers associated with this automation answer.
- `providerMaterializerId`: The optional selected provider materializer identifier.
- `providerMaterializationState`: The optional provider-materialization state for this automation.
- `providerMaterializationObservedAtUtc`: The optional UTC timestamp when the provider-materialization state was last observed.
- `providerMaterializationError`: The optional operator-facing provider-materialization error summary.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-actionmode"></a>

##### `ActionMode`

```csharp
string ActionMode { get; }
```

Gets the normalized action posture.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-automationmode"></a>

##### `AutomationMode`

```csharp
string AutomationMode { get; }
```

Gets the normalized automation posture.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-dependencyids"></a>

##### `DependencyIds`

```csharp
IReadOnlyList<string> DependencyIds { get; }
```

Gets the normalized dependency identifiers observed across the related health-isolation answers.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the traffic-automation posture.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing traffic-automation name.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-edgenodeids"></a>

##### `EdgeNodeIds`

```csharp
IReadOnlyList<string> EdgeNodeIds { get; }
```

Gets the optional edge-node identifiers associated with this automation answer.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-governancemode"></a>

##### `GovernanceMode`

```csharp
string GovernanceMode { get; }
```

Gets the governance posture inherited from the governed route.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable traffic-automation identifier.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-materializationmode"></a>

##### `MaterializationMode`

```csharp
string MaterializationMode { get; }
```

Gets the normalized materialization posture.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets the original authored route metadata.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-policysource"></a>

##### `PolicySource`

```csharp
string PolicySource { get; }
```

Gets the source of the effective automation policy.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-providerid"></a>

##### `ProviderId`

```csharp
string ProviderId { get; }
```

Gets the optional external provider or control-plane identifier that materializes this automation.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-providermaterializationerror"></a>

##### `ProviderMaterializationError`

```csharp
string ProviderMaterializationError { get; }
```

Gets the optional operator-facing provider-materialization error summary.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-providermaterializationobservedatutc"></a>

##### `ProviderMaterializationObservedAtUtc`

```csharp
DateTimeOffset? ProviderMaterializationObservedAtUtc { get; }
```

Gets the optional UTC timestamp when the provider-materialization state was last observed.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-providermaterializationstate"></a>

##### `ProviderMaterializationState`

```csharp
string ProviderMaterializationState { get; }
```

Gets the optional provider-materialization state for this automation.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-providermaterializerid"></a>

##### `ProviderMaterializerId`

```csharp
string ProviderMaterializerId { get; }
```

Gets the optional selected provider materializer identifier.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-requiredcapabilitykey"></a>

##### `RequiredCapabilityKey`

```csharp
string RequiredCapabilityKey { get; }
```

Gets the optional capability key inherited from the governed route.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-routeid"></a>

##### `RouteId`

```csharp
string RouteId { get; }
```

Gets the governed cell-route identifier that this automation answer applies to.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-routingstrategy"></a>

##### `RoutingStrategy`

```csharp
string RoutingStrategy { get; }
```

Gets the routing strategy inherited from the governed route.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-runtimemetadata"></a>

##### `RuntimeMetadata`

```csharp
IReadOnlyDictionary<string, string> RuntimeMetadata { get; }
```

Gets runtime-only metadata such as policy notes or overlay provenance.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-sourcecellid"></a>

##### `SourceCellId`

```csharp
string SourceCellId { get; }
```

Gets the source cell identifier.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-sourcehealthisolationids"></a>

##### `SourceHealthIsolationIds`

```csharp
IReadOnlyList<string> SourceHealthIsolationIds { get; }
```

Gets the normalized source-cell health-isolation identifiers.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the module that owns the governed route.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-targetcellid"></a>

##### `TargetCellId`

```csharp
string TargetCellId { get; }
```

Gets the target cell identifier.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-targethealthisolationids"></a>

##### `TargetHealthIsolationIds`

```csharp
IReadOnlyList<string> TargetHealthIsolationIds { get; }
```

Gets the normalized target-cell health-isolation identifiers.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-transportids"></a>

##### `TransportIds`

```csharp
IReadOnlyList<string> TransportIds { get; }
```

Gets the normalized transport identifiers inherited from the governed route.

<a id="member-p-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-triggermode"></a>

##### `TriggerMode`

```csharp
string TriggerMode { get; }
```

Gets the normalized trigger posture.

<a id="type-cephalon-abstractions-technologies-icellboundarycatalog"></a>

### `ICellBoundaryCatalog`

Exposes the cell boundaries visible to the current runtime.

#### Declaration
```csharp
public interface ICellBoundaryCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-icellboundarycatalog-cellboundaries"></a>

##### `CellBoundaries`

```csharp
IReadOnlyList<CellBoundaryDescriptor> CellBoundaries { get; }
```

Gets all cell boundaries visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellboundarycatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
CellBoundaryDescriptor GetById(string cellId)
```

Gets one cell boundary by its stable identifier.

Returns: The matching cell boundary, or `null` when it is not active.

Parameters:
- `cellId`: The cell identifier to resolve.

<a id="member-m-cephalon-abstractions-technologies-icellboundarycatalog-getbymodule-system-string"></a>

##### `GetByModule`

```csharp
IReadOnlyList<CellBoundaryDescriptor> GetByModule(string moduleId)
```

Gets all cell boundaries that include the requested module.

Returns: The matching cell boundaries, or an empty list when none are active.

Parameters:
- `moduleId`: The module identifier to filter by.

<a id="type-cephalon-abstractions-technologies-icellboundarycontributor"></a>

### `ICellBoundaryContributor`

Allows a module to contribute cell boundaries to the active runtime.

#### Declaration
```csharp
public interface ICellBoundaryContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellboundarycontributor-registercellboundaries-cephalon-abstractions-technologies-icellboundaryregistry"></a>

##### `RegisterCellBoundaries`

```csharp
void RegisterCellBoundaries(ICellBoundaryRegistry cells)
```

Registers the cell boundaries owned by the contributing module.

Parameters:
- `cells`: The registry that receives cell-boundary descriptors.

<a id="type-cephalon-abstractions-technologies-icellboundaryregistry"></a>

### `ICellBoundaryRegistry`

Collects cell-boundary descriptors during runtime composition.

#### Declaration
```csharp
public interface ICellBoundaryRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellboundaryregistry-add-cephalon-abstractions-technologies-cellboundarydescriptor"></a>

##### `Add`

```csharp
void Add(CellBoundaryDescriptor cellBoundary)
```

Adds one cell-boundary descriptor to the active runtime composition.

Parameters:
- `cellBoundary`: The cell-boundary descriptor to add.

<a id="type-cephalon-abstractions-technologies-icellhealthisolationcatalog"></a>

### `ICellHealthIsolationCatalog`

Exposes the cell health-isolation answers visible to the current runtime.

#### Declaration
```csharp
public interface ICellHealthIsolationCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-icellhealthisolationcatalog-healthisolations"></a>

##### `HealthIsolations`

```csharp
IReadOnlyList<CellHealthIsolationDescriptor> HealthIsolations { get; }
```

Gets all cell health-isolation answers visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellhealthisolationcatalog-getbycellid-system-string"></a>

##### `GetByCellId`

```csharp
IReadOnlyList<CellHealthIsolationDescriptor> GetByCellId(string cellId)
```

Gets all cell health-isolation answers that govern the requested cell.

Returns: The matching cell health-isolation answers, or an empty list when none are active.

Parameters:
- `cellId`: The cell identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icellhealthisolationcatalog-getbydependencyid-system-string"></a>

##### `GetByDependencyId`

```csharp
IReadOnlyList<CellHealthIsolationDescriptor> GetByDependencyId(string dependencyId)
```

Gets all cell health-isolation answers that reference the requested dependency.

Returns: The matching cell health-isolation answers, or an empty list when none are active.

Parameters:
- `dependencyId`: The dependency identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icellhealthisolationcatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
CellHealthIsolationDescriptor GetById(string healthIsolationId)
```

Gets one cell health-isolation answer by its stable identifier.

Returns: The matching cell health-isolation answer, or `null` when it is not active.

Parameters:
- `healthIsolationId`: The health-isolation identifier to resolve.

<a id="member-m-cephalon-abstractions-technologies-icellhealthisolationcatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<CellHealthIsolationDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all cell health-isolation answers owned by the requested source module.

Returns: The matching cell health-isolation answers, or an empty list when none are active.

Parameters:
- `sourceModuleId`: The source-module identifier to filter by.

<a id="type-cephalon-abstractions-technologies-icellhealthisolationcontributor"></a>

### `ICellHealthIsolationContributor`

Allows a module to contribute cell health-isolation answers to the active runtime.

#### Declaration
```csharp
public interface ICellHealthIsolationContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellhealthisolationcontributor-registercellhealthisolations-cephalon-abstractions-technologies-icellhealthisolationregistry"></a>

##### `RegisterCellHealthIsolations`

```csharp
void RegisterCellHealthIsolations(ICellHealthIsolationRegistry healthIsolations)
```

Registers the cell health-isolation answers owned by the contributing module.

Parameters:
- `healthIsolations`: The registry that receives cell health-isolation descriptors.

<a id="type-cephalon-abstractions-technologies-icellhealthisolationregistry"></a>

### `ICellHealthIsolationRegistry`

Collects cell health-isolation descriptors during runtime composition.

#### Declaration
```csharp
public interface ICellHealthIsolationRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellhealthisolationregistry-add-cephalon-abstractions-technologies-cellhealthisolationdescriptor"></a>

##### `Add`

```csharp
void Add(CellHealthIsolationDescriptor healthIsolation)
```

Adds one cell health-isolation descriptor to the active runtime composition.

Parameters:
- `healthIsolation`: The cell health-isolation descriptor to add.

<a id="type-cephalon-abstractions-technologies-icellroutecatalog"></a>

### `ICellRouteCatalog`

Exposes the cell-to-cell routing and governance answers visible to the current runtime.

#### Declaration
```csharp
public interface ICellRouteCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-icellroutecatalog-routes"></a>

##### `Routes`

```csharp
IReadOnlyList<CellRouteDescriptor> Routes { get; }
```

Gets all cell routes visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellroutecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
CellRouteDescriptor GetById(string routeId)
```

Gets one cell route by its stable identifier.

Returns: The matching cell route, or `null` when it is not active.

Parameters:
- `routeId`: The cell-route identifier to resolve.

<a id="member-m-cephalon-abstractions-technologies-icellroutecatalog-getbysourcecellid-system-string"></a>

##### `GetBySourceCellId`

```csharp
IReadOnlyList<CellRouteDescriptor> GetBySourceCellId(string sourceCellId)
```

Gets all cell routes that originate from the requested source cell.

Returns: The matching cell routes, or an empty list when none are active.

Parameters:
- `sourceCellId`: The source-cell identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icellroutecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<CellRouteDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all cell routes owned by the requested source module.

Returns: The matching cell routes, or an empty list when none are active.

Parameters:
- `sourceModuleId`: The source-module identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icellroutecatalog-getbytargetcellid-system-string"></a>

##### `GetByTargetCellId`

```csharp
IReadOnlyList<CellRouteDescriptor> GetByTargetCellId(string targetCellId)
```

Gets all cell routes that target the requested cell.

Returns: The matching cell routes, or an empty list when none are active.

Parameters:
- `targetCellId`: The target-cell identifier to filter by.

<a id="type-cephalon-abstractions-technologies-icellroutecontributor"></a>

### `ICellRouteContributor`

Allows a module to contribute cell routes to the active runtime.

#### Declaration
```csharp
public interface ICellRouteContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellroutecontributor-registercellroutes-cephalon-abstractions-technologies-icellrouteregistry"></a>

##### `RegisterCellRoutes`

```csharp
void RegisterCellRoutes(ICellRouteRegistry routes)
```

Registers the cell routes owned by the contributing module.

Parameters:
- `routes`: The registry that receives cell-route descriptors.

<a id="type-cephalon-abstractions-technologies-icellrouteregistry"></a>

### `ICellRouteRegistry`

Collects cell-route descriptors during runtime composition.

#### Declaration
```csharp
public interface ICellRouteRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icellrouteregistry-add-cephalon-abstractions-technologies-cellroutedescriptor"></a>

##### `Add`

```csharp
void Add(CellRouteDescriptor cellRoute)
```

Adds one cell-route descriptor to the active runtime composition.

Parameters:
- `cellRoute`: The cell-route descriptor to add.

<a id="type-cephalon-abstractions-technologies-icelltrafficautomationprovidermaterializer"></a>

### `ICellTrafficAutomationProviderMaterializer`

Applies provider-managed cell traffic automation posture to one external control plane.

#### Declaration
```csharp
public interface ICellTrafficAutomationProviderMaterializer
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-icelltrafficautomationprovidermaterializer-materializerid"></a>

##### `MaterializerId`

```csharp
string MaterializerId { get; }
```

Gets the stable materializer identifier that should appear on operator-facing runtime answers.

<a id="member-p-cephalon-abstractions-technologies-icelltrafficautomationprovidermaterializer-providerid"></a>

##### `ProviderId`

```csharp
string ProviderId { get; }
```

Gets the external provider identifier that this materializer reconciles.

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationprovidermaterializer-materializeasync-cephalon-abstractions-technologies-celltrafficautomationruntimedescriptor-system-threading-cancellationtoken"></a>

##### `MaterializeAsync`

```csharp
ValueTask<CellTrafficAutomationProviderMaterializationResult> MaterializeAsync(CellTrafficAutomationRuntimeDescriptor automation, CancellationToken cancellationToken)
```

Applies or reconciles the requested traffic automation answer against the owned provider.

Returns: The observed provider-materialization result.

Parameters:
- `automation`: The effective traffic automation answer to materialize.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog"></a>

### `ICellTrafficAutomationRuntimeCatalog`

Exposes the effective cell traffic-automation answers visible to the current runtime.

#### Declaration
```csharp
public interface ICellTrafficAutomationRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-automations"></a>

##### `Automations`

```csharp
IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> Automations { get; }
```

Gets all effective cell traffic-automation answers visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-getbyedgenodeid-system-string"></a>

##### `GetByEdgeNodeId`

```csharp
IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByEdgeNodeId(string edgeNodeId)
```

Gets all effective cell traffic-automation answers that target the requested edge node.

Returns: The matching runtime descriptors, or an empty list when none are active.

Parameters:
- `edgeNodeId`: The edge-node identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-getbyhealthisolationid-system-string"></a>

##### `GetByHealthIsolationId`

```csharp
IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByHealthIsolationId(string healthIsolationId)
```

Gets all effective cell traffic-automation answers that reference the requested health-isolation identifier.

Returns: The matching runtime descriptors, or an empty list when none are active.

Parameters:
- `healthIsolationId`: The health-isolation identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
CellTrafficAutomationRuntimeDescriptor GetById(string automationId)
```

Gets one effective cell traffic-automation answer by its stable identifier.

Returns: The matching runtime descriptor, or `null` when it is not active.

Parameters:
- `automationId`: The traffic-automation identifier to resolve.

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByProvider(string provider)
```

Gets all effective cell traffic-automation answers that target the requested external provider.

Returns: The matching runtime descriptors, or an empty list when none are active.

Parameters:
- `provider`: The provider identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-getbyrouteid-system-string"></a>

##### `GetByRouteId`

```csharp
CellTrafficAutomationRuntimeDescriptor GetByRouteId(string routeId)
```

Gets one effective cell traffic-automation answer by its governed route identifier.

Returns: The matching runtime descriptor, or `null` when it is not active.

Parameters:
- `routeId`: The governed route identifier to resolve.

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-getbysourcecellid-system-string"></a>

##### `GetBySourceCellId`

```csharp
IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceCellId(string sourceCellId)
```

Gets all effective cell traffic-automation answers that originate from the requested source cell.

Returns: The matching runtime descriptors, or an empty list when none are active.

Parameters:
- `sourceCellId`: The source-cell identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all effective cell traffic-automation answers owned by the requested module.

Returns: The matching runtime descriptors, or an empty list when none are active.

Parameters:
- `sourceModuleId`: The module identifier to filter by.

<a id="member-m-cephalon-abstractions-technologies-icelltrafficautomationruntimecatalog-getbytargetcellid-system-string"></a>

##### `GetByTargetCellId`

```csharp
IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByTargetCellId(string targetCellId)
```

Gets all effective cell traffic-automation answers that target the requested cell.

Returns: The matching runtime descriptors, or an empty list when none are active.

Parameters:
- `targetCellId`: The target-cell identifier to filter by.

<a id="type-cephalon-abstractions-technologies-itechnologycapabilitycontributor"></a>

### `ITechnologyCapabilityContributor`

Contributes capabilities when specific technology profiles are active.

#### Declaration
```csharp
public interface ITechnologyCapabilityContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologycapabilitycontributor-registertechnologycapabilities-cephalon-abstractions-capabilities-icapabilityregistry-cephalon-abstractions-technologies-technologyselection"></a>

##### `RegisterTechnologyCapabilities`

```csharp
void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
```

Registers capabilities for the active technology selection.

Parameters:
- `capabilities`: The capability registry receiving technology capabilities.
- `technologies`: The active technology selection.

<a id="type-cephalon-abstractions-technologies-itechnologycontributor"></a>

### `ITechnologyContributor`

Contributes technology descriptors to the runtime catalog.

#### Declaration
```csharp
public interface ITechnologyContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologycontributor-registertechnologies-cephalon-abstractions-technologies-itechnologyregistry"></a>

##### `RegisterTechnologies`

```csharp
void RegisterTechnologies(ITechnologyRegistry technologies)
```

Registers one or more technology descriptors.

Parameters:
- `technologies`: The technology registry receiving contributed technologies.

<a id="type-cephalon-abstractions-technologies-itechnologyregistry"></a>

### `ITechnologyRegistry`

Registers technology descriptors for the runtime catalog.

#### Declaration
```csharp
public interface ITechnologyRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologyregistry-add-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `Add`

```csharp
void Add(TechnologyDescriptor technology)
```

Adds a technology descriptor to the registry.

Parameters:
- `technology`: The technology descriptor to register.

<a id="type-cephalon-abstractions-technologies-itechnologyruntimecatalog"></a>

### `ITechnologyRuntimeCatalog`

Exposes the merged runtime surfaces projected by active technology packs.

#### Declaration
```csharp
public interface ITechnologyRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-itechnologyruntimecatalog-surfaces"></a>

##### `Surfaces`

```csharp
IReadOnlyList<TechnologyRuntimeSurface> Surfaces { get; }
```

Gets all active technology runtime surfaces visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologyruntimecatalog-getbytechnology-system-string"></a>

##### `GetByTechnology`

```csharp
IReadOnlyList<TechnologyRuntimeSurface> GetByTechnology(string technologyId)
```

Gets the runtime surfaces associated with a specific technology identifier.

Returns: The matching runtime surfaces, or an empty collection when none are active.

Parameters:
- `technologyId`: The technology identifier to filter by.

<a id="type-cephalon-abstractions-technologies-itechnologyruntimecontributor"></a>

### `ITechnologyRuntimeContributor`

Contributes one runtime surface projected by an active technology pack.

#### Declaration
```csharp
public interface ITechnologyRuntimeContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologyruntimecontributor-describeruntimesurface"></a>

##### `DescribeRuntimeSurface`

```csharp
TechnologyRuntimeSurface DescribeRuntimeSurface()
```

Describes the runtime surface projected by the contributor.

Returns: The runtime surface description.

<a id="type-cephalon-abstractions-technologies-itechnologyservicecontributor"></a>

### `ITechnologyServiceContributor`

Configures services required by active technology profiles.

#### Declaration
```csharp
public interface ITechnologyServiceContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologyservicecontributor-configuretechnologyservices-microsoft-extensions-dependencyinjection-iservicecollection-cephalon-abstractions-technologies-technologyselection"></a>

##### `ConfigureTechnologyServices`

```csharp
void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
```

Configures services for the active technology selection.

Parameters:
- `services`: The service collection receiving technology services.
- `technologies`: The active technology selection.

<a id="type-cephalon-abstractions-technologies-technologydescriptor"></a>

### `TechnologyDescriptor`

Describes one technology profile that can be activated for an app.

#### Declaration
```csharp
public sealed class TechnologyDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-technologydescriptor-ctor-system-string-system-string-system-string-cephalon-abstractions-technologies-technologykind-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TechnologyDescriptor`

```csharp
TechnologyDescriptor(string id, string displayName, string description, TechnologyKind kind, IReadOnlyList<string> aliases, IReadOnlyList<string> tags, IReadOnlyList<string> requiresPatterns, IReadOnlyList<string> requiresTransports, IReadOnlyList<string> requiresTechnologies, IReadOnlyList<string> conflictsWith, IReadOnlyList<string> packageHints, IReadOnlyList<string> guidance, IReadOnlyDictionary<string, string> metadata)
```

Creates a technology descriptor.

Parameters:
- `id`: The stable technology identifier.
- `displayName`: The human-readable technology name.
- `description`: The technology description.
- `kind`: The category of the technology.
- `aliases`: Optional aliases that can resolve to the same technology.
- `tags`: The tags associated with the technology.
- `requiresPatterns`: The pattern identifiers required by the technology.
- `requiresTransports`: The transport identifiers required by the technology.
- `requiresTechnologies`: The technology identifiers required by the technology.
- `conflictsWith`: The technology identifiers that conflict with the technology.
- `packageHints`: The companion-package hints associated with the technology.
- `guidance`: The guidance entries associated with the technology.
- `metadata`: Optional technology metadata.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-aliases"></a>

##### `Aliases`

```csharp
IReadOnlyList<string> Aliases { get; }
```

Gets optional aliases that can resolve to the same technology.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-conflictswith"></a>

##### `ConflictsWith`

```csharp
IReadOnlyList<string> ConflictsWith { get; }
```

Gets the technology identifiers that conflict with the technology.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the technology description.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable technology name.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-guidance"></a>

##### `Guidance`

```csharp
IReadOnlyList<string> Guidance { get; }
```

Gets the guidance entries associated with the technology.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable technology identifier.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-kind"></a>

##### `Kind`

```csharp
TechnologyKind Kind { get; }
```

Gets the category of the technology.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional technology metadata.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-packagehints"></a>

##### `PackageHints`

```csharp
IReadOnlyList<string> PackageHints { get; }
```

Gets the companion-package hints associated with the technology.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-requirespatterns"></a>

##### `RequiresPatterns`

```csharp
IReadOnlyList<string> RequiresPatterns { get; }
```

Gets the pattern identifiers required by the technology.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-requirestechnologies"></a>

##### `RequiresTechnologies`

```csharp
IReadOnlyList<string> RequiresTechnologies { get; }
```

Gets the technology identifiers required by the technology.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-requirestransports"></a>

##### `RequiresTransports`

```csharp
IReadOnlyList<string> RequiresTransports { get; }
```

Gets the transport identifiers required by the technology.

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the tags associated with the technology.

<a id="type-cephalon-abstractions-technologies-technologykind"></a>

### `TechnologyKind`

Categorizes the role a technology profile plays in an app.

#### Declaration
```csharp
public enum TechnologyKind
```

#### Fields

<a id="member-f-cephalon-abstractions-technologies-technologykind-data"></a>

##### `Data`

```csharp
const TechnologyKind Data
```

Identifies a data-oriented technology.

<a id="member-f-cephalon-abstractions-technologies-technologykind-deployment"></a>

##### `Deployment`

```csharp
const TechnologyKind Deployment
```

Identifies a deployment-oriented technology.

<a id="member-f-cephalon-abstractions-technologies-technologykind-experience"></a>

##### `Experience`

```csharp
const TechnologyKind Experience
```

Identifies an experience-oriented technology.

<a id="member-f-cephalon-abstractions-technologies-technologykind-intelligence"></a>

##### `Intelligence`

```csharp
const TechnologyKind Intelligence
```

Identifies an intelligence-oriented technology.

<a id="member-f-cephalon-abstractions-technologies-technologykind-messaging"></a>

##### `Messaging`

```csharp
const TechnologyKind Messaging
```

Identifies a messaging-oriented technology.

<a id="member-f-cephalon-abstractions-technologies-technologykind-platform"></a>

##### `Platform`

```csharp
const TechnologyKind Platform
```

Identifies a platform- or runtime-oriented technology.

<a id="member-f-cephalon-abstractions-technologies-technologykind-security"></a>

##### `Security`

```csharp
const TechnologyKind Security
```

Identifies a security-oriented technology.

<a id="type-cephalon-abstractions-technologies-technologyruntimeentry"></a>

### `TechnologyRuntimeEntry`

Describes one runtime-visible entry inside a technology surface.

#### Declaration
```csharp
public sealed class TechnologyRuntimeEntry
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-technologyruntimeentry-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TechnologyRuntimeEntry`

```csharp
TechnologyRuntimeEntry(string id, string displayName, string description, IReadOnlyDictionary<string, string> metadata)
```

Creates a new technology runtime entry.

Parameters:
- `id`: The stable entry identifier.
- `displayName`: The operator-facing display name.
- `description`: A human-readable description of the entry.
- `metadata`: Additional metadata associated with the entry.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-technologyruntimeentry-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the entry.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimeentry-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the entry.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimeentry-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable identifier for the entry.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimeentry-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional metadata projected for the entry.

<a id="type-cephalon-abstractions-technologies-technologyruntimesurface"></a>

### `TechnologyRuntimeSurface`

Describes one operator-facing runtime surface exposed by an active technology pack.

#### Declaration
```csharp
public sealed class TechnologyRuntimeSurface
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-technologyruntimesurface-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologyruntimeentry"></a>

##### `TechnologyRuntimeSurface`

```csharp
TechnologyRuntimeSurface(string technologyId, string surfaceId, string displayName, string description, IReadOnlyList<TechnologyRuntimeEntry> entries)
```

Creates a new technology runtime surface.

Parameters:
- `technologyId`: The owning technology identifier.
- `surfaceId`: The stable surface identifier within that technology.
- `displayName`: The operator-facing display name for the surface.
- `description`: A human-readable description of the surface.
- `entries`: The entries currently projected by the surface.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the surface.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the surface.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-entries"></a>

##### `Entries`

```csharp
IReadOnlyList<TechnologyRuntimeEntry> Entries { get; }
```

Gets the entries currently projected by this surface.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-surfaceid"></a>

##### `SurfaceId`

```csharp
string SurfaceId { get; }
```

Gets the stable identifier of this surface within the owning technology.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-technologyid"></a>

##### `TechnologyId`

```csharp
string TechnologyId { get; }
```

Gets the identifier of the technology profile that owns this surface.

<a id="type-cephalon-abstractions-technologies-technologyselection"></a>

### `TechnologySelection`

Provides lookup helpers over selected and available technology profiles.

#### Declaration
```csharp
public sealed class TechnologySelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-technologyselection-ctor-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `TechnologySelection`

```csharp
TechnologySelection(IReadOnlyList<TechnologyDescriptor> selected, IReadOnlyList<TechnologyDescriptor> catalog)
```

Creates a technology-selection view.

Parameters:
- `selected`: The technology profiles currently selected for the app.
- `catalog`: The technology profiles available to the runtime.

#### Properties

<a id="member-p-cephalon-abstractions-technologies-technologyselection-catalog"></a>

##### `Catalog`

```csharp
IReadOnlyList<TechnologyDescriptor> Catalog { get; }
```

Gets the technology profiles available to the runtime.

<a id="member-p-cephalon-abstractions-technologies-technologyselection-selected"></a>

##### `Selected`

```csharp
IReadOnlyList<TechnologyDescriptor> Selected { get; }
```

Gets the technology profiles currently selected for the app.

#### Methods

<a id="member-m-cephalon-abstractions-technologies-technologyselection-isavailable-system-string"></a>

##### `IsAvailable`

```csharp
bool IsAvailable(string value)
```

Determines whether a technology is available in the runtime catalog.

Returns: `true` when the technology is available; otherwise `false`.

Parameters:
- `value`: The technology identifier or display name to match.

<a id="member-m-cephalon-abstractions-technologies-technologyselection-isselected-system-string"></a>

##### `IsSelected`

```csharp
bool IsSelected(string value)
```

Determines whether a technology is selected.

Returns: `true` when the technology is selected; otherwise `false`.

Parameters:
- `value`: The technology identifier or display name to match.

<a id="member-m-cephalon-abstractions-technologies-technologyselection-trygetavailable-system-string-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `TryGetAvailable`

```csharp
bool TryGetAvailable(string value, out TechnologyDescriptor technology)
```

Attempts to resolve one available technology from the runtime catalog.

Returns: `true` when the technology is available; otherwise `false`.

Parameters:
- `value`: The technology identifier or display name to match.
- `technology`: The resolved available technology when one is found.

<a id="member-m-cephalon-abstractions-technologies-technologyselection-trygetselected-system-string-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `TryGetSelected`

```csharp
bool TryGetSelected(string value, out TechnologyDescriptor technology)
```

Attempts to resolve one selected technology.

Returns: `true` when the technology is selected; otherwise `false`.

Parameters:
- `value`: The technology identifier or display name to match.
- `technology`: The resolved selected technology when one is found.

<a id="namespace-cephalon-abstractions-tenancy"></a>

## Namespace Cephalon.Abstractions.Tenancy

<a id="type-cephalon-abstractions-tenancy-itenantcontextaccessor"></a>

### `ITenantContextAccessor`

Exposes the tenant context currently active for the ambient runtime scope.

#### Declaration
```csharp
public interface ITenantContextAccessor
```

#### Properties

<a id="member-p-cephalon-abstractions-tenancy-itenantcontextaccessor-current"></a>

##### `Current`

```csharp
TenantContext Current { get; }
```

Gets the tenant context currently active for the ambient runtime scope.

<a id="type-cephalon-abstractions-tenancy-itenantresolver"></a>

### `ITenantResolver`

Resolves the tenant context for the current operation from host-neutral hints.

#### Declaration
```csharp
public interface ITenantResolver
```

#### Methods

<a id="member-m-cephalon-abstractions-tenancy-itenantresolver-resolveasync-cephalon-abstractions-tenancy-tenantresolutionrequest-system-threading-cancellationtoken"></a>

##### `ResolveAsync`

```csharp
ValueTask<TenantResolutionResult> ResolveAsync(TenantResolutionRequest request, CancellationToken cancellationToken)
```

Resolves the tenant context for the supplied request.

Returns: A task that completes with the resulting tenant-resolution outcome.

Parameters:
- `request`: The host-neutral resolution request.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-abstractions-tenancy-tenantcontext"></a>

### `TenantContext`

Describes the tenant currently associated with an operation or ambient runtime scope.

#### Declaration
```csharp
public sealed class TenantContext
```

#### Constructors

<a id="member-m-cephalon-abstractions-tenancy-tenantcontext-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantContext`

```csharp
TenantContext(string tenantId, string tenantKey, string displayName, string parentTenantId, IReadOnlyList<string> domains, IReadOnlyDictionary<string, string> attributes)
```

Creates a new tenant context.

Parameters:
- `tenantId`: The stable tenant identifier.
- `tenantKey`: The tenant key, slug, or subdomain-friendly identifier when one is known.
- `displayName`: The human-readable tenant name when one is known.
- `parentTenantId`: The parent tenant identifier when one is known.
- `domains`: Optional domains associated with the tenant.
- `attributes`: Optional tenant attributes.

#### Properties

<a id="member-p-cephalon-abstractions-tenancy-tenantcontext-attributes"></a>

##### `Attributes`

```csharp
IReadOnlyDictionary<string, string> Attributes { get; }
```

Gets the tenant attributes.

<a id="member-p-cephalon-abstractions-tenancy-tenantcontext-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable tenant name when one is known.

<a id="member-p-cephalon-abstractions-tenancy-tenantcontext-domains"></a>

##### `Domains`

```csharp
IReadOnlyList<string> Domains { get; }
```

Gets the domains associated with the tenant.

<a id="member-p-cephalon-abstractions-tenancy-tenantcontext-parenttenantid"></a>

##### `ParentTenantId`

```csharp
string ParentTenantId { get; }
```

Gets the parent tenant identifier when one is known.

<a id="member-p-cephalon-abstractions-tenancy-tenantcontext-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the stable tenant identifier.

<a id="member-p-cephalon-abstractions-tenancy-tenantcontext-tenantkey"></a>

##### `TenantKey`

```csharp
string TenantKey { get; }
```

Gets the tenant key, slug, or subdomain-friendly identifier when one is known.

<a id="type-cephalon-abstractions-tenancy-tenantresolutionrequest"></a>

### `TenantResolutionRequest`

Describes the host-neutral hints available when resolving a tenant for the current operation.

#### Declaration
```csharp
public sealed class TenantResolutionRequest
```

#### Constructors

<a id="member-m-cephalon-abstractions-tenancy-tenantresolutionrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TenantResolutionRequest`

```csharp
TenantResolutionRequest(string hostName, string pathBase, string requestedTenantId, string requestedTenantKey, string userId, IReadOnlyDictionary<string, string> attributes)
```

Creates a new tenant-resolution request.

Parameters:
- `hostName`: The host name associated with the current request when one is known.
- `pathBase`: The path base associated with the current request when one is known.
- `requestedTenantId`: The explicitly requested tenant identifier when one is known.
- `requestedTenantKey`: The explicitly requested tenant key when one is known.
- `userId`: The current user identifier when one is known.
- `attributes`: Optional resolution hints supplied by the host or caller.

#### Properties

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionrequest-attributes"></a>

##### `Attributes`

```csharp
IReadOnlyDictionary<string, string> Attributes { get; }
```

Gets optional resolution hints supplied by the host or caller.

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionrequest-hostname"></a>

##### `HostName`

```csharp
string HostName { get; }
```

Gets the host name associated with the current request when one is known.

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionrequest-pathbase"></a>

##### `PathBase`

```csharp
string PathBase { get; }
```

Gets the path base associated with the current request when one is known.

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionrequest-requestedtenantid"></a>

##### `RequestedTenantId`

```csharp
string RequestedTenantId { get; }
```

Gets the explicitly requested tenant identifier when one is known.

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionrequest-requestedtenantkey"></a>

##### `RequestedTenantKey`

```csharp
string RequestedTenantKey { get; }
```

Gets the explicitly requested tenant key when one is known.

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionrequest-userid"></a>

##### `UserId`

```csharp
string UserId { get; }
```

Gets the current user identifier when one is known.

<a id="type-cephalon-abstractions-tenancy-tenantresolutionresult"></a>

### `TenantResolutionResult`

Describes the outcome of one tenant-resolution attempt.

#### Declaration
```csharp
public sealed class TenantResolutionResult
```

#### Constructors

<a id="member-m-cephalon-abstractions-tenancy-tenantresolutionresult-ctor-cephalon-abstractions-tenancy-tenantcontext-system-string-system-string"></a>

##### `TenantResolutionResult`

```csharp
TenantResolutionResult(TenantContext tenant, string source, string reason)
```

Creates a new tenant-resolution result.

Parameters:
- `tenant`: The resolved tenant context when resolution succeeded.
- `source`: The source or strategy that produced the result when one is known.
- `reason`: The human-readable reason associated with the result.

#### Properties

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionresult-isresolved"></a>

##### `IsResolved`

```csharp
bool IsResolved { get; }
```

Gets a value indicating whether tenant resolution succeeded.

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionresult-reason"></a>

##### `Reason`

```csharp
string Reason { get; }
```

Gets the human-readable reason associated with the result.

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionresult-source"></a>

##### `Source`

```csharp
string Source { get; }
```

Gets the source or strategy that produced the result when one is known.

<a id="member-p-cephalon-abstractions-tenancy-tenantresolutionresult-tenant"></a>

##### `Tenant`

```csharp
TenantContext Tenant { get; }
```

Gets the resolved tenant context when resolution succeeded.

<a id="namespace-cephalon-abstractions-transports"></a>

## Namespace Cephalon.Abstractions.Transports

<a id="type-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor"></a>

### `BackendForFrontendRestDocumentRuntimeDescriptor`

Describes one backend-for-frontend REST documentation surface materialized for one scope and one published OpenAPI document.

Remarks: These runtime descriptors keep client-aware REST documentation surfaces introspectable without turning OpenAPI JSON or Scalar page routes into a second source of truth outside the shared backend-for-frontend and REST runtime catalogs.

#### Declaration
```csharp
public sealed class BackendForFrontendRestDocumentRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `BackendForFrontendRestDocumentRuntimeDescriptor`

```csharp
BackendForFrontendRestDocumentRuntimeDescriptor(string id, string kind, string scopeId, string clientId, string documentName, string openApiPath, string scalarPath, IReadOnlyList<string> bindingIds, IReadOnlyList<string> sourceModuleIds, IReadOnlyList<string> runtimeEndpointIds, IReadOnlyList<string> restEndpointIds)
```

Creates a backend-for-frontend REST documentation runtime descriptor.

Parameters:
- `id`: The stable documentation-surface identifier.
- `kind`: The stable scope kind. Supported values are `BindingKind` and `ClientKind`.
- `scopeId`: The stable scope identifier. For binding-scoped surfaces this is the binding identifier, while client-scoped surfaces use the client identifier.
- `clientId`: The stable client identifier served by the materialized document.
- `documentName`: The resolved OpenAPI document name.
- `openApiPath`: The rooted OpenAPI JSON path for the filtered document.
- `scalarPath`: The rooted Scalar page path for the filtered document.
- `bindingIds`: The backend-for-frontend binding identifiers that contribute to the materialized document. Binding-scoped surfaces contain one value, while client-scoped surfaces can aggregate more than one binding.
- `sourceModuleIds`: The published-endpoint source-module identifiers represented in the materialized document.
- `runtimeEndpointIds`: The client-aware runtime endpoint identifiers included in the materialized document.
- `restEndpointIds`: The published REST endpoint identifiers included in the materialized document.

#### Fields

<a id="member-f-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-bindingkind"></a>

##### `BindingKind`

```csharp
const string BindingKind
```

The stable kind identifier used when the documentation surface is scoped to one binding.

<a id="member-f-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-clientkind"></a>

##### `ClientKind`

```csharp
const string ClientKind
```

The stable kind identifier used when the documentation surface is scoped to one client.

#### Properties

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-bindingid"></a>

##### `BindingId`

```csharp
string BindingId { get; }
```

Gets the binding identifier when the surface is scoped to one binding.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-bindingids"></a>

##### `BindingIds`

```csharp
IReadOnlyList<string> BindingIds { get; }
```

Gets the contributing backend-for-frontend binding identifiers.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-clientid"></a>

##### `ClientId`

```csharp
string ClientId { get; }
```

Gets the stable client identifier represented by the materialized document.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-documentname"></a>

##### `DocumentName`

```csharp
string DocumentName { get; }
```

Gets the resolved OpenAPI document name.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable documentation-surface identifier.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-kind"></a>

##### `Kind`

```csharp
string Kind { get; }
```

Gets the stable scope kind.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-openapipath"></a>

##### `OpenApiPath`

```csharp
string OpenApiPath { get; }
```

Gets the rooted OpenAPI JSON path for the filtered document.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-restendpointids"></a>

##### `RestEndpointIds`

```csharp
IReadOnlyList<string> RestEndpointIds { get; }
```

Gets the included published REST endpoint identifiers.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-runtimeendpointids"></a>

##### `RuntimeEndpointIds`

```csharp
IReadOnlyList<string> RuntimeEndpointIds { get; }
```

Gets the included client-aware runtime endpoint identifiers.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-scalarpath"></a>

##### `ScalarPath`

```csharp
string ScalarPath { get; }
```

Gets the rooted Scalar page path for the filtered document.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-scopeid"></a>

##### `ScopeId`

```csharp
string ScopeId { get; }
```

Gets the stable scope identifier.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestdocumentruntimedescriptor-sourcemoduleids"></a>

##### `SourceModuleIds`

```csharp
IReadOnlyList<string> SourceModuleIds { get; }
```

Gets the published-endpoint source-module identifiers represented in the materialized document.

<a id="type-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor"></a>

### `BackendForFrontendRestEndpointRuntimeDescriptor`

Describes one backend-for-frontend client binding matched to one published REST endpoint.

Remarks: This runtime surface keeps the host-agnostic backend-for-frontend binding contract separate from the host-owned REST runtime catalog while still letting operator tooling answer which published REST endpoints are currently visible to a specific client binding.

#### Declaration
```csharp
public sealed class BackendForFrontendRestEndpointRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-ctor-system-string-cephalon-abstractions-patterns-backendforfrontendclientbindingdescriptor-cephalon-abstractions-transports-restendpointruntimedescriptor-system-boolean-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `BackendForFrontendRestEndpointRuntimeDescriptor`

```csharp
BackendForFrontendRestEndpointRuntimeDescriptor(string id, BackendForFrontendClientBindingDescriptor binding, RestEndpointRuntimeDescriptor endpoint, bool matchedByDefault, IReadOnlyList<string> matchedBehaviorIds, IReadOnlyList<string> matchedCapabilityKeys, IReadOnlyList<string> matchedTags)
```

Creates a backend-for-frontend REST endpoint runtime descriptor.

Parameters:
- `id`: The stable binding-plus-endpoint identifier.
- `binding`: The backend-for-frontend client binding that selected the endpoint.
- `endpoint`: The published REST endpoint selected for that binding.
- `matchedByDefault`: `true` when the endpoint stayed visible because the binding declared no positive include filters and the endpoint was not excluded.
- `matchedBehaviorIds`: The included behavior identifiers that matched the published endpoint when explicit behavior-id filters were part of the binding.
- `matchedCapabilityKeys`: The included required-capability keys that matched the published endpoint when explicit capability filters were part of the binding.
- `matchedTags`: The included tags that matched the published endpoint when explicit tag filters were part of the binding.

#### Properties

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-binding"></a>

##### `Binding`

```csharp
BackendForFrontendClientBindingDescriptor Binding { get; }
```

Gets the backend-for-frontend client binding that selected the endpoint.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-bindingid"></a>

##### `BindingId`

```csharp
string BindingId { get; }
```

Gets the stable backend-for-frontend binding identifier.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-clientid"></a>

##### `ClientId`

```csharp
string ClientId { get; }
```

Gets the stable client identifier.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-endpoint"></a>

##### `Endpoint`

```csharp
RestEndpointRuntimeDescriptor Endpoint { get; }
```

Gets the published REST endpoint selected for that binding.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable binding-plus-endpoint identifier.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-matchedbehaviorids"></a>

##### `MatchedBehaviorIds`

```csharp
IReadOnlyList<string> MatchedBehaviorIds { get; }
```

Gets the included behavior identifiers that matched the published endpoint.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-matchedbydefault"></a>

##### `MatchedByDefault`

```csharp
bool MatchedByDefault { get; }
```

Gets a value indicating whether the endpoint stayed visible because the binding declared no positive include filters and the endpoint was not excluded.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-matchedcapabilitykeys"></a>

##### `MatchedCapabilityKeys`

```csharp
IReadOnlyList<string> MatchedCapabilityKeys { get; }
```

Gets the included required-capability keys that matched the published endpoint.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-matchedtags"></a>

##### `MatchedTags`

```csharp
IReadOnlyList<string> MatchedTags { get; }
```

Gets the included tags that matched the published endpoint.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-restendpointid"></a>

##### `RestEndpointId`

```csharp
string RestEndpointId { get; }
```

Gets the stable published REST endpoint identifier.

<a id="member-p-cephalon-abstractions-transports-backendforfrontendrestendpointruntimedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the binding-owner module identifier.

<a id="type-cephalon-abstractions-transports-ibackendforfrontendrestdocumentruntimecatalog"></a>

### `IBackendForFrontendRestDocumentRuntimeCatalog`

Exposes the client-aware REST documentation surfaces derived from the active backend-for-frontend bindings and published REST endpoint catalog.

Remarks: This runtime surface keeps filtered OpenAPI JSON and Scalar materialization aligned with the existing backend-for-frontend binding and REST endpoint runtime catalogs instead of introducing a host-only documentation registry.

#### Declaration
```csharp
public interface IBackendForFrontendRestDocumentRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-ibackendforfrontendrestdocumentruntimecatalog-documents"></a>

##### `Documents`

```csharp
IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> Documents { get; }
```

Gets all client-aware REST documentation surfaces visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-transports-ibackendforfrontendrestdocumentruntimecatalog-getbybindingid-system-string"></a>

##### `GetByBindingId`

```csharp
IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> GetByBindingId(string bindingId)
```

Gets all binding-scoped REST documentation surfaces owned by the requested binding.

Returns: The matching runtime descriptors, or an empty list when the binding is not active.

Parameters:
- `bindingId`: The backend-for-frontend binding identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-ibackendforfrontendrestdocumentruntimecatalog-getbyclientid-system-string"></a>

##### `GetByClientId`

```csharp
IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> GetByClientId(string clientId)
```

Gets all client-scoped REST documentation surfaces owned by the requested client.

Returns: The matching runtime descriptors, or an empty list when the client is not active.

Parameters:
- `clientId`: The client identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-ibackendforfrontendrestdocumentruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
BackendForFrontendRestDocumentRuntimeDescriptor GetById(string documentId)
```

Gets one client-aware REST documentation surface by its stable identifier.

Returns: The matching runtime descriptor, or `null` when it is not active.

Parameters:
- `documentId`: The documentation-surface identifier to resolve.

<a id="type-cephalon-abstractions-transports-ibackendforfrontendrestruntimecatalog"></a>

### `IBackendForFrontendRestRuntimeCatalog`

Exposes the client-aware published REST endpoint projections derived from the active backend-for-frontend bindings.

Remarks: This surface keeps backend-for-frontend client bindings and published REST endpoint material separate from the broader binding catalog so hosts can answer which REST endpoints are visible to each client binding without inventing a host-only registry outside the shared runtime truth.

#### Declaration
```csharp
public interface IBackendForFrontendRestRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-ibackendforfrontendrestruntimecatalog-endpoints"></a>

##### `Endpoints`

```csharp
IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> Endpoints { get; }
```

Gets all client-aware published REST endpoint projections visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-transports-ibackendforfrontendrestruntimecatalog-getbybindingid-system-string"></a>

##### `GetByBindingId`

```csharp
IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByBindingId(string bindingId)
```

Gets all client-aware published REST endpoint projections owned by the requested binding.

Returns: The matching runtime descriptors, or an empty list when the binding is not active.

Parameters:
- `bindingId`: The backend-for-frontend binding identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-ibackendforfrontendrestruntimecatalog-getbyclientid-system-string"></a>

##### `GetByClientId`

```csharp
IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByClientId(string clientId)
```

Gets all client-aware published REST endpoint projections owned by the requested client.

Returns: The matching runtime descriptors, or an empty list when the client is not active.

Parameters:
- `clientId`: The client identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-ibackendforfrontendrestruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
BackendForFrontendRestEndpointRuntimeDescriptor GetById(string runtimeEndpointId)
```

Gets one client-aware published REST endpoint projection by its stable identifier.

Returns: The matching runtime descriptor, or `null` when it is not active.

Parameters:
- `runtimeEndpointId`: The binding-plus-endpoint identifier to resolve.

<a id="member-m-cephalon-abstractions-transports-ibackendforfrontendrestruntimecatalog-getbyrestendpointid-system-string"></a>

##### `GetByRestEndpointId`

```csharp
IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByRestEndpointId(string restEndpointId)
```

Gets all client-aware runtime projections that expose the requested published REST endpoint.

Returns: The matching runtime descriptors, or an empty list when the endpoint is not visible.

Parameters:
- `restEndpointId`: The published REST endpoint identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-ibackendforfrontendrestruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all client-aware published REST endpoint projections contributed by the requested binding-owner module.

Returns: The matching runtime descriptors, or an empty list when the module is not active.

Parameters:
- `sourceModuleId`: The binding-owner module identifier to filter by.

<a id="type-cephalon-abstractions-transports-irestendpointauthoringpolicyruntimecatalog"></a>

### `IRestEndpointAuthoringPolicyRuntimeCatalog`

Exposes behavior-level REST authoring-policy visibility for the current runtime.

Remarks: This surface complements `IRestEndpointPublicationGroupRuntimeCatalog` by making authoring-policy intent plus authoring-policy-specific runtime effect readable without reopening grouped publication answers, while also keeping explicitly configured policies visible even when no current REST endpoint candidates match the behavior boundary.

#### Declaration
```csharp
public interface IRestEndpointAuthoringPolicyRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-irestendpointauthoringpolicyruntimecatalog-policies"></a>

##### `Policies`

```csharp
IReadOnlyList<RestEndpointAuthoringPolicyDescriptor> Policies { get; }
```

Gets the behavior-level REST authoring-policy answers visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-transports-irestendpointauthoringpolicyruntimecatalog-getbybehaviorid-system-string"></a>

##### `GetByBehaviorId`

```csharp
RestEndpointAuthoringPolicyDescriptor GetByBehaviorId(string behaviorId)
```

Gets one REST authoring-policy answer by behavior identifier.

Returns: The matching authoring-policy descriptor, or `null` when it is not present.

Parameters:
- `behaviorId`: The stable behavior identifier to resolve.

<a id="type-cephalon-abstractions-transports-irestendpointcandidateruntimecatalog"></a>

### `IRestEndpointCandidateRuntimeCatalog`

Exposes the module-owned REST endpoint candidates visible to the current runtime.

Remarks: This surface complements `IRestEndpointRuntimeCatalog` by showing candidate projections that were published or suppressed after precedence resolution rather than only the final active public REST endpoints.

#### Declaration
```csharp
public interface IRestEndpointCandidateRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-irestendpointcandidateruntimecatalog-candidates"></a>

##### `Candidates`

```csharp
IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> Candidates { get; }
```

Gets all REST endpoint candidates visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-transports-irestendpointcandidateruntimecatalog-getbybehaviorid-system-string"></a>

##### `GetByBehaviorId`

```csharp
IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> GetByBehaviorId(string behaviorId)
```

Gets all REST endpoint candidates that target the requested behavior identifier.

Returns: The matching candidate descriptors, or an empty list when no candidates exist.

Parameters:
- `behaviorId`: The stable behavior identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-irestendpointcandidateruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
RestEndpointCandidateRuntimeDescriptor GetById(string candidateId)
```

Gets one REST endpoint candidate by its stable identifier.

Returns: The matching candidate descriptor, or `null` when it is not present.

Parameters:
- `candidateId`: The candidate identifier to resolve.

<a id="member-m-cephalon-abstractions-transports-irestendpointcandidateruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all REST endpoint candidates owned by the requested source module.

Returns: The matching candidate descriptors, or an empty list when no candidates exist.

Parameters:
- `sourceModuleId`: The stable source-module identifier to filter by.

<a id="type-cephalon-abstractions-transports-irestendpointcandidateruntimeregistry"></a>

### `IRestEndpointCandidateRuntimeRegistry`

Collects REST endpoint candidates while the active host resolves publication precedence.

Remarks: Host adapters and transport helpers use this registry to publish candidate visibility into `IRestEndpointCandidateRuntimeCatalog`. Consumer code should normally read the catalog instead of mutating the registry directly.

#### Declaration
```csharp
public interface IRestEndpointCandidateRuntimeRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-irestendpointcandidateruntimeregistry-clear"></a>

##### `Clear`

```csharp
void Clear()
```

Clears any previously registered runtime candidates before a host rematerializes its REST surface.

<a id="member-m-cephalon-abstractions-transports-irestendpointcandidateruntimeregistry-register-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor"></a>

##### `Register`

```csharp
void Register(RestEndpointCandidateRuntimeDescriptor candidate)
```

Registers one REST endpoint candidate with the runtime catalog.

Parameters:
- `candidate`: The candidate descriptor to register.

<a id="type-cephalon-abstractions-transports-irestendpointoverrideruntimecatalog"></a>

### `IRestEndpointOverrideRuntimeCatalog`

Exposes the REST endpoint override rules visible to the current runtime.

Remarks: This surface complements `IRestEndpointCandidateRuntimeCatalog` by publishing the configured host-level override rules that can rewrite descriptor-backed module-owned REST candidates that participate in host governance, including shorthand candidates and explicit module-DSL route groups that opted in, before precedence resolution selects the final public REST surface.

#### Declaration
```csharp
public interface IRestEndpointOverrideRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-irestendpointoverrideruntimecatalog-overriderules"></a>

##### `OverrideRules`

```csharp
IReadOnlyList<RestEndpointOverrideDescriptor> OverrideRules { get; }
```

Gets all REST endpoint override rules visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-transports-irestendpointoverrideruntimecatalog-getbybehaviorid-system-string"></a>

##### `GetByBehaviorId`

```csharp
IReadOnlyList<RestEndpointOverrideDescriptor> GetByBehaviorId(string behaviorId)
```

Gets all REST endpoint override rules that target the requested behavior identifier, either directly or through configured behavior-id prefixes.

Returns: The matching override descriptors, or an empty list when no rules exist.

Parameters:
- `behaviorId`: The stable behavior identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-irestendpointoverrideruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
RestEndpointOverrideDescriptor GetById(string overrideId)
```

Gets one REST endpoint override rule by its stable identifier.

Returns: The matching override descriptor, or `null` when it is not present.

Parameters:
- `overrideId`: The override identifier to resolve.

<a id="member-m-cephalon-abstractions-transports-irestendpointoverrideruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<RestEndpointOverrideDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all REST endpoint override rules that target the requested source module identifier.

Returns: The matching override descriptors, or an empty list when no rules exist.

Parameters:
- `sourceModuleId`: The stable source-module identifier to filter by.

<a id="type-cephalon-abstractions-transports-irestendpointpublicationgroupruntimecatalog"></a>

### `IRestEndpointPublicationGroupRuntimeCatalog`

Exposes grouped REST endpoint publication visibility for behavior-backed public REST candidates.

Remarks: This surface complements `IRestEndpointCandidateRuntimeCatalog` by grouping the candidate-level truth per behavior so operators can inspect published, precedence-suppressed, and governance-suppressed shorthand outcomes without reconstructing that story manually.

#### Declaration
```csharp
public interface IRestEndpointPublicationGroupRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-irestendpointpublicationgroupruntimecatalog-groups"></a>

##### `Groups`

```csharp
IReadOnlyList<RestEndpointPublicationGroupDescriptor> Groups { get; }
```

Gets the grouped REST endpoint publication answers visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-transports-irestendpointpublicationgroupruntimecatalog-getbybehaviorid-system-string"></a>

##### `GetByBehaviorId`

```csharp
RestEndpointPublicationGroupDescriptor GetByBehaviorId(string behaviorId)
```

Gets one grouped REST endpoint publication answer by behavior identifier.

Returns: The matching grouped publication answer, or `null` when it is not present.

Parameters:
- `behaviorId`: The stable behavior identifier to resolve.

<a id="type-cephalon-abstractions-transports-irestendpointruntimecatalog"></a>

### `IRestEndpointRuntimeCatalog`

Exposes the resolved public REST endpoints visible to the current runtime.

Remarks: This surface is runtime-facing rather than app-model-facing because it reflects the effective REST endpoints published by the active host adapter after route prefixes, API version defaults, and endpoint materialization have been resolved.

#### Declaration
```csharp
public interface IRestEndpointRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-irestendpointruntimecatalog-endpoints"></a>

##### `Endpoints`

```csharp
IReadOnlyList<RestEndpointRuntimeDescriptor> Endpoints { get; }
```

Gets all resolved public REST endpoints visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-transports-irestendpointruntimecatalog-getbybehaviorid-system-string"></a>

##### `GetByBehaviorId`

```csharp
IReadOnlyList<RestEndpointRuntimeDescriptor> GetByBehaviorId(string behaviorId)
```

Gets all resolved public REST endpoints that dispatch through the requested behavior identifier.

Returns: The matching endpoint descriptors, or an empty list when the behavior is not exposed publicly.

Parameters:
- `behaviorId`: The stable behavior identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-irestendpointruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
RestEndpointRuntimeDescriptor GetById(string endpointId)
```

Gets one resolved public REST endpoint by its stable identifier.

Returns: The matching endpoint descriptor, or `null` when it is not active.

Parameters:
- `endpointId`: The endpoint identifier to resolve.

<a id="member-m-cephalon-abstractions-transports-irestendpointruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<RestEndpointRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all resolved public REST endpoints owned by the requested source module.

Returns: The matching endpoint descriptors, or an empty list when the module is not active.

Parameters:
- `sourceModuleId`: The stable source-module identifier to filter by.

<a id="type-cephalon-abstractions-transports-irestendpointruntimeregistry"></a>

### `IRestEndpointRuntimeRegistry`

Collects resolved public REST endpoints while the active host materializes transport routes.

Remarks: Host adapters and transport helpers use this registry to publish the resolved Cephalon-owned REST endpoint surface into `IRestEndpointRuntimeCatalog`. Consumer code should normally read the catalog instead of mutating the registry directly.

#### Declaration
```csharp
public interface IRestEndpointRuntimeRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-irestendpointruntimeregistry-clear"></a>

##### `Clear`

```csharp
void Clear()
```

Clears any previously registered runtime endpoints before a host rematerializes its REST surface.

<a id="member-m-cephalon-abstractions-transports-irestendpointruntimeregistry-register-cephalon-abstractions-transports-restendpointruntimedescriptor"></a>

##### `Register`

```csharp
void Register(RestEndpointRuntimeDescriptor endpoint)
```

Registers one resolved public REST endpoint with the runtime catalog.

Parameters:
- `endpoint`: The resolved endpoint descriptor to register.

<a id="type-cephalon-abstractions-transports-irestendpointsuppressionruntimecatalog"></a>

### `IRestEndpointSuppressionRuntimeCatalog`

Exposes the REST endpoint suppression rules visible to the current runtime.

Remarks: This surface complements `IRestEndpointCandidateRuntimeCatalog` by publishing the configured host-level suppression rules that can hide descriptor-backed module-owned REST candidates that participate in host governance, including shorthand candidates and explicit module-DSL route groups that opted in, before precedence resolution selects the final public REST surface.

#### Declaration
```csharp
public interface IRestEndpointSuppressionRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-irestendpointsuppressionruntimecatalog-suppressions"></a>

##### `Suppressions`

```csharp
IReadOnlyList<RestEndpointSuppressionDescriptor> Suppressions { get; }
```

Gets all REST endpoint suppression rules visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-transports-irestendpointsuppressionruntimecatalog-getbybehaviorid-system-string"></a>

##### `GetByBehaviorId`

```csharp
IReadOnlyList<RestEndpointSuppressionDescriptor> GetByBehaviorId(string behaviorId)
```

Gets all REST endpoint suppression rules that target the requested behavior identifier, either directly or through configured behavior-id prefixes.

Returns: The matching suppression descriptors, or an empty list when no rules exist.

Parameters:
- `behaviorId`: The stable behavior identifier to filter by.

<a id="member-m-cephalon-abstractions-transports-irestendpointsuppressionruntimecatalog-getbyid-system-string"></a>

##### `GetById`

```csharp
RestEndpointSuppressionDescriptor GetById(string suppressionId)
```

Gets one REST endpoint suppression rule by its stable identifier.

Returns: The matching suppression descriptor, or `null` when it is not present.

Parameters:
- `suppressionId`: The suppression identifier to resolve.

<a id="member-m-cephalon-abstractions-transports-irestendpointsuppressionruntimecatalog-getbysourcemodule-system-string"></a>

##### `GetBySourceModule`

```csharp
IReadOnlyList<RestEndpointSuppressionDescriptor> GetBySourceModule(string sourceModuleId)
```

Gets all REST endpoint suppression rules that target the requested source module identifier.

Returns: The matching suppression descriptors, or an empty list when no rules exist.

Parameters:
- `sourceModuleId`: The stable source-module identifier to filter by.

<a id="type-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor"></a>

### `RestEndpointAuthoringPolicyAuthoringStyleDescriptor`

Describes one authoring-style partition inside a rule-centric REST authoring-policy runtime answer.

Remarks: This descriptor keeps one behavior-level authoring-policy answer readable without reopening the broader publication-group surface when operators need to understand how authoring-policy, precedence, and later governance outcomes distribute across authoring styles.

#### Declaration
```csharp
public sealed class RestEndpointAuthoringPolicyAuthoringStyleDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionsummarydescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernancesuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceskippedsuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceskippedoverridesummarydescriptor"></a>

##### `RestEndpointAuthoringPolicyAuthoringStyleDescriptor`

```csharp
RestEndpointAuthoringPolicyAuthoringStyleDescriptor(string authoringStyle, IReadOnlyList<string> candidateIds, IReadOnlyList<string> retainedCandidateIds, IReadOnlyList<string> publishedCandidateIds, IReadOnlyList<string> precedenceSuppressedCandidateIds, IReadOnlyList<string> governanceSuppressedCandidateIds, IReadOnlyList<string> suppressedCandidateIds, IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind> suppressionKinds, IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor> suppressionSummaries, IReadOnlyList<string> hostGovernanceEligibleCandidateIds, IReadOnlyList<string> hostGovernanceIneligibleCandidateIds, IReadOnlyList<string> skippedSuppressionIds, IReadOnlyList<string> skippedOverrideIds, IReadOnlyList<RestEndpointGovernanceSuppressionSummaryDescriptor> governanceSuppressionSummaries, IReadOnlyList<RestEndpointGovernanceOverrideSummaryDescriptor> governanceOverrideSummaries, IReadOnlyList<RestEndpointGovernanceSkippedSuppressionSummaryDescriptor> skippedSuppressionSummaries, IReadOnlyList<RestEndpointGovernanceSkippedOverrideSummaryDescriptor> skippedOverrideSummaries)
```

Creates an authoring-style partition for a rule-centric REST authoring-policy runtime answer.

Parameters:
- `authoringStyle`: The normalized authoring style summarized by this entry.
- `candidateIds`: The ordered candidate identifiers contributed by this authoring style before authoring-policy enforcement is considered.
- `retainedCandidateIds`: The ordered candidate identifiers that survived authoring-policy enforcement for this authoring style, even if later precedence or host governance suppressed them.
- `publishedCandidateIds`: The ordered candidate identifiers that remain published after all runtime publication steps for this authoring style.
- `precedenceSuppressedCandidateIds`: The ordered candidate identifiers that survived authoring policy but were later suppressed by candidate precedence for this authoring style.
- `governanceSuppressedCandidateIds`: The ordered candidate identifiers that survived authoring policy but were later suppressed by host-level REST governance for this authoring style.
- `suppressedCandidateIds`: The ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement for this authoring style.
- `suppressionKinds`: The grouped authoring-policy suppression kinds that appear in the runtime effect for this authoring style.
- `suppressionSummaries`: The grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.
- `hostGovernanceEligibleCandidateIds`: The ordered candidate identifiers whose original projections allowed host governance to participate for this authoring style.
- `hostGovernanceIneligibleCandidateIds`: The ordered candidate identifiers whose original projections kept host governance out of scope for this authoring style.
- `skippedSuppressionIds`: The ordered suppression-rule identifiers that targeted host-governance-ineligible candidates for this authoring style.
- `skippedOverrideIds`: The ordered override-rule identifiers that targeted host-governance-ineligible candidates for this authoring style.
- `governanceSuppressionSummaries`: The grouped host-governance suppression-rule outcomes summarized by rule for this authoring style.
- `governanceOverrideSummaries`: The grouped host-governance override-rule outcomes summarized by rule for this authoring style.
- `skippedSuppressionSummaries`: The grouped host-governance-skipped suppression-rule outcomes summarized by rule for this authoring style.
- `skippedOverrideSummaries`: The grouped host-governance-skipped override-rule outcomes summarized by rule for this authoring style.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-authoringstyle"></a>

##### `AuthoringStyle`

```csharp
string AuthoringStyle { get; }
```

Gets the normalized authoring style summarized by this entry.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers contributed by this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-governanceoverridesummaries"></a>

##### `GovernanceOverrideSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceOverrideSummaryDescriptor> GovernanceOverrideSummaries { get; }
```

Gets the grouped host-governance override-rule outcomes summarized by rule for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-governancesuppressedcandidateids"></a>

##### `GovernanceSuppressedCandidateIds`

```csharp
IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that survived authoring policy but were later suppressed by host governance for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-governancesuppressionsummaries"></a>

##### `GovernanceSuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSuppressionSummaryDescriptor> GovernanceSuppressionSummaries { get; }
```

Gets the grouped host-governance suppression-rule outcomes summarized by rule for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-hostgovernanceeligiblecandidateids"></a>

##### `HostGovernanceEligibleCandidateIds`

```csharp
IReadOnlyList<string> HostGovernanceEligibleCandidateIds { get; }
```

Gets the ordered candidate identifiers whose original projections allowed host governance to participate for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-hostgovernanceineligiblecandidateids"></a>

##### `HostGovernanceIneligibleCandidateIds`

```csharp
IReadOnlyList<string> HostGovernanceIneligibleCandidateIds { get; }
```

Gets the ordered candidate identifiers whose original projections kept host governance out of scope for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-precedencesuppressedcandidateids"></a>

##### `PrecedenceSuppressedCandidateIds`

```csharp
IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that survived authoring policy but were later suppressed by precedence for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-publishedcandidateids"></a>

##### `PublishedCandidateIds`

```csharp
IReadOnlyList<string> PublishedCandidateIds { get; }
```

Gets the ordered candidate identifiers that remain published after all runtime publication steps for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-retainedcandidateids"></a>

##### `RetainedCandidateIds`

```csharp
IReadOnlyList<string> RetainedCandidateIds { get; }
```

Gets the ordered candidate identifiers that survived authoring-policy enforcement for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-skippedoverrideids"></a>

##### `SkippedOverrideIds`

```csharp
IReadOnlyList<string> SkippedOverrideIds { get; }
```

Gets the ordered override-rule identifiers that targeted host-governance-ineligible candidates for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-skippedoverridesummaries"></a>

##### `SkippedOverrideSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSkippedOverrideSummaryDescriptor> SkippedOverrideSummaries { get; }
```

Gets the grouped host-governance-skipped override-rule outcomes summarized by rule for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-skippedsuppressionids"></a>

##### `SkippedSuppressionIds`

```csharp
IReadOnlyList<string> SkippedSuppressionIds { get; }
```

Gets the ordered suppression-rule identifiers that targeted host-governance-ineligible candidates for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-skippedsuppressionsummaries"></a>

##### `SkippedSuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSkippedSuppressionSummaryDescriptor> SkippedSuppressionSummaries { get; }
```

Gets the grouped host-governance-skipped suppression-rule outcomes summarized by rule for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-suppressedcandidateids"></a>

##### `SuppressedCandidateIds`

```csharp
IReadOnlyList<string> SuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-suppressionkinds"></a>

##### `SuppressionKinds`

```csharp
IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind> SuppressionKinds { get; }
```

Gets the grouped authoring-policy suppression kinds visible in the runtime effect for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-suppressionsummaries"></a>

##### `SuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor> SuppressionSummaries { get; }
```

Gets the grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.

<a id="type-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor"></a>

### `RestEndpointAuthoringPolicyDescriptor`

Describes one behavior-level REST authoring policy together with its runtime effect.

Remarks: This descriptor keeps authoring-policy intent readable as a first-class runtime answer while preserving separate buckets for authoring-policy suppression, later precedence suppression, and later governance suppression.

#### Declaration
```csharp
public sealed class RestEndpointAuthoringPolicyDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-ctor-system-string-system-boolean-system-boolean-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionsummarydescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointauthoringpolicyauthoringstyledescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernancesuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceskippedsuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceskippedoverridesummarydescriptor"></a>

##### `RestEndpointAuthoringPolicyDescriptor`

```csharp
RestEndpointAuthoringPolicyDescriptor(string behaviorId, bool isConfigured, bool allowMultiplePublishedCandidates, string preferredAuthoringStyle, IReadOnlyList<string> allowedAuthoringStyles, IReadOnlyList<string> disallowedAuthoringStyles, IReadOnlyList<string> candidateIds, IReadOnlyList<string> retainedCandidateIds, IReadOnlyList<string> publishedCandidateIds, IReadOnlyList<string> precedenceSuppressedCandidateIds, IReadOnlyList<string> governanceSuppressedCandidateIds, IReadOnlyList<string> suppressedCandidateIds, IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind> suppressionKinds, IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor> suppressionSummaries, IReadOnlyList<string> hostGovernanceEligibleCandidateIds, IReadOnlyList<string> hostGovernanceIneligibleCandidateIds, IReadOnlyList<string> skippedSuppressionIds, IReadOnlyList<string> skippedOverrideIds, IReadOnlyList<RestEndpointAuthoringPolicyAuthoringStyleDescriptor> authoringStyleSummaries, IReadOnlyList<RestEndpointGovernanceSuppressionSummaryDescriptor> governanceSuppressionSummaries, IReadOnlyList<RestEndpointGovernanceOverrideSummaryDescriptor> governanceOverrideSummaries, IReadOnlyList<RestEndpointGovernanceSkippedSuppressionSummaryDescriptor> skippedSuppressionSummaries, IReadOnlyList<RestEndpointGovernanceSkippedOverrideSummaryDescriptor> skippedOverrideSummaries)
```

Creates a REST authoring-policy runtime descriptor.

Parameters:
- `behaviorId`: The stable behavior identifier that this policy applies to.
- `isConfigured`: `true` when the policy was supplied through host configuration; otherwise `false` when the runtime is exposing the implicit default policy.
- `allowMultiplePublishedCandidates`: `true` when the policy explicitly allows more than one projection candidate to remain published for the same behavior boundary after authoring-policy enforcement.
- `preferredAuthoringStyle`: The normalized preferred authoring style when the policy declares one.
- `allowedAuthoringStyles`: The normalized authoring styles that the policy explicitly allows for this behavior boundary when one or more are declared.
- `disallowedAuthoringStyles`: The normalized authoring styles that the policy explicitly disallows for this behavior boundary when one or more are declared.
- `candidateIds`: The ordered candidate identifiers visible for this behavior boundary before authoring-policy enforcement is considered.
- `retainedCandidateIds`: The ordered candidate identifiers that survived authoring-policy enforcement, even if a later precedence or host-governance step suppressed them.
- `publishedCandidateIds`: The ordered candidate identifiers that remain published after all runtime publication steps.
- `precedenceSuppressedCandidateIds`: The ordered candidate identifiers that survived authoring policy but were later suppressed by candidate precedence.
- `governanceSuppressedCandidateIds`: The ordered candidate identifiers that survived authoring policy but were later suppressed by host-level REST governance.
- `suppressedCandidateIds`: The ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
- `suppressionKinds`: The grouped authoring-policy suppression kinds that appear in the runtime effect.
- `suppressionSummaries`: The grouped authoring-policy suppression outcomes summarized by suppression kind.
- `hostGovernanceEligibleCandidateIds`: The ordered candidate identifiers whose original projections allowed host governance to participate for this behavior boundary.
- `hostGovernanceIneligibleCandidateIds`: The ordered candidate identifiers whose original projections kept host governance out of scope for this behavior boundary.
- `skippedSuppressionIds`: The ordered suppression-rule identifiers that targeted host-governance-ineligible candidates in this behavior boundary.
- `skippedOverrideIds`: The ordered override-rule identifiers that targeted host-governance-ineligible candidates in this behavior boundary.
- `authoringStyleSummaries`: The per-authoring-style runtime buckets that explain how this policy's candidate, retained, published, precedence-suppressed, governance-suppressed, and authoring-policy-suppressed outcomes distribute across authoring styles.
- `governanceSuppressionSummaries`: The grouped host-governance suppression-rule outcomes summarized by rule for this behavior boundary.
- `governanceOverrideSummaries`: The grouped host-governance override-rule outcomes summarized by rule for this behavior boundary.
- `skippedSuppressionSummaries`: The grouped host-governance-skipped suppression-rule outcomes summarized by rule for this behavior boundary.
- `skippedOverrideSummaries`: The grouped host-governance-skipped override-rule outcomes summarized by rule for this behavior boundary.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-allowedauthoringstyles"></a>

##### `AllowedAuthoringStyles`

```csharp
IReadOnlyList<string> AllowedAuthoringStyles { get; }
```

Gets the normalized authoring styles that the policy explicitly allows.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-allowmultiplepublishedcandidates"></a>

##### `AllowMultiplePublishedCandidates`

```csharp
bool AllowMultiplePublishedCandidates { get; }
```

Gets a value indicating whether the policy explicitly allows multiple published candidates for the same behavior boundary after authoring-policy enforcement.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-authoringstylesummaries"></a>

##### `AuthoringStyleSummaries`

```csharp
IReadOnlyList<RestEndpointAuthoringPolicyAuthoringStyleDescriptor> AuthoringStyleSummaries { get; }
```

Gets the per-authoring-style runtime buckets that explain how this policy's outcomes distribute across authoring styles.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the stable behavior identifier that this authoring policy applies to.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers visible for this behavior boundary.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-disallowedauthoringstyles"></a>

##### `DisallowedAuthoringStyles`

```csharp
IReadOnlyList<string> DisallowedAuthoringStyles { get; }
```

Gets the normalized authoring styles that the policy explicitly disallows.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-governanceoverridesummaries"></a>

##### `GovernanceOverrideSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceOverrideSummaryDescriptor> GovernanceOverrideSummaries { get; }
```

Gets the grouped host-governance override-rule outcomes summarized by rule.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-governancesuppressedcandidateids"></a>

##### `GovernanceSuppressedCandidateIds`

```csharp
IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that survived authoring policy but were later suppressed by host governance.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-governancesuppressionsummaries"></a>

##### `GovernanceSuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSuppressionSummaryDescriptor> GovernanceSuppressionSummaries { get; }
```

Gets the grouped host-governance suppression-rule outcomes summarized by rule.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-hostgovernanceeligiblecandidateids"></a>

##### `HostGovernanceEligibleCandidateIds`

```csharp
IReadOnlyList<string> HostGovernanceEligibleCandidateIds { get; }
```

Gets the ordered candidate identifiers whose original projections allowed host governance to participate.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-hostgovernanceineligiblecandidateids"></a>

##### `HostGovernanceIneligibleCandidateIds`

```csharp
IReadOnlyList<string> HostGovernanceIneligibleCandidateIds { get; }
```

Gets the ordered candidate identifiers whose original projections kept host governance out of scope.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-isconfigured"></a>

##### `IsConfigured`

```csharp
bool IsConfigured { get; }
```

Gets a value indicating whether this authoring policy came from explicit host configuration.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-precedencesuppressedcandidateids"></a>

##### `PrecedenceSuppressedCandidateIds`

```csharp
IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that survived authoring policy but were later suppressed by precedence.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-preferredauthoringstyle"></a>

##### `PreferredAuthoringStyle`

```csharp
string PreferredAuthoringStyle { get; }
```

Gets the normalized preferred authoring style when the policy declares one.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-publishedcandidateids"></a>

##### `PublishedCandidateIds`

```csharp
IReadOnlyList<string> PublishedCandidateIds { get; }
```

Gets the ordered candidate identifiers that remain published after all runtime publication steps.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-retainedcandidateids"></a>

##### `RetainedCandidateIds`

```csharp
IReadOnlyList<string> RetainedCandidateIds { get; }
```

Gets the ordered candidate identifiers that survived authoring-policy enforcement.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-skippedoverrideids"></a>

##### `SkippedOverrideIds`

```csharp
IReadOnlyList<string> SkippedOverrideIds { get; }
```

Gets the ordered override-rule identifiers that targeted host-governance-ineligible candidates in this behavior boundary.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-skippedoverridesummaries"></a>

##### `SkippedOverrideSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSkippedOverrideSummaryDescriptor> SkippedOverrideSummaries { get; }
```

Gets the grouped host-governance-skipped override-rule outcomes summarized by rule.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-skippedsuppressionids"></a>

##### `SkippedSuppressionIds`

```csharp
IReadOnlyList<string> SkippedSuppressionIds { get; }
```

Gets the ordered suppression-rule identifiers that targeted host-governance-ineligible candidates in this behavior boundary.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-skippedsuppressionsummaries"></a>

##### `SkippedSuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSkippedSuppressionSummaryDescriptor> SkippedSuppressionSummaries { get; }
```

Gets the grouped host-governance-skipped suppression-rule outcomes summarized by rule.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-suppressedcandidateids"></a>

##### `SuppressedCandidateIds`

```csharp
IReadOnlyList<string> SuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-suppressionkinds"></a>

##### `SuppressionKinds`

```csharp
IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind> SuppressionKinds { get; }
```

Gets the grouped authoring-policy suppression kinds visible in the runtime effect.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicydescriptor-suppressionsummaries"></a>

##### `SuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor> SuppressionSummaries { get; }
```

Gets the grouped authoring-policy suppression outcomes summarized by suppression kind.

<a id="type-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind"></a>

### `RestEndpointAuthoringPolicySuppressionKind`

Describes why a REST endpoint candidate was suppressed by authoring-policy enforcement.

#### Declaration
```csharp
public enum RestEndpointAuthoringPolicySuppressionKind
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-disallowedauthoringstyle"></a>

##### `DisallowedAuthoringStyle`

```csharp
const RestEndpointAuthoringPolicySuppressionKind DisallowedAuthoringStyle
```

The candidate authoring style is explicitly disallowed by the behavior-level authoring policy.

<a id="member-f-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-notallowedauthoringstyle"></a>

##### `NotAllowedAuthoringStyle`

```csharp
const RestEndpointAuthoringPolicySuppressionKind NotAllowedAuthoringStyle
```

The candidate authoring style is outside the explicitly allowed authoring-style set.

<a id="member-f-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-preferredauthoringstyleselected"></a>

##### `PreferredAuthoringStyleSelected`

```csharp
const RestEndpointAuthoringPolicySuppressionKind PreferredAuthoringStyleSelected
```

The candidate was suppressed because a preferred authoring style is present for the behavior boundary.

<a id="member-f-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-unspecified"></a>

##### `Unspecified`

```csharp
const RestEndpointAuthoringPolicySuppressionKind Unspecified
```

The candidate was not classified with an authoring-policy suppression kind.

<a id="type-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkindextensions"></a>

### `RestEndpointAuthoringPolicySuppressionKindExtensions`

Provides canonical wire-name helpers for `RestEndpointAuthoringPolicySuppressionKind`.

#### Declaration
```csharp
public static class RestEndpointAuthoringPolicySuppressionKindExtensions
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkindextensions-getwirename-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind"></a>

##### `GetWireName`

```csharp
string GetWireName(this RestEndpointAuthoringPolicySuppressionKind kind)
```

Gets the stable wire name used by JSON serialization and runtime introspection for the suppression kind.

Returns: The stable wire name.

Parameters:
- `kind`: The suppression kind.

<a id="member-m-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkindextensions-tryparsewirename-system-string-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind"></a>

##### `TryParseWireName`

```csharp
bool TryParseWireName(string value, out RestEndpointAuthoringPolicySuppressionKind kind)
```

Tries to parse the stable wire name used by JSON serialization and runtime introspection into a suppression kind.

Returns: `true` when the wire name maps to a supported suppression kind; otherwise, `false`.

Parameters:
- `value`: The wire name to parse.
- `kind`: The parsed suppression kind when the wire name is recognized.

<a id="type-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionsummarydescriptor"></a>

### `RestEndpointAuthoringPolicySuppressionSummaryDescriptor`

Describes one grouped authoring-policy suppression outcome for a rule-centric REST runtime answer.

#### Declaration
```csharp
public sealed class RestEndpointAuthoringPolicySuppressionSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionsummarydescriptor-ctor-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointAuthoringPolicySuppressionSummaryDescriptor`

```csharp
RestEndpointAuthoringPolicySuppressionSummaryDescriptor(RestEndpointAuthoringPolicySuppressionKind kind, IReadOnlyList<string> candidateIds)
```

Creates an authoring-policy suppression summary descriptor.

Parameters:
- `kind`: The authoring-policy suppression kind summarized by this entry.
- `candidateIds`: The ordered candidate identifiers suppressed by this suppression kind.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionsummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers suppressed by this suppression kind.

<a id="member-p-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionsummarydescriptor-kind"></a>

##### `Kind`

```csharp
RestEndpointAuthoringPolicySuppressionKind Kind { get; }
```

Gets the authoring-policy suppression kind summarized by this entry.

<a id="type-cephalon-abstractions-transports-restendpointbindingdescriptor"></a>

### `RestEndpointBindingDescriptor`

Describes one resolved request-binding rule for a public REST endpoint.

#### Declaration
```csharp
public sealed class RestEndpointBindingDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointbindingdescriptor-ctor-system-string-cephalon-abstractions-transports-restendpointbindingsource-system-string"></a>

##### `RestEndpointBindingDescriptor`

```csharp
RestEndpointBindingDescriptor(string propertyName, RestEndpointBindingSource source, string name)
```

Creates a resolved REST endpoint binding descriptor.

Parameters:
- `propertyName`: The target request-model property name.
- `source`: The HTTP request source that supplies the value.
- `name`: The route/query/header/body member name when one is available.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointbindingdescriptor-name"></a>

##### `Name`

```csharp
string Name { get; }
```

Gets the route/query/header/body member name when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointbindingdescriptor-propertyname"></a>

##### `PropertyName`

```csharp
string PropertyName { get; }
```

Gets the target request-model property name.

<a id="member-p-cephalon-abstractions-transports-restendpointbindingdescriptor-source"></a>

##### `Source`

```csharp
RestEndpointBindingSource Source { get; }
```

Gets the HTTP request source that supplies the value.

<a id="type-cephalon-abstractions-transports-restendpointbindingfallbackmode"></a>

### `RestEndpointBindingFallbackMode`

Describes a resolved REST request-binding fallback mode when the runtime preserves behavior beyond the explicit binding plan.

#### Declaration
```csharp
public enum RestEndpointBindingFallbackMode
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-restendpointbindingfallbackmode-preserveremainingbodyfallback"></a>

##### `PreserveRemainingBodyFallback`

```csharp
const RestEndpointBindingFallbackMode PreserveRemainingBodyFallback
```

Preserves the deterministic remaining request-body fallback surface for unbound properties on body-capable endpoints that still expose an explicit binding plan.

<a id="member-f-cephalon-abstractions-transports-restendpointbindingfallbackmode-preservesourceimplicitfallback"></a>

##### `PreserveSourceImplicitFallback`

```csharp
const RestEndpointBindingFallbackMode PreserveSourceImplicitFallback
```

Preserves the remaining implicit fallback surface from the source shorthand projection.

<a id="type-cephalon-abstractions-transports-restendpointbindingfallbackmodeextensions"></a>

### `RestEndpointBindingFallbackModeExtensions`

Provides canonical wire-name helpers for `RestEndpointBindingFallbackMode`.

#### Declaration
```csharp
public static class RestEndpointBindingFallbackModeExtensions
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-restendpointbindingfallbackmodeextensions-getwirename-cephalon-abstractions-transports-restendpointbindingfallbackmode"></a>

##### `GetWireName`

```csharp
string GetWireName(this RestEndpointBindingFallbackMode mode)
```

Gets the stable wire name used by JSON serialization and compatibility metadata for the fallback mode.

Returns: The stable wire name.

Parameters:
- `mode`: The fallback mode.

<a id="member-m-cephalon-abstractions-transports-restendpointbindingfallbackmodeextensions-tryparsewirename-system-string-cephalon-abstractions-transports-restendpointbindingfallbackmode"></a>

##### `TryParseWireName`

```csharp
bool TryParseWireName(string value, out RestEndpointBindingFallbackMode mode)
```

Tries to parse the stable wire name used by JSON serialization and compatibility metadata into a fallback mode.

Returns: `true` when the wire name maps to a supported fallback mode; otherwise, `false`.

Parameters:
- `value`: The wire name to parse.
- `mode`: The parsed fallback mode when the wire name is recognized.

<a id="type-cephalon-abstractions-transports-restendpointbindingsource"></a>

### `RestEndpointBindingSource`

Identifies which part of the HTTP request populates one resolved REST endpoint input binding.

#### Declaration
```csharp
public enum RestEndpointBindingSource
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-restendpointbindingsource-body"></a>

##### `Body`

```csharp
const RestEndpointBindingSource Body
```

Reads the value from the JSON request body.

<a id="member-f-cephalon-abstractions-transports-restendpointbindingsource-header"></a>

##### `Header`

```csharp
const RestEndpointBindingSource Header
```

Reads the value from an HTTP header.

<a id="member-f-cephalon-abstractions-transports-restendpointbindingsource-query"></a>

##### `Query`

```csharp
const RestEndpointBindingSource Query
```

Reads the value from the query string.

<a id="member-f-cephalon-abstractions-transports-restendpointbindingsource-route"></a>

##### `Route`

```csharp
const RestEndpointBindingSource Route
```

Reads the value from a route placeholder such as `{orderId}`.

<a id="member-f-cephalon-abstractions-transports-restendpointbindingsource-unspecified"></a>

##### `Unspecified`

```csharp
const RestEndpointBindingSource Unspecified
```

No explicit source has been selected.

<a id="type-cephalon-abstractions-transports-restendpointbindingsourceextensions"></a>

### `RestEndpointBindingSourceExtensions`

Provides canonical wire-name helpers for `RestEndpointBindingSource`.

#### Declaration
```csharp
public static class RestEndpointBindingSourceExtensions
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-restendpointbindingsourceextensions-getwirename-cephalon-abstractions-transports-restendpointbindingsource"></a>

##### `GetWireName`

```csharp
string GetWireName(this RestEndpointBindingSource source)
```

Gets the stable wire name used by JSON serialization and REST governance config for the binding source.

Returns: The stable wire name.

Parameters:
- `source`: The binding source.

<a id="member-m-cephalon-abstractions-transports-restendpointbindingsourceextensions-tryparsewirename-system-string-cephalon-abstractions-transports-restendpointbindingsource"></a>

##### `TryParseWireName`

```csharp
bool TryParseWireName(string value, out RestEndpointBindingSource source)
```

Tries to parse the stable wire name used by JSON serialization and REST governance config into a binding source.

Returns: `true` when the wire name maps to a supported binding source; otherwise, `false`.

Parameters:
- `value`: The wire name to parse.
- `source`: The parsed binding source when the wire name is recognized.

<a id="type-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor"></a>

### `RestEndpointCandidateProjectionDescriptor`

Describes one module-owned REST candidate projection shape before publication precedence or host-level overrides are applied.

#### Declaration
```csharp
public sealed class RestEndpointCandidateProjectionDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-ctor-system-string-system-string-system-string-system-string-system-nullable-system-int32-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingdescriptor-system-nullable-cephalon-abstractions-transports-restendpointbindingfallbackmode-system-string-system-boolean-system-string"></a>

##### `RestEndpointCandidateProjectionDescriptor`

```csharp
RestEndpointCandidateProjectionDescriptor(string method, string routePattern, string routeGroupPrefix, string relativePattern, int? apiVersionMajor, string openApiDocumentName, IReadOnlyList<RestEndpointBindingDescriptor> bindingDescriptors, RestEndpointBindingFallbackMode? bindingFallbackMode, string tagName, bool allowsHostGovernance, string hostGovernanceScope)
```

Creates a REST endpoint candidate projection descriptor.

Parameters:
- `method`: The projected HTTP method.
- `routePattern`: The projected route pattern including the host REST prefix.
- `routeGroupPrefix`: The projected route-group prefix including the host REST prefix.
- `relativePattern`: The projected route pattern relative to the owning route group.
- `apiVersionMajor`: The projected public API major version when one is available.
- `openApiDocumentName`: The projected OpenAPI document name when one is available.
- `bindingDescriptors`: The projected request-binding descriptors when the projection exposes an explicit binding plan.
- `bindingFallbackMode`: The projected request-binding fallback mode when the projection preserves deterministic request-binding behavior beyond the explicit binding plan, such as preserved source implicit-query fallback or preserved remaining request-body fallback.
- `tagName`: The projected primary OpenAPI tag name when one is available.
- `allowsHostGovernance`: `true` when host-level REST suppression and override rules are allowed to govern this projection candidate; otherwise, `false`. Shorthand candidates typically enable this automatically, while explicit module-DSL candidates stay authoritative unless the owning route group opts into host governance.
- `hostGovernanceScope`: The stable host-governance scope carried by the original authored route group when one is available.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-allowshostgovernance"></a>

##### `AllowsHostGovernance`

```csharp
bool AllowsHostGovernance { get; }
```

Gets a value indicating whether host-level REST suppression and override rules are allowed to govern this projection candidate. Shorthand candidates typically enable this automatically, while explicit module-DSL candidates stay authoritative unless the owning route group opts into host governance.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-apiversionmajor"></a>

##### `ApiVersionMajor`

```csharp
int? ApiVersionMajor { get; }
```

Gets the projected public API major version when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-bindingdescriptors"></a>

##### `BindingDescriptors`

```csharp
IReadOnlyList<RestEndpointBindingDescriptor> BindingDescriptors { get; }
```

Gets the projected request-binding descriptors when the projection exposes an explicit binding plan.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-bindingfallbackmode"></a>

##### `BindingFallbackMode`

```csharp
RestEndpointBindingFallbackMode? BindingFallbackMode { get; }
```

Gets the projected request-binding fallback mode when the projection preserves deterministic request-binding behavior beyond the explicit binding plan, such as preserved source implicit-query fallback or preserved remaining request-body fallback.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-hostgovernancescope"></a>

##### `HostGovernanceScope`

```csharp
string HostGovernanceScope { get; }
```

Gets the stable host-governance scope carried by the original authored route group when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-method"></a>

##### `Method`

```csharp
string Method { get; }
```

Gets the projected HTTP method.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-openapidocumentname"></a>

##### `OpenApiDocumentName`

```csharp
string OpenApiDocumentName { get; }
```

Gets the projected OpenAPI document name when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-relativepattern"></a>

##### `RelativePattern`

```csharp
string RelativePattern { get; }
```

Gets the projected route pattern relative to the owning route group.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-routegroupprefix"></a>

##### `RouteGroupPrefix`

```csharp
string RouteGroupPrefix { get; }
```

Gets the projected route-group prefix including the host REST prefix.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-routepattern"></a>

##### `RoutePattern`

```csharp
string RoutePattern { get; }
```

Gets the projected route pattern including the host REST prefix.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-tagname"></a>

##### `TagName`

```csharp
string TagName { get; }
```

Gets the projected primary OpenAPI tag name when one is available.

<a id="type-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor"></a>

### `RestEndpointCandidateRuntimeDescriptor`

Describes one module-owned REST endpoint candidate and whether it was published or suppressed.

#### Declaration
```csharp
public sealed class RestEndpointCandidateRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-ctor-system-string-cephalon-abstractions-transports-restendpointruntimedescriptor-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-system-string-system-int32-cephalon-abstractions-transports-restendpointcandidatestatus-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-nullable-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-system-string-system-nullable-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-system-nullable-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointoverrideactionkind-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointoverrideactionkind"></a>

##### `RestEndpointCandidateRuntimeDescriptor`

```csharp
RestEndpointCandidateRuntimeDescriptor(string id, RestEndpointRuntimeDescriptor projectedEndpoint, RestEndpointCandidateProjectionDescriptor originalProjection, string authoringStyle, int precedenceRank, RestEndpointCandidateStatus status, string suppressedByCandidateId, string suppressedBySuppressionId, string appliedOverrideId, IReadOnlyList<string> matchedSuppressionIds, IReadOnlyList<string> matchedOverrideIds, string suppressionReason, RestEndpointAuthoringPolicySuppressionKind? suppressedByAuthoringPolicyKind, string selectedOverrideId, RestEndpointGovernanceRuleSelectionBasis? suppressionSelectionBasis, RestEndpointGovernanceRuleSelectionBasis? overrideSelectionBasis, IReadOnlyList<string> skippedSuppressionIds, IReadOnlyList<string> skippedOverrideIds, IReadOnlyList<RestEndpointOverrideActionKind> selectedOverrideActionKinds, IReadOnlyList<RestEndpointOverrideActionKind> appliedOverrideActionKinds)
```

Creates a REST endpoint candidate runtime descriptor.

Parameters:
- `id`: The stable candidate identifier derived from the original projection before any host-level overrides are applied.
- `projectedEndpoint`: The resolved endpoint shape the candidate would publish when it wins precedence.
- `originalProjection`: The original projection shape contributed by the source authoring path before host-level overrides are applied.
- `authoringStyle`: The normalized authoring style such as `behavior-module-dsl`.
- `precedenceRank`: The precedence rank used during publication resolution. Lower values win.
- `status`: The publication status assigned to the candidate.
- `suppressedByCandidateId`: The winning candidate identifier when this candidate was suppressed.
- `suppressedBySuppressionId`: The host-level suppression identifier when this candidate was suppressed by REST governance.
- `appliedOverrideId`: The host-level override identifier when this candidate shape was rewritten by REST governance.
- `matchedSuppressionIds`: The ordered suppression-rule identifiers that matched this candidate before one winner was selected.
- `matchedOverrideIds`: The ordered override-rule identifiers that matched this candidate before one winner was selected.
- `suppressionReason`: The operator-facing suppression reason when one is available.
- `suppressedByAuthoringPolicyKind`: The authoring-policy suppression kind when this candidate was suppressed by behavior-level authoring-policy enforcement.
- `selectedOverrideId`: The selected host-level override identifier when one winning override rule was resolved for this candidate, even if that winning rule became a runtime no-op.
- `suppressionSelectionBasis`: The earliest decisive specificity rule that selected the winning suppression rule when this candidate was suppressed by REST governance.
- `overrideSelectionBasis`: The earliest decisive specificity rule that selected the winning override rule when one was resolved for this candidate.
- `skippedSuppressionIds`: The ordered suppression-rule identifiers that otherwise target this candidate but were skipped because the original projection did not allow host governance to participate.
- `skippedOverrideIds`: The ordered override-rule identifiers that otherwise target this candidate but were skipped because the original projection did not allow host governance to participate.
- `selectedOverrideActionKinds`: The normalized action dimensions declared by the selected override rule when one winning override rule was resolved for this candidate.
- `appliedOverrideActionKinds`: The normalized action dimensions that materially changed the candidate's effective runtime answer when the selected override rule was not a runtime no-op.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-appliedoverrideactionkinds"></a>

##### `AppliedOverrideActionKinds`

```csharp
IReadOnlyList<RestEndpointOverrideActionKind> AppliedOverrideActionKinds { get; }
```

Gets the normalized action dimensions that materially changed the candidate's effective runtime answer when the selected override rule was not a runtime no-op.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-appliedoverrideid"></a>

##### `AppliedOverrideId`

```csharp
string AppliedOverrideId { get; }
```

Gets the host-level override identifier when this candidate shape was rewritten by REST governance.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-authoringstyle"></a>

##### `AuthoringStyle`

```csharp
string AuthoringStyle { get; }
```

Gets the normalized authoring style used to produce the candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable candidate identifier derived from the original projection before any host-level overrides are applied.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-matchedoverrideids"></a>

##### `MatchedOverrideIds`

```csharp
IReadOnlyList<string> MatchedOverrideIds { get; }
```

Gets the ordered override-rule identifiers that matched this candidate before one winner was selected.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-matchedsuppressionids"></a>

##### `MatchedSuppressionIds`

```csharp
IReadOnlyList<string> MatchedSuppressionIds { get; }
```

Gets the ordered suppression-rule identifiers that matched this candidate before one winner was selected.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-originalprojection"></a>

##### `OriginalProjection`

```csharp
RestEndpointCandidateProjectionDescriptor OriginalProjection { get; }
```

Gets the original projection shape contributed by the source authoring path before host-level overrides are applied.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-overrideselectionbasis"></a>

##### `OverrideSelectionBasis`

```csharp
RestEndpointGovernanceRuleSelectionBasis? OverrideSelectionBasis { get; }
```

Gets the earliest decisive specificity rule that selected the winning override rule when one was resolved for this candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-precedencerank"></a>

##### `PrecedenceRank`

```csharp
int PrecedenceRank { get; }
```

Gets the precedence rank used during publication resolution. Lower values win.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-projectedendpoint"></a>

##### `ProjectedEndpoint`

```csharp
RestEndpointRuntimeDescriptor ProjectedEndpoint { get; }
```

Gets the resolved endpoint shape the candidate would publish when it wins precedence.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-selectedoverrideactionkinds"></a>

##### `SelectedOverrideActionKinds`

```csharp
IReadOnlyList<RestEndpointOverrideActionKind> SelectedOverrideActionKinds { get; }
```

Gets the normalized action dimensions declared by the selected override rule when one winning override rule was resolved for this candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-selectedoverrideid"></a>

##### `SelectedOverrideId`

```csharp
string SelectedOverrideId { get; }
```

Gets the selected host-level override identifier when one winning override rule was resolved for this candidate, even if that winning rule became a runtime no-op.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-skippedoverrideids"></a>

##### `SkippedOverrideIds`

```csharp
IReadOnlyList<string> SkippedOverrideIds { get; }
```

Gets the ordered override-rule identifiers that otherwise target this candidate but were skipped because the original projection did not allow host governance.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-skippedsuppressionids"></a>

##### `SkippedSuppressionIds`

```csharp
IReadOnlyList<string> SkippedSuppressionIds { get; }
```

Gets the ordered suppression-rule identifiers that otherwise target this candidate but were skipped because the original projection did not allow host governance.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-status"></a>

##### `Status`

```csharp
RestEndpointCandidateStatus Status { get; }
```

Gets whether the candidate is published or suppressed in the active runtime.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-suppressedbyauthoringpolicykind"></a>

##### `SuppressedByAuthoringPolicyKind`

```csharp
RestEndpointAuthoringPolicySuppressionKind? SuppressedByAuthoringPolicyKind { get; }
```

Gets the authoring-policy suppression kind when this candidate was suppressed by behavior-level authoring-policy enforcement.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-suppressedbycandidateid"></a>

##### `SuppressedByCandidateId`

```csharp
string SuppressedByCandidateId { get; }
```

Gets the winning candidate identifier when this candidate was suppressed.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-suppressedbysuppressionid"></a>

##### `SuppressedBySuppressionId`

```csharp
string SuppressedBySuppressionId { get; }
```

Gets the host-level suppression identifier when this candidate was suppressed by REST governance.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-suppressionreason"></a>

##### `SuppressionReason`

```csharp
string SuppressionReason { get; }
```

Gets the operator-facing suppression reason when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-suppressionselectionbasis"></a>

##### `SuppressionSelectionBasis`

```csharp
RestEndpointGovernanceRuleSelectionBasis? SuppressionSelectionBasis { get; }
```

Gets the earliest decisive specificity rule that selected the winning suppression rule when this candidate was suppressed by REST governance.

<a id="type-cephalon-abstractions-transports-restendpointcandidatestatus"></a>

### `RestEndpointCandidateStatus`

Describes whether a REST endpoint candidate is published or suppressed in the active runtime.

#### Declaration
```csharp
public enum RestEndpointCandidateStatus
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-restendpointcandidatestatus-published"></a>

##### `Published`

```csharp
const RestEndpointCandidateStatus Published
```

The candidate is published into the active public REST surface.

<a id="member-f-cephalon-abstractions-transports-restendpointcandidatestatus-suppressed"></a>

##### `Suppressed`

```csharp
const RestEndpointCandidateStatus Suppressed
```

The candidate was considered but suppressed from the active public REST surface.

<a id="member-f-cephalon-abstractions-transports-restendpointcandidatestatus-unspecified"></a>

##### `Unspecified`

```csharp
const RestEndpointCandidateStatus Unspecified
```

The candidate has not been classified.

<a id="type-cephalon-abstractions-transports-restendpointcandidatestatusextensions"></a>

### `RestEndpointCandidateStatusExtensions`

Provides canonical wire-name helpers for `RestEndpointCandidateStatus`.

#### Declaration
```csharp
public static class RestEndpointCandidateStatusExtensions
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-restendpointcandidatestatusextensions-getwirename-cephalon-abstractions-transports-restendpointcandidatestatus"></a>

##### `GetWireName`

```csharp
string GetWireName(this RestEndpointCandidateStatus status)
```

Gets the stable wire name used by JSON serialization for the candidate status.

Returns: The stable wire name.

Parameters:
- `status`: The candidate status.

<a id="member-m-cephalon-abstractions-transports-restendpointcandidatestatusextensions-tryparsewirename-system-string-cephalon-abstractions-transports-restendpointcandidatestatus"></a>

##### `TryParseWireName`

```csharp
bool TryParseWireName(string value, out RestEndpointCandidateStatus status)
```

Tries to parse the stable wire name used by JSON serialization into a candidate status.

Returns: `true` when the wire name maps to a supported candidate status; otherwise, `false`.

Parameters:
- `value`: The wire name to parse.
- `status`: The parsed candidate status when the wire name is recognized.

<a id="type-cephalon-abstractions-transports-restendpointgovernanceoverrideactionkindsummarydescriptor"></a>

### `RestEndpointGovernanceOverrideActionKindSummaryDescriptor`

Describes one grouped override-action bucket within a rule-centric REST governance summary.

#### Declaration
```csharp
public sealed class RestEndpointGovernanceOverrideActionKindSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointgovernanceoverrideactionkindsummarydescriptor-ctor-cephalon-abstractions-transports-restendpointoverrideactionkind-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointGovernanceOverrideActionKindSummaryDescriptor`

```csharp
RestEndpointGovernanceOverrideActionKindSummaryDescriptor(RestEndpointOverrideActionKind actionKind, IReadOnlyList<string> candidateIds)
```

Creates a grouped override-action bucket for a rule-centric REST governance summary.

Parameters:
- `actionKind`: The override action dimension represented by this grouped bucket.
- `candidateIds`: The ordered candidate identifiers that selected or materially applied this override action dimension.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverrideactionkindsummarydescriptor-actionkind"></a>

##### `ActionKind`

```csharp
RestEndpointOverrideActionKind ActionKind { get; }
```

Gets the override action dimension represented by this grouped bucket.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverrideactionkindsummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers that selected or materially applied this override action dimension.

<a id="type-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor"></a>

### `RestEndpointGovernanceOverrideSummaryDescriptor`

Describes one grouped override-rule outcome within a rule-centric REST governance summary.

#### Declaration
```csharp
public sealed class RestEndpointGovernanceOverrideSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceselectionbasissummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceoverrideactionkindsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceoverrideactionkindsummarydescriptor"></a>

##### `RestEndpointGovernanceOverrideSummaryDescriptor`

```csharp
RestEndpointGovernanceOverrideSummaryDescriptor(string ruleId, IReadOnlyList<string> matchedCandidateIds, IReadOnlyList<string> selectedCandidateIds, IReadOnlyList<string> appliedCandidateIds, IReadOnlyList<RestEndpointGovernanceSelectionBasisSummaryDescriptor> selectionBasisSummaries, IReadOnlyList<RestEndpointGovernanceOverrideActionKindSummaryDescriptor> selectedActionKindSummaries, IReadOnlyList<RestEndpointGovernanceOverrideActionKindSummaryDescriptor> appliedActionKindSummaries)
```

Creates a grouped override-rule summary.

Parameters:
- `ruleId`: The stable host-level override-rule identifier summarized by this entry.
- `matchedCandidateIds`: The ordered candidate identifiers that matched this override rule before one winner was selected.
- `selectedCandidateIds`: The ordered candidate identifiers that selected this override rule, including runtime no-op selections.
- `appliedCandidateIds`: The ordered candidate identifiers whose effective runtime answer was materially changed by this override rule.
- `selectionBasisSummaries`: The grouped decisive selection-basis buckets for the candidates that selected this override rule.
- `selectedActionKindSummaries`: The grouped declared override-action buckets for the candidates that selected this override rule.
- `appliedActionKindSummaries`: The grouped materially applied override-action buckets for the candidates this override rule changed.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-appliedactionkindsummaries"></a>

##### `AppliedActionKindSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceOverrideActionKindSummaryDescriptor> AppliedActionKindSummaries { get; }
```

Gets the grouped materially applied override-action buckets for the candidates this override rule changed.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-appliedcandidateids"></a>

##### `AppliedCandidateIds`

```csharp
IReadOnlyList<string> AppliedCandidateIds { get; }
```

Gets the ordered candidate identifiers whose effective runtime answer was materially changed by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-matchedcandidateids"></a>

##### `MatchedCandidateIds`

```csharp
IReadOnlyList<string> MatchedCandidateIds { get; }
```

Gets the ordered candidate identifiers that matched this override rule before one winner was selected.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the stable host-level override-rule identifier summarized by this entry.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-selectedactionkindsummaries"></a>

##### `SelectedActionKindSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceOverrideActionKindSummaryDescriptor> SelectedActionKindSummaries { get; }
```

Gets the grouped declared override-action buckets for the candidates that selected this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-selectedcandidateids"></a>

##### `SelectedCandidateIds`

```csharp
IReadOnlyList<string> SelectedCandidateIds { get; }
```

Gets the ordered candidate identifiers that selected this override rule, including runtime no-op selections.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceoverridesummarydescriptor-selectionbasissummaries"></a>

##### `SelectionBasisSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSelectionBasisSummaryDescriptor> SelectionBasisSummaries { get; }
```

Gets the grouped decisive selection-basis buckets for the candidates that selected this override rule.

<a id="type-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis"></a>

### `RestEndpointGovernanceRuleSelectionBasis`

Describes the earliest decisive specificity rule that selected one matching REST governance rule over another.

#### Declaration
```csharp
public enum RestEndpointGovernanceRuleSelectionBasis
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-behaviortargeting"></a>

##### `BehaviorTargeting`

```csharp
const RestEndpointGovernanceRuleSelectionBasis BehaviorTargeting
```

A rule that explicitly targeted behaviors won over a broader module-level rule.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-candidatetargeting"></a>

##### `CandidateTargeting`

```csharp
const RestEndpointGovernanceRuleSelectionBasis CandidateTargeting
```

A rule that targeted explicit candidate ids won over a broader rule that did not.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-fewertargetvalues"></a>

##### `FewerTargetValues`

```csharp
const RestEndpointGovernanceRuleSelectionBasis FewerTargetValues
```

A rule with fewer total selector values won over an otherwise equally ranked broader rule.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-moretargetdimensions"></a>

##### `MoreTargetDimensions`

```csharp
const RestEndpointGovernanceRuleSelectionBasis MoreTargetDimensions
```

A rule that constrained more selector dimensions won over a less specific rule.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-narrowerauthoringstylescope"></a>

##### `NarrowerAuthoringStyleScope`

```csharp
const RestEndpointGovernanceRuleSelectionBasis NarrowerAuthoringStyleScope
```

A rule that constrained fewer authoring styles won over a broader authoring-style scope.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-narrowerbehaviorscope"></a>

##### `NarrowerBehaviorScope`

```csharp
const RestEndpointGovernanceRuleSelectionBasis NarrowerBehaviorScope
```

A rule that targeted a narrower behavior-id scope won over a broader behavior-targeted rule.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-narrowercandidateset"></a>

##### `NarrowerCandidateSet`

```csharp
const RestEndpointGovernanceRuleSelectionBasis NarrowerCandidateSet
```

A rule that targeted a smaller candidate-id set won over a broader candidate-targeted rule.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-singlematch"></a>

##### `SingleMatch`

```csharp
const RestEndpointGovernanceRuleSelectionBasis SingleMatch
```

Only one governance rule matched the candidate, so no tie-breaker was required.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-stableruleid"></a>

##### `StableRuleId`

```csharp
const RestEndpointGovernanceRuleSelectionBasis StableRuleId
```

The winning rule was selected by the final stable rule-id tie-breaker.

<a id="member-f-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-unspecified"></a>

##### `Unspecified`

```csharp
const RestEndpointGovernanceRuleSelectionBasis Unspecified
```

The selection basis was not classified.

<a id="type-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasisextensions"></a>

### `RestEndpointGovernanceRuleSelectionBasisExtensions`

Provides canonical wire-name helpers for `RestEndpointGovernanceRuleSelectionBasis`.

#### Declaration
```csharp
public static class RestEndpointGovernanceRuleSelectionBasisExtensions
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasisextensions-getwirename-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis"></a>

##### `GetWireName`

```csharp
string GetWireName(this RestEndpointGovernanceRuleSelectionBasis basis)
```

Gets the stable wire name used by JSON serialization and runtime introspection for the selection basis.

Returns: The stable wire name.

Parameters:
- `basis`: The selection basis.

<a id="member-m-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasisextensions-tryparsewirename-system-string-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis"></a>

##### `TryParseWireName`

```csharp
bool TryParseWireName(string value, out RestEndpointGovernanceRuleSelectionBasis basis)
```

Tries to parse the stable wire name used by JSON serialization and runtime introspection into a selection basis.

Returns: `true` when the wire name maps to a supported selection basis; otherwise, `false`.

Parameters:
- `value`: The wire name to parse.
- `basis`: The parsed selection basis when the wire name is recognized.

<a id="type-cephalon-abstractions-transports-restendpointgovernanceselectionbasissummarydescriptor"></a>

### `RestEndpointGovernanceSelectionBasisSummaryDescriptor`

Describes one grouped selection-basis bucket within a rule-centric REST governance summary.

#### Declaration
```csharp
public sealed class RestEndpointGovernanceSelectionBasisSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointgovernanceselectionbasissummarydescriptor-ctor-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointGovernanceSelectionBasisSummaryDescriptor`

```csharp
RestEndpointGovernanceSelectionBasisSummaryDescriptor(RestEndpointGovernanceRuleSelectionBasis selectionBasis, IReadOnlyList<string> candidateIds)
```

Creates a grouped selection-basis bucket for a rule-centric REST governance summary.

Parameters:
- `selectionBasis`: The decisive specificity basis that selected the winning governance rule for the grouped candidates.
- `candidateIds`: The ordered candidate identifiers that resolved the winning governance rule with this selection basis.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceselectionbasissummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers that resolved the winning governance rule with this selection basis.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceselectionbasissummarydescriptor-selectionbasis"></a>

##### `SelectionBasis`

```csharp
RestEndpointGovernanceRuleSelectionBasis SelectionBasis { get; }
```

Gets the decisive specificity basis that selected the winning governance rule for the grouped candidates.

<a id="type-cephalon-abstractions-transports-restendpointgovernanceskippedoverridesummarydescriptor"></a>

### `RestEndpointGovernanceSkippedOverrideSummaryDescriptor`

Describes one grouped governance-skipped override-rule outcome within a rule-centric REST governance summary.

#### Declaration
```csharp
public sealed class RestEndpointGovernanceSkippedOverrideSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointgovernanceskippedoverridesummarydescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointGovernanceSkippedOverrideSummaryDescriptor`

```csharp
RestEndpointGovernanceSkippedOverrideSummaryDescriptor(string ruleId, IReadOnlyList<string> candidateIds)
```

Creates a grouped governance-skipped override-rule summary.

Parameters:
- `ruleId`: The stable host-level override-rule identifier summarized by this entry.
- `candidateIds`: The ordered candidate identifiers that this override rule targeted before host governance was skipped.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceskippedoverridesummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers that this override rule targeted before host governance was skipped.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceskippedoverridesummarydescriptor-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the stable host-level override-rule identifier summarized by this entry.

<a id="type-cephalon-abstractions-transports-restendpointgovernanceskippedsuppressionsummarydescriptor"></a>

### `RestEndpointGovernanceSkippedSuppressionSummaryDescriptor`

Describes one grouped governance-skipped suppression-rule outcome within a rule-centric REST governance summary.

#### Declaration
```csharp
public sealed class RestEndpointGovernanceSkippedSuppressionSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointgovernanceskippedsuppressionsummarydescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointGovernanceSkippedSuppressionSummaryDescriptor`

```csharp
RestEndpointGovernanceSkippedSuppressionSummaryDescriptor(string ruleId, IReadOnlyList<string> candidateIds)
```

Creates a grouped governance-skipped suppression-rule summary.

Parameters:
- `ruleId`: The stable host-level suppression-rule identifier summarized by this entry.
- `candidateIds`: The ordered candidate identifiers that this suppression rule targeted before host governance was skipped.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceskippedsuppressionsummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers that this suppression rule targeted before host governance was skipped.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernanceskippedsuppressionsummarydescriptor-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the stable host-level suppression-rule identifier summarized by this entry.

<a id="type-cephalon-abstractions-transports-restendpointgovernancesuppressionsummarydescriptor"></a>

### `RestEndpointGovernanceSuppressionSummaryDescriptor`

Describes one grouped suppression-rule outcome within a rule-centric REST governance summary.

#### Declaration
```csharp
public sealed class RestEndpointGovernanceSuppressionSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointgovernancesuppressionsummarydescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceselectionbasissummarydescriptor"></a>

##### `RestEndpointGovernanceSuppressionSummaryDescriptor`

```csharp
RestEndpointGovernanceSuppressionSummaryDescriptor(string ruleId, IReadOnlyList<string> matchedCandidateIds, IReadOnlyList<string> suppressedCandidateIds, IReadOnlyList<RestEndpointGovernanceSelectionBasisSummaryDescriptor> selectionBasisSummaries)
```

Creates a grouped suppression-rule summary.

Parameters:
- `ruleId`: The stable host-level suppression-rule identifier summarized by this entry.
- `matchedCandidateIds`: The ordered candidate identifiers that matched this suppression rule before one winner was selected.
- `suppressedCandidateIds`: The ordered candidate identifiers that this suppression rule ultimately suppressed.
- `selectionBasisSummaries`: The grouped decisive selection-basis buckets for the candidates this suppression rule ultimately suppressed.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointgovernancesuppressionsummarydescriptor-matchedcandidateids"></a>

##### `MatchedCandidateIds`

```csharp
IReadOnlyList<string> MatchedCandidateIds { get; }
```

Gets the ordered candidate identifiers that matched this suppression rule before one winner was selected.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernancesuppressionsummarydescriptor-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the stable host-level suppression-rule identifier summarized by this entry.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernancesuppressionsummarydescriptor-selectionbasissummaries"></a>

##### `SelectionBasisSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSelectionBasisSummaryDescriptor> SelectionBasisSummaries { get; }
```

Gets the grouped decisive selection-basis buckets for the candidates this suppression rule ultimately suppressed.

<a id="member-p-cephalon-abstractions-transports-restendpointgovernancesuppressionsummarydescriptor-suppressedcandidateids"></a>

##### `SuppressedCandidateIds`

```csharp
IReadOnlyList<string> SuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that this suppression rule ultimately suppressed.

<a id="type-cephalon-abstractions-transports-restendpointoverrideactionkind"></a>

### `RestEndpointOverrideActionKind`

Describes one configured or materially applied REST override action dimension.

#### Declaration
```csharp
public enum RestEndpointOverrideActionKind
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-apiversionmajor"></a>

##### `ApiVersionMajor`

```csharp
const RestEndpointOverrideActionKind ApiVersionMajor
```

The rule changes the effective public API major version.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-clearbindings"></a>

##### `ClearBindings`

```csharp
const RestEndpointOverrideActionKind ClearBindings
```

The rule clears the explicit request-binding plan and returns to the implicit baseline.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-cleardescription"></a>

##### `ClearDescription`

```csharp
const RestEndpointOverrideActionKind ClearDescription
```

The rule clears any previously declared endpoint description.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-clearendpointname"></a>

##### `ClearEndpointName`

```csharp
const RestEndpointOverrideActionKind ClearEndpointName
```

The rule clears any previously declared endpoint name.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-clearrequiredcapability"></a>

##### `ClearRequiredCapability`

```csharp
const RestEndpointOverrideActionKind ClearRequiredCapability
```

The rule clears any previously declared required Cephalon capability key.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-clearrequiredfeatureflags"></a>

##### `ClearRequiredFeatureFlags`

```csharp
const RestEndpointOverrideActionKind ClearRequiredFeatureFlags
```

The rule clears any previously declared required Cephalon feature-flag identifiers.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-clearsummary"></a>

##### `ClearSummary`

```csharp
const RestEndpointOverrideActionKind ClearSummary
```

The rule clears any previously declared endpoint summary.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-description"></a>

##### `Description`

```csharp
const RestEndpointOverrideActionKind Description
```

The rule changes the effective endpoint description.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-endpointname"></a>

##### `EndpointName`

```csharp
const RestEndpointOverrideActionKind EndpointName
```

The rule changes the effective endpoint name.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-mergebindings"></a>

##### `MergeBindings`

```csharp
const RestEndpointOverrideActionKind MergeBindings
```

The rule merges changes into the explicit request-binding plan.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-method"></a>

##### `Method`

```csharp
const RestEndpointOverrideActionKind Method
```

The rule changes the effective HTTP method.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-openapidocumentname"></a>

##### `OpenApiDocumentName`

```csharp
const RestEndpointOverrideActionKind OpenApiDocumentName
```

The rule changes the effective OpenAPI document name.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-pattern"></a>

##### `Pattern`

```csharp
const RestEndpointOverrideActionKind Pattern
```

The rule changes the effective relative route pattern.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-preserveimplicitqueryfallback"></a>

##### `PreserveImplicitQueryFallback`

```csharp
const RestEndpointOverrideActionKind PreserveImplicitQueryFallback
```

The rule opts the matched explicit-binding shorthand candidate into preserved implicit-query fallback.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-removebindingproperties"></a>

##### `RemoveBindingProperties`

```csharp
const RestEndpointOverrideActionKind RemoveBindingProperties
```

The rule removes explicit request-binding properties from the source plan.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-replacebindings"></a>

##### `ReplaceBindings`

```csharp
const RestEndpointOverrideActionKind ReplaceBindings
```

The rule replaces the explicit request-binding plan.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-requiredcapabilitykey"></a>

##### `RequiredCapabilityKey`

```csharp
const RestEndpointOverrideActionKind RequiredCapabilityKey
```

The rule changes the required Cephalon capability key.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-requiredfeatureflagids"></a>

##### `RequiredFeatureFlagIds`

```csharp
const RestEndpointOverrideActionKind RequiredFeatureFlagIds
```

The rule changes the required Cephalon feature-flag identifiers.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-routegroupprefix"></a>

##### `RouteGroupPrefix`

```csharp
const RestEndpointOverrideActionKind RouteGroupPrefix
```

The rule changes the effective published route-group prefix.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-summary"></a>

##### `Summary`

```csharp
const RestEndpointOverrideActionKind Summary
```

The rule changes the effective endpoint summary.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-tagname"></a>

##### `TagName`

```csharp
const RestEndpointOverrideActionKind TagName
```

The rule changes the effective primary OpenAPI tag name.

<a id="member-f-cephalon-abstractions-transports-restendpointoverrideactionkind-unspecified"></a>

##### `Unspecified`

```csharp
const RestEndpointOverrideActionKind Unspecified
```

The action kind was not classified.

<a id="type-cephalon-abstractions-transports-restendpointoverrideactionkindextensions"></a>

### `RestEndpointOverrideActionKindExtensions`

Provides canonical wire-name helpers for `RestEndpointOverrideActionKind`.

#### Declaration
```csharp
public static class RestEndpointOverrideActionKindExtensions
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-restendpointoverrideactionkindextensions-getwirename-cephalon-abstractions-transports-restendpointoverrideactionkind"></a>

##### `GetWireName`

```csharp
string GetWireName(this RestEndpointOverrideActionKind actionKind)
```

Gets the stable wire name used by JSON serialization for the override action kind.

Returns: The stable wire name.

Parameters:
- `actionKind`: The override action kind.

<a id="member-m-cephalon-abstractions-transports-restendpointoverrideactionkindextensions-tryparsewirename-system-string-cephalon-abstractions-transports-restendpointoverrideactionkind"></a>

##### `TryParseWireName`

```csharp
bool TryParseWireName(string value, out RestEndpointOverrideActionKind actionKind)
```

Tries to parse the stable wire name used by JSON serialization into an override action kind.

Returns: `true` when the wire name maps to a supported override action kind; otherwise, `false`.

Parameters:
- `value`: The wire name to parse.
- `actionKind`: The parsed override action kind when the wire name is recognized.

<a id="type-cephalon-abstractions-transports-restendpointoverridebindingmode"></a>

### `RestEndpointOverrideBindingMode`

Describes how a REST endpoint override rule applies its explicit binding descriptors.

#### Declaration
```csharp
public enum RestEndpointOverrideBindingMode
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-restendpointoverridebindingmode-mergeexplicit"></a>

##### `MergeExplicit`

```csharp
const RestEndpointOverrideBindingMode MergeExplicit
```

Merges configured binding descriptors into the candidate's explicit binding plan by property name and can also remove selected explicit bindings.

<a id="member-f-cephalon-abstractions-transports-restendpointoverridebindingmode-replaceexplicit"></a>

##### `ReplaceExplicit`

```csharp
const RestEndpointOverrideBindingMode ReplaceExplicit
```

Replaces the candidate's explicit binding plan with the configured descriptors.

<a id="member-f-cephalon-abstractions-transports-restendpointoverridebindingmode-unspecified"></a>

##### `Unspecified`

```csharp
const RestEndpointOverrideBindingMode Unspecified
```

No explicit binding-override mode has been selected.

<a id="type-cephalon-abstractions-transports-restendpointoverridebindingmodeextensions"></a>

### `RestEndpointOverrideBindingModeExtensions`

Provides canonical wire-name helpers for `RestEndpointOverrideBindingMode`.

#### Declaration
```csharp
public static class RestEndpointOverrideBindingModeExtensions
```

#### Methods

<a id="member-m-cephalon-abstractions-transports-restendpointoverridebindingmodeextensions-getwirename-cephalon-abstractions-transports-restendpointoverridebindingmode"></a>

##### `GetWireName`

```csharp
string GetWireName(this RestEndpointOverrideBindingMode bindingMode)
```

Gets the stable wire name used by JSON serialization and compatibility metadata for the override binding mode.

Returns: The stable wire name.

Parameters:
- `bindingMode`: The override binding mode.

<a id="member-m-cephalon-abstractions-transports-restendpointoverridebindingmodeextensions-tryparsewirename-system-string-cephalon-abstractions-transports-restendpointoverridebindingmode"></a>

##### `TryParseWireName`

```csharp
bool TryParseWireName(string value, out RestEndpointOverrideBindingMode bindingMode)
```

Tries to parse the stable wire name used by JSON serialization and compatibility metadata into an override binding mode.

Returns: `true` when the wire name maps to a supported override binding mode; otherwise, `false`.

Parameters:
- `value`: The wire name to parse.
- `bindingMode`: The parsed override binding mode when the wire name is recognized.

<a id="type-cephalon-abstractions-transports-restendpointoverridedescriptor"></a>

### `RestEndpointOverrideDescriptor`

Describes one host-level REST endpoint override rule visible to the current runtime for module-owned REST candidates that participate in host governance.

#### Declaration
```csharp
public sealed class RestEndpointOverrideDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointoverridedescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-nullable-system-int32-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-collections-generic-ireadonlylist-system-string-system-boolean-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingdescriptor-system-collections-generic-ireadonlylist-system-string-system-boolean-cephalon-abstractions-transports-restendpointoverridebindingmode-system-boolean-system-boolean-system-boolean-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingfallbackmode-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingdescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointoverrideactionkind-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointoverrideactionkind-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceselectionbasissummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceoverrideactionkindsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceoverrideactionkindsummarydescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-boolean"></a>

##### `RestEndpointOverrideDescriptor`

```csharp
RestEndpointOverrideDescriptor(string id, IReadOnlyList<string> candidateIds, IReadOnlyList<string> behaviorIds, IReadOnlyList<string> sourceModuleIds, IReadOnlyList<string> authoringStyles, IReadOnlyList<int> apiVersionMajors, IReadOnlyList<string> methods, IReadOnlyList<string> relativePatterns, IReadOnlyList<string> routeGroupPrefixes, int? apiVersionMajor, string method, string pattern, string routeGroupPrefix, string openApiDocumentName, string tagName, string endpointName, string summary, string description, string requiredCapabilityKey, bool clearRequiredCapability, IReadOnlyList<string> requiredFeatureFlagIds, bool clearRequiredFeatureFlags, IReadOnlyList<RestEndpointBindingDescriptor> bindings, IReadOnlyList<string> removedBindingProperties, bool clearBindings, RestEndpointOverrideBindingMode bindingMode, bool clearEndpointName, bool clearSummary, bool clearDescription, IReadOnlyList<string> openApiDocumentNames, IReadOnlyList<string> tagNames, IReadOnlyList<string> endpointNames, IReadOnlyList<RestEndpointBindingFallbackMode> bindingFallbackModes, IReadOnlyList<RestEndpointBindingDescriptor> targetBindings, IReadOnlyList<string> matchedCandidateIds, IReadOnlyList<string> selectedCandidateIds, IReadOnlyList<string> appliedCandidateIds, IReadOnlyList<string> skippedCandidateIds, IReadOnlyList<RestEndpointGovernanceRuleSelectionBasis> selectionBases, IReadOnlyList<RestEndpointOverrideActionKind> selectedActionKinds, IReadOnlyList<RestEndpointOverrideActionKind> appliedActionKinds, IReadOnlyList<RestEndpointGovernanceSelectionBasisSummaryDescriptor> selectionBasisSummaries, IReadOnlyList<RestEndpointGovernanceOverrideActionKindSummaryDescriptor> selectedActionKindSummaries, IReadOnlyList<RestEndpointGovernanceOverrideActionKindSummaryDescriptor> appliedActionKindSummaries, IReadOnlyList<string> hostGovernanceScopes, IReadOnlyList<string> behaviorIdPrefixes, bool preserveImplicitQueryFallback)
```

Creates a REST endpoint override descriptor.

Parameters:
- `id`: The stable override identifier.
- `candidateIds`: The original candidate identifiers targeted by the override rule.
- `behaviorIds`: The behavior identifiers targeted by the override rule.
- `sourceModuleIds`: The source-module identifiers targeted by the override rule.
- `authoringStyles`: The normalized authoring styles targeted by the override rule. Explicit module-DSL routes participate only when their owning route group opted into host governance.
- `apiVersionMajors`: The effective API major versions targeted by the override rule.
- `methods`: The effective HTTP methods targeted by the override rule.
- `relativePatterns`: The shorthand relative route patterns targeted by the override rule.
- `routeGroupPrefixes`: The published route-group prefixes targeted by the override rule.
- `apiVersionMajor`: The effective API major version applied when the rule matches.
- `method`: The effective HTTP method applied when the rule matches.
- `pattern`: The effective relative route pattern applied when the rule matches.
- `routeGroupPrefix`: The effective published route-group prefix applied when the rule matches.
- `openApiDocumentName`: The effective OpenAPI document name applied when the rule matches.
- `tagName`: The effective primary OpenAPI tag name applied when the rule matches.
- `endpointName`: The effective endpoint name applied when the rule matches.
- `summary`: The effective OpenAPI summary applied when the rule matches.
- `description`: The effective OpenAPI description applied when the rule matches.
- `requiredCapabilityKey`: The required Cephalon capability key enforced at the REST boundary when the rule matches.
- `clearRequiredCapability`: `true` when the rule removes any previously declared Cephalon capability boundary from the matched candidate.
- `requiredFeatureFlagIds`: The required Cephalon feature-flag identifiers enforced at the REST boundary when the rule matches.
- `clearRequiredFeatureFlags`: `true` when the rule removes any previously declared Cephalon feature-flag requirements from the matched candidate.
- `bindings`: The effective explicit request-binding plan applied when the rule matches.
- `removedBindingProperties`: The explicit binding properties removed from the source binding plan when the rule matches.
- `clearBindings`: `true` when the rule removes the matched candidate's entire explicit binding plan and returns publication to the implicit request-binding baseline.
- `bindingMode`: The mode used to apply `bindings` and `removedBindingProperties` to the candidate's explicit binding plan.
- `clearEndpointName`: `true` when the rule removes any previously declared endpoint name from the matched candidate.
- `clearSummary`: `true` when the rule removes any previously declared endpoint summary from the matched candidate.
- `clearDescription`: `true` when the rule removes any previously declared endpoint description from the matched candidate.
- `openApiDocumentNames`: The original candidate OpenAPI document names targeted by the override rule before override actions are applied.
- `tagNames`: The original candidate primary OpenAPI tag names targeted by the override rule before override actions are applied.
- `endpointNames`: The original candidate endpoint names targeted by the override rule before override actions are applied.
- `hostGovernanceScopes`: The original candidate host-governance scopes targeted by the override rule before override actions are applied.
- `bindingFallbackModes`: The original candidate request-binding fallback modes targeted by the override rule before override actions are applied.
- `targetBindings`: The original candidate explicit binding descriptors targeted by the override rule before override actions are applied.
- `matchedCandidateIds`: The runtime candidate identifiers that matched this override rule, including candidates where another override rule won selection.
- `selectedCandidateIds`: The runtime candidate identifiers that selected this override rule as the winning rule, including runtime no-op selections.
- `appliedCandidateIds`: The runtime candidate identifiers whose effective answer was materially changed by this override rule.
- `skippedCandidateIds`: The runtime candidate identifiers that this rule would otherwise target but skipped because the original projection did not allow host governance to participate.
- `selectionBases`: The union of decisive specificity rules that selected this override rule for one or more runtime candidates.
- `selectedActionKinds`: The union of configured override action dimensions that were selected for one or more runtime candidates, including runtime no-op selections.
- `appliedActionKinds`: The union of override action dimensions that materially changed one or more runtime candidates.
- `selectionBasisSummaries`: The grouped selection-basis buckets for runtime candidates that selected this override rule, including runtime no-op selections.
- `selectedActionKindSummaries`: The grouped override-action buckets for runtime candidates that selected this override rule, including runtime no-op selections.
- `appliedActionKindSummaries`: The grouped override-action buckets for runtime candidates materially changed by this override rule.
- `behaviorIdPrefixes`: The behavior-id prefixes targeted by the override rule. Prefix matches use the stable dot-separated behavior-id hierarchy, so a prefix targets the exact behavior id and any descendant behavior ids beneath that prefix.
- `preserveImplicitQueryFallback`: `true` when the rule opts the matched explicit-binding shorthand candidate into preserved implicit-query fallback for any remaining unbound query properties.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-actionkinds"></a>

##### `ActionKinds`

```csharp
IReadOnlyList<RestEndpointOverrideActionKind> ActionKinds { get; }
```

Gets the normalized action dimensions declared by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-apiversionmajor"></a>

##### `ApiVersionMajor`

```csharp
int? ApiVersionMajor { get; }
```

Gets the effective API major version applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-apiversionmajors"></a>

##### `ApiVersionMajors`

```csharp
IReadOnlyList<int> ApiVersionMajors { get; }
```

Gets the effective API major versions targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-appliedactionkinds"></a>

##### `AppliedActionKinds`

```csharp
IReadOnlyList<RestEndpointOverrideActionKind> AppliedActionKinds { get; }
```

Gets the union of override action dimensions that materially changed one or more runtime candidates.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-appliedactionkindsummaries"></a>

##### `AppliedActionKindSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceOverrideActionKindSummaryDescriptor> AppliedActionKindSummaries { get; }
```

Gets the grouped override-action buckets for runtime candidates materially changed by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-appliedcandidateids"></a>

##### `AppliedCandidateIds`

```csharp
IReadOnlyList<string> AppliedCandidateIds { get; }
```

Gets the runtime candidate identifiers whose effective answer was materially changed by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-authoringstyles"></a>

##### `AuthoringStyles`

```csharp
IReadOnlyList<string> AuthoringStyles { get; }
```

Gets the normalized authoring styles targeted by this override rule. Explicit module-DSL routes participate only when their owning route group opted into host governance.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-behavioridprefixes"></a>

##### `BehaviorIdPrefixes`

```csharp
IReadOnlyList<string> BehaviorIdPrefixes { get; }
```

Gets the behavior-id prefixes targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-behaviorids"></a>

##### `BehaviorIds`

```csharp
IReadOnlyList<string> BehaviorIds { get; }
```

Gets the behavior identifiers targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-bindingfallbackmodes"></a>

##### `BindingFallbackModes`

```csharp
IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }
```

Gets the original candidate request-binding fallback modes targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-bindingmode"></a>

##### `BindingMode`

```csharp
RestEndpointOverrideBindingMode BindingMode { get; }
```

Gets how `Bindings` and `RemovedBindingProperties` apply to the candidate's explicit binding plan.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-bindings"></a>

##### `Bindings`

```csharp
IReadOnlyList<RestEndpointBindingDescriptor> Bindings { get; }
```

Gets the effective explicit request-binding plan applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the original candidate identifiers targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-clearbindings"></a>

##### `ClearBindings`

```csharp
bool ClearBindings { get; }
```

Gets a value indicating whether this override rule clears the matched candidate's entire explicit binding plan.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-cleardescription"></a>

##### `ClearDescription`

```csharp
bool ClearDescription { get; }
```

Gets a value indicating whether this override rule clears any previously declared endpoint description from the matched candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-clearendpointname"></a>

##### `ClearEndpointName`

```csharp
bool ClearEndpointName { get; }
```

Gets a value indicating whether this override rule clears any previously declared endpoint name from the matched candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-clearrequiredcapability"></a>

##### `ClearRequiredCapability`

```csharp
bool ClearRequiredCapability { get; }
```

Gets a value indicating whether this override rule clears any previously declared Cephalon capability boundary from the matched candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-clearrequiredfeatureflags"></a>

##### `ClearRequiredFeatureFlags`

```csharp
bool ClearRequiredFeatureFlags { get; }
```

Gets a value indicating whether this override rule clears any previously declared Cephalon feature-flag requirements from the matched candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-clearsummary"></a>

##### `ClearSummary`

```csharp
bool ClearSummary { get; }
```

Gets a value indicating whether this override rule clears any previously declared endpoint summary from the matched candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the effective OpenAPI description applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-endpointname"></a>

##### `EndpointName`

```csharp
string EndpointName { get; }
```

Gets the effective endpoint name applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-endpointnames"></a>

##### `EndpointNames`

```csharp
IReadOnlyList<string> EndpointNames { get; }
```

Gets the original candidate endpoint names targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-hostgovernancescopes"></a>

##### `HostGovernanceScopes`

```csharp
IReadOnlyList<string> HostGovernanceScopes { get; }
```

Gets the original candidate host-governance scopes targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable override identifier.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-matchedcandidateids"></a>

##### `MatchedCandidateIds`

```csharp
IReadOnlyList<string> MatchedCandidateIds { get; }
```

Gets the runtime candidate identifiers that matched this override rule, including candidates where another override rule won selection.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-method"></a>

##### `Method`

```csharp
string Method { get; }
```

Gets the effective HTTP method applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-methods"></a>

##### `Methods`

```csharp
IReadOnlyList<string> Methods { get; }
```

Gets the effective HTTP methods targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-openapidocumentname"></a>

##### `OpenApiDocumentName`

```csharp
string OpenApiDocumentName { get; }
```

Gets the effective OpenAPI document name applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-openapidocumentnames"></a>

##### `OpenApiDocumentNames`

```csharp
IReadOnlyList<string> OpenApiDocumentNames { get; }
```

Gets the original candidate OpenAPI document names targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-pattern"></a>

##### `Pattern`

```csharp
string Pattern { get; }
```

Gets the effective relative route pattern applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-preserveimplicitqueryfallback"></a>

##### `PreserveImplicitQueryFallback`

```csharp
bool PreserveImplicitQueryFallback { get; }
```

Gets a value indicating whether this override rule opts the matched explicit-binding shorthand candidate into preserved implicit-query fallback for remaining unbound query properties.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-relativepatterns"></a>

##### `RelativePatterns`

```csharp
IReadOnlyList<string> RelativePatterns { get; }
```

Gets the relative route patterns targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-removedbindingproperties"></a>

##### `RemovedBindingProperties`

```csharp
IReadOnlyList<string> RemovedBindingProperties { get; }
```

Gets the explicit binding properties removed from the source binding plan when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-requiredcapabilitykey"></a>

##### `RequiredCapabilityKey`

```csharp
string RequiredCapabilityKey { get; }
```

Gets the required Cephalon capability key enforced at the REST boundary when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-requiredfeatureflagids"></a>

##### `RequiredFeatureFlagIds`

```csharp
IReadOnlyList<string> RequiredFeatureFlagIds { get; }
```

Gets the required Cephalon feature-flag identifiers enforced at the REST boundary when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-routegroupprefix"></a>

##### `RouteGroupPrefix`

```csharp
string RouteGroupPrefix { get; }
```

Gets the effective published route-group prefix applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-routegroupprefixes"></a>

##### `RouteGroupPrefixes`

```csharp
IReadOnlyList<string> RouteGroupPrefixes { get; }
```

Gets the published route-group prefixes targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-selectedactionkinds"></a>

##### `SelectedActionKinds`

```csharp
IReadOnlyList<RestEndpointOverrideActionKind> SelectedActionKinds { get; }
```

Gets the union of configured override action dimensions that were selected for one or more runtime candidates, including runtime no-op selections.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-selectedactionkindsummaries"></a>

##### `SelectedActionKindSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceOverrideActionKindSummaryDescriptor> SelectedActionKindSummaries { get; }
```

Gets the grouped override-action buckets for runtime candidates that selected this override rule, including runtime no-op selections.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-selectedcandidateids"></a>

##### `SelectedCandidateIds`

```csharp
IReadOnlyList<string> SelectedCandidateIds { get; }
```

Gets the runtime candidate identifiers that selected this override rule as the winning rule, including runtime no-op selections.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-selectionbases"></a>

##### `SelectionBases`

```csharp
IReadOnlyList<RestEndpointGovernanceRuleSelectionBasis> SelectionBases { get; }
```

Gets the union of decisive specificity rules that selected this override rule for one or more runtime candidates.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-selectionbasissummaries"></a>

##### `SelectionBasisSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSelectionBasisSummaryDescriptor> SelectionBasisSummaries { get; }
```

Gets the grouped selection-basis buckets for runtime candidates that selected this override rule, including runtime no-op selections.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-skippedcandidateids"></a>

##### `SkippedCandidateIds`

```csharp
IReadOnlyList<string> SkippedCandidateIds { get; }
```

Gets the runtime candidate identifiers that this rule would otherwise target but skipped because the original projection did not allow host governance to participate.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-sourcemoduleids"></a>

##### `SourceModuleIds`

```csharp
IReadOnlyList<string> SourceModuleIds { get; }
```

Gets the source-module identifiers targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-summary"></a>

##### `Summary`

```csharp
string Summary { get; }
```

Gets the effective OpenAPI summary applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-tagname"></a>

##### `TagName`

```csharp
string TagName { get; }
```

Gets the effective primary OpenAPI tag name applied when this override rule matches.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-tagnames"></a>

##### `TagNames`

```csharp
IReadOnlyList<string> TagNames { get; }
```

Gets the original candidate primary OpenAPI tag names targeted by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointoverridedescriptor-targetbindings"></a>

##### `TargetBindings`

```csharp
IReadOnlyList<RestEndpointBindingDescriptor> TargetBindings { get; }
```

Gets the original candidate explicit binding descriptors targeted by this override rule before override actions are applied.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor"></a>

### `RestEndpointPublicationGroupAuthoringPolicyDescriptor`

Describes the effective authoring-policy intent for one behavior-level REST publication group.

Remarks: This descriptor captures authoring-policy intent, not the already-resolved publication outcome. The grouped publication answer remains authoritative for which candidates actually published or were suppressed at runtime.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupAuthoringPolicyDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-ctor-system-string-system-boolean-system-boolean-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointPublicationGroupAuthoringPolicyDescriptor`

```csharp
RestEndpointPublicationGroupAuthoringPolicyDescriptor(string behaviorId, bool isConfigured, bool allowMultiplePublishedCandidates, string preferredAuthoringStyle, IReadOnlyList<string> allowedAuthoringStyles, IReadOnlyList<string> disallowedAuthoringStyles)
```

Creates a behavior-level REST publication-group authoring policy descriptor.

Parameters:
- `behaviorId`: The stable behavior identifier that this authoring policy applies to.
- `isConfigured`: `true` when the policy was supplied through host configuration; otherwise `false` when the runtime is exposing the implicit default policy.
- `allowMultiplePublishedCandidates`: `true` when the policy explicitly allows more than one projection candidate to remain published for the same behavior boundary after authoring-policy enforcement.
- `preferredAuthoringStyle`: The normalized preferred authoring style when the policy declares one.
- `allowedAuthoringStyles`: The normalized authoring styles that the policy explicitly allows for this behavior boundary when one or more are declared.
- `disallowedAuthoringStyles`: The normalized authoring styles that the policy explicitly disallows for this behavior boundary when one or more are declared.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-allowedauthoringstyles"></a>

##### `AllowedAuthoringStyles`

```csharp
IReadOnlyList<string> AllowedAuthoringStyles { get; }
```

Gets the normalized authoring styles that the policy explicitly allows.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-allowmultiplepublishedcandidates"></a>

##### `AllowMultiplePublishedCandidates`

```csharp
bool AllowMultiplePublishedCandidates { get; }
```

Gets a value indicating whether the policy explicitly allows multiple published candidates for the same behavior boundary after authoring-policy enforcement.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the stable behavior identifier that this authoring policy applies to.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-disallowedauthoringstyles"></a>

##### `DisallowedAuthoringStyles`

```csharp
IReadOnlyList<string> DisallowedAuthoringStyles { get; }
```

Gets the normalized authoring styles that the policy explicitly disallows.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-isconfigured"></a>

##### `IsConfigured`

```csharp
bool IsConfigured { get; }
```

Gets a value indicating whether this authoring policy came from explicit host configuration.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-preferredauthoringstyle"></a>

##### `PreferredAuthoringStyle`

```csharp
string PreferredAuthoringStyle { get; }
```

Gets the normalized preferred authoring style when the policy declares one.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicysuppressiondescriptor"></a>

### `RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor`

Describes one grouped authoring-policy suppression outcome within a REST publication-group answer.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicysuppressiondescriptor-ctor-cephalon-abstractions-transports-restendpointauthoringpolicysuppressionkind-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor`

```csharp
RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor(RestEndpointAuthoringPolicySuppressionKind kind, IReadOnlyList<string> candidateIds)
```

Creates a grouped authoring-policy suppression descriptor.

Parameters:
- `kind`: The authoring-policy suppression kind summarized by this entry.
- `candidateIds`: The ordered candidate identifiers suppressed by this suppression kind.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicysuppressiondescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers suppressed by this suppression kind.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicysuppressiondescriptor-kind"></a>

##### `Kind`

```csharp
RestEndpointAuthoringPolicySuppressionKind Kind { get; }
```

Gets the authoring-policy suppression kind summarized by this entry.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor"></a>

### `RestEndpointPublicationGroupAuthoringStyleDescriptor`

Describes the grouped publication outcome for one authoring style within a behavior-level REST publication group.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupAuthoringStyleDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicysuppressiondescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor"></a>

##### `RestEndpointPublicationGroupAuthoringStyleDescriptor`

```csharp
RestEndpointPublicationGroupAuthoringStyleDescriptor(string authoringStyle, IReadOnlyList<string> sourceModuleIds, IReadOnlyList<int> precedenceRanks, IReadOnlyList<string> candidateIds, IReadOnlyList<string> publishedCandidateIds, IReadOnlyList<string> precedenceSuppressedCandidateIds, IReadOnlyList<string> governanceSuppressedCandidateIds, IReadOnlyList<string> authoringPolicySuppressedCandidateIds, IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor> authoringPolicySuppressionSummaries, IReadOnlyList<string> hostGovernanceEligibleCandidateIds, IReadOnlyList<string> hostGovernanceIneligibleCandidateIds, IReadOnlyList<string> skippedSuppressionIds, IReadOnlyList<string> skippedOverrideIds, IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> governanceSuppressionSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor> governanceOverrideSummaries)
```

Creates a grouped publication descriptor for one authoring style.

Parameters:
- `authoringStyle`: The normalized authoring style that contributed the grouped candidates.
- `sourceModuleIds`: The distinct source-module identifiers that contributed candidates for this authoring style.
- `precedenceRanks`: The distinct precedence ranks visible for this authoring style within the group.
- `candidateIds`: The ordered candidate identifiers contributed by this authoring style.
- `publishedCandidateIds`: The ordered candidate identifiers that remain published for this authoring style.
- `precedenceSuppressedCandidateIds`: The ordered candidate identifiers that were suppressed by another candidate through precedence resolution.
- `governanceSuppressedCandidateIds`: The ordered candidate identifiers that were suppressed by host-level REST governance.
- `authoringPolicySuppressedCandidateIds`: The ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
- `authoringPolicySuppressionSummaries`: The grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.
- `hostGovernanceEligibleCandidateIds`: The ordered candidate identifiers whose original projections allowed host governance to participate.
- `hostGovernanceIneligibleCandidateIds`: The ordered candidate identifiers whose original projections kept host governance out of scope.
- `skippedSuppressionIds`: The ordered suppression-rule identifiers that targeted ineligible candidates for this authoring style.
- `skippedOverrideIds`: The ordered override-rule identifiers that targeted ineligible candidates for this authoring style.
- `governanceSuppressionSummaries`: The grouped host-governance suppression-rule outcomes summarized by rule for this authoring style.
- `governanceOverrideSummaries`: The grouped host-governance override-rule outcomes summarized by rule for this authoring style.

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicysuppressiondescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedsuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedoverridesummarydescriptor"></a>

##### `RestEndpointPublicationGroupAuthoringStyleDescriptor`

```csharp
RestEndpointPublicationGroupAuthoringStyleDescriptor(string authoringStyle, IReadOnlyList<string> sourceModuleIds, IReadOnlyList<int> precedenceRanks, IReadOnlyList<string> candidateIds, IReadOnlyList<string> publishedCandidateIds, IReadOnlyList<string> precedenceSuppressedCandidateIds, IReadOnlyList<string> governanceSuppressedCandidateIds, IReadOnlyList<string> authoringPolicySuppressedCandidateIds, IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor> authoringPolicySuppressionSummaries, IReadOnlyList<string> hostGovernanceEligibleCandidateIds, IReadOnlyList<string> hostGovernanceIneligibleCandidateIds, IReadOnlyList<string> skippedSuppressionIds, IReadOnlyList<string> skippedOverrideIds, IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> governanceSuppressionSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor> governanceOverrideSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor> skippedSuppressionSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor> skippedOverrideSummaries)
```

Creates a grouped publication descriptor for one authoring style, including grouped skipped-governance summaries.

Parameters:
- `authoringStyle`: The normalized authoring style that contributed the grouped candidates.
- `sourceModuleIds`: The distinct source-module identifiers that contributed candidates for this authoring style.
- `precedenceRanks`: The distinct precedence ranks visible for this authoring style within the group.
- `candidateIds`: The ordered candidate identifiers contributed by this authoring style.
- `publishedCandidateIds`: The ordered candidate identifiers that remain published for this authoring style.
- `precedenceSuppressedCandidateIds`: The ordered candidate identifiers that were suppressed by another candidate through precedence resolution.
- `governanceSuppressedCandidateIds`: The ordered candidate identifiers that were suppressed by host-level REST governance.
- `authoringPolicySuppressedCandidateIds`: The ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
- `authoringPolicySuppressionSummaries`: The grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.
- `hostGovernanceEligibleCandidateIds`: The ordered candidate identifiers whose original projections allowed host governance to participate.
- `hostGovernanceIneligibleCandidateIds`: The ordered candidate identifiers whose original projections kept host governance out of scope.
- `skippedSuppressionIds`: The ordered suppression-rule identifiers that targeted ineligible candidates for this authoring style.
- `skippedOverrideIds`: The ordered override-rule identifiers that targeted ineligible candidates for this authoring style.
- `governanceSuppressionSummaries`: The grouped host-governance suppression-rule outcomes summarized by rule for this authoring style.
- `governanceOverrideSummaries`: The grouped host-governance override-rule outcomes summarized by rule for this authoring style.
- `skippedSuppressionSummaries`: The grouped host-governance-skipped suppression-rule outcomes summarized by rule for this authoring style.
- `skippedOverrideSummaries`: The grouped host-governance-skipped override-rule outcomes summarized by rule for this authoring style.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-authoringpolicysuppressedcandidateids"></a>

##### `AuthoringPolicySuppressedCandidateIds`

```csharp
IReadOnlyList<string> AuthoringPolicySuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-authoringpolicysuppressionsummaries"></a>

##### `AuthoringPolicySuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor> AuthoringPolicySuppressionSummaries { get; }
```

Gets the grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-authoringstyle"></a>

##### `AuthoringStyle`

```csharp
string AuthoringStyle { get; }
```

Gets the normalized authoring style that contributed the grouped candidates.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers contributed by this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-governanceoverridesummaries"></a>

##### `GovernanceOverrideSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor> GovernanceOverrideSummaries { get; }
```

Gets the grouped host-governance override-rule outcomes summarized by rule for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-governancesuppressedcandidateids"></a>

##### `GovernanceSuppressedCandidateIds`

```csharp
IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that were suppressed by host-level REST governance.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-governancesuppressionsummaries"></a>

##### `GovernanceSuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> GovernanceSuppressionSummaries { get; }
```

Gets the grouped host-governance suppression-rule outcomes summarized by rule for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-hostgovernanceeligiblecandidateids"></a>

##### `HostGovernanceEligibleCandidateIds`

```csharp
IReadOnlyList<string> HostGovernanceEligibleCandidateIds { get; }
```

Gets the ordered candidate identifiers whose original projections allowed host governance to participate.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-hostgovernanceineligiblecandidateids"></a>

##### `HostGovernanceIneligibleCandidateIds`

```csharp
IReadOnlyList<string> HostGovernanceIneligibleCandidateIds { get; }
```

Gets the ordered candidate identifiers whose original projections kept host governance out of scope.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-precedenceranks"></a>

##### `PrecedenceRanks`

```csharp
IReadOnlyList<int> PrecedenceRanks { get; }
```

Gets the distinct precedence ranks visible for this authoring style within the group.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-precedencesuppressedcandidateids"></a>

##### `PrecedenceSuppressedCandidateIds`

```csharp
IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that were suppressed by precedence resolution.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-publishedcandidateids"></a>

##### `PublishedCandidateIds`

```csharp
IReadOnlyList<string> PublishedCandidateIds { get; }
```

Gets the ordered candidate identifiers that remain published for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-skippedoverrideids"></a>

##### `SkippedOverrideIds`

```csharp
IReadOnlyList<string> SkippedOverrideIds { get; }
```

Gets the ordered override-rule identifiers that targeted ineligible candidates for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-skippedoverridesummaries"></a>

##### `SkippedOverrideSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor> SkippedOverrideSummaries { get; }
```

Gets the grouped host-governance-skipped override-rule outcomes summarized by rule for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-skippedsuppressionids"></a>

##### `SkippedSuppressionIds`

```csharp
IReadOnlyList<string> SkippedSuppressionIds { get; }
```

Gets the ordered suppression-rule identifiers that targeted ineligible candidates for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-skippedsuppressionsummaries"></a>

##### `SkippedSuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor> SkippedSuppressionSummaries { get; }
```

Gets the grouped host-governance-skipped suppression-rule outcomes summarized by rule for this authoring style.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupauthoringstyledescriptor-sourcemoduleids"></a>

##### `SourceModuleIds`

```csharp
IReadOnlyList<string> SourceModuleIds { get; }
```

Gets the distinct source-module identifiers that contributed candidates for this authoring style.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor"></a>

### `RestEndpointPublicationGroupDescriptor`

Describes the grouped public REST publication outcome for one behavior across all of its visible candidates.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-nullable-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicysuppressiondescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor"></a>

##### `RestEndpointPublicationGroupDescriptor`

```csharp
RestEndpointPublicationGroupDescriptor(string behaviorId, IReadOnlyList<string> sourceModuleIds, int? winningPrecedenceRank, IReadOnlyList<string> publishedCandidateIds, IReadOnlyList<string> precedenceSuppressedCandidateIds, IReadOnlyList<string> governanceSuppressedCandidateIds, IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates, RestEndpointPublicationGroupAuthoringPolicyDescriptor authoringPolicy, IReadOnlyList<string> authoringPolicySuppressedCandidateIds, IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor> authoringPolicySuppressionSummaries, IReadOnlyList<string> hostGovernanceEligibleCandidateIds, IReadOnlyList<string> hostGovernanceIneligibleCandidateIds, IReadOnlyList<string> skippedSuppressionIds, IReadOnlyList<string> skippedOverrideIds, IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> governanceSuppressionSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor> governanceOverrideSummaries)
```

Creates a grouped REST endpoint publication descriptor.

Parameters:
- `behaviorId`: The stable behavior identifier for the grouped publication answer.
- `sourceModuleIds`: The distinct source-module identifiers that contributed the grouped candidates.
- `winningPrecedenceRank`: The winning precedence rank for the published candidates when one or more candidates remain published.
- `publishedCandidateIds`: The candidate identifiers that remain published for this behavior.
- `precedenceSuppressedCandidateIds`: The candidate identifiers that were suppressed by another candidate through precedence resolution.
- `governanceSuppressedCandidateIds`: The candidate identifiers that were suppressed by host-level REST governance.
- `candidates`: The ordered candidate set that produced this grouped publication answer.
- `authoringPolicy`: The effective authoring-policy intent for this behavior-level publication group.
- `authoringPolicySuppressedCandidateIds`: The candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
- `authoringPolicySuppressionSummaries`: The grouped authoring-policy suppression outcomes summarized by suppression kind.
- `hostGovernanceEligibleCandidateIds`: The candidate identifiers whose original projections allowed host governance to participate.
- `hostGovernanceIneligibleCandidateIds`: The candidate identifiers whose original projections kept host governance out of scope.
- `skippedSuppressionIds`: The ordered suppression-rule identifiers that targeted ineligible candidates in this behavior group.
- `skippedOverrideIds`: The ordered override-rule identifiers that targeted ineligible candidates in this behavior group.
- `governanceSuppressionSummaries`: The grouped host-governance suppression-rule outcomes summarized by rule.
- `governanceOverrideSummaries`: The grouped host-governance override-rule outcomes summarized by rule.

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-nullable-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointcandidateruntimedescriptor-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicysuppressiondescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedsuppressionsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedoverridesummarydescriptor"></a>

##### `RestEndpointPublicationGroupDescriptor`

```csharp
RestEndpointPublicationGroupDescriptor(string behaviorId, IReadOnlyList<string> sourceModuleIds, int? winningPrecedenceRank, IReadOnlyList<string> publishedCandidateIds, IReadOnlyList<string> precedenceSuppressedCandidateIds, IReadOnlyList<string> governanceSuppressedCandidateIds, IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates, RestEndpointPublicationGroupAuthoringPolicyDescriptor authoringPolicy, IReadOnlyList<string> authoringPolicySuppressedCandidateIds, IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor> authoringPolicySuppressionSummaries, IReadOnlyList<string> hostGovernanceEligibleCandidateIds, IReadOnlyList<string> hostGovernanceIneligibleCandidateIds, IReadOnlyList<string> skippedSuppressionIds, IReadOnlyList<string> skippedOverrideIds, IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> governanceSuppressionSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor> governanceOverrideSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor> skippedSuppressionSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor> skippedOverrideSummaries)
```

Creates a grouped REST endpoint publication descriptor, including grouped skipped-governance summaries.

Parameters:
- `behaviorId`: The stable behavior identifier for the grouped publication answer.
- `sourceModuleIds`: The distinct source-module identifiers that contributed the grouped candidates.
- `winningPrecedenceRank`: The winning precedence rank for the published candidates when one or more candidates remain published.
- `publishedCandidateIds`: The candidate identifiers that remain published for this behavior.
- `precedenceSuppressedCandidateIds`: The candidate identifiers that were suppressed by another candidate through precedence resolution.
- `governanceSuppressedCandidateIds`: The candidate identifiers that were suppressed by host-level REST governance.
- `candidates`: The ordered candidate set that produced this grouped publication answer.
- `authoringPolicy`: The effective authoring-policy intent for this behavior-level publication group.
- `authoringPolicySuppressedCandidateIds`: The candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
- `authoringPolicySuppressionSummaries`: The grouped authoring-policy suppression outcomes summarized by suppression kind.
- `hostGovernanceEligibleCandidateIds`: The candidate identifiers whose original projections allowed host governance to participate.
- `hostGovernanceIneligibleCandidateIds`: The candidate identifiers whose original projections kept host governance out of scope.
- `skippedSuppressionIds`: The ordered suppression-rule identifiers that targeted ineligible candidates in this behavior group.
- `skippedOverrideIds`: The ordered override-rule identifiers that targeted ineligible candidates in this behavior group.
- `governanceSuppressionSummaries`: The grouped host-governance suppression-rule outcomes summarized by rule.
- `governanceOverrideSummaries`: The grouped host-governance override-rule outcomes summarized by rule.
- `skippedSuppressionSummaries`: The grouped host-governance-skipped suppression-rule outcomes summarized by rule.
- `skippedOverrideSummaries`: The grouped host-governance-skipped override-rule outcomes summarized by rule.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-authoringpolicy"></a>

##### `AuthoringPolicy`

```csharp
RestEndpointPublicationGroupAuthoringPolicyDescriptor AuthoringPolicy { get; }
```

Gets the effective authoring-policy intent for this behavior-level publication group.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-authoringpolicysuppressedcandidateids"></a>

##### `AuthoringPolicySuppressedCandidateIds`

```csharp
IReadOnlyList<string> AuthoringPolicySuppressedCandidateIds { get; }
```

Gets the candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-authoringpolicysuppressionsummaries"></a>

##### `AuthoringPolicySuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor> AuthoringPolicySuppressionSummaries { get; }
```

Gets the grouped authoring-policy suppression outcomes summarized by suppression kind.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-authoringstylesummaries"></a>

##### `AuthoringStyleSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupAuthoringStyleDescriptor> AuthoringStyleSummaries { get; }
```

Gets the grouped publication outcome summarized by authoring style for this behavior.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the stable behavior identifier for the grouped publication answer.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-candidates"></a>

##### `Candidates`

```csharp
IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> Candidates { get; }
```

Gets the ordered candidate set that produced this grouped publication answer.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-governanceoverridesummaries"></a>

##### `GovernanceOverrideSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor> GovernanceOverrideSummaries { get; }
```

Gets the grouped host-governance override-rule outcomes summarized by rule.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-governancesuppressedcandidateids"></a>

##### `GovernanceSuppressedCandidateIds`

```csharp
IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }
```

Gets the candidate identifiers that were suppressed by host-level REST governance.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-governancesuppressionsummaries"></a>

##### `GovernanceSuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> GovernanceSuppressionSummaries { get; }
```

Gets the grouped host-governance suppression-rule outcomes summarized by rule.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-hostgovernanceeligiblecandidateids"></a>

##### `HostGovernanceEligibleCandidateIds`

```csharp
IReadOnlyList<string> HostGovernanceEligibleCandidateIds { get; }
```

Gets the candidate identifiers whose original projections allowed host governance to participate.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-hostgovernanceineligiblecandidateids"></a>

##### `HostGovernanceIneligibleCandidateIds`

```csharp
IReadOnlyList<string> HostGovernanceIneligibleCandidateIds { get; }
```

Gets the candidate identifiers whose original projections kept host governance out of scope.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-precedencesuppressedcandidateids"></a>

##### `PrecedenceSuppressedCandidateIds`

```csharp
IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }
```

Gets the candidate identifiers that were suppressed by precedence resolution.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-publishedcandidateids"></a>

##### `PublishedCandidateIds`

```csharp
IReadOnlyList<string> PublishedCandidateIds { get; }
```

Gets the candidate identifiers that remain published for this behavior.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-skippedoverrideids"></a>

##### `SkippedOverrideIds`

```csharp
IReadOnlyList<string> SkippedOverrideIds { get; }
```

Gets the ordered override-rule identifiers that targeted ineligible candidates in this behavior group.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-skippedoverridesummaries"></a>

##### `SkippedOverrideSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor> SkippedOverrideSummaries { get; }
```

Gets the grouped host-governance-skipped override-rule outcomes summarized by rule.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-skippedsuppressionids"></a>

##### `SkippedSuppressionIds`

```csharp
IReadOnlyList<string> SkippedSuppressionIds { get; }
```

Gets the ordered suppression-rule identifiers that targeted ineligible candidates in this behavior group.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-skippedsuppressionsummaries"></a>

##### `SkippedSuppressionSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor> SkippedSuppressionSummaries { get; }
```

Gets the grouped host-governance-skipped suppression-rule outcomes summarized by rule.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-sourcemoduleids"></a>

##### `SourceModuleIds`

```csharp
IReadOnlyList<string> SourceModuleIds { get; }
```

Gets the distinct source-module identifiers that contributed the grouped candidates.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupdescriptor-winningprecedencerank"></a>

##### `WinningPrecedenceRank`

```csharp
int? WinningPrecedenceRank { get; }
```

Gets the winning precedence rank for the published candidates when one or more remain published.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverrideactionkindsummarydescriptor"></a>

### `RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor`

Describes one grouped override-action bucket within a REST publication-group governance summary.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverrideactionkindsummarydescriptor-ctor-cephalon-abstractions-transports-restendpointoverrideactionkind-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor`

```csharp
RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor(RestEndpointOverrideActionKind actionKind, IReadOnlyList<string> candidateIds)
```

Creates a grouped override-action bucket for a REST publication-group governance summary.

Parameters:
- `actionKind`: The override action dimension represented by this grouped bucket.
- `candidateIds`: The ordered candidate identifiers that selected or materially applied this override action dimension.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverrideactionkindsummarydescriptor-actionkind"></a>

##### `ActionKind`

```csharp
RestEndpointOverrideActionKind ActionKind { get; }
```

Gets the override action dimension represented by this grouped bucket.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverrideactionkindsummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers that selected or materially applied this override action dimension.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor"></a>

### `RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor`

Describes one grouped host-governance override-rule outcome within a REST publication-group answer.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceselectionbasissummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverrideactionkindsummarydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverrideactionkindsummarydescriptor"></a>

##### `RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor`

```csharp
RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor(string ruleId, IReadOnlyList<string> matchedCandidateIds, IReadOnlyList<string> selectedCandidateIds, IReadOnlyList<string> appliedCandidateIds, IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor> selectionBasisSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor> selectedActionKindSummaries, IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor> appliedActionKindSummaries)
```

Creates a grouped host-governance override-rule summary.

Parameters:
- `ruleId`: The stable host-level override-rule identifier summarized by this entry.
- `matchedCandidateIds`: The ordered candidate identifiers that matched this override rule before one winner was selected.
- `selectedCandidateIds`: The ordered candidate identifiers that selected this override rule, including runtime no-op selections.
- `appliedCandidateIds`: The ordered candidate identifiers whose effective runtime answer was materially changed by this override rule.
- `selectionBasisSummaries`: The grouped decisive selection-basis buckets for the candidates that selected this override rule.
- `selectedActionKindSummaries`: The grouped declared override-action buckets for the candidates that selected this override rule.
- `appliedActionKindSummaries`: The grouped materially applied override-action buckets for the candidates this override rule changed.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-appliedactionkindsummaries"></a>

##### `AppliedActionKindSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor> AppliedActionKindSummaries { get; }
```

Gets the grouped materially applied override-action buckets for the candidates this override rule changed.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-appliedcandidateids"></a>

##### `AppliedCandidateIds`

```csharp
IReadOnlyList<string> AppliedCandidateIds { get; }
```

Gets the ordered candidate identifiers whose effective runtime answer was materially changed by this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-matchedcandidateids"></a>

##### `MatchedCandidateIds`

```csharp
IReadOnlyList<string> MatchedCandidateIds { get; }
```

Gets the ordered candidate identifiers that matched this override rule before one winner was selected.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the stable host-level override-rule identifier summarized by this entry.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-selectedactionkindsummaries"></a>

##### `SelectedActionKindSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor> SelectedActionKindSummaries { get; }
```

Gets the grouped declared override-action buckets for the candidates that selected this override rule.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-selectedcandidateids"></a>

##### `SelectedCandidateIds`

```csharp
IReadOnlyList<string> SelectedCandidateIds { get; }
```

Gets the ordered candidate identifiers that selected this override rule, including runtime no-op selections.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceoverridesummarydescriptor-selectionbasissummaries"></a>

##### `SelectionBasisSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor> SelectionBasisSummaries { get; }
```

Gets the grouped decisive selection-basis buckets for the candidates that selected this override rule.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceselectionbasissummarydescriptor"></a>

### `RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor`

Describes one grouped selection-basis bucket within a REST publication-group governance summary.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceselectionbasissummarydescriptor-ctor-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor`

```csharp
RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor(RestEndpointGovernanceRuleSelectionBasis selectionBasis, IReadOnlyList<string> candidateIds)
```

Creates a grouped selection-basis bucket for a REST publication-group governance summary.

Parameters:
- `selectionBasis`: The decisive specificity basis that selected the winning governance rule for the grouped candidates.
- `candidateIds`: The ordered candidate identifiers that resolved the winning governance rule with this selection basis.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceselectionbasissummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers that resolved the winning governance rule with this selection basis.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceselectionbasissummarydescriptor-selectionbasis"></a>

##### `SelectionBasis`

```csharp
RestEndpointGovernanceRuleSelectionBasis SelectionBasis { get; }
```

Gets the decisive specificity basis that selected the winning governance rule for the grouped candidates.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedoverridesummarydescriptor"></a>

### `RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor`

Describes one grouped host-governance-skipped override-rule outcome within a REST publication-group answer.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedoverridesummarydescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor`

```csharp
RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor(string ruleId, IReadOnlyList<string> candidateIds)
```

Creates a grouped host-governance-skipped override-rule summary.

Parameters:
- `ruleId`: The stable host-level override-rule identifier summarized by this entry.
- `candidateIds`: The ordered candidate identifiers that this override rule targeted before host governance was skipped.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedoverridesummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers that this override rule targeted before host governance was skipped.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedoverridesummarydescriptor-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the stable host-level override-rule identifier summarized by this entry.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedsuppressionsummarydescriptor"></a>

### `RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor`

Describes one grouped host-governance-skipped suppression-rule outcome within a REST publication-group answer.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedsuppressionsummarydescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor`

```csharp
RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor(string ruleId, IReadOnlyList<string> candidateIds)
```

Creates a grouped host-governance-skipped suppression-rule summary.

Parameters:
- `ruleId`: The stable host-level suppression-rule identifier summarized by this entry.
- `candidateIds`: The ordered candidate identifiers that this suppression rule targeted before host governance was skipped.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedsuppressionsummarydescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the ordered candidate identifiers that this suppression rule targeted before host governance was skipped.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceskippedsuppressionsummarydescriptor-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the stable host-level suppression-rule identifier summarized by this entry.

<a id="type-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor"></a>

### `RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor`

Describes one grouped host-governance suppression-rule outcome within a REST publication-group answer.

#### Declaration
```csharp
public sealed class RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupgovernanceselectionbasissummarydescriptor"></a>

##### `RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor`

```csharp
RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor(string ruleId, IReadOnlyList<string> matchedCandidateIds, IReadOnlyList<string> suppressedCandidateIds, IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor> selectionBasisSummaries)
```

Creates a grouped host-governance suppression-rule summary.

Parameters:
- `ruleId`: The stable host-level suppression-rule identifier summarized by this entry.
- `matchedCandidateIds`: The ordered candidate identifiers that matched this suppression rule before one winner was selected.
- `suppressedCandidateIds`: The ordered candidate identifiers that this suppression rule ultimately suppressed.
- `selectionBasisSummaries`: The grouped decisive selection-basis buckets for the candidates this suppression rule ultimately suppressed.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-matchedcandidateids"></a>

##### `MatchedCandidateIds`

```csharp
IReadOnlyList<string> MatchedCandidateIds { get; }
```

Gets the ordered candidate identifiers that matched this suppression rule before one winner was selected.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-ruleid"></a>

##### `RuleId`

```csharp
string RuleId { get; }
```

Gets the stable host-level suppression-rule identifier summarized by this entry.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-selectionbasissummaries"></a>

##### `SelectionBasisSummaries`

```csharp
IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor> SelectionBasisSummaries { get; }
```

Gets the grouped decisive selection-basis buckets for the candidates this suppression rule ultimately suppressed.

<a id="member-p-cephalon-abstractions-transports-restendpointpublicationgroupgovernancesuppressionsummarydescriptor-suppressedcandidateids"></a>

##### `SuppressedCandidateIds`

```csharp
IReadOnlyList<string> SuppressedCandidateIds { get; }
```

Gets the ordered candidate identifiers that this suppression rule ultimately suppressed.

<a id="type-cephalon-abstractions-transports-restendpointruntimedescriptor"></a>

### `RestEndpointRuntimeDescriptor`

Describes one resolved public REST endpoint visible to the current runtime.

#### Declaration
```csharp
public sealed class RestEndpointRuntimeDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointruntimedescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-int32-system-string-system-string-system-string-system-nullable-system-int32-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-string-system-string-system-string-system-string-cephalon-abstractions-transports-restendpointcandidateprojectiondescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingdescriptor-system-nullable-cephalon-abstractions-transports-restendpointbindingfallbackmode-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-nullable-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointoverrideactionkind-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointoverrideactionkind-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointRuntimeDescriptor`

```csharp
RestEndpointRuntimeDescriptor(string id, string transportId, string sourceKind, string method, string routePattern, string sourceModuleId, string sourceModuleVersion, int? sourceModuleVersionMajor, string behaviorId, string endpointName, string openApiDocumentName, int? apiVersionMajor, IReadOnlyList<string> tags, string summary, string description, string originalEndpointName, string originalSummary, string originalDescription, string candidateId, RestEndpointCandidateProjectionDescriptor originalProjection, IReadOnlyList<RestEndpointBindingDescriptor> bindingDescriptors, RestEndpointBindingFallbackMode? bindingFallbackMode, IReadOnlyDictionary<string, string> metadata, string authoringStyle, string routeGroupPrefix, string relativePattern, string behaviorType, string sourceId, string requiredCapabilityKey, string originalRequiredCapabilityKey, string appliedOverrideId, IReadOnlyList<string> matchedOverrideIds, string selectedOverrideId, RestEndpointGovernanceRuleSelectionBasis? overrideSelectionBasis, IReadOnlyList<string> skippedSuppressionIds, IReadOnlyList<string> skippedOverrideIds, IReadOnlyList<RestEndpointOverrideActionKind> selectedOverrideActionKinds, IReadOnlyList<RestEndpointOverrideActionKind> appliedOverrideActionKinds, IReadOnlyList<string> requiredFeatureFlagIds, IReadOnlyList<string> originalRequiredFeatureFlagIds)
```

Creates a resolved REST endpoint runtime descriptor.

Parameters:
- `id`: The stable endpoint identifier.
- `transportId`: The stable transport identifier that published the endpoint.
- `sourceKind`: The source kind that produced the endpoint, such as `module-dsl` or `manual`.
- `method`: The resolved HTTP method.
- `routePattern`: The resolved route pattern including the host REST prefix.
- `sourceModuleId`: The stable source-module identifier when one is known.
- `sourceModuleVersion`: The declared source-module version when one is available.
- `sourceModuleVersionMajor`: The parsed source-module major version when one is available.
- `behaviorId`: The stable behavior identifier when the endpoint dispatches through a Cephalon behavior.
- `endpointName`: The resolved endpoint or operation name when one is available.
- `openApiDocumentName`: The resolved OpenAPI document name when one is available.
- `apiVersionMajor`: The resolved public API major version when one is available.
- `tags`: The resolved OpenAPI tags when any are published.
- `summary`: The resolved endpoint summary when one is available.
- `description`: The resolved endpoint description when one is available.
- `originalEndpointName`: The original endpoint or operation name before later endpoint-governance rewrites when the runtime can classify that source answer.
- `originalSummary`: The original endpoint summary before later endpoint-governance rewrites when the runtime can classify that source answer.
- `originalDescription`: The original endpoint description before later endpoint-governance rewrites when the runtime can classify that source answer.
- `candidateId`: The stable originating candidate identifier when this endpoint was published from the module-owned behavior projection pipeline.
- `originalProjection`: The original projection shape before later host-level overrides are applied when the endpoint was published from the module-owned behavior projection pipeline.
- `bindingDescriptors`: The resolved request-binding descriptors when the endpoint exposes an explicit binding plan.
- `bindingFallbackMode`: The resolved request-binding fallback mode when the endpoint preserves deterministic request-binding behavior beyond the explicit binding plan, such as preserved source implicit-query fallback or preserved remaining request-body fallback.
- `metadata`: Optional additive metadata.
- `authoringStyle`: The normalized authoring style such as `behavior-module-profile` or `minimal-api` when the runtime can classify how the endpoint was published.
- `routeGroupPrefix`: The resolved route-group prefix including the host REST prefix when the runtime can classify the grouped publication boundary that produced the endpoint.
- `relativePattern`: The resolved route pattern relative to the grouped publication boundary when the runtime can classify that source shape.
- `behaviorType`: The concrete behavior implementation type name when the endpoint dispatches through a Cephalon behavior and the runtime can classify that implementation identity.
- `sourceId`: The stable source identity for the published endpoint when the runtime can classify the authored source shape behind that publication.
- `requiredCapabilityKey`: The required Cephalon capability key enforced at the REST boundary when one is available.
- `originalRequiredCapabilityKey`: The original required Cephalon capability key before later endpoint-governance rewrites when the runtime can classify that source answer.
- `appliedOverrideId`: The host-level override identifier when runtime governance actually changes the published endpoint answer.
- `matchedOverrideIds`: The ordered override identifiers that matched this endpoint's originating candidate before one winner was selected.
- `selectedOverrideId`: The selected override identifier when one winning override rule was resolved for this endpoint's originating candidate, even if that winning rule became a runtime no-op.
- `overrideSelectionBasis`: The earliest decisive specificity rule that selected the winning override rule when one was resolved for this endpoint's originating candidate.
- `skippedSuppressionIds`: The ordered suppression identifiers that otherwise target this endpoint's originating candidate but were skipped because the original projection did not allow host governance to participate.
- `skippedOverrideIds`: The ordered override identifiers that otherwise target this endpoint's originating candidate but were skipped because the original projection did not allow host governance to participate.
- `selectedOverrideActionKinds`: The normalized action dimensions declared by the selected override rule when one winning override rule was resolved for this endpoint's originating candidate.
- `appliedOverrideActionKinds`: The normalized action dimensions that materially changed the published endpoint answer when the selected override rule was not a runtime no-op.
- `requiredFeatureFlagIds`: The required Cephalon feature-flag identifiers enforced at the REST boundary when any are available.
- `originalRequiredFeatureFlagIds`: The original required Cephalon feature-flag identifiers before later endpoint-governance rewrites when the runtime can classify that source answer.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-apiversionmajor"></a>

##### `ApiVersionMajor`

```csharp
int? ApiVersionMajor { get; }
```

Gets the resolved public API major version when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-appliedoverrideactionkinds"></a>

##### `AppliedOverrideActionKinds`

```csharp
IReadOnlyList<RestEndpointOverrideActionKind> AppliedOverrideActionKinds { get; }
```

Gets the normalized action dimensions that materially changed the published endpoint answer when the selected override rule was not a runtime no-op.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-appliedoverrideid"></a>

##### `AppliedOverrideId`

```csharp
string AppliedOverrideId { get; }
```

Gets the host-level override identifier when runtime governance actually changes the published endpoint answer.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-authoringstyle"></a>

##### `AuthoringStyle`

```csharp
string AuthoringStyle { get; }
```

Gets the normalized authoring style such as `behavior-module-profile` or `minimal-api` when the runtime can classify how the endpoint was published.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-behaviorid"></a>

##### `BehaviorId`

```csharp
string BehaviorId { get; }
```

Gets the stable behavior identifier when the endpoint dispatches through a Cephalon behavior.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-behaviortype"></a>

##### `BehaviorType`

```csharp
string BehaviorType { get; }
```

Gets the concrete behavior implementation type name when the endpoint dispatches through a Cephalon behavior and the runtime can classify that implementation identity.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-bindingdescriptors"></a>

##### `BindingDescriptors`

```csharp
IReadOnlyList<RestEndpointBindingDescriptor> BindingDescriptors { get; }
```

Gets the resolved request-binding descriptors when the endpoint exposes an explicit binding plan.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-bindingfallbackmode"></a>

##### `BindingFallbackMode`

```csharp
RestEndpointBindingFallbackMode? BindingFallbackMode { get; }
```

Gets the resolved request-binding fallback mode when the endpoint preserves deterministic request-binding behavior beyond the explicit binding plan, such as preserved source implicit-query fallback or preserved remaining request-body fallback.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-candidateid"></a>

##### `CandidateId`

```csharp
string CandidateId { get; }
```

Gets the stable originating candidate identifier when this endpoint was published from the module-owned behavior projection pipeline.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the resolved endpoint description when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-endpointname"></a>

##### `EndpointName`

```csharp
string EndpointName { get; }
```

Gets the resolved endpoint or operation name when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable endpoint identifier.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-matchedoverrideids"></a>

##### `MatchedOverrideIds`

```csharp
IReadOnlyList<string> MatchedOverrideIds { get; }
```

Gets the ordered override identifiers that matched this endpoint's originating candidate before one winner was selected.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional additive metadata.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-method"></a>

##### `Method`

```csharp
string Method { get; }
```

Gets the resolved HTTP method.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-openapidocumentname"></a>

##### `OpenApiDocumentName`

```csharp
string OpenApiDocumentName { get; }
```

Gets the resolved OpenAPI document name when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-originaldescription"></a>

##### `OriginalDescription`

```csharp
string OriginalDescription { get; }
```

Gets the original endpoint description before later endpoint-governance rewrites when the runtime can classify that source answer.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-originalendpointname"></a>

##### `OriginalEndpointName`

```csharp
string OriginalEndpointName { get; }
```

Gets the original endpoint or operation name before later endpoint-governance rewrites when the runtime can classify that source answer.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-originalprojection"></a>

##### `OriginalProjection`

```csharp
RestEndpointCandidateProjectionDescriptor OriginalProjection { get; }
```

Gets the original projection shape before later host-level overrides are applied when the endpoint was published from the module-owned behavior projection pipeline, including whether host governance was allowed to participate for that source route.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-originalrequiredcapabilitykey"></a>

##### `OriginalRequiredCapabilityKey`

```csharp
string OriginalRequiredCapabilityKey { get; }
```

Gets the original required Cephalon capability key before later endpoint-governance rewrites when the runtime can classify that source answer.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-originalrequiredfeatureflagids"></a>

##### `OriginalRequiredFeatureFlagIds`

```csharp
IReadOnlyList<string> OriginalRequiredFeatureFlagIds { get; }
```

Gets the original required Cephalon feature-flag identifiers before later endpoint-governance rewrites when the runtime can classify that source answer.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-originalsummary"></a>

##### `OriginalSummary`

```csharp
string OriginalSummary { get; }
```

Gets the original endpoint summary before later endpoint-governance rewrites when the runtime can classify that source answer.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-overrideselectionbasis"></a>

##### `OverrideSelectionBasis`

```csharp
RestEndpointGovernanceRuleSelectionBasis? OverrideSelectionBasis { get; }
```

Gets the earliest decisive specificity rule that selected the winning override rule when one was resolved for this endpoint's originating candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-relativepattern"></a>

##### `RelativePattern`

```csharp
string RelativePattern { get; }
```

Gets the resolved route pattern relative to the grouped publication boundary when the runtime can classify that source shape.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-requiredcapabilitykey"></a>

##### `RequiredCapabilityKey`

```csharp
string RequiredCapabilityKey { get; }
```

Gets the required Cephalon capability key enforced at the REST boundary when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-requiredfeatureflagids"></a>

##### `RequiredFeatureFlagIds`

```csharp
IReadOnlyList<string> RequiredFeatureFlagIds { get; }
```

Gets the required Cephalon feature-flag identifiers enforced at the REST boundary when any are available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-routegroupprefix"></a>

##### `RouteGroupPrefix`

```csharp
string RouteGroupPrefix { get; }
```

Gets the resolved route-group prefix including the host REST prefix when the runtime can classify the grouped publication boundary that produced the endpoint.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-routepattern"></a>

##### `RoutePattern`

```csharp
string RoutePattern { get; }
```

Gets the resolved route pattern including the host REST prefix.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-selectedoverrideactionkinds"></a>

##### `SelectedOverrideActionKinds`

```csharp
IReadOnlyList<RestEndpointOverrideActionKind> SelectedOverrideActionKinds { get; }
```

Gets the normalized action dimensions declared by the selected override rule when one winning override rule was resolved for this endpoint's originating candidate.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-selectedoverrideid"></a>

##### `SelectedOverrideId`

```csharp
string SelectedOverrideId { get; }
```

Gets the selected override identifier when one winning override rule was resolved for this endpoint's originating candidate, even if that winning rule became a runtime no-op.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-skippedoverrideids"></a>

##### `SkippedOverrideIds`

```csharp
IReadOnlyList<string> SkippedOverrideIds { get; }
```

Gets the ordered override identifiers that otherwise target this endpoint's originating candidate but were skipped because the original projection did not allow host governance.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-skippedsuppressionids"></a>

##### `SkippedSuppressionIds`

```csharp
IReadOnlyList<string> SkippedSuppressionIds { get; }
```

Gets the ordered suppression identifiers that otherwise target this endpoint's originating candidate but were skipped because the original projection did not allow host governance.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-sourceid"></a>

##### `SourceId`

```csharp
string SourceId { get; }
```

Gets the stable source identity for the published endpoint when the runtime can classify the authored source shape behind that publication.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-sourcekind"></a>

##### `SourceKind`

```csharp
string SourceKind { get; }
```

Gets the source kind that produced the endpoint.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the stable source-module identifier when one is known.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-sourcemoduleversion"></a>

##### `SourceModuleVersion`

```csharp
string SourceModuleVersion { get; }
```

Gets the declared source-module version when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-sourcemoduleversionmajor"></a>

##### `SourceModuleVersionMajor`

```csharp
int? SourceModuleVersionMajor { get; }
```

Gets the parsed source-module major version when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-summary"></a>

##### `Summary`

```csharp
string Summary { get; }
```

Gets the resolved endpoint summary when one is available.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the resolved OpenAPI tags when any are published.

<a id="member-p-cephalon-abstractions-transports-restendpointruntimedescriptor-transportid"></a>

##### `TransportId`

```csharp
string TransportId { get; }
```

Gets the stable transport identifier that published the endpoint.

<a id="type-cephalon-abstractions-transports-restendpointsuppressiondescriptor"></a>

### `RestEndpointSuppressionDescriptor`

Describes one host-level REST endpoint suppression rule visible to the current runtime for module-owned REST candidates that participate in host governance.

#### Declaration
```csharp
public sealed class RestEndpointSuppressionDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-restendpointsuppressiondescriptor-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingfallbackmode-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingdescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceruleselectionbasis-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointgovernanceselectionbasissummarydescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointSuppressionDescriptor`

```csharp
RestEndpointSuppressionDescriptor(string id, IReadOnlyList<string> candidateIds, IReadOnlyList<string> behaviorIds, IReadOnlyList<string> sourceModuleIds, IReadOnlyList<string> authoringStyles, IReadOnlyList<int> apiVersionMajors, IReadOnlyList<string> methods, IReadOnlyList<string> relativePatterns, IReadOnlyList<string> routeGroupPrefixes, IReadOnlyList<string> openApiDocumentNames, IReadOnlyList<string> tagNames, IReadOnlyList<string> endpointNames, IReadOnlyList<RestEndpointBindingFallbackMode> bindingFallbackModes, IReadOnlyList<RestEndpointBindingDescriptor> targetBindings, IReadOnlyList<string> matchedCandidateIds, IReadOnlyList<string> suppressedCandidateIds, IReadOnlyList<string> skippedCandidateIds, IReadOnlyList<RestEndpointGovernanceRuleSelectionBasis> selectionBases, IReadOnlyList<RestEndpointGovernanceSelectionBasisSummaryDescriptor> selectionBasisSummaries, IReadOnlyList<string> hostGovernanceScopes, IReadOnlyList<string> behaviorIdPrefixes)
```

Creates a REST endpoint suppression descriptor.

Parameters:
- `id`: The stable suppression identifier.
- `candidateIds`: The original candidate identifiers targeted by the suppression rule.
- `behaviorIds`: The behavior identifiers targeted by the suppression rule.
- `sourceModuleIds`: The source-module identifiers targeted by the suppression rule.
- `authoringStyles`: The normalized authoring styles targeted by the suppression rule. Explicit module-DSL routes participate only when their owning route group opted into host governance.
- `apiVersionMajors`: The effective API major versions targeted by the suppression rule.
- `methods`: The effective HTTP methods targeted by the suppression rule.
- `relativePatterns`: The shorthand relative route patterns targeted by the suppression rule.
- `routeGroupPrefixes`: The published route-group prefixes targeted by the suppression rule.
- `openApiDocumentNames`: The original candidate OpenAPI document names targeted by the suppression rule.
- `tagNames`: The original candidate primary OpenAPI tag names targeted by the suppression rule.
- `endpointNames`: The original candidate endpoint names targeted by the suppression rule.
- `hostGovernanceScopes`: The original candidate host-governance scopes targeted by the suppression rule.
- `bindingFallbackModes`: The original candidate request-binding fallback modes targeted by the suppression rule.
- `targetBindings`: The original candidate explicit binding descriptors targeted by the suppression rule before any override actions are applied.
- `matchedCandidateIds`: The runtime candidate identifiers that matched this suppression rule, including candidates where another suppression rule won selection.
- `suppressedCandidateIds`: The runtime candidate identifiers that were actually suppressed by this rule after governance selection completed.
- `skippedCandidateIds`: The runtime candidate identifiers that this rule would otherwise target but skipped because the original projection did not allow host governance to participate.
- `selectionBases`: The union of decisive specificity rules that selected this suppression rule for one or more runtime candidates.
- `selectionBasisSummaries`: The grouped selection-basis buckets for runtime candidates that were actually suppressed by this rule.
- `behaviorIdPrefixes`: The behavior-id prefixes targeted by the suppression rule. Prefix matches use the stable dot-separated behavior-id hierarchy, so a prefix targets the exact behavior id and any descendant behavior ids beneath that prefix.

#### Properties

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-apiversionmajors"></a>

##### `ApiVersionMajors`

```csharp
IReadOnlyList<int> ApiVersionMajors { get; }
```

Gets the effective API major versions targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-authoringstyles"></a>

##### `AuthoringStyles`

```csharp
IReadOnlyList<string> AuthoringStyles { get; }
```

Gets the normalized authoring styles targeted by this suppression rule. Explicit module-DSL routes participate only when their owning route group opted into host governance.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-behavioridprefixes"></a>

##### `BehaviorIdPrefixes`

```csharp
IReadOnlyList<string> BehaviorIdPrefixes { get; }
```

Gets the behavior-id prefixes targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-behaviorids"></a>

##### `BehaviorIds`

```csharp
IReadOnlyList<string> BehaviorIds { get; }
```

Gets the behavior identifiers targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-bindingfallbackmodes"></a>

##### `BindingFallbackModes`

```csharp
IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }
```

Gets the original candidate request-binding fallback modes targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the original candidate identifiers targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-endpointnames"></a>

##### `EndpointNames`

```csharp
IReadOnlyList<string> EndpointNames { get; }
```

Gets the original candidate endpoint names targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-hostgovernancescopes"></a>

##### `HostGovernanceScopes`

```csharp
IReadOnlyList<string> HostGovernanceScopes { get; }
```

Gets the original candidate host-governance scopes targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable suppression identifier.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-matchedcandidateids"></a>

##### `MatchedCandidateIds`

```csharp
IReadOnlyList<string> MatchedCandidateIds { get; }
```

Gets the runtime candidate identifiers that matched this suppression rule, including candidates where another suppression rule won selection.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-methods"></a>

##### `Methods`

```csharp
IReadOnlyList<string> Methods { get; }
```

Gets the effective HTTP methods targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-openapidocumentnames"></a>

##### `OpenApiDocumentNames`

```csharp
IReadOnlyList<string> OpenApiDocumentNames { get; }
```

Gets the original candidate OpenAPI document names targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-relativepatterns"></a>

##### `RelativePatterns`

```csharp
IReadOnlyList<string> RelativePatterns { get; }
```

Gets the relative route patterns targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-routegroupprefixes"></a>

##### `RouteGroupPrefixes`

```csharp
IReadOnlyList<string> RouteGroupPrefixes { get; }
```

Gets the published route-group prefixes targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-selectionbases"></a>

##### `SelectionBases`

```csharp
IReadOnlyList<RestEndpointGovernanceRuleSelectionBasis> SelectionBases { get; }
```

Gets the union of decisive specificity rules that selected this suppression rule for one or more runtime candidates.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-selectionbasissummaries"></a>

##### `SelectionBasisSummaries`

```csharp
IReadOnlyList<RestEndpointGovernanceSelectionBasisSummaryDescriptor> SelectionBasisSummaries { get; }
```

Gets the grouped selection-basis buckets for runtime candidates that were actually suppressed by this rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-skippedcandidateids"></a>

##### `SkippedCandidateIds`

```csharp
IReadOnlyList<string> SkippedCandidateIds { get; }
```

Gets the runtime candidate identifiers that this rule would otherwise target but skipped because the original projection did not allow host governance to participate.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-sourcemoduleids"></a>

##### `SourceModuleIds`

```csharp
IReadOnlyList<string> SourceModuleIds { get; }
```

Gets the source-module identifiers targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-suppressedcandidateids"></a>

##### `SuppressedCandidateIds`

```csharp
IReadOnlyList<string> SuppressedCandidateIds { get; }
```

Gets the runtime candidate identifiers that were actually suppressed by this rule after governance selection completed.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-tagnames"></a>

##### `TagNames`

```csharp
IReadOnlyList<string> TagNames { get; }
```

Gets the original candidate primary OpenAPI tag names targeted by this suppression rule.

<a id="member-p-cephalon-abstractions-transports-restendpointsuppressiondescriptor-targetbindings"></a>

##### `TargetBindings`

```csharp
IReadOnlyList<RestEndpointBindingDescriptor> TargetBindings { get; }
```

Gets the original candidate explicit binding descriptors targeted by this suppression rule before any override actions are applied.

<a id="type-cephalon-abstractions-transports-transportdescriptor"></a>

### `TransportDescriptor`

Describes one transport exposed by an app.

#### Declaration
```csharp
public sealed class TransportDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-transportdescriptor-ctor-system-string-system-string-system-string-cephalon-abstractions-transports-transportfeatures-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TransportDescriptor`

```csharp
TransportDescriptor(string id, string displayName, string description, TransportFeatures features, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a transport descriptor.

Parameters:
- `id`: The stable transport identifier.
- `displayName`: The human-readable transport name.
- `description`: The transport description.
- `features`: The features supported by the transport.
- `tags`: The tags associated with the transport.
- `metadata`: Optional transport metadata.

#### Properties

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the transport description.

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the human-readable transport name.

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-features"></a>

##### `Features`

```csharp
TransportFeatures Features { get; }
```

Gets the features supported by the transport.

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable transport identifier.

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional transport metadata.

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the tags associated with the transport.

<a id="type-cephalon-abstractions-transports-transportfeatures"></a>

### `TransportFeatures`

Describes the protocol capabilities supported by a transport.

#### Declaration
```csharp
public enum TransportFeatures
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-transportfeatures-clientstreaming"></a>

##### `ClientStreaming`

```csharp
const TransportFeatures ClientStreaming
```

Indicates client-streaming interactions are supported.

<a id="member-f-cephalon-abstractions-transports-transportfeatures-duplexstreaming"></a>

##### `DuplexStreaming`

```csharp
const TransportFeatures DuplexStreaming
```

Indicates duplex-streaming interactions are supported.

<a id="member-f-cephalon-abstractions-transports-transportfeatures-none"></a>

##### `None`

```csharp
const TransportFeatures None
```

Indicates no transport features.

<a id="member-f-cephalon-abstractions-transports-transportfeatures-requestresponse"></a>

##### `RequestResponse`

```csharp
const TransportFeatures RequestResponse
```

Indicates request-response interactions are supported.

<a id="member-f-cephalon-abstractions-transports-transportfeatures-serverstreaming"></a>

##### `ServerStreaming`

```csharp
const TransportFeatures ServerStreaming
```

Indicates server-streaming interactions are supported.
