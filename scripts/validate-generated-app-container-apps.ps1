param(
    [string]$AppName = "Cephalon.ContainerAppsSmoke",
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$SkipDockerBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProjectPath = Join-Path $repoRoot "src\Cephalon.Cli\Cephalon.Cli.csproj"
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-container-apps-" + [Guid]::NewGuid().ToString("N"))
$generatedRoot = Join-Path $tempRoot "app"
$guidePath = Join-Path $generatedRoot "deploy\azure-container-apps\README.md"
$deployScriptPath = Join-Path $generatedRoot "deploy\azure-container-apps\deploy-up.ps1"
$dockerfilePath = Join-Path $generatedRoot "Dockerfile"
$nuGetConfigPath = Join-Path $generatedRoot "NuGet.config"
$dockerImageTag = ("cephalon-containerapps-smoke:" + [Guid]::NewGuid().ToString("N")).ToLowerInvariant()
$dockerImageBuilt = $false

function Invoke-Process {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & $FileName @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$FileName command failed: $FileName $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-PwshScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & pwsh -NoLogo -NoProfile -File $ScriptPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "PowerShell script failed: $ScriptPath $($Arguments -join ' ')"
    }

    return ($output | Out-String)
}

try {
    & az containerapp up --help 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI with 'az containerapp up' support is required for the Container Apps validation baseline."
    }

    if (-not $SkipDockerBuild) {
        & docker version 1>$null 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Docker is required for the generated Container Apps validation baseline unless -SkipDockerBuild is supplied."
        }
    }

    New-Item -ItemType Directory -Path $generatedRoot -Force | Out-Null

    Invoke-Process -FileName "dotnet" -WorkingDirectory $repoRoot -Arguments @(
        "run",
        "--project", $cliProjectPath,
        "--",
        "new",
        $AppName,
        "--output", $generatedRoot)

    $publishPackageArguments = @{
        Configuration = $Configuration
        OutputPath = (Join-Path $generatedRoot ".cephalon\packages")
    }

    if ($SkipPackageBuild) {
        $publishPackageArguments["SkipBuild"] = $true
    }

    & $publishPackagesScriptPath @publishPackageArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing failed."
    }

    foreach ($path in @(
        $guidePath,
        $deployScriptPath,
        $dockerfilePath,
        $nuGetConfigPath))
    {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Expected generated asset at '$path'."
        }
    }

    if (-not $SkipDockerBuild) {
        Invoke-Process -FileName "docker" -WorkingDirectory $generatedRoot -Arguments @(
            "build",
            "-t", $dockerImageTag,
            ".")
        $dockerImageBuilt = $true
    }

    $preview = Invoke-PwshScript -ScriptPath $deployScriptPath -Arguments @(
        "-ResourceGroupName", "cephalon-smoke-rg",
        "-Location", "eastus",
        "-AppName", "cephalon-container-smoke",
        "-ContainerAppEnvironment", "cephalon-smoke-env",
        "-Preview")
    $previewPlain = [regex]::Replace($preview, '\x1B\[[0-9;]*m', '')

    if (-not $previewPlain.Contains("az containerapp up", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected az containerapp up command."
    }

    if (-not $previewPlain.Contains("--source", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected source-root argument."
    }

    if (-not $previewPlain.Contains("--ingress external", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected external ingress configuration."
    }

    if (-not $previewPlain.Contains("--target-port 8080", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected target port."
    }

    if (-not $previewPlain.Contains("--environment cephalon-smoke-env", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected Container Apps environment argument."
    }

    if (-not $previewPlain.Contains("ASPNETCORE_HTTP_PORTS=8080", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected ASPNETCORE_HTTP_PORTS environment variable."
    }

    if (-not $previewPlain.Contains("DOTNET_ENVIRONMENT=Production", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected DOTNET_ENVIRONMENT environment variable."
    }

    if (-not $previewPlain.Contains($generatedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Preview output did not point at the generated app root."
    }

    Write-Host ""
    Write-Host "Generated app Azure Container Apps validation completed successfully." -ForegroundColor Green
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan

    if ($dockerImageBuilt) {
        Write-Host "Validated Docker image tag: $dockerImageTag" -ForegroundColor Cyan
    }
}
finally {
    if ($dockerImageBuilt) {
        & docker image rm $dockerImageTag 1>$null 2>$null
        $null = $LASTEXITCODE
    }

    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
