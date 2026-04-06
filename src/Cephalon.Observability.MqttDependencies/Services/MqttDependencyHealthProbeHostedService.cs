using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.MqttDependencies.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.MqttDependencies.Services;

internal sealed class MqttDependencyHealthProbeHostedService(
    MqttDependencyHealthOptions options,
    IMqttDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<MqttDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<MqttDependencyHealthOptions, MqttDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.MqttDependencies";
    protected override string DefaultDependencyId => "mqtt-dependency";
    protected override string ProviderLabel => "MQTT";

    protected override string? ValidateDependency(MqttDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Host)
            ? "MQTT host is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(MqttDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        MqttDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        MqttDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
}

internal interface IMqttDependencyProbeClient
{
    ValueTask<string> ProbeAsync(MqttDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class MqttDependencyProbeClient : IMqttDependencyProbeClient
{
    private const string DefaultClientId = "cephalon-dependency-health";

    public async ValueTask<string> ProbeAsync(MqttDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var host = dependency.Host.Trim();
        var port = dependency.Port > 0 ? dependency.Port : 1883;

        using var client = new TcpClient
        {
            NoDelay = true
        };

        await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        Stream stream = client.GetStream();
        if (dependency.UseTls)
        {
            var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
            await sslStream
                .AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = ResolveTlsServerName(dependency),
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            stream = sslStream;
        }

        await WritePacketAsync(stream, CreateConnectPacket(dependency), cancellationToken).ConfigureAwait(false);
        var connAck = ParseConnAck(await ReadPacketAsync(stream, cancellationToken).ConfigureAwait(false));

        await WritePacketAsync(stream, [0xC0, 0x00], cancellationToken).ConfigureAwait(false);
        EnsurePingResponse(await ReadPacketAsync(stream, cancellationToken).ConfigureAwait(false));

        var target = $"{host}:{port}";
        var description = $"MQTT endpoint '{target}' accepted CONNECT and responded to PINGREQ/PINGRESP";
        if (connAck.SessionPresent)
        {
            description += " with an existing session";
        }

        description += ".";
        return description;
    }

    internal static byte[] CreateConnectPacket(MqttDependencyDefinition dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        if (!string.IsNullOrWhiteSpace(dependency.Password) && string.IsNullOrWhiteSpace(dependency.Username))
        {
            throw new InvalidOperationException("MQTT username/password authentication requires Username when Password is configured.");
        }

        var clientId = string.IsNullOrWhiteSpace(dependency.ClientId)
            ? DefaultClientId
            : dependency.ClientId.Trim();
        var keepAliveSeconds = Math.Clamp(dependency.KeepAliveSeconds, 1, ushort.MaxValue);

        using var variableHeader = new MemoryStream();
        WriteUtf8String(variableHeader, "MQTT");
        variableHeader.WriteByte(0x04);
        variableHeader.WriteByte(CreateConnectFlags(dependency));
        WriteUInt16(variableHeader, (ushort)keepAliveSeconds);

        using var payload = new MemoryStream();
        WriteUtf8String(payload, clientId);
        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            WriteUtf8String(payload, dependency.Username.Trim());
            WriteUtf8String(payload, dependency.Password ?? string.Empty);
        }

        using var packet = new MemoryStream();
        packet.WriteByte(0x10);
        WriteRemainingLength(packet, checked((int)(variableHeader.Length + payload.Length)));
        variableHeader.WriteTo(packet);
        payload.WriteTo(packet);
        return packet.ToArray();
    }

    internal static MqttConnAck ParseConnAck(MqttPacket packet)
    {
        if (packet.PacketType != 2)
        {
            throw new InvalidOperationException($"Expected MQTT CONNACK but received packet type '{packet.PacketType}'.");
        }

        if (packet.Flags != 0)
        {
            throw new InvalidOperationException($"Expected MQTT CONNACK flags of 0 but received '{packet.Flags}'.");
        }

        if (packet.Payload.Length != 2)
        {
            throw new InvalidOperationException("MQTT CONNACK packets must carry exactly two payload bytes.");
        }

        var sessionPresent = (packet.Payload[0] & 0x01) == 0x01;
        var returnCode = packet.Payload[1];
        if (returnCode != 0)
        {
            throw new InvalidOperationException($"MQTT broker rejected CONNECT with return code '{returnCode}': {GetReturnCodeDescription(returnCode)}");
        }

        return new MqttConnAck(sessionPresent, returnCode);
    }

    internal static void EnsurePingResponse(MqttPacket packet)
    {
        if (packet.PacketType != 13 || packet.Flags != 0 || packet.Payload.Length != 0)
        {
            throw new InvalidOperationException("MQTT broker did not return a valid PINGRESP packet.");
        }
    }

    private static byte CreateConnectFlags(MqttDependencyDefinition dependency)
    {
        byte flags = 0x02;

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            flags |= 0x80;

            if (dependency.Password is not null)
            {
                flags |= 0x40;
            }
        }

        return flags;
    }

