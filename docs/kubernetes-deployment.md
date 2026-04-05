# Kubernetes deployment

This guide is the platform-neutral Kubernetes deployment baseline for freshly generated Cephalon apps.

Use it when you want a generated host to move from scaffolded source into a generic Kubernetes baseline through the shipped Dockerfile, `deploy/kubernetes/*` manifests, and `kubectl kustomize`, without inventing a Helm chart or provider-specific packaging workflow first.

## What ships now

Generated app roots from `cephalon new` and the app-focused `dotnet new cephalon-*` starters now include:

- `NuGet.config`
- `.cephalon/packages/README.md`
- `Dockerfile`
- `deploy/kubernetes/README.md`
- `deploy/kubernetes/apply.ps1`
- `deploy/kubernetes/kustomization.yaml`
- `deploy/kubernetes/namespace.yaml`
- `deploy/kubernetes/deployment.yaml`
- `deploy/kubernetes/service.yaml`

The generated deployment script validates the generated app root, renders the manifest set locally through `kubectl kustomize`, and then applies the rendered namespace, deployment, and service resources to the current cluster context when you drop `-Preview`.

## Prerequisites

- a generated app root from `cephalon new` or `dotnet new cephalon-*`
- `Cephalon*` packages available either through `./.cephalon/packages` or a shared package source that replaces the generated `cephalon` feed entry
- `kubectl` with `kustomize` support installed on the machine that performs the render/apply step
- a pullable container image built from the shipped Dockerfile

If you want to locally validate the generated Dockerfile before you render the Kubernetes baseline, keep Docker Desktop or another compatible Docker engine available too.
If you want a provider-neutral registry build/tag/push step first, continue with [Container image publishing](container-image-publishing.md).

## Build and publish an image

Example for a scaffolded modular-monolith app:

```powershell
pwsh ./Acme.Store/deploy/container-image/publish-image.ps1 `
  -Image ghcr.io/acme/acme-store:1.0.0 `
  -AdditionalTags ghcr.io/acme/acme-store:latest `
  -Push
```

The target cluster must be able to pull the image you pass to the generated apply script.

## Preview the Kubernetes manifest contract

Example for a scaffolded modular-monolith app:

```powershell
pwsh ./Acme.Store/deploy/kubernetes/apply.ps1 `
  -Image ghcr.io/acme/acme-store:1.0.0 `
  -Namespace acme-store `
  -Preview
```

The preview step:

- validates that the generated app root still contains `Dockerfile` and `NuGet.config`
- validates that the shipped Kubernetes manifest set is present
- renders the manifest through `kubectl kustomize`
- prints the rendered YAML without contacting a live cluster

The generated manifests keep:

- `ASPNETCORE_HTTP_PORTS=8080`
- `DOTNET_ENVIRONMENT=Production`
- `readinessProbe` on `/health/ready`
- `livenessProbe` on `/health/live`
- `startupProbe` on `/health/ready`
- a `ClusterIP` service on port `80` that targets container port `8080`

## Apply to the current cluster context

When you are ready to deploy for real:

```powershell
kubectl config current-context
pwsh ./Acme.Store/deploy/kubernetes/apply.ps1 `
  -Image ghcr.io/acme/acme-store:1.0.0 `
  -Namespace acme-store
```

The generated script applies the rendered manifest to the current `kubectl` context. Keep namespace selection explicit with `-Namespace` when the cluster policy, tenancy, or promotion path should not use the generated default.

## Inspect the running host

Once the workload is ready, port-forward the generated service:

```powershell
kubectl -n acme-store port-forward service/acme-store 18080:80
```

Then inspect:

- `/engine`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/health`
- `/health/ready`
- `/scalar`

These routes should behave the same way they do under `dotnet run`, the published-output smoke path, the local container path, and the Azure Container Apps baseline.

## Optional repo-native validation script

To replay the generated Kubernetes baseline from the Cephalon repository in one command, run:

```powershell
pwsh ./scripts/validate-generated-app-kubernetes.ps1
```

That script:

- scaffolds a fresh app through `Cephalon.Cli`
- seeds `./.cephalon/packages` with repo-local package artifacts
- validates that the generated app root contains the shipped Docker and Kubernetes assets
- builds the generated container image locally unless `-SkipDockerBuild` is supplied
- replays the shipped Kubernetes apply script in preview mode
- validates the rendered namespace, deployment, service, image, environment, and probe contract
- tears the temporary app back down afterward unless `-KeepOutput` is supplied

## Related docs

- [Getting started](getting-started.md)
- [Generated app publishing](generated-app-publishing.md)
- [Container image publishing](container-image-publishing.md)
- [Container runtime](container-runtime.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [Operations](operations.md)
