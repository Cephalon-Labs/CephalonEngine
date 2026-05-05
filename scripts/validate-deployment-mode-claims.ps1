#requires -Version 7.0

<#
.SYNOPSIS
    Validates Cephalon's deployment-mode claim truth (trim, Native AOT, single-file).

.DESCRIPTION
    Audits whether the support contract recorded in scripts/deployment-mode-support.json
    matches what the repository actually proves. The harness runs three phases:

    1. project-property audit   — does each src/Cephalon.* csproj that the manifest claims
                                  the mode for actually set the matching MSBuild property?
    2. analyzer audit           — is the matching analyzer enabled where the property is set?
    3. publish probe (optional) — does dotnet publish for the requested mode complete cleanly
                                  on the representative target set?
    4. package-scoped claims    — do deploymentModeEligibility package entries that opt into
                                  a mode carry the expected per-package project properties?

    The harness then computes a per-mode verdict and an aggregate verdict and writes both a
    machine-readable JSON report and a human-readable Markdown report under the output dir.

    Verdict semantics:
      claim-truthful                  manifest claims the mode and every audit/probe passes
      claim-overstated                manifest claims the mode but at least one audit/probe shows drift
      not-claimed                     manifest says not-claimed and no unscoped project sets the property
      not-claimed-with-property-drift manifest says not-claimed yet some project does set the property
      mixed                           per-mode verdicts disagree across modes (aggregate only)

    Exit code is 0 when the aggregate verdict is anything other than `claim-overstated`. The harness
    explicitly does NOT widen the support contract by itself; it only proves whether the current
    manifest claim is truthful against actual repository state. Promoting `not-claimed` to `claimed`
    in the manifest still requires the human-driven change set named in
    docs/deployment-mode-support.md.

.PARAMETER DeploymentMode
    Which mode to validate. One of: trim, nativeAot, singleFile, all.
    Default: all.

.PARAMETER Configuration
    Build configuration passed to the publish probe.
    Default: Release.

.PARAMETER OutputPath
    Directory where the harness writes its JSON + Markdown report.
    Default: artifacts/deployment-mode-claims-release.

.PARAMETER ManifestPath
    Path to the deployment-mode support manifest.
    Default: scripts/deployment-mode-support.json.

.PARAMETER RepoRoot
    Repository root. Defaults to the parent of the script directory when omitted so the script can
    be invoked from anywhere.

.PARAMETER PublishTargets
    Optional explicit set of csproj paths to publish during the publish probe. When omitted the probe
    is skipped (treated the same as -SkipPublish).

.PARAMETER ProjectPaths
    Optional explicit set of csproj paths to audit. When omitted the harness scans
    $RepoRoot/src for Cephalon.*.csproj.

.PARAMETER SkipPublish
    Skip the dotnet publish probe phase. Useful in audit-only runs.

.PARAMETER SkipAnalyzerCheck
    Skip the analyzer audit phase.

.PARAMETER SkipPropertyAudit
    Skip the project-property audit phase. Reserved for diagnostic runs only — verdict computation
    treats a skipped audit as "no evidence", not "audit passed".

.PARAMETER DotnetCommand
    The dotnet executable path used by the publish probe. Defaults to "dotnet". Tests override this
    to inject a deterministic stub.

.EXAMPLE
    pwsh ./scripts/validate-deployment-mode-claims.ps1

    Runs the full audit-only harness (publish probe is skipped because no -PublishTargets was given)
    against the current repo and writes the report to artifacts/deployment-mode-claims-release/.

.EXAMPLE
    pwsh ./scripts/validate-deployment-mode-claims.ps1 -DeploymentMode trim -SkipPublish

    Runs the trim-only audit-only flow.
#>

