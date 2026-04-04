param(
    [string]$AppName = "Cephalon.WindowsServiceSmoke",
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProjectPath = Join-Path $repoRoot "src\Cephalon.Cli\Cephalon.Cli.csproj"
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-windows-service-" + [Guid]::NewGuid().ToString("N"))
$generatedRoot = Join-Path $tempRoot "app"
$hostProjectName = "$AppName.Host"
$hostProjectPath = Join-Path $generatedRoot "src\$hostProjectName\$hostProjectName.csproj"
$programPath = Join-Path $generatedRoot "src\$hostProjectName\Program.cs"
$publishOutputPath = Join-Path $generatedRoot "artifacts\publish\$hostProjectName"
$publishedDllPath = Join-Path $publishOutputPath "$hostProjectName.dll"
$windowsServiceReadmePath = Join-Path $generatedRoot "deploy\windows-service\README.md"
$installScriptPath = Join-Path $generatedRoot "deploy\windows-service\install-service.ps1"
$removeScriptPath = Join-Path $generatedRoot "deploy\windows-service\remove-service.ps1"

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

function Invoke-PwshScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & pwsh -NoLogo -NoProfile -File $ScriptPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "PowerShell script failed: $ScriptPath $($Arguments -join ' ')"
    }

    return ($output | Out-String)
}

try {
    New-Item -ItemType Directory -Path $generatedRoot -Force | Out-Null

    Invoke-DotNet -WorkingDirectory $repoRoot -Arguments @(
        "run",
        "--project", $cliProjectPath,
        "--",
        "new",
        $AppName,
        "--output", $generatedRoot)

    $publishPackageArguments = @{
        Configuration = $Configuration
        OutputPath = (Join-Path $generatedRoot ".cephalon\packages")
    }

    if ($SkipPackageBuild) {
        $publishPackageArguments["SkipBuild"] = $true
    }

    & $publishPackagesScriptPath @publishPackageArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing failed."
    }

    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @(
        "publish",
        $hostProjectPath,
        "-c", $Configuration,
        "-p:PublishProfile=CephalonFolder")

    foreach ($path in @(
        $publishedDllPath,
        $windowsServiceReadmePath,
        $installScriptPath,
        $removeScriptPath,
        $programPath,
        $hostProjectPath))
    {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Expected generated asset at '$path'."
        }
    }

    $programContents = Get-Content -LiteralPath $programPath -Raw
    if (-not $programContents.Contains("WindowsServiceHelpers.IsWindowsService()", [System.StringComparison]::Ordinal)) {
        throw "Generated host did not include Windows Service content-root handling."
    }

    if (-not $programContents.Contains("builder.Host.UseWindowsService();", [System.StringComparison]::Ordinal)) {
        throw "Generated host did not enable Windows Service lifetime handling."
    }

    $hostProjectContents = Get-Content -LiteralPath $hostProjectPath -Raw
    if (-not $hostProjectContents.Contains("Microsoft.Extensions.Hosting.WindowsServices", [System.StringComparison]::Ordinal)) {
        throw "Generated host project did not include Microsoft.Extensions.Hosting.WindowsServices."
    }

    $installPreview = Invoke-PwshScript -ScriptPath $installScriptPath -Arguments @(
        "-PublishRoot", $publishOutputPath,
        "-Preview")
    $removePreview = Invoke-PwshScript -ScriptPath $removeScriptPath -Arguments @(
        "-Preview")

    if (-not $installPreview.Contains("sc.exe create", [System.StringComparison]::Ordinal)) {
        throw "Install preview did not render the expected sc.exe create command."
    }

    if (-not $installPreview.Contains($publishedDllPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Install preview did not point at the published host DLL."
    }

    if (-not $installPreview.Contains("--contentRoot", [System.StringComparison]::Ordinal)) {
        throw "Install preview did not include the expected --contentRoot argument."
    }

    if (-not $installPreview.Contains("sc.exe failure", [System.StringComparison]::Ordinal)) {
        throw "Install preview did not render the expected service recovery configuration."
    }

    if (-not $removePreview.Contains("sc.exe delete", [System.StringComparison]::Ordinal)) {
        throw "Remove preview did not render the expected sc.exe delete command."
    }

    Write-Host ""
    Write-Host "Generated app Windows Service validation completed successfully." -ForegroundColor Green
    Write-Host "Published output: $publishOutputPath" -ForegroundColor Cyan
}
finally {
    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
