using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cephalon.Cli.Commands;

internal sealed record DeploymentModeSupportContract(
    DeploymentModeSupportShippingBaseline ShippingBaseline,
    DeploymentModeSupportModes DeploymentModes,
    DeploymentModeEligibility? DeploymentModeEligibility)
{
    private const string ResourceName = "Cephalon.Cli.Resources.deployment-mode-support.json";

    internal static bool TryLoad(out DeploymentModeSupportContract? contract, out string? error)
    {
        contract = null;
        error = null;

        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream is null)
            {
                error = $"Embedded resource '{ResourceName}' was not found.";
                return false;
            }

            contract = JsonSerializer.Deserialize(stream, DeploymentModeSupportContractJsonContext.Default.DeploymentModeSupportContract);
            if (contract is null)
            {
                error = "Embedded deployment-mode support contract could not be deserialized.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(contract.ShippingBaseline.StableTargetFramework) ||
                string.IsNullOrWhiteSpace(contract.ShippingBaseline.ReadinessLaneTargetFramework) ||
                string.IsNullOrWhiteSpace(contract.ShippingBaseline.ReadinessLaneStatus) ||
                string.IsNullOrWhiteSpace(contract.DeploymentModes.Trim.Status) ||
                string.IsNullOrWhiteSpace(contract.DeploymentModes.NativeAot.Status) ||
                string.IsNullOrWhiteSpace(contract.DeploymentModes.SingleFile.Status))
            {
                error = "Embedded deployment-mode support contract is missing required fields.";
                contract = null;
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            contract = null;
            return false;
        }
    }
}

internal sealed record DeploymentModeSupportShippingBaseline(
    string StableTargetFramework,
    string ReadinessLaneTargetFramework,
    string ReadinessLaneStatus);

internal sealed record DeploymentModeSupportModes(
    DeploymentModeSupportMode Trim,
    DeploymentModeSupportMode NativeAot,
    DeploymentModeSupportMode SingleFile);

internal sealed record DeploymentModeSupportMode(
    string Status,
    string Summary);

internal sealed record DeploymentModeEligibility(
    IReadOnlyList<DeploymentModePackageEligibility> Packages);

internal sealed record DeploymentModePackageEligibility(
    string PackageName,
    string NugetId,
    string ClaimAuditTier,
    IReadOnlyList<string> SupportedModes,
    IReadOnlyList<string> RequiredProjectProperties);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(DeploymentModeSupportContract))]
internal sealed partial class DeploymentModeSupportContractJsonContext : JsonSerializerContext
{
}