[CmdletBinding()]
param(
    [ValidateSet("trim", "nativeAot", "singleFile", "all")]
    [string]$DeploymentMode = "all",

    [string]$Configuration = "Release",

    [string]$OutputPath = "artifacts/deployment-mode-claims-release",

    [string]$ManifestPath = "scripts/deployment-mode-support.json",

    [string]$RepoRoot = "",

    [string[]]$PublishTargets = @(),

    [string[]]$ProjectPaths = @(),

    [switch]$SkipPublish,

    [switch]$SkipAnalyzerCheck,

    [switch]$SkipPropertyAudit,

    [string]$DotnetCommand = "dotnet"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# === Constants ===

$Script:DeploymentModeConfigs = @{
    trim       = [pscustomobject]@{
        Mode             = "trim"
        DisplayName      = "Trim"
        ProjectProperty  = "PublishTrimmed"
        AnalyzerProperty = "EnableTrimAnalyzer"
        PublishArg       = "/p:PublishTrimmed=true"
        WarningRegex     = "(?i)IL2\d{3}|trim warning|TrimAnalysis"
    }
    nativeAot  = [pscustomobject]@{
        Mode             = "nativeAot"
        DisplayName      = "Native AOT"
        ProjectProperty  = "PublishAot"
        AnalyzerProperty = "IsAotCompatible"
        PublishArg       = "/p:PublishAot=true"
        WarningRegex     = "(?i)IL3\d{3}|IL2\d{3}|AOT warning|AOT analysis"
    }
    singleFile = [pscustomobject]@{
        Mode             = "singleFile"
        DisplayName      = "Single-file"
        ProjectProperty  = "PublishSingleFile"
        AnalyzerProperty = "EnableSingleFileAnalyzer"
        PublishArg       = "/p:PublishSingleFile=true"
        WarningRegex     = "(?i)IL3\d{3}|single-file warning"
    }
}

$Script:ValidVerdicts = @(
    "claim-truthful",
    "claim-overstated",
    "not-claimed",
    "not-claimed-with-property-drift",
    "unknown",
    "mixed"
)

# === Helpers ===

function Invoke-Step {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$Title,
        [string]$Detail = ""
    )
    Write-Host "==> $Title" -ForegroundColor Cyan
    if ($Detail) {
        Write-Host "    $Detail" -ForegroundColor DarkGray
    }
}

function Test-IsTruthyMsBuildValue {
    [CmdletBinding()]
    param([string]$Value)
    if ($null -eq $Value) { return $false }
    return ($Value.Trim() -ieq "true")
}

function Read-DeploymentModeManifest {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Deployment-mode manifest not found at: $Path"
    }

    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if (-not $raw -or $raw.Trim().Length -eq 0) {
        throw "Deployment-mode manifest is empty: $Path"
    }

    try {
        $manifest = $raw | ConvertFrom-Json -Depth 16
    }
    catch {
        throw "Failed to parse deployment-mode manifest at ${Path}: $($_.Exception.Message)"
    }

    if ($null -eq $manifest) {
        throw "Deployment-mode manifest parsed to null: $Path"
    }
    if (-not $manifest.PSObject.Properties.Match("deploymentModes").Count) {
        throw "Deployment-mode manifest missing 'deploymentModes' field: $Path"
    }

    return $manifest
}

function Get-DeploymentModeConfig {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string]$Mode)
    if (-not $Script:DeploymentModeConfigs.ContainsKey($Mode)) {
        throw "Unknown deployment mode: $Mode (expected one of: $($Script:DeploymentModeConfigs.Keys -join ', '))"
    }
    return $Script:DeploymentModeConfigs[$Mode]
}

function Get-ManifestModeStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [Parameter(Mandatory)] [string]$Mode
    )

    $modes = $Manifest.deploymentModes
    if ($null -eq $modes) { return $null }
    if (-not $modes.PSObject.Properties.Match($Mode).Count) {
        return $null
    }
    $entry = $modes.$Mode
    if ($null -eq $entry) { return $null }
    if (-not $entry.PSObject.Properties.Match("status").Count) {
        return $null
    }
    return [string]$entry.status
}

function Get-DeploymentModeConfigFromManifest {
    <#
    .SYNOPSIS
        Builds a deployment-mode config from the manifest's schema 1.1.0 per-mode fields,
        falling back to the hardcoded $Script:DeploymentModeConfigs entry when manifest fields
        are missing or incomplete (so older manifest revisions still work).
    .DESCRIPTION
        The manifest fields consumed are:
          deploymentModes.<mode>.requiredProjectProperties[0]   -> ProjectProperty
          deploymentModes.<mode>.requiredAnalyzerProperties[0]  -> AnalyzerProperty
          deploymentModes.<mode>.warningPatterns[]              -> WarningRegex (joined as alternation)
        DisplayName, PublishArg, and Mode are always taken from the hardcoded fallback so the
        publish-arg shape stays stable across manifest edits.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [AllowNull()] $Manifest,
        [Parameter(Mandatory)] [string]$Mode
    )

    $fallback = Get-DeploymentModeConfig -Mode $Mode

    if ($null -eq $Manifest -or -not $Manifest.PSObject.Properties.Match('deploymentModes').Count) {
        return $fallback
    }
    $modes = $Manifest.deploymentModes
    if (-not $modes.PSObject.Properties.Match($Mode).Count) {
        return $fallback
    }
    $entry = $modes.$Mode
    if ($null -eq $entry) {
        return $fallback
    }

    $projectProp = $fallback.ProjectProperty
    if ($entry.PSObject.Properties.Match('requiredProjectProperties').Count) {
        $arr = @($entry.requiredProjectProperties)
        if ($arr.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($arr[0])) {
            $projectProp = [string]$arr[0]
        }
    }

    $analyzerProp = $fallback.AnalyzerProperty
    if ($entry.PSObject.Properties.Match('requiredAnalyzerProperties').Count) {
        $arr = @($entry.requiredAnalyzerProperties)
        if ($arr.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($arr[0])) {
            $analyzerProp = [string]$arr[0]
        }
    }

    $warningRegex = $fallback.WarningRegex
    if ($entry.PSObject.Properties.Match('warningPatterns').Count) {
        $patterns = @($entry.warningPatterns | Where-Object { $_ -and -not [string]::IsNullOrWhiteSpace($_) })
        if ($patterns.Count -gt 0) {
            # join as alternation; each pattern is a regex fragment, escaped only for known control chars
            $warningRegex = '(?i)' + (($patterns | ForEach-Object { [regex]::Escape($_) }) -join '|')
        }
    }

    return [pscustomobject]@{
        Mode             = $fallback.Mode
        DisplayName      = $fallback.DisplayName
        ProjectProperty  = $projectProp
        AnalyzerProperty = $analyzerProp
        PublishArg       = $fallback.PublishArg
        WarningRegex     = $warningRegex
        Source           = 'manifest'
    }
}

