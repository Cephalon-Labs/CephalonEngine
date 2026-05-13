#requires -Version 7.0

[CmdletBinding()]
param(
    [string]$RepoFullName,
    [string]$ProjectTitle = "@Cephalon-Engine",
    [int]$ProjectNumber = 2,
    [string]$IssueListJsonPath,
    [int]$IssueLimit = 200,
    [string[]]$RequiredFields = @("Status", "Estimate", "Iteration", "Test", "Benchmark")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepositoryFullName {
    param([string]$ExplicitRepo)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitRepo)) {
        return $ExplicitRepo
    }

    $originUrl = (& git remote get-url origin).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($originUrl)) {
        throw "Unable to determine repository name. Pass -RepoFullName explicitly."
    }

    if ($originUrl -match "github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)(?:\.git)?$") {
        return "$($Matches.owner)/$($Matches.repo)"
    }

    throw "Unable to parse GitHub repository from remote URL '$originUrl'."
}

function Get-IssueLabelNames {
    param($Issue)

    $names = @()
    if (-not ($Issue.PSObject.Properties.Name -contains "labels")) {
        return $names
    }

    foreach ($label in @($Issue.labels)) {
        if ($null -eq $label) {
            continue
        }

        if ($label -is [string]) {
            $names += $label
            continue
        }

        if ($label.PSObject.Properties.Name -contains "name") {
            $names += [string]$label.name
        }
    }

    return $names
}

