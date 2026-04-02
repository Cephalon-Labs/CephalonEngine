using System.IO;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.MemcachedDependencies.Hosting;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class MemcachedDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonMemcachedDependencyHealthReportsHealthyVersionProbe()
    {
        await using var server = new FakeMemcachedServer();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:Id"] = "shared-cache";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:DisplayName"] = "Shared Cache";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:Host"] = "127.0.0.1";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:Port"] = server.Port.ToString(CultureInfo.InvariantCulture);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:Required"] = "false";
        builder.AddCephalon();
        builder.Services.AddCephalonMemcachedDependencyHealth(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());
        Assert.Equal("shared-cache", dependency.Id);
        Assert.Equal("Shared Cache", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Contains("1.6.31", dependency.Description, StringComparison.Ordinal);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonMemcachedDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var port = ReservePort();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:Id"] = "required-cache";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:Host"] = "127.0.0.1";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:Port"] = port.ToString(CultureInfo.InvariantCulture);
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Memcached:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonMemcachedDependencyHealth(builder.Configuration);

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
            $"Expected a Memcached failure description but received '{dependency.Description}'.");

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

    private sealed class FakeMemcachedServer : IAsyncDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly Task acceptLoop;

        public FakeMemcachedServer()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
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

        private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var ownedClient = client;
            await using var stream = ownedClient.GetStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                string line;
                try
                {
                    line = await ReadLineAsync(stream, cancellationToken);
                }
                catch (IOException)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                if (string.Equals(line, "version", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteLineAsync(stream, "VERSION 1.6.31", cancellationToken);
                }
                else
                {
                    await WriteLineAsync(stream, "ERROR", cancellationToken);
                }
            }
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
                    throw new IOException("Unexpected end of stream while reading Memcached command.");
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

        private static async Task WriteLineAsync(NetworkStream stream, string value, CancellationToken cancellationToken)
        {
            var bytes = Encoding.ASCII.GetBytes($"{value}\r\n");
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
    }
}
