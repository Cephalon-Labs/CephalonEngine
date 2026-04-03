using Cephalon.Sample.MicroserviceSuite.Foundation.Contracts;

namespace Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;

/// <summary>
/// Provides the shared suite-level conventions used by the microservice-suite sample.
/// </summary>
public static class CommerceSuiteConventions
{
    /// <summary>
    /// Gets the shared suite name used by the sample services.
    /// </summary>
    public const string SuiteName = "CommerceSuite";

    /// <summary>
    /// Gets the catalog service identifier used by the sample suite.
    /// </summary>
    public const string CatalogService = "CatalogService";

    /// <summary>
    /// Gets the orders service identifier used by the sample suite.
    /// </summary>
    public const string OrdersService = "OrdersService";

    /// <summary>
    /// Gets the shared foundation project identifier used by the sample suite.
    /// </summary>
    public const string SharedFoundationProject = "Cephalon.Sample.MicroserviceSuite.Shared.Foundation";

    /// <summary>
    /// Gets the shared convention profile name used by the sample suite.
    /// </summary>
    public const string ConventionProfile = "shared-foundation-first";

    /// <summary>
    /// Gets the service boundaries that participate in the sample suite.
    /// </summary>
    public static IReadOnlyList<string> KnownServices { get; } =
    [
        CatalogService,
        OrdersService
    ];

    /// <summary>
    /// Builds the shared suite summary returned by one sample service.
    /// </summary>
    /// <param name="service">
    /// The service boundary producing the response.
    /// </param>
    /// <param name="message">
    /// The service-specific message to include in the response.
    /// </param>
    /// <returns>
    /// The shared suite summary contract for the requested service.
    /// </returns>
    public static SuiteServiceSummaryContract CreateServiceSummary(string service, string message)
    {
        var normalizedService = NormalizeService(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        EnsureKnownService(normalizedService);

        return new SuiteServiceSummaryContract(
            Suite: SuiteName,
            Service: normalizedService,
            ConventionProfile: ConventionProfile,
            SharedFoundation: SharedFoundationProject,
            PartnerServices: GetPartnerServices(normalizedService),
            Message: message.Trim());
    }

    /// <summary>
    /// Gets the peer services that belong to the suite alongside the requested service.
    /// </summary>
    /// <param name="service">
    /// The service boundary whose partner services should be returned.
    /// </param>
    /// <returns>
    /// The peer services that share the same suite foundation.
    /// </returns>
    public static IReadOnlyList<string> GetPartnerServices(string service)
    {
        var normalizedService = NormalizeService(service);
        EnsureKnownService(normalizedService);

        return KnownServices
            .Where(candidate => !string.Equals(candidate, normalizedService, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string NormalizeService(string service)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        return service.Trim();
    }

    private static void EnsureKnownService(string service)
    {
        if (!KnownServices.Contains(service, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Service '{service}' does not belong to the '{SuiteName}' sample suite.");
        }
    }
}
