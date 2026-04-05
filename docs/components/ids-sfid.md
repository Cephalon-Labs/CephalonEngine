# Cephalon.Ids.Sfid

`Cephalon.Ids.Sfid` is the official `Sfid.Net`-backed id-strategy companion package for Cephalon.

## What it owns

- wraps the official `Sfid.Net` generator behind `Cephalon.Abstractions.Ids.IIdGenerator`
- exposes the official `SfidNet.Abstractions.ISfidGenerator` to companion packs that need package-native typed-id integration
- maps Cephalon runtime selection through `AppProfile.Data.IdGenerator` to the `sfid` strategy
- reads optional generator topology from `Engine:Data:Ids:Sfid`
- keeps Snowflake/Sfid implementation details out of `Cephalon.Engine` and `Cephalon.Abstractions`

## Main surfaces

- `Configuration/SfidIdOptions.cs`
- `Registration/SfidEngineBuilderExtensions.cs`

## How it fits

This package is the first concrete slice of the phase-8 id strategy story. `Cephalon.Abstractions` stays host-agnostic through `IIdGenerator`, `Cephalon.Engine` continues to surface the selected id strategy through `AppProfile.Data.IdGenerator`, and this pack supplies the actual generator implementation by delegating to the official `Sfid.Net` package instead of creating a Cephalon-specific Snowflake runtime.

The pack is intentionally small. It gives Cephalon one truthful id baseline now, while also exposing the official generator interface so `Cephalon.Data.EntityFramework` can light up `Sfid.EntityFramework` conventions and save-time key assignment without duplicating Snowfake logic in Cephalon code.

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
- [Operations](../operations.md)
