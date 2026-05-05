#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester test suite for scripts/validate-deployment-mode-claims.ps1.

.DESCRIPTION
    Covers every public function in the harness plus the end-to-end Invoke-DeploymentModeClaimValidation
    integration path. The harness is dot-sourced under the CEPHALON_VALIDATE_DEPLOYMENT_MODE_NO_RUN
    environment guard so the main entry block does not fire during test loading.

.NOTES
    Run from the repo root:

        Invoke-Pester -Path tests/Cephalon.Tests.Scripts/validate-deployment-mode-claims.Tests.ps1 -Output Detailed

    or, when the broader test runner script lands, the release-validation flow will pick this file up
    via its glob. Tests intentionally avoid invoking real `dotnet publish` so they stay deterministic
    and fast; the publish probe is exercised through a stub command.
#>

BeforeAll {
    $env:CEPHALON_VALIDATE_DEPLOYMENT_MODE_NO_RUN = "1"

    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\validate-deployment-mode-claims.ps1"

    if (-not (Test-Path -LiteralPath $script:scriptPath)) {
        throw "Could not find harness script at $script:scriptPath"
    }

    . $script:scriptPath

    $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-validate-tests-$(Get-Random)"
    New-Item -Path $script:tempRoot -ItemType Directory -Force | Out-Null

    function script:New-TempManifest {
        param([Parameter(Mandatory)] $Object)
        $path = Join-Path $script:tempRoot "manifest-$(Get-Random).json"
        $Object | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path -Encoding UTF8
        return $path
    }

    function script:New-TempCsproj {
        param(
            [string]$Name = "Cephalon.Sample",
            [hashtable]$Properties = @{}
        )
        $path = Join-Path $script:tempRoot "$Name-$(Get-Random).csproj"
        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine('<Project Sdk="Microsoft.NET.Sdk">')
        [void]$sb.AppendLine('  <PropertyGroup>')
        foreach ($k in $Properties.Keys) {
            [void]$sb.AppendLine("    <$k>$($Properties[$k])</$k>")
        }
        [void]$sb.AppendLine('  </PropertyGroup>')
        [void]$sb.AppendLine('</Project>')
        Set-Content -LiteralPath $path -Value $sb.ToString() -Encoding UTF8
        return $path
    }

    function script:New-TempRepoRoot {
        param([hashtable[]]$Projects = @())
        $root = Join-Path $script:tempRoot "repo-$(Get-Random)"
        $src = Join-Path $root "src"
        New-Item -Path $src -ItemType Directory -Force | Out-Null
        $paths = @()
        foreach ($p in $Projects) {
            $name = $p.Name
            $dir = Join-Path $src $name
            New-Item -Path $dir -ItemType Directory -Force | Out-Null
            $csproj = Join-Path $dir "$name.csproj"
            $sb = [System.Text.StringBuilder]::new()
            [void]$sb.AppendLine('<Project Sdk="Microsoft.NET.Sdk">')
            [void]$sb.AppendLine('  <PropertyGroup>')
            foreach ($k in $p.Properties.Keys) {
                [void]$sb.AppendLine("    <$k>$($p.Properties[$k])</$k>")
            }
            [void]$sb.AppendLine('  </PropertyGroup>')
            [void]$sb.AppendLine('</Project>')
            Set-Content -LiteralPath $csproj -Value $sb.ToString() -Encoding UTF8
            $paths += $csproj
        }
        return [pscustomobject]@{ Root = $root; Projects = $paths }
    }
}

