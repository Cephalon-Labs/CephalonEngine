#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/publish-engine-completion-scorecard.ps1.

.DESCRIPTION
    Verifies that the engine completion scorecard emitter reads the hand-authored
    scorecard document as a read model, emits stable JSON/Markdown artifacts, and
    fails loudly when scorecard statuses or evidence-source references drift.
#>

BeforeAll {
    $env:CEPHALON_ENGINE_COMPLETION_SCORECARD_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\publish-engine-completion-scorecard.ps1"
    . $script:scriptPath
}
AfterAll {
    Remove-Item Env:\CEPHALON_ENGINE_COMPLETION_SCORECARD_NO_RUN -ErrorAction SilentlyContinue
}

Describe "publish-engine-completion-scorecard.ps1" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-scorecard-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    function script:New-TestDeploymentModeClaimsReport {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Path,
            [Parameter(Mandatory = $true)]
            [string]$ManifestPath
        )

        $publishTargets = @(
            "samples/Cephalon.Sample.ModularMonolith/Cephalon.Sample.ModularMonolith.csproj",
            "samples/Cephalon.Sample.ModularVerticalSlice/Cephalon.Sample.ModularVerticalSlice.csproj",
            "samples/Cephalon.Sample.Microservice/Cephalon.Sample.Microservice.csproj",
            "samples/Cephalon.Sample.MicroserviceSuite/services/CatalogService/Cephalon.Sample.MicroserviceSuite.CatalogService.csproj",
            "samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj"
        )

        $report = [ordered]@{
            DeploymentMode = "singleFile"
            ManifestPath = $ManifestPath
            Configuration = "Release"
            ValidationStrategy = "publish-required"
            AggregateVerdict = "not-claimed"
            PublishProbePolicy = [ordered]@{
                Source = "manifest"
                ReleaseValidationMode = "single-file-publish-gate"
                ReleaseValidationDeploymentModes = @("singleFile")
                ReleaseValidationSkipsPublish = $false
                CurrentRunSkipsPublish = $false
                NonOptOutGate = $true
                GatedModes = @("singleFile")
                AuditOnlyModes = @("trim", "nativeAot")
                FailureBlocksRelease = $true
                FailOnWarnings = $true
                GatePromotion = "eng-510-single-file-publish-probe-release-gate"
                RepresentativePublishTargets = 5
            }
            PublishProbeGate = [ordered]@{
                Status = "passed"
                Enabled = $true
                FailureBlocksRelease = $true
                FailOnWarnings = $true
                GatedModes = @("singleFile")
                FailureCount = 0
                Reasons = @("all gated publish probes passed")
            }
            Modes = @(
                [ordered]@{
                    Mode = "singleFile"
                    Verdict = "not-claimed"
                    PublishProbe = [ordered]@{
                        Mode = "singleFile"
                        Skipped = $false
                        Targets = @($publishTargets | ForEach-Object {
                            [ordered]@{
                                Target = $_
                                ExitCode = 0
                                WarningCount = 0
                                ErrorCount = 0
                                Warnings = @()
                                Errors = @()
                                Success = $true
                            }
                        })
                    }
                    PackageClaimAudits = @(
                        [ordered]@{ Mode = "singleFile"; PackageName = "Cephalon.Diagnostics"; Verdict = "claim-truthful" },
                        [ordered]@{ Mode = "singleFile"; PackageName = "Cephalon.Abstractions"; Verdict = "claim-truthful" },
                        [ordered]@{ Mode = "singleFile"; PackageName = "Cephalon.Scaffolding"; Verdict = "claim-truthful" }
                    )
                }
            )
            HazardInventory = [ordered]@{
                TotalPackages = 9
                PackagesWithScopedClaims = 3
                TotalKnownHazards = 15
                KnownTransitiveHazardAudit = [ordered]@{
                    Status = "matched"
                    MissingEntries = 0
                    LockFileCount = 116
                }
                BoundaryAnnotationAuditStatus = "matched"
                BoundaryAnnotationAuditFailureCount = 0
                CoreRouteDelegateAuditStatus = "matched"
                CoreRouteDelegateAuditFailureCount = 0
                FullCommonRouteDelegateAuditStatus = "matched"
                FullCommonRouteDelegateAuditFailureCount = 0
                FullOperatorRouteDelegateAuditStatus = "matched"
                FullOperatorRouteDelegateAuditFailureCount = 0
                OperatorResponseJsonContractAuditStatus = "matched"
                OperatorResponseJsonContractAuditFailureCount = 0
                NonOperatorEndpointAuditStatus = "matched"
                NonOperatorEndpointAuditFailureCount = 0
                FrameworkEndpointBoundaryAuditStatus = "matched"
                FrameworkEndpointBoundaryAuditFailureCount = 0
            }
        }

        $parent = Split-Path -Parent $Path
        if (-not [string]::IsNullOrWhiteSpace($parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }

        $report | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $Path -Encoding UTF8
    }

    It "emits JSON and Markdown artifacts from the current scorecard" {
        $outputPath = Join-Path $script:tempRoot "artifacts"
        $claimsReportPath = Join-Path $script:tempRoot "claim-validation-report.json"
        $deploymentManifestPath = (Resolve-Path (Join-Path $script:repoRoot "scripts\deployment-mode-support.json")).Path
        $publishTargets = @(
            "samples/Cephalon.Sample.ModularMonolith/Cephalon.Sample.ModularMonolith.csproj",
            "samples/Cephalon.Sample.ModularVerticalSlice/Cephalon.Sample.ModularVerticalSlice.csproj",
            "samples/Cephalon.Sample.Microservice/Cephalon.Sample.Microservice.csproj",
            "samples/Cephalon.Sample.MicroserviceSuite/services/CatalogService/Cephalon.Sample.MicroserviceSuite.CatalogService.csproj",
            "samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj"
        )
        $claimsReport = [ordered]@{
            DeploymentMode = "singleFile"
            ManifestPath = $deploymentManifestPath
            Configuration = "Release"
            ValidationStrategy = "publish-required"
            AggregateVerdict = "not-claimed"
            PublishProbePolicy = [ordered]@{
                Source = "manifest"
                ReleaseValidationMode = "single-file-publish-gate"
                ReleaseValidationDeploymentModes = @("singleFile")
                ReleaseValidationSkipsPublish = $false
                CurrentRunSkipsPublish = $false
                NonOptOutGate = $true
                GatedModes = @("singleFile")
                AuditOnlyModes = @("trim", "nativeAot")
                FailureBlocksRelease = $true
                FailOnWarnings = $true
                GatePromotion = "eng-510-single-file-publish-probe-release-gate"
                RepresentativePublishTargets = 5
            }
            PublishProbeGate = [ordered]@{
                Status = "passed"
                Enabled = $true
                FailureBlocksRelease = $true
                FailOnWarnings = $true
                GatedModes = @("singleFile")
                FailureCount = 0
                Reasons = @("all gated publish probes passed")
            }
            Modes = @(
                [ordered]@{
                    Mode = "singleFile"
                    Verdict = "not-claimed"
                    PublishProbe = [ordered]@{
                        Mode = "singleFile"
                        Skipped = $false
                        Targets = @($publishTargets | ForEach-Object {
                            [ordered]@{
                                Target = $_
                                ExitCode = 0
                                WarningCount = 0
                                ErrorCount = 0
                                Warnings = @()
                                Errors = @()
                                Success = $true
                            }
                        })
                    }
                    PackageClaimAudits = @(
                        [ordered]@{
                            Mode = "singleFile"
                            PackageName = "Cephalon.Diagnostics"
                            Verdict = "claim-truthful"
                        },
                        [ordered]@{
                            Mode = "singleFile"
                            PackageName = "Cephalon.Abstractions"
                            Verdict = "claim-truthful"
                        },
                        [ordered]@{
                            Mode = "singleFile"
                            PackageName = "Cephalon.Scaffolding"
                            Verdict = "claim-truthful"
                        }
                    )
                }
            )
            HazardInventory = [ordered]@{
                TotalPackages = 9
                PackagesWithScopedClaims = 3
                TotalKnownHazards = 15
                KnownTransitiveHazardAudit = [ordered]@{
                    Status = "matched"
                    MissingEntries = 0
                    LockFileCount = 116
                }
                BoundaryAnnotationAuditStatus = "matched"
                BoundaryAnnotationAuditFailureCount = 0
                CoreRouteDelegateAuditStatus = "matched"
                CoreRouteDelegateAuditFailureCount = 0
                FullCommonRouteDelegateAuditStatus = "matched"
                FullCommonRouteDelegateAuditFailureCount = 0
                FullOperatorRouteDelegateAuditStatus = "matched"
                FullOperatorRouteDelegateAuditFailureCount = 0
                OperatorResponseJsonContractAuditStatus = "matched"
                OperatorResponseJsonContractAuditFailureCount = 0
                NonOperatorEndpointAuditStatus = "matched"
                NonOperatorEndpointAuditFailureCount = 0
                FrameworkEndpointBoundaryAuditStatus = "matched"
                FrameworkEndpointBoundaryAuditFailureCount = 0
            }
        }
        $claimsReport | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $claimsReportPath -Encoding UTF8

        $result = Invoke-EngineCompletionScorecardPublish `
            -ScorecardPath "docs/engine-completion-scorecard.md" `
            -ConformanceMatrixPath "docs/conformance-matrix.md" `
            -DeploymentModeClaimsReportPath $claimsReportPath `
            -OutputPath $outputPath `
            -RepoRoot $script:repoRoot

        Test-Path -LiteralPath $result.Paths.JsonPath -PathType Leaf | Should -BeTrue
        Test-Path -LiteralPath $result.Paths.MarkdownPath -PathType Leaf | Should -BeTrue

        $json = Get-Content -LiteralPath $result.Paths.JsonPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16

        $json.'$schemaVersion' | Should -Be "1.25.0"
        $json.SourceDocument | Should -Be "docs/engine-completion-scorecard.md"
        $json.ConformanceMatrix | Should -Be "docs/conformance-matrix.md"
        $json.DeploymentModeManifest | Should -Be "scripts/deployment-mode-support.json"
        $json.DeploymentModeClaimsReport | Should -Be (Get-RepoRelativePath -Path $claimsReportPath -RepoRoot $script:repoRoot)
        $json.AdoptionSmokeManifest | Should -Be "scripts/adoption-smoke-support.json"
        $json.ProviderIntegrationManifest | Should -Be "scripts/provider-integration-support.json"
        $json.EventingOperationalSuperiorityManifest | Should -Be "scripts/eventing-operational-superiority-support.json"
        $json.SrePostureManifest | Should -Be "scripts/sre-posture-support.json"
        $json.SupplyChainManifest | Should -Be "scripts/supply-chain-release-support.json"
        $json.TestCoverageRoadmap | Should -Be "docs/test-coverage-roadmap.md"
        $json.PublicApiDeltaScript | Should -Be "scripts/summarise-public-api-deltas.ps1"
        $json.StatusVocabulary.Count | Should -Be 6
        $json.EvidenceSources.Count | Should -Be 14
        $json.EvidenceSourceReferences.Count | Should -Be 37
        $json.PlatformGates.Count | Should -Be 12
        $json.QualityDimensions.Count | Should -Be 12
        $json.PackageFamilies.Count | Should -Be 9
        $json.PackageGAReadiness.Count | Should -Be 90
        $json.Summary.PlatformGateCount | Should -Be 12
        $json.Summary.QualityDimensionCount | Should -Be 12
        $json.Summary.PackageFamilyCount | Should -Be 9
        $json.Summary.PackageGAReadinessCount | Should -Be 90
        $json.Summary.DeploymentModeGlobalClaimCount | Should -Be 3
        $json.Summary.DeploymentModeGlobalNotClaimedCount | Should -Be 3
        $json.Summary.DeploymentModePackageEntryCount | Should -Be 9
        $json.Summary.DeploymentModePackageScopedClaimPackageCount | Should -Be 3
        $json.Summary.DeploymentModePackageScopedClaimCount | Should -Be 3
        $json.Summary.DeploymentModeKnownHazardPackageCount | Should -Be 3
        $json.Summary.DeploymentModeKnownHazardEntryCount | Should -Be 15
        $json.Summary.DeploymentModeTransitiveAuditEntryCount | Should -Be 7
        $json.Summary.DeploymentModeRepresentativePublishTargetCount | Should -Be 5
        $json.Summary.DeploymentModeClaimsReportPresent | Should -BeTrue
        $json.Summary.DeploymentModeClaimsReportPublishProbeTargetCount | Should -Be 5
        $json.Summary.DeploymentModeClaimsReportPublishProbeWarningCount | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportPublishProbeErrorCount | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportPackageClaimTruthfulCount | Should -Be 3
        $json.Summary.DeploymentModeClaimsReportPackageClaimOverstatedCount | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportBoundaryAnnotationAuditFailures | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportCoreRouteDelegateAuditFailures | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportNonOperatorEndpointAuditFailures | Should -Be 0
        $json.Summary.DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures | Should -Be 0
        $json.Summary.AdoptionSmokeScenarioCount | Should -Be 1
        $json.Summary.AdoptionSmokeRuntimeProbeCount | Should -Be 6
        $json.Summary.AdoptionSmokeAssertionCount | Should -Be 7
        $json.Summary.AdoptionSmokeExecutionReportRequiredFieldCount | Should -Be 9
        $json.Summary.AdoptionSmokeGoldenUseCaseCount | Should -Be 7
        $json.Summary.AdoptionSmokeGoldenUseCaseExecutionReadyCount | Should -Be 5
        $json.Summary.AdoptionSmokeGoldenUseCaseExecutionReportCount | Should -Be 5
        $json.Summary.ProviderIntegrationEvidenceRowCount | Should -Be 33
        $json.Summary.ProviderIntegrationLiveProofCount | Should -Be 33
        $json.Summary.ProviderIntegrationCompositionOnlyCount | Should -Be 0
        $json.Summary.ProviderIntegrationExternalServiceGateCount | Should -Be 14
        $json.Summary.ProviderIntegrationDefaultSkippedCount | Should -Be 14
        $json.Summary.ProviderIntegrationRuntimeContractCount | Should -Be 99
        $json.Summary.EventingOperationalSuperiorityRequiredDimensionCount | Should -Be 6
        $json.Summary.EventingOperationalSuperiorityCoveredDimensionCount | Should -Be 6
        $json.Summary.EventingOperationalSuperiorityPartialDimensionCount | Should -Be 0
        $json.Summary.EventingOperationalSuperiorityMissingDimensionCount | Should -Be 0
        $json.Summary.EventingOperationalSuperiorityCoveragePercent | Should -Be 100
        $json.Summary.EventingOperationalSuperiorityPromotionAllowed | Should -BeTrue
        $json.Summary.EventingOperationalSuperiorityWolverineRequired | Should -BeFalse
        $json.Summary.EventingOperationalSuperiorityRuntimeConcordanceMatched | Should -BeTrue
        $json.Summary.EventingOperationalSuperiorityRuntimeConcordanceTokenCount | Should -Be 19
        $json.Summary.EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount | Should -Be 0
        $json.Summary.SreSliCount | Should -Be 11
        $json.Summary.SreTargetDeclaredCount | Should -Be 11
        $json.Summary.SrePendingStableBaselineCount | Should -Be 1
        $json.Summary.SreStableBaselineCount | Should -Be 10
        $json.Summary.SreGuardrailMappedSliCount | Should -Be 6
        $json.Summary.SreGuardrailPendingSliCount | Should -Be 0
        $json.Summary.SreGuardrailNotApplicableSliCount | Should -Be 5
        $json.Summary.SreGuardrailReferenceCount | Should -Be 8
        $json.Summary.SrePendingBaselineRowCount | Should -Be 1
        $json.Summary.SrePendingBaselineBlockerCount | Should -Be 1
        $json.Summary.SrePendingBaselineEvidenceCount | Should -Be 1
        $json.Summary.SupplyChainEvidenceItemCount | Should -Be 12
        $json.Summary.SupplyChainWorkflowReadyCount | Should -Be 9
        $json.Summary.SupplyChainExternalPolicyPendingCount | Should -Be 3
        $json.Summary.SupplyChainExternalPolicyPreflightCheckCount | Should -Be 3
        $json.Summary.SupplyChainSignedReleaseDryRunStatus | Should -Be "blocked"
        $json.Summary.SupplyChainSignedReleaseDryRunBlockerClass | Should -Be "dispatch-identity-actions-disabled"
        $json.Summary.SupplyChainBlockedCount | Should -Be 0
        $json.Summary.TestCoverageLayeredProjectCount | Should -Be 8
        $json.Summary.TestCoverageGapCriterionCount | Should -Be 4
        $json.Summary.TestCoverageRecommendationCount | Should -Be 11
        $json.Summary.TestCoverageShippedRecommendationCount | Should -Be 10
        $json.Summary.TestCoverageGatedRecommendationCount | Should -Be 1
        $json.Summary.TestCoverageActiveGapRecommendationCount | Should -Be 0
        $json.Summary.TestCoverageQuarantineEntryCount | Should -Be 2
        $json.Summary.TestCoverageOpenQuarantineEntryCount | Should -Be 0
        $json.Summary.PublicApiPackageCount | Should -Be 104
        $json.Summary.PublicApiPendingPackageCount | Should -Be 0
        $json.Summary.PublicApiAdditiveEntryCount | Should -Be 0
        $json.Summary.PublicApiRemovalEntryCount | Should -Be 0
        $json.Summary.EvidenceSourceCount | Should -Be 14
        $json.Summary.EvidenceSourceReferenceCount | Should -Be 37
        $json.Summary.PlatformStatusCounts.'ready-for-preview' | Should -Be 3
        $json.Summary.PlatformStatusCounts.partial | Should -Be 8
        $json.Summary.PlatformStatusCounts.'not-claimed' | Should -Be 1
        $json.Summary.PlatformStatusCounts.'needs-refresh' | Should -Be 0
        $json.Summary.PackageStatusCounts.'ready-for-preview' | Should -Be 4
        $json.Summary.PackageStatusCounts.partial | Should -Be 5
        $json.Summary.PackageGAStatusCounts.partial | Should -Be 89
        $json.Summary.PackageGAStatusCounts.'not-claimed' | Should -Be 1
        $json.Summary.PackageGAStatusCounts.'needs-refresh' | Should -Be 0

        $json.EvidenceSourceReferences.Reference | Should -Contain "docs/engine-surface-maturity-audit.md"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/deployment-mode-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/adoption-smoke-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/provider-integration-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/eventing-operational-superiority-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "docs/components/eventing.md"
        $json.EvidenceSourceReferences.Reference | Should -Contain "docs/project-memory.md"
        $json.EvidenceSourceReferences.Reference | Should -Contain "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/observability-dependency-health-providers.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/sre-posture-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/sre-stable-baselines.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/supply-chain-release-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain ".github/workflows/publish-release.yml"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/validate-out-of-tree-package-adoption.ps1"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/summarise-public-api-deltas.ps1"

        $json.DeploymentModeEvidence.ManifestSchemaVersion | Should -Be "1.5.0"
        $json.DeploymentModeEvidence.ShippingStableTargetFramework | Should -Be "net10.0"
        $json.DeploymentModeEvidence.ReadinessLaneTargetFramework | Should -Be "net11.0"
        $json.DeploymentModeEvidence.ReadinessLaneStatus | Should -Be "assessment-only"
        $json.DeploymentModeEvidence.SourceDocuments | Should -Contain "docs/deployment-mode-support.md"
        $json.DeploymentModeEvidence.ValidationScripts | Should -Contain "scripts/validate-deployment-mode-claims.ps1"
        $json.DeploymentModeEvidence.ValidationScripts | Should -Contain "tests/Cephalon.Tests.Scripts/validate-deployment-mode-claims.Tests.ps1"
        $json.DeploymentModeEvidence.GlobalClaimCount | Should -Be 3
        $json.DeploymentModeEvidence.GlobalNotClaimedCount | Should -Be 3
        $json.DeploymentModeEvidence.GlobalClaimStatuses.Mode | Should -Contain "trim"
        $json.DeploymentModeEvidence.GlobalClaimStatuses.Mode | Should -Contain "nativeAot"
        $json.DeploymentModeEvidence.GlobalClaimStatuses.Mode | Should -Contain "singleFile"
        $json.DeploymentModeEvidence.PackageEntryCount | Should -Be 9
        $json.DeploymentModeEvidence.PackageScopedClaimPackageCount | Should -Be 3
        $json.DeploymentModeEvidence.PackageScopedClaimCount | Should -Be 3
        $json.DeploymentModeEvidence.KnownHazardPackageCount | Should -Be 3
        $json.DeploymentModeEvidence.KnownHazardEntryCount | Should -Be 15
        $json.DeploymentModeEvidence.ClaimAuditTierCounts.high | Should -Be 3
        $json.DeploymentModeEvidence.ClaimAuditTierCounts.'excluded-by-design' | Should -Be 3
        $json.DeploymentModeEvidence.ClaimAuditTierCounts.'clean-baseline' | Should -Be 3
        $json.DeploymentModeEvidence.TransitiveAuditEntryCount | Should -Be 7
        $json.DeploymentModeEvidence.TransitiveAuditLockFileGlobCount | Should -Be 3
        $json.DeploymentModeEvidence.RepresentativePublishTargetCount | Should -Be 5
        $json.DeploymentModeEvidence.PublishProbeReleaseValidationMode | Should -Be "single-file-publish-gate"
        $json.DeploymentModeEvidence.PublishProbeReleaseValidationDeploymentModes | Should -Contain "singleFile"
        $json.DeploymentModeEvidence.PublishProbeReleaseValidationSkipsPublish | Should -BeFalse
        $json.DeploymentModeEvidence.PublishProbeNonOptOutGate | Should -BeTrue
        $json.DeploymentModeEvidence.PublishProbeGatedModes | Should -Contain "singleFile"
        $json.DeploymentModeEvidence.PublishProbeAuditOnlyModes | Should -Contain "trim"
        $json.DeploymentModeEvidence.PublishProbeAuditOnlyModes | Should -Contain "nativeAot"
        $json.DeploymentModeEvidence.PublishProbeFailureBlocksRelease | Should -BeTrue
        $json.DeploymentModeEvidence.PublishProbeFailOnWarnings | Should -BeTrue
        $json.DeploymentModeEvidence.ClaimsReportPresent | Should -BeTrue
        $json.DeploymentModeEvidence.ClaimsReportAggregateVerdict | Should -Be "not-claimed"
        $json.DeploymentModeEvidence.ClaimsReportDeploymentMode | Should -Be "singleFile"
        $json.DeploymentModeEvidence.ClaimsReportPublishProbeGateStatus | Should -Be "passed"
        $json.DeploymentModeEvidence.ClaimsReportPublishProbeTargetCount | Should -Be 5
        $json.DeploymentModeEvidence.ClaimsReportPublishProbeWarningCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportPublishProbeErrorCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportPackageClaimTruthfulCount | Should -Be 3
        $json.DeploymentModeEvidence.ClaimsReportPackageClaimOverstatedCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryTransitiveAuditStatus | Should -Be "matched"
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus | Should -Be "matched"
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus | Should -Be "matched"
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus | Should -Be "matched"
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus | Should -Be "matched"
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus | Should -Be "matched"
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus | Should -Be "matched"
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount | Should -Be 0
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus | Should -Be "matched"
        $json.DeploymentModeEvidence.ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount | Should -Be 0
        $json.DeploymentModeEvidence.PackageRows.PackageName | Should -Contain "Cephalon.Diagnostics"
        $json.DeploymentModeEvidence.PackageRows.PackageName | Should -Contain "Cephalon.Abstractions"
        $json.DeploymentModeEvidence.PackageRows.PackageName | Should -Contain "Cephalon.Scaffolding"
        $json.DeploymentModeEvidence.PackageRows.PackageName | Should -Contain "Cephalon.Data.MySql.SciSharpReplication"
        $json.DeploymentModeEvidence.TransitiveAuditRows.PackagePattern | Should -Contain "Newtonsoft.Json"

        $json.AdoptionSmokeEvidence.ManifestSchemaVersion | Should -Be "1.6.0"
        $json.AdoptionSmokeEvidence.ScenarioId | Should -Be "out-of-tree-generated-app-package-stage"
        $json.AdoptionSmokeEvidence.Status | Should -Be "execution-report-ready"
        $json.AdoptionSmokeEvidence.ValidationScript | Should -Be "scripts/validate-out-of-tree-package-adoption.ps1"
        $json.AdoptionSmokeEvidence.ReferenceModuleProject | Should -Be "samples/Cephalon.ReferenceModule.Operations/Cephalon.ReferenceModule.Operations.csproj"
        $json.AdoptionSmokeEvidence.SupportingScripts | Should -Contain "scripts/validate-generated-app-adoption.ps1"
        $json.AdoptionSmokeEvidence.SupportingScripts | Should -Contain "scripts/validate-modular-monolith-adoption.ps1"
        $json.AdoptionSmokeEvidence.SupportingScripts | Should -Contain "scripts/validate-vertical-slice-eventing-adoption.ps1"
        $json.AdoptionSmokeEvidence.SourceDocuments | Should -Contain "docs/package-publishing.md"
        $json.AdoptionSmokeEvidence.RuntimeProbes.Path | Should -Contain "/engine/packages"
        $json.AdoptionSmokeEvidence.RuntimeProbes.Path | Should -Contain "/engine/trust-policy"
        $json.AdoptionSmokeEvidence.RuntimeProbes.Path | Should -Contain "/api/operations/status"
        $json.AdoptionSmokeEvidence.ExecutionReport.SchemaVersion | Should -Be "1.0.0"
        $json.AdoptionSmokeEvidence.ExecutionReport.DefaultPath | Should -Be "artifacts/adoption-smoke/out-of-tree-package-adoption.json"
        $json.AdoptionSmokeEvidence.ExecutionReport.StatusValues | Should -Contain "passed"
        $json.AdoptionSmokeEvidence.ExecutionReport.StatusValues | Should -Contain "failed"
        $json.AdoptionSmokeEvidence.ExecutionReport.RequiredFields.Count | Should -Be 9
        $json.AdoptionSmokeEvidence.ExecutionReport.RequiredFields | Should -Contain '$schemaVersion'
        $json.AdoptionSmokeEvidence.ExecutionReport.RequiredFields | Should -Contain "RuntimeProbes"
        $json.AdoptionSmokeEvidence.GoldenUseCases.Count | Should -Be 7
        $json.AdoptionSmokeEvidence.GoldenUseCases.Id | Should -Contain "out-of-tree-package-adoption"
        $json.AdoptionSmokeEvidence.GoldenUseCases.Id | Should -Contain "generated-app-runtime-foundation"
        $json.AdoptionSmokeEvidence.GoldenUseCases.Id | Should -Contain "template-pack-dotnet-new-parity"
        $json.AdoptionSmokeEvidence.GoldenUseCases.Id | Should -Contain "modular-monolith-rest-worker-data"
        $json.AdoptionSmokeEvidence.GoldenUseCases.Id | Should -Contain "vertical-slice-eventing-outbox"
        $json.AdoptionSmokeEvidence.GoldenUseCases.Id | Should -Contain "microservice-multi-transport-operations"
        $json.AdoptionSmokeEvidence.GoldenUseCases.Id | Should -Contain "saas-tenant-governance-audit"
        $json.AdoptionSmokeEvidence.GoldenUseCases.Status | Should -Contain "execution-report-ready"
        $json.AdoptionSmokeEvidence.GoldenUseCases.PrimaryValidationPath | Should -Contain "scripts/validate-out-of-tree-package-adoption.ps1"
        $json.AdoptionSmokeEvidence.GoldenUseCases.PrimaryValidationPath | Should -Contain "scripts/validate-generated-app-adoption.ps1"
        $json.AdoptionSmokeEvidence.GoldenUseCases.PrimaryValidationPath | Should -Contain "scripts/validate-template-pack-adoption.ps1"
        $json.AdoptionSmokeEvidence.GoldenUseCases.PrimaryValidationPath | Should -Contain "scripts/validate-modular-monolith-adoption.ps1"
        $json.AdoptionSmokeEvidence.GoldenUseCases.PrimaryValidationPath | Should -Contain "scripts/validate-vertical-slice-eventing-adoption.ps1"
        $goldenUseCaseExecutionReportPaths = @(
            $json.AdoptionSmokeEvidence.GoldenUseCases |
                Where-Object { $null -ne $_.ExecutionReport } |
                ForEach-Object { $_.ExecutionReport.DefaultPath }
        )
        $goldenUseCaseExecutionReportPaths | Should -Contain "artifacts/adoption-smoke/out-of-tree-package-adoption.json"
        $goldenUseCaseExecutionReportPaths | Should -Contain "artifacts/adoption-smoke/generated-app-adoption.json"
        $goldenUseCaseExecutionReportPaths | Should -Contain "artifacts/adoption-smoke/template-pack-adoption.json"
        $goldenUseCaseExecutionReportPaths | Should -Contain "artifacts/adoption-smoke/modular-monolith-adoption.json"
        $goldenUseCaseExecutionReportPaths | Should -Contain "artifacts/adoption-smoke/vertical-slice-eventing-adoption.json"

        $json.TestCoverageEvidence.Roadmap | Should -Be "docs/test-coverage-roadmap.md"
        $json.TestCoverageEvidence.LayeredProjectCount | Should -Be 8
        $json.TestCoverageEvidence.GapDefinitionCriterionCount | Should -Be 4
        $json.TestCoverageEvidence.RecommendationCount | Should -Be 11
        $json.TestCoverageEvidence.ShippedRecommendationCount | Should -Be 10
        $json.TestCoverageEvidence.GatedRecommendationCount | Should -Be 1
        $json.TestCoverageEvidence.ActiveGapRecommendationCount | Should -Be 0
        $json.TestCoverageEvidence.QuarantineEntryCount | Should -Be 2
        $json.TestCoverageEvidence.OpenQuarantineEntryCount | Should -Be 0
        $json.TestCoverageEvidence.QuarantineQueueStatus | Should -Be "empty"
        $json.TestCoverageEvidence.LayeredProjects.Reference | Should -Contain "tests/Cephalon.Tests.ProviderIntegration"
        $json.TestCoverageEvidence.LayeredProjects.Reference | Should -Contain "benchmarks/Cephalon.Benchmarks"
        $json.TestCoverageEvidence.GapCriteria.Id | Should -Contain "a"
        $json.TestCoverageEvidence.Recommendations.Number | Should -Contain 6
        $json.TestCoverageEvidence.Recommendations.Status | Should -Contain "gated"
        $json.TestCoverageEvidence.QuarantineRows.Status | Should -Not -Contain "open"
        $json.TestCoverageEvidence.ValidatedReferences.Reference | Should -Contain "docs/test-coverage-roadmap.md"

        $json.ProviderIntegrationEvidence.ManifestSchemaVersion | Should -Be "1.0.0"
        $json.ProviderIntegrationEvidence.Status | Should -Be "expanded-live-provider-evidence"
        $json.ProviderIntegrationEvidence.SourceDocuments | Should -Contain "docs/components/observability.md"
        $json.ProviderIntegrationEvidence.SourceDocuments | Should -Contain "scripts/observability-dependency-health-providers.json"
        $json.ProviderIntegrationEvidence.ValidationProjects | Should -Contain "tests/Cephalon.Tests.Hosting/Cephalon.Tests.Hosting.csproj"
        $json.ProviderIntegrationEvidence.DependencyHealthProviderManifest.Reference | Should -Be "scripts/observability-dependency-health-providers.json"
        $json.ProviderIntegrationEvidence.DependencyHealthProviderManifest.ManifestSchemaVersion | Should -Be "1.0.0"
        $json.ProviderIntegrationEvidence.DependencyHealthProviderManifest.Status | Should -Be "source-derived-provider-family-contract"
        $json.ProviderIntegrationEvidence.DependencyHealthProviderManifest.ProviderCount | Should -Be 18
        $json.ProviderIntegrationEvidence.EvidenceRowCount | Should -Be 33
        $json.ProviderIntegrationEvidence.LiveProofCount | Should -Be 33
        $json.ProviderIntegrationEvidence.CompositionOnlyCount | Should -Be 0
        $json.ProviderIntegrationEvidence.ExternalServiceGateCount | Should -Be 14
        $json.ProviderIntegrationEvidence.DefaultSkippedCount | Should -Be 14
        $json.ProviderIntegrationEvidence.RuntimeContractCount | Should -Be 99
        $json.ProviderIntegrationEvidence.EnvironmentVariableCount | Should -Be 35
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "redis-data-event-sourcing-live"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "mongodb-data-runtime-surface"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "cassandra-data-runtime-surface"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "clickhouse-data-runtime-surface"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "elasticsearch-data-runtime-surface"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "nats-data-runtime-surface"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "neo4j-data-runtime-surface"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "opensearch-data-runtime-surface"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "qdrant-data-runtime-surface"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "smtp-invitation-delivery-live"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "postgres-cdc-live"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "sqlserver-dependency-health-live"
        $json.ProviderIntegrationEvidence.ProviderRows.ExternalServiceGate | Should -Contain "provider-integration"
        $json.ProviderIntegrationEvidence.ProviderRows.ExternalServiceGate | Should -Contain "cdc-integration"
        $json.ProviderIntegrationEvidence.ProviderRows.RuntimeContracts | Should -Contain "dependency-health.sqlserver"
        $json.ProviderIntegrationEvidence.ProviderRows.RuntimeContracts | Should -Contain "IOutbox.EnqueueAsync"
        $json.ProviderIntegrationEvidence.ProviderRows.RuntimeContracts | Should -Contain "IInbox.HasProcessedAsync"
        $json.ProviderIntegrationEvidence.ProviderRows.RuntimeContracts | Should -Contain "dispatch-policy.unsupported"
        $json.ProviderIntegrationEvidence.ProviderRows.RuntimeContracts | Should -Contain "smtp-relay-handoff"
        $json.ProviderIntegrationEvidence.ProviderRows.TestFiles | Should -Contain "tests/Cephalon.Tests.Hosting/ObservabilityDependencyHealthProviderInvariantTests.cs"
        $json.ProviderIntegrationEvidence.ProviderRows.TestFiles | Should -Contain "tests/Cephalon.Tests.ProviderIntegration/LiveDataProviderIntegrationTests.cs"
        $json.ProviderIntegrationEvidence.ProviderRows.TestFiles | Should -Contain "tests/Cephalon.Tests.ProviderIntegration/SmtpDeliveryProviderIntegrationTests.cs"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_REDIS_CONNECTION_STRING"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_CASSANDRA_CONTACT_POINTS"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_CLICKHOUSE_HOST"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_ELASTICSEARCH_URI"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_NATS_URI"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_NEO4J_URI"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_OPENSEARCH_URI"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_QDRANT_HOST"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_SMTP_HOST"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_SMTP_API_URI"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_CDC_POSTGRES_CONNECTION_STRING"

        $json.EventingOperationalSuperiorityEvidence.ManifestSchemaVersion | Should -Be "1.1.0"
        $json.EventingOperationalSuperiorityEvidence.Status | Should -Be "claimed"
        $json.EventingOperationalSuperiorityEvidence.Technology | Should -Be "event-driven-integration"
        $json.EventingOperationalSuperiorityEvidence.SurfaceId | Should -Be "eventing-superiority-profile"
        $json.EventingOperationalSuperiorityEvidence.ProfileEntryId | Should -Be "operational-superiority-coverage"
        $json.EventingOperationalSuperiorityEvidence.SourceDocuments | Should -Contain "docs/components/eventing.md"
        $json.EventingOperationalSuperiorityEvidence.ValidationProjects | Should -Contain "tests/Cephalon.Tests.Composition/Cephalon.Tests.Composition.csproj"
        $json.EventingOperationalSuperiorityEvidence.ValidationProjects | Should -Contain "benchmarks/Cephalon.Benchmarks/Cephalon.Benchmarks.csproj"
        $json.EventingOperationalSuperiorityEvidence.ValidationFiles | Should -Contain "tests/Cephalon.Tests.Composition/Composition/EntityFrameworkDataPackTests.cs"
        $json.EventingOperationalSuperiorityEvidence.ComparisonBaseline | Should -Contain "MassTransit"
        $json.EventingOperationalSuperiorityEvidence.ComparisonBaseline | Should -Contain "NServiceBus"
        $json.EventingOperationalSuperiorityEvidence.ComparisonBaseline | Should -Contain "Wolverine"
        $json.EventingOperationalSuperiorityEvidence.ComparisonBaseline | Should -Contain "MediatR"
        $json.EventingOperationalSuperiorityEvidence.ProviderNeutral | Should -BeTrue
        $json.EventingOperationalSuperiorityEvidence.WolverineRequired | Should -BeFalse
        $json.EventingOperationalSuperiorityEvidence.HotPathBindingMode | Should -Be "code-first-publish-subscribe"
        $json.EventingOperationalSuperiorityEvidence.ConfigurationRole | Should -Be "environment-policy-and-provider-selection"
        $json.EventingOperationalSuperiorityEvidence.RuntimeConcordanceStatus | Should -Be "matched"
        $json.EventingOperationalSuperiorityEvidence.RuntimeConcordanceSource | Should -Be "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs"
        $json.EventingOperationalSuperiorityEvidence.RuntimeConcordanceTokenCount | Should -Be 19
        $json.EventingOperationalSuperiorityEvidence.RuntimeConcordanceMatchedTokenCount | Should -Be 19
        $json.EventingOperationalSuperiorityEvidence.RuntimeConcordanceMissingTokenCount | Should -Be 0
        $json.EventingOperationalSuperiorityEvidence.RuntimeConcordanceRequiredTokens | Should -Contain "providerNeutral=true; wolverineRequired=false"
        $json.EventingOperationalSuperiorityEvidence.RuntimeConcordanceRequiredTokens | Should -Contain "comparisonBaseline=MassTransit,NServiceBus,Wolverine,MediatR"
        $json.EventingOperationalSuperiorityEvidence.PromotionGate | Should -Be "allowed"
        $json.EventingOperationalSuperiorityEvidence.PromotionAllowed | Should -BeTrue
        $json.EventingOperationalSuperiorityEvidence.PromotionPolicy | Should -Be "all-operational-superiority-dimensions-claimed"
        $json.EventingOperationalSuperiorityEvidence.PromotionEvidenceContract | Should -Be "cephalon-eventing-operational-superiority-promotion-v1"
        $json.EventingOperationalSuperiorityEvidence.PromotionEvidenceContractVersion | Should -Be "1.0.0"
        $json.EventingOperationalSuperiorityEvidence.PromotionTarget | Should -Be "eventing-operational-superiority"
        $json.EventingOperationalSuperiorityEvidence.PromotionRequiredStatus | Should -Be "claimed"
        $json.EventingOperationalSuperiorityEvidence.PromotionDecisionCode | Should -Be "all-required-dimensions-claimed"
        $json.EventingOperationalSuperiorityEvidence.RequiredDimensionCount | Should -Be 6
        $json.EventingOperationalSuperiorityEvidence.CoveredDimensionCount | Should -Be 6
        $json.EventingOperationalSuperiorityEvidence.PartialDimensionCount | Should -Be 0
        $json.EventingOperationalSuperiorityEvidence.MissingDimensionCount | Should -Be 0
        $json.EventingOperationalSuperiorityEvidence.BlockingDimensionCount | Should -Be 0
        $json.EventingOperationalSuperiorityEvidence.CoveragePercent | Should -Be 100
        $json.EventingOperationalSuperiorityEvidence.OperationalSuperiorityComplete | Should -BeTrue
        $json.EventingOperationalSuperiorityEvidence.RequiredDimensions.Id | Should -Contain "provider-operated-runtime-proof-coverage"
        $json.EventingOperationalSuperiorityEvidence.RequiredDimensions.Id | Should -Contain "choreography-handoff-ownership"
        $json.EventingOperationalSuperiorityEvidence.RequiredDimensions.Id | Should -Contain "durable-remediation-command-audit"
        $json.EventingOperationalSuperiorityEvidence.RequiredDimensions.Id | Should -Contain "durable-command-journal-replay-cursor"
        $json.EventingOperationalSuperiorityEvidence.RequiredDimensions.Id | Should -Contain "observability-compliance-and-auditability"
        $json.EventingOperationalSuperiorityEvidence.RequiredDimensions.Id | Should -Contain "testability-and-benchmark-evidence"
        $json.EventingOperationalSuperiorityEvidence.ValidatedReferences.Reference | Should -Contain "scripts/eventing-operational-superiority-support.json"
        $json.EventingOperationalSuperiorityEvidence.ValidatedReferences.Reference | Should -Contain "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs"
        $json.EventingOperationalSuperiorityEvidence.ValidatedReferences.Reference | Should -Contain "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json"

        $json.SrePostureEvidence.ManifestSchemaVersion | Should -Be "1.10.0"
        $json.SrePostureEvidence.Status | Should -Be "partial-stable-baseline-published"
        $json.SrePostureEvidence.ReleaseValidationSummaryMode | Should -Be "release-validation-console-and-scorecard-artifact"
        $json.SrePostureEvidence.StableBaselinesPublished | Should -BeTrue
        $json.SrePostureEvidence.StableBaselineManifest | Should -Be "scripts/sre-stable-baselines.json"
        $json.SrePostureEvidence.StableBaselineManifestSchemaVersion | Should -Be "1.8.0"
        $json.SrePostureEvidence.StableBaselineManifestStatus | Should -Be "partial-stable-baseline-published"
        $json.SrePostureEvidence.GuardrailCatalog | Should -Be "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json"
        $json.SrePostureEvidence.GuardrailCatalogEntryCount | Should -Be 40
        $json.SrePostureEvidence.SliCount | Should -Be 11
        $json.SrePostureEvidence.TargetDeclaredCount | Should -Be 11
        $json.SrePostureEvidence.PendingStableBaselineCount | Should -Be 1
        $json.SrePostureEvidence.StableBaselineCount | Should -Be 10
        $json.SrePostureEvidence.GuardrailMappedSliCount | Should -Be 6
        $json.SrePostureEvidence.GuardrailPendingSliCount | Should -Be 0
        $json.SrePostureEvidence.GuardrailNotApplicableSliCount | Should -Be 5
        $json.SrePostureEvidence.GuardrailReferenceCount | Should -Be 8
        $json.SrePostureEvidence.StableBaselineRowCount | Should -Be 10
        $json.SrePostureEvidence.StableBaselineMeasurementCount | Should -Be 12
        $json.SrePostureEvidence.PendingBaselineRowCount | Should -Be 1
        $json.SrePostureEvidence.PendingBaselineBlockerCount | Should -Be 1
        $json.SrePostureEvidence.PendingBaselineEvidenceCount | Should -Be 1
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.behavior.dispatch.latency.p95"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.behavior.dispatch.latency.p99"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.behavior.dispatch.alloc.bytes-per-op"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.aspnetcore.request.alloc.bytes-per-op"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.deployment-mode-claims.truthful-fraction"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.dotnet.restore.wall-time.lock-mode"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.reference-docs.wall-time"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.validate-release.wall-time"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.worker.cold-start.p95"
        $json.SrePostureEvidence.StableBaselinePublishedSliIds | Should -Contain "engine.aspnetcore.minimal-api.cold-start.p95"
        $json.SrePostureEvidence.PendingBaselineSliIds | Should -Contain "engine.tests.flake-rate.7d"
        $json.SrePostureEvidence.PendingBaselineSliIds | Should -Not -Contain "engine.aspnetcore.minimal-api.cold-start.p95"
        $json.SrePostureEvidence.PendingBaselineSliIds | Should -Not -Contain "engine.worker.cold-start.p95"
        $json.SrePostureEvidence.PendingBaselineSliIds | Should -Not -Contain "engine.reference-docs.wall-time"
        $json.SrePostureEvidence.PendingBaselineSliIds | Should -Not -Contain "engine.validate-release.wall-time"
        $json.SrePostureEvidence.PendingBaselineRows.SliId | Should -Contain "engine.tests.flake-rate.7d"
        $flakeRatePendingBaseline = $json.SrePostureEvidence.PendingBaselineRows | Where-Object { $_.SliId -eq "engine.tests.flake-rate.7d" }
        $flakeRatePendingBaseline.BlockerClass | Should -Be "ci-metadata-unavailable"
        $flakeRatePendingBaseline.Evidence.Kind | Should -Be "ci-flake-rate-report"
        $flakeRatePendingBaseline.Evidence.EvidenceScript | Should -Be "scripts/measure-ci-flake-rate.ps1"
        $flakeRatePendingBaseline.Evidence.ReportPath | Should -Be "artifacts/sre-ci-flake-rate/ci-flake-rate.json"
        $flakeRatePendingBaseline.Evidence.ActionsReadinessStatus | Should -Be "actions-enabled"
        $flakeRatePendingBaseline.Evidence.WorkflowReadinessStatus | Should -Be "active-workflows"
        $flakeRatePendingBaseline.Evidence.WorkflowDispatchReadinessStatus | Should -Be "workflow-dispatch-configured"
        $flakeRatePendingBaseline.Evidence.ReadinessBlockerClass | Should -Be "no-completed-actions-history"
        $flakeRatePendingBaseline.Evidence.MatchingWorkflowCount | Should -Be 3
        $flakeRatePendingBaseline.Evidence.ActiveWorkflowCount | Should -Be 3
        $flakeRatePendingBaseline.Evidence.DispatchConfiguredWorkflowCount | Should -Be 3
        $flakeRatePendingBaseline.Evidence.PromotionAllowed | Should -BeFalse
        $json.SrePostureEvidence.SourceDocuments | Should -Contain "docs/sre-posture.md"
        $json.SrePostureEvidence.SourceDocuments | Should -Contain "docs/benchmarking.md"
        $json.SrePostureEvidence.ValidationScripts | Should -Contain "scripts/validate-release.ps1"
        $json.SrePostureEvidence.ValidationScripts | Should -Contain "scripts/measure-ci-flake-rate.ps1"
        $json.SrePostureEvidence.SliRows.Id | Should -Contain "engine.behavior.dispatch.latency.p95"
        $json.SrePostureEvidence.SliRows.BaselineStatus | Should -Contain "pending-stable-baseline"
        $json.SrePostureEvidence.SliRows.BaselineStatus | Should -Contain "stable-baseline-published"
        $json.SrePostureEvidence.SliRows.GuardrailCoverageStatus | Should -Contain "guardrail-catalog-mapped"
        $json.SrePostureEvidence.SliRows.GuardrailCoverageStatus | Should -Contain "not-applicable"
        $behaviorLatencySli = $json.SrePostureEvidence.SliRows | Where-Object { $_.Id -eq "engine.behavior.dispatch.latency.p95" }
        $behaviorLatencySli.GuardrailReferences.ReportFileName | Should -Contain "Cephalon.Benchmarks.HotPath.BehaviorDispatchBenchmarks-report.csv"
        $behaviorLatencySli.GuardrailReferences.Benchmark | Should -Contain "DispatchBehavior"
        $requestAllocationSli = $json.SrePostureEvidence.SliRows | Where-Object { $_.Id -eq "engine.aspnetcore.request.alloc.bytes-per-op" }
        $requestAllocationSli.GuardrailCoverageStatus | Should -Be "guardrail-catalog-mapped"
        $requestAllocationSli.GuardrailReferences.ReportFileName | Should -Contain "Cephalon.Benchmarks.Runtime.AspNetCoreRequestLoggingBenchmarks-report.csv"
        $requestAllocationSli.GuardrailReferences.Benchmark | Should -Contain "HandleLoggedJsonRequest"
        $requestAllocationSli.GuardrailReferences.Benchmark | Should -Contain "HandleTruncatedJsonRequest"
        $requestAllocationSli.GuardrailReferences.Benchmark | Should -Contain "HandleConcurrentLoggedJsonRequest"
        $aspNetCoreColdStartSli = $json.SrePostureEvidence.SliRows | Where-Object { $_.Id -eq "engine.aspnetcore.minimal-api.cold-start.p95" }
        $aspNetCoreColdStartSli.BaselineStatus | Should -Be "stable-baseline-published"
        $aspNetCoreColdStartSli.GuardrailCoverageStatus | Should -Be "guardrail-catalog-mapped"
        $aspNetCoreColdStartSli.GuardrailReferences.Benchmark | Should -Contain "BuildStartHandleFirstRequestAspNetCore"
        $workerColdStartSli = $json.SrePostureEvidence.SliRows | Where-Object { $_.Id -eq "engine.worker.cold-start.p95" }
        $workerColdStartSli.BaselineStatus | Should -Be "stable-baseline-published"
        $workerColdStartSli.GuardrailCoverageStatus | Should -Be "guardrail-catalog-mapped"
        $workerColdStartSli.GuardrailReferences.Benchmark | Should -Contain "BuildStartWorkerHost"
        $requestAllocationBaseline = $json.SrePostureEvidence.StableBaselineRows | Where-Object { $_.SliId -eq "engine.aspnetcore.request.alloc.bytes-per-op" }
        $requestAllocationBaseline.Measurements.Count | Should -Be 3
        $requestAllocationBaseline.Measurements.Benchmark | Should -Contain "HandleLoggedJsonRequest"
        $claimTruthBaseline = $json.SrePostureEvidence.StableBaselineRows | Where-Object { $_.SliId -eq "engine.deployment-mode-claims.truthful-fraction" }
        $claimTruthBaseline.MeasurementKind | Should -Be "deployment-mode-claims-report-baseline"
        $claimTruthBaseline.Measurements.ClaimsReportPath | Should -Be "artifacts/deployment-mode-claims-release/claim-validation-report.json"
        $claimTruthBaseline.Measurements.PublishProbeGateStatus | Should -Be "passed"
        $claimTruthBaseline.Measurements.PackageClaimTruthfulCount | Should -Be 3
        $claimTruthBaseline.Measurements.PackageClaimOverstatedCount | Should -Be 0
        $restoreWallTimeBaseline = $json.SrePostureEvidence.StableBaselineRows | Where-Object { $_.SliId -eq "engine.dotnet.restore.wall-time.lock-mode" }
        $restoreWallTimeBaseline.MeasurementKind | Should -Be "release-validation-step-wall-time-baseline"
        $restoreWallTimeBaseline.Measurements.TimingReportPath | Should -Be "artifacts/sre-release-validation/restore-wall-time.json"
        $restoreWallTimeBaseline.Measurements.StepName | Should -Be "Restore solution (locked mode)"
        $restoreWallTimeBaseline.Measurements.Status | Should -Be "passed"
        $restoreWallTimeBaseline.Measurements.ElapsedMilliseconds | Should -BeLessOrEqual 90000
        $referenceDocsWallTimeBaseline = $json.SrePostureEvidence.StableBaselineRows | Where-Object { $_.SliId -eq "engine.reference-docs.wall-time" }
        $referenceDocsWallTimeBaseline.MeasurementKind | Should -Be "release-validation-step-wall-time-baseline"
        $referenceDocsWallTimeBaseline.Measurements.TimingReportPath | Should -Be "artifacts/sre-release-validation/reference-docs-wall-time.json"
        $referenceDocsWallTimeBaseline.Measurements.StepName | Should -Be "Publish reference docs (Release)"
        $referenceDocsWallTimeBaseline.Measurements.Command | Should -Be "pwsh scripts/publish-reference-docs.ps1 -Configuration Release -OutputPath artifacts/reference-docs-release -SkipBuild"
        $referenceDocsWallTimeBaseline.Measurements.Status | Should -Be "passed"
        $referenceDocsWallTimeBaseline.Measurements.ElapsedMilliseconds | Should -BeLessOrEqual 300000
        $validateReleaseWallTimeBaseline = $json.SrePostureEvidence.StableBaselineRows | Where-Object { $_.SliId -eq "engine.validate-release.wall-time" }
        $validateReleaseWallTimeBaseline.MeasurementKind | Should -Be "release-validation-step-wall-time-baseline"
        $validateReleaseWallTimeBaseline.Measurements.TimingReportPath | Should -Be "artifacts/sre-release-validation/validate-release-wall-time.json"
        $validateReleaseWallTimeBaseline.Measurements.StepName | Should -Be "Validate release (canonical full run)"
        $validateReleaseWallTimeBaseline.Measurements.Command | Should -Be "pwsh scripts/validate-release.ps1"
        $validateReleaseWallTimeBaseline.Measurements.Status | Should -Be "passed"
        $validateReleaseWallTimeBaseline.Measurements.ElapsedMilliseconds | Should -BeLessOrEqual 1500000
        $workerColdStartBaseline = $json.SrePostureEvidence.StableBaselineRows | Where-Object { $_.SliId -eq "engine.worker.cold-start.p95" }
        $workerColdStartBaseline.MeasurementKind | Should -Be "benchmark-mean-baseline-proxy"
        $workerColdStartBaseline.Measurements.ReportFileName | Should -Be "Cephalon.Benchmarks.Runtime.ColdStartBenchmarks-report.csv"
        $workerColdStartBaseline.Measurements.Benchmark | Should -Be "BuildStartWorkerHost"
        $workerColdStartBaseline.Measurements.MeanNanoseconds | Should -BeLessOrEqual 500000000
        $workerColdStartBaseline.Measurements.GuardrailMaxMeanNanoseconds | Should -Be 5000000
        $workerColdStartBaseline.Measurements.GuardrailMaxAllocatedBytes | Should -Be 500000
        $aspNetCoreColdStartBaseline = $json.SrePostureEvidence.StableBaselineRows | Where-Object { $_.SliId -eq "engine.aspnetcore.minimal-api.cold-start.p95" }
        $aspNetCoreColdStartBaseline.MeasurementKind | Should -Be "benchmark-mean-baseline-proxy"
        $aspNetCoreColdStartBaseline.Measurements.ReportFileName | Should -Be "Cephalon.Benchmarks.Runtime.ColdStartBenchmarks-report.csv"
        $aspNetCoreColdStartBaseline.Measurements.Benchmark | Should -Be "BuildStartHandleFirstRequestAspNetCore"
        $aspNetCoreColdStartBaseline.Measurements.MeanNanoseconds | Should -BeLessOrEqual 800000000
        $aspNetCoreColdStartBaseline.Measurements.GuardrailMaxMeanNanoseconds | Should -Be 800000000
        $aspNetCoreColdStartBaseline.Measurements.GuardrailMaxAllocatedBytes | Should -Be 30000000
        $requestAllocationBaseline.Measurements.AllocatedBytes | Should -Contain 27914.24

        $json.SupplyChainEvidence.ManifestSchemaVersion | Should -Be "1.6.0"
        $json.SupplyChainEvidence.Status | Should -Be "workflow-ready-external-policy-pending"
        $json.SupplyChainEvidence.ReleaseWorkflow | Should -Be ".github/workflows/publish-release.yml"
        $json.SupplyChainEvidence.SourceDocuments | Should -Contain "docs/package-publishing.md"
        $json.SupplyChainEvidence.SourceDocuments | Should -Contain "docs/supply-chain-uplift-plan.md"
        $json.SupplyChainEvidence.ValidationScripts | Should -Contain "scripts/validate-release.ps1"
        $json.SupplyChainEvidence.ValidationScripts | Should -Contain "scripts/validate-nuget-vulnerability-audit.ps1"
        $json.SupplyChainEvidence.ValidationScripts | Should -Contain "scripts/validate-supply-chain-external-policy-preflight.ps1"
        $json.SupplyChainEvidence.ValidationScripts | Should -Contain "scripts/invoke-signed-release-dry-run.ps1"
        $json.SupplyChainEvidence.RequiredWorkflowTokens | Should -Contain "actions/attest-build-provenance"
        $json.SupplyChainEvidence.RequiredWorkflowTokens | Should -Contain "NuGet/login"
        $json.SupplyChainEvidence.RequiredWorkflowTokens | Should -Contain "Validate supply-chain external policy preflight"
        $json.SupplyChainEvidence.RequiredWorkflowTokens | Should -Contain "artifacts/nuget-vulnerability-audit-release"
        $json.SupplyChainEvidence.RequiredWorkflowTokens | Should -Contain "CEPHALON_NUGET_TRUSTED_PUBLISHING_POLICY_CONFIRMED"
        $json.SupplyChainEvidence.RequiredWorkflowTokens | Should -Contain "CEPHALON_NUGET_PREFIX_RESERVATION_CONFIRMED"
        $json.SupplyChainEvidence.EvidenceItemCount | Should -Be 12
        $json.SupplyChainEvidence.WorkflowReadyCount | Should -Be 9
        $json.SupplyChainEvidence.ExternalPolicyPendingCount | Should -Be 3
        $json.SupplyChainEvidence.ExternalPolicyPreflightCheckCount | Should -Be 3
        $json.SupplyChainEvidence.ExternalPolicyPreflight.Status | Should -Be "required-before-real-tag-push"
        $json.SupplyChainEvidence.ExternalPolicyPreflight.ValidationScript | Should -Be "scripts/validate-supply-chain-external-policy-preflight.ps1"
        $json.SupplyChainEvidence.ExternalPolicyPreflight.OutputPath | Should -Be "artifacts/supply-chain-external-policy/external-policy-preflight.json"
        $json.SupplyChainEvidence.ExternalPolicyPreflight.RequiredCheckCount | Should -Be 3
        $json.SupplyChainEvidence.ExternalPolicyPreflight.RequiredChecks.EvidenceItemId | Should -Contain "nuget-trusted-publishing-policy"
        $json.SupplyChainEvidence.ExternalPolicyPreflight.RequiredChecks.EvidenceItemId | Should -Contain "nuget-prefix-reservation"
        $json.SupplyChainEvidence.ExternalPolicyPreflight.RequiredChecks.EvidenceItemId | Should -Contain "nuget-user-secret"
        $json.SupplyChainEvidence.ExternalPolicyPreflight.ReleaseWorkflowTokens | Should -Contain "-RequireAll"
        $json.SupplyChainEvidence.SignedReleaseDryRun.Status | Should -Be "blocked"
        $json.SupplyChainEvidence.SignedReleaseDryRun.CurrentProofState | Should -Be "partial"
        $json.SupplyChainEvidence.SignedReleaseDryRun.CurrentBlockerClass | Should -Be "dispatch-identity-actions-disabled"
        $json.SupplyChainEvidence.SignedReleaseDryRun.ReadinessPolicy | Should -Be "workflow-dispatch-run-required-before-signed-release-proof"
        $json.SupplyChainEvidence.SignedReleaseDryRun.ValidationScript | Should -Be "scripts/invoke-signed-release-dry-run.ps1"
        $json.SupplyChainEvidence.SignedReleaseDryRun.OutputPath | Should -Be "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json"
        $json.SupplyChainEvidence.SignedReleaseDryRun.HandoffOutputPath | Should -Be "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md"
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredCommand | Should -Be "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated"
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredStatus | Should -Be "submitted"
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredRunCreated | Should -BeTrue
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredRunUrl | Should -BeTrue
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredReportFieldCount | Should -Be 9
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredReportFields | Should -Contain "RunUrl"
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredReportFields | Should -Contain "DispatchActor"
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredReportFields | Should -Contain "DispatchIdentityStatus"
        $json.SupplyChainEvidence.SignedReleaseDryRun.RequiredReportFields | Should -Contain "RequiredReleaseManagerAction"
        $json.SupplyChainEvidence.BlockedCount | Should -Be 0
        $json.SupplyChainEvidence.EvidenceItems.Id | Should -Contain "nuget-vulnerability-audit"
        $json.SupplyChainEvidence.EvidenceItems.Id | Should -Contain "cyclonedx-sbom-per-package"
        $json.SupplyChainEvidence.EvidenceItems.Id | Should -Contain "nuget-trusted-publishing-policy"
        $json.SupplyChainEvidence.EvidenceItems.Status | Should -Contain "external-policy-pending"

        $json.PublicApiCompatibilityEvidence.DeltaScript | Should -Be "scripts/summarise-public-api-deltas.ps1"
        $json.PublicApiCompatibilityEvidence.PackageCount | Should -Be 104
        $json.PublicApiCompatibilityEvidence.PendingPackageCount | Should -Be 0
        $json.PublicApiCompatibilityEvidence.HeaderOnlyPackageCount | Should -Be 104
        $json.PublicApiCompatibilityEvidence.AdditiveEntryCount | Should -Be 0
        $json.PublicApiCompatibilityEvidence.RemovalEntryCount | Should -Be 0
        $json.PublicApiCompatibilityEvidence.HasRemovalEntries | Should -BeFalse
        $json.PublicApiCompatibilityEvidence.PackageDeltas.Count | Should -Be 104
        $abstractionsDelta = $json.PublicApiCompatibilityEvidence.PackageDeltas | Where-Object { $_.Package -eq "Cephalon.Abstractions" }
        $abstractionsDelta.Project | Should -Be "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj"
        $abstractionsDelta.Unshipped | Should -Be "src/Cephalon.Abstractions/PublicAPI.Unshipped.txt"
        $abstractionsDelta.Shipped | Should -Be "src/Cephalon.Abstractions/PublicAPI.Shipped.txt"
        $abstractionsDelta.AdditiveEntryCount | Should -Be 0
        $abstractionsDelta.RemovalEntryCount | Should -Be 0
        $abstractionsDelta.HasPendingChanges | Should -BeFalse

        $corePackage = $json.PackageGAReadiness | Where-Object { $_.Package -eq "Cephalon.Abstractions" }
        $corePackage.Family | Should -Be "Core runtime"
        $corePackage.Maturity | Should -Be "M4"
        $corePackage.GAGateStatus | Should -Be "partial"
        $corePackage.GABlockerClass | Should -Be "release-evidence"

        $notClaimedPackages = @($json.PackageGAReadiness | Where-Object { $_.GAGateStatus -eq "not-claimed" })
        $notClaimedPackages.Package | Should -Contain "Cephalon.Data.MySql.SciSharpReplication"
        @($notClaimedPackages.Package | Where-Object { $_ -match "Cephalon\.Observability\.\*Dependencies" }).Count | Should -Be 0
        $notClaimedPackages.GABlockerClass | Select-Object -Unique | Should -Be "runtime-support-not-claimed"

        $markdown = Get-Content -LiteralPath $result.Paths.MarkdownPath -Raw -Encoding UTF8
        $markdown | Should -Match "Engine Completion Scorecard Report"
        $markdown | Should -Match "Platform gates: 12"
        $markdown | Should -Match "Package families: 9"
        $markdown | Should -Match "Deployment-Mode Evidence"
        $markdown | Should -Match "Deployment-mode known hazards: 15"
        $markdown | Should -Match "Publish-probe release validation mode: single-file-publish-gate"
        $markdown | Should -Match "Publish-probe release validation deployment modes: singleFile"
        $markdown | Should -Match "Publish-probe gated modes: singleFile"
        $markdown | Should -Match "SRE Posture Evidence"
        $markdown | Should -Match "SRE SLIs: 11"
        $markdown | Should -Match "SRE pending stable baselines: 1"
        $markdown | Should -Match "SRE stable baselines: 10"
        $markdown | Should -Match "SRE stable baseline rows: 10"
        $markdown | Should -Match "Pending baseline SLI"
        $markdown | Should -Match "ci-metadata-unavailable"
        $markdown | Should -Match "ci-flake-rate-report"
        $markdown | Should -Match "BuildStartHandleFirstRequestAspNetCore"
        $markdown | Should -Match "deployment-mode-claims-report-baseline"
        $markdown | Should -Match "release-validation-step-wall-time-baseline"
        $markdown | Should -Match "package claims 3/3 truthful"
        $markdown | Should -Match "Restore solution \(locked mode\)"
        $markdown | Should -Match "Publish reference docs \(Release\)"
        $markdown | Should -Match "Validate release \(canonical full run\)"
        $markdown | Should -Match "BuildStartWorkerHost"
        $markdown | Should -Match "Stable baseline manifest"
        $markdown | Should -Match "SRE guardrail-mapped SLIs: 6"
        $markdown | Should -Match "SRE pending guardrail coverage SLIs: 0"
        $markdown | Should -Match "Guardrail coverage"
        $markdown | Should -Match "Supply-Chain Release Evidence"
        $markdown | Should -Match "Supply-chain evidence items: 12"
        $markdown | Should -Match "Signed-release dry-run status: blocked"
        $markdown | Should -Match "Signed-Release Dry Run"
        $markdown | Should -Match "External-Policy Preflight"
        $markdown | Should -Match "External-policy preflight checks: 3"
        $markdown | Should -Match "external-policy-pending"
        $markdown | Should -Match "Public API Compatibility Evidence"
        $markdown | Should -Match "Public API packages with pending changes: 0"
        $markdown | Should -Match "Public API additive entries: 0"
        $markdown | Should -Match "Header-only packages: 104"
        $markdown | Should -Match "Cephalon.Abstractions"
        $markdown | Should -Match "Adoption Smoke Evidence"
        $markdown | Should -Match "out-of-tree-generated-app-package-stage"
        $markdown | Should -Match "Adoption smoke golden use cases: 7"
        $markdown | Should -Match "Adoption smoke golden use-case execution reports: 5"
        $markdown | Should -Match "out-of-tree-package-adoption"
        $markdown | Should -Match "generated-app-runtime-foundation"
        $markdown | Should -Match "template-pack-dotnet-new-parity"
        $markdown | Should -Match "modular-monolith-rest-worker-data"
        $markdown | Should -Match "vertical-slice-eventing-outbox"
        $markdown | Should -Match "Provider Integration Evidence"
        $markdown | Should -Match "Provider integration evidence rows: 33"
        $markdown | Should -Match "live proofs: 33"
        $markdown | Should -Match "composition-only rows: 0"
        $markdown | Should -Match "dependency-health"
        $markdown | Should -Match "Evidence Source References"
        $markdown | Should -Match "Package GA Readiness"
    }

    It "fails when public API unshipped files are missing shipped baselines" {
        $fixtureRoot = Join-Path $script:tempRoot "public-api-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $packageRoot = Join-Path $fixtureRoot "src\Cephalon.Fixture"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $scriptsRoot "summarise-public-api-deltas.ps1") -Value "# public API delta fixture" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $packageRoot "Cephalon.Fixture.csproj") -Value "<Project />" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $packageRoot "PublicAPI.Unshipped.txt") -Value "#nullable enable`nCephalon.Fixture.PendingApi" -Encoding UTF8

        {
            Convert-PublicApiCompatibilityEvidence `
                -ResolvedPublicApiDeltaScriptPath (Join-Path $scriptsRoot "summarise-public-api-deltas.ps1") `
                -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*missing PublicAPI.Shipped.txt*"
    }

    It "fails when deployment-mode support manifest paths drift away from repo files" {
        $fixtureRoot = Join-Path $script:tempRoot "deployment-mode-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null

        $manifestPath = Join-Path $scriptsRoot "deployment-mode-support.json"
        Set-Content -LiteralPath $manifestPath -Value @'
{
  "$schemaVersion": "1.4.0",
  "shippingBaseline": {
    "stableTargetFramework": "net10.0",
    "readinessLaneTargetFramework": "net11.0",
    "readinessLaneStatus": "assessment-only"
  },
  "documentation": {
    "guidePath": "docs/missing-deployment-mode-support.md",
    "readinessGuidePath": "docs/dotnet11-readiness.md",
    "compatibilityGuidePath": "docs/compatibility.md",
    "packagePublishingGuidePath": "docs/package-publishing.md",
    "validationHarnessPath": "scripts/validate-deployment-mode-claims.ps1",
    "validationHarnessTestsPath": "tests/Cephalon.Tests.Scripts/validate-deployment-mode-claims.Tests.ps1"
  }
}
'@ -Encoding UTF8

        {
            Convert-DeploymentModeEvidence `
                -ResolvedManifestPath $manifestPath `
                -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*documentation.guidePath*"
    }

    It "fails when deployment-mode claims report gate drifts from publish proof" {
        $reportPath = Join-Path $script:tempRoot "drifted-claim-validation-report.json"
        $manifestPath = (Resolve-Path (Join-Path $script:repoRoot "scripts\deployment-mode-support.json")).Path
        $report = [ordered]@{
            DeploymentMode = "singleFile"
            ManifestPath = $manifestPath
            AggregateVerdict = "not-claimed"
            PublishProbePolicy = [ordered]@{
                ReleaseValidationMode = "single-file-publish-gate"
                ReleaseValidationDeploymentModes = @("singleFile")
                ReleaseValidationSkipsPublish = $false
                CurrentRunSkipsPublish = $false
                NonOptOutGate = $true
                GatedModes = @("singleFile")
                AuditOnlyModes = @("trim", "nativeAot")
                FailureBlocksRelease = $true
                FailOnWarnings = $true
            }
            PublishProbeGate = [ordered]@{
                Status = "failed"
                Enabled = $true
                FailureBlocksRelease = $true
                FailOnWarnings = $true
                FailureCount = 1
            }
            Modes = @()
            HazardInventory = [ordered]@{
                TotalPackages = 9
                PackagesWithScopedClaims = 3
                TotalKnownHazards = 15
                KnownTransitiveHazardAudit = [ordered]@{
                    Status = "matched"
                    MissingEntries = 0
                    LockFileCount = 116
                }
                BoundaryAnnotationAuditStatus = "matched"
                BoundaryAnnotationAuditFailureCount = 0
                CoreRouteDelegateAuditStatus = "matched"
                CoreRouteDelegateAuditFailureCount = 0
                FullCommonRouteDelegateAuditStatus = "matched"
                FullCommonRouteDelegateAuditFailureCount = 0
                FullOperatorRouteDelegateAuditStatus = "matched"
                FullOperatorRouteDelegateAuditFailureCount = 0
                OperatorResponseJsonContractAuditStatus = "matched"
                OperatorResponseJsonContractAuditFailureCount = 0
            }
        }
        $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reportPath -Encoding UTF8

        {
            Convert-DeploymentModeEvidence `
                -ResolvedManifestPath $manifestPath `
                -ResolvedClaimsReportPath $reportPath `
                -ResolvedRepoRoot $script:repoRoot
        } | Should -Throw "*publish-probe gate must be passed*"
    }

    It "fails when adoption smoke runtime probes drift away from the replay script" {
        $fixtureRoot = Join-Path $script:tempRoot "adoption-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $sampleRoot = Join-Path $fixtureRoot "samples\Cephalon.ReferenceModule.Operations"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $sampleRoot -Force | Out-Null
        $claimsReportPath = Join-Path $script:tempRoot "adoption-claim-validation-report.json"
        $deploymentManifestPath = (Resolve-Path (Join-Path $script:repoRoot "scripts\deployment-mode-support.json")).Path
        New-TestDeploymentModeClaimsReport -Path $claimsReportPath -ManifestPath $deploymentManifestPath

        $replayScriptPath = Join-Path $scriptsRoot "validate-out-of-tree-package-adoption.ps1"
        Set-Content -LiteralPath $replayScriptPath -Value @'
Cephalon.Cli
cephalon package stage
Cephalon.ReferenceModule.Operations
PackageDirectories
PackagePolicy
Trust
RequireTrustedPackages
TrustedPublishers
Start-Process
"run"
ReportPath
artifacts/adoption-smoke/out-of-tree-package-adoption.json
/health/ready
/engine/packages
'@ -Encoding UTF8

        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-generated-app-adoption.ps1") -Value "# generated app replay" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-template-pack-adoption.ps1") -Value "# template pack replay" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-modular-monolith-adoption.ps1") -Value "# modular monolith replay" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-vertical-slice-eventing-adoption.ps1") -Value "# vertical slice eventing replay" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $sampleRoot "Cephalon.ReferenceModule.Operations.csproj") -Value "<Project />" -Encoding UTF8
        foreach ($docName in @("getting-started.md", "operations.md", "external-package-lifecycle.md", "engine-completion-scorecard.md", "release-checklist.md")) {
            Set-Content -LiteralPath (Join-Path $docsRoot $docName) -Value "# $docName" -Encoding UTF8
        }

        $manifestPath = Join-Path $scriptsRoot "adoption-smoke-support.json"
        @{
            '$schemaVersion' = "1.6.0"
            scenarioId = "fixture"
            status = "execution-report-ready"
            validationScript = $replayScriptPath
            supportingScripts = @(
                (Join-Path $scriptsRoot "validate-generated-app-adoption.ps1"),
                (Join-Path $scriptsRoot "validate-template-pack-adoption.ps1"),
                (Join-Path $scriptsRoot "validate-modular-monolith-adoption.ps1"),
                (Join-Path $scriptsRoot "validate-vertical-slice-eventing-adoption.ps1")
            )
            sourceDocs = @(
                (Join-Path $docsRoot "getting-started.md"),
                (Join-Path $docsRoot "operations.md"),
                (Join-Path $docsRoot "external-package-lifecycle.md"),
                (Join-Path $docsRoot "engine-completion-scorecard.md"),
                (Join-Path $docsRoot "release-checklist.md")
            )
            referenceModuleProject = (Join-Path $sampleRoot "Cephalon.ReferenceModule.Operations.csproj")
            goldenUseCases = @(
                @{
                    id = "fixture-use-case"
                    title = "Fixture use case"
                    appShape = "modular-monolith"
                    status = "execution-report-ready"
                    proofLevel = "executable-smoke"
                    primaryValidationPath = $replayScriptPath
                    sourceDocs = @(
                        (Join-Path $docsRoot "getting-started.md")
                    )
                    qualityDimensions = @("Reliability")
                    requiredEngineCapabilities = @("runtime introspection")
                    proofTargets = @(
                        @{ kind = "runtime-probe"; reference = "/engine/packages" }
                    )
                    executionReport = @{
                        schemaVersion = "1.0.0"
                        defaultPath = "artifacts/adoption-smoke/out-of-tree-package-adoption.json"
                        statusValues = @("passed", "failed")
                        requiredFields = @(
                            '$schemaVersion',
                            "ScenarioId",
                            "Status",
                            "StartedAtUtc",
                            "CompletedAtUtc",
                            "DurationMilliseconds",
                            "Assertions",
                            "RuntimeProbes",
                            "Paths"
                        )
                    }
                }
            )
            assertions = @{
                runsOutsideRepository = $true
                publishesLocalPackages = $true
                installsCliFromTemporaryFeed = $true
                scaffoldsGeneratedApp = $true
                stagesReferenceModulePackage = $true
                patchesPackagePolicyAndTrust = $true
                runsGeneratedHost = $true
            }
            requiredScriptTokens = @("Cephalon.Cli", "cephalon package stage", "Cephalon.ReferenceModule.Operations", "PackageDirectories", "PackagePolicy", "Trust", "RequireTrustedPackages", "TrustedPublishers", "Start-Process", '"run"')
            executionReport = @{
                schemaVersion = "1.0.0"
                defaultPath = "artifacts/adoption-smoke/out-of-tree-package-adoption.json"
                statusValues = @("passed", "failed")
                requiredFields = @(
                    '$schemaVersion',
                    "ScenarioId",
                    "Status",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "DurationMilliseconds",
                    "Assertions",
                    "RuntimeProbes",
                    "Paths"
                )
            }
            runtimeProbes = @(
                @{ kind = "package-runtime"; path = "/engine/packages" },
                @{ kind = "trust-policy"; path = "/engine/trust-policy" }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        {
            Invoke-EngineCompletionScorecardPublish `
                -ScorecardPath "docs/engine-completion-scorecard.md" `
                -ConformanceMatrixPath "docs/conformance-matrix.md" `
                -DeploymentModeClaimsReportPath $claimsReportPath `
                -AdoptionSmokeManifestPath $manifestPath `
                -OutputPath (Join-Path $script:tempRoot "artifacts") `
                -RepoRoot $script:repoRoot
        } | Should -Throw "*runtime probe '/engine/trust-policy'*"
    }

    It "fails when SRE posture manifest SLIs drift away from source docs" {
        $fixtureRoot = Join-Path $script:tempRoot "sre-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $guardrailRoot = Join-Path $fixtureRoot "benchmarks\Cephalon.Benchmarks\guardrails"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $guardrailRoot -Force | Out-Null

        Set-Content -LiteralPath (Join-Path $docsRoot "sre-posture.md") -Value "# SRE posture fixture`nThis fixture intentionally omits the declared SLI." -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $docsRoot "benchmarking.md") -Value "# Benchmarking fixture" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-release.ps1") -Value "# release validation fixture" -Encoding UTF8
        @{
            version = "1.0"
            entries = @(
                @{
                    reportFileName = "fixture.csv"
                    benchmark = "Fixture"
                    maxMeanNanoseconds = 1
                    maxAllocatedBytes = 1
                }
            )
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $guardrailRoot "performance-guardrails.json") -Encoding UTF8

        $manifestPath = Join-Path $scriptsRoot "sre-posture-support.json"
        @{
            '$schemaVersion' = "1.0.0"
            status = "target-declared"
            summary = "fixture"
            releaseValidationSummaryMode = "scorecard-artifact"
            stableBaselinesPublished = $false
            sourceDocs = @(
                "docs/sre-posture.md",
                "docs/benchmarking.md"
            )
            validationScripts = @("scripts/validate-release.ps1")
            guardrailCatalog = "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json"
            slis = @(
                @{
                    id = "engine.fixture.missing"
                    category = "fixture"
                    measurementSurface = "benchmark"
                    sourceDocument = "docs/sre-posture.md"
                    sloTarget = "fixture"
                    window = "fixture"
                    targetStatus = "target-declared"
                    baselineStatus = "pending-stable-baseline"
                    guardrailCoverageStatus = "guardrail-catalog-mapped"
                    guardrailReferences = @(
                        @{
                            reportFileName = "fixture.csv"
                            benchmark = "Fixture"
                        }
                    )
                }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        {
            Convert-SrePostureEvidence `
                -ResolvedManifestPath $manifestPath `
                -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*does not contain SLI 'engine.fixture.missing'*"
    }

    It "fails when SRE guardrail references drift away from the guardrail catalog" {
        $fixtureRoot = Join-Path $script:tempRoot "sre-guardrail-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $guardrailRoot = Join-Path $fixtureRoot "benchmarks\Cephalon.Benchmarks\guardrails"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $guardrailRoot -Force | Out-Null

        Set-Content -LiteralPath (Join-Path $docsRoot "sre-posture.md") -Value "# SRE posture fixture`nengine.fixture.present" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $docsRoot "benchmarking.md") -Value "# Benchmarking fixture" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-release.ps1") -Value "# release validation fixture" -Encoding UTF8
        @{
            version = "1.0"
            entries = @(
                @{
                    reportFileName = "fixture.csv"
                    benchmark = "Fixture"
                    maxMeanNanoseconds = 1
                    maxAllocatedBytes = 1
                }
            )
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $guardrailRoot "performance-guardrails.json") -Encoding UTF8

        $manifestPath = Join-Path $scriptsRoot "sre-posture-support.json"
        @{
            '$schemaVersion' = "1.1.0"
            status = "target-declared"
            summary = "fixture"
            releaseValidationSummaryMode = "scorecard-artifact"
            stableBaselinesPublished = $false
            sourceDocs = @(
                "docs/sre-posture.md",
                "docs/benchmarking.md"
            )
            validationScripts = @("scripts/validate-release.ps1")
            guardrailCatalog = "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json"
            slis = @(
                @{
                    id = "engine.fixture.present"
                    category = "fixture"
                    measurementSurface = "benchmark"
                    sourceDocument = "docs/sre-posture.md"
                    sloTarget = "fixture"
                    window = "fixture"
                    targetStatus = "target-declared"
                    baselineStatus = "pending-stable-baseline"
                    guardrailCoverageStatus = "guardrail-catalog-mapped"
                    guardrailReferences = @(
                        @{
                            reportFileName = "missing.csv"
                            benchmark = "Missing"
                        }
                    )
                }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        {
            Convert-SrePostureEvidence `
                -ResolvedManifestPath $manifestPath `
                -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*references guardrail 'missing.csv' / 'Missing'*"
    }

    It "fails when SRE stable baseline rows drift away from promoted SLI rows" {
        $fixtureRoot = Join-Path $script:tempRoot "sre-stable-baseline-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $guardrailRoot = Join-Path $fixtureRoot "benchmarks\Cephalon.Benchmarks\guardrails"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $guardrailRoot -Force | Out-Null

        Set-Content -LiteralPath (Join-Path $docsRoot "sre-posture.md") -Value "# SRE posture fixture`nengine.fixture.present" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $docsRoot "benchmarking.md") -Value "# Benchmarking fixture" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-release.ps1") -Value "# release validation fixture" -Encoding UTF8
        @{
            version = "1.0"
            entries = @(
                @{
                    reportFileName = "fixture.csv"
                    benchmark = "Fixture"
                    maxMeanNanoseconds = 10
                    maxAllocatedBytes = 20
                }
            )
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $guardrailRoot "performance-guardrails.json") -Encoding UTF8

        @{
            '$schemaVersion' = "1.0.0"
            status = "benchmark-baseline-published"
            capturedAtUtc = "2026-05-08T09:08:04.9633564Z"
            capturedFromCommit = "fixture"
            publishedBaselineSliIds = @("engine.fixture.present")
            pendingBaselineSliIds = @()
            baselineRows = @(
                @{
                    sliId = "engine.fixture.other"
                    status = "stable-baseline-published"
                    measurementKind = "benchmark-mean-baseline-proxy"
                    measurements = @(
                        @{
                            reportFileName = "fixture.csv"
                            benchmark = "Fixture"
                            meanNanoseconds = 1
                            errorNanoseconds = 1
                            stdDevNanoseconds = 1
                            allocatedBytes = 1
                            guardrailMaxMeanNanoseconds = 10
                            guardrailMaxAllocatedBytes = 20
                        }
                    )
                }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $scriptsRoot "sre-stable-baselines.json") -Encoding UTF8

        $manifestPath = Join-Path $scriptsRoot "sre-posture-support.json"
        @{
            '$schemaVersion' = "1.2.0"
            status = "partial-stable-baseline-published"
            summary = "fixture"
            releaseValidationSummaryMode = "scorecard-artifact"
            stableBaselinesPublished = $true
            stableBaselineManifest = "scripts/sre-stable-baselines.json"
            sourceDocs = @(
                "docs/sre-posture.md",
                "docs/benchmarking.md"
            )
            validationScripts = @("scripts/validate-release.ps1")
            guardrailCatalog = "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json"
            slis = @(
                @{
                    id = "engine.fixture.present"
                    category = "fixture"
                    measurementSurface = "benchmark"
                    sourceDocument = "docs/sre-posture.md"
                    sloTarget = "fixture"
                    window = "fixture"
                    targetStatus = "target-declared"
                    baselineStatus = "stable-baseline-published"
                    guardrailCoverageStatus = "guardrail-catalog-mapped"
                    guardrailReferences = @(
                        @{
                            reportFileName = "fixture.csv"
                            benchmark = "Fixture"
                        }
                    )
                }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        {
            Convert-SrePostureEvidence `
                -ResolvedManifestPath $manifestPath `
                -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*stable baseline row references SLI 'engine.fixture.other'*"
    }

    It "fails when SRE pending baseline rows reference stable SLI rows" {
        $fixtureRoot = Join-Path $script:tempRoot "sre-pending-baseline-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $guardrailRoot = Join-Path $fixtureRoot "benchmarks\Cephalon.Benchmarks\guardrails"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $guardrailRoot -Force | Out-Null

        Set-Content -LiteralPath (Join-Path $docsRoot "sre-posture.md") -Value "# SRE posture fixture`nengine.fixture.stable`nengine.fixture.pending" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $docsRoot "benchmarking.md") -Value "# Benchmarking fixture" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-release.ps1") -Value "# release validation fixture" -Encoding UTF8
        @{
            version = "1.0"
            entries = @(
                @{
                    reportFileName = "fixture.csv"
                    benchmark = "Fixture"
                    maxMeanNanoseconds = 10
                    maxAllocatedBytes = 20
                }
            )
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $guardrailRoot "performance-guardrails.json") -Encoding UTF8

        @{
            '$schemaVersion' = "1.6.0"
            status = "pending-baseline-evidence-published"
            capturedAtUtc = "2026-05-08T16:51:22.2560222Z"
            capturedFromCommit = "fixture"
            publishedBaselineSliIds = @("engine.fixture.stable")
            pendingBaselineSliIds = @("engine.fixture.pending")
            pendingBaselineRows = @(
                @{
                    sliId = "engine.fixture.stable"
                    status = "pending-stable-baseline"
                    blockerClass = "fixture-blocker"
                    blockerSummary = "fixture"
                    nextEvidenceNeeded = "fixture"
                    evidence = @(
                        @{
                            kind = "fixture"
                            promotionAllowed = $false
                        }
                    )
                }
            )
            baselineRows = @(
                @{
                    sliId = "engine.fixture.stable"
                    status = "stable-baseline-published"
                    measurementKind = "benchmark-mean-baseline-proxy"
                    measurements = @(
                        @{
                            reportFileName = "fixture.csv"
                            benchmark = "Fixture"
                            meanNanoseconds = 1
                            errorNanoseconds = 1
                            stdDevNanoseconds = 1
                            allocatedBytes = 1
                            guardrailMaxMeanNanoseconds = 10
                            guardrailMaxAllocatedBytes = 20
                        }
                    )
                }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $scriptsRoot "sre-stable-baselines.json") -Encoding UTF8

        $manifestPath = Join-Path $scriptsRoot "sre-posture-support.json"
        @{
            '$schemaVersion' = "1.8.0"
            status = "partial-stable-baseline-published"
            summary = "fixture"
            releaseValidationSummaryMode = "scorecard-artifact"
            stableBaselinesPublished = $true
            stableBaselineManifest = "scripts/sre-stable-baselines.json"
            sourceDocs = @(
                "docs/sre-posture.md",
                "docs/benchmarking.md"
            )
            validationScripts = @("scripts/validate-release.ps1")
            guardrailCatalog = "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json"
            slis = @(
                @{
                    id = "engine.fixture.stable"
                    category = "fixture"
                    measurementSurface = "benchmark"
                    sourceDocument = "docs/sre-posture.md"
                    sloTarget = "fixture"
                    window = "fixture"
                    targetStatus = "target-declared"
                    baselineStatus = "stable-baseline-published"
                    guardrailCoverageStatus = "guardrail-catalog-mapped"
                    guardrailReferences = @(
                        @{
                            reportFileName = "fixture.csv"
                            benchmark = "Fixture"
                        }
                    )
                },
                @{
                    id = "engine.fixture.pending"
                    category = "fixture"
                    measurementSurface = "benchmark"
                    sourceDocument = "docs/sre-posture.md"
                    sloTarget = "fixture"
                    window = "fixture"
                    targetStatus = "target-declared"
                    baselineStatus = "pending-stable-baseline"
                    guardrailCoverageStatus = "not-applicable"
                    guardrailReferences = @()
                }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        {
            Convert-SrePostureEvidence `
                -ResolvedManifestPath $manifestPath `
                -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*pending baseline row references SLI 'engine.fixture.stable', but its SRE posture baselineStatus is 'stable-baseline-published'*"
    }

    It "fails when supply-chain release evidence drifts away from the release workflow" {
        $fixtureRoot = Join-Path $script:tempRoot "supply-chain-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $workflowRoot = Join-Path $fixtureRoot ".github\workflows"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $workflowRoot -Force | Out-Null

        Set-Content -LiteralPath (Join-Path $workflowRoot "publish-release.yml") -Value @'
name: Publish Release
permissions:
  id-token: write
jobs:
  build-sign-attest:
    steps:
      - name: Generate SLSA
        uses: actions/attest-build-provenance@v2
'@ -Encoding UTF8

        Set-Content -LiteralPath (Join-Path $docsRoot "package-publishing.md") -Value "# Package publishing fixture`nCycloneDX SBOM`nNuGet trusted-publishing login" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $docsRoot "supply-chain-uplift-plan.md") -Value "# Supply-chain fixture`nNuGet lock files" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-release.ps1") -Value "# release validation fixture" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "publish-package-artifacts.ps1") -Value "# package publishing fixture" -Encoding UTF8

        $manifestPath = Join-Path $scriptsRoot "supply-chain-release-support.json"
        @{
            '$schemaVersion' = "1.0.0"
            status = "workflow-ready"
            summary = "fixture"
            releaseWorkflow = ".github/workflows/publish-release.yml"
            sourceDocs = @(
                "docs/package-publishing.md",
                "docs/supply-chain-uplift-plan.md"
            )
            validationScripts = @(
                "scripts/validate-release.ps1",
                "scripts/publish-package-artifacts.ps1"
            )
            requiredWorkflowTokens = @("CycloneDX")
            evidenceItems = @(
                @{
                    id = "fixture-sbom"
                    category = "sbom"
                    status = "workflow-ready"
                    summary = "fixture"
                    sourceDocument = "docs/package-publishing.md"
                    sourceToken = "CycloneDX SBOM"
                    workflowTokens = @("CycloneDX")
                    externalPolicyRequired = $false
                }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        {
            Convert-SupplyChainEvidence `
                -ResolvedManifestPath $manifestPath `
                -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*does not contain required token 'CycloneDX'*"
    }

    It "fails when external-policy-pending supply-chain evidence lacks a preflight contract" {
        $fixtureRoot = Join-Path $script:tempRoot "supply-chain-preflight-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $workflowRoot = Join-Path $fixtureRoot ".github\workflows"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $workflowRoot -Force | Out-Null

        Set-Content -LiteralPath (Join-Path $workflowRoot "publish-release.yml") -Value @'
name: Publish Release
permissions:
  id-token: write
jobs:
  publish:
    steps:
      - name: NuGet trusted publishing (login)
        uses: NuGet/login@v1
'@ -Encoding UTF8

        Set-Content -LiteralPath (Join-Path $docsRoot "package-publishing.md") -Value "# Package publishing fixture`nNuGet trusted-publishing login" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-release.ps1") -Value "# release validation fixture" -Encoding UTF8

        $manifestPath = Join-Path $scriptsRoot "supply-chain-release-support.json"
        @{
            '$schemaVersion' = "1.1.0"
            status = "workflow-ready-external-policy-pending"
            summary = "fixture"
            releaseWorkflow = ".github/workflows/publish-release.yml"
            sourceDocs = @("docs/package-publishing.md")
            validationScripts = @("scripts/validate-release.ps1")
            requiredWorkflowTokens = @("NuGet/login")
            evidenceItems = @(
                @{
                    id = "nuget-trusted-publishing-policy"
                    category = "publisher-identity"
                    status = "external-policy-pending"
                    summary = "fixture"
                    sourceDocument = "docs/package-publishing.md"
                    sourceToken = "NuGet trusted-publishing login"
                    workflowTokens = @("NuGet/login")
                    externalPolicyRequired = $true
                }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        {
            Convert-SupplyChainEvidence `
                -ResolvedManifestPath $manifestPath `
                -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*must declare externalPolicyPreflight when external-policy-pending evidence items exist*"
    }

    It "fails when the status vocabulary contains an unsupported status" {
        $fixtureRoot = Join-Path $script:tempRoot "fixture"
        $docsRoot = Join-Path $fixtureRoot "docs"
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null

        $sourcePath = Join-Path $script:repoRoot "docs\engine-completion-scorecard.md"
        $fixturePath = Join-Path $docsRoot "engine-completion-scorecard.md"
        $contents = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
        $contents = $contents.Replace('| `ready-for-preview` | Enough evidence exists for preview adopters, with documented gaps and no unsupported claim. |', '| `almost-ready` | Unsupported test fixture status. |')
        $contents | Should -Match '\| `almost-ready` \| Unsupported test fixture status\. \|'
        Set-Content -LiteralPath $fixturePath -Value $contents -Encoding UTF8

        {
            Invoke-EngineCompletionScorecardPublish `
                -ScorecardPath "docs/engine-completion-scorecard.md" `
                -ConformanceMatrixPath "docs/conformance-matrix.md" `
                -OutputPath (Join-Path $fixtureRoot "artifacts") `
                -RepoRoot $fixtureRoot
        } | Should -Throw "*Unsupported scorecard status vocabulary entry 'almost-ready'*"
    }

    It "fails when an evidence source reference points at a missing repo file" {
        $fixtureRoot = Join-Path $script:tempRoot "fixture"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $workflowRoot = Join-Path $fixtureRoot ".github\workflows"
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $workflowRoot -Force | Out-Null

        Copy-Item -LiteralPath (Join-Path $script:repoRoot "docs\conformance-matrix.md") -Destination (Join-Path $docsRoot "conformance-matrix.md")
        Copy-Item -LiteralPath (Join-Path $script:repoRoot "scripts\deployment-mode-support.json") -Destination (Join-Path $scriptsRoot "deployment-mode-support.json")
        Copy-Item -LiteralPath (Join-Path $script:repoRoot "scripts\supply-chain-release-support.json") -Destination (Join-Path $scriptsRoot "supply-chain-release-support.json")
        Copy-Item -LiteralPath (Join-Path $script:repoRoot ".github\workflows\publish-release.yml") -Destination (Join-Path $workflowRoot "publish-release.yml")

        $supportingDocs = @(
            "benchmarking.md",
            "compatibility.md",
            "deployment-mode-support.md",
            "dotnet11-readiness.md",
            "package-publishing.md",
            "release-checklist.md",
            "runtime-contract-index.md",
            "sre-posture.md",
            "supply-chain-uplift-plan.md",
            "test-coverage-roadmap.md"
        )
        foreach ($doc in $supportingDocs) {
            Copy-Item -LiteralPath (Join-Path $script:repoRoot "docs\$doc") -Destination (Join-Path $docsRoot $doc)
        }

        $sourcePath = Join-Path $script:repoRoot "docs\engine-completion-scorecard.md"
        $fixturePath = Join-Path $docsRoot "engine-completion-scorecard.md"
        $contents = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
        $contents = $contents.Replace("(engine-surface-maturity-audit.md)", "(missing-scorecard-source.md)")
        $contents | Should -Match "missing-scorecard-source.md"
        Set-Content -LiteralPath $fixturePath -Value $contents -Encoding UTF8

        {
            Invoke-EngineCompletionScorecardPublish `
                -ScorecardPath "docs/engine-completion-scorecard.md" `
                -ConformanceMatrixPath "docs/conformance-matrix.md" `
                -OutputPath (Join-Path $fixtureRoot "artifacts") `
                -RepoRoot $fixtureRoot
        } | Should -Throw "*Scorecard evidence source reference 'missing-scorecard-source.md'*"
    }

    It "fails when eventing operational-superiority runtime concordance drifts from source" {
        $fixtureRoot = Join-Path $script:tempRoot "eventing-concordance-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null

        $manifestPath = Join-Path $scriptsRoot "eventing-operational-superiority-support.json"
        Copy-Item -LiteralPath (Join-Path $script:repoRoot "scripts\eventing-operational-superiority-support.json") -Destination $manifestPath
        $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32

        $referencedPaths = [System.Collections.Generic.List[string]]::new()
        @($manifest.sourceDocs + $manifest.validationProjects + $manifest.validationFiles + @($manifest.runtimeConcordance.source)) |
            ForEach-Object { $referencedPaths.Add([string]$_) }
        foreach ($dimension in @($manifest.requiredDimensions)) {
            @($dimension.sourceDocs + $dimension.validationFiles) |
                ForEach-Object { $referencedPaths.Add([string]$_) }
        }

        foreach ($reference in @($referencedPaths | Sort-Object -Unique)) {
            $path = Join-Path $fixtureRoot $reference
            New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
            "" | Set-Content -LiteralPath $path -Encoding UTF8
        }

        $missingToken = "comparisonBaseline=MassTransit,NServiceBus,Wolverine,MediatR"
        $runtimeTokens = @(
            [string]$manifest.runtimeConcordance.profileSurfaceToken
            $manifest.runtimeConcordance.requiredTokens |
                Where-Object { [string]$_ -ne $missingToken } |
                ForEach-Object { [string]$_ }
        )
        Set-Content -LiteralPath (Join-Path $fixtureRoot $manifest.runtimeConcordance.source) -Value ($runtimeTokens -join [Environment]::NewLine) -Encoding UTF8

        {
            Convert-EventingOperationalSuperiorityEvidence -ResolvedManifestPath $manifestPath -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*runtimeConcordance source 'src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs' is missing required token(s): $missingToken*"
    }

    It "fails when dependency-health provider rows omit their source-derived manifest" {
        $manifestPath = Join-Path $script:tempRoot "provider-integration-support.json"
        $manifestContents = Get-Content -LiteralPath (Join-Path $script:repoRoot "scripts\provider-integration-support.json") -Raw -Encoding UTF8
        $manifestContents = [regex]::Replace(
            $manifestContents,
            '(?m)^\s+"dependencyHealthProviderManifest": "scripts/observability-dependency-health-providers\.json",\r?\n',
            "")

        $manifestContents | Should -Not -Match "dependencyHealthProviderManifest"
        Set-Content -LiteralPath $manifestPath -Value $manifestContents -Encoding UTF8

        {
            Convert-ProviderIntegrationEvidence -ResolvedManifestPath $manifestPath -ResolvedRepoRoot $script:repoRoot
        } | Should -Throw "*Provider integration support manifest must declare dependencyHealthProviderManifest when dependency-health provider rows are present.*"
    }

    It "fails when an externally gated live-provider row omits its gate" {
        $manifestPath = Join-Path $script:tempRoot "provider-integration-support.json"
        $manifestContents = Get-Content -LiteralPath (Join-Path $script:repoRoot "scripts\provider-integration-support.json") -Raw -Encoding UTF8
        $manifestContents = [regex]::Replace(
            $manifestContents,
            '(?s)("id": "redis-data-event-sourcing-live".*?)\r?\n\s+"externalServiceGate": "provider-integration",',
            '$1',
            1)

        $redisRow = ($manifestContents | ConvertFrom-Json -Depth 32).providerRows |
            Where-Object { $_.id -eq "redis-data-event-sourcing-live" } |
            Select-Object -First 1
        $redisRow.PSObject.Properties.Name | Should -Not -Contain "externalServiceGate"
        Set-Content -LiteralPath $manifestPath -Value $manifestContents -Encoding UTF8

        {
            Convert-ProviderIntegrationEvidence -ResolvedManifestPath $manifestPath -ResolvedRepoRoot $script:repoRoot
        } | Should -Throw "*Provider integration live-proof row 'redis-data-event-sourcing-live' must declare externalServiceGate unless defaultRunBehavior is runs-without-external-services.*"
    }

    It "fails when dependency-health provider rows drift from the source-derived manifest" {
        $manifestPath = Join-Path $script:tempRoot "provider-integration-support.json"
        $manifestContents = Get-Content -LiteralPath (Join-Path $script:repoRoot "scripts\provider-integration-support.json") -Raw -Encoding UTF8
        $driftRegex = [regex]::new('("id": "sqlserver-dependency-health-live",\s+"provider": )"SQL Server"')
        $manifestContents = $driftRegex.Replace($manifestContents, '$1"SQL Server Drift"', 1)

        $manifestContents | Should -Match "SQL Server Drift"
        Set-Content -LiteralPath $manifestPath -Value $manifestContents -Encoding UTF8

        {
            Convert-ProviderIntegrationEvidence -ResolvedManifestPath $manifestPath -ResolvedRepoRoot $script:repoRoot
        } | Should -Throw "*Provider integration dependency-health row 'sqlserver-dependency-health-live' provider must match source-derived manifest value 'SQL Server' but found 'SQL Server Drift'.*"
    }

    It "fails when a test coverage roadmap layered project reference is missing" {
        $fixtureRoot = Join-Path $script:tempRoot "test-coverage-fixture"
        $docsRoot = Join-Path $fixtureRoot "docs"
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        foreach ($directory in @(
            "tests\Cephalon.Tests.Composition",
            "tests\Cephalon.Tests.Hosting",
            "tests\Cephalon.Tests.Tooling",
            "tests\Cephalon.Tests.CdcIntegration",
            "tests\Cephalon.Tests.Scripts",
            "tests\Cephalon.Tests.Support",
            "benchmarks\Cephalon.Benchmarks"
        )) {
            New-Item -ItemType Directory -Path (Join-Path $fixtureRoot $directory) -Force | Out-Null
        }

        $roadmapPath = Join-Path $docsRoot "test-coverage-roadmap.md"
        $contents = Get-Content -LiteralPath (Join-Path $script:repoRoot "docs\test-coverage-roadmap.md") -Raw -Encoding UTF8
        $contents = $contents.Replace("../tests/Cephalon.Tests.ProviderIntegration", "../tests/Cephalon.Tests.ProviderIntegration.Missing")
        $contents | Should -Match "ProviderIntegration\.Missing"
        Set-Content -LiteralPath $roadmapPath -Value $contents -Encoding UTF8

        {
            Convert-TestCoverageEvidence -ResolvedRoadmapPath $roadmapPath -ResolvedRepoRoot $fixtureRoot
        } | Should -Throw "*Test coverage roadmap layered project reference '../tests/Cephalon.Tests.ProviderIntegration.Missing'*"
    }

    It "keeps the architecture-review scorecard checkpoint aligned with the current scorecard contract" {
        $emitter = Get-Content -LiteralPath $script:scriptPath -Raw -Encoding UTF8
        $schemaMatch = [regex]::Match($emitter, '\$Script:SchemaVersion\s*=\s*"(?<version>[^"]+)"')
        $schemaMatch.Success | Should -BeTrue

        $currentSchemaVersion = $schemaMatch.Groups['version'].Value
        $followups = Get-Content -LiteralPath (Join-Path $script:repoRoot "docs\architecture-review-2026-05-followups.md") -Raw -Encoding UTF8
        $checkpointMatch = [regex]::Match($followups, '(?ms)^> Current scorecard checkpoint.*?(?=\r?\n\r?\n>)')
        $checkpointMatch.Success | Should -BeTrue

        $checkpoint = $checkpointMatch.Value
        $checkpoint | Should -Match ([regex]::Escape("schema ``$currentSchemaVersion"))
        $checkpoint | Should -Match "ENG-673"
        $checkpoint | Should -Match "ENG-674"
        $checkpoint | Should -Match "ENG-675"
        $checkpoint | Should -Match "ENG-679"
        $checkpoint | Should -Match "Eventing operational-superiority"
        $checkpoint | Should -Match "runtime concordance ``matched``"
        $checkpoint | Should -Match "wolverineRequired=false"
        $checkpoint | Should -Match "19 of 19 tokens"
        $checkpoint | Should -Match "SupplyChainEvidence\.SignedReleaseDryRun"
        $checkpoint | Should -Match "dispatch-identity-actions-disabled"
        $checkpoint | Should -Match "``104`` baselines / ``0`` pending packages / ``0`` additions / ``0`` removals"
        $checkpoint | Should -Match "cephalon doctor --scorecard"
        $checkpoint | Should -Not -Match "schema ``1\.20\.0``"
    }

    It "keeps release validation wired to the scorecard artifact" {
        $releaseValidation = Get-Content -LiteralPath (Join-Path $script:repoRoot "scripts\validate-release.ps1") -Raw -Encoding UTF8

        $releaseValidation | Should -Match '\[switch\]\$SkipEngineCompletionScorecard'
        $releaseValidation | Should -Match "publish-engine-completion-scorecard\.ps1"
        $releaseValidation | Should -Match "engine-completion-scorecard-release"
        $releaseValidation | Should -Match "Publish engine completion scorecard artifact"
        $releaseValidation | Should -Match "Write-EngineCompletionScorecardEvidenceSummary"
        $releaseValidation | Should -Match "DeploymentModeEvidence"
        $releaseValidation | Should -Match "Deployment-mode evidence"
        $releaseValidation | Should -Match "ProviderIntegrationEvidence"
        $releaseValidation | Should -Match "DependencyHealthProviderManifest"
        $releaseValidation | Should -Match "ProviderIntegrationEvidence\.DependencyHealthProviderManifest"
        $releaseValidation | Should -Match "Provider integration evidence"
        $releaseValidation | Should -Match "dependency-health providers"
        $releaseValidation | Should -Match "EventingOperationalSuperiorityEvidence"
        $releaseValidation | Should -Match "Eventing operational-superiority evidence"
        $releaseValidation | Should -Match "RuntimeConcordanceStatus"
        $releaseValidation | Should -Match "runtime concordance"
        $releaseValidation | Should -Match "Wolverine required"
        $releaseValidation | Should -Match "TestCoverageEvidence"
        $releaseValidation | Should -Match "Test coverage evidence"
        $releaseValidation | Should -Match "OpenQuarantineEntryCount"
        $releaseValidation | Should -Match "SrePostureEvidence"
        $releaseValidation | Should -Match "guardrail-mapped"
        $releaseValidation | Should -Match "SupplyChainEvidence"
        $releaseValidation | Should -Match "Supply-chain release evidence"
        $releaseValidation | Should -Match "external policy preflight checks"
        $releaseValidation | Should -Match "SupplyChainEvidence\.ExternalPolicyPreflight"
        $releaseValidation | Should -Match "SupplyChainEvidence\.SignedReleaseDryRun"
        $releaseValidation | Should -Match "Signed-release dry-run evidence"
        $releaseValidation | Should -Match "PublicApiCompatibilityEvidence"
        $releaseValidation | Should -Match "Public API compatibility"
        $releaseValidation | Should -Match "Assert-EngineCompletionScorecardHardBlockers"
        $releaseValidation | Should -Match "BlockedPlatformGates"
        $releaseValidation | Should -Match "SupplyChainEvidence.*BlockedCount"
        $releaseValidation | Should -Match "PublicApiCompatibilityEvidence.*RemovalEntryCount"
        $releaseValidation | Should -Match "hard release blocker"
        $releaseValidation | Should -Match "summarise-public-api-deltas\.ps1"
        $releaseValidation | Should -Match "public-api-delta-release"
        $releaseValidation | Should -Match "public-api-delta\.json"
        $releaseValidation | Should -Match "JsonOutputPath"
        $releaseValidation | Should -Match "FailOnRemovals"
    }
}
