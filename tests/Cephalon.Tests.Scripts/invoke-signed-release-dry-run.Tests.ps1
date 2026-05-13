#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/invoke-signed-release-dry-run.ps1.
.DESCRIPTION
    Verifies signed-release workflow dry-run readiness reporting without
    dispatching a real GitHub Actions workflow during the test run.
#>

BeforeAll {
    $env:CEPHALON_SIGNED_RELEASE_DRY_RUN_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\invoke-signed-release-dry-run.ps1"
    . $script:scriptPath

    function New-FakeGhResult {
        param(
            [int]$ExitCode,
            [string]$Output
        )

        return [pscustomobject]@{
            ExitCode = $ExitCode
            Output   = $Output
        }
    }

    function New-WorkflowListJson {
        return @{
            workflows = @(
                @{
                    id = 269899503
                    name = "Publish Release"
                    path = ".github/workflows/publish-release.yml"
                    state = "active"
                }
            )
        } | ConvertTo-Json -Depth 8
    }

    function New-ActionsPermissionsJson {
        return @{
            enabled = $true
            allowed_actions = "all"
            sha_pinning_required = $false
        } | ConvertTo-Json -Depth 8
    }
}

AfterAll {
    Remove-Item Env:\CEPHALON_SIGNED_RELEASE_DRY_RUN_NO_RUN -ErrorAction SilentlyContinue
}

