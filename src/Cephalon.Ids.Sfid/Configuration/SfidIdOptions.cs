using Microsoft.Extensions.Configuration;

namespace Cephalon.Ids.Sfid.Configuration;

/// <summary>
/// Describes the host-owned configuration used to bootstrap the official <c>Sfid.Net</c> generator.
/// </summary>
public sealed class SfidIdOptions
{
    /// <summary>
    /// Gets the default configuration path used by the Sfid id-strategy pack.
    /// </summary>
    public const string DefaultSectionPath = "Engine:Data:Ids:Sfid";

    /// <summary>
    /// Gets an empty options instance.
    /// </summary>
    public static SfidIdOptions Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SfidIdOptions" /> class.
    /// </summary>
    /// <param name="datacenterId">The datacenter identifier supplied to the generator.</param>
    /// <param name="workerId">The worker identifier supplied to the generator.</param>
    /// <param name="workerCapacity">The optional worker-capacity override supplied to the generator.</param>
    /// <param name="clockRegressionToleranceMilliseconds">
    /// The optional clock-regression tolerance, in milliseconds, supplied to the generator.
    /// </param>
    public SfidIdOptions(
        int? datacenterId = null,
        int? workerId = null,
        int? workerCapacity = null,
        int? clockRegressionToleranceMilliseconds = null)
    {
        DatacenterId = datacenterId;
        WorkerId = workerId;
        WorkerCapacity = workerCapacity;
        ClockRegressionToleranceMilliseconds = clockRegressionToleranceMilliseconds;
    }

    /// <summary>
    /// Gets the datacenter identifier supplied to the generator.
    /// </summary>
    public int? DatacenterId { get; set; }

    /// <summary>
    /// Gets the worker identifier supplied to the generator.
    /// </summary>
    public int? WorkerId { get; set; }

    /// <summary>
    /// Gets the optional worker-capacity override supplied to the generator.
    /// </summary>
    public int? WorkerCapacity { get; set; }

    /// <summary>
    /// Gets the optional clock-regression tolerance, in milliseconds, supplied to the generator.
    /// </summary>
    public int? ClockRegressionToleranceMilliseconds { get; set; }

    /// <summary>
    /// Gets a value indicating whether any explicit Sfid-generator inputs were supplied.
    /// </summary>
    public bool HasValues =>
        DatacenterId.HasValue ||
        WorkerId.HasValue ||
        WorkerCapacity.HasValue ||
        ClockRegressionToleranceMilliseconds.HasValue;

    /// <summary>
    /// Reads Sfid id-strategy options from configuration.
    /// </summary>
    /// <param name="configuration">The configuration root to read.</param>
    /// <param name="sectionPath">
    /// The configuration path that contains the Sfid id-strategy settings. The default is <c>Engine:Data:Ids:Sfid</c>.
    /// </param>
    /// <returns>The parsed options.</returns>
    public static SfidIdOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = DefaultSectionPath)
    {
        if (configuration is null)
        {
            return Empty;
        }

        var section = configuration.GetSection(string.IsNullOrWhiteSpace(sectionPath) ? DefaultSectionPath : sectionPath.Trim());

        return new SfidIdOptions(
            datacenterId: TryParseInt32(section["DatacenterId"]),
            workerId: TryParseInt32(section["WorkerId"]),
            workerCapacity: TryParseInt32(section["WorkerCapacity"]),
            clockRegressionToleranceMilliseconds: TryParseInt32(section["ClockRegressionToleranceMilliseconds"]));
    }

    private static int? TryParseInt32(string? value)
    {
        return int.TryParse(value, out var parsed)
            ? parsed
            : null;
    }
}
