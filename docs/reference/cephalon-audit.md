# Cephalon.Audit

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Audit)
## Namespaces

- `Cephalon.Audit.Configuration`
- `Cephalon.Audit.Conventions`
- `Cephalon.Audit.Registration`
- `Cephalon.Audit.Services`

<a id="namespace-cephalon-audit-configuration"></a>

## Namespace Cephalon.Audit.Configuration

<a id="type-cephalon-audit-configuration-auditruntimeoptions"></a>

### `AuditRuntimeOptions`

Describes host-agnostic runtime options for the Cephalon audit companion pack.

#### Declaration
```csharp
public sealed class AuditRuntimeOptions
```

#### Constructors

<a id="member-m-cephalon-audit-configuration-auditruntimeoptions-ctor-system-int32"></a>

##### `AuditRuntimeOptions`

```csharp
AuditRuntimeOptions(int inMemoryBufferCapacity)
```

Initializes a new instance of the `AuditRuntimeOptions` class.

Parameters:
- `inMemoryBufferCapacity`: The maximum number of audit entries retained by the default in-memory writer.

#### Properties

<a id="member-p-cephalon-audit-configuration-auditruntimeoptions-enableinmemorywriter"></a>

##### `EnableInMemoryWriter`

```csharp
bool EnableInMemoryWriter { get; set; }
```

Gets or sets a value indicating whether the built-in in-memory audit writer should remain active.

<a id="member-p-cephalon-audit-configuration-auditruntimeoptions-inmemorybuffercapacity"></a>

##### `InMemoryBufferCapacity`

```csharp
int InMemoryBufferCapacity { get; set; }
```

Gets or sets the maximum number of audit entries retained by the default in-memory writer.

#### Methods

<a id="member-m-cephalon-audit-configuration-auditruntimeoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
AuditRuntimeOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads audit runtime options from configuration.

Returns: The parsed audit runtime options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The root configuration section path to read from.

<a id="namespace-cephalon-audit-conventions"></a>

## Namespace Cephalon.Audit.Conventions

<a id="type-cephalon-audit-conventions-auditmetadatakeys"></a>

### `AuditMetadataKeys`

Defines shared metadata keys used by the Cephalon audit companion pack.

#### Declaration
```csharp
public static class AuditMetadataKeys
```

#### Fields

<a id="member-f-cephalon-audit-conventions-auditmetadatakeys-action"></a>

##### `Action`

```csharp
const string Action
```

The metadata key that stores the audit action identifier.

<a id="member-f-cephalon-audit-conventions-auditmetadatakeys-actorid"></a>

##### `ActorId`

```csharp
const string ActorId
```

The metadata key that stores the actor identifier associated with an audit entry.

<a id="member-f-cephalon-audit-conventions-auditmetadatakeys-category"></a>

##### `Category`

```csharp
const string Category
```

The metadata key that stores the audit category.

<a id="member-f-cephalon-audit-conventions-auditmetadatakeys-correlationid"></a>

##### `CorrelationId`

```csharp
const string CorrelationId
```

The metadata key that stores the correlation identifier associated with an audit entry.

<a id="member-f-cephalon-audit-conventions-auditmetadatakeys-subjectid"></a>

##### `SubjectId`

```csharp
const string SubjectId
```

The metadata key that stores the audited subject identifier.

<a id="member-f-cephalon-audit-conventions-auditmetadatakeys-subjecttype"></a>

##### `SubjectType`

```csharp
const string SubjectType
```

The metadata key that stores the audited subject type.

<a id="member-f-cephalon-audit-conventions-auditmetadatakeys-tenantid"></a>

##### `TenantId`

```csharp
const string TenantId
```

The metadata key that stores the tenant identifier associated with an audit entry.

<a id="namespace-cephalon-audit-registration"></a>

## Namespace Cephalon.Audit.Registration

<a id="type-cephalon-audit-registration-auditenginebuilderextensions"></a>

### `AuditEngineBuilderExtensions`

Registers the host-agnostic Cephalon audit companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class AuditEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-audit-registration-auditenginebuilderextensions-addaudit-cephalon-engine-composition-enginebuilder-system-action-cephalon-audit-configuration-auditruntimeoptions"></a>

##### `AddAudit`

```csharp
EngineBuilder AddAudit(this EngineBuilder builder, Action<AuditRuntimeOptions> configure)
```

Adds the Cephalon audit companion pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures host-owned audit runtime options.

<a id="namespace-cephalon-audit-services"></a>

## Namespace Cephalon.Audit.Services

<a id="type-cephalon-audit-services-auditrecordrequest"></a>

### `AuditRecordRequest`

Describes one audit request before Cephalon fills its default runtime context.

#### Declaration
```csharp
public sealed class AuditRecordRequest
```

#### Constructors

<a id="member-m-cephalon-audit-services-auditrecordrequest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-cephalon-abstractions-audit-auditactor-cephalon-abstractions-audit-auditoutcome-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-audit-auditchange-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AuditRecordRequest`

