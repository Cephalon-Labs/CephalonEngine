#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/validate-supply-chain-external-policy-preflight.ps1.
.DESCRIPTION
    Verifies the publish-workflow preflight that fail-closes real tag pushes until
    NuGet external policy confirmations and the publishing secret are present.
#>

BeforeAll {
    $env:CEPHALON_SUPPLY_CHAIN_EXTERNAL_POLICY_PREFLIGHT_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\validate-supply-chain-external-policy-preflight.ps1"
    . $script:scriptPath
}

AfterAll {
    Remove-Item Env:\CEPHALON_SUPPLY_CHAIN_EXTERNAL_POLICY_PREFLIGHT_NO_RUN -ErrorAction SilentlyContinue
}

Describe "validate-supply-chain-external-policy-preflight.ps1" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-supply-chain-preflight-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    It "writes a pending report without failing when external inputs are absent" {
        $result = Invoke-SupplyChainExternalPolicyPreflight `
            -OutputPath $script:tempRoot `
            -NuGetUser "" `
            -TrustedPublishingPolicyConfirmed "" `
            -PrefixReservationConfirmed ""

        Test-Path -LiteralPath $result.JsonPath -PathType Leaf | Should -BeTrue
        $result.Report.Status | Should -Be "pending"
        $result.Report.CheckCount | Should -Be 3
        $result.Report.PendingCheckCount | Should -Be 3
        $result.Report.VerifiedCheckCount | Should -Be 0
        $result.Report.Checks.EvidenceItemId | Should -Contain "nuget-trusted-publishing-policy"
        $result.Report.Checks.EvidenceItemId | Should -Contain "nuget-prefix-reservation"
        $result.Report.Checks.EvidenceItemId | Should -Contain "nuget-user-secret"
    }

    It "fails closed when RequireAll is set and external inputs are absent" {
        {
            Invoke-SupplyChainExternalPolicyPreflight `
                -OutputPath $script:tempRoot `
                -RequireAll `
                -NuGetUser "" `
                -TrustedPublishingPolicyConfirmed "" `
                -PrefixReservationConfirmed ""
        } | Should -Throw "*pending checks: nuget-trusted-publishing-policy, nuget-prefix-reservation, nuget-user-secret*"

        $jsonPath = Join-Path $script:tempRoot "external-policy-preflight.json"
        Test-Path -LiteralPath $jsonPath -PathType Leaf | Should -BeTrue
        $report = Get-Content -LiteralPath $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 12
        $report.Status | Should -Be "failed"
        $report.PendingCheckCount | Should -Be 3
    }

    It "passes when the NuGet user and release-manager confirmations are present" {
        $result = Invoke-SupplyChainExternalPolicyPreflight `
            -OutputPath $script:tempRoot `
            -RequireAll `
            -NuGetUser "Cephalon-Labs" `
            -TrustedPublishingPolicyConfirmed "true" `
            -PrefixReservationConfirmed "confirmed"

        $result.Report.Status | Should -Be "passed"
        $result.Report.PendingCheckCount | Should -Be 0
        $result.Report.VerifiedCheckCount | Should -Be 3
        $result.Report.Checks.Status | Should -Not -Contain "pending"
    }

    It "accepts explicit confirmation values used by protected release variables" {
        Convert-ToBooleanConfirmation -Value "true" | Should -BeTrue
        Convert-ToBooleanConfirmation -Value "confirmed" | Should -BeTrue
        Convert-ToBooleanConfirmation -Value "yes" | Should -BeTrue
        Convert-ToBooleanConfirmation -Value "false" | Should -BeFalse
        Convert-ToBooleanConfirmation -Value "" | Should -BeFalse
    }
}
