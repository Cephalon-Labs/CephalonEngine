## Summary
Keep the showcase hosting verification aligned with Cephalon's settled module-owned REST contract by making the orders REST tests version-aware, separating module-owned REST assertions from the generic behavior transport matrix, and keeping the showcase test harness bound to the real sample project content root.

## Why
`ENG-058-T53`, `ENG-058-T58`, and `ENG-058-T59` tightened the public REST and versioning story: module major versions now matter, module-owned REST is the public REST/OpenAPI/Scalar surface, and the generic behavior transport adapters stay non-REST. The showcase hosting tests were still hardcoding `/api/v1/showcase/orders` even though `OrdersModule` is now `2.0.0`, and the transport-projection coverage was still expecting `http.rest` to appear inside the generic behavior transport matrix instead of validating public REST through the separate `restOperations` catalog.

## Scope
- point the showcase hosting harness at the real `samples/Cephalon.Sample.Showcase` content root under test-server runs
- let version-sensitive showcase REST assertions derive their route prefix from `OrdersModule.Descriptor.Version` instead of hardcoding `v1`
- update the transport-projection coverage so `behaviors[].transportIds` remains the non-REST adapter matrix while `restOperations` is the module-owned public REST answer
- keep the existing dirty showcase module work intact without reverting unrelated changes
- refresh project memory plus roadmap/backlog tracking so the test-contract follow-through is explicit

## Acceptance Criteria
- the showcase orders REST tests continue to pass when the orders module major version changes, without hand-editing every route literal
- the showcase transport projection no longer treats module-owned REST as a generic behavior transport id
- the showcase test harness loads split configuration from the real sample project content root instead of relying on transitive test-output copies
- showcase hosting coverage stays green with the current `OrdersModule` `2.0.0` contract

## Verification
- `dotnet test tests/Cephalon.Tests.Hosting/Cephalon.Tests.Hosting.csproj --filter "FullyQualifiedName~Cephalon.Tests.Hosting.ShowcaseSampleHostingTests" -v minimal --no-restore`

## Relationship
- follow-up to #321
- same ENG-058 track parent: #155
