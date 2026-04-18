param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "artifacts/dotnet-readiness-release",
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipReferenceDocs,
    [switch]$SkipPackages
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "CephalonEngine.slnx"
$referenceDocsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "publish-reference-docs.ps1")
$packageArtifactsScriptPath = [System.IO.Path]::Combine($repoRoot, "scripts", "publish-package-artifacts.ps1")
$globalJsonPath = [System.IO.Path]::Combine($repoRoot, "global.json")
$testProjectPaths = @(
    [System.IO.Path]::Combine($repoRoot, "tests", "Cephalon.Tests.Composition", "Cephalon.Tests.Composition.csproj"),
    [System.IO.Path]::Combine($repoRoot, "tests", "Cephalon.Tests.Hosting", "Cephalon.Tests.Hosting.csproj"),
    [System.IO.Path]::Combine($repoRoot, "tests", "Cephalon.Tests.Tooling", "Cephalon.Tests.Tooling.csproj")
)

function Get-DirectoryUri {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $trimChars = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) |
        Select-Object -Unique
    $normalizedPath = $fullPath.TrimEnd($trimChars) + [System.IO.Path]::DirectorySeparatorChar
    return [System.Uri]::new($normalizedPath)
}

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Get-RepoRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $repoRootUri = Get-DirectoryUri -Path $repoRoot
    $pathUri = [System.Uri]::new([System.IO.Path]::GetFullPath($Path))
    $relativePath = [System.Uri]::UnescapeDataString($repoRootUri.MakeRelativeUri($pathUri).ToString())
    return $relativePath.Replace([System.IO.Path]::DirectorySeparatorChar, '/').Replace([System.IO.Path]::AltDirectorySeparatorChar, '/')
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Action
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet command failed: dotnet $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Get-DotNetOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        $output = (& dotnet @Arguments 2>&1 | Out-String).Trim()
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet command failed: dotnet $($Arguments -join ' ')"
        }

        return $output
    }
    finally {
        Pop-Location
    }
}

function Get-PowerShellHostPath {
    try {
        $processPath = (Get-Process -Id $PID).Path
        if (-not [string]::IsNullOrWhiteSpace($processPath) -and (Test-Path -LiteralPath $processPath)) {
            return $processPath
        }
    }
    catch {
    }

    foreach ($candidate in @("pwsh", "powershell")) {
        $command = Get-Command -Name $candidate -CommandType Application -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
            return $command.Source
        }
    }

    throw "Unable to resolve the current PowerShell host executable."
}

function Invoke-PowerShellScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    $powerShellHostPath = Get-PowerShellHostPath

    Push-Location $WorkingDirectory
    try {
        & $powerShellHostPath -NoLogo -NoProfile -File $Path @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "PowerShell script failed: $Path $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Get-ProjectMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    [xml]$document = Get-Content -LiteralPath $Path
    $propertyGroups = @($document.Project.PropertyGroup)
    $targetFrameworks = [System.Collections.Generic.List[string]]::new()
    $legacyTargetFrameworkVersions = [System.Collections.Generic.List[string]]::new()
    $properties = [ordered]@{}

    foreach ($group in $propertyGroups) {
        $targetFrameworkNode = $group.PSObject.Properties["TargetFramework"]
        $targetFramework = if ($null -ne $targetFrameworkNode) { [string]$targetFrameworkNode.Value } else { $null }
        if (-not [string]::IsNullOrWhiteSpace($targetFramework)) {
            $targetFrameworks.Add($targetFramework.Trim())
        }

        $targetFrameworksNode = $group.PSObject.Properties["TargetFrameworks"]
        $targetFrameworkList = if ($null -ne $targetFrameworksNode) { [string]$targetFrameworksNode.Value } else { $null }
        if (-not [string]::IsNullOrWhiteSpace($targetFrameworkList)) {
            foreach ($framework in $targetFrameworkList.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries)) {
                $targetFrameworks.Add($framework.Trim())
            }
        }

        $legacyTargetFrameworkVersionNode = $group.PSObject.Properties["TargetFrameworkVersion"]
        $legacyTargetFrameworkVersion = if ($null -ne $legacyTargetFrameworkVersionNode) { [string]$legacyTargetFrameworkVersionNode.Value } else { $null }
        if (-not [string]::IsNullOrWhiteSpace($legacyTargetFrameworkVersion)) {
            $legacyTargetFrameworkVersions.Add($legacyTargetFrameworkVersion.Trim())
        }

        foreach ($propertyName in @(
            "IsTrimmable",
            "PublishTrimmed",
            "EnableTrimAnalyzer",
            "IsAotCompatible",
            "PublishAot",
            "EnableAotAnalyzer",
            "PublishSingleFile",
            "SelfContained",
            "TrimMode"
        )) {
            $propertyNode = $group.PSObject.Properties[$propertyName]
            if ($null -eq $propertyNode) {
                continue
            }

            $propertyValue = [string]$propertyNode.Value
            if ([string]::IsNullOrWhiteSpace($propertyValue)) {
                continue
            }

            if (-not $properties.Contains($propertyName)) {
                $properties[$propertyName] = [System.Collections.Generic.List[string]]::new()
            }

            $properties[$propertyName].Add($propertyValue.Trim())
        }
    }

    return [pscustomobject]@{
        Path = $Path
        RelativePath = Get-RepoRelativePath -Path $Path
        TargetFrameworks = @($targetFrameworks.ToArray() | Sort-Object -Unique)
        LegacyTargetFrameworkVersions = @($legacyTargetFrameworkVersions.ToArray() | Sort-Object -Unique)
        Properties = $properties
    }
}

