# Sensor Ingest Servicer 🌾

[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![C# Version](https://img.shields.io/badge/C%23-14.0-239120)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/rdpresser/tc-agro-sensor-ingest-service)
[![Tests](https://img.shields.io/badge/tests-241%20passing-brightgreen)](https://github.com/rdpresser/tc-agro-sensor-ingest-service)
[![Coverage](https://img.shields.io/badge/coverage-94%25-brightgreen)](https://github.com/rdpresser/tc-agro-sensor-ingest-service)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

> High-throughput sensor data ingestion microservice for the TC Agro Solutions agricultural IoT platform. Receives sensor readings via REST API, validates and persists data to TimescaleDB, publishes integration events for downstream analytics, and pushes real-time updates via SignalR.


## Architecture

The service follows **Hexagonal Architecture** (Ports & Adapters) with a clear separation between Core business logic and infrastructure concerns:

```
                    ┌──────────────────────────────────────────┐
                    │            Inbound Adapters               │
                    │  FastEndpoints │ Wolverine │ Quartz Jobs  │
                    └───────────────┬──────────────────────────┘
                                    │
                    ┌───────────────▼──────────────────────────┐
                    │              Application                   │
                    │  Command Handlers │ Query Handlers         │
                    │  Mappers │ Validators │ Snapshot Handlers  │
                    └───────────────┬──────────────────────────┘
                                    │
                    ┌───────────────▼──────────────────────────┐
                    │               Domain                      │
                    │  Aggregates │ Snapshots │ Value Objects   │
                    └───────────────┬──────────────────────────┘
                                    │
                    ┌───────────────▼──────────────────────────┐
                    │           Outbound Adapters                │
                    │  EF Core │ Redis │ RabbitMQ │ SignalR     │
                    └──────────────────────────────────────────┘
```

## Tech Stack

| Component        | Technology                                         |
| ---------------- | -------------------------------------------------- |
| Runtime          | .NET 10, C# 13                                     |
| API Framework    | FastEndpoints                                      |
| Database         | PostgreSQL + TimescaleDB (hypertables)              |
| ORM              | Entity Framework Core 10                           |
| Messaging        | RabbitMQ via Wolverine (Transactional Outbox)       |
| Cache            | FusionCache (L1 in-memory + L2 Redis)              |
| Real-time        | SignalR (WebSocket hub)                             |
| Scheduling       | Quartz.NET                                         |
| Validation       | FluentValidation                                   |
| Observability    | OpenTelemetry, Serilog, Prometheus                 |
| Testing          | xUnit v3, Shouldly, FakeItEasy                     |

## API Endpoints

| Method | Route                              | Description                    | Roles                   |
| ------ | ---------------------------------- | ------------------------------ | ----------------------- |
| POST   | `/readings`                        | Ingest a single sensor reading | Admin, Producer, Sensor |
| POST   | `/readings/batch`                  | Ingest batch sensor readings   | Admin, Producer, Sensor |
| GET    | `/readings/latest`                 | Get latest readings (filtered) | Admin, Producer         |
| GET    | `/sensors/{sensorId}/readings`     | Get historical readings        | Admin, Producer         |
| GET    | `/dashboard/latest`                | Get latest dashboard data      | Admin, Producer         |

**SignalR Hub:** `/dashboard/sensorshub` (JWT-authenticated, real-time sensor updates)

## Messaging

### Events Published

| Event                              | Trigger                          | Consumers               |
| ---------------------------------- | -------------------------------- | ----------------------- |
| `SensorIngestedIntegrationEvent`   | New reading ingested             | Analytics Worker        |

### Events Consumed

| Event                                            | Source           | Action                         |
| ------------------------------------------------ | ---------------- | ------------------------------ |
| `UserCreatedIntegrationEvent`                    | Identity Service | Creates OwnerSnapshot          |
| `UserUpdatedIntegrationEvent`                    | Identity Service | Updates OwnerSnapshot          |
| `UserDeactivatedIntegrationEvent`                | Identity Service | Soft-deletes OwnerSnapshot     |
| `SensorRegisteredIntegrationEvent`               | Farm Service     | Creates SensorSnapshot         |
| `SensorOperationalStatusChangedIntegrationEvent` | Farm Service     | Updates SensorSnapshot         |
| `SensorDeactivatedIntegrationEvent`              | Farm Service     | Soft-deletes SensorSnapshot    |

## Configuration

Key environment variables and configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=agro_sensor_ingest;...",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "...",
    "Issuer": "agro-identity-service",
    "Audience": "agro-services"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672
  },
  "SensorReadingsJob": {
    "IntervalSeconds": 30,
    "Enabled": true
  },
  "WeatherProvider": {
    "Latitude": -23.55,
    "Longitude": -46.63
  }
}
```

## Running Locally

### Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL, Redis, RabbitMQ)

### Start Dependencies

```bash
docker compose up -d postgres redis rabbitmq
```

### Run the Service

```bash
cd src/Adapters/Inbound/TC.Agro.SensorIngest.Service
dotnet run
```

The service starts at `http://localhost:5003` with Swagger at `/swagger`.

### Run via Docker

```bash
docker build -t agro-sensor-ingest .
docker run -p 5003:8080 agro-sensor-ingest
```

## Testing

### Run All Tests

```bash
# If .NET 10 SDK is installed locally:
dotnet test

# Using Docker (recommended if local SDK is .NET 9):
docker run --rm -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:10.0 dotnet test --verbosity normal
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~CreateBatchReadingsCommandHandlerTests"
```

### Test Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Project Structure

```
sensor-ingest-service/
├── src/
│   ├── Adapters/
│   │   ├── Inbound/TC.Agro.SensorIngest.Service/
│   │   │   ├── Endpoints/          # FastEndpoints (REST API)
│   │   │   ├── Hubs/               # SignalR hub for real-time updates
│   │   │   ├── Jobs/               # Quartz.NET scheduled jobs
│   │   │   ├── Providers/          # External data providers (weather)
│   │   │   ├── Services/           # Support services (hub notifier)
│   │   │   ├── Middleware/          # Telemetry middleware
│   │   │   ├── Extensions/         # DI service registration
│   │   │   └── Program.cs
│   │   └── Outbound/TC.Agro.SensorIngest.Infrastructure/
│   │       ├── ApplicationDbContext.cs
│   │       ├── Configurations/     # EF Core entity configurations
│   │       ├── Repositories/       # Repository implementations
│   │       └── Migrations/         # EF Core migrations
│   └── Core/
│       ├── TC.Agro.SensorIngest.Application/
│       │   ├── UseCases/           # CQRS commands & queries
│       │   ├── MessageBrokerHandlers/  # Integration event handlers
│       │   └── Abstractions/       # Ports, mappers, constants
│       └── TC.Agro.SensorIngest.Domain/
│           ├── Aggregates/         # SensorReadingAggregate
│           ├── Snapshots/          # SensorSnapshot, OwnerSnapshot
│           └── ValueObjects/       # AlertStatus, AlertSeverity, SensorStatus
└── test/
    └── TC.Agro.SensorIngest.Tests/
        ├── Application/
        │   ├── Handlers/           # Handler unit tests
        │   ├── Mappers/            # Mapper unit tests
        │   └── Validators/         # Validator unit tests
        ├── Domain/
        │   ├── Aggregates/         # Aggregate unit tests
        │   ├── Snapshots/          # Snapshot unit tests
        │   └── ValueObjects/       # Value object unit tests
        └── Service/
            ├── Endpoints/          # Endpoint tests
            └── Jobs/               # Job tests
```

## Observability

- **Metrics**: Prometheus-compatible via OpenTelemetry (ASP.NET Core, HTTP client, runtime, FusionCache, Npgsql)
- **Tracing**: Distributed tracing with correlation IDs propagated through middleware pipeline
- **Logging**: Structured logging via Serilog with correlation IDs
- **Health Checks**: `/health/live` and `/health/ready` endpoints (PostgreSQL, Redis, memory)
