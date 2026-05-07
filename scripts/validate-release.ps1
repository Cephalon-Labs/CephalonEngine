param(
    [switch]$SkipRestore,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipDotNetReadiness,
    [switch]$SkipDeploymentModeClaims,
    [switch]$SkipEngineCompletionScorecard,
    [switch]$SkipOperationalConventions,
    [switch]$SkipPhase8Conventions,
    [switch]$SkipBenchmarks,
    [switch]$SkipPackages,
    [switch]$SkipPublicApiDeltaSummary,
    [switch]$SkipReferenceDocs,
    [string[]]$BenchmarkFilters = @(
        "*EngineBuilderBenchmarks*",
        "*EngineRuntimeBenchmarks*",
        "*AspNetCoreRequestLoggingBenchmarks*",
        "*RestEndpointProjectionGovernanceBenchmarks*",
        "*ScaffoldGeneratorBenchmarks*",
        "*DataDispatchBenchmarks*",
        "*BehaviorDispatchBenchmarks*",
        "*AuthorizationEvaluationBenchmarks*",
        "*TenantResolutionBenchmarks*",
        "*EventSourcingBenchmarks*",
        "*OutboxStagingBenchmarks*"
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
$deploymentModeSupportManifestPath = [System.IO.Path]::Combine($repoRoot, "scripts", "deployment-mode-support.json")
$dotNetReadinessScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "validate-dotnet-readiness.ps1")
$deploymentModeClaimsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "validate-deployment-mode-claims.ps1")
$engineCompletionScorecardScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "publish-engine-completion-scorecard.ps1")
$referenceDocsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "publish-reference-docs.ps1")
$packageArtifactsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "publish-package-artifacts.ps1")
$operationalConventionsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "validate-operational-conventions.ps1")
$phase8ConventionsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "validate-phase8-conventions.ps1")
$dotNetReadinessOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "dotnet-readiness-release")
$deploymentModeClaimsOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "deployment-mode-claims-release")
$engineCompletionScorecardOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "engine-completion-scorecard-release")
$referenceDocsOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "reference-docs-release")
$packageArtifactsOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "packages-release")
$publicApiDeltaScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "summarise-public-api-deltas.ps1")
$publicApiDeltaOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "public-api-delta-release", "public-api-delta.md")
$publicApiDeltaJsonOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "public-api-delta-release", "public-api-delta.json")

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

