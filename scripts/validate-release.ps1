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
        "*ColdStartBenchmarks*",
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
$sreReleaseValidationOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "sre-release-validation")
$referenceDocsOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "reference-docs-release")
$packageArtifactsOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "packages-release")
$publicApiDeltaScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "summarise-public-api-deltas.ps1")
$publicApiDeltaOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "public-api-delta-release", "public-api-delta.md")
$publicApiDeltaJsonOutputPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "public-api-delta-release", "public-api-delta.json")
$releaseValidationWallTimeTargetMilliseconds = 1500000
$canonicalBenchmarkFilters = @(
    "*EngineBuilderBenchmarks*",
    "*EngineRuntimeBenchmarks*",
    "*ColdStartBenchmarks*",
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

function Get-DeploymentModeReleaseValidationPolicy {
    param([Parameter(Mandatory = $true)] [string]$ManifestPath)

    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        throw "Deployment-mode support manifest not found: $ManifestPath"
    }

    $manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
    if ($manifest.PSObject.Properties.Match("publishProbePolicy").Count -eq 0) {
        return [pscustomobject]@{
            ReleaseValidationMode            = "audit-only"
            ReleaseValidationDeploymentModes = @("all")
            ReleaseValidationSkipsPublish    = $true
        }
    }

    $policy = $manifest.publishProbePolicy
    $releaseValidationMode = "audit-only"
    if ($policy.PSObject.Properties.Match("releaseValidationMode").Count -gt 0 -and
        -not [string]::IsNullOrWhiteSpace([string]$policy.releaseValidationMode)) {
        $releaseValidationMode = [string]$policy.releaseValidationMode
    }

    $releaseValidationSkipsPublish = $true
    if ($policy.PSObject.Properties.Match("releaseValidationSkipsPublish").Count -gt 0) {
        if ($policy.releaseValidationSkipsPublish -is [bool]) {
            $releaseValidationSkipsPublish = $policy.releaseValidationSkipsPublish
        }
        else {
            $releaseValidationSkipsPublish = [System.Convert]::ToBoolean([string]$policy.releaseValidationSkipsPublish, [System.Globalization.CultureInfo]::InvariantCulture)
        }
    }

    $releaseValidationDeploymentModes = @()
    if ($policy.PSObject.Properties.Match("releaseValidationDeploymentModes").Count -gt 0) {
        $releaseValidationDeploymentModes = @(
            $policy.releaseValidationDeploymentModes |
                ForEach-Object { [string]$_ } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
    }
    if ($releaseValidationDeploymentModes.Count -eq 0) {
        $releaseValidationDeploymentModes = @("all")
    }

    return [pscustomobject]@{
        ReleaseValidationMode            = $releaseValidationMode
        ReleaseValidationDeploymentModes = $releaseValidationDeploymentModes
        ReleaseValidationSkipsPublish    = $releaseValidationSkipsPublish
    }
}

function Get-RepositoryCommit {
    $commit = (& git -C $repoRoot rev-parse HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace([string]$commit)) {
        return "unknown"
    }

    return [string]$commit
}

function Write-SreReleaseValidationStepTiming {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName,
        [Parameter(Mandatory = $true)]
        [string]$SliId,
        [Parameter(Mandatory = $true)]
        [string]$StepName,
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [Parameter(Mandatory = $true)]
        [double]$ElapsedMilliseconds,
        [Parameter(Mandatory = $true)]
        [int]$TargetMilliseconds,
        [string]$OutputPath = $sreReleaseValidationOutputPath
    )

    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

    if ($ElapsedMilliseconds -le 0) {
        throw "SRE timing for '$SliId' must be greater than zero milliseconds."
    }

    if ($ElapsedMilliseconds -gt $TargetMilliseconds) {
        throw "SRE timing for '$SliId' exceeded target: $([math]::Round($ElapsedMilliseconds, 4))ms > ${TargetMilliseconds}ms."
    }

    [ordered]@{
        '$schemaVersion' = "1.0.0"
        sliId = $SliId
        stepName = $StepName
        command = $Command
        status = "passed"
        elapsedMilliseconds = [math]::Round($ElapsedMilliseconds, 4)
        targetMilliseconds = $TargetMilliseconds
        capturedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
        capturedFromCommit = Get-RepositoryCommit
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputPath $FileName) -Encoding UTF8
}