Describe "invoke-signed-release-dry-run.ps1" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-signed-release-dry-run-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    It "reports ready prerequisites without dispatch when SkipDispatch is set" {
        $invoker = {
            param([string[]]$Arguments)

            $command = $Arguments -join " "
            if ($command -eq "api repos/Cephalon-Labs/CephalonEngine/actions/workflows") {
                return New-FakeGhResult -ExitCode 0 -Output (New-WorkflowListJson)
            }

            if ($command -eq "api repos/Cephalon-Labs/CephalonEngine/actions/permissions") {
                return New-FakeGhResult -ExitCode 0 -Output (New-ActionsPermissionsJson)
            }

            if ($command -eq "api user --jq .login") {
                return New-FakeGhResult -ExitCode 0 -Output "Cephalon-Neza"
            }

            return New-FakeGhResult -ExitCode 1 -Output "Unexpected command: $command"
        }

        $result = Invoke-SignedReleaseDryRunReadiness -OutputPath $script:tempRoot -SkipDispatch -GitHubCliInvoker $invoker

        Test-Path -LiteralPath $result.JsonPath -PathType Leaf | Should -BeTrue
        $result.Report.'$schemaVersion' | Should -Be "1.1.0"
        $result.Report.Status | Should -Be "ready"
        $result.Report.BlockerClass | Should -BeNullOrEmpty
        $result.Report.DispatchActor | Should -Be "Cephalon-Neza"
        $result.Report.DispatchIdentityStatus | Should -Be "resolved"
        $result.Report.DispatchCommand | Should -Be 'gh workflow run "Publish Release" --repo Cephalon-Labs/CephalonEngine --ref master -f dry_run=true'
        $result.Report.RequiredReleaseManagerAction | Should -Match "RequireRunCreated"
        $result.Report.WorkflowActive | Should -BeTrue
        $result.Report.RepositoryActionsEnabled | Should -BeTrue
        $result.Report.DispatchAttempted | Should -BeFalse
        $result.Report.RunCreated | Should -BeFalse
    }

    It "maps an Actions-disabled dispatch identity to a stable blocker class" {
        $invoker = {
            param([string[]]$Arguments)

            $command = $Arguments -join " "
            if ($command -eq "api repos/Cephalon-Labs/CephalonEngine/actions/workflows") {
                return New-FakeGhResult -ExitCode 0 -Output (New-WorkflowListJson)
            }

            if ($command -eq "api repos/Cephalon-Labs/CephalonEngine/actions/permissions") {
                return New-FakeGhResult -ExitCode 0 -Output (New-ActionsPermissionsJson)
            }

            if ($command -eq "api user --jq .login") {
                return New-FakeGhResult -ExitCode 0 -Output "Cephalon-Neza"
            }

            if ($command -eq "workflow run Publish Release --repo Cephalon-Labs/CephalonEngine --ref master -f dry_run=true") {
                return New-FakeGhResult `
                    -ExitCode 1 `
                    -Output "could not create workflow dispatch event: HTTP 422: Actions has been disabled for this user."
            }

            return New-FakeGhResult -ExitCode 1 -Output "Unexpected command: $command"
        }

        $result = Invoke-SignedReleaseDryRunReadiness -OutputPath $script:tempRoot -GitHubCliInvoker $invoker

        $result.Report.Status | Should -Be "blocked"
        $result.Report.BlockerClass | Should -Be "dispatch-identity-actions-disabled"
        $result.Report.DispatchActor | Should -Be "Cephalon-Neza"
        $result.Report.DispatchIdentityStatus | Should -Be "resolved"
        $result.Report.RequiredReleaseManagerAction | Should -Be "Enable GitHub Actions for dispatch identity 'Cephalon-Neza' or rerun the probe with an Actions-enabled release-manager identity."
        $result.Report.DispatchAttempted | Should -BeTrue
        $result.Report.DispatchExitCode | Should -Be 1
        $result.Report.RunCreated | Should -BeFalse
        $result.Report.DispatchOutput | Should -Match "Actions has been disabled for this user"
    }

    It "records the workflow run when a dry-run dispatch succeeds" {
        $runList = @(
            @{
                databaseId = 123456789
                displayTitle = "Publish Release"
                event = "workflow_dispatch"
                status = "queued"
                conclusion = $null
                createdAt = "2026-05-13T06:00:00Z"
                url = "https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/123456789"
                headBranch = "master"
                headSha = "4f655c68"
            }
        ) | ConvertTo-Json -Depth 8

        $invoker = {
            param([string[]]$Arguments)

            $command = $Arguments -join " "
            if ($command -eq "api repos/Cephalon-Labs/CephalonEngine/actions/workflows") {
                return New-FakeGhResult -ExitCode 0 -Output (New-WorkflowListJson)
            }

            if ($command -eq "api repos/Cephalon-Labs/CephalonEngine/actions/permissions") {
                return New-FakeGhResult -ExitCode 0 -Output (New-ActionsPermissionsJson)
            }

            if ($command -eq "api user --jq .login") {
                return New-FakeGhResult -ExitCode 0 -Output "Cephalon-Neza"
            }

            if ($command -eq "workflow run Publish Release --repo Cephalon-Labs/CephalonEngine --ref master -f dry_run=true") {
                return New-FakeGhResult -ExitCode 0 -Output ""
            }

            if ($command -eq "run list --repo Cephalon-Labs/CephalonEngine --workflow Publish Release --limit 10 --json databaseId,displayTitle,event,status,conclusion,createdAt,url,headBranch,headSha") {
                return New-FakeGhResult -ExitCode 0 -Output $runList
            }

            return New-FakeGhResult -ExitCode 1 -Output "Unexpected command: $command"
        }

        $result = Invoke-SignedReleaseDryRunReadiness -OutputPath $script:tempRoot -GitHubCliInvoker $invoker

        $result.Report.Status | Should -Be "submitted"
        $result.Report.BlockerClass | Should -BeNullOrEmpty
        $result.Report.DispatchAttempted | Should -BeTrue
        $result.Report.RunCreated | Should -BeTrue
        $result.Report.RunLookupStatus | Should -Be "found"
        $result.Report.RunId | Should -Be "123456789"
        $result.Report.RunUrl | Should -Be "https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/123456789"
        $result.Report.RequiredReleaseManagerAction | Should -Match "Attach the generated report"
    }

    It "fails when RequireRunCreated is set and the dispatch remains blocked" {
        $invoker = {
            param([string[]]$Arguments)

            $command = $Arguments -join " "
            if ($command -eq "api repos/Cephalon-Labs/CephalonEngine/actions/workflows") {
                return New-FakeGhResult -ExitCode 0 -Output (New-WorkflowListJson)
            }

            if ($command -eq "api repos/Cephalon-Labs/CephalonEngine/actions/permissions") {
                return New-FakeGhResult -ExitCode 0 -Output (New-ActionsPermissionsJson)
            }

            if ($command -eq "api user --jq .login") {
                return New-FakeGhResult -ExitCode 0 -Output "Cephalon-Neza"
            }

            if ($command -eq "workflow run Publish Release --repo Cephalon-Labs/CephalonEngine --ref master -f dry_run=true") {
                return New-FakeGhResult `
                    -ExitCode 1 `
                    -Output "could not create workflow dispatch event: HTTP 422: Actions has been disabled for this user."
            }

            return New-FakeGhResult -ExitCode 1 -Output "Unexpected command: $command"
        }

        {
            Invoke-SignedReleaseDryRunReadiness -OutputPath $script:tempRoot -RequireRunCreated -GitHubCliInvoker $invoker
        } | Should -Throw "*readiness status is blocked/dispatch-identity-actions-disabled*"

        $jsonPath = Join-Path $script:tempRoot "signed-release-dry-run-readiness.json"
        Test-Path -LiteralPath $jsonPath -PathType Leaf | Should -BeTrue
        $report = Get-Content -LiteralPath $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
        $report.BlockerClass | Should -Be "dispatch-identity-actions-disabled"
        $report.DispatchActor | Should -Be "Cephalon-Neza"
        $report.RequiredReleaseManagerAction | Should -Match "Actions-enabled release-manager identity"
    }
}
