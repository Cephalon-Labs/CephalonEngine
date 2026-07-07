#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/summarise-public-api-deltas.ps1.

.DESCRIPTION
    Verifies that the public API delta summary remains useful as a release-notes
    artifact while release validation can fail closed when a pending delta contains
    removal entries.
#>

BeforeAll {
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\summarise-public-api-deltas.ps1"
    $script:powerShellHostPath = (Get-Process -Id $PID).Path

    function New-PublicApiFixturePackage {
        param(
            [Parameter(Mandatory = $true)]
            [string]$FixtureRoot,
            [Parameter(Mandatory = $true)]
            [string]$PackageId,
            [Parameter(Mandatory = $true)]
            [string[]]$UnshippedLines
        )

        $packageRoot = Join-Path $FixtureRoot "src\$PackageId"
        New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $packageRoot "PublicAPI.Shipped.txt") -Value "#nullable enable" -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $packageRoot "PublicAPI.Unshipped.txt") -Value $UnshippedLines -Encoding UTF8
    }
}

Describe "summarise-public-api-deltas.ps1" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-public-api-delta-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    It "writes markdown and JSON removal counts without failing when the release gate is not requested" {
        New-PublicApiFixturePackage `
            -FixtureRoot $script:tempRoot `
            -PackageId "Cephalon.Fixture" `
            -UnshippedLines @(
                "#nullable enable",
                "Cephalon.Fixture.AddedApi",
                "*REMOVED* Cephalon.Fixture.OldApi"
            )

        $outputPath = Join-Path $script:tempRoot "artifacts\public-api-delta.md"
        $jsonOutputPath = Join-Path $script:tempRoot "artifacts\public-api-delta.json"
        $output = & $script:powerShellHostPath -NoLogo -NoProfile -File $script:scriptPath -RepoRoot $script:tempRoot -OutputPath $outputPath -JsonOutputPath $jsonOutputPath 2>&1

        $LASTEXITCODE | Should -Be 0
        ($output | Out-String) | Should -Match "Total removal entries: 1"
        ($output | Out-String) | Should -Match "Wrote public-API delta JSON"

        $report = Get-Content -LiteralPath $outputPath -Raw -Encoding UTF8
        $report | Should -Match "Total additive entries: \*\*1\*\*"
        $report | Should -Match "Total removal entries: \*\*1\*\*"
        $report | Should -Match "\*REMOVED\* Cephalon\.Fixture\.OldApi"

        $json = Get-Content -LiteralPath $jsonOutputPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
        $json.'$schemaVersion' | Should -Be "1.0.0"
        $json.UnshippedPackageCount | Should -Be 1
        $json.PendingPackageCount | Should -Be 1
        $json.HeaderOnlyPackageCount | Should -Be 0
        $json.AdditiveEntryCount | Should -Be 1
        $json.RemovalEntryCount | Should -Be 1
        $json.HasRemovalEntries | Should -BeTrue
        $json.RemovalGateWouldFail | Should -BeFalse
        $json.PackageDeltas.Count | Should -Be 1
        $json.PackageDeltas[0].PackageId | Should -Be "Cephalon.Fixture"
        $json.PackageDeltas[0].UnshippedPath.Replace('\', '/') | Should -Be "src/Cephalon.Fixture/PublicAPI.Unshipped.txt"
        $json.PackageDeltas[0].AdditiveEntryCount | Should -Be 1
        $json.PackageDeltas[0].RemovalEntryCount | Should -Be 1
        @($json.PackageDeltas[0].Additions).Count | Should -Be 1
        @($json.PackageDeltas[0].Removals).Count | Should -Be 1
    }

    It "writes the report then fails when removal entries are gated" {
        New-PublicApiFixturePackage `
            -FixtureRoot $script:tempRoot `
            -PackageId "Cephalon.Fixture" `
            -UnshippedLines @(
                "#nullable enable",
                "*REMOVED* Cephalon.Fixture.OldApi"
            )

        $outputPath = Join-Path $script:tempRoot "artifacts\public-api-delta.md"
        $jsonOutputPath = Join-Path $script:tempRoot "artifacts\public-api-delta.json"
        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = $script:powerShellHostPath
        foreach ($argument in @(
                "-NoLogo",
                "-NoProfile",
                "-File",
                $script:scriptPath,
                "-RepoRoot",
                $script:tempRoot,
                "-OutputPath",
                $outputPath,
                "-JsonOutputPath",
                $jsonOutputPath,
                "-FailOnRemovals")) {
            $startInfo.ArgumentList.Add($argument)
        }

        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.UseShellExecute = $false
        $process = [System.Diagnostics.Process]::Start($startInfo)
        $standardOutput = $process.StandardOutput.ReadToEnd()
        $standardError = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        $output = $standardOutput + [Environment]::NewLine + $standardError

        $process.ExitCode | Should -Not -Be 0
        Test-Path -LiteralPath $outputPath -PathType Leaf | Should -BeTrue
        Test-Path -LiteralPath $jsonOutputPath -PathType Leaf | Should -BeTrue

        $report = Get-Content -LiteralPath $outputPath -Raw -Encoding UTF8
        $report | Should -Match "Total removal entries: \*\*1\*\*"
        $json = Get-Content -LiteralPath $jsonOutputPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
        $json.RemovalGateWouldFail | Should -BeTrue
        ($output | Out-String) | Should -Match "Public API removal entries detected: 1 removal\(s\) across 1 package\(s\): Cephalon\.Fixture"
    }

    It "passes the removal gate when only additive entries are pending" {
        New-PublicApiFixturePackage `
            -FixtureRoot $script:tempRoot `
            -PackageId "Cephalon.Fixture" `
            -UnshippedLines @(
                "#nullable enable",
                "Cephalon.Fixture.AddedApi"
            )

        $output = & $script:powerShellHostPath -NoLogo -NoProfile -File $script:scriptPath -RepoRoot $script:tempRoot -FailOnRemovals 2>&1

        $LASTEXITCODE | Should -Be 0
        ($output | Out-String) | Should -Match "Total additive entries: \*\*1\*\*"
        ($output | Out-String) | Should -Match "Total removal entries: \*\*0\*\*"
    }

    It "keeps release validation wired to the removal gate" {
        $releaseValidation = Get-Content -LiteralPath (Join-Path $script:repoRoot "scripts\validate-release.ps1") -Raw -Encoding UTF8

        $releaseValidation | Should -Match "PublicApiCompatibilityEvidence"
        $releaseValidation | Should -Match "Public API compatibility"
        $releaseValidation | Should -Match "summarise-public-api-deltas\.ps1"
        $releaseValidation | Should -Match "public-api-delta\.json"
        $releaseValidation | Should -Match "JsonOutputPath"
        $releaseValidation | Should -Match "FailOnRemovals"
    }
}
