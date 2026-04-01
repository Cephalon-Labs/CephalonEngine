using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Abstractions.Technologies;

public interface ITechnologyServiceContributor
{
    void ConfigureTechnologyServices(
        IServiceCollection services,
        TechnologySelection technologies);
}