function Test-ExplicitTrue {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Values
    )

    return @($Values | Where-Object { $_ -match '^(?i:true)$' }).Count -gt 0
}

function New-CheckResult {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Status,
        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    return [pscustomobject]@{
        Name = $Name
        Status = $Status
        Detail = $Detail
    }
}

function Add-StepResult {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Results,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Status,
        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    $Results.Add([pscustomobject]@{
        Name = $Name
        Status = $Status
        Detail = $Detail
    }) | Out-Null
}

$resolvedOutputPath = Resolve-FullPath -Path $OutputPath
$referenceDocsOutputPath = Join-Path $resolvedOutputPath "reference-docs"
$packageArtifactsOutputPath = Join-Path $resolvedOutputPath "packages"
$jsonReportPath = Join-Path $resolvedOutputPath "dotnet-readiness-report.json"
$markdownReportPath = Join-Path $resolvedOutputPath "README.md"
$dotNetWorkingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-dotnet-readiness-" + [Guid]::NewGuid().ToString("N"))
$stepResults = [System.Collections.Generic.List[object]]::new()

if (Test-Path -LiteralPath $resolvedOutputPath) {
    Get-ChildItem -LiteralPath $resolvedOutputPath -Force | Remove-Item -Recurse -Force
}
else {
    New-Item -ItemType Directory -Path $resolvedOutputPath -Force | Out-Null
}

New-Item -ItemType Directory -Path $dotNetWorkingDirectory -Force | Out-Null

