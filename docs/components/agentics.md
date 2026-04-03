# Cephalon.Agentics

`Cephalon.Agentics` is the baseline technology pack for agentic workloads.

## What it owns

- agentic runtime options
- module and registration entry points for the `AgenticWorkloads` technology
- tool descriptors, registries, and catalogs
- orchestration-link validation for tool descriptors that point back to capabilities, execution graphs, or hosted executions
- runtime-surface contribution for introspection

## Main surfaces

- `Configuration/AgenticRuntimeOptions.cs`
- `Modules/AgenticsModule.cs`
- `Registration/AgenticEngineBuilderExtensions.cs`
- `Services/AgentToolDescriptor.cs`
- `Services/AgentToolRegistry.cs`
- `Services/AgentToolCatalog.cs`
- `Services/IAgentToolContributor.cs`
- `Services/IAgentToolCatalog.cs`
- `Services/AgenticsRuntimeSurfaceContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack is the reference pattern for future AI or agent runtime behavior in Cephalon. Modules can contribute tools without forcing the host to own the entire catalog, and those tools can now stay grounded in the existing runtime model by linking back to published capability keys, execution graphs, and hosted executions. The resulting operator-facing answer flows through `/engine/technology-surfaces` and `/engine/snapshot` instead of a separate agent-specific endpoint.

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
