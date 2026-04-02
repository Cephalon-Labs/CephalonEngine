using System.Net;
using System.Net.Sockets;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.MqttDependencies.Configuration;
using Cephalon.Observability.MqttDependencies.Hosting;
using Cephalon.Observability.MqttDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class MqttDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonMqttDependencyHealthReportsHealthyBrokerHandshake()
    {
        var probeClient = new FakeMqttDependencyProbeClient(dependency =>
            $"MQTT endpoint '{dependency.Host}:{dependency.Port}' accepted CONNECT and responded to PINGREQ/PINGRESP.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:Id"] = "edge-broker";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:DisplayName"] = "Edge MQTT";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:Host"] = "mqtt.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:Port"] = "1884";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:ClientId"] = "cephalon-runtime";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:KeepAliveSeconds"] = "45";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:TimeoutSeconds"] = "7";
        builder.AddCephalon();
        builder.Services.AddCephalonMqttDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IMqttDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("edge-broker", dependency.Id);
        Assert.Equal("Edge MQTT", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.MqttDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("mqtt.internal.example", captured.Host);
        Assert.Equal(1884, captured.Port);
        Assert.Equal("cephalon-runtime", captured.ClientId);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal(45, captured.KeepAliveSeconds);
        Assert.Equal(7, captured.TimeoutSeconds);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonMqttDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeMqttDependencyProbeClient(_ => throw new InvalidOperationException("not authorized"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:Id"] = "required-mqtt";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:Host"] = "mqtt.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Mqtt:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonMqttDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IMqttDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("not authorized", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("mqtt.internal.example", captured.Host);

        await host.StopAsync();
    }

    [Fact]
    public void CreateConnectPacketBuildsUsernamePasswordPayload()
    {
        var dependency = new MqttDependencyDefinition
        {
            Host = "mqtt.internal.example",
            Port = 1883,
            ClientId = "cephalon-runtime",
            Username = "cephalon",
            Password = "secret",
            KeepAliveSeconds = 45
        };

        var packet = MqttDependencyProbeClient.CreateConnectPacket(dependency);
        var parsed = ParsePacket(packet);
        var index = 0;

        Assert.Equal(1, parsed.PacketType);
        Assert.Equal(0, parsed.Flags);
        Assert.Equal("MQTT", ReadUtf8String(parsed.Payload, ref index));
        Assert.Equal(4, parsed.Payload[index++]);

        var connectFlags = parsed.Payload[index++];
        Assert.True((connectFlags & 0x80) == 0x80);
        Assert.True((connectFlags & 0x40) == 0x40);
        Assert.True((connectFlags & 0x02) == 0x02);

        var keepAlive = (parsed.Payload[index++] << 8) | parsed.Payload[index++];
        Assert.Equal(45, keepAlive);
        Assert.Equal("cephalon-runtime", ReadUtf8String(parsed.Payload, ref index));
        Assert.Equal("cephalon", ReadUtf8String(parsed.Payload, ref index));
        Assert.Equal("secret", ReadUtf8String(parsed.Payload, ref index));
        Assert.Equal(parsed.Payload.Length, index);
    }

    [Fact]
    public void ParseConnAckReadsSessionState()
    {
        var connAck = MqttDependencyProbeClient.ParseConnAck(new MqttPacket(
            PacketType: 2,
            Flags: 0,
            Payload: [0x01, 0x00]));

        Assert.True(connAck.SessionPresent);
        Assert.Equal(0, connAck.ReturnCode);
    }

    [Fact]
    public async Task ProbeAsyncCompletesPlaintextConnectAndPingHandshake()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var accepted = RunPlaintextMqttServerAsync(listener, expectedClientId: "cephalon-runtime");
        var probeClient = new MqttDependencyProbeClient();

        var description = await probeClient.ProbeAsync(
            new MqttDependencyDefinition
            {
                Host = IPAddress.Loopback.ToString(),
                Port = port,
                ClientId = "cephalon-runtime",
                Username = "cephalon",
                Password = "secret",
                KeepAliveSeconds = 45
            },
            CancellationToken.None);

        await accepted;

        Assert.Contains("accepted CONNECT", description, StringComparison.Ordinal);
        Assert.Contains("PINGREQ/PINGRESP", description, StringComparison.Ordinal);
    }

    private static async Task RunPlaintextMqttServerAsync(TcpListener listener, string expectedClientId)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();

        var packet = await ReadPacketAsync(stream);
        var index = 0;

        Assert.Equal(1, packet.PacketType);
        Assert.Equal(0, packet.Flags);
        Assert.Equal("MQTT", ReadUtf8String(packet.Payload, ref index));
        Assert.Equal(4, packet.Payload[index++]);

        var connectFlags = packet.Payload[index++];
        Assert.True((connectFlags & 0x82) == 0x82);
        Assert.True((connectFlags & 0x40) == 0x40);

        var keepAlive = (packet.Payload[index++] << 8) | packet.Payload[index++];
        Assert.Equal(45, keepAlive);
        Assert.Equal(expectedClientId, ReadUtf8String(packet.Payload, ref index));
        Assert.Equal("cephalon", ReadUtf8String(packet.Payload, ref index));
        Assert.Equal("secret", ReadUtf8String(packet.Payload, ref index));

        await stream.WriteAsync(new byte[] { 0x20, 0x02, 0x00, 0x00 });

        var pingRequest = await ReadPacketAsync(stream);
        Assert.Equal(12, pingRequest.PacketType);
        Assert.Empty(pingRequest.Payload);

        await stream.WriteAsync(new byte[] { 0xD0, 0x00 });
        listener.Stop();
    }

    private static MqttPacket ParsePacket(byte[] packetBytes)
    {
        var index = 0;
        var header = packetBytes[index++];
        var remainingLength = ReadRemainingLength(packetBytes, ref index);
        var payload = packetBytes[index..];

        Assert.Equal(remainingLength, payload.Length);

        return new MqttPacket(
            PacketType: (byte)(header >> 4),
            Flags: (byte)(header & 0x0F),
            Payload: payload);
    }

    private static async Task<MqttPacket> ReadPacketAsync(Stream stream)
    {
        var fixedHeader = new byte[1];
        await ReadExactAsync(stream, fixedHeader);

        var remainingLength = await ReadRemainingLengthAsync(stream);
        var payload = new byte[remainingLength];
        await ReadExactAsync(stream, payload);

        return new MqttPacket(
            PacketType: (byte)(fixedHeader[0] >> 4),
            Flags: (byte)(fixedHeader[0] & 0x0F),
            Payload: payload);
    }

    private static async Task<int> ReadRemainingLengthAsync(Stream stream)
    {
        var multiplier = 1;
        var value = 0;

        while (true)
        {
            var encoded = new byte[1];
            await ReadExactAsync(stream, encoded);
            value += (encoded[0] & 0x7F) * multiplier;
            if ((encoded[0] & 0x80) == 0)
            {
                return value;
            }

            multiplier *= 128;
        }
    }

    private static int ReadRemainingLength(byte[] packetBytes, ref int index)
    {
        var multiplier = 1;
        var value = 0;

        while (true)
        {
            var encoded = packetBytes[index++];
            value += (encoded & 0x7F) * multiplier;
            if ((encoded & 0x80) == 0)
            {
                return value;
            }

            multiplier *= 128;
        }
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset));
            if (bytesRead == 0)
            {
                throw new IOException("The socket closed before the packet was fully read.");
            }

            offset += bytesRead;
        }
    }

    private static string ReadUtf8String(byte[] payload, ref int index)
    {
        var length = (payload[index++] << 8) | payload[index++];
        var value = Encoding.UTF8.GetString(payload, index, length);
        index += length;
        return value;
    }

    private sealed class FakeMqttDependencyProbeClient(Func<MqttDependencyDefinition, string> onProbe) : IMqttDependencyProbeClient
    {
        public List<MqttDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(MqttDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