function Convert-RequiredProjectPropertyExpectation {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string]$Entry)

    $raw = $Entry.Trim()
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "requiredProjectProperties entry cannot be empty"
    }

    $parts = $raw -split "=", 2
    $name = $parts[0].Trim()
    if ([string]::IsNullOrWhiteSpace($name)) {
        throw "requiredProjectProperties entry '$Entry' is missing a property name"
    }

    $expectedValue = if ($parts.Count -gt 1) { $parts[1].Trim() } else { "true" }
    if ($expectedValue -notin @("true", "false")) {
        throw "requiredProjectProperties entry '$Entry' must use '=true' or '=false' when an expected value is supplied"
    }

    return [pscustomobject]@{
        Name          = $name
        ExpectedValue = $expectedValue
        Raw           = $raw
    }
}

function Test-CsprojProperty {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$CsprojPath,
        [Parameter(Mandatory)] [string]$Property
    )

    $result = [ordered]@{
        Path     = $CsprojPath
        Property = $Property
        Found    = $false
        Value    = $null
        Truthy   = $false
        Error    = $null
    }

    if (-not (Test-Path -LiteralPath $CsprojPath)) {
        $result.Error = "csproj not found"
        return [pscustomobject]$result
    }

    try {
        [xml]$xml = Get-Content -LiteralPath $CsprojPath -Raw -Encoding UTF8
    }
    catch {
        $result.Error = "csproj parse error: $($_.Exception.Message)"
        return [pscustomobject]$result
    }

    $node = $null
    if ($xml.DocumentElement) {
        $node = $xml.SelectSingleNode("//PropertyGroup/$Property")
    }
    if ($null -eq $node) {
        return [pscustomobject]$result
    }

    $result.Found = $true
    $result.Value = [string]$node.InnerText
    $result.Truthy = Test-IsTruthyMsBuildValue -Value $result.Value
    return [pscustomobject]$result
}

function Test-CsprojPropertyExpectation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$CsprojPath,
        [Parameter(Mandatory)] [string]$Entry
    )

    $expectation = Convert-RequiredProjectPropertyExpectation -Entry $Entry
    $observation = Test-CsprojProperty -CsprojPath $CsprojPath -Property $expectation.Name
    $actualValue = if ($null -eq $observation.Value) { "" } else { ([string]$observation.Value).Trim() }
    $matched = $observation.Found -and
        [string]::IsNullOrWhiteSpace([string]$observation.Error) -and
        $actualValue -ieq $expectation.ExpectedValue

    return [pscustomobject]@{
        Path          = $observation.Path
        Property      = $expectation.Name
        ExpectedValue = $expectation.ExpectedValue
        Found         = $observation.Found
        Value         = $observation.Value
        Matched       = $matched
        Error         = $observation.Error
        Raw           = $expectation.Raw
    }
}

