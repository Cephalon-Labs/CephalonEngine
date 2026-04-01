namespace Cephalon.Sample.Microservice.Contracts;

public sealed record WelcomeContract(
    string Service,
    string TenantPolicy,
    string Message);
