# Linux systemd deployment

This guide is the self-hosted Linux service-manager baseline for freshly generated Cephalon apps.

Use it when you want a generated host to move from scaffolded source to deterministic published output and then into a Linux `systemd` service shape without inventing your own install contract first.

## What ships now

Generated app roots from `cephalon new` and the app-focused `dotnet new cephalon-*` starters now include:

- `Properties/PublishProfiles/CephalonFolder.pubxml`
- `deploy/linux/systemd/README.md`
- `deploy/linux/systemd/<AppName>.service`
- `deploy/linux/systemd/<AppName>.env`

That means a generated host now carries its own Linux service-manager baseline instead of leaving the `/opt/*` layout, environment-file path, and `systemctl` install flow implicit.

## Publish the host

For scaffolded multi-project apps from `cephalon new`:

```powershell
dotnet publish ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj -p:PublishProfile=CephalonFolder
```

For app-starter templates from `dotnet new cephalon-*`:

```powershell
dotnet publish ./Acme.Store.TemplateStarter/Acme.Store.TemplateStarter.csproj -p:PublishProfile=CephalonFolder
```

## Install the published output

The shipped service baseline assumes:

- published output is copied to `/opt/<AppName>/current`
- the optional environment override file lives at `/etc/cephalon/<AppName>.env`

Example for a scaffolded modular-monolith app:

```bash
sudo install -d /opt/Acme.Store/current
sudo cp -R ./Acme.Store/artifacts/publish/Acme.Store.Host/. /opt/Acme.Store/current/
sudo install -d /etc/cephalon
sudo install -m 0644 ./Acme.Store/deploy/linux/systemd/Acme.Store.env /etc/cephalon/Acme.Store.env
sudo install -m 0644 ./Acme.Store/deploy/linux/systemd/Acme.Store.service /etc/systemd/system/Acme.Store.service
```

The generated `.env` file starts with safe defaults for:

- `DOTNET_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://0.0.0.0:8080`
- optional commented OpenTelemetry settings for a local or self-hosted collector path

`cephalon doctor --app-root ./Acme.Store` now also validates that `deploy/linux/systemd/Acme.Store.env` keeps those generated environment and commented telemetry defaults explicit before teams rely on the shipped `systemd` install baseline.

## Verify and enable the service

On the Linux target, verify and install the unit with:

```bash
sudo systemd-analyze verify /etc/systemd/system/Acme.Store.service
sudo systemctl daemon-reload
sudo systemctl enable --now Acme.Store.service
sudo systemctl status Acme.Store.service
```

To inspect startup logs:

```bash
sudo journalctl -u Acme.Store.service -f
```

Once the service is running, inspect:

- `/engine`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/health`
- `/health/ready`
- `/scalar`

## Optional repo-native validation script

To replay the generated Linux `systemd` baseline from the Cephalon repository in one command, run:

```powershell
pwsh ./scripts/validate-generated-app-systemd.ps1
```

That script:

- scaffolds a fresh app through `Cephalon.Cli`
- seeds `./.cephalon/packages` with repo-local package artifacts
- publishes the generated host through `CephalonFolder.pubxml`
- rewrites the generated unit into a temporary WSL-visible path that points at the published output
- runs `systemd-analyze verify` under WSL
- tears the temporary app back down afterward unless `-KeepOutput` is supplied

If you are working from Windows, use WSL for the verification step or run the same commands directly on the Linux target.

## Related docs

- [Getting started](getting-started.md)
- [Generated app publishing](generated-app-publishing.md)
- [Container runtime](container-runtime.md)
- [Operations](operations.md)
