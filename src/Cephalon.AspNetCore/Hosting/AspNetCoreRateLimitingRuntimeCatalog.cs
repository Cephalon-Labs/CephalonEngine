using Cephalon.Abstractions.Resilience;
using Cephalon.AspNetCore.Documentation;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class AspNetCoreRateLimitingRuntimeCatalog : IRateLimitingRuntimeCatalog
{
    private readonly IReadOnlyList<RateLimitingRuntimeDescriptor> policies;
    private readonly Dictionary<string, RateLimitingRuntimeDescriptor> policiesById;
    private readonly Dictionary<string, IReadOnlyList<RateLimitingRuntimeDescriptor>> policiesByTransportId;

    public AspNetCoreRateLimitingRuntimeCatalog(
        IRuntime runtime,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var requested = runtime.Manifest.AppProfile.Resilience.RateLimiting;
        var resolved = AspNetCoreRateLimitingPolicyResolver.Resolve(
            runtime.Manifest,
            configuration,
            ReferenceDocsHostingOptions.FromConfiguration(
                configuration,
                contentRootPath: environment.ContentRootPath));
        if (!string.Equals(
                resolved.ExecutionMode,
                AspNetCoreRateLimitingPolicyResolver.EnabledExecutionMode,
                StringComparison.OrdinalIgnoreCase))
        {
            policies = [];
            policiesById = new Dictionary<string, RateLimitingRuntimeDescriptor>(StringComparer.OrdinalIgnoreCase);
            policiesByTransportId = new Dictionary<string, IReadOnlyList<RateLimitingRuntimeDescriptor>>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        policies =
        [
            resolved.ToDescriptor(requested)
        ];
        policiesById = policies.ToDictionary(static policy => policy.Id, StringComparer.OrdinalIgnoreCase);
        policiesByTransportId = policies
            .SelectMany(static policy => policy.TransportIds.Select(transportId => new KeyValuePair<string, RateLimitingRuntimeDescriptor>(transportId, policy)))
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RateLimitingRuntimeDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<RateLimitingRuntimeDescriptor> Policies => policies;

    public RateLimitingRuntimeDescriptor? GetById(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            return null;
        }

        return policiesById.TryGetValue(policyId.Trim(), out var policy)
            ? policy
            : null;
    }

    public IReadOnlyList<RateLimitingRuntimeDescriptor> GetByTransportId(string transportId)
    {
        if (string.IsNullOrWhiteSpace(transportId))
        {
            return [];
        }

        return policiesByTransportId.TryGetValue(transportId.Trim(), out var matches)
            ? matches
            : [];
    }
}
