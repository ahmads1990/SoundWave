# MassTransit & RabbitMQ Architecture Guide — SoundWave

This guide documents the messaging topology, patterns, and design decisions in **SoundWave** using **MassTransit** and **RabbitMQ**.

---

## 1. Architectural Evolution

| Capability | Raw RabbitMQ (Legacy) | MassTransit (Current) |
|---|---|---|
| **Topology Declaration** | Hand-declared exchanges, queues, bindings, and DLXs per worker | Auto-declared by MassTransit from C# consumer types |
| **Routing** | String routing keys with manual dispatchers (`switch`/dictionaries) | Type-safe CLR type routing via fanout exchanges |
| **Transactional Outbox** | Custom `OutboxMessages` table + manual background polling worker | Built-in `MassTransit.EntityFrameworkCore` transactional outbox |
| **Consumer Lifecycle** | Custom `BackgroundService` with manual loop, channel pooling & QoS | Hosted bus with auto-managed concurrency, scopes & retries |
| **Error Handling** | Manual DLX/DLQ queue configuration | Built-in exponential backoff retry + automatic `_error` queues |
| **Deduplication** | None (at-least-once with manual idempotency) | Built-in `InboxState` consumer deduplication |

---

## 2. Type-Based Topology & Exchange-to-Exchange (E2E) Routing

MassTransit uses **C# Types as the routing system**. Every integration event record defines its own fanout exchange in RabbitMQ.

```mermaid
flowchart TD
    subgraph Publisher ["Catalog Module (Command Handler)"]
        Cmd[ApproveArtistAccountCommandHandler] -->|publishEndpoint.Publish| Outbox[(EF Core Outbox)]
        Outbox -.->|MassTransit Outbox Delivery| ExType["Exchange: SoundWave.Catalog.Contracts.IntegrationEvents:ArtistApplicationApprovedEvent (fanout)"]
    end

    subgraph Broker ["RabbitMQ Broker"]
        ExType -->|E2E Binding| ExConsumer["Exchange: artist-application-approved-consumer (fanout)"]
        ExConsumer --> Q["Queue: artist-application-approved-consumer"]
    end

    subgraph Consumer ["Identity Module"]
        Q --> C["ArtistApplicationApprovedConsumer.Consume()"]
        C --> DB[(IdentityDbContext: User.Role = Artist)]
        C --> Hangfire[Hangfire: Enqueue Welcome Email]
    end
```

### Why 2 Exchanges per Consumer? (E2E Binding)
1. **Message Exchange (`ArtistApplicationApprovedEvent`):** Represents *what happened*. Any number of independent microservices/modules can bind to it.
2. **Consumer Exchange (`artist-application-approved-consumer`):** Represents the *inbox* for that consumer. Multiple event types can be funneled into one consumer inbox without changing publisher code.

---

## 3. MassTransit Transactional Outbox Flow

When a command handler publishes an event, MassTransit intercepts the call and writes an `OutboxMessage` entity into the same `CatalogDbContext` transaction.

```mermaid
sequenceDiagram
    autonumber
    participant Handler as ApproveArtistAccountCommandHandler
    participant EF as CatalogDbContext
    participant SQL as SQL Server (SharedKernel schema)
    participant Delivery as MassTransit Outbox Service
    participant RMQ as RabbitMQ

    Handler->>EF: 1. Add(Artist)
    Handler->>EF: 2. publishEndpoint.Publish(ArtistApplicationApprovedEvent)
    Note over EF: OutboxMessage entity staged in CatalogDbContext ChangeTracker
    Handler->>EF: 3. await dbContext.SaveChangesAsync()
    EF->>SQL: 4. COMMIT TRANSACTION (Artist + OutboxMessage saved atomically)
    
    loop Background Delivery
        Delivery->>SQL: 5. Read unpublished OutboxMessages
        Delivery->>RMQ: 6. Publish to RabbitMQ exchange
        Delivery->>SQL: 7. Mark OutboxMessage delivered / remove from queue
    end
```

### Outbox Tables in SQL Server
All MassTransit outbox/inbox tables are scoped cleanly to the `SharedKernel` schema:
* `[SharedKernel].[OutboxMessages]` — pending messages awaiting delivery to RabbitMQ.
* `[SharedKernel].[OutboxState]` — outbox delivery coordination and locking.
* `[SharedKernel].[InboxState]` — consumer idempotency and message deduplication.

---

## 4. Modular Consumer Registration

Each module encapsulates its own consumers and outbox configuration:

```csharp
// Program.cs — Composition Root
builder.Services.AddMassTransitBus(builder.Configuration, x =>
{
    // Module Outbox configuration
    CatalogModule.ConfigureMassTransitOutbox(x);

    // Module Consumer registrations
    IdentityModule.RegisterConsumers(x);
});
```

### Inside `IdentityModule.cs`:
```csharp
public static void RegisterConsumers(IBusRegistrationConfigurator configurator)
{
    configurator.AddConsumer<ArtistApplicationApprovedConsumer>();
    configurator.AddConsumer<ArtistApplicationRejectedConsumer>();
    configurator.AddConsumer<ArtistApplicationSubmittedConsumer>();
}
```

---

## 5. Resiliency & Fault Tolerance

### Exponential Backoff Retry Policy
Configured globally across all consumers in `SharedKernelExtensions.cs`:
```csharp
cfg.UseMessageRetry(r => r.Exponential(
    retryLimit: 5,
    minInterval: TimeSpan.FromSeconds(1),
    maxInterval: TimeSpan.FromSeconds(30),
    intervalDelta: TimeSpan.FromSeconds(2)
));
```

### Poison Messages & `_error` Queues
If all 5 retry attempts fail:
1. MassTransit captures the complete exception, stack trace, and host headers into the message envelope.
2. The message is automatically moved to the consumer's error queue (e.g. `artist-application-approved-consumer_error`).
3. The main queue continues processing other messages without blocking or CPU thrashing.