try {
    foreach ($testProjectPath in $testProjectPaths) {
        if (-not (Test-Path -LiteralPath $testProjectPath)) {
            throw "Expected test project '$testProjectPath' was not found."
        }
    }

    $repoSelectedSdkVersion = Get-DotNetOutput -Arguments @("--version") -WorkingDirectory $repoRoot
    $readinessSelectedSdkVersion = Get-DotNetOutput -Arguments @("--version") -WorkingDirectory $dotNetWorkingDirectory
    $installedSdks = (Get-DotNetOutput -Arguments @("--list-sdks") -WorkingDirectory $dotNetWorkingDirectory).
        Split([Environment]::NewLine, [System.StringSplitOptions]::RemoveEmptyEntries)

    $globalJson = Get-Content -LiteralPath $globalJsonPath -Raw | ConvertFrom-Json
    $projectMetadata = Get-ChildItem -Path (Join-Path $repoRoot "src"), (Join-Path $repoRoot "tests"), (Join-Path $repoRoot "samples"), (Join-Path $repoRoot "templates"), (Join-Path $repoRoot "benchmarks") -Recurse -Filter *.csproj |
        Sort-Object FullName |
        ForEach-Object { Get-ProjectMetadata -Path $_.FullName }

    $shippingExceptions = @{
        "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj" = "netstandard2.0"
    }

    $shippedSourceProjects = @($projectMetadata | Where-Object { $_.RelativePath.StartsWith("src/", [System.StringComparison]::OrdinalIgnoreCase) })
    $starterTemplateProjects = @($projectMetadata | Where-Object { $_.RelativePath.StartsWith("templates/Cephalon.TemplatePack/templates/", [System.StringComparison]::OrdinalIgnoreCase) })
    $templatePackProject = $projectMetadata | Where-Object { $_.RelativePath -eq "templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj" }

    $unexpectedShippedSourceFrameworks = @(
        foreach ($project in $shippedSourceProjects) {
            $expectedFramework = if ($shippingExceptions.ContainsKey($project.RelativePath)) { $shippingExceptions[$project.RelativePath] } else { "net10.0" }
            if ($project.TargetFrameworks.Count -ne 1 -or $project.TargetFrameworks[0] -ne $expectedFramework) {
                [pscustomobject]@{
                    Project = $project.RelativePath
                    Expected = $expectedFramework
                    Actual = @($project.TargetFrameworks)
                }
            }
        }
    )

    $unexpectedStarterTemplateFrameworks = @(
        foreach ($project in $starterTemplateProjects) {
            if ($project.TargetFrameworks.Count -ne 1 -or $project.TargetFrameworks[0] -ne "net10.0") {
                [pscustomobject]@{
                    Project = $project.RelativePath
                    Expected = "net10.0"
                    Actual = @($project.TargetFrameworks)
                }
            }
        }
    )

    $legacyTargetFrameworkProjects = @($projectMetadata | Where-Object { $_.LegacyTargetFrameworkVersions.Count -gt 0 })

    $claimEntries = @(
        foreach ($project in $projectMetadata) {
            foreach ($propertyName in $project.Properties.Keys) {
                [pscustomobject]@{
                    Project = $project.RelativePath
                    Property = $propertyName
                    Values = @($project.Properties[$propertyName])
                }
            }
        }
    )

    $trimClaimProjects = @(
        $claimEntries |
            Where-Object {
                $_.Property -in @("IsTrimmable", "PublishTrimmed") -and
                (Test-ExplicitTrue -Values $_.Values)
            }
    )
    $aotClaimProjects = @(
        $claimEntries |
            Where-Object {
                $_.Property -in @("IsAotCompatible", "PublishAot") -and
                (Test-ExplicitTrue -Values $_.Values)
            }
    )
    $singleFileClaimProjects = @(
        $claimEntries |
            Where-Object {
                $_.Property -eq "PublishSingleFile" -and
                (Test-ExplicitTrue -Values $_.Values)
            }
    )
    $trimAnalyzerProjects = @(
        $claimEntries |
            Where-Object { $_.Property -eq "EnableTrimAnalyzer" -and (Test-ExplicitTrue -Values $_.Values) }
    )
    $aotAnalyzerProjects = @(
        $claimEntries |
            Where-Object { $_.Property -eq "EnableAotAnalyzer" -and (Test-ExplicitTrue -Values $_.Values) }
    )

    $checks = [System.Collections.Generic.List[object]]::new()

    if ($unexpectedShippedSourceFrameworks.Count -eq 0) {
        $checks.Add((New-CheckResult -Name "shipping-baseline" -Status "pass" -Detail "All shipped src/Cephalon.* projects keep the expected net10.0 baseline, with Cephalon.Behaviors.SourceGen remaining on netstandard2.0.")) | Out-Null
    }
    else {
        $details = @($unexpectedShippedSourceFrameworks | ForEach-Object { "$($_.Project) expected '$($_.Expected)' but found '$(@($_.Actual) -join ', ')'" }) -join "; "
        $checks.Add((New-CheckResult -Name "shipping-baseline" -Status "fail" -Detail $details)) | Out-Null
    }

    if ($null -ne $templatePackProject -and $templatePackProject.TargetFrameworks.Count -eq 1 -and $templatePackProject.TargetFrameworks[0] -eq "netstandard2.0") {
        $checks.Add((New-CheckResult -Name "template-pack-baseline" -Status "pass" -Detail "Cephalon.TemplatePack remains on netstandard2.0 as the intentional packaging exception.")) | Out-Null
    }
    else {
        $actual = if ($null -eq $templatePackProject) { "<missing>" } else { @($templatePackProject.TargetFrameworks) -join ", " }
        $checks.Add((New-CheckResult -Name "template-pack-baseline" -Status "fail" -Detail "Cephalon.TemplatePack is expected to target netstandard2.0 but currently reports '$actual'.")) | Out-Null
    }

    if ($unexpectedStarterTemplateFrameworks.Count -eq 0) {
        $checks.Add((New-CheckResult -Name "starter-template-baseline" -Status "pass" -Detail "All starter template project files still target net10.0.")) | Out-Null
    }
    else {
        $details = @($unexpectedStarterTemplateFrameworks | ForEach-Object { "$($_.Project) expected 'net10.0' but found '$(@($_.Actual) -join ', ')'" }) -join "; "
        $checks.Add((New-CheckResult -Name "starter-template-baseline" -Status "fail" -Detail $details)) | Out-Null
    }

    if ($legacyTargetFrameworkProjects.Count -eq 0) {
        $checks.Add((New-CheckResult -Name "legacy-target-framework-version" -Status "pass" -Detail "No project files use the legacy TargetFrameworkVersion property.")) | Out-Null
    }
    else {
        $details = @($legacyTargetFrameworkProjects | ForEach-Object { "$($_.RelativePath): $(@($_.LegacyTargetFrameworkVersions) -join ', ')" }) -join "; "
        $checks.Add((New-CheckResult -Name "legacy-target-framework-version" -Status "fail" -Detail $details)) | Out-Null
    }

    if ($trimClaimProjects.Count -eq 0 -and $aotClaimProjects.Count -eq 0 -and $singleFileClaimProjects.Count -eq 0) {
        $checks.Add((New-CheckResult -Name "deployment-mode-claims" -Status "pass" -Detail "No shipped project currently asserts trim, Native AOT, or single-file support as repo truth.")) | Out-Null
    }
    else {
        $details = [System.Collections.Generic.List[string]]::new()
        foreach ($entry in @($trimClaimProjects + $aotClaimProjects + $singleFileClaimProjects)) {
            $details.Add("$($entry.Project) sets $($entry.Property)=$(@($entry.Values) -join ', ')") | Out-Null
        }

        $checks.Add((New-CheckResult -Name "deployment-mode-claims" -Status "fail" -Detail ($details -join "; "))) | Out-Null
    }

    if (-not $SkipBuild) {
        Invoke-Step "Build solution with readiness SDK selection" {
            Invoke-DotNet -Arguments @("build", $solutionPath, "-c", $Configuration) -WorkingDirectory $dotNetWorkingDirectory
        }

        Add-StepResult -Results $stepResults -Name "build" -Status "pass" -Detail "Built CephalonEngine.slnx from '$dotNetWorkingDirectory' so repo global.json did not pin the readiness SDK selection."
    }
    else {
        Add-StepResult -Results $stepResults -Name "build" -Status "skipped" -Detail "Skipped by request."
    }

    if (-not $SkipTests) {
        Invoke-Step "Run split test projects with readiness SDK selection" {
            foreach ($testProjectPath in $testProjectPaths) {
                $arguments = @("test", $testProjectPath, "-c", $Configuration)
                if (-not $SkipBuild) {
                    $arguments += @("--no-build", "--no-restore")
                }

                Invoke-DotNet -Arguments $arguments -WorkingDirectory $dotNetWorkingDirectory
            }
        }

        Add-StepResult -Results $stepResults -Name "tests" -Status "pass" -Detail ("Executed test projects: " + (@($testProjectPaths | ForEach-Object { Get-RepoRelativePath -Path $_ }) -join ", "))
    }
    else {
        Add-StepResult -Results $stepResults -Name "tests" -Status "skipped" -Detail "Skipped by request."
    }

    if (-not $SkipReferenceDocs) {
        Invoke-Step "Publish reference docs with readiness SDK selection" {
            $arguments = @(
                "-Configuration", $Configuration,
                "-OutputPath", $referenceDocsOutputPath,
                "-DotNetWorkingDirectory", $dotNetWorkingDirectory
            )

            if (-not $SkipBuild) {
                $arguments += "-SkipBuild"
            }

            Invoke-PowerShellScript -Path $referenceDocsScriptPath -Arguments $arguments -WorkingDirectory $dotNetWorkingDirectory
        }

        Add-StepResult -Results $stepResults -Name "reference-docs" -Status "pass" -Detail "Published reference docs into '$referenceDocsOutputPath'."
    }
    else {
        Add-StepResult -Results $stepResults -Name "reference-docs" -Status "skipped" -Detail "Skipped by request."
    }

    if (-not $SkipPackages) {
        Invoke-Step "Publish package artifacts with readiness SDK selection" {
            $arguments = @(
                "-Configuration", $Configuration,
                "-OutputPath", $packageArtifactsOutputPath,
                "-DotNetWorkingDirectory", $dotNetWorkingDirectory
            )

            if (-not $SkipBuild) {
                $arguments += "-SkipBuild"
            }

            Invoke-PowerShellScript -Path $packageArtifactsScriptPath -Arguments $arguments -WorkingDirectory $dotNetWorkingDirectory
        }

        Add-StepResult -Results $stepResults -Name "packages" -Status "pass" -Detail "Published package artifacts into '$packageArtifactsOutputPath'."
    }
    else {
        Add-StepResult -Results $stepResults -Name "packages" -Status "skipped" -Detail "Skipped by request."
    }

    $report = [pscustomobject]@{
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        Configuration = $Configuration
        GlobalJson = [pscustomobject]@{
            Version = $globalJson.sdk.version
            RollForward = $globalJson.sdk.rollForward
        }
        SdkSelection = [pscustomobject]@{
            RepoSelectedVersion = $repoSelectedSdkVersion
            ReadinessSelectedVersion = $readinessSelectedSdkVersion
            ReadinessWorkingDirectory = $dotNetWorkingDirectory
            InstalledSdks = $installedSdks
        }
        ShippingBaseline = [pscustomobject]@{
            StableTargetFramework = "net10.0"
            SourceGeneratorException = "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj -> netstandard2.0"
            TemplatePackException = "templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj -> netstandard2.0"
            UnexpectedShippedSourceFrameworks = $unexpectedShippedSourceFrameworks
            UnexpectedStarterTemplateFrameworks = $unexpectedStarterTemplateFrameworks
            LegacyTargetFrameworkProjects = @($legacyTargetFrameworkProjects | ForEach-Object {
                [pscustomobject]@{
                    Project = $_.RelativePath
                    Values = $_.LegacyTargetFrameworkVersions
                }
            })
        }
        Claims = [pscustomobject]@{
            Trim = [pscustomobject]@{
                Status = if ($trimClaimProjects.Count -eq 0) { "not-claimed" } else { "claimed" }
                ClaimProjects = $trimClaimProjects
                AnalyzerProjects = $trimAnalyzerProjects
            }
            NativeAot = [pscustomobject]@{
                Status = if ($aotClaimProjects.Count -eq 0) { "not-claimed" } else { "claimed" }
                ClaimProjects = $aotClaimProjects
                AnalyzerProjects = $aotAnalyzerProjects
            }
            SingleFile = [pscustomobject]@{
                Status = if ($singleFileClaimProjects.Count -eq 0) { "not-claimed" } else { "claimed" }
                ClaimProjects = $singleFileClaimProjects
            }
        }
        Checks = $checks
        Steps = $stepResults
    }

    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonReportPath -Encoding utf8

    $checkLines = @($checks | ForEach-Object { "- **$($_.Name)**: $($_.Status) — $($_.Detail)" })
    $stepLines = @($stepResults | ForEach-Object { "- **$($_.Name)**: $($_.Status) — $($_.Detail)" })
    $installedSdkLines = @($installedSdks | ForEach-Object { "- $_" })

    $markdown = @(
        "# Cephalon .NET Readiness Report"
        ""
        "Generated at: $($report.GeneratedAtUtc)"
        ""
        "## SDK selection"
        ""
        ('- Repo-selected SDK (`global.json`-aware): `{0}`' -f $repoSelectedSdkVersion)
        ('- Readiness SDK selection (temp working directory): `{0}`' -f $readinessSelectedSdkVersion)
        ('- `global.json`: version `{0}`, roll-forward `{1}`' -f $globalJson.sdk.version, $globalJson.sdk.rollForward)
        ""
        "Installed SDKs:"
        ""
        $installedSdkLines
        ""
        "## Shipping baseline"
        ""
        '- Stable shipping floor remains `net10.0`.'
        '- Allowed exceptions remain `Cephalon.Behaviors.SourceGen` and `Cephalon.TemplatePack`, both on `netstandard2.0`.'
        '- Starter template project files remain expected to target `net10.0` until an intentional migration lane changes repo truth.'
        ""
        "## Deployment-mode claims"
        ""
        ('- Trim status: **{0}**' -f $report.Claims.Trim.Status)
        ('- Native AOT status: **{0}**' -f $report.Claims.NativeAot.Status)
        ('- Single-file status: **{0}**' -f $report.Claims.SingleFile.Status)
        '- Analyzer-only flags do not become support claims by themselves.'
        ""
        "## Checks"
        ""
        $checkLines
        ""
        "## Executed steps"
        ""
        $stepLines
    ) -join [Environment]::NewLine

    $markdown | Set-Content -LiteralPath $markdownReportPath -Encoding utf8

    $failedChecks = @($checks | Where-Object { $_.Status -eq "fail" })
    if ($failedChecks.Count -gt 0) {
        throw ".NET readiness validation found $($failedChecks.Count) failing contract check(s). See '$jsonReportPath' for details."
    }

    Write-Host ""
    Write-Host "Dotnet readiness validation completed successfully." -ForegroundColor Green
}
finally {
    if (Test-Path -LiteralPath $dotNetWorkingDirectory) {
        Remove-Item -LiteralPath $dotNetWorkingDirectory -Recurse -Force
    }
}