```csharp
AuditRecordRequest(string category, string action, string summary, string subjectType, string subjectId, string entryId, DateTimeOffset? occurredAtUtc, AuditActor actor, AuditOutcome outcome, string tenantId, string correlationId, IReadOnlyList<AuditChange> changes, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

Creates a new audit-record request.

Parameters:
- `category`: The logical audit category.
- `action`: The logical action identifier associated with the audit event.
- `summary`: The human-readable audit summary.
- `subjectType`: The logical subject type associated with the entry.
- `subjectId`: The stable subject identifier associated with the entry when one is known.
- `entryId`: The stable audit-entry identifier when the caller wants to supply one explicitly.
- `occurredAtUtc`: The occurrence time when the caller wants to supply it explicitly.
- `actor`: The actor responsible for the audited operation when the caller wants to supply one explicitly.
- `outcome`: The outcome recorded for the audited operation.
- `tenantId`: The tenant identifier associated with the audited operation.
- `correlationId`: The correlation identifier associated with the audited operation.
- `changes`: Optional field-level changes captured for the operation.
- `tags`: Optional descriptive tags associated with the entry.
- `metadata`: Optional audit metadata.

#### Properties

<a id="member-p-cephalon-audit-services-auditrecordrequest-action"></a>

##### `Action`

```csharp
string Action { get; }
```

Gets the logical action identifier associated with the audit event.

<a id="member-p-cephalon-audit-services-auditrecordrequest-actor"></a>

##### `Actor`

```csharp
AuditActor Actor { get; }
```

Gets the actor responsible for the audited operation when one was supplied explicitly.

<a id="member-p-cephalon-audit-services-auditrecordrequest-category"></a>

##### `Category`

```csharp
string Category { get; }
```

Gets the logical audit category.

<a id="member-p-cephalon-audit-services-auditrecordrequest-changes"></a>

##### `Changes`

```csharp
IReadOnlyList<AuditChange> Changes { get; }
```

Gets the field-level changes captured for the operation.

<a id="member-p-cephalon-audit-services-auditrecordrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the correlation identifier associated with the audited operation.

<a id="member-p-cephalon-audit-services-auditrecordrequest-entryid"></a>

##### `EntryId`

```csharp
string EntryId { get; }
```

Gets the caller-supplied audit-entry identifier when one is known.

<a id="member-p-cephalon-audit-services-auditrecordrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets audit metadata associated with the entry.

<a id="member-p-cephalon-audit-services-auditrecordrequest-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset? OccurredAtUtc { get; }
```

Gets the caller-supplied occurrence time when one is known.

<a id="member-p-cephalon-audit-services-auditrecordrequest-outcome"></a>

##### `Outcome`

```csharp
AuditOutcome Outcome { get; }
```

Gets the outcome recorded for the audited operation.

<a id="member-p-cephalon-audit-services-auditrecordrequest-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; }
```

Gets the stable subject identifier associated with the entry when one is known.

<a id="member-p-cephalon-audit-services-auditrecordrequest-subjecttype"></a>

##### `SubjectType`

```csharp
string SubjectType { get; }
```

Gets the logical subject type associated with the entry.

<a id="member-p-cephalon-audit-services-auditrecordrequest-summary"></a>

##### `Summary`

```csharp
string Summary { get; }
```

Gets the human-readable audit summary.

<a id="member-p-cephalon-audit-services-auditrecordrequest-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets descriptive tags associated with the entry.

<a id="member-p-cephalon-audit-services-auditrecordrequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; }
```

Gets the tenant identifier associated with the audited operation.

<a id="type-cephalon-audit-services-iauditactoraccessor"></a>

### `IAuditActorAccessor`

Exposes the audit actor currently associated with the ambient runtime scope when one is known.

#### Declaration
```csharp
public interface IAuditActorAccessor
```

#### Properties

<a id="member-p-cephalon-audit-services-iauditactoraccessor-current"></a>

##### `Current`

```csharp
AuditActor Current { get; }
```

Gets the audit actor currently associated with the ambient runtime scope when one is known.

<a id="type-cephalon-audit-services-iauditrecorder"></a>

### `IAuditRecorder`

Records audit entries through the active Cephalon audit pipeline.

#### Declaration
```csharp
public interface IAuditRecorder
```

#### Methods

<a id="member-m-cephalon-audit-services-iauditrecorder-recordasync-cephalon-audit-services-auditrecordrequest-system-threading-cancellationtoken"></a>

##### `RecordAsync`

```csharp
ValueTask<AuditEntry> RecordAsync(AuditRecordRequest request, CancellationToken cancellationToken)
```

Records one audit entry and returns the normalized entry that was written.

Returns: The normalized audit entry that was written.

Parameters:
- `request`: The audit request to record.
- `cancellationToken`: The token that cancels the operation.
