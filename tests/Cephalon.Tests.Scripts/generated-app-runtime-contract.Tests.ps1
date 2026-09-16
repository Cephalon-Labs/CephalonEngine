BeforeAll {
    . (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'scripts/generated-app-runtime-contract.ps1')
    function New-RuntimeFixture {
        $manifest = @'
{"manifestVersion":"2.0","engineVersion":"0.1.0","appProfile":{"blueprintId":"modular-monolith","patterns":[{"id":"shared-foundation"}],"transports":[{"id":"rest-api"}],"technologies":[]},"modules":[{"id":"foundation","version":"1.0.0","assemblyName":"Probe.Foundation"}],"capabilities":[{"key":"foundation.status","sourceModuleId":"foundation"}],"packages":[]}
'@ | ConvertFrom-Json
        @{
            Manifest = $manifest
            Snapshot = [pscustomobject]@{
                manifest = ($manifest | ConvertTo-Json -Depth 100 | ConvertFrom-Json)
                status = [pscustomobject]@{ status = 4; startedAtUtc = '2026-09-16T00:00:00Z'; lastFailure = $null }
                futureSurface = @{}
            }
            Configuration = [pscustomobject]@{ Engine = [pscustomobject]@{
                Blueprint = 'modular-monolith'; Patterns = @('shared-foundation'); Transports = @('rest-api')
                Technologies = @(); Discovery = [pscustomobject]@{ Assemblies = @('Probe.Foundation') }
            } }
        }
    }
}

Describe 'Generated app runtime contract' {
    BeforeEach { $fixture = New-RuntimeFixture }
    It 'accepts the declared wire contract and additional snapshot surfaces' {
        $result = Assert-GeneratedAppRuntimeContract @fixture
        $result.RuntimeStatus | Should -Be 'Started'
        $result.ModuleCount | Should -Be 1
        $result.CapabilityCount | Should -Be 1
    }
    It 'rejects a successful HTTP response with an unrelated body' {
        $fixture.Snapshot = [pscustomobject]@{ ok = $true }
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw
    }
    It 'accepts equivalent objects whose JSON properties are ordered differently' {
        $fixture.Snapshot.manifest.modules[0] = [pscustomobject][ordered]@{
            assemblyName = 'Probe.Foundation'; version = '1.0.0'; id = 'foundation'
        }
        (Assert-GeneratedAppRuntimeContract @fixture).ModuleCount | Should -Be 1
    }
    It 'rejects a stale manifest schema' {
        $fixture.Manifest.manifestVersion = '1.0'
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*schema 2.0*'
    }
    It 'rejects runtime transport drift from the generated file' {
        $fixture.Manifest.appProfile.transports[0].id = 'grpc'
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*transports*configuration*'
    }
    It 'rejects a configured assembly absent from runtime discovery' {
        $fixture.Manifest.modules[0].assemblyName = 'Other.Assembly'
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*was not discovered*'
    }
    It 'rejects a capability attributed to an unknown module' {
        $fixture.Manifest.capabilities[0].sourceModuleId = 'missing'
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*Capability source*'
    }
    It 'rejects duplicate module identifiers' {
        $fixture.Manifest.modules += $fixture.Manifest.modules[0]
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*distinct IDs*'
    }
    It 'rejects missing module version metadata' {
        $fixture.Manifest.modules[0].version = ''
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*version is missing*'
    }
    It 'rejects a different manifest inside the snapshot' {
        $fixture.Snapshot.manifest.engineVersion = '0.2.0'
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*engineVersion differs*'
    }
    It 'rejects a snapshot that has not completed startup' {
        $fixture.Snapshot.status.status = 3
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*successfully started*'
    }
    It 'rejects a retained lifecycle failure' {
        $fixture.Snapshot.status.lastFailure = [pscustomobject]@{ reason = 'start failed' }
        { Assert-GeneratedAppRuntimeContract @fixture } | Should -Throw '*successfully started*'
    }
}
