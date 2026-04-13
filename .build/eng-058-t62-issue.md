## Summary
Ship the Step 4 REST-authoring follow-through by letting `RestBehaviorModuleBase` consume metadata-only behavior REST profiles through an explicit module-owned shorthand path such as `MapProfile<TBehavior>()`, while keeping public REST module-owned, introspectable, and governed by the same normalized projection/runtime-catalog pipeline.

## Why
`ENG-058-T61` established the behavior-authored metadata contract through `BehaviorRestProfileAttribute`, build-time diagnostics, and source-generated `GetRestProfiles()` hints, but that metadata still had no official consumption path. Cephalon therefore had the authoring contract without the low-ceremony module surface it was meant to unlock. The next bounded step is not convention-backed publication from auto-registered behaviors. The next bounded step is an explicit module-owned shorthand that consumes those hints through the existing `RestBehaviorModuleBase` projection pipeline so the public boundary stays deterministic, runtime-catalog truth stays aligned, and teams can write less route boilerplate for common cases.

## Scope
- add `MapProfile<TBehavior>()` overloads to `IRestBehaviorEndpointGroupBuilder`
- resolve behavior REST profiles through source-generated `GetRestProfiles()` hints first, with a bounded per-behavior attribute fallback when generated hints are unavailable
- keep profile consumption inside `Cephalon.Behaviors.Http` and avoid introducing a dependency from `Cephalon.Behaviors` back into HTTP concerns
- let profile metadata contribute only method, relative pattern, and optional candidate API major version while the owning module still controls group prefix, tags, and published OpenAPI documents
- seed a REST group version from profile metadata only when the group has no explicit `.ApiVersion(...)`, and fail fast when profiled behaviors in one group declare conflicting candidate versions
- keep runtime publication on the existing module-owned REST path, with `sourceKind = module-dsl` and additive runtime provenance that distinguishes profile-driven module shorthand from fully explicit DSL endpoints
- cover projection, composition, hosting, and package-surface behavior with targeted tests
- update component docs, module-authoring guidance, architecture strategy, project memory, roadmap, backlog, and GitHub tracking

## Acceptance Criteria
- `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` can expose a behavior through `group.MapProfile<TBehavior>()` without restating the HTTP method or relative pattern in module code
- `MapProfile<TBehavior>()` fails fast when the behavior does not declare a valid `BehaviorRestProfileAttribute`
- explicit module `.ApiVersion(...)` overrides any profile-declared candidate version, while conflicting profile-declared versions in one group fail fast until the module resolves them explicitly
- public REST route publication still requires explicit module consumption; profile metadata alone does not publish a public REST boundary
- `/engine/rest-endpoints` and `snapshot.RestEndpoints` continue to expose truthful public REST answers and now distinguish profile-driven module shorthand through additive runtime metadata

## Verification
- `dotnet test tests/Cephalon.Tests.Hosting/Cephalon.Tests.Hosting.csproj --filter "FullyQualifiedName~BehaviorRestProjectionTests|FullyQualifiedName~BehaviorRestRuntimeCatalogHostingTests" -v minimal`
- `dotnet test tests/Cephalon.Tests.Composition/Cephalon.Tests.Composition.csproj --filter "FullyQualifiedName~BehaviorOwnerModuleTests" -v minimal`
- `dotnet test tests/Cephalon.Tests.Tooling/Cephalon.Tests.Tooling.csproj --filter "FullyQualifiedName~PackageSurfaceTests.BehaviorsHttpAssemblyExposesOnlyTheDocumentedContractSurface" -v minimal`

## Relationship
- follow-up to #324
- same ENG-058 track parent: #155
