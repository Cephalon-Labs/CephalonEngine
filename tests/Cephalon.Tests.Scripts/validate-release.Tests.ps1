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
            LiveProofCount = 6
            CompositionOnlyCount = 26
            ExternalServiceGateCount = 6
            DefaultSkippedCount = 6
            RuntimeContractCount = 61
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
            '$schemaVersion' = "1.8.0"
            SourceDocument = "docs/engine-completion-scorecard.md"
            DeploymentModeEvidence = [ordered]@{
                GlobalClaimCount = 3
                GlobalNotClaimedCount = 3
                PackageScopedClaimPackageCount = 1
                KnownHazardEntryCount = 14
                TransitiveAuditEntryCount = 7
                PublishProbeReleaseValidationMode = "audit-only"
            }
            ProviderIntegrationEvidence = $providerIntegrationEvidence
            SrePostureEvidence = [ordered]@{
                SliCount = 11
                TargetDeclaredCount = 11
                PendingStableBaselineCount = 11
                StableBaselineCount = 0
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

        $output | Should -Match "Provider integration evidence: 32 rows; live proofs 6; composition-only 26; external-service gates 6; default-skipped 6; runtime contracts 61; dependency-health providers 18 from scripts/observability-dependency-health-providers\.json schema 1\.0\.0 \(source-derived-provider-family-contract\)\."
        $output | Should -Match "Engine completion scorecard hard-blocker gate: no blocked platform gates"
    }

    It "fails when provider integration dependency-health manifest readback is missing" {
        Write-ScorecardFixture -Scorecard (New-ScorecardFixture -OmitDependencyHealthProviderManifest) -OutputPath $script:tempRoot | Out-Null

        {
            Write-EngineCompletionScorecardEvidenceSummary -ScorecardOutputPath $script:tempRoot
        } | Should -Throw "*Engine completion scorecard JSON is missing ProviderIntegrationEvidence.DependencyHealthProviderManifest.*"
    }
}
