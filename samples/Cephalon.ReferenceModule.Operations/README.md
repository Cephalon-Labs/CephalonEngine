# Cephalon.ReferenceModule.Operations

Reference module package for Cephalon authoring.

## What it demonstrates

- `ModuleDescriptor` and package metadata
- dependency-free service registration
- capability registration
- deterministic lifecycle state updates
- package-owned localized text
- REST contribution through `IRestModule`

## How to load it

Register the module package through assembly discovery:

```json
{
  "Engine": {
    "Discovery": {
      "Assemblies": [ "Cephalon.ReferenceModule.Operations" ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Or add it directly in code:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddModule(new OperationsModule());
});
```
