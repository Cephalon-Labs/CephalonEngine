# IIS deployment

These assets provide the hosted Windows IIS baseline for `CephalonTemplateApp` after the host is published through `CephalonFolder.pubxml`.

The generated publish output already includes the SDK-generated `web.config` for ASP.NET Core Module V2 (`AspNetCoreModuleV2`), so IIS can proxy the app through `dotnet .\CephalonTemplateApp.dll` without extra host-specific code.

The generated install script assumes:

- published output lives at `C:\inetpub\sites\CephalonTemplateApp\current`
- IIS plus the ASP.NET Core Hosting Bundle are installed on the Windows target
- the real install step is run from an elevated PowerShell session

Files in this folder:

- `install-site.ps1`
- `remove-site.ps1`

From the generated app root, publish the host with:

```powershell
dotnet publish CephalonTemplateApp.csproj -p:PublishProfile=CephalonFolder
```

On the Windows target, copy the published output and preview the IIS install contract with:

```powershell
New-Item -ItemType Directory -Path 'C:\inetpub\sites\CephalonTemplateApp\current' -Force | Out-Null
Copy-Item -Path .\artifacts\publish\CephalonTemplateApp\* -Destination 'C:\inetpub\sites\CephalonTemplateApp\current' -Recurse -Force
pwsh ./deploy/iis/install-site.ps1 -PhysicalPath 'C:\inetpub\sites\CephalonTemplateApp\current' -Preview
```

When you are ready to install the site for real, rerun the same command from an elevated PowerShell session without `-Preview`:

```powershell
pwsh ./deploy/iis/install-site.ps1 -PhysicalPath 'C:\inetpub\sites\CephalonTemplateApp\current'
```

That creates:

- an application pool named `CephalonTemplateApp` with `No Managed Code`
- a site named `CephalonTemplateApp` bound by default to `http/*:8080:`
- the site's root application mapped to the generated app pool

To remove the site later:

```powershell
pwsh ./deploy/iis/remove-site.ps1
```

Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`.
