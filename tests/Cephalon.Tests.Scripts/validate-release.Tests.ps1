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
            [switch]$OmitEventingOperationalSuperiorityEvidence,
            [switch]$BlockEventingOperationalSuperiorityEvidence,
            [switch]$DriftEventingOperationalSuperiorityRuntimeConcordance,
            [switch]$OmitTestCoverageEvidence,
            [switch]$OpenTestCoverageQueue
        )

        $providerIntegrationEvidence = [ordered]@{
            EvidenceRowCount = 33
            LiveProofCount = 33
            CompositionOnlyCount = 0
            ExternalServiceGateCount = 14
            DefaultSkippedCount = 14
            RuntimeContractCount = 104
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
            '$schemaVersion' = "1.25.0"
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
                SignedReleaseDryRun = [ordered]@{
                    Status = "blocked"
                    CurrentProofState = "partial"
                    CurrentBlockerClass = "dispatch-identity-actions-disabled"
                    RequiredCommand = "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated"
                    OutputPath = "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json"
                    HandoffOutputPath = "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md"
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

        if (-not $OmitEventingOperationalSuperiorityEvidence) {
            $scorecard.EventingOperationalSuperiorityEvidence = [ordered]@{
                Status = if ($BlockEventingOperationalSuperiorityEvidence) { "partial" } else { "claimed" }
                RequiredDimensionCount = 6
                CoveredDimensionCount = if ($BlockEventingOperationalSuperiorityEvidence) { 5 } else { 6 }
                PartialDimensionCount = if ($BlockEventingOperationalSuperiorityEvidence) { 1 } else { 0 }
                MissingDimensionCount = 0
                CoveragePercent = if ($BlockEventingOperationalSuperiorityEvidence) { 83 } else { 100 }
                PromotionGate = if ($BlockEventingOperationalSuperiorityEvidence) { "blocked" } else { "allowed" }
                PromotionAllowed = -not $BlockEventingOperationalSuperiorityEvidence
                PromotionEvidenceContract = "cephalon-eventing-operational-superiority-promotion-v1"
                PromotionEvidenceContractVersion = "1.0.0"
                PromotionTarget = "eventing-operational-superiority"
                PromotionRequiredStatus = "claimed"
                PromotionDecisionCode = if ($BlockEventingOperationalSuperiorityEvidence) { "missing-required-dimensions" } else { "all-required-dimensions-claimed" }
                WolverineRequired = $false
                RuntimeConcordanceStatus = if ($DriftEventingOperationalSuperiorityRuntimeConcordance) { "drifted" } else { "matched" }
                RuntimeConcordanceSource = "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs"
                RuntimeConcordanceTokenCount = 19
                RuntimeConcordanceMatchedTokenCount = if ($DriftEventingOperationalSuperiorityRuntimeConcordance) { 18 } else { 19 }
                RuntimeConcordanceMissingTokenCount = if ($DriftEventingOperationalSuperiorityRuntimeConcordance) { 1 } else { 0 }
            }
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

    It "writes an investigation timing report when elapsed time exceeds target" {
        Write-SreReleaseValidationStepTiming `
            -FileName "validate-release-wall-time.json" `
            -SliId "engine.validate-release.wall-time" `
            -StepName "Validate release (canonical full run)" `
            -Command "pwsh scripts/validate-release.ps1" `
            -ElapsedMilliseconds 1500000.1 `
            -TargetMilliseconds 1500000 `
            -OutputPath $script:tempRoot

        $report = Get-Content -LiteralPath (Join-Path $script:tempRoot "validate-release-wall-time.json") -Raw | ConvertFrom-Json -Depth 8

        $report.sliId | Should -Be "engine.validate-release.wall-time"
        $report.status | Should -Be "investigate"
        $report.targetExceeded | Should -BeTrue
        $report.excessMilliseconds | Should -BeGreaterThan 0
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
        $output | Should -Match "Provider integration evidence: 33 rows; live proofs 33; composition-only 0; external-service gates 14; default-skipped 14; runtime contracts 104; dependency-health providers 18 from scripts/observability-dependency-health-providers\.json schema 1\.0\.0 \(source-derived-provider-family-contract\)\."
        $output | Should -Match "Eventing operational-superiority evidence: contract cephalon-eventing-operational-superiority-promotion-v1 1\.0\.0; target eventing-operational-superiority; status claimed; required claimed; dimensions 6/6 covered, partial 0, missing 0; coverage 100%; promotion gate allowed; promotion allowed True; decision all-required-dimensions-claimed; runtime concordance matched \(19/19 tokens, missing 0\) from src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor\.cs; Wolverine required False\."
        $output | Should -Match "SRE posture: 11 SLIs; target-declared 11; pending stable baselines 1; stable baselines 10; stable baseline rows 10; stable baseline measurements 12; pending baseline rows 1; blockers 1; pending evidence 1; guardrail-mapped 6; pending guardrail coverage 0; guardrail not-applicable 5; summary mode release-validation-console-and-scorecard-artifact; stable baseline manifest scripts/sre-stable-baselines\.json\."
        $output | Should -Match "Test coverage evidence: 8 layered projects; gap criteria 4; recommendations 11; shipped 10; gated 1; active gaps 0; open quarantine entries 0; queue status empty\."
        $output | Should -Match "Supply-chain release evidence: 12 items; workflow-ready 9; external policy pending 3; external policy preflight checks 3; preflight status required-before-real-tag-push; blocked 0; status workflow-ready-external-policy-pending\."
        $output | Should -Match "Signed-release dry-run evidence: status blocked; proof partial; blocker dispatch-identity-actions-disabled; required command pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated; output artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json; handoff artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md\."
        $output | Should -Match "Engine completion scorecard hard-blocker gate: no blocked platform gates, no supply-chain blocked items, no public API removals, no active test-coverage gaps or open quarantine entries, and eventing operational-superiority promotion is runtime-concordant without Wolverine\."
    }

    It "fails when provider integration dependency-health manifest readback is missing" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -OmitDependencyHealthProviderManifest) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*Engine completion scorecard JSON is missing ProviderIntegrationEvidence.DependencyHealthProviderManifest.*"
    }

    It "fails when eventing operational-superiority evidence is missing" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -OmitEventingOperationalSuperiorityEvidence) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*Engine completion scorecard JSON is missing EventingOperationalSuperiorityEvidence.*"
    }

    It "fails when eventing operational-superiority promotion is blocked" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -BlockEventingOperationalSuperiorityEvidence) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*eventing operational-superiority promotion: status partial; gate blocked; required claimed; decision missing-required-dimensions; promotionAllowed False; runtimeConcordance matched; runtimeConcordanceMissingTokens 0; wolverineRequired False; partial dimensions 1; missing dimensions 0*"
    }

    It "fails when eventing operational-superiority runtime concordance drifts" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -DriftEventingOperationalSuperiorityRuntimeConcordance) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*eventing operational-superiority promotion: status claimed; gate allowed; required claimed; decision all-required-dimensions-claimed; promotionAllowed True; runtimeConcordance drifted; runtimeConcordanceMissingTokens 1; wolverineRequired False; partial dimensions 0; missing dimensions 0*"
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

    It "fails when supply-chain evidence omits signed-release dry-run readback" {
        $scorecard = New-ScorecardFixture
        $scorecard.SupplyChainEvidence.Remove("SignedReleaseDryRun")
        Write-ScorecardFixture -Scorecard $scorecard -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*Engine completion scorecard JSON is missing SupplyChainEvidence.SignedReleaseDryRun.*"
    }
}
