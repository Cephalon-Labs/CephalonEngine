using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.NatsDependencies.Configuration;
using Cephalon.Observability.NatsDependencies.Hosting;
using Cephalon.Observability.NatsDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class NatsDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonNatsDependencyHealthReportsHealthyBrokerHandshake()
    {
        var probeClient = new FakeNatsDependencyProbeClient(dependency =>
            $"NATS endpoint '{dependency.Host}:{dependency.Port}' responded to CONNECT and PING/PONG on server 'ops-nats-1' running version '2.11.0'.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:Id"] = "events-nats";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:DisplayName"] = "Events NATS";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:Host"] = "nats.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:Port"] = "4223";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:Token"] = "operations-token";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:ClientName"] = "cephalon-runtime";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:TimeoutSeconds"] = "7";
        builder.AddCephalon();
        builder.Services.AddCephalonNatsDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<INatsDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("events-nats", dependency.Id);
        Assert.Equal("Events NATS", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.NatsDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("nats.internal.example", captured.Host);
        Assert.Equal(4223, captured.Port);
        Assert.Equal("operations-token", captured.Token);
        Assert.Equal("cephalon-runtime", captured.ClientName);
        Assert.Equal(7, captured.TimeoutSeconds);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonNatsDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeNatsDependencyProbeClient(_ => throw new InvalidOperationException("authorization violation"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:Id"] = "required-nats";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:Host"] = "nats.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Nats:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonNatsDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<INatsDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("authorization violation", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("nats.internal.example", captured.Host);

        await host.StopAsync();
    }

    [Fact]
    public void CreateConnectCommandBuildsTokenBasedPayload()
    {
        var dependency = new NatsDependencyDefinition
        {
            Host = "nats.internal.example",
            Port = 4222,
            UseTls = true,
            Token = "operations-token",
            ClientName = "cephalon-runtime"
        };

        var command = NatsDependencyProbeClient.CreateConnectCommand(dependency);

        Assert.StartsWith("CONNECT ", command, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(command["CONNECT ".Length..]);
        var root = document.RootElement;

        Assert.False(root.GetProperty("verbose").GetBoolean());
        Assert.False(root.GetProperty("pedantic").GetBoolean());
        Assert.True(root.GetProperty("tls_required").GetBoolean());
        Assert.Equal("cephalon-runtime", root.GetProperty("name").GetString());
        Assert.Equal("csharp", root.GetProperty("lang").GetString());
        Assert.Equal("operations-token", root.GetProperty("auth_token").GetString());
        Assert.Equal(1, root.GetProperty("protocol").GetInt32());
        Assert.False(root.GetProperty("echo").GetBoolean());
    }

    [Fact]
    public void ParseInfoLineReadsServerMetadata()
    {
        var info = NatsDependencyProbeClient.ParseInfoLine("INFO {\"server_name\":\"ops-nats-1\",\"version\":\"2.11.0\",\"host\":\"127.0.0.1\",\"port\":4222,\"cluster\":\"operations\",\"auth_required\":true,\"tls_required\":false}");

        Assert.Equal("ops-nats-1", info.ServerName);
        Assert.Equal("2.11.0", info.Version);
        Assert.Equal("127.0.0.1", info.Host);
        Assert.Equal(4222, info.Port);
        Assert.Equal("operations", info.Cluster);
        Assert.True(info.AuthRequired);
        Assert.False(info.TlsRequired);
    }

    [Fact]
    public async Task ProbeAsyncCompletesPlaintextPingPongHandshake()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var accepted = RunPlaintextNatsServerAsync(listener, expectedToken: "operations-token");
        var probeClient = new NatsDependencyProbeClient();

        var description = await probeClient.ProbeAsync(
            new NatsDependencyDefinition
            {
                Host = IPAddress.Loopback.ToString(),
                Port = port,
                Token = "operations-token",
                ClientName = "cephalon-runtime"
            },
            CancellationToken.None);

        await accepted;

        Assert.Contains("CONNECT and PING/PONG", description, StringComparison.Ordinal);
        Assert.Contains("ops-nats-1", description, StringComparison.Ordinal);
        Assert.Contains("2.11.0", description, StringComparison.Ordinal);
    }

    private static async Task RunPlaintextNatsServerAsync(TcpListener listener, string expectedToken)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();

        await WriteLineAsync(stream, "INFO {\"server_name\":\"ops-nats-1\",\"version\":\"2.11.0\",\"host\":\"127.0.0.1\",\"port\":4222,\"auth_required\":true,\"tls_required\":false}");

        var connectLine = await ReadLineAsync(stream);
        var pingLine = await ReadLineAsync(stream);

        Assert.Equal("PING", pingLine);
        Assert.StartsWith("CONNECT ", connectLine, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(connectLine["CONNECT ".Length..]);
        var root = document.RootElement;
        Assert.Equal("cephalon-runtime", root.GetProperty("name").GetString());
        Assert.Equal(expectedToken, root.GetProperty("auth_token").GetString());
        Assert.Equal(1, root.GetProperty("protocol").GetInt32());

        await WriteLineAsync(stream, "PONG");
        listener.Stop();
    }

    private static async Task<string> ReadLineAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        var singleByte = new byte[1];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(singleByte);
            if (bytesRead == 0)
            {
                throw new IOException("The socket closed before a line was read.");
            }

            if (singleByte[0] == (byte)'\n')
            {
                break;
            }

            if (singleByte[0] != (byte)'\r')
            {
                buffer.WriteByte(singleByte[0]);
            }
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static Task WriteLineAsync(Stream stream, string line)
    {
        var payload = Encoding.UTF8.GetBytes($"{line}\r\n");
        return stream.WriteAsync(payload).AsTask();
    }

    private sealed class FakeNatsDependencyProbeClient(Func<NatsDependencyDefinition, string> onProbe) : INatsDependencyProbeClient
    {
        public List<NatsDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(NatsDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
