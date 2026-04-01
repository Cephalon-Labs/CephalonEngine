namespace Cephalon.Playground.Services;

public sealed class GreetingComposer
{
    private static readonly string[] Traits = ["modular", "observable", "composable"];
    private readonly IEngineClock clock;

    public GreetingComposer(IEngineClock clock)
    {
        this.clock = clock;
    }

    public GreetingEnvelope Compose(string? name)
    {
        var visitor = string.IsNullOrWhiteSpace(name) ? "builder" : name.Trim();

        return new GreetingEnvelope(
            Message: $"Hello, {visitor} from the Cephalon future stack.",
            GeneratedAtUtc: clock.GetUtcNow(),
            Traits: Traits);
    }
}

public sealed record GreetingEnvelope(
    string Message,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<string> Traits);
