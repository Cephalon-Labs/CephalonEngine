namespace CephalonTemplateApp.Contracts;

public sealed record WelcomeContract(
    string Service,
    string TenantPolicy,
    string Message);
