# Cephalon Sample: Microservice

This sample is the single-service microservice blueprint baseline for Cephalon.

It carries the same narrow phase-8 starter contract as the other shipped blueprint samples: canonical `Engine` ids, structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections, plus low-ceremony `Sfid` id generation and `Cephalon.Audit` wiring.

The sample settings live in `microservice.settings.json`.

## Run from source

```powershell
dotnet run --project samples/Cephalon.Sample.Microservice
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

This sample stays focused on one service boundary rather than a coordinated suite. Use it when you want the smallest truthful reference for Cephalon's microservice host shape plus the shipped phase-8 starter defaults.
