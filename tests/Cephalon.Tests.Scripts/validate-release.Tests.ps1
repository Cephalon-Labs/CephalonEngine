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
            [switch]$OmitDependencyHealthProviderManifest,
            [switch]$OmitTestCoverageEvidence,
            [switch]$OpenTestCoverageQueue
        )

        $providerIntegrationEvidence = [ordered]@{
            EvidenceRowCount = 33
            LiveProofCount = 33
            CompositionOnlyCount = 0
            ExternalServiceGateCount = 14
            DefaultSkippedCount = 14
            RuntimeContractCount = 99
        }

        if (-not $OmitDependencyHealthProviderManifest) {
            $providerIntegrationEvidence.DependencyHealthProviderManifest = [ordered]@{
                Reference = "scripts/observability-dependency-health-providers.json"
                ManifestSchemaVersion = "1.0.0"
                Status = "source-derived-provider-family-contract"
                ProviderCount = 18
            }
        }

        $scorecard = [ordered]@{
            '$schemaVersion' = "1.20.0"
            SourceDocument = "docs/engine-completion-scorecard.md"
            DeploymentModeEvidence = [ordered]@{
                GlobalClaimCount = 3
                GlobalNotClaimedCount = 3
                PackageScopedClaimPackageCount = 3
                KnownHazardEntryCount = 15
                TransitiveAuditEntryCount = 7
                PublishProbeReleaseValidationMode = "single-file-publish-gate"
                ClaimsReport = "artifacts/deployment-mode-claims-release/claim-validation-report.json"
                ClaimsReportPresent = $true
                ClaimsReportPublishProbeGateStatus = "passed"
                ClaimsReportPublishProbeTargetCount = 5
                ClaimsReportPublishProbeWarningCount = 0
                ClaimsReportPublishProbeErrorCount = 0
                ClaimsReportPackageClaimTruthfulCount = 3
                ClaimsReportPackageClaimOverstatedCount = 0
                ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus = "matched"
                ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount = 0
                ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus = "matched"
                ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount = 0
                ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus = "matched"
                ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount = 0
                ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus = "matched"
                ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount = 0
                ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus = "matched"
                ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount = 0
                ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus = "matched"
                ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount = 0
                ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus = "matched"
                ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount = 0
            }
            ProviderIntegrationEvidence = $providerIntegrationEvidence
            SrePostureEvidence = [ordered]@{
                SliCount = 11
                TargetDeclaredCount = 11
                PendingStableBaselineCount = 1
                StableBaselineCount = 10
                StableBaselineManifest = "scripts/sre-stable-baselines.json"
                StableBaselineRowCount = 10
                StableBaselineMeasurementCount = 12
                PendingBaselineRowCount = 1
                PendingBaselineBlockerCount = 1
                PendingBaselineEvidenceCount = 1
                GuardrailMappedSliCount = 6
                GuardrailPendingSliCount = 0
                GuardrailNotApplicableSliCount = 5
                ReleaseValidationSummaryMode = "release-validation-console-and-scorecard-artifact"
            }
            SupplyChainEvidence = [ordered]@{
                EvidenceItemCount = 12
                WorkflowReadyCount = 9
                ExternalPolicyPendingCount = 3
                ExternalPolicyPreflightCheckCount = 3
                ExternalPolicyPreflight = [ordered]@{
                    Status = "required-before-real-tag-push"
                    RequiredCheckCount = 3
                }
                BlockedCount = 0
                Status = "workflow-ready-external-policy-pending"
            }
            PublicApiCompatibilityEvidence = [ordered]@{
                PackageCount = 104
                PendingPackageCount = 0
                AdditiveEntryCount = 0
                RemovalEntryCount = 0
                HasRemovalEntries = $false
            }
            Summary = [ordered]@{
                BlockedPlatformGates = 0
                PublicApiRemovalEntryCount = 0
            }
            PlatformGates = @()
        }

        if (-not $OmitTestCoverageEvidence) {
            $scorecard.TestCoverageEvidence = [ordered]@{
                LayeredProjectCount = 8
                GapDefinitionCriterionCount = 4
                RecommendationCount = 11
                ShippedRecommendationCount = 10
                GatedRecommendationCount = 1
                ActiveGapRecommendationCount = if ($OpenTestCoverageQueue) { 1 } else { 0 }
                QuarantineEntryCount = 2
                OpenQuarantineEntryCount = if ($OpenTestCoverageQueue) { 1 } else { 0 }
                QuarantineQueueStatus = if ($OpenTestCoverageQueue) { "open" } else { "empty" }
            }
        }

        return $scorecard
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
Describe "validate-release.ps1 SRE timing output" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-validate-release-timing-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    It "recognizes the default invocation as the canonical full release-validation lane" {
        Test-IsCanonicalReleaseValidationRun | Should -BeTrue
    }

    It "writes a passed timing report when elapsed time is within target" {
        Write-SreReleaseValidationStepTiming `
            -FileName "validate-release-wall-time.json" `
            -SliId "engine.validate-release.wall-time" `
            -StepName "Validate release (canonical full run)" `
            -Command "pwsh scripts/validate-release.ps1" `
            -ElapsedMilliseconds 1234.5678 `
            -TargetMilliseconds 1500000 `
            -OutputPath $script:tempRoot

        $report = Get-Content -LiteralPath (Join-Path $script:tempRoot "validate-release-wall-time.json") -Raw | ConvertFrom-Json -Depth 8

        $report.sliId | Should -Be "engine.validate-release.wall-time"
        $report.stepName | Should -Be "Validate release (canonical full run)"
        $report.command | Should -Be "pwsh scripts/validate-release.ps1"
        $report.status | Should -Be "passed"
        $report.elapsedMilliseconds | Should -Be 1234.5678
        $report.targetMilliseconds | Should -Be 1500000
        $report.capturedFromCommit | Should -Not -BeNullOrEmpty
    }

    It "fails before writing a passed timing report when elapsed time exceeds target" {
        {
            Write-SreReleaseValidationStepTiming `
                -FileName "validate-release-wall-time.json" `
                -SliId "engine.validate-release.wall-time" `
                -StepName "Validate release (canonical full run)" `
                -Command "pwsh scripts/validate-release.ps1" `
                -ElapsedMilliseconds 1500000.1 `
                -TargetMilliseconds 1500000 `
                -OutputPath $script:tempRoot
        } | Should -Throw "*exceeded target*"

        Test-Path -LiteralPath (Join-Path $script:tempRoot "validate-release-wall-time.json") | Should -BeFalse
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

        $output | Should -Match "Deployment-mode evidence: 3 global claims; not-claimed 3; package-scoped claim packages 3; known hazards 15; transitive audit entries 7; publish probes single-file-publish-gate; claims report artifacts/deployment-mode-claims-release/claim-validation-report\.json; gate passed; targets 5; warnings 0; errors 0; truthful package claims 3; overstated package claims 0; boundary audit matched/0; core route-delegate audit matched/0; full common route-delegate audit matched/0; full operator route-delegate audit matched/0; operator response JSON contract audit matched/0; non-operator endpoint audit matched/0; framework endpoint boundary audit matched/0\."
        $output | Should -Match "Provider integration evidence: 33 rows; live proofs 33; composition-only 0; external-service gates 14; default-skipped 14; runtime contracts 99; dependency-health providers 18 from scripts/observability-dependency-health-providers\.json schema 1\.0\.0 \(source-derived-provider-family-contract\)\."
        $output | Should -Match "SRE posture: 11 SLIs; target-declared 11; pending stable baselines 1; stable baselines 10; stable baseline rows 10; stable baseline measurements 12; pending baseline rows 1; blockers 1; pending evidence 1; guardrail-mapped 6; pending guardrail coverage 0; guardrail not-applicable 5; summary mode release-validation-console-and-scorecard-artifact; stable baseline manifest scripts/sre-stable-baselines\.json\."
        $output | Should -Match "Test coverage evidence: 8 layered projects; gap criteria 4; recommendations 11; shipped 10; gated 1; active gaps 0; open quarantine entries 0; queue status empty\."
        $output | Should -Match "Supply-chain release evidence: 12 items; workflow-ready 9; external policy pending 3; external policy preflight checks 3; preflight status required-before-real-tag-push; blocked 0; status workflow-ready-external-policy-pending\."
        $output | Should -Match "Engine completion scorecard hard-blocker gate: no blocked platform gates, no supply-chain blocked items, no public API removals, and no active test-coverage gaps or open quarantine entries\."
    }

    It "fails when provider integration dependency-health manifest readback is missing" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -OmitDependencyHealthProviderManifest) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*Engine completion scorecard JSON is missing ProviderIntegrationEvidence.DependencyHealthProviderManifest.*"
    }

    It "fails when test coverage evidence is missing" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -OmitTestCoverageEvidence) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*Engine completion scorecard JSON is missing TestCoverageEvidence.*"
    }

    It "fails when test coverage reports active gaps or an open quarantine queue" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -OpenTestCoverageQueue) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*test coverage active gaps: 1; open quarantine entries: 1; queue status: open*"
    }

    It "fails when external-policy pending supply-chain evidence omits preflight readback" {
        $scorecard = New-ScorecardFixture
        $scorecard.SupplyChainEvidence.Remove("ExternalPolicyPreflight")
        Write-ScorecardFixture -Scorecard $scorecard -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*Engine completion scorecard JSON is missing SupplyChainEvidence.ExternalPolicyPreflight.*"
    }
}