function Test-IsCanonicalReleaseValidationRun {
    if ($SkipRestore -or
        $SkipBuild -or
        $SkipTests -or
        $SkipDotNetReadiness -or
        $SkipDeploymentModeClaims -or
        $SkipEngineCompletionScorecard -or
        $SkipOperationalConventions -or
        $SkipPhase8Conventions -or
        $SkipBenchmarks -or
        $SkipPackages -or
        $SkipPublicApiDeltaSummary -or
        $SkipReferenceDocs) {
        return $false
    }

    if ($BenchmarkFilters.Count -ne $canonicalBenchmarkFilters.Count) {
        return $false
    }

    for ($index = 0; $index -lt $canonicalBenchmarkFilters.Count; $index++) {
        if ($BenchmarkFilters[$index] -ne $canonicalBenchmarkFilters[$index]) {
            return $false
        }
    }

    return $true
}

function Get-ScorecardIntegerProperty {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Object,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName
    )

    if ($null -eq $Object) {
        return 0
    }

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value) {
        return 0
    }

    return [System.Convert]::ToInt32($property.Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Get-ScorecardBooleanProperty {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Object,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName
    )

    if ($null -eq $Object) {
        return $false
    }

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value) {
        return $false
    }

    return [System.Convert]::ToBoolean($property.Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Get-ScorecardRequiredPropertyValue {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Object,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName,
        [Parameter(Mandatory = $true)]
        [string]$OwnerName
    )

    if ($null -eq $Object) {
        throw "Engine completion scorecard JSON is missing $OwnerName."
    }

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value) {
        throw "Engine completion scorecard JSON is missing $OwnerName.$PropertyName."
    }

    return $property.Value
}

