param(
    [string]$AppName = "Cephalon.SaasTenantGovernanceSmoke",
    [string]$HostUrl = "http://127.0.0.1:18088",
    [string]$GrpcHostUrl = "http://127.0.0.1:18089",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [string]$ReportPath = "artifacts/adoption-smoke/saas-tenant-governance-audit.json",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validationStartedAtUtc = [DateTimeOffset]::UtcNow
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cstga-" + ([Guid]::NewGuid().ToString("N")).Substring(0, 12))
$packageFeedPath = Join-Path $tempRoot "package-feed"
$toolPath = Join-Path $tempRoot ".tools\cephalon"
$nuGetPackagesPath = Join-Path $tempRoot ".nuget\packages"
$workspaceRoot = Join-Path $tempRoot "workspace"
$generatedRoot = Join-Path $workspaceRoot $AppName
$generatedPackageFeedPath = Join-Path $generatedRoot ".cephalon\packages"
$grpcProbeRoot = Join-Path $workspaceRoot "$AppName.GrpcProbe"
$grpcProbeProjectPath = Join-Path $grpcProbeRoot "$AppName.GrpcProbe.csproj"
$grpcProbeProgramPath = Join-Path $grpcProbeRoot "Program.cs"
$stdoutLogPath = Join-Path $tempRoot "saas-tenant-governance.stdout.log"
$stderrLogPath = Join-Path $tempRoot "saas-tenant-governance.stderr.log"
$grpcProbeStdoutLogPath = Join-Path $tempRoot "saas-tenant-governance-grpc-probe.stdout.log"
$grpcProbeStderrLogPath = Join-Path $tempRoot "saas-tenant-governance-grpc-probe.stderr.log"
$packageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.Engine.SourceGen/Cephalon.Engine.SourceGen.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.AspNetCore.JsonRpc/Cephalon.AspNetCore.JsonRpc.csproj",
    "src/Cephalon.AspNetCore.Grpc/Cephalon.AspNetCore.Grpc.csproj",
    "src/Cephalon.Audit/Cephalon.Audit.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Cli/Cephalon.Cli.csproj",
    "src/Cephalon.MultiTenancy/Cephalon.MultiTenancy.csproj",
    "src/Cephalon.MultiTenancy.Governance/Cephalon.MultiTenancy.Governance.csproj",
    "src/Cephalon.MultiTenancy.Governance.AspNetCore/Cephalon.MultiTenancy.Governance.AspNetCore.csproj",
    "src/Cephalon.Observability/Cephalon.Observability.csproj",
    "src/Cephalon.Observability.DependencyHealth.Core/Cephalon.Observability.DependencyHealth.Core.csproj",
    "src/Cephalon.Observability.HttpDependencies/Cephalon.Observability.HttpDependencies.csproj",
    "src/Cephalon.Observability.OpenTelemetry/Cephalon.Observability.OpenTelemetry.csproj",
    "src/Cephalon.Observability.Serilog/Cephalon.Observability.Serilog.csproj",
    "src/Cephalon.ReferenceDocs/Cephalon.ReferenceDocs.csproj",
    "src/Cephalon.Resilience/Cephalon.Resilience.csproj",
    "src/Cephalon.Scaffolding/Cephalon.Scaffolding.csproj"
)
$cephalonExecutableFileName = if ($IsWindows) { "cephalon.exe" } else { "cephalon" }
$cephalonExecutablePath = Join-Path $toolPath $cephalonExecutableFileName
$environmentSnapshot = @{}

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

function Invoke-Cephalon {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & $cephalonExecutablePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Cephalon CLI command failed: $cephalonExecutablePath $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Set-ScopedEnvironmentVariable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [AllowNull()]
        [string]$Value
    )

    if (-not $script:environmentSnapshot.ContainsKey($Name)) {
        $current = Get-Item -LiteralPath "Env:\$Name" -ErrorAction SilentlyContinue
        $script:environmentSnapshot[$Name] = if ($null -eq $current) { $null } else { $current.Value }
    }

    if ($null -eq $Value) {
        Remove-Item -LiteralPath "Env:\$Name" -ErrorAction SilentlyContinue
        return
    }

    Set-Item -LiteralPath "Env:\$Name" -Value $Value
}

function Restore-ScopedEnvironmentVariables {
    foreach ($entry in $script:environmentSnapshot.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            Remove-Item -LiteralPath "Env:\$($entry.Key)" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -LiteralPath "Env:\$($entry.Key)" -Value $entry.Value
        }
    }
}

