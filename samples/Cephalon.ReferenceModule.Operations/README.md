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

## From a published artifact

To prove the independent package flow outside the repo-local assembly path:

```powershell
dotnet pack ./samples/Cephalon.ReferenceModule.Operations/Cephalon.ReferenceModule.Operations.csproj `
  -c Release `
  -o ./artifacts/reference-packages

cephalon package stage `
  --package ./artifacts/reference-packages/Cephalon.ReferenceModule.Operations.1.0.0.nupkg `
  --output ./plugins/reference-operations
```

Then point `Engine:Discovery` at the staged package directory:

```json
{
  "Engine": {
    "Discovery": {
      "PackageDirectories": [
        {
          "Path": "plugins",
          "IncludeSubdirectories": true
        }
      ]
    },
    "PackagePolicy": {
      "AllowAssemblyPathPackages": false,
      "RequireVersion": true,
      "RequireMinimumEngineVersion": true,
      "RequireSupportedTargetFrameworks": true,
      "RequirePublisherId": true
    },
    "Trust": {
      "RequireTrustedPackages": true,
      "TrustedPublishers": [ "cephalon-labs" ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

With the host running, inspect `/api/operations/status`, `/engine/packages`, `/engine/package-policy`, `/engine/trust-policy`, and `/engine/snapshot`.
