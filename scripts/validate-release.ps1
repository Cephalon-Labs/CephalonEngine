param(
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipBenchmarks,
    [switch]$SkipReferenceDocs,
    [string[]]$BenchmarkFilters = @(
        "*EngineBuilderBenchmarks*",
        "*EngineRuntimeBenchmarks*",
        "*ScaffoldGeneratorBenchmarks*"
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "CephalonEngine.slnx"
$testsProjectPath = Join-Path $repoRoot "tests\Cephalon.Tests\Cephalon.Tests.csproj"
$benchmarkProjectPath = Join-Path $repoRoot "benchmarks\Cephalon.Benchmarks\Cephalon.Benchmarks.csproj"
$referenceDocsScriptPath = Join-Path $repoRoot "scripts\publish-reference-docs.ps1"
$referenceDocsOutputPath = Join-Path $repoRoot "artifacts\reference-docs-release"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Action
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

function Invoke-PowerShellScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & powershell -ExecutionPolicy Bypass -File $Path @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "PowerShell script failed: powershell -ExecutionPolicy Bypass -File $Path $($Arguments -join ' ')"
    }
}

Push-Location $repoRoot
try {
    if (-not $SkipBuild) {
        Invoke-Step "Build solution (Release)" {
            Invoke-DotNet @("build", $solutionPath, "-c", "Release")
        }
    }

    if (-not $SkipTests) {
        Invoke-Step "Run tests (Release)" {
            $arguments = @("test", $testsProjectPath, "-c", "Release")
            if (-not $SkipBuild) {
                $arguments += "--no-build"
            }

            Invoke-DotNet $arguments
        }
    }

    if (-not $SkipBenchmarks) {
        Invoke-Step "Run benchmark smoke suite (Release)" {
            $arguments = @("run", "-c", "Release", "--project", $benchmarkProjectPath, "--", "--filter")
            $arguments += $BenchmarkFilters
            Invoke-DotNet $arguments
        }

        Invoke-Step "Validate benchmark guardrails" {
            Invoke-DotNet @("run", "-c", "Release", "--project", $benchmarkProjectPath, "--", "--validate-guardrails")
        }
    }

    if (-not $SkipReferenceDocs) {
        Invoke-Step "Publish reference docs (Release)" {
            $arguments = @(
                "-Configuration", "Release",
                "-OutputPath", $referenceDocsOutputPath
            )

            if (-not $SkipBuild) {
                $arguments += "-SkipBuild"
            }

            Invoke-PowerShellScript -Path $referenceDocsScriptPath -Arguments $arguments
        }
    }

    Write-Host ""
    Write-Host "Release validation completed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
