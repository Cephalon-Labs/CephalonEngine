# Pure assertion helper shared by the external app smoke and its malformed-payload tests.
function Assert-GeneratedAppRuntimeContract {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Manifest,
        [Parameter(Mandatory)]$Snapshot,
        [Parameter(Mandatory)]$Configuration,
        [string]$ExpectedBlueprint = 'modular-monolith'
    )
    Set-StrictMode -Version Latest
    if ($Configuration.Engine.Blueprint -cne $ExpectedBlueprint) { throw 'Generated configuration has an unexpected blueprint.' }
    if (@($Configuration.Engine.Patterns) -cnotcontains 'shared-foundation' -or
        @($Configuration.Engine.Transports) -cnotcontains 'rest-api') { throw 'Generated configuration lost the required foundation or REST transport.' }

    foreach ($candidate in @($Manifest, $Snapshot.manifest)) {
        if ($candidate.manifestVersion -cne '2.0') { throw 'Expected manifest schema 2.0.' }
        if ([string]::IsNullOrWhiteSpace($candidate.engineVersion)) { throw 'Manifest engine version is missing.' }
        if ($candidate.appProfile.blueprintId -cne $ExpectedBlueprint) { throw 'Runtime blueprint does not match generated configuration.' }
        foreach ($dimension in @('patterns', 'transports', 'technologies')) {
            $actual = @($candidate.appProfile.$dimension | ForEach-Object { $_.id } | Sort-Object -CaseSensitive)
            $expected = @($Configuration.Engine.$dimension | Sort-Object -CaseSensitive)
            if (($actual | ConvertTo-Json -Compress -AsArray) -cne ($expected | ConvertTo-Json -Compress -AsArray)) {
                throw "Runtime $dimension do not match generated configuration."
            }
        }
        $moduleIds = @($candidate.modules | ForEach-Object { $_.id })
        if ($moduleIds.Count -eq 0 -or @($moduleIds | Sort-Object -Unique).Count -ne $moduleIds.Count) { throw 'Manifest modules must have distinct IDs.' }
        foreach ($module in $candidate.modules) {
            if ([string]::IsNullOrWhiteSpace($module.id) -or [string]::IsNullOrWhiteSpace($module.version)) { throw 'Manifest module identity or version is missing.' }
        }
        $assemblies = @($candidate.modules | ForEach-Object { $_.assemblyName })
        foreach ($assembly in $Configuration.Engine.Discovery.Assemblies) {
            if ($assemblies -cnotcontains $assembly) { throw "Configured module assembly '$assembly' was not discovered." }
        }
        if (@($candidate.capabilities).Count -eq 0) { throw 'Generated modules contributed no capabilities.' }
        foreach ($capability in $candidate.capabilities) {
            if ([string]::IsNullOrWhiteSpace($capability.key) -or $moduleIds -cnotcontains $capability.sourceModuleId) {
                throw 'Capability source does not identify a loaded module.'
            }
        }
    }
    # JSON object order is irrelevant; array order remains part of the deterministic manifest.
    foreach ($field in @('manifestVersion', 'engineVersion', 'appProfile', 'modules', 'capabilities', 'packages')) {
        $left = [System.Text.Json.Nodes.JsonNode]::Parse((ConvertTo-Json -InputObject $Manifest.$field -Depth 100 -Compress))
        $right = [System.Text.Json.Nodes.JsonNode]::Parse((ConvertTo-Json -InputObject $Snapshot.manifest.$field -Depth 100 -Compress))
        if (-not [System.Text.Json.Nodes.JsonNode]::DeepEquals($left, $right)) { throw "Snapshot manifest $field differs from /engine. Collection ordering is part of the contract." }
    }
    if ($Snapshot.status.status -cne 4 -or [string]::IsNullOrWhiteSpace($Snapshot.status.startedAtUtc) -or
        $null -ne $Snapshot.status.lastFailure) { throw 'Snapshot does not prove a successfully started runtime.' }
    [pscustomobject][ordered]@{
        ManifestVersion = $Manifest.manifestVersion; EngineVersion = $Manifest.engineVersion
        BlueprintId = $Manifest.appProfile.blueprintId; RuntimeStatus = 'Started'
        ModuleCount = @($Manifest.modules).Count; CapabilityCount = @($Manifest.capabilities).Count
        PatternIds = @($Configuration.Engine.Patterns); TransportIds = @($Configuration.Engine.Transports)
        ConfiguredAssemblies = @($Configuration.Engine.Discovery.Assemblies)
    }
}
