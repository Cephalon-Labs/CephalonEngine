namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the stable probe-freshness runtime state published for one database role.
/// </summary>
public sealed class DatabaseRoleProbeDescriptor
{
    /// <summary>
    /// Creates a new database-role probe descriptor.
    /// </summary>
    /// <param name="cacheEnabled">Whether cached probe answers are enabled for the role.</param>
    /// <param name="freshnessSeconds">The configured or default freshness window in seconds.</param>
    /// <param name="freshnessOrigin">The source of the effective freshness window, such as <c>configured</c> or <c>default</c>.</param>
    /// <param name="source">The source of the current answer, such as <c>live</c> or <c>cache</c>.</param>
    /// <param name="freshUntilUtc">The UTC timestamp until which the current answer remains fresh, when known.</param>
    /// <param name="ageSeconds">The age in seconds of the current answer, when known.</param>
    public DatabaseRoleProbeDescriptor(
        bool cacheEnabled,
        int freshnessSeconds,
        string? freshnessOrigin = null,
        string? source = null,
        DateTimeOffset? freshUntilUtc = null,
        int? ageSeconds = null)
    {
        if (freshnessSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(freshnessSeconds), "Probe freshness seconds must be zero or greater.");
        }

        if (ageSeconds is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ageSeconds), "Probe age seconds must be zero or greater when supplied.");
        }

        CacheEnabled = cacheEnabled;
        FreshnessSeconds = freshnessSeconds;
        FreshnessOrigin = string.IsNullOrWhiteSpace(freshnessOrigin) ? null : freshnessOrigin.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        FreshUntilUtc = freshUntilUtc;
        AgeSeconds = ageSeconds;
    }

    /// <summary>
    /// Gets a value indicating whether cached probe answers are enabled for the role.
    /// </summary>
    public bool CacheEnabled { get; }

    /// <summary>
    /// Gets the configured or default freshness window in seconds.
    /// </summary>
    public int FreshnessSeconds { get; }

    /// <summary>
    /// Gets the source of the effective freshness window, when known.
    /// </summary>
    public string? FreshnessOrigin { get; }

    /// <summary>
    /// Gets the source of the current answer, such as <c>live</c> or <c>cache</c>, when known.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the UTC timestamp until which the current answer remains fresh, when known.
    /// </summary>
    public DateTimeOffset? FreshUntilUtc { get; }

    /// <summary>
    /// Gets the age in seconds of the current answer, when known.
    /// </summary>
    public int? AgeSeconds { get; }
}
