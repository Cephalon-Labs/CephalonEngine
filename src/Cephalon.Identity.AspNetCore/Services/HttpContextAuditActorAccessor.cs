using Cephalon.Abstractions.Audit;
using Cephalon.Audit.Services;
using Microsoft.AspNetCore.Http;

namespace Cephalon.Identity.AspNetCore.Services;

internal sealed class HttpContextAuditActorAccessor(
    IHttpContextAccessor httpContextAccessor,
    IdentityPrincipalDescriptorFactory principalDescriptorFactory) : IAuditActorAccessor
{
    public AuditActor? Current
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (!principalDescriptorFactory.TryCreate(principal, out var descriptor, out _))
            {
                return null;
            }

            return new AuditActor(
                actorId: descriptor!.SubjectId,
                displayName: descriptor.DisplayName,
                actorType: "principal",
                isSystem: false,
                attributes: descriptor.AuditActorAttributes);
        }
    }
}
