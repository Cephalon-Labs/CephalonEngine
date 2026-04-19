# Cephalon.Eventing.Behaviors

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Eventing.Behaviors)
## Namespaces

- `Cephalon.Eventing.Behaviors.Registration`

<a id="namespace-cephalon-eventing-behaviors-registration"></a>

## Namespace Cephalon.Eventing.Behaviors.Registration

<a id="type-cephalon-eventing-behaviors-registration-behavioreventingenginebuilderextensions"></a>

### `BehaviorEventingEngineBuilderExtensions`

Registers the optional behavior-to-eventing choreography bridge with an `EngineBuilder`.

#### Declaration
```csharp
public static class BehaviorEventingEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-eventing-behaviors-registration-behavioreventingenginebuilderextensions-addbehavioreventingbridge-cephalon-engine-composition-enginebuilder"></a>

##### `AddBehaviorEventingBridge`

```csharp
EngineBuilder AddBehaviorEventingBridge(this EngineBuilder builder)
```

Adds the explicit saga-choreography bridge that stages behavior publications through the shared `Cephalon.Eventing` publication path.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
