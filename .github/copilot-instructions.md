# GitHub Copilot Instructions - TC Agro Sensor Ingest Service

## Project Context

**Name:** TC Agro Sensor Ingest Service
**Repository:** https://github.com/rdpresser/tc-agro-sensor-ingest-service
**Purpose:** Time-series sensor data ingestion for agricultural IoT monitoring platform
**Parent Project:** TC Agro Solutions - Phase 5 (Hackathon 8NETT FIAP)
**Deadline:** February 27, 2026
**Architecture:** Microservices on Kubernetes (k3d local, AKS production)

**Core Responsibilities:**

- Receiving sensor data (temperature, humidity, soil moisture, rainfall) via REST API
- Persisting time-series data to PostgreSQL with TimescaleDB (hypertables)
- Publishing integration events for Analytics Worker processing
- Providing optimized queries for latest readings with caching

---

## Technology Stack

### Backend

- **Language:** C# / .NET 10.0
- **Web Framework:** FastEndpoints (not MVC Controllers)
- **ORM:** Entity Framework Core 10.0
- **Database:** PostgreSQL + TimescaleDB (Azure PostgreSQL Flexible Server in production)
- **Cache:** Redis via FusionCache (Azure Redis Cache in production)
- **Messaging:** RabbitMQ local / Azure Service Bus production
- **Message Bus:** Wolverine + RabbitMQ
- **Validation:** FluentValidation

### Infrastructure

- **Local Orchestration:** k3d (Kubernetes)
- **Cloud Orchestration:** Azure Kubernetes Service (AKS)
- **GitOps:** ArgoCD
- **CI/CD:** GitHub Actions
- **Container Registry:** Docker Hub / Azure Container Registry

### Observability

- **Telemetry:** OpenTelemetry (OTLP, AspNetCore, Http, Runtime)
- **Metrics:** Prometheus
- **Logging:** Serilog (Console, Grafana Loki)
- **Tracing:** Distributed tracing with OpenTelemetry
- **APM:** Application Insights (production)

### Code Quality

- **Analyzers:** SonarAnalyzer.CSharp
- **Package Management:** Central Package Management (CPM)
- **Target Framework:** .NET 10.0 (enforced via Directory.Build.targets)
- **Nullable:** Enabled
- **Warnings as Errors:** Enabled

---

## Project Structure

```
src/
├── Adapters/
│   ├── Inbound/
│   │   └── TC.Agro.SensorIngest.Service/     # Web API Layer
│   │       ├── Endpoints/
│   │       │   └── Readings/
│   │       │       ├── CreateReadingEndpoint.cs
│   │       │       ├── CreateBatchReadingsEndpoint.cs
│   │       │       └── GetLatestReadingsEndpoint.cs
│   │       ├── Extensions/
│   │       ├── Telemetry/
│   │       └── Program.cs
│   │
│   └── Outbound/
│       └── TC.Agro.SensorIngest.Infrastructure/   # Data Access Layer
│           ├── Configurations/
│           │   └── SensorReadingConfiguration.cs
│           ├── Repositories/
│           │   ├── SensorReadingRepository.cs
│           │   └── SensorReadingReadStore.cs
│           ├── Messaging/
│           │   └── SensorIngestOutbox.cs
│           └── Persistence/
│               └── ApplicationDbContext.cs
│
└── Core/
    ├── TC.Agro.SensorIngest.Domain/         # Domain Layer
    │   ├── Aggregates/
    │   │   ├── SensorReadingAggregate.cs
    │   │   └── SensorReadingDomainErrors.cs
    │   └── Abstractions/
    │       └── DomainError.cs
    │
    └── TC.Agro.SensorIngest.Application/    # Application Layer
        ├── Abstractions/
        │   ├── AppConstants.cs
        │   ├── Mappers/
        │   └── Ports/
        │       ├── ISensorReadingRepository.cs
        │       └── ISensorReadingReadStore.cs
        ├── UseCases/
        │   ├── CreateReading/
        │   │   ├── CreateReadingCommand.cs
        │   │   ├── CreateReadingCommandHandler.cs
        │   │   ├── CreateReadingCommandValidator.cs
        │   │   ├── CreateReadingMapper.cs
        │   │   └── CreateReadingResponse.cs
        │   ├── CreateBatchReadings/
        │   │   ├── CreateBatchReadingsCommand.cs
        │   │   ├── CreateBatchReadingsCommandHandler.cs
        │   │   ├── CreateBatchReadingsCommandValidator.cs
        │   │   └── CreateBatchReadingsResponse.cs
        │   └── GetLatestReadings/
        │       ├── GetLatestReadingsQuery.cs
        │       ├── GetLatestReadingsQueryHandler.cs
        │       └── GetLatestReadingsResponse.cs
        └── IntegrationEvents/
            └── SensorIngestedIntegrationEvent.cs

test/
└── TC.Agro.SensorIngest.Tests/
```

---

## API Endpoints

