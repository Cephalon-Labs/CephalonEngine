# Cephalon Samples

These samples are adoption-quality blueprint examples.

They now also serve as the narrow phase-8 starter baseline for the shipped blueprint shapes: canonical `Engine` ids, structured phase-8 config sections, and a low-ceremony `Sfid` plus `Audit` path are already present so runtime introspection and starter guidance stay aligned. When a shipped sample exposes behavior-backed public REST, the public boundary is owned through `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` plus `MapProfile<TBehavior>()` so the sample baseline matches scaffolding and template starters.

- `Cephalon.ReferenceModule.Operations`: generic non-behavior `IRestModule` reference package that demonstrates lifecycle, capability, localization, and manual REST contribution authoring
- `Cephalon.Sample.ModularMonolith`: module-first organization inside one ASP.NET Core host with behavior-backed public REST starter ownership
- `Cephalon.Sample.ModularVerticalSlice`: feature-slice organization inside a bounded module with behavior-backed public REST starter ownership
- `Cephalon.Sample.Microservice`: service-boundary example with explicit contracts and behavior-backed public REST starter ownership
- `Cephalon.Sample.MicroserviceSuite`: coordinated multi-service sample with a shared foundation project, shared governance package, separate catalog and orders services, and the same behavior-backed public REST baseline per service
- `Cephalon.Sample.Showcase`: the comprehensive engine prove-out host covering split configuration, transport/docs/operator surfaces, and migration-first runtime behavior

These are intentionally different from `playground/`.

- `playground/` is for freeform experimentation
- `samples/` is for showing the intended blueprint shape other teams should copy

The blueprint-shape starters now each carry their own README so the shipped phase-8 baseline stays visible at the sample boundary:

- `samples/Cephalon.Sample.ModularMonolith/README.md`
- `samples/Cephalon.Sample.ModularVerticalSlice/README.md`
- `samples/Cephalon.Sample.Microservice/README.md`
- `samples/Cephalon.Sample.Showcase/README.md`
