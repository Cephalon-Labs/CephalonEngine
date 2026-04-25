param(
    [string]$AppName = "Cephalon.ExternalPackageSmoke",
    [string]$HostUrl = "http://127.0.0.1:18083",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-external-package-adoption-" + [Guid]::NewGuid().ToString("N"))
$packageFeedPath = Join-Path $tempRoot "package-feed"
$referencePackageArtifactsPath = Join-Path $tempRoot "reference-packages"
$toolPath = Join-Path $tempRoot ".tools\cephalon"
$nuGetPackagesPath = Join-Path $tempRoot ".nuget\packages"
$workspaceRoot = Join-Path $tempRoot "workspace"
$generatedRoot = Join-Path $workspaceRoot $AppName
$generatedPackageFeedPath = Join-Path $generatedRoot ".cephalon\packages"
$pluginsRootPath = Join-Path $generatedRoot "plugins"
$stagedPackagePath = Join-Path $pluginsRootPath "reference-operations"
$stdoutLogPath = Join-Path $tempRoot "external-package.stdout.log"
$stderrLogPath = Join-Path $tempRoot "external-package.stderr.log"
$packageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Cli/Cephalon.Cli.csproj",
    "src/Cephalon.Observability/Cephalon.Observability.csproj",
    "src/Cephalon.Observability.OpenTelemetry/Cephalon.Observability.OpenTelemetry.csproj",
    "src/Cephalon.Observability.Serilog/Cephalon.Observability.Serilog.csproj",
    "src/Cephalon.ReferenceDocs/Cephalon.ReferenceDocs.csproj",
    "src/Cephalon.Scaffolding/Cephalon.Scaffolding.csproj"
)
$referenceModuleProjectPath = "samples/Cephalon.ReferenceModule.Operations/Cephalon.ReferenceModule.Operations.csproj"
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

function Resolve-GeneratedAppModelConfigurationPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot
    )

    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $preferredPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.AppModel.json"
    if (Test-Path -LiteralPath $preferredPath -PathType Leaf) {
        return $preferredPath
    }

    $candidates = @(Get-ChildItem -Path $GeneratedRoot -Recurse -Filter "AddEngine.AppModel.json" -File | Sort-Object FullName)
    if ($candidates.Count -eq 1) {
        return $candidates[0].FullName
    }

    throw "Could not determine the generated AddEngine.AppModel.json path under '$GeneratedRoot'."
}

function Set-OutOfTreePackagePolicyConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigurationPath,
        [Parameter(Mandatory = $true)]
        [string]$PluginsRootPath
    )

    $configuration = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json -Depth 20
    if ($null -eq $configuration.Engine) {
        throw "Expected '$ConfigurationPath' to contain an Engine section."
    }

    if ($null -eq $configuration.Engine.Discovery) {
        $configuration.Engine | Add-Member -NotePropertyName Discovery -NotePropertyValue ([pscustomobject]@{}) -Force
    }

    $configuration.Engine.Discovery | Add-Member -NotePropertyName PackageDirectories -NotePropertyValue @(
        [pscustomobject]@{
            Path = $PluginsRootPath
            IncludeSubdirectories = $true
        }
    ) -Force

    $configuration.Engine | Add-Member -NotePropertyName PackagePolicy -NotePropertyValue ([pscustomobject]@{
            AllowAssemblyPathPackages = $false
            RequireVersion = $true
            RequireMinimumEngineVersion = $true
            RequireSupportedTargetFrameworks = $true
            RequirePublisherId = $true
        }) -Force

    $configuration.Engine | Add-Member -NotePropertyName Trust -NotePropertyValue ([pscustomobject]@{
            RequireTrustedPackages = $true
            TrustedPublishers = @("cephalon-labs")
        }) -Force

    $configuration | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $ConfigurationPath -Encoding utf8
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

function Resolve-ReferencePackagePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArtifactsRootPath
    )

    $packages = @(Get-ChildItem -Path $ArtifactsRootPath -Filter "*.nupkg" -File |
            Where-Object { -not $_.Name.EndsWith(".snupkg", [System.StringComparison]::OrdinalIgnoreCase) } |
            Sort-Object Name)
    if ($packages.Count -ne 1) {
        throw "Expected exactly one reference module package in '$ArtifactsRootPath'."
    }

    return $packages[0].FullName
}

