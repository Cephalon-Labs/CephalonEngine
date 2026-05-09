#requires -Version 7.0

<#
.SYNOPSIS
    Validates Cephalon's deployment-mode claim truth (trim, Native AOT, single-file).

.DESCRIPTION
    Audits whether the support contract recorded in scripts/deployment-mode-support.json
    matches what the repository actually proves. The harness runs six phases:

    1. project-property audit   — does each src/Cephalon.* csproj that the manifest claims
                                  the mode for actually set the matching MSBuild property?
    2. analyzer audit           — is the matching analyzer enabled where the property is set?
    3. publish probe (optional) — does dotnet publish for the requested mode complete cleanly
                                  on the representative target set?
    4. package-scoped claims    — do deploymentModeEligibility package entries that opt into
                                  a mode carry the expected per-package project properties?
    5. publish-probe policy     — emit the manifest-backed release-validation gate posture
                                  so audit-only runs are visible in release artifacts.
    6. hazard inventory         — emit the manifest-backed per-package hazard, scoped-claim,
                                  transitive-hazard, and boundary-annotation inventory for
                                  release managers and follow-up automation.

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

function ConvertTo-BooleanValue {
    [CmdletBinding()]
    param(
        $Value,
        [bool]$DefaultValue = $false
    )

    if ($null -eq $Value) {
        return $DefaultValue
    }
    if ($Value -is [bool]) {
        return $Value
    }

    $parsed = $false
    if ([bool]::TryParse([string]$Value, [ref]$parsed)) {
        return $parsed
    }

    return $DefaultValue
}

