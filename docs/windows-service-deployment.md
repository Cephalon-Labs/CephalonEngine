# Windows Service deployment

This guide is the self-hosted Windows Service baseline for freshly generated Cephalon apps.

Use it when you want a generated host to move from scaffolded source to deterministic published output and then into a Windows Service install shape without inventing your own `sc.exe` contract first.

## What ships now

Generated app roots from `cephalon new` and the app-focused `dotnet new cephalon-*` starters now include:

- `Properties/PublishProfiles/CephalonFolder.pubxml`
- `deploy/windows-service/README.md`
- `deploy/windows-service/install-service.ps1`
- `deploy/windows-service/remove-service.ps1`

The generated host also now carries Windows Service-aware startup wiring through `Microsoft.Extensions.Hosting.WindowsServices`, `WindowsServiceHelpers.IsWindowsService()`, and `builder.Host.UseWindowsService()`.

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

The shipped Windows Service baseline assumes:

- published output is copied to `C:\Services\<AppName>\current`
- `dotnet` is available on the Windows target
- the real install or removal step is run from an elevated PowerShell session

Example for a scaffolded modular-monolith app:

```powershell
New-Item -ItemType Directory -Path 'C:\Services\Acme.Store\current' -Force | Out-Null
Copy-Item -Path .\Acme.Store\artifacts\publish\Acme.Store.Host\* -Destination 'C:\Services\Acme.Store\current' -Recurse -Force
pwsh ./Acme.Store/deploy/windows-service/install-service.ps1 -PublishRoot 'C:\Services\Acme.Store\current' -Preview
```

The preview step prints the `sc.exe create`, `sc.exe description`, and recovery commands without requiring admin rights. When you are ready to install the service for real, rerun the same command from an elevated PowerShell session without `-Preview`:

```powershell
pwsh ./Acme.Store/deploy/windows-service/install-service.ps1 -PublishRoot 'C:\Services\Acme.Store\current'
Start-Service -Name 'Acme.Store'
Get-Service -Name 'Acme.Store'
sc.exe qc 'Acme.Store'
```

The generated install script passes:

- the published DLL path under `C:\Services\<AppName>\current`
- `--contentRoot` pointing at that same published folder
- `--environment Production`
- `--urls http://127.0.0.1:8080`

That keeps config loading and content-root behavior deterministic when the Service Control Manager launches the process.

## Remove or inspect the service

To remove the service later:

```powershell
pwsh ./Acme.Store/deploy/windows-service/remove-service.ps1
```

Once the service is running, inspect:

- `/engine`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/health`
- `/health/ready`
- `/scalar`

If you need host-level startup details, inspect the Windows Service state with `Get-Service`, `sc.exe qc`, and the Windows Event Viewer / Application log for process-start failures.

## Optional repo-native validation script

To replay the generated Windows Service baseline from the Cephalon repository in one command, run:

```powershell
pwsh ./scripts/validate-generated-app-windows-service.ps1
```

That script:

- scaffolds a fresh app through `Cephalon.Cli`
- seeds `./.cephalon/packages` with repo-local package artifacts
- publishes the generated host through `CephalonFolder.pubxml`
- verifies that the generated host is wired for Windows Service startup
- replays the shipped install/remove scripts in preview mode against the published output
- tears the temporary app back down afterward unless `-KeepOutput` is supplied

## Related docs

- [Getting started](getting-started.md)
- [Generated app publishing](generated-app-publishing.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [Operations](operations.md)