function Resolve-GeneratedHostProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot,
        [Parameter(Mandatory = $true)]
        [string]$AppName
    )

    $sourceRoot = Join-Path $GeneratedRoot "src"
    $projects = @(Get-ChildItem -Path $sourceRoot -Recurse -Filter "*.csproj" -File | Sort-Object FullName)
    if ($projects.Count -eq 0) {
        throw "No generated host project was found under '$sourceRoot'."
    }

    $preferredProject = @(
        $projects | Where-Object { $_.BaseName -eq "$AppName.Host" } | Select-Object -First 1
        $projects | Where-Object { $_.BaseName -eq "$AppName.Service" } | Select-Object -First 1
        $projects | Where-Object { $_.Name.EndsWith(".Host.csproj", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        $projects | Where-Object { $_.Name.EndsWith(".Service.csproj", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
    ) | Where-Object { $null -ne $_ } | Select-Object -First 1

    if ($null -ne $preferredProject) {
        return $preferredProject.FullName
    }

    if ($projects.Count -eq 1) {
        return $projects[0].FullName
    }

    throw "Could not determine the generated host project under '$sourceRoot'."
}

function Wait-ForHttpSuccess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds,
        [System.Diagnostics.Process]$Process = $null
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if ($null -ne $Process -and $Process.HasExited) {
            throw "Generated host exited before '$Uri' became ready. Exit code: $($Process.ExitCode)."
        }

        try {
            $response = Invoke-WebRequest -Uri $Uri -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                Write-Host "Validated $Uri" -ForegroundColor Green
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    }
    while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for HTTP success from '$Uri'."
}

function Invoke-HttpJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [string]$Method = "GET",
        [object]$Body = $null
    )

    $arguments = @{
        Uri = $Uri
        Method = $Method
        TimeoutSec = 20
    }
    if ($null -ne $Body) {
        $arguments["ContentType"] = "application/json"
        $arguments["Body"] = ($Body | ConvertTo-Json -Depth 16)
    }

    $response = Invoke-WebRequest @arguments
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "Expected HTTP success from '$Uri' but received $($response.StatusCode)."
    }

    return $response.Content
}

function Wait-ForDependencyHealthReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [string]$DependencyId,
        [Parameter(Mandatory = $true)]
        [string]$Source,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds,
        [System.Diagnostics.Process]$Process = $null
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastContent = ""
    do {
        if ($null -ne $Process -and $Process.HasExited) {
            throw "Generated host exited before '$Uri' reported dependency '$DependencyId'. Exit code: $($Process.ExitCode)."
        }

        try {
            $lastContent = Invoke-HttpJson -Uri $Uri
            $reports = @($lastContent | ConvertFrom-Json -Depth 32)
            $report = $reports |
                Where-Object { $_.id -eq $DependencyId -or $_.Id -eq $DependencyId } |
                Select-Object -First 1

            if ($null -ne $report) {
                $state = if ($report.PSObject.Properties.Name -contains "state") { $report.state } else { $report.State }
                $reportSource = if ($report.PSObject.Properties.Name -contains "source") { [string]$report.source } else { [string]$report.Source }
                $isHealthy = ([string]$state -eq "0" -or [string]$state -eq "Healthy")
                $hasSource = $reportSource.Contains($Source, [System.StringComparison]::OrdinalIgnoreCase)

                if ($isHealthy -and $hasSource) {
                    Write-Host "Validated dependency '$DependencyId' from $Uri" -ForegroundColor Green
                    return $lastContent
                }
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    }
    while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for '$Uri' to report healthy dependency '$DependencyId' from '$Source'. Last payload: $lastContent"
}

function Write-RecentLogs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Write-Host ""
    Write-Host "${Label}:" -ForegroundColor Yellow
    Get-Content -LiteralPath $Path -Tail 120
}

function Resolve-ReportPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Add-GeneratedHostPackageReferences {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath,
        [Parameter(Mandatory = $true)]
        [string[]]$Packages
    )

    $projectText = Get-Content -LiteralPath $HostProjectPath -Raw
    $missingPackages = @($Packages | Where-Object {
            -not $projectText.Contains("Include=""$_""", [System.StringComparison]::Ordinal)
        })

    if ($missingPackages.Count -eq 0) {
        return
    }

    $packageReferenceLines = $missingPackages |
        ForEach-Object { "    <PackageReference Include=""$_"" />" }
    $itemGroup = @"

  <ItemGroup>
$($packageReferenceLines -join [Environment]::NewLine)
  </ItemGroup>
"@

    $projectText = $projectText.Replace("</Project>", "$itemGroup`r`n</Project>")
    Set-Content -LiteralPath $HostProjectPath -Value $projectText -Encoding UTF8

    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $generatedSourceRoot = Split-Path -Parent $hostProjectDirectory
    $generatedRoot = Split-Path -Parent $generatedSourceRoot
    $directoryPackagesPath = Join-Path $generatedRoot "Directory.Packages.props"
    if (-not (Test-Path -LiteralPath $directoryPackagesPath -PathType Leaf)) {
        throw "Expected generated central package version file '$directoryPackagesPath'."
    }

    $directoryPackages = Get-Content -LiteralPath $directoryPackagesPath -Raw
    $missingPackageVersions = @($Packages | Where-Object {
            -not $directoryPackages.Contains("Include=""$_""", [System.StringComparison]::Ordinal)
        })
    if ($missingPackageVersions.Count -eq 0) {
        return
    }

    $packageVersionLines = $missingPackageVersions |
        ForEach-Object { "    <PackageVersion Include=""$_"" Version=""0.1.0-preview"" />" }
    $directoryPackages = $directoryPackages.Replace(
        "  </ItemGroup>",
        "$($packageVersionLines -join [Environment]::NewLine)`r`n  </ItemGroup>")
    Set-Content -LiteralPath $directoryPackagesPath -Value $directoryPackages -Encoding UTF8
}

function Add-Using {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProgramText,
        [Parameter(Mandatory = $true)]
        [string]$UsingLine
    )

    if ($ProgramText.Contains($UsingLine, [System.StringComparison]::Ordinal)) {
        return $ProgramText
    }

    return "$UsingLine`r`n$ProgramText"
}

