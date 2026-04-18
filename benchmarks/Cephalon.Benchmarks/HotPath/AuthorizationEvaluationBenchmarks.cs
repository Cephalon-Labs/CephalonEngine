using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Authorization;
using Cephalon.Benchmarks.Support;
using Cephalon.Data.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Wolverine.Registration;
using Cephalon.Identity.Registration;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Audit.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures the per-call overhead of the built-in metadata-driven authorization evaluator.
/// The evaluator resolves policies from the catalog, evaluates role-based and attribute-based
/// rules, and produces an <see cref="AuthorizationDecision" />. These benchmarks measure
/// both the allow and deny evaluation paths.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class AuthorizationEvaluationBenchmarks
{
    private const int EvaluationsPerIteration = 8192;

    private ServiceProvider provider = null!;
    private IAuthorizationEvaluator evaluator = null!;
    private AuthorizationSubject adminSubject = null!;
    private AuthorizationSubject regularSubject = null!;
    private AuthorizationResource resource = null!;
    private AuthorizationContext adminContext = null!;

    /// <summary>
    /// Builds the engine with identity access and authorization policies, then resolves the evaluator.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        var builder = new EngineBuilder(services);

        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["clean-architecture", "ddd", "cqrs"],
            transports: ["rest-api"],
            technologies: ["identity-access"],
            identity: new IdentitySettings(
                enabled: true,
                authorizationModes: ["RBAC"])));

        builder.AddData();
        builder.AddSfidIds();
        builder.AddEventing();
        builder.AddWolverineEventing();
        builder.AddIdentityAccess();
        builder.AddMultiTenancy();
        builder.AddAudit();
        builder.AddModule(new BenchmarkClockModule());
        builder.AddModule(new BenchmarkAuthorizationPolicyModule());

        using var runtime = builder.Build();
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider);

        evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        adminSubject = new AuthorizationSubject(
            subjectId: "user-001",
            displayName: "Benchmark Admin",
            roles: ["admin", "user"],
            tenantIds: ["tenant-alpha"]);

        regularSubject = new AuthorizationSubject(
            subjectId: "user-002",
            displayName: "Benchmark User",
            roles: ["user"],
            tenantIds: ["tenant-alpha"]);

        resource = new AuthorizationResource(
            resourceType: "benchmark-resource",
            resourceId: "res-001",
            tenantId: "tenant-alpha");

        adminContext = new AuthorizationContext(
            action: "read",
            policyId: BenchmarkAuthorizationPolicyModule.AdminPolicyId,
            tenantId: "tenant-alpha");

        // Warm the evaluator
        await evaluator.EvaluateAsync(adminSubject, resource, adminContext);
        await evaluator.EvaluateAsync(regularSubject, resource, adminContext);
    }

    /// <summary>
    /// Releases all DI resources.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        provider.Dispose();
    }

    /// <summary>
    /// Evaluates an authorization request where the subject has the required role and the decision is allowed.
    /// </summary>
    [Benchmark(OperationsPerInvoke = EvaluationsPerIteration)]
    public async Task<int> EvaluateRbacAllow()
    {
        var allowCount = 0;
        for (var i = 0; i < EvaluationsPerIteration; i++)
        {
            var decision = await evaluator.EvaluateAsync(adminSubject, resource, adminContext);
            if (decision.IsAllowed) allowCount++;
        }

        return allowCount;
    }

    /// <summary>
    /// Evaluates an authorization request where the subject lacks the required role and the decision is denied.
    /// </summary>
    [Benchmark(OperationsPerInvoke = EvaluationsPerIteration)]
    public async Task<int> EvaluateRbacDeny()
    {
        var denyCount = 0;
        for (var i = 0; i < EvaluationsPerIteration; i++)
        {
            var decision = await evaluator.EvaluateAsync(regularSubject, resource, adminContext);
            if (!decision.IsAllowed) denyCount++;
        }

        return denyCount;
    }
}
