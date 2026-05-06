# Cephalon.ReferenceDocs

> **Maturity:** `M4` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.ReferenceDocs` is the optional repo-local publishing tool that turns compiled assemblies plus XML comments into browsable API reference output.

Stable public surface:

- `ReferenceDocsApplication`
- `ReferenceDocsGenerator`
- `ReferenceDocsRequest`
- `RenderedReferenceDocs`
- `ReferenceDocFile`
- `ReferenceDocsWriter`

Internal generation helpers:

- `DocumentationLoadContext`
- `ReferenceBrowserRenderer`

## What it owns

- loading compiled assemblies and XML docs for documentation generation
- Markdown page generation for assemblies, namespaces, types, and members
- browser UI rendering assets and machine-readable manifest generation
- filesystem writing for generated reference docs

## Main implementation surfaces

- `Generation/ReferenceDocsGenerator.cs`
- `Generation/ReferenceDocsRequest.cs`
- `Generation/ReferenceBrowserRenderer.cs`
- `Generation/DocumentationLoadContext.cs`
- `Generation/RenderedReferenceDocs.cs`
- `Generation/ReferenceDocFile.cs`
- `IO/ReferenceDocsWriter.cs`
- `ReferenceDocsApplication.cs`

## Source structure

- `Generation`
- `IO`

## How it fits

This package is not the source of truth for Cephalon's product documentation. Hand-authored `.md` files under `README.md` and `docs/` explain what Cephalon is, what it ships, and how to adopt it. `Cephalon.ReferenceDocs` exists so the same XML comments that power IntelliSense can also be published as optional reference output when the repository wants a browsable API artifact.

The ASP.NET Core host can then serve that generated output, and the CLI can publish, validate, and open it.

For package-surface hardening, the reusable library contract stays centered on request/generate/write flows. Assembly-load plumbing and browser-asset rendering stay internal so future docs-site changes do not widen the public API unnecessarily.

## Deployment-mode posture

`Cephalon.ReferenceDocs` is a deliberate package-level `not-claimed` surface for trim, Native AOT, and single-file publishing. The generator's job is to load arbitrary referenced assemblies and enumerate public constructors, fields, properties, and methods so XML-comment-backed API docs can be published. That by-design reflection is the feature, not an accidental implementation detail.

The project file explicitly declares `IsTrimmable=false`, `IsAotCompatible=false`, `PublishTrimmed=false`, `PublishAot=false`, and `PublishSingleFile=false`; `scripts/deployment-mode-support.json` records the same values in this package's `requiredProjectProperties`; and the manifest Pester suite verifies those values against the project file. A future documentation pipeline can add a separate source-generated or descriptor-backed publisher, but this package itself must not be presented as trim, Native AOT, or single-file compatible while it remains the runtime assembly-introspection tool.

## Related docs

- [Reference docs publishing](../reference-docs.md)
- [Architecture](../architecture.md)
