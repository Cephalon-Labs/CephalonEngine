# Cephalon.Engine.SourceGen

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Engine.SourceGen` is the compiler-only source generator that emits module discovery metadata for Cephalon modules.

See also: [Cephalon.Engine](engine.md), [Cephalon.Abstractions](abstractions.md), [Module authoring](../module-authoring.md), [Package publishing](../package-publishing.md), and [Trim / Native AOT / single-file hazard inventory](../trim-aot-hazard-inventory.md).

## What it owns

- compile-time discovery of eligible `IModule` implementations in the consuming assembly
- generated `ModuleDiscoveryDescriptor` registrations into `ModuleDiscoveryRegistry`
- keeping `AddModulesFromAssembly(...)`, package discovery, and `Engine:Discovery:Assemblies` usable without runtime assembly-type scans
- compiler-only deployment-mode posture: `IsTrimmable=false`, `IsAotCompatible=false`, `PublishTrimmed=false`, `PublishAot=false`, and `PublishSingleFile=false`
- publish-probe isolation through `TreatAsLocalProperty` on the project and `CephalonCompilerOnlyProjectReferenceGlobalPropertiesToRemove` on compiler-only analyzer references, covering trim, Native AOT, single-file, self-contained, and RID globals

## Main surfaces

- `ModuleSourceGenerator.cs`
- generated `CephalonGeneratedModuleDiscovery_<AssemblyName>.g.cs` module initializer output
- `AnalyzerReleases.Shipped.md`
- `AnalyzerReleases.Unshipped.md`

## Authoring Contract

Module projects that should be found through assembly, package, or configuration-driven discovery must reference this package as an analyzer. The generator emits descriptors for non-abstract, non-generic module classes with an accessible parameterless constructor.

Modules that require custom factories or runtime state during construction should be registered explicitly through `AddModule(...)` or package registration code instead of relying on generated assembly discovery.

## Boundaries

- does not ship runtime code into published applications
- does not replace `Cephalon.Engine`; it feeds the engine's descriptor-backed discovery path
- does not generate behavior, REST, topology, or transport metadata; those stay in their owning source-generator or runtime packages
