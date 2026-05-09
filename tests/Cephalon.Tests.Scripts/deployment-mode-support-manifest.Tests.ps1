#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester test suite for scripts/deployment-mode-support.json schema 1.x.

.DESCRIPTION
    Asserts the deployment-mode support manifest parses successfully and carries
    the expected schema 1.1.0 fields. Protects the manifest from accidental
    breakage when contributors edit it directly. Does not validate the values of
    individual fields beyond shape; that is intentionally not a test concern
    because the manifest's content is meant to evolve.

.NOTES
    Run from the repo root:

        Invoke-Pester -Path tests/Cephalon.Tests.Scripts/deployment-mode-support-manifest.Tests.ps1 -Output Detailed

    or as part of the full Cephalon.Tests.Scripts directory.
#>

BeforeAll {
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:manifestPath = Join-Path $script:repoRoot "scripts\deployment-mode-support.json"

    if (-not (Test-Path -LiteralPath $script:manifestPath)) {
        throw "Could not find manifest at $script:manifestPath"
    }

    $script:manifestRaw = Get-Content -LiteralPath $script:manifestPath -Raw -Encoding UTF8
    $script:manifest = $script:manifestRaw | ConvertFrom-Json -Depth 16
    $script:validateReleaseRaw = Get-Content -LiteralPath (Join-Path $script:repoRoot "scripts\validate-release.ps1") -Raw -Encoding UTF8
    $script:directoryBuildPropsRaw = Get-Content -LiteralPath (Join-Path $script:repoRoot "Directory.Build.props") -Raw -Encoding UTF8
    [xml]$script:directoryBuildPropsXml = $script:directoryBuildPropsRaw
    $script:compilerOnlyGlobalPropertiesToRemove = [string](
        $script:directoryBuildPropsXml.Project.PropertyGroup |
            ForEach-Object { $_.CephalonCompilerOnlyProjectReferenceGlobalPropertiesToRemove } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Select-Object -First 1
    )

    function Test-CsprojPropertyExpectation {
        param(
            [Parameter(Mandatory = $true)][string]$CsprojPath,
            [Parameter(Mandatory = $true)][string]$Entry
        )

        $parts = $Entry -split "=", 2
        $propertyName = $parts[0].Trim()
        if ([string]::IsNullOrWhiteSpace($propertyName)) {
            throw "requiredProjectProperties entry '$Entry' is missing a property name"
        }

        $expectedValue = if ($parts.Count -gt 1) { $parts[1].Trim() } else { "true" }
        if ($expectedValue -notin @("true", "false")) {
            throw "requiredProjectProperties entry '$Entry' must use '=true' or '=false' when an expected value is supplied"
        }

        [xml]$projectXml = Get-Content -LiteralPath $CsprojPath -Raw -Encoding UTF8
        $values = @(
            $projectXml.Project.PropertyGroup |
                ForEach-Object {
                    $_.ChildNodes |
                        Where-Object { $_.NodeType -eq [System.Xml.XmlNodeType]::Element -and $_.Name -eq $propertyName } |
                        ForEach-Object { $_.InnerText.Trim() }
                } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )

        $actualValue = if ($values.Count -gt 0) { $values[$values.Count - 1] } else { $null }

        return [pscustomobject]@{
            Property      = $propertyName
            ExpectedValue = $expectedValue
            ActualValue   = $actualValue
            Matched       = $null -ne $actualValue -and [string]::Equals($actualValue, $expectedValue, [System.StringComparison]::OrdinalIgnoreCase)
        }
    }

    function Test-CsprojTreatAsLocalPropertyExpectation {
        param(
            [Parameter(Mandatory = $true)][string]$CsprojPath,
            [Parameter(Mandatory = $true)][string[]]$ExpectedProperties
        )

        [xml]$projectXml = Get-Content -LiteralPath $CsprojPath -Raw -Encoding UTF8
        $declared = [string]$projectXml.Project.TreatAsLocalProperty
        $tokens = @(
            $declared -split ";" |
                ForEach-Object { $_.Trim() } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )

        $missing = @($ExpectedProperties | Where-Object { $tokens -notcontains $_ })
        return [pscustomobject]@{
            Declared = $declared
            Missing  = $missing
            Matched  = $missing.Count -eq 0
        }
    }

    function ConvertTo-CompilerOnlyGlobalPropertiesToRemoveTokens {
        param(
            [AllowNull()][string]$Value
        )

        $resolved = [string]$Value
        if ($resolved -eq '$(CephalonCompilerOnlyProjectReferenceGlobalPropertiesToRemove)') {
            $resolved = $script:compilerOnlyGlobalPropertiesToRemove
        }

        return @(
            $resolved -split ";" |
                ForEach-Object { $_.Trim() } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
    }

    function Get-LockFilePackageRows {
        param(
            [Parameter(Mandatory = $true)][string]$RepoRoot,
            [string[]]$LockFileGlobs = @()
        )

        $normalizedGlobs = @(
            $LockFileGlobs |
                ForEach-Object { [string]$_ } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                ForEach-Object { ($_ -replace '\\', '/') }
        )

        $lockFiles = Get-ChildItem -LiteralPath $RepoRoot -Recurse -Filter "packages.lock.json" -File -ErrorAction SilentlyContinue |
            Where-Object {
                if ($normalizedGlobs.Count -eq 0) { return $true }

                $relativePath = [System.IO.Path]::GetRelativePath($RepoRoot, $_.FullName) -replace '\\', '/'
                foreach ($glob in $normalizedGlobs) {
                    $matcher = [System.Management.Automation.WildcardPattern]::new(
                        $glob,
                        [System.Management.Automation.WildcardOptions]::IgnoreCase)
                    if ($matcher.IsMatch($relativePath)) {
                        return $true
                    }
                }

                return $false
            }

        $rows = @()
        foreach ($lockFile in $lockFiles) {
            $relativePath = [System.IO.Path]::GetRelativePath($RepoRoot, $lockFile.FullName) -replace '\\', '/'
            $lockJson = Get-Content -LiteralPath $lockFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 100
            if ($null -eq $lockJson -or
                -not $lockJson.PSObject.Properties.Match("dependencies").Count -or
                $null -eq $lockJson.dependencies) {
                continue
            }

            foreach ($tfm in $lockJson.dependencies.PSObject.Properties) {
                foreach ($dependency in $tfm.Value.PSObject.Properties) {
                    $rows += [pscustomobject]@{
                        PackageId = [string]$dependency.Name
                        LockFile  = $relativePath
                    }
                }
            }
        }

        return $rows
    }
}

Describe "deployment-mode-support.json — top-level schema" {
    It "parses as JSON without errors" {
        $script:manifest | Should -Not -BeNullOrEmpty
    }

    It "declares schema version 1.x" {
        $script:manifest.PSObject.Properties.Name | Should -Contain '$schemaVersion'
        $script:manifest.'$schemaVersion' | Should -Not -BeNullOrEmpty
        # accept any version that starts with 1. — schema 1.x is the manifest family
        $script:manifest.'$schemaVersion' | Should -Match '^1\.\d+\.\d+$'
    }

    It "declares analyzerOnlySignalsDoNotCount = true" {
        $script:manifest.analyzerOnlySignalsDoNotCount | Should -BeTrue
    }

    It "declares a validationStrategy" {
        $script:manifest.PSObject.Properties.Name | Should -Contain 'validationStrategy'
        $script:manifest.validationStrategy | Should -BeIn @('analyzer-only', 'publish-required', 'full-flow')
    }

    It "carries the expected top-level fields" {
        $expected = @(
            'shippingBaseline',
            'documentation',
            'analyzerOnlySignalsDoNotCount',
            'validationStrategy',
            'publishProbePolicy',
            'supportChangeRequirements',
            'deploymentModes',
            'representativePublishTargets',
            'expectedPublishOutputShape',
            'deploymentModeEligibility',
            'knownTransitiveHazardAudit',
            'knownTransitiveHazards'
        )
        foreach ($field in $expected) {
            $script:manifest.PSObject.Properties.Name | Should -Contain $field
        }
    }
}

Describe "publishProbePolicy" {
    It "promotes the single-file publish probe as a non-opt-out release gate without widening global support claims" {
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'comment'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'releaseValidationMode'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'releaseValidationDeploymentModes'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'releaseValidationSkipsPublish'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'nonOptOutGate'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'gatedModes'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'auditOnlyModes'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'failureBlocksRelease'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'failOnWarnings'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'gatePromotion'
        $script:manifest.publishProbePolicy.PSObject.Properties.Name | Should -Contain 'promotionRequirements'
        $script:manifest.publishProbePolicy.releaseValidationMode | Should -Be 'single-file-publish-gate'
        $script:manifest.publishProbePolicy.releaseValidationDeploymentModes | Should -Contain 'singleFile'
        $script:manifest.publishProbePolicy.releaseValidationSkipsPublish | Should -BeFalse
        $script:manifest.publishProbePolicy.nonOptOutGate | Should -BeTrue
        $script:manifest.publishProbePolicy.gatedModes | Should -Contain 'singleFile'
        $script:manifest.publishProbePolicy.auditOnlyModes | Should -Contain 'trim'
        $script:manifest.publishProbePolicy.auditOnlyModes | Should -Contain 'nativeAot'
        $script:manifest.publishProbePolicy.failureBlocksRelease | Should -BeTrue
        $script:manifest.publishProbePolicy.failOnWarnings | Should -BeTrue
        $script:manifest.publishProbePolicy.gatePromotion | Should -Be 'eng-510-single-file-publish-probe-release-gate'
        $script:manifest.deploymentModes.trim.status | Should -Be 'not-claimed'
        $script:manifest.deploymentModes.nativeAot.status | Should -Be 'not-claimed'
        $script:manifest.deploymentModes.singleFile.status | Should -Be 'not-claimed'
        @($script:manifest.publishProbePolicy.promotionRequirements).Count | Should -BeGreaterThan 0
    }

    It "names source, workflow, docs, and release-validation promotion requirements" {
        $requirements = @($script:manifest.publishProbePolicy.promotionRequirements) -join "|"
        $requirements | Should -Match 'deploymentModes'
        $requirements | Should -Match 'validate-deployment-mode-claims\.ps1'
        $requirements | Should -Match 'validate-release\.ps1'
        $requirements | Should -Match 'release-validation workflow'
        $requirements | Should -Match 'docs/deployment-mode-support\.md'
        $requirements | Should -Match 'docs/project-memory\.md'
    }

    It "keeps validate-release wired to the manifest-backed release-validation policy" {
        $script:validateReleaseRaw | Should -Match 'Get-DeploymentModeReleaseValidationPolicy'
        $script:validateReleaseRaw | Should -Match 'publishProbePolicy'
        $script:validateReleaseRaw | Should -Match 'releaseValidationDeploymentModes'
        $script:validateReleaseRaw | Should -Match 'releaseValidationSkipsPublish'
        $script:validateReleaseRaw | Should -Match '-DeploymentMode", \$releaseValidationDeploymentMode'
        $script:validateReleaseRaw | Should -Match '-SkipPublish'
    }

    It "keeps compiler-only analyzer references isolated from host publish-mode global properties" {
        $expectedGlobalPropertiesToRemove = @(
            "PublishTrimmed",
            "PublishAot",
            "PublishSingleFile",
            "SelfContained",
            "RuntimeIdentifier",
            "RuntimeIdentifiers"
        )

        $script:directoryBuildPropsRaw | Should -Match 'CephalonCompilerOnlyProjectReferenceGlobalPropertiesToRemove'
        $centralTokens = ConvertTo-CompilerOnlyGlobalPropertiesToRemoveTokens -Value $script:compilerOnlyGlobalPropertiesToRemove
        foreach ($propertyName in $expectedGlobalPropertiesToRemove) {
            $centralTokens | Should -Contain $propertyName
        }

        $projectFiles = @(
            Get-ChildItem -LiteralPath $script:repoRoot -Recurse -Include "*.csproj", "*.props" -File |
                Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
        )
        $compilerOnlyReferences = @()
        foreach ($file in $projectFiles) {
            [xml]$projectXml = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
            $compilerOnlyReferences += @(
                $projectXml.Project.ItemGroup.ProjectReference |
                    Where-Object {
                        $_.OutputItemType -eq "Analyzer" -and
                        $_.ReferenceOutputAssembly -eq "false"
                    } |
                    ForEach-Object {
                        [pscustomobject]@{
                            File = [System.IO.Path]::GetRelativePath($script:repoRoot, $file.FullName)
                            Include = $_.Include
                            GlobalPropertiesToRemove = $_.GlobalPropertiesToRemove
                        }
                    }
            )
        }

        $compilerOnlyReferences.Count | Should -BeGreaterThan 0
        foreach ($reference in $compilerOnlyReferences) {
            $reference.GlobalPropertiesToRemove | Should -Not -BeNullOrEmpty -Because "$($reference.File) -> $($reference.Include)"
            $referenceTokens = ConvertTo-CompilerOnlyGlobalPropertiesToRemoveTokens -Value $reference.GlobalPropertiesToRemove
            foreach ($propertyName in $expectedGlobalPropertiesToRemove) {
                $referenceTokens | Should -Contain $propertyName -Because "$($reference.File) -> $($reference.Include)"
            }
        }
    }
}

Describe "shippingBaseline" {
    It "names a stable target framework and a readiness lane TFM" {
        $script:manifest.shippingBaseline.stableTargetFramework | Should -Not -BeNullOrEmpty
        $script:manifest.shippingBaseline.readinessLaneTargetFramework | Should -Not -BeNullOrEmpty
    }

    It "records the readiness lane status" {
        $script:manifest.shippingBaseline.readinessLaneStatus | Should -BeIn @('assessment-only', 'active', 'deprecated')
    }
}

Describe "documentation block" {
    It "points at the support guide and readiness guide" {
        $script:manifest.documentation.guidePath | Should -Be 'docs/deployment-mode-support.md'
        $script:manifest.documentation.readinessGuidePath | Should -Be 'docs/dotnet11-readiness.md'
    }

    It "points at the validation harness script and its tests" {
        $script:manifest.documentation.PSObject.Properties.Name | Should -Contain 'validationHarnessPath'
        $script:manifest.documentation.PSObject.Properties.Name | Should -Contain 'validationHarnessTestsPath'
        $script:manifest.documentation.validationHarnessPath | Should -Be 'scripts/validate-deployment-mode-claims.ps1'
    }
}

Describe "supportChangeRequirements" {
    It "is a non-empty list" {
        $script:manifest.supportChangeRequirements | Should -Not -BeNullOrEmpty
        $script:manifest.supportChangeRequirements.Count | Should -BeGreaterThan 0
    }

    It "names the harness verdict gate" {
        ($script:manifest.supportChangeRequirements -join "|") | Should -Match 'validate-deployment-mode-claims\.ps1'
        ($script:manifest.supportChangeRequirements -join "|") | Should -Match 'claim-truthful'
    }
}

Describe "deploymentModes" {
    It "covers trim, nativeAot, and singleFile" {
        $script:manifest.deploymentModes.PSObject.Properties.Name | Should -Contain 'trim'
        $script:manifest.deploymentModes.PSObject.Properties.Name | Should -Contain 'nativeAot'
        $script:manifest.deploymentModes.PSObject.Properties.Name | Should -Contain 'singleFile'
    }

    It "every mode declares status, summary, requiredProjectProperties, requiredAnalyzerProperties, and warningPatterns" {
        foreach ($mode in @('trim', 'nativeAot', 'singleFile')) {
            $entry = $script:manifest.deploymentModes.$mode
            $entry.PSObject.Properties.Name | Should -Contain 'status'
            $entry.PSObject.Properties.Name | Should -Contain 'summary'
            $entry.PSObject.Properties.Name | Should -Contain 'requiredProjectProperties'
            $entry.PSObject.Properties.Name | Should -Contain 'requiredAnalyzerProperties'
            $entry.PSObject.Properties.Name | Should -Contain 'warningPatterns'
        }
    }

    It "every mode's status is one of the supported values" {
        foreach ($mode in @('trim', 'nativeAot', 'singleFile')) {
            $script:manifest.deploymentModes.$mode.status | Should -BeIn @('not-claimed', 'claimed')
        }
    }

    It "trim uses PublishTrimmed and EnableTrimAnalyzer" {
        $script:manifest.deploymentModes.trim.requiredProjectProperties | Should -Contain 'PublishTrimmed'
        $script:manifest.deploymentModes.trim.requiredAnalyzerProperties | Should -Contain 'EnableTrimAnalyzer'
    }

    It "nativeAot uses PublishAot and IsAotCompatible" {
        $script:manifest.deploymentModes.nativeAot.requiredProjectProperties | Should -Contain 'PublishAot'
        $script:manifest.deploymentModes.nativeAot.requiredAnalyzerProperties | Should -Contain 'IsAotCompatible'
    }

    It "singleFile uses PublishSingleFile and EnableSingleFileAnalyzer" {
        $script:manifest.deploymentModes.singleFile.requiredProjectProperties | Should -Contain 'PublishSingleFile'
        $script:manifest.deploymentModes.singleFile.requiredAnalyzerProperties | Should -Contain 'EnableSingleFileAnalyzer'
    }

    It "trim warningPatterns include canonical IL2026" {
        $script:manifest.deploymentModes.trim.warningPatterns | Should -Contain 'IL2026'
    }

    It "nativeAot warningPatterns include canonical IL3050" {
        $script:manifest.deploymentModes.nativeAot.warningPatterns | Should -Contain 'IL3050'
    }
}

Describe "representativePublishTargets" {
    It "exists with a comment and a projects array" {
        $script:manifest.representativePublishTargets.PSObject.Properties.Name | Should -Contain 'comment'
        $script:manifest.representativePublishTargets.PSObject.Properties.Name | Should -Contain 'projects'
    }

    It "projects is a non-empty array" {
        # Wrap in @() so PowerShell's pipe semantics do not unwrap single-element arrays into the
        # element type; we want to assert the manifest's projects field is array-shaped even when
        # future edits shrink or grow the target list.
        $projects = @($script:manifest.representativePublishTargets.projects)
        $projects.GetType().IsArray | Should -BeTrue -Because "publish targets must stay array-shaped"
        $projects.Count | Should -BeGreaterThan 0 -Because "the harness now has manifest-declared default publish-probe targets"
    }

    It "covers the staged sample publish-probe surface" {
        $projects = @($script:manifest.representativePublishTargets.projects)
        $expectedProjects = @(
            'samples/Cephalon.Sample.ModularMonolith/Cephalon.Sample.ModularMonolith.csproj',
            'samples/Cephalon.Sample.ModularVerticalSlice/Cephalon.Sample.ModularVerticalSlice.csproj',
            'samples/Cephalon.Sample.Microservice/Cephalon.Sample.Microservice.csproj',
            'samples/Cephalon.Sample.MicroserviceSuite/services/CatalogService/Cephalon.Sample.MicroserviceSuite.CatalogService.csproj',
            'samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj'
        )

        $projects.Count | Should -Be $expectedProjects.Count -Because "ENG-447 deliberately stages the smallest five-host publish-probe surface"
        foreach ($expectedProject in $expectedProjects) {
            $projects | Should -Contain $expectedProject
        }
    }

    It "every projects entry is a relative csproj path string" {
        foreach ($entry in @($script:manifest.representativePublishTargets.projects)) {
            $entry | Should -BeOfType [string] -Because "publish targets are relative csproj paths"
            $entry | Should -Match '\.csproj$' -Because "publish targets must point at a project file"
            [System.IO.Path]::IsPathRooted($entry) | Should -BeFalse -Because "publish targets stay relative to repo root"
        }
    }

    It "every projects entry resolves to a csproj file that exists on disk" {
        # ENG-431: prevent the harness from being seeded with a path that has been moved or
        # renamed without updating the manifest in the same slice. The harness's default-targets
        # behavior (Invoke-PublishProbe in scripts/validate-deployment-mode-claims.ps1) silently
        # skips an entry whose file is missing, which would mask drift. Surface it here instead.
        foreach ($entry in @($script:manifest.representativePublishTargets.projects)) {
            $resolved = Join-Path $script:repoRoot ($entry -replace '/', [System.IO.Path]::DirectorySeparatorChar)
            Test-Path -LiteralPath $resolved -PathType Leaf | Should -BeTrue -Because "representativePublishTargets.projects entry '$entry' must exist at '$resolved'"
        }
    }
}

Describe "expectedPublishOutputShape" {
    It "covers all three deployment modes" {
        $script:manifest.expectedPublishOutputShape.PSObject.Properties.Name | Should -Contain 'trim'
        $script:manifest.expectedPublishOutputShape.PSObject.Properties.Name | Should -Contain 'nativeAot'
        $script:manifest.expectedPublishOutputShape.PSObject.Properties.Name | Should -Contain 'singleFile'
    }

    It "every mode entry declares expectFile, maxAllowedSizeBytes, and allowedWarningCategories" {
        foreach ($mode in @('trim', 'nativeAot', 'singleFile')) {
            $shape = $script:manifest.expectedPublishOutputShape.$mode
            $shape.PSObject.Properties.Name | Should -Contain 'expectFile'
            $shape.PSObject.Properties.Name | Should -Contain 'maxAllowedSizeBytes'
            $shape.PSObject.Properties.Name | Should -Contain 'allowedWarningCategories'
        }
    }
}

Describe "deploymentModeEligibility" {
    It "exists with a comment and packages array" {
        $script:manifest.deploymentModeEligibility.PSObject.Properties.Name | Should -Contain 'comment'
        $script:manifest.deploymentModeEligibility.PSObject.Properties.Name | Should -Contain 'packages'
    }

    It "every package entry carries the per-package shape ENG-427 seeded" {
        $packages = $script:manifest.deploymentModeEligibility.packages
        # packages may be empty in a future state; only validate shape when populated
        foreach ($pkg in $packages) {
            $pkg.PSObject.Properties.Name | Should -Contain 'packageName'
            $pkg.PSObject.Properties.Name | Should -Contain 'nugetId'
            $pkg.PSObject.Properties.Name | Should -Contain 'claimAuditTier'
            $pkg.PSObject.Properties.Name | Should -Contain 'supportedModes'
            $pkg.PSObject.Properties.Name | Should -Contain 'requiredProjectProperties'
            $pkg.PSObject.Properties.Name | Should -Contain 'knownHazards'
            $pkg.PSObject.Properties.Name | Should -Contain 'evidence'
            $pkg.PSObject.Properties.Name | Should -Contain 'introducedBy'

            $pkg.packageName | Should -Match '^Cephalon\.'
            $pkg.nugetId | Should -Match '^Cephalon\.'
            $pkg.claimAuditTier | Should -BeIn @('excluded-by-design', 'clean-baseline', 'low', 'medium', 'high')

            $supportedModes = @($pkg.supportedModes)
            foreach ($mode in $supportedModes) {
                $mode | Should -BeIn @('trim', 'nativeAot', 'singleFile')
            }

            if ($supportedModes.Count -gt 0) {
                $pkg.claimAuditTier | Should -Be 'clean-baseline' -Because "package-scoped support claims must start from clean-baseline packages"
                $pkg.requiredProjectProperties | Should -Not -BeNullOrEmpty -Because "package-scoped support claims must be backed by explicit project properties"

                if ($supportedModes -contains 'singleFile') {
                    $pkg.requiredProjectProperties | Should -Contain 'PublishSingleFile=true'
                    $pkg.requiredProjectProperties | Should -Contain 'EnableSingleFileAnalyzer=true'
                }
            }
        }
    }

    It "every hazard entry on a non-excluded package carries kind/site/pattern/remediation" {
        $packages = $script:manifest.deploymentModeEligibility.packages
        foreach ($pkg in $packages) {
            if ($pkg.claimAuditTier -in @('excluded-by-design', 'clean-baseline')) { continue }
            $pkg.knownHazards | Should -Not -BeNullOrEmpty -Because "non-excluded package $($pkg.packageName) must record at least one knownHazards entry"
            foreach ($hz in $pkg.knownHazards) {
                $hz.PSObject.Properties.Name | Should -Contain 'kind'
                $hz.PSObject.Properties.Name | Should -Contain 'site'
                $hz.PSObject.Properties.Name | Should -Contain 'pattern'
                $hz.PSObject.Properties.Name | Should -Contain 'remediation'
                $hz.kind | Should -Not -BeNullOrEmpty
                $hz.site | Should -Not -BeNullOrEmpty
            }
        }
    }

    It "every package's packageName resolves to an existing src csproj on disk" {
        # ENG-431: drift-protect the inventory ↔ manifest seeding from ENG-426 / ENG-427. When a
        # package is renamed or moved, this test fails until the manifest entry is updated, so a
        # rename slice cannot silently leave the per-package hazard list pointing at a stale name.
        # Convention: every shipped Cephalon.* runtime package lives at src/<packageName>/<packageName>.csproj.
        $packages = $script:manifest.deploymentModeEligibility.packages
        foreach ($pkg in $packages) {
            $relativePath = "src/$($pkg.packageName)/$($pkg.packageName).csproj"
            $resolved = Join-Path $script:repoRoot ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
            Test-Path -LiteralPath $resolved -PathType Leaf | Should -BeTrue -Because "deploymentModeEligibility.packages entry '$($pkg.packageName)' must exist at '$resolved'"
        }
    }

    It "every package requiredProjectProperties entry matches its project file" {
        # ENG-478: requiredProjectProperties now protects both scoped support claims and permanent
        # not-claimed package postures. If the manifest says a package explicitly disables or
        # enables a deployment-mode property, the owning project must carry that exact value.
        $packages = $script:manifest.deploymentModeEligibility.packages
        foreach ($pkg in $packages) {
            $entries = @($pkg.requiredProjectProperties | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
            if ($entries.Count -eq 0) { continue }

            $relativePath = "src/$($pkg.packageName)/$($pkg.packageName).csproj"
            $resolved = Join-Path $script:repoRoot ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
            foreach ($entry in $entries) {
                $result = Test-CsprojPropertyExpectation -CsprojPath $resolved -Entry $entry
                $result.Matched | Should -BeTrue -Because "deploymentModeEligibility.packages entry '$($pkg.packageName)' requires '$entry', but '$($result.Property)' in '$relativePath' was '$($result.ActualValue)'"
            }
        }
    }

    It "excluded-by-design compiler-only projects localize publish-mode globals before project properties apply" {
        $expectedLocalProperties = @(
            "PublishTrimmed",
            "PublishAot",
            "PublishSingleFile",
            "SelfContained",
            "RuntimeIdentifier",
            "RuntimeIdentifiers"
        )

        $compilerOnlyPackages = @($script:manifest.deploymentModeEligibility.packages | Where-Object { $_.claimAuditTier -eq "excluded-by-design" })
        $compilerOnlyPackages.Count | Should -BeGreaterThan 0

        foreach ($package in $compilerOnlyPackages) {
            $packageName = [string]$package.packageName
            $csprojPath = Join-Path $script:repoRoot "src\$packageName\$packageName.csproj"
            $result = Test-CsprojTreatAsLocalPropertyExpectation -CsprojPath $csprojPath -ExpectedProperties $expectedLocalProperties

            $result.Matched | Should -BeTrue -Because "$packageName must be able to force publish-mode properties back to false even when a host publish probe passes them globally; missing: $($result.Missing -join ', ')"
        }
    }

    It "every non-excluded package's first knownHazards.site points at a file that exists on disk" {
        # ENG-431: drift-protect the per-hazard call-site references seeded from the inventory.
        # Each hazard.site is recorded as either 'src/.../File.cs' or 'src/.../File.cs:line'; we
        # strip any trailing ':line' suffix and assert the .cs file itself exists. Line numbers
        # are intentionally not validated because they shift with unrelated edits, but a rename or
        # move of the hazard file must be reflected in the manifest in the same slice.
        $packages = $script:manifest.deploymentModeEligibility.packages
        foreach ($pkg in $packages) {
            if ($pkg.claimAuditTier -in @('excluded-by-design', 'clean-baseline')) { continue }
            foreach ($hz in $pkg.knownHazards) {
                # site shape examples:
                #   "src/Cephalon.Engine/Composition/ModuleDiscovery.cs:113"
                #   "src/Cephalon.ReferenceDocs/Generation/ReferenceDocsGenerator.cs:295"
                # take the first ".cs" path (stop at the first close-paren / whitespace+'(' / ':line' boundary)
                $rawSite = [string]$hz.site
                $firstCs = ($rawSite -split '\s|\(' | Where-Object { $_ -match '\.cs(:|$)' } | Select-Object -First 1)
                if (-not $firstCs) { continue }
                $relPath = ($firstCs -replace ':\d+$', '')
                $resolved = Join-Path $script:repoRoot ($relPath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
                Test-Path -LiteralPath $resolved -PathType Leaf | Should -BeTrue -Because "knownHazards.site '$($hz.site)' on package '$($pkg.packageName)' must resolve to an existing file at '$resolved'"
            }
        }
    }

    It "dynamic Minimal API operator-route hazards point at source sites carrying trim and AOT boundary annotations" {
        $dynamicHazards = @(
            foreach ($pkg in $script:manifest.deploymentModeEligibility.packages) {
                foreach ($hz in @($pkg.knownHazards)) {
                    if ([string]::Equals([string]$hz.kind, "dynamic-minimal-api-operator-route-binding", [System.StringComparison]::OrdinalIgnoreCase)) {
                        [pscustomobject]@{
                            PackageName = [string]$pkg.packageName
                            Hazard = $hz
                        }
                    }
                }
            }
        )

        $dynamicHazards.Count | Should -BeGreaterThan 0 -Because "the ASP.NET Core route boundary hazard should stay manifest-backed"
        foreach ($row in $dynamicHazards) {
            $rawSite = [string]$row.Hazard.site
            $match = [regex]::Match($rawSite.Trim(), '^(?<path>.+?\.cs)(?::(?<line>\d+))?')
            $match.Success | Should -BeTrue -Because "dynamic route hazard '$rawSite' on package '$($row.PackageName)' must include a .cs source path"

            $relativePath = $match.Groups["path"].Value
            $lineNumber = if ($match.Groups["line"].Success) { [int]$match.Groups["line"].Value } else { 0 }
            $resolved = Join-Path $script:repoRoot ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)

            Test-Path -LiteralPath $resolved -PathType Leaf | Should -BeTrue -Because "dynamic route hazard '$rawSite' on package '$($row.PackageName)' must resolve to an existing file"
            $sourceLines = @(Get-Content -LiteralPath $resolved -Encoding UTF8)
            if ($lineNumber -gt 0) {
                ($lineNumber -le $sourceLines.Count) | Should -BeTrue -Because "dynamic route hazard '$rawSite' line number must remain inside '$relativePath'"
                $start = [Math]::Max(1, $lineNumber - 8)
                $end = [Math]::Min($sourceLines.Count, $lineNumber + 8)
                $window = $sourceLines[($start - 1)..($end - 1)] -join "`n"
            }
            else {
                $window = $sourceLines -join "`n"
            }

            $window | Should -Match '\[RequiresUnreferencedCode\(' -Because "dynamic route hazard '$rawSite' must surface the trim boundary to consumers and analyzers"
            $window | Should -Match '\[RequiresDynamicCode\(' -Because "dynamic route hazard '$rawSite' must surface the Native AOT boundary to consumers and analyzers"
        }
    }

    It "core and full common ASP.NET Core operator routes use request delegates instead of Minimal API delegate binding" {
        $sourcePath = Join-Path $script:repoRoot "src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs"
        $sourcePath = $sourcePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
        Test-Path -LiteralPath $sourcePath -PathType Leaf | Should -BeTrue

        $source = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
        $fullCommonStart = $source.IndexOf("private static void MapCephalonFullCommonOperatorRoutes(", [System.StringComparison]::Ordinal)
        $coreStart = $source.IndexOf("private static void MapCephalonCoreOperatorRoutes(", [System.StringComparison]::Ordinal)
        $helperStart = $source.IndexOf("private static void MapGetRequestDelegate(", [System.StringComparison]::Ordinal)
        $nextHelperStart = $source.IndexOf("private static TService GetRequiredService", [System.StringComparison]::Ordinal)

        $fullCommonStart | Should -BeGreaterOrEqual 0 -Because "the full common operator route method should stay explicit"
        $coreStart | Should -BeGreaterOrEqual 0 -Because "the core operator route method should stay explicit"
        $coreStart | Should -BeGreaterThan $fullCommonStart -Because "the core route method should follow the full common route method"
        $helperStart | Should -BeGreaterThan $coreStart -Because "the request-delegate helper should follow the core route method"
        $nextHelperStart | Should -BeGreaterThan $helperStart -Because "the request-delegate helper block should stay parseable"

        $fullCommonBlock = $source.Substring($fullCommonStart, $coreStart - $fullCommonStart)
        $coreBlock = $source.Substring($coreStart, $helperStart - $coreStart)
        $helperBlock = $source.Substring($helperStart, $nextHelperStart - $helperStart)

        $fullCommonBlock | Should -Not -Match '\.MapGet\(' -Because "full common operator routes must not reintroduce Minimal API delegate binding"
        $fullCommonBlock | Should -Match 'MapGetRequestDelegate' -Because "full common operator routes should stay on the request-delegate helper"
        $coreBlock | Should -Not -Match '\.MapGet\(' -Because "core operator routes must not reintroduce Minimal API delegate binding"
        $coreBlock | Should -Match 'MapGetRequestDelegate' -Because "core operator routes should stay on the request-delegate helper"
        $helperBlock | Should -Match 'RequestDelegate requestDelegate' -Because "the helper must accept prebuilt request delegates"
        $helperBlock | Should -Match '\.MapMethods\(' -Because "request delegates should be mapped through MapMethods rather than Delegate route handlers"
    }

    It "full workflow ASP.NET Core operator routes use request delegates instead of Minimal API delegate binding" {
        $sourcePath = Join-Path $script:repoRoot "src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs"
        $sourcePath = $sourcePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
        Test-Path -LiteralPath $sourcePath -PathType Leaf | Should -BeTrue

        $source = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
        $workflowStart = $source.IndexOf("MapCephalonFullCommonOperatorRoutes(engineGroup);", [System.StringComparison]::Ordinal)
        $foundationStart = $source.IndexOf('MapGetResultRequestDelegate(engineGroup, "/rate-limiting"', [System.StringComparison]::Ordinal)
        $helperStart = $source.IndexOf("private static void MapGetResultRequestDelegate(", [System.StringComparison]::Ordinal)
        $nextHelperStart = $source.IndexOf("private static TService GetRequiredService", [System.StringComparison]::Ordinal)

        $workflowStart | Should -BeGreaterOrEqual 0 -Because "the full operator route catalog should keep its common route boundary visible"
        $foundationStart | Should -BeGreaterThan $workflowStart -Because "the rate-limiting route marks the next full route family after the workflow request-delegate block"
        $helperStart | Should -BeGreaterThan $foundationStart -Because "the result request-delegate helper should follow the route catalog"
        $nextHelperStart | Should -BeGreaterThan $helperStart -Because "the result request-delegate helper block should stay parseable"

        $workflowBlock = $source.Substring($workflowStart, $foundationStart - $workflowStart)
        $helperBlock = $source.Substring($helperStart, $nextHelperStart - $helperStart)

        $workflowBlock | Should -Not -Match 'engineGroup\.Map(Get|Post)\(' -Because "behavior-resilience, saga choreography, and durable execution full routes must not reintroduce Minimal API delegate binding"
        $workflowBlock | Should -Match 'MapGetResultRequestDelegate' -Because "full workflow operator routes should stay on the result request-delegate helper"
        $workflowBlock | Should -Match '/behavior-resilience' -Because "the behavior resilience full route family should stay in the audited request-delegate block"
        $workflowBlock | Should -Match '/saga-choreographies/runtime/publications/\{publicationStateId\}' -Because "the saga choreography runtime route family should stay in the audited request-delegate block"
        $workflowBlock | Should -Match '/durable-executions/runtime/streams/\{streamId\}' -Because "the durable execution runtime route family should stay in the audited request-delegate block"
        $helperBlock | Should -Match 'Func<HttpContext, IResult> handler' -Because "the helper should execute a prebuilt result handler without ASP.NET Core delegate binding"
        $helperBlock | Should -Match 'MapGetRequestDelegate' -Because "result request delegates should flow through the MapMethods-backed GET helper"
    }

    It "full foundation ASP.NET Core operator routes use request delegates instead of Minimal API delegate binding" {
        $sourcePath = Join-Path $script:repoRoot "src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs"
        $sourcePath = $sourcePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
        Test-Path -LiteralPath $sourcePath -PathType Leaf | Should -BeTrue

        $source = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
        $foundationStart = $source.IndexOf('MapGetResultRequestDelegate(engineGroup, "/rate-limiting"', [System.StringComparison]::Ordinal)
        $cdcRuntimeStart = $source.IndexOf('MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes"', [System.StringComparison]::Ordinal)
        $tailStart = $source.IndexOf('MapGetResultRequestDelegate(engineGroup, "/transports"', [System.StringComparison]::Ordinal)
        $hostInfrastructureStart = $source.IndexOf("MapCephalonHostInfrastructure(", $tailStart, [System.StringComparison]::Ordinal)

        $foundationStart | Should -BeGreaterOrEqual 0 -Because "the full foundation route block should start at rate limiting"
        $cdcRuntimeStart | Should -BeGreaterThan $foundationStart -Because "CDC runtime routes are the next larger full route family after the foundation block"
        $tailStart | Should -BeGreaterThan $cdcRuntimeStart -Because "the tail introspection block should stay visible after the richer full route catalog"
        $hostInfrastructureStart | Should -BeGreaterThan $tailStart -Because "the host infrastructure mapping should follow the final operator routes"

        $foundationBlock = $source.Substring($foundationStart, $cdcRuntimeStart - $foundationStart)
        $tailBlock = $source.Substring($tailStart, $hostInfrastructureStart - $tailStart)

        $foundationBlock | Should -Not -Match 'engineGroup\.Map(Get|Post)\(' -Because "rate limiting, REST metadata, database, execution, and CDC capture list routes must not reintroduce Minimal API delegate binding"
        $tailBlock | Should -Not -Match 'engineGroup\.Map(Get|Post)\(' -Because "final transport, diagnostics, policy, status, and module routes must not reintroduce Minimal API delegate binding"
        $foundationBlock | Should -Match '/rate-limiting' -Because "rate limiting should stay in the audited request-delegate block"
        $foundationBlock | Should -Match '/rest-endpoint-suppressions/\{suppressionId\}' -Because "REST endpoint governance should stay in the audited request-delegate block"
        $foundationBlock | Should -Match '/database-migration-playbook' -Because "database operational playbook should stay in the audited request-delegate block"
        $foundationBlock | Should -Match '/hosted-executions/\{hostedExecutionId\}' -Because "hosted execution lookup should stay in the audited request-delegate block"
        $foundationBlock | Should -Match '/cdc-captures' -Because "CDC capture catalog list should stay in the audited request-delegate block"
        $tailBlock | Should -Match '/diagnostics-conventions' -Because "diagnostics convention readback should stay in the audited request-delegate block"
        $tailBlock | Should -Match '/modules/\{moduleId\}' -Because "module lookup should stay in the audited request-delegate block"
    }

    It "full CDC runtime ASP.NET Core operator routes use request delegates instead of Minimal API delegate binding" {
        $sourcePath = Join-Path $script:repoRoot "src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs"
        $sourcePath = $sourcePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
        Test-Path -LiteralPath $sourcePath -PathType Leaf | Should -BeTrue

        $source = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
        $cdcRuntimeStart = $source.IndexOf('MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes"', [System.StringComparison]::Ordinal)
        $cdcReportStart = $source.IndexOf('engineGroup.MapPost("/cdc-capture-runtimes/{executionRuntimeId}/reports"', $cdcRuntimeStart, [System.StringComparison]::Ordinal)
        $helperStart = $source.IndexOf("private static void MapCdcCaptureRuntimeCollectionRoute(", [System.StringComparison]::Ordinal)
        $nextHelperStart = $source.IndexOf("private static TService GetRequiredService", [System.StringComparison]::Ordinal)

        $cdcRuntimeStart | Should -BeGreaterOrEqual 0 -Because "the CDC runtime read-only route block should stay visible"
        $cdcReportStart | Should -BeGreaterThan $cdcRuntimeStart -Because "CDC runtime report submission remains the following POST action seam"
        $helperStart | Should -BeGreaterThan $cdcReportStart -Because "the CDC runtime request-delegate helper should follow the route catalog"
        $nextHelperStart | Should -BeGreaterThan $helperStart -Because "the CDC runtime helper block should stay parseable"

        $cdcRuntimeBlock = $source.Substring($cdcRuntimeStart, $cdcReportStart - $cdcRuntimeStart)
        $helperBlock = $source.Substring($helperStart, $nextHelperStart - $helperStart)

        $cdcRuntimeBlock | Should -Not -Match 'engineGroup\.MapGet\(' -Because "CDC runtime read-only routes must not reintroduce Minimal API delegate binding"
        ([regex]::Matches($cdcRuntimeBlock, 'MapCdcCaptureRuntimeCollectionRoute')).Count | Should -Be 126
        ([regex]::Matches($cdcRuntimeBlock, 'MapCdcCaptureRuntimeDescriptorRoute')).Count | Should -Be 19
        ([regex]::Matches($cdcRuntimeBlock, 'MapGetResultRequestDelegate')).Count | Should -Be 1
        $cdcRuntimeBlock | Should -Match '/cdc-capture-runtimes/provider-specific-control-plane-materializers/current-nodes/\{canUseOnCurrentNode:bool\}' -Because "provider-specific bool route constraints should stay covered by the CDC runtime block"
        $cdcRuntimeBlock | Should -Match '/cdc-capture-runtimes/\{executionRuntimeId\}/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardening' -Because "descriptor drilldowns should stay covered by the CDC runtime block"
        $cdcRuntimeBlock | Should -Match 'GetBooleanRouteValue' -Because "bool route values should be parsed from RouteValues instead of Minimal API parameter binding"
        $helperBlock | Should -Match 'Func<HttpContext, ICdcCaptureExecutionRuntimeCatalog, IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor>> selector' -Because "collection routes should execute typed catalog selectors behind a request delegate"
    }

    It "full ASP.NET Core read-only operator GET routes use request delegates instead of Minimal API delegate binding" {
        $sourcePath = Join-Path $script:repoRoot "src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs"
        $sourcePath = $sourcePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
        Test-Path -LiteralPath $sourcePath -PathType Leaf | Should -BeTrue

        $source = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
        $mapCephalonStart = $source.IndexOf("public static WebApplication MapCephalon(", [System.StringComparison]::Ordinal)
        $catalogStart = $source.IndexOf("MapCephalonFullCommonOperatorRoutes(engineGroup);", $mapCephalonStart, [System.StringComparison]::Ordinal)
        $hostInfrastructureStart = $source.IndexOf("MapCephalonHostInfrastructure(", $catalogStart, [System.StringComparison]::Ordinal)
        $helperStart = $source.IndexOf("private static void MapGetAsyncResultRequestDelegate(", [System.StringComparison]::Ordinal)

        $mapCephalonStart | Should -BeGreaterOrEqual 0 -Because "the full operator route catalog should stay parseable"
        $catalogStart | Should -BeGreaterThan $mapCephalonStart -Because "the full/common operator catalog should follow the core-mode branch"
        $hostInfrastructureStart | Should -BeGreaterThan $catalogStart -Because "host infrastructure should still follow operator routes"
        $helperStart | Should -BeGreaterThan $hostInfrastructureStart -Because "async request-delegate helper should follow the route catalog"

        $operatorRouteCatalog = $source.Substring($catalogStart, $hostInfrastructureStart - $catalogStart)
        $operatorRouteCatalog | Should -Not -Match 'engineGroup\.MapGet\(' -Because "all read-only operator GET routes should avoid Minimal API delegate binding"
        ([regex]::Matches($operatorRouteCatalog, 'engineGroup\.MapPost\(')).Count | Should -Be 6 -Because "bounded action POST seams remain explicit and unclaimed"
        $operatorRouteCatalog | Should -Match '/event-publications/runtime/\{publicationId\}' -Because "eventing runtime read routes should stay in the request-delegate catalog"
        $operatorRouteCatalog | Should -Match '/agent-tool-runs/by-tool/\{toolId\}' -Because "agentics read routes should stay in the request-delegate catalog"
        $operatorRouteCatalog | Should -Match '/audit-history/export' -Because "async audit-history export GET should stay in the request-delegate catalog"
        $operatorRouteCatalog | Should -Match '/strangler-fig/cutover/resolve' -Because "async strangler-fig resolve GETs should stay in the request-delegate catalog"
        $operatorRouteCatalog | Should -Match '/backend-for-frontend/rest-documents/\{documentId\}' -Because "BFF document read routes should stay in the request-delegate catalog"
        $operatorRouteCatalog | Should -Match '/cell-traffic-automations/health-isolations/\{healthIsolationId\}' -Because "cell automation read routes should stay in the request-delegate catalog"
        $operatorRouteCatalog | Should -Match '/knowledge-indexes/\{collectionId\}' -Because "knowledge-index read routes should stay in the request-delegate catalog"
        $source | Should -Match 'TryGetNullableDateTimeOffsetQueryValue' -Because "query-bound audit date filters should be parsed without Minimal API delegate binding"
    }
}

Describe "knownTransitiveHazards" {
    It "exists with a comment and per-mode lists" {
        $script:manifest.knownTransitiveHazards.PSObject.Properties.Name | Should -Contain 'comment'
        $script:manifest.knownTransitiveHazards.PSObject.Properties.Name | Should -Contain 'trim'
        $script:manifest.knownTransitiveHazards.PSObject.Properties.Name | Should -Contain 'nativeAot'
        $script:manifest.knownTransitiveHazards.PSObject.Properties.Name | Should -Contain 'singleFile'
    }
}

Describe "knownTransitiveHazardAudit" {
    It "exists with a comment, lock-file globs, and entries" {
        $script:manifest.knownTransitiveHazardAudit.PSObject.Properties.Name | Should -Contain 'comment'
        $script:manifest.knownTransitiveHazardAudit.PSObject.Properties.Name | Should -Contain 'lockFileGlobs'
        $script:manifest.knownTransitiveHazardAudit.PSObject.Properties.Name | Should -Contain 'entries'
        @($script:manifest.knownTransitiveHazardAudit.lockFileGlobs).Count | Should -BeGreaterThan 0
        @($script:manifest.knownTransitiveHazardAudit.entries).Count | Should -BeGreaterThan 0
    }

    It "uses valid entry shape and deployment modes" {
        foreach ($entry in @($script:manifest.knownTransitiveHazardAudit.entries)) {
            $entry.PSObject.Properties.Name | Should -Contain 'packagePattern'
            $entry.PSObject.Properties.Name | Should -Contain 'modes'
            $entry.PSObject.Properties.Name | Should -Contain 'minimumLockFileMatches'
            $entry.PSObject.Properties.Name | Should -Contain 'evidence'
            [string]$entry.packagePattern | Should -Not -BeNullOrEmpty
            [int]$entry.minimumLockFileMatches | Should -BeGreaterThan 0
            foreach ($mode in @($entry.modes)) {
                [string]$mode | Should -BeIn @('trim', 'nativeAot', 'singleFile')
            }
        }
    }

    It "matches the declared audited package patterns against current lock files" {
        $rows = @(Get-LockFilePackageRows -RepoRoot $script:repoRoot -LockFileGlobs @($script:manifest.knownTransitiveHazardAudit.lockFileGlobs))
        $rows.Count | Should -BeGreaterThan 0

        foreach ($entry in @($script:manifest.knownTransitiveHazardAudit.entries)) {
            $matcher = [System.Management.Automation.WildcardPattern]::new(
                [string]$entry.packagePattern,
                [System.Management.Automation.WildcardOptions]::IgnoreCase)
            $lockFileMatches = @(
                $rows |
                    Where-Object { $matcher.IsMatch($_.PackageId) } |
                    ForEach-Object { $_.LockFile } |
                    Sort-Object -Unique
            )
            ($lockFileMatches.Count -ge [int]$entry.minimumLockFileMatches) |
                Should -BeTrue -Because "packagePattern '$($entry.packagePattern)' should appear in at least $($entry.minimumLockFileMatches) audited lock file(s)"
        }
    }
}
