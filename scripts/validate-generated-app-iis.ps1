param(
    [string]$AppName = "Cephalon.IisSmoke",
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProjectPath = Join-Path $repoRoot "src\Cephalon.Cli\Cephalon.Cli.csproj"
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-iis-" + [Guid]::NewGuid().ToString("N"))
$generatedRoot = Join-Path $tempRoot "app"
$hostProjectName = "$AppName.Host"
$hostProjectPath = Join-Path $generatedRoot "src\$hostProjectName\$hostProjectName.csproj"
$publishOutputPath = Join-Path $generatedRoot "artifacts\publish\$hostProjectName"
$publishedDllPath = Join-Path $publishOutputPath "$hostProjectName.dll"
$webConfigPath = Join-Path $publishOutputPath "web.config"
$iisReadmePath = Join-Path $generatedRoot "deploy\iis\README.md"
$installScriptPath = Join-Path $generatedRoot "deploy\iis\install-site.ps1"
$removeScriptPath = Join-Path $generatedRoot "deploy\iis\remove-site.ps1"

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
        $webConfigPath,
        $iisReadmePath,
        $installScriptPath,
        $removeScriptPath))
    {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Expected generated asset at '$path'."
        }
    }

    $webConfigContents = Get-Content -LiteralPath $webConfigPath -Raw
    if (-not $webConfigContents.Contains("AspNetCoreModuleV2", [System.StringComparison]::Ordinal)) {
        throw "Published output did not include the expected ASP.NET Core Module handler."
    }

    if (-not $webConfigContents.Contains('processPath="dotnet"', [System.StringComparison]::Ordinal)) {
        throw "Published output did not include the expected dotnet processPath in web.config."
    }

    if (-not $webConfigContents.Contains("$hostProjectName.dll", [System.StringComparison]::Ordinal)) {
        throw "Published output web.config did not point at the generated host DLL."
    }

    if (-not $webConfigContents.Contains('hostingModel="inprocess"', [System.StringComparison]::Ordinal)) {
        throw "Published output web.config did not keep the expected hosting model."
    }

    $installPreview = Invoke-PwshScript -ScriptPath $installScriptPath -Arguments @(
        "-PhysicalPath", $publishOutputPath,
        "-Preview")
    $removePreview = Invoke-PwshScript -ScriptPath $removeScriptPath -Arguments @(
        "-Preview")

    if (-not $installPreview.Contains("add apppool", [System.StringComparison]::Ordinal)) {
        throw "Install preview did not render the expected appcmd add apppool command."
    }

    if (-not $installPreview.Contains("add site", [System.StringComparison]::Ordinal)) {
        throw "Install preview did not render the expected appcmd add site command."
    }

    if (-not $installPreview.Contains($publishOutputPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Install preview did not point at the published output path."
    }

    if (-not $installPreview.Contains("*:8080:", [System.StringComparison]::Ordinal)) {
        throw "Install preview did not render the expected default binding."
    }

    if (-not $removePreview.Contains("delete site", [System.StringComparison]::Ordinal)) {
        throw "Remove preview did not render the expected appcmd delete site command."
    }

    if (-not $removePreview.Contains("delete apppool", [System.StringComparison]::Ordinal)) {
        throw "Remove preview did not render the expected appcmd delete apppool command."
    }

    Write-Host ""
    Write-Host "Generated app IIS validation completed successfully." -ForegroundColor Green
    Write-Host "Published output: $publishOutputPath" -ForegroundColor Cyan
}
finally {
    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
