using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Audit.Configuration;

/// <summary>
/// Describes host-agnostic runtime options for the Cephalon audit companion pack.
/// </summary>
public sealed class AuditRuntimeOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditRuntimeOptions" /> class.
    /// </summary>
    /// <param name="inMemoryBufferCapacity">The maximum number of audit entries retained by the default in-memory writer.</param>
    public AuditRuntimeOptions(
        int inMemoryBufferCapacity = 1024)
    {
        if (inMemoryBufferCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inMemoryBufferCapacity), "Audit in-memory buffer capacity must be greater than zero.");
        }

        InMemoryBufferCapacity = inMemoryBufferCapacity;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the built-in in-memory audit writer should remain active.
    /// </summary>
    public bool EnableInMemoryWriter { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of audit entries retained by the default in-memory writer.
    /// </summary>
    public int InMemoryBufferCapacity { get; set; } = 1024;

    /// <summary>
    /// Reads audit runtime options from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed audit runtime options.</returns>
    public static AuditRuntimeOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        if (configuration is null)
        {
            return new AuditRuntimeOptions();
        }

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Audit");

        return new AuditRuntimeOptions(
            inMemoryBufferCapacity: ParsePositiveInt(section["InMemoryBufferCapacity"], defaultValue: 1024))
        {
            EnableInMemoryWriter = ParseBoolean(section["EnableInMemoryWriter"], defaultValue: true)
        };
    }

    private static bool ParseBoolean(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value.Trim(), out var parsed)
            ? parsed
            : defaultValue;
    }

    private static int ParsePositiveInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }
}
