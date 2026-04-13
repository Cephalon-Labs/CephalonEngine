## Summary
Harden ASP.NET Core hosting determinism around Cephalon's versioned public route surface by making operator endpoint service binding explicit, isolating hosting tests from transitive sample configuration, and keeping showcase route assertions aligned with module-owned REST versioning.

## Why
The current hosting slice exposed three coupled problems. First, generic ASP.NET Core hosting tests were accidentally inheriting split `Configurations/**` content from transitive sample references, which changed selected transports and module discovery in scenarios that were meant to stay isolated. Second, the showcase hosting harness was relying on copied test-output configuration instead of the real sample content root. Third, the showcase orders tests were still hardcoding `/api/v1/showcase/orders` even though `OrdersModule` now declares version `2.0.0`, while the transport projection test still treated module-owned REST as a generic behavior transport instead of a separate public REST surface.

## Scope
- make `/engine/*` Minimal API service bindings explicit where .NET 10 parameter inference should not guess between DI and request payload binding
- keep generic hosting tests isolated from transitive sample `Configurations/**` output
- let the showcase host builder accept an explicit content root so sample-host tests load the real project configuration graph
- make showcase orders REST assertions derive their route prefix from `OrdersModule` metadata instead of hardcoding `v1`
- align showcase transport projection expectations with the settled contract that module-owned REST is reported separately from generic behavior transport ids
- update project memory, backlog, roadmap, and project tracking so the determinism and version-aware test contract stay explicit

## Acceptance Criteria
- targeted ASP.NET Core hosting tests no longer fail because transitive sample configuration silently selected extra transports or discovery assemblies
- the showcase hosting harness resolves split configuration from the real `samples/Cephalon.Sample.Showcase` content root instead of copied output files
- showcase orders tests continue to target the correct public REST version when the module major version changes
- showcase transport projection coverage no longer expects `http.rest` inside the generic behavior transport list for module-owned REST endpoints

## Verification
- `dotnet test tests/Cephalon.Tests.Hosting/Cephalon.Tests.Hosting.csproj --filter "FullyQualifiedName~Cephalon.Tests.Hosting.AspNetCoreHostingTests" -v minimal --no-restore`
- `dotnet test tests/Cephalon.Tests.Hosting/Cephalon.Tests.Hosting.csproj --filter "FullyQualifiedName~Cephalon.Tests.Hosting.ShowcaseSampleHostingTests" -v minimal --no-restore`

## Relationship
- follow-up to #321
- same ENG-058 track parent: #155
