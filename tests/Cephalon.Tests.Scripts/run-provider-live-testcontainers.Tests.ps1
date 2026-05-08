#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/run-provider-live-testcontainers.ps1.

.DESCRIPTION
    Verifies the provider live Testcontainers execution contract without starting Docker containers.
#>

BeforeAll {
    $env:CEPHALON_PROVIDER_LIVE_TESTCONTAINERS_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\run-provider-live-testcontainers.ps1"
    $script:workflowPath = Join-Path $script:repoRoot ".github\workflows\provider-live-testcontainers.yml"
    . $script:scriptPath
}

AfterAll {
    Remove-Item Env:\CEPHALON_PROVIDER_LIVE_TESTCONTAINERS_NO_RUN -ErrorAction SilentlyContinue
}

Describe "run-provider-live-testcontainers.ps1 provider matrix" {
    It "tracks the eight Docker-backed provider live proof lanes" {
        $matrix = Get-ProviderLiveTestMatrix

        $matrix.Keys | Should -Be @(
            "Cassandra",
            "ClickHouse",
            "Elasticsearch",
            "Nats",
            "Neo4j",
            "OpenSearch",
            "Qdrant",
            "Smtp"
        )

        $matrix.Cassandra.FilterToken | Should -Be "CassandraProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
        $matrix.ClickHouse.FilterToken | Should -Be "ClickHouseProvider_StagesOutboxAndInboxAgainstLiveService"
        $matrix.Elasticsearch.FilterToken | Should -Be "ElasticsearchProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
        $matrix.Nats.FilterToken | Should -Be "NatsProvider_StagesOutboxInboxAndDispatchAgainstLiveJetStream"
        $matrix.Neo4j.FilterToken | Should -Be "Neo4jProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
        $matrix.OpenSearch.FilterToken | Should -Be "OpenSearchProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
        $matrix.Qdrant.FilterToken | Should -Be "QdrantProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
        $matrix.Smtp.FilterToken | Should -Be "SmtpDelivery_DispatchesInvitationThroughLiveRelay"
    }

    It "expands All into every provider" {
        $providers = Resolve-ProviderLiveTestSelection -ProviderNames @("All")

        $providers.Provider | Should -Be @(
            "Cassandra",
            "ClickHouse",
            "Elasticsearch",
            "Nats",
            "Neo4j",
            "OpenSearch",
            "Qdrant",
            "Smtp"
        )
    }

    It "accepts provider names case-insensitively like ValidateSet does" {
        $providers = Resolve-ProviderLiveTestSelection -ProviderNames @("nats")

        $providers.Provider | Should -Be @("Nats")
        $providers.FilterToken | Should -Be @("NatsProvider_StagesOutboxInboxAndDispatchAgainstLiveJetStream")
    }

    It "rejects unknown providers before test execution" {
        { Resolve-ProviderLiveTestSelection -ProviderNames @("CosmosDb") } |
            Should -Throw "*Unknown provider 'CosmosDb'*"
    }

    It "keeps restore locked by default" {
        $scriptText = Get-Content -LiteralPath $script:scriptPath -Raw -Encoding UTF8

        $scriptText | Should -Match "dotnet"
        $scriptText | Should -Match "--locked-mode"
        $scriptText | Should -Match "CEPHALON_PROVIDER_EXTERNAL_SERVICES"
        $scriptText | Should -Match "CEPHALON_PROVIDER_TESTCONTAINERS"
    }

    It "resolves the Windows docker executable when both docker.exe and docker shims are on PATH" {
        Mock Get-Command {
            @(
                [pscustomobject]@{ Source = "C:\Program Files\Docker\Docker\resources\bin\docker.exe" },
                [pscustomobject]@{ Source = "C:\Program Files\Docker\Docker\resources\bin\docker" }
            )
        } -ParameterFilter { $Name -eq "docker" -and $CommandType -eq "Application" }

        Resolve-ProviderLiveDockerCommandPath | Should -Be "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
    }
}

Describe "provider-live-testcontainers workflow" {
    BeforeAll {
        $script:workflowText = Get-Content -LiteralPath $script:workflowPath -Raw -Encoding UTF8
    }

    It "stays scheduled and manually dispatchable" {
        $script:workflowText | Should -Match "workflow_dispatch:"
        $script:workflowText | Should -Match "schedule:"
        $script:workflowText | Should -Match "cron:"
    }

    It "runs the same provider matrix as the script" {
        foreach ($provider in (Get-ProviderLiveTestMatrix).Keys) {
            $script:workflowText | Should -Match "- $provider"
        }
    }

    It "uses the shared script instead of duplicating dotnet test logic" {
        $script:workflowText | Should -Match "run-provider-live-testcontainers\.ps1"
        $script:workflowText | Should -Match "-Providers"
        $script:workflowText | Should -Match "actions/setup-dotnet@v4"
        $script:workflowText | Should -Match "docker info"
    }
}
