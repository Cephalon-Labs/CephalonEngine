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
$deploymentModeSupportManifestPath = [System.IO.Path]::Combine($repoRoot, "scripts", "deployment-mode-support.json")
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
            "EnableSingleFileAnalyzer",
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

function Get-PackageScopedDeploymentModeClaims {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Manifest
    )

    if ($null -eq $Manifest -or -not $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count) {
        return @()
    }

    $eligibility = $Manifest.deploymentModeEligibility
    if ($null -eq $eligibility -or -not $eligibility.PSObject.Properties.Match("packages").Count) {
        return @()
    }

    return @(
        foreach ($pkg in @($eligibility.packages)) {
            $supportedModes = @($pkg.supportedModes | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
            if ($supportedModes.Count -eq 0) {
                continue
            }

            $packageName = [string]$pkg.packageName
            [pscustomobject]@{
                PackageName = $packageName
                NugetId = [string]$pkg.nugetId
                ClaimAuditTier = [string]$pkg.claimAuditTier
                SupportedModes = $supportedModes
                Project = "src/$packageName/$packageName.csproj"
                RequiredProjectProperties = @($pkg.requiredProjectProperties)
            }
        }
    )
}

function Get-ScopedClaimProjectsForMode {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$PackageClaims,
        [Parameter(Mandatory = $true)]
        [string]$Mode
    )

    return @(
        $PackageClaims |
            Where-Object { @($_.SupportedModes) -contains $Mode } |
            ForEach-Object { [string]$_.Project }
    )
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
    if (-not (Test-Path -LiteralPath $deploymentModeSupportManifestPath)) {
        throw "Expected deployment-mode support manifest '$deploymentModeSupportManifestPath' was not found."
    }

    $deploymentModeSupport = Get-Content -LiteralPath $deploymentModeSupportManifestPath -Raw | ConvertFrom-Json
    $deploymentModeSupportManifestRelativePath = Get-RepoRelativePath -Path $deploymentModeSupportManifestPath
    $projectMetadata = Get-ChildItem -Path (Join-Path $repoRoot "src"), (Join-Path $repoRoot "tests"), (Join-Path $repoRoot "samples"), (Join-Path $repoRoot "templates"), (Join-Path $repoRoot "benchmarks") -Recurse -Filter *.csproj |
        Sort-Object FullName |
        ForEach-Object { Get-ProjectMetadata -Path $_.FullName }

    $shippingExceptions = @{
        "src/Cephalon.Analyzers/Cephalon.Analyzers.csproj" = "netstandard2.0"
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
    $packageScopedClaims = @(Get-PackageScopedDeploymentModeClaims -Manifest $deploymentModeSupport)
    $trimScopedClaimProjects = @(Get-ScopedClaimProjectsForMode -PackageClaims $packageScopedClaims -Mode "trim")
    $nativeAotScopedClaimProjects = @(Get-ScopedClaimProjectsForMode -PackageClaims $packageScopedClaims -Mode "nativeAot")
    $singleFileScopedClaimProjects = @(Get-ScopedClaimProjectsForMode -PackageClaims $packageScopedClaims -Mode "singleFile")
    $trimUnscopedClaimProjects = @($trimClaimProjects | Where-Object { $trimScopedClaimProjects -notcontains $_.Project })
    $nativeAotUnscopedClaimProjects = @($aotClaimProjects | Where-Object { $nativeAotScopedClaimProjects -notcontains $_.Project })
    $singleFileUnscopedClaimProjects = @($singleFileClaimProjects | Where-Object { $singleFileScopedClaimProjects -notcontains $_.Project })
    $trimAnalyzerProjects = @(
        $claimEntries |
            Where-Object { $_.Property -eq "EnableTrimAnalyzer" -and (Test-ExplicitTrue -Values $_.Values) }
    )
    $aotAnalyzerProjects = @(
        $claimEntries |
            Where-Object { $_.Property -eq "EnableAotAnalyzer" -and (Test-ExplicitTrue -Values $_.Values) }
    )
    $singleFileAnalyzerProjects = @(
        $claimEntries |
            Where-Object { $_.Property -eq "EnableSingleFileAnalyzer" -and (Test-ExplicitTrue -Values $_.Values) }
    )
    $trimDetectedStatus = if ($trimUnscopedClaimProjects.Count -eq 0) { "not-claimed" } else { "claimed" }
    $nativeAotDetectedStatus = if ($nativeAotUnscopedClaimProjects.Count -eq 0) { "not-claimed" } else { "claimed" }
    $singleFileDetectedStatus = if ($singleFileUnscopedClaimProjects.Count -eq 0) { "not-claimed" } else { "claimed" }

    $allowedDeploymentModeStatuses = @("not-claimed", "claimed")
    $expectedTrimStatus = [string]$deploymentModeSupport.deploymentModes.trim.status
    $expectedNativeAotStatus = [string]$deploymentModeSupport.deploymentModes.nativeAot.status
    $expectedSingleFileStatus = [string]$deploymentModeSupport.deploymentModes.singleFile.status

    foreach ($statusEntry in @(
        @{ Name = "trim"; Status = $expectedTrimStatus },
        @{ Name = "nativeAot"; Status = $expectedNativeAotStatus },
        @{ Name = "singleFile"; Status = $expectedSingleFileStatus }
    )) {
        if ($statusEntry.Status -notin $allowedDeploymentModeStatuses) {
            throw "Deployment-mode support manifest declares unsupported status '$($statusEntry.Status)' for '$($statusEntry.Name)'."
        }
    }

    $supportGuideRelativePath = [string]$deploymentModeSupport.documentation.guidePath
    $readinessGuideRelativePath = [string]$deploymentModeSupport.documentation.readinessGuidePath
    $compatibilityGuideRelativePath = [string]$deploymentModeSupport.documentation.compatibilityGuidePath
    $packagePublishingGuideRelativePath = [string]$deploymentModeSupport.documentation.packagePublishingGuidePath
    $supportGuidePaths = @(
        @{ Label = "support guide"; RelativePath = $supportGuideRelativePath },
        @{ Label = "readiness guide"; RelativePath = $readinessGuideRelativePath },
        @{ Label = "compatibility guide"; RelativePath = $compatibilityGuideRelativePath },
        @{ Label = "package-publishing guide"; RelativePath = $packagePublishingGuideRelativePath }
    )
    $missingSupportGuides = [System.Collections.Generic.List[string]]::new()
    foreach ($pathEntry in $supportGuidePaths) {
        if ([string]::IsNullOrWhiteSpace($pathEntry.RelativePath)) {
            $missingSupportGuides.Add("$($pathEntry.Label) path is empty in $deploymentModeSupportManifestRelativePath") | Out-Null
            continue
        }

        $resolvedGuidePath = Resolve-FullPath -Path $pathEntry.RelativePath
        if (-not (Test-Path -LiteralPath $resolvedGuidePath)) {
            $missingSupportGuides.Add("$($pathEntry.RelativePath) ($($pathEntry.Label))") | Out-Null
        }
    }

    $deploymentModeSupportMismatches = [System.Collections.Generic.List[string]]::new()
    foreach ($comparison in @(
        @{ Name = "trim"; Expected = $expectedTrimStatus; Actual = $trimDetectedStatus },
        @{ Name = "Native AOT"; Expected = $expectedNativeAotStatus; Actual = $nativeAotDetectedStatus },
        @{ Name = "single-file"; Expected = $expectedSingleFileStatus; Actual = $singleFileDetectedStatus }
    )) {
        if ($comparison.Expected -ne $comparison.Actual) {
            $deploymentModeSupportMismatches.Add("$($comparison.Name) manifest status '$($comparison.Expected)' does not match detected project status '$($comparison.Actual)'") | Out-Null
        }
    }

    $checks = [System.Collections.Generic.List[object]]::new()

    if ($unexpectedShippedSourceFrameworks.Count -eq 0) {
        $checks.Add((New-CheckResult -Name "shipping-baseline" -Status "pass" -Detail "All shipped src/Cephalon.* projects keep the expected net10.0 baseline, with Cephalon.Analyzers and Cephalon.Behaviors.SourceGen remaining on netstandard2.0.")) | Out-Null
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

    if ($missingSupportGuides.Count -eq 0) {
        $checks.Add((New-CheckResult -Name "deployment-mode-support-docs" -Status "pass" -Detail "Deployment-mode support manifest documentation paths resolve successfully.")) | Out-Null
    }
    else {
        $checks.Add((New-CheckResult -Name "deployment-mode-support-docs" -Status "fail" -Detail ($missingSupportGuides -join "; "))) | Out-Null
    }

    if ($deploymentModeSupportMismatches.Count -eq 0) {
        $checks.Add((New-CheckResult -Name "deployment-mode-support-contract" -Status "pass" -Detail "Project-detected deployment-mode statuses match scripts/deployment-mode-support.json.")) | Out-Null
    }
    else {
        $checks.Add((New-CheckResult -Name "deployment-mode-support-contract" -Status "fail" -Detail ($deploymentModeSupportMismatches -join "; "))) | Out-Null
    }

    $detectedDeploymentModeSummary = "Trim=$trimDetectedStatus; NativeAOT=$nativeAotDetectedStatus; SingleFile=$singleFileDetectedStatus"
    if ($deploymentModeSupportMismatches.Count -eq 0) {
        $checks.Add((New-CheckResult -Name "deployment-mode-claims" -Status "pass" -Detail "Detected deployment-mode claim status matches the repo support contract ($detectedDeploymentModeSummary).")) | Out-Null
    }
    else {
        $checks.Add((New-CheckResult -Name "deployment-mode-claims" -Status "fail" -Detail ("$detectedDeploymentModeSummary. " + ($deploymentModeSupportMismatches -join "; ")))) | Out-Null
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
            AnalyzerMetaPackageException = "src/Cephalon.Analyzers/Cephalon.Analyzers.csproj -> netstandard2.0"
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
        DeploymentModeSupport = [pscustomobject]@{
            ManifestPath = $deploymentModeSupportManifestRelativePath
            ShippingBaseline = [pscustomobject]@{
                StableTargetFramework = [string]$deploymentModeSupport.shippingBaseline.stableTargetFramework
                ReadinessLaneTargetFramework = [string]$deploymentModeSupport.shippingBaseline.readinessLaneTargetFramework
                ReadinessLaneStatus = [string]$deploymentModeSupport.shippingBaseline.readinessLaneStatus
            }
            Documentation = [pscustomobject]@{
                GuidePath = $supportGuideRelativePath
                ReadinessGuidePath = $readinessGuideRelativePath
                CompatibilityGuidePath = $compatibilityGuideRelativePath
                PackagePublishingGuidePath = $packagePublishingGuideRelativePath
            }
            AnalyzerOnlySignalsDoNotCount = [bool]$deploymentModeSupport.analyzerOnlySignalsDoNotCount
            SupportChangeRequirements = @($deploymentModeSupport.supportChangeRequirements)
            PackageScopedClaims = $packageScopedClaims
            DeploymentModes = [pscustomobject]@{
                Trim = [pscustomobject]@{
                    Status = $expectedTrimStatus
                    Summary = [string]$deploymentModeSupport.deploymentModes.trim.summary
                }
                NativeAot = [pscustomobject]@{
                    Status = $expectedNativeAotStatus
                    Summary = [string]$deploymentModeSupport.deploymentModes.nativeAot.summary
                }
                SingleFile = [pscustomobject]@{
                    Status = $expectedSingleFileStatus
                    Summary = [string]$deploymentModeSupport.deploymentModes.singleFile.summary
                }
            }
        }
        Claims = [pscustomobject]@{
            Trim = [pscustomobject]@{
                Status = $trimDetectedStatus
                SupportContractStatus = $expectedTrimStatus
                ClaimProjects = $trimUnscopedClaimProjects
                PackageScopedClaimProjects = $trimScopedClaimProjects
                AllClaimProjects = $trimClaimProjects
                AnalyzerProjects = $trimAnalyzerProjects
            }
            NativeAot = [pscustomobject]@{
                Status = $nativeAotDetectedStatus
                SupportContractStatus = $expectedNativeAotStatus
                ClaimProjects = $nativeAotUnscopedClaimProjects
                PackageScopedClaimProjects = $nativeAotScopedClaimProjects
                AllClaimProjects = $aotClaimProjects
                AnalyzerProjects = $aotAnalyzerProjects
            }
            SingleFile = [pscustomobject]@{
                Status = $singleFileDetectedStatus
                SupportContractStatus = $expectedSingleFileStatus
                ClaimProjects = $singleFileUnscopedClaimProjects
                PackageScopedClaimProjects = $singleFileScopedClaimProjects
                AllClaimProjects = $singleFileClaimProjects
                AnalyzerProjects = $singleFileAnalyzerProjects
            }
        }
        DeploymentModeClaimValidation = [pscustomobject]@{
            HarnessPath = 'scripts/validate-deployment-mode-claims.ps1'
            HarnessTestsPath = 'tests/Cephalon.Tests.Scripts/validate-deployment-mode-claims.Tests.ps1'
            ManifestSchemaTestsPath = 'tests/Cephalon.Tests.Scripts/deployment-mode-support-manifest.Tests.ps1'
            ExpectedReportDirectory = 'artifacts/deployment-mode-claims-release'
            ExpectedReportPath = 'artifacts/deployment-mode-claims-release/claim-validation-report.json'
            VerdictGate = 'claim-truthful is the only verdict that promotes a deployment mode from not-claimed to claimed'
            Note = 'Run scripts/validate-deployment-mode-claims.ps1 (or scripts/validate-release.ps1 without -SkipDeploymentModeClaims) to produce this report; this readiness pass does not invoke the harness.'
        }
        Checks = $checks
        Steps = $stepResults
    }

    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonReportPath -Encoding utf8

    $checkLines = @($checks | ForEach-Object { "- **$($_.Name)**: $($_.Status) — $($_.Detail)" })
    $stepLines = @($stepResults | ForEach-Object { "- **$($_.Name)**: $($_.Status) — $($_.Detail)" })
    $installedSdkLines = @($installedSdks | ForEach-Object { "- $_" })
    $deploymentModeSupportLines = @(
        ('- Source manifest: `{0}`' -f $report.DeploymentModeSupport.ManifestPath)
        ('- Support guide: `{0}`' -f $report.DeploymentModeSupport.Documentation.GuidePath)
        ('- Stable shipping floor: `{0}`' -f $report.DeploymentModeSupport.ShippingBaseline.StableTargetFramework)
        ('- Readiness lane target framework: `{0}` ({1})' -f $report.DeploymentModeSupport.ShippingBaseline.ReadinessLaneTargetFramework, $report.DeploymentModeSupport.ShippingBaseline.ReadinessLaneStatus)
        ('- Trim support contract: **{0}**' -f $report.DeploymentModeSupport.DeploymentModes.Trim.Status)
        ('- Native AOT support contract: **{0}**' -f $report.DeploymentModeSupport.DeploymentModes.NativeAot.Status)
        ('- Single-file support contract: **{0}**' -f $report.DeploymentModeSupport.DeploymentModes.SingleFile.Status)
        ('- Package-scoped claims: **{0}**' -f (@($report.DeploymentModeSupport.PackageScopedClaims).Count))
        '- Project-detected deployment-mode statuses must match the manifest before repo truth changes.'
    )

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
        '- Allowed exceptions remain `Cephalon.Analyzers`, `Cephalon.Behaviors.SourceGen`, and `Cephalon.TemplatePack`, all on `netstandard2.0`.'
        '- Starter template project files remain expected to target `net10.0` until an intentional migration lane changes repo truth.'
        ""
        "## Deployment-mode support contract"
        ""
        $deploymentModeSupportLines
        ""
        "## Deployment-mode claims"
        ""
        ('- Trim status: **{0}**' -f $report.Claims.Trim.Status)
        ('- Native AOT status: **{0}**' -f $report.Claims.NativeAot.Status)
        ('- Single-file status: **{0}**' -f $report.Claims.SingleFile.Status)
        '- Manifest-backed support statements and project-detected statuses now travel together in the readiness report.'
        '- Analyzer-only flags do not become support claims by themselves.'
        ""
        "## Deployment-mode claim validation harness"
        ""
        ('- Harness script: `{0}`' -f $report.DeploymentModeClaimValidation.HarnessPath)
        ('- Harness Pester suite: `{0}`' -f $report.DeploymentModeClaimValidation.HarnessTestsPath)
        ('- Manifest schema Pester suite: `{0}`' -f $report.DeploymentModeClaimValidation.ManifestSchemaTestsPath)
        ('- Report path when the harness runs: `{0}`' -f $report.DeploymentModeClaimValidation.ExpectedReportPath)
        ('- Verdict gate: {0}' -f $report.DeploymentModeClaimValidation.VerdictGate)
        '- This readiness pass does not invoke the harness; run `scripts/validate-deployment-mode-claims.ps1` directly or via `scripts/validate-release.ps1`.'
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
