using Cephalon.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Registers the ASP.NET Core multi-tenancy governance adapter on a <see cref="WebApplicationBuilder" />.
/// </summary>
public static class MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Cephalon ASP.NET Core multi-tenancy governance adapter to the target application builder.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven ASP.NET Core governance adapter options.
    /// </param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder AddCephalonMultiTenancyGovernanceAspNetCore(
        this WebApplicationBuilder builder,
        Action<MultiTenancyGovernanceAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddCephalonProjectConfigurations();
        builder.Services.AddCephalonMultiTenancyGovernanceAspNetCore(builder.Configuration, configure);
        return builder;
    }
}
