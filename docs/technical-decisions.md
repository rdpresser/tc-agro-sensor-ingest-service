# Technical Decisions

Architectural Decision Records (ADRs) for the Sensor Ingest Service.

---

## ADR-001: TimescaleDB for Time-Series Data

**Status**: Accepted

**Context**: Sensor readings are time-series data with high write throughput and time-range queries. Standard PostgreSQL tables degrade in performance as data volume grows into millions of rows.

**Decision**: Use TimescaleDB hypertables on top of PostgreSQL for storing sensor readings.

**Consequences**:
- Automatic partitioning by time interval (chunks) improves write and query performance
- `time_bucket()` function enables efficient aggregation queries (hourly, daily averages)
- Transparent to EF Core — hypertable creation is handled via migrations
- Retention policies can be applied to automatically drop old chunks
- Requires TimescaleDB extension installed on PostgreSQL instances

---

## ADR-002: Snapshot Pattern for Cross-Service Data

**Status**: Accepted

**Context**: The Sensor Ingest Service needs to validate that sensors exist (from Farm Service) and identify owners (from Identity Service) during reading ingestion. Synchronous HTTP calls would create runtime coupling and latency.

**Decision**: Maintain local read-optimized snapshots (`SensorSnapshot`, `OwnerSnapshot`) updated via integration events.

**Consequences**:
- No runtime dependency on Farm or Identity services during request processing
- Eventually consistent — brief window where snapshot may be stale after upstream changes
- Idempotent handlers prevent duplicate snapshot creation from redelivered events
- Additional storage for snapshot data in the local database
- Requires handling of all relevant lifecycle events (created, updated, deactivated)

---

## ADR-003: Transactional Outbox via Wolverine

**Status**: Accepted

**Context**: After persisting sensor readings, integration events must be published to RabbitMQ for downstream processing (analytics worker). Without atomicity, there is a risk of data being persisted without the event being published (or vice versa).

**Decision**: Use Wolverine's built-in Transactional Outbox with PostgreSQL durability.

**Consequences**:
- Atomicity between data persistence and event publishing — both succeed or both fail
- Wolverine manages outbox delivery, retries, and dead-letter handling
- Events may be delivered more than once (at-least-once semantics); consumers must be idempotent
- Slight delay between persistence and event delivery (background agent polling)
- PostgreSQL stores outbox messages, adding some write overhead

---

## ADR-004: FusionCache (L1 Memory + L2 Redis)

**Status**: Accepted

**Context**: Query endpoints (latest readings, history) are read-heavy and benefit from caching. The service may run as multiple instances behind a load balancer, requiring cache consistency.

**Decision**: Use FusionCache with in-memory L1 cache and Redis L2 cache.

**Consequences**:
- L1 provides sub-millisecond reads for frequently accessed data
- L2 ensures all instances see the same cached data
- Tag-based invalidation allows commands to bust related query caches
- Cache stampede protection via FusionCache's built-in mechanisms
- Redis dependency for distributed scenarios (graceful degradation to L1-only if Redis unavailable)

---

## ADR-005: SignalR for Real-Time Dashboard Updates

**Status**: Accepted

**Context**: The dashboard POC requires real-time sensor data updates without polling. WebSocket-based communication provides the lowest latency for pushing updates.

**Decision**: Use ASP.NET SignalR with a dedicated hub (`SensorHub`) at `/dashboard/sensorshub`.

**Consequences**:
- Real-time push of sensor readings to connected clients
- JWT authentication via query string parameter for WebSocket handshake
- Requires sticky sessions or Redis backplane for multi-instance deployments
- Hub notifier (`ISensorHubNotifier`) is called after each reading ingestion
- Adds SignalR as a dependency; clients must implement SignalR protocol

---

## ADR-006: Quartz.NET for Scheduled Sensor Simulation

**Status**: Accepted

**Context**: For demonstration and development purposes, the service needs to generate simulated sensor readings at configurable intervals. The simulation should integrate with real weather data when available.

**Decision**: Use Quartz.NET with `SimulatedSensorReadingsJob` that runs on a configurable schedule.

**Consequences**:
- Configurable interval via `SensorReadingsJobOptions.IntervalSeconds`
- Integrates with Open-Meteo weather API for realistic data; falls back to Bogus-generated data
- `[DisallowConcurrentExecution]` prevents overlapping job runs
- Job creates readings for all active sensors and publishes events through the same pipeline as real readings
- Can be disabled via configuration in production environments

---

## ADR-007: BaseCommandHandler 6-Step Pipeline

**Status**: Accepted

**Context**: Command handlers follow a consistent pattern: validate, execute, persist, publish events, invalidate cache, return response. Duplicating this pipeline in every handler leads to boilerplate.

**Decision**: Use the SharedKernel `BaseCommandHandler<TCommand, TResponse>` and `BaseHandler` abstractions that provide the standard execution pipeline.

**Consequences**:
- Handlers only implement `ExecuteAsync` with business logic
- Cache invalidation and logging are handled by the base class/post-processors
- Consistent behavior across all commands (validation, error handling)
- Queries use `BaseQueryHandler` with caching pre/post-processors
- Reduces code duplication but requires understanding the base class pipeline
