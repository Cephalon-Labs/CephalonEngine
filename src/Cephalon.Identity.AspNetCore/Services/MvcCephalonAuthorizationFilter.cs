using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cephalon.Identity.AspNetCore.Services;

internal sealed class MvcCephalonAuthorizationFilter(
    CephalonAuthorizationBoundaryExecutor executor,
    RestAuthorizationRequestMetadata metadata) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var allowAnonymous = context.Filters.OfType<IAllowAnonymousFilter>().Any() ||
            context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var result = await executor.ExecuteAsync(
            context.HttpContext,
            metadata,
            allowAnonymous,
            context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!result.IsAllowed)
        {
            context.Result = result.ToMvcActionResult();
        }
    }
}
