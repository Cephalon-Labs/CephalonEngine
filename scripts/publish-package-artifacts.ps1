param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "artifacts/packages-release",
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

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

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $repoRootWithSeparator = $repoRoot.TrimEnd('\') + '\'
    $repoRootUri = [System.Uri]::new($repoRootWithSeparator)
    $pathUri = [System.Uri]::new($fullPath)
    $relativePath = [System.Uri]::UnescapeDataString($repoRootUri.MakeRelativeUri($pathUri).ToString())
    return $relativePath.Replace('\', '/')
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($Arguments -join ' ')"
    }
}

function Get-ReleasePackageProjects {
    $projects = [System.Collections.Generic.List[string]]::new()

    foreach ($srcDirectory in Get-ChildItem -Path (Join-Path $repoRoot "src") -Directory -Filter "Cephalon.*" | Sort-Object Name) {
        if ($srcDirectory.Name -eq "Cephalon.Cli") {
            continue
        }

        $projectPath = Join-Path $srcDirectory.FullName ($srcDirectory.Name + ".csproj")
        if (Test-Path -LiteralPath $projectPath) {
            $projects.Add($projectPath)
        }
    }

    foreach ($extraProject in @(
        (Join-Path $repoRoot "samples\Cephalon.ReferenceModule.Operations\Cephalon.ReferenceModule.Operations.csproj"),
        (Join-Path $repoRoot "templates\Cephalon.TemplatePack\Cephalon.TemplatePack.csproj")
    )) {
        if (Test-Path -LiteralPath $extraProject) {
            $projects.Add($extraProject)
        }
    }

    return $projects.ToArray() | Sort-Object -Unique
}

$resolvedOutputPath = Resolve-FullPath -Path $OutputPath
New-Item -ItemType Directory -Path $resolvedOutputPath -Force | Out-Null
Get-ChildItem -LiteralPath $resolvedOutputPath -Force | Remove-Item -Recurse -Force

$artifacts = [System.Collections.Generic.List[object]]::new()
$projects = Get-ReleasePackageProjects

Push-Location $repoRoot
try {
    foreach ($project in $projects) {
        $before = @(Get-ChildItem -LiteralPath $resolvedOutputPath -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
        $arguments = [System.Collections.Generic.List[string]]::new()
        foreach ($argument in @("pack", $project, "-c", $Configuration, "-o", $resolvedOutputPath, "-p:ContinuousIntegrationBuild=true")) {
            $arguments.Add($argument)
        }
        if ($SkipBuild) {
            $arguments.Add("--no-build")
        }

        Invoke-DotNet -Arguments $arguments.ToArray()

        $after = @(Get-ChildItem -LiteralPath $resolvedOutputPath -File | Select-Object -ExpandProperty FullName)
        $packageFiles = @($after | Where-Object { $before -notcontains $_ } | Sort-Object)
        if ($packageFiles.Count -eq 0) {
            throw "No package artifacts were produced for '$project'."
        }

        $artifacts.Add([pscustomobject]@{
            Project = Get-RepoRelativePath -Path $project
            PackageFiles = @($packageFiles | ForEach-Object { Get-RepoRelativePath -Path $_ })
        })
    }

    $manifestPath = Join-Path $resolvedOutputPath "package-artifacts-manifest.json"
    $manifest = [pscustomobject]@{
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        Configuration = $Configuration
        Artifacts = $artifacts
    }

    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath

    Write-Host ""
    Write-Host "Published $($artifacts.Count) package projects to '$resolvedOutputPath'." -ForegroundColor Green
    foreach ($artifact in $artifacts) {
        Write-Host "- $($artifact.Project)" -ForegroundColor Cyan
    }
}
finally {
    Pop-Location
}
