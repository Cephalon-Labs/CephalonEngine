namespace Cephalon.EventSourcing.Neo4j;

/// <summary>
/// Holds schema constants and the Cypher statement that bootstraps the event-store constraint in Neo4j.
/// </summary>
public static class Neo4jEventSourcingConfiguration
{
    /// <summary>
    /// The name of the node key constraint that enforces uniqueness on <c>(streamId, streamVersion)</c> pairs.
    /// </summary>
    public const string ConstraintName = "cephalon_event_stream_version";

    /// <summary>
    /// The Cypher statement that creates the compound node key constraint on <c>(streamId, streamVersion)</c>.
    /// <c>IS NODE KEY</c> enforces both uniqueness and existence, providing the primary optimistic concurrency guard.
    /// </summary>
    public const string CreateConstraintCypher =
        "CREATE CONSTRAINT cephalon_event_stream_version IF NOT EXISTS " +
        "FOR (e:Event) REQUIRE (e.streamId, e.streamVersion) IS NODE KEY";
}
