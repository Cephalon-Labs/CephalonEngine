# Cephalon.Abstractions

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Abstractions)
## Namespaces

- `Cephalon.Abstractions.AppModel`
- `Cephalon.Abstractions.AppModel.Scaffolding`
- `Cephalon.Abstractions.Capabilities`
- `Cephalon.Abstractions.Execution`
- `Cephalon.Abstractions.Health`
- `Cephalon.Abstractions.Localization`
- `Cephalon.Abstractions.Modules`
- `Cephalon.Abstractions.Patterns`
- `Cephalon.Abstractions.Technologies`
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

<a id="member-m-cephalon-abstractions-appmodel-appprofile-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-patterns-patterndescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-transportdescriptor"></a>

##### `AppProfile`

```csharp
AppProfile(string blueprintId, string blueprintDisplayName, string blueprintDescription, IReadOnlyList<PatternDescriptor> patterns, IReadOnlyList<TechnologyDescriptor> technologies, IReadOnlyList<TransportDescriptor> transports)
```

Creates an app profile without scaffold metadata.

Parameters:
- `blueprintId`: The selected blueprint identifier.
- `blueprintDisplayName`: The selected blueprint display name.
- `blueprintDescription`: The selected blueprint description.
- `patterns`: The patterns active for the app.
- `technologies`: The selected technology profiles.
- `transports`: The selected transports.

<a id="member-m-cephalon-abstractions-appmodel-appprofile-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-patterns-patterndescriptor-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-transportdescriptor"></a>

##### `AppProfile`

```csharp
AppProfile(string blueprintId, string blueprintDisplayName, string blueprintDescription, IReadOnlyList<PatternDescriptor> patterns, ScaffoldPlan scaffold, IReadOnlyList<TechnologyDescriptor> technologies, IReadOnlyList<TransportDescriptor> transports)
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

#### Properties

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

<a id="member-p-cephalon-abstractions-appmodel-appprofile-transports"></a>

##### `Transports`

```csharp
IReadOnlyList<TransportDescriptor> Transports { get; }
```

Gets the selected transports.

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

<a id="member-m-cephalon-abstractions-patterns-patterndescriptor-ctor-system-string-system-string-system-string-cephalon-abstractions-patterns-patternkind-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `PatternDescriptor`

```csharp
PatternDescriptor(string id, string displayName, string description, PatternKind kind, IReadOnlyList<string> tags, IReadOnlyList<string> requires, IReadOnlyList<string> conflictsWith, IReadOnlyDictionary<string, string> metadata)
```

Creates a pattern descriptor.

Parameters:
- `id`: The stable pattern identifier.
- `displayName`: The human-readable pattern name.
- `description`: The pattern description.
- `kind`: The category of the pattern.
- `tags`: The tags associated with the pattern.
- `requires`: The pattern identifiers required by this pattern.
- `conflictsWith`: The pattern identifiers that conflict with this pattern.
- `metadata`: Optional pattern metadata.

#### Properties

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

<a id="member-f-cephalon-abstractions-patterns-patternkind-composition"></a>

##### `Composition`

```csharp
const PatternKind Composition
```

Identifies a composition pattern.

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

<a id="member-m-cephalon-abstractions-technologies-technologydescriptor-ctor-system-string-system-string-system-string-cephalon-abstractions-technologies-technologykind-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `TechnologyDescriptor`

```csharp
TechnologyDescriptor(string id, string displayName, string description, TechnologyKind kind, IReadOnlyList<string> tags, IReadOnlyList<string> requiresPatterns, IReadOnlyList<string> requiresTransports, IReadOnlyList<string> requiresTechnologies, IReadOnlyList<string> conflictsWith, IReadOnlyList<string> packageHints, IReadOnlyList<string> guidance, IReadOnlyDictionary<string, string> metadata)
```

Creates a technology descriptor.

Parameters:
- `id`: The stable technology identifier.
- `displayName`: The human-readable technology name.
- `description`: The technology description.
- `kind`: The category of the technology.
- `tags`: The tags associated with the technology.
- `requiresPatterns`: The pattern identifiers required by the technology.
- `requiresTransports`: The transport identifiers required by the technology.
- `requiresTechnologies`: The technology identifiers required by the technology.
- `conflictsWith`: The technology identifiers that conflict with the technology.
- `packageHints`: The companion-package hints associated with the technology.
- `guidance`: The guidance entries associated with the technology.
- `metadata`: Optional technology metadata.

#### Properties

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
