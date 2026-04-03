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

function Get-OutputRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $rootWithSeparator = [System.IO.Path]::GetFullPath($RootPath).TrimEnd('\') + '\'
    $rootUri = [System.Uri]::new($rootWithSeparator)
    $pathUri = [System.Uri]::new([System.IO.Path]::GetFullPath($Path))
    $relativePath = [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString())
    return $relativePath.Replace('\', '/')
}

function Get-Sha256Hex {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            return [System.BitConverter]::ToString($sha256.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
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

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & git @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }
}

function Get-PackageKind {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $relativePath = Get-RepoRelativePath -Path $ProjectPath
    if ($relativePath -eq "src/Cephalon.Cli/Cephalon.Cli.csproj") {
        return "dotnet-tool"
    }

    if ($relativePath.StartsWith("templates/", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "template-pack"
    }

    if ($relativePath.StartsWith("samples/", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "reference-module"
    }

    return "library"
}

function Get-ReleasePackageProjects {
    $projects = [System.Collections.Generic.List[string]]::new()

    foreach ($srcDirectory in Get-ChildItem -Path (Join-Path $repoRoot "src") -Directory -Filter "Cephalon.*" | Sort-Object Name) {
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
$sourceRepository = Invoke-Git -Arguments @("-C", $repoRoot, "remote", "get-url", "origin")
$sourceRevision = Invoke-Git -Arguments @("-C", $repoRoot, "rev-parse", "HEAD")

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
            PackageKind = Get-PackageKind -ProjectPath $project
            PackageFiles = @($packageFiles | ForEach-Object {
                $fileHash = Get-Sha256Hex -Path $_
                [pscustomobject]@{
                    Path = Get-OutputRelativePath -RootPath $resolvedOutputPath -Path $_
                    FileName = [System.IO.Path]::GetFileName($_)
                    SizeBytes = [System.IO.FileInfo]::new($_).Length
                    Sha256 = $fileHash
                }
            })
        })
    }

    $manifestPath = Join-Path $resolvedOutputPath "package-artifacts-manifest.json"
    $checksumPath = Join-Path $resolvedOutputPath "package-artifacts.sha256"
    $manifest = [pscustomobject]@{
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        Configuration = $Configuration
        SourceRepository = $sourceRepository
        SourceRevision = $sourceRevision
        ChecksumFile = Get-OutputRelativePath -RootPath $resolvedOutputPath -Path $checksumPath
        Artifacts = $artifacts
    }

    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8

    $checksumLines = $artifacts |
        ForEach-Object { $_.PackageFiles } |
        ForEach-Object { "{0} *{1}" -f $_.Sha256, $_.FileName }

    $checksumLines | Set-Content -LiteralPath $checksumPath -Encoding utf8

    Write-Host ""
    Write-Host "Published $($artifacts.Count) package projects to '$resolvedOutputPath'." -ForegroundColor Green
    foreach ($artifact in $artifacts) {
        Write-Host "- $($artifact.Project)" -ForegroundColor Cyan
    }
}
finally {
    Pop-Location
}
