namespace Cephalon.Agentics.Services;

internal static class AgentToolExecutionIdempotencyDurabilityModes
{
    public const string ProcessLocal = "process-local";

    public const string Inbox = "inbox";

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ProcessLocal;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            ProcessLocal => ProcessLocal,
            Inbox => Inbox,
            var unknown => throw new InvalidOperationException(
                $"Agentics execution idempotency durability '{unknown}' is not supported. Use '{ProcessLocal}' or '{Inbox}'.")
        };
    }
}
