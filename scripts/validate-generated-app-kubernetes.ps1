param(
    [string]$AppName = "Cephalon.KubernetesSmoke",
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
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-kubernetes-" + [Guid]::NewGuid().ToString("N"))
$generatedRoot = Join-Path $tempRoot "app"
$guidePath = Join-Path $generatedRoot "deploy\kubernetes\README.md"
$applyScriptPath = Join-Path $generatedRoot "deploy\kubernetes\apply.ps1"
$kustomizationPath = Join-Path $generatedRoot "deploy\kubernetes\kustomization.yaml"
$namespaceManifestPath = Join-Path $generatedRoot "deploy\kubernetes\namespace.yaml"
$deploymentManifestPath = Join-Path $generatedRoot "deploy\kubernetes\deployment.yaml"
$serviceManifestPath = Join-Path $generatedRoot "deploy\kubernetes\service.yaml"
$dockerfilePath = Join-Path $generatedRoot "Dockerfile"
$nuGetConfigPath = Join-Path $generatedRoot "NuGet.config"
$dockerImageTag = ("cephalon-kubernetes-smoke:" + [Guid]::NewGuid().ToString("N")).ToLowerInvariant()
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
    & kubectl version --client 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "kubectl is required for the generated Kubernetes validation baseline."
    }

    & kubectl kustomize --help 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "kubectl with kustomize support is required for the generated Kubernetes validation baseline."
    }

    if (-not $SkipDockerBuild) {
        & docker version 1>$null 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Docker is required for the generated Kubernetes validation baseline unless -SkipDockerBuild is supplied."
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
        $applyScriptPath,
        $kustomizationPath,
        $namespaceManifestPath,
        $deploymentManifestPath,
        $serviceManifestPath,
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

    $preview = Invoke-PwshScript -ScriptPath $applyScriptPath -Arguments @(
        "-Image", $dockerImageTag,
        "-Namespace", "cephalon-k8s-smoke",
        "-Preview")
    $previewPlain = [regex]::Replace($preview, '\x1B\[[0-9;]*m', '')

    foreach ($expectedValue in @(
        "kind: Namespace",
        "kind: Deployment",
        "kind: Service",
        "namespace: cephalon-k8s-smoke",
        "name: cephalon-k8s-smoke",
        "image: $dockerImageTag",
        "name: ASPNETCORE_HTTP_PORTS",
        "value: ""8080""",
        "name: DOTNET_ENVIRONMENT",
        "value: Production",
        "path: /health/ready",
        "path: /health/live",
        "containerPort: 8080",
        "port: 80",
        "targetPort: http",
        "type: ClusterIP"))
    {
        if (-not $previewPlain.Contains($expectedValue, [System.StringComparison]::Ordinal)) {
            throw "Preview output did not render the expected Kubernetes manifest content '$expectedValue'."
        }
    }

    Write-Host ""
    Write-Host "Generated app Kubernetes validation completed successfully." -ForegroundColor Green
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
