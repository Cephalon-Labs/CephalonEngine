# Cephalon.Data.MySql.SciSharpReplication

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data.MySql.SciSharpReplication)
## Namespaces

- `Cephalon.Data.MySql.SciSharpReplication.Registration`

<a id="namespace-cephalon-data-mysql-scisharpreplication-registration"></a>

## Namespace Cephalon.Data.MySql.SciSharpReplication.Registration

<a id="type-cephalon-data-mysql-scisharpreplication-registration-mysqlscisharpreplicationenginebuilderextensions"></a>

### `MySqlSciSharpReplicationEngineBuilderExtensions`

Registers the SciSharp-backed MySQL binlog transport adapter for `EngineBuilder`.

Remarks: This package intentionally isolates the current `SciSharp.MySQL.Replication` adapter path from the core MySQL data pack so trim, AOT, and single-file governance can reason about the risk at package granularity.

#### Declaration
```csharp
public static class MySqlSciSharpReplicationEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-mysql-scisharpreplication-registration-mysqlscisharpreplicationenginebuilderextensions-addscisharpmysqlbinlogreplication-cephalon-engine-composition-enginebuilder"></a>

##### `AddSciSharpMySqlBinlogReplication`

```csharp
EngineBuilder AddSciSharpMySqlBinlogReplication(this EngineBuilder builder)
```

Adds the SciSharp-backed MySQL binlog transport used by configured MySQL CDC captures.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