function Update-GeneratedSaasHostProgram {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath
    )

    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $programPath = Join-Path $hostProjectDirectory "Program.cs"
    if (-not (Test-Path -LiteralPath $programPath -PathType Leaf)) {
        throw "Expected generated Program.cs at '$programPath'."
    }

    $program = Get-Content -LiteralPath $programPath -Raw
    foreach ($usingLine in @(
            "using Cephalon.Abstractions.Audit;",
            "using Cephalon.Audit.Registration;",
            "using Cephalon.Audit.Services;",
            "using Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;",
            "using Cephalon.MultiTenancy.Governance.Registration;",
            "using Cephalon.MultiTenancy.Governance.Services;",
            "using Cephalon.Observability.HttpDependencies.Hosting;",
            "using Microsoft.Extensions.DependencyInjection;")) {
        $program = Add-Using -ProgramText $program -UsingLine $usingLine
    }

    $serviceRegistration = @"
builder.Services.AddSingleton<ITenantInvitationDeliverySender, SmokeTenantInvitationDeliverySender>();
builder.Services.AddCephalonHttpDependencyHealth(builder.Configuration);
builder.AddCephalonMultiTenancyGovernanceAspNetCore(options =>
{
    options.RequireTenantAdministrationAuthorization = false;
    options.RequireTenantInvitationDeliveryDispatchAuthorization = false;
    options.RequireTenantInvitationDeliveryStatusCallbackAuthorization = false;
    options.RequireTenantInvitationDeliveryStatusObservationAuthorization = false;
    options.TenantInvitationDeliveryStatusObservationDefaultLimit = 25;
    options.TenantInvitationDeliveryStatusObservationMaxLimit = 25;
});
"@

    if (-not $program.Contains("SmokeTenantInvitationDeliverySender", [System.StringComparison]::Ordinal)) {
        $program = $program.Replace(
            "builder.AddCephalonProjectConfigurations();",
            "builder.AddCephalonProjectConfigurations();`r`n$serviceRegistration")
    }

    if (-not $program.Contains("engine.AddMultiTenancyGovernance();", [System.StringComparison]::Ordinal)) {
        $program = $program.Replace(
            "    engine.AddMultiTenancy();",
            "    engine.AddMultiTenancy();`r`n    engine.AddAudit();`r`n    engine.AddMultiTenancyGovernance();")
    }

    $endpointBlock = @"
app.MapCephalonTenantDomainOwnershipHttpProofs();
app.MapCephalonTenantAdministrationCommands();
app.MapCephalonTenantInvitationDeliveryDispatches();
app.MapCephalonTenantInvitationDeliveryStatusCallbacks();
app.MapCephalonTenantInvitationDeliveryStatusObservations();
app.MapPost("/api/tenants/{tenantId}/audit-proof", async (
    string tenantId,
    IAuditRecorder auditRecorder,
    CancellationToken cancellationToken) =>
{
    var entry = await auditRecorder.RecordAsync(
        new AuditRecordRequest(
            category: "tenant-governance",
            action: "adoption-smoke",
            summary: "SaaS tenant governance adoption smoke recorded an audit event.",
            subjectType: "tenant",
            subjectId: tenantId,
            actor: new AuditActor("operator", "smoke-runner"),
            outcome: AuditOutcome.Succeeded,
            tenantId: tenantId,
            correlationId: "saas-governance-smoke",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["source"] = "validate-saas-tenant-governance-audit-adoption"
            }),
        cancellationToken);

    return Results.Ok(new
    {
        entry.Id,
        entry.Category,
        entry.Action,
        entry.SubjectType,
        entry.SubjectId,
        entry.TenantId,
        Outcome = entry.Outcome.ToString()
    });
})
   .ExcludeFromDescription();
"@

    $senderBlock = @"

internal sealed class SmokeTenantInvitationDeliverySender : ITenantInvitationDeliverySender
{
    public string SenderId => "smoke-email";

    public ValueTask<TenantInvitationDeliverySenderResult> SendAsync(
        TenantInvitationDeliveryContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new TenantInvitationDeliverySenderResult(
            TenantInvitationDeliveryOutcomes.Dispatched,
            dispatched: true,
            providerMessageId: "provider-message-saas-smoke",
            reason: "Accepted by the SaaS governance adoption smoke sender.",
            dispatchedAtUtc: context.DispatchedAtUtc,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["senderMetadata"] = "accepted",
                ["senderOwnership"] = "consumer-registered"
            }));
    }
}
"@

    if (-not $program.Contains("/api/tenants/{tenantId}/audit-proof", [System.StringComparison]::Ordinal)) {
        $needle = "app.MapCephalon();`r`napp.Run();"
        if (-not $program.Contains($needle, [System.StringComparison]::Ordinal)) {
            $needle = "app.MapCephalon();`napp.Run();"
        }

        if (-not $program.Contains($needle, [System.StringComparison]::Ordinal)) {
            throw "Could not find the generated app.MapCephalon/app.Run block to extend."
        }

        $program = $program.Replace($needle, "app.MapCephalon();`r`n$endpointBlock`r`napp.Run();$senderBlock")
    }

    Set-Content -LiteralPath $programPath -Value $program -Encoding UTF8
}

