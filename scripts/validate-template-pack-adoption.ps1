param(
    [string]$AppName = "Cephalon.TemplateAdoptionSmoke",
    [string]$HostUrl = "http://127.0.0.1:18082",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [string]$ReportPath = "artifacts/adoption-smoke/template-pack-adoption.json",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validationStartedAtUtc = [DateTimeOffset]::UtcNow
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-template-adoption-" + [Guid]::NewGuid().ToString("N"))
$packageFeedPath = Join-Path $tempRoot "package-feed"
$toolPath = Join-Path $tempRoot ".tools\cephalon"
$customHivePath = Join-Path $tempRoot ".template-hive"
$nuGetPackagesPath = Join-Path $tempRoot ".nuget\packages"
$workspaceRoot = Join-Path $tempRoot "workspace"
$generatedRoot = Join-Path $workspaceRoot $AppName
$generatedPackageFeedPath = Join-Path $generatedRoot ".cephalon\packages"
$stdoutLogPath = Join-Path $tempRoot "template-app.stdout.log"
$stderrLogPath = Join-Path $tempRoot "template-app.stderr.log"
$initialPackageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Analyzers/Cephalon.Analyzers.csproj",
    "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.Engine.SourceGen/Cephalon.Engine.SourceGen.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.Audit/Cephalon.Audit.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Cli/Cephalon.Cli.csproj",
    "src/Cephalon.Ids.Sfid/Cephalon.Ids.Sfid.csproj",
    "src/Cephalon.Observability/Cephalon.Observability.csproj",
    "src/Cephalon.Observability.OpenTelemetry/Cephalon.Observability.OpenTelemetry.csproj",
    "src/Cephalon.Observability.Serilog/Cephalon.Observability.Serilog.csproj",
    "src/Cephalon.ReferenceDocs/Cephalon.ReferenceDocs.csproj",
    "src/Cephalon.Resilience/Cephalon.Resilience.csproj",
    "src/Cephalon.Scaffolding/Cephalon.Scaffolding.csproj",
    "templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj"
)
$generatedPackageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Analyzers/Cephalon.Analyzers.csproj",
    "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.Engine.SourceGen/Cephalon.Engine.SourceGen.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.Audit/Cephalon.Audit.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Ids.Sfid/Cephalon.Ids.Sfid.csproj",
    "src/Cephalon.Observability/Cephalon.Observability.csproj",
    "src/Cephalon.Observability.OpenTelemetry/Cephalon.Observability.OpenTelemetry.csproj",
    "src/Cephalon.Resilience/Cephalon.Resilience.csproj",
    "src/Cephalon.Observability.Serilog/Cephalon.Observability.Serilog.csproj"
)
$cephalonExecutableFileName = if ($IsWindows) { "cephalon.exe" } else { "cephalon" }
$cephalonExecutablePath = Join-Path $toolPath $cephalonExecutableFileName

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

function Invoke-Cephalon {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & $cephalonExecutablePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Cephalon CLI command failed: $cephalonExecutablePath $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Stop-StaleToolingTestHosts {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $toolingBinRoot = [System.IO.Path]::Combine($RepoRoot, "tests", "Cephalon.Tests.Tooling", "bin")
    $toolingBinRoot = [System.IO.Path]::GetFullPath($toolingBinRoot)
    $trimChars = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) |
        Select-Object -Unique
    $normalizedToolingBinRoot = $toolingBinRoot.TrimEnd($trimChars) + [System.IO.Path]::DirectorySeparatorChar

    $staleTestHosts = @(Get-Process testhost -ErrorAction SilentlyContinue | Where-Object {
            $processPath = $_.Path
            if ([string]::IsNullOrWhiteSpace($processPath)) {
                return $false
            }

            $normalizedProcessPath = [System.IO.Path]::GetFullPath($processPath)
            return $normalizedProcessPath.StartsWith($normalizedToolingBinRoot, [System.StringComparison]::OrdinalIgnoreCase)
        })

    foreach ($staleTestHost in $staleTestHosts) {
        Stop-Process -Id $staleTestHost.Id -Force -ErrorAction SilentlyContinue
    }
}

