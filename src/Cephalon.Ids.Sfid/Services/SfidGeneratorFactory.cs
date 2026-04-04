using Cephalon.Ids.Sfid.Configuration;
using SfidNet;
using SfidNet.Abstractions;

namespace Cephalon.Ids.Sfid.Services;

internal static class SfidGeneratorFactory
{
    public static ISfidGenerator Create(
        SfidIdOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var generatorOptions = new SfidOptions();
        if (options.DatacenterId.HasValue)
        {
            generatorOptions.DatacenterId = options.DatacenterId.Value;
        }

        if (options.WorkerId.HasValue)
        {
            generatorOptions.WorkerId = options.WorkerId.Value;
        }

        if (options.WorkerCapacity.HasValue)
        {
            generatorOptions.WorkerCapacity = options.WorkerCapacity.Value;
        }

        if (options.ClockRegressionToleranceMilliseconds.HasValue)
        {
            generatorOptions.ClockRegressionTolerance = TimeSpan.FromMilliseconds(options.ClockRegressionToleranceMilliseconds.Value);
        }

        generatorOptions.Validate();
        return new SfidGenerator(generatorOptions, timeProvider);
    }
}
