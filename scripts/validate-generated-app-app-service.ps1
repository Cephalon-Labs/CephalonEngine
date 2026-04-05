param(
    [string]$AppName = "Cephalon.AppServiceSmoke",
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProjectPath = Join-Path $repoRoot "src\Cephalon.Cli\Cephalon.Cli.csproj"
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-app-service-" + [Guid]::NewGuid().ToString("N"))
$generatedRoot = Join-Path $tempRoot "app"
$hostProjectName = "$AppName.Host"
$hostProjectPath = Join-Path $generatedRoot "src\$hostProjectName\$hostProjectName.csproj"
$publishOutputPath = Join-Path $generatedRoot "artifacts\publish\$hostProjectName"
$publishedDllPath = Join-Path $publishOutputPath "$hostProjectName.dll"
$webConfigPath = Join-Path $publishOutputPath "web.config"
$guidePath = Join-Path $generatedRoot "deploy\azure-app-service\README.md"
$deployScriptPath = Join-Path $generatedRoot "deploy\azure-app-service\deploy-zip.ps1"
$packagePath = Join-Path $generatedRoot "artifacts\deploy\$hostProjectName\azure-app-service.zip"

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
    $null = & az version
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI is required for the App Service validation baseline."
    }

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
        $guidePath,
        $deployScriptPath))
    {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Expected generated asset at '$path'."
        }
    }

    $preview = Invoke-PwshScript -ScriptPath $deployScriptPath -Arguments @(
        "-ResourceGroupName", "cephalon-smoke-rg",
        "-AppName", "cephalon-smoke-app",
        "-Preview")
    $previewPlain = [regex]::Replace($preview, '\x1B\[[0-9;]*m', '')

    if (-not (Test-Path -LiteralPath $packagePath)) {
        throw "Expected Azure App Service ZIP package at '$packagePath'."
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $entryNames = $archive.Entries | ForEach-Object FullName

        if ($entryNames -notcontains "$hostProjectName.dll") {
            throw "ZIP package did not include the generated host DLL."
        }

        if ($entryNames -notcontains "web.config") {
            throw "ZIP package did not include web.config."
        }
    }
    finally {
        $archive.Dispose()
    }

    if (-not $previewPlain.Contains("WEBSITE_RUN_FROM_PACKAGE=1", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected WEBSITE_RUN_FROM_PACKAGE setting command."
    }

    if (-not $previewPlain.Contains("az webapp deploy", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected az webapp deploy command."
    }

    if (-not $previewPlain.Contains("--type zip", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected ZIP deployment type."
    }

    if (-not $previewPlain.Contains("--src-path", [System.StringComparison]::Ordinal)) {
        throw "Preview output did not render the expected deploy package argument."
    }

    if (-not $previewPlain.Contains("azure-app-service.zip", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Preview output did not point at the generated App Service ZIP package name."
    }

    Write-Host ""
    Write-Host "Generated app Azure App Service validation completed successfully." -ForegroundColor Green
    Write-Host "Published output: $publishOutputPath" -ForegroundColor Cyan
    Write-Host "Deployment package: $packagePath" -ForegroundColor Cyan
}
finally {
    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
