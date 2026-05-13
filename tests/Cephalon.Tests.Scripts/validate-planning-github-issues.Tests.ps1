#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $env:CEPHALON_VALIDATE_PLANNING_GITHUB_ISSUES_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\validate-planning-github-issues.ps1"
    . $script:scriptPath
}

AfterAll {
    Remove-Item Env:\CEPHALON_VALIDATE_PLANNING_GITHUB_ISSUES_NO_RUN -ErrorAction SilentlyContinue
}

Describe "validate-planning-github-issues.ps1" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-planning-issues-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    function script:Write-BacklogFixture {
        param([Parameter(Mandatory = $true)][string]$Path)

        @'
# Backlog fixture

### ENG-647 Eventing durable-retry proof summary readback

Status: done
GitHub issue: #1308

### ENG-700 Active planning item

Status: in-progress
GitHub issue: #1400
'@ | Set-Content -LiteralPath $Path -Encoding UTF8
    }

    function script:Write-IssueFixture {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Path,
            [Parameter(Mandatory = $true)]
            [object[]]$Issues
        )

        $Issues | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $Path -Encoding UTF8
    }

    It "fails when an open issue tracks an ENG row already done at another GitHub issue number" {
        $backlogPath = Join-Path $script:tempRoot "engine-backlog.md"
        $issuesPath = Join-Path $script:tempRoot "issues.json"
        Write-BacklogFixture -Path $backlogPath
        Write-IssueFixture -Path $issuesPath -Issues @(
            [pscustomobject]@{
                number = 1307
                title = "ENG-647 Eventing durable-retry proof summary readback"
                state = "OPEN"
                url = "https://example.test/issues/1307"
                labels = @([pscustomobject]@{ name = "track:eng-647" })
            }
        )

        $backlogRows = Get-BacklogIssueRows -Path $backlogPath
        $issues = Import-IssueList -JsonPath $issuesPath
        $result = Test-PlanningGitHubIssueState -BacklogRows $backlogRows -Issues $issues

        $result.ErrorCount | Should -Be 1
        $result.Errors[0] | Should -BeLike "*ENG-647*#1308*#1307*"
    }

    It "fails when multiple open GitHub issues track the same active ENG id" {
        $backlogPath = Join-Path $script:tempRoot "engine-backlog.md"
        $issuesPath = Join-Path $script:tempRoot "issues.json"
        Write-BacklogFixture -Path $backlogPath
        Write-IssueFixture -Path $issuesPath -Issues @(
            [pscustomobject]@{
                number = 1400
                title = "ENG-700 Active planning item"
                state = "OPEN"
                url = "https://example.test/issues/1400"
                labels = @([pscustomobject]@{ name = "track:eng-700" })
            },
            [pscustomobject]@{
                number = 1401
                title = "Follow-up for ENG-700"
                state = "OPEN"
                url = "https://example.test/issues/1401"
                labels = @()
            }
        )

        $backlogRows = Get-BacklogIssueRows -Path $backlogPath
        $issues = Import-IssueList -JsonPath $issuesPath
        $result = Test-PlanningGitHubIssueState -BacklogRows $backlogRows -Issues $issues

        $result.ErrorCount | Should -Be 1
        $result.Errors[0] | Should -Be "Duplicate open GitHub issues track ENG-700: #1400, #1401."
    }

    It "passes for one active open issue and warns for the same done issue during closeout" {
        $backlogPath = Join-Path $script:tempRoot "engine-backlog.md"
        $issuesPath = Join-Path $script:tempRoot "issues.json"
        Write-BacklogFixture -Path $backlogPath
        Write-IssueFixture -Path $issuesPath -Issues @(
            [pscustomobject]@{
                number = 1308
                title = "ENG-647 Eventing durable-retry proof summary readback"
                state = "OPEN"
                url = "https://example.test/issues/1308"
                labels = @([pscustomobject]@{ name = "track:eng-647" })
            },
            [pscustomobject]@{
                number = 1400
                title = "ENG-700 Active planning item"
                state = "OPEN"
                url = "https://example.test/issues/1400"
                labels = @([pscustomobject]@{ name = "track:eng-700" })
            }
        )

        $backlogRows = Get-BacklogIssueRows -Path $backlogPath
        $issues = Import-IssueList -JsonPath $issuesPath
        $result = Test-PlanningGitHubIssueState -BacklogRows $backlogRows -Issues $issues

        $result.ErrorCount | Should -Be 0
        $result.WarningCount | Should -Be 1
        $result.Warnings[0] | Should -BeLike "*ENG-647*#1308*still open*"
    }
}
