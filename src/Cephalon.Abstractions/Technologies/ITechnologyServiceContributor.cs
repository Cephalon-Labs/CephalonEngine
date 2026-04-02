using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Configures services required by active technology profiles.
/// </summary>
public interface ITechnologyServiceContributor
{
    /// <summary>
    /// Configures services for the active technology selection.
    /// </summary>
    /// <param name="services">The service collection receiving technology services.</param>
    /// <param name="technologies">The active technology selection.</param>
    void ConfigureTechnologyServices(
        IServiceCollection services,
        TechnologySelection technologies);
}