function Assert-GeneratedSaasGovernanceFoundation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath
    )

    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $sourceRoot = Split-Path -Parent $hostProjectDirectory
    $appModelSettingsPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.AppModel.json"
    $programPath = Join-Path $hostProjectDirectory "Program.cs"

    foreach ($path in @($appModelSettingsPath, $programPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Expected generated SaaS adoption file '$path'."
        }
    }

    $projectText = (
        Get-ChildItem -Path $sourceRoot -Recurse -Filter "*.csproj" -File |
            ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
    ) -join [Environment]::NewLine

    foreach ($token in @(
            "Cephalon.MultiTenancy",
            "Cephalon.MultiTenancy.Governance",
            "Cephalon.MultiTenancy.Governance.AspNetCore",
            "Cephalon.Audit",
            "Cephalon.AspNetCore.JsonRpc",
            "Cephalon.AspNetCore.Grpc",
            "Cephalon.Observability.HttpDependencies")) {
        if (-not $projectText.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated project graph to contain package reference token '$token'."
        }
    }

    $program = Get-Content -LiteralPath $programPath -Raw
    foreach ($token in @(
            "builder.AddJsonRpcTransport();",
            "builder.AddGrpcTransport();",
            "builder.Services.AddCephalonHttpDependencyHealth(builder.Configuration);",
            "engine.AddMultiTenancy();",
            "engine.AddAudit();",
            "engine.AddMultiTenancyGovernance();",
            "app.MapCephalonTenantAdministrationCommands();",
            "app.MapCephalonTenantInvitationDeliveryDispatches();",
            "app.MapCephalonTenantInvitationDeliveryStatusCallbacks();",
            "app.MapCephalonTenantInvitationDeliveryStatusObservations();",
            "IAuditRecorder",
            "SmokeTenantInvitationDeliverySender")) {
        if (-not $program.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated Program.cs to contain '$token'."
        }
    }
}

function Write-GrpcProbeProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageFeedPath
    )

    New-Item -ItemType Directory -Path $grpcProbeRoot -Force | Out-Null
    $nugetConfigPath = Join-Path $grpcProbeRoot "NuGet.config"

    @"
<configuration>
  <packageSources>
    <clear />
    <add key="cephalon" value="$PackageFeedPath" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="cephalon">
      <package pattern="Cephalon*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfigPath -Encoding UTF8

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Cephalon.AspNetCore.Grpc" Version="0.1.0-preview" />
    <PackageReference Include="Grpc.Net.Client" Version="2.76.0" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $grpcProbeProjectPath -Encoding UTF8

    @'
using System.Net;
using Cephalon.AspNetCore.Grpc.Contracts.Discovery;
using Grpc.Net.Client;

var grpcUrl = args.Length > 0
    ? args[0]
    : throw new InvalidOperationException("The gRPC host URL argument is required.");

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

using var httpHandler = new GrpcSubdirectoryHandler(new SocketsHttpHandler(), "/grpc");
using var httpClient = new HttpClient(httpHandler)
{
    DefaultRequestVersion = HttpVersion.Version20,
    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
};
using var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
{
    HttpClient = httpClient,
    DisposeHttpClient = false
});
var client = new DiscoveryService.DiscoveryServiceClient(channel);
var reply = await client.SayHelloAsync(new HelloRequest
{
    Name = "Codex"
});

Console.WriteLine($"grpc-message={reply.Message}");
Console.WriteLine($"grpc-traits={string.Join(",", reply.Traits)}");

if (!reply.Message.Contains("Hello, Codex", StringComparison.Ordinal))
{
    return 2;
}

if (!reply.Message.Contains("Platform", StringComparison.OrdinalIgnoreCase))
{
    return 3;
}

if (!reply.Traits.Contains("grpc"))
{
    return 4;
}

return 0;

internal sealed class GrpcSubdirectoryHandler : DelegatingHandler
{
    private readonly string subdirectory;

    public GrpcSubdirectoryHandler(HttpMessageHandler innerHandler, string subdirectory)
        : base(innerHandler)
    {
        ArgumentNullException.ThrowIfNull(innerHandler);
        ArgumentException.ThrowIfNullOrWhiteSpace(subdirectory);

        this.subdirectory = subdirectory.StartsWith('/')
            ? subdirectory.TrimEnd('/')
            : "/" + subdirectory.TrimEnd('/');
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RequestUri);

        var requestUri = request.RequestUri;
        request.RequestUri = new UriBuilder(requestUri)
        {
            Path = subdirectory + requestUri.AbsolutePath
        }.Uri;

        return base.SendAsync(request, cancellationToken);
    }
}
'@ | Set-Content -LiteralPath $grpcProbeProgramPath -Encoding UTF8

    return [pscustomobject]@{
        Root = $grpcProbeRoot
        Project = $grpcProbeProjectPath
        Program = $grpcProbeProgramPath
    }
}

function Get-NonSymbolPackageCount {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return 0
    }

    return @(Get-ChildItem -Path $Path -Filter "*.nupkg" -File | Where-Object { -not $_.Name.EndsWith(".symbols.nupkg", [System.StringComparison]::OrdinalIgnoreCase) }).Count
}

function New-ProofSnippet {
    param(
        [AllowNull()]
        [string]$Value,
        [int]$MaximumLength = 700
    )

    if ([string]::IsNullOrEmpty($Value)) {
        return ""
    }

    $normalized = $Value.Replace("`r", "").Replace("`n", " ")
    if ($normalized.Length -le $MaximumLength) {
        return $normalized
    }

    return $normalized.Substring(0, $MaximumLength)
}

