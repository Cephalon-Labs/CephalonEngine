namespace Cephalon.AspNetCore.Hosting;

internal sealed class AspNetCoreHostEnvironmentSnapshot
{
    internal AspNetCoreHostEnvironmentSnapshot(string? environmentName)
    {
        EnvironmentName = string.IsNullOrWhiteSpace(environmentName)
            ? null
            : environmentName.Trim();
    }

    internal string? EnvironmentName { get; }
}
