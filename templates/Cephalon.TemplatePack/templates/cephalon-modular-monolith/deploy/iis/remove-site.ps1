param(
    [string]$SiteName = "CephalonTemplateApp",
    [string]$AppPoolName = "CephalonTemplateApp",
    [string]$AppCmdPath = "$env:WinDir\System32\inetsrv\appcmd.exe",
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $principal = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$stopSiteArguments = @("stop", "site", "/site.name:$SiteName")
$deleteSiteArguments = @("delete", "site", "/site.name:$SiteName")
$deleteAppPoolArguments = @("delete", "apppool", "/apppool.name:$AppPoolName")

if ($Preview) {
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host "$AppCmdPath $($stopSiteArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($deleteSiteArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($deleteAppPoolArguments -join ' ')" -ForegroundColor Cyan
    return
}

if (-not (Test-IsAdministrator)) {
    throw "Removing an IIS site requires an elevated PowerShell session."
}

if (-not (Test-Path -LiteralPath $AppCmdPath)) {
    throw "Could not find appcmd.exe at '$AppCmdPath'. Install IIS and the management tools first."
}

& $AppCmdPath @stopSiteArguments
& $AppCmdPath @deleteSiteArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd delete site failed for '$SiteName'."
}

& $AppCmdPath @deleteAppPoolArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd delete apppool failed for '$AppPoolName'."
}

Write-Host "IIS site '$SiteName' and app pool '$AppPoolName' deleted successfully." -ForegroundColor Green