function New-SaasTenantGovernanceAdoptionAssertionRows {
    param([bool]$Passed)

    $status = if ($Passed) { "passed" } else { "not-run-or-failed" }
    return @(
        [pscustomobject]@{ Id = "runs-outside-repository"; Status = $status; Evidence = $generatedRoot },
        [pscustomobject]@{ Id = "publishes-local-nuget-packages"; Status = $status; Evidence = $packageFeedPath },
        [pscustomobject]@{ Id = "generates-modular-monolith-saas-host"; Status = $status; Evidence = $hostProjectPath },
        [pscustomobject]@{ Id = "proves-rest-jsonrpc-grpc-business-boundary"; Status = $status; Evidence = "$HostUrl and $GrpcHostUrl" },
        [pscustomobject]@{ Id = "proves-tenant-administration-workflow"; Status = $status; Evidence = "/engine/tenant-administration/commands" },
        [pscustomobject]@{ Id = "proves-invitation-delivery-and-status-observation"; Status = $status; Evidence = "/engine/tenant-invitations/*" },
        [pscustomobject]@{ Id = "proves-audit-recording-and-store-truth"; Status = $status; Evidence = "/api/tenants/{tenantId}/audit-proof and /engine/audit-stores" },
        [pscustomobject]@{ Id = "proves-dependency-health-runtime-truth"; Status = $status; Evidence = "/engine/dependencies" }
    )
}

function New-SaasTenantGovernanceAdoptionRuntimeProbeRows {
    param([string]$Status)

    return @(
        [pscustomobject]@{ Kind = "runtime-probe"; Path = "/health/ready"; Status = $Status },
        [pscustomobject]@{ Kind = "runtime-probe"; Path = "/engine/snapshot"; Status = $Status },
        [pscustomobject]@{ Kind = "runtime-probe"; Path = "/engine/transports"; Status = $Status },
        [pscustomobject]@{ Kind = "business-endpoint"; Path = "/api/v1/platform/status"; Status = $Status },
        [pscustomobject]@{ Kind = "business-endpoint"; Path = "/json-rpc/platform"; Status = $Status },
        [pscustomobject]@{ Kind = "business-endpoint"; Path = "gRPC DiscoveryService.SayHello via /grpc"; Status = $Status },
        [pscustomobject]@{ Kind = "operator-action"; Path = "/engine/tenant-administration/commands"; Status = $Status },
        [pscustomobject]@{ Kind = "operator-action"; Path = "/engine/tenant-invitations/delivery-dispatches"; Status = $Status },
        [pscustomobject]@{ Kind = "operator-ingress"; Path = "/engine/tenant-invitations/delivery-status"; Status = $Status },
        [pscustomobject]@{ Kind = "operator-read"; Path = "/engine/tenant-invitations/delivery-status/observations"; Status = $Status },
        [pscustomobject]@{ Kind = "operator-surface"; Path = "/engine/technology-surfaces/multi-tenancy"; Status = $Status },
        [pscustomobject]@{ Kind = "operator-surface"; Path = "/engine/audit-stores"; Status = $Status },
        [pscustomobject]@{ Kind = "operator-surface"; Path = "/engine/dependencies"; Status = $Status }
    )
}

function Write-SaasTenantGovernanceAdoptionExecutionReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status,
        [string]$ErrorMessage = ""
    )

    $completedAtUtc = [DateTimeOffset]::UtcNow
    $passed = $Status.Equals("passed", [System.StringComparison]::OrdinalIgnoreCase)
    $runtimeProbeStatus = if ($passed) { "passed" } else { "not-run-or-failed" }
    $resolvedReportPath = Resolve-ReportPath -Path $ReportPath
    $reportDirectory = Split-Path -Parent $resolvedReportPath
    if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
        New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
    }

    $report = [pscustomobject]([ordered]@{
        '$schemaVersion' = "1.0.0"
        ScenarioId = "saas-tenant-governance-audit"
        Status = $Status
        AppName = $AppName
        HostUrl = $HostUrl
        GrpcHostUrl = $GrpcHostUrl
        Configuration = $Configuration
        SkipPackageBuild = [bool]$SkipPackageBuild
        KeepOutput = [bool]$KeepOutput
        StartedAtUtc = $validationStartedAtUtc.ToString("O")
        CompletedAtUtc = $completedAtUtc.ToString("O")
        DurationMilliseconds = [math]::Round(($completedAtUtc - $validationStartedAtUtc).TotalMilliseconds, 2)
        Assertions = New-SaasTenantGovernanceAdoptionAssertionRows -Passed:$passed
        RuntimeProbes = New-SaasTenantGovernanceAdoptionRuntimeProbeRows -Status $runtimeProbeStatus
        Evidence = [pscustomobject]([ordered]@{
            LocalPackageCount = Get-NonSymbolPackageCount -Path $packageFeedPath
            GeneratedPackageCount = Get-NonSymbolPackageCount -Path $generatedPackageFeedPath
            RequiredTransports = @("rest-api", "json-rpc", "grpc")
            RestStatusSnippet = New-ProofSnippet -Value $restStatus
            JsonRpcStatusSnippet = New-ProofSnippet -Value $rpcStatus
            GrpcProbeSnippet = New-ProofSnippet -Value $grpcProbeOutput
            TenantAdministrationSnippet = New-ProofSnippet -Value $tenantAdministration
            InvitationDispatchSnippet = New-ProofSnippet -Value $invitationDispatch
            DeliveryStatusSnippet = New-ProofSnippet -Value $deliveryStatus
            DeliveryObservationSnippet = New-ProofSnippet -Value $deliveryObservations
            AuditProofSnippet = New-ProofSnippet -Value $auditProof
            AuditStoresSnippet = New-ProofSnippet -Value $auditStores
            MultiTenancySurfaceSnippet = New-ProofSnippet -Value $multiTenancySurface
            DependencyHealthSnippet = New-ProofSnippet -Value $dependencies
        })
        DependencyProbe = [pscustomobject]([ordered]@{
            Id = "saas-governance-self"
            Endpoint = "$HostUrl/engine"
            Provider = "Cephalon.Observability.HttpDependencies"
            Required = $false
        })
        Paths = [pscustomobject]([ordered]@{
            TemporaryRoot = $tempRoot
            PackageFeed = $packageFeedPath
            ToolPath = $toolPath
            NuGetPackages = $nuGetPackagesPath
            WorkspaceRoot = $workspaceRoot
            GeneratedAppRoot = $generatedRoot
            GeneratedPackageFeed = $generatedPackageFeedPath
            Solution = $solutionPath
            HostProject = $hostProjectPath
            HostAssembly = $hostAssemblyPath
            GrpcProbeRoot = $grpcProbeRoot
            GrpcProbeProject = $grpcProbeProjectPath
            StdoutLog = $stdoutLogPath
            StderrLog = $stderrLogPath
            GrpcProbeStdoutLog = $grpcProbeStdoutLogPath
            GrpcProbeStderrLog = $grpcProbeStderrLogPath
            TemporaryOutputRetained = [bool]$KeepOutput
        })
        Error = $ErrorMessage
    })

    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedReportPath -Encoding UTF8
    Write-Host "SaaS tenant governance audit adoption execution report: $resolvedReportPath" -ForegroundColor Cyan
}

