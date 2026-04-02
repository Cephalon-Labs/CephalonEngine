using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.RedisDependencies.Hosting;
using Cephalon.Tests.Support;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class RedisDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonRedisDependencyHealthReportsHealthyPing()
    {
        await using var server = new FakeRedisServer();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Id"] = "redis-cache";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:DisplayName"] = "Redis Cache";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Host"] = "127.0.0.1";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Port"] = server.Port.ToString(CultureInfo.InvariantCulture);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Required"] = "false";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });
        builder.Services.AddCephalonRedisDependencyHealth(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());
        Assert.Equal("redis-cache", dependency.Id);
        Assert.Equal("Redis Cache", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonRedisDependencyHealthSupportsAuthenticatedPing()
    {
        await using var server = new FakeRedisServer(password: "secret-password");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Id"] = "secured-redis";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Host"] = "127.0.0.1";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Port"] = server.Port.ToString(CultureInfo.InvariantCulture);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Password"] = "secret-password";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Database"] = "2";
        builder.AddCephalon();
        builder.Services.AddCephalonRedisDependencyHealth(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());
        Assert.Equal(HealthState.Healthy, dependency.State);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonRedisDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var port = ReservePort();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Id"] = "required-redis";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Host"] = "127.0.0.1";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Port"] = port.ToString(CultureInfo.InvariantCulture);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Redis:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonRedisDependencyHealth(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.True(
            dependency.Description.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            dependency.Description.Contains("timed out", StringComparison.OrdinalIgnoreCase),
            $"Expected a Redis failure description but received '{dependency.Description}'.");

        await host.StopAsync();
    }

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private sealed class FakeRedisServer : IAsyncDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly string? password;
        private readonly Task acceptLoop;

        public FakeRedisServer(string? password = null)
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            this.password = password;
            acceptLoop = Task.Run(AcceptLoopAsync);
        }

        public int Port { get; }

        public async ValueTask DisposeAsync()
        {
            cancellationSource.Cancel();
            listener.Stop();

            try
            {
                await acceptLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }

            cancellationSource.Dispose();
        }

        private async Task AcceptLoopAsync()
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, cancellationSource.Token), CancellationToken.None);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var ownedClient = client;
            await using var stream = ownedClient.GetStream();

            var authenticated = string.IsNullOrWhiteSpace(password);

            while (!cancellationToken.IsCancellationRequested)
            {
                string[] command;
                try
                {
                    command = await ReadCommandAsync(stream, cancellationToken);
                }
                catch (IOException)
                {
                    break;
                }

                if (command.Length == 0)
                {
                    break;
                }

                var verb = command[0].ToUpperInvariant();
                switch (verb)
                {
                    case "AUTH":
                        var suppliedPassword = command[^1];
                        if (password is not null && suppliedPassword == password)
                        {
                            authenticated = true;
                            await WriteSimpleStringAsync(stream, "OK", cancellationToken);
                        }
                        else
                        {
                            await WriteErrorAsync(stream, "ERR invalid password", cancellationToken);
                        }

                        break;
                    case "SELECT":
                        if (!authenticated)
                        {
                            await WriteErrorAsync(stream, "NOAUTH Authentication required.", cancellationToken);
                            break;
                        }

                        await WriteSimpleStringAsync(stream, "OK", cancellationToken);
                        break;
                    case "PING":
                        if (!authenticated)
                        {
                            await WriteErrorAsync(stream, "NOAUTH Authentication required.", cancellationToken);
                            break;
                        }

                        await WriteSimpleStringAsync(stream, "PONG", cancellationToken);
                        break;
                    default:
                        await WriteErrorAsync(stream, $"ERR unsupported command '{verb}'", cancellationToken);
                        break;
                }
            }
        }

        private static async Task<string[]> ReadCommandAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var header = await ReadLineAsync(stream, cancellationToken);
            if (string.IsNullOrWhiteSpace(header))
            {
                return [];
            }

            if (!header.StartsWith('*') || !int.TryParse(header[1..], out var count) || count < 0)
            {
                throw new IOException($"Unexpected Redis array header '{header}'.");
            }

            var parts = new string[count];
            for (var index = 0; index < count; index++)
            {
                var bulkHeader = await ReadLineAsync(stream, cancellationToken);
                if (!bulkHeader.StartsWith('$') || !int.TryParse(bulkHeader[1..], out var length) || length < 0)
                {
                    throw new IOException($"Unexpected Redis bulk header '{bulkHeader}'.");
                }

                parts[index] = await ReadFixedStringAsync(stream, length, cancellationToken);
                await ReadLineAsync(stream, cancellationToken);
            }

            return parts;
        }

        private static async Task<string> ReadFixedStringAsync(NetworkStream stream, int length, CancellationToken cancellationToken)
        {
            var buffer = new byte[length];
            var offset = 0;
            while (offset < length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
                if (read == 0)
                {
                    throw new IOException("Unexpected end of stream while reading Redis bulk string.");
                }

                offset += read;
            }

            return Encoding.UTF8.GetString(buffer);
        }

        private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            var buffer = new byte[1];
            var sawCarriageReturn = false;

            while (true)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    throw new IOException("Unexpected end of stream while reading Redis line.");
                }

                var current = (char)buffer[0];
                if (sawCarriageReturn)
                {
                    if (current == '\n')
                    {
                        break;
                    }

                    builder.Append('\r');
                    sawCarriageReturn = false;
                }

                if (current == '\r')
                {
                    sawCarriageReturn = true;
                    continue;
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private static Task WriteSimpleStringAsync(NetworkStream stream, string value, CancellationToken cancellationToken) =>
            WriteFrameAsync(stream, $"+{value}\r\n", cancellationToken);

        private static Task WriteErrorAsync(NetworkStream stream, string value, CancellationToken cancellationToken) =>
            WriteFrameAsync(stream, $"-{value}\r\n", cancellationToken);

        private static async Task WriteFrameAsync(NetworkStream stream, string value, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
    }
}
