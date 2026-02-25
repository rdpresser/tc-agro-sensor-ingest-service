# Architecture

## Hexagonal Architecture Overview

The Sensor Ingest Service implements Hexagonal Architecture (Ports & Adapters) to isolate domain logic from infrastructure concerns. The Core layers (Domain and Application) define business rules and port interfaces, while Adapter layers provide concrete implementations.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Inbound Adapters                             │
│  ┌──────────────┐  ┌──────────────────┐  ┌────────────────────┐    │
│  │ FastEndpoints │  │ Wolverine Broker │  │ Quartz Scheduler   │    │
│  │ (REST API)    │  │ (Event Handlers) │  │ (Simulated Jobs)   │    │
│  └──────┬───────┘  └────────┬─────────┘  └─────────┬──────────┘    │
│         │                   │                       │               │
│─────────┼───────────────────┼───────────────────────┼───────────────│
│         ▼                   ▼                       ▼               │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    Application Layer                         │    │
│  │  Command Handlers ─── Query Handlers ─── Snapshot Handlers  │    │
│  │  Validators ────────── Mappers ─────────── Abstractions     │    │
│  │                                                              │    │
│  │  Ports (Interfaces):                                         │    │
│  │  - ISensorReadingRepository   - ISensorReadingReadStore     │    │
│  │  - ISensorSnapshotStore       - IOwnerSnapshotStore         │    │
│  │  - ITransactionalOutbox       - IUnitOfWork                 │    │
│  └─────────────────────────┬───────────────────────────────────┘    │
│                             │                                       │
│  ┌──────────────────────────▼──────────────────────────────────┐    │
│  │                      Domain Layer                            │    │
│  │  SensorReadingAggregate ── SensorSnapshot ── OwnerSnapshot  │    │
│  │  AlertStatus ── AlertSeverity ── SensorStatus               │    │
│  │  Domain Events (SensorReadingCreatedDomainEvent)            │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                             │                                       │
│─────────────────────────────┼───────────────────────────────────────│
│                             ▼                                       │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    Outbound Adapters                         │    │
│  │  ┌─────────────┐ ┌────────┐ ┌──────────┐ ┌──────────────┐  │    │
│  │  │ EF Core +   │ │ Redis  │ │ RabbitMQ │ │ SignalR Hub   │  │    │
│  │  │ TimescaleDB │ │ Cache  │ │ Outbox   │ │ Notifications │  │    │
│  │  └─────────────┘ └────────┘ └──────────┘ └──────────────┘  │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

### Domain Layer (`TC.Agro.SensorIngest.Domain`)

The innermost layer, with zero dependencies on external frameworks (except SharedKernel base classes).

- **Aggregates**: `SensorReadingAggregate` — encapsulates sensor reading data with factory validation (range checks for temperature, humidity, soil moisture, rainfall, battery). Emits `SensorReadingCreatedDomainEvent` on creation.
- **Snapshots**: `SensorSnapshot` and `OwnerSnapshot` — read-optimized projections of data from Farm and Identity services. Maintained via integration event handlers.
- **Value Objects**: `AlertStatus`, `AlertSeverity`, `SensorStatus` — sealed records with factory methods and implicit string conversion.

### Application Layer (`TC.Agro.SensorIngest.Application`)

Orchestrates use cases through CQRS commands and queries.

- **Command Handlers**: `CreateReadingCommandHandler`, `CreateBatchReadingsCommandHandler` — validate, persist, and publish integration events atomically via the Transactional Outbox.
- **Query Handlers**: `GetLatestReadingsQueryHandler`, `GetReadingsHistoryQueryHandler` — read from optimized read stores with caching.
- **Snapshot Handlers**: `SensorSnapshotHandler`, `OwnerSnapshotHandler` — consume integration events from Farm and Identity services to maintain local snapshots.
- **Ports (Interfaces)**: Define contracts for repositories, read stores, outbox, and external dependencies.
- **Mappers**: Static mapping classes for converting between commands, aggregates, and integration events.

### Inbound Adapters (`TC.Agro.SensorIngest.Service`)

