using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.Abstractions.Coordination;

/// <summary>An immutable, expiring plan derived solely from supplied intent and observation.</summary>
public sealed class ReconciliationPlan
{
    /// <summary>Creates a deterministic plan without reading a clock or invoking a provider.</summary>
    /// <param name="request">The immutable intent.</param>
    /// <param name="observedRevision">The observed provider revision.</param>
    /// <param name="createdAtUtc">The observation time and earliest apply time.</param>
    /// <param name="expiresAtUtc">The exclusive deadline, at most one day after creation.</param>
    public ReconciliationPlan(ReconciliationRequest request, string observedRevision,
        DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        Request = request;
        ObservedRevision = ReconciliationRequest.RequireIdentifier(observedRevision, nameof(observedRevision));
        if (expiresAtUtc <= createdAtUtc || expiresAtUtc - createdAtUtc > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Plan validity must be positive and at most one day.");
        }

        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        State = observedRevision == request.DesiredRevision ? ReconciliationPlanState.Converged
            : observedRevision != request.ExpectedRevision ? ReconciliationPlanState.Stale
            : ReconciliationPlanState.Ready;
        // Length-prefixed UTF-8 strings prevent delimiter collisions; UTC ticks canonicalize offsets.
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, new UTF8Encoding(false, true), leaveOpen: true))
        {
            foreach (var value in new[] { "cephalon-reconciliation-v1", request.OperationId, request.TenantId,
                request.ActorId, request.ActionId, request.TargetId, request.DesiredRevision,
                request.ExpectedRevision, observedRevision, CreatedAtUtc.Ticks.ToString(CultureInfo.InvariantCulture),
                ExpiresAtUtc.Ticks.ToString(CultureInfo.InvariantCulture) })
            {
                writer.Write(value);
            }
        }

        Fingerprint = Convert.ToHexString(SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)));
    }

    /// <summary>Gets the immutable intent.</summary>
    public ReconciliationRequest Request { get; }
    /// <summary>Gets the observed revision; the effect must check it atomically again at mutation.</summary>
    public string ObservedRevision { get; }
    /// <summary>Gets the inclusive earliest apply time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }
    /// <summary>Gets the exclusive total execution deadline.</summary>
    public DateTimeOffset ExpiresAtUtc { get; }
    /// <summary>Gets the deterministic planning decision.</summary>
    public ReconciliationPlanState State { get; }
    /// <summary>Gets the SHA-256 intent/observation/time binding. This is not a signature or authorization grant.</summary>
    public string Fingerprint { get; }
}