function Assert-EngineCompletionScorecardHardBlockers {
    param([Parameter(Mandatory = $true)] [object]$Scorecard)

    $hardBlockers = [System.Collections.Generic.List[string]]::new()
    $summary = $Scorecard.Summary

    $blockedPlatformGates = Get-ScorecardIntegerProperty -Object $summary -PropertyName "BlockedPlatformGates"
    if ($blockedPlatformGates -gt 0) {
        $blockedGateNames = @(
            $Scorecard.PlatformGates |
                Where-Object { @($_.Statuses) -contains "blocked" } |
                ForEach-Object { $_.Gate }
        )
        $detail = "blocked platform gates: $blockedPlatformGates"
        if ($blockedGateNames.Count -gt 0) {
            $detail = "$detail ($($blockedGateNames -join ', '))"
        }

        $hardBlockers.Add($detail)
    }

    $supplyChainBlockedCount = Get-ScorecardIntegerProperty -Object $Scorecard.SupplyChainEvidence -PropertyName "BlockedCount"
    if ($supplyChainBlockedCount -gt 0) {
        $blockedEvidenceIds = @(
            $Scorecard.SupplyChainEvidence.EvidenceItems |
                Where-Object { $_.Status -eq "blocked" } |
                ForEach-Object { $_.Id }
        )
        $detail = "supply-chain blocked items: $supplyChainBlockedCount"
        if ($blockedEvidenceIds.Count -gt 0) {
            $detail = "$detail ($($blockedEvidenceIds -join ', '))"
        }

        $hardBlockers.Add($detail)
    }

    $publicApiRemovalCount = Get-ScorecardIntegerProperty -Object $Scorecard.PublicApiCompatibilityEvidence -PropertyName "RemovalEntryCount"
    $summaryPublicApiRemovalCount = Get-ScorecardIntegerProperty -Object $summary -PropertyName "PublicApiRemovalEntryCount"
    if ($summaryPublicApiRemovalCount -gt $publicApiRemovalCount) {
        $publicApiRemovalCount = $summaryPublicApiRemovalCount
    }

    $hasPublicApiRemovals = Get-ScorecardBooleanProperty -Object $Scorecard.PublicApiCompatibilityEvidence -PropertyName "HasRemovalEntries"
    if ($publicApiRemovalCount -gt 0 -or $hasPublicApiRemovals) {
        $hardBlockers.Add("public API removal entries: $publicApiRemovalCount")
    }

    if ($hardBlockers.Count -gt 0) {
        throw "Engine completion scorecard has hard release blocker(s): $($hardBlockers -join '; ')."
    }

    Write-Host "Engine completion scorecard hard-blocker gate: no blocked platform gates, no supply-chain blocked items, and no public API removals."
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

    $deploymentModeClaimsReport = Get-ScorecardRequiredPropertyValue `
        -Object $scorecard.DeploymentModeEvidence `
        -PropertyName "ClaimsReport" `
        -OwnerName "DeploymentModeEvidence"
    $deploymentModeClaimsReportPresent = [System.Convert]::ToBoolean(
        (Get-ScorecardRequiredPropertyValue `
            -Object $scorecard.DeploymentModeEvidence `
            -PropertyName "ClaimsReportPresent" `
            -OwnerName "DeploymentModeEvidence"),
        [System.Globalization.CultureInfo]::InvariantCulture)
    $deploymentModeClaimsReportGateStatus = Get-ScorecardRequiredPropertyValue `
        -Object $scorecard.DeploymentModeEvidence `
        -PropertyName "ClaimsReportPublishProbeGateStatus" `
        -OwnerName "DeploymentModeEvidence"
    $deploymentModeClaimsReportTargetCount = [System.Convert]::ToInt32(
        (Get-ScorecardRequiredPropertyValue `
            -Object $scorecard.DeploymentModeEvidence `
            -PropertyName "ClaimsReportPublishProbeTargetCount" `
            -OwnerName "DeploymentModeEvidence"),
        [System.Globalization.CultureInfo]::InvariantCulture)
    $deploymentModeClaimsReportWarningCount = [System.Convert]::ToInt32(
        (Get-ScorecardRequiredPropertyValue `
            -Object $scorecard.DeploymentModeEvidence `
            -PropertyName "ClaimsReportPublishProbeWarningCount" `
            -OwnerName "DeploymentModeEvidence"),
        [System.Globalization.CultureInfo]::InvariantCulture)
    $deploymentModeClaimsReportErrorCount = [System.Convert]::ToInt32(
        (Get-ScorecardRequiredPropertyValue `
            -Object $scorecard.DeploymentModeEvidence `
            -PropertyName "ClaimsReportPublishProbeErrorCount" `
            -OwnerName "DeploymentModeEvidence"),
        [System.Globalization.CultureInfo]::InvariantCulture)
    $deploymentModeClaimsReportTruthfulClaimCount = [System.Convert]::ToInt32(
        (Get-ScorecardRequiredPropertyValue `
            -Object $scorecard.DeploymentModeEvidence `
            -PropertyName "ClaimsReportPackageClaimTruthfulCount" `
            -OwnerName "DeploymentModeEvidence"),
        [System.Globalization.CultureInfo]::InvariantCulture)
    $deploymentModeClaimsReportOverstatedClaimCount = [System.Convert]::ToInt32(
        (Get-ScorecardRequiredPropertyValue `
            -Object $scorecard.DeploymentModeEvidence `
            -PropertyName "ClaimsReportPackageClaimOverstatedCount" `
            -OwnerName "DeploymentModeEvidence"),
        [System.Globalization.CultureInfo]::InvariantCulture)

    if (-not $deploymentModeClaimsReportPresent -or
        $deploymentModeClaimsReportGateStatus -ne "passed" -or
        $deploymentModeClaimsReportWarningCount -ne 0 -or
        $deploymentModeClaimsReportErrorCount -ne 0 -or
        $deploymentModeClaimsReportOverstatedClaimCount -ne 0) {
        throw "Engine completion scorecard deployment-mode claims report readback is not release-ready: report '$deploymentModeClaimsReport', present=$deploymentModeClaimsReportPresent, gate=$deploymentModeClaimsReportGateStatus, warnings=$deploymentModeClaimsReportWarningCount, errors=$deploymentModeClaimsReportErrorCount, overstatedPackageClaims=$deploymentModeClaimsReportOverstatedClaimCount."
    }

    $dependencyHealthProviderManifest = Get-ScorecardRequiredPropertyValue `
        -Object $scorecard.ProviderIntegrationEvidence `
        -PropertyName "DependencyHealthProviderManifest" `
        -OwnerName "ProviderIntegrationEvidence"
    $dependencyHealthProviderManifestReference = Get-ScorecardRequiredPropertyValue `
        -Object $dependencyHealthProviderManifest `
        -PropertyName "Reference" `
        -OwnerName "ProviderIntegrationEvidence.DependencyHealthProviderManifest"
    $dependencyHealthProviderManifestSchemaVersion = Get-ScorecardRequiredPropertyValue `
        -Object $dependencyHealthProviderManifest `
        -PropertyName "ManifestSchemaVersion" `
        -OwnerName "ProviderIntegrationEvidence.DependencyHealthProviderManifest"
    $dependencyHealthProviderManifestStatus = Get-ScorecardRequiredPropertyValue `
        -Object $dependencyHealthProviderManifest `
        -PropertyName "Status" `
        -OwnerName "ProviderIntegrationEvidence.DependencyHealthProviderManifest"
    $dependencyHealthProviderCount = [System.Convert]::ToInt32(
        (Get-ScorecardRequiredPropertyValue `
            -Object $dependencyHealthProviderManifest `
            -PropertyName "ProviderCount" `
            -OwnerName "ProviderIntegrationEvidence.DependencyHealthProviderManifest"),
        [System.Globalization.CultureInfo]::InvariantCulture)

    if ($null -eq $scorecard.SupplyChainEvidence) {
        throw "Engine completion scorecard JSON is missing SupplyChainEvidence."
    }

    if ($null -eq $scorecard.PublicApiCompatibilityEvidence) {
        throw "Engine completion scorecard JSON is missing PublicApiCompatibilityEvidence."
    }

    $sreStableBaselineManifest = Get-ScorecardRequiredPropertyValue `
        -Object $scorecard.SrePostureEvidence `
        -PropertyName "StableBaselineManifest" `
        -OwnerName "SrePostureEvidence"
    $sreStableBaselineRowCount = [System.Convert]::ToInt32(
        (Get-ScorecardRequiredPropertyValue `
            -Object $scorecard.SrePostureEvidence `
            -PropertyName "StableBaselineRowCount" `
            -OwnerName "SrePostureEvidence"),
        [System.Globalization.CultureInfo]::InvariantCulture)
    $sreStableBaselineMeasurementCount = [System.Convert]::ToInt32(
        (Get-ScorecardRequiredPropertyValue `
            -Object $scorecard.SrePostureEvidence `
            -PropertyName "StableBaselineMeasurementCount" `
            -OwnerName "SrePostureEvidence"),
        [System.Globalization.CultureInfo]::InvariantCulture)

    Write-Host ("Deployment-mode evidence: {0} global claims; not-claimed {1}; package-scoped claim packages {2}; known hazards {3}; transitive audit entries {4}; publish probes {5}; claims report {6}; gate {7}; targets {8}; warnings {9}; errors {10}; truthful package claims {11}; overstated package claims {12}." -f `
        $scorecard.DeploymentModeEvidence.GlobalClaimCount,
        $scorecard.DeploymentModeEvidence.GlobalNotClaimedCount,
        $scorecard.DeploymentModeEvidence.PackageScopedClaimPackageCount,
        $scorecard.DeploymentModeEvidence.KnownHazardEntryCount,
        $scorecard.DeploymentModeEvidence.TransitiveAuditEntryCount,
        $scorecard.DeploymentModeEvidence.PublishProbeReleaseValidationMode,
        $deploymentModeClaimsReport,
        $deploymentModeClaimsReportGateStatus,
        $deploymentModeClaimsReportTargetCount,
        $deploymentModeClaimsReportWarningCount,
        $deploymentModeClaimsReportErrorCount,
        $deploymentModeClaimsReportTruthfulClaimCount,
        $deploymentModeClaimsReportOverstatedClaimCount)

    Write-Host ("Provider integration evidence: {0} rows; live proofs {1}; composition-only {2}; external-service gates {3}; default-skipped {4}; runtime contracts {5}; dependency-health providers {6} from {7} schema {8} ({9})." -f `
        $scorecard.ProviderIntegrationEvidence.EvidenceRowCount,
        $scorecard.ProviderIntegrationEvidence.LiveProofCount,
        $scorecard.ProviderIntegrationEvidence.CompositionOnlyCount,
        $scorecard.ProviderIntegrationEvidence.ExternalServiceGateCount,
        $scorecard.ProviderIntegrationEvidence.DefaultSkippedCount,
        $scorecard.ProviderIntegrationEvidence.RuntimeContractCount,
        $dependencyHealthProviderCount,
        $dependencyHealthProviderManifestReference,
        $dependencyHealthProviderManifestSchemaVersion,
        $dependencyHealthProviderManifestStatus)

    Write-Host ("SRE posture: {0} SLIs; target-declared {1}; pending stable baselines {2}; stable baselines {3}; stable baseline rows {4}; stable baseline measurements {5}; guardrail-mapped {6}; pending guardrail coverage {7}; guardrail not-applicable {8}; summary mode {9}; stable baseline manifest {10}." -f `
        $scorecard.SrePostureEvidence.SliCount,
        $scorecard.SrePostureEvidence.TargetDeclaredCount,
        $scorecard.SrePostureEvidence.PendingStableBaselineCount,
        $scorecard.SrePostureEvidence.StableBaselineCount,
        $sreStableBaselineRowCount,
        $sreStableBaselineMeasurementCount,
        $scorecard.SrePostureEvidence.GuardrailMappedSliCount,
        $scorecard.SrePostureEvidence.GuardrailPendingSliCount,
        $scorecard.SrePostureEvidence.GuardrailNotApplicableSliCount,
        $scorecard.SrePostureEvidence.ReleaseValidationSummaryMode,
        $sreStableBaselineManifest)

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

    Assert-EngineCompletionScorecardHardBlockers -Scorecard $scorecard
}

if ($env:CEPHALON_VALIDATE_RELEASE_NO_RUN -eq "1") {
    return
}

Push-Location $repoRoot
$releaseValidationStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
try {
    foreach ($testProjectPath in $testProjectPaths) {
        if (-not (Test-Path -LiteralPath $testProjectPath)) {
            throw "Expected test project '$testProjectPath' was not found."
        }
    }

    if (-not $SkipRestore) {
        Invoke-Step "Restore solution (locked mode)" {
            $restoreStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            Invoke-DotNet @("restore", $solutionPath, "--locked-mode")
            $restoreStopwatch.Stop()
            Write-SreReleaseValidationStepTiming `
                -FileName "restore-wall-time.json" `
                -SliId "engine.dotnet.restore.wall-time.lock-mode" `
                -StepName "Restore solution (locked mode)" `
                -Command "dotnet restore CephalonEngine.slnx --locked-mode" `
                -ElapsedMilliseconds $restoreStopwatch.Elapsed.TotalMilliseconds `
                -TargetMilliseconds 90000
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
        $releaseValidationPolicy = Get-DeploymentModeReleaseValidationPolicy -ManifestPath $deploymentModeSupportManifestPath
        $releaseValidationDeploymentModes = @($releaseValidationPolicy.ReleaseValidationDeploymentModes)
        $releaseValidationSkipsPublish = $releaseValidationPolicy.ReleaseValidationSkipsPublish
        $deploymentModeStepName = if ($releaseValidationSkipsPublish) {
            "Validate deployment-mode claim truthfulness (audit-only)"
        }
        else {
            "Validate deployment-mode claim truthfulness (publish-required)"
        }
        $deploymentModeStepName = "$deploymentModeStepName [$($releaseValidationDeploymentModes -join ', ')]"

        Invoke-Step $deploymentModeStepName {
            foreach ($releaseValidationDeploymentMode in $releaseValidationDeploymentModes) {
                $modeOutputPath = if ($releaseValidationDeploymentModes.Count -eq 1) {
                    $deploymentModeClaimsOutputPath
                }
                else {
                    Join-Path $deploymentModeClaimsOutputPath $releaseValidationDeploymentMode
                }

                $deploymentModeClaimArguments = @(
                    "-DeploymentMode", $releaseValidationDeploymentMode,
                    "-Configuration", "Release",
                    "-OutputPath", $modeOutputPath
                )
                if ($releaseValidationSkipsPublish) {
                    $deploymentModeClaimArguments += "-SkipPublish"
                }

                Invoke-PowerShellScript -Path $deploymentModeClaimsScriptPath -Arguments $deploymentModeClaimArguments
            }
        }
    }

    if (-not $SkipReferenceDocs) {
        Invoke-Step "Publish reference docs (Release)" {
            $arguments = @(
                "-Configuration", "Release",
                "-OutputPath", $referenceDocsOutputPath
            )
            $referenceDocsCommand = "pwsh scripts/publish-reference-docs.ps1 -Configuration Release -OutputPath artifacts/reference-docs-release"

            if (-not $SkipBuild) {
                $arguments += "-SkipBuild"
                $referenceDocsCommand = "$referenceDocsCommand -SkipBuild"
            }

            $referenceDocsStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            Invoke-PowerShellScript -Path $referenceDocsScriptPath -Arguments $arguments
            $referenceDocsStopwatch.Stop()
            Write-SreReleaseValidationStepTiming `
                -FileName "reference-docs-wall-time.json" `
                -SliId "engine.reference-docs.wall-time" `
                -StepName "Publish reference docs (Release)" `
                -Command $referenceDocsCommand `
                -ElapsedMilliseconds $referenceDocsStopwatch.Elapsed.TotalMilliseconds `
                -TargetMilliseconds 300000
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

    if (Test-IsCanonicalReleaseValidationRun) {
        $releaseValidationStopwatch.Stop()
        Write-SreReleaseValidationStepTiming `
            -FileName "validate-release-wall-time.json" `
            -SliId "engine.validate-release.wall-time" `
            -StepName "Validate release (canonical full run)" `
            -Command "pwsh scripts/validate-release.ps1" `
            -ElapsedMilliseconds $releaseValidationStopwatch.Elapsed.TotalMilliseconds `
            -TargetMilliseconds $releaseValidationWallTimeTargetMilliseconds
    }

    Write-Host ""
    Write-Host "Release validation completed successfully." -ForegroundColor Green
}
finally {
    if ($releaseValidationStopwatch.IsRunning) {
        $releaseValidationStopwatch.Stop()
    }

    Pop-Location
}
