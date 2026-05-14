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
        Test-Path -LiteralPath $result.HandoffPath -PathType Leaf | Should -BeTrue
        $result.Report.'$schemaVersion' | Should -Be "1.2.0"
        $result.Report.Status | Should -Be "ready"
        $result.Report.BlockerClass | Should -BeNullOrEmpty
        $result.Report.DispatchBlockerScope | Should -Be "none"
        $result.Report.ReadinessDiagnostic | Should -Match "dispatch was skipped by request"
        $result.Report.WorkflowDispatchPrerequisitesStatus | Should -Be "repository-and-workflow-ready-dispatch-skipped"
        $result.Report.DispatchActor | Should -Be "Cephalon-Neza"
        $result.Report.DispatchIdentityStatus | Should -Be "resolved"
        $result.Report.DispatchCommand | Should -Be 'gh workflow run "Publish Release" --repo Cephalon-Labs/CephalonEngine --ref master -f dry_run=true'
        $result.Report.RequiredReleaseManagerAction | Should -Match "RequireRunCreated"
        $result.Report.WorkflowActive | Should -BeTrue
        $result.Report.WorkflowDispatchDeclared | Should -BeTrue
        $result.Report.RepositoryActionsEnabled | Should -BeTrue
        $result.Report.RepositoryWorkflowDispatchReady | Should -BeTrue
        $result.Report.DispatchAttempted | Should -BeFalse
        $result.Report.RunCreated | Should -BeFalse
        $handoff = Get-Content -LiteralPath $result.HandoffPath -Raw -Encoding UTF8
        $handoff | Should -Match "# Signed-release dry-run handoff"
        $handoff | Should -Match "Status: ``ready``"
        $handoff | Should -Match "DispatchBlockerScope: ``none``"
        $handoff | Should -Match "RepositoryWorkflowDispatchReady: ``True``"
        $handoff | Should -Match "WorkflowDispatchPrerequisitesStatus: ``repository-and-workflow-ready-dispatch-skipped``"
        $handoff | Should -Match "DispatchActor: ``Cephalon-Neza``"
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
        $result.Report.DispatchBlockerScope | Should -Be "identity"
        $result.Report.RepositoryWorkflowDispatchReady | Should -BeTrue
        $result.Report.WorkflowDispatchPrerequisitesStatus | Should -Be "repository-and-workflow-ready-identity-blocked"
        $result.Report.ReadinessDiagnostic | Should -Match "blocked by dispatch identity 'Cephalon-Neza'"
        $result.Report.DispatchActor | Should -Be "Cephalon-Neza"
        $result.Report.DispatchIdentityStatus | Should -Be "resolved"
        $result.Report.RequiredReleaseManagerAction | Should -Be "Enable GitHub Actions for dispatch identity 'Cephalon-Neza' or rerun the probe with an Actions-enabled release-manager identity."
        $result.Report.DispatchAttempted | Should -BeTrue
        $result.Report.DispatchExitCode | Should -Be 1
        $result.Report.RunCreated | Should -BeFalse
        $result.Report.DispatchOutput | Should -Match "Actions has been disabled for this user"
        Test-Path -LiteralPath $result.HandoffPath -PathType Leaf | Should -BeTrue
        $handoff = Get-Content -LiteralPath $result.HandoffPath -Raw -Encoding UTF8
        $handoff | Should -Match "BlockerClass: ``dispatch-identity-actions-disabled``"
        $handoff | Should -Match "DispatchBlockerScope: ``identity``"
        $handoff | Should -Match "RepositoryWorkflowDispatchReady: ``True``"
        $handoff | Should -Match "WorkflowDispatchPrerequisitesStatus: ``repository-and-workflow-ready-identity-blocked``"
        $handoff | Should -Match "DispatchActor: ``Cephalon-Neza``"
        $handoff | Should -Match "Enable GitHub Actions for dispatch identity 'Cephalon-Neza'"
        $handoff | Should -Match "Actions has been disabled for this user"
    }

    It "keeps generated report fields aligned with the supply-chain support contract" {
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

        $supportManifestPath = Join-Path $script:repoRoot "scripts\supply-chain-release-support.json"
        $supportManifest = Get-Content -LiteralPath $supportManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32
        $supportManifest.'$schemaVersion' | Should -Be "1.7.0"
        $supportManifest.signedReleaseDryRun.handoffOutputPath | Should -Be "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md"

        $requiredFields = @($supportManifest.signedReleaseDryRun.requiredReportFields)
        $requiredFields.Count | Should -BeGreaterThan 0
        $requiredFields | Should -Contain "DispatchActor"
        $requiredFields | Should -Contain "DispatchIdentityStatus"
        $requiredFields | Should -Contain "DispatchCommand"
        $requiredFields | Should -Contain "DispatchBlockerScope"
        $requiredFields | Should -Contain "ReadinessDiagnostic"
        $requiredFields | Should -Contain "RepositoryWorkflowDispatchReady"
        $requiredFields | Should -Contain "WorkflowDispatchPrerequisitesStatus"
        $requiredFields | Should -Contain "RequiredReleaseManagerAction"

        $reportPropertyNames = @($result.Report.PSObject.Properties.Name)
        foreach ($requiredField in $requiredFields) {
            $reportPropertyNames | Should -Contain $requiredField
        }

        $persistedReport = Get-Content -LiteralPath $result.JsonPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
        $persistedPropertyNames = @($persistedReport.PSObject.Properties.Name)
        foreach ($requiredField in $requiredFields) {
            $persistedPropertyNames | Should -Contain $requiredField
        }

        $handoff = Get-Content -LiteralPath $result.HandoffPath -Raw -Encoding UTF8
        foreach ($requiredField in $requiredFields) {
            $handoff | Should -Match $requiredField
        }
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
        $result.Report.DispatchBlockerScope | Should -Be "none"
        $result.Report.RepositoryWorkflowDispatchReady | Should -BeTrue
        $result.Report.WorkflowDispatchPrerequisitesStatus | Should -Be "workflow-dispatch-submitted-run-found"
        $result.Report.ReadinessDiagnostic | Should -Match "workflow run URL was found"
        $result.Report.DispatchAttempted | Should -BeTrue
        $result.Report.RunCreated | Should -BeTrue
        $result.Report.RunLookupStatus | Should -Be "found"
        $result.Report.RunId | Should -Be "123456789"
        $result.Report.RunUrl | Should -Be "https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/123456789"
        $result.Report.RequiredReleaseManagerAction | Should -Match "Attach the generated report"
        Test-Path -LiteralPath $result.HandoffPath -PathType Leaf | Should -BeTrue
        $handoff = Get-Content -LiteralPath $result.HandoffPath -Raw -Encoding UTF8
        $handoff | Should -Match "Status: ``submitted``"
        $handoff | Should -Match "RunCreated: ``True``"
        $handoff | Should -Match "https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/123456789"
        $handoff | Should -Match "Attach the generated report"
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
        $handoffPath = Join-Path $script:tempRoot "signed-release-dry-run-handoff.md"
        Test-Path -LiteralPath $handoffPath -PathType Leaf | Should -BeTrue
        $report = Get-Content -LiteralPath $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
        $report.BlockerClass | Should -Be "dispatch-identity-actions-disabled"
        $report.DispatchBlockerScope | Should -Be "identity"
        $report.RepositoryWorkflowDispatchReady | Should -BeTrue
        $report.WorkflowDispatchPrerequisitesStatus | Should -Be "repository-and-workflow-ready-identity-blocked"
        $report.ReadinessDiagnostic | Should -Match "blocked by dispatch identity 'Cephalon-Neza'"
        $report.DispatchActor | Should -Be "Cephalon-Neza"
        $report.RequiredReleaseManagerAction | Should -Match "Actions-enabled release-manager identity"
        $handoff = Get-Content -LiteralPath $handoffPath -Raw -Encoding UTF8
        $handoff | Should -Match "RunCreated: ``False``"
        $handoff | Should -Match "Actions-enabled release-manager identity"
    }
}
