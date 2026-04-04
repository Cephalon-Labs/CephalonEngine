param(
    [string]$ResourceGroupName = "replace-with-resource-group",
    [string]$AppName = "replace-with-app-service-name",
    [string]$SlotName = "",
    [string]$PublishRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "artifacts\publish\CephalonTemplateApp"),
    [string]$PackagePath = (Join-Path (Join-Path (Join-Path $PSScriptRoot "..\..") "artifacts\deploy\CephalonTemplateApp") "azure-app-service.zip"),
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-PlaceholderValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.StartsWith("replace-with-", [System.StringComparison]::Ordinal)
}

function Get-AzCommandArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    if ([string]::IsNullOrWhiteSpace($SlotName)) {
        return $Arguments
    }

    return $Arguments + @("--slot", $SlotName)
}

function Format-Command {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $parts = @($Command) + $Arguments

    return ($parts | ForEach-Object {
        if ($_ -match '\s') {
            '"' + $_ + '"'
        }
        else {
            $_
        }
    }) -join ' '
}

if (-not (Test-Path -LiteralPath $PublishRoot)) {
    throw "Expected published output at '$PublishRoot'. Run dotnet publish with CephalonFolder first."
}

$publishDllPath = Join-Path $PublishRoot "CephalonTemplateApp.dll"
$webConfigPath = Join-Path $PublishRoot "web.config"

foreach ($path in @($publishDllPath, $webConfigPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected published asset at '$path'."
    }
}

$packageDirectory = Split-Path -Parent $PackagePath
if (-not [string]::IsNullOrWhiteSpace($packageDirectory)) {
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
}

if (Test-Path -LiteralPath $PackagePath) {
    Remove-Item -LiteralPath $PackagePath -Force
}

Compress-Archive -Path (Join-Path $PublishRoot '*') -DestinationPath $PackagePath -Force

$setRunFromPackageArguments = Get-AzCommandArguments -Arguments @(
    "webapp", "config", "appsettings", "set",
    "--resource-group", $ResourceGroupName,
    "--name", $AppName,
    "--settings", "WEBSITE_RUN_FROM_PACKAGE=1")
$deployArguments = Get-AzCommandArguments -Arguments @(
    "webapp", "deploy",
    "--resource-group", $ResourceGroupName,
    "--name", $AppName,
    "--src-path", $PackagePath,
    "--type", "zip",
    "--clean", "true")

if ($Preview) {
    Write-Host "Created Azure App Service ZIP package: $PackagePath" -ForegroundColor Green
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host (Format-Command -Command "az" -Arguments $setRunFromPackageArguments) -ForegroundColor Cyan
    Write-Host (Format-Command -Command "az" -Arguments $deployArguments) -ForegroundColor Cyan
    return
}

if (Test-PlaceholderValue -Value $ResourceGroupName -or Test-PlaceholderValue -Value $AppName) {
    throw "Set both -ResourceGroupName and -AppName before running a live Azure App Service deployment."
}

$null = Get-Command az -ErrorAction Stop

& az @setRunFromPackageArguments
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set WEBSITE_RUN_FROM_PACKAGE for '$AppName'."
}

& az @deployArguments
if ($LASTEXITCODE -ne 0) {
    throw "Failed to deploy ZIP package to '$AppName'."
}

Write-Host ""
Write-Host "Azure App Service deployment completed successfully." -ForegroundColor Green
Write-Host "Package: $PackagePath" -ForegroundColor Cyan
Write-Host "App: $AppName" -ForegroundColor Cyan
