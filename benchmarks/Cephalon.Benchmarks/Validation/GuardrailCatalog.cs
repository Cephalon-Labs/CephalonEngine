using System.Text.Json;

namespace Cephalon.Benchmarks.Validation;

public sealed class GuardrailCatalog
{
    public GuardrailCatalog(
        string version,
        IReadOnlyList<GuardrailEntry> entries)
    {
        Version = string.IsNullOrWhiteSpace(version) ? "1.0" : version.Trim();
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
    }

    public string Version { get; }

    public IReadOnlyList<GuardrailEntry> Entries { get; }

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
