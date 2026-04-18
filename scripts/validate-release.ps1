param(
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipDotNetReadiness,
    [switch]$SkipOperationalConventions,
    [switch]$SkipPhase8Conventions,
    [switch]$SkipBenchmarks,
    [switch]$SkipPackages,
    [switch]$SkipReferenceDocs,
    [string[]]$BenchmarkFilters = @(
        "*EngineBuilderBenchmarks*",
        "*EngineRuntimeBenchmarks*",
        "*AspNetCoreRequestLoggingBenchmarks*",
        "*ScaffoldGeneratorBenchmarks*"
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "CephalonEngine.slnx"
$testProjectPaths = @(
    [System.IO.Path]::Combine($repoRoot, "tests", "Cephalon.Tests.Composition", "Cephalon.Tests.Composition.csproj"),
    [System.IO.Path]::Combine($repoRoot, "tests", "Cephalon.Tests.Hosting", "Cephalon.Tests.Hosting.csproj"),
    [System.IO.Path]::Combine($repoRoot, "tests", "Cephalon.Tests.Tooling", "Cephalon.Tests.Tooling.csproj")
)
$benchmarkProjectPath = [System.IO.Path]::Combine($repoRoot, "benchmarks", "Cephalon.Benchmarks", "Cephalon.Benchmarks.csproj")
$dotNetReadinessScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "validate-dotnet-readiness.ps1")
$referenceDocsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "publish-reference-docs.ps1")
$packageArtifactsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "publish-package-artifacts.ps1")
$operationalConventionsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "validate-operational-conventions.ps1")
$phase8ConventionsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "validate-phase8-conventions.ps1")
$dotNetReadinessOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "dotnet-readiness-release")
$referenceDocsOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "reference-docs-release")
$packageArtifactsOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "packages-release")

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

function Get-PowerShellHostPath {
    try {
        $processPath = (Get-Process -Id $PID).Path
        if (-not [string]::IsNullOrWhiteSpace($processPath) -and (Test-Path -LiteralPath $processPath)) {
            return $processPath
        }
    }
    catch {
    }

    foreach ($candidate in @("pwsh", "powershell")) {
        $command = Get-Command -Name $candidate -CommandType Application -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
            return $command.Source
        }
    }

    throw "Unable to resolve the current PowerShell host executable."
}

function Invoke-PowerShellScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $powerShellHostPath = Get-PowerShellHostPath
    & $powerShellHostPath -NoLogo -NoProfile -File $Path @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "PowerShell script failed: $Path $($Arguments -join ' ')"
    }
}

Push-Location $repoRoot
try {
    foreach ($testProjectPath in $testProjectPaths) {
        if (-not (Test-Path -LiteralPath $testProjectPath)) {
            throw "Expected test project '$testProjectPath' was not found."
        }
    }

    if (-not $SkipBuild) {
        Invoke-Step "Build solution (Release)" {
            Invoke-DotNet @("build", $solutionPath, "-c", "Release")
        }
    }

    if (-not $SkipTests) {
        Invoke-Step "Run tests (Release)" {
            foreach ($testProjectPath in $testProjectPaths) {
                $arguments = @("test", $testProjectPath, "-c", "Release")
                if (-not $SkipBuild) {
                    $arguments += @("--no-build", "--no-restore")
                }

                Invoke-DotNet $arguments
            }
        }
    }

    if (-not $SkipDotNetReadiness) {
        Invoke-Step "Validate .NET readiness contract" {
            Invoke-PowerShellScript -Path $dotNetReadinessScriptPath -Arguments @(
                "-Configuration", "Release",
                "-OutputPath", $dotNetReadinessOutputPath,
                "-SkipBuild",
                "-SkipTests",
                "-SkipReferenceDocs",
                "-SkipPackages"
            )
        }
    }

    if (-not $SkipOperationalConventions) {
        Invoke-Step "Validate operational health and export conventions (Release)" {
            $arguments = @(
                "-Configuration", "Release"
            )

            if ((-not $SkipBuild) -or (-not $SkipTests)) {
                $arguments += "-NoBuild"
            }

            Invoke-PowerShellScript -Path $operationalConventionsScriptPath -Arguments $arguments
        }
    }

    if (-not $SkipPhase8Conventions) {
        Invoke-Step "Validate phase-8 architecture, runtime, and starter conventions (Release)" {
            $arguments = @(
                "-Configuration", "Release"
            )

            if ((-not $SkipBuild) -or (-not $SkipTests)) {
                $arguments += "-NoBuild"
            }

            Invoke-PowerShellScript -Path $phase8ConventionsScriptPath -Arguments $arguments
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

    if (-not $SkipPackages) {
        Invoke-Step "Publish package artifacts (Release)" {
            $arguments = @(
                "-Configuration", "Release",
                "-OutputPath", $packageArtifactsOutputPath
            )

            if (-not $SkipBuild) {
                $arguments += "-SkipBuild"
            }

            Invoke-PowerShellScript -Path $packageArtifactsScriptPath -Arguments $arguments
        }
    }

    Write-Host ""
    Write-Host "Release validation completed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
