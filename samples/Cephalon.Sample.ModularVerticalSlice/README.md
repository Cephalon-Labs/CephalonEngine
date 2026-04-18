# Cephalon Sample: Modular Vertical Slice

This sample is the vertical-slice blueprint baseline for Cephalon.

It carries the same narrow phase-8 starter contract as the other shipped blueprint samples: canonical `Engine` ids, structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections, plus low-ceremony `Sfid` id generation and `Cephalon.Audit` wiring.
Its public REST boundary is also behavior-backed: the starter module owns routes through `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` and `MapProfile<TBehavior>()`, matching the shipped `cephalon-slice` starter baseline.

The sample settings live in `modular-vertical-slice.settings.json`.

## Run from source

```powershell
dotnet run --project samples/Cephalon.Sample.ModularVerticalSlice
```

Inspect the URL printed by ASP.NET Core and then visit:

- `/engine`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/engine/technology-surfaces`
- `/engine/diagnostics`
- `/health`
- `/health/ready`
- `/scalar`

This sample is intentionally lighter than the modular monolith compose stack. It exists to show the module-plus-slice shape and the low-ceremony phase-8 starter defaults without extra infrastructure.
