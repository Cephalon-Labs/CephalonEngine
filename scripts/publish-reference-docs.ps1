param(
    [string]$Configuration = "Debug",
    [string]$TargetFramework = "net10.0",
    [string]$OutputPath,
    [string[]]$Assemblies = @(),
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "CephalonEngine.slnx"
$projectPath = Join-Path $repoRoot "src\Cephalon.ReferenceDocs\Cephalon.ReferenceDocs.csproj"
$resolvedOutputPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path $repoRoot "docs\reference"
}
else {
    [System.IO.Path]::GetFullPath($OutputPath)
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($Arguments -join ' ')"
    }
}

function Clear-OutputDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRepoRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)

    if (-not $fullPath.StartsWith($fullRepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear output directory outside the repository: $fullPath"
    }

    if (Test-Path $fullPath) {
        Get-ChildItem -Path $fullPath -Force | Remove-Item -Recurse -Force
    }
    else {
        New-Item -Path $fullPath -ItemType Directory | Out-Null
    }
}

Push-Location $repoRoot
try {
    if (-not $SkipBuild) {
        Write-Host "==> Build solution ($Configuration)" -ForegroundColor Cyan
        Invoke-DotNet @("build", $solutionPath, "-c", $Configuration)
    }

    Clear-OutputDirectory -Path $resolvedOutputPath -RepositoryRoot $repoRoot

    $arguments = @(
        "run",
        "--project", $projectPath,
        "--",
        "--root", $repoRoot,
        "--output", $resolvedOutputPath,
        "--configuration", $Configuration,
        "--target-framework", $TargetFramework
    )

    foreach ($assembly in $Assemblies) {
        if (-not [string]::IsNullOrWhiteSpace($assembly)) {
            $arguments += @("--assembly", $assembly.Trim())
        }
    }

    Write-Host "==> Publish reference docs" -ForegroundColor Cyan
    Invoke-DotNet $arguments

    Write-Host ""
    Write-Host "Reference docs published to '$resolvedOutputPath'." -ForegroundColor Green
}
finally {
    Pop-Location
}
