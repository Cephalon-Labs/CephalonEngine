# Cephalon.Cli

`Cephalon.Cli` is the user-facing command-line surface for Cephalon scaffolding and documentation workflows.

Stable public surface:

- `CliApplication`

Internal command pipeline:

- `Commands/*`
- `Console/CliConsole.cs`
- `Commands/DocumentationLauncher.cs`

## What it owns

- blueprint-driven app generation
- optional reference-doc publishing
- hosted reference-doc configuration updates
- hosted reference-doc validation
- optional open-in-browser flows for local or hosted docs

## Main implementation surfaces

- `CliApplication.cs`
- `Commands/NewAppCommand.cs`
- `Commands/DocsPublishCommand.cs`
- `Commands/DocsEnableHostingCommand.cs`
- `Commands/DocsValidateHostingCommand.cs`
- `Commands/DocumentationLauncher.cs`
- `Console/CliConsole.cs`

## Source structure

- `Commands`
- `Console`
- `Properties`

## How it fits

This package is the shell over engine, scaffolding, and optional reference-doc services. It should stay aligned with engine semantics rather than inventing its own blueprint or documentation behavior.

For package-surface hardening, the command handlers, parsed option objects, console abstraction, and browser launcher are implementation details. External callers should integrate through `CliApplication` rather than binding directly to individual command types.

## Related docs

- [Reference docs publishing](../reference-docs.md)
- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