function Get-DeploymentModeReleaseValidationSkipsPublish {
    param([Parameter(Mandatory = $true)] [string]$ManifestPath)

    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        throw "Deployment-mode support manifest not found: $ManifestPath"
    }

    $manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
    if ($manifest.PSObject.Properties.Match("publishProbePolicy").Count -eq 0) {
        return $true
    }

    $policy = $manifest.publishProbePolicy
    if ($policy.PSObject.Properties.Match("releaseValidationSkipsPublish").Count -eq 0) {
        return $true
    }

    if ($policy.releaseValidationSkipsPublish -is [bool]) {
        return $policy.releaseValidationSkipsPublish
    }

    return [System.Convert]::ToBoolean([string]$policy.releaseValidationSkipsPublish, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Write-EngineCompletionScorecardEvidenceSummary {
    param([Parameter(Mandatory = $true)] [string]$ScorecardOutputPath)

    $scorecardJsonPath = [System.IO.Path]::Combine($ScorecardOutputPath, "engine-completion-scorecard.json")
    if (-not (Test-Path -LiteralPath $scorecardJsonPath -PathType Leaf)) {
        throw "Engine completion scorecard JSON was not found at '$scorecardJsonPath'."
    }

    $scorecard = Get-Content -LiteralPath $scorecardJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
    if ($null -eq $scorecard.DeploymentModeEvidence) {
        throw "Engine completion scorecard JSON is missing DeploymentModeEvidence."
    }

    if ($null -eq $scorecard.SrePostureEvidence) {
        throw "Engine completion scorecard JSON is missing SrePostureEvidence."
    }

    if ($null -eq $scorecard.ProviderIntegrationEvidence) {
        throw "Engine completion scorecard JSON is missing ProviderIntegrationEvidence."
    }

    if ($null -eq $scorecard.SupplyChainEvidence) {
        throw "Engine completion scorecard JSON is missing SupplyChainEvidence."
    }

    if ($null -eq $scorecard.PublicApiCompatibilityEvidence) {
        throw "Engine completion scorecard JSON is missing PublicApiCompatibilityEvidence."
    }

    Write-Host ("Deployment-mode evidence: {0} global claims; not-claimed {1}; package-scoped claim packages {2}; known hazards {3}; transitive audit entries {4}; publish probes {5}." -f `
        $scorecard.DeploymentModeEvidence.GlobalClaimCount,
        $scorecard.DeploymentModeEvidence.GlobalNotClaimedCount,
        $scorecard.DeploymentModeEvidence.PackageScopedClaimPackageCount,
        $scorecard.DeploymentModeEvidence.KnownHazardEntryCount,
        $scorecard.DeploymentModeEvidence.TransitiveAuditEntryCount,
        $scorecard.DeploymentModeEvidence.PublishProbeReleaseValidationMode)

    Write-Host ("Provider integration evidence: {0} rows; live proofs {1}; composition-only {2}; external-service gates {3}; default-skipped {4}; runtime contracts {5}." -f `
        $scorecard.ProviderIntegrationEvidence.EvidenceRowCount,
        $scorecard.ProviderIntegrationEvidence.LiveProofCount,
        $scorecard.ProviderIntegrationEvidence.CompositionOnlyCount,
        $scorecard.ProviderIntegrationEvidence.ExternalServiceGateCount,
        $scorecard.ProviderIntegrationEvidence.DefaultSkippedCount,
        $scorecard.ProviderIntegrationEvidence.RuntimeContractCount)

    Write-Host ("SRE posture: {0} SLIs; target-declared {1}; pending stable baselines {2}; stable baselines {3}; guardrail-mapped {4}; pending guardrail coverage {5}; guardrail not-applicable {6}; summary mode {7}." -f `
        $scorecard.SrePostureEvidence.SliCount,
        $scorecard.SrePostureEvidence.TargetDeclaredCount,
        $scorecard.SrePostureEvidence.PendingStableBaselineCount,
        $scorecard.SrePostureEvidence.StableBaselineCount,
        $scorecard.SrePostureEvidence.GuardrailMappedSliCount,
        $scorecard.SrePostureEvidence.GuardrailPendingSliCount,
        $scorecard.SrePostureEvidence.GuardrailNotApplicableSliCount,
        $scorecard.SrePostureEvidence.ReleaseValidationSummaryMode)

    Write-Host ("Supply-chain release evidence: {0} items; workflow-ready {1}; external policy pending {2}; blocked {3}; status {4}." -f `
        $scorecard.SupplyChainEvidence.EvidenceItemCount,
        $scorecard.SupplyChainEvidence.WorkflowReadyCount,
        $scorecard.SupplyChainEvidence.ExternalPolicyPendingCount,
        $scorecard.SupplyChainEvidence.BlockedCount,
        $scorecard.SupplyChainEvidence.Status)

    Write-Host ("Public API compatibility: {0} packages; pending packages {1}; additions {2}; removals {3}." -f `
        $scorecard.PublicApiCompatibilityEvidence.PackageCount,
        $scorecard.PublicApiCompatibilityEvidence.PendingPackageCount,
        $scorecard.PublicApiCompatibilityEvidence.AdditiveEntryCount,
        $scorecard.PublicApiCompatibilityEvidence.RemovalEntryCount)
}

Push-Location $repoRoot
try {
    foreach ($testProjectPath in $testProjectPaths) {
        if (-not (Test-Path -LiteralPath $testProjectPath)) {
            throw "Expected test project '$testProjectPath' was not found."
        }
    }

    if (-not $SkipRestore) {
        Invoke-Step "Restore solution (locked mode)" {
            Invoke-DotNet @("restore", $solutionPath, "--locked-mode")
        }
    }

    if (-not $SkipBuild) {
        Invoke-Step "Build solution (Release)" {
            $arguments = @("build", $solutionPath, "-c", "Release")
            if (-not $SkipRestore) {
                $arguments += "--no-restore"
            }

            Invoke-DotNet $arguments
        }
    }

    if (-not $SkipTests) {
        Invoke-Step "Run tests (Release)" {
            foreach ($testProjectPath in $testProjectPaths) {
                $arguments = @("test", $testProjectPath, "-c", "Release")
                if (-not $SkipBuild) {
                    $arguments += @("--no-build", "--no-restore")
                }
                elseif (-not $SkipRestore) {
                    $arguments += "--no-restore"
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

    if (-not $SkipDeploymentModeClaims) {
        $releaseValidationSkipsPublish = Get-DeploymentModeReleaseValidationSkipsPublish -ManifestPath $deploymentModeSupportManifestPath
        $deploymentModeStepName = if ($releaseValidationSkipsPublish) {
            "Validate deployment-mode claim truthfulness (audit-only)"
        }
        else {
            "Validate deployment-mode claim truthfulness (publish-required)"
        }

        Invoke-Step $deploymentModeStepName {
            $deploymentModeClaimArguments = @(
                "-DeploymentMode", "all",
                "-Configuration", "Release",
                "-OutputPath", $deploymentModeClaimsOutputPath
            )
            if ($releaseValidationSkipsPublish) {
                $deploymentModeClaimArguments += "-SkipPublish"
            }

            Invoke-PowerShellScript -Path $deploymentModeClaimsScriptPath -Arguments $deploymentModeClaimArguments
        }
    }

    if (-not $SkipEngineCompletionScorecard) {
        Invoke-Step "Publish engine completion scorecard artifact" {
            Invoke-PowerShellScript -Path $engineCompletionScorecardScriptPath -Arguments @(
                "-OutputPath", $engineCompletionScorecardOutputPath
            )
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $engineCompletionScorecardOutputPath
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

    if (-not $SkipPublicApiDeltaSummary) {
        Invoke-Step "Summarise public-API delta across PublicAPI.Unshipped.txt" {
            Invoke-PowerShellScript -Path $publicApiDeltaScriptPath -Arguments @(
                "-OutputPath", $publicApiDeltaOutputPath,
                "-JsonOutputPath", $publicApiDeltaJsonOutputPath,
                "-FailOnRemovals"
            )
        }
    }

    Write-Host ""
    Write-Host "Release validation completed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
