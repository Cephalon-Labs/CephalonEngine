using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Cephalon.Observability.DigitalOcean.Hosting;

internal sealed class DigitalOceanTrustedCaHttpClientHandler : HttpClientHandler
{
    private readonly X509Certificate2Collection trustedCertificates;

    public DigitalOceanTrustedCaHttpClientHandler(string trustedCaCertificatePath)
    {
        trustedCertificates = LoadTrustedCertificates(trustedCaCertificatePath);
        ServerCertificateCustomValidationCallback = ValidateServerCertificate;
    }

    public static HttpClient CreateHttpClient(string trustedCaCertificatePath)
    {
        return new HttpClient(new DigitalOceanTrustedCaHttpClientHandler(trustedCaCertificatePath), disposeHandler: true);
    }

    private bool ValidateServerCertificate(
        HttpRequestMessage _,
        X509Certificate2? certificate,
        X509Chain? __,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (certificate is null || trustedCertificates.Count == 0)
        {
            return false;
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

        foreach (var trustedCertificate in trustedCertificates)
        {
            chain.ChainPolicy.ExtraStore.Add(trustedCertificate);
            chain.ChainPolicy.CustomTrustStore.Add(trustedCertificate);
        }

        return chain.Build(certificate);
    }

    private static X509Certificate2Collection LoadTrustedCertificates(string trustedCaCertificatePath)
    {
        if (string.IsNullOrWhiteSpace(trustedCaCertificatePath))
        {
            throw new InvalidOperationException(
                "Cephalon DigitalOcean observability integration requires Engine:Observability:Telemetry:DigitalOcean:TrustedCaCertificatePath to be configured when a custom CA bundle is requested.");
        }

        var normalizedPath = trustedCaCertificatePath.Trim();
        if (!File.Exists(normalizedPath))
        {
            throw new InvalidOperationException(
                $"DigitalOcean trusted CA bundle '{normalizedPath}' was not found.");
        }

        var certificates = new X509Certificate2Collection();
        var fileBytes = File.ReadAllBytes(normalizedPath);
        var fileText = Encoding.ASCII.GetString(fileBytes);
        var pemMatches = Regex.Matches(
            fileText,
            "-----BEGIN CERTIFICATE-----.*?-----END CERTIFICATE-----",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        if (pemMatches.Count > 0)
        {
            foreach (Match pemMatch in pemMatches)
            {
                certificates.Add(X509CertificateLoader.LoadCertificate(Encoding.UTF8.GetBytes(pemMatch.Value)));
            }
        }
        else
        {
            try
            {
                certificates.Add(X509CertificateLoader.LoadCertificateFromFile(normalizedPath));
            }
            catch (CryptographicException exception)
            {
                throw new InvalidOperationException(
                    $"DigitalOcean trusted CA bundle '{normalizedPath}' could not be parsed as a PEM or DER certificate file.",
                    exception);
            }
        }

        if (certificates.Count == 0)
        {
            throw new InvalidOperationException(
                $"DigitalOcean trusted CA bundle '{normalizedPath}' did not contain any certificates.");
        }

        return certificates;
    }
}