function Get-DefaultProjectPaths {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string]$RepoRoot)

    $srcDir = Join-Path $RepoRoot "src"
    if (-not (Test-Path -LiteralPath $srcDir)) {
        return @()
    }
    return @(
        Get-ChildItem -LiteralPath $srcDir -Recurse -Filter "Cephalon.*.csproj" -File `
            | ForEach-Object { $_.FullName }
    )
}

function Get-ProjectPropertyAudit {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$RepoRoot,
        [Parameter(Mandatory)] [string]$Property,
        [string[]]$ProjectPaths
    )

    if (-not $ProjectPaths -or $ProjectPaths.Count -eq 0) {
        $ProjectPaths = Get-DefaultProjectPaths -RepoRoot $RepoRoot
    }

    $results = @()
    foreach ($p in $ProjectPaths) {
        $results += Test-CsprojProperty -CsprojPath $p -Property $Property
    }

    $passed = @($results | Where-Object { $_.Truthy })
    $unset = @($results | Where-Object { -not $_.Found -and -not $_.Error })
    $errored = @($results | Where-Object { $_.Error })
    $falsy = @($results | Where-Object { $_.Found -and -not $_.Truthy })

    return [pscustomobject]@{
        Property        = $Property
        TotalScanned    = $results.Count
        PassedCount     = $passed.Count
        UnsetCount      = $unset.Count
        FalsyCount      = $falsy.Count
        ErrorCount      = $errored.Count
        Passed          = $passed
        Unset           = $unset
        Falsy           = $falsy
        Errored         = $errored
        ScannedProjects = $ProjectPaths
    }
}

function Resolve-PackageProjectPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$RepoRoot,
        [Parameter(Mandatory)] [string]$PackageName
    )

    return Join-Path $RepoRoot (Join-Path "src" (Join-Path $PackageName "$PackageName.csproj"))
}

function Get-DeploymentModePackageClaimAudits {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [Parameter(Mandatory)] [string]$Mode,
        [Parameter(Mandatory)] [string]$RepoRoot
    )

    if ($null -eq $Manifest -or -not $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count) {
        return @()
    }

    $eligibility = $Manifest.deploymentModeEligibility
    if ($null -eq $eligibility -or -not $eligibility.PSObject.Properties.Match("packages").Count) {
        return @()
    }

    $audits = @()
    foreach ($pkg in @($eligibility.packages)) {
        $supportedModes = @($pkg.supportedModes | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($supportedModes -notcontains $Mode) {
            continue
        }

        $packageName = [string]$pkg.packageName
        $projectPath = Resolve-PackageProjectPath -RepoRoot $RepoRoot -PackageName $packageName
        $requiredEntries = @($pkg.requiredProjectProperties | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $propertyResults = @()
        $reasons = @()

        if ([string]::IsNullOrWhiteSpace($packageName)) {
            $reasons += "package-scoped claim is missing packageName"
        }

        $claimAuditTier = [string]$pkg.claimAuditTier
        if ($claimAuditTier -ne "clean-baseline") {
            $reasons += "package-scoped claim for '$packageName' requires claimAuditTier 'clean-baseline' but found '$claimAuditTier'"
        }

        if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
            $reasons += "package project not found at $projectPath"
        }

        if ($requiredEntries.Count -eq 0) {
            $reasons += "package-scoped claim for '$packageName' has no requiredProjectProperties entries"
        }

        foreach ($entry in $requiredEntries) {
            try {
                $propertyResults += Test-CsprojPropertyExpectation -CsprojPath $projectPath -Entry $entry
            }
            catch {
                $reasons += $_.Exception.Message
            }
        }

        foreach ($failed in @($propertyResults | Where-Object { -not $_.Matched })) {
            if ($failed.Error) {
                $reasons += "$packageName expected $($failed.Property)=$($failed.ExpectedValue) but audit failed: $($failed.Error)"
            }
            elseif (-not $failed.Found) {
                $reasons += "$packageName expected $($failed.Property)=$($failed.ExpectedValue) but the property was not declared"
            }
            else {
                $reasons += "$packageName expected $($failed.Property)=$($failed.ExpectedValue) but found '$($failed.Value)'"
            }
        }

        $verdict = if ($reasons.Count -eq 0) { "claim-truthful" } else { "claim-overstated" }
        $audits += [pscustomobject]@{
            Mode                      = $Mode
            PackageName               = $packageName
            NugetId                   = [string]$pkg.nugetId
            ClaimAuditTier            = $claimAuditTier
            SupportedModes            = $supportedModes
            ProjectPath               = $projectPath
            RequiredProjectProperties = $requiredEntries
            PropertyResults           = $propertyResults
            Verdict                   = $verdict
            Reasons                   = if ($reasons.Count -eq 0) { @("package-scoped claim properties match manifest") } else { $reasons }
        }
    }

    return $audits
}

function Get-AnalyzerAudit {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$RepoRoot,
        [Parameter(Mandatory)] [string]$AnalyzerProperty,
        [string[]]$ProjectPaths
    )

    if (-not $ProjectPaths -or $ProjectPaths.Count -eq 0) {
        $ProjectPaths = Get-DefaultProjectPaths -RepoRoot $RepoRoot
    }

    $results = @()
    foreach ($p in $ProjectPaths) {
        $results += Test-CsprojProperty -CsprojPath $p -Property $AnalyzerProperty
    }

    $enabled = @($results | Where-Object { $_.Truthy })
    $missing = @($results | Where-Object { -not $_.Truthy })

    return [pscustomobject]@{
        AnalyzerProperty       = $AnalyzerProperty
        TotalScanned           = $results.Count
        EnabledCount           = $enabled.Count
        DisabledOrMissingCount = $missing.Count
        Enabled                = $enabled
        DisabledOrMissing      = $missing
        ScannedProjects        = $ProjectPaths
    }
}

function New-PackagesLockSnapshot {
    [CmdletBinding()]
    param([string]$Root = "")

    $snapshotRoot = if ([string]::IsNullOrWhiteSpace($Root)) {
        (Get-Location).Path
    }
    else {
        (Resolve-Path -LiteralPath $Root).Path
    }

    $files = @(
        Get-ChildItem -Path $snapshotRoot -Recurse -Filter "packages.lock.json" -File -ErrorAction SilentlyContinue
    )
    $contents = @{}
    foreach ($file in $files) {
        $contents[$file.FullName] = [System.IO.File]::ReadAllBytes($file.FullName)
    }

    [pscustomobject]@{
        Root     = $snapshotRoot
        Contents = $contents
    }
}

function Restore-PackagesLockSnapshot {
    [CmdletBinding()]
    param([Parameter(Mandatory)] $Snapshot)

    if ($null -eq $Snapshot -or [string]::IsNullOrWhiteSpace([string]$Snapshot.Root)) {
        return
    }

    $knownPaths = @($Snapshot.Contents.Keys)
    $currentFiles = @(
        Get-ChildItem -Path $Snapshot.Root -Recurse -Filter "packages.lock.json" -File -ErrorAction SilentlyContinue
    )

    foreach ($file in $currentFiles) {
        if ($knownPaths -notcontains $file.FullName) {
            Remove-Item -LiteralPath $file.FullName -Force
        }
    }

    foreach ($path in $knownPaths) {
        [System.IO.File]::WriteAllBytes($path, [byte[]]$Snapshot.Contents[$path])
    }
}

function Invoke-PublishProbe {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $ModeConfig,
        [string[]]$Targets = @(),
        [string]$Configuration = "Release",
        [string]$DotnetCommand = "dotnet",
        [string]$RepoRoot = ""
    )

    if (-not $Targets -or $Targets.Count -eq 0) {
        return [pscustomobject]@{
            Mode    = $ModeConfig.Mode
            Skipped = $true
            Reason  = "no publish targets supplied"
            Targets = @()
        }
    }

    $perTarget = @()
    $lockSnapshot = New-PackagesLockSnapshot -Root $RepoRoot
    try {
        foreach ($t in $Targets) {
            $publishArgs = @(
                "publish",
                $t,
                "-c",
                $Configuration,
                $ModeConfig.PublishArg
            )
            $output = & $DotnetCommand @publishArgs 2>&1
            $exitCode = $LASTEXITCODE
            $lines = @($output | ForEach-Object { [string]$_ })
            $warningLines = @($lines | Where-Object { $_ -match $ModeConfig.WarningRegex -or $_ -match "(?i)warning|warn" })
            $errorLines = @($lines | Where-Object { $_ -match "(?i)\berror\b|fatal" })

            $perTarget += [pscustomobject]@{
                Target       = $t
                ExitCode     = $exitCode
                WarningCount = $warningLines.Count
                ErrorCount   = $errorLines.Count
                Warnings     = $warningLines
                Errors       = $errorLines
                Success      = ($exitCode -eq 0 -and $errorLines.Count -eq 0)
            }
        }
    }
    finally {
        Restore-PackagesLockSnapshot -Snapshot $lockSnapshot
    }

    return [pscustomobject]@{
        Mode    = $ModeConfig.Mode
        Skipped = $false
        Reason  = $null
        Targets = $perTarget
    }
}

function Compute-ModeVerdict {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$Mode,
        [Parameter(Mandatory)] [AllowEmptyString()] [AllowNull()] $ManifestStatus,
        $PropertyAudit,
        $AnalyzerAudit,
        $PublishProbe,
        $PackageClaimAudits = @(),
        [bool]$PropertyAuditSkipped = $false,
        [bool]$AnalyzerSkipped = $false,
        [bool]$PublishSkipped = $false
    )

    $reasons = @()
    $statusString = if ($null -eq $ManifestStatus) { "" } else { [string]$ManifestStatus }

    if ([string]::IsNullOrWhiteSpace($statusString)) {
        return [pscustomobject]@{
            Mode    = $Mode
            Verdict = "unknown"
            Reasons = @("manifest does not declare a status for mode '$Mode'")
        }
    }

    $packageClaims = @(
        $PackageClaimAudits |
            Where-Object {
                $null -ne $_ -and
                $_.PSObject.Properties.Match("Mode").Count -gt 0 -and
                $_.Mode -eq $Mode
            }
    )
    $failedPackageClaims = @($packageClaims | Where-Object { $_.Verdict -ne "claim-truthful" })
    if ($failedPackageClaims.Count -gt 0) {
        foreach ($claim in $failedPackageClaims) {
            $reasons += "package-scoped claim failed for $($claim.PackageName): $($claim.Reasons -join '; ')"
        }

        return [pscustomobject]@{
            Mode    = $Mode
            Verdict = "claim-overstated"
            Reasons = $reasons
        }
    }

    if ($statusString -eq "not-claimed") {
        if ($PropertyAuditSkipped -or -not $PropertyAudit) {
            return [pscustomobject]@{
                Mode    = $Mode
                Verdict = "not-claimed"
                Reasons = @("manifest claim is not-claimed; property audit skipped — no drift detection")
            }
        }

        $passedProjects = if ($PropertyAudit.PSObject.Properties.Match("Passed").Count) { @($PropertyAudit.Passed) } else { @() }
        $scopedClaimPaths = @($packageClaims | Where-Object { $_.Verdict -eq "claim-truthful" } | ForEach-Object { $_.ProjectPath })
        $unexpectedPassedProjects = @($passedProjects | Where-Object { $scopedClaimPaths -notcontains $_.Path })
        if ($PropertyAudit.PassedCount -eq 0 -or $unexpectedPassedProjects.Count -eq 0) {
            $reason = if ($PropertyAudit.PassedCount -eq 0) {
                "manifest claim is not-claimed; no project sets $($PropertyAudit.Property)"
            }
            else {
                $packageNames = ($packageClaims | ForEach-Object { $_.PackageName }) -join ", "
                "manifest claim is not-claimed globally; package-scoped claim(s) account for all projects setting $($PropertyAudit.Property): $packageNames"
            }

            return [pscustomobject]@{
                Mode    = $Mode
                Verdict = "not-claimed"
                Reasons = @($reason)
            }
        }
        $reasons += "manifest claim is not-claimed but $($unexpectedPassedProjects.Count) unscoped project(s) set $($PropertyAudit.Property)"
        return [pscustomobject]@{
            Mode    = $Mode
            Verdict = "not-claimed-with-property-drift"
            Reasons = $reasons
        }
    }

    if ($statusString -eq "claimed") {
        $hasIssues = $false

        if ($PropertyAuditSkipped -or -not $PropertyAudit) {
            $reasons += "manifest claims '$Mode' but property audit was skipped — claim cannot be verified"
            $hasIssues = $true
        }
        elseif ($PropertyAudit.PassedCount -eq 0) {
            $reasons += "manifest claims '$Mode' but no project actually sets $($PropertyAudit.Property)"
            $hasIssues = $true
        }

        if (-not $AnalyzerSkipped -and $AnalyzerAudit -and $PropertyAudit) {
            if ($AnalyzerAudit.EnabledCount -lt $PropertyAudit.PassedCount) {
                $reasons += "manifest claims '$Mode' but analyzer $($AnalyzerAudit.AnalyzerProperty) is not enabled on all $($PropertyAudit.PassedCount) claiming project(s) (only $($AnalyzerAudit.EnabledCount) enabled)"
                $hasIssues = $true
            }
        }
        elseif ($AnalyzerSkipped) {
            $reasons += "analyzer audit skipped — analyzer enablement not verified for '$Mode'"
        }

        if (-not $PublishSkipped -and $PublishProbe -and -not $PublishProbe.Skipped) {
            $failed = @($PublishProbe.Targets | Where-Object { -not $_.Success })
            if ($failed.Count -gt 0) {
                $reasons += "publish probe failed for $($failed.Count) of $($PublishProbe.Targets.Count) target(s)"
                $hasIssues = $true
            }
            $totalWarnings = ($PublishProbe.Targets | Measure-Object -Property WarningCount -Sum).Sum
            if ($totalWarnings -and $totalWarnings -gt 0) {
                $reasons += "publish probe emitted $totalWarnings warning(s) across $($PublishProbe.Targets.Count) target(s)"
                $hasIssues = $true
            }
        }
        elseif (-not $PublishSkipped -and $PublishProbe -and $PublishProbe.Skipped) {
            $reasons += "publish probe was not run: $($PublishProbe.Reason)"
        }
        elseif ($PublishSkipped) {
            $reasons += "publish probe skipped — publish-time enforcement not verified for '$Mode'"
        }

        if ($hasIssues) {
            return [pscustomobject]@{
                Mode    = $Mode
                Verdict = "claim-overstated"
                Reasons = $reasons
            }
        }
        return [pscustomobject]@{
            Mode    = $Mode
            Verdict = "claim-truthful"
            Reasons = if ($reasons.Count -gt 0) { $reasons } else { @("all audits and probes pass") }
        }
    }

    return [pscustomobject]@{
        Mode    = $Mode
        Verdict = "unknown"
        Reasons = @("manifest status was '$statusString' which is not one of the supported values (claimed, not-claimed)")
    }
}

function Compute-AggregateVerdict {
    [CmdletBinding()]
    param([Parameter(Mandatory)] $ModeVerdicts)

    if (-not $ModeVerdicts -or $ModeVerdicts.Count -eq 0) {
        return "not-claimed"
    }

    $verdicts = @($ModeVerdicts | ForEach-Object { $_.Verdict })

    if ($verdicts -contains "claim-overstated") { return "claim-overstated" }
    if ($verdicts -contains "not-claimed-with-property-drift") { return "not-claimed-with-property-drift" }
    if ($verdicts -contains "unknown") { return "unknown" }

    $unique = @($verdicts | Select-Object -Unique)
    if ($unique.Count -eq 1) { return $unique[0] }

    return "mixed"
}

function Write-ValidationReport {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$OutputDir,
        [Parameter(Mandatory)] $Report
    )

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
    }

    $jsonPath = Join-Path $OutputDir "claim-validation-report.json"
    $mdPath = Join-Path $OutputDir "README.md"

    $Report | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("# Deployment-mode claim validation report")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("Generated at: $($Report.GeneratedAtUtc)")
    [void]$sb.AppendLine("Repo root: $($Report.RepoRoot)")
    [void]$sb.AppendLine("Manifest: $($Report.ManifestPath)")
    [void]$sb.AppendLine("Configuration: $($Report.Configuration)")
    [void]$sb.AppendLine("Requested mode: $($Report.DeploymentMode)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## Aggregate verdict")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("**$($Report.AggregateVerdict)**")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## Per-mode verdicts")
    [void]$sb.AppendLine("")
    foreach ($v in $Report.Verdicts) {
        [void]$sb.AppendLine("- **$($v.Mode)**: $($v.Verdict)")
        foreach ($r in $v.Reasons) {
            [void]$sb.AppendLine("  - $r")
        }
    }
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## Package-scoped claims")
    [void]$sb.AppendLine("")
    $packageClaimRows = @($Report.Modes | ForEach-Object { $_.PackageClaimAudits } | Where-Object { $null -ne $_ })
    if ($packageClaimRows.Count -eq 0) {
        [void]$sb.AppendLine("No package-scoped claims were declared for the requested mode set.")
    }
    else {
        foreach ($claim in $packageClaimRows) {
            [void]$sb.AppendLine("- **$($claim.PackageName)** / **$($claim.Mode)**: $($claim.Verdict)")
            foreach ($r in $claim.Reasons) {
                [void]$sb.AppendLine("  - $r")
            }
        }
    }
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("See ``claim-validation-report.json`` next to this README for the full structured report.")

    Set-Content -LiteralPath $mdPath -Value $sb.ToString() -Encoding UTF8

    return [pscustomobject]@{
        JsonPath     = $jsonPath
        MarkdownPath = $mdPath
    }
}

function Invoke-DeploymentModeClaimValidation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$DeploymentMode,
        [string]$Configuration = "Release",
        [string]$OutputPath = "artifacts/deployment-mode-claims-release",
        [Parameter(Mandatory)] [string]$ManifestPath,
        [Parameter(Mandatory)] [string]$RepoRoot,
        [string[]]$PublishTargets = @(),
        [string[]]$ProjectPaths = @(),
        [switch]$SkipPublish,
        [switch]$SkipAnalyzerCheck,
        [switch]$SkipPropertyAudit,
        [string]$DotnetCommand = "dotnet"
    )

    if (-not [System.IO.Path]::IsPathRooted($ManifestPath)) {
        $ManifestPath = Join-Path $RepoRoot $ManifestPath
    }
    if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath = Join-Path $RepoRoot $OutputPath
    }

    Invoke-Step -Title "Loading manifest" -Detail $ManifestPath
    $manifest = Read-DeploymentModeManifest -Path $ManifestPath

    # When the caller did not pass explicit -PublishTargets and did not pass -SkipPublish, default to
    # the manifest-declared representativePublishTargets.projects list so the publish probe runs
    # against the deliberately-staged audit set without forcing every caller to repeat the list.
    if ((-not $SkipPublish) -and ($null -eq $PublishTargets -or $PublishTargets.Count -eq 0)) {
        $manifestTargets = $null
        if ($manifest.PSObject.Properties.Match("representativePublishTargets").Count -gt 0) {
            $manifestTargets = $manifest.representativePublishTargets.projects
        }
        if ($null -ne $manifestTargets -and $manifestTargets.Count -gt 0) {
            $PublishTargets = @($manifestTargets)
            Invoke-Step -Title "Default publish targets from manifest" -Detail ("count=" + $PublishTargets.Count)
        }
    }

    $modesToCheck = if ($DeploymentMode -eq "all") {
        @("trim", "nativeAot", "singleFile")
    }
    else {
        @($DeploymentMode)
    }

    $modeReports = @()
    foreach ($mode in $modesToCheck) {
        Invoke-Step -Title "Validating mode" -Detail $mode
        $cfg = Get-DeploymentModeConfigFromManifest -Manifest $manifest -Mode $mode
        $manifestStatus = Get-ManifestModeStatus -Manifest $manifest -Mode $mode
        $packageClaimAudits = Get-DeploymentModePackageClaimAudits -Manifest $manifest -Mode $mode -RepoRoot $RepoRoot

        $propAudit = $null
        if (-not $SkipPropertyAudit) {
            $propAudit = Get-ProjectPropertyAudit -RepoRoot $RepoRoot -Property $cfg.ProjectProperty -ProjectPaths $ProjectPaths
        }

        $analyzerAudit = $null
        if (-not $SkipAnalyzerCheck) {
            $analyzerAudit = Get-AnalyzerAudit -RepoRoot $RepoRoot -AnalyzerProperty $cfg.AnalyzerProperty -ProjectPaths $ProjectPaths
        }

        $publishProbe = $null
        if (-not $SkipPublish) {
            $publishProbe = Invoke-PublishProbe -ModeConfig $cfg -Targets $PublishTargets -Configuration $Configuration -DotnetCommand $DotnetCommand -RepoRoot $RepoRoot
        }

        $verdict = Compute-ModeVerdict -Mode $mode -ManifestStatus $manifestStatus `
            -PropertyAudit $propAudit -AnalyzerAudit $analyzerAudit -PublishProbe $publishProbe `
            -PackageClaimAudits $packageClaimAudits `
            -PropertyAuditSkipped:$SkipPropertyAudit `
            -AnalyzerSkipped:$SkipAnalyzerCheck `
            -PublishSkipped:$SkipPublish

        $modeReports += [pscustomobject]@{
            Mode           = $mode
            ManifestStatus = $manifestStatus
            PropertyAudit  = $propAudit
            AnalyzerAudit  = $analyzerAudit
            PublishProbe   = $publishProbe
            PackageClaimAudits = $packageClaimAudits
            Verdict        = $verdict.Verdict
            Reasons        = $verdict.Reasons
        }
    }

    $aggregateVerdict = Compute-AggregateVerdict -ModeVerdicts $modeReports

    $report = [ordered]@{
        GeneratedAtUtc      = (Get-Date).ToUniversalTime().ToString("o")
        DeploymentMode      = $DeploymentMode
        Configuration       = $Configuration
        RepoRoot            = $RepoRoot
        ManifestPath        = $ManifestPath
        ManifestSnapshot    = $manifest
        Modes               = $modeReports
        Verdicts            = @($modeReports | ForEach-Object { [pscustomobject]@{ Mode = $_.Mode; Verdict = $_.Verdict; Reasons = $_.Reasons } })
        AggregateVerdict    = $aggregateVerdict
        ValidVerdicts       = $Script:ValidVerdicts
        ValidationStrategy  = if ($SkipPublish) { "audit-only" } else { "publish-required" }
    }
    $reportObj = [pscustomobject]$report

    Invoke-Step -Title "Writing report" -Detail $OutputPath
    $paths = Write-ValidationReport -OutputDir $OutputPath -Report $reportObj

    Invoke-Step -Title "Aggregate verdict" -Detail $aggregateVerdict

    if ($aggregateVerdict -eq "claim-overstated") {
        throw "claim-overstated: see $($paths.JsonPath) for details"
    }

    return [pscustomobject]@{
        Report = $reportObj
        Paths  = $paths
    }
}

# === Main entry guard ===
# Tests dot-source this script and skip the auto-run by setting CEPHALON_VALIDATE_DEPLOYMENT_MODE_NO_RUN.
if (-not $env:CEPHALON_VALIDATE_DEPLOYMENT_MODE_NO_RUN) {
    $resolvedRoot = $RepoRoot
    if (-not $resolvedRoot) {
        $resolvedRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    }

    $invokeArgs = @{
        DeploymentMode      = $DeploymentMode
        Configuration       = $Configuration
        OutputPath          = $OutputPath
        ManifestPath        = $ManifestPath
        RepoRoot            = $resolvedRoot
        PublishTargets      = $PublishTargets
        ProjectPaths        = $ProjectPaths
        SkipPublish         = $SkipPublish
        SkipAnalyzerCheck   = $SkipAnalyzerCheck
        SkipPropertyAudit   = $SkipPropertyAudit
        DotnetCommand       = $DotnetCommand
    }
    $null = Invoke-DeploymentModeClaimValidation @invokeArgs
}
