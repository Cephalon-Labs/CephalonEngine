# Cephalon.Behaviors.SourceGen

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Behaviors.SourceGen` is the M5 compile-time tooling layer of the Adaptive Behavior Topology (ABT).
It provides a Roslyn incremental source generator and diagnostic analyzer that validate behavior authoring
conventions at build time and produce a compile-time-known registration hint file.

## What it owns

- **BehaviorSourceGenerator** — combined Roslyn `IIncrementalGenerator` + diagnostic analyzer
  - Uses `ForAttributeWithMetadataName` for efficient incremental processing
  - Emits `BehaviorRegistrationHints.g.cs` listing all discovered `[AppBehavior]` IDs
  - Emits `BehaviorAutoRegistration.g.cs` for generated module registration, zero-reflection DI/type registration, execution-slot descriptors, and pre-built topology descriptors when compile-time extraction succeeds or attribute-only topology can be synthesized
  - Emits a `RegisterGeneratedBehaviors()` module initializer that registers generated hints with `BehaviorGeneratedModuleRegistry` so `Cephalon.Behaviors` does not reflect over generated carrier methods
  - Emits `GetExecutionSlots()` with closed `BehaviorGeneratedExecutionSlotDescriptor` / `BehaviorExecutionSlot.For<TBehavior, TInput, TOutput>()` calls so `Cephalon.Behaviors` can prefer source-generated dispatch startup over open-generic slot reflection
  - Emits closed `DurableExecutionSlot.For<TBehavior, TInput, TState, TOutput>()` registrations when a behavior implements `IDurableExecution<TInput, TState, TOutput>` so `Cephalon.Behaviors.Patterns` can execute durable workflows and project durable metadata without runtime open-generic fallback
  - Emits closed `SagaChoreographyRuntimeSlot.For<TBehavior, TInput, TResult>(...)` registrations when a behavior declares `saga-choreography` topology and references the pattern runtime slot, so `Cephalon.Behaviors.Patterns` can project choreography authoring/result-shape metadata without runtime interface-shape inspection
  - Emits source-generated metadata-only REST profile hints through `GetRestProfiles()` when behaviors declare valid `BehaviorRestProfileAttribute` metadata, including descriptor-backed scalar/object input contracts for explicit binding validation, then registers those hints through a module initializer and `BehaviorRestGeneratedProfileRegistry` so runtime profile consumption does not reflectively find generated REST carrier methods or inspect behavior/input types for profile binding shape
  - Extracts compile-time topology from supported `ConfigureTopology(...)` fluent chains for pattern, transports, feature flags, and literal `WithApiSurface(...)` overrides; when no static topology exists, it emits attribute-only descriptors from unambiguous `[BehaviorAllowedPatterns]` / `[BehaviorAllowedTransports]` metadata, with transport-only declarations resolving to `direct`
- Reports ABT0010–ABT0027 diagnostics on invalid behavior declarations, metadata-only REST profile hints, malformed REST profile placeholder syntax, explicit REST binding metadata, and invalid preserved implicit query-fallback authoring before `GetRestProfiles()` is generated

## Diagnostic rules

| Code | Severity | Description |
|------|----------|-------------|
| ABT0010 | Error | `[AppBehavior]` class does not implement `IAppBehavior<TIn, TOut>` |
| ABT0011 | Error | `[AppBehavior]` ID is null or empty |
| ABT0012 | Error | `[AppBehavior]` class is abstract |
| ABT0013 | Error | `[AppBehavior]` class is static |
| ABT0014 | Error | REST is declared in behavior topology instead of a module-owned REST surface |
| ABT0015 | Error | `[BehaviorRestProfile]` does not select a supported REST method |
| ABT0016 | Error | `[BehaviorRestProfile]` uses an empty relative pattern |
| ABT0017 | Error | `[BehaviorRestProfile]` uses a non-positive `ApiVersionMajor` |
| ABT0018 | Error | `[BehaviorRestProfile]` uses a relative pattern that does not start with `/` |
| ABT0019 | Error | `[BehaviorRestBinding]` does not name a target input property |
| ABT0020 | Error | `[BehaviorRestBinding]` does not select a supported binding source |
| ABT0021 | Error | `[BehaviorRestBinding]` metadata is declared for a scalar input or an input without public readable properties |
| ABT0022 | Error | `[BehaviorRestBinding]` targets an input property that does not exist |
| ABT0023 | Error | `[BehaviorRestBinding]` declares the same input property more than once |
| ABT0024 | Error | `[BehaviorRestBinding]` uses `Body` on a REST method that does not accept a request body |
| ABT0025 | Error | `[BehaviorRestBinding]` uses a route placeholder that is not declared in `[BehaviorRestProfile(...)]` |
| ABT0026 | Error | `[BehaviorRestProfile]` uses malformed route placeholder syntax such as unbalanced or empty `{...}` segments |
| ABT0027 | Error | `[BehaviorRestProfile(PreserveImplicitQueryFallback = true)]` is declared without any explicit `[BehaviorRestBinding]` metadata |

## Generated output

`BehaviorRegistrationHints.g.cs` and `BehaviorAutoRegistration.g.cs` are emitted into the consuming project at build time:

```csharp
// <auto-generated/>
// Generated by Cephalon.Behaviors.SourceGen — do not edit.

