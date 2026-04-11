# Cephalon Samples

These samples are adoption-quality blueprint examples.

They now also serve as the narrow phase-8 starter baseline for the shipped blueprint shapes: canonical `Engine` ids, structured phase-8 config sections, and a low-ceremony `Sfid` plus `Audit` path are already present so runtime introspection and starter guidance stay aligned.

- `Cephalon.ReferenceModule.Operations`: reference module package that demonstrates lifecycle, capability, localization, and REST contribution authoring
- `Cephalon.Sample.ModularMonolith`: module-first organization inside one ASP.NET Core host
- `Cephalon.Sample.ModularVerticalSlice`: feature-slice organization inside a bounded module
- `Cephalon.Sample.Microservice`: service-boundary example with explicit contracts
- `Cephalon.Sample.MicroserviceSuite`: coordinated multi-service sample with a shared foundation project, shared governance package, and separate catalog and orders services
- `Cephalon.Sample.Showcase`: the comprehensive engine prove-out host covering split configuration, transport/docs/operator surfaces, and migration-first runtime behavior

These are intentionally different from `playground/`.

- `playground/` is for freeform experimentation
- `samples/` is for showing the intended blueprint shape other teams should copy

The blueprint-shape starters now each carry their own README so the shipped phase-8 baseline stays visible at the sample boundary:

- `samples/Cephalon.Sample.ModularMonolith/README.md`
- `samples/Cephalon.Sample.ModularVerticalSlice/README.md`
- `samples/Cephalon.Sample.Microservice/README.md`
- `samples/Cephalon.Sample.Showcase/README.md`