AfterAll {
    if ($script:tempRoot -and (Test-Path -LiteralPath $script:tempRoot)) {
        Remove-Item -LiteralPath $script:tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    Remove-Item Env:\CEPHALON_VALIDATE_DEPLOYMENT_MODE_NO_RUN -ErrorAction SilentlyContinue
}

Describe "Test-IsTruthyMsBuildValue" {
    It "treats lowercase 'true' as truthy" {
        Test-IsTruthyMsBuildValue -Value "true" | Should -BeTrue
    }
    It "treats PascalCase 'True' as truthy" {
        Test-IsTruthyMsBuildValue -Value "True" | Should -BeTrue
    }
    It "treats UPPER 'TRUE' as truthy" {
        Test-IsTruthyMsBuildValue -Value "TRUE" | Should -BeTrue
    }
    It "treats whitespace-padded ' true ' as truthy after trim" {
        Test-IsTruthyMsBuildValue -Value " true " | Should -BeTrue
    }
    It "treats 'false' as not truthy" {
        Test-IsTruthyMsBuildValue -Value "false" | Should -BeFalse
    }
    It "treats empty string as not truthy" {
        Test-IsTruthyMsBuildValue -Value "" | Should -BeFalse
    }
    It "treats null as not truthy" {
        Test-IsTruthyMsBuildValue -Value $null | Should -BeFalse
    }
    It "treats numeric '1' as not truthy (MSBuild only accepts 'true')" {
        Test-IsTruthyMsBuildValue -Value "1" | Should -BeFalse
    }
    It "treats arbitrary text as not truthy" {
        Test-IsTruthyMsBuildValue -Value "yes" | Should -BeFalse
    }
}

Describe "Read-DeploymentModeManifest" {
    Context "with a well-formed manifest" {
        It "returns an object whose deploymentModes field is preserved" {
            $path = New-TempManifest -Object @{
                deploymentModes = @{ trim = @{ status = "not-claimed" } }
            }
            $result = Read-DeploymentModeManifest -Path $path
            $result.deploymentModes.trim.status | Should -Be "not-claimed"
        }
    }

    Context "with a missing file" {
        It "throws a clear error" {
            { Read-DeploymentModeManifest -Path (Join-Path $script:tempRoot "does-not-exist.json") } |
                Should -Throw "*not found*"
        }
    }

    Context "with an empty file" {
        It "throws" {
            $path = Join-Path $script:tempRoot "empty.json"
            "" | Set-Content -LiteralPath $path -Encoding UTF8
            { Read-DeploymentModeManifest -Path $path } | Should -Throw "*empty*"
        }
    }

    Context "with malformed JSON" {
        It "throws with a parse error" {
            $path = Join-Path $script:tempRoot "malformed.json"
            "not valid json {{{{" | Set-Content -LiteralPath $path -Encoding UTF8
            { Read-DeploymentModeManifest -Path $path } | Should -Throw "*parse*"
        }
    }

    Context "with a JSON object missing deploymentModes" {
        It "throws" {
            $path = New-TempManifest -Object @{ shippingBaseline = @{ stableTargetFramework = "net10.0" } }
            { Read-DeploymentModeManifest -Path $path } | Should -Throw "*missing 'deploymentModes'*"
        }
    }
}

Describe "Get-DeploymentModeConfig" {
    It "returns config for trim with the correct property and analyzer fields" {
        $cfg = Get-DeploymentModeConfig -Mode "trim"
        $cfg.Mode | Should -Be "trim"
        $cfg.ProjectProperty | Should -Be "PublishTrimmed"
        $cfg.AnalyzerProperty | Should -Be "EnableTrimAnalyzer"
        $cfg.PublishArg | Should -Match "PublishTrimmed=true"
    }
    It "returns config for nativeAot" {
        $cfg = Get-DeploymentModeConfig -Mode "nativeAot"
        $cfg.ProjectProperty | Should -Be "PublishAot"
        $cfg.AnalyzerProperty | Should -Be "IsAotCompatible"
        $cfg.PublishArg | Should -Match "PublishAot=true"
    }
    It "returns config for singleFile" {
        $cfg = Get-DeploymentModeConfig -Mode "singleFile"
        $cfg.ProjectProperty | Should -Be "PublishSingleFile"
        $cfg.AnalyzerProperty | Should -Be "EnableSingleFileAnalyzer"
        $cfg.PublishArg | Should -Match "PublishSingleFile=true"
    }
    It "throws on unknown mode" {
        { Get-DeploymentModeConfig -Mode "selfDestruct" } | Should -Throw "*Unknown deployment mode*"
    }
}

Describe "Get-ManifestModeStatus" {
    BeforeAll {
        $script:manifest = [pscustomobject]@{
            deploymentModes = [pscustomobject]@{
                trim       = [pscustomobject]@{ status = "not-claimed" }
                nativeAot  = [pscustomobject]@{ status = "claimed" }
                singleFile = [pscustomobject]@{ summary = "no status field present" }
            }
        }
    }

    It "returns the status when the mode declares one" {
        Get-ManifestModeStatus -Manifest $script:manifest -Mode "trim" | Should -Be "not-claimed"
        Get-ManifestModeStatus -Manifest $script:manifest -Mode "nativeAot" | Should -Be "claimed"
    }
    It "returns null when the mode entry has no status field" {
        Get-ManifestModeStatus -Manifest $script:manifest -Mode "singleFile" | Should -BeNullOrEmpty
    }
    It "returns null when the mode is not present" {
        Get-ManifestModeStatus -Manifest $script:manifest -Mode "neverDefined" | Should -BeNullOrEmpty
    }
}

Describe "Get-DeploymentModeConfigFromManifest" {
    BeforeAll {
        # build a synthesized manifest with full schema 1.1.0 fields for trim
        $script:manifestFull = [pscustomobject]@{
            deploymentModes = [pscustomobject]@{
                trim       = [pscustomobject]@{
                    status                     = "claimed"
                    requiredProjectProperties  = @("PublishTrimmed", "ExtraProp")
                    requiredAnalyzerProperties = @("EnableTrimAnalyzer", "ExtraAnalyzer")
                    warningPatterns            = @("IL2026", "IL2099", "trim warning")
                }
                nativeAot  = [pscustomobject]@{ status = "not-claimed" }
                singleFile = [pscustomobject]@{ status = "not-claimed" }
            }
        }

        # manifest with mode entry but no schema 1.1.0 fields (legacy / pre-1.1.0)
        $script:manifestLegacy = [pscustomobject]@{
            deploymentModes = [pscustomobject]@{
                trim       = [pscustomobject]@{ status = "not-claimed" }
                nativeAot  = [pscustomobject]@{ status = "not-claimed" }
                singleFile = [pscustomobject]@{ status = "not-claimed" }
            }
        }

        # manifest with empty schema 1.1.0 arrays
        $script:manifestEmpty = [pscustomobject]@{
            deploymentModes = [pscustomobject]@{
                trim = [pscustomobject]@{
                    status                     = "claimed"
                    requiredProjectProperties  = @()
                    requiredAnalyzerProperties = @()
                    warningPatterns            = @()
                }
            }
        }
    }

    It "uses the manifest-driven ProjectProperty for trim when schema 1.1.0 fields are present" {
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $script:manifestFull -Mode "trim"
        $cfg.ProjectProperty | Should -Be "PublishTrimmed"
        $cfg.Source | Should -Be "manifest"
    }

    It "uses the manifest-driven AnalyzerProperty when schema 1.1.0 fields are present" {
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $script:manifestFull -Mode "trim"
        $cfg.AnalyzerProperty | Should -Be "EnableTrimAnalyzer"
    }

    It "joins manifest warningPatterns into a regex alternation (regex-escaped)" {
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $script:manifestFull -Mode "trim"
        # Each manifest pattern is regex-escaped before being joined into the alternation, so a
        # literal space in the manifest pattern becomes "\ " inside the joined regex. The test
        # asserts both the bare alphanumeric pattern (which has no escape effect) and the escaped
        # form of the multi-word pattern, so accidentally removing the escape pass would fail
        # this test as well as the dedicated regex-escape security test below.
        $cfg.WarningRegex | Should -Match "IL2026"
        $cfg.WarningRegex | Should -Match "IL2099"
        $cfg.WarningRegex | Should -Match "trim\\ warning"
        $cfg.WarningRegex | Should -BeLike "(?i)*"
    }

    It "always preserves the hardcoded DisplayName, PublishArg, and Mode" {
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $script:manifestFull -Mode "trim"
        $cfg.Mode | Should -Be "trim"
        $cfg.DisplayName | Should -Be "Trim"
        $cfg.PublishArg | Should -Match "PublishTrimmed=true"
    }

    It "falls back to hardcoded values when the manifest mode entry has no schema 1.1.0 fields" {
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $script:manifestLegacy -Mode "trim"
        $cfg.ProjectProperty | Should -Be "PublishTrimmed"
        $cfg.AnalyzerProperty | Should -Be "EnableTrimAnalyzer"
        $cfg.WarningRegex | Should -Match "IL2"
    }

    It "falls back to hardcoded values when manifest schema 1.1.0 arrays are empty" {
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $script:manifestEmpty -Mode "trim"
        $cfg.ProjectProperty | Should -Be "PublishTrimmed"
        $cfg.AnalyzerProperty | Should -Be "EnableTrimAnalyzer"
        $cfg.WarningRegex | Should -Match "IL2"
    }

    It "returns the hardcoded fallback when the manifest is null" {
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $null -Mode "trim"
        $cfg.ProjectProperty | Should -Be "PublishTrimmed"
        $cfg.AnalyzerProperty | Should -Be "EnableTrimAnalyzer"
    }

    It "returns hardcoded fallback when the manifest does not declare the mode" {
        $partial = [pscustomobject]@{ deploymentModes = [pscustomobject]@{} }
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $partial -Mode "nativeAot"
        $cfg.ProjectProperty | Should -Be "PublishAot"
        $cfg.AnalyzerProperty | Should -Be "IsAotCompatible"
    }

    It "throws on an unknown mode (delegates to the hardcoded Get-DeploymentModeConfig)" {
        { Get-DeploymentModeConfigFromManifest -Manifest $script:manifestFull -Mode "imaginary" } | Should -Throw "*Unknown deployment mode*"
    }

    It "regex-escapes manifest warning patterns so manifest edits cannot inject malformed regex" {
        $manifestWeird = [pscustomobject]@{
            deploymentModes = [pscustomobject]@{
                trim = [pscustomobject]@{
                    status                     = "claimed"
                    requiredProjectProperties  = @("PublishTrimmed")
                    requiredAnalyzerProperties = @("EnableTrimAnalyzer")
                    warningPatterns            = @("IL2026", "weird.literal[chars]")
                }
            }
        }
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $manifestWeird -Mode "trim"

        # Compare against the .NET regex escape that the harness applies to each manifest pattern
        # before joining the alternation. Use String.Contains so PowerShell wildcard / regex
        # metacharacters in the expected substring (e.g. `[`) do not get interpreted by Pester's
        # -BeLike or -Match operators. The security property under test is that a manifest pattern
        # with regex metacharacters is treated as a literal substring rather than as injectable
        # regex syntax.
        $expectedEscaped = [regex]::Escape("weird.literal[chars]")
        $cfg.WarningRegex.Contains($expectedEscaped) | Should -BeTrue -Because "manifest warning patterns must be regex-escaped before joining the alternation"

        # Also assert the regex compiles and treats the literal pattern as a literal match (not as
        # the `[chars]` character class it would have been if the escape had been skipped).
        $compiled = [regex]::new($cfg.WarningRegex)
        $compiled.IsMatch("weird.literal[chars]") | Should -BeTrue
        $compiled.IsMatch("weirdXliteralc") | Should -BeFalse
    }
}

Describe "Test-CsprojProperty" {
    It "finds a true-valued property" {
        $path = New-TempCsproj -Properties @{ PublishTrimmed = "true" }
        $result = Test-CsprojProperty -CsprojPath $path -Property "PublishTrimmed"
        $result.Found | Should -BeTrue
        $result.Truthy | Should -BeTrue
        $result.Value | Should -Be "true"
        $result.Error | Should -BeNullOrEmpty
    }

    It "reports Found=true but Truthy=false when the property is set to false" {
        $path = New-TempCsproj -Properties @{ PublishTrimmed = "false" }
        $result = Test-CsprojProperty -CsprojPath $path -Property "PublishTrimmed"
        $result.Found | Should -BeTrue
        $result.Truthy | Should -BeFalse
    }

    It "reports Found=false when the csproj does not declare the property" {
        $path = New-TempCsproj -Properties @{ TargetFramework = "net10.0" }
        $result = Test-CsprojProperty -CsprojPath $path -Property "PublishTrimmed"
        $result.Found | Should -BeFalse
        $result.Truthy | Should -BeFalse
        $result.Error | Should -BeNullOrEmpty
    }

    It "reports an Error for a missing csproj" {
        $result = Test-CsprojProperty -CsprojPath (Join-Path $script:tempRoot "nope.csproj") -Property "PublishTrimmed"
        $result.Found | Should -BeFalse
        $result.Error | Should -Match "not found"
    }

    It "reports an Error for malformed XML" {
        $path = Join-Path $script:tempRoot "broken-$(Get-Random).csproj"
        "<Project>< broken xml" | Set-Content -LiteralPath $path -Encoding UTF8
        $result = Test-CsprojProperty -CsprojPath $path -Property "PublishTrimmed"
        $result.Found | Should -BeFalse
        $result.Error | Should -Not -BeNullOrEmpty
    }
}

Describe "Get-ProjectPropertyAudit" {
    It "audits an explicit project list and computes counts correctly" {
        $repo = New-TempRepoRoot -Projects @(
            @{ Name = "A"; Properties = @{ PublishTrimmed = "true" } },
            @{ Name = "B"; Properties = @{ PublishTrimmed = "false" } },
            @{ Name = "C"; Properties = @{ TargetFramework = "net10.0" } }
        )
        $result = Get-ProjectPropertyAudit -RepoRoot $repo.Root -Property "PublishTrimmed" -ProjectPaths $repo.Projects
        $result.TotalScanned | Should -Be 3
        $result.PassedCount | Should -Be 1
        $result.FalsyCount | Should -Be 1
        $result.UnsetCount | Should -Be 1
        $result.ErrorCount | Should -Be 0
    }

    It "falls back to scanning $RepoRoot/src/Cephalon.*.csproj when ProjectPaths is empty" {
        $repo = New-TempRepoRoot -Projects @(
            @{ Name = "Cephalon.AlphaPack"; Properties = @{ PublishTrimmed = "true" } },
            @{ Name = "Cephalon.BetaPack"; Properties = @{ TargetFramework = "net10.0" } }
        )
        $result = Get-ProjectPropertyAudit -RepoRoot $repo.Root -Property "PublishTrimmed"
        $result.TotalScanned | Should -Be 2
        $result.PassedCount | Should -Be 1
    }

    It "reports zero counts for an empty repo" {
        $emptyRoot = Join-Path $script:tempRoot "empty-$(Get-Random)"
        New-Item -Path (Join-Path $emptyRoot "src") -ItemType Directory -Force | Out-Null
        $result = Get-ProjectPropertyAudit -RepoRoot $emptyRoot -Property "PublishTrimmed"
        $result.TotalScanned | Should -Be 0
        $result.PassedCount | Should -Be 0
    }
}

Describe "Get-AnalyzerAudit" {
    It "audits analyzer enablement across an explicit project list" {
        $repo = New-TempRepoRoot -Projects @(
            @{ Name = "X"; Properties = @{ EnableTrimAnalyzer = "true" } },
            @{ Name = "Y"; Properties = @{ EnableTrimAnalyzer = "false" } },
            @{ Name = "Z"; Properties = @{ TargetFramework = "net10.0" } }
        )
        $result = Get-AnalyzerAudit -RepoRoot $repo.Root -AnalyzerProperty "EnableTrimAnalyzer" -ProjectPaths $repo.Projects
        $result.TotalScanned | Should -Be 3
        $result.EnabledCount | Should -Be 1
        $result.DisabledOrMissingCount | Should -Be 2
    }
}

Describe "Invoke-PublishProbe" {
    BeforeAll {
        $script:cfgTrim = Get-DeploymentModeConfig -Mode "trim"

        $script:successStub = Join-Path $script:tempRoot "stub-success.ps1"
        @"
param([Parameter(ValueFromRemainingArguments)] `$rest)
Write-Output 'publish completed cleanly'
exit 0
"@ | Set-Content -LiteralPath $script:successStub -Encoding UTF8

        $script:warningStub = Join-Path $script:tempRoot "stub-warning.ps1"
        @"
param([Parameter(ValueFromRemainingArguments)] `$rest)
Write-Output 'IL2026: trim warning detected in TransitiveDep.dll'
Write-Output 'build succeeded with warnings'
exit 0
"@ | Set-Content -LiteralPath $script:warningStub -Encoding UTF8

        $script:errorStub = Join-Path $script:tempRoot "stub-error.ps1"
        @"
param([Parameter(ValueFromRemainingArguments)] `$rest)
Write-Output 'IL3050: error: AOT analysis failed for SomePackage'
Write-Output 'fatal: cannot publish'
exit 1
"@ | Set-Content -LiteralPath $script:errorStub -Encoding UTF8

        $script:lockMutatingStub = Join-Path $script:tempRoot "stub-lock-mutating.ps1"
        @"
param([Parameter(ValueFromRemainingArguments)] `$rest)
Set-Content -LiteralPath `$env:CEPHALON_TEST_LOCK_FILE -Value 'mutated by publish probe' -Encoding UTF8
exit 0
"@ | Set-Content -LiteralPath $script:lockMutatingStub -Encoding UTF8
    }

    It "returns Skipped=true when no targets are supplied" {
        $r = Invoke-PublishProbe -ModeConfig $script:cfgTrim -Targets @()
        $r.Skipped | Should -BeTrue
        $r.Reason | Should -Match "no publish targets"
    }

    It "captures success output cleanly with the success stub" {
        $r = Invoke-PublishProbe -ModeConfig $script:cfgTrim -Targets @("Cephalon.Engine.csproj") -DotnetCommand $script:successStub -RepoRoot $script:tempRoot
        $r.Skipped | Should -BeFalse
        $r.Targets.Count | Should -Be 1
        $r.Targets[0].ExitCode | Should -Be 0
        $r.Targets[0].ErrorCount | Should -Be 0
        $r.Targets[0].Success | Should -BeTrue
    }

    It "detects warnings via the mode-specific warning regex" {
        $r = Invoke-PublishProbe -ModeConfig $script:cfgTrim -Targets @("Cephalon.Engine.csproj") -DotnetCommand $script:warningStub -RepoRoot $script:tempRoot
        $r.Targets[0].WarningCount | Should -BeGreaterThan 0
        ($r.Targets[0].Warnings -join " ") | Should -Match "IL2026"
    }

    It "detects errors and reports Success=false" {
        $r = Invoke-PublishProbe -ModeConfig $script:cfgTrim -Targets @("Cephalon.Engine.csproj") -DotnetCommand $script:errorStub -RepoRoot $script:tempRoot
        $r.Targets[0].ExitCode | Should -Be 1
        $r.Targets[0].ErrorCount | Should -BeGreaterThan 0
        $r.Targets[0].Success | Should -BeFalse
    }

    It "aggregates over multiple targets" {
        $r = Invoke-PublishProbe -ModeConfig $script:cfgTrim -Targets @("A.csproj", "B.csproj") -DotnetCommand $script:successStub -RepoRoot $script:tempRoot
        $r.Targets.Count | Should -Be 2
        ($r.Targets | Where-Object { $_.Success }).Count | Should -Be 2
    }

    It "restores packages.lock.json files after publish probes" {
        $repoRoot = Join-Path $script:tempRoot "lock-restore-repo"
        New-Item -ItemType Directory -Path $repoRoot | Out-Null
        $lockFile = Join-Path $repoRoot "packages.lock.json"
        Set-Content -LiteralPath $lockFile -Value 'original lock content' -Encoding UTF8

        $env:CEPHALON_TEST_LOCK_FILE = $lockFile
        try {
            $r = Invoke-PublishProbe -ModeConfig $script:cfgTrim -Targets @("Cephalon.Engine.csproj") -DotnetCommand $script:lockMutatingStub -RepoRoot $repoRoot
            $r.Targets[0].Success | Should -BeTrue

            (Get-Content -LiteralPath $lockFile -Raw).Trim() | Should -Be "original lock content"
        }
        finally {
            Remove-Item Env:\CEPHALON_TEST_LOCK_FILE -ErrorAction SilentlyContinue
        }
    }
}

Describe "Compute-ModeVerdict" {
    BeforeAll {
        function script:New-PropertyAudit {
            param(
                [int]$PassedCount = 0,
                [int]$Total = 0,
                [string]$Property = "PublishTrimmed",
                [string[]]$PassedPaths = @()
            )
            $passed = @()
            if ($PassedPaths.Count -gt 0) {
                $passed = @($PassedPaths | ForEach-Object { [pscustomobject]@{ Path = $_; Property = $Property; Truthy = $true } })
            }
            elseif ($PassedCount -gt 0) {
                $passed = @(0..($PassedCount - 1) | ForEach-Object { [pscustomobject]@{ Path = "claiming-$_.csproj"; Property = $Property; Truthy = $true } })
            }
            [pscustomobject]@{
                Property = $Property
                TotalScanned = $Total
                PassedCount = $PassedCount
                UnsetCount = ($Total - $PassedCount)
                FalsyCount = 0
                ErrorCount = 0
                Passed = $passed
            }
        }
        function script:New-AnalyzerAudit {
            param([int]$EnabledCount = 0, [int]$Total = 0, [string]$AnalyzerProperty = "EnableTrimAnalyzer")
            [pscustomobject]@{
                AnalyzerProperty = $AnalyzerProperty
                TotalScanned = $Total
                EnabledCount = $EnabledCount
                DisabledOrMissingCount = ($Total - $EnabledCount)
            }
        }
        function script:New-PublishProbe {
            param([int]$Failures = 0, [int]$Warnings = 0, [int]$Total = 1, [bool]$Skipped = $false)
            $targets = @()
            for ($i = 0; $i -lt $Total; $i++) {
                $targets += [pscustomobject]@{
                    Target = "T$i.csproj"
                    ExitCode = if ($i -lt $Failures) { 1 } else { 0 }
                    WarningCount = if ($i -lt $Warnings) { 1 } else { 0 }
                    ErrorCount = if ($i -lt $Failures) { 1 } else { 0 }
                    Warnings = @()
                    Errors = @()
                    Success = (($i -ge $Failures))
                }
            }
            [pscustomobject]@{
                Mode = "trim"
                Skipped = $Skipped
                Reason = $null
                Targets = $targets
            }
        }
    }

    It "returns 'unknown' when manifest status is null or empty" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus $null `
            -PropertyAudit (New-PropertyAudit) -AnalyzerAudit (New-AnalyzerAudit) -PublishProbe (New-PublishProbe -Skipped $true)).Verdict |
            Should -Be "unknown"
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "" `
            -PropertyAudit (New-PropertyAudit) -AnalyzerAudit (New-AnalyzerAudit) -PublishProbe (New-PublishProbe -Skipped $true)).Verdict |
            Should -Be "unknown"
    }

    It "returns 'unknown' for a status the harness does not recognize" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "weird-value" `
            -PropertyAudit (New-PropertyAudit) -AnalyzerAudit (New-AnalyzerAudit) -PublishProbe (New-PublishProbe -Skipped $true)).Verdict |
            Should -Be "unknown"
    }

    It "returns 'not-claimed' when manifest is not-claimed and no project sets the property" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "not-claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 0) `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 0) `
            -PublishProbe (New-PublishProbe -Skipped $true)).Verdict |
            Should -Be "not-claimed"
    }

    It "returns 'not-claimed-with-property-drift' when manifest is not-claimed but a project DOES set the property" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "not-claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 1) `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 1) `
            -PublishProbe (New-PublishProbe -Skipped $true)).Verdict |
            Should -Be "not-claimed-with-property-drift"
    }

    It "keeps global 'not-claimed' when a project property is explained by a truthful package-scoped claim" {
        $projectPath = Join-Path $script:tempRoot "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj"
        $packageClaims = @(
            [pscustomobject]@{
                Mode = "singleFile"
                PackageName = "Cephalon.Diagnostics"
                ProjectPath = $projectPath
                Verdict = "claim-truthful"
                Reasons = @("package-scoped claim properties match manifest")
            }
        )

        (Compute-ModeVerdict -Mode "singleFile" -ManifestStatus "not-claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 1 -Property "PublishSingleFile" -PassedPaths @($projectPath)) `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 1 -AnalyzerProperty "EnableSingleFileAnalyzer") `
            -PublishProbe (New-PublishProbe -Skipped $true) `
            -PackageClaimAudits $packageClaims).Verdict |
            Should -Be "not-claimed"
    }

    It "returns 'claim-overstated' when a package-scoped claim fails its project-property audit" {
        $packageClaims = @(
            [pscustomobject]@{
                Mode = "singleFile"
                PackageName = "Cephalon.Diagnostics"
                ProjectPath = "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj"
                Verdict = "claim-overstated"
                Reasons = @("Cephalon.Diagnostics expected EnableSingleFileAnalyzer=true but the property was not declared")
            }
        )

        (Compute-ModeVerdict -Mode "singleFile" -ManifestStatus "not-claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 0 -Property "PublishSingleFile") `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 0 -AnalyzerProperty "EnableSingleFileAnalyzer") `
            -PublishProbe (New-PublishProbe -Skipped $true) `
            -PackageClaimAudits $packageClaims).Verdict |
            Should -Be "claim-overstated"
    }

    It "returns 'claim-truthful' when manifest claims the mode and every check passes" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 3) `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 3) `
            -PublishProbe (New-PublishProbe -Total 1 -Failures 0 -Warnings 0)).Verdict |
            Should -Be "claim-truthful"
    }

    It "returns 'claim-overstated' when manifest claims the mode but no project sets the property" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 0) `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 0) `
            -PublishProbe (New-PublishProbe -Total 1)).Verdict |
            Should -Be "claim-overstated"
    }

    It "returns 'claim-overstated' when claim is set but analyzers are not enabled on every claiming project" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 3) `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 1) `
            -PublishProbe (New-PublishProbe -Total 1)).Verdict |
            Should -Be "claim-overstated"
    }

    It "returns 'claim-overstated' when publish probe has failures" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 3) `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 3) `
            -PublishProbe (New-PublishProbe -Total 2 -Failures 1)).Verdict |
            Should -Be "claim-overstated"
    }

    It "returns 'claim-overstated' when publish probe has warnings" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "claimed" `
            -PropertyAudit (New-PropertyAudit -Total 5 -PassedCount 3) `
            -AnalyzerAudit (New-AnalyzerAudit -Total 5 -EnabledCount 3) `
            -PublishProbe (New-PublishProbe -Total 2 -Warnings 1)).Verdict |
            Should -Be "claim-overstated"
    }

    It "still returns 'not-claimed' when audit is skipped and manifest is not-claimed (no drift detection possible)" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "not-claimed" `
            -PropertyAudit $null -AnalyzerAudit $null -PublishProbe $null `
            -PropertyAuditSkipped:$true -AnalyzerSkipped:$true -PublishSkipped:$true).Verdict |
            Should -Be "not-claimed"
    }

    It "returns 'claim-overstated' when claim is set but property audit was skipped" {
        (Compute-ModeVerdict -Mode "trim" -ManifestStatus "claimed" `
            -PropertyAudit $null -AnalyzerAudit $null -PublishProbe $null `
            -PropertyAuditSkipped:$true).Verdict |
            Should -Be "claim-overstated"
    }
}

Describe "Compute-AggregateVerdict" {
    It "returns 'not-claimed' for an empty mode-verdict list" {
        Compute-AggregateVerdict -ModeVerdicts @() | Should -Be "not-claimed"
    }
    It "returns the single verdict when all modes agree" {
        $list = @(
            [pscustomobject]@{ Mode = "trim"; Verdict = "not-claimed" },
            [pscustomobject]@{ Mode = "nativeAot"; Verdict = "not-claimed" },
            [pscustomobject]@{ Mode = "singleFile"; Verdict = "not-claimed" }
        )
        Compute-AggregateVerdict -ModeVerdicts $list | Should -Be "not-claimed"
    }
    It "returns 'claim-overstated' if ANY mode is claim-overstated" {
        $list = @(
            [pscustomobject]@{ Mode = "trim"; Verdict = "claim-truthful" },
            [pscustomobject]@{ Mode = "nativeAot"; Verdict = "claim-overstated" },
            [pscustomobject]@{ Mode = "singleFile"; Verdict = "not-claimed" }
        )
        Compute-AggregateVerdict -ModeVerdicts $list | Should -Be "claim-overstated"
    }
    It "returns 'mixed' when truthful and not-claimed coexist without overstatement" {
        $list = @(
            [pscustomobject]@{ Mode = "trim"; Verdict = "claim-truthful" },
            [pscustomobject]@{ Mode = "nativeAot"; Verdict = "not-claimed" }
        )
        Compute-AggregateVerdict -ModeVerdicts $list | Should -Be "mixed"
    }
    It "returns 'unknown' if any mode verdict is unknown (and no overstatement)" {
        $list = @(
            [pscustomobject]@{ Mode = "trim"; Verdict = "unknown" },
            [pscustomobject]@{ Mode = "nativeAot"; Verdict = "not-claimed" }
        )
        Compute-AggregateVerdict -ModeVerdicts $list | Should -Be "unknown"
    }
    It "returns 'not-claimed-with-property-drift' when present without overstatement" {
        $list = @(
            [pscustomobject]@{ Mode = "trim"; Verdict = "not-claimed-with-property-drift" },
            [pscustomobject]@{ Mode = "nativeAot"; Verdict = "not-claimed" }
        )
        Compute-AggregateVerdict -ModeVerdicts $list | Should -Be "not-claimed-with-property-drift"
    }
    It "prefers claim-overstated over not-claimed-with-property-drift" {
        $list = @(
            [pscustomobject]@{ Mode = "trim"; Verdict = "not-claimed-with-property-drift" },
            [pscustomobject]@{ Mode = "nativeAot"; Verdict = "claim-overstated" }
        )
        Compute-AggregateVerdict -ModeVerdicts $list | Should -Be "claim-overstated"
    }
}

Describe "Write-ValidationReport" {
    It "creates the output directory if missing and writes JSON + Markdown" {
        $outDir = Join-Path $script:tempRoot "report-$(Get-Random)"
        $report = [pscustomobject]@{
            GeneratedAtUtc = "2026-05-02T00:00:00Z"
            DeploymentMode = "trim"
            Configuration = "Release"
            RepoRoot = $script:tempRoot
            ManifestPath = "manifest.json"
            ManifestSnapshot = @{}
            Modes = @()
            Verdicts = @(
                [pscustomobject]@{ Mode = "trim"; Verdict = "not-claimed"; Reasons = @("clean") }
            )
            HazardInventory = [pscustomobject]@{
                TotalPackages = 1
                PackagesWithKnownHazards = 1
                TotalKnownHazards = 2
                PackagesWithScopedClaims = 0
                TierCounts = @([pscustomobject]@{ Tier = "high"; Count = 1 })
                SupportedModeClaims = @([pscustomobject]@{ Mode = "trim"; PackageCount = 0; Packages = @() })
                KnownTransitiveHazards = @([pscustomobject]@{ Mode = "trim"; Count = 1; Entries = @("Newtonsoft.Json") })
            }
            AggregateVerdict = "not-claimed"
        }
        $paths = Write-ValidationReport -OutputDir $outDir -Report $report
        Test-Path -LiteralPath $paths.JsonPath | Should -BeTrue
        Test-Path -LiteralPath $paths.HazardInventoryPath | Should -BeTrue
        Test-Path -LiteralPath $paths.MarkdownPath | Should -BeTrue
        $json = Get-Content -LiteralPath $paths.JsonPath -Raw | ConvertFrom-Json
        $json.AggregateVerdict | Should -Be "not-claimed"
        $inventoryJson = Get-Content -LiteralPath $paths.HazardInventoryPath -Raw | ConvertFrom-Json
        $inventoryJson.TotalKnownHazards | Should -Be 2
        $md = Get-Content -LiteralPath $paths.MarkdownPath -Raw
        $md | Should -Match "# Deployment-mode claim validation report"
        $md | Should -Match "Aggregate verdict"
        $md | Should -Match "Hazard inventory"
    }
}

Describe "Test-CsprojPropertyExpectation" {
    It "matches a true-valued expected property" {
        $path = New-TempCsproj -Properties @{ PublishSingleFile = "true" }
        $result = Test-CsprojPropertyExpectation -CsprojPath $path -Entry "PublishSingleFile=true"
        $result.Property | Should -Be "PublishSingleFile"
        $result.ExpectedValue | Should -Be "true"
        $result.Matched | Should -BeTrue
    }

    It "matches a false-valued expected property" {
        $path = New-TempCsproj -Properties @{ PublishSingleFile = "false" }
        $result = Test-CsprojPropertyExpectation -CsprojPath $path -Entry "PublishSingleFile=false"
        $result.Matched | Should -BeTrue
    }

    It "defaults bare property names to true expectations" {
        $path = New-TempCsproj -Properties @{ EnableSingleFileAnalyzer = "true" }
        $result = Test-CsprojPropertyExpectation -CsprojPath $path -Entry "EnableSingleFileAnalyzer"
        $result.ExpectedValue | Should -Be "true"
        $result.Matched | Should -BeTrue
    }
}

Describe "Get-DeploymentModePackageClaimAudits" {
    It "returns claim-truthful for a clean-baseline package whose required properties match" {
        $repo = New-TempRepoRoot -Projects @(
            @{ Name = "Cephalon.Diagnostics"; Properties = @{ PublishSingleFile = "true"; EnableSingleFileAnalyzer = "true" } }
        )
        $manifest = [pscustomobject]@{
            deploymentModeEligibility = [pscustomobject]@{
                packages = @(
                    [pscustomobject]@{
                        packageName = "Cephalon.Diagnostics"
                        nugetId = "Cephalon.Diagnostics"
                        claimAuditTier = "clean-baseline"
                        supportedModes = @("singleFile")
                        requiredProjectProperties = @("PublishSingleFile=true", "EnableSingleFileAnalyzer=true")
                    }
                )
            }
        }

        $result = Get-DeploymentModePackageClaimAudits -Manifest $manifest -Mode "singleFile" -RepoRoot $repo.Root
        $result.Count | Should -Be 1
        $result[0].PackageName | Should -Be "Cephalon.Diagnostics"
        $result[0].Verdict | Should -Be "claim-truthful"
    }

    It "returns claim-overstated when a supported package is not clean-baseline" {
        $repo = New-TempRepoRoot -Projects @(
            @{ Name = "Cephalon.Engine"; Properties = @{ PublishSingleFile = "true"; EnableSingleFileAnalyzer = "true" } }
        )
        $manifest = [pscustomobject]@{
            deploymentModeEligibility = [pscustomobject]@{
                packages = @(
                    [pscustomobject]@{
                        packageName = "Cephalon.Engine"
                        nugetId = "Cephalon.Engine"
                        claimAuditTier = "high"
                        supportedModes = @("singleFile")
                        requiredProjectProperties = @("PublishSingleFile=true", "EnableSingleFileAnalyzer=true")
                    }
                )
            }
        }

        $result = Get-DeploymentModePackageClaimAudits -Manifest $manifest -Mode "singleFile" -RepoRoot $repo.Root
        $result[0].Verdict | Should -Be "claim-overstated"
        ($result[0].Reasons -join " ") | Should -Match "clean-baseline"
    }
}

Describe "Get-DeploymentModeHazardInventory" {
    It "emits tier, hazard, scoped-claim, and transitive-hazard counts from the manifest" {
        $manifest = [pscustomobject]@{
            '$schemaVersion' = "1.2.0"
            deploymentModeEligibility = [pscustomobject]@{
                packages = @(
                    [pscustomobject]@{
                        packageName = "Cephalon.Diagnostics"
                        nugetId = "Cephalon.Diagnostics"
                        claimAuditTier = "clean-baseline"
                        supportedModes = @("singleFile")
                        requiredProjectProperties = @("PublishSingleFile=true", "EnableSingleFileAnalyzer=true")
                        knownHazards = @()
                        introducedBy = "ENG-454"
                    },
                    [pscustomobject]@{
                        packageName = "Cephalon.Engine"
                        nugetId = "Cephalon.Engine"
                        claimAuditTier = "high"
                        supportedModes = @()
                        requiredProjectProperties = @()
                        knownHazards = @(
                            [pscustomobject]@{ kind = "reflection-assembly-scan"; site = "src/Cephalon.Engine/Composition/ModuleDiscovery.cs:113"; pattern = "Activator.CreateInstance"; remediation = "source generator" },
                            [pscustomobject]@{ kind = "reflection-method-invoke"; site = "src/Cephalon.Engine/Runtime/Sample.cs:42"; pattern = "Invoke"; remediation = "closed table" }
                        )
                        introducedBy = "ENG-426"
                    }
                )
            }
            knownTransitiveHazards = [pscustomobject]@{
                trim = @("Newtonsoft.Json")
                nativeAot = @("Grpc.AspNetCore", "Azure.Identity")
                singleFile = @("BenchmarkDotNet")
            }
        }

        $inventory = Get-DeploymentModeHazardInventory -Manifest $manifest

        $inventory.ManifestSchemaVersion | Should -Be "1.2.0"
        $inventory.TotalPackages | Should -Be 2
        $inventory.PackagesWithKnownHazards | Should -Be 1
        $inventory.TotalKnownHazards | Should -Be 2
        $inventory.PackagesWithScopedClaims | Should -Be 1
        ($inventory.TierCounts | Where-Object Tier -eq "high").Count | Should -Be 1
        ($inventory.TierCounts | Where-Object Tier -eq "clean-baseline").Count | Should -Be 1
        ($inventory.SupportedModeClaims | Where-Object Mode -eq "singleFile").PackageCount | Should -Be 1
        ($inventory.HazardKindCounts | Where-Object Kind -eq "reflection-assembly-scan").Count | Should -Be 1
        ($inventory.KnownTransitiveHazards | Where-Object Mode -eq "nativeAot").Count | Should -Be 2
    }

    It "returns an empty inventory when the manifest has no eligibility block" {
        $inventory = Get-DeploymentModeHazardInventory -Manifest ([pscustomobject]@{ deploymentModes = @{} })
        $inventory.TotalPackages | Should -Be 0
        $inventory.TotalKnownHazards | Should -Be 0
        $inventory.Packages.Count | Should -Be 0
    }
}

Describe "Invoke-DeploymentModeClaimValidation (integration)" {
    It "runs end-to-end against a not-claimed manifest and returns a not-claimed aggregate verdict" {
        $repo = New-TempRepoRoot -Projects @(
            @{ Name = "Cephalon.AlphaPack"; Properties = @{ TargetFramework = "net10.0" } }
        )
        $manifestPath = Join-Path $repo.Root "deployment-mode-support.json"
        @{
            shippingBaseline = @{ stableTargetFramework = "net10.0" }
            deploymentModes  = @{
                trim       = @{ status = "not-claimed" }
                nativeAot  = @{ status = "not-claimed" }
                singleFile = @{ status = "not-claimed" }
            }
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        $outDir = Join-Path $repo.Root "out"
        $result = Invoke-DeploymentModeClaimValidation `
            -DeploymentMode "all" `
            -ManifestPath $manifestPath `
            -OutputPath $outDir `
            -RepoRoot $repo.Root `
            -SkipPublish

        $result.Report.AggregateVerdict | Should -Be "not-claimed"
        $result.Report.HazardInventory.TotalPackages | Should -Be 0
        Test-Path -LiteralPath $result.Paths.JsonPath | Should -BeTrue
        Test-Path -LiteralPath $result.Paths.HazardInventoryPath | Should -BeTrue
        Test-Path -LiteralPath $result.Paths.MarkdownPath | Should -BeTrue
    }

    It "throws when the aggregate verdict is claim-overstated" {
        $repo = New-TempRepoRoot -Projects @(
            @{ Name = "Cephalon.AlphaPack"; Properties = @{ TargetFramework = "net10.0" } }
        )
        $manifestPath = Join-Path $repo.Root "deployment-mode-support.json"
        @{
            deploymentModes = @{
                trim       = @{ status = "claimed" }
                nativeAot  = @{ status = "not-claimed" }
                singleFile = @{ status = "not-claimed" }
            }
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        $outDir = Join-Path $repo.Root "out"
        { Invoke-DeploymentModeClaimValidation `
                -DeploymentMode "all" `
                -ManifestPath $manifestPath `
                -OutputPath $outDir `
                -RepoRoot $repo.Root `
                -SkipPublish } | Should -Throw "*claim-overstated*"
    }

    It "validates a single mode when -DeploymentMode trim is requested" {
        $repo = New-TempRepoRoot -Projects @(
            @{ Name = "Cephalon.AlphaPack"; Properties = @{ PublishTrimmed = "true"; EnableTrimAnalyzer = "true" } }
        )
        $manifestPath = Join-Path $repo.Root "deployment-mode-support.json"
        @{
            deploymentModes = @{
                trim       = @{ status = "claimed" }
                nativeAot  = @{ status = "not-claimed" }
                singleFile = @{ status = "not-claimed" }
            }
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

        $outDir = Join-Path $repo.Root "out"
        $result = Invoke-DeploymentModeClaimValidation `
            -DeploymentMode "trim" `
            -ManifestPath $manifestPath `
            -OutputPath $outDir `
            -RepoRoot $repo.Root `
            -SkipPublish

        $result.Report.Modes.Count | Should -Be 1
        $result.Report.Modes[0].Mode | Should -Be "trim"
        $result.Report.Modes[0].Verdict | Should -Be "claim-truthful"
    }
}