namespace Cephalon.Behaviors.Generated;

internal static class BehaviorRegistrationHints
{
    internal static readonly IReadOnlyList<string> BehaviorIds =
    [
        "order.place",
        "order.get",
    ];
}
```

When the generator can statically understand `ConfigureTopology(...)`, or when a behavior uses an
unambiguous attribute-only topology declaration, it also emits zero-reflection registration and
topology data, including literal `WithApiSurface(...)` overrides:

```csharp
internal static class BehaviorAutoRegistration
{
    [ModuleInitializer]
    internal static void RegisterGeneratedBehaviors()
    {
        BehaviorGeneratedModuleRegistry.Register(
            typeof(BehaviorAutoRegistration).Assembly,
            new BehaviorGeneratedModuleRegistration(
                Register,
                GetExecutionSlots(),
                GetTopologyDescriptors(),
                GetBehaviorsNeedingRuntimeTopology()));
    }

    internal static void Register(IServiceCollection services)
    {
        if (BehaviorImplementationRegistration.TryRegister(
            services,
            "catalog.lookup",
            typeof(CatalogLookupBehavior),
            BehaviorIdempotencyMode.Unknown))
        {
            services.TryAddTransient(typeof(CatalogLookupBehavior));
        }

        services.Add(ServiceDescriptor.Singleton(
            typeof(DurableExecutionSlot),
            DurableExecutionSlot.For<OrderWorkflowBehavior, OrderWorkflowInput, OrderWorkflowState, OrderWorkflowOutput>()));
    }

    internal static IReadOnlyList<BehaviorGeneratedExecutionSlotDescriptor> GetExecutionSlots()
    {
        return
        [
            new BehaviorGeneratedExecutionSlotDescriptor(
                "catalog.lookup",
                typeof(CatalogLookupBehavior),
                BehaviorExecutionSlot.For<CatalogLookupBehavior, CatalogLookupInput, CatalogLookupResult>())
        ];
    }

    internal static IReadOnlyList<BehaviorTopologyDescriptor> GetTopologyDescriptors()
    {
        return
        [
            new BehaviorTopologyDescriptor(
                "catalog.lookup",
                "cqrs",
                new[] { "http.jsonrpc", "http.sse" },
                apiSurface: new BehaviorApiSurfaceDescriptor("catalog/items", "lookup"))
        ];
    }
}
```

When a behavior also declares a valid metadata-only REST profile, the generated registration type
now emits future-facing REST profile hints and registers them through the shared REST profile
registry without publishing any public REST routes:

```csharp
internal static class BehaviorAutoRegistration
{
    [ModuleInitializer]
    internal static void RegisterRestProfiles()
    {
        BehaviorRestGeneratedProfileRegistry.Register(
            typeof(BehaviorAutoRegistration).Assembly,
            GetRestProfiles(),
            GetRestProfileBehaviorTypes());
    }

