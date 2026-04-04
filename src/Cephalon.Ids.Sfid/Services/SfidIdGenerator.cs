using System.Globalization;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Ids;
using SfidNet.Abstractions;

namespace Cephalon.Ids.Sfid.Services;

internal sealed class SfidIdGenerator : IIdGenerator
{
    private readonly ISfidGenerator generator;

    public SfidIdGenerator(
        ISfidGenerator generator,
        AppProfile appProfile)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(appProfile);

        ValidateSelection(appProfile);
        this.generator = generator;
    }

    public string StrategyId => "sfid";

    public ValueTask<string> GenerateAsync(
        IdGenerationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(generator.NextId().ToString(CultureInfo.InvariantCulture));
    }

    private static void ValidateSelection(AppProfile appProfile)
    {
        var configuredGenerator = appProfile.Data.IdGenerator;
        if (string.IsNullOrWhiteSpace(configuredGenerator))
        {
            return;
        }

        var normalized = configuredGenerator.Trim();
        if (string.Equals(normalized, "sfid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "snowfake", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"The active app profile selected id generator '{configuredGenerator}', but Cephalon.Ids.Sfid only satisfies the 'Sfid' strategy. Update Engine:Data:Ids:Generator or register the matching id pack.");
    }
}
