using Cephalon.Engine.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class AspNetCoreStranglerFigCutoverOptions
{
    private const string RedirectMode = "redirect";
    private const string ProxyMode = "proxy";

    public static AspNetCoreStranglerFigCutoverOptions Disabled { get; } = new();

    public AspNetCoreStranglerFigCutoverOptions(
        bool enabled = false,
        string absoluteEndpointMode = RedirectMode,
        int redirectStatusCode = StatusCodes.Status307TemporaryRedirect)
    {
        Enabled = enabled;
        AbsoluteEndpointMode = NormalizeAbsoluteEndpointMode(absoluteEndpointMode);
        RedirectStatusCode = NormalizeRedirectStatusCode(redirectStatusCode);
    }

    public bool Enabled { get; }

    public string AbsoluteEndpointMode { get; }

    public int RedirectStatusCode { get; }

    public bool UsesProxyForAbsoluteEndpoints =>
        string.Equals(AbsoluteEndpointMode, ProxyMode, StringComparison.OrdinalIgnoreCase);

    public static AspNetCoreStranglerFigCutoverOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Migration")
            .GetSection("StranglerFig")
            .GetSection("AspNetCore");

        if (!section.Exists())
        {
            return Disabled;
        }

        return new AspNetCoreStranglerFigCutoverOptions(
            enabled: ParseOptionalBoolean(section["Enabled"]) ?? false,
            absoluteEndpointMode: section["AbsoluteEndpointMode"] ?? RedirectMode,
            redirectStatusCode: ParseOptionalInt(
                    section["RedirectStatusCode"],
                    "Migration:StranglerFig:AspNetCore:RedirectStatusCode")
                ?? StatusCodes.Status307TemporaryRedirect);
    }

    private static string NormalizeAbsoluteEndpointMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RedirectMode;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            RedirectMode => RedirectMode,
            ProxyMode => ProxyMode,
            _ => throw new InvalidOperationException(
                $"ASP.NET Core strangler-fig absolute endpoint mode '{value}' is not supported. Expected 'redirect' or 'proxy'.")
        };
    }

    private static int NormalizeRedirectStatusCode(int value)
    {
        return value switch
        {
            StatusCodes.Status307TemporaryRedirect => value,
            StatusCodes.Status308PermanentRedirect => value,
            _ => throw new InvalidOperationException(
                $"ASP.NET Core strangler-fig redirect status code '{value}' is not supported. Expected 307 or 308.")
        };
    }

    private static bool? ParseOptionalBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (bool.TryParse(value.Trim(), out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"ASP.NET Core strangler-fig enabled flag '{value}' is not supported. Expected 'true' or 'false'.");
    }

    private static int? ParseOptionalInt(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value.Trim(), out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"ASP.NET Core strangler-fig integer value '{value}' is not supported for '{parameterName}'.");
    }
}
