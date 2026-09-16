BeforeAll {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile(
        (Join-Path $repoRoot 'scripts/sync-planning-github.ps1'), [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
    # Load pure planning functions only. Never execute the GitHub mutation entry point in tests.
    foreach ($name in @('Get-BacklogPhaseMap', 'Get-BacklogIssueSpecs', 'Get-ResolvedIterationTitle',
            'Test-IsLaterPlanningStatus', 'Get-TopLevelPlanningContext',
            'Get-DesiredTestFieldValue', 'Get-DesiredBenchmarkFieldValue', 'Get-PlanningIterationTitles',
            'Get-PhaseLabelName', 'Parse-PlanningMetadataFromBody', 'Normalize-Text',
            'Get-ManagedChildLabels', 'Get-TrackLabelNameForSpec', 'Get-IterationLabelName',
            'ConvertTo-IterationSlug')) {
        $definition = $ast.Find({ param($node)
            $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
        }, $true)
        . ([scriptblock]::Create($definition.Extent.Text))
    }
    function Get-GitHubAnchorSlug { param($Value) $Value.ToLowerInvariant() }
    function Get-RepositoryDocumentUrl { param($RepositoryContext, $Path, $Anchor) "$Path#$Anchor" }
    function Get-MilestoneWebUrl { param($RepositoryContext, $MilestoneNumber) "milestone/$MilestoneNumber" }
}

Describe 'Planning document metadata' {
    It 'keeps an ENG child identifier independent from its parent tracking label' {
        $parent = [pscustomobject]@{ Kind = 'Backlog'; EngCode = 'ENG-714'; PhaseNumber = 14; PlanningStatus = 'backlog' }
        $child = [pscustomobject]@{ Kind = 'Backlog'; EngCode = 'ENG-720' }
        $labels = @(Get-ManagedChildLabels -ParentSpec $parent -ChildSpec $child -IterationTitle 'Later / not scheduled yet')
        $labels | Should -Contain 'track:eng-720'
        $labels | Should -Not -Contain 'track:eng-714'
        $labels | Should -Contain 'phase:14-m3-m4-elevation'
        @(Get-ManagedChildLabels -ParentSpec $parent -IterationTitle 'Later / not scheduled yet') | Should -Contain 'track:eng-714'
    }

    It 'discovers declared iterations and preserves child metadata in both supported formats' {
        $spec = [pscustomobject]@{ ContentBody = 'Iteration: Capacity window A'; PlanningStatus = 'backlog' }
        @(Get-PlanningIterationTitles -DesiredIssues @($spec) -TopLevelIterationMap @{}) | Should -Contain 'Capacity window A'
        $plain = Parse-PlanningMetadataFromBody -Body "Parent epic: #1405`nEstimate: 24`nIteration: Later / not scheduled yet"
        $legacy = Parse-PlanningMetadataFromBody -Body "Parent epic: #1405`nEstimate: **24**`nPlanned iteration: **Sprint 2**"
        $plain.ParentIssueNumber | Should -Be 1405
        $plain.Estimate | Should -Be 24
        $plain.IterationTitle | Should -Be 'Later / not scheduled yet'
        $legacy.Estimate | Should -Be 24
        $legacy.IterationTitle | Should -Be 'Sprint 2'
        Get-PhaseLabelName -PhaseNumber 14 | Should -Be 'phase:14-m3-m4-elevation'
        Get-PhaseLabelName -PhaseNumber 15 | Should -Be 'phase:15-release-completeness'
        Get-PhaseLabelName -PhaseNumber 16 | Should -Be 'phase:16-adoption-evolution'
    }

    It 'uses explicit validation evidence instead of inferring success from a closed state or title' {
        Get-DesiredTestFieldValue -Title 'ENG-718 Planning' -Body 'Test: Passed' -State 'closed' | Should -Be 'Passed'
        Get-DesiredTestFieldValue -Title 'Closed experiment' -Body 'Test: Failed' -State 'closed' | Should -Be 'Failed'
        Get-DesiredBenchmarkFieldValue -Title 'Performance study' -Body 'Benchmark: Needed' -State 'closed' | Should -Be 'Needed'
    }

    It 'keeps a dated shipped task closed and uses its explicit phase' {
        $path = Join-Path $TestDrive 'backlog.md'
        @'
## Program
### ENG-717 Retained delivery
Status: shipped (September 16, 2026)
Estimate: 16
Phase: 14
Iteration: Operational Sprint 0
'@ | Set-Content -LiteralPath $path
        $spec = @(Get-BacklogIssueSpecs -Path $path)[0]
        $spec.State | Should -Be 'closed'
        $spec.PhaseNumber | Should -Be 14
        $spec.Estimate | Should -Be 16
        $spec.SyncKey | Should -Be 'backlog:eng-717'
    }

    It 'preserves the legacy phase fallback and leaves future work open' {
        $path = Join-Path $TestDrive 'backlog.md'
        @'
## Program
### ENG-005 Existing work
Status: backlog
Estimate: 8
'@ | Set-Content -LiteralPath $path
        $spec = @(Get-BacklogIssueSpecs -Path $path)[0]
        $spec.PhaseNumber | Should -Be 1
        $spec.State | Should -Be 'open'
    }

    It 'prefers an explicit unscheduled iteration over historical sprint placement' {
        $spec = [pscustomobject]@{
            Kind = 'Backlog'; EngCode = 'ENG-719'; Title = 'ENG-719 Contract inventory'
            ContentBody = "Phase: 14`nIteration: Later / not scheduled yet."
            PlanningStatus = 'backlog'; SourcePath = 'docs/engine-backlog.md'; PhaseNumber = $null
        }
        $context = Get-TopLevelPlanningContext -Spec $spec -RepositoryContext @{} -PhaseMilestones @{} `
            -IterationMap @{ 'ENG-719' = 'Sprint 125' } -BoardUrl 'project/2'
        $context.IterationTitle | Should -Be 'Later / not scheduled yet'
    }

    It 'keeps legacy sprint lookup when no explicit iteration exists' {
        $spec = [pscustomobject]@{
            Kind = 'Backlog'; EngCode = 'ENG-005'; Title = 'ENG-005 Existing work'
            ContentBody = 'Status: backlog'; PlanningStatus = 'backlog'
            SourcePath = 'docs/engine-backlog.md'; PhaseNumber = $null
        }
        $context = Get-TopLevelPlanningContext -Spec $spec -RepositoryContext @{} -PhaseMilestones @{} `
            -IterationMap @{ 'ENG-005' = 'Sprint 2' } -BoardUrl 'project/2'
        $context.IterationTitle | Should -Be 'Sprint 2'
    }

    It 'retains every September task once with phase, estimates and open implementation state' {
        $specs = @(Get-BacklogIssueSpecs -Path (Join-Path $repoRoot 'docs/engine-backlog.md'))
        $wave = @($specs | Where-Object { $_.EngCode -match '^ENG-7(1[89]|[23][0-9])$' -and [int]$_.EngCode.Substring(4) -le 738 })
        $wave.Count | Should -Be 21
        @($wave | Group-Object EngCode | Where-Object Count -ne 1).Count | Should -Be 0
        ($wave | Where-Object EngCode -eq 'ENG-718').State | Should -Be 'closed'
        $remaining = @($wave | Where-Object EngCode -ne 'ENG-718')
        @($remaining | Where-Object State -ne 'open').Count | Should -Be 0
        ($remaining | Measure-Object Estimate -Sum).Sum | Should -Be 536
        @($remaining | Where-Object { $_.PhaseNumber -notin @(14, 15, 16) }).Count | Should -Be 0
    }
}