    private static async Task<MqttPacket> ReadPacketAsync(Stream stream, CancellationToken cancellationToken)
    {
        var fixedHeader = new byte[1];
        await ReadExactAsync(stream, fixedHeader, cancellationToken).ConfigureAwait(false);

        var remainingLength = await ReadRemainingLengthAsync(stream, cancellationToken).ConfigureAwait(false);
        var payload = new byte[remainingLength];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);

        return new MqttPacket(
            PacketType: (byte)(fixedHeader[0] >> 4),
            Flags: (byte)(fixedHeader[0] & 0x0F),
            Payload: payload);
    }

    private static async Task<int> ReadRemainingLengthAsync(Stream stream, CancellationToken cancellationToken)
    {
        var multiplier = 1;
        var value = 0;

        while (true)
        {
            var encodedByte = new byte[1];
            await ReadExactAsync(stream, encodedByte, cancellationToken).ConfigureAwait(false);

            value += (encodedByte[0] & 0x7F) * multiplier;
            if ((encodedByte[0] & 0x80) == 0)
            {
                return value;
            }

            multiplier *= 128;
            if (multiplier > 128 * 128 * 128)
            {
                throw new InvalidOperationException("MQTT remaining-length field exceeded four bytes.");
            }
        }
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var bytesRead = await stream
                .ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken)
                .ConfigureAwait(false);

            if (bytesRead == 0)
            {
                throw new IOException("The MQTT connection closed before the probe completed.");
            }

            offset += bytesRead;
        }
    }

    private static Task WritePacketAsync(Stream stream, byte[] payload, CancellationToken cancellationToken) =>
        stream.WriteAsync(payload.AsMemory(), cancellationToken).AsTask();

    private static void WriteRemainingLength(Stream stream, int value)
    {
        do
        {
            var encodedByte = value % 128;
            value /= 128;
            if (value > 0)
            {
                encodedByte |= 0x80;
            }

            stream.WriteByte((byte)encodedByte);
        }
        while (value > 0);
    }

    private static void WriteUtf8String(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteUInt16(stream, checked((ushort)bytes.Length));
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)(value & 0xFF));
    }

    private static string ResolveTlsServerName(MqttDependencyDefinition dependency) =>
        string.IsNullOrWhiteSpace(dependency.TlsServerName)
            ? dependency.Host.Trim()
            : dependency.TlsServerName.Trim();

    private static string GetReturnCodeDescription(byte returnCode) =>
        returnCode switch
        {
            1 => "Unacceptable protocol version",
            2 => "Identifier rejected",
            3 => "Server unavailable",
            4 => "Bad user name or password",
            5 => "Not authorized",
            _ => "Unknown return code"
        };
}

internal sealed record MqttPacket(byte PacketType, byte Flags, byte[] Payload);

internal sealed record MqttConnAck(bool SessionPresent, byte ReturnCode);

internal static class MqttDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(MqttDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, MqttDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        MqttDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(MqttDependencyHealthDiagnosticsConventions.ProbeFailed.Id, MqttDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        MqttDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
