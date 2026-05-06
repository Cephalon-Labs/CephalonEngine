# Cephalon.Data.MySql.SciSharpReplication

> **Maturity:** `M0` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.MySql.SciSharpReplication` installs the current SciSharp-backed binlog transport adapter for the MySQL provider-native CDC pack.

## What it owns

- registers the runtime `IMySqlBinlogTransport` implementation consumed by `Cephalon.Data.MySql` when MySQL binlog captures are configured
- keeps the `SciSharp.MySQL.Replication` and `MySql.Data` dependency chain out of the core MySQL data package
- isolates the current non-public SciSharp adapter path behind an explicitly named optional package until a NuGet release exposes a public start-position API or Cephalon ships a first-party binlog protocol transport
- preserves Cephalon-managed binlog checkpoint ownership for hosts that deliberately opt into the current adapter

## Main surfaces

- `Registration/MySqlSciSharpReplicationEngineBuilderExtensions.cs`
- `Services/SciSharpMySqlBinlogTransport.cs`

## Source structure

- `Registration` -> public engine-builder registration
- `Services` -> internal SciSharp-backed binlog transport implementation

## Registration

```csharp
services.AddCephalon(configuration, engine =>
{
    engine
        .AddData()
        .AddMySqlData(
            connectionString: "Server=mysql;User ID=replica;Password=secret;Database=orders",
            databaseName: "orders",
            configure: options =>
            {
                options.CdcCaptures.Add(new MySqlBinlogCaptureOptions
                {
                    Id = "orders-cdc",
                    SourceModuleId = "orders",
                    TableName = "Orders",
                    ServerId = 42,
                    OutboxId = "orders-outbox",
                    ChannelId = "orders",
                    MessageType = "orders.changed"
                });
            })
        .AddSciSharpMySqlBinlogReplication();
});
```

## How it fits

`Cephalon.Data.MySql` now owns the MySQL CDC contract, descriptors, hosted execution, checkpoint metadata model, and failure projection. This package owns only the concrete SciSharp transport adapter. If a host configures MySQL CDC without installing an adapter, the core package fails fast with `failureKind = "binlog-transport-adapter-missing"` instead of hiding an implicit third-party dependency in the main pack.

The split is deliberate. The NuGet release currently available for `SciSharp.MySQL.Replication` does not expose Cephalon's required start-position overload, while upstream source has started moving in that direction. Keeping the adapter optional lets the main MySQL package remain clean for deployment-mode governance without pretending the current SciSharp reflection path is broadly trim, AOT, or single-file friendly.

## Deployment-mode posture

This package is not a trim, native AOT, or single-file support claim. The current implementation still carries the known third-party reflective adapter path. It exists so teams can make a conscious runtime tradeoff while Cephalon keeps the safer package boundary ready for a future public upstream API or first-party transport.

## Related docs

- [Cephalon.Data.MySql](data-mysql.md)
- [Deployment-mode support](../deployment-mode-support.md)
- [Compatibility](../compatibility.md)
