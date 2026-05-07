namespace Cephalon.Data.Services;

internal static class DataCdcRuntimeSurfaceMetadata
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "secret",
        "token",
        "apiKey",
        "apikey",
        "accessKey",
        "connectionString",
        "credential",
        "privateKey",
        "clientSecret",
        "sharedAccessSignature"
    ];

    public static Dictionary<string, string> CreateSanitized(
        IReadOnlyDictionary<string, string>? source = null)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddSanitized(metadata, source);
        return metadata;
    }

    public static void AddSanitized(
        IDictionary<string, string> metadata,
        IReadOnlyDictionary<string, string>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var (key, value) in source)
        {
            UpsertSanitized(metadata, key, value);
        }
    }

    public static void UpsertSanitized(
        IDictionary<string, string> metadata,
        string key,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalizedKey = key.Trim();
        metadata[normalizedKey] = IsSensitiveKey(normalizedKey)
            ? "redacted"
            : RedactUriCredentials(value.Trim());

        if (string.Equals(metadata[normalizedKey], "redacted", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(metadata[normalizedKey], value.Trim(), StringComparison.Ordinal))
        {
            metadata["secretProjection"] = "redacted";
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        return SensitiveKeyFragments.Any(fragment =>
            key.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string RedactUriCredentials(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return value;
        }

        if (string.IsNullOrWhiteSpace(uri.UserInfo) &&
            !HasSensitiveQueryKey(uri))
        {
            return value;
        }

        var host = uri.Host.Contains(':', StringComparison.Ordinal) &&
            !uri.Host.StartsWith('[')
                ? $"[{uri.Host}]"
                : uri.Host;
        var authority = uri.IsDefaultPort
            ? host
            : $"{host}:{uri.Port}";
        var path = uri.AbsolutePath == "/"
            ? string.Empty
            : uri.AbsolutePath;

        return $"{uri.Scheme}://{authority}{path}";
    }

    private static bool HasSensitiveQueryKey(Uri uri)
    {
        if (string.IsNullOrWhiteSpace(uri.Query))
        {
            return false;
        }

        return uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static pair =>
            {
                var separatorIndex = pair.IndexOf('=');
                return separatorIndex < 0 ? pair : pair[..separatorIndex];
            })
            .Any(IsSensitiveKey);
    }
}
