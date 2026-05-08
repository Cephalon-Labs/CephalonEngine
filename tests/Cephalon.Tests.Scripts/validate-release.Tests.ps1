#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/validate-release.ps1.
.DESCRIPTION
    Verifies release-validation scorecard readback behavior without running the full release flow.
#>

BeforeAll {
    $env:CEPHALON_VALIDATE_RELEASE_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\validate-release.ps1"
    . $script:scriptPath

    function New-ScorecardFixture {
        param(
            [switch]$OmitDependencyHealthProviderManifest
        )

        $providerIntegrationEvidence = [ordered]@{
            EvidenceRowCount = 32
            LiveProofCount = 14
            CompositionOnlyCount = 18
            ExternalServiceGateCount = 13
            DefaultSkippedCount = 13
            RuntimeContractCount = 94
        }

        if (-not $OmitDependencyHealthProviderManifest) {
            $providerIntegrationEvidence.DependencyHealthProviderManifest = [ordered]@{
                Reference = "scripts/observability-dependency-health-providers.json"
                ManifestSchemaVersion = "1.0.0"
                Status = "source-derived-provider-family-contract"
                ProviderCount = 18
            }
        }

        return [ordered]@{
            '$schemaVersion' = "1.10.0"
            SourceDocument = "docs/engine-completion-scorecard.md"
            DeploymentModeEvidence = [ordered]@{
                GlobalClaimCount = 3
                GlobalNotClaimedCount = 3
                PackageScopedClaimPackageCount = 1
                KnownHazardEntryCount = 14
                TransitiveAuditEntryCount = 7
                PublishProbeReleaseValidationMode = "single-file-publish-gate"
                ClaimsReport = "artifacts/deployment-mode-claims-release/claim-validation-report.json"
                ClaimsReportPresent = $true
                ClaimsReportPublishProbeGateStatus = "passed"
                ClaimsReportPublishProbeTargetCount = 5
                ClaimsReportPublishProbeWarningCount = 0
                ClaimsReportPublishProbeErrorCount = 0
                ClaimsReportPackageClaimTruthfulCount = 1
                ClaimsReportPackageClaimOverstatedCount = 0
            }
            ProviderIntegrationEvidence = $providerIntegrationEvidence
            SrePostureEvidence = [ordered]@{
                SliCount = 11
                TargetDeclaredCount = 11
                PendingStableBaselineCount = 7
                StableBaselineCount = 4
                StableBaselineManifest = "scripts/sre-stable-baselines.json"
                StableBaselineRowCount = 4
                StableBaselineMeasurementCount = 6
                GuardrailMappedSliCount = 4
                GuardrailPendingSliCount = 2
                GuardrailNotApplicableSliCount = 5
                ReleaseValidationSummaryMode = "release-validation-console-and-scorecard-artifact"
            }
            SupplyChainEvidence = [ordered]@{
                EvidenceItemCount = 10
                WorkflowReadyCount = 7
                ExternalPolicyPendingCount = 3
                BlockedCount = 0
                Status = "workflow-ready-external-policy-pending"
            }
            PublicApiCompatibilityEvidence = [ordered]@{
                PackageCount = 104
                PendingPackageCount = 22
                AdditiveEntryCount = 293
                RemovalEntryCount = 0
                HasRemovalEntries = $false
            }
            Summary = [ordered]@{
                BlockedPlatformGates = 0
                PublicApiRemovalEntryCount = 0
            }
            PlatformGates = @()
        }
    }

    function Write-ScorecardFixture {
        param(
            [Parameter(Mandatory = $true)]
            [object]$Scorecard,
            [Parameter(Mandatory = $true)]
            [string]$OutputPath
        )

        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        $jsonPath = Join-Path $OutputPath "engine-completion-scorecard.json"
        $Scorecard | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
        return $jsonPath
    }
}

AfterAll {
    Remove-Item Env:\CEPHALON_VALIDATE_RELEASE_NO_RUN -ErrorAction SilentlyContinue
}

Describe "validate-release.ps1 deployment-mode policy" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-validate-release-policy-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    It "reads the manifest-backed single-file publish gate policy" {
        $manifestPath = Join-Path $script:tempRoot "deployment-mode-support.json"
        @{
            publishProbePolicy = @{
                releaseValidationMode = "single-file-publish-gate"
                releaseValidationDeploymentModes = @("singleFile")
                releaseValidationSkipsPublish = $false
            }
        } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        $policy = Get-DeploymentModeReleaseValidationPolicy -ManifestPath $manifestPath

        $policy.ReleaseValidationMode | Should -Be "single-file-publish-gate"
        $policy.ReleaseValidationDeploymentModes | Should -Contain "singleFile"
        $policy.ReleaseValidationSkipsPublish | Should -BeFalse
    }

    It "defaults older manifests to all-mode audit-only validation" {
        $manifestPath = Join-Path $script:tempRoot "deployment-mode-support.json"
        @{
            deploymentModes = @{}
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        $policy = Get-DeploymentModeReleaseValidationPolicy -ManifestPath $manifestPath

        $policy.ReleaseValidationMode | Should -Be "audit-only"
        $policy.ReleaseValidationDeploymentModes | Should -Contain "all"
        $policy.ReleaseValidationSkipsPublish | Should -BeTrue
    }
}

Describe "validate-release.ps1 scorecard readback" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-validate-release-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    It "prints dependency-health provider manifest readback from provider integration evidence" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture) -OutputPath $script:tempRoot | Out-Null

        $output = & {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } 6>&1 | Out-String

        $output | Should -Match "Deployment-mode evidence: 3 global claims; not-claimed 3; package-scoped claim packages 1; known hazards 14; transitive audit entries 7; publish probes single-file-publish-gate; claims report artifacts/deployment-mode-claims-release/claim-validation-report\.json; gate passed; targets 5; warnings 0; errors 0; truthful package claims 1; overstated package claims 0\."
        $output | Should -Match "Provider integration evidence: 32 rows; live proofs 14; composition-only 18; external-service gates 13; default-skipped 13; runtime contracts 94; dependency-health providers 18 from scripts/observability-dependency-health-providers\.json schema 1\.0\.0 \(source-derived-provider-family-contract\)\."
        $output | Should -Match "SRE posture: 11 SLIs; target-declared 11; pending stable baselines 7; stable baselines 4; stable baseline rows 4; stable baseline measurements 6; guardrail-mapped 4; pending guardrail coverage 2; guardrail not-applicable 5; summary mode release-validation-console-and-scorecard-artifact; stable baseline manifest scripts/sre-stable-baselines\.json\."
        $output | Should -Match "Engine completion scorecard hard-blocker gate: no blocked platform gates"
    }

    It "fails when provider integration dependency-health manifest readback is missing" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -OmitDependencyHealthProviderManifest) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*Engine completion scorecard JSON is missing ProviderIntegrationEvidence.DependencyHealthProviderManifest.*"
    }
}
