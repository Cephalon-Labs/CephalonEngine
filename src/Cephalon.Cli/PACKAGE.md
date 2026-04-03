# Cephalon CLI

`Cephalon.Cli` is the user-facing command-line shell for Cephalon blueprint generation and reference-doc workflows.

## Install

Install from a packaged artifact:

```powershell
dotnet tool install --tool-path .\.tools\cephalon Cephalon.Cli `
  --add-source .\artifacts\packages-release `
  --ignore-failed-sources `
  --no-cache
```

Or update an existing local tool-path install:

```powershell
dotnet tool update --tool-path .\.tools\cephalon Cephalon.Cli `
  --add-source .\artifacts\packages-release `
  --ignore-failed-sources `
  --no-cache
```

## Usage

```powershell
.\.tools\cephalon\cephalon --help
.\.tools\cephalon\cephalon new Acme.Store --blueprint Microservice
.\.tools\cephalon\cephalon docs publish --root .
```

## Docs

- [Docs hub](https://github.com/Cephalon-Labs/CephalonEngine/tree/master/docs)
- [App models](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/app-models.md)
- [Reference docs publishing](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/reference-docs.md)
