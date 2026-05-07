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

    It "emits JSON and Markdown artifacts from the current scorecard" {
        $outputPath = Join-Path $script:tempRoot "artifacts"

        $result = Invoke-EngineCompletionScorecardPublish `
            -ScorecardPath "docs/engine-completion-scorecard.md" `
            -ConformanceMatrixPath "docs/conformance-matrix.md" `
            -OutputPath $outputPath `
            -RepoRoot $script:repoRoot

        Test-Path -LiteralPath $result.Paths.JsonPath -PathType Leaf | Should -BeTrue
        Test-Path -LiteralPath $result.Paths.MarkdownPath -PathType Leaf | Should -BeTrue

        $json = Get-Content -LiteralPath $result.Paths.JsonPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16

        $json.'$schemaVersion' | Should -Be "1.8.0"
        $json.SourceDocument | Should -Be "docs/engine-completion-scorecard.md"
        $json.ConformanceMatrix | Should -Be "docs/conformance-matrix.md"
        $json.DeploymentModeManifest | Should -Be "scripts/deployment-mode-support.json"
        $json.AdoptionSmokeManifest | Should -Be "scripts/adoption-smoke-support.json"
        $json.ProviderIntegrationManifest | Should -Be "scripts/provider-integration-support.json"
        $json.SrePostureManifest | Should -Be "scripts/sre-posture-support.json"
        $json.SupplyChainManifest | Should -Be "scripts/supply-chain-release-support.json"
        $json.PublicApiDeltaScript | Should -Be "scripts/summarise-public-api-deltas.ps1"
        $json.StatusVocabulary.Count | Should -Be 6
        $json.EvidenceSources.Count | Should -Be 13
        $json.EvidenceSourceReferences.Count | Should -Be 21
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
        $json.Summary.DeploymentModePackageEntryCount | Should -Be 6
        $json.Summary.DeploymentModePackageScopedClaimPackageCount | Should -Be 1
        $json.Summary.DeploymentModePackageScopedClaimCount | Should -Be 1
        $json.Summary.DeploymentModeKnownHazardPackageCount | Should -Be 2
        $json.Summary.DeploymentModeKnownHazardEntryCount | Should -Be 14
        $json.Summary.DeploymentModeTransitiveAuditEntryCount | Should -Be 7
        $json.Summary.DeploymentModeRepresentativePublishTargetCount | Should -Be 5
        $json.Summary.AdoptionSmokeScenarioCount | Should -Be 1
        $json.Summary.AdoptionSmokeRuntimeProbeCount | Should -Be 6
        $json.Summary.AdoptionSmokeAssertionCount | Should -Be 7
        $json.Summary.ProviderIntegrationEvidenceRowCount | Should -Be 32
        $json.Summary.ProviderIntegrationLiveProofCount | Should -Be 6
        $json.Summary.ProviderIntegrationCompositionOnlyCount | Should -Be 26
        $json.Summary.ProviderIntegrationExternalServiceGateCount | Should -Be 6
        $json.Summary.ProviderIntegrationDefaultSkippedCount | Should -Be 6
        $json.Summary.ProviderIntegrationRuntimeContractCount | Should -Be 61
        $json.Summary.SreSliCount | Should -Be 11
        $json.Summary.SreTargetDeclaredCount | Should -Be 11
        $json.Summary.SrePendingStableBaselineCount | Should -Be 11
        $json.Summary.SreStableBaselineCount | Should -Be 0
        $json.Summary.SreGuardrailMappedSliCount | Should -Be 3
        $json.Summary.SreGuardrailPendingSliCount | Should -Be 3
        $json.Summary.SreGuardrailNotApplicableSliCount | Should -Be 5
        $json.Summary.SreGuardrailReferenceCount | Should -Be 3
        $json.Summary.SupplyChainEvidenceItemCount | Should -Be 10
        $json.Summary.SupplyChainWorkflowReadyCount | Should -Be 7
        $json.Summary.SupplyChainExternalPolicyPendingCount | Should -Be 3
        $json.Summary.SupplyChainBlockedCount | Should -Be 0
        $json.Summary.PublicApiPackageCount | Should -Be 104
        $json.Summary.PublicApiPendingPackageCount | Should -Be 22
        $json.Summary.PublicApiAdditiveEntryCount | Should -Be 293
        $json.Summary.PublicApiRemovalEntryCount | Should -Be 0
        $json.Summary.EvidenceSourceCount | Should -Be 13
        $json.Summary.EvidenceSourceReferenceCount | Should -Be 21
        $json.Summary.PlatformStatusCounts.'ready-for-preview' | Should -Be 3
        $json.Summary.PlatformStatusCounts.partial | Should -Be 7
        $json.Summary.PlatformStatusCounts.'not-claimed' | Should -Be 1
        $json.Summary.PlatformStatusCounts.'needs-refresh' | Should -Be 1
        $json.Summary.PackageStatusCounts.'ready-for-preview' | Should -Be 4
        $json.Summary.PackageStatusCounts.partial | Should -Be 5
        $json.Summary.PackageGAStatusCounts.partial | Should -Be 89
        $json.Summary.PackageGAStatusCounts.'not-claimed' | Should -Be 1
        $json.Summary.PackageGAStatusCounts.'needs-refresh' | Should -Be 0

        $json.EvidenceSourceReferences.Reference | Should -Contain "docs/engine-surface-maturity-audit.md"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/deployment-mode-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/adoption-smoke-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/provider-integration-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/observability-dependency-health-providers.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/sre-posture-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/supply-chain-release-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain ".github/workflows/publish-release.yml"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/validate-out-of-tree-package-adoption.ps1"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/summarise-public-api-deltas.ps1"

        $json.DeploymentModeEvidence.ManifestSchemaVersion | Should -Be "1.4.0"
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
        $json.DeploymentModeEvidence.PackageEntryCount | Should -Be 6
        $json.DeploymentModeEvidence.PackageScopedClaimPackageCount | Should -Be 1
        $json.DeploymentModeEvidence.PackageScopedClaimCount | Should -Be 1
        $json.DeploymentModeEvidence.KnownHazardPackageCount | Should -Be 2
        $json.DeploymentModeEvidence.KnownHazardEntryCount | Should -Be 14
        $json.DeploymentModeEvidence.ClaimAuditTierCounts.high | Should -Be 2
        $json.DeploymentModeEvidence.ClaimAuditTierCounts.'excluded-by-design' | Should -Be 3
        $json.DeploymentModeEvidence.ClaimAuditTierCounts.'clean-baseline' | Should -Be 1
        $json.DeploymentModeEvidence.TransitiveAuditEntryCount | Should -Be 7
        $json.DeploymentModeEvidence.TransitiveAuditLockFileGlobCount | Should -Be 3
        $json.DeploymentModeEvidence.RepresentativePublishTargetCount | Should -Be 5
        $json.DeploymentModeEvidence.PublishProbeReleaseValidationMode | Should -Be "audit-only"
        $json.DeploymentModeEvidence.PublishProbeReleaseValidationSkipsPublish | Should -BeTrue
        $json.DeploymentModeEvidence.PublishProbeNonOptOutGate | Should -BeFalse
        $json.DeploymentModeEvidence.PackageRows.PackageName | Should -Contain "Cephalon.Diagnostics"
        $json.DeploymentModeEvidence.PackageRows.PackageName | Should -Contain "Cephalon.Data.MySql.SciSharpReplication"
        $json.DeploymentModeEvidence.TransitiveAuditRows.PackagePattern | Should -Contain "Newtonsoft.Json"

        $json.AdoptionSmokeEvidence.ManifestSchemaVersion | Should -Be "1.0.0"
        $json.AdoptionSmokeEvidence.ScenarioId | Should -Be "out-of-tree-generated-app-package-stage"
        $json.AdoptionSmokeEvidence.Status | Should -Be "replay-available"
        $json.AdoptionSmokeEvidence.ValidationScript | Should -Be "scripts/validate-out-of-tree-package-adoption.ps1"
        $json.AdoptionSmokeEvidence.ReferenceModuleProject | Should -Be "samples/Cephalon.ReferenceModule.Operations/Cephalon.ReferenceModule.Operations.csproj"
        $json.AdoptionSmokeEvidence.SupportingScripts | Should -Contain "scripts/validate-generated-app-adoption.ps1"
        $json.AdoptionSmokeEvidence.SourceDocuments | Should -Contain "docs/package-publishing.md"
        $json.AdoptionSmokeEvidence.RuntimeProbes.Path | Should -Contain "/engine/packages"
        $json.AdoptionSmokeEvidence.RuntimeProbes.Path | Should -Contain "/engine/trust-policy"
        $json.AdoptionSmokeEvidence.RuntimeProbes.Path | Should -Contain "/api/operations/status"

        $json.ProviderIntegrationEvidence.ManifestSchemaVersion | Should -Be "1.0.0"
        $json.ProviderIntegrationEvidence.Status | Should -Be "partial-live-provider-evidence"
        $json.ProviderIntegrationEvidence.SourceDocuments | Should -Contain "docs/components/observability.md"
        $json.ProviderIntegrationEvidence.SourceDocuments | Should -Contain "scripts/observability-dependency-health-providers.json"
        $json.ProviderIntegrationEvidence.ValidationProjects | Should -Contain "tests/Cephalon.Tests.Hosting/Cephalon.Tests.Hosting.csproj"
        $json.ProviderIntegrationEvidence.EvidenceRowCount | Should -Be 32
        $json.ProviderIntegrationEvidence.LiveProofCount | Should -Be 6
        $json.ProviderIntegrationEvidence.CompositionOnlyCount | Should -Be 26
        $json.ProviderIntegrationEvidence.ExternalServiceGateCount | Should -Be 6
        $json.ProviderIntegrationEvidence.DefaultSkippedCount | Should -Be 6
        $json.ProviderIntegrationEvidence.RuntimeContractCount | Should -Be 61
        $json.ProviderIntegrationEvidence.EnvironmentVariableCount | Should -Be 11
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "redis-data-event-sourcing-live"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "postgres-cdc-live"
        $json.ProviderIntegrationEvidence.ProviderRows.Id | Should -Contain "sqlserver-dependency-health-invariant"
        $json.ProviderIntegrationEvidence.ProviderRows.ExternalServiceGate | Should -Contain "provider-integration"
        $json.ProviderIntegrationEvidence.ProviderRows.ExternalServiceGate | Should -Contain "cdc-integration"
        $json.ProviderIntegrationEvidence.ProviderRows.RuntimeContracts | Should -Contain "dependency-health.sqlserver"
        $json.ProviderIntegrationEvidence.ProviderRows.TestFiles | Should -Contain "tests/Cephalon.Tests.Hosting/ObservabilityDependencyHealthProviderInvariantTests.cs"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_PROVIDER_REDIS_CONNECTION_STRING"
        $json.ProviderIntegrationEvidence.EnvironmentVariables | Should -Contain "CEPHALON_CDC_POSTGRES_CONNECTION_STRING"

        $json.SrePostureEvidence.ManifestSchemaVersion | Should -Be "1.1.0"
        $json.SrePostureEvidence.Status | Should -Be "target-declared"
        $json.SrePostureEvidence.ReleaseValidationSummaryMode | Should -Be "release-validation-console-and-scorecard-artifact"
        $json.SrePostureEvidence.StableBaselinesPublished | Should -BeFalse
        $json.SrePostureEvidence.GuardrailCatalog | Should -Be "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json"
        $json.SrePostureEvidence.GuardrailCatalogEntryCount | Should -Be 29
        $json.SrePostureEvidence.SliCount | Should -Be 11
        $json.SrePostureEvidence.TargetDeclaredCount | Should -Be 11
        $json.SrePostureEvidence.PendingStableBaselineCount | Should -Be 11
        $json.SrePostureEvidence.StableBaselineCount | Should -Be 0
        $json.SrePostureEvidence.GuardrailMappedSliCount | Should -Be 3
        $json.SrePostureEvidence.GuardrailPendingSliCount | Should -Be 3
        $json.SrePostureEvidence.GuardrailNotApplicableSliCount | Should -Be 5
        $json.SrePostureEvidence.GuardrailReferenceCount | Should -Be 3
        $json.SrePostureEvidence.SourceDocuments | Should -Contain "docs/sre-posture.md"
        $json.SrePostureEvidence.SourceDocuments | Should -Contain "docs/benchmarking.md"
        $json.SrePostureEvidence.ValidationScripts | Should -Contain "scripts/validate-release.ps1"
        $json.SrePostureEvidence.SliRows.Id | Should -Contain "engine.behavior.dispatch.latency.p95"
        $json.SrePostureEvidence.SliRows.BaselineStatus | Select-Object -Unique | Should -Be "pending-stable-baseline"
        $json.SrePostureEvidence.SliRows.GuardrailCoverageStatus | Should -Contain "guardrail-catalog-mapped"
        $json.SrePostureEvidence.SliRows.GuardrailCoverageStatus | Should -Contain "pending-stable-baseline"
        $json.SrePostureEvidence.SliRows.GuardrailCoverageStatus | Should -Contain "not-applicable"
        $behaviorLatencySli = $json.SrePostureEvidence.SliRows | Where-Object { $_.Id -eq "engine.behavior.dispatch.latency.p95" }
        $behaviorLatencySli.GuardrailReferences.ReportFileName | Should -Contain "Cephalon.Benchmarks.HotPath.BehaviorDispatchBenchmarks-report.csv"
        $behaviorLatencySli.GuardrailReferences.Benchmark | Should -Contain "DispatchBehavior"

        $json.SupplyChainEvidence.ManifestSchemaVersion | Should -Be "1.0.0"
        $json.SupplyChainEvidence.Status | Should -Be "workflow-ready-external-policy-pending"
        $json.SupplyChainEvidence.ReleaseWorkflow | Should -Be ".github/workflows/publish-release.yml"
        $json.SupplyChainEvidence.SourceDocuments | Should -Contain "docs/package-publishing.md"
        $json.SupplyChainEvidence.SourceDocuments | Should -Contain "docs/supply-chain-uplift-plan.md"
        $json.SupplyChainEvidence.ValidationScripts | Should -Contain "scripts/validate-release.ps1"
        $json.SupplyChainEvidence.RequiredWorkflowTokens | Should -Contain "actions/attest-build-provenance"
        $json.SupplyChainEvidence.RequiredWorkflowTokens | Should -Contain "NuGet/login"
        $json.SupplyChainEvidence.EvidenceItemCount | Should -Be 10
        $json.SupplyChainEvidence.WorkflowReadyCount | Should -Be 7
        $json.SupplyChainEvidence.ExternalPolicyPendingCount | Should -Be 3
        $json.SupplyChainEvidence.BlockedCount | Should -Be 0
        $json.SupplyChainEvidence.EvidenceItems.Id | Should -Contain "cyclonedx-sbom-per-package"
        $json.SupplyChainEvidence.EvidenceItems.Id | Should -Contain "nuget-trusted-publishing-policy"
        $json.SupplyChainEvidence.EvidenceItems.Status | Should -Contain "external-policy-pending"

        $json.PublicApiCompatibilityEvidence.DeltaScript | Should -Be "scripts/summarise-public-api-deltas.ps1"
        $json.PublicApiCompatibilityEvidence.PackageCount | Should -Be 104
        $json.PublicApiCompatibilityEvidence.PendingPackageCount | Should -Be 22
        $json.PublicApiCompatibilityEvidence.HeaderOnlyPackageCount | Should -Be 82
        $json.PublicApiCompatibilityEvidence.AdditiveEntryCount | Should -Be 293
        $json.PublicApiCompatibilityEvidence.RemovalEntryCount | Should -Be 0
        $json.PublicApiCompatibilityEvidence.HasRemovalEntries | Should -BeFalse
        $json.PublicApiCompatibilityEvidence.PackageDeltas.Count | Should -Be 104
        $abstractionsDelta = $json.PublicApiCompatibilityEvidence.PackageDeltas | Where-Object { $_.Package -eq "Cephalon.Abstractions" }
        $abstractionsDelta.Project | Should -Be "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj"
        $abstractionsDelta.Unshipped | Should -Be "src/Cephalon.Abstractions/PublicAPI.Unshipped.txt"
        $abstractionsDelta.Shipped | Should -Be "src/Cephalon.Abstractions/PublicAPI.Shipped.txt"
        $abstractionsDelta.AdditiveEntryCount | Should -Be 15
        $abstractionsDelta.RemovalEntryCount | Should -Be 0
        $abstractionsDelta.HasPendingChanges | Should -BeTrue

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
        $markdown | Should -Match "Deployment-mode known hazards: 14"
        $markdown | Should -Match "Publish-probe release validation mode: audit-only"
        $markdown | Should -Match "SRE Posture Evidence"
        $markdown | Should -Match "SRE SLIs: 11"
        $markdown | Should -Match "SRE pending stable baselines: 11"
        $markdown | Should -Match "SRE guardrail-mapped SLIs: 3"
        $markdown | Should -Match "Guardrail coverage"
        $markdown | Should -Match "Supply-Chain Release Evidence"
        $markdown | Should -Match "Supply-chain evidence items: 10"
        $markdown | Should -Match "external-policy-pending"
        $markdown | Should -Match "Public API Compatibility Evidence"
        $markdown | Should -Match "Public API packages with pending changes: 22"
        $markdown | Should -Match "Cephalon.Abstractions"
        $markdown | Should -Match "Adoption Smoke Evidence"
        $markdown | Should -Match "out-of-tree-generated-app-package-stage"
        $markdown | Should -Match "Provider Integration Evidence"
        $markdown | Should -Match "Provider integration evidence rows: 32"
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

    It "fails when adoption smoke runtime probes drift away from the replay script" {
        $fixtureRoot = Join-Path $script:tempRoot "adoption-fixture"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        $docsRoot = Join-Path $fixtureRoot "docs"
        $sampleRoot = Join-Path $fixtureRoot "samples\Cephalon.ReferenceModule.Operations"
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $sampleRoot -Force | Out-Null

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
/health/ready
/engine/packages
'@ -Encoding UTF8

        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-generated-app-adoption.ps1") -Value "# generated app replay" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $scriptsRoot "validate-template-pack-adoption.ps1") -Value "# template pack replay" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $sampleRoot "Cephalon.ReferenceModule.Operations.csproj") -Value "<Project />" -Encoding UTF8
        foreach ($docName in @("getting-started.md", "operations.md", "external-package-lifecycle.md", "engine-completion-scorecard.md", "release-checklist.md")) {
            Set-Content -LiteralPath (Join-Path $docsRoot $docName) -Value "# $docName" -Encoding UTF8
        }

        $manifestPath = Join-Path $scriptsRoot "adoption-smoke-support.json"
        @{
            '$schemaVersion' = "1.0.0"
            scenarioId = "fixture"
            status = "replay-available"
            validationScript = $replayScriptPath
            supportingScripts = @(
                (Join-Path $scriptsRoot "validate-generated-app-adoption.ps1"),
                (Join-Path $scriptsRoot "validate-template-pack-adoption.ps1")
            )
            sourceDocs = @(
                (Join-Path $docsRoot "getting-started.md"),
                (Join-Path $docsRoot "operations.md"),
                (Join-Path $docsRoot "external-package-lifecycle.md"),
                (Join-Path $docsRoot "engine-completion-scorecard.md"),
                (Join-Path $docsRoot "release-checklist.md")
            )
            referenceModuleProject = (Join-Path $sampleRoot "Cephalon.ReferenceModule.Operations.csproj")
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
            runtimeProbes = @(
                @{ kind = "package-runtime"; path = "/engine/packages" },
                @{ kind = "trust-policy"; path = "/engine/trust-policy" }
            )
        } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        {
            Invoke-EngineCompletionScorecardPublish `
                -ScorecardPath "docs/engine-completion-scorecard.md" `
                -ConformanceMatrixPath "docs/conformance-matrix.md" `
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
        $releaseValidation | Should -Match "Provider integration evidence"
        $releaseValidation | Should -Match "SrePostureEvidence"
        $releaseValidation | Should -Match "guardrail-mapped"
        $releaseValidation | Should -Match "SupplyChainEvidence"
        $releaseValidation | Should -Match "Supply-chain release evidence"
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
