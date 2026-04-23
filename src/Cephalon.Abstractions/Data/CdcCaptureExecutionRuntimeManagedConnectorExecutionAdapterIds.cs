namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable provider execution-adapter identifiers used by managed-connector execution-adapter answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds
{
    /// <summary>
    /// No provider execution adapter currently applies.
    /// </summary>
    public const string None = "none";

    /// <summary>
    /// The Debezium or Kafka Connect REST management adapter currently applies.
    /// </summary>
    public const string DebeziumKafkaConnectRest = "debezium-kafka-connect-rest";
}
