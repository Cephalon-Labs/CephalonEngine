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
- `Cephalon.Abstractions.Health`
- `Cephalon.Abstractions.Ids`
- `Cephalon.Abstractions.Localization`
- `Cephalon.Abstractions.Modules`
- `Cephalon.Abstractions.Patterns`
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

<a id="member-m-cephalon-abstractions-appmodel-appprofile-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-patterns-patterndescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-transportdescriptor-cephalon-abstractions-appmodel-dataselection-cephalon-abstractions-appmodel-identityselection-cephalon-abstractions-appmodel-tenancyselection-cephalon-abstractions-appmodel-auditselection-cephalon-abstractions-appmodel-messagingselection"></a>

##### `AppProfile`

```csharp
AppProfile(string blueprintId, string blueprintDisplayName, string blueprintDescription, IReadOnlyList<PatternDescriptor> patterns, IReadOnlyList<TechnologyDescriptor> technologies, IReadOnlyList<TransportDescriptor> transports, DataSelection data, IdentitySelection identity, TenancySelection tenancy, AuditSelection audit, MessagingSelection messaging)
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
- `identity`: The selected identity and authorization inputs.
- `tenancy`: The selected multi-tenancy inputs.
- `audit`: The selected audit inputs.
- `messaging`: The selected messaging inputs.

<a id="member-m-cephalon-abstractions-appmodel-appprofile-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-patterns-patterndescriptor-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-transportdescriptor-cephalon-abstractions-appmodel-dataselection-cephalon-abstractions-appmodel-identityselection-cephalon-abstractions-appmodel-tenancyselection-cephalon-abstractions-appmodel-auditselection-cephalon-abstractions-appmodel-messagingselection"></a>

##### `AppProfile`

```csharp
AppProfile(string blueprintId, string blueprintDisplayName, string blueprintDescription, IReadOnlyList<PatternDescriptor> patterns, ScaffoldPlan scaffold, IReadOnlyList<TechnologyDescriptor> technologies, IReadOnlyList<TransportDescriptor> transports, DataSelection data, IdentitySelection identity, TenancySelection tenancy, AuditSelection audit, MessagingSelection messaging)
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
- `identity`: The selected identity and authorization inputs.
- `tenancy`: The selected multi-tenancy inputs.
- `audit`: The selected audit inputs.
- `messaging`: The selected messaging inputs.

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

<a id="type-cephalon-abstractions-appmodel-auditselection"></a>

### `AuditSelection`

Describes the active audit and history inputs resolved for a Cephalon app.

#### Declaration
```csharp
public sealed class AuditSelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-auditselection-ctor-system-nullable-system-boolean"></a>

##### `AuditSelection`

```csharp
AuditSelection(bool? enabled)
```

Initializes a new instance of the `AuditSelection` class.

Parameters:
- `enabled`: Whether audit support was explicitly enabled.

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

Restricts which patterns ops config may activate for this behavior. If absent, no allowlist restriction applies.

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

Restricts which transports ops config may activate for this behavior. If absent, no allowlist restriction applies.

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

Describes the resolved topology for a single behavior: its pattern, transports, and feature flags.

#### Declaration
```csharp
public sealed class BehaviorTopologyDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-behaviors-behaviortopologydescriptor-ctor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-boolean-system-boolean-system-boolean-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `BehaviorTopologyDescriptor`

```csharp
BehaviorTopologyDescriptor(string id, string pattern, IReadOnlyList<string> transportIds, bool inboxEnabled, bool outboxEnabled, bool eventSourcingEnabled, string displayName, string description, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of `BehaviorTopologyDescriptor`.

#### Properties

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

Gets the pattern identifier (e.g. "cqrs", "event-driven", "saga-step", "process-manager", "direct").

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

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-build-system-string"></a>

##### `Build`

```csharp
BehaviorTopologyDescriptor Build(string behaviorId)
```

Builds the final descriptor. Called internally by the engine — do not call directly.

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

<a id="member-m-cephalon-abstractions-behaviors-ibehaviortopologybuilder-viahttprest"></a>

##### `ViaHttpRest`

```csharp
IBehaviorTopologyBuilder ViaHttpRest()
```

Adds the HTTP REST transport (GET/POST routing).

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

<a id="member-m-cephalon-abstractions-data-outboxdescriptor-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `OutboxDescriptor`

```csharp
OutboxDescriptor(string id, string displayName, string description, string sourceModuleId, string provider, string mode, IReadOnlyList<string> channelIds, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
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

<a id="namespace-cephalon-abstractions-technologies"></a>

## Namespace Cephalon.Abstractions.Technologies

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
