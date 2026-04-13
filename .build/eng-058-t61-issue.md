## Summary
Ship the Step 3 REST-authoring follow-through by adding metadata-only behavior REST profiles in `Cephalon.Behaviors.Http`, build-time diagnostics in `Cephalon.Behaviors.SourceGen`, and source-generated REST profile hints that prepare future module-owned shorthand projection without publishing public REST directly from behaviors.

## Why
Cephalon already settled that public REST stays module-owned and that lower-ceremony REST should arrive as metadata-first projection material rather than direct behavior-owned route activation. The repo still lacked the build-time contract for that future path. Behavior authors had no official way to declare a candidate REST method, relative route pattern, or API version for future generated module projections, and the source generator could not validate or emit any REST-profile hints. That left the strategy documented but not yet embodied in code.

## Scope
- add `BehaviorRestMethod`, `BehaviorRestProfileAttribute`, and `BehaviorRestProfileDescriptor` to `Cephalon.Behaviors.Http`
- keep the REST profile contract metadata-only so it does not publish public REST routes by itself
- extend `BehaviorSourceGenerator` with REST-profile extraction, `ABT0015` through `ABT0018`, and source-generated `GetRestProfiles()` hints
- update the REST-module guidance in source-generated diagnostics and runtime validation messages so it points at `RestBehaviorModuleBase.ConfigureRestBehaviors(...)`
- cover the new generator and package-surface behavior with targeted tests
- update the architecture strategy, component docs, project memory, roadmap, backlog, and project tracking

## Acceptance Criteria
- behavior authors can declare one metadata-only candidate REST profile through `BehaviorRestProfileAttribute`
- build-time diagnostics fail fast when a REST profile omits a method, omits a relative pattern, uses a non-positive API version, or uses a pattern that does not match the current module-owned DSL shape
- source-generated output emits REST profile hints for valid behavior profiles without publishing public REST routes
- repo-facing docs explain that REST profiles are only future projection metadata and do not override module ownership or host OpenAPI publication policy

## Verification
- `dotnet test tests/Cephalon.Tests.Composition/Cephalon.Tests.Composition.csproj --filter "FullyQualifiedName~BehaviorSourceGeneratorTests" -v minimal`
- `dotnet test tests/Cephalon.Tests.Tooling/Cephalon.Tests.Tooling.csproj --filter "FullyQualifiedName~PackageSurfaceTests.BehaviorsHttpAssemblyExposesOnlyTheDocumentedContractSurface" -v minimal`

## Relationship
- follow-up to #322
- same ENG-058 track parent: #155
