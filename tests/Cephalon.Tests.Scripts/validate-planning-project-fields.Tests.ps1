#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $env:CEPHALON_VALIDATE_PLANNING_PROJECT_FIELDS_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\validate-planning-project-fields.ps1"
    . $script:scriptPath
}

AfterAll {
    Remove-Item Env:\CEPHALON_VALIDATE_PLANNING_PROJECT_FIELDS_NO_RUN -ErrorAction SilentlyContinue
}

Describe "validate-planning-project-fields.ps1" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-planning-project-fields-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    function script:New-PlanningFieldValue {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Field,
            [string]$Name,
            [string]$Title,
            [double]$Number
        )

        $record = [ordered]@{
            field = [pscustomobject]@{ name = $Field }
        }

        if ($PSBoundParameters.ContainsKey("Name")) {
            $record.name = $Name
        }

        if ($PSBoundParameters.ContainsKey("Title")) {
            $record.title = $Title
        }

        if ($PSBoundParameters.ContainsKey("Number")) {
            $record.number = $Number
        }

        return [pscustomobject]$record
    }

    function script:New-PlanningIssueFixture {
        param(
            [int]$Number = 1346,
            [string]$Title = "ENG-678 Planning Project field completeness guard",
            [object[]]$FieldValues = @(
                (New-PlanningFieldValue -Field "Status" -Name "Todo"),
                (New-PlanningFieldValue -Field "Estimate" -Number 1),
                (New-PlanningFieldValue -Field "Iteration" -Title "Sprint 125"),
                (New-PlanningFieldValue -Field "Test" -Name "Needed"),
                (New-PlanningFieldValue -Field "Benchmark" -Name "N/A")
            ),
            [switch]$WithoutProjectItem
        )

        $projectItems = @()
        if (-not $WithoutProjectItem) {
            $projectItems = @(
                [pscustomobject]@{
                    id = "PVTI_example"
                    project = [pscustomobject]@{
                        title = "@Cephalon-Engine"
                        number = 2
                    }
                    fieldValues = [pscustomobject]@{
                        nodes = $FieldValues
                    }
                }
            )
        }

        return [pscustomobject]@{
            number = $Number
            title = $Title
            state = "OPEN"
            url = "https://example.test/issues/$Number"
            labels = @([pscustomobject]@{ name = "track:eng-678" })
            projectItems = $projectItems
        }
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

    It "passes when every tracked open ENG issue has all required Project fields" {
        $issues = @(
            (New-PlanningIssueFixture),
            [pscustomobject]@{
                number = 2000
                title = "Maintenance without tracking id"
                state = "OPEN"
                url = "https://example.test/issues/2000"
                labels = @()
            }
        )

        $result = Test-PlanningProjectFieldState -Issues $issues

        $result.ErrorCount | Should -Be 0
        $result.TrackedOpenIssueCount | Should -Be 1
        $result.ProjectItemCount | Should -Be 1
    }

    It "fails when a tracked ENG issue is missing required Project fields" {
        $issues = @(
            (New-PlanningIssueFixture -FieldValues @(
                (New-PlanningFieldValue -Field "Status" -Name "Todo"),
                (New-PlanningFieldValue -Field "Test" -Name "Needed"),
                (New-PlanningFieldValue -Field "Benchmark" -Name "N/A")
            ))
        )

        $result = Test-PlanningProjectFieldState -Issues $issues

        $result.ErrorCount | Should -Be 2
        $result.Errors[0] | Should -BeLike "*#1346*ENG-678*'Estimate'*"
        $result.Errors[1] | Should -BeLike "*#1346*ENG-678*'Iteration'*"
    }

    It "fails when a tracked ENG issue has no matching Project 2 item" {
        $issues = @(
            (New-PlanningIssueFixture -WithoutProjectItem)
        )

        $result = Test-PlanningProjectFieldState -Issues $issues

        $result.ErrorCount | Should -Be 1
        $result.Errors[0] | Should -Be "Open GitHub issue #1346 (ENG-678) is not present in Project 2."
    }

    It "imports offline JSON fixtures with GraphQL-shaped project field values" {
        $issuesPath = Join-Path $script:tempRoot "issues.json"
        Write-IssueFixture -Path $issuesPath -Issues @(
            (New-PlanningIssueFixture -Number 1346)
        )

        $issues = Import-PlanningProjectIssueList -JsonPath $issuesPath
        $result = Test-PlanningProjectFieldState -Issues $issues

        $issues.Count | Should -Be 1
        $result.ErrorCount | Should -Be 0
    }
}
