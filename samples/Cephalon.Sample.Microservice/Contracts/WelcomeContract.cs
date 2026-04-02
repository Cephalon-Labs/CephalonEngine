namespace Cephalon.Sample.Microservice.Contracts;

/// <summary>
/// Represents the welcome payload returned by the microservice sample.
/// </summary>
/// <param name="Service">
/// The service boundary that produced the response.
/// </param>
/// <param name="TenantPolicy">
/// The tenant policy applied while building the response.
/// </param>
/// <param name="Message">
/// The user-facing welcome message.
/// </param>
public sealed record WelcomeContract(
    string Service,
    string TenantPolicy,
    string Message);
