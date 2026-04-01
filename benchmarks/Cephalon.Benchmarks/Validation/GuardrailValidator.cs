namespace Cephalon.Benchmarks.Validation;

public sealed class GuardrailValidator
{
    public static GuardrailValidationResult Validate(
        GuardrailCatalog catalog,
        IReadOnlyList<BenchmarkMeasurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(measurements);

        var messages = new List<string>();
        var passed = true;

        foreach (var entry in catalog.Entries)
        {
            var measurement = measurements.FirstOrDefault(item =>
                string.Equals(item.ReportFileName, entry.ReportFileName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Benchmark, entry.Benchmark, StringComparison.OrdinalIgnoreCase));

            if (measurement is null)
            {
                passed = false;
                messages.Add(
                    $"Missing benchmark result for '{entry.Benchmark}' in '{entry.ReportFileName}'.");
                continue;
            }

            if (measurement.MeanNanoseconds > entry.MaxMeanNanoseconds)
            {
                passed = false;
                messages.Add(
                    $"Mean for '{entry.Benchmark}' exceeded guardrail: {measurement.MeanNanoseconds:N0} ns > {entry.MaxMeanNanoseconds:N0} ns.");
            }

            if (entry.MaxAllocatedBytes is not null &&
                measurement.AllocatedBytes is not null &&
                measurement.AllocatedBytes > entry.MaxAllocatedBytes.Value)
            {
                passed = false;
                messages.Add(
                    $"Allocation for '{entry.Benchmark}' exceeded guardrail: {measurement.AllocatedBytes:N0} B > {entry.MaxAllocatedBytes.Value:N0} B.");
            }
        }

        if (passed)
        {
            messages.Add(
                $"Benchmark guardrails passed for {catalog.Entries.Count} benchmark entries.");
        }

        return new GuardrailValidationResult(passed, messages);
    }
}