    internal static IReadOnlyList<BehaviorRestProfileDescriptor> GetRestProfiles()
    {
        return
        [
            new BehaviorRestProfileDescriptor(
                "catalog.lookup",
                BehaviorRestMethod.Get,
                "/{itemId}",
                2,
                [
                    new BehaviorRestBindingDescriptor(
                        "ItemId",
                        BehaviorRestBindingSource.Route,
                        "itemId")
                ])
            {
                InputContract = new BehaviorRestInputContractDescriptor(
                    typeof(CatalogLookupInput),
                    false,
                    [
                        new BehaviorRestInputPropertyDescriptor(
                            "ItemId",
                            typeof(string))
                    ])
            }
        ];
    }

    internal static IReadOnlyList<BehaviorRestProfileBehaviorTypeDescriptor> GetRestProfileBehaviorTypes()
    {
        return
        [
            new BehaviorRestProfileBehaviorTypeDescriptor("catalog.lookup", typeof(CatalogLookupBehavior))
        ];
    }
}
```

For REST profile methods and binding sources, the generator now validates against the stable
wire-name vocabularies instead of hardcoding enum member names: `BehaviorRestMethod` uses `get`,
`post`, `put`, `patch`, and `delete`, while `BehaviorRestBindingSource` uses `route`, `query`,
`header`, and `body`. Generated `GetRestProfiles()` hints still emit the resolved enum member
names, so future package versions can rename those members without breaking valid metadata as long
as the stable wire-name contracts stay intact.

## Integration

The generator is automatically applied when `Cephalon.Behaviors` is referenced. No additional setup required.
The `Cephalon.Behaviors` package references `Cephalon.Behaviors.SourceGen` as an analyzer, so the generator
and diagnostics activate for any project that references `Cephalon.Behaviors`.
Those compiler-only project references remove host publish-mode globals through
`CephalonCompilerOnlyProjectReferenceGlobalPropertiesToRemove`, and the source-generator project declares
`TreatAsLocalProperty` for trim, Native AOT, single-file, self-contained, and RID globals. This keeps
representative publish probes focused on runtime behavior instead of letting app publish settings leak into
the `netstandard2.0` compiler-only generator project.

At runtime, `Cephalon.Behaviors` consumes `BehaviorGeneratedModuleRegistry` entries populated by the
generated module initializer instead of reflectively locating generated carrier methods through
`ContainsBehaviorsAttribute.RegistrationType`. The `GetExecutionSlots()` hints remove
`BehaviorExecutionSlot.ForType(...)` open-generic slot materialization from the normal source-generated
dispatch path. The generated `DurableExecutionSlot` service registrations likewise remove durable
open-generic adapter materialization from the normal source-generated durable path used by
`Cephalon.Behaviors.Patterns`. The generated `SagaChoreographyRuntimeSlot` registrations remove saga
choreography runtime-catalog shape inspection from the normal source-generated choreography path used
by `Cephalon.Behaviors.Patterns`. The durable, choreography, runtime assembly-scan, behavior
dispatch, behavior implementation-registry, and REST manual/profile route-contract fallbacks are now
removed; these fast paths return the behavior package family to clean-baseline absence from the
active first-party deployment-mode hazard table, but they still do not make the packages trim/AOT
claimed until scoped package claims and publish-probe policy are promoted deliberately.

Compile-time topology extraction intentionally stays conservative. Literal `WithApiSurface(...)`
arguments are supported, while more complex expressions are emitted as unsupported generated
topology declarations. `Cephalon.Behaviors` now fails fast for those declarations during generated
auto-registration instead of invoking `ConfigureTopology(...)` reflectively at runtime; move the
topology to a source-generator-supported fluent chain, unambiguous `[BehaviorAllowedPatterns]` /
`[BehaviorAllowedTransports]` metadata, or explicit module/fluent registration. Public REST is
module-owned and therefore sits outside the
behavior source-generator topology model; `ABT0014` now rejects `http.rest` and `ViaHttpRest(...)`
so authors map REST in a module with `RestBehaviorModuleBase.ConfigureRestBehaviors(...)`, or with
manual `MapBehaviorRestGroup(...)` wiring when they intentionally stay on the low-level REST module
path.
`BehaviorRestProfileAttribute` plus optional repeated `BehaviorRestBindingAttribute` declarations
are now the shipped metadata-only bridge for future low-ceremony REST: the generator validates the
core profile shape plus explicit binding metadata, preserved implicit query-fallback authoring, and
emits `GetRestProfiles()` hints, including explicit binding descriptors and
`preserveImplicitQueryFallback: true` when present, descriptor-backed input contract metadata for
runtime binding validation, descriptor-based `GetRestProfileBehaviorTypes()` hints for the generated
module-owned shorthand path, and `GetBehaviorContracts()` endpoint-contract metadata consumed by
manual/profile REST routes. A generated module initializer registers REST profiles into
`BehaviorRestGeneratedProfileRegistry` and endpoint contracts into `BehaviorContractRegistry`, but
that metadata still does not publish public REST routes by itself and does not override host OpenAPI
document publication policy.
`Cephalon.Behaviors.Http` now consumes those hints through the explicit module-owned
`MapProfile<TBehavior>()`, `MapGeneratedProfiles(...)`, and
`IRestBehaviorModuleBuilder.MapGeneratedProfileGroups(...)` shorthands. `MapProfile<TBehavior>()`
now requires generated or explicitly registered profile descriptors plus behavior-type hints for
the explicitly targeted behavior type, while generated-profile group mapping requires the
source-generated registry hints for the owning module assembly and does not scan that assembly for
attributed behavior types.
Generated REST profile and binding hints now resolve their enum member names from the actual
attribute arguments instead of assuming fixed numeric ordinals.
The build now rejects unsupported binding sources, malformed route placeholder syntax, missing or
duplicate input-property targets, scalar-input misuse, body-binding verb restrictions,
route-placeholder mismatches, and preserved implicit-query fallback without any explicit bindings
earlier, while `Cephalon.Behaviors.Http` still re-checks the same contract when generated or
explicitly registered descriptors are consumed through `BehaviorRestInputContractDescriptor`
metadata and uses `BehaviorContractDescriptor` metadata for endpoint input/output contracts;
generated-profile mapping uses the registry hints directly. Runtime
normalization still lets ASP.NET Core route parsing stay authoritative for the final route-shape
truth even after the generator moves the most common placeholder-shape mistakes and preserved-
fallback authoring errors to compile time.
Likewise, explicit module ownership through `IBehaviorOwnerModule`, `BehaviorModuleBase`, or
`RestBehaviorModuleBase` remains a runtime-composition concern rather than a source-generated
topology concern: the generator still focuses on behavior shape and topology, while the engine owns
which module claims each behavior.
When a behavior has no `ConfigureTopology(...)` method but does declare unambiguous allowlist
metadata, the generator now emits the attribute-only baseline descriptor from those attributes.
Zero or one allowed pattern is supported; transport-only declarations resolve to `direct`. If
multiple allowed patterns are declared, runtime resolution still fails fast until another topology
source selects one explicitly.

## Status

> Status: ✅ Shipped — targeted source-generator tests 38/38

## Related components

- `Cephalon.Behaviors` — dispatcher, catalog, resolver (M1)
- `Cephalon.Behaviors.Http` — HTTP transport bindings (M2)
- `Cephalon.Behaviors.Messaging` — messaging transport bindings (M3)
- `Cephalon.Behaviors.Patterns` — pattern execution strategies (M4)