function ConvertTo-StringArray {
    [CmdletBinding()]
    param($Value)

    if ($null -eq $Value) {
        return @()
    }

    return @(
        $Value |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Get-ObjectPropertyValue {
    [CmdletBinding()]
    param(
        $Object,
        [Parameter(Mandatory)] [string]$PropertyName,
        $DefaultValue = $null
    )

    if ($null -ne $Object -and $Object.PSObject.Properties.Match($PropertyName).Count -gt 0) {
        return $Object.$PropertyName
    }

    return $DefaultValue
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

function Get-PublishProbePolicySnapshot {
    [CmdletBinding()]
    param(
        $Manifest,
        [bool]$CurrentRunSkipsPublish,
        [int]$RepresentativePublishTargetCount = 0
    )

    $policy = $null
    $source = "default"
    if ($null -ne $Manifest -and $Manifest.PSObject.Properties.Match("publishProbePolicy").Count -gt 0) {
        $policy = $Manifest.publishProbePolicy
        $source = "manifest"
    }

    $releaseValidationMode = [string](Get-ObjectPropertyValue -Object $policy -PropertyName "releaseValidationMode" -DefaultValue "audit-only")
    if ([string]::IsNullOrWhiteSpace($releaseValidationMode)) {
        $releaseValidationMode = "audit-only"
    }

    $releaseValidationDeploymentModes = @(ConvertTo-StringArray -Value (Get-ObjectPropertyValue -Object $policy -PropertyName "releaseValidationDeploymentModes" -DefaultValue @()))
    if ($releaseValidationDeploymentModes.Count -eq 0) {
        $releaseValidationDeploymentModes = @("all")
    }

    $gatedModes = @(ConvertTo-StringArray -Value (Get-ObjectPropertyValue -Object $policy -PropertyName "gatedModes" -DefaultValue @()))
    $auditOnlyModes = @(ConvertTo-StringArray -Value (Get-ObjectPropertyValue -Object $policy -PropertyName "auditOnlyModes" -DefaultValue @()))

    $gatePromotion = [string](Get-ObjectPropertyValue -Object $policy -PropertyName "gatePromotion" -DefaultValue "requires-deliberate-release-manager-decision")
    if ([string]::IsNullOrWhiteSpace($gatePromotion)) {
        $gatePromotion = "requires-deliberate-release-manager-decision"
    }

    $promotionRequirements = @()
    if ($null -ne $policy -and $policy.PSObject.Properties.Match("promotionRequirements").Count -gt 0) {
        $promotionRequirements = @(
            $policy.promotionRequirements |
                ForEach-Object { [string]$_ } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
    }

    $releaseValidationSkipsPublish = ConvertTo-BooleanValue -Value (Get-ObjectPropertyValue -Object $policy -PropertyName "releaseValidationSkipsPublish" -DefaultValue $true) -DefaultValue $true
    $nonOptOutGate = ConvertTo-BooleanValue -Value (Get-ObjectPropertyValue -Object $policy -PropertyName "nonOptOutGate" -DefaultValue $false) -DefaultValue $false

    return [pscustomobject]@{
        Source                        = $source
        ReleaseValidationMode         = $releaseValidationMode
        ReleaseValidationDeploymentModes = $releaseValidationDeploymentModes
        ReleaseValidationSkipsPublish = $releaseValidationSkipsPublish
        CurrentRunSkipsPublish        = $CurrentRunSkipsPublish
        NonOptOutGate                 = $nonOptOutGate
        GatedModes                    = $gatedModes
        AuditOnlyModes                = $auditOnlyModes
        FailureBlocksRelease          = ConvertTo-BooleanValue -Value (Get-ObjectPropertyValue -Object $policy -PropertyName "failureBlocksRelease" -DefaultValue $nonOptOutGate) -DefaultValue $nonOptOutGate
        FailOnWarnings                = ConvertTo-BooleanValue -Value (Get-ObjectPropertyValue -Object $policy -PropertyName "failOnWarnings" -DefaultValue $true) -DefaultValue $true
        GatePromotion                 = $gatePromotion
        RepresentativePublishTargets  = $RepresentativePublishTargetCount
        PromotionRequirements         = $promotionRequirements
    }
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

function Get-DeploymentModeLockFilePackageRows {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$RepoRoot,
        [string[]]$LockFileGlobs = @()
    )

    if ([string]::IsNullOrWhiteSpace($RepoRoot) -or -not (Test-Path -LiteralPath $RepoRoot -PathType Container)) {
        return @()
    }

    $normalizedGlobs = @(
        $LockFileGlobs |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { ($_ -replace '\\', '/') }
    )

    $lockFiles = Get-ChildItem -LiteralPath $RepoRoot -Recurse -Filter "packages.lock.json" -File -ErrorAction SilentlyContinue |
        Where-Object {
            if ($normalizedGlobs.Count -eq 0) { return $true }

            $relativePath = [System.IO.Path]::GetRelativePath($RepoRoot, $_.FullName) -replace '\\', '/'
            foreach ($glob in $normalizedGlobs) {
                $matcher = [System.Management.Automation.WildcardPattern]::new(
                    $glob,
                    [System.Management.Automation.WildcardOptions]::IgnoreCase)
                if ($matcher.IsMatch($relativePath)) {
                    return $true
                }
            }

            return $false
        }

    $rows = @()
    foreach ($lockFile in $lockFiles) {
        $relativePath = [System.IO.Path]::GetRelativePath($RepoRoot, $lockFile.FullName) -replace '\\', '/'
        try {
            $lockJson = Get-Content -LiteralPath $lockFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 100
        }
        catch {
            throw "Unable to parse packages lock file '$relativePath': $($_.Exception.Message)"
        }

        if ($null -eq $lockJson -or
            -not $lockJson.PSObject.Properties.Match("dependencies").Count -or
            $null -eq $lockJson.dependencies) {
            continue
        }

        foreach ($tfm in $lockJson.dependencies.PSObject.Properties) {
            foreach ($dependency in $tfm.Value.PSObject.Properties) {
                $dependencyValue = $dependency.Value
                $rows += [pscustomobject]@{
                    PackageId = [string]$dependency.Name
                    Type      = if ($null -ne $dependencyValue -and $dependencyValue.PSObject.Properties.Match("type").Count) { [string]$dependencyValue.type } else { "" }
                    Target    = [string]$tfm.Name
                    LockFile  = $relativePath
                }
            }
        }
    }

    return $rows
}

function Get-DeploymentModeTransitiveHazardAudit {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [string]$RepoRoot = ""
    )

    $emptyAudit = {
        param([string]$Status)
        [pscustomobject]@{
            Source            = "knownTransitiveHazardAudit"
            Status            = $Status
            TotalEntries      = 0
            MissingEntries    = 0
            LockFileCount     = 0
            PackageMatchCount = 0
            Entries           = @()
        }
    }

    if ($null -eq $Manifest -or
        -not $Manifest.PSObject.Properties.Match("knownTransitiveHazardAudit").Count -or
        $null -eq $Manifest.knownTransitiveHazardAudit) {
        return & $emptyAudit "not-configured"
    }

    $audit = $Manifest.knownTransitiveHazardAudit
    $entries = @()
    if ($audit.PSObject.Properties.Match("entries").Count -gt 0) {
        $entries = @($audit.entries | Where-Object { $null -ne $_ })
    }

    if ($entries.Count -eq 0) {
        return & $emptyAudit "not-configured"
    }

    $lockFileGlobs = @()
    if ($audit.PSObject.Properties.Match("lockFileGlobs").Count -gt 0) {
        $lockFileGlobs = @($audit.lockFileGlobs | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }

    $lockFilePackages = @()
    if (-not [string]::IsNullOrWhiteSpace($RepoRoot) -and (Test-Path -LiteralPath $RepoRoot -PathType Container)) {
        $lockFilePackages = @(Get-DeploymentModeLockFilePackageRows -RepoRoot $RepoRoot -LockFileGlobs $lockFileGlobs)
    }

    $notAudited = [string]::IsNullOrWhiteSpace($RepoRoot) -or -not (Test-Path -LiteralPath $RepoRoot -PathType Container)
    $auditRows = @()
    foreach ($entry in $entries) {
        $packagePattern = if ($entry.PSObject.Properties.Match("packagePattern").Count -gt 0) { [string]$entry.packagePattern } else { "" }
        $modes = @()
        if ($entry.PSObject.Properties.Match("modes").Count -gt 0) {
            $modes = @($entry.modes | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        }

        $minimumLockFileMatches = 1
        if ($entry.PSObject.Properties.Match("minimumLockFileMatches").Count -gt 0) {
            $minimumLockFileMatches = [Math]::Max(1, [int]$entry.minimumLockFileMatches)
        }

        $matches = @()
        if (-not $notAudited -and -not [string]::IsNullOrWhiteSpace($packagePattern)) {
            $matcher = [System.Management.Automation.WildcardPattern]::new(
                $packagePattern,
                [System.Management.Automation.WildcardOptions]::IgnoreCase)
            $matches = @($lockFilePackages | Where-Object { $matcher.IsMatch($_.PackageId) })
        }

        $matchedPackageIds = @($matches | ForEach-Object { $_.PackageId } | Sort-Object -Unique)
        $matchedLockFiles = @($matches | ForEach-Object { $_.LockFile } | Sort-Object -Unique)
        $entryStatus = if ($notAudited) {
            "not-audited"
        }
        elseif ([string]::IsNullOrWhiteSpace($packagePattern)) {
            "invalid-pattern"
        }
        elseif ($matchedLockFiles.Count -ge $minimumLockFileMatches) {
            "matched"
        }
        else {
            "missing-lock-file-match"
        }

        $auditRows += [pscustomobject]@{
            PackagePattern         = $packagePattern
            Modes                  = $modes
            MinimumLockFileMatches = $minimumLockFileMatches
            MatchCount             = $matchedLockFiles.Count
            MatchedPackageIds      = $matchedPackageIds
            MatchedLockFiles       = $matchedLockFiles
            Status                 = $entryStatus
            Evidence               = if ($entry.PSObject.Properties.Match("evidence").Count -gt 0) { [string]$entry.evidence } else { "" }
        }
    }

    $missingEntries = @($auditRows | Where-Object { $_.Status -ne "matched" }).Count
    $status = if ($notAudited) {
        "not-audited"
    }
    elseif ($missingEntries -gt 0) {
        "missing-lock-file-match"
    }
    else {
        "matched"
    }

    return [pscustomobject]@{
        Source            = "knownTransitiveHazardAudit"
        Status            = $status
        TotalEntries      = $auditRows.Count
        MissingEntries    = $missingEntries
        LockFileCount     = @($lockFilePackages | ForEach-Object { $_.LockFile } | Sort-Object -Unique).Count
        PackageMatchCount = @($auditRows | ForEach-Object { $_.MatchedPackageIds } | Sort-Object -Unique).Count
        Entries           = @($auditRows)
    }
}

function Get-DeploymentModeBoundaryAnnotationAudits {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [string]$RepoRoot = ""
    )

    $rows = @()
    if ($null -eq $Manifest -or
        -not $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count -or
        $null -eq $Manifest.deploymentModeEligibility -or
        -not $Manifest.deploymentModeEligibility.PSObject.Properties.Match("packages").Count) {
        return @()
    }

    foreach ($pkg in @($Manifest.deploymentModeEligibility.packages)) {
        if ($null -eq $pkg -or $pkg.PSObject.Properties.Match("knownHazards").Count -eq 0) { continue }

        $packageName = if ($pkg.PSObject.Properties.Match("packageName").Count -gt 0) { [string]$pkg.packageName } else { "" }
        foreach ($hazard in @($pkg.knownHazards)) {
            if ($null -eq $hazard -or $hazard.PSObject.Properties.Match("kind").Count -eq 0) { continue }

            $kind = [string]$hazard.kind
            if (-not [string]::Equals($kind, "dynamic-minimal-api-operator-route-binding", [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $site = if ($hazard.PSObject.Properties.Match("site").Count -gt 0) { [string]$hazard.site } else { "" }
            $sourceRelativePath = $site
            $lineNumber = 0
            $match = [regex]::Match($site.Trim(), '^(?<path>.+?\.cs)(?::(?<line>\d+))?$')
            if ($match.Success) {
                $sourceRelativePath = $match.Groups["path"].Value
                if ($match.Groups["line"].Success) {
                    $lineNumber = [int]$match.Groups["line"].Value
                }
            }

            $resolvedPath = $sourceRelativePath
            if (-not [System.IO.Path]::IsPathRooted($resolvedPath) -and -not [string]::IsNullOrWhiteSpace($RepoRoot)) {
                $resolvedPath = Join-Path $RepoRoot ($sourceRelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
            }

            $sourceExists = Test-Path -LiteralPath $resolvedPath -PathType Leaf
            $hasRequiresUnreferencedCode = $false
            $hasRequiresDynamicCode = $false
            if ($sourceExists) {
                $sourceLines = @(Get-Content -LiteralPath $resolvedPath -Encoding UTF8)
                if ($lineNumber -gt 0 -and $sourceLines.Count -gt 0 -and $lineNumber -le $sourceLines.Count) {
                    $start = [Math]::Max(1, $lineNumber - 8)
                    $end = [Math]::Min($sourceLines.Count, $lineNumber + 8)
                    $window = $sourceLines[($start - 1)..($end - 1)] -join "`n"
                }
                elseif ($lineNumber -eq 0) {
                    $window = $sourceLines -join "`n"
                }
                else {
                    $window = ""
                }

                $hasRequiresUnreferencedCode = $window -match '\[RequiresUnreferencedCode\('
                $hasRequiresDynamicCode = $window -match '\[RequiresDynamicCode\('
            }

            $status = if (-not $sourceExists) {
                "missing-source"
            }
            elseif ($hasRequiresUnreferencedCode -and $hasRequiresDynamicCode) {
                "annotated"
            }
            else {
                "missing-annotation"
            }

            $rows += [pscustomobject]@{
                PackageName              = $packageName
                HazardKind               = $kind
                Site                     = $site
                SourcePath               = $sourceRelativePath
                LineNumber               = $lineNumber
                SourceExists             = $sourceExists
                RequiresUnreferencedCode = $hasRequiresUnreferencedCode
                RequiresDynamicCode      = $hasRequiresDynamicCode
                Status                   = $status
            }
        }
    }

    return @($rows)
}

function Get-DeploymentModeCoreRouteDelegateAudits {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [string]$RepoRoot = ""
    )

    $rows = @()
    if ($null -eq $Manifest -or
        -not $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count -or
        $null -eq $Manifest.deploymentModeEligibility -or
        -not $Manifest.deploymentModeEligibility.PSObject.Properties.Match("packages").Count) {
        return @()
    }

    foreach ($pkg in @($Manifest.deploymentModeEligibility.packages)) {
        if ($null -eq $pkg -or $pkg.PSObject.Properties.Match("knownHazards").Count -eq 0) { continue }

        $packageName = if ($pkg.PSObject.Properties.Match("packageName").Count -gt 0) { [string]$pkg.packageName } else { "" }
        if (-not [string]::Equals($packageName, "Cephalon.AspNetCore", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        foreach ($hazard in @($pkg.knownHazards)) {
            if ($null -eq $hazard -or $hazard.PSObject.Properties.Match("kind").Count -eq 0) { continue }

            $kind = [string]$hazard.kind
            if (-not [string]::Equals($kind, "dynamic-minimal-api-operator-route-binding", [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $site = if ($hazard.PSObject.Properties.Match("site").Count -gt 0) { [string]$hazard.site } else { "" }
            $sourceRelativePath = $site
            $match = [regex]::Match($site.Trim(), '^(?<path>.+?\.cs)(?::(?<line>\d+))?$')
            if ($match.Success) {
                $sourceRelativePath = $match.Groups["path"].Value
            }

            $resolvedPath = $sourceRelativePath
            if (-not [System.IO.Path]::IsPathRooted($resolvedPath) -and -not [string]::IsNullOrWhiteSpace($RepoRoot)) {
                $resolvedPath = Join-Path $RepoRoot ($sourceRelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
            }

            $sourceExists = Test-Path -LiteralPath $resolvedPath -PathType Leaf
            $coreMethodFound = $false
            $requestDelegateHelperFound = $false
            $helperBlockFound = $false
            $coreRoutesUseMinimalApiMapGet = $false
            $coreRoutesUseRequestDelegateHelper = $false
            $helperAcceptsRequestDelegate = $false
            $helperUsesMapMethods = $false
            $failures = @()

            if (-not $sourceExists) {
                $failures += "source-missing"
            }
            else {
                $source = Get-Content -LiteralPath $resolvedPath -Raw -Encoding UTF8
                $coreStart = $source.IndexOf("private static void MapCephalonCoreOperatorRoutes(", [System.StringComparison]::Ordinal)
                $helperStart = $source.IndexOf("private static void MapGetRequestDelegate(", [System.StringComparison]::Ordinal)
                $nextHelperStart = $source.IndexOf("private static TService GetRequiredService", [System.StringComparison]::Ordinal)

                $coreMethodFound = $coreStart -ge 0
                $requestDelegateHelperFound = $helperStart -gt $coreStart
                $helperBlockFound = $requestDelegateHelperFound -and $nextHelperStart -gt $helperStart

                if (-not $coreMethodFound) {
                    $failures += "core-route-method-missing"
                }
                if (-not $requestDelegateHelperFound) {
                    $failures += "request-delegate-helper-missing"
                }
                if (-not $helperBlockFound) {
                    $failures += "request-delegate-helper-block-unparseable"
                }

                if ($coreMethodFound -and $requestDelegateHelperFound) {
                    $coreBlock = $source.Substring($coreStart, $helperStart - $coreStart)
                    $coreRoutesUseMinimalApiMapGet = $coreBlock -match '\.MapGet\('
                    $coreRoutesUseRequestDelegateHelper = $coreBlock -match 'MapGetRequestDelegate'
                    if ($coreRoutesUseMinimalApiMapGet) {
                        $failures += "core-routes-use-mapget-delegate-binding"
                    }
                    if (-not $coreRoutesUseRequestDelegateHelper) {
                        $failures += "core-routes-missing-request-delegate-helper"
                    }
                }

                if ($helperBlockFound) {
                    $helperBlock = $source.Substring($helperStart, $nextHelperStart - $helperStart)
                    $helperAcceptsRequestDelegate = $helperBlock -match 'RequestDelegate\s+requestDelegate'
                    $helperUsesMapMethods = $helperBlock -match '\.MapMethods\('
                    if (-not $helperAcceptsRequestDelegate) {
                        $failures += "helper-missing-requestdelegate-parameter"
                    }
                    if (-not $helperUsesMapMethods) {
                        $failures += "helper-missing-mapmethods"
                    }
                }
            }

            $rows += [pscustomobject]@{
                PackageName                         = $packageName
                HazardKind                          = $kind
                Site                                = $site
                SourcePath                          = $sourceRelativePath
                SourceExists                        = $sourceExists
                CoreMethodFound                     = $coreMethodFound
                RequestDelegateHelperFound          = $requestDelegateHelperFound
                HelperBlockFound                    = $helperBlockFound
                CoreRoutesUseMinimalApiMapGet       = $coreRoutesUseMinimalApiMapGet
                CoreRoutesUseRequestDelegateHelper  = $coreRoutesUseRequestDelegateHelper
                HelperAcceptsRequestDelegate        = $helperAcceptsRequestDelegate
                HelperUsesMapMethods                = $helperUsesMapMethods
                Status                              = if ($failures.Count -eq 0) { "matched" } else { "failed" }
                Failures                            = @($failures)
            }
        }
    }

    return @($rows)
}

function Get-DeploymentModeFullCommonRouteDelegateAudits {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [string]$RepoRoot = ""
    )

    $rows = @()
    if ($null -eq $Manifest -or
        -not $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count -or
        $null -eq $Manifest.deploymentModeEligibility -or
        -not $Manifest.deploymentModeEligibility.PSObject.Properties.Match("packages").Count) {
        return @()
    }

    foreach ($pkg in @($Manifest.deploymentModeEligibility.packages)) {
        if ($null -eq $pkg -or $pkg.PSObject.Properties.Match("knownHazards").Count -eq 0) { continue }

        $packageName = if ($pkg.PSObject.Properties.Match("packageName").Count -gt 0) { [string]$pkg.packageName } else { "" }
        if (-not [string]::Equals($packageName, "Cephalon.AspNetCore", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        foreach ($hazard in @($pkg.knownHazards)) {
            if ($null -eq $hazard -or $hazard.PSObject.Properties.Match("kind").Count -eq 0) { continue }

            $kind = [string]$hazard.kind
            if (-not [string]::Equals($kind, "dynamic-minimal-api-operator-route-binding", [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $site = if ($hazard.PSObject.Properties.Match("site").Count -gt 0) { [string]$hazard.site } else { "" }
            $sourceRelativePath = $site
            $match = [regex]::Match($site.Trim(), '^(?<path>.+?\.cs)(?::(?<line>\d+))?$')
            if ($match.Success) {
                $sourceRelativePath = $match.Groups["path"].Value
            }

            $resolvedPath = $sourceRelativePath
            if (-not [System.IO.Path]::IsPathRooted($resolvedPath) -and -not [string]::IsNullOrWhiteSpace($RepoRoot)) {
                $resolvedPath = Join-Path $RepoRoot ($sourceRelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
            }

            $sourceExists = Test-Path -LiteralPath $resolvedPath -PathType Leaf
            $fullCommonMethodFound = $false
            $coreMethodFound = $false
            $requestDelegateHelperFound = $false
            $helperBlockFound = $false
            $fullCommonRoutesUseMinimalApiMapGet = $false
            $fullCommonRoutesUseRequestDelegateHelper = $false
            $helperAcceptsRequestDelegate = $false
            $helperUsesMapMethods = $false
            $failures = @()

            if (-not $sourceExists) {
                $failures += "source-missing"
            }
            else {
                $source = Get-Content -LiteralPath $resolvedPath -Raw -Encoding UTF8
                $fullCommonStart = $source.IndexOf("private static void MapCephalonFullCommonOperatorRoutes(", [System.StringComparison]::Ordinal)
                $coreStart = $source.IndexOf("private static void MapCephalonCoreOperatorRoutes(", [System.StringComparison]::Ordinal)
                $helperStart = $source.IndexOf("private static void MapGetRequestDelegate(", [System.StringComparison]::Ordinal)
                $nextHelperStart = $source.IndexOf("private static TService GetRequiredService", [System.StringComparison]::Ordinal)

                $fullCommonMethodFound = $fullCommonStart -ge 0
                $coreMethodFound = $coreStart -gt $fullCommonStart
                $requestDelegateHelperFound = $helperStart -gt $coreStart
                $helperBlockFound = $requestDelegateHelperFound -and $nextHelperStart -gt $helperStart

                if (-not $fullCommonMethodFound) {
                    $failures += "full-common-route-method-missing"
                }
                if (-not $coreMethodFound) {
                    $failures += "core-route-method-missing"
                }
                if (-not $requestDelegateHelperFound) {
                    $failures += "request-delegate-helper-missing"
                }
                if (-not $helperBlockFound) {
                    $failures += "request-delegate-helper-block-unparseable"
                }

                if ($fullCommonMethodFound -and $coreMethodFound) {
                    $fullCommonBlock = $source.Substring($fullCommonStart, $coreStart - $fullCommonStart)
                    $fullCommonRoutesUseMinimalApiMapGet = $fullCommonBlock -match '\.MapGet\('
                    $fullCommonRoutesUseRequestDelegateHelper = $fullCommonBlock -match 'MapGetRequestDelegate'
                    if ($fullCommonRoutesUseMinimalApiMapGet) {
                        $failures += "full-common-routes-use-mapget-delegate-binding"
                    }
                    if (-not $fullCommonRoutesUseRequestDelegateHelper) {
                        $failures += "full-common-routes-missing-request-delegate-helper"
                    }
                }

                if ($helperBlockFound) {
                    $helperBlock = $source.Substring($helperStart, $nextHelperStart - $helperStart)
                    $helperAcceptsRequestDelegate = $helperBlock -match 'RequestDelegate\s+requestDelegate'
                    $helperUsesMapMethods = $helperBlock -match '\.MapMethods\('
                    if (-not $helperAcceptsRequestDelegate) {
                        $failures += "helper-missing-requestdelegate-parameter"
                    }
                    if (-not $helperUsesMapMethods) {
                        $failures += "helper-missing-mapmethods"
                    }
                }
            }

            $rows += [pscustomobject]@{
                PackageName                                 = $packageName
                HazardKind                                  = $kind
                Site                                        = $site
                SourcePath                                  = $sourceRelativePath
                SourceExists                                = $sourceExists
                FullCommonMethodFound                       = $fullCommonMethodFound
                CoreMethodFound                             = $coreMethodFound
                RequestDelegateHelperFound                  = $requestDelegateHelperFound
                HelperBlockFound                            = $helperBlockFound
                FullCommonRoutesUseMinimalApiMapGet         = $fullCommonRoutesUseMinimalApiMapGet
                FullCommonRoutesUseRequestDelegateHelper    = $fullCommonRoutesUseRequestDelegateHelper
                HelperAcceptsRequestDelegate                = $helperAcceptsRequestDelegate
                HelperUsesMapMethods                        = $helperUsesMapMethods
                Status                                      = if ($failures.Count -eq 0) { "matched" } else { "failed" }
                Failures                                    = @($failures)
            }
        }
    }

    return @($rows)
}

function Get-DeploymentModeFullOperatorRouteDelegateAudits {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [string]$RepoRoot = ""
    )

    $rows = @()
    if ($null -eq $Manifest -or
        -not $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count -or
        $null -eq $Manifest.deploymentModeEligibility -or
        -not $Manifest.deploymentModeEligibility.PSObject.Properties.Match("packages").Count) {
        return @()
    }

    $expectedRequestBodyJsonContracts = @(
        "CdcCaptureRuntimeObservationArray",
        "CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest",
        "EventPublicationHttpRequest",
        "AgentToolExecutionHttpRequest",
        "KnowledgeQueryHttpRequest"
    )

    foreach ($pkg in @($Manifest.deploymentModeEligibility.packages)) {
        if ($null -eq $pkg -or $pkg.PSObject.Properties.Match("knownHazards").Count -eq 0) { continue }

        $packageName = if ($pkg.PSObject.Properties.Match("packageName").Count -gt 0) { [string]$pkg.packageName } else { "" }
        if (-not [string]::Equals($packageName, "Cephalon.AspNetCore", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        foreach ($hazard in @($pkg.knownHazards)) {
            if ($null -eq $hazard -or $hazard.PSObject.Properties.Match("kind").Count -eq 0) { continue }

            $kind = [string]$hazard.kind
            if (-not [string]::Equals($kind, "dynamic-minimal-api-operator-route-binding", [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $site = if ($hazard.PSObject.Properties.Match("site").Count -gt 0) { [string]$hazard.site } else { "" }
            $sourceRelativePath = $site
            $match = [regex]::Match($site.Trim(), '^(?<path>.+?\.cs)(?::(?<line>\d+))?$')
            if ($match.Success) {
                $sourceRelativePath = $match.Groups["path"].Value
            }

            $resolvedPath = $sourceRelativePath
            if (-not [System.IO.Path]::IsPathRooted($resolvedPath) -and -not [string]::IsNullOrWhiteSpace($RepoRoot)) {
                $resolvedPath = Join-Path $RepoRoot ($sourceRelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
            }

            $sourceExists = Test-Path -LiteralPath $resolvedPath -PathType Leaf
            $fullOperatorBlockFound = $false
            $fullOperatorBlockParseable = $false
            $fullOperatorRoutesUseMinimalApiMapGet = $false
            $fullOperatorRoutesUseMinimalApiMapPost = $false
            $fullOperatorRoutesUseGetRequestDelegateHelper = $false
            $fullOperatorRoutesUsePostRequestDelegateHelper = $false
            $getHelperAcceptsRequestDelegate = $false
            $getHelperUsesMapMethods = $false
            $postHelperAcceptsRequestDelegate = $false
            $postHelperUsesMapMethods = $false
            $postAsyncHelperUsesPostRequestDelegate = $false
            $requestBodyJsonHelperFound = $false
            $sourceGeneratedRequestBodyContracts = @()
            $missingRequestBodyJsonContracts = @()
            $failures = @()

            if (-not $sourceExists) {
                $failures += "source-missing"
            }
            else {
                $source = Get-Content -LiteralPath $resolvedPath -Raw -Encoding UTF8
                $fullOperatorStart = $source.IndexOf("MapCephalonFullCommonOperatorRoutes(engineGroup);", [System.StringComparison]::Ordinal)
                $hostInfrastructureStart = if ($fullOperatorStart -ge 0) {
                    $source.IndexOf("MapCephalonHostInfrastructure(", $fullOperatorStart, [System.StringComparison]::Ordinal)
                }
                else {
                    -1
                }
                $getHelperStart = $source.IndexOf("private static void MapGetRequestDelegate(", [System.StringComparison]::Ordinal)
                $postHelperStart = $source.IndexOf("private static void MapPostRequestDelegate(", [System.StringComparison]::Ordinal)
                $getResultHelperStart = $source.IndexOf("private static void MapGetResultRequestDelegate(", [System.StringComparison]::Ordinal)
                $postAsyncHelperStart = $source.IndexOf("private static void MapPostAsyncResultRequestDelegate(", [System.StringComparison]::Ordinal)
                $cdcCollectionHelperStart = $source.IndexOf("private static void MapCdcCaptureRuntimeCollectionRoute(", [System.StringComparison]::Ordinal)

                $fullOperatorBlockFound = $fullOperatorStart -ge 0
                $fullOperatorBlockParseable = $fullOperatorBlockFound -and $hostInfrastructureStart -gt $fullOperatorStart

                if (-not $fullOperatorBlockFound) {
                    $failures += "full-operator-block-missing"
                }
                if (-not $fullOperatorBlockParseable) {
                    $failures += "full-operator-block-unparseable"
                }

                if ($fullOperatorBlockParseable) {
                    $fullOperatorBlock = $source.Substring($fullOperatorStart, $hostInfrastructureStart - $fullOperatorStart)
                    $fullOperatorRoutesUseMinimalApiMapGet = $fullOperatorBlock -match 'engineGroup\.MapGet\('
                    $fullOperatorRoutesUseMinimalApiMapPost = $fullOperatorBlock -match 'engineGroup\.MapPost\('
                    $fullOperatorRoutesUseGetRequestDelegateHelper = $fullOperatorBlock -match 'MapGet(?:Result|AsyncResult)?RequestDelegate'
                    $fullOperatorRoutesUsePostRequestDelegateHelper = $fullOperatorBlock -match 'MapPostAsyncResultRequestDelegate'
                    $requestBodyJsonHelperFound = $fullOperatorBlock -match 'ReadOptionalJsonBodyAsync'

                    foreach ($contract in $expectedRequestBodyJsonContracts) {
                        if ($fullOperatorBlock -match [regex]::Escape("AspNetCoreJsonSerializerContext.Default.$contract")) {
                            $sourceGeneratedRequestBodyContracts += $contract
                        }
                        else {
                            $missingRequestBodyJsonContracts += $contract
                        }
                    }

                    if ($fullOperatorRoutesUseMinimalApiMapGet) {
                        $failures += "full-operator-routes-use-mapget-delegate-binding"
                    }
                    if ($fullOperatorRoutesUseMinimalApiMapPost) {
                        $failures += "full-operator-routes-use-mappost-delegate-binding"
                    }
                    if (-not $fullOperatorRoutesUseGetRequestDelegateHelper) {
                        $failures += "full-operator-routes-missing-get-request-delegate-helper"
                    }
                    if (-not $fullOperatorRoutesUsePostRequestDelegateHelper) {
                        $failures += "full-operator-routes-missing-post-request-delegate-helper"
                    }
                    if (-not $requestBodyJsonHelperFound) {
                        $failures += "full-operator-routes-missing-request-body-json-helper"
                    }
                    foreach ($missingContract in $missingRequestBodyJsonContracts) {
                        $failures += "source-generated-request-body-contract-missing:$missingContract"
                    }
                }

                if ($getHelperStart -ge 0 -and $postHelperStart -gt $getHelperStart) {
                    $getHelperBlock = $source.Substring($getHelperStart, $postHelperStart - $getHelperStart)
                    $getHelperAcceptsRequestDelegate = $getHelperBlock -match 'RequestDelegate\s+requestDelegate'
                    $getHelperUsesMapMethods = $getHelperBlock -match '\.MapMethods\(' -and $getHelperBlock -match 'HttpMethods\.Get'
                }
                else {
                    $failures += "get-request-delegate-helper-block-unparseable"
                }

                if ($postHelperStart -ge 0 -and $getResultHelperStart -gt $postHelperStart) {
                    $postHelperBlock = $source.Substring($postHelperStart, $getResultHelperStart - $postHelperStart)
                    $postHelperAcceptsRequestDelegate = $postHelperBlock -match 'RequestDelegate\s+requestDelegate'
                    $postHelperUsesMapMethods = $postHelperBlock -match '\.MapMethods\(' -and $postHelperBlock -match 'HttpMethods\.Post'
                }
                else {
                    $failures += "post-request-delegate-helper-block-unparseable"
                }

                if ($postAsyncHelperStart -ge 0 -and $cdcCollectionHelperStart -gt $postAsyncHelperStart) {
                    $postAsyncHelperBlock = $source.Substring($postAsyncHelperStart, $cdcCollectionHelperStart - $postAsyncHelperStart)
                    $postAsyncHelperUsesPostRequestDelegate = $postAsyncHelperBlock -match 'MapPostRequestDelegate'
                }
                else {
                    $failures += "post-async-result-helper-block-unparseable"
                }

                if (-not $getHelperAcceptsRequestDelegate) {
                    $failures += "get-helper-missing-requestdelegate-parameter"
                }
                if (-not $getHelperUsesMapMethods) {
                    $failures += "get-helper-missing-get-mapmethods"
                }
                if (-not $postHelperAcceptsRequestDelegate) {
                    $failures += "post-helper-missing-requestdelegate-parameter"
                }
                if (-not $postHelperUsesMapMethods) {
                    $failures += "post-helper-missing-post-mapmethods"
                }
                if (-not $postAsyncHelperUsesPostRequestDelegate) {
                    $failures += "post-async-result-helper-missing-post-request-delegate"
                }
            }

            $rows += [pscustomobject]@{
                PackageName                                  = $packageName
                HazardKind                                   = $kind
                Site                                         = $site
                SourcePath                                   = $sourceRelativePath
                SourceExists                                 = $sourceExists
                FullOperatorBlockFound                       = $fullOperatorBlockFound
                FullOperatorBlockParseable                   = $fullOperatorBlockParseable
                FullOperatorRoutesUseMinimalApiMapGet        = $fullOperatorRoutesUseMinimalApiMapGet
                FullOperatorRoutesUseMinimalApiMapPost       = $fullOperatorRoutesUseMinimalApiMapPost
                FullOperatorRoutesUseGetRequestDelegateHelper = $fullOperatorRoutesUseGetRequestDelegateHelper
                FullOperatorRoutesUsePostRequestDelegateHelper = $fullOperatorRoutesUsePostRequestDelegateHelper
                GetHelperAcceptsRequestDelegate              = $getHelperAcceptsRequestDelegate
                GetHelperUsesMapMethods                      = $getHelperUsesMapMethods
                PostHelperAcceptsRequestDelegate             = $postHelperAcceptsRequestDelegate
                PostHelperUsesMapMethods                     = $postHelperUsesMapMethods
                PostAsyncHelperUsesPostRequestDelegate       = $postAsyncHelperUsesPostRequestDelegate
                RequestBodyJsonHelperFound                   = $requestBodyJsonHelperFound
                SourceGeneratedRequestBodyContracts          = @($sourceGeneratedRequestBodyContracts)
                MissingRequestBodyJsonContracts              = @($missingRequestBodyJsonContracts)
                Status                                       = if ($failures.Count -eq 0) { "matched" } else { "failed" }
                Failures                                     = @($failures)
            }
        }
    }

    return @($rows)
}

function Get-DeploymentModeOperatorResponseJsonContractAudits {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [string]$RepoRoot = ""
    )

    $rows = @()
    if ($null -eq $Manifest -or
        -not $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count -or
        $null -eq $Manifest.deploymentModeEligibility -or
        -not $Manifest.deploymentModeEligibility.PSObject.Properties.Match("packages").Count) {
        return @()
    }

    $expectedResponseJsonContracts = @(
        "RuntimeManifest",
        "RuntimeIntrospectionSnapshot",
        "AppProfile",
        "ResilienceSelection",
        "ScaffoldPlan",
        "IReadOnlyList<CapabilityManifest>",
        "IReadOnlyList<ModuleManifest>",
        "IReadOnlyList<PackageManifest>",
        "IReadOnlyList<PatternDescriptor>",
        "IReadOnlyList<TechnologyDescriptor>",
        "IReadOnlyList<TechnologyRuntimeSurface>",
        "IReadOnlyList<TransportDescriptor>",
        "DependencyHealthReport[]",
        "LocalizedResourcesSnapshot",
        "ReferenceDocsSurface",
        "EngineOptions",
        "PackagePolicy",
        "FailurePolicy",
        "TrustSnapshot",
        "RuntimeStatusSnapshot",
        "RuntimeOperationalStory",
        "DiagnosticsSurface",
        "DiagnosticsConventionsSurface"
    )

    foreach ($pkg in @($Manifest.deploymentModeEligibility.packages)) {
        if ($null -eq $pkg -or $pkg.PSObject.Properties.Match("knownHazards").Count -eq 0) { continue }

        $packageName = if ($pkg.PSObject.Properties.Match("packageName").Count -gt 0) { [string]$pkg.packageName } else { "" }
        if (-not [string]::Equals($packageName, "Cephalon.AspNetCore", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        foreach ($hazard in @($pkg.knownHazards)) {
            if ($null -eq $hazard -or $hazard.PSObject.Properties.Match("kind").Count -eq 0) { continue }

            $kind = [string]$hazard.kind
            if (-not [string]::Equals($kind, "dynamic-minimal-api-operator-route-binding", [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $jsonContextRelativePath = "src/Cephalon.AspNetCore/AspNetCoreJsonSerializerContext.cs"
            $builderExtensionsRelativePath = "src/Cephalon.AspNetCore/Hosting/EngineWebApplicationBuilderExtensions.cs"
            $jsonContextPath = $jsonContextRelativePath
            $builderExtensionsPath = $builderExtensionsRelativePath
            if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) {
                $jsonContextPath = Join-Path $RepoRoot ($jsonContextRelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
                $builderExtensionsPath = Join-Path $RepoRoot ($builderExtensionsRelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
            }

            $jsonContextExists = Test-Path -LiteralPath $jsonContextPath -PathType Leaf
            $builderExtensionsExists = Test-Path -LiteralPath $builderExtensionsPath -PathType Leaf
            $sourceGeneratedResponseContracts = @()
            $missingResponseJsonContracts = @()
            $httpJsonOptionsResolverRegistered = $false
            $mvcJsonOptionsResolverRegistered = $false
            $failures = @()

            if (-not $jsonContextExists) {
                $failures += "json-context-source-missing"
            }
            else {
                $jsonContextSource = Get-Content -LiteralPath $jsonContextPath -Raw -Encoding UTF8
                foreach ($contract in $expectedResponseJsonContracts) {
                    if ($jsonContextSource -match [regex]::Escape("typeof($contract)")) {
                        $sourceGeneratedResponseContracts += $contract
                    }
                    else {
                        $missingResponseJsonContracts += $contract
                    }
                }

                foreach ($missingContract in $missingResponseJsonContracts) {
                    $failures += "source-generated-response-contract-missing:$missingContract"
                }
            }

            if (-not $builderExtensionsExists) {
                $failures += "builder-extensions-source-missing"
            }
            else {
                $builderExtensionsSource = Get-Content -LiteralPath $builderExtensionsPath -Raw -Encoding UTF8
                $httpJsonOptionsResolverRegistered =
                    $builderExtensionsSource -match [regex]::Escape("Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>") -and
                    $builderExtensionsSource -match [regex]::Escape("AspNetCoreJsonSerializerContext.Default") -and
                    $builderExtensionsSource -match [regex]::Escape("TypeInfoResolverChain.Insert(0, AspNetCoreJsonSerializerContext.Default)")
                $mvcJsonOptionsResolverRegistered =
                    $builderExtensionsSource -match [regex]::Escape("Configure<Microsoft.AspNetCore.Mvc.JsonOptions>") -and
                    $builderExtensionsSource -match [regex]::Escape("AspNetCoreJsonSerializerContext.Default") -and
                    $builderExtensionsSource -match [regex]::Escape("JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AspNetCoreJsonSerializerContext.Default)")

                if (-not $httpJsonOptionsResolverRegistered) {
                    $failures += "http-json-source-generated-resolver-not-registered"
                }
                if (-not $mvcJsonOptionsResolverRegistered) {
                    $failures += "mvc-json-source-generated-resolver-not-registered"
                }
            }

            $rows += [pscustomobject]@{
                PackageName                           = $packageName
                HazardKind                            = $kind
                JsonContextPath                       = $jsonContextRelativePath
                BuilderExtensionsPath                 = $builderExtensionsRelativePath
                JsonContextExists                     = $jsonContextExists
                BuilderExtensionsExists               = $builderExtensionsExists
                HttpJsonOptionsResolverRegistered     = $httpJsonOptionsResolverRegistered
                MvcJsonOptionsResolverRegistered      = $mvcJsonOptionsResolverRegistered
                SourceGeneratedResponseContracts      = @($sourceGeneratedResponseContracts)
                MissingResponseJsonContracts          = @($missingResponseJsonContracts)
                Status                                = if ($failures.Count -eq 0) { "matched" } else { "failed" }
                Failures                              = @($failures)
            }
        }
    }

    return @($rows)
}

function Get-DeploymentModeHazardInventory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Manifest,
        [string]$RepoRoot = ""
    )

    $tierOrder = @("excluded-by-design", "clean-baseline", "low", "medium", "high")
    $modeOrder = @("trim", "nativeAot", "singleFile")
    $tierCounts = [ordered]@{}
    foreach ($tier in $tierOrder) {
        $tierCounts[$tier] = 0
    }

    $packages = @()
    if ($null -ne $Manifest -and
        $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count -gt 0 -and
        $null -ne $Manifest.deploymentModeEligibility -and
        $Manifest.deploymentModeEligibility.PSObject.Properties.Match("packages").Count -gt 0) {

        foreach ($pkg in @($Manifest.deploymentModeEligibility.packages)) {
            if ($null -eq $pkg) { continue }

            $packageName = if ($pkg.PSObject.Properties.Match("packageName").Count -gt 0) { [string]$pkg.packageName } else { "" }
            $nugetId = if ($pkg.PSObject.Properties.Match("nugetId").Count -gt 0) { [string]$pkg.nugetId } else { "" }
            $claimAuditTier = if ($pkg.PSObject.Properties.Match("claimAuditTier").Count -gt 0) { [string]$pkg.claimAuditTier } else { "unknown" }
            if (-not $tierCounts.Contains($claimAuditTier)) {
                $tierCounts[$claimAuditTier] = 0
            }
            $tierCounts[$claimAuditTier]++

            $supportedModes = @()
            if ($pkg.PSObject.Properties.Match("supportedModes").Count -gt 0) {
                $supportedModes = @($pkg.supportedModes | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
            }

            $requiredProjectProperties = @()
            if ($pkg.PSObject.Properties.Match("requiredProjectProperties").Count -gt 0) {
                $requiredProjectProperties = @($pkg.requiredProjectProperties | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
            }

            $knownHazards = @()
            if ($pkg.PSObject.Properties.Match("knownHazards").Count -gt 0) {
                $knownHazards = @($pkg.knownHazards | Where-Object { $null -ne $_ })
            }

            $hazardKinds = @(
                $knownHazards |
                    ForEach-Object {
                        if ($_.PSObject.Properties.Match("kind").Count -gt 0) { [string]$_.kind }
                    } |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                    Select-Object -Unique
            )

            $packages += [pscustomobject]@{
                PackageName               = $packageName
                NugetId                   = $nugetId
                ClaimAuditTier            = $claimAuditTier
                SupportedModes            = $supportedModes
                RequiredProjectProperties = $requiredProjectProperties
                KnownHazardCount          = $knownHazards.Count
                HazardKinds               = $hazardKinds
                IntroducedBy              = if ($pkg.PSObject.Properties.Match("introducedBy").Count -gt 0) { [string]$pkg.introducedBy } else { "" }
                ExtendedBy                = if ($pkg.PSObject.Properties.Match("extendedBy").Count -gt 0) { [string]$pkg.extendedBy } else { "" }
                Evidence                  = if ($pkg.PSObject.Properties.Match("evidence").Count -gt 0) { [string]$pkg.evidence } else { "" }
            }
        }
    }

    $allHazardKinds = @()
    if ($null -ne $Manifest -and
        $Manifest.PSObject.Properties.Match("deploymentModeEligibility").Count -gt 0 -and
        $null -ne $Manifest.deploymentModeEligibility -and
        $Manifest.deploymentModeEligibility.PSObject.Properties.Match("packages").Count -gt 0) {
        foreach ($pkg in @($Manifest.deploymentModeEligibility.packages)) {
            if ($null -eq $pkg -or $pkg.PSObject.Properties.Match("knownHazards").Count -eq 0) { continue }
            foreach ($hazard in @($pkg.knownHazards)) {
                if ($null -eq $hazard -or $hazard.PSObject.Properties.Match("kind").Count -eq 0) { continue }
                $kind = [string]$hazard.kind
                if (-not [string]::IsNullOrWhiteSpace($kind)) {
                    $allHazardKinds += $kind
                }
            }
        }
    }

    $knownTransitiveHazards = @()
    foreach ($mode in $modeOrder) {
        $entries = @()
        if ($null -ne $Manifest -and
            $Manifest.PSObject.Properties.Match("knownTransitiveHazards").Count -gt 0 -and
            $null -ne $Manifest.knownTransitiveHazards -and
            $Manifest.knownTransitiveHazards.PSObject.Properties.Match($mode).Count -gt 0) {
            $entries = @($Manifest.knownTransitiveHazards.$mode | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        }

        $knownTransitiveHazards += [pscustomobject]@{
            Mode    = $mode
            Count   = $entries.Count
            Entries = $entries
        }
    }

    $knownTransitiveHazardAudit = Get-DeploymentModeTransitiveHazardAudit -Manifest $Manifest -RepoRoot $RepoRoot
    $boundaryAnnotationAudits = @(Get-DeploymentModeBoundaryAnnotationAudits -Manifest $Manifest -RepoRoot $RepoRoot)
    $boundaryAnnotationAuditFailures = @($boundaryAnnotationAudits | Where-Object { $_.Status -ne "annotated" })
    $boundaryAnnotationAuditStatus = if ($boundaryAnnotationAudits.Count -eq 0) {
        "not-applicable"
    }
    elseif ($boundaryAnnotationAuditFailures.Count -eq 0) {
        "matched"
    }
    else {
        "failed"
    }

    $coreRouteDelegateAudits = @(Get-DeploymentModeCoreRouteDelegateAudits -Manifest $Manifest -RepoRoot $RepoRoot)
    $coreRouteDelegateAuditFailures = @($coreRouteDelegateAudits | Where-Object { $_.Status -ne "matched" })
    $coreRouteDelegateAuditStatus = if ($coreRouteDelegateAudits.Count -eq 0) {
        "not-applicable"
    }
    elseif ($coreRouteDelegateAuditFailures.Count -eq 0) {
        "matched"
    }
    else {
        "failed"
    }

    $fullCommonRouteDelegateAudits = @(Get-DeploymentModeFullCommonRouteDelegateAudits -Manifest $Manifest -RepoRoot $RepoRoot)
    $fullCommonRouteDelegateAuditFailures = @($fullCommonRouteDelegateAudits | Where-Object { $_.Status -ne "matched" })
    $fullCommonRouteDelegateAuditStatus = if ($fullCommonRouteDelegateAudits.Count -eq 0) {
        "not-applicable"
    }
    elseif ($fullCommonRouteDelegateAuditFailures.Count -eq 0) {
        "matched"
    }
    else {
        "failed"
    }

    $fullOperatorRouteDelegateAudits = @(Get-DeploymentModeFullOperatorRouteDelegateAudits -Manifest $Manifest -RepoRoot $RepoRoot)
    $fullOperatorRouteDelegateAuditFailures = @($fullOperatorRouteDelegateAudits | Where-Object { $_.Status -ne "matched" })
    $fullOperatorRouteDelegateAuditStatus = if ($fullOperatorRouteDelegateAudits.Count -eq 0) {
        "not-applicable"
    }
    elseif ($fullOperatorRouteDelegateAuditFailures.Count -eq 0) {
        "matched"
    }
    else {
        "failed"
    }

    $operatorResponseJsonContractAudits = @(Get-DeploymentModeOperatorResponseJsonContractAudits -Manifest $Manifest -RepoRoot $RepoRoot)
    $operatorResponseJsonContractAuditFailures = @($operatorResponseJsonContractAudits | Where-Object { $_.Status -ne "matched" })
    $operatorResponseJsonContractAuditStatus = if ($operatorResponseJsonContractAudits.Count -eq 0) {
        "not-applicable"
    }
    elseif ($operatorResponseJsonContractAuditFailures.Count -eq 0) {
        "matched"
    }
    else {
        "failed"
    }

    $tierRows = foreach ($tierName in $tierCounts.Keys) {
        [pscustomobject]@{
            Tier  = $tierName
            Count = [int]$tierCounts[$tierName]
        }
    }

    $supportedModeRows = foreach ($mode in $modeOrder) {
        $matchingPackages = @($packages | Where-Object { @($_.SupportedModes) -contains $mode } | ForEach-Object { $_.PackageName })
        [pscustomobject]@{
            Mode         = $mode
            PackageCount = $matchingPackages.Count
            Packages     = $matchingPackages
        }
    }

    $hazardKindRows = @(
        $allHazardKinds |
            Group-Object |
            Sort-Object -Property @{ Expression = "Count"; Descending = $true }, @{ Expression = "Name"; Ascending = $true } |
            ForEach-Object { [pscustomobject]@{ Kind = $_.Name; Count = $_.Count } }
    )

    $schemaVersion = ""
    if ($null -ne $Manifest -and $Manifest.PSObject.Properties.Match('$schemaVersion').Count -gt 0) {
        $schemaVersion = [string]$Manifest.'$schemaVersion'
    }

    $totalKnownHazards = 0
    if ($packages.Count -gt 0) {
        $hazardSum = $packages | Measure-Object -Property KnownHazardCount -Sum
        if ($null -ne $hazardSum -and $null -ne $hazardSum.Sum) {
            $totalKnownHazards = [int]$hazardSum.Sum
        }
    }

    return [pscustomobject]@{
        Source                    = "deploymentModeEligibility"
        ManifestSchemaVersion     = $schemaVersion
        TotalPackages             = $packages.Count
        PackagesWithKnownHazards  = @($packages | Where-Object { $_.KnownHazardCount -gt 0 }).Count
        PackagesWithScopedClaims  = @($packages | Where-Object { @($_.SupportedModes).Count -gt 0 }).Count
        TotalKnownHazards         = $totalKnownHazards
        TierCounts                = @($tierRows)
        HazardKindCounts          = @($hazardKindRows)
        SupportedModeClaims       = @($supportedModeRows)
        KnownTransitiveHazards    = @($knownTransitiveHazards)
        KnownTransitiveHazardAudit = $knownTransitiveHazardAudit
        BoundaryAnnotationAuditStatus       = $boundaryAnnotationAuditStatus
        BoundaryAnnotationAuditCount        = $boundaryAnnotationAudits.Count
        BoundaryAnnotationAuditFailureCount = $boundaryAnnotationAuditFailures.Count
        BoundaryAnnotationAuditFailures     = @($boundaryAnnotationAuditFailures)
        BoundaryAnnotationAudits            = @($boundaryAnnotationAudits)
        CoreRouteDelegateAuditStatus        = $coreRouteDelegateAuditStatus
        CoreRouteDelegateAuditCount         = $coreRouteDelegateAudits.Count
        CoreRouteDelegateAuditFailureCount  = $coreRouteDelegateAuditFailures.Count
        CoreRouteDelegateAuditFailures      = @($coreRouteDelegateAuditFailures)
        CoreRouteDelegateAudits             = @($coreRouteDelegateAudits)
        FullCommonRouteDelegateAuditStatus        = $fullCommonRouteDelegateAuditStatus
        FullCommonRouteDelegateAuditCount         = $fullCommonRouteDelegateAudits.Count
        FullCommonRouteDelegateAuditFailureCount  = $fullCommonRouteDelegateAuditFailures.Count
        FullCommonRouteDelegateAuditFailures      = @($fullCommonRouteDelegateAuditFailures)
        FullCommonRouteDelegateAudits             = @($fullCommonRouteDelegateAudits)
        FullOperatorRouteDelegateAuditStatus        = $fullOperatorRouteDelegateAuditStatus
        FullOperatorRouteDelegateAuditCount         = $fullOperatorRouteDelegateAudits.Count
        FullOperatorRouteDelegateAuditFailureCount  = $fullOperatorRouteDelegateAuditFailures.Count
        FullOperatorRouteDelegateAuditFailures      = @($fullOperatorRouteDelegateAuditFailures)
        FullOperatorRouteDelegateAudits             = @($fullOperatorRouteDelegateAudits)
        OperatorResponseJsonContractAuditStatus        = $operatorResponseJsonContractAuditStatus
        OperatorResponseJsonContractAuditCount         = $operatorResponseJsonContractAudits.Count
        OperatorResponseJsonContractAuditFailureCount  = $operatorResponseJsonContractAuditFailures.Count
        OperatorResponseJsonContractAuditFailures      = @($operatorResponseJsonContractAuditFailures)
        OperatorResponseJsonContractAudits             = @($operatorResponseJsonContractAudits)
        Packages                  = @($packages)
    }
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

function Get-PublishProbeAdditionalArgs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $ModeConfig
    )

    if ($ModeConfig.Mode -ne "nativeAot") {
        return @()
    }

    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
    return @("/p:_targetArchitecture=$architecture")
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
            ) + @(Get-PublishProbeAdditionalArgs -ModeConfig $ModeConfig)
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

function Compute-PublishProbeGateResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $ModeReports,
        $PublishProbePolicy
    )

    $gatedModes = @()
    if ($null -ne $PublishProbePolicy -and $PublishProbePolicy.PSObject.Properties.Match("GatedModes").Count -gt 0) {
        $gatedModes = @(ConvertTo-StringArray -Value $PublishProbePolicy.GatedModes)
    }

    $nonOptOutGate = $false
    if ($null -ne $PublishProbePolicy -and $PublishProbePolicy.PSObject.Properties.Match("NonOptOutGate").Count -gt 0) {
        $nonOptOutGate = ConvertTo-BooleanValue -Value $PublishProbePolicy.NonOptOutGate -DefaultValue $false
    }

    $failureBlocksRelease = $nonOptOutGate
    if ($null -ne $PublishProbePolicy -and $PublishProbePolicy.PSObject.Properties.Match("FailureBlocksRelease").Count -gt 0) {
        $failureBlocksRelease = ConvertTo-BooleanValue -Value $PublishProbePolicy.FailureBlocksRelease -DefaultValue $nonOptOutGate
    }

    $failOnWarnings = $true
    if ($null -ne $PublishProbePolicy -and $PublishProbePolicy.PSObject.Properties.Match("FailOnWarnings").Count -gt 0) {
        $failOnWarnings = ConvertTo-BooleanValue -Value $PublishProbePolicy.FailOnWarnings -DefaultValue $true
    }

    if (-not $nonOptOutGate -or -not $failureBlocksRelease -or $gatedModes.Count -eq 0) {
        return [pscustomobject]@{
            Status               = "not-enabled"
            Enabled              = $false
            FailureBlocksRelease = $failureBlocksRelease
            FailOnWarnings       = $failOnWarnings
            GatedModes           = $gatedModes
            FailureCount         = 0
            Reasons              = @("publish-probe release gate is not enabled by publishProbePolicy")
        }
    }

    $auditOnlyModes = @()
    if ($null -ne $PublishProbePolicy -and $PublishProbePolicy.PSObject.Properties.Match("AuditOnlyModes").Count -gt 0) {
        $auditOnlyModes = @(ConvertTo-StringArray -Value $PublishProbePolicy.AuditOnlyModes)
    }

    $evaluatedModes = @(
        $ModeReports |
            Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_.Mode) } |
            ForEach-Object { [string]$_.Mode } |
            Select-Object -Unique
    )
    $evaluatedGatedModes = @($evaluatedModes | Where-Object { $gatedModes -contains $_ })
    if ($evaluatedModes.Count -gt 0 -and $evaluatedGatedModes.Count -eq 0 -and $auditOnlyModes.Count -gt 0) {
        $nonAuditOnlyModes = @($evaluatedModes | Where-Object { $auditOnlyModes -notcontains $_ })
        if ($nonAuditOnlyModes.Count -eq 0) {
            return [pscustomobject]@{
                Status               = "not-applicable"
                Enabled              = $false
                FailureBlocksRelease = $failureBlocksRelease
                FailOnWarnings       = $failOnWarnings
                GatedModes           = $gatedModes
                FailureCount         = 0
                Reasons              = @("current run evaluated only audit-only deployment modes: $($evaluatedModes -join ', ')")
            }
        }
    }

    $reasons = @()
    foreach ($mode in $gatedModes) {
        $modeReport = @($ModeReports | Where-Object { $null -ne $_ -and $_.Mode -eq $mode }) | Select-Object -First 1
        if ($null -eq $modeReport) {
            $reasons += "gated mode '$mode' was not evaluated"
            continue
        }

        $publishProbe = $modeReport.PublishProbe
        if ($null -eq $publishProbe -or $publishProbe.Skipped) {
            $reason = if ($null -ne $publishProbe -and -not [string]::IsNullOrWhiteSpace([string]$publishProbe.Reason)) {
                [string]$publishProbe.Reason
            }
            else {
                "publish probe did not run"
            }
            $reasons += "gated mode '$mode' did not run a publish probe: $reason"
            continue
        }

        $targets = @($publishProbe.Targets)
        if ($targets.Count -eq 0) {
            $reasons += "gated mode '$mode' did not evaluate any publish target"
            continue
        }

        $failedTargets = @($targets | Where-Object { -not $_.Success })
        if ($failedTargets.Count -gt 0) {
            $reasons += "gated mode '$mode' failed $($failedTargets.Count) of $($targets.Count) publish target(s)"
        }

        $totalWarnings = ($targets | Measure-Object -Property WarningCount -Sum).Sum
        if ($failOnWarnings -and $totalWarnings -and $totalWarnings -gt 0) {
            $reasons += "gated mode '$mode' emitted $totalWarnings publish warning(s)"
        }
    }

    if ($reasons.Count -gt 0) {
        return [pscustomobject]@{
            Status               = "failed"
            Enabled              = $true
            FailureBlocksRelease = $failureBlocksRelease
            FailOnWarnings       = $failOnWarnings
            GatedModes           = $gatedModes
            FailureCount         = $reasons.Count
            Reasons              = $reasons
        }
    }

    return [pscustomobject]@{
        Status               = "passed"
        Enabled              = $true
        FailureBlocksRelease = $failureBlocksRelease
        FailOnWarnings       = $failOnWarnings
        GatedModes           = $gatedModes
        FailureCount         = 0
        Reasons              = @("all gated publish probes passed")
    }
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
    $hazardInventoryPath = Join-Path $OutputDir "hazard-inventory.json"
    $mdPath = Join-Path $OutputDir "README.md"

    $Report | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
    $hazardInventory = $null
    if ($Report.PSObject.Properties.Match("HazardInventory").Count -gt 0) {
        $hazardInventory = $Report.HazardInventory
        if ($null -ne $hazardInventory) {
            $hazardInventory | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $hazardInventoryPath -Encoding UTF8
        }
    }

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
    [void]$sb.AppendLine("## Publish-probe policy")
    [void]$sb.AppendLine("")
    $publishProbePolicy = $null
    if ($Report.PSObject.Properties.Match("PublishProbePolicy").Count -gt 0) {
        $publishProbePolicy = $Report.PublishProbePolicy
    }
    if ($null -eq $publishProbePolicy) {
        [void]$sb.AppendLine("No publish-probe policy was emitted for this report.")
    }
    else {
        [void]$sb.AppendLine("- Source: $($publishProbePolicy.Source)")
        [void]$sb.AppendLine("- Release-validation mode: $($publishProbePolicy.ReleaseValidationMode)")
        [void]$sb.AppendLine("- Release-validation deployment modes: $(@($publishProbePolicy.ReleaseValidationDeploymentModes) -join ', ')")
        [void]$sb.AppendLine("- Release validation skips publish: $($publishProbePolicy.ReleaseValidationSkipsPublish)")
        [void]$sb.AppendLine("- Current run skipped publish: $($publishProbePolicy.CurrentRunSkipsPublish)")
        [void]$sb.AppendLine("- Non-opt-out gate: $($publishProbePolicy.NonOptOutGate)")
        [void]$sb.AppendLine("- Gated modes: $(@($publishProbePolicy.GatedModes) -join ', ')")
        [void]$sb.AppendLine("- Audit-only modes: $(@($publishProbePolicy.AuditOnlyModes) -join ', ')")
        [void]$sb.AppendLine("- Failure blocks release: $($publishProbePolicy.FailureBlocksRelease)")
        [void]$sb.AppendLine("- Fail on warnings: $($publishProbePolicy.FailOnWarnings)")
        [void]$sb.AppendLine("- Gate promotion: $($publishProbePolicy.GatePromotion)")
        [void]$sb.AppendLine("- Representative publish targets: $($publishProbePolicy.RepresentativePublishTargets)")
        $requirements = @($publishProbePolicy.PromotionRequirements)
        if ($requirements.Count -gt 0) {
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine("Promotion requirements:")
            foreach ($requirement in $requirements) {
                [void]$sb.AppendLine("- $requirement")
            }
        }
    }
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## Publish-probe release gate")
    [void]$sb.AppendLine("")
    if ($Report.PSObject.Properties.Match("PublishProbeGate").Count -eq 0 -or $null -eq $Report.PublishProbeGate) {
        [void]$sb.AppendLine("No publish-probe release gate was emitted for this report.")
    }
    else {
        [void]$sb.AppendLine("- Status: $($Report.PublishProbeGate.Status)")
        [void]$sb.AppendLine("- Enabled: $($Report.PublishProbeGate.Enabled)")
        [void]$sb.AppendLine("- Failure blocks release: $($Report.PublishProbeGate.FailureBlocksRelease)")
        [void]$sb.AppendLine("- Fail on warnings: $($Report.PublishProbeGate.FailOnWarnings)")
        [void]$sb.AppendLine("- Gated modes: $(@($Report.PublishProbeGate.GatedModes) -join ', ')")
        foreach ($reason in @($Report.PublishProbeGate.Reasons)) {
            [void]$sb.AppendLine("  - $reason")
        }
    }
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## Hazard inventory")
    [void]$sb.AppendLine("")
    if ($null -eq $hazardInventory) {
        [void]$sb.AppendLine("No hazard inventory was emitted for this report.")
    }
    else {
        [void]$sb.AppendLine("- Packages in manifest inventory: $($hazardInventory.TotalPackages)")
        [void]$sb.AppendLine("- Packages with known hazards: $($hazardInventory.PackagesWithKnownHazards)")
        [void]$sb.AppendLine("- Known hazard entries: $($hazardInventory.TotalKnownHazards)")
        [void]$sb.AppendLine("- Packages with scoped claims: $($hazardInventory.PackagesWithScopedClaims)")
        if ($hazardInventory.PSObject.Properties.Match("BoundaryAnnotationAuditStatus").Count -gt 0) {
            [void]$sb.AppendLine("- Dynamic route boundary annotation audit: $($hazardInventory.BoundaryAnnotationAuditStatus)")
            [void]$sb.AppendLine("- Boundary annotation audit entries: $($hazardInventory.BoundaryAnnotationAuditCount)")
            [void]$sb.AppendLine("- Boundary annotation audit failures: $($hazardInventory.BoundaryAnnotationAuditFailureCount)")
        }
        if ($hazardInventory.PSObject.Properties.Match("CoreRouteDelegateAuditStatus").Count -gt 0) {
            [void]$sb.AppendLine("- Core operator route-delegate audit: $($hazardInventory.CoreRouteDelegateAuditStatus)")
            [void]$sb.AppendLine("- Core route-delegate audit entries: $($hazardInventory.CoreRouteDelegateAuditCount)")
            [void]$sb.AppendLine("- Core route-delegate audit failures: $($hazardInventory.CoreRouteDelegateAuditFailureCount)")
        }
        if ($hazardInventory.PSObject.Properties.Match("FullCommonRouteDelegateAuditStatus").Count -gt 0) {
            [void]$sb.AppendLine("- Full common operator route-delegate audit: $($hazardInventory.FullCommonRouteDelegateAuditStatus)")
            [void]$sb.AppendLine("- Full common route-delegate audit entries: $($hazardInventory.FullCommonRouteDelegateAuditCount)")
            [void]$sb.AppendLine("- Full common route-delegate audit failures: $($hazardInventory.FullCommonRouteDelegateAuditFailureCount)")
        }
        if ($hazardInventory.PSObject.Properties.Match("FullOperatorRouteDelegateAuditStatus").Count -gt 0) {
            [void]$sb.AppendLine("- Full operator route-delegate audit: $($hazardInventory.FullOperatorRouteDelegateAuditStatus)")
            [void]$sb.AppendLine("- Full operator route-delegate audit entries: $($hazardInventory.FullOperatorRouteDelegateAuditCount)")
            [void]$sb.AppendLine("- Full operator route-delegate audit failures: $($hazardInventory.FullOperatorRouteDelegateAuditFailureCount)")
        }
        if ($hazardInventory.PSObject.Properties.Match("OperatorResponseJsonContractAuditStatus").Count -gt 0) {
            [void]$sb.AppendLine("- Operator response JSON contract audit: $($hazardInventory.OperatorResponseJsonContractAuditStatus)")
            [void]$sb.AppendLine("- Operator response JSON contract audit entries: $($hazardInventory.OperatorResponseJsonContractAuditCount)")
            [void]$sb.AppendLine("- Operator response JSON contract audit failures: $($hazardInventory.OperatorResponseJsonContractAuditFailureCount)")
        }
        [void]$sb.AppendLine("")
        [void]$sb.AppendLine("Tier counts:")
        foreach ($tier in @($hazardInventory.TierCounts)) {
            [void]$sb.AppendLine("- **$($tier.Tier)**: $($tier.Count)")
        }
        [void]$sb.AppendLine("")
        [void]$sb.AppendLine("Supported mode claims:")
        foreach ($modeClaim in @($hazardInventory.SupportedModeClaims)) {
            $packageList = if (@($modeClaim.Packages).Count -gt 0) { @($modeClaim.Packages) -join ", " } else { "none" }
            [void]$sb.AppendLine("- **$($modeClaim.Mode)**: $($modeClaim.PackageCount) ($packageList)")
        }
        [void]$sb.AppendLine("")
        [void]$sb.AppendLine("Known transitive hazard hints:")
        foreach ($modeHazard in @($hazardInventory.KnownTransitiveHazards)) {
            [void]$sb.AppendLine("- **$($modeHazard.Mode)**: $($modeHazard.Count)")
        }
        if ($hazardInventory.PSObject.Properties.Match("KnownTransitiveHazardAudit").Count -gt 0 -and
            $null -ne $hazardInventory.KnownTransitiveHazardAudit) {
            $audit = $hazardInventory.KnownTransitiveHazardAudit
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine("Known transitive hazard lock-file audit:")
            [void]$sb.AppendLine("- Status: $($audit.Status)")
            [void]$sb.AppendLine("- Audit entries: $($audit.TotalEntries)")
            [void]$sb.AppendLine("- Missing entries: $($audit.MissingEntries)")
            [void]$sb.AppendLine("- Lock files scanned: $($audit.LockFileCount)")
            [void]$sb.AppendLine("- Package families matched: $($audit.PackageMatchCount)")
            foreach ($entry in @($audit.Entries)) {
                $modes = if (@($entry.Modes).Count -gt 0) { @($entry.Modes) -join ", " } else { "none" }
                $packages = if (@($entry.MatchedPackageIds).Count -gt 0) { @($entry.MatchedPackageIds) -join ", " } else { "none" }
                [void]$sb.AppendLine("- **$($entry.PackagePattern)**: $($entry.Status), lock-file matches=$($entry.MatchCount), modes=$modes, packages=$packages")
            }
        }
        if ($hazardInventory.PSObject.Properties.Match("BoundaryAnnotationAudits").Count -gt 0 -and
            @($hazardInventory.BoundaryAnnotationAudits).Count -gt 0) {
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine("Dynamic route boundary annotation audit:")
            foreach ($audit in @($hazardInventory.BoundaryAnnotationAudits)) {
                [void]$sb.AppendLine("- **$($audit.PackageName)** at ``$($audit.Site)``: $($audit.Status), RequiresUnreferencedCode=$($audit.RequiresUnreferencedCode), RequiresDynamicCode=$($audit.RequiresDynamicCode)")
            }
        }
        if ($hazardInventory.PSObject.Properties.Match("CoreRouteDelegateAudits").Count -gt 0 -and
            @($hazardInventory.CoreRouteDelegateAudits).Count -gt 0) {
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine("Core operator route-delegate audit:")
            foreach ($audit in @($hazardInventory.CoreRouteDelegateAudits)) {
                $failures = if (@($audit.Failures).Count -gt 0) { @($audit.Failures) -join ", " } else { "none" }
                [void]$sb.AppendLine("- **$($audit.PackageName)** at ``$($audit.SourcePath)``: $($audit.Status), usesRequestDelegateHelper=$($audit.CoreRoutesUseRequestDelegateHelper), usesMinimalApiMapGet=$($audit.CoreRoutesUseMinimalApiMapGet), helperUsesMapMethods=$($audit.HelperUsesMapMethods), failures=$failures")
            }
        }
        if ($hazardInventory.PSObject.Properties.Match("FullCommonRouteDelegateAudits").Count -gt 0 -and
            @($hazardInventory.FullCommonRouteDelegateAudits).Count -gt 0) {
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine("Full common operator route-delegate audit:")
            foreach ($audit in @($hazardInventory.FullCommonRouteDelegateAudits)) {
                $failures = if (@($audit.Failures).Count -gt 0) { @($audit.Failures) -join ", " } else { "none" }
                [void]$sb.AppendLine("- **$($audit.PackageName)** at ``$($audit.SourcePath)``: $($audit.Status), usesRequestDelegateHelper=$($audit.FullCommonRoutesUseRequestDelegateHelper), usesMinimalApiMapGet=$($audit.FullCommonRoutesUseMinimalApiMapGet), helperUsesMapMethods=$($audit.HelperUsesMapMethods), failures=$failures")
            }
        }
        if ($hazardInventory.PSObject.Properties.Match("FullOperatorRouteDelegateAudits").Count -gt 0 -and
            @($hazardInventory.FullOperatorRouteDelegateAudits).Count -gt 0) {
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine("Full operator route-delegate audit:")
            foreach ($audit in @($hazardInventory.FullOperatorRouteDelegateAudits)) {
                $failures = if (@($audit.Failures).Count -gt 0) { @($audit.Failures) -join ", " } else { "none" }
                $contracts = if (@($audit.SourceGeneratedRequestBodyContracts).Count -gt 0) { @($audit.SourceGeneratedRequestBodyContracts) -join ", " } else { "none" }
                [void]$sb.AppendLine("- **$($audit.PackageName)** at ``$($audit.SourcePath)``: $($audit.Status), usesGetRequestDelegateHelper=$($audit.FullOperatorRoutesUseGetRequestDelegateHelper), usesPostRequestDelegateHelper=$($audit.FullOperatorRoutesUsePostRequestDelegateHelper), usesMinimalApiMapGet=$($audit.FullOperatorRoutesUseMinimalApiMapGet), usesMinimalApiMapPost=$($audit.FullOperatorRoutesUseMinimalApiMapPost), requestBodyJsonHelper=$($audit.RequestBodyJsonHelperFound), sourceGeneratedRequestBodies=$contracts, failures=$failures")
            }
        }
        if ($hazardInventory.PSObject.Properties.Match("OperatorResponseJsonContractAudits").Count -gt 0 -and
            @($hazardInventory.OperatorResponseJsonContractAudits).Count -gt 0) {
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine("Operator response JSON contract audit:")
            foreach ($audit in @($hazardInventory.OperatorResponseJsonContractAudits)) {
                $failures = if (@($audit.Failures).Count -gt 0) { @($audit.Failures) -join ", " } else { "none" }
                $contracts = if (@($audit.SourceGeneratedResponseContracts).Count -gt 0) { @($audit.SourceGeneratedResponseContracts) -join ", " } else { "none" }
                [void]$sb.AppendLine("- **$($audit.PackageName)**: $($audit.Status), httpJsonResolver=$($audit.HttpJsonOptionsResolverRegistered), mvcJsonResolver=$($audit.MvcJsonOptionsResolverRegistered), sourceGeneratedResponses=$contracts, failures=$failures")
            }
        }
    }
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("See ``claim-validation-report.json`` next to this README for the full structured report.")
    if ($null -ne $hazardInventory) {
        [void]$sb.AppendLine("See ``hazard-inventory.json`` for the manifest-backed inventory snapshot.")
    }

    Set-Content -LiteralPath $mdPath -Value $sb.ToString() -Encoding UTF8

    return [pscustomobject]@{
        JsonPath             = $jsonPath
        HazardInventoryPath  = if ($null -ne $hazardInventory) { $hazardInventoryPath } else { $null }
        MarkdownPath         = $mdPath
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
    $hazardInventory = Get-DeploymentModeHazardInventory -Manifest $manifest -RepoRoot $RepoRoot
    Invoke-Step -Title "Building hazard inventory" -Detail ("packages=" + $hazardInventory.TotalPackages + ", hazards=" + $hazardInventory.TotalKnownHazards)

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

    $representativePublishTargetCount = @($PublishTargets).Count
    if ($representativePublishTargetCount -eq 0 -and
        $manifest.PSObject.Properties.Match("representativePublishTargets").Count -gt 0 -and
        $null -ne $manifest.representativePublishTargets.projects) {
        $representativePublishTargetCount = @($manifest.representativePublishTargets.projects).Count
    }

    $publishProbePolicy = Get-PublishProbePolicySnapshot `
        -Manifest $manifest `
        -CurrentRunSkipsPublish:$SkipPublish `
        -RepresentativePublishTargetCount $representativePublishTargetCount
    Invoke-Step -Title "Publish-probe policy" -Detail ("releaseValidationMode=" + $publishProbePolicy.ReleaseValidationMode + ", currentRunSkipsPublish=" + $publishProbePolicy.CurrentRunSkipsPublish)

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
    $publishProbeGate = Compute-PublishProbeGateResult -ModeReports $modeReports -PublishProbePolicy $publishProbePolicy

    $report = [ordered]@{
        GeneratedAtUtc      = (Get-Date).ToUniversalTime().ToString("o")
        DeploymentMode      = $DeploymentMode
        Configuration       = $Configuration
        RepoRoot            = $RepoRoot
        ManifestPath        = $ManifestPath
        ManifestSnapshot    = $manifest
        Modes               = $modeReports
        Verdicts            = @($modeReports | ForEach-Object { [pscustomobject]@{ Mode = $_.Mode; Verdict = $_.Verdict; Reasons = $_.Reasons } })
        HazardInventory     = $hazardInventory
        PublishProbePolicy  = $publishProbePolicy
        PublishProbeGate    = $publishProbeGate
        AggregateVerdict    = $aggregateVerdict
        ValidVerdicts       = $Script:ValidVerdicts
        ValidationStrategy  = if ($SkipPublish) { "audit-only" } else { "publish-required" }
    }
    $reportObj = [pscustomobject]$report

    Invoke-Step -Title "Writing report" -Detail $OutputPath
    $paths = Write-ValidationReport -OutputDir $OutputPath -Report $reportObj

    Invoke-Step -Title "Aggregate verdict" -Detail $aggregateVerdict
    Invoke-Step -Title "Publish-probe release gate" -Detail $publishProbeGate.Status
    if ($hazardInventory.PSObject.Properties.Match("BoundaryAnnotationAuditStatus").Count -gt 0) {
        Invoke-Step -Title "Boundary annotation audit" -Detail $hazardInventory.BoundaryAnnotationAuditStatus
    }
    if ($hazardInventory.PSObject.Properties.Match("CoreRouteDelegateAuditStatus").Count -gt 0) {
        Invoke-Step -Title "Core route-delegate audit" -Detail $hazardInventory.CoreRouteDelegateAuditStatus
    }
    if ($hazardInventory.PSObject.Properties.Match("FullCommonRouteDelegateAuditStatus").Count -gt 0) {
        Invoke-Step -Title "Full common route-delegate audit" -Detail $hazardInventory.FullCommonRouteDelegateAuditStatus
    }
    if ($hazardInventory.PSObject.Properties.Match("FullOperatorRouteDelegateAuditStatus").Count -gt 0) {
        Invoke-Step -Title "Full operator route-delegate audit" -Detail $hazardInventory.FullOperatorRouteDelegateAuditStatus
    }
    if ($hazardInventory.PSObject.Properties.Match("OperatorResponseJsonContractAuditStatus").Count -gt 0) {
        Invoke-Step -Title "Operator response JSON contract audit" -Detail $hazardInventory.OperatorResponseJsonContractAuditStatus
    }

    if ($aggregateVerdict -eq "claim-overstated") {
        throw "claim-overstated: see $($paths.JsonPath) for details"
    }
    if ($publishProbeGate.Status -eq "failed") {
        throw "publish-probe-gate-failed: see $($paths.JsonPath) for details"
    }
    if ($hazardInventory.PSObject.Properties.Match("BoundaryAnnotationAuditStatus").Count -gt 0 -and
        $hazardInventory.BoundaryAnnotationAuditStatus -eq "failed") {
        $detailsPath = if ($paths.HazardInventoryPath) { $paths.HazardInventoryPath } else { $paths.JsonPath }
        throw "boundary-annotation-audit-failed: see $detailsPath for details"
    }
    if ($hazardInventory.PSObject.Properties.Match("CoreRouteDelegateAuditStatus").Count -gt 0 -and
        $hazardInventory.CoreRouteDelegateAuditStatus -eq "failed") {
        $detailsPath = if ($paths.HazardInventoryPath) { $paths.HazardInventoryPath } else { $paths.JsonPath }
        throw "core-route-delegate-audit-failed: see $detailsPath for details"
    }
    if ($hazardInventory.PSObject.Properties.Match("FullCommonRouteDelegateAuditStatus").Count -gt 0 -and
        $hazardInventory.FullCommonRouteDelegateAuditStatus -eq "failed") {
        $detailsPath = if ($paths.HazardInventoryPath) { $paths.HazardInventoryPath } else { $paths.JsonPath }
        throw "full-common-route-delegate-audit-failed: see $detailsPath for details"
    }
    if ($hazardInventory.PSObject.Properties.Match("FullOperatorRouteDelegateAuditStatus").Count -gt 0 -and
        $hazardInventory.FullOperatorRouteDelegateAuditStatus -eq "failed") {
        $detailsPath = if ($paths.HazardInventoryPath) { $paths.HazardInventoryPath } else { $paths.JsonPath }
        throw "full-operator-route-delegate-audit-failed: see $detailsPath for details"
    }
    if ($hazardInventory.PSObject.Properties.Match("OperatorResponseJsonContractAuditStatus").Count -gt 0 -and
        $hazardInventory.OperatorResponseJsonContractAuditStatus -eq "failed") {
        $detailsPath = if ($paths.HazardInventoryPath) { $paths.HazardInventoryPath } else { $paths.JsonPath }
        throw "operator-response-json-contract-audit-failed: see $detailsPath for details"
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