function Resolve-GeneratedHostProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot,
        [Parameter(Mandatory = $true)]
        [string]$AppName
    )

    $projects = @(Get-ChildItem -Path $GeneratedRoot -Filter "*.csproj" -File | Sort-Object FullName)
    if ($projects.Count -eq 0) {
        throw "No generated host project was found at '$GeneratedRoot'."
    }

    $preferredProject = $projects | Where-Object { $_.BaseName -eq $AppName } | Select-Object -First 1
    if ($null -ne $preferredProject) {
        return $preferredProject.FullName
    }

    if ($projects.Count -eq 1) {
        return $projects[0].FullName
    }

    throw "Could not determine the generated host project at '$GeneratedRoot'."
}

function Wait-ForHttpSuccess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds,
        [System.Diagnostics.Process]$Process = $null
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if ($null -ne $Process -and $Process.HasExited) {
            throw "Generated host exited before '$Uri' became ready. Exit code: $($Process.ExitCode)."
        }

        try {
            $response = Invoke-WebRequest -Uri $Uri -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                Write-Host "Validated $Uri" -ForegroundColor Green
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    }
    while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for HTTP success from '$Uri'."
}

function Write-RecentLogs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Write-Host ""
    Write-Host "${Label}:" -ForegroundColor Yellow
    Get-Content -LiteralPath $Path -Tail 80
}

function Resolve-ReportPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function New-TemplatePackAdoptionRuntimeProbeRows {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status
    )

    @(
        [pscustomobject]([ordered]@{ Kind = "health"; Path = "/health/ready"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "engine-root"; Path = "/engine"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "snapshot"; Path = "/engine/snapshot"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "rest-docs"; Path = "/scalar"; Status = $Status })
    )
}

function New-TemplatePackAdoptionAssertionRows {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Passed
    )

    @(
        [pscustomobject]([ordered]@{ Name = "runsOutsideRepository"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "publishesLocalPackages"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "installsCliFromTemporaryFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "replaysMachineDoctor"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "installsTemplatePackIntoIsolatedHive"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "listsInstalledCephalonTemplates"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "replaysTemplateAwareDoctor"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "scaffoldsTemplatePackApp"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "seedsGeneratedLocalPackageFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "replaysGeneratedAppDoctor"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "restoresGeneratedHost"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "buildsGeneratedHost"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "runsGeneratedHost"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesOperatorSurfaces"; Passed = $Passed })
    )
}

function Write-TemplatePackAdoptionExecutionReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status,
        [string]$ErrorMessage = ""
    )

    $completedAtUtc = [DateTimeOffset]::UtcNow
    $passed = $Status.Equals("passed", [System.StringComparison]::OrdinalIgnoreCase)
    $runtimeProbeStatus = if ($passed) { "passed" } else { "not-run-or-failed" }
    $resolvedReportPath = Resolve-ReportPath -Path $ReportPath
    $reportDirectory = Split-Path -Parent $resolvedReportPath
    if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
        New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
    }

    $report = [pscustomobject]([ordered]@{
        '$schemaVersion' = "1.0.0"
        ScenarioId = "template-pack-dotnet-new-parity"
        Status = $Status
        AppName = $AppName
        HostUrl = $HostUrl
        Configuration = $Configuration
        SkipPackageBuild = [bool]$SkipPackageBuild
        KeepOutput = [bool]$KeepOutput
        StartedAtUtc = $validationStartedAtUtc.ToString("O")
        CompletedAtUtc = $completedAtUtc.ToString("O")
        DurationMilliseconds = [math]::Round(($completedAtUtc - $validationStartedAtUtc).TotalMilliseconds, 2)
        Assertions = New-TemplatePackAdoptionAssertionRows -Passed:$passed
        RuntimeProbes = New-TemplatePackAdoptionRuntimeProbeRows -Status $runtimeProbeStatus
        Paths = [pscustomobject]([ordered]@{
            TemporaryRoot = $tempRoot
            PackageFeed = $packageFeedPath
            ToolPath = $toolPath
            CustomHive = $customHivePath
            NuGetPackages = $nuGetPackagesPath
            WorkspaceRoot = $workspaceRoot
            GeneratedAppRoot = $generatedRoot
            GeneratedPackageFeed = $generatedPackageFeedPath
            HostProject = $hostProjectPath
            StdoutLog = $stdoutLogPath
            StderrLog = $stderrLogPath
            TemporaryOutputRetained = [bool]$KeepOutput
        })
        Error = $ErrorMessage
    })

    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedReportPath -Encoding UTF8
    Write-Host "Template-pack adoption execution report: $resolvedReportPath" -ForegroundColor Cyan
}