function Assert-ExternalPackageRuntimeTruth {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostUrl
    )

    $packages = @(Invoke-RestMethod -Uri "$HostUrl/engine/packages" -TimeoutSec 15)
    $package = $packages | Where-Object { $_.id -eq "reference-operations" } | Select-Object -First 1
    if ($null -eq $package) {
        throw "Expected /engine/packages to contain the staged 'reference-operations' package."
    }

    if ($package.kind -ne "directory-manifest") {
        throw "Expected the staged package kind to be 'directory-manifest', but found '$($package.kind)'."
    }

    if ($package.publisherId -ne "cephalon-labs") {
        throw "Expected the staged package publisher to be 'cephalon-labs', but found '$($package.publisherId)'."
    }

    if (-not $package.isTrusted) {
        throw "Expected the staged package to be trusted."
    }

    $packagePolicy = Invoke-RestMethod -Uri "$HostUrl/engine/package-policy" -TimeoutSec 15
    if ($packagePolicy.allowAssemblyPathPackages) {
        throw "Expected /engine/package-policy to disable assembly-path package loading."
    }

    if (-not $packagePolicy.requireVersion -or
        -not $packagePolicy.requireMinimumEngineVersion -or
        -not $packagePolicy.requireSupportedTargetFrameworks -or
        -not $packagePolicy.requirePublisherId) {
        throw "Expected /engine/package-policy to require version, minimum engine version, supported target frameworks, and publisher metadata."
    }

    $trustPolicy = Invoke-RestMethod -Uri "$HostUrl/engine/trust-policy" -TimeoutSec 15
    if (-not $trustPolicy.policy.requireTrustedPackages) {
        throw "Expected /engine/trust-policy to require trusted packages."
    }

    if (-not ($trustPolicy.policy.trustedPublishers -contains "cephalon-labs")) {
        throw "Expected /engine/trust-policy to include the trusted publisher 'cephalon-labs'."
    }

    $trustDecision = @($trustPolicy.packages) | Where-Object {
        $_.packageId -eq "reference-operations" -and $_.publisherId -eq "cephalon-labs"
    } | Select-Object -First 1
    if ($null -eq $trustDecision -or -not $trustDecision.isTrusted) {
        throw "Expected /engine/trust-policy to expose a trusted decision for 'reference-operations'."
    }

    $snapshot = Invoke-RestMethod -Uri "$HostUrl/engine/snapshot" -TimeoutSec 15
    $snapshotPackage = @($snapshot.manifest.packages) | Where-Object { $_.id -eq "reference-operations" } | Select-Object -First 1
    if ($null -eq $snapshotPackage -or -not $snapshotPackage.isTrusted) {
        throw "Expected /engine/snapshot to expose the trusted staged package."
    }

    $snapshotModule = @($snapshot.manifest.modules) | Where-Object {
        $_.id -eq "operations" -and $_.packageId -eq "reference-operations"
    } | Select-Object -First 1
    if ($null -eq $snapshotModule -or -not $snapshotModule.isTrusted) {
        throw "Expected /engine/snapshot to expose the trusted 'operations' module from the staged package."
    }

    $operationsStatus = Invoke-WebRequest -Uri "$HostUrl/api/operations/status" -TimeoutSec 15
    if (-not $operationsStatus.Content.Contains("Operations module is running.", [System.StringComparison]::Ordinal)) {
        throw "Expected /api/operations/status to confirm the staged operations module."
    }
}

$process = $null
$previousNuGetPackages = $null
$restoreRepoPackageAssets = $false

try {
    New-Item -ItemType Directory -Path $packageFeedPath -Force | Out-Null
    New-Item -ItemType Directory -Path $referencePackageArtifactsPath -Force | Out-Null
    New-Item -ItemType Directory -Path $toolPath -Force | Out-Null
    New-Item -ItemType Directory -Path $nuGetPackagesPath -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null

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
    Write-Host "Packing the reference module for out-of-tree staging..." -ForegroundColor Cyan
    & $publishPackagesScriptPath -Configuration $Configuration -OutputPath $referencePackageArtifactsPath -ProjectPaths $referenceModuleProjectPath
    if ($LASTEXITCODE -ne 0) {
        throw "Reference module package publishing failed."
    }

    $referencePackagePath = Resolve-ReferencePackagePath -ArtifactsRootPath $referencePackageArtifactsPath

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
    Write-Host "Staging the reference module package into the generated app..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $pluginsRootPath -Force | Out-Null
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @(
        "package",
        "stage",
        "--package", $referencePackagePath,
        "--output", $stagedPackagePath)

    foreach ($expectedStagedFile in @(
            "cephalon.package.json",
            "Cephalon.ReferenceModule.Operations.dll",
            "PACKAGE.md"))
    {
        $expectedStagedPath = Join-Path $stagedPackagePath $expectedStagedFile
        if (-not (Test-Path -LiteralPath $expectedStagedPath -PathType Leaf)) {
            throw "Expected staged package asset '$expectedStagedPath'."
        }
    }

    Write-Host ""
    Write-Host "Configuring package discovery, policy, and trust for the staged module..." -ForegroundColor Cyan
    $appModelConfigurationPath = Resolve-GeneratedAppModelConfigurationPath -HostProjectPath $hostProjectPath -GeneratedRoot $generatedRoot
    Set-OutOfTreePackagePolicyConfiguration -ConfigurationPath $appModelConfigurationPath -PluginsRootPath $pluginsRootPath

    Write-Host ""
    Write-Host "Replaying generated-app doctor checks through the installed CLI..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("doctor", "--app-root", $generatedRoot)

    Write-Host ""
    Write-Host "Restoring the generated solution..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("restore", $solutionPath)

    Write-Host ""
    Write-Host "Building the generated solution..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("build", $solutionPath, "-c", $Configuration, "--no-restore")

    Write-Host ""
    Write-Host "Running the generated host with the staged external package..." -ForegroundColor Cyan
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
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/packages" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/package-policy" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/trust-policy" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/api/operations/status" -TimeoutSeconds $TimeoutSeconds -Process $process
    Assert-ExternalPackageRuntimeTruth -HostUrl $HostUrl

    Write-Host ""
    Write-Host "Out-of-tree package adoption validation completed successfully." -ForegroundColor Green
    Write-Host "Temporary package feed: $packageFeedPath" -ForegroundColor Cyan
    Write-Host "Reference module package artifact: $referencePackagePath" -ForegroundColor Cyan
    Write-Host "Installed tool path: $toolPath" -ForegroundColor Cyan
    Write-Host "NuGet package cache: $nuGetPackagesPath" -ForegroundColor Cyan
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan
    Write-Host "Staged package root: $stagedPackagePath" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "Out-of-tree package adoption validation failed." -ForegroundColor Yellow
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

    if ($restoreRepoPackageAssets) {
        try {
            Write-Host ""
            Write-Host "Restoring repo package assets back to the default NuGet cache..." -ForegroundColor Cyan
            foreach ($projectPath in @($packageProjectPaths + $referenceModuleProjectPath) | Sort-Object -Unique) {
                Invoke-DotNet -WorkingDirectory $repoRoot -Arguments @("restore", $projectPath)
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
