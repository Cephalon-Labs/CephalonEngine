# Container image publishing

This guide is the provider-neutral container image publishing baseline for freshly generated Cephalon apps.

Use it when you want a generated host to move from scaffolded source into a reusable build/tag/push image contract through the shipped Dockerfile and `deploy/container-image/publish-image.ps1`, before you hand that image to Kubernetes or another hosted container platform.

## What ships now

Generated app roots from `cephalon new` and the app-focused `dotnet new cephalon-*` starters now include:

- `NuGet.config`
- `.cephalon/packages/README.md`
- `Dockerfile`
- `deploy/container-image/README.md`
- `deploy/container-image/publish-image.ps1`

The generated publish script validates the generated app root, builds the shipped Dockerfile with one or more tags, and can optionally push those tags to the registry you choose without introducing provider-specific packaging first.

## Prerequisites

- a generated app root from `cephalon new` or `dotnet new cephalon-*`
- `Cephalon*` packages available either through `./.cephalon/packages` or a shared package source that replaces the generated `cephalon` feed entry
- Docker Desktop or another compatible Docker engine on the machine that performs the build
- `docker login` already completed for the target registry before you invoke the generated script with `-Push`

## Preview the build and push contract

Example for a scaffolded modular-monolith app:

```powershell
pwsh ./Acme.Store/deploy/container-image/publish-image.ps1 `
  -Image ghcr.io/acme/acme-store:1.0.0 `
  -AdditionalTags ghcr.io/acme/acme-store:latest `
  -Push `
  -Preview
```

The preview step:

- validates that the generated app root still contains `Dockerfile` and `NuGet.config`
- prints the `docker build` command it would run
- prints one `docker push` command per image tag when `-Push` is supplied
- does not contact the registry or start a real image build

If you need to target a specific runtime platform, also pass `-Platform linux/amd64` or another Docker-compatible platform string.

## Build the generated image locally

If you only want the local image first:

```powershell
pwsh ./Acme.Store/deploy/container-image/publish-image.ps1 `
  -Image ghcr.io/acme/acme-store:1.0.0
```

That keeps the tag local so you can validate the Docker image before you publish it.

## Publish the image to a registry

When you are ready to push the image:

```powershell
docker login ghcr.io
pwsh ./Acme.Store/deploy/container-image/publish-image.ps1 `
  -Image ghcr.io/acme/acme-store:1.0.0 `
  -AdditionalTags ghcr.io/acme/acme-store:latest `
  -Push
```

The generated script:

- builds the image from the generated app root and Dockerfile
- applies the primary `-Image` tag plus any `-AdditionalTags`
- pushes each tag to the registry when `-Push` is supplied

Once the push completes, reuse the same image tag with the generated Kubernetes apply script or another hosted container deployment surface.

## Continue into deployment

After you have a pullable image, continue with:

- [Kubernetes deployment](kubernetes-deployment.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)

Those guides keep the image reference explicit instead of rebuilding different images for each deploy target.

## Optional repo-native validation script

To replay the generated container-image baseline from the Cephalon repository in one command, run:

```powershell
pwsh ./scripts/validate-generated-app-container-image.ps1
```

That script:

- scaffolds a fresh app through `Cephalon.Cli`
- seeds `./.cephalon/packages` with repo-local package artifacts
- validates that the generated app root contains the shipped Docker and container-image assets
- previews the generated build/push contract
- builds the generated container image through the shipped publish script
- by default, starts a local Docker registry and proves that the shipped publish script can push the generated tags into that registry
- tears the temporary app and registry back down afterward unless `-KeepOutput` is supplied

## Related docs

- [Getting started](getting-started.md)
- [Generated app publishing](generated-app-publishing.md)
- [Container runtime](container-runtime.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)
- [Kubernetes deployment](kubernetes-deployment.md)
- [Operations](operations.md)
