# IIS deployment

This guide is the hosted Windows IIS baseline for freshly generated Cephalon apps.

Use it when you want a generated host to move from scaffolded source to deterministic published output and then into an IIS site plus app-pool shape without inventing your own ASP.NET Core Module contract first.

## What ships now

Generated app roots from `cephalon new` and the app-focused `dotnet new cephalon-*` starters now include:

- `Properties/PublishProfiles/CephalonFolder.pubxml`
- `deploy/iis/README.md`
- `deploy/iis/install-site.ps1`
- `deploy/iis/remove-site.ps1`

The generated publish output already includes the SDK-generated `web.config` for ASP.NET Core Module V2 (`AspNetCoreModuleV2`), so IIS can proxy the app through `dotnet .\<HostProject>.dll` without extra host-specific code.

## Publish the host

For scaffolded multi-project apps from `cephalon new`:

```powershell
dotnet publish ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj -p:PublishProfile=CephalonFolder
```

For app-starter templates from `dotnet new cephalon-*`:

```powershell
dotnet publish ./Acme.Store.TemplateStarter/Acme.Store.TemplateStarter.csproj -p:PublishProfile=CephalonFolder
```

## Install the published output under IIS

The shipped IIS baseline assumes:

- published output is copied to `C:\inetpub\sites\<AppName>\current`
- IIS plus the ASP.NET Core Hosting Bundle are installed on the Windows target
- the real install or removal step is run from an elevated PowerShell session

Example for a scaffolded modular-monolith app:

```powershell
New-Item -ItemType Directory -Path 'C:\inetpub\sites\Acme.Store\current' -Force | Out-Null
Copy-Item -Path .\Acme.Store\artifacts\publish\Acme.Store.Host\* -Destination 'C:\inetpub\sites\Acme.Store\current' -Recurse -Force
pwsh ./Acme.Store/deploy/iis/install-site.ps1 -PhysicalPath 'C:\inetpub\sites\Acme.Store\current' -Preview
```

The preview step prints the `appcmd.exe` commands without requiring admin-only repo validation. When you are ready to install the site for real, rerun the same command from an elevated PowerShell session without `-Preview`:

```powershell
pwsh ./Acme.Store/deploy/iis/install-site.ps1 -PhysicalPath 'C:\inetpub\sites\Acme.Store\current'
& "$env:WinDir\System32\inetsrv\appcmd.exe" list site "Acme.Store"
& "$env:WinDir\System32\inetsrv\appcmd.exe" list apppool "Acme.Store"
Invoke-WebRequest -Uri http://localhost:8080/health/ready
```

That creates:

- an application pool named `Acme.Store` with `No Managed Code`
- a site named `Acme.Store` bound by default to `http/*:8080:`
- the site's root application mapped to the generated app pool and SDK-generated `web.config`

## Remove or inspect the site

To remove the site later:

```powershell
pwsh ./Acme.Store/deploy/iis/remove-site.ps1
```

`cephalon doctor --app-root ./Acme.Store` now also validates that `deploy/iis/remove-site.ps1` keeps the generated `stop site`, `delete site`, and `delete apppool` teardown flow explicit before teams rely on that IIS removal path.

Once the site is running, inspect:

- `/engine`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/health`
- `/health/ready`
- `/scalar`

If you need host-level startup details, inspect the generated `web.config`, IIS site/app-pool state, and the IIS logs plus Windows Event Viewer / Application log for process-start failures.

## Optional repo-native validation script

To replay the generated IIS baseline from the Cephalon repository in one command, run:

```powershell
pwsh ./scripts/validate-generated-app-iis.ps1
```

That script:

- scaffolds a fresh app through `Cephalon.Cli`
- seeds `./.cephalon/packages` with repo-local package artifacts
- publishes the generated host through `CephalonFolder.pubxml`
- verifies that the published output carries the expected ASP.NET Core Module `web.config`
- replays the shipped IIS install/remove scripts in preview mode against the published output
- tears the temporary app back down afterward unless `-KeepOutput` is supplied

## Related docs

- [Getting started](getting-started.md)
- [Generated app publishing](generated-app-publishing.md)
- [Windows Service deployment](windows-service-deployment.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [Operations](operations.md)
