# Cephalon Template Pack

`Cephalon.TemplatePack` provides `dotnet new` templates for the shipped Cephalon v1 blueprints:

- `cephalon-monolith`
- `cephalon-slice`
- `cephalon-microservice`
- `cephalon-module`
- `cephalon-rest-module`

These templates are the lightweight installation surface for teams that want a fast starting point without cloning the full Cephalon repository.

## Local packaging workflow

```powershell
dotnet pack templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj -c Release -o artifacts/template-pack
dotnet new install .\artifacts\template-pack\Cephalon.TemplatePack.0.1.0-preview.nupkg
```

## Create an app

```powershell
dotnet new cephalon-monolith -n Acme.Store
dotnet new cephalon-slice -n Acme.Store
dotnet new cephalon-microservice -n Acme.Customers
dotnet new cephalon-module -n Acme.Orders.Module
dotnet new cephalon-rest-module -n Acme.Orders.RestModule
```

## Notes

- The templates mirror the current shipped blueprint set in the repository.
- For richer customization, `Cephalon.Cli` and `Cephalon.Scaffolding` remain the more expressive generation path.
- Generated projects assume you will restore Cephalon packages from the feed or local package source you target.

## Compatibility expectations

- keep the template-pack version, starter project target frameworks, and starter `cephalon.package.json` files aligned with the current Cephalon release baseline
- keep the module starters aligned with the same manifest contract described in `docs/module-authoring.md`
- when blueprint, transport, version, or docs-hosting behavior changes, keep `Cephalon.TemplatePack`, `Cephalon.Cli`, and `Cephalon.Scaffolding` aligned rather than letting one generation path drift
- use `docs/compatibility.md` as the maintainer checklist for cross-surface compatibility changes
