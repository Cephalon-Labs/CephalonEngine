using System.Net.Http;
using Cephalon.Observability.DigitalOcean.Configuration;
using OpenTelemetry.Resources;

namespace Cephalon.Observability.DigitalOcean.Hosting;

internal sealed class DigitalOceanDropletResourceDetector : IResourceDetector
{
    private static readonly Uri DefaultMetadataEndpoint = new("http://169.254.169.254/metadata/v1/", UriKind.Absolute);
    private readonly DigitalOceanTelemetryExportOptions options;

    public DigitalOceanDropletResourceDetector(DigitalOceanTelemetryExportOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Resource Detect()
    {
        if (!options.UseDropletMetadataDefaults)
        {
            return Resource.Empty;
        }

        try
        {
            var attributes = new List<KeyValuePair<string, object>>();

            using var cancellationTokenSource = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(ResolveMetadataTimeoutMilliseconds(options)));
            using var client = new HttpClient
            {
                BaseAddress = ResolveMetadataEndpoint(options),
                Timeout = Timeout.InfiniteTimeSpan
            };

            if (string.IsNullOrWhiteSpace(options.DropletId))
            {
                AddMetadataAttribute(client, "id", "host.id", attributes, cancellationTokenSource.Token);
            }

            if (string.IsNullOrWhiteSpace(options.Region))
            {
                AddMetadataAttribute(client, "region", "cloud.region", attributes, cancellationTokenSource.Token);
            }

            AddMetadataAttribute(client, "hostname", "host.name", attributes, cancellationTokenSource.Token);

            return attributes.Count == 0 ? Resource.Empty : new Resource(attributes);
        }
        catch
        {
            return Resource.Empty;
        }
    }

    internal static Uri ResolveMetadataEndpoint(DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.DropletMetadataEndpoint))
        {
            return DefaultMetadataEndpoint;
        }

        if (!Uri.TryCreate(options.DropletMetadataEndpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"DigitalOcean droplet metadata endpoint '{options.DropletMetadataEndpoint}' is not a valid absolute URI.");
        }

        return endpoint.AbsoluteUri.EndsWith('/')
            ? endpoint
            : new Uri($"{endpoint.AbsoluteUri}/", UriKind.Absolute);
    }

    internal static int ResolveMetadataTimeoutMilliseconds(DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.MetadataTimeoutMilliseconds is > 0
            ? options.MetadataTimeoutMilliseconds.Value
            : 1000;
    }

    private static void AddMetadataAttribute(
        HttpClient client,
        string relativePath,
        string attributeName,
        List<KeyValuePair<string, object>> attributes,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        using var response = client.Send(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        var value = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        attributes.Add(new KeyValuePair<string, object>(attributeName, value.Trim()));
    }
}
