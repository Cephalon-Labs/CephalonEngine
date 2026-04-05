param(
    [string]$ResourceGroupName = "replace-with-resource-group",
    [string]$AppName = "cephalon-template-app",
    [string]$Location = "replace-with-azure-region",
    [string]$ContainerAppEnvironment = "",
    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
    [ValidateSet("external", "internal")]
    [string]$Ingress = "external",
    [ValidateRange(1, 65535)]
    [int]$TargetPort = 8080,
    [string[]]$EnvironmentVariables = @(
        "ASPNETCORE_HTTP_PORTS=8080",
        "DOTNET_ENVIRONMENT=Production"),
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

function Get-AzCommandArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSourceRoot
    )

    $arguments = @(
        "containerapp", "up",
        "--name", $AppName,
        "--resource-group", $ResourceGroupName,
        "--location", $Location,
        "--source", $ResolvedSourceRoot,
        "--ingress", $Ingress,
        "--target-port", $TargetPort.ToString([System.Globalization.CultureInfo]::InvariantCulture))

    if (-not [string]::IsNullOrWhiteSpace($ContainerAppEnvironment)) {
        $arguments += @("--environment", $ContainerAppEnvironment)
    }

    if ($EnvironmentVariables.Count -gt 0) {
        $arguments += @("--env-vars") + $EnvironmentVariables
    }

    return $arguments
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Expected generated app root at '$SourceRoot'."
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$dockerfilePath = Join-Path $resolvedSourceRoot "Dockerfile"
$nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"
$hostProjectPath = Join-Path $resolvedSourceRoot "CephalonTemplateApp.csproj"

foreach ($path in @($dockerfilePath, $nuGetConfigPath, $hostProjectPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected generated asset at '$path'."
    }
}

$upArguments = Get-AzCommandArguments -ResolvedSourceRoot $resolvedSourceRoot

if ($Preview) {
    Write-Host "Validated Azure Container Apps source root: $resolvedSourceRoot" -ForegroundColor Green
    Write-Host "Detected generated host project: $hostProjectPath" -ForegroundColor Green
    Write-Host "Preview only. The following command would run:" -ForegroundColor Yellow
    Write-Host (Format-Command -Command "az" -Arguments $upArguments) -ForegroundColor Cyan
    return
}

if (Test-PlaceholderValue -Value $ResourceGroupName -or Test-PlaceholderValue -Value $Location) {
    throw "Set both -ResourceGroupName and -Location before running a live Azure Container Apps deployment."
}

if ($AppName -notmatch '^[a-z](?:[a-z0-9-]{0,29}[a-z0-9])?$' -or $AppName.Contains("--", [System.StringComparison]::Ordinal)) {
    throw "Container App names must use lower-case letters, digits, or '-', start with a letter, end with a letter or digit, avoid '--', and stay under 32 characters."
}

$null = Get-Command az -ErrorAction Stop

& az @upArguments
if ($LASTEXITCODE -ne 0) {
    throw "Failed to deploy source root '$resolvedSourceRoot' to Container App '$AppName'."
}

Write-Host ""
Write-Host "Azure Container Apps deployment completed successfully." -ForegroundColor Green
Write-Host "Source root: $resolvedSourceRoot" -ForegroundColor Cyan
Write-Host "App: $AppName" -ForegroundColor Cyan
