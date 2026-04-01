using System.Text.Json;

namespace Cephalon.Benchmarks.Validation;

/// <summary>
/// Represents the persisted benchmark guardrail catalog used during validation.
/// </summary>
public sealed class GuardrailCatalog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuardrailCatalog"/> class.
    /// </summary>
    /// <param name="version">
    /// The schema or content version of the guardrail catalog.
    /// </param>
    /// <param name="entries">
    /// The guardrail entries that define the accepted performance envelope.
    /// </param>
    public GuardrailCatalog(
        string version,
        IReadOnlyList<GuardrailEntry> entries)
    {
        Version = string.IsNullOrWhiteSpace(version) ? "1.0" : version.Trim();
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
    }

    /// <summary>
    /// Gets the catalog version recorded in the guardrail file.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the benchmark entries enforced by the catalog.
    /// </summary>
    public IReadOnlyList<GuardrailEntry> Entries { get; }

    /// <summary>
    /// Loads a guardrail catalog from disk.
    /// </summary>
    /// <param name="path">
    /// The path to the JSON guardrail catalog file.
    /// </param>
    /// <returns>
    /// The parsed guardrail catalog.
    /// </returns>
    public static GuardrailCatalog Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var version = root.TryGetProperty("version", out var versionElement)
            ? versionElement.GetString()
            : "1.0";

        var entries = new List<GuardrailEntry>();
        foreach (var entryElement in root.GetProperty("entries").EnumerateArray())
        {
            entries.Add(new GuardrailEntry(
                ReportFileName: entryElement.GetProperty("reportFileName").GetString()
                    ?? throw new InvalidOperationException("Guardrail entry reportFileName was missing."),
                Benchmark: entryElement.GetProperty("benchmark").GetString()
                    ?? throw new InvalidOperationException("Guardrail entry benchmark was missing."),
                MaxMeanNanoseconds: entryElement.GetProperty("maxMeanNanoseconds").GetDouble(),
                MaxAllocatedBytes: entryElement.TryGetProperty("maxAllocatedBytes", out var allocatedElement)
                    ? allocatedElement.GetDouble()
                    : null,
                Notes: entryElement.TryGetProperty("notes", out var notesElement)
                    ? notesElement.GetString()
                    : null));
        }

        return new GuardrailCatalog(version ?? "1.0", entries);
    }
}
