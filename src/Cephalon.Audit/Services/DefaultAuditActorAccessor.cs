using Cephalon.Abstractions.Audit;

namespace Cephalon.Audit.Services;

internal sealed class DefaultAuditActorAccessor : IAuditActorAccessor
{
    public AuditActor? Current => null;
}
