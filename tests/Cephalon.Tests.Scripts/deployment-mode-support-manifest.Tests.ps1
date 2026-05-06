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
            'supportChangeRequirements',
            'deploymentModes',
            'representativePublishTargets',
            'expectedPublishOutputShape',
            'deploymentModeEligibility',
            'knownTransitiveHazards'
        )
        foreach ($field in $expected) {
            $script:manifest.PSObject.Properties.Name | Should -Contain $field
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
}

Describe "knownTransitiveHazards" {
    It "exists with a comment and per-mode lists" {
        $script:manifest.knownTransitiveHazards.PSObject.Properties.Name | Should -Contain 'comment'
        $script:manifest.knownTransitiveHazards.PSObject.Properties.Name | Should -Contain 'trim'
        $script:manifest.knownTransitiveHazards.PSObject.Properties.Name | Should -Contain 'nativeAot'
        $script:manifest.knownTransitiveHazards.PSObject.Properties.Name | Should -Contain 'singleFile'
    }
}