$process = $null
$hostProjectPath = $null
$previousNuGetPackages = $null
$previousTemplateHive = $null
$restoreRepoPackageAssets = $false

try {
    New-Item -ItemType Directory -Path $packageFeedPath -Force | Out-Null
    New-Item -ItemType Directory -Path $toolPath -Force | Out-Null
    New-Item -ItemType Directory -Path $customHivePath -Force | Out-Null
    New-Item -ItemType Directory -Path $nuGetPackagesPath -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null

    $previousNuGetPackages = $env:NUGET_PACKAGES
    $previousTemplateHive = $env:CEPHALON_DOCTOR_TEMPLATE_HIVE
    $env:NUGET_PACKAGES = $nuGetPackagesPath
    $restoreRepoPackageAssets = $true

    # Tooling test hosts can keep repo-local assemblies locked after focused validation runs.
    Stop-StaleToolingTestHosts -RepoRoot $repoRoot

    Write-Host ""
    Write-Host "Publishing repo-local Cephalon packages and template pack..." -ForegroundColor Cyan
    $publishPackageArguments = @{
        Configuration = $Configuration
        OutputPath = $packageFeedPath
        ProjectPaths = $initialPackageProjectPaths
    }
    if ($SkipPackageBuild) {
        $publishPackageArguments["SkipBuild"] = $true
    }

    & $publishPackagesScriptPath @publishPackageArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing to the temporary feed failed."
    }

    Write-Host ""
    Write-Host "Installing Cephalon CLI from the temporary package feed..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $workspaceRoot -Arguments @(
        "tool",
        "install",
        "--tool-path", $toolPath,
        "Cephalon.Cli",
        "--add-source", $packageFeedPath,
        "--ignore-failed-sources",
        "--no-cache",
        "--prerelease")

    if (-not (Test-Path -LiteralPath $cephalonExecutablePath -PathType Leaf)) {
        throw "Expected installed Cephalon CLI at '$cephalonExecutablePath'."
    }

    Write-Host ""
    Write-Host "Replaying machine-level doctor checks before template-pack install..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("doctor")

    Write-Host ""
    Write-Host "Installing Cephalon.TemplatePack into an isolated custom hive..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $workspaceRoot -Arguments @(
        "new",
        "install",
        "Cephalon.TemplatePack",
        "--nuget-source", $packageFeedPath,
        "--debug:custom-hive", $customHivePath)

    Write-Host ""
    Write-Host "Verifying the installed template-pack starter catalog..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $workspaceRoot -Arguments @(
        "new",
        "list",
        "cephalon",
        "--debug:custom-hive", $customHivePath)

    $env:CEPHALON_DOCTOR_TEMPLATE_HIVE = $customHivePath

    Write-Host ""
    Write-Host "Replaying doctor checks with template-pack availability enabled..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("doctor")

    Write-Host ""
    Write-Host "Scaffolding a fresh template-pack app outside the repository..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $workspaceRoot -Arguments @(
        "new",
        "cephalon-monolith",
        "-n", $AppName,
        "-o", $generatedRoot,
        "--debug:custom-hive", $customHivePath)

    $hostProjectPath = Resolve-GeneratedHostProjectPath -GeneratedRoot $generatedRoot -AppName $AppName

    Write-Host ""
    Write-Host "Seeding the generated local package feed..." -ForegroundColor Cyan
    & $publishPackagesScriptPath -Configuration $Configuration -OutputPath $generatedPackageFeedPath -SkipBuild -ProjectPaths $generatedPackageProjectPaths
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing to the generated local feed failed."
    }

    $generatedPackageReadmePath = Join-Path $generatedPackageFeedPath "README.md"
    if (-not (Test-Path -LiteralPath $generatedPackageReadmePath -PathType Leaf)) {
        throw "Expected generated local package-feed README at '$generatedPackageReadmePath' after seeding packages."
    }

    $generatedPackageReadme = Get-Content -LiteralPath $generatedPackageReadmePath -Raw
    if (-not $generatedPackageReadme.Contains("publish-package-artifacts.ps1", [System.StringComparison]::Ordinal)) {
        throw "Expected the generated local package-feed README to remain intact after seeding packages."
    }

    Write-Host ""
    Write-Host "Replaying generated-app doctor checks through the installed CLI..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("doctor", "--app-root", $generatedRoot)

    Write-Host ""
    Write-Host "Restoring the generated template-pack host..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("restore", $hostProjectPath)

    Write-Host ""
    Write-Host "Building the generated template-pack host..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("build", $hostProjectPath, "-c", $Configuration, "--no-restore")

    Write-Host ""
    Write-Host "Running the generated template-pack host..." -ForegroundColor Cyan
    $previousAspNetCoreUrls = $env:ASPNETCORE_URLS
    $previousDotNetEnvironment = $env:DOTNET_ENVIRONMENT
    try {
        $env:ASPNETCORE_URLS = $HostUrl
        $env:DOTNET_ENVIRONMENT = "Development"

        $process = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--project", $hostProjectPath, "-c", $Configuration, "--no-build") `
            -WorkingDirectory $generatedRoot `
            -RedirectStandardOutput $stdoutLogPath `
            -RedirectStandardError $stderrLogPath `
            -PassThru `
            -NoNewWindow
    }
    finally {
        $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
        $env:DOTNET_ENVIRONMENT = $previousDotNetEnvironment
    }

    Wait-ForHttpSuccess -Uri "$HostUrl/health/ready" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/scalar" -TimeoutSeconds $TimeoutSeconds -Process $process

    Write-TemplatePackAdoptionExecutionReport -Status "passed"

    Write-Host ""
    Write-Host "Template-pack adoption validation completed successfully." -ForegroundColor Green
    Write-Host "Temporary package feed: $packageFeedPath" -ForegroundColor Cyan
    Write-Host "Installed tool path: $toolPath" -ForegroundColor Cyan
    Write-Host "Template custom hive: $customHivePath" -ForegroundColor Cyan
    Write-Host "NuGet package cache: $nuGetPackagesPath" -ForegroundColor Cyan
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan
}
catch {
    $failureMessage = $_.Exception.Message
    Write-Host ""
    Write-Host "Template-pack adoption validation failed." -ForegroundColor Yellow
    try {
        Write-TemplatePackAdoptionExecutionReport -Status "failed" -ErrorMessage $failureMessage
    }
    catch {
        Write-Warning "Could not write template-pack adoption execution report: $($_.Exception.Message)"
    }

    Write-RecentLogs -Path $stdoutLogPath -Label "Generated host stdout"
    Write-RecentLogs -Path $stderrLogPath -Label "Generated host stderr"
    throw
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit()
    }

    $env:NUGET_PACKAGES = $previousNuGetPackages
    $env:CEPHALON_DOCTOR_TEMPLATE_HIVE = $previousTemplateHive

    if ($restoreRepoPackageAssets) {
        try {
            Write-Host ""
            Write-Host "Restoring repo package assets back to the default NuGet cache..." -ForegroundColor Cyan
            foreach ($packageProjectPath in $generatedPackageProjectPaths) {
                Invoke-DotNet -WorkingDirectory $repoRoot -Arguments @("restore", $packageProjectPath)
            }
        }
        catch {
            Write-Warning "Could not restore repo package assets back to the default NuGet cache: $($_.Exception.Message)"
        }
    }

    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
