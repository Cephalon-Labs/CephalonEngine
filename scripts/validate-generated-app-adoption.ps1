param(
    [string]$AppName = "Cephalon.AdoptionSmoke",
    [string]$HostUrl = "http://127.0.0.1:18081",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [string]$ReportPath = "artifacts/adoption-smoke/generated-app-adoption.json",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validationStartedAtUtc = [DateTimeOffset]::UtcNow
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'generated-app-runtime-contract.ps1')
$runtimeContractEvidence = $null
$toolchainEvidence = [ordered]@{
    SourceRevision = ''; WorkingTreeDirty = $true; RepositorySdk = ''; GeneratedHostSdk = ''
    TargetFramework = 'net10.0'; Os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
    RuntimeIdentifier = [Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
}
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-adoption-" + [Guid]::NewGuid().ToString("N"))
$packageFeedPath = Join-Path $tempRoot "package-feed"
$toolPath = Join-Path $tempRoot ".tools\cephalon"
$nuGetPackagesPath = Join-Path $tempRoot ".nuget\packages"
$workspaceRoot = Join-Path $tempRoot "workspace"
$generatedRoot = Join-Path $workspaceRoot $AppName
$generatedPackageFeedPath = Join-Path $generatedRoot ".cephalon\packages"
$stdoutLogPath = Join-Path $tempRoot "generated-app.stdout.log"
$stderrLogPath = Join-Path $tempRoot "generated-app.stderr.log"
$packageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Analyzers/Cephalon.Analyzers.csproj",
    "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.Engine.SourceGen/Cephalon.Engine.SourceGen.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Resilience/Cephalon.Resilience.csproj",
    "src/Cephalon.Cli/Cephalon.Cli.csproj",
    "src/Cephalon.Observability/Cephalon.Observability.csproj",
    "src/Cephalon.Observability.OpenTelemetry/Cephalon.Observability.OpenTelemetry.csproj",
    "src/Cephalon.Observability.Serilog/Cephalon.Observability.Serilog.csproj",
    "src/Cephalon.ReferenceDocs/Cephalon.ReferenceDocs.csproj",
    "src/Cephalon.Scaffolding/Cephalon.Scaffolding.csproj"
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

function Resolve-GeneratedHostProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot,
        [Parameter(Mandatory = $true)]
        [string]$AppName
    )

    $sourceRoot = Join-Path $GeneratedRoot "src"
    $projects = @(Get-ChildItem -Path $sourceRoot -Recurse -Filter "*.csproj" -File | Sort-Object FullName)
    if ($projects.Count -eq 0) {
        throw "No generated host project was found under '$sourceRoot'."
    }

    $preferredProject = @(
        $projects | Where-Object { $_.BaseName -eq "$AppName.Host" } | Select-Object -First 1
        $projects | Where-Object { $_.BaseName -eq "$AppName.Service" } | Select-Object -First 1
        $projects | Where-Object { $_.Name.EndsWith(".Host.csproj", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        $projects | Where-Object { $_.Name.EndsWith(".Service.csproj", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
    ) | Where-Object { $null -ne $_ } | Select-Object -First 1

    if ($null -ne $preferredProject) {
        return $preferredProject.FullName
    }

    if ($projects.Count -eq 1) {
        return $projects[0].FullName
    }

    throw "Could not determine the generated host project under '$sourceRoot'."
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

function New-GeneratedAppAdoptionRuntimeProbeRows {
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

function New-GeneratedAppAdoptionAssertionRows {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Passed
    )

    @(
        [pscustomobject]([ordered]@{ Name = "runsOutsideRepository"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "publishesLocalPackages"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "installsCliFromTemporaryFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "replaysMachineDoctor"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "scaffoldsGeneratedApp"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "seedsGeneratedLocalPackageFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "replaysGeneratedAppDoctor"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "restoresGeneratedSolution"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "buildsGeneratedSolution"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "runsGeneratedHost"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesOperatorSurfaces"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesManifestV2AndSnapshot"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "matchesGeneratedConfiguration"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesCapabilitySources"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesStartedRuntime"; Passed = $Passed })
    )
}

function Write-GeneratedAppAdoptionExecutionReport {
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
        '$schemaVersion' = "1.1.0"
        ScenarioId = "generated-app-runtime-foundation"
        Status = $Status
        AppName = $AppName
        HostUrl = $HostUrl
        Configuration = $Configuration
        SkipPackageBuild = [bool]$SkipPackageBuild
        KeepOutput = [bool]$KeepOutput
        StartedAtUtc = $validationStartedAtUtc.ToString("O")
        CompletedAtUtc = $completedAtUtc.ToString("O")
        DurationMilliseconds = [math]::Round(($completedAtUtc - $validationStartedAtUtc).TotalMilliseconds, 2)
        Assertions = New-GeneratedAppAdoptionAssertionRows -Passed:$passed
        RuntimeProbes = New-GeneratedAppAdoptionRuntimeProbeRows -Status $runtimeProbeStatus
        RuntimeContract = $runtimeContractEvidence
        Toolchain = $toolchainEvidence
        Paths = [pscustomobject]([ordered]@{
            TemporaryRoot = $tempRoot
            PackageFeed = $packageFeedPath
            ToolPath = $toolPath
            NuGetPackages = $nuGetPackagesPath
            WorkspaceRoot = $workspaceRoot
            GeneratedAppRoot = $generatedRoot
            GeneratedPackageFeed = $generatedPackageFeedPath
            Solution = $solutionPath
            HostProject = $hostProjectPath
            StdoutLog = $stdoutLogPath
            StderrLog = $stderrLogPath
            TemporaryOutputRetained = [bool]$KeepOutput
        })
        Error = $ErrorMessage
    })

    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedReportPath -Encoding UTF8
    Write-Host "Generated app adoption execution report: $resolvedReportPath" -ForegroundColor Cyan
}

$process = $null
$previousNuGetPackages = $null
$restoreRepoPackageAssets = $false
$hostProjectPath = ""
$solutionPath = ""

try {
    New-Item -ItemType Directory -Path $packageFeedPath -Force | Out-Null
    New-Item -ItemType Directory -Path $toolPath -Force | Out-Null
    New-Item -ItemType Directory -Path $nuGetPackagesPath -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null
    # Pin only this disposable consumer workspace; the generated application's files stay intact.
    Copy-Item -LiteralPath (Join-Path $repoRoot 'global.json') -Destination (Join-Path $workspaceRoot 'global.json')
    $toolchainEvidence.SourceRevision = (& git -C $repoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Could not read the candidate source revision.' }
    $toolchainEvidence.WorkingTreeDirty = -not [string]::IsNullOrWhiteSpace((& git -C $repoRoot status --porcelain | Out-String))
    $toolchainEvidence.RepositorySdk = (Invoke-DotNet -WorkingDirectory $repoRoot -Arguments @('--version') | Out-String).Trim()

    $previousNuGetPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $nuGetPackagesPath
    $restoreRepoPackageAssets = $true

    Write-Host ""
    Write-Host "Publishing repo-local Cephalon packages..." -ForegroundColor Cyan
    $publishPackageArguments = @{
        Configuration = $Configuration
        OutputPath = $packageFeedPath
        ProjectPaths = $packageProjectPaths
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
    Write-Host "Replaying machine-level doctor checks through the installed CLI..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("doctor")

    Write-Host ""
    Write-Host "Scaffolding a fresh app outside the repository..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("new", $AppName, "--output", $generatedRoot)

    $hostProjectPath = Resolve-GeneratedHostProjectPath -GeneratedRoot $generatedRoot -AppName $AppName
    $solutionPath = Join-Path $generatedRoot "$AppName.slnx"
    if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        throw "Expected generated solution at '$solutionPath'."
    }

    Write-Host ""
    Write-Host "Seeding the generated local package feed..." -ForegroundColor Cyan
    & $publishPackagesScriptPath -Configuration $Configuration -OutputPath $generatedPackageFeedPath -SkipBuild -ProjectPaths $packageProjectPaths
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
    Write-Host "Restoring the generated solution..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("restore", $solutionPath)

    Write-Host ""
    Write-Host "Building the generated solution..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("build", $solutionPath, "-c", $Configuration, "--no-restore")
    $toolchainEvidence.GeneratedHostSdk = (Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @(
        'msbuild', $hostProjectPath, '-getProperty:NETCoreSdkVersion', '-nologo') | Out-String).Trim()
    if ($toolchainEvidence.RepositorySdk -cne $toolchainEvidence.GeneratedHostSdk) { throw 'The generated host selected a different SDK from the declared validation toolchain.' }

    Write-Host ""
    Write-Host "Running the generated host..." -ForegroundColor Cyan
    $previousAspNetCoreUrls = $env:ASPNETCORE_URLS
    $previousDotNetEnvironment = $env:DOTNET_ENVIRONMENT
    try {
        $env:ASPNETCORE_URLS = $HostUrl
        $env:DOTNET_ENVIRONMENT = "Development"

        $hostDirectory = Split-Path -Parent $hostProjectPath
        $hostAssembly = Join-Path $hostDirectory "bin/$Configuration/net10.0/$([IO.Path]::GetFileNameWithoutExtension($hostProjectPath)).dll"
        $startArguments = @{
            FilePath = 'dotnet'; ArgumentList = @('"' + $hostAssembly + '"')
            WorkingDirectory = $hostDirectory; RedirectStandardOutput = $stdoutLogPath
            RedirectStandardError = $stderrLogPath; PassThru = $true
        }
        if ($IsWindows) { $startArguments.WindowStyle = 'Hidden' }
        else { $startArguments.NoNewWindow = $true }
        $process = Start-Process @startArguments
    }
    finally {
        $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
        $env:DOTNET_ENVIRONMENT = $previousDotNetEnvironment
    }

    Wait-ForHttpSuccess -Uri "$HostUrl/health/ready" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/scalar" -TimeoutSeconds $TimeoutSeconds -Process $process

    $manifestResponse = Invoke-WebRequest -Uri "$HostUrl/engine" -TimeoutSec 20
    $snapshotResponse = Invoke-WebRequest -Uri "$HostUrl/engine/snapshot" -TimeoutSec 20
    $appModelPath = Join-Path (Split-Path -Parent $hostProjectPath) 'Configurations/AddEngine.AppModel.json'
    $runtimeContractEvidence = Assert-GeneratedAppRuntimeContract `
        -Manifest ($manifestResponse.Content | ConvertFrom-Json -Depth 100) `
        -Snapshot ($snapshotResponse.Content | ConvertFrom-Json -Depth 100) `
        -Configuration (Get-Content -LiteralPath $appModelPath -Raw | ConvertFrom-Json)
    $evidenceDirectory = Join-Path (Split-Path -Parent (Resolve-ReportPath -Path $ReportPath)) 'generated-app-runtime-contract'
    New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
    $manifestResponse.Content | Set-Content -LiteralPath (Join-Path $evidenceDirectory 'manifest.json') -Encoding utf8
    $snapshotResponse.Content | Set-Content -LiteralPath (Join-Path $evidenceDirectory 'snapshot.json') -Encoding utf8
    Copy-Item -LiteralPath $appModelPath -Destination (Join-Path $evidenceDirectory 'app-model.json') -Force
    $runtimeContractEvidence | Add-Member -NotePropertyName Artifacts -NotePropertyValue @(
        Get-ChildItem -LiteralPath $evidenceDirectory -File | Sort-Object Name | ForEach-Object {
            [pscustomobject]@{ Name = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
        }
    )

    Write-Host ""
    Write-Host "Generated app adoption validation completed successfully." -ForegroundColor Green
    Write-GeneratedAppAdoptionExecutionReport -Status "passed"
    Write-Host "Temporary package feed: $packageFeedPath" -ForegroundColor Cyan
    Write-Host "Installed tool path: $toolPath" -ForegroundColor Cyan
    Write-Host "NuGet package cache: $nuGetPackagesPath" -ForegroundColor Cyan
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "Generated app adoption validation failed." -ForegroundColor Yellow
    Write-RecentLogs -Path $stdoutLogPath -Label "Generated host stdout"
    Write-RecentLogs -Path $stderrLogPath -Label "Generated host stderr"
    try {
        Write-GeneratedAppAdoptionExecutionReport -Status "failed" -ErrorMessage $_.Exception.Message
    }
    catch {
        Write-Warning "Could not write generated app adoption execution report: $($_.Exception.Message)"
    }
    throw
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        $process.Kill($true)
        if (-not $process.WaitForExit(5000)) { Write-Warning 'Generated host did not exit within the cleanup limit.' }
    }

    $env:NUGET_PACKAGES = $previousNuGetPackages

    if ($restoreRepoPackageAssets) {
        try {
            Write-Host ""
            Write-Host "Restoring repo package assets back to the default NuGet cache..." -ForegroundColor Cyan
            foreach ($packageProjectPath in $packageProjectPaths) {
                Invoke-DotNet -WorkingDirectory $repoRoot -Arguments @("restore", $packageProjectPath)
            }
        }
        catch {
            Write-Warning "Could not restore repo package assets back to the default NuGet cache: $($_.Exception.Message)"
        }
    }

    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        $resolvedTemporaryRoot = (Resolve-Path -LiteralPath $tempRoot).Path
        $expectedParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
        if ([IO.Path]::GetDirectoryName($resolvedTemporaryRoot) -ne $expectedParent -or
            [IO.Path]::GetFileName($resolvedTemporaryRoot) -notmatch '^cephalon-generated-adoption-[0-9a-f]{32}$' -or
            (Get-Item -LiteralPath $resolvedTemporaryRoot).LinkType) { throw 'Refusing cleanup outside the owned temporary workspace.' }
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
