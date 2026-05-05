#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/publish-engine-completion-scorecard.ps1.

.DESCRIPTION
    Verifies that the engine completion scorecard emitter reads the hand-authored
    scorecard document as a read model, emits stable JSON/Markdown artifacts, and
    fails loudly when scorecard statuses drift outside the supported vocabulary.
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
            -OutputPath $outputPath `
            -RepoRoot $script:repoRoot

        Test-Path -LiteralPath $result.Paths.JsonPath -PathType Leaf | Should -BeTrue
        Test-Path -LiteralPath $result.Paths.MarkdownPath -PathType Leaf | Should -BeTrue

        $json = Get-Content -LiteralPath $result.Paths.JsonPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16

        $json.'$schemaVersion' | Should -Be "1.0.0"
        $json.SourceDocument | Should -Be "docs/engine-completion-scorecard.md"
        $json.StatusVocabulary.Count | Should -Be 6
        $json.EvidenceSources.Count | Should -Be 10
        $json.PlatformGates.Count | Should -Be 12
        $json.QualityDimensions.Count | Should -Be 12
        $json.PackageFamilies.Count | Should -Be 9
        $json.Summary.PlatformGateCount | Should -Be 12
        $json.Summary.QualityDimensionCount | Should -Be 12
        $json.Summary.PackageFamilyCount | Should -Be 9
        $json.Summary.PlatformStatusCounts.'ready-for-preview' | Should -Be 3
        $json.Summary.PlatformStatusCounts.partial | Should -Be 7
        $json.Summary.PlatformStatusCounts.'not-claimed' | Should -Be 1
        $json.Summary.PlatformStatusCounts.'needs-refresh' | Should -Be 1
        $json.Summary.PackageStatusCounts.'ready-for-preview' | Should -Be 4
        $json.Summary.PackageStatusCounts.partial | Should -Be 5

        $markdown = Get-Content -LiteralPath $result.Paths.MarkdownPath -Raw -Encoding UTF8
        $markdown | Should -Match "Engine Completion Scorecard Report"
        $markdown | Should -Match "Platform gates: 12"
        $markdown | Should -Match "Package families: 9"
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
                -OutputPath (Join-Path $fixtureRoot "artifacts") `
                -RepoRoot $fixtureRoot
        } | Should -Throw "*Unsupported scorecard status vocabulary entry 'almost-ready'*"
    }

    It "keeps release validation wired to the scorecard artifact" {
        $releaseValidation = Get-Content -LiteralPath (Join-Path $script:repoRoot "scripts\validate-release.ps1") -Raw -Encoding UTF8

        $releaseValidation | Should -Match '\[switch\]\$SkipEngineCompletionScorecard'
        $releaseValidation | Should -Match "publish-engine-completion-scorecard\.ps1"
        $releaseValidation | Should -Match "engine-completion-scorecard-release"
        $releaseValidation | Should -Match "Publish engine completion scorecard artifact"
    }
}
