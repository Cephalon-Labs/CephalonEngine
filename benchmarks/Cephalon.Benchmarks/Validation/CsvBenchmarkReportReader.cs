namespace Cephalon.Benchmarks.Validation;

public static class CsvBenchmarkReportReader
{
    public static IReadOnlyList<BenchmarkMeasurement> ReadDirectory(string resultsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resultsDirectory);

        if (!Directory.Exists(resultsDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Benchmark results directory '{resultsDirectory}' was not found.");
        }

        var measurements = new List<BenchmarkMeasurement>();
        foreach (var path in Directory.GetFiles(resultsDirectory, "*-report.csv", SearchOption.TopDirectoryOnly))
        {
            measurements.AddRange(ReadFile(path));
        }

        return measurements;
    }

    public static IReadOnlyList<BenchmarkMeasurement> ReadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            return [];
        }

        var headers = SplitCsvLine(lines[0]);
        var methodIndex = FindIndex(headers, "Method");
        var meanIndex = FindIndex(headers, "Mean");
        var allocatedIndex = FindIndex(headers, "Allocated");

        var results = new List<BenchmarkMeasurement>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var cells = SplitCsvLine(lines[lineIndex]);
            var method = GetCell(cells, methodIndex);
            var mean = ParseMeasurementToNanoseconds(GetCell(cells, meanIndex));
            var allocated = allocatedIndex >= 0
                ? ParseSizeToBytes(GetCell(cells, allocatedIndex))
                : null;

            results.Add(new BenchmarkMeasurement(
                ReportFileName: Path.GetFileName(path),
                Benchmark: method,
                MeanNanoseconds: mean,
                AllocatedBytes: allocated));
        }

        return results;
    }

    private static int FindIndex(List<string> headers, string columnName)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            if (string.Equals(headers[index], columnName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        if (string.Equals(columnName, "Allocated", StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        throw new InvalidOperationException($"Benchmark report column '{columnName}' was not found.");
    }

    private static string GetCell(List<string> cells, int index)
    {
        if (index < 0 || index >= cells.Count)
        {
            return string.Empty;
        }

        return cells[index].Trim();
    }

    private static List<string> SplitCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var insideQuotes = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                insideQuotes = !insideQuotes;
                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static double ParseMeasurementToNanoseconds(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Benchmark mean value was empty.");
        }

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Benchmark mean value '{value}' could not be parsed.");
        }

        var amount = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
        return parts[1] switch
        {
            "ns" => amount,
            "μs" => amount * 1_000d,
            "us" => amount * 1_000d,
            "ms" => amount * 1_000_000d,
            "s" => amount * 1_000_000_000d,
            _ => throw new InvalidOperationException($"Benchmark mean unit '{parts[1]}' is not supported.")
        };
    }

    private static double? ParseSizeToBytes(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "NA", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Benchmark allocation value '{value}' could not be parsed.");
        }

        var amount = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
        return parts[1] switch
        {
            "B" => amount,
            "KB" => amount * 1024d,
            "MB" => amount * 1024d * 1024d,
            "GB" => amount * 1024d * 1024d * 1024d,
            _ => throw new InvalidOperationException($"Benchmark allocation unit '{parts[1]}' is not supported.")
        };
    }
}
