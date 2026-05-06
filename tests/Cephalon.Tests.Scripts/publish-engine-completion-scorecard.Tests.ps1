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

        $json.'$schemaVersion' | Should -Be "1.2.0"
        $json.SourceDocument | Should -Be "docs/engine-completion-scorecard.md"
        $json.ConformanceMatrix | Should -Be "docs/conformance-matrix.md"
        $json.AdoptionSmokeManifest | Should -Be "scripts/adoption-smoke-support.json"
        $json.StatusVocabulary.Count | Should -Be 6
        $json.EvidenceSources.Count | Should -Be 11
        $json.EvidenceSourceReferences.Count | Should -Be 15
        $json.PlatformGates.Count | Should -Be 12
        $json.QualityDimensions.Count | Should -Be 12
        $json.PackageFamilies.Count | Should -Be 9
        $json.PackageGAReadiness.Count | Should -Be 90
        $json.Summary.PlatformGateCount | Should -Be 12
        $json.Summary.QualityDimensionCount | Should -Be 12
        $json.Summary.PackageFamilyCount | Should -Be 9
        $json.Summary.PackageGAReadinessCount | Should -Be 90
        $json.Summary.AdoptionSmokeScenarioCount | Should -Be 1
        $json.Summary.AdoptionSmokeRuntimeProbeCount | Should -Be 6
        $json.Summary.AdoptionSmokeAssertionCount | Should -Be 7
        $json.Summary.EvidenceSourceReferenceCount | Should -Be 15
        $json.Summary.PlatformStatusCounts.'ready-for-preview' | Should -Be 3
        $json.Summary.PlatformStatusCounts.partial | Should -Be 7
        $json.Summary.PlatformStatusCounts.'not-claimed' | Should -Be 1
        $json.Summary.PlatformStatusCounts.'needs-refresh' | Should -Be 1
        $json.Summary.PackageStatusCounts.'ready-for-preview' | Should -Be 4
        $json.Summary.PackageStatusCounts.partial | Should -Be 5
        $json.Summary.PackageGAStatusCounts.partial | Should -Be 88
        $json.Summary.PackageGAStatusCounts.'not-claimed' | Should -Be 2
        $json.Summary.PackageGAStatusCounts.'needs-refresh' | Should -Be 0

        $json.EvidenceSourceReferences.Reference | Should -Contain "docs/engine-surface-maturity-audit.md"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/deployment-mode-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/adoption-smoke-support.json"
        $json.EvidenceSourceReferences.Reference | Should -Contain "scripts/validate-out-of-tree-package-adoption.ps1"

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

        $corePackage = $json.PackageGAReadiness | Where-Object { $_.Package -eq "Cephalon.Abstractions" }
        $corePackage.Family | Should -Be "Core runtime"
        $corePackage.Maturity | Should -Be "M4"
        $corePackage.GAGateStatus | Should -Be "partial"
        $corePackage.GABlockerClass | Should -Be "release-evidence"

        $notClaimedPackages = @($json.PackageGAReadiness | Where-Object { $_.GAGateStatus -eq "not-claimed" })
        $notClaimedPackages.Package | Should -Contain "Cephalon.Data.MySql.SciSharpReplication"
        @($notClaimedPackages.Package | Where-Object { $_ -match "Cephalon\.Observability\.\*Dependencies" }).Count | Should -Be 1
        $notClaimedPackages.GABlockerClass | Select-Object -Unique | Should -Be "runtime-support-not-claimed"

        $markdown = Get-Content -LiteralPath $result.Paths.MarkdownPath -Raw -Encoding UTF8
        $markdown | Should -Match "Engine Completion Scorecard Report"
        $markdown | Should -Match "Platform gates: 12"
        $markdown | Should -Match "Package families: 9"
        $markdown | Should -Match "Adoption Smoke Evidence"
        $markdown | Should -Match "out-of-tree-generated-app-package-stage"
        $markdown | Should -Match "Evidence Source References"
        $markdown | Should -Match "Package GA Readiness"
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
        New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $scriptsRoot -Force | Out-Null

        Copy-Item -LiteralPath (Join-Path $script:repoRoot "docs\conformance-matrix.md") -Destination (Join-Path $docsRoot "conformance-matrix.md")
        Copy-Item -LiteralPath (Join-Path $script:repoRoot "scripts\deployment-mode-support.json") -Destination (Join-Path $scriptsRoot "deployment-mode-support.json")

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
    }
}
