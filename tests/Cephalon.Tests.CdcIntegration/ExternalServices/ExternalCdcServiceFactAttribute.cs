namespace Cephalon.Tests.CdcIntegration.ExternalServices;

/// <summary>
/// Marks CDC integration tests that require an external provider runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ExternalCdcServiceFactAttribute : FactAttribute
{
    public ExternalCdcServiceFactAttribute()
        : this(ExternalCdcServiceProvider.Any)
    {
    }

    public ExternalCdcServiceFactAttribute(ExternalCdcServiceProvider provider)
    {
        var skipReason = ExternalCdcServiceGate.FromEnvironment().GetSkipReason(provider);
        if (skipReason is not null)
        {
            Skip = skipReason;
        }
    }
}