function Get-IssueEngIds {
    param($Issue)

    $ids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $title = if ($Issue.PSObject.Properties.Name -contains "title") { [string]$Issue.title } else { "" }
    foreach ($match in [regex]::Matches($title, "\bENG-\d+\b", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        [void]$ids.Add($match.Value.ToUpperInvariant())
    }

    foreach ($labelName in Get-IssueLabelNames -Issue $Issue) {
        if ($labelName -match "^track:(eng-\d+)$") {
            [void]$ids.Add($Matches[1].ToUpperInvariant())
        }
    }

    return @($ids)
}

function Import-OfflineProjectIssueList {
    param([string]$JsonPath)

    if (-not (Test-Path -LiteralPath $JsonPath)) {
        throw "Issue list JSON path '$JsonPath' does not exist."
    }

    $json = Get-Content -LiteralPath $JsonPath -Raw -Encoding utf8
    if ([string]::IsNullOrWhiteSpace($json)) {
        return @()
    }

    return @($json | ConvertFrom-Json)
}

function Import-GitHubOpenIssueList {
    param(
        [string]$Repo,
        [int]$Limit
    )

    $output = & gh issue list `
        --repo $Repo `
        --state open `
        --limit $Limit `
        --json number,title,state,url,labels

    if ($LASTEXITCODE -ne 0) {
        throw "gh issue list failed for '$Repo'."
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        return @()
    }

    return @($output | ConvertFrom-Json)
}

function Invoke-IssueProjectItemsQuery {
    param(
        [string]$Repo,
        [int]$IssueNumber
    )

    $repoParts = $Repo.Split("/", 2)
    if ($repoParts.Count -ne 2) {
        throw "Repository '$Repo' must use owner/name format."
    }

    $query = @'
query($owner:String!, $repo:String!, $number:Int!) {
  repository(owner:$owner, name:$repo) {
    issue(number:$number) {
      number
      title
      url
      projectItems(first: 20) {
        nodes {
          id
          project { title number }
          fieldValues(first: 50) {
            nodes {
              __typename
              ... on ProjectV2ItemFieldTextValue { text field { ... on ProjectV2FieldCommon { name } } }
              ... on ProjectV2ItemFieldNumberValue { number field { ... on ProjectV2FieldCommon { name } } }
              ... on ProjectV2ItemFieldDateValue { date field { ... on ProjectV2FieldCommon { name } } }
              ... on ProjectV2ItemFieldSingleSelectValue { name field { ... on ProjectV2FieldCommon { name } } }
              ... on ProjectV2ItemFieldIterationValue { title startDate duration field { ... on ProjectV2FieldCommon { name } } }
            }
          }
        }
      }
    }
  }
}
'@

    $output = & gh api graphql `
        -f query=$query `
        -F "owner=$($repoParts[0])" `
        -F "repo=$($repoParts[1])" `
        -F "number=$IssueNumber"

    if ($LASTEXITCODE -ne 0) {
        throw "gh GraphQL project item query failed for issue #$IssueNumber."
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        throw "gh GraphQL project item query returned no output for issue #$IssueNumber."
    }

    $result = $output | ConvertFrom-Json
    return $result.data.repository.issue
}

function Import-PlanningProjectIssueList {
    param(
        [string]$Repo,
        [string]$JsonPath,
        [int]$Limit
    )

    if (-not [string]::IsNullOrWhiteSpace($JsonPath)) {
        return Import-OfflineProjectIssueList -JsonPath $JsonPath
    }

    $resolvedRepo = Get-RepositoryFullName -ExplicitRepo $Repo
    $openIssues = Import-GitHubOpenIssueList -Repo $resolvedRepo -Limit $Limit
    $records = @()

    foreach ($issue in $openIssues) {
        if (@(Get-IssueEngIds -Issue $issue).Count -eq 0) {
            $records += $issue
            continue
        }

        $record = Invoke-IssueProjectItemsQuery -Repo $resolvedRepo -IssueNumber ([int]$issue.number)
        $records += [pscustomobject]@{
            number = [int]$issue.number
            title = [string]$issue.title
            state = if ($issue.PSObject.Properties.Name -contains "state") { [string]$issue.state } else { "" }
            url = if ($issue.PSObject.Properties.Name -contains "url") { [string]$issue.url } else { "" }
            labels = if ($issue.PSObject.Properties.Name -contains "labels") { @($issue.labels) } else { @() }
            projectItems = @($record.projectItems.nodes)
        }
    }

    return $records
}

function Get-ProjectItems {
    param($Issue)

    if (-not ($Issue.PSObject.Properties.Name -contains "projectItems")) {
        return @()
    }

    $projectItems = $Issue.projectItems
    if ($null -eq $projectItems) {
        return @()
    }

    if ($projectItems.PSObject.Properties.Name -contains "nodes") {
        return @($projectItems.nodes)
    }

    return @($projectItems)
}

function Select-PlanningProjectItem {
    param(
        $Issue,
        [string]$ExpectedProjectTitle,
        [int]$ExpectedProjectNumber
    )

    foreach ($item in Get-ProjectItems -Issue $Issue) {
        if ($null -eq $item -or -not ($item.PSObject.Properties.Name -contains "project")) {
            continue
        }

        $project = $item.project
        if ($null -eq $project) {
            continue
        }

        $numberMatches = $false
        if ($project.PSObject.Properties.Name -contains "number") {
            $numberMatches = ([int]$project.number) -eq $ExpectedProjectNumber
        }

        $titleMatches = $true
        if (-not [string]::IsNullOrWhiteSpace($ExpectedProjectTitle) -and ($project.PSObject.Properties.Name -contains "title")) {
            $titleMatches = [string]::Equals([string]$project.title, $ExpectedProjectTitle, [System.StringComparison]::OrdinalIgnoreCase)
        }

        if ($numberMatches -and $titleMatches) {
            return $item
        }
    }

    return $null
}

function Get-ProjectItemFieldValueNodes {
    param($ProjectItem)

    if ($null -eq $ProjectItem -or -not ($ProjectItem.PSObject.Properties.Name -contains "fieldValues")) {
        return @()
    }

    $fieldValues = $ProjectItem.fieldValues
    if ($null -eq $fieldValues) {
        return @()
    }

    if ($fieldValues.PSObject.Properties.Name -contains "nodes") {
        return @($fieldValues.nodes)
    }

    return @($fieldValues)
}

function Get-ProjectItemPopulatedFields {
    param($ProjectItem)

    $fields = @{}
    foreach ($fieldValue in Get-ProjectItemFieldValueNodes -ProjectItem $ProjectItem) {
        if ($null -eq $fieldValue) {
            continue
        }

        $propertyNames = @($fieldValue.PSObject.Properties.Name)
        $fieldName = ""
        if ($propertyNames -contains "field") {
            $field = $fieldValue.field
            if ($field -is [string]) {
                $fieldName = $field
            }
            elseif ($null -ne $field -and ($field.PSObject.Properties.Name -contains "name")) {
                $fieldName = [string]$field.name
            }
        }
        elseif ($propertyNames -contains "fieldName") {
            $fieldName = [string]$fieldValue.fieldName
        }

        if ([string]::IsNullOrWhiteSpace($fieldName)) {
            continue
        }

        $hasValue = $false
        foreach ($valueProperty in @("name", "text", "number", "date", "title", "startDate")) {
            if (-not ($propertyNames -contains $valueProperty)) {
                continue
            }

            $value = $fieldValue.$valueProperty
            if ($null -eq $value) {
                continue
            }

            if ($value -is [string] -and [string]::IsNullOrWhiteSpace($value)) {
                continue
            }

            $hasValue = $true
            break
        }

        $fields[$fieldName] = $hasValue
    }

    return $fields
}

function Test-PlanningProjectFieldState {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Issues,
        [string]$ExpectedProjectTitle = "@Cephalon-Engine",
        [int]$ExpectedProjectNumber = 2,
        [string[]]$RequiredFieldNames = @("Status", "Estimate", "Iteration", "Test", "Benchmark")
    )

    $errors = @()
    $trackedIssueCount = 0
    $projectItemCount = 0

    foreach ($issue in $Issues) {
        $engIds = @(Get-IssueEngIds -Issue $issue)
        if ($engIds.Count -eq 0) {
            continue
        }

        $trackedIssueCount++
        $issueNumber = [int]$issue.number
        $trackedIds = $engIds -join ", "
        $projectItem = Select-PlanningProjectItem `
            -Issue $issue `
            -ExpectedProjectTitle $ExpectedProjectTitle `
            -ExpectedProjectNumber $ExpectedProjectNumber

        if ($null -eq $projectItem) {
            $errors += "Open GitHub issue #$issueNumber ($trackedIds) is not present in Project $ExpectedProjectNumber."
            continue
        }

        $projectItemCount++
        $populatedFields = Get-ProjectItemPopulatedFields -ProjectItem $projectItem
        foreach ($requiredField in $RequiredFieldNames) {
            if (-not $populatedFields.ContainsKey($requiredField) -or -not $populatedFields[$requiredField]) {
                $errors += "Open GitHub issue #$issueNumber ($trackedIds) Project $ExpectedProjectNumber item is missing required field '$requiredField'."
            }
        }
    }

    return [pscustomobject]@{
        OpenIssueCount = @($Issues).Count
        TrackedOpenIssueCount = $trackedIssueCount
        ProjectItemCount = $projectItemCount
        RequiredFieldCount = @($RequiredFieldNames).Count
        ErrorCount = $errors.Count
        Errors = $errors
    }
}

if (-not $env:CEPHALON_VALIDATE_PLANNING_PROJECT_FIELDS_NO_RUN) {
    $issues = Import-PlanningProjectIssueList -Repo $RepoFullName -JsonPath $IssueListJsonPath -Limit $IssueLimit
    $result = Test-PlanningProjectFieldState `
        -Issues $issues `
        -ExpectedProjectTitle $ProjectTitle `
        -ExpectedProjectNumber $ProjectNumber `
        -RequiredFieldNames $RequiredFields

    if ($result.ErrorCount -gt 0) {
        foreach ($errorText in $result.Errors) {
            Write-Error $errorText -ErrorAction Continue
        }

        throw "Planning Project field validation failed with $($result.ErrorCount) error(s)."
    }

    Write-Host ("Planning Project field validation passed: {0} open issues, {1} tracked ENG issue(s), {2} Project item(s), {3} required field(s)." -f `
        $result.OpenIssueCount,
        $result.TrackedOpenIssueCount,
        $result.ProjectItemCount,
        $result.RequiredFieldCount)
}