$process = $null
$previousNuGetPackages = $null
$restoreRepoPackageAssets = $false
$hostProjectPath = ""
$solutionPath = ""
$hostAssemblyPath = ""
$restStatus = ""
$rpcStatus = ""
$grpcProbeOutput = ""
$tenantAdministration = ""
$invitationDispatch = ""
$deliveryStatus = ""
$deliveryObservations = ""
$auditProof = ""
$auditStores = ""
$multiTenancySurface = ""
$dependencies = ""
$snapshot = ""
$transports = ""

try {
    New-Item -ItemType Directory -Path $packageFeedPath -Force | Out-Null
    New-Item -ItemType Directory -Path $toolPath -Force | Out-Null
    New-Item -ItemType Directory -Path $nuGetPackagesPath -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null

    $previousNuGetPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $nuGetPackagesPath
    $restoreRepoPackageAssets = $true

    Write-Host ""
    Write-Host "Publishing repo-local Cephalon packages for SaaS tenant governance adoption..." -ForegroundColor Cyan
    $publishPackageArguments = @{
        Configuration = $Configuration
        OutputPath = $packageFeedPath
        ProjectPaths = $packageProjectPaths
    }
    if ($SkipPackageBuild) {
        $publishPackageArguments["SkipBuild"] = $true
    }

    & $publishPackagesScriptPath @publishPackageArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing to the temporary feed failed."
    }

    Write-Host ""
    Write-Host "Installing Cephalon CLI from the temporary package feed..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $workspaceRoot -Arguments @(
        "tool",
        "install",
        "--tool-path", $toolPath,
        "Cephalon.Cli",
        "--add-source", $packageFeedPath,
        "--ignore-failed-sources",
        "--no-cache",
        "--prerelease")

    if (-not (Test-Path -LiteralPath $cephalonExecutablePath -PathType Leaf)) {
        throw "Expected installed Cephalon CLI at '$cephalonExecutablePath'."
    }

    Write-Host ""
    Write-Host "Scaffolding a SaaS tenant host with REST, JSON-RPC, gRPC, and multi-tenancy outside the repository..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @(
        "new",
        $AppName,
        "--output", $generatedRoot,
        "--blueprint", "ModularMonolith",
        "--module", "Platform",
        "--feature", "TenantPortal",
        "--technology", "MultiTenancy",
        "--transport", "RestApi",
        "--transport", "JsonRpc",
        "--transport", "Grpc")

    $hostProjectPath = Resolve-GeneratedHostProjectPath -GeneratedRoot $generatedRoot -AppName $AppName
    $solutionPath = Join-Path $generatedRoot "$AppName.slnx"
    if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        throw "Expected generated solution at '$solutionPath'."
    }

    Add-GeneratedHostPackageReferences -HostProjectPath $hostProjectPath -Packages @(
        "Cephalon.Audit",
        "Cephalon.MultiTenancy.Governance",
        "Cephalon.MultiTenancy.Governance.AspNetCore",
        "Cephalon.Observability.DependencyHealth.Core",
        "Cephalon.Observability.HttpDependencies")
    Update-GeneratedSaasHostProgram -HostProjectPath $hostProjectPath
    Assert-GeneratedSaasGovernanceFoundation -HostProjectPath $hostProjectPath

    Write-Host ""
    Write-Host "Seeding the generated local package feed..." -ForegroundColor Cyan
    & $publishPackagesScriptPath -Configuration $Configuration -OutputPath $generatedPackageFeedPath -SkipBuild -ProjectPaths $packageProjectPaths
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing to the generated local feed failed."
    }

    Write-Host ""
    Write-Host "Replaying generated-app doctor checks through the installed CLI..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("doctor", "--app-root", $generatedRoot)

    Write-Host ""
    Write-Host "Restoring the generated solution..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("restore", $solutionPath)

    Write-Host ""
    Write-Host "Building the generated solution..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("build", $solutionPath, "-c", $Configuration, "--no-restore")

    Write-Host ""
    Write-Host "Running the generated SaaS tenant governance host..." -ForegroundColor Cyan
    $hostProjectDirectory = Split-Path -Parent $hostProjectPath
    $hostAssemblyPath = Join-Path $hostProjectDirectory "bin\$Configuration\net10.0\$([System.IO.Path]::GetFileNameWithoutExtension($hostProjectPath)).dll"
    if (-not (Test-Path -LiteralPath $hostAssemblyPath -PathType Leaf)) {
        throw "Expected built generated host assembly at '$hostAssemblyPath'."
    }

    Set-ScopedEnvironmentVariable -Name "ASPNETCORE_URLS" -Value $null
    Set-ScopedEnvironmentVariable -Name "DOTNET_ENVIRONMENT" -Value "Development"
    Set-ScopedEnvironmentVariable -Name "Kestrel__Endpoints__Http__Url" -Value $HostUrl
    Set-ScopedEnvironmentVariable -Name "Kestrel__Endpoints__Http__Protocols" -Value "Http1"
    Set-ScopedEnvironmentVariable -Name "Kestrel__Endpoints__Grpc__Url" -Value $GrpcHostUrl
    Set-ScopedEnvironmentVariable -Name "Kestrel__Endpoints__Grpc__Protocols" -Value "Http2"
    Set-ScopedEnvironmentVariable -Name "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT" -Value "1"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__RefreshIntervalSeconds" -Value "1"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__Id" -Value "saas-governance-self"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__DisplayName" -Value "Generated SaaS governance engine self probe"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__Endpoint" -Value "$HostUrl/engine"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__Method" -Value "GET"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__Required" -Value "false"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__TimeoutSeconds" -Value "5"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__ExpectedStatusCodes__0" -Value "200"

    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @($hostAssemblyPath) `
        -WorkingDirectory $hostProjectDirectory `
        -RedirectStandardOutput $stdoutLogPath `
        -RedirectStandardError $stderrLogPath `
        -PassThru `
        -WindowStyle Hidden

    Wait-ForHttpSuccess -Uri "$HostUrl/health/ready" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/health/live" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/transports" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/diagnostics" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/audit-stores" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/technology-surfaces/multi-tenancy" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/scalar" -TimeoutSeconds $TimeoutSeconds -Process $process

    $transports = Invoke-HttpJson -Uri "$HostUrl/engine/transports"
    foreach ($token in @("rest-api", "json-rpc", "grpc")) {
        if (-not $transports.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected /engine/transports to contain '$token'."
        }
    }

    $snapshot = Invoke-HttpJson -Uri "$HostUrl/engine/snapshot"
    foreach ($token in @("modular-monolith", "multi-tenancy", "tenant-invitations", "tenant-administration", "audit")) {
        if (-not $snapshot.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected /engine/snapshot to contain '$token'."
        }
    }

    $restStatus = Invoke-HttpJson -Uri "$HostUrl/api/v1/platform/status"
    foreach ($token in @("platform", "Generated behavior-backed REST module is running")) {
        if (-not $restStatus.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected generated REST status response to contain '$token'."
        }
    }

    $rpcBody = [pscustomobject]([ordered]@{
        jsonRpc = "2.0"
        method = "platform.status.get"
        params = [pscustomobject]([ordered]@{
            name = "Codex"
        })
        id = "saas-jsonrpc"
    })
    $rpcStatus = Invoke-HttpJson -Uri "$HostUrl/json-rpc/platform" -Method "POST" -Body $rpcBody
    foreach ($token in @("saas-jsonrpc", "Hello, Codex", "platform", "json-rpc", "grpc")) {
        if (-not $rpcStatus.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected generated JSON-RPC response to contain '$token'."
        }
    }

    Write-Host ""
    Write-Host "Creating and running the gRPC client probe..." -ForegroundColor Cyan
    $grpcProbe = Write-GrpcProbeProject -PackageFeedPath $generatedPackageFeedPath
    Invoke-DotNet -WorkingDirectory $grpcProbe.Root -Arguments @("restore", $grpcProbe.Project)
    Invoke-DotNet -WorkingDirectory $grpcProbe.Root -Arguments @("build", $grpcProbe.Project, "-c", $Configuration, "--no-restore")

    $grpcProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $grpcProbe.Project, "-c", $Configuration, "--no-build", "--", $GrpcHostUrl) `
        -WorkingDirectory $grpcProbe.Root `
        -RedirectStandardOutput $grpcProbeStdoutLogPath `
        -RedirectStandardError $grpcProbeStderrLogPath `
        -PassThru `
        -Wait `
        -WindowStyle Hidden

    if ($grpcProcess.ExitCode -ne 0) {
        throw "gRPC probe failed with exit code $($grpcProcess.ExitCode)."
    }

    $grpcProbeOutput = Get-Content -LiteralPath $grpcProbeStdoutLogPath -Raw
    foreach ($token in @("grpc-message=Hello, Codex", "Platform", "grpc-traits=", "grpc")) {
        if (-not $grpcProbeOutput.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected gRPC probe output to contain '$token'."
        }
    }

    $tenantAdministration = Invoke-HttpJson -Uri "$HostUrl/engine/tenant-administration/commands" -Method "POST" -Body ([pscustomobject]([ordered]@{
        command = "issue-invitation"
        tenantId = "tenant-saas-smoke"
        invitationId = "invite-saas-smoke"
        inviteeId = "user-saas-smoke"
        inviteeKind = "user"
        displayName = "SaaS Smoke Invitation"
        roles = @("member")
        actor = "tenant-owner"
        reason = "Adoption smoke bootstrap"
        correlationId = "saas-governance-smoke"
        metadata = [pscustomobject]([ordered]@{
            source = "validate-saas-tenant-governance-audit-adoption"
        })
    }))
    foreach ($token in @("applied", "issue-invitation", "tenant-saas-smoke", "invite-saas-smoke")) {
        if (-not $tenantAdministration.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected tenant administration response to contain '$token'."
        }
    }

    $invitationDispatch = Invoke-HttpJson -Uri "$HostUrl/engine/tenant-invitations/delivery-dispatches" -Method "POST" -Body ([pscustomobject]([ordered]@{
        tenantId = "tenant-saas-smoke"
        invitationId = "invite-saas-smoke"
        channel = "email"
        senderId = "smoke-email"
        actor = "tenant-owner"
        correlationId = "saas-governance-smoke"
        metadata = [pscustomobject]([ordered]@{
            dispatchReason = "welcome"
        })
    }))
    foreach ($token in @("dispatched", "provider-message-saas-smoke", "smoke-email")) {
        if (-not $invitationDispatch.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected invitation dispatch response to contain '$token'."
        }
    }

    $deliveryStatus = Invoke-HttpJson -Uri "$HostUrl/engine/tenant-invitations/delivery-status" -Method "POST" -Body ([pscustomobject]([ordered]@{
        tenantId = "tenant-saas-smoke"
        invitationId = "invite-saas-smoke"
        status = "delivered"
        providerMessageId = "provider-message-saas-smoke"
        senderId = "smoke-email"
        channel = "email"
        reason = "Receiver accepted the invitation."
        actor = "notification-provider"
        correlationId = "saas-governance-smoke"
        metadata = [pscustomobject]([ordered]@{
            providerStatusCode = "250"
        })
    }))
    foreach ($token in @("reconciled", "delivered", "provider-message-saas-smoke")) {
        if (-not $deliveryStatus.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected delivery status response to contain '$token'."
        }
    }

    $deliveryObservations = Invoke-HttpJson -Uri "$HostUrl/engine/tenant-invitations/delivery-status/observations?tenantId=tenant-saas-smoke&providerMessageId=provider-message-saas-smoke&limit=25"
    foreach ($token in @("tenant-saas-smoke", "invite-saas-smoke", "delivered", "summaryCount", "provider-message-saas-smoke")) {
        if (-not $deliveryObservations.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected delivery observation response to contain '$token'."
        }
    }

    $auditProof = Invoke-HttpJson -Uri "$HostUrl/api/tenants/tenant-saas-smoke/audit-proof" -Method "POST" -Body ([pscustomobject]@{})
    foreach ($token in @("tenant-governance", "adoption-smoke", "tenant-saas-smoke", "Succeeded")) {
        if (-not $auditProof.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected audit proof response to contain '$token'."
        }
    }

    $auditStores = Invoke-HttpJson -Uri "$HostUrl/engine/audit-stores"
    foreach ($token in @("memory", "volatile-buffer", "audit-default")) {
        if (-not $auditStores.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected audit store surface to contain '$token'."
        }
    }

    $multiTenancySurface = Invoke-HttpJson -Uri "$HostUrl/engine/technology-surfaces/multi-tenancy"
    foreach ($token in @("tenant-invitations", "tenant-administration", "tenant-invitation-delivery-http-endpoints", "tenant-invitation-delivery-status-http-endpoints")) {
        if (-not $multiTenancySurface.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected multi-tenancy runtime surface to contain '$token'."
        }
    }

    $dependencies = Wait-ForDependencyHealthReport `
        -Uri "$HostUrl/engine/dependencies" `
        -DependencyId "saas-governance-self" `
        -Source "Cephalon.Observability.HttpDependencies" `
        -TimeoutSeconds $TimeoutSeconds `
        -Process $process

    Write-SaasTenantGovernanceAdoptionExecutionReport -Status "passed"
    Write-Host ""
    Write-Host "SaaS tenant governance audit adoption validation completed successfully." -ForegroundColor Green
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan
    Write-Host "HTTP host URL: $HostUrl" -ForegroundColor Cyan
    Write-Host "gRPC host URL: $GrpcHostUrl" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "SaaS tenant governance audit adoption validation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-RecentLogs -Path $stdoutLogPath -Label "Generated host stdout"
    Write-RecentLogs -Path $stderrLogPath -Label "Generated host stderr"
    Write-RecentLogs -Path $grpcProbeStdoutLogPath -Label "gRPC probe stdout"
    Write-RecentLogs -Path $grpcProbeStderrLogPath -Label "gRPC probe stderr"
    Write-SaasTenantGovernanceAdoptionExecutionReport -Status "failed" -ErrorMessage $_.Exception.Message
    throw
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(5000) | Out-Null
    }

    Restore-ScopedEnvironmentVariables

    if ($null -ne $previousNuGetPackages) {
        $env:NUGET_PACKAGES = $previousNuGetPackages
    }
    else {
        Remove-Item -LiteralPath "Env:\NUGET_PACKAGES" -ErrorAction SilentlyContinue
    }

    if ($restoreRepoPackageAssets) {
        & dotnet restore (Join-Path $repoRoot "CephalonEngine.slnx") --locked-mode | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to restore repository assets after the adoption smoke."
        }
    }

    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
