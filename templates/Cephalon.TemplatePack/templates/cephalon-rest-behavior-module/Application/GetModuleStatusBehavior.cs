using System.Threading;
using System.Threading.Tasks;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Localization;
using Cephalon.Behaviors.Http.Abstractions;
using CephalonTemplateModule.Contracts;

namespace CephalonTemplateModule.Application;

[AppBehavior("template.behavior-rest-module.status.get")]
[BehaviorAllowedPatterns("direct")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/status", ApiVersionMajor = 1)]
public sealed class GetModuleStatusBehavior : IAppBehavior<GetModuleStatusInput, Result<RestBehaviorModuleStatusSnapshot>>
{
    private readonly RestBehaviorModuleStatusService statusService;
    private readonly ILocalizedTextCatalog textCatalog;

    public GetModuleStatusBehavior(
        RestBehaviorModuleStatusService statusService,
        ILocalizedTextCatalog textCatalog)
    {
        this.statusService = statusService;
        this.textCatalog = textCatalog;
    }

    public Task<Result<RestBehaviorModuleStatusSnapshot>> HandleAsync(
        GetModuleStatusInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var culture = string.IsNullOrWhiteSpace(input.Culture)
            ? textCatalog.DefaultCulture
            : input.Culture.Trim();
        var message = textCatalog.ResolveText(
            key: "module.template.behavior-rest.status",
            culture: culture,
            fallback: "Template behavior-backed REST module is running.");

        return Task.FromResult(Result.Ok(
            statusService.CreateSnapshot(culture, message),
            message: "Template behavior-backed REST module status resolved."));
    }

    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect();
    }
}
