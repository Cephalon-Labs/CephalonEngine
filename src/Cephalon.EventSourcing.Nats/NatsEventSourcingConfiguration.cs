using System.Text;

namespace Cephalon.EventSourcing.Nats;

internal static class NatsEventSourcingConfiguration
{
    internal const string SnapshotKeyPrefix = "snapshots/";

    internal static string SnapshotKey(string streamId, string stateType) =>
        string.Concat(SnapshotKeyPrefix, streamId, "/", ToKeyComponent(stateType));

    internal static string CreateStateTypeKey<TState>()
    {
        var type = typeof(TState);
        var assemblyName = type.Assembly.GetName().Name;
        var typeName = type.FullName ?? type.Name;

        return string.IsNullOrWhiteSpace(assemblyName)
            ? typeName
            : string.Concat(assemblyName, ":", typeName);
    }

    private static string ToKeyComponent(string value)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        return encoded.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