### Sensor Readings

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| POST | `/sensors/readings` | Ingest single sensor reading | JWT (Admin, Producer, Sensor) |
| POST | `/sensors/batch` | Ingest batch of readings (max 1000) | JWT (Admin, Producer, Sensor) |
| GET | `/sensors/readings/latest` | Get latest readings (cached 60s) | JWT (Admin, Producer) |

### Request/Response Examples

**POST /sensors/readings**
```json
{
  "sensorId": "sensor-001",
  "plotId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "timestamp": "2026-01-28T10:30:00Z",
  "temperature": 28.5,
  "humidity": 65.2,
  "soilMoisture": 42.1,
  "rainfall": 0.0,
  "batteryLevel": 85.0
}
```

**Response 202 Accepted**
```json
{
  "readingId": "uuid",
  "sensorId": "sensor-001",
  "plotId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "timestamp": "2026-01-28T10:30:00Z",
  "message": "Reading received successfully"
}
```

**POST /sensors/batch**
```json
{
  "readings": [
    {
      "sensorId": "sensor-001",
      "plotId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "timestamp": "2026-01-28T10:30:00Z",
      "temperature": 28.5,
      "humidity": 65.2,
      "soilMoisture": 42.1,
      "rainfall": 0.0,
      "batteryLevel": 85.0
    }
  ]
}
```

**GET /sensors/readings/latest?sensorId=sensor-001&limit=10**
```json
{
  "readings": [
    {
      "id": "uuid",
      "sensorId": "sensor-001",
      "plotId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "time": "2026-01-28T10:30:00Z",
      "temperature": 28.5,
      "humidity": 65.2,
      "soilMoisture": 42.1,
      "rainfall": 0.0,
      "batteryLevel": 85.0
    }
  ]
}
```

---

## C# Coding Conventions

### Naming Conventions

- **Namespaces:** `TC.Agro.SensorIngest.{Layer}`
- **Classes:** PascalCase
- **Methods:** PascalCase
- **Local variables:** camelCase
- **Constants:** UPPER_CASE or PascalCase
- **Interfaces:** Prefix `I` (e.g., `ISensorReadingRepository`)

### FastEndpoints - Endpoint Structure

```csharp
public sealed class CreateReadingEndpoint : BaseApiEndpoint<CreateReadingCommand, CreateReadingResponse>
{
    public override void Configure()
    {
        Post("readings");
        RoutePrefixOverride("sensors");
        Roles("Admin", "Producer", "Sensor");

        Description(x => x
            .Produces<CreateReadingResponse>(202)
            .ProducesProblemDetails());

        Summary(s =>
        {
            s.Summary = "Ingests a single sensor reading.";
            s.Description = "Receives sensor data for a specific plot.";
        });
    }

    public override async Task HandleAsync(CreateReadingCommand req, CancellationToken ct)
    {
        var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

        if (response.IsSuccess)
        {
            await Send.AcceptedAsync(response.Value, cancellation: ct);
            return;
        }

        await MatchResultAsync(response, ct);
    }
}
```

### Domain Aggregate Pattern

```csharp
public sealed class SensorReadingAggregate : BaseAggregateRoot
{
    public string SensorId { get; private set; }
    public Guid PlotId { get; private set; }
    public DateTime Time { get; private set; }
    public double? Temperature { get; private set; }
    public double? Humidity { get; private set; }
    public double? SoilMoisture { get; private set; }
    public double? Rainfall { get; private set; }
    public double? BatteryLevel { get; private set; }

    public static Result<SensorReadingAggregate> Create(
        string sensorId, Guid plotId, DateTime time,
        double? temperature, double? humidity,
        double? soilMoisture, double? rainfall, double? batteryLevel)
    {
        // Validation logic
        // Returns Result with aggregate or validation errors
    }
}
```

### CQRS with Wolverine

```csharp
// Command
public sealed record CreateReadingCommand(
    string SensorId,
    Guid PlotId,
    DateTime Timestamp,
    double? Temperature,
    double? Humidity,
    double? SoilMoisture,
    double? Rainfall,
    double? BatteryLevel) : IBaseCommand<CreateReadingResponse>;

// Handler publishes integration event to Analytics Worker
await Outbox.EnqueueAsync(new SensorIngestedIntegrationEvent(...), ct);
```

---

## TimescaleDB Configuration

The `sensor_readings` table uses TimescaleDB hypertable for efficient time-series storage:

```sql
-- Create hypertable (in migration)
SELECT create_hypertable('sensor_readings', 'time');

-- Indexes for common queries
CREATE INDEX ix_sensor_readings_sensor_id_time ON sensor_readings (sensor_id, time);
CREATE INDEX ix_sensor_readings_plot_id_time ON sensor_readings (plot_id, time);
```

### Aggregation Queries

