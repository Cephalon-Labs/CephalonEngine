namespace Cephalon.Abstractions.Capabilities;

public sealed class Capability
{
    public Capability(
        string key,
        string displayName,
        string description,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Capability key is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Capability display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Capability description is required.", nameof(description));
        }

        Key = key.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string Key { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
