# Outbox to inbox with an idempotent consumer

```mermaid
sequenceDiagram
    autonumber
    participant Producer as Producer handler
    participant PDB as Producer DbContext
    participant Worker as OutboxDispatcher (per module)
    participant CDB as Consumer DbContext

    Note over Producer,PDB: The business change and the event commit together
    Producer->>PDB: BEGIN
    Producer->>PDB: write the business rows
    Producer->>PDB: insert outbox_messages (type, payload, organization_id)
    Producer->>PDB: COMMIT
    Note over Producer,PDB: No external I/O inside this transaction

    Note over Worker,CDB: A periodic pass, one row at a time
    Worker->>Worker: PeriodicTimer (TimeProvider) fires
    Worker->>PDB: BEGIN, claim one due row with FOR UPDATE SKIP LOCKED
    Worker->>PDB: SAVEPOINT outbox_claim
    Worker->>Worker: resolve the event type from the row
    alt producer and consumer share a DbContext
        Worker->>PDB: inbox row and consumer work in the same transaction
    else different DbContexts
        Worker->>CDB: BEGIN
        Worker->>CDB: inbox_messages exists for (consumer, event_id)?
        alt already applied
            CDB-->>Worker: yes, skip the consumer
        else first delivery
            Worker->>CDB: insert inbox_messages + consumer work
            Worker->>CDB: COMMIT
        end
    end
    Worker->>PDB: processed_at = now, COMMIT (releases the row lock)
    Note over Worker,PDB: A crash between the two commits redelivers the row, and the inbox primary key turns the replay into a no-op

    Note over Worker,PDB: Failure path
    Worker->>PDB: ROLLBACK TO SAVEPOINT outbox_claim
    Worker->>PDB: attempts = attempts + 1, last_error masked
    alt attempts below the cap
        Worker->>PDB: next_attempt_at = now + exponential backoff with jitter, COMMIT
    else cap reached
        Worker->>PDB: dead_lettered_at = now, COMMIT, no further retry
    end
```

Reading guide: the outbox row is written in the same transaction as the business change, so either both exist or neither does. One dispatcher runs per module database as a `BackgroundService`; it claims due rows with `FOR UPDATE SKIP LOCKED`, so two instances never deliver the same row, and it holds the row lock until the outcome is recorded. A consumer that owns another database runs in its own transaction: it checks the inbox primary key `(consumer, event_id)`, inserts the inbox row and does its work, and commits before the producer row is marked processed. A crash between those two commits redelivers the event, and the inbox key makes the second delivery a no-op; delivery is at-least-once, and because consumers only do database work the result is effectively once. Failure is recorded in the claim transaction itself after rolling back to the savepoint, with exponential backoff and jitter, and after the attempt cap the row becomes a dead letter with a masked last error. The same transaction is used when producer and consumer share a DbContext, which is why the diagram has both branches. Consumers never call an external service, so a retry cannot duplicate a side effect outside the database.

Verified against: `backend/src/Parley.SharedKernel/Outbox/Dispatching/OutboxDispatcher.cs`, `Outbox/Publishing/DbContextOutbox.cs`, `Outbox/Messages/**`, `Outbox/Consumers/**`, and the `AddOutbox` and `AddIntegrationEventConsumer` registrations in every module `Module.cs`.





