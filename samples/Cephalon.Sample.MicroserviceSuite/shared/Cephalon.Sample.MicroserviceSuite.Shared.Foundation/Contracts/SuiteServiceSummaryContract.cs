namespace Cephalon.Sample.MicroserviceSuite.Foundation.Contracts;

/// <summary>
/// Represents the shared suite-level summary returned by the microservice-suite sample services.
/// </summary>
/// <param name="Suite">
/// The logical suite name shared by the participating services.
/// </param>
/// <param name="Service">
/// The service boundary that produced the response.
/// </param>
/// <param name="ConventionProfile">
/// The shared convention profile applied by the suite foundation.
/// </param>
/// <param name="SharedFoundation">
/// The shared foundation project identifier reused by every service in the suite.
/// </param>
/// <param name="PartnerServices">
/// The peer services that belong to the same suite sample.
/// </param>
/// <param name="Message">
/// The service-specific summary message.
/// </param>
public sealed record SuiteServiceSummaryContract(
    string Suite,
    string Service,
    string ConventionProfile,
    string SharedFoundation,
    IReadOnlyList<string> PartnerServices,
    string Message);
