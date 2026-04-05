# Windows Service deployment

These assets provide the self-hosted Windows Service baseline for `CephalonTemplateApp` after the host is published through `CephalonFolder.pubxml`.

The generated host already includes Windows Service-aware startup wiring through `Microsoft.Extensions.Hosting.WindowsServices` so the service lifetime and content root stay aligned when the Service Control Manager launches the process.

The generated install script assumes:

- published output lives at `C:\Services\CephalonTemplateApp\current`
- `dotnet` is available on the Windows target
- the real install step is run from an elevated PowerShell session

Files in this folder:

- `install-service.ps1`
- `remove-service.ps1`

From the generated app root, publish the host with:

```powershell
dotnet publish CephalonTemplateApp.csproj -p:PublishProfile=CephalonFolder
```

On the Windows target, copy the published output and preview the install contract with:

```powershell
New-Item -ItemType Directory -Path 'C:\Services\CephalonTemplateApp\current' -Force | Out-Null
Copy-Item -Path .\artifacts\publish\CephalonTemplateApp\* -Destination 'C:\Services\CephalonTemplateApp\current' -Recurse -Force
pwsh ./deploy/windows-service/install-service.ps1 -PublishRoot 'C:\Services\CephalonTemplateApp\current' -Preview
```

When you are ready to install the service for real, rerun the same command from an elevated PowerShell session without `-Preview`:

```powershell
pwsh ./deploy/windows-service/install-service.ps1 -PublishRoot 'C:\Services\CephalonTemplateApp\current'
Start-Service -Name 'CephalonTemplateApp'
Get-Service -Name 'CephalonTemplateApp'
sc.exe qc 'CephalonTemplateApp'
```

To remove the service later:

```powershell
pwsh ./deploy/windows-service/remove-service.ps1
```

Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`.
