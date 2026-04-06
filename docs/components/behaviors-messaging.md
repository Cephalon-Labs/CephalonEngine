# Cephalon.Behaviors.Messaging

`Cephalon.Behaviors.Messaging` is the messaging transport baseline for the Adaptive Behavior Topology (ABT).

## What it owns

- **InMemoryTransportBinding** — zero-infra bounded `Channel<T>` for tests and local dev
- **RabbitMqTransportBinding** — AMQP/RabbitMQ transport with lazy connect, retry, and dead-letter routing
- **KafkaTransportBinding** — Confluent Kafka transport with consumer group, manual offset commit
- **MessagingBehaviorBindingRegistry** — `FrozenDictionary` O(1) registry for all 3 bindings
- **Hosting** — `AddMessagingBehaviorBindings()` extension on `IBehaviorCollectionBuilder`

## Transport identifiers

| Transport ID | Binding | Infrastructure |
|---|---|---|
| `in-memory` | `InMemoryTransportBinding` | None (in-process `Channel<T>`) |
| `rabbitmq` | `RabbitMqTransportBinding` | RabbitMQ / AMQP 0-9-1 |
| `kafka` | `KafkaTransportBinding` | Apache Kafka / Confluent |

## Registration

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors
        .AddMessagingBehaviorBindings(
            rabbitMq: opts => opts.HostName = "rabbitmq.internal",
            kafka: opts => opts.BootstrapServers = "kafka:9092"
        )
    )
);
```

## Status

> Status: 🚧 In Progress (Sprint 22)

## Related components

- `Cephalon.Behaviors` — dispatcher, catalog, resolver (M1)
- `Cephalon.Behaviors.Http` — HTTP transport bindings (M2)
- `Cephalon.Behaviors.Patterns` — pattern execution strategies (M4, upcoming)
