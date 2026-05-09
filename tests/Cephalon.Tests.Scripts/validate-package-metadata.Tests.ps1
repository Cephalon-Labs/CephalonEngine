#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/validate-package-metadata.ps1.
.DESCRIPTION
    Verifies the NuGet artifact metadata validator against deterministic fixture packages.
#>

BeforeAll {
    $env:CEPHALON_VALIDATE_PACKAGE_METADATA_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\validate-package-metadata.ps1"
    . $script:scriptPath

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    function Add-ZipTextEntry {
        param(
            [Parameter(Mandatory = $true)]
            [System.IO.Compression.ZipArchive]$Archive,
            [Parameter(Mandatory = $true)]
            [string]$Path,
            [Parameter(Mandatory = $true)]
            [string]$Content
        )

        $entry = $Archive.CreateEntry($Path)
        $stream = $entry.Open()
        try {
            $writer = [System.IO.StreamWriter]::new($stream, [System.Text.Encoding]::UTF8)
            try {
                $writer.Write($Content)
            }
            finally {
                $writer.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }

    function New-NuspecText {
        param(
            [string]$Id = "Cephalon.Engine",
            [string]$Version = "0.1.0-preview",
            [string]$Description = "Fixture package.",
            [string]$Tags = "cephalon;dotnet;framework;engine",
            [string]$Readme = "PACKAGE.md",
            [string[]]$PackageTypes = @()
        )

        $packageTypesXml = ""
        if (@($PackageTypes).Count -gt 0) {
            $packageTypeEntries = @(
                $PackageTypes | ForEach-Object { '<packageType name="{0}" />' -f $_ }
            ) -join ""
            $packageTypesXml = "<packageTypes>$packageTypeEntries</packageTypes>"
        }

        return @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>$Id</id>
    <version>$Version</version>
    <authors>Cephalon</authors>
    <description>$Description</description>
    <tags>$Tags</tags>
    <license type="expression">MIT</license>
    <projectUrl>https://github.com/Cephalon-Labs/CephalonEngine</projectUrl>
    <repository type="git" url="https://github.com/Cephalon-Labs/CephalonEngine" commit="0123456789abcdef" />
    <readme>$Readme</readme>
    $packageTypesXml
  </metadata>
</package>
"@
    }

    function New-TestPackage {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Path,
            [string]$Id = "Cephalon.Engine",
            [string]$Version = "0.1.0-preview",
            [string[]]$Entries = @("PACKAGE.md", "lib/net10.0/Cephalon.Engine.dll"),
            [string[]]$PackageTypes = @(),
            [string]$Readme = "PACKAGE.md",
            [switch]$SymbolsPackage
        )

        $archive = [System.IO.Compression.ZipFile]::Open($Path, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            $nuspecPackageTypes = if ($SymbolsPackage) { @("SymbolsPackage") } else { $PackageTypes }
            Add-ZipTextEntry -Archive $archive -Path "$Id.nuspec" -Content (New-NuspecText -Id $Id -Version $Version -PackageTypes $nuspecPackageTypes -Readme $Readme)
            foreach ($entry in $Entries) {
                Add-ZipTextEntry -Archive $archive -Path $entry -Content "fixture"
            }
        }
        finally {
            $archive.Dispose()
        }
    }

    function Write-PackageManifest {
        param(
            [Parameter(Mandatory = $true)]
            [string]$ArtifactsPath,
            [Parameter(Mandatory = $true)]
            [string]$Project,
            [Parameter(Mandatory = $true)]
            [string]$PackageKind,
            [Parameter(Mandatory = $true)]
            [string[]]$Files
        )

        $manifestPath = Join-Path $ArtifactsPath "package-artifacts-manifest.json"
        @{
            GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
            Configuration = "Release"
            SourceRepository = "https://github.com/Cephalon-Labs/CephalonEngine.git"
            SourceRevision = "0123456789abcdef"
            Artifacts = @(
                @{
                    Project = $Project
                    PackageKind = $PackageKind
                    PackageFiles = @(
                        $Files | ForEach-Object {
                            @{
                                Path = $_
                                FileName = [System.IO.Path]::GetFileName($_)
                                SizeBytes = 1
                                Sha256 = "00"
                            }
                        }
                    )
                }
            )
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
        return $manifestPath
    }
}

AfterAll {
    Remove-Item Env:\CEPHALON_VALIDATE_PACKAGE_METADATA_NO_RUN -ErrorAction SilentlyContinue
}

Describe "validate-package-metadata.ps1" {
    BeforeEach {
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-package-metadata-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
    }

    AfterEach {
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    It "passes for a Cephalon library package with readme, tags, repository metadata, and a matching snupkg" {
        New-TestPackage -Path (Join-Path $script:tempRoot "Cephalon.Engine.0.1.0-preview.nupkg")
        New-TestPackage `
            -Path (Join-Path $script:tempRoot "Cephalon.Engine.0.1.0-preview.snupkg") `
            -Entries @("lib/net10.0/Cephalon.Engine.pdb") `
            -SymbolsPackage
        $manifestPath = Write-PackageManifest `
            -ArtifactsPath $script:tempRoot `
            -Project "src/Cephalon.Engine/Cephalon.Engine.csproj" `
            -PackageKind "library" `
            -Files @("Cephalon.Engine.0.1.0-preview.nupkg", "Cephalon.Engine.0.1.0-preview.snupkg")

        $result = Invoke-PackageMetadataValidation -PackageArtifactsPath $script:tempRoot -ManifestPath $manifestPath

        $result.Report.Status | Should -Be "passed"
        $result.Report.PackageCount | Should -Be 1
        $result.Report.SymbolRequiredPackageCount | Should -Be 1
        $result.Report.SymbolPackageCount | Should -Be 1
    }

    It "fails when the nuspec readme does not exist in the package" {
        New-TestPackage `
            -Path (Join-Path $script:tempRoot "Cephalon.Engine.0.1.0-preview.nupkg") `
            -Entries @("lib/net10.0/Cephalon.Engine.dll")
        New-TestPackage `
            -Path (Join-Path $script:tempRoot "Cephalon.Engine.0.1.0-preview.snupkg") `
            -Entries @("lib/net10.0/Cephalon.Engine.pdb") `
            -SymbolsPackage
        $manifestPath = Write-PackageManifest `
            -ArtifactsPath $script:tempRoot `
            -Project "src/Cephalon.Engine/Cephalon.Engine.csproj" `
            -PackageKind "library" `
            -Files @("Cephalon.Engine.0.1.0-preview.nupkg", "Cephalon.Engine.0.1.0-preview.snupkg")

        {
            Invoke-PackageMetadataValidation -PackageArtifactsPath $script:tempRoot -ManifestPath $manifestPath
        } | Should -Throw "*readme*"
    }

    It "fails when a managed package omits its symbol package" {
        New-TestPackage -Path (Join-Path $script:tempRoot "Cephalon.Engine.0.1.0-preview.nupkg")
        $manifestPath = Write-PackageManifest `
            -ArtifactsPath $script:tempRoot `
            -Project "src/Cephalon.Engine/Cephalon.Engine.csproj" `
            -PackageKind "library" `
            -Files @("Cephalon.Engine.0.1.0-preview.nupkg")

        {
            Invoke-PackageMetadataValidation -PackageArtifactsPath $script:tempRoot -ManifestPath $manifestPath
        } | Should -Throw "*symbol-package-exists*"
    }

    It "allows template packages without symbol packages when they have no managed output" {
        New-TestPackage `
            -Path (Join-Path $script:tempRoot "Cephalon.TemplatePack.0.1.0-preview.nupkg") `
            -Id "Cephalon.TemplatePack" `
            -Entries @("PACKAGE.md", "content/templates/.template.config/template.json") `
            -PackageTypes @("Template")
        $manifestPath = Write-PackageManifest `
            -ArtifactsPath $script:tempRoot `
            -Project "templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj" `
            -PackageKind "template-pack" `
            -Files @("Cephalon.TemplatePack.0.1.0-preview.nupkg")

        $result = Invoke-PackageMetadataValidation -PackageArtifactsPath $script:tempRoot -ManifestPath $manifestPath

        $result.Report.Status | Should -Be "passed"
        $result.Report.SymbolRequiredPackageCount | Should -Be 0
    }
}
