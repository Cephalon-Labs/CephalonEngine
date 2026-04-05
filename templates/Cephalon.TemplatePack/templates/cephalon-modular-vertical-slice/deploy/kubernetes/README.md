# Kubernetes deployment

These assets provide the platform-neutral Kubernetes baseline for `CephalonTemplateApp` from the generated app root and Dockerfile.

The generated deployment script renders the shipped `deploy/kubernetes/*` manifests through `kubectl kustomize` so the generated host can move from scaffolded source into a generic Kubernetes baseline without inventing a Helm chart or provider-specific packaging workflow first. If you want a provider-neutral registry build/tag/push step before you apply the manifest set, use `../container-image/publish-image.ps1` first.

The generated deploy script assumes:

- the generated app root keeps the shipped `Dockerfile` and `NuGet.config`
- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- `kubectl` with `kustomize` support is installed on the deployment machine
- the target cluster can pull the image you pass to the deployment script
- your current `kubectl` context already targets the cluster you want to update

Files in this folder:

- `apply.ps1`
- `kustomization.yaml`
- `namespace.yaml`
- `deployment.yaml`
- `service.yaml`

Preview the Kubernetes deployment contract locally with:

```powershell
pwsh ./deploy/kubernetes/apply.ps1 -Image ghcr.io/example/cephalon-template-app:latest -Preview
```

By default the generated manifests target namespace `cephalon-template-app` and expose the host through a `ClusterIP` service on port `80` to container port `8080`. Pass `-Namespace my-namespace` when you need a different namespace.

When you are ready to apply the rendered manifest set to the current cluster context:

```powershell
kubectl config current-context
pwsh ./deploy/kubernetes/apply.ps1 -Image ghcr.io/example/cephalon-template-app:latest -Namespace cephalon-template-app
```

After the workload is ready, port-forward the generated service to inspect the host locally:

```powershell
kubectl -n cephalon-template-app port-forward service/cephalon-template-app 18080:80
```

The generated manifests keep `ASPNETCORE_HTTP_PORTS=8080`, `DOTNET_ENVIRONMENT=Production`, `readinessProbe` on `/health/ready`, `livenessProbe` on `/health/live`, and a `startupProbe` on `/health/ready`.

Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`.
