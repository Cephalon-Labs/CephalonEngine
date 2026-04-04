param(
    [string]$ServiceName = "CephalonTemplateApp",
    [string]$DisplayName = "CephalonTemplateApp",
    [string]$Description = "Cephalon host for CephalonTemplateApp",
    [string]$PublishRoot = "C:\Services\CephalonTemplateApp\current",
    [string]$DotnetPath = "dotnet",
    [string]$EnvironmentName = "Production",
    [string]$Urls = "http://127.0.0.1:8080",
    [ValidateSet("auto", "demand", "disabled")]
    [string]$StartupType = "auto",
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $principal = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$dllPath = Join-Path $PublishRoot "CephalonTemplateApp.dll"

if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Expected published host at '$dllPath'."
}

$resolvedDotnetPath = (Get-Command $DotnetPath -ErrorAction Stop).Source
$binaryPath = "`"$resolvedDotnetPath`" `"$dllPath`" --contentRoot `"$PublishRoot`" --environment `"$EnvironmentName`" --urls `"$Urls`""

$createArguments = @(
    "create",
    $ServiceName,
    "binPath=",
    $binaryPath,
    "start=",
    $StartupType,
    "displayname=",
    $DisplayName)
$descriptionArguments = @("description", $ServiceName, $Description)
$failureArguments = @("failure", $ServiceName, "reset=", "0", "actions=", "restart/60000/restart/60000/restart/60000")

if ($Preview) {
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host "sc.exe $($createArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "sc.exe $($descriptionArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "sc.exe $($failureArguments -join ' ')" -ForegroundColor Cyan
    return
}

if (-not (Test-IsAdministrator)) {
    throw "Installing a Windows Service requires an elevated PowerShell session."
}

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $existingService) {
    throw "Service '$ServiceName' already exists. Remove it first or choose another -ServiceName."
}

& sc.exe @createArguments
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe create failed for '$ServiceName'."
}

& sc.exe @descriptionArguments
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe description failed for '$ServiceName'."
}

& sc.exe @failureArguments
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe failure failed for '$ServiceName'."
}

Write-Host ""
Write-Host "Windows Service '$ServiceName' created successfully." -ForegroundColor Green
Write-Host "Start it with: Start-Service -Name '$ServiceName'" -ForegroundColor Cyan
