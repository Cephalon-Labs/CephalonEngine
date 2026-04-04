param(
    [string]$Image = "replace-with-registry/cephalon-template-app:latest",
    [string]$Namespace = "cephalon-template-app",
    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
    [string]$ManifestRoot = $PSScriptRoot,
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

function Test-KubernetesName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value -match '^[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?$'
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Contents
    )

    [System.IO.File]::WriteAllText($Path, $Contents, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-Kubectl {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        $output = & kubectl @Arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "kubectl command failed: kubectl $($Arguments -join ' ')"
        }

        return ($output | Out-String)
    }
    finally {
        Pop-Location
    }
}

function New-RenderedManifestSet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestRoot
    )

    $renderRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-kubernetes-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $renderRoot -Force | Out-Null

    Copy-Item -Path (Join-Path $ResolvedManifestRoot '*') -Destination $renderRoot -Recurse -Force

    $kustomizationPath = Join-Path $renderRoot "kustomization.yaml"
    $namespacePath = Join-Path $renderRoot "namespace.yaml"
    $deploymentPath = Join-Path $renderRoot "deployment.yaml"

    $kustomizationContents = (Get-Content -LiteralPath $kustomizationPath -Raw).Replace("namespace: cephalon-template-app", "namespace: $Namespace")
    $namespaceContents = (Get-Content -LiteralPath $namespacePath -Raw).Replace("name: cephalon-template-app", "name: $Namespace")
    $deploymentContents = (Get-Content -LiteralPath $deploymentPath -Raw).Replace("image: replace-with-registry/cephalon-template-app:latest", "image: $Image")

    Write-Utf8File -Path $kustomizationPath -Contents $kustomizationContents
    Write-Utf8File -Path $namespacePath -Contents $namespaceContents
    Write-Utf8File -Path $deploymentPath -Contents $deploymentContents

    $manifest = Invoke-Kubectl -WorkingDirectory $renderRoot -Arguments @("kustomize", ".")
    $manifestPath = Join-Path $renderRoot "rendered-manifest.yaml"
    Write-Utf8File -Path $manifestPath -Contents $manifest

    return @{
        RenderRoot = $renderRoot
        Manifest = $manifest
        ManifestPath = $manifestPath
    }
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Expected generated app root at '$SourceRoot'."
}

if (-not (Test-Path -LiteralPath $ManifestRoot)) {
    throw "Expected generated Kubernetes manifest root at '$ManifestRoot'."
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$resolvedManifestRoot = (Resolve-Path -LiteralPath $ManifestRoot).Path
$dockerfilePath = Join-Path $resolvedSourceRoot "Dockerfile"
$nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"

foreach ($path in @(
    $dockerfilePath,
    $nuGetConfigPath,
    (Join-Path $resolvedManifestRoot "kustomization.yaml"),
    (Join-Path $resolvedManifestRoot "namespace.yaml"),
    (Join-Path $resolvedManifestRoot "deployment.yaml"),
    (Join-Path $resolvedManifestRoot "service.yaml")))
{
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected generated asset at '$path'."
    }
}

if (-not (Test-KubernetesName -Value $Namespace)) {
    throw "Kubernetes namespace names must use lower-case letters, digits, or '-', start and end with a letter or digit, and stay under 64 characters."
}

$null = Get-Command kubectl -ErrorAction Stop

$renderedManifest = $null

try {
    $renderedManifest = New-RenderedManifestSet -ResolvedManifestRoot $resolvedManifestRoot

    if ($Preview) {
        Write-Host "Validated generated app root: $resolvedSourceRoot" -ForegroundColor Green
        Write-Host "Validated Kubernetes manifest root: $resolvedManifestRoot" -ForegroundColor Green
        Write-Host "Preview only. The rendered manifest follows:" -ForegroundColor Yellow
        Write-Host $renderedManifest.Manifest -ForegroundColor Cyan
        Write-Host "Use the same command without -Preview to apply this manifest to the current kubectl context." -ForegroundColor Yellow
        return
    }

    if (Test-PlaceholderValue -Value $Image) {
        throw "Set -Image to a pullable container image before running a live Kubernetes apply."
    }

    $null = Invoke-Kubectl -WorkingDirectory $renderedManifest.RenderRoot -Arguments @("apply", "-f", $renderedManifest.ManifestPath)

    Write-Host ""
    Write-Host "Kubernetes deployment apply completed successfully." -ForegroundColor Green
    Write-Host "Source root: $resolvedSourceRoot" -ForegroundColor Cyan
    Write-Host "Namespace: $Namespace" -ForegroundColor Cyan
    Write-Host "Image: $Image" -ForegroundColor Cyan
}
finally {
    if ($renderedManifest -and (Test-Path -LiteralPath $renderedManifest.RenderRoot)) {
        Remove-Item -LiteralPath $renderedManifest.RenderRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
