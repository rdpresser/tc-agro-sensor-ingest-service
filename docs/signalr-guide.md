# SignalR - Sensor Ingest Service

Usage guide for the WebSocket (SignalR) integration for receiving real-time sensor readings and status changes.

## Overview

The `SensorHub` allows clients to receive sensor data in real time via SignalR. Data is grouped by **Plot** — when joining a plot group, the client receives all readings from sensors linked to that plot.

**Endpoint:** `/dashboard/sensorshub`
**Port:** `5003` (local)
**Authorization:** JWT Bearer — roles `Admin` or `Producer`

---

## Authentication

The connection requires a valid JWT token. The token is passed via query string automatically by the SignalR client.

### Obtaining the Token

```bash
curl -s -X POST http://localhost:5001/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tcagro.com","password":"Admin@123"}' | jq -r '.token'
```

Available users (seed):
| Email | Password | Role |
|---|---|---|
| `admin@tcagro.com` | `Admin@123` | Admin |
| `producer@tcagro.com` | `Producer@123` | Producer |

---

## Connection (JavaScript)

```javascript
const token = "your-jwt-token-here";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/dashboard/sensorshub", {
        accessTokenFactory: () => token
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

await connection.start();
```

### SignalR Client CDN

```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js"></script>
```

### NPM

```bash
npm install @microsoft/signalr
```

---

## Hub Methods (Client -> Server)

### `JoinPlotGroup(plotId: string)`

Joins a plot group to receive real-time readings. Upon joining, the hub automatically sends the **last 10 readings** for that plot.

```javascript
await connection.invoke("JoinPlotGroup", "dec770c5-d45d-4192-a2fe-8d0ea5de8484");
```

**Parameter:** Valid plot GUID (cannot be empty).
**Error:** Throws `HubException` if the plotId is invalid.

### `LeavePlotGroup(plotId: string)`

Leaves a plot group, stopping real-time readings.

```javascript
await connection.invoke("LeavePlotGroup", "dec770c5-d45d-4192-a2fe-8d0ea5de8484");
```

---

## Events (Server -> Client)

### `sensorReading`

Received when a new sensor reading is generated (every 2 minutes by the Quartz job, or via API).

```javascript
connection.on("sensorReading", (data) => {
    console.log("Reading:", data);
});
```

**Payload:**

```json
{
    "sensorId": "10c41afc-92f3-43c2-a12e-fc0254935423",
    "temperature": 28.5,
    "humidity": 65.2,
    "soilMoisture": 42.1,
    "timestamp": "2026-02-21T14:30:00+00:00"
}
```

| Field | Type | Description |
|---|---|---|
| `sensorId` | `string (GUID)` | Sensor ID |
| `temperature` | `number?` | Temperature in Celsius |
| `humidity` | `number?` | Relative humidity (%) |
| `soilMoisture` | `number?` | Soil moisture (%) |
| `timestamp` | `string (ISO 8601)` | Reading date/time |

> **Important:** Event names use **camelCase** (SignalR default). In C# the method is `SensorReading`, but in JS the listener must be `sensorReading`.

### `sensorStatusChanged`

Received when a sensor's operational status changes.

```javascript
connection.on("sensorStatusChanged", (data) => {
    console.log("Status changed:", data);
});
```

**Payload:**

```json
{
    "sensorId": "10c41afc-92f3-43c2-a12e-fc0254935423",
    "status": "Inactive"
}
```

| Field | Type | Description |
|---|---|---|
| `sensorId` | `string (GUID)` | Sensor ID |
| `status` | `string` | New status (`Active`, `Inactive`, `Maintenance`, `Faulty`) |

---

## Groups

Clients are organized into groups by plot using the format `plot:{plotId}`. When a reading is generated for a sensor, the system internally resolves the `PlotId` from the `sensor_snapshots` table and sends it to all clients in the corresponding group.

```
Sensor -> SensorSnapshot (PlotId) -> Group "plot:{plotId}" -> Connected clients
```

---

## Full Example

```html
<!DOCTYPE html>
<html>
<head>
    <title>Sensor Monitor</title>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js"></script>
</head>
<body>
    <div id="readings"></div>

    <script>
        const TOKEN = "your-jwt-token";
        const PLOT_ID = "dec770c5-d45d-4192-a2fe-8d0ea5de8484";

        async function start() {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/dashboard/sensorshub", {
                    accessTokenFactory: () => TOKEN
                })
                .withAutomaticReconnect()
                .build();

            // Listen for readings
            connection.on("sensorReading", (data) => {
                const div = document.getElementById("readings");
                div.innerHTML += `
                    <p>Sensor ${data.sensorId}:
                       Temp=${data.temperature}C
                       Hum=${data.humidity}%
                       Soil=${data.soilMoisture}%
                    </p>`;
            });

            // Listen for status changes
            connection.on("sensorStatusChanged", (data) => {
                console.log(`Sensor ${data.sensorId} -> ${data.status}`);
            });

            // Automatic reconnection
            connection.onreconnected(async () => {
                await connection.invoke("JoinPlotGroup", PLOT_ID);
            });

            // Connect and join group
            await connection.start();
            await connection.invoke("JoinPlotGroup", PLOT_ID);
        }

        start();
    </script>
</body>
</html>
```

---

## Test Page

A test page is available at:

```
http://localhost:5003/signalr-test.html
```

Features:
- Input for JWT token
- Connect/Disconnect from hub
- Join/Leave groups by Plot ID
- Log of all received events in real time

---

## Data Flow

```
+--------------+     +------------------+     +-------------+
|  Quartz Job  |---->| SensorHubNotifier|---->|  SensorHub  |
|  (2 min)     |     |                  |     |  (SignalR)  |
+--------------+     +------------------+     +------+------+
                              |                       |
                     Resolves PlotId via          Broadcasts to
                     SensorSnapshot table        plot:{plotId}
                                                      |
                                                      v
                                              +--------------+
                                              |   Connected  |
                                              |   Clients    |
                                              +--------------+
```

1. **SimulatedSensorReadingsJob** (Quartz) runs every 2 minutes
2. Generates readings with weather data (Open-Meteo API) for all active sensors
3. Persists to database (TimescaleDB)
4. Publishes integration event (RabbitMQ -> analytics-worker)
5. Calls `SensorHubNotifier.NotifySensorReadingAsync(sensorId, ...)`
6. The notifier resolves the `PlotId` from `sensor_snapshots` (cached via FusionCache)
7. Sends to the `plot:{plotId}` group via SignalR

---

## Troubleshooting

| Problem | Cause | Solution |
|---|---|---|
| 401 on connection | Token expired or invalid | Generate new token via `/api/identity/login` |
| 403 on connection | Incorrect role | Use a user with `Admin` or `Producer` role |
| Connected but not receiving data | Not in a group | Call `JoinPlotGroup` with the plot GUID |
| Joined group but not receiving | No active sensors in plot | Check `sensor_snapshots` in database |
| Events not showing in JS | Event name in PascalCase | Use `sensorReading` (camelCase) in listener |
| Old data not appearing | Normal | When joining a group, the last 10 readings are sent automatically |
