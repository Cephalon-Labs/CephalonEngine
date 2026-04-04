# Cephalon Docs

This directory is the documentation hub for the Cephalon engine, its host adapters, companion packages, tooling, and planning artifacts.

## Documentation model

- hand-authored `.md` files in `README.md` and `docs/` are the human-facing guides that explain what Cephalon is, what it ships, and how teams should use it
- detailed XML comments in `src/` are the API explanation layer for IntelliSense and external documentation generators
- `Cephalon.ReferenceDocs` and `docs/reference/` are optional publishing tooling and output for XML-driven API reference bundles; they do not replace the hand-authored guides

## Start here

- [Getting started](getting-started.md)
- [Generated app publishing](generated-app-publishing.md)
- [Container image publishing](container-image-publishing.md)
- [Windows Service deployment](windows-service-deployment.md)
- [IIS deployment](iis-deployment.md)
- [Azure App Service deployment](azure-app-service-deployment.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)
- [Kubernetes deployment](kubernetes-deployment.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [Architecture](architecture.md)
- [Component catalog](components/README.md)
- [Compatibility](compatibility.md)
- [App models](app-models.md)
- [Module authoring](module-authoring.md)
- [Observability provider authoring](observability-provider-authoring.md)
- [Package publishing](package-publishing.md)
- [External package lifecycle](external-package-lifecycle.md)
- [Technology packs](technology-packs.md)

## Runtime and operations

- [Operations](operations.md)
- [Container runtime](container-runtime.md)
- [Generated app publishing](generated-app-publishing.md)
- [Container image publishing](container-image-publishing.md)
- [Windows Service deployment](windows-service-deployment.md)
- [IIS deployment](iis-deployment.md)
- [Azure App Service deployment](azure-app-service-deployment.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)
- [Kubernetes deployment](kubernetes-deployment.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [Operational hardening gap inventory](operational-hardening-gap-inventory.md)
- [Runtime failure policy](runtime-failure-policy.md)
- [Benchmarking](benchmarking.md)
- [Reference docs publishing](reference-docs.md)

The component catalog now includes the observability baseline package plus the optional Cassandra dependency-health, ClickHouse dependency-health, Consul dependency-health, Elasticsearch dependency-health, HTTP dependency-health, Kafka dependency-health, Memcached dependency-health, MongoDB dependency-health, MQTT dependency-health, MySQL dependency-health, NATS dependency-health, Neo4j dependency-health, OpenSearch dependency-health, Oracle dependency-health, Postgres dependency-health, RabbitMQ dependency-health, Redis dependency-health, SQL Server dependency-health, OpenTelemetry exporter, Alibaba Cloud, AWS, DigitalOcean, GCP, Huawei Cloud, Oracle Cloud, Kubernetes, OpenShift, Tanzu, Azure Monitor, and Serilog provider companions so operator-facing docs stay aligned with the shipped host integration paths, diagnostics conventions, runtime-story surface, and release-validation guidance.

Downstream provider and edge-runtime integrations that are not shipped as first-party Cephalon packages should follow the companion-authoring guidance in [Observability provider authoring](observability-provider-authoring.md) so the shared telemetry contract, diagnostics conventions, and planning language stay consistent.

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