- **FastEndpoints**: REST API endpoints with FluentValidation, role-based authorization, and Swagger documentation.
- **Wolverine Handlers**: Message broker integration for consuming integration events from RabbitMQ.
- **Quartz.NET Jobs**: `SimulatedSensorReadingsJob` generates sensor readings on a schedule, integrating with Open-Meteo weather API for realistic data.
- **SignalR Hub**: `SensorHub` pushes real-time sensor updates to connected dashboard clients.

### Outbound Adapters (`TC.Agro.SensorIngest.Infrastructure`)

- **EF Core**: `ApplicationDbContext` with TimescaleDB hypertable configurations.
- **Repositories**: `SensorReadingRepository`, snapshot stores implementing port interfaces.
- **Redis**: L2 cache backing for FusionCache.
- **RabbitMQ**: Wolverine-managed Transactional Outbox for reliable event publishing.

## Data Flow Diagrams

### HTTP Sensor Reading Ingestion

```
Client ──POST /readings/batch──▶ FastEndpoint
                                     │
                              FluentValidation
                                     │
                             Command Handler
                                     │
                    ┌────────────────┼────────────────┐
                    ▼                ▼                 ▼
              Validate         Create          Lookup Active
              Aggregate     Aggregates         Sensors (Snapshot)
                    │                │
                    ▼                ▼
              Repository        Transactional
             AddRangeAsync       Outbox
                    │           EnqueueAsync
                    ▼                │
              TimescaleDB           ▼
              (Persist)         RabbitMQ
                               (Publish SensorIngestedIntegrationEvent)
```

### Message Broker Event Flow

```
Identity Service                     Farm Service
      │                                    │
      ▼                                    ▼
  UserCreated/Updated/              SensorRegistered/
  Deactivated Event                 StatusChanged/Deactivated
      │                                    │
      ▼                                    ▼
  RabbitMQ ──────────────────────▶ Wolverine Handler
                                         │
                              OwnerSnapshotHandler /
                              SensorSnapshotHandler
                                         │
                                    ┌────┼────┐
                                    ▼         ▼
                              Snapshot     UnitOfWork
                               Store     SaveChanges
                                    │
                                    ▼
                              PostgreSQL
```

### Simulated Sensor Reading Pipeline

```
Quartz Scheduler (configurable interval)
         │
         ▼
SimulatedSensorReadingsJob
         │
    ┌────┼──────────────┐
    ▼                    ▼
Get Active         Get Weather
Sensors            (Open-Meteo API)
    │                    │
    └────────┬───────────┘
             ▼
    Generate Readings
    (with weather variance)
             │
    ┌────────┼────────────────┐
    ▼        ▼                ▼
Repository  Message Bus    SignalR Hub
AddRange    Publish        NotifyAsync
    │        │                │
    ▼        ▼                ▼
TimescaleDB RabbitMQ      Dashboard
                          (Real-time)
```

## Caching Strategy

The service uses **FusionCache** with a two-layer architecture:

- **L1 (In-Memory)**: Fast local cache for frequently accessed queries. Default duration configured per query type.
- **L2 (Redis)**: Distributed cache shared across service instances. Provides consistency in multi-instance deployments.

Cache invalidation is handled via **cache tags**:
- Commands implement `IInvalidateCache` with tags like `Readings`, `Dashboard`
- Queries implement `ICachedQuery<T>` with matching tags
- When a command executes, all cached queries with overlapping tags are invalidated

## Snapshot Pattern

Cross-service data is maintained locally via the **Snapshot Pattern**:

1. **Purpose**: Avoid runtime cross-service calls by keeping local read-optimized copies of external data (owners from Identity, sensors from Farm).
2. **Mechanism**: Integration events trigger snapshot handlers that create/update/delete local projections.
3. **Idempotency**: Handlers check for existing snapshots before creating to handle duplicate events.
4. **Consistency**: Eventually consistent — snapshots are updated asynchronously when events arrive.

## Transactional Outbox

The service uses Wolverine's **Transactional Outbox** pattern to ensure atomicity between data persistence and event publishing:

1. Command handler persists aggregates to the database
2. Integration events are enqueued to the outbox (same transaction)
3. Wolverine's background agent delivers outbox messages to RabbitMQ
4. If the database transaction rolls back, outbox messages are also rolled back

This guarantees that events are published if and only if the data is persisted.
