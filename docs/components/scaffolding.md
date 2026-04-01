# Cephalon.Scaffolding

`Cephalon.Scaffolding` turns the app-model scaffold contract into concrete files and folders.

## What it owns

- scaffold request parsing for generation-time decisions
- rendered solution, project, file, and folder models
- package version catalog used by generated output
- filesystem writing for generated scaffolds

## Main surfaces

- `Generation/ScaffoldGenerator.cs`
- `Generation/ScaffoldRequest.cs`
- `Generation/RenderedScaffold.cs`
- `Generation/RenderedProject.cs`
- `Generation/RenderedFile.cs`
- `Generation/PackageVersionCatalog.cs`
- `IO/FileSystemScaffoldWriter.cs`

## Source structure

- `Generation`
- `IO`

## How it fits

This package consumes the same `AppProfile.Scaffold` model exposed by the runtime. It is the shared implementation behind CLI generation and future template or automation workflows.

## Related docs

- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
