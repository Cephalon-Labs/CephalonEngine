using System.Threading;
using Cephalon.Abstractions.Tenancy;

namespace Cephalon.MultiTenancy.Services;

internal sealed class AmbientTenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> CurrentTenant = new();

    public TenantContext? Current => CurrentTenant.Value;

    public static void SetCurrent(TenantContext? tenant)
    {
        CurrentTenant.Value = tenant;
    }
}
