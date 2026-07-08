BeforeAll {
    $script:repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
    $script:publisher = Join-Path $script:repoRoot "scripts\publish-surface-maturity-report.ps1"
    $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-surface-maturity-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
}

AfterAll {
    if (Test-Path -LiteralPath $script:tempRoot) {
        Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
    }
}

Describe "publish-surface-maturity-report" {
    It "publishes a drift-free report for the repository" {
        $outputPath = Join-Path $script:tempRoot "repository-report.json"

        & $script:publisher -RepositoryRoot $script:repoRoot -OutputPath $outputPath -FailOnDrift

        $report = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json -Depth 32
        $report.schemaVersion | Should -Be "1.0.0"
        $report.status | Should -Be "passed"
        $report.summary.sourceProjectCount | Should -Be 107
        $report.summary.componentDocumentCount | Should -Be 107
        $report.summary.packageEvidenceCount | Should -Be 107
        $report.summary.exactConformanceRowCount | Should -Be 89
        $report.summary.providerManifestRowCount | Should -Be 18
        $report.summary.driftCount | Should -Be 0
        $report.summary.maturityCounts.M0 | Should -Be 1
        $report.summary.maturityCounts.M1 | Should -Be 39
        $report.summary.maturityCounts.M2 | Should -Be 51
        $report.summary.maturityCounts.M3 | Should -Be 7
        $report.summary.maturityCounts.M4 | Should -Be 9
    }

    It "fails closed when the component badge and conformance declaration disagree" {
        $fixtureRoot = Join-Path $script:tempRoot "drift-fixture"
        $sourceRoot = Join-Path $fixtureRoot "src\Cephalon.Test"
        $docsRoot = Join-Path $fixtureRoot "docs\components"
        $scriptsRoot = Join-Path $fixtureRoot "scripts"
        New-Item -ItemType Directory -Path $sourceRoot, $docsRoot, $scriptsRoot -Force | Out-Null

        Set-Content -LiteralPath (Join-Path $sourceRoot "Cephalon.Test.csproj") -Value '<Project Sdk="Microsoft.NET.Sdk" />'
        Set-Content -LiteralPath (Join-Path $docsRoot "test.md") -Value @'
# Cephalon.Test

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)
'@
        Set-Content -LiteralPath (Join-Path $docsRoot "README.md") -Value '[Cephalon.Test](test.md)'
        Set-Content -LiteralPath (Join-Path $fixtureRoot "docs\engine-surface-maturity-audit.md") -Value 'Surface maturity in this document reflects the repository state as of `June 30, 2026`.'
        Set-Content -LiteralPath (Join-Path $fixtureRoot "docs\engine-backlog.md") -Value 'Backlog status in this document reflects the repository state as of `June 30, 2026`.'
        Set-Content -LiteralPath (Join-Path $fixtureRoot "docs\conformance-matrix.md") -Value '| `Cephalon.Test` | M1 | cephalon-managed | — | — | — | fixture |'
        Set-Content -LiteralPath (Join-Path $scriptsRoot "observability-dependency-health-providers.json") -Value '{"schemaVersion":"1.0.0","providerCount":0,"providers":[]}'
        $outputPath = Join-Path $fixtureRoot "artifacts\report.json"

        {
            & $script:publisher -RepositoryRoot $fixtureRoot -OutputPath $outputPath -FailOnDrift
        } | Should -Throw "*maturity-mismatch*"

        $report = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json -Depth 16
        $report.status | Should -Be "drift-detected"
        $report.summary.driftCount | Should -Be 1
        $report.drift[0].code | Should -Be "maturity-mismatch"
    }

    It "keeps release validation and artifact publishing wired to the report" {
        $releaseValidation = Get-Content -LiteralPath (Join-Path $script:repoRoot "scripts\validate-release.ps1") -Raw
        $workflow = Get-Content -LiteralPath (Join-Path $script:repoRoot ".github\workflows\release-validation.yml") -Raw

        $releaseValidation | Should -Match '\[switch\]\$SkipSurfaceMaturityReport'
        $releaseValidation | Should -Match 'Publish surface maturity evidence artifact'
        $releaseValidation | Should -Match 'publish-surface-maturity-report\.ps1'
        $releaseValidation | Should -Match '-FailOnDrift'
        $workflow | Should -Match 'Upload surface maturity evidence'
        $workflow | Should -Match 'artifacts/surface-maturity-report-release'
        $workflow | Should -Match 'if-no-files-found: error'
    }
}