```sql
SELECT
  time_bucket('1 hour', time) AS hour,
  AVG(temperature) AS avg_temperature,
  MAX(temperature) AS max_temperature,
  MIN(temperature) AS min_temperature,
  AVG(humidity) AS avg_humidity,
  AVG(soil_moisture) AS avg_soil_moisture
FROM sensor_readings
WHERE sensor_id = 'sensor-001'
  AND time > now() - interval '7 days'
GROUP BY hour
ORDER BY hour DESC;
```

---

## Validation Rules

| Field | Validation |
|-------|------------|
| SensorId | Required, max 100 chars |
| PlotId | Required, valid GUID |
| Timestamp | Required, not in future |
| Temperature | Optional, -50 to 70 |
| Humidity | Optional, 0 to 100 |
| SoilMoisture | Optional, 0 to 100 |
| Rainfall | Optional, >= 0 |
| BatteryLevel | Optional, 0 to 100 |
| Metrics | At least one metric (temperature, humidity, soilMoisture, or rainfall) required |

---

## Inter-Service Communication

### Events Published
- `SensorIngestedIntegrationEvent` -> Analytics.Worker (for rule evaluation and alert generation)

### Dependencies
- **Identity.Api** - JWT token validation
- **Farm.Api** - Sensor and Plot registry (optional validation)

---

## Important Rules

### ALWAYS Do:

- Use **FastEndpoints** for APIs (not MVC Controllers)
- Use **async/await** in all I/O operations with `CancellationToken`
- Implement **structured logging** with Serilog
- Add **validation** with FluentValidation
- Use **DTOs** for requests/responses (never expose EF entities)
- Use **Redis caching** with FusionCache (60s TTL for latest readings)
- Follow **Pragmatic CQRS** with Wolverine (separate Commands/Queries)
- Use **Ardalis.Result** pattern for success/error handling
- Follow **Central Package Management** (no versions in .csproj)
- Write **unit tests** for business logic

### NEVER Do:

- Use MVC Controllers (use FastEndpoints)
- Expose domain entities directly in APIs
- Block on I/O operations
- Hardcode configuration values
- Log sensitive data
- Add package versions in .csproj (use Directory.Packages.props)
- Use TargetFramework other than net10.0

### Security:

- All endpoints require JWT authentication
- Validate input on all endpoints with FluentValidation
- Never log sensitive information
- Use HTTPS in production

### Performance:

- Use Redis cache for latest readings (TTL 60s)
- Use TimescaleDB hypertables for time-series data
- Implement batch processing for bulk ingestion (max 1000)
- Enable async operations throughout
- Lazy loading disabled in EF Core

---

## Useful Commands

### Run Service
```bash
dotnet run --project src/Adapters/Inbound/TC.Agro.SensorIngest.Service
```

### Run Tests
```bash
dotnet test
```

### EF Migrations
```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project src/Adapters/Outbound/TC.Agro.SensorIngest.Infrastructure \
  --startup-project src/Adapters/Inbound/TC.Agro.SensorIngest.Service

# Apply migrations
dotnet ef database update \
  --project src/Adapters/Outbound/TC.Agro.SensorIngest.Infrastructure \
  --startup-project src/Adapters/Inbound/TC.Agro.SensorIngest.Service
```

### Docker Commands
```bash
# Build Docker image
docker build -t localhost:5000/sensor-ingest-service:latest \
  -f src/Adapters/Inbound/TC.Agro.SensorIngest.Service/Dockerfile .

# Push to local registry
docker push localhost:5000/sensor-ingest-service:latest

# Run locally
docker run -p 8080:8080 --env-file .env sensor-ingest-service:latest
```

### Kubernetes Commands
```bash
# Apply manifests
kubectl apply -f k8s/

# Check pods
kubectl get pods -n agro

# View logs
kubectl logs -f <pod-name> -n agro

# Port forward
kubectl port-forward svc/sensor-ingest-service 8080:80 -n agro
```

---

## Documentation and Language Standards

### Chat Responses

- **Match user's language:** Respond in the same language the user initiated the chat
  - If user starts in **Portuguese** -> respond in Portuguese
  - If user starts in **English** -> respond in English
- **Consistent language:** Maintain the chat language throughout the conversation

### Code and Documentation Files

- **No automatic .md file creation:** Do not create markdown files unless explicitly requested
- **Use English for all content:** All files, code, comments, filenames must use English
  - Exception: Chat responses follow user's language

---

## References

### Technology Documentation

- **FastEndpoints:** https://fast-endpoints.com/
- **EF Core 10:** https://learn.microsoft.com/ef/core/
- **Wolverine:** https://wolverine.netlify.app/
- **TimescaleDB:** https://docs.timescale.com/
- **OpenTelemetry:** https://opentelemetry.io/docs/languages/net/
- **FusionCache:** https://github.com/ZiggyCreatures/FusionCache

### Project Resources

- **Repository:** https://github.com/rdpresser/tc-agro-sensor-ingest-service
- **Parent Project:** TC Agro Solutions (Hackathon Phase 5)

---

> **Last update:** January 2026
> **Version:** 1.0
> Use these instructions to guide code generation in the TC Agro Sensor Ingest Service project.
