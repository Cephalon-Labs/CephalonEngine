# Cephalon Docs

This directory is the documentation hub for the Cephalon engine, its host adapters, companion packages, tooling, and planning artifacts.

## Documentation model

- hand-authored `.md` files in `README.md` and `docs/` are the human-facing guides that explain what Cephalon is, what it ships, and how teams should use it
- detailed XML comments in `src/` are the API explanation layer for IntelliSense and external documentation generators
- `Cephalon.ReferenceDocs` and `docs/reference/` are optional publishing tooling and output for XML-driven API reference bundles; they do not replace the hand-authored guides

## Start here

- [Architecture](architecture.md)
- [Component catalog](components/README.md)
- [App models](app-models.md)
- [Module authoring](module-authoring.md)
- [Technology packs](technology-packs.md)

## Runtime and operations

- [Operations](operations.md)
- [Runtime failure policy](runtime-failure-policy.md)
- [Benchmarking](benchmarking.md)
- [Reference docs publishing](reference-docs.md)

## Optional generated reference docs

- [Reference landing page](reference/README.md)
- [Reference browser](reference/browse.html)
- [Namespace index](reference/namespaces.md)
- [Type index](reference/types.md)
- [Member index](reference/members.md)
- [Reference manifest](reference/reference-manifest.json)

## Planning

- [Engine roadmap](engine-roadmap.md)
- [Engine backlog](engine-backlog.md)
- planning issues and phase milestones can be synchronized from those docs through `scripts/sync-planning-github.ps1` and `.github/workflows/planning-sync.yml`
- set `CEPHALON_PROJECT_TOKEN` with `repo`, `project`, and `read:org` scopes when the workflow needs to update the organization-level GitHub Project as well as repository issues and milestones

## Visual diagrams

- `cephalon-architecture.drawio`
- `cephalon-app-models.drawio`
- `cephalon-engine-roadmap.drawio`

## Coverage

`docs/components/` contains one hand-authored Markdown page for every shipped `src/Cephalon.*` project so the public engine surface, adapters, companion packs, and tooling each have a stable explanation alongside the code.
