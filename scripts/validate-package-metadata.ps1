param(
    [string]$PackageArtifactsPath = "artifacts/packages-release",
    [string]$ManifestPath,
    [string]$OutputPath,
    [switch]$SkipSymbolPackageValidation
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

function Get-ObjectPropertyValue {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Object,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName,
        [object]$DefaultValue = $null
    )

    if ($null -eq $Object) {
        return $DefaultValue
    }

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value) {
        return $DefaultValue
    }

    return $property.Value
}

function Get-ZipEntryText {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.Compression.ZipArchive]$Archive,
        [Parameter(Mandatory = $true)]
        [string]$EntryName
    )

    $entry = $Archive.Entries |
        Where-Object { $_.FullName.Equals($EntryName, [System.StringComparison]::OrdinalIgnoreCase) } |
        Select-Object -First 1
    if ($null -eq $entry) {
        throw "Package archive is missing '$EntryName'."
    }

    $stream = $entry.Open()
    try {
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::UTF8, $true)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Test-ZipEntryExists {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Entries,
        [Parameter(Mandatory = $true)]
        [string]$EntryName
    )

    $normalizedEntryName = $EntryName.TrimStart('/', '\').Replace('\', '/')
    foreach ($entry in $Entries) {
        if ($entry.Replace('\', '/').Equals($normalizedEntryName, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Get-NuspecTextValue {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlNode]$MetadataNode,
        [Parameter(Mandatory = $true)]
        [string]$ElementName
    )

    $node = $MetadataNode.SelectSingleNode("*[local-name()='$ElementName']")
    if ($null -eq $node) {
        return ""
    }

    return ([string]$node.InnerText).Trim()
}

function Get-NuspecInfo {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackagePath
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entries = @($archive.Entries | ForEach-Object { $_.FullName })
        $nuspecEntry = $archive.Entries |
            Where-Object { $_.FullName.EndsWith(".nuspec", [System.StringComparison]::OrdinalIgnoreCase) } |
            Sort-Object FullName |
            Select-Object -First 1
        if ($null -eq $nuspecEntry) {
            throw "Package '$PackagePath' does not contain a .nuspec entry."
        }

        [xml]$nuspec = Get-ZipEntryText -Archive $archive -EntryName $nuspecEntry.FullName
        $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
        if ($null -eq $metadata) {
            throw "Package '$PackagePath' does not contain nuspec metadata."
        }

        $licenseNode = $metadata.SelectSingleNode("*[local-name()='license']")
        $repositoryNode = $metadata.SelectSingleNode("*[local-name()='repository']")
        $packageTypes = @(
            $metadata.SelectNodes("*[local-name()='packageTypes']/*[local-name()='packageType']") |
                ForEach-Object { [string]$_.GetAttribute("name") } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        $tags = @(
            (Get-NuspecTextValue -MetadataNode $metadata -ElementName "tags") -split '[;\s]+' |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        $managedEntries = @(
            $entries |
                Where-Object {
                    ($_.StartsWith("lib/", [System.StringComparison]::OrdinalIgnoreCase) -or
                        $_.StartsWith("tools/", [System.StringComparison]::OrdinalIgnoreCase) -or
                        $_.StartsWith("analyzers/", [System.StringComparison]::OrdinalIgnoreCase)) -and
                    ($_.EndsWith(".dll", [System.StringComparison]::OrdinalIgnoreCase) -or
                        $_.EndsWith(".exe", [System.StringComparison]::OrdinalIgnoreCase))
                }
        )

        return [pscustomobject]([ordered]@{
            Id             = Get-NuspecTextValue -MetadataNode $metadata -ElementName "id"
            Version        = Get-NuspecTextValue -MetadataNode $metadata -ElementName "version"
            Authors        = Get-NuspecTextValue -MetadataNode $metadata -ElementName "authors"
            Description    = Get-NuspecTextValue -MetadataNode $metadata -ElementName "description"
            Tags           = $tags
            LicenseType    = if ($null -eq $licenseNode) { "" } else { [string]$licenseNode.GetAttribute("type") }
            LicenseValue   = if ($null -eq $licenseNode) { "" } else { ([string]$licenseNode.InnerText).Trim() }
            ProjectUrl     = Get-NuspecTextValue -MetadataNode $metadata -ElementName "projectUrl"
            RepositoryType = if ($null -eq $repositoryNode) { "" } else { [string]$repositoryNode.GetAttribute("type") }
            RepositoryUrl  = if ($null -eq $repositoryNode) { "" } else { [string]$repositoryNode.GetAttribute("url") }
            RepositoryCommit = if ($null -eq $repositoryNode) { "" } else { [string]$repositoryNode.GetAttribute("commit") }
            Readme         = Get-NuspecTextValue -MetadataNode $metadata -ElementName "readme"
            PackageTypes   = $packageTypes
            Entries        = $entries
            ManagedEntries = $managedEntries
        })
    }
    finally {
        $archive.Dispose()
    }
}

function Add-PackageMetadataCheck {
    param(
        [System.Collections.Generic.List[object]]$Checks,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [bool]$Passed,
        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    $Checks.Add([pscustomobject]([ordered]@{
        Name = $Name
        Passed = $Passed
        Detail = $Detail
    }))
}

function Test-PackageMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Artifact,
        [Parameter(Mandatory = $true)]
        [string]$ArtifactsRoot,
        [Parameter(Mandatory = $true)]
        [bool]$ValidateSymbols
    )

    $project = [string](Get-ObjectPropertyValue -Object $Artifact -PropertyName "Project" -DefaultValue "")
    $packageKind = [string](Get-ObjectPropertyValue -Object $Artifact -PropertyName "PackageKind" -DefaultValue "")
    $packageFiles = @(
        Get-ObjectPropertyValue -Object $Artifact -PropertyName "PackageFiles" -DefaultValue @() |
            ForEach-Object { $_ }
    )
    $primaryPackageFiles = @(
        $packageFiles |
            Where-Object {
                $fileName = [string](Get-ObjectPropertyValue -Object $_ -PropertyName "FileName" -DefaultValue "")
                $fileName.EndsWith(".nupkg", [System.StringComparison]::OrdinalIgnoreCase) -and
                -not $fileName.EndsWith(".snupkg", [System.StringComparison]::OrdinalIgnoreCase) -and
                -not $fileName.EndsWith(".symbols.nupkg", [System.StringComparison]::OrdinalIgnoreCase)
            }
    )

    $checks = [System.Collections.Generic.List[object]]::new()
    if ($primaryPackageFiles.Count -ne 1) {
        Add-PackageMetadataCheck -Checks $checks -Name "primary-package-count" -Passed $false -Detail "Expected exactly one primary .nupkg for '$project', found $($primaryPackageFiles.Count)."
        return [pscustomobject]([ordered]@{
            Project = $project
            PackageKind = $packageKind
            PackageId = ""
            Version = ""
            Nupkg = ""
            Snupkg = ""
            RequiresSymbols = $false
            Status = "failed"
            Checks = $checks
        })
    }

    $primaryPackage = $primaryPackageFiles[0]
    $relativePackagePath = [string](Get-ObjectPropertyValue -Object $primaryPackage -PropertyName "Path" -DefaultValue "")
    $primaryPackagePath = [System.IO.Path]::GetFullPath((Join-Path $ArtifactsRoot $relativePackagePath))
    if (-not (Test-Path -LiteralPath $primaryPackagePath -PathType Leaf)) {
        Add-PackageMetadataCheck -Checks $checks -Name "primary-package-exists" -Passed $false -Detail "Primary package '$relativePackagePath' does not exist."
        return [pscustomobject]([ordered]@{
            Project = $project
            PackageKind = $packageKind
            PackageId = ""
            Version = ""
            Nupkg = $relativePackagePath
            Snupkg = ""
            RequiresSymbols = $false
            Status = "failed"
            Checks = $checks
        })
    }

    $info = Get-NuspecInfo -PackagePath $primaryPackagePath
    $expectedRepositoryUrl = "https://github.com/Cephalon-Labs/CephalonEngine"
    $expectedSymbolPackageFileName = ([System.IO.Path]::GetFileNameWithoutExtension($relativePackagePath) + ".snupkg")
    $symbolPackageFile = $packageFiles |
        Where-Object {
            $fileName = [string](Get-ObjectPropertyValue -Object $_ -PropertyName "FileName" -DefaultValue "")
            $fileName.Equals($expectedSymbolPackageFileName, [System.StringComparison]::OrdinalIgnoreCase)
        } |
        Select-Object -First 1
    $relativeSymbolPackagePath = if ($null -eq $symbolPackageFile) { "" } else { [string](Get-ObjectPropertyValue -Object $symbolPackageFile -PropertyName "Path" -DefaultValue "") }
    $symbolPackagePath = if ([string]::IsNullOrWhiteSpace($relativeSymbolPackagePath)) { "" } else { [System.IO.Path]::GetFullPath((Join-Path $ArtifactsRoot $relativeSymbolPackagePath)) }
    $runtimeManagedEntries = @(
        $info.ManagedEntries |
            Where-Object {
                $_.StartsWith("lib/", [System.StringComparison]::OrdinalIgnoreCase) -or
                $_.StartsWith("tools/", [System.StringComparison]::OrdinalIgnoreCase)
            }
    )
    $requiresSymbols = $ValidateSymbols -and $packageKind -ne "template-pack" -and $runtimeManagedEntries.Count -gt 0

    Add-PackageMetadataCheck -Checks $checks -Name "id-prefix" -Passed $info.Id.StartsWith("Cephalon.", [System.StringComparison]::Ordinal) -Detail "Package id is '$($info.Id)'."
    Add-PackageMetadataCheck -Checks $checks -Name "version" -Passed (-not [string]::IsNullOrWhiteSpace($info.Version)) -Detail "Package version is '$($info.Version)'."
    Add-PackageMetadataCheck -Checks $checks -Name "authors" -Passed ($info.Authors -match '(^|[,;\s])Cephalon([,;\s]|$)') -Detail "Authors are '$($info.Authors)'."
    Add-PackageMetadataCheck -Checks $checks -Name "description" -Passed (-not [string]::IsNullOrWhiteSpace($info.Description)) -Detail "Description is present."
    Add-PackageMetadataCheck -Checks $checks -Name "tags" -Passed (@($info.Tags) -contains "cephalon") -Detail "Tags are '$(@($info.Tags) -join ';')'."
    Add-PackageMetadataCheck -Checks $checks -Name "license" -Passed ($info.LicenseType -eq "expression" -and $info.LicenseValue -eq "MIT") -Detail "License is type '$($info.LicenseType)' value '$($info.LicenseValue)'."
    Add-PackageMetadataCheck -Checks $checks -Name "project-url" -Passed ($info.ProjectUrl -eq $expectedRepositoryUrl) -Detail "Project URL is '$($info.ProjectUrl)'."
    Add-PackageMetadataCheck -Checks $checks -Name "repository" -Passed ($info.RepositoryType -eq "git" -and $info.RepositoryUrl -eq $expectedRepositoryUrl) -Detail "Repository is type '$($info.RepositoryType)' url '$($info.RepositoryUrl)' commit '$($info.RepositoryCommit)'."
    Add-PackageMetadataCheck -Checks $checks -Name "readme" -Passed ((-not [string]::IsNullOrWhiteSpace($info.Readme)) -and (Test-ZipEntryExists -Entries $info.Entries -EntryName $info.Readme)) -Detail "Readme is '$($info.Readme)'."

    if ($packageKind -eq "dotnet-tool") {
        Add-PackageMetadataCheck -Checks $checks -Name "dotnet-tool-package-type" -Passed (@($info.PackageTypes) -contains "DotnetTool") -Detail "Package types are '$(@($info.PackageTypes) -join ';')'."
    }
    elseif ($packageKind -eq "template-pack") {
        Add-PackageMetadataCheck -Checks $checks -Name "template-package-type" -Passed (@($info.PackageTypes) -contains "Template") -Detail "Package types are '$(@($info.PackageTypes) -join ';')'."
    }

    if ($requiresSymbols) {
        $symbolPackageExists = -not [string]::IsNullOrWhiteSpace($symbolPackagePath) -and (Test-Path -LiteralPath $symbolPackagePath -PathType Leaf)
        Add-PackageMetadataCheck -Checks $checks -Name "symbol-package-exists" -Passed $symbolPackageExists -Detail "Expected symbol package '$expectedSymbolPackageFileName'."
        if ($symbolPackageExists) {
            $symbolInfo = Get-NuspecInfo -PackagePath $symbolPackagePath
            Add-PackageMetadataCheck -Checks $checks -Name "symbol-package-identity" -Passed ($symbolInfo.Id -eq $info.Id -and $symbolInfo.Version -eq $info.Version) -Detail "Symbol package id/version are '$($symbolInfo.Id)'/'$($symbolInfo.Version)'."
            Add-PackageMetadataCheck -Checks $checks -Name "symbol-package-type" -Passed (@($symbolInfo.PackageTypes) -contains "SymbolsPackage") -Detail "Symbol package types are '$(@($symbolInfo.PackageTypes) -join ';')'."
            Add-PackageMetadataCheck -Checks $checks -Name "symbol-pdbs" -Passed (@($symbolInfo.Entries | Where-Object { $_.EndsWith(".pdb", [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) -Detail "Symbol package contains portable PDB entries."
        }
    }

    $failedChecks = @($checks | Where-Object { -not $_.Passed })
    return [pscustomobject]([ordered]@{
        Project = $project
        PackageKind = $packageKind
        PackageId = $info.Id
        Version = $info.Version
        Nupkg = $relativePackagePath
        Snupkg = $relativeSymbolPackagePath
        RequiresSymbols = $requiresSymbols
        RepositoryCommit = $info.RepositoryCommit
        Status = if ($failedChecks.Count -eq 0) { "passed" } else { "failed" }
        Checks = $checks
    })
}

function Invoke-PackageMetadataValidation {
    param(
        [string]$PackageArtifactsPath = "artifacts/packages-release",
        [string]$ManifestPath,
        [string]$OutputPath,
        [switch]$SkipSymbolPackageValidation
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $resolvedArtifactsPath = Resolve-FullPath -Path $PackageArtifactsPath
    if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
        $ManifestPath = Join-Path $resolvedArtifactsPath "package-artifacts-manifest.json"
    }
    $resolvedManifestPath = Resolve-FullPath -Path $ManifestPath
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        $OutputPath = Join-Path $resolvedArtifactsPath "package-metadata-validation.json"
    }
    $resolvedOutputPath = Resolve-FullPath -Path $OutputPath

    if (-not (Test-Path -LiteralPath $resolvedArtifactsPath -PathType Container)) {
        throw "Package artifacts path '$resolvedArtifactsPath' was not found."
    }
    if (-not (Test-Path -LiteralPath $resolvedManifestPath -PathType Leaf)) {
        throw "Package artifacts manifest '$resolvedManifestPath' was not found."
    }

    $manifest = Get-Content -LiteralPath $resolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32
    $artifacts = @(
        Get-ObjectPropertyValue -Object $manifest -PropertyName "Artifacts" -DefaultValue @() |
            ForEach-Object { $_ }
    )
    if ($artifacts.Count -eq 0) {
        throw "Package artifacts manifest '$resolvedManifestPath' does not contain any artifacts."
    }

    $sourceRepository = [string](Get-ObjectPropertyValue -Object $manifest -PropertyName "SourceRepository" -DefaultValue "")
    $sourceRevision = [string](Get-ObjectPropertyValue -Object $manifest -PropertyName "SourceRevision" -DefaultValue "")
    $packageReports = @(
        $artifacts |
            ForEach-Object {
                Test-PackageMetadata `
                    -Artifact $_ `
                    -ArtifactsRoot $resolvedArtifactsPath `
                    -ValidateSymbols (-not $SkipSymbolPackageValidation)
            }
    )
    $failedPackages = @($packageReports | Where-Object { $_.Status -ne "passed" })
    $report = [pscustomobject]([ordered]@{
        '$schemaVersion' = "1.0.0"
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        SourceManifest = $resolvedManifestPath
        SourceRepository = $sourceRepository
        SourceRevision = $sourceRevision
        Status = if ($failedPackages.Count -eq 0 -and -not [string]::IsNullOrWhiteSpace($sourceRepository) -and -not [string]::IsNullOrWhiteSpace($sourceRevision)) { "passed" } else { "failed" }
        PackageCount = $packageReports.Count
        FailedPackageCount = $failedPackages.Count
        SymbolPackageCount = @($packageReports | Where-Object { -not [string]::IsNullOrWhiteSpace($_.Snupkg) }).Count
        SymbolRequiredPackageCount = @($packageReports | Where-Object { $_.RequiresSymbols }).Count
        Packages = $packageReports
    })

    $outputDirectory = Split-Path -Parent $resolvedOutputPath
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    $report | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8

    if ([string]::IsNullOrWhiteSpace($sourceRepository) -or [string]::IsNullOrWhiteSpace($sourceRevision)) {
        throw "Package artifacts manifest must include SourceRepository and SourceRevision before metadata validation can pass."
    }

    if ($failedPackages.Count -gt 0) {
        $failedSummaries = @(
            $failedPackages |
                ForEach-Object {
                    $failedChecks = @($_.Checks | Where-Object { -not $_.Passed } | ForEach-Object { $_.Name })
                    "$($_.PackageId) [$($failedChecks -join ', ')]"
                }
        )
        throw "Package metadata validation failed for $($failedPackages.Count) package(s): $($failedSummaries -join '; '). Report: $resolvedOutputPath"
    }

    Write-Host "Package metadata validation passed for $($packageReports.Count) package(s). Report: $resolvedOutputPath" -ForegroundColor Green

    return [pscustomobject]([ordered]@{
        Report = $report
        JsonPath = $resolvedOutputPath
    })
}

if ($env:CEPHALON_VALIDATE_PACKAGE_METADATA_NO_RUN -eq "1") {
    return
}

Invoke-PackageMetadataValidation `
    -PackageArtifactsPath $PackageArtifactsPath `
    -ManifestPath $ManifestPath `
    -OutputPath $OutputPath `
    -SkipSymbolPackageValidation:$SkipSymbolPackageValidation | Out-Null
