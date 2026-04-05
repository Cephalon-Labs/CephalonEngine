using Cephalon.AspNetCore.Hosting;
using Cephalon.Identity.AspNetCore.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.Identity.AspNetCore.Hosting;

/// <summary>
/// Registers the ASP.NET Core identity adapter on a <see cref="WebApplicationBuilder" />.
/// </summary>
public static class IdentityAspNetCoreWebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Cephalon ASP.NET Core identity adapter to the target application builder.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven ASP.NET Core identity adapter options.
    /// </param>
    /// <returns>The same builder instance for fluent composition.</returns>
    /// <remarks>
    /// This keeps ASP.NET Core-specific principal, claim, and route-bound authorization mapping in the host layer while
    /// still feeding the host-agnostic Cephalon authorization contracts. When <c>Cephalon.Audit</c> is also active and
    /// no custom <see cref="Cephalon.Audit.Services.IAuditActorAccessor" /> has been registered, the same adapter also
    /// bridges the current authenticated principal into the ambient audit actor contract.
    /// </remarks>
    public static WebApplicationBuilder AddCephalonIdentityAspNetCore(
        this WebApplicationBuilder builder,
        Action<IdentityAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddCephalonProjectConfigurations();
        builder.Services.AddCephalonIdentityAspNetCore(builder.Configuration, configure);
        return builder;
    }
}
