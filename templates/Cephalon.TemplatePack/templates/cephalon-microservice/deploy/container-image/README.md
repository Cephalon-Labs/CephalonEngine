# Container image publishing

These assets provide the provider-neutral container image publishing baseline for `CephalonTemplateApp` from the generated app root and Dockerfile.

The generated publish script validates the generated app root, builds the shipped Dockerfile with one or more image tags, and can optionally push those tags to the registry you choose without inventing provider-specific packaging first.

The generated publish script assumes:

- the generated app root keeps the shipped `Dockerfile` and `NuGet.config`
- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- Docker Desktop or another compatible Docker engine is installed on the build machine
- `docker login` has already been completed for the target registry before you use `-Push`

Files in this folder:

- `publish-image.ps1`

Preview the Docker build and push contract locally with:

```powershell
pwsh ./deploy/container-image/publish-image.ps1 -Image replace-with-registry/cephalon-template-app:latest -Push -Preview
```

Build the generated image locally without pushing it yet:

```powershell
pwsh ./deploy/container-image/publish-image.ps1 -Image replace-with-registry/cephalon-template-app:latest
```

When you are ready to publish the image to the registry:

```powershell
docker login ghcr.io
pwsh ./deploy/container-image/publish-image.ps1 -Image replace-with-registry/cephalon-template-app:latest -AdditionalTags replace-with-registry/cephalon-template-app:stable -Push
```

If you need to target a specific runtime platform during the build, also pass `-Platform linux/amd64` or another Docker-compatible platform string.

After the push completes, reuse the same image tag with `deploy/kubernetes/apply.ps1` or another hosted container deployment surface.
