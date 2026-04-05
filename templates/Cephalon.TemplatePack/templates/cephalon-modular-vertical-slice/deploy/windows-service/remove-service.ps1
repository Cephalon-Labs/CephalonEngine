param(
    [string]$ServiceName = "CephalonTemplateApp",
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $principal = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if ($Preview) {
    Write-Host "sc.exe delete $ServiceName" -ForegroundColor Cyan
    return
}

if (-not (Test-IsAdministrator)) {
    throw "Removing a Windows Service requires an elevated PowerShell session."
}

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $existingService) {
    Write-Host "Windows Service '$ServiceName' is not installed." -ForegroundColor Yellow
    return
}

if ($existingService.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Stopped) {
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
}

& sc.exe delete $ServiceName
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe delete failed for '$ServiceName'."
}

Write-Host "Windows Service '$ServiceName' deleted successfully." -ForegroundColor Green
