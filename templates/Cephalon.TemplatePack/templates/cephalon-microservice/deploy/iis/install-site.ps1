param(
    [string]$SiteName = "CephalonTemplateApp",
    [string]$AppPoolName = "CephalonTemplateApp",
    [string]$PhysicalPath = "C:\inetpub\sites\CephalonTemplateApp\current",
    [string]$BindingInformation = "*:8080:",
    [string]$AppCmdPath = "$env:WinDir\System32\inetsrv\appcmd.exe",
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $principal = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$webConfigPath = Join-Path $PhysicalPath "web.config"
if (-not (Test-Path -LiteralPath $webConfigPath)) {
    throw "Expected published IIS host assets at '$webConfigPath'."
}

$addAppPoolArguments = @("add", "apppool", "/name:$AppPoolName", "/managedRuntimeVersion:", "/managedPipelineMode:Integrated")
$setAppPoolArguments = @("set", "apppool", "/apppool.name:$AppPoolName", "/processModel.idleTimeout:00:00:00", "/startMode:AlwaysRunning")
$addSiteArguments = @("add", "site", "/name:$SiteName", "/bindings:http/$BindingInformation", "/physicalPath:$PhysicalPath")
$setAppArguments = @("set", "app", "/app.name:$SiteName/", "/applicationPool:$AppPoolName")
$startSiteArguments = @("start", "site", "/site.name:$SiteName")

if ($Preview) {
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host "$AppCmdPath $($addAppPoolArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($setAppPoolArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($addSiteArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($setAppArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($startSiteArguments -join ' ')" -ForegroundColor Cyan
    return
}

if (-not (Test-IsAdministrator)) {
    throw "Installing an IIS site requires an elevated PowerShell session."
}

if (-not (Test-Path -LiteralPath $AppCmdPath)) {
    throw "Could not find appcmd.exe at '$AppCmdPath'. Install IIS and the management tools first."
}

& $AppCmdPath @addAppPoolArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd add apppool failed for '$AppPoolName'."
}

& $AppCmdPath @setAppPoolArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd set apppool failed for '$AppPoolName'."
}

& $AppCmdPath @addSiteArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd add site failed for '$SiteName'."
}

& $AppCmdPath @setAppArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd set app failed for '$SiteName/'."
}

& $AppCmdPath @startSiteArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd start site failed for '$SiteName'."
}

Write-Host ""
Write-Host "IIS site '$SiteName' created successfully." -ForegroundColor Green
Write-Host "App pool: $AppPoolName" -ForegroundColor Cyan
Write-Host "Physical path: $PhysicalPath" -ForegroundColor Cyan
