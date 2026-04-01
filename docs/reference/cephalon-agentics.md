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

#### Properties

<a id="member-p-cephalon-agentics-configuration-agenticruntimeoptions-enableexecution"></a>

##### `EnableExecution`

```csharp
bool EnableExecution { get; set; }
```

Gets or sets a value indicating whether tool execution features are enabled.

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

<a id="member-m-cephalon-agentics-registration-agenticenginebuilderextensions-addagentics-cephalon-engine-composition-enginebuilder-system-action-1-cephalon-agentics-configuration-agenticruntimeoptions"></a>

##### `AddAgentics`

```csharp
EngineBuilder AddAgentics(this EngineBuilder builder, Action<AgenticRuntimeOptions> configure)
```

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

<a id="member-m-cephalon-agentics-services-agenttooldescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-system-string"></a>

##### `AgentToolDescriptor`

```csharp
AgentToolDescriptor(string id, string displayName, string description, IReadOnlyList<string> tags)
```

#### Properties

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

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable tool identifier.

<a id="member-p-cephalon-agentics-services-agenttooldescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the tool.

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
