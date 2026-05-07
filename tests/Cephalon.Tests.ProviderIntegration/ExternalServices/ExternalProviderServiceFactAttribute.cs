namespace Cephalon.Tests.ProviderIntegration.ExternalServices;

/// <summary>
/// Marks provider integration tests that require an external provider runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ExternalProviderServiceFactAttribute : FactAttribute
{
    public ExternalProviderServiceFactAttribute()
        : this(ExternalProviderServiceProvider.Any)
    {
    }

    public ExternalProviderServiceFactAttribute(ExternalProviderServiceProvider provider)
    {
        var skipReason = ExternalProviderServiceGate.FromEnvironment().GetSkipReason(provider);
        if (skipReason is not null)
        {
            Skip = skipReason;
        }
    }
}